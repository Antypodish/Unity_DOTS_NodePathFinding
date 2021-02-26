using UnityEngine ;
using System.Collections.Generic ;
using System.Collections ;

using Antypodish.NodePathFinding.DOTS ;

namespace Antypodish.NodesPathFinding.Scripts.OOP
{

    [ExecuteInEditMode]
    public class PathNodeLinkRangeMB : MonoBehaviour
    {
        [Header("Important: Hover over the value name, to see details.")]
        [Header("This is just to help draw gizmo. No other functionality.")]
        [Tooltip ( "Range -1 = any range. Range 0 >= use current max range. Make sure that following range component authoring matches the value." )]
        [Range ( -1f, 500f )]
        public float f_maxRange ;
        
        [Header("Atm. elevation links are not rendered.")]
        [Header("And collisions are ignored.")]
        public bool canRenderGizmo = true ;

        private int i_gizmoInit = 0 ;
        
        private List <PathNode> l_pathNode ;

        // PathNodeLinkRangeComponent DOTS_pathNodeLinkRangeComponent ;

        /*
        void Awake()
        {
            l_pathNode = new List <PathNode> () ;
        }
        

        // Use this for initialization
        void Start ( )
        {
            l_pathNode = new List <PathNode> () ;
            // Debug.Log("Editor causes this Start") ;
        }
        */

        // Update is called once per frame
        void Update ( )
        {
            i_gizmoInit = 0 ; // Reset
            // Debug.Log("Editor causes this Update") ;
        }

        /*
        void OnDrawGizmos ()
        // void OnDrawGizmosSelected ()
        {
            // DOTS_pathNodeLinkRangeComponent = this.GetComponent <PathNodeLinkRangeComponent> () ;

            // Draw a yellow sphere at the transform's position
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere ( transform.position, f_maxRange ) ;

            // DOTS_pathNodeLinkRangeComponent.f_maxRange = f_maxRange ;

        }
        */

        /*
        void OnMouseUp ()
        {
            Debug.Log ( "Gizmos" ) ;

        }
        
        void OnMouseDown ()
        {
            Debug.Log ( "Gizmos" ) ;

        }
        */


        public class PathNode
        {
            public Vector3 V3_position ;
            public float f_maxRange ;
            public float f_distanceBetweenNodes ;
        }

        void OnDrawGizmosSelected ()
        {

            if ( canRenderGizmo )
            {
                
                i_gizmoInit ++ ;

                switch ( i_gizmoInit )
                {
                    case 1:
                        l_pathNode = new List <PathNode> () ;
                        break ;

                    case 10: // Provide small startup delay.
                        return ;
                        break ;
                    
                    case 11:
                        
                        PathNodeLinkRangeMB [] a_goTemp = GameObject.FindObjectsOfType <PathNodeLinkRangeMB> () ;

                        for ( int i = 0; i < a_goTemp.Length; i ++ )
                        {

                            PathNodeLinkRangeMB goTemp = a_goTemp [i] ;

                            Vector3 V3_goPos = goTemp.transform.position ;

                            if ( V3_goPos.y == transform.position.y && ( V3_goPos - transform.position ).sqrMagnitude > 0.1f )
                            {

                                float f_distanceBetweenNodes = Vector3.Magnitude ( V3_goPos - transform.position ) ;

                                if ( f_maxRange < 0 || f_maxRange >= f_distanceBetweenNodes )
                                {
                                    PathNode pathNode               = new PathNode () ; 
                                    pathNode.V3_position            = V3_goPos ;
                                    pathNode.f_maxRange             = goTemp.GetComponent <PathNodeLinkRangeMB> ().f_maxRange ;
                                    pathNode.f_distanceBetweenNodes = f_distanceBetweenNodes ; 
                                    l_pathNode.Add ( pathNode ) ;
                                }

                            }

                        } // for

                        break ;

                }
                

                // if ( i_gizmoInit < 10 ) return ;


                for ( int i = 0; i < l_pathNode.Count; i ++ )
                {
                    PathNode pathNode = l_pathNode [i] ;
                    
                    Gizmos.color      = Color.white ;
                    Gizmos.DrawWireSphere ( pathNode.V3_position, pathNode.f_maxRange ) ;

                    Debug.DrawLine ( transform.position, pathNode.V3_position, Color.magenta ) ;

                    // Return line.
                    float f_maxRange          = Mathf.Min ( pathNode.f_distanceBetweenNodes, pathNode.f_maxRange ) ;
                    Vector3 V3_offsetPosition = Vector3.one * 0.2f ;
                    Vector3 V3_endPosition    = pathNode.f_maxRange < 0 ? transform.position : pathNode.V3_position + ( transform.position - pathNode.V3_position ).normalized * f_maxRange ;

                    Debug.DrawLine ( pathNode.V3_position + V3_offsetPosition, V3_endPosition + V3_offsetPosition, Color.cyan ) ;
                    
                }
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere ( transform.position, f_maxRange ) ;

                // DOTS_pathNodeLinkRangeComponent = this.GetComponent <PathNodeLinkRangeComponent> () ;

                // DOTS_pathNodeLinkRangeComponent.f_maxRange = f_maxRange ;

            }

        }

    }
}