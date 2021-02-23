using UnityEngine ;
using System.Collections.Generic ;

using Unity.Entities ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    // [AlwaysUpdateSystem]
    [UpdateInGroup ( typeof ( ManagerPreSimulationSystemGroup ) )]  
    public class ManagerTimerSystem : SystemBase
    {
        
        BeginSimulationEntityCommandBufferSystem becb ;

        EntityQuery group_MMMamager ;
        // EntityQuery group_MMMamagerNotYetActive ;
        EntityQuery group_finishedPopulation ;

        
        private List <NNManagerSharedComponent> l_managerSharedData = new List <NNManagerSharedComponent> ( 1000 ) ;


        protected override void OnCreate ( )
        {

            becb = World.GetOrCreateSystem <BeginSimulationEntityCommandBufferSystem> () ;

            group_MMMamager = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               ComponentType.ReadOnly <NNManagerComponent> (),

               ComponentType.Exclude <NNMangerIsSpawningNewGenerationTag> ()
            ) ;
            
            group_finishedPopulation = EntityManager.CreateEntityQuery
            (
               ComponentType.ReadOnly <IsAliveTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (), ...

               ComponentType.ReadOnly <NNBrainTag> (),
               ComponentType.ReadOnly <NNIsFinishedTag> (),
               // ComponentType.ReadOnly <NNIsFinishedTag> (),
               ComponentType.Exclude <NNIsPreviousGenerationTag> (),
               ComponentType.Exclude <NNIsFirstGenerationTag> (),

               ComponentType.ReadWrite <NNManagerSharedComponent> ()
            ) ;

        }

        private double i_startTime = 0 ;



        // Use this for initialization
        protected override void OnStartRunning ( )
        {
            i_startTime = Time.ElapsedTime + 2 ; // 2 sec.
        }

        protected override void OnDestroy ( )
        {
        }


        // Update is called once per frame
        protected override void OnUpdate ( )
        {
            
            if ( Time.ElapsedTime <= i_startTime ) return ; // Delay startup.


            EntityCommandBuffer ecb = becb.CreateCommandBuffer () ;
            
            ComponentDataFromEntity <NNManagerComponent> a_manager ;


            if ( group_MMMamager.CalculateChunkCount () == 0 )
            {
                // Debug.LogWarning ( "There is no active managers yet." ) ;
                return ;
            }

            
            a_manager                                                                       = GetComponentDataFromEntity <NNManagerComponent> ( false ) ;
            // NNManagerComponent manager ;
            ComponentDataFromEntity <NNTimerComponent> a_managerTimer                       = GetComponentDataFromEntity <NNTimerComponent> ( false ) ;
            ComponentDataFromEntity <IsTimeUpTag> a_isTimeUpTag                             = GetComponentDataFromEntity <IsTimeUpTag> ( true ) ;
            

            int i_activeManager  = 0 ;
  

            l_managerSharedData.Clear () ;
            EntityManager.GetAllUniqueSharedComponentData ( l_managerSharedData ) ;


            // Ignore default manager entity ( index = 0, version = 0 ), taken from prefab entity.
            for ( int i = 0; i < l_managerSharedData.Count; i++ )
            {
                
                NNManagerSharedComponent mangerSharedComponent = l_managerSharedData [i] ;
                Entity managerEntity                           = new Entity () { Index = mangerSharedComponent.i_entityIndex, Version = mangerSharedComponent.i_entityVersion } ;
                
                ComponentDataFromEntity <IsAliveTag> a_isAliveTag = GetComponentDataFromEntity <IsAliveTag> ( false ) ;
// Debug.Log ( "nnManagerEntity: " + nnManagerEntity ) ;
                

                // Entity manager must be valid and active.
                if ( ManagerMethods._SkipInvalidManager ( managerEntity, ref a_isAliveTag ) ) continue ;

                if ( !a_isTimeUpTag.HasComponent ( managerEntity ) )
                {
                    NNTimerComponent managerTimer                       = a_managerTimer [managerEntity] ;
                
                    NNManagerComponent manager                          = a_manager [managerEntity] ;
                
// Debug.Log ( "Timer" ) ;                
                    group_finishedPopulation.SetSharedComponentFilter ( mangerSharedComponent ) ;

                    if ( Time.ElapsedTime >= managerTimer.f || group_finishedPopulation.CalculateEntityCount () >= manager.i_populationSize )
                    {

                        managerTimer.f                  = (float) Time.ElapsedTime + manager.i_startLifeTime ;

                        a_managerTimer [managerEntity]  = managerTimer ; // Set back.

                        ecb.AddComponent <IsTimeUpTag> ( managerEntity ) ;

// Debug.LogError ( "Set" ) ;       
                    }

                }
                else // if ( a_isTimeUpTag.Exists ( managerEntity ) )
                {
// Debug.LogError ( "Reset" ) ;       
                    ecb.RemoveComponent <IsTimeUpTag> ( managerEntity ) ;
                }

                i_activeManager ++ ;

            } // for

            becb.AddJobHandleForProducer ( Dependency ) ;

        }
        
    }

}
