using Unity.Entities;

namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public struct NNMangerIsSpawningNewGenerationTag : IComponentData {}

    public struct NNManagerBestFitnessComponent : IComponentData
    {
        public int i ;
        public Entity entity ;
        public int i_previousGeneration ;
        public Entity previousGenerationEntity ;
    }
    
    
    public struct NNScoreComponent : IComponentData 
    {
        public int i ;
        public int i_elite ;
        public int i_previousGeneration ;
    }
    

    public struct NNGenerationCountComponent : IComponentData 
    {
        public float i ;
    }

    public struct IsTimeUpTag : IComponentData {}

    public struct NNTimerComponent : IComponentData 
    {
        public float f ;
    }
    
    public struct NNLayersNeuronsCountComponent : IComponentData 
    {
        public int i_inputLayerNeuronsCount ;
        public int i_hiddenLayerNeuronsCount ;
        public int i_outputLayerNeuronsCount ;
    }
    
    /// <summary>
    /// Index to the population.
    /// </summary>
    [InternalBufferCapacity (0)]
    public struct NNINdexProbabilityBuffer : IBufferElementData
    {
        /// <summary>
        /// Index to the population.
        /// </summary>
        public int i ;
    }
    
    [InternalBufferCapacity (0)]
    public struct NNPNewPopulationBuffer : IBufferElementData
    {
        public Entity entity ;
    }

}