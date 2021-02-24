using UnityEngine;

using Unity.Entities;
using Unity.Transforms ;

namespace Antypodish.Hove.DOTS
{

    [GenerateAuthoringComponent]
    public struct PathNodeElevationLinkComponent : IComponentData 
    {
        public Entity linkedEntity ;
    }

}
