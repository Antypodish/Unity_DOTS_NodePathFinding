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
        public float f_distance2PreviousNode ;
    }
    
    /// <summary>
    /// See corresponding PathNodeMaskWeightsBuffer, on node entity.
    /// See mask variable for more details.
    /// </summary>
    public struct PathPlannerWeightsMaskComponent : IComponentData
    {
        /// <summary>
        /// Mask examples.
        /// 0 size buffer, or mask set to -1, means no additional weight will be used.
        /// Buffer size 1 and mask set to 0, will match and first buffer index weight and its weight will be evaluated.
        /// Buffer size 2 and mask set to 1, will match and second buffer index and its weight will be evaluated.
        /// Buffer size 3 and mask set to 3, will match and first and second (not third) buffer indexes and its weights will be evaluated.
        /// Buffer size 4 and mask set to 5, will match and first and forth buffer indexes and its weights will be evaluated.
        /// Etc.
        /// </summary>
        public int i_mask ;
    }
    
    public struct PathTotalLengthComponent : IComponentData
    {
        public float f ;
    }
    
    public struct CanFindPathTag : IComponentData {}
}
