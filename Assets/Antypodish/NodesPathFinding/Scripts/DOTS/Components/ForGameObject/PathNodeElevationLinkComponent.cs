using UnityEngine;

using Unity.Entities;
using Unity.Transforms ;

namespace Antypodish.NodePathFinding.DOTS
{

    [GenerateAuthoringComponent]
    public struct PathNodeElevationLinkComponent : IComponentData 
    {
        public Entity linkedEntity ;
    }

}
