using UnityEngine ;

using Unity.Physics ;
using Unity.Physics.Systems ;
using Unity.Physics.Extensions ;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Collections ;
using Unity.Mathematics ;

using Antypodish.DOTS ;


namespace Antypodish.Hove.DOTS
{

    
    [UpdateAfter ( typeof ( FixedStepSimulationSystemGroup ))]
    public class PathPlanningSystem : SystemBase
    {

        BeginInitializationEntityCommandBufferSystem becb ;
        
        BuildPhysicsWorld  buildPhysicsWorld ;

        EntityQuery group_pathNodes ;

        

        protected override void OnCreate ( )
        {
            becb              = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
            
            buildPhysicsWorld = World.GetOrCreateSystem <BuildPhysicsWorld> () ;

            group_pathNodes = EntityManager.CreateEntityQuery 
            (
                ComponentType.ReadOnly <PathNodeTag> (),

                ComponentType.Exclude <IsAliveTag> ()
            ) ;

            
        }

        protected override void OnUpdate ( )
        {
            
            int i_pathNodesCount = group_pathNodes.CalculateEntityCount () ;
            
// Debug.Log ( string.Format ( "{0} AA ", i_pathNodesCount ) ) ;

            if ( i_pathNodesCount == 0 ) return ; // Early exit.

            NativeMultiHashMap <float, Entity> nmhm_pathNodesByElevation = default ;

            if ( !nmhm_pathNodesByElevation.IsCreated )
            {
                nmhm_pathNodesByElevation = new NativeMultiHashMap <float, Entity> ( i_pathNodesCount, Allocator.Persistent ) ;
            }

            EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;
            
            ComponentDataFromEntity <Translation> a_position = GetComponentDataFromEntity <Translation> ( true ) ;

            CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld ;
            
            Entities.WithName ( "SetPathNodesEleveationEntityJob" )
                .WithAll <PathNodeTag> ()
                .WithNone <IsInitializedTag> ()
                .ForEach ( ( Entity nodeEntity, ref PathNodeElevationComponent pathNodeElevation, in Translation position ) => 
            {
                pathNodeElevation.f = position.Value.y ;

                nmhm_pathNodesByElevation.Add ( pathNodeElevation.f, nodeEntity ) ;

            }).Schedule () ;


            Dependency = new AddPathLinksJob ()
            {
                collisionWorld            = collisionWorld,
                nmhm_pathNodesByElevation = nmhm_pathNodesByElevation,
                a_position                = a_position,
                a_pathNodeElevationLink   = GetComponentDataFromEntity <PathNodeElevationLinkComponent> ( true ),
                pathNodeLinksBuffer       = GetBufferFromEntity <PathNodeLinksBuffer> ( false )


            }.Schedule ( Dependency ) ;

            
            Entities.WithName ( "AddPathLinkElevationIfExistsJob" )
                .WithAll <PathNodeTag> ()
                .WithNone <IsInitializedTag> ()
                .WithReadOnly ( a_position )
                .ForEach ( ( ref DynamicBuffer <PathNodeLinksBuffer> a_pathNodeLinks, in PathNodeElevationLinkComponent pathNodeElevationLink, in Translation position ) => 
            {
                
                if ( pathNodeElevationLink.linkedEntity.Version > 0 )
                {
                    float3 f3_linkedNodePosition = a_position [pathNodeElevationLink.linkedEntity].Value ;
                    float3 f3_pathElevationLinkPosition = f3_linkedNodePosition;
                    float f_distance                    = math.length ( f3_linkedNodePosition - position.Value ) ;

                    a_pathNodeLinks.Add ( new PathNodeLinksBuffer () { f3 = f3_pathElevationLinkPosition, f_distance = f_distance, entity = pathNodeElevationLink.linkedEntity } ) ;

Debug.DrawLine ( position.Value, f3_pathElevationLinkPosition, Color.green, 5 ) ; // Length of ray, until hit collider.
                }

            }).Schedule () ;
            

            Entities.WithName ( "SetPathNodeAsInitializedJob" )
                .WithAll <PathNodeTag, PathNodeElevationComponent> ()
                .WithNone <IsInitializedTag> ()
                .ForEach ( ( Entity nodeEntity, int entityInQueryIndex ) => 
            {
                ecbp.AddComponent <IsAliveTag> ( entityInQueryIndex, nodeEntity ) ;
                ecbp.AddComponent <IsInitializedTag> ( entityInQueryIndex, nodeEntity ) ;

            }).ScheduleParallel () ;
            
            becb.AddJobHandleForProducer ( Dependency ) ;

            nmhm_pathNodesByElevation.Dispose ( Dependency ) ;
            
        }


        [BurstCompile]
        struct AddPathLinksJob : IJob
        {

            [ReadOnly]
            public CollisionWorld collisionWorld ;

            [ReadOnly]
            public NativeMultiHashMap <float, Entity> nmhm_pathNodesByElevation ;

            [ReadOnly]
            public ComponentDataFromEntity <Translation> a_position ;
            
            [ReadOnly]
            public ComponentDataFromEntity <PathNodeElevationLinkComponent> a_pathNodeElevationLink ;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity <PathNodeLinksBuffer> pathNodeLinksBuffer ;

