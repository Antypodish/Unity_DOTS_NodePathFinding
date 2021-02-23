using System.Collections.Generic ;

using UnityEngine;
using System.Collections;

using Unity.Jobs ;
using Unity.Burst ;
using Unity.Entities ;
using Unity.Mathematics ;
using Unity.Collections ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{


    [UpdateAfter ( typeof ( NewGenerationkIsSpawingSystem ))]
    public class DNACrossOvereSystem : SystemBase
    {
        
        BeginSimulationEntityCommandBufferSystem becb ;
        
        
        EntityQuery group_MMMamager ;
        EntityQuery group_parentPopulation ;
        EntityQuery group_offspringPopulation ;

        private List <NNManagerSharedComponent> l_managerSharedData ;
            

        Unity.Mathematics.Random random ;
        
        protected override void OnCreate ( )
        {
            
            becb = World.GetOrCreateSystem <BeginSimulationEntityCommandBufferSystem> () ;

            group_MMMamager = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               ComponentType.ReadOnly <NNMangerIsSpawningNewGenerationTag> ()
               // ComponentType.ReadOnly <NNManagerComponent> ()
            ) ;
            
            group_parentPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (), ...

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <NNIsPreviousGenerationTag> (),

               ComponentType.ReadOnly <NNManagerSharedComponent> ()
            ) ;

            group_offspringPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsSpawningTag> (),

               ComponentType.ReadOnly <NNBrainTag> (),

               ComponentType.ReadOnly <NNManagerSharedComponent> ()
            ) ;

            l_managerSharedData = new List <NNManagerSharedComponent> ( 1000 ) ;

        }

        protected override void OnStartRunning ( )
        {
            random = new Unity.Mathematics.Random ( (uint) ( 6000 + Time.ElapsedTime ) ) ;
            random.NextUInt4 () ;
        }
        protected override void OnDestroy ( )
        {
        }

        protected override void OnUpdate ( )
        {
            
            if ( group_MMMamager.CalculateChunkCount () == 0 )
            {
                Debug.LogWarning ( "There is no active manager." ) ;
                return ;
            }
            
            EntityCommandBuffer ecb                 = becb.CreateCommandBuffer ();
            EntityCommandBuffer.ParallelWriter ecbp = ecb.AsParallelWriter () ;
            
            
            l_managerSharedData.Clear () ;
            EntityManager.GetAllUniqueSharedComponentData ( l_managerSharedData ) ;

            ComponentDataFromEntity <NNManagerBestFitnessComponent> a_managerBestFitness                 = GetComponentDataFromEntity <NNManagerBestFitnessComponent> ( false ) ;
            ComponentDataFromEntity <NNManagerComponent> a_manager                                       = GetComponentDataFromEntity <NNManagerComponent> ( true ) ;
            ComponentDataFromEntity <NNScoreComponent> a_managerScore                                    = GetComponentDataFromEntity <NNScoreComponent> ( true ) ;
            
            ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore                                 = GetComponentDataFromEntity <NNBrainScoreComponent> ( true ) ;

            ComponentDataFromEntity <NNMangerIsSpawningNewGenerationTag> a_mangerIsSpawningNewGeneration = GetComponentDataFromEntity <NNMangerIsSpawningNewGenerationTag> ( false ) ;
            
            BufferFromEntity <NNInput2HiddenLayersWeightsBuffer> NNInput2HiddenLayersWeightsBuffer       = GetBufferFromEntity <NNInput2HiddenLayersWeightsBuffer> ( false ) ;
            BufferFromEntity <NNHidden2OutputLayersWeightsBuffer> NNHidden2OutputLayersWeightsBuffer     = GetBufferFromEntity <NNHidden2OutputLayersWeightsBuffer> ( false ) ;

            // BufferFromEntity <NNHiddenLayersNeuronsBiasBuffer> NNHiddenLayersNeuronsBiasBuffer           = GetBufferFromEntity <NNHiddenLayersNeuronsBiasBuffer> ( false ) ;
           
            
            // ComponentDataFromEntity <NNScoreComponent> a_managerScore                                           = GetComponentDataFromEntity <NNScoreComponent> ( true ) ;
            
            BufferFromEntity <NNINdexProbabilityBuffer> indexProbabilityBuffer                           = GetBufferFromEntity <NNINdexProbabilityBuffer> ( false ) ;
            
            // int i_validManagersCount                                                                     = 0 ;
            // bool canCalculateCrossovers                                                                  = false ;

            for ( int i = 0; i < l_managerSharedData.Count; i++ )
            {
                
                NNManagerSharedComponent mangerSharedComponent = l_managerSharedData [i] ;
                Entity nnManagerEntity                         = new Entity () { Index = mangerSharedComponent.i_entityIndex, Version = mangerSharedComponent.i_entityVersion } ;

                if ( a_mangerIsSpawningNewGeneration.HasComponent ( nnManagerEntity ) )
                {

                    group_parentPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;
                    group_offspringPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;
                    
                    NativeArray <Entity> na_parentPopulationEntities            = group_parentPopulation.ToEntityArray ( Allocator.TempJob ) ;
                    NativeArray <Entity> na_offspringPopulationEntities         = group_offspringPopulation.ToEntityArray ( Allocator.TempJob ) ;
                    
                    DynamicBuffer <NNINdexProbabilityBuffer> a_indexProbability = indexProbabilityBuffer [nnManagerEntity] ;

                    
                    NNScoreComponent managerScore                               = a_managerScore [nnManagerEntity] ; 
                    // int i_eliteScore                                            = managerScore.i ;
                    

                        
Debug.Log ( "Total score: " + managerScore.i + "; elite score: " + managerScore.i_elite ) ;

                    
                    if ( managerScore.i_elite <= 1 )
                    {
                        
                        Dependency = new CopyLastBestGenerationDNAJob ()
                        {
            
                            na_parentPopulationEntities      = na_parentPopulationEntities,
                            na_offspringPopulationEntities   = na_offspringPopulationEntities,

                            // na_indexProbability              = na_indexProbability,

                            input2HiddenLayersWeightsBuffer  = NNInput2HiddenLayersWeightsBuffer,
                            hidden2OutputLayersWeightsBuffer = NNHidden2OutputLayersWeightsBuffer,

                            // hiddenLayersNeuronsBiasBuffer    = NNHiddenLayersNeuronsBiasBuffer

                        }.Schedule ( na_parentPopulationEntities.Length, 256 , Dependency ) ;

                        Dependency.Complete () ;

                    }
                    else 
                    {

                        // New score is fine.

                        // Calculate index probability, to get best parents.
                        // Each entity indicies will be in the array, as many times, as many score has
                        // e.g. 
                        // 0th entity with 0 points won't be in the array
                        // 1st entity with 2 points will be 2 times
                        // nth entity with xth score will be xth times in the array
                        
                        NNManagerComponent manager                                     = a_manager [nnManagerEntity] ;

                        NativeMultiHashMap <int, EntityIndex> nmhm_parentEntitiesScore = new NativeMultiHashMap <int, EntityIndex> ( na_parentPopulationEntities.Length, Allocator.TempJob ) ;

// Debug.Log ( "crossover parent score" ) ;
                        Dependency = new CommonJobs.GetPopulationScoreJob ( )
                        {
                            canGetEachScore              = false,
                            na_populationEntities        = na_parentPopulationEntities, 
                            a_brainScore                 = a_brainScore, 

                            nmhm_populationEntitiesScore = nmhm_parentEntitiesScore.AsParallelWriter ()

                        }.Schedule ( na_parentPopulationEntities.Length, 256, Dependency ) ;

                        Dependency.Complete () ;

                        NativeArray <int> na_parentSortedKeysWithDuplicates             = nmhm_parentEntitiesScore.GetKeyArray ( Allocator.TempJob ) ;
                        // This stores key keys in order. But keeps first unique keys at the front of an array.
                        // Total array size matches of total elements.
                        na_parentSortedKeysWithDuplicates.Sort () ;
                        // Sorted.
                        int i_uniqueKeyCount                                            = na_parentSortedKeysWithDuplicates.Unique () ;

                        int i_eltieCountTemp                                            = (int) ( na_parentSortedKeysWithDuplicates.Length * manager.f_eliteSize ) ;
                        // Minimum elite size mus be met.
                        int i_eltiesCount                                               = i_eltieCountTemp > 0 ? i_eltieCountTemp : na_parentSortedKeysWithDuplicates.Length ;

                        if ( na_parentSortedKeysWithDuplicates.Length == 0 )
                        {
                            Debug.LogError ( "Not enough elites for training. Please increase population, or elites %." ) ;
                            
                            na_offspringPopulationEntities.Dispose () ;
                            na_parentPopulationEntities.Dispose () ;
                            nmhm_parentEntitiesScore.Dispose () ;
                            na_parentSortedKeysWithDuplicates.Dispose () ;

                            continue ;
                        }

                        NativeArray <EntityIndex> na_elities = new NativeArray <EntityIndex> ( i_eltiesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;
                        
                        DynamicBuffer <NNINdexProbabilityBuffer> a_eliteIndexProbability = indexProbabilityBuffer [nnManagerEntity] ;
                        int i_totalElitesScore                                           = managerScore.i_elite ;
                        a_eliteIndexProbability.ResizeUninitialized ( i_totalElitesScore ) ;

                        Dependency = new CommonJobs.GetElitesEntitiesJob ()
                        {

                            i_eltiesCount                      = i_eltiesCount,

                            na_elities                         = na_elities,
                            nmhm_entitiesScore                 = nmhm_parentEntitiesScore,
                            na_currentSortedKeysWithDuplicates = na_parentSortedKeysWithDuplicates

                        }.Schedule () ;

                        

                        Dependency = new CalculateIndexProbabilityOfPopulationJob ()
                        {

                                na_populationEntities = na_elities,

                                a_indexProbability    = a_eliteIndexProbability,

                                a_brainScore          = a_brainScore

                        }.Schedule ( Dependency ) ;
                    
                        
                        NativeArray <int> na_randomValues = new NativeArray <int> ( na_parentPopulationEntities.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory ) ;

                        random.NextInt2 () ;
                        Dependency = new RandomIntsJob ()
                        {
            
                            na_randomValues = na_randomValues,
                            random          = random

                        }.Schedule ( Dependency ) ;

                        Dependency.Complete () ;

// Debug.LogError ( "parent pop: " + na_parentPopulationEntities.Length + "; offspring pop: " + na_offspringPopulationEntities.Length ) ;
                        Dependency = new DNACrossOverJob ()
                        {
            
                            na_parentPopulationEntities      = na_parentPopulationEntities,
                            na_offspringPopulationEntities   = na_offspringPopulationEntities,

                            na_indexProbability              = a_eliteIndexProbability.Reinterpret <int> ().AsNativeArray (),

                            input2HiddenLayersWeightsBuffer  = NNInput2HiddenLayersWeightsBuffer,
                            hidden2OutputLayersWeightsBuffer = NNHidden2OutputLayersWeightsBuffer,
                            
                            na_randomValues                  = na_randomValues,
                            random                           = random,

                            // i_eliteScore                     = i_eliteScore

                        }.Schedule ( na_parentPopulationEntities.Length, 256 , Dependency ) ;
                        
                        Dependency.Complete () ;
                        
                        na_randomValues.Dispose () ;
                        na_elities.Dispose () ;
                        nmhm_parentEntitiesScore.Dispose () ;
                        na_parentSortedKeysWithDuplicates.Dispose () ;

                    }

                    ecb.RemoveComponent <NNMangerIsSpawningNewGenerationTag> ( nnManagerEntity ) ;
                    becb.AddJobHandleForProducer ( Dependency ) ;
                    
                    
                    na_offspringPopulationEntities.Dispose () ;
                    na_parentPopulationEntities.Dispose () ;

                }
                
            } // for
            
            
            Entities
                .WithName ( "GenerationSpawningIsCompleteJob" )
                .WithAll <NNBrainTag, IsSpawningTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex ) =>
            {

                ecbp.RemoveComponent <IsSpawningTag> ( entityInQueryIndex, entity ) ;
                ecbp.AddComponent <IsSpawningCompleteTag> ( entityInQueryIndex, entity ) ;

            }).ScheduleParallel () ;

            becb.AddJobHandleForProducer ( Dependency ) ;
        
        }
        
        
        [BurstCompile]
        public struct RandomIntsJob : IJob
        {

            public NativeArray <int> na_randomValues ;
            
            public Unity.Mathematics.Random random ;

            public void Execute ( )
            {
                for ( int i = 0; i < na_randomValues.Length; i ++ )
                {
                    na_randomValues [i] = random.NextInt ( -999999, 999999 ) ;
                }
            }

        }


        [BurstCompile]
        public struct CalculateIndexProbabilityOfPopulationJob : IJob
        {

            [ReadOnly]
            public NativeArray <EntityIndex> na_populationEntities ;

            [NativeDisableParallelForRestriction]
            public DynamicBuffer <NNINdexProbabilityBuffer> a_indexProbability ;

            [ReadOnly]
            public ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore ;

            public void Execute ( )
            {
                // int i_actualIndex = 0 ;

                a_indexProbability.ResizeUninitialized ( 0 ) ;

                for ( int i_entityIndex = 0; i_entityIndex < na_populationEntities.Length; i_entityIndex ++ )
                {

                    Entity populationEntity              = na_populationEntities [i_entityIndex].entity ;

                    NNBrainScoreComponent brainScore     = a_brainScore [populationEntity] ;
                    
// Debug.Log ( "probability Current scoring: " + i_entityIndex + " / " + na_populationEntities.Length + "; entity: " + populationEntity + " >> " + brainScore.i + "; actual: " + i_actualIndex ) ;

                    bool canStopCheckingIndexProbability = false ;

                    for ( int j = 0; j < brainScore.i; j++ )
                    {

                        // canStopCheckingIndexProbability = i_actualIndex >= a_indexProbability.Length ;

                        if ( canStopCheckingIndexProbability ) break ;

                        a_indexProbability.Add ( new NNINdexProbabilityBuffer () { i = i_entityIndex } ) ;
                        // a_indexProbability [i_actualIndex] = new NNINdexProbabilityBuffer () { i = i_entityIndex } ;
                        // i_actualIndex ++ ;

                    }
                        
                    // if ( canStopCheckingIndexProbability ) break ;

                } // for

            }

        }

        /// <summary>
        /// Select randomly each synapsys weight from parent A or B.
        /// </summary>
        [BurstCompile]
        struct DNACrossOverJob : IJobParallelFor
        {
            
            [ReadOnly]
            public NativeArray <Entity> na_parentPopulationEntities ;
            [ReadOnly]
            public NativeArray <Entity> na_offspringPopulationEntities ;
            
            [ReadOnly]
            public NativeArray <int> na_indexProbability ;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity <NNInput2HiddenLayersWeightsBuffer> input2HiddenLayersWeightsBuffer ;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <NNHidden2OutputLayersWeightsBuffer> hidden2OutputLayersWeightsBuffer ;
            
            [ReadOnly]
            public NativeArray <int> na_randomValues ;
            public Unity.Mathematics.Random random ;

            // public int i_eliteScore ;


            public void Execute ( int i )
            {

                int i_eliteProbablityCount    = na_indexProbability.Length ;

                random.InitState ( (uint) ( random.NextInt ( i_eliteProbablityCount ) + na_randomValues [i] ) ) ;
                random.NextInt2 () ;

                int i_firstPorbabilityIndex   = random.NextInt ( 0, i_eliteProbablityCount ) ;
                int i_secondPorbabilityIndex  = random.NextInt ( 0, i_eliteProbablityCount ) ;

                int i_firstParentEntityIndex  = na_indexProbability [ i_firstPorbabilityIndex ] ;
                int i_secondParentEntityIndex = na_indexProbability [ i_secondPorbabilityIndex ] ;

                    

                Entity firstParentEntity                                                                    = na_parentPopulationEntities [i_firstParentEntityIndex] ;
                Entity secondParentEntity                                                                   = na_parentPopulationEntities [i_secondParentEntityIndex] ;

                DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_firstParentInput2HiddenLayersWeights    = input2HiddenLayersWeightsBuffer [firstParentEntity] ;
                DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_firstParentHidden2OutputLayersWeights  = hidden2OutputLayersWeightsBuffer [firstParentEntity] ;
                    
                DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_secondParentInput2HiddenLayersWeights   = input2HiddenLayersWeightsBuffer [secondParentEntity] ;
                DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_secondParentHidden2OutputLayersWeights = hidden2OutputLayersWeightsBuffer [secondParentEntity] ;

                Entity offspringEntity                                                                      = na_offspringPopulationEntities [i] ;

                DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_offspringInput2HiddenLayersWeights      = input2HiddenLayersWeightsBuffer [offspringEntity] ;
                DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_offspringtHidden2OutputLayersWeights   = hidden2OutputLayersWeightsBuffer [offspringEntity] ;


                int i_input2HiddenLayersWeightsCount  = a_firstParentInput2HiddenLayersWeights.Length ;
                int i_hidden2OutputLayersWeightsCount = a_firstParentHidden2OutputLayersWeights.Length ;
                        
                a_offspringInput2HiddenLayersWeights.ResizeUninitialized ( i_input2HiddenLayersWeightsCount ) ;
                a_offspringtHidden2OutputLayersWeights.ResizeUninitialized ( i_hidden2OutputLayersWeightsCount ) ;

                // 50 / 50 chance to get trait (NN weights) from parent A or B.
                for ( int j = 0; j < a_offspringInput2HiddenLayersWeights.Length; j ++ )
                {
                    float f = random.NextFloat () < 0.5f ? a_firstParentInput2HiddenLayersWeights [j].f : a_secondParentInput2HiddenLayersWeights [j].f ;
                    a_offspringInput2HiddenLayersWeights [j] = new NNInput2HiddenLayersWeightsBuffer () { f = f } ;
                }

                for ( int j = 0; j < a_offspringtHidden2OutputLayersWeights.Length; j ++ )
                {
                    float f = random.NextFloat () < 0.5f ? a_firstParentHidden2OutputLayersWeights [j].f : a_secondParentHidden2OutputLayersWeights [j].f ;
                    a_offspringtHidden2OutputLayersWeights [j] = new NNHidden2OutputLayersWeightsBuffer () { f = f } ;
                }
             
            }

        }

        [BurstCompile]
        struct CopyLastBestGenerationDNAJob : IJobParallelFor
        {
            
            [ReadOnly]
            public NativeArray <Entity> na_parentPopulationEntities ;
            [ReadOnly]
            public NativeArray <Entity> na_offspringPopulationEntities ;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity <NNInput2HiddenLayersWeightsBuffer> input2HiddenLayersWeightsBuffer ;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <NNHidden2OutputLayersWeightsBuffer> hidden2OutputLayersWeightsBuffer ;
            
            public void Execute ( int i )
            {

                Entity firstParentEntity                                                                   = na_parentPopulationEntities [i] ;
                Entity offspringEntity                                                                     = na_offspringPopulationEntities [i] ;

                DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_firstParentInput2HiddenLayersWeights   = input2HiddenLayersWeightsBuffer [firstParentEntity] ;
                DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_firstParentHidden2OutputLayersWeights = hidden2OutputLayersWeightsBuffer [firstParentEntity] ;

                DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_offspringInput2HiddenLayersWeights     = input2HiddenLayersWeightsBuffer [offspringEntity] ;
                DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_offspringtHidden2OutputLayersWeights  = hidden2OutputLayersWeightsBuffer [offspringEntity] ;

                int i_input2HiddenLayersWeightsCount                                                       = a_firstParentInput2HiddenLayersWeights.Length ;
                int i_hidden2OutputLayersWeightsCount                                                      = a_firstParentHidden2OutputLayersWeights.Length ;

                a_offspringInput2HiddenLayersWeights.ResizeUninitialized ( i_input2HiddenLayersWeightsCount ) ;
                a_offspringtHidden2OutputLayersWeights.ResizeUninitialized ( i_hidden2OutputLayersWeightsCount ) ;

                for ( int j = 0; j < a_offspringInput2HiddenLayersWeights.Length; j ++ )
                {
                    a_offspringInput2HiddenLayersWeights [j] = new NNInput2HiddenLayersWeightsBuffer () { f = a_firstParentInput2HiddenLayersWeights [j].f } ;
                }

                for ( int j = 0; j < a_offspringtHidden2OutputLayersWeights.Length; j ++ )
                {
                    a_offspringtHidden2OutputLayersWeights [j] = new NNHidden2OutputLayersWeightsBuffer () { f = a_firstParentHidden2OutputLayersWeights [j].f } ;
                }

            }

        }

    }

}