using Unity.Entities ;
using Unity.Mathematics ;

namespace Antypodish.NodePathFinding.DOTS
{

    public struct PathPlannerComponent : IComponentData 
    {
        public Entity entityA ;
        public Entity entityB ;
    }

    [InternalBufferCapacity ( 10 )]
    public struct PathNodesBuffer : IBufferElementData
    { 
        public float3 f3_position ;
    }
    
    public struct PathPlannerWeightsMaskComponent : IComponentData
    {
        public int i_mask ;
    }

    public struct CanFindPathTag : IComponentData {}

}