            public void Execute ()
            {
                
                NativeArray <float> na_elevationAsKeys = nmhm_pathNodesByElevation.GetKeyArray ( Allocator.Temp ) ;

                na_elevationAsKeys.Sort () ;

                int i_uniqueKeys = na_elevationAsKeys.Unique () ;
                
                NativeArray <Entity> na_pathNodeOnEleveation = new NativeArray<Entity> ( na_elevationAsKeys.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;

                for ( int i = 0; i < i_uniqueKeys; i ++ )
                {

                    int i_pathNodeIndex = 0 ;

                    float f_elevationAsKey = na_elevationAsKeys [i] ;

                    Entity pathNodeEntity ;
                    NativeMultiHashMapIterator <float> it ;

                    // Iterate though nodes on given level.
                    if ( nmhm_pathNodesByElevation.TryGetFirstValue ( f_elevationAsKey, out pathNodeEntity, out it ) )
                    {
                        na_pathNodeOnEleveation [i_pathNodeIndex] = pathNodeEntity ;
 // Debug.Log ( i_uniqueKeys + "; New: " + pathNodeEntity ) ;
                        i_pathNodeIndex ++ ;

                    }
                
                    while ( nmhm_pathNodesByElevation.TryGetNextValue ( out pathNodeEntity, ref it ) )
                    {
                        na_pathNodeOnEleveation [i_pathNodeIndex] = pathNodeEntity ;

// Debug.Log ( i_uniqueKeys + "; Next: " + pathNodeEntity ) ;
                        i_pathNodeIndex ++ ;
                    }

                    
                    NativeList <Unity.Physics.RaycastHit> nl_allHits = new NativeList <Unity.Physics.RaycastHit> ( i_pathNodeIndex, Allocator.Temp ) ;
                    
                    CollisionFilter collisionFilter = default ;
                    collisionFilter.CollidesWith = 1 << 9 ; // Elevation Nodes.
                    collisionFilter.CollidesWith += 1 << 1 ; // Floor.
                    collisionFilter.CollidesWith += 1 << 2 ; // Walls.
                    collisionFilter.CollidesWith += 1 << 3 ; // Ramps.

                    // Iterate though nodes on a given level.
                    for ( int j = 0; j < i_pathNodeIndex; j ++ )
                    {

                        pathNodeEntity                                      = na_pathNodeOnEleveation [j] ;
                        float3 f3_pathNodePosition                          = a_position [pathNodeEntity].Value ;

                        DynamicBuffer <PathNodeLinksBuffer> a_pathNodeLinks = pathNodeLinksBuffer [pathNodeEntity] ;

                        // Lookup for linked near nodes on a given level.
                        for ( int k = 0; k < i_pathNodeIndex; k ++ )
                        {

                            Entity targetPathNode = na_pathNodeOnEleveation [k] ;
                            float3 f3_endPoint    = a_position [targetPathNode].Value ;

                            var raycastInput = new RaycastInput
                            {
                                Start  = f3_pathNodePosition,
                                End    = f3_endPoint,
                                Filter = CollisionFilter.Default
                            } ;
                    
                            // raycastInput.Filter.CollidesWith = 2 ; // Scores layer.
                            raycastInput.Filter.CollidesWith = collisionFilter.CollidesWith ; // Barriers layer.
                    
                            // var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;

                            nl_allHits.Clear () ;

                            if ( collisionWorld.CastRay ( raycastInput, ref nl_allHits ) )
                            // if ( collisionWorld.CastRay ( raycastInput, ref collector ) )
                            {
                                // Unity.Physics.RaycastHit closestHit = collector..ClosestHit ;
 
// Ray to any next node.
Debug.DrawLine ( f3_pathNodePosition, f3_endPoint, Color.grey, 2 ) ;                    

                                float f_closestHitDistance = 999999 ;
                                float3 f3_closestPosition  = 0 ;
                                Entity closestEntity       = default ;

                                // Get closest hit.
                                for ( int l = 0; l < nl_allHits.Length; l ++ ) 
                                {

                                    Unity.Physics.RaycastHit hit = nl_allHits [l] ;

                                    float f_distance = math.lengthsq ( hit.Position - f3_pathNodePosition ) ;
                                    // Debug.Log ( "#" + k + "; f: " + f ) ;



                                    if ( f_distance > 1 && f_distance <= f_closestHitDistance )
                                    {
                                        // Debug.Log ( "Closest hit: " + hit.point + "; at distance: " + hit.distance ) ;
                                        // closestHit = hit ;

                                        f_closestHitDistance = f_distance ;
                                        f3_closestPosition   = hit.Position ;
                                        closestEntity        = hit.Entity ;

                                    }
                                    
                                } // for
                                

                                // Closest hit is found.
                                if ( closestEntity.Index == targetPathNode.Index )
                                {
Debug.DrawLine ( f3_pathNodePosition, f3_closestPosition, Color.green, 5 ) ; // Length of ray, until hit collider.

                                    a_pathNodeLinks.Add ( new PathNodeLinksBuffer () { f3 = f3_closestPosition, f_distance = math.sqrt ( f_closestHitDistance ), entity = closestEntity } ) ;

// Debug.Log ( "closest hit: " + f3_closestPosition + "; distance: " + f_closestHitDistance + "; entity: " + closestEntity ) ;
                                }
                                else if ( closestEntity.Index > 0 )
                                {
Debug.DrawLine ( f3_pathNodePosition, f3_closestPosition, Color.red, 3 ) ; // Length of ray, until hit collider.
                                }

                            }

                        } // for

                    } // for
                    
                } // for

            }

        }

    }

}