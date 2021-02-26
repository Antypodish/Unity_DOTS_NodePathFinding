using UnityEngine;

using Unity.Entities;

namespace Antypodish.NodePathFinding.DOTS
{

    /// <summary>
    /// Buffer size should be in range 0 to 32.    
    /// Layer mask is used by path planners in PathPlannerWeightsMaskComponent.
    /// Use with ( 1 << mask value ).
    /// See buffer value for more details.
    /// </summary>
    [GenerateAuthoringComponent]
    [InternalBufferCapacity ( 10 )]
    public struct PathNodeMaskWeightsBuffer : IBufferElementData 
    {

        /// <summary>
        /// Each element float value should be typically 0 to inf.
        /// But negative value is also acceptable.
        /// However, value above 100k, is conidered as path barrier, with no path allowed.
        /// </summary>
        [Header("Mask weight used by path planners.")]
        [Tooltip ( "Mask weight used by path planners." )]
        [Range ( -Mathf.Infinity, Mathf.Infinity )]
        public float f_weight ;
    }
    
}
