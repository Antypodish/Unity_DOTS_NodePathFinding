using UnityEngine ;
using System.IO ;
using System.Collections.Generic ;

using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public struct EntityIndex
    {
        public int i_index ;
        public Entity entity ;
    }

    public struct LayersNeuronCounts
    {
        /// <summary>
        /// Based on lasers count.
        /// </summary>
        public int i_inputLayerNeuronsCount ; 
        /// <summary>
        /// Required for movement
        /// </summary>
        public int i_outputLayerNeuronsCount ; 
        /// <summary>
        /// FOr now assumed, takes larges layer input, or output.
        /// Will be calcualted
        /// </summary>
        public int i_hiddenLayerNeuronsCount ; 
    }

}