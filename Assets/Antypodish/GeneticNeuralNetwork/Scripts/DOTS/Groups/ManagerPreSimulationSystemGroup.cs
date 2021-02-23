using Unity.Entities;  

namespace Antypodish.GeneticNueralNetwork.DOTS
{
  
    // A group that runs right at the very beginning of SimulationSystemGroup  
    [UpdateInGroup ( typeof ( SimulationSystemGroup ), OrderFirst = true )]  
    [UpdateBefore ( typeof ( BeginSimulationEntityCommandBufferSystem ))]  
    public class ManagerPreSimulationSystemGroup : ComponentSystemGroup { }

}