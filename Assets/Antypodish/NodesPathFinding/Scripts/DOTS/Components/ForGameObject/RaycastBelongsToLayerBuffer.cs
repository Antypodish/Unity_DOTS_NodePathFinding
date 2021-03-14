using UnityEngine;

using Unity.Entities;

namespace Antypodish.NodePathFinding.DOTS
{

    [GenerateAuthoringComponent]
    public struct RaycastBelongsToLayerBuffer : IBufferElementData
    {
        /// <summary>
        /// Use in conjunction with 1 << layer
        /// </summary>
        public int i_layerID ;
    } ;
    
}
