using UnityEngine;

using Unity.Entities;

namespace Antypodish.NodePathFinding.DOTS
{

    [GenerateAuthoringComponent]
    public struct PathNodeLinkRangeComponent : IComponentData 
    {
        [Tooltip ( "Range -1 = any range. Range 0 >= use current max range." )]
        [Range ( -1f, 100f )]
        public float f_maxRange ;
    }
    
}
