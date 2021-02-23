using Unity.Entities ;

using Antypodish.DOTS ;

namespace Antypodish.GeneticNueralNetwork.DOTS
{


    public class InitializeManagerSystem : SystemBase
    {

        BeginInitializationEntityCommandBufferSystem becb ;

        protected override void OnCreate ( )
        {
            becb = World.GetOrCreateSystem <BeginInitializationEntityCommandBufferSystem> () ;
        }

        protected override void OnUpdate ( )
        {

            EntityCommandBuffer.ParallelWriter ecbp = becb.CreateCommandBuffer ().AsParallelWriter () ;

            Entities
                .WithName ( "NNResizeFirstGenerationBuffersJob" )
                .WithAll <NNManagerComponent> ()
                .WithNone <IsAliveTag, IsInitializedTag> ()
                .ForEach ( ( Entity managerEntity, int entityInQueryIndex ) =>
            {
                
                ecbp.AddComponent ( entityInQueryIndex, managerEntity, new NNManagerBestFitnessComponent () { i = 0, i_previousGeneration = 0, entity = default, previousGenerationEntity = default } ) ;
                ecbp.AddComponent ( entityInQueryIndex, managerEntity, new NNScoreComponent () { i = 0, i_previousGeneration = 0 } ) ;
                
                ecbp.AddComponent ( entityInQueryIndex, managerEntity, new NNTimerComponent () { f = 0 } ) ;
                ecbp.AddComponent ( entityInQueryIndex, managerEntity, new NNGenerationCountComponent () { i = 0 } ) ;
                ecbp.AddComponent ( entityInQueryIndex, managerEntity, new NNLayersNeuronsCountComponent () { i_inputLayerNeuronsCount = 0, i_hiddenLayerNeuronsCount = 0, i_outputLayerNeuronsCount = 0 } ) ;

                DynamicBuffer <NNINdexProbabilityBuffer> a_probablity  = ecbp.AddBuffer <NNINdexProbabilityBuffer> ( entityInQueryIndex, managerEntity ) ;
                DynamicBuffer <NNPNewPopulationBuffer> a_newPopulation = ecbp.AddBuffer <NNPNewPopulationBuffer> ( entityInQueryIndex, managerEntity ) ;

                // Suspected minimum reaseanable capacity. May be lower, or bigger, depending on application.
                a_probablity.ResizeUninitialized ( 4096 ) ; 
                // a_probablity.ResizeUninitialized ( 0 ) ; // Then set back to 0.
                
// a_newPopulation.ResizeUninitialized ( 2048 ) ; // Error: Causes manager entity destruction?!
                // a_newPopulation.ResizeUninitialized ( 0 ) ; // Then set back to 0.

                ecbp.AddComponent <IsInitializedTag> ( entityInQueryIndex, managerEntity ) ;

            }).ScheduleParallel () ;
            
            becb.AddJobHandleForProducer ( Dependency ) ;

        }

    }

}