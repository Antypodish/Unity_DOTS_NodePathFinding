using UnityEngine ;
using System.Collections.Generic ;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public class ManagerJobs
    {
            
        /*
        [BurstCompile]
        struct KillBadAncestorsJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            // [DeallocateOnJobCompletion]
            public NativeArray <Entity> na_populationEntities ;

            public void Execute ( int i )
            {

                Entity populationEntity = na_populationEntities [i] ;
                ecbp.DestroyEntity ( i, populationEntity ) ;

            }

        }
        */

        [BurstCompile]
        public struct SetFirstGenerationJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;

            public void Execute ( int i )
            {
                Entity populationEntity = na_populationEntities [i] ;
                
                ecbp.AddComponent <NNIsFirstGenerationTag> ( i, populationEntity ) ;

            }

        }


        [BurstCompile]
        public struct SetFirstGenerationAsAncestorsJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray <Entity> na_populationEntities ;

            public void Execute ( int i )
            {

                Entity populationEntity = na_populationEntities [i] ;

                ecbp.AddComponent <NNIsPreviousGenerationTag> ( i, populationEntity ) ;
                ecbp.AddComponent <NNIsFinishedTag> ( i, populationEntity ) ;
                ecbp.RemoveComponent <NNIsFirstGenerationTag> ( i, populationEntity ) ;

            }

        }
        

        [BurstCompile]
        public struct InjectEllites2ParrentsJob : IJob
        {
            
            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer ecb ;

            // public int i_currentElitesCount ;

            [NativeDisableParallelForRestriction]
            public NativeArray <EntityIndex> na_elities ;

            [NativeDisableParallelForRestriction]
            public NativeArray <Entity> na_currentPopulationEntities ;
            [NativeDisableParallelForRestriction]
            public NativeArray <Entity> na_parentPopulationEntities ;

            [ReadOnly]
            public NativeMultiHashMap <int, EntityIndex> nmhm_parentEntitiesScore ;
            [ReadOnly]
            public NativeArray <int> na_parentKeysWithDuplicates ;

            [ReadOnly]
            public NativeArray <NNINdexProbabilityBuffer> na_currentEliteIndexProbability ;

            [ReadOnly]
            public ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore ;

            public Unity.Mathematics.Random random ;

            public void Execute ( )
            {

                NativeHashMap <int, bool> nhm_checkedEliteEntities = new NativeHashMap <int, bool> ( na_elities.Length, Allocator.Temp ) ;

                int i_parentUniqueKeyIndex                    = 0 ;
                bool isNextParentUniqueKey                    = true ;
                
                NativeMultiHashMapIterator <int> it = default ;

                for ( int i_eliteIndex = 0; i_eliteIndex < na_elities.Length; i_eliteIndex ++ )
                {

                    EntityIndex currentEntityIndex        = na_elities [i_eliteIndex] ;
                    Entity currentEliteEntity             = currentEntityIndex.entity ;

                    // Check if this entity has not been tested already.
                    if ( nhm_checkedEliteEntities.TryAdd ( currentEliteEntity.Index, true ) ) 
                    {
                        
                        int i_currentEntityIndex          = currentEntityIndex.i_index ;
                        int i_currentPopulationBrainScore = a_brainScore [currentEliteEntity].i ;

// Debug.Log ( "* Inject Elite: " + i_eliteIndex + " / " + na_currentEliteIndexProbability.Length + "; " + currentEliteEntity + "; with current score: " + i_currentPopulationBrainScore ) ;
// Debug.Log ( "* Inject Elite: " + i_probablity + " / " + i_perentageOfElites + "; " + eliteEntity + "; with current score: " + i_currentPopulationBrainScore ) ;
 
                        EntityIndex parentIndex = default ;

                        // Look up through parents' scores, starting from lowest score ascending.
                        if ( !isNextParentUniqueKey && nmhm_parentEntitiesScore.TryGetNextValue ( out parentIndex, ref it ) )
                        {
// Debug.Log ( "try get next value: " + parentIndex.entity + "; " + parentIndex.i_index ) ;
                        }
                        else
                        {
// Debug.LogWarning ( "try get but NO MORE." ) ;
                            isNextParentUniqueKey = true ;
                        }
                        
                        if ( isNextParentUniqueKey && nmhm_parentEntitiesScore.TryGetFirstValue ( na_parentKeysWithDuplicates [i_parentUniqueKeyIndex], out parentIndex, out it ) )
                        {
// Debug.LogWarning ( "try get first value: " + parentIndex.entity + "; " + parentIndex.i_index ) ;

                            i_parentUniqueKeyIndex ++ ;
                            isNextParentUniqueKey = false ;
                        }


                        // Parent is valid.
                        if ( !isNextParentUniqueKey && parentIndex.entity.Version > 0 )
                        {
                            int i_parentPopulationBrainScore = a_brainScore [parentIndex.entity].i ;

// Debug.Log ( "score: " + i_currentPopulationBrainScore + " >> " + i_parentPopulationBrainScore ) ;

                            if ( i_currentPopulationBrainScore > i_parentPopulationBrainScore )
                            {
                                // isBetterScore = true ;

                                na_parentPopulationEntities [parentIndex.i_index] = currentEliteEntity ;

// Debug.LogWarning ( "Parent index: " + parentIndex.i_index + " / " + na_parentPopulationEntities.Length + "; inject : " + currentEliteEntity + "; for: " + parentIndex.entity + "; score: " + i_currentPopulationBrainScore + " >> " + i_parentPopulationBrainScore ) ;
// Debug.LogWarning ( "Parent index: " + parent.i_index + " / " + na_parentPopulationEntities.Length + "; inject : " + eliteEntity + "; for: " + parent.entity + "; score: " + i_currentPopulationBrainScore + " >> " + i_parentPopulationBrainScore + "; index prob: " + i_indexProbability ) ;
                               
                                // Swap entities.
                                
                                na_currentPopulationEntities [i_currentEntityIndex] = parentIndex.entity ;
                                // na_elities [i_eliteIndex] = parent.entity ;
                                // na_currentPopulationEntities [i_indexProbability] = parent.entity ;
                            }

                        }
                        
                        if ( isNextParentUniqueKey || i_parentUniqueKeyIndex >= na_parentKeysWithDuplicates.Length )
                        {
// Debug.LogError ( "No more parent entities." ) ;
                            break ; // No more parent entities.
                        }

                    }

                } // for

                nhm_checkedEliteEntities.Dispose () ;
                // na_currentPopulationEntitiesCopy.Dispose () ;

            }

        }


        [BurstCompile]
        public struct SeParentGenerationJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;

            public void Execute ( int i )
            {
                Entity populationEntity = na_populationEntities [i] ;
                
                // Also checks, incase is parent already.
                ecbp.AddComponent <NNIsPreviousGenerationTag> ( i, populationEntity ) ;
                ecbp.AddComponent <NNIsFinishedTag> ( i, populationEntity ) ;

            }

        }
        

        [BurstCompile]
        public struct CalculateTotalScoresOfPopulationJob : IJob
        {

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;

            [NativeDisableParallelForRestriction]
            public NativeArray <int> na_totalScore ;

            [ReadOnly]
            public ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore ;

            public void Execute ()
            {

                int i_score = 0 ;

                for ( int i = 0; i < na_populationEntities.Length; i ++ )
                {

                    Entity populationEntity = na_populationEntities [i] ;

                    NNBrainScoreComponent brainScore = a_brainScore [populationEntity] ;

                    i_score += brainScore.i ;
// Debug.Log ( "* Current scoring: " + i + " / " + na_populationEntities.Length + "; in " + populationEntity + " >> " + brainScore.i + "; total: " + i_score ) ;
                } // for

                na_totalScore [0] = i_score ;
// Debug.LogWarning ( "* Total scoring: " + i_score ) ;

            }

        }

        
        [BurstCompile]
        public struct ReuseBrainsJob : IJobParallelFor
        {
            
            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;

            public void Execute ( int i )
            {

                Entity populationEntity                    = na_populationEntities [i] ;

                ecbp.RemoveComponent <NNIsPreviousGenerationTag> ( i, populationEntity ) ;
                ecbp.RemoveComponent <NNIsFinishedTag> ( i, populationEntity ) ;
                ecbp.RemoveComponent <IsInitializedTag> ( i, populationEntity ) ;
                ecbp.RemoveComponent <IsAliveTag> ( i, populationEntity ) ;
                ecbp.RemoveComponent <IsSpawningCompleteTag> ( i, populationEntity ) ;
                ecbp.RemoveComponent <Unity.Physics.PhysicsExclude> ( i, populationEntity ) ;
                // IsSpawningTag must be removed too.

            }

        }


// [BurstCompile] unable to execute with shared data component.
        public struct AssignManager2BrainJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;
            
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity <NNAssignedToManagerComponent> a_assignedToManager ;

            [ReadOnly]
            public Entity nnManagerEntity ;

            public void Execute ( int i )
            {

                Entity populationEntity = na_populationEntities [i] ;
                
                a_assignedToManager [populationEntity] = new NNAssignedToManagerComponent () { entity = nnManagerEntity } ;
                ecbp.SetSharedComponent ( i, populationEntity, new NNManagerSharedComponent () { i_entityIndex = nnManagerEntity.Index, i_entityVersion = nnManagerEntity.Version } ) ;

            }

        }


        [BurstCompile]
        public struct IsSpawningNownJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter ecbp ;

            [ReadOnly]
            public NativeArray <Entity> na_populationEntities ;

            public void Execute ( int i )
            {
                Entity populationEntity = na_populationEntities [i] ;
                
                ecbp.AddComponent <IsSpawningTag> ( i, populationEntity ) ;

            }

        }


    }

}