using UnityEngine ;
using System.IO ;
using System.Collections.Generic ;

using Unity.Entities ;
using Unity.Collections ;

using Antypodish.DOTS ;


namespace Antypodish.GeneticNueralNetwork.DOTS
{
    
    public class ManagerMethods
    {
                
        [System.Serializable]
        public class JsonNeuralNetworkMangers
        {

            public List <Manager> l_managers = new List <Manager> () ;


            public void _AddAndInitializeManger ( int i_brainsCountPerManger, int i_input2HiddenLayerWeightsCountPerManger, int i_hidden2OutputLayerWeightsCountPerManger, int i_hiddenLayersCount, ref List <Manager> l_managers )
            {

                Manager manager = new Manager () ;

                // int i_brainsCountPerManger = i_brainsCountPerManger ;
                manager.l_brains.Capacity  = i_brainsCountPerManger ;

                for ( int j = 0; j < i_brainsCountPerManger; j++ )
                {

                    Manager.Brain brain = new Manager.Brain () ;

                    _InitializeWeights ( ref brain, i_input2HiddenLayerWeightsCountPerManger, i_hidden2OutputLayerWeightsCountPerManger, i_hiddenLayersCount ) ;

                    manager.l_brains.Add ( brain ) ;

                } // for

                l_managers.Add ( manager ) ;

                Debug.Log ( "Init manager: " + l_managers.Count + "; with elite brains count: " + manager.l_brains.Count ) ;

            }

            static public void _InitializeWeights ( ref Manager.Brain brain, int i_input2HiddenLayerWeightsCountPerManger, int i_hidden2OutputLayerWeightsCountPerManger, int i_hiddenLayersCount )
            {
                
                brain.i_score                              = 0 ;

                brain.l_input2HiddenLayerWeights.Capacity  = i_input2HiddenLayerWeightsCountPerManger ;
                brain.l_hidden2OutputLayerWeights.Capacity = i_hidden2OutputLayerWeightsCountPerManger ;

                for ( int i = 0; i < i_input2HiddenLayerWeightsCountPerManger; i++ )
                {
                    brain.l_input2HiddenLayerWeights.Add ( 0 ) ; // Defualt
                } // for
                
                for ( int i = 0; i < i_hidden2OutputLayerWeightsCountPerManger; i++ )
                {
                    brain.l_hidden2OutputLayerWeights.Add ( 0 ) ; // Defualt
                } // for
                
                for ( int i = 0; i < i_hiddenLayersCount; i++ )
                {
                    brain.l_hiddenLayersNeuronsBias.Add ( 0 ) ; // Defualt
                } // for
                
            }


            [System.Serializable]
            public class Manager
            {
                
                public List <Brain> l_brains = new List <Brain> () ;

                [System.Serializable]
                public class Brain
                {
                    public int i_score ;
                    public List <float> l_input2HiddenLayerWeights  = new List <float> () ;
                    public List <float> l_hidden2OutputLayerWeights = new List <float> () ;
                    public List <float> l_hiddenLayersNeuronsBias   = new List <float> () ;
                    

                }

            }
        }
        

        
        static public bool _SkipInvalidManager ( Entity managerEntity, ref ComponentDataFromEntity <IsAliveTag> a_isAliveTag )
        {
            return managerEntity.Version == 0 || !a_isAliveTag.HasComponent ( managerEntity ) ;
        }

        static public void _ReadDNAFromFile ( SystemBase system, ref JsonNeuralNetworkMangers jsonNeuralNetworkMangers, in LayersNeuronCounts layersNeuronCounts, in EntityQuery group_populationForWrittingTo, in NNManagerComponent manager, int i_activeManager, string s_path )
        {

            if ( manager.f_readFromFile )
            {

                // Read brains from json.
                StreamReader reader = new StreamReader ( s_path ) ; 
                string s_json       = reader.ReadToEnd () ;
                reader.Close () ;
                    
                JsonNeuralNetworkMangers.Manager manger = JsonUtility.FromJson <JsonNeuralNetworkMangers.Manager> ( s_json ) ;
                jsonNeuralNetworkMangers.l_managers [i_activeManager] = manger ;

                int i_elitesCount = _ElitesCount ( in manager ) ;

                if ( manger.l_brains.Count > i_elitesCount ) manger.l_brains.RemoveRange ( i_elitesCount, manger.l_brains.Count - i_elitesCount ) ;
// Debug.Log ( "load manager brains capacity: " + manger.l_brains.Capacity + " / " + manger.l_brains.Count ) ;

                BufferFromEntity <NNInput2HiddenLayersWeightsBuffer> input2HiddenLayersWeightsBuffer   = system.GetBufferFromEntity <NNInput2HiddenLayersWeightsBuffer> ( false ) ;
                BufferFromEntity <NNHidden2OutputLayersWeightsBuffer> hidden2OutputLayersWeightsBuffer = system.GetBufferFromEntity <NNHidden2OutputLayersWeightsBuffer> ( false ) ;

                NativeArray <Entity> na_spawningNewGenerationEntities = group_populationForWrittingTo.ToEntityArray ( Allocator.TempJob ) ;


                for ( int j = 0; j < na_spawningNewGenerationEntities.Length; j ++ )
                {

                    int i_brainIndex = j % manger.l_brains.Count ; // Modulo

                    // If saved brains count is lower, than spawner brains, start saved index from 0.
                    JsonNeuralNetworkMangers.Manager.Brain brain = manger.l_brains [i_brainIndex] ;

                    Entity brainEntity = na_spawningNewGenerationEntities [j] ;
                                
                    DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_input2HiddenLayersWeights   = input2HiddenLayersWeightsBuffer [brainEntity] ;
                    DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_hidden2OutputLayersWeights = hidden2OutputLayersWeightsBuffer [brainEntity] ;
                                
                                
                    for ( int k = 0; k < a_input2HiddenLayersWeights.Length; k ++ )
                    {
                        a_input2HiddenLayersWeights [k] = new NNInput2HiddenLayersWeightsBuffer () { f = brain.l_input2HiddenLayerWeights [k] } ;
// Debug.Log ( "Read weight: " + j + " / " + manger.l_brains.Count + "; hidden 2 Output : " + k + " / " + a_input2HiddenLayersWeights.Length + "; value: " + a_input2HiddenLayersWeights [k] ) ;
                    } // for
                                
                    for ( int k = 0; k < a_hidden2OutputLayersWeights.Length; k ++ )
                    {
                        a_hidden2OutputLayersWeights [k] = new NNHidden2OutputLayersWeightsBuffer () { f = brain.l_hidden2OutputLayerWeights [k] } ;
// Debug.Log ( "Read weight: " + j + " / " + manger.l_brains.Count + "; hidden 2 Output : " + k + " / " + a_hidden2OutputLayersWeights.Length + "; value: " + a_hidden2OutputLayersWeights [k] ) ;
                    } // for

                } // for

                // Add required brains for saving Json.
                for ( int j = 0; j < i_elitesCount; j ++ )
                {
                                
                    if ( j == manger.l_brains.Count )  
                    {
                        JsonNeuralNetworkMangers.Manager.Brain newBrain = new JsonNeuralNetworkMangers.Manager.Brain () ;

                                    
                        JsonNeuralNetworkMangers._InitializeWeights ( 
                            ref newBrain, 
                            NewGenerationkIsSpawingSystem._Input2HiddenLayerWeightsCount ( layersNeuronCounts.i_inputLayerNeuronsCount, layersNeuronCounts.i_hiddenLayerNeuronsCount ), 
                            NewGenerationkIsSpawingSystem._Hidden2OutputLayerWeightsCount ( layersNeuronCounts.i_outputLayerNeuronsCount, layersNeuronCounts.i_hiddenLayerNeuronsCount ),
                            layersNeuronCounts.i_hiddenLayerNeuronsCount
                        ) ;

                        manger.l_brains.Add ( newBrain ) ;
                    }

                } // for
                            
          
                na_spawningNewGenerationEntities.Dispose () ;

            }

        }


        /// <summary>
        /// Save elite brains.
        /// </summary>
        static public void _SaveDNA2File ( SystemBase system, in JsonNeuralNetworkMangers jsonNeuralNetworkMangers, in NNManagerComponent manager, in NativeArray <EntityIndex> na_elities, int i_activeManager, string s_path )
        {


            // Json utility.
            // Store current brains status.
            if ( manager.f_write2File )
            {
                                
                ComponentDataFromEntity <NNBrainScoreComponent> a_brainScore                           = system.GetComponentDataFromEntity <NNBrainScoreComponent> ( true ) ;
                BufferFromEntity <NNInput2HiddenLayersWeightsBuffer> input2HiddenLayersWeightsBuffer   = system.GetBufferFromEntity <NNInput2HiddenLayersWeightsBuffer> ( true ) ;
                BufferFromEntity <NNHidden2OutputLayersWeightsBuffer> hidden2OutputLayersWeightsBuffer = system.GetBufferFromEntity <NNHidden2OutputLayersWeightsBuffer> ( true ) ;

                JsonNeuralNetworkMangers.Manager jsonNeuralNetworkManger = jsonNeuralNetworkMangers.l_managers [i_activeManager] ;

                int i_elitsCount = _ElitesCount ( in manager ) ;

                                                
                Debug.Log ( "Try save brains to json: " ) ;
                Debug.Log ( "elites: " + i_elitsCount + " / " + jsonNeuralNetworkManger.l_brains.Count ) ;

                // Grab best elites to save.
                for ( int i = 0; i < i_elitsCount; i ++ )
                {

                    Entity brainEntity = na_elities [i].entity ;
                                    
                    NNBrainScoreComponent brainScore                                                = a_brainScore [brainEntity] ;

                    DynamicBuffer <NNInput2HiddenLayersWeightsBuffer> a_input2HiddenLayersWeights   = input2HiddenLayersWeightsBuffer [brainEntity] ;
                    DynamicBuffer <NNHidden2OutputLayersWeightsBuffer> a_hidden2OutputLayersWeights = hidden2OutputLayersWeightsBuffer [brainEntity] ;
                                    

                    JsonNeuralNetworkMangers.Manager.Brain brain                                    = jsonNeuralNetworkManger.l_brains [i] ;

                    brain.i_score = brainScore.i ;

// Debug.LogError ( i + " / " + i_elitsCount + " / brains: " + jsonNeuralNetworkManger.l_brains.Count + "; weights: " + a_input2HiddenLayersWeights.Length + " / " + brain.l_input2HiddenLayerWeights.Count ) ;
                    
                    for ( int j = 0; j < a_input2HiddenLayersWeights.Length; j ++ )
                    {
                        brain.l_input2HiddenLayerWeights [j] = a_input2HiddenLayersWeights [j].f ;
                    }

                    for ( int j = 0; j < a_hidden2OutputLayersWeights.Length; j ++ )
                    {
                        brain.l_hidden2OutputLayerWeights [j] = a_hidden2OutputLayersWeights [j].f ;
                    }

                } // for

                string s_saveBrains = JsonUtility.ToJson ( jsonNeuralNetworkManger ) ;

                //Write some text to the test.txt file
                StreamWriter writer = new StreamWriter ( s_path ) ;
                writer.WriteLine ( s_saveBrains ) ;
                writer.Close () ;
                                
                Debug.Log ( "Saved. " ) ;
            }

        }
        
        static public int _ElitesCount ( in NNManagerComponent manager )
        {
            return (int) ( manager.i_populationSize * manager.f_eliteSize ) ;
        }

    }

}