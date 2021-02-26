using UnityEngine;

using Unity.Entities;

namespace Antypodish.NodePathFinding.DOTS
{

    [GenerateAuthoringComponent]
    public struct PathNodeLinkRangeComponent : IComponentData 
    {
        [Header ( "Hover over the value name for tooltip." ) ]
        [Tooltip ( "Range -1 = any range. Range 0 >= use current max range. Use MB component above, for drawing gizmo in editor ." )]
        [Range ( -1f, 100f )]
        public float f_maxRange ;
    }
    
}
