using UnityEngine ;
using System.Collections.Generic ;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public class CommonJobs
    {

        [BurstCompile]
        public struct GetPopulationScoreJob : IJobParallelFor
        {
            
            public bool canGetEachScore ;

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;
            [ReadOnly]
            public ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore ;
            
            [NativeDisableParallelForRestriction]
            public NativeMultiHashMap <int, EntityIndex>.ParallelWriter nmhm_populationEntitiesScore ;

            public void Execute ( int i )
            {

                Entity entiy = na_populationEntities [i] ;
                int i_score = a_brainScore [entiy].i ;

// Debug.Log ( "Try add score: " + entiy + "; " + i_score ) ;
                if ( canGetEachScore || i_score > 0 )
                {
                    nmhm_populationEntitiesScore.Add ( i_score, new EntityIndex () { entity = entiy, i_index = i } ) ;
                }

            }

        }


        [BurstCompile]
        public struct GetElitesEntitiesJob : IJob
        {
            
            public int i_eltiesCount ;
            
            [NativeDisableParallelForRestriction]
            public NativeArray <EntityIndex> na_elities ;

            [ReadOnly]
            public NativeMultiHashMap <int, EntityIndex> nmhm_entitiesScore ;
            [ReadOnly]
            public NativeArray <int> na_currentSortedKeysWithDuplicates ;

            public void Execute ()
            {
        
                int i_nextBestUniqueKeyIndex                         = i_eltiesCount - 1 ;
                bool canCheckForElite                                = i_nextBestUniqueKeyIndex >= 0 ? true : false ;
                int i_eliteIndex                                     = 0 ;
                bool isNextUniqueKey                                 = true ;
                        
                EntityIndex parentEntityScore                       = default ;
                NativeMultiHashMapIterator <int> it                  = default ;

                while ( canCheckForElite )
                {

                    if ( !isNextUniqueKey && nmhm_entitiesScore.TryGetNextValue ( out parentEntityScore, ref it ) )
                    {
// Debug.Log ( "try get nexxt value: " + parent.entity + "; " + parent.i_index ) ;
                    }
                    else
                    {
// Debug.LogWarning ( "try get but NO MORE." ) ;
                        isNextUniqueKey = true ;
                    }
                        
                    if ( isNextUniqueKey && nmhm_entitiesScore.TryGetFirstValue ( na_currentSortedKeysWithDuplicates [i_nextBestUniqueKeyIndex], out parentEntityScore, out it ) )
                    {
// Debug.LogWarning ( "try get first value: " + parent.entity + "; " + parent.i_index ) ;

                        i_nextBestUniqueKeyIndex -- ;
                        isNextUniqueKey = false ;
                    }

                    na_elities [i_eliteIndex] = parentEntityScore ;
                    i_eliteIndex ++ ;

                    if ( i_eliteIndex >= i_eltiesCount )
                    {
                        // Stop lookup.
                        canCheckForElite = false ;
                    }
                            
                } // while

            }

        }


    }

}