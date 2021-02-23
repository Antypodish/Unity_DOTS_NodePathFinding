using Unity.Jobs ;
using Unity.Entities ;
using Unity.Mathematics ;

using Antypodish.DOTS ;

namespace Antypodish.GeneticNueralNetwork.DOTS
{


    [UpdateAfter ( typeof ( NewGenerationkIsSpawingSystem ))]
    public class InitializePopulationSystem : SystemBase
    {

        EndSimulationEntityCommandBufferSystem eecb ;
        
        Unity.Mathematics.Random random ;


        protected override void OnCreate ( )
        {
            eecb = World.GetOrCreateSystem <EndSimulationEntityCommandBufferSystem> () ;

            random = new Unity.Mathematics.Random ( (uint) System.DateTime.UtcNow.Millisecond + 5000 ) ;
            random.NextInt2 () ;
        }

        protected override void OnUpdate ( )
        {

            EntityCommandBuffer.ParallelWriter ecbp = eecb.CreateCommandBuffer ().AsParallelWriter () ;
            
            this.random.NextInt2 () ;
            Unity.Mathematics.Random random = this.random ;
            
          
            ComponentDataFromEntity <NNManagerComponent> a_manager = GetComponentDataFromEntity <NNManagerComponent> ( true ) ;

            Entities
                .WithName ( "NNCreateFirstGenerationWeightsJob" )
                .WithAll <NNBrainTag, IsSpawningCompleteTag, NNIsFirstGenerationTag> ()
                .WithNone <IsInitializedTag> ()
                .WithReadOnly ( a_manager )
                .ForEach ( ( Entity entity, ref DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_input2hiddenLayerWeights, ref DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_hidden2OutputLayerWeights, in NNAssignedToManagerComponent assignedManager ) =>
            {
                
                
                random.InitState ( (uint) ( random.NextInt () + entity.Index ) ) ;
                random.NextInt2 () ;

                NNManagerComponent managerComponent = a_manager [assignedManager.entity] ;

                float f_muatationRange              = managerComponent.f_muatationRange ;
             
                // Initialize random weights.
                for ( int i = 0; i < a_input2hiddenLayerWeights.Length; i ++ )
                {
                    a_input2hiddenLayerWeights [i] = new NNInput2HiddenLayersWeightsBuffer () { f = random.NextFloat ( -f_muatationRange, f_muatationRange ) } ;
                }
                
                // Initialize random weights.
                for ( int i = 0; i < a_hidden2OutputLayerWeights.Length; i ++ )
                {
                    a_hidden2OutputLayerWeights [i] = new NNHidden2OutputLayersWeightsBuffer () { f = random.NextFloat ( -f_muatationRange, f_muatationRange ) } ;
                }

            }).ScheduleParallel () ;
            
            
            this.random.NextInt2 () ;
            random = this.random ;

            // DNA mutation.
            Entities
                .WithName ( "NNDNAMutationJob" )
                .WithAll <NNBrainTag, IsSpawningCompleteTag> ()
                .WithNone <IsInitializedTag, NNIsFirstGenerationTag> ()
                .WithReadOnly ( a_manager )
                .ForEach ( ( Entity entity, ref DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_offspringInput2HiddenLayersWeights, ref DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_offspringtHidden2OutputLayersWeights, in NNAssignedToManagerComponent assignedManager ) =>
            {
                
                random.InitState ( (uint) ( random.NextInt () + entity.Index ) ) ;
                random.NextInt2 () ;

                NNManagerComponent managerComponent = a_manager [assignedManager.entity] ;

                float f_range                       = managerComponent.f_muatationRange ;

                float f_groupSelection = random.NextFloat () ;

                if ( f_groupSelection <= managerComponent.f_firstGroupSizeInPercentage )
                {
                
                    float f_majorMutationChance         = managerComponent.f_majorMutationChance0 ;
                    float f_minorMutationChance         = managerComponent.f_minorMutationChance0 ;
                    float f_minorMutationRangeScale     = managerComponent.f_minorMutationRangeScale0 ;

                    _MutationChances ( ref a_offspringInput2HiddenLayersWeights, ref a_offspringtHidden2OutputLayersWeights, ref random, f_range, f_majorMutationChance, f_minorMutationChance, f_minorMutationRangeScale ) ;

                }
                else if ( f_groupSelection <= managerComponent.f_secondGroupSizeInPercentage )
                {
                    
                    float f_majorMutationChance         = managerComponent.f_majorMutationChance1 ;
                    float f_minorMutationChance         = managerComponent.f_minorMutationChance1 ;
                    float f_minorMutationRangeScale     = managerComponent.f_minorMutationRangeScale1 ;
                    
                    _MutationChances ( ref a_offspringInput2HiddenLayersWeights, ref a_offspringtHidden2OutputLayersWeights, ref random, f_range, f_majorMutationChance, f_minorMutationChance, f_minorMutationRangeScale ) ;

                }
                else if ( f_groupSelection <= managerComponent.f_thirdGroupSizeInPercentage )
                {
                    
                    float f_majorMutationChance         = managerComponent.f_majorMutationChance2 ;
                    float f_minorMutationChance         = managerComponent.f_minorMutationChance2 ;
                    float f_minorMutationRangeScale     = managerComponent.f_minorMutationRangeScale2 ;
                    
                    _MutationChances ( ref a_offspringInput2HiddenLayersWeights, ref a_offspringtHidden2OutputLayersWeights, ref random, f_range, f_majorMutationChance, f_minorMutationChance, f_minorMutationRangeScale ) ;

                }
                else
                {
                    
                    float f_majorMutationChance         = managerComponent.f_majorMutationChance2 ;
                    float f_minorMutationChance         = managerComponent.f_minorMutationChance2 ;
                    float f_minorMutationRangeScale     = managerComponent.f_minorMutationRangeScale2 ;
                    
                    _MutationChances ( ref a_offspringInput2HiddenLayersWeights, ref a_offspringtHidden2OutputLayersWeights, ref random, f_range, f_majorMutationChance, f_minorMutationChance, f_minorMutationRangeScale ) ;

                }

            }).ScheduleParallel () ;
            
            random.NextUInt2 () ;

            // DNA mutation.
            Entities
                .WithName ( "NNDNAMutationOfFirstGenerationJob" )
                .WithAll <NNBrainTag, IsSpawningCompleteTag, NNIsFirstGenerationTag> ()
                .WithNone <IsInitializedTag> ()
                .WithReadOnly ( a_manager )
                .ForEach ( ( Entity entity, ref DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_offspringInput2HiddenLayersWeights, ref DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_offspringtHidden2OutputLayersWeights, in NNAssignedToManagerComponent assignedManager ) =>
            {
                
                random.InitState ( (uint) ( random.NextInt () + entity.Index ) ) ;
                random.NextInt2 () ;

                NNManagerComponent managerComponent = a_manager [assignedManager.entity] ;

                float f_range                       = managerComponent.f_muatationRange ;

                for ( int i = 0; i < a_offspringInput2HiddenLayersWeights.Length; i ++ )
                {
                    float f = random.NextFloat ( -f_range, f_range ) ;
                    a_offspringInput2HiddenLayersWeights [i] = new NNInput2HiddenLayersWeightsBuffer () { f = f } ;
                }

                for ( int i = 0; i < a_offspringtHidden2OutputLayersWeights.Length; i ++ )
                {
                    float f = random.NextFloat ( -f_range, f_range ) ;
                    a_offspringtHidden2OutputLayersWeights [i] = new NNHidden2OutputLayersWeightsBuffer () { f = f } ;
                }

            }).ScheduleParallel () ;

            Entities
                .WithName ( "NNActivateNewPopulationJob" )
                .WithAll <NNBrainTag, IsSpawningCompleteTag> ()
                .WithNone <IsInitializedTag> ()
                .ForEach ( ( Entity entity, int entityInQueryIndex ) =>
            {

                ecbp.AddComponent <IsInitializedTag> ( entityInQueryIndex, entity ) ;
                ecbp.AddComponent <IsAliveTag> ( entityInQueryIndex, entity ) ;

            }).ScheduleParallel () ;
            
            eecb.AddJobHandleForProducer ( Dependency ) ;
        }

        static private void _MutationChances ( ref DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_offspringInput2HiddenLayersWeights, ref DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_offspringtHidden2OutputLayersWeights, ref Unity.Mathematics.Random random, float f_range, float f_majorMutationChance, float f_minorMutationChance, float f_minorMutationRangeScale )
        {
            
            for ( int i = 0; i < a_offspringInput2HiddenLayersWeights.Length; i ++ )
            {
                float f = random.NextFloat () < f_majorMutationChance ? random.NextFloat ( -f_range, f_range ) : ( random.NextFloat () < f_minorMutationChance ? a_offspringInput2HiddenLayersWeights [i].f : math.clamp ( a_offspringInput2HiddenLayersWeights [i].f + random.NextFloat ( -f_range, f_range ) * f_minorMutationRangeScale, -f_range, f_range ) ) ;
                a_offspringInput2HiddenLayersWeights [i] = new NNInput2HiddenLayersWeightsBuffer () { f = f } ;
            }

            for ( int i = 0; i < a_offspringtHidden2OutputLayersWeights.Length; i ++ )
            {
                float f = random.NextFloat () < f_majorMutationChance ? random.NextFloat ( -f_range, f_range ) : ( random.NextFloat () < f_minorMutationChance ? a_offspringInput2HiddenLayersWeights [i].f : math.clamp ( a_offspringtHidden2OutputLayersWeights [i].f + random.NextFloat ( -f_range, f_range ) * f_minorMutationRangeScale, -f_range, f_range ) ) ;
                a_offspringtHidden2OutputLayersWeights [i] = new NNHidden2OutputLayersWeightsBuffer () { f = f } ;
            }

        }

    }

}