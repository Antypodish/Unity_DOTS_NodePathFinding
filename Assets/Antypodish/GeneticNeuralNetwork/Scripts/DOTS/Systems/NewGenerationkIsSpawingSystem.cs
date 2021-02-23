using Unity.Jobs ;
using Unity.Entities ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{


    public class NewGenerationkIsSpawingSystem : SystemBase
    {

        protected override void OnCreate ( )
        {
        }

        protected override void OnUpdate ( )
        {
                        
            ComponentDataFromEntity <NNLayersNeuronsCountComponent> a_managerLayersNeuronsCount = GetComponentDataFromEntity <NNLayersNeuronsCountComponent> ( true ) ;
            
            Entities
                .WithName ( "NNResizeFirstGenerationBuffersJob" )
                .WithAll <NNBrainTag, IsSpawningTag> ()
                .WithNone <IsInitializedTag> ()
                .WithReadOnly ( a_managerLayersNeuronsCount )
                .ForEach ( ( ref DynamicBuffer <NNInputNeuronsValuesBuffer> a_inputLayerValues, ref DynamicBuffer <NNHiddenNeuronsValuesBuffer> a_hiddenLayerValues, ref DynamicBuffer <NNOutputNeuronsValuesBuffer> a_outputLayerValues, ref DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_input2hiddenLayerWeights, ref DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_hidden2OutputLayerWeights, ref NNBrainScoreComponent brainScore, in NNAssignedToManagerComponent assignedToManager ) =>
            {

                brainScore.f                 = 0 ;
                brainScore.i                 = 0 ;
                brainScore.triggeredByEntity = default ;
                
                NNLayersNeuronsCountComponent managerLayersNeuronsCount = a_managerLayersNeuronsCount [assignedToManager.entity] ;

                int i_inputLayerNeuronsCount                            = managerLayersNeuronsCount.i_inputLayerNeuronsCount ;
                int i_hiddenLayerNeuronsCount                           = managerLayersNeuronsCount.i_hiddenLayerNeuronsCount ;
                int i_outputLayerNeuronsCount                           = managerLayersNeuronsCount.i_outputLayerNeuronsCount ;

                a_inputLayerValues.ResizeUninitialized ( i_inputLayerNeuronsCount ) ;
                a_hiddenLayerValues.ResizeUninitialized ( i_hiddenLayerNeuronsCount ) ;
                a_outputLayerValues.ResizeUninitialized ( i_outputLayerNeuronsCount ) ;

                // a_hiddenLayersNeuronsBias.ResizeUninitialized ( i_hiddenLayerNeuronsCount ) ;

                a_input2hiddenLayerWeights.ResizeUninitialized ( _Input2HiddenLayerWeightsCount  ( i_inputLayerNeuronsCount, i_hiddenLayerNeuronsCount ) ) ;
                a_hidden2OutputLayerWeights.ResizeUninitialized ( _Hidden2OutputLayerWeightsCount ( i_outputLayerNeuronsCount, i_hiddenLayerNeuronsCount ) ) ;

            }).ScheduleParallel () ;
               
        }
    
        static public int _Input2HiddenLayerWeightsCount  ( int i_inputLayerNeuronsCount, int i_hiddenLayerNeuronsCount )
        {
            return i_inputLayerNeuronsCount * i_hiddenLayerNeuronsCount ;
        }
        
        static public int _Hidden2OutputLayerWeightsCount ( int i_outputLayerNeuronsCount, int i_hiddenLayerNeuronsCount )
        {
            return i_outputLayerNeuronsCount * i_hiddenLayerNeuronsCount ;
        }

    }

}