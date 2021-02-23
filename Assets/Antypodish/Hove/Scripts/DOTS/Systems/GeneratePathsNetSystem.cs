using UnityEngine ;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Transforms ;
using Unity.Collections ;
using Unity.Mathematics ;

using Antypodish.DOTS ;


namespace Antypodish.Hove.DOTS
{

    // [AlwaysUpdateSystem] // Why this is required to run system?
    // [UpdateInGroup ( typeof ( SimulationSystemGroup ) )]
    public class GeneratePathsNetSystem : SystemBase
    {

        BeginInitializationEntityCommandBufferSystem becb ;

        EntityQuery group_pathNodes ;

        

        protected override void OnCreate ( )
        {
            becb = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;

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
            
            
            Entities.WithName ( "SetPathNodesEleveationEntityJob" )
                .WithAll <PathNodeTag> ()
                .WithNone <IsInitializedTag> ()
                .ForEach ( ( Entity nodeEntity, ref PathNodeElevationComponent pathNodeElevation, in Translation position ) => 
            {
                pathNodeElevation.f = position.Value.y ;

                nmhm_pathNodesByElevation.Add ( pathNodeElevation.f, nodeEntity ) ;
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

    }

}