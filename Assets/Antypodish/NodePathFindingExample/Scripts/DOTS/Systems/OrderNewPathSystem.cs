using UnityEngine ;

using Unity.Physics ;
using Unity.Physics.Systems ;
using Unity.Physics.Extensions ;

using Unity.Jobs ;
using Unity.Entities ;
using Unity.Collections ;
using Unity.Mathematics ;

using Antypodish.DOTS ;

using Antypodish.NodePathFinding.DOTS ;

namespace Antypodish.NodePathFindingExample.DOTS
{

    [AlwaysUpdateSystem]
    [UpdateAfter ( typeof ( FixedStepSimulationSystemGroup ))]
    [UpdateBefore ( typeof ( PathFindingSystem ) )]
    public class OrderNewPathSystem : SystemBase
    {
        
        BeginSimulationEntityCommandBufferSystem becb ;

        BuildPhysicsWorld  buildPhysicsWorld ;

        EntityQuery group_pathPlanners ;

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
            becb = World.GetOrCreateSystem <BeginSimulationEntityCommandBufferSystem> () ;
            
            buildPhysicsWorld = World.GetOrCreateSystem <BuildPhysicsWorld> () ;

            group_pathPlanners = EntityManager.CreateEntityQuery 
            (
                ComponentType.ReadOnly <IsAliveTag> (),
                ComponentType.Exclude <CanFindPathTag> (),

                ComponentType.ReadWrite <PathPlannerComponent> ()
            ) ;
            
            EntityArchetype entityArchetype = EntityManager.CreateArchetype
            (
                typeof ( IsAliveTag ),
                typeof ( PathPlannerComponent ),
                typeof ( PathPlannerWeightsMaskComponent ),
                typeof ( PathNodesBuffer ),
                
                typeof ( Prefab )
            ) ;

            // Example 
            {
                Entity pathPlannerPrefabEntity = EntityManager.CreateEntity ( entityArchetype ) ;
                EntityManager.SetName ( pathPlannerPrefabEntity, "PathPlannar" ) ;
                NativeArray <Entity> na_pathPLannerEntities = EntityManager.Instantiate ( pathPlannerPrefabEntity, 1, Allocator.Temp ) ;
                na_pathPLannerEntities.Dispose () ;
            
                EntityManager.SetName ( pathPlannerPrefabEntity, "PathPlannarPrefab" ) ;
            }

        }

        protected override void OnStartRunning ( )
        {
        }

        protected override void OnDestroy ( )
        {
        }

        protected override void OnUpdate ( )
        {
            

            int i_pathPlannerCount = group_pathPlanners.CalculateEntityCount () ;
            
            if ( i_pathPlannerCount == 0 ) return ;
            
            CollisionWorld collisionWorld        = buildPhysicsWorld.PhysicsWorld.CollisionWorld ;



            float3 f_pointerPosition             = Input.mousePosition ;
            UnityEngine.Ray pointerRay           = Camera.main.ScreenPointToRay ( f_pointerPosition ) ;

Debug.DrawLine ( pointerRay.origin, pointerRay.origin + pointerRay.direction * 200, Color.blue ) ;

            
            CollisionFilter collisionFilter = default ;
            collisionFilter.CollidesWith    = 1 << (int) CollisionFilters.ElevationNodes ; // Elevation Nodes.
            // collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Floor ; // Floor.
            // collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Walls ; // Walls.
            // collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Ramps ; // Ramps.
            // collisionFilter.CollidesWith += 1 << (int) CollisionFilters.Other ; // Other. // Optional

            var raycastInput = new RaycastInput
            {
                Start  = pointerRay.origin,
                End    = pointerRay.origin + pointerRay.direction * 200,
                Filter = CollisionFilter.Default
            } ;
                    
            // raycastInput.Filter.CollidesWith = 2 ; // Scores layer.
            raycastInput.Filter.CollidesWith = collisionFilter.CollidesWith ; // Barriers layer.
                    
            var collector = new IgnoreTransparentClosestHitCollector ( collisionWorld ) ;

            if ( collisionWorld.CastRay ( raycastInput, ref collector ) )
            {

                if ( Input.GetMouseButtonUp ( 0 ) || Input.GetMouseButtonUp ( 1 ) )
                {
   
Debug.Log ( "Hits: " + collector.ClosestHit.Entity + " @ pos: " + collector.ClosestHit.Position ) ;
                    
                    ComponentDataFromEntity <PathPlannerComponent> a_pathPlanners = GetComponentDataFromEntity <PathPlannerComponent> ( false ) ;
                    
                    NativeArray <Entity> na_pathPlanners = group_pathPlanners.ToEntityArray ( Allocator.TempJob ) ;
                    
                    Entity pathPlannersEntity ;
                    PathPlannerComponent pathPlanner ; ;
                    
                    // Test.
                    for ( int i = 0; i < na_pathPlanners.Length; i ++ )
                    {

                        pathPlannersEntity = na_pathPlanners [i] ;
                        pathPlanner        = a_pathPlanners [pathPlannersEntity] ;

                        if (  Input.GetMouseButtonUp ( 0 ) )
                        {
                            // A
                            pathPlanner.entityA = collector.ClosestHit.Entity ;

                        }
                        else if ( Input.GetMouseButtonUp ( 1 ) )
                        {
                            // B
                            pathPlanner.entityB = collector.ClosestHit.Entity ;
                        }

                        a_pathPlanners [pathPlannersEntity] = pathPlanner ; // Set back
                        
                    } // for
                    
                    pathPlannersEntity = na_pathPlanners [0] ;
                    pathPlanner        = a_pathPlanners [pathPlannersEntity] ;

                    na_pathPlanners.Dispose () ;

                    if ( pathPlanner.entityA.Version > 0 && pathPlanner.entityB.Version > 0 )
                    {
                        
                        EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;

Debug.Log ( "Start entity: " + pathPlanner.entityA + " target entity: " + pathPlanner.entityB ) ;
                        
                        Entities
                            .WithName ( "OrderPathSearchJob" )
                            .WithAll <IsAliveTag, PathPlannerComponent> ()
                            .ForEach ( ( Entity entity, int entityInQueryIndex ) => 
                        { 
                            ecbp.AddComponent <CanFindPathTag> ( entityInQueryIndex, entity ) ;
                        }).Schedule () ;

                        becb.AddJobHandleForProducer ( Dependency ) ;
                    }

                }

            }

        }

    }

}