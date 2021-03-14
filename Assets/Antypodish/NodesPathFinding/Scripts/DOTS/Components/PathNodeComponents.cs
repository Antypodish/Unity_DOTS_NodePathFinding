using Unity.Entities;
using Unity.Mathematics ;

namespace Antypodish.NodePathFinding.DOTS
{

    public struct PathNodeElevationComponent : IComponentData 
    {
        public float f ;
    }
    
    [InternalBufferCapacity ( 10 )]
    public struct PathNodeLinksBuffer : IBufferElementData
    {
        public Entity entity ;
        public float3 f3 ;
        public float f_distance ;
    }

}
