using UnityEngine ;

using Unity.Physics ;
using Unity.Physics.Systems ;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Collections ;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics ;

using Antypodish.DOTS ;


namespace Antypodish.NodePathFinding.DOTS
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

            PhysicsWorld physicsWorld     = buildPhysicsWorld.PhysicsWorld ;
            CollisionWorld collisionWorld = physicsWorld.CollisionWorld ;
            
            /*
            CollisionFilter collisionFilter = default ;
            collisionFilter.CollidesWith = 1 << (int) CollisionFilters.ElevationNodes ; // Elevation Nodes.
            collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Floor ; // Floor.
            collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Walls ; // Walls.
            collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Ramps ; // Ramps.
            collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Other ; // Other. // Optional
            */

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
                physicsWorld              = physicsWorld,
                collisionWorld            = collisionWorld,
                a_physicsCollider         = GetComponentDataFromEntity <PhysicsCollider> ( true ),
                // collisionFilter           = collisionFilter,
                nmhm_pathNodesByElevation = nmhm_pathNodesByElevation,
                a_position                = a_position,
                a_pathNodeLinkRange       = GetComponentDataFromEntity <PathNodeLinkRangeComponent> ( true ),
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
            public PhysicsWorld physicsWorld ;

            [ReadOnly]
            public CollisionWorld collisionWorld ;

            [ReadOnly]
            public ComponentDataFromEntity <PhysicsCollider> a_physicsCollider ;

            // [ReadOnly]
            // public CollisionFilter collisionFilter ;

            [ReadOnly]
            public NativeMultiHashMap <float, Entity> nmhm_pathNodesByElevation ;

            [ReadOnly]
            public ComponentDataFromEntity <Translation> a_position ;
            
            [ReadOnly]
            public ComponentDataFromEntity <PathNodeLinkRangeComponent> a_pathNodeLinkRange ;

            [ReadOnly]
            public ComponentDataFromEntity <PathNodeElevationLinkComponent> a_pathNodeElevationLink ;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity <PathNodeLinksBuffer> pathNodeLinksBuffer ;

            // Prevent memory allocation
            [NativeDisableContainerSafetyRestriction] 
            private NativeArray <CollisionFilter> na_collisionFilters ;

            [NativeDisableContainerSafetyRestriction] 
            NativeArray <Entity> na_pathNodeOnEleveation ;
            public void Execute ()
            {
                
                NativeArray <float> na_elevationAsKeys = nmhm_pathNodesByElevation.GetKeyArray ( Allocator.Temp ) ;

                na_elevationAsKeys.Sort () ;

                int i_elevationAsUniqueKeys = na_elevationAsKeys.Unique () ;
                
                na_pathNodeOnEleveation     = new NativeArray <Entity> ( na_elevationAsKeys.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;
                na_collisionFilters         = new NativeArray <CollisionFilter> ( na_elevationAsKeys.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory ) ;

                for ( int i_elevationIndex = 0; i_elevationIndex < i_elevationAsUniqueKeys; i_elevationIndex ++ )
                {

                    int i_pathNodeCount = 0 ;

                    float f_elevationAsKey = na_elevationAsKeys [i_elevationIndex] ;

                    Entity pathNodeEntity ;
                    NativeMultiHashMapIterator <float> it ;


                    // Iterate though nodes on given level.
                    if ( nmhm_pathNodesByElevation.TryGetFirstValue ( f_elevationAsKey, out pathNodeEntity, out it ) )
                    {
                        na_pathNodeOnEleveation [i_pathNodeCount] = pathNodeEntity ;

// Useful resources about physics collider.
// https://docs.unity3d.com/Packages/com.unity.physics@0.5/manual/core_components.html#modifying-physicscollider
// https://docs.unity3d.com/Packages/com.unity.physics@0.0/manual/collision_queries.html
                        PhysicsCollider physicsCollider           = a_physicsCollider [pathNodeEntity] ;
                        na_collisionFilters [i_elevationIndex]    = physicsCollider.Value.Value.Filter ;
                        
                    /*
RigidBody rb                    = physicsWorld.Bodies [hit.RigidBodyIndex] ;
CollisionFilter collisionFilter = rb.Collider.Value.Filter ;
Entity e                        = rb.Entity ;
                    
                                var col = new Unity.Physics.Collider () ; 
                                col.Filter
                     */

 // Debug.Log ( i_uniqueKeys + "; New: " + pathNodeEntity ) ;
                        i_pathNodeCount ++ ;

                    }

                
                    while ( nmhm_pathNodesByElevation.TryGetNextValue ( out pathNodeEntity, ref it ) )
                    {
                        na_pathNodeOnEleveation [i_pathNodeCount] = pathNodeEntity ;

                        PhysicsCollider physicsCollider           = a_physicsCollider [pathNodeEntity] ;
                        na_collisionFilters [i_elevationIndex]    = physicsCollider.Value.Value.Filter ;

// Debug.Log ( i_uniqueKeys + "; Next: " + pathNodeEntity ) ;
                        i_pathNodeCount ++ ;
                    }

                    NativeList <Unity.Physics.RaycastHit> nl_allHits = new NativeList <Unity.Physics.RaycastHit> ( i_pathNodeCount, Allocator.Temp ) ;
                    
                    

                    // Iterate though nodes on a given level.
                    for ( int i_pathNodeIndex = 0; i_pathNodeIndex < i_pathNodeCount; i_pathNodeIndex ++ )
                    {

                        pathNodeEntity                                      = na_pathNodeOnEleveation [i_pathNodeIndex] ;
                        float3 f3_pathNodePosition                          = a_position [pathNodeEntity].Value ;

                        DynamicBuffer <PathNodeLinksBuffer> a_pathNodeLinks = pathNodeLinksBuffer [pathNodeEntity] ;
                        PathNodeLinkRangeComponent pathNodeLinkRange        = a_pathNodeLinkRange [pathNodeEntity] ;

                        // If max range is less than 0, then any range is acceptable.
                        float f_maxRange                                    = pathNodeLinkRange.f_maxRange ; // < 0 ? math.INFINITY : pathNodeLinkRange.f_maxRange ;

                        // Lookup for linked near nodes on a given level.
                        for ( int k = 0; k < i_pathNodeCount; k ++ )
                        {

                            Entity targetPathNode           = na_pathNodeOnEleveation [k] ;
                            float3 f3_endPoint              = a_position [targetPathNode].Value ;


                            var raycastInput = new RaycastInput
                            {
                                Start  = f3_pathNodePosition,
                                End    = f_maxRange < 0 ? f3_endPoint : f3_pathNodePosition + math.normalize ( f3_endPoint - f3_pathNodePosition ) * f_maxRange,
                                Filter = CollisionFilter.Default
                            } ;
                    
                            CollisionFilter collisionFilter  = na_collisionFilters [i_elevationIndex] ;
                            raycastInput.Filter.CollidesWith = collisionFilter.BelongsTo ;
                            // raycastInput.Filter.BelongsTo    = collisionFilter.BelongsTo ;

                            // var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;

                            nl_allHits.Clear () ;

                            if ( collisionWorld.CastRay ( raycastInput, ref nl_allHits ) )
                            // if ( collisionWorld.CastRay ( raycastInput, ref collector ) )
                            {
                                // Unity.Physics.RaycastHit closestHit = collector..ClosestHit ;
 
// Ray to any next node.
Debug.DrawLine ( f3_pathNodePosition, f3_endPoint, Color.grey, 2 ) ;                    

                                float f_closestHitDistance          = math.INFINITY ;
                                float3 f3_closestNodePositionDebug  = 0 ;
                                Entity closestEntity                = new Entity () { Index = -1, Version = -1 } ; ;

                                // Get closest hit.
                                for ( int l = 0; l < nl_allHits.Length; l ++ ) 
                                {
                                    Unity.Physics.RaycastHit hit = nl_allHits [l] ;

                                   
                                    float3 f3_clostHitPosition = hit.Position ;
                                    float f_distance           = math.length ( f3_clostHitPosition - f3_pathNodePosition ) ;
                                    // Debug.Log ( "#" + k + "; f: " + f ) ;

                                    

                                    if ( f_distance > 1 && f_distance <= f_closestHitDistance ) // && f_distance < f_maxRange )
                                    {
                                        // Debug.Log ( "Closest hit: " + hit.point + "; at distance: " + hit.distance ) ;
                                        // closestHit = hit ;

                                        f_closestHitDistance        = f_distance ;
                                        f3_closestNodePositionDebug = f3_clostHitPosition ;
                                        closestEntity               = hit.Entity ;

                                    }
                                    
                                } // for
                                

                                // Closest hit is found.
                                if ( closestEntity.Index == targetPathNode.Index )
                                {
                                    
                                    float3 f3_hitNodePosition = a_position [closestEntity].Value ;

Debug.DrawLine ( f3_pathNodePosition, f3_hitNodePosition, Color.green, 5 ) ; // Length of ray, until hit collider.

                                    a_pathNodeLinks.Add ( new PathNodeLinksBuffer () { f3 = f3_hitNodePosition, f_distance = f_closestHitDistance, entity = closestEntity } ) ;

// Debug.Log ( "closest hit: " + f3_closestPosition + "; distance: " + f_closestHitDistance + "; entity: " + closestEntity ) ;
                                }
                                else if ( closestEntity.Index > 0 )
                                {
Debug.DrawLine ( f3_pathNodePosition, f3_closestNodePositionDebug, Color.red, 3 ) ; // Length of ray, until hit collider.
                                }

                            }

                        } // for

                    } // for
                    
                } // for

            }

        }

    }

}