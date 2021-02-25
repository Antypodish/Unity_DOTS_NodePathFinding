using UnityEngine;

using Unity.Entities;

namespace Antypodish.NodePathFinding.DOTS
{

    /// <summary>
    /// Range 0 to 32.
    /// Layer mask used by path planners.
    /// Use with ( 1 << mask value ).
    /// </summary>
    [GenerateAuthoringComponent]
    [InternalBufferCapacity ( 10 )]
    public struct PathNodeMaskWeightsBuffer : IBufferElementData 
    {
        
        [Header("Mask weight used by path planners.")]
        [Tooltip ( "Mask weight used by path planners." )]
        [Range ( 0, 32 )]
        public int f_weight ;
    }
    
}
