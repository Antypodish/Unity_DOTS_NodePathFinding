using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;


namespace Antypodish.Hove.DOTS
{

    public class InitializePathsNetSystem : SystemBase
    {

        BeginInitializationEntityCommandBufferSystem becb ;


        protected override void OnCreate ( )
        {
            becb = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
        }

        protected override void OnUpdate ( )
        {
            
            EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;
            
            Entities.WithName ( "InitializePathNodeAsJob" )
                .WithAll <PathNodeTag> ()
                .WithNone <PathNodeElevationComponent> ()
                .ForEach ( ( Entity nodeEntity, int entityInQueryIndex ) => 
            {
                ecbp.AddComponent <PathNodeElevationComponent> ( entityInQueryIndex, nodeEntity ) ;
                ecbp.AddBuffer <PathNodeLinksBuffer> ( entityInQueryIndex, nodeEntity ) ;
                
            }).ScheduleParallel () ;
            
            becb.AddJobHandleForProducer ( Dependency ) ;

        }

    }

}