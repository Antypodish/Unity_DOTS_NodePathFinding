using UnityEngine;
using System.Collections;

using Antypodish.NodePathFinding.DOTS ;

namespace Antypodish.NodesPathFinding.Scripts.OOP
{
    public class PathNodeLinkRangeMB : MonoBehaviour
    {
        [Header("Important: Hover over the value name, to see details.")]
        [Header("This is just to help draw gizmo. No other functionality.")]
        [Tooltip ( "Range -1 = any range. Range 0 >= use current max range. Make sure that following range component authoring matches the value." )]
        [Range ( -1f, 500f )]
        public float f_maxRange ;
        
        // PathNodeLinkRangeComponent DOTS_pathNodeLinkRangeComponent ;


        // Use this for initialization
        void Start ( )
        {
        }

        // Update is called once per frame
        void Update ( )
        {

        }

        void OnDrawGizmosSelected ()
        {
            // DOTS_pathNodeLinkRangeComponent = this.GetComponent <PathNodeLinkRangeComponent> () ;

            // Draw a yellow sphere at the transform's position
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere ( transform.position, f_maxRange ) ;

            // DOTS_pathNodeLinkRangeComponent.f_maxRange = f_maxRange ;

        }

    }
}