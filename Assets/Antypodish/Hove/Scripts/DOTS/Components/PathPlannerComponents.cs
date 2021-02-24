using Unity.Entities ;
using Unity.Mathematics ;

namespace Antypodish.Hove.DOTS
{

    public struct PathPlannerComponents : IComponentData 
    {
        public float3 f3_positionA ;
        public float3 f3_positionB ;

        public Entity entityA ;
        public Entity entityB ;

        public bool isToggleAorB ;
    }

    /*
    /// <summary>
    /// Path finding will alternate between last visited buffer, to find next linked nodes.
    /// </summary>
    [InternalBufferCapacity ( 10 )]
    public struct LastIVisitedPathNodesABuffer : IBufferElementData
    {
        public int i_index ;
        public float f_weight ;
        public Entity entity ;
    }
    
    /// <summary>
    /// Path finding will alternate between last visited buffer, to find next linked nodes.
    /// </summary>
    [InternalBufferCapacity ( 10 )]
    public struct LastIVisitedPathNodesBBuffer : IBufferElementData
    {
        public int i_index ;
        public float f_weight ;
        public Entity entity ;
    }
    */

}
