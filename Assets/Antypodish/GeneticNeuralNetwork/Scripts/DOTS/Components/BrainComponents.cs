using System ;

using Unity.Entities;

namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public struct NNBrainTag : IComponentData {}

    public struct NNIsFirstGenerationTag : IComponentData {}
    public struct NNIsPreviousGenerationTag : IComponentData {}

    public struct NNIsFinishedTag : IComponentData {}

    public struct NNManagerSharedComponent : ISharedComponentData //, IEquatable <NNManagerSharedComponent>
    {
        public int i_entityIndex ;
        public int i_entityVersion ;

        /*
        public bool Equals ( NNManagerSharedComponent other )
        {
            return i_entityIndex == other.i_entityIndex &&
                i_entityVersion == other.i_entityVersion ;
        }
 
        public override int GetHashCode ()
        {
            int hash = 0;
            hash    ^= i_entityIndex.GetHashCode () ;
            hash    ^= i_entityVersion.GetHashCode () ;

            return hash ;
        }
        */

    }


    public struct NNAssignedToManagerComponent : IComponentData 
    {
        public Entity entity ;
    }

    public struct NNBrainScoreComponent : IComponentData 
    {
        public int i ;
        public float f ;
        public Entity triggeredByEntity ;
    }


    [InternalBufferCapacity (4)]
    public struct NNInputNeuronsValuesBuffer : IBufferElementData 
    {
        public float f ;
    }
    
    [InternalBufferCapacity (4)]
    public struct NNHiddenNeuronsValuesBuffer : IBufferElementData 
    {
        public float f ;
    }

    /// <summary>
    /// Output layer returns range values 0f 0f to 1f.
    /// </summary>
    [InternalBufferCapacity (2)]
    public struct NNOutputNeuronsValuesBuffer : IBufferElementData 
    {
        /// <summary>
        /// Output layer returns range values 0f 0f to 1f.
        /// </summary>
        public float f ;
    }
    
    /*
    /// <summary>
    /// It is assumed, that only 3 layers (input, hidden, outpu) are suffiecent.
    /// Pair 2 of them.
    /// </summary>
    [InternalBufferCapacity (4)]
    public struct NNHiddenLayersNeuronsBiasBuffer : IBufferElementData 
    {
        public float f ;
    }
    */

    /// <summary>
    /// It is assumed, that only 3 layers (input, hidden, outpu) are suffiecent.
    /// Pair 2 of them.
    /// </summary>
    [InternalBufferCapacity (4)]
    public struct NNInput2HiddenLayersWeightsBuffer : IBufferElementData 
    {
        public float f ;
    }

    /// <summary>
    /// It is assumed, that only 3 layers (input, hidden, outpu) are suffiecent.
    /// Pair 2 of them.
    /// </summary>
    [InternalBufferCapacity (4)]
    public struct NNHidden2OutputLayersWeightsBuffer : IBufferElementData 
    {
        public float f ;
    }

}