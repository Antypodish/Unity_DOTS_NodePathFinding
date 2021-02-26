using UnityEngine;
using System.Collections;

using Antypodish.NodePathFinding.DOTS ;

namespace Antypodish.NodesPathFinding.Scripts.OOP
{
    public class PathNodeMaskWeightsBufferMB : MonoBehaviour
    {
        [TextArea (1, 2)]
        //[Range ( -1f, 500f )]
        public string BufferDescription = "This is helper component, with help description for Path Node Mask Weights Buffer Authoring script." ;
        
        [TextArea (1, 5)]
        //[Range ( -1f, 500f )]
        public string ElementsDescription = "Mask Weights Buffer Authoring should take typically weights float values in range 0 to infinity. " +
            "However, negative value is also acceptable.However, value above 100k, is conidered as path barrier, with no path allowed." ;
        
        [TextArea (1, 5)]
        //[Range ( -1f, 500f )]
        public string BufferSizeDescription = "Buffer size should be in range of 0 to 32, as each element corresponds to different mask. " +
            "Path Planner Entity should have PathPlannerWeightsMaskComponent, with corresponding mask set. " ;
        
        [TextArea (1, 7)]
        //[Range ( -1f, 500f )]
        public string PathPlannerDescription = "Path Planner Entity PathPlannerWeightsMaskComponent mask examples: " +
            "0 size buffer, or mask set to -1, means no additional weight will be used." +
            "Buffer size 1 and mask set to 0, will match and first buffer index weight and its weight will be evaluated." +
            "Buffer size 2 and mask set to 1, will match and second buffer index and its weight will be evaluated." +
            "Buffer size 3 and mask set to 3, will match and first and second (not third) buffer indexes and its weights will be evaluated." +
            "Buffer size 4 and mask set to 5, will match and first and forth buffer indexes and its weights will be evaluated." +
            "Etc." ;
        // PathNodeLinkRangeComponent DOTS_pathNodeLinkRangeComponent ;


        // Use this for initialization
        void Start ( )
        {
        }

        // Update is called once per frame
        void Update ( )
        {

        }

    }
}