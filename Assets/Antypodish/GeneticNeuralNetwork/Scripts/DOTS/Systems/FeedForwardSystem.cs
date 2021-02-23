using Unity.Entities ;
using Unity.Mathematics ;

using Antypodish.DOTS ;

namespace Antypodish.GeneticNueralNetwork.DOTS
{
 
    [UpdateAfter ( typeof ( DNACrossOvereSystem ))]
    [UpdateAfter ( typeof ( NewGenerationkIsSpawingSystem ))]
    public class FeedForwardSystem : SystemBase
    {

        protected override void OnCreate ( )
        {

        }

        protected override void OnUpdate ( )
        {
            // Ouptut layer returns values of range 0f to 1f.
            Entities
                .WithName ( "NNFeedForwardJob" )
                .WithAll <NNBrainTag, IsAliveTag> ()
                .WithNone <NNIsFinishedTag> ()
                .ForEach ( ( ref DynamicBuffer <NNHiddenNeuronsValuesBuffer> a_hiddenLayerValues, ref DynamicBuffer <NNOutputNeuronsValuesBuffer> a_outputLayerValues, in DynamicBuffer <NNInputNeuronsValuesBuffer> a_inputLayerValues, in DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_input2hiddenLayerWeights, in DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_hidden2OutputLayerWeights ) =>
            {

                float f_bias = 0 ;

                // Pass forward inputs from input layer to hidden layer.
                for ( int i = 0; i < a_hiddenLayerValues.Length; i++ )
                {

                    float f_sum = 0 ;

                    int i_linkOffsetIndex = i * a_inputLayerValues.Length ;

                    for ( int j = 0; j < a_inputLayerValues.Length; j++ )
                    {
                        f_sum += a_input2hiddenLayerWeights [i_linkOffsetIndex + j].f * a_inputLayerValues [j].f  ;
                    }

                    f_sum += f_bias ;
                    a_hiddenLayerValues [i] = new NNHiddenNeuronsValuesBuffer () { f = math.max ( 0, f_sum ) } ;

                } // for 

                int i_synapsesCountBetweenHiddenAndOutputNeuron = a_hidden2OutputLayerWeights.Length ;


                // Pass forward values from hidden layer to output layer.
                for ( int i = 0; i < a_outputLayerValues.Length; i++ )
                {

                    float f_sum = 0 ;
                    
                    int i_linkOffsetIndex = i * a_hiddenLayerValues.Length ;
                    
                    for ( int j = 0; j < a_hiddenLayerValues.Length; j++ )
                    {
                        f_sum += a_hidden2OutputLayerWeights [i_linkOffsetIndex + j].f * a_hiddenLayerValues [j].f ;

// UnityEngine.Debug.Log ( "* i: " + i + " / " + j + "; " + a_hidden2OutputLayerWeights [i_linkOffsetIndex + j].f + " * " + a_hiddenLayerValues [j].f ) ;
                    }

                    a_outputLayerValues [i] = new NNOutputNeuronsValuesBuffer () { f = _Sigmoid ( f_sum ) } ;
                    
// UnityEngine.Debug.Log ( "sigmoid: " + i + "; " + f_sum + "; out: " + a_outputLayerValues [i].f ) ;

                } // for 

            } ).ScheduleParallel () ;

        }

        static private float _Sigmoid ( float input )
        {
            return 1 / (float) (1 + math.pow ( 2.71828182845904523536028747135f, -input ) ) ;
        }

    }

}