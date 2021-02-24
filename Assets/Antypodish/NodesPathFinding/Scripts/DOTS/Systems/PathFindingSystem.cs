using UnityEngine ;

using Unity.Physics ;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Collections ;
using Unity.Mathematics ;

using Antypodish.DOTS ;


namespace Antypodish.NodePathFinding.DOTS
{

    
    [UpdateAfter ( typeof ( FixedStepSimulationSystemGroup ))]
    public class PathFindingSystem : SystemBase
    {
        
        EndFixedStepSimulationEntityCommandBufferSystem eecb ;

        EntityQuery group_pathPlanners ;
        EntityQuery group_netNodes ;

        
        NativeHashMap <Entity, int> nhm_entityIndex ;
                
        bool isSystemInitialized ;

        /// <summary>
        /// Path finding will alternate between last visited buffer, to find next linked nodes.
        /// </summary>
        public struct LastVisitedPathNodes
        {
            public int i_index ;
            public float f_weight ;
            public Entity entity ;
        }
        
        public struct LastBestPath
        {
            public bool isBetterPathFound ;
            public float f_weight ;
        }

        protected override void OnCreate ( )
        {
            
            eecb = World.GetOrCreateSystem <EndFixedStepSimulationEntityCommandBufferSystem> () ;

            group_pathPlanners = EntityManager.CreateEntityQuery 
            (
                ComponentType.ReadOnly <IsAliveTag> (),
                ComponentType.ReadOnly <CanFindPathTag> (),

                ComponentType.ReadWrite <PathPlannerComponent> ()
            ) ;
            
            group_netNodes = EntityManager.CreateEntityQuery 
            (
                ComponentType.ReadOnly <IsAliveTag> (),

                ComponentType.ReadOnly <PathNodeTag> ()
            ) ;
            
            nhm_entityIndex     = default ;
            
            isSystemInitialized = false ;
        }

        protected override void OnStartRunning ( )
        {
        }

        protected override void OnDestroy ( )
        {
            nhm_entityIndex.Dispose () ;
        }

        protected override void OnUpdate ( )
        {
            
            if ( !isSystemInitialized )
            {
            
                NativeArray <Entity> na_netNodes = group_netNodes.ToEntityArray ( Allocator.Temp ) ;


                if ( na_netNodes.Length == 0 ) return ; // Early exit.

                isSystemInitialized = true ;

                // nhm_entityIndex.Dispose () ;
                nhm_entityIndex = new NativeHashMap <Entity, int> ( na_netNodes.Length, Allocator.Persistent ) ;

                // Map node entities to hash map.
                for ( int i = 0; i < na_netNodes.Length; i ++ )
                {
                    Entity nodeEntity = na_netNodes [i] ;
                    nhm_entityIndex.Add ( nodeEntity, i ) ;
                } // for

                na_netNodes.Dispose () ;

            }

            EntityCommandBuffer.ParallelWriter ecbp = eecb.CreateCommandBuffer ().AsParallelWriter () ;

            Dependency = new PathFindingJob ()
            {
                na_netNodes           = group_netNodes.ToEntityArray ( Allocator.TempJob ),
                            
                nhm_entityIndex       = nhm_entityIndex,
                a_pathNodesPosition   = GetComponentDataFromEntity <Translation> ( true ),
                pathNodeLinksBuffer   = GetBufferFromEntity <PathNodeLinksBuffer> ( true ),

                pathPlannersHandle    = GetComponentTypeHandle <PathPlannerComponent> ( false ),
                pathNodesBufferHandle = GetBufferTypeHandle <PathNodesBuffer> ( false ),


            }.Schedule ( group_pathPlanners, Dependency ) ;

            Entities
                .WithName ( "PathSearchedJob" )
                .WithAll <IsAliveTag, PathPlannerComponent, CanFindPathTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex ) => 
            { 
                ecbp.RemoveComponent <CanFindPathTag> ( entityInQueryIndex, entity ) ;
            }).Schedule () ;

            eecb.AddJobHandleForProducer ( Dependency ) ;

        }


        [BurstCompile]
        private struct PathFindingJob : IJobChunk
        {

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray <Entity> na_netNodes ;
            
            [ReadOnly]
            public NativeHashMap <Entity, int> nhm_entityIndex ;
                        
            [ReadOnly]
            public ComponentDataFromEntity <Translation> a_pathNodesPosition ;

            [ReadOnly]
            public BufferFromEntity <PathNodeLinksBuffer> pathNodeLinksBuffer ;

            [NativeDisableParallelForRestriction]
            public ComponentTypeHandle <PathPlannerComponent> pathPlannersHandle ;
            
            [NativeDisableParallelForRestriction]
            public BufferTypeHandle <PathNodesBuffer> pathNodesBufferHandle ;


            public void Execute ( ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex )
            { 

                NativeArray <PathPlannerComponent> na_pathPlanners = chunk.GetNativeArray ( pathPlannersHandle ) ;
                BufferAccessor <PathNodesBuffer> pathNodesBuffer   = chunk.GetBufferAccessor <PathNodesBuffer> ( pathNodesBufferHandle ) ;

                for ( int i = 0; i < na_pathPlanners.Length; i ++ )
                {

                    PathPlannerComponent pathPlanner            = na_pathPlanners [i] ;
                    DynamicBuffer <PathNodesBuffer> a_pathNodes = pathNodesBuffer [i] ;
                    a_pathNodes.ResizeUninitialized ( 0 ) ;

                    _EagerDijkstra_BestPath ( ref pathPlanner, ref a_pathNodes, in nhm_entityIndex, in na_netNodes, in pathNodeLinksBuffer, in a_pathNodesPosition ) ;

                }

            }

        }



        static private void _EagerDijkstra_BestPath ( ref PathPlannerComponent pathPlanner, ref DynamicBuffer <PathNodesBuffer> a_pathNodes, in NativeHashMap <Entity, int> nhm_entityIndex, in NativeArray <Entity> na_netNodes, in BufferFromEntity <PathNodeLinksBuffer> pathNodeLinksBuffer, in ComponentDataFromEntity <Translation> a_pathNodesPosition )
        {

            NativeArray <float> na_netNodesBestDistance2Node        = new NativeArray <float> ( na_netNodes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;
            NativeArray <bool> na_isNetNodesAlreadyVisited          = new NativeArray <bool> ( na_netNodes.Length, Allocator.Temp, NativeArrayOptions.ClearMemory ) ;
            NativeArray <int> na_previouslyVisitedByNodeIndex       = new NativeArray <int> ( na_netNodes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;

            Entity startingNodeEntity                               = pathPlanner.entityA ;
            Entity targetNodeEntity                                 = pathPlanner.entityB ;
            
            // Path finding will alternate between last visited buffer, to find next linked nodes.
            NativeArray <LastVisitedPathNodes> na_lastVisitedPathNodesA = new NativeArray <LastVisitedPathNodes> ( na_netNodes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;
            NativeArray <LastVisitedPathNodes> na_lastVisitedPathNodesB = new NativeArray <LastVisitedPathNodes> ( na_netNodes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;

            // Set initial infinity max distances for nodes.
            // If final results of nodes will stay infity, that means is unreachable.
            for ( int i = 0; i < na_netNodesBestDistance2Node.Length; i ++ )
            {
                na_netNodesBestDistance2Node [i] = math.INFINITY ;
            } // for
            
            
            nhm_entityIndex.TryGetValue ( startingNodeEntity, out int i_nodeIndex ) ;
            int i_lastVisitedPathNodesAIndex                        = 0 ;
            na_lastVisitedPathNodesA [i_lastVisitedPathNodesAIndex] = new LastVisitedPathNodes () { i_index = i_nodeIndex, entity = startingNodeEntity, f_weight = 0 } ;
            i_lastVisitedPathNodesAIndex ++ ;
            
            na_previouslyVisitedByNodeIndex [i_nodeIndex]           = -1 ; // Set first node, with no visiting other nodes.
            int i_lastVisitedPathNodesBIndex                        = 0 ;

            bool canLookupLinkNodes                                 = true ;
            bool alternateLastVisitedPathNodes                      = false ;

            // Target entity can not be the same as source entity, to search path.
            bool foundShortestPath                                  = false && startingNodeEntity.Index != targetNodeEntity.Index ;
            // bool isBestPathFound                                    = false ;

            LastBestPath lastBestPath                               = new LastBestPath () { isBetterPathFound = false, f_weight = math.INFINITY } ;


            // Lookup link nodes.
            while ( canLookupLinkNodes )
            {

                // canLookupLinkNodes = false ;

                int i_lastVisitedPathNodesTargetIndex ;

                if ( !alternateLastVisitedPathNodes )
                {
                    _LooUpNextLayer ( ref na_netNodesBestDistance2Node, ref na_isNetNodesAlreadyVisited, ref na_previouslyVisitedByNodeIndex, ref na_lastVisitedPathNodesA, ref i_lastVisitedPathNodesAIndex, ref na_lastVisitedPathNodesB, ref i_lastVisitedPathNodesBIndex, ref foundShortestPath, ref lastBestPath, in nhm_entityIndex, in pathNodeLinksBuffer, in a_pathNodesPosition, targetNodeEntity ) ;
                    i_lastVisitedPathNodesTargetIndex = i_lastVisitedPathNodesBIndex ;
                }
                else
                {
                    _LooUpNextLayer ( ref na_netNodesBestDistance2Node, ref na_isNetNodesAlreadyVisited, ref na_previouslyVisitedByNodeIndex, ref na_lastVisitedPathNodesB, ref i_lastVisitedPathNodesBIndex, ref na_lastVisitedPathNodesA, ref i_lastVisitedPathNodesAIndex, ref foundShortestPath, ref lastBestPath, in nhm_entityIndex, in pathNodeLinksBuffer, in a_pathNodesPosition, targetNodeEntity ) ;
                    i_lastVisitedPathNodesTargetIndex = i_lastVisitedPathNodesAIndex ;
                }


                alternateLastVisitedPathNodes = !alternateLastVisitedPathNodes ;

                canLookupLinkNodes = !lastBestPath.isBetterPathFound && i_lastVisitedPathNodesTargetIndex > 0 ;

            } // while


            
            nhm_entityIndex.TryGetValue ( targetNodeEntity, out int i_targetNodeIndex ) ;
            
            // Get path.
            if ( foundShortestPath )
            {

                int i_previousNodeIndex = i_targetNodeIndex ;

                float3 f3_previousPosition = 0 ;

                while ( na_previouslyVisitedByNodeIndex [i_previousNodeIndex] >= 0 )
                {
                    
                    Entity currentNodeEnity = na_netNodes [i_previousNodeIndex] ;

                    i_previousNodeIndex = na_previouslyVisitedByNodeIndex [i_previousNodeIndex] ;

                    Entity previousNodeEnity = na_netNodes [i_previousNodeIndex] ;

                    float3 f3_currentPosition  = a_pathNodesPosition [currentNodeEnity].Value ;
                    f3_previousPosition        = a_pathNodesPosition [previousNodeEnity].Value ;
// Debug.DrawLine ( f3_currentPosition, f3_previousPosition, Color.red, 7 ) ;
                    
                    a_pathNodes.Add ( new PathNodesBuffer () { f3_position = f3_currentPosition } ) ;

                }

                a_pathNodes.Add ( new PathNodesBuffer () { f3_position = f3_previousPosition } ) ;

            }

        }


        static private void _LooUpNextLayer ( ref NativeArray <float> na_netNodesBestDistance2Node, ref NativeArray <bool> na_isNetNodesAlreadyVisited, ref NativeArray <int> na_previouslyVisitedByNodeIndex, ref NativeArray <LastVisitedPathNodes> na_lastVisitedPathNodesSource, ref int i_lastVisitedPathNodesSourceIndex, ref NativeArray <LastVisitedPathNodes> na_lastVisitedPathNodesTarget, ref int i_lastVisitedPathNodesTargetIndex, ref bool foundShortestPath, ref LastBestPath lastBestPath, in NativeHashMap <Entity, int> nhm_entityIndex, in BufferFromEntity <PathNodeLinksBuffer> pathNodeLinksBuffer, in ComponentDataFromEntity <Translation> a_posDebug, Entity targetNodeEntity )
        {

            i_lastVisitedPathNodesTargetIndex = 0 ; // Reset.

            // Sort weights in ascending order.
            _BubbleSort ( ref na_lastVisitedPathNodesSource, i_lastVisitedPathNodesSourceIndex ) ;

            bool canLookForAnotherBestPath = false ;

            for ( int i = 0; i < i_lastVisitedPathNodesSourceIndex; i ++ )
            {

                LastVisitedPathNodes lastVisitedPathNodes           = na_lastVisitedPathNodesSource [i] ;

                int i_nodeIndex                                     = lastVisitedPathNodes.i_index ;
                na_isNetNodesAlreadyVisited [i_nodeIndex]           = true ;
                float f_weight2ThisNode                             = lastVisitedPathNodes.f_weight ;

                DynamicBuffer <PathNodeLinksBuffer> a_pathNodeLinks = pathNodeLinksBuffer [lastVisitedPathNodes.entity] ;

                bool isThisTargetNodeWithShortestPath = foundShortestPath && lastVisitedPathNodes.entity.Index == targetNodeEntity.Index ? true : false ;
                
float3 f3_currentNodePos = a_posDebug [lastVisitedPathNodes.entity].Value ;

                // Visit next linked nodes.
                for ( int j = 0; j < a_pathNodeLinks.Length; j ++ )
                {

                    PathNodeLinksBuffer pathNodeLinks = a_pathNodeLinks [j] ;
                    Entity nextNodeEntity             = pathNodeLinks.entity ;
                    float f_nextNodeDistance          = pathNodeLinks.f_distance ;

                    nhm_entityIndex.TryGetValue ( nextNodeEntity, out int i_nextNodeIndex ) ;
 
float3 f3_nextNodePos = a_posDebug [nextNodeEntity].Value ;

// Debug.DrawLine ( f3_currentNodePos, f3_nextNodePos, Color.white, 2 ) ;  
                    
                    float f_weight2NextNode = f_weight2ThisNode + f_nextNodeDistance ;

                    if ( nextNodeEntity.Index == targetNodeEntity.Index )
                    {
                        foundShortestPath = true ;
                        
// Debug.DrawLine ( f3_currentNodePos, f3_currentNodePos + math.normalize ( f3_nextNodePos - f3_currentNodePos ) * math.length ( f3_nextNodePos - f3_currentNodePos ) * 0.7f, Color.blue, 4 ) ; 
                        if ( f_weight2NextNode <= lastBestPath.f_weight )
                        {
                            lastBestPath.f_weight = f_weight2NextNode ;

// Debug.DrawLine ( f3_currentNodePos, f3_currentNodePos + math.normalize ( f3_nextNodePos - f3_currentNodePos ) * math.length ( f3_nextNodePos - f3_currentNodePos ) * 0.5f, Color.green, 5 ) ; 
                        }
                        
                    }
                    
                    if ( isThisTargetNodeWithShortestPath )
                    {
                        canLookForAnotherBestPath |= !na_isNetNodesAlreadyVisited [i_nextNodeIndex] ;

                        // Do not allow to follw path from target node.
                        // Only path to node is allowed.
                        if ( canLookForAnotherBestPath ) continue ; // Skip this node.
                    }


                    // Getting better distance to next node.
                    // If node is already not visited.
                    if ( !na_isNetNodesAlreadyVisited [i_nextNodeIndex] && f_weight2NextNode <= na_netNodesBestDistance2Node [i_nextNodeIndex] )
                    {
                        na_netNodesBestDistance2Node [i_nextNodeIndex] = f_weight2NextNode ;

                        na_previouslyVisitedByNodeIndex [i_nextNodeIndex] = i_nodeIndex ;

                        na_lastVisitedPathNodesTarget [i_lastVisitedPathNodesTargetIndex] = new LastVisitedPathNodes () { i_index = i_nextNodeIndex, entity = nextNodeEntity, f_weight = f_weight2NextNode } ;
                        i_lastVisitedPathNodesTargetIndex ++ ;

                    }

                } // for
                
                lastBestPath.isBetterPathFound = isThisTargetNodeWithShortestPath && !canLookForAnotherBestPath ;
                
                // Best path already found. No need for futher search.
                if ( lastBestPath.isBetterPathFound ) continue ;

            } // for

        }


        /// <summary>
        /// Sort weights in ascending order.
        /// </summary>
        /// <param name="a_lastVisitedPathNodes"></param>
        static private void _BubbleSort ( ref NativeArray <LastVisitedPathNodes> na_lastIVisitedPathNodes, int i_lastVisitedPathNodesSorceIndex )
        {
            
            int i_max = i_lastVisitedPathNodesSorceIndex - 1 ;

            bool canKeepSwapping = true ;

            while ( canKeepSwapping )
            {

                canKeepSwapping = false ;

                for ( int i = 0; i < i_max; i ++ )
                {

                    int i_nexIndex = i + 1 ;

                    LastVisitedPathNodes A = na_lastIVisitedPathNodes [i] ;
                    LastVisitedPathNodes B = na_lastIVisitedPathNodes [i_nexIndex] ;

                    float f_A = A.f_weight ;
                    float f_B = B.f_weight ;

                    // Swap.
                    if ( f_A > f_B )
                    {
                        A.f_weight = f_B ;
                        B.f_weight = f_A ;
                        na_lastIVisitedPathNodes [i] = A ;
                        na_lastIVisitedPathNodes [i_nexIndex] = B ;

                        canKeepSwapping = true ;
                    }

                }

            } // while

        }

    }

}