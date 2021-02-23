using UnityEngine ;
using System.Collections.Generic ;

using Unity.Jobs ;
using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public class ManagerJobsMethods
    {
        
        public static void _SetFirstGeneration ( ref SystemBase systemBase, ref JobHandle jobHandle, ref EntityCommandBuffer.ParallelWriter ecbp, in DynamicBuffer <NNPNewPopulationBuffer> a_newPopulation, Entity managerEntity )
        {
            
            jobHandle = new ManagerJobs.SetFirstGenerationJob ()
            {

                ecbp                  = ecbp,
                na_populationEntities = a_newPopulation.Reinterpret <Entity> ().AsNativeArray ()

            }.Schedule ( a_newPopulation.Length, 256, jobHandle ) ;
                        
            jobHandle = new ManagerJobs.AssignManager2BrainJob ()
            {

                ecbp                   = ecbp,
                na_populationEntities = a_newPopulation.Reinterpret <Entity> ().AsNativeArray (),
                a_assignedToManager   = systemBase.GetComponentDataFromEntity <NNAssignedToManagerComponent> ( false ),
                nnManagerEntity       = managerEntity

            }.Schedule  ( a_newPopulation.Length, 256, jobHandle ) ;

        }

        
        public static void _SpawningGeneration ( ref SystemBase systemBase, ref JobHandle jobHandle, ref EntityCommandBuffer.ParallelWriter ecbp, in NativeArray <Entity> na_spawningNewGenerationEntities, Entity managerEntity )
        {
        
            jobHandle = new ManagerJobs.IsSpawningNownJob ()
            {
                ecbp                  = ecbp,
                na_populationEntities = na_spawningNewGenerationEntities
                            
            }.Schedule  ( na_spawningNewGenerationEntities.Length, 256, jobHandle ) ;

            jobHandle = new ManagerJobs.AssignManager2BrainJob ()
            {
                ecbp                  = ecbp,
                na_populationEntities = na_spawningNewGenerationEntities,
                a_assignedToManager   = systemBase.GetComponentDataFromEntity <NNAssignedToManagerComponent> ( false ),
                nnManagerEntity       = managerEntity
                            
            }.Schedule  ( na_spawningNewGenerationEntities.Length, 256, jobHandle ) ;

        }


        public static void _EvaluateElites ( ref JobHandle jobHandle, ref DynamicBuffer <NNINdexProbabilityBuffer> a_currentEliteIndexProbability, ref EntityCommandBuffer.ParallelWriter ecbp, ref EntityCommandBuffer ecb, ref Unity.Mathematics.Random random, ref NativeArray <EntityIndex> na_elities, ref NativeArray <Entity> na_currentPopulationEntities, ref NativeArray <Entity> na_parentPopulationEntities, in NativeMultiHashMap <int, EntityIndex> nmhm_parentEntitiesScore, in NativeArray <int> na_parentKeysWithDuplicates, in ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore )
        {

            // Grab % ellites selection from the probability group.
            if ( a_currentEliteIndexProbability.Length > 0 )
            {

// Debug.LogError ( "parent keys with duplicates: " + na_parentSortedKeysWithDuplicates.Length ) ;

                jobHandle = new GeneticNueralNetwork.DOTS.ManagerJobs.InjectEllites2ParrentsJob ( )
                {
                    ecb                                 = ecb,

                    // i_currentElitesCount                = i_eltiesCount,
                                
                    na_elities                          = na_elities,
                    na_currentPopulationEntities        = na_currentPopulationEntities,
                    na_parentPopulationEntities         = na_parentPopulationEntities,


                    // nhm_checkedEliteEntities         = new NativeHashMap <int, bool> ( i_perentageOfElites, Allocator.TempJob ),
                    nmhm_parentEntitiesScore            = nmhm_parentEntitiesScore,
                    na_parentKeysWithDuplicates         = na_parentKeysWithDuplicates,

                    na_currentEliteIndexProbability     = a_currentEliteIndexProbability.AsNativeArray (),

                    a_brainScore                        = a_brainScore,
                            
                    random                              = random

                }.Schedule ( jobHandle ) ;
                            
                // Dependency.Complete () ;
                            
                jobHandle = new GeneticNueralNetwork.DOTS.ManagerJobs.SeParentGenerationJob ()
                {
                    ecbp                  = ecbp,
                    na_populationEntities = na_parentPopulationEntities
                            
                }.Schedule ( na_parentPopulationEntities.Length, 256, jobHandle ) ;

            }

        }


        static public void _NextGeneration ( ref SystemBase systemBase, ref JobHandle jobHandle, ref BeginInitializationEntityCommandBufferSystem becb, ref EntityCommandBuffer.ParallelWriter ecbp, ref EntityCommandBuffer ecb, ref Unity.Mathematics.Random random, ref ManagerMethods.JsonNeuralNetworkMangers jsonNeuralNetworkMangers, ref NativeArray <int> na_totalScore, ref NativeArray <Entity> na_parentPopulationEntities, ref NativeArray <Entity> na_currentPopulationEntities, ref BufferFromEntity <NNINdexProbabilityBuffer> indexProbabilityBuffer, ref NNManagerComponent manager, ref NNManagerBestFitnessComponent managerBestFitness, ref NNScoreComponent managerScore, in ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore, Entity managerEntity, int i_activeManager, string s_path )
        {
            
            NativeMultiHashMap <int, EntityIndex> nmhm_parentEntitiesScore  = new NativeMultiHashMap <int, EntityIndex> ( na_currentPopulationEntities.Length, Allocator.TempJob ) ;
            NativeMultiHashMap <int, EntityIndex> nmhm_currentEntitiesScore = new NativeMultiHashMap <int, EntityIndex> ( na_parentPopulationEntities.Length, Allocator.TempJob ) ;


            _CalcuateTotalScore ( ref jobHandle, ref na_totalScore, ref nmhm_parentEntitiesScore, ref nmhm_currentEntitiesScore, in na_parentPopulationEntities, in na_currentPopulationEntities, in a_brainScore ) ; // , in manager ) ;

                    
            int i_currentPopulationTotalScore = na_totalScore [0] ;
            // managerScore.i                 = i_currentPopulationTotalScore ;
            int i_totalElitesScoreTemp        = (int) ( i_currentPopulationTotalScore * manager.f_eliteSize ) ;
            int i_currentPopulationTemp       = (int) ( na_currentPopulationEntities.Length * manager.f_eliteSize ) ;
            int i_totalElitesScore            = i_currentPopulationTotalScore <= i_currentPopulationTemp ? i_currentPopulationTotalScore : i_totalElitesScoreTemp ;
                        
Debug.Log ( "Current total score of pop: " + i_currentPopulationTotalScore + "; elite score: " + i_totalElitesScore ) ;
                        

            NativeArray <int> na_parentSortedKeysWithDuplicates = nmhm_parentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
            // This stores key keys in order. But keeps first unique keys at the front of an array.
            // Total array size matches of total elements.
            na_parentSortedKeysWithDuplicates.Sort () ;
            // Sorted.
            int i_parentUniqueKeyCount        = na_parentSortedKeysWithDuplicates.Unique () ;

// Debug.LogError ( "sorted keys: " + na_parentSortedKeysWithDuplicates.Length ) ;

                        

            NativeArray <int> na_currentSortedKeysWithDuplicates = nmhm_currentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
            // This stores key keys in order. But keeps first unique keys at the front of an array.
            // Total array size matches of total elements.
            na_currentSortedKeysWithDuplicates.Sort () ;
            // Sorted.
            int i_uniqueKeyCount              = na_currentSortedKeysWithDuplicates.Unique () ;

            int i_eltieCountTemp              = (int) ( na_currentSortedKeysWithDuplicates.Length * manager.f_eliteSize ) ;
            // Minimum elite size mus be met.
            int i_eltiesCount                 = i_eltieCountTemp <= i_totalElitesScoreTemp ? na_currentSortedKeysWithDuplicates.Length : i_eltieCountTemp ;
                    

            NativeArray <EntityIndex> na_elities = new NativeArray <EntityIndex> ( i_eltiesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;

            jobHandle = new CommonJobs.GetElitesEntitiesJob ()
            {
                i_eltiesCount                      = i_eltiesCount,

                na_elities                         = na_elities,
                nmhm_entitiesScore                 = nmhm_currentEntitiesScore,
                na_currentSortedKeysWithDuplicates = na_currentSortedKeysWithDuplicates

            }.Schedule ( jobHandle ) ;

//                    Dependency.Complete () ;
                        

            DynamicBuffer <NNINdexProbabilityBuffer> a_currentEliteIndexProbability = indexProbabilityBuffer [managerEntity] ;

            a_currentEliteIndexProbability.ResizeUninitialized ( i_totalElitesScore ) ;


Debug.Log ( "current pop total score: " + i_currentPopulationTotalScore + "; total elite score: " + i_totalElitesScore + "; elites count: " + i_eltiesCount + " of current population: " + na_currentPopulationEntities.Length ) ;


            _EvaluateElites ( ref jobHandle, ref a_currentEliteIndexProbability, ref ecbp, ref ecb, ref random, ref na_elities, ref na_currentPopulationEntities, ref na_parentPopulationEntities, in nmhm_parentEntitiesScore, in na_parentSortedKeysWithDuplicates, in a_brainScore ) ;

            // becb.AddJobHandleForProducer ( Dependency ) ;

            // Dependency.Complete () ;


            jobHandle = new ManagerJobs.CalculateTotalScoresOfPopulationJob ()
            {
                na_populationEntities = na_parentPopulationEntities,
                na_totalScore         = na_totalScore,

                a_brainScore          = a_brainScore

            }.Schedule ( jobHandle ) ;
                        
                        
            becb.AddJobHandleForProducer ( jobHandle ) ;
            jobHandle.Complete () ;


            
            int i_totalScore          = na_totalScore [0] ;
            managerScore.i            = i_totalScore ;
            managerScore.i_elite      = i_totalElitesScore ; 
                    
            int i_bestEntityIndex     = na_parentSortedKeysWithDuplicates [i_parentUniqueKeyCount-1] ;
            nmhm_parentEntitiesScore.TryGetFirstValue ( i_bestEntityIndex, out EntityIndex bestEntityIndex, out NativeMultiHashMapIterator <int> it ) ;
            
            na_parentPopulationEntities.Dispose () ;
            nmhm_parentEntitiesScore.Dispose () ;
            nmhm_currentEntitiesScore.Dispose () ;
            na_parentSortedKeysWithDuplicates.Dispose () ;
            na_currentSortedKeysWithDuplicates.Dispose () ; 

            managerBestFitness.i      = a_brainScore [bestEntityIndex.entity].i ;
            managerBestFitness.entity = bestEntityIndex.entity ;

            // Save elite brains.
            ManagerMethods._SaveDNA2File ( systemBase, in jsonNeuralNetworkMangers, in manager, in na_elities, i_activeManager, s_path ) ;

            na_elities.Dispose () ;

        }

        
        public static void _CalcuateTotalScore ( ref JobHandle jobHandle, ref NativeArray <int> na_totalScore, ref NativeMultiHashMap <int, EntityIndex> nmhm_parentEntitiesScore, ref NativeMultiHashMap <int, EntityIndex> nmhm_currentEntitiesScore, in NativeArray <Entity> na_parentPopulationEntities, in  NativeArray <Entity> na_currentPopulationEntities, in ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore ) // , in NNManagerComponent manager )
        {

            jobHandle = new CommonJobs.GetPopulationScoreJob ( )
            {
                canGetEachScore              = true,
                na_populationEntities        = na_parentPopulationEntities, 
                a_brainScore                 = a_brainScore, 

                nmhm_populationEntitiesScore = nmhm_parentEntitiesScore.AsParallelWriter ()

            }.Schedule ( na_parentPopulationEntities.Length, 256, jobHandle ) ;
                        
            jobHandle = new CommonJobs.GetPopulationScoreJob ( )
            {
                canGetEachScore              = false,
                na_populationEntities        = na_currentPopulationEntities, 
                a_brainScore                 = a_brainScore, 

                nmhm_populationEntitiesScore = nmhm_currentEntitiesScore.AsParallelWriter ()

            }.Schedule ( na_currentPopulationEntities.Length, 256, jobHandle ) ;

            jobHandle = new GeneticNueralNetwork.DOTS.ManagerJobs.CalculateTotalScoresOfPopulationJob ()
            {

                na_populationEntities = na_currentPopulationEntities,
                na_totalScore         = na_totalScore,

                a_brainScore          = a_brainScore

            }.Schedule ( jobHandle ) ;


            jobHandle.Complete () ;

            /*
            int i_currentPopulationTotalScore = na_totalScore [0] ;
            // managerScore.i                 = i_currentPopulationTotalScore ;
            int i_totalElitesScoreTemp        = (int) ( i_currentPopulationTotalScore * manager.f_eliteSize ) ;
            int i_currentPopulationTemp       = (int) ( na_currentPopulationEntities.Length * manager.f_eliteSize ) ;
            int i_totalElitesScore            = i_currentPopulationTotalScore <= i_currentPopulationTemp ? i_currentPopulationTotalScore : i_totalElitesScoreTemp ;
                        
Debug.Log ( "Current total score of pop: " + i_currentPopulationTotalScore + "; elite score: " + i_totalElitesScore ) ;
                        
            NativeArray <int> na_parentSortedKeysWithDuplicates = nmhm_parentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
            // This stores key keys in order. But keeps first unique keys at the front of an array.
            // Total array size matches of total elements.
            na_parentSortedKeysWithDuplicates.Sort () ;
            // Sorted.
            int i_parentUniqueKeyCount        = na_parentSortedKeysWithDuplicates.Unique () ;

// Debug.LogError ( "sorted keys: " + na_parentSortedKeysWithDuplicates.Length ) ;

                        
            NativeArray <int> na_currentSortedKeysWithDuplicates = nmhm_currentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
            // This stores key keys in order. But keeps first unique keys at the front of an array.
            // Total array size matches of total elements.
            na_currentSortedKeysWithDuplicates.Sort () ;
            // Sorted.
            int i_uniqueKeyCount              = na_currentSortedKeysWithDuplicates.Unique () ;

            int i_eltieCountTemp              = (int) ( na_currentSortedKeysWithDuplicates.Length * manager.f_eliteSize ) ;
            // Minimum elite size mus be met.
            int i_eltiesCount                 = i_eltieCountTemp <= i_totalElitesScoreTemp ? na_currentSortedKeysWithDuplicates.Length : i_eltieCountTemp ;
            */
        }

    }

}