using UnityEngine ;

namespace Antypodish.NodesPathFinding.Scripts.OOP
{

    public class RaycastCollideBelongLayersMB : MonoBehaviour
    {
        [TextArea (1, 4)]
        public string GeneralDescription = "Raycast Belongs To and Collides With Layer Buffers " +
            "are used only for path finding generation, " +
            "which uses raycast, to check if nodes are on visible path. Creating that way the link." +
            "See also CollisionFilter.cs for example details." ;

        [TextArea (1, 3)]
        public string BelongsToLayerDescription = 
            "Buffer layer by defualt has one element, and it matches raycast belongs to layer. " +
            "The values are used to evaluate mask with 1 << layerID. " +
            "If multiple layer are used, all are OR-ed." ;
        
        [TextArea (1, 6)]
        public string CollidesWithLayerDescription = 
            "Buffer layer by defualt has few elements, and it matches static objects' belongs to layers." +
            "For example Wall belongs to layerID 1 and ramp belongs to layerID 2." +
            "The values are used to evaluate mask with 1 << layerID. " +
            "If multiple layer are used, all are OR-ed." +
            "Used static objects in this example, have also set collide with raycast layer." +
            "See static object prefabs in inspector, for details." ;
            
    }
}