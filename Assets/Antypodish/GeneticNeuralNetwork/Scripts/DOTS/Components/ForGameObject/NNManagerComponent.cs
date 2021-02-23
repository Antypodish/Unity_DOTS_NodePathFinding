using UnityEngine ;
using Unity.Entities;

namespace Antypodish.GeneticNueralNetwork.DOTS
{

    [GenerateAuthoringComponent]
    public struct NNManagerComponent : IComponentData
    {
        
        [UnityEngine.Range (2,10000)]
        public int i_populationSize ;
        [UnityEngine.Range (0,1)]
        public float f_eliteSize ;
        // [UnityEngine.Range (1,10000)]
        public int i_startLifeTime ;
        // [UnityEngine.Range (1,10000)]
        public int i_maxLifeTime ;
        [UnityEngine.Range (1,100)]
        public int i_incrementLifeTime ;
        
        [UnityEngine.Range (0.1f,100.0f)]
        public float f_muatationRange ;

        [Header("First Group")]
        [Tooltip("% of population, which will use these parameters")]
        [UnityEngine.Range (0.0f,1.0f)]
        public float f_firstGroupSizeInPercentage ;
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationChance0 ;
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationRangeScale0 ;
        [UnityEngine.Range (0f,1f)]
        public float f_majorMutationChance0 ;
        // public float RadiansPerSecond;
        
        [Header("Second Group")]
        [UnityEngine.Range (0.0f,1.0f)]
        public float f_secondGroupSizeInPercentage ;
        [Tooltip("% of population, which will use these parameters")]
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationChance1 ;
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationRangeScale1 ;
        [UnityEngine.Range (0f,1f)]
        public float f_majorMutationChance1 ;
        
        [Header("Third Group")]
        [UnityEngine.Range (0.0f,1.0f)]
        public float f_thirdGroupSizeInPercentage ;
        [Tooltip("% of population, which will use these parameters")]
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationChance2 ;
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationRangeScale2 ;
        [UnityEngine.Range (0f,1f)]
        public float f_majorMutationChance2 ;

        [Header("Forth Group")]
        [UnityEngine.Range (0.1f,1.0f)]
        public float f_minorMutationChance3 ;
        [UnityEngine.Range (0f,1f)]
        public float f_minorMutationRangeScale3 ;
        [UnityEngine.Range (0f,1f)]
        public float f_majorMutationChance3 ;
        
        [Header("Storing / Restoring Data")]
        public bool f_readFromFile ;
        public bool f_write2File ;
    }

}