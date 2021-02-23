using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antypodish.Hove.OOP.PathFinding
{
    
    public struct Node
    {
        public Vector3 f3_position ;
        public int i_nodeIndex ;
        public float f_nodeElevation ;
        
        public int i_linkedNodeIndex ;
        public float f_linkedNodeElevation ;
    }
        
    struct MappedNode
    {
        public int i_weight ;
        public List <Vector3> l_previousNodes ;
    }

    sealed public class PathNodes : MonoBehaviour
    {

        static private int i_nodesCount ;

        static public Dictionary <float, List <Node> > dic_nodesAtElevation ;
        static public Dictionary <Vector3, Node> dic_nodes;
        /// <summary>
        /// Nodes, where elevation changes.
        /// </summary>
        List <PathNode> l_elevatedNodes ;

        static public Dictionary <Vector3, List <Node>> dic_nodesNetwork ;

        // List <Vector3> l_path ;


        // private Node startNode ;
        // private Node targetNode ;


        // Start is called before the first frame update
        void Start ()
        {
        
            i_nodesCount         = transform.childCount ;

            dic_nodesAtElevation = new Dictionary <float, List <Node>> ( i_nodesCount ) ;
            dic_nodes            = new Dictionary <Vector3, Node> ( i_nodesCount ) ;
            dic_nodesNetwork     = new Dictionary <Vector3, List <Node>> ( i_nodesCount ) ;

            l_elevatedNodes = new List <PathNode> ( i_nodesCount ) ;

            for ( int i = 0; i < i_nodesCount; i ++ )
            {

                Transform tr_child          = transform.GetChild ( i ) ;
                PathNode pathNodeComponent  = tr_child.GetComponent <PathNode> () ;

                List <Node> l_pathNode ;

                float f_elevation          = tr_child.position.y ;
                bool hasOtherElevationNode = pathNodeComponent.tr_elevationPathNode == null ? false : true ;

                if ( !dic_nodesAtElevation.TryGetValue ( f_elevation, out l_pathNode ) )
                {
                    // Not exists yet.

                    l_pathNode = new List <Node> ( i_nodesCount ) ; // Set with max posiible capacity.
                    dic_nodesAtElevation.Add ( f_elevation, l_pathNode ) ;
                }

                int i_elevationNodeIndex = l_pathNode.Count ;
                pathNodeComponent._SetLevelNodeIndex ( i_elevationNodeIndex ) ;
                pathNodeComponent._SetLevelNodeElevation ( f_elevation ) ;

                Node node = new Node () 
                { 
                    f3_position     = tr_child.position,
                    i_nodeIndex     = i_elevationNodeIndex, 
                    f_nodeElevation = f_elevation 
                } ;

                l_pathNode.Add ( node ) ;

                if ( hasOtherElevationNode )
                {
                    l_elevatedNodes.Add ( pathNodeComponent ) ;
                }
            
                dic_nodes.Add ( pathNodeComponent.transform.position, node ) ;

            } // for


            // Assign elevations for nodes.
            for ( int i = 0; i < l_elevatedNodes.Count; i ++ )
            {
                PathNode pathNodeComponent = l_elevatedNodes [i] ;
            
                List <Node> l_pathNode ;

                float f_levelNodeElevation = pathNodeComponent._GetLevelNodeElevation () ;

                // Get node
                if ( dic_nodesAtElevation.TryGetValue ( f_levelNodeElevation, out l_pathNode ) )
                {
                    Node node                                            = l_pathNode [pathNodeComponent._GetLevelNodeIndex ()] ;

                    PathNode linkedPathNodeComponent                     = pathNodeComponent.tr_elevationPathNode.GetComponent <PathNode> () ;

                    node.f_linkedNodeElevation                           = linkedPathNodeComponent._GetLevelNodeElevation () ;
                    node.i_linkedNodeIndex                               = linkedPathNodeComponent._GetLevelNodeIndex () ;

                    l_pathNode [pathNodeComponent._GetLevelNodeIndex ()] = node ; // Set back ;
                    dic_nodesAtElevation [f_levelNodeElevation]          = l_pathNode ; // Set back.
                }

            } // for


            // int i_pathNOdeLayerMask = -1 ;
            // int i_pathNOdeLayerMask = LayerMask.NameToLayer ( "PathNode" ) ;

            int i_test = 0 ;

            // Create net per level.
            foreach ( List <Node> l_nodes in dic_nodesAtElevation.Values )
            {

                for ( int i = 0; i < l_nodes.Count; i ++ )
                {
                    Node node = l_nodes [i] ;
                
                    List <Node> l_nodesNetwork = new List <Node> ( 100 ) ;

                    for ( int j = 0; j < l_nodes.Count; j ++ )
                    {
                    
                        Node linkedNode = l_nodes [j] ;
                    
                        Vector3 V3_dir = ( linkedNode.f3_position - node.f3_position ).normalized ;
                        Ray ray = new Ray ( node.f3_position, V3_dir ) ;
                    
                        RaycastHit [] a_rayHits = Physics.RaycastAll ( ray, 100 ) ; // , i_pathNOdeLayerMask ) )
                    
                        RaycastHit closestHit = default ;
                        closestHit.distance = 99999 ;

                        // Get closest hit
                        for ( int k = 0; k < a_rayHits.Length; k ++ ) 
                        {
                            RaycastHit hit = a_rayHits [k] ;

                            float f = ( hit.point - node.f3_position ).sqrMagnitude ;
                            // Debug.Log ( "#" + k + "; f: " + f ) ;

                            if ( f > 1 && hit.distance <= closestHit.distance )
                            {
                                // Debug.Log ( "Closest hit: " + hit.point + "; at distance: " + hit.distance ) ;
                                closestHit = hit ;
                            }

                        }
                    
                        Node outNode ;

                        if ( closestHit.collider != null && dic_nodes.TryGetValue ( closestHit.transform.position, out outNode ) )
                        {

                            l_nodesNetwork.Add ( outNode ) ;

                            Vector3 V3_diff = ( closestHit.point - ray.origin ) * 0.45f ;
                            V3_dir          = ( closestHit.point - ray.origin ).normalized ;
                    
                            // Debug.DrawLine ( node.f3_position, linkedNode.f3_position, Color.yellow, 20 ) ;

                            Debug.DrawLine ( ray.origin, ray.origin + V3_diff, Color.blue, 20 ) ;
                            Debug.DrawLine ( ray.origin, ray.origin + V3_dir * 3, Color.red, 20 ) ;

                            // Debug.Log ( "Found" ) ;
                            Debug.DrawLine ( ray.origin, ray.origin + V3_dir * 1, Color.green, 20 ) ;

                        }

                    } // for

                    // Elevation linked node.
                    List <Node> l_nodes2 ;
                    if ( dic_nodesAtElevation.TryGetValue ( node.f_linkedNodeElevation, out l_nodes2 ) )
                    {
                    
                        Node linkedNode = l_nodes2 [node.i_linkedNodeIndex] ;
                    
                        l_nodesNetwork.Add ( linkedNode ) ;

                        Vector3 V3_dir = ( linkedNode.f3_position - node.f3_position ).normalized ;
                        Ray ray = new Ray ( node.f3_position, V3_dir ) ;

                        Vector3 V3_diff = ( linkedNode.f3_position - node.f3_position ) * 0.45f ;
                        V3_dir          = ( linkedNode.f3_position - node.f3_position ).normalized ;
                    
                        // Debug.DrawLine ( node.f3_position, linkedNode.f3_position, Color.yellow, 20 ) ;

                        Debug.DrawLine ( ray.origin, ray.origin + V3_diff, Color.blue, 20 ) ;
                        Debug.DrawLine ( ray.origin, ray.origin + V3_dir * 3, Color.red, 20 ) ;

                        // Debug.Log ( "Found" ) ;
                        Debug.DrawLine ( ray.origin, ray.origin + V3_dir * 1, Color.green, 20 ) ;

                    }
                
                    dic_nodesNetwork.Add (node.f3_position, l_nodesNetwork ) ;

                } // for

            

            } // foreach ;


        }

        /*
        // Update is called once per frame
        void Update ()
        {
        
            Ray pointerRay = Camera.main.ScreenPointToRay ( Input.mousePosition ) ;

            RaycastHit hit ;

            if ( Input.GetMouseButtonUp ( 0 ) && Physics.Raycast ( pointerRay, out hit, 150 ) )
            {

                // Node targetNode ;
                if ( dic_nodes.TryGetValue ( hit.transform.position, out targetNode ) )
                {
                    // Debug.Log ( "Hit: " + targetNode.f3_position ) ;

                    l_path = new List <Vector3> ( i_nodesCount ) ;
                    l_path.Add  ( startNode.f3_position ) ;

                    _PathFindingAStar ( ref l_path ) ;
                }

            }

        }
        */


        static public int _GetNodesCount ()
        {
            return i_nodesCount ;
        }


        /*
        public void _PathFindingAStar ( ref List <Vector3> l_path )
        // static private void _PathFindingAStar ( in Node startNode, in Node targetNode, in Dictionary <Vector3, List <Node>> dic_nodesNetwork, int i_nodesCount, ref List <Vector3> l_path )
        {

            int i_weight = 0 ;
        
            // Debug.Log ( targetNode.f3_position ) ;
            if ( targetNode.f3_position.sqrMagnitude < 1 ) return ;

            Dictionary <Vector3, MappedNode> dic_nodePositionAndWeight = new Dictionary<Vector3, MappedNode> ( i_nodesCount ) ;

            MappedNode mappedNode = new MappedNode () { i_weight = i_weight, l_previousNodes = new List <Vector3> () } ;
            // mappedNode.l_previousNodes.Add ( startNode.f3_position ) ;
            dic_nodePositionAndWeight.Add ( startNode.f3_position, mappedNode ) ;

            Dictionary <Vector3, Node> dic_mappedNodesForWeight = new Dictionary <Vector3, Node> ( i_nodesCount ) ;

            // Initial search.
            _SearchNeghbourNodes ( startNode, targetNode, dic_nodesNetwork, i_nodesCount, ref dic_nodePositionAndWeight, ref dic_mappedNodesForWeight, i_weight ) ;

            int i_whileWatchdog = 0 ;
            while ( !_SearchAStart ( targetNode, dic_nodesNetwork, i_nodesCount, ref dic_nodePositionAndWeight, ref dic_mappedNodesForWeight, ref i_weight ) )
            {
                // Debug.LogError ( "Next search." ) ;

                i_whileWatchdog ++ ;
                if ( i_whileWatchdog > 1000 ) { Debug.LogError ( "While inf loop!" ) ; return ; } ;
            }


            MappedNode targetMappedNode = dic_nodePositionAndWeight [targetNode.f3_position] ;
        
            MappedNode currentMappedNode      = targetMappedNode ;
            Vector3 currentMappedNodePosition = targetNode.f3_position ;

            int i_curentWeight = targetMappedNode.i_weight ;
            // Debug.LogError ( "Target weight: " + i_curentWeight ) ;


            // Generate fnal path.
            while ( i_curentWeight > 0 )
            {

                // Debug.LogError ( "current weight: " + i_curentWeight + "; prev count: " + currentMappedNode.l_previousNodes.Count ) ;
            
                for ( int i = 0; i < currentMappedNode.l_previousNodes.Count; i ++ )
                {
                    Vector3 V3_previousNodePosition = currentMappedNode.l_previousNodes [i] ;
                    MappedNode previousMappedNode   = dic_nodePositionAndWeight [V3_previousNodePosition] ;
                
                    // Debug.Log ( i + "; " + previousMappedNode.i_weight ) ;
                
                
                    if ( previousMappedNode.i_weight < i_curentWeight )
                    {
                        // Debug.DrawLine ( V3_previousNodePosition, currentMappedNodePosition, Color.magenta, 10 ) ;
                        currentMappedNode         = previousMappedNode ;
                        currentMappedNodePosition = V3_previousNodePosition ;

                        l_path.Add ( V3_previousNodePosition ) ;
                        // i_curentWeight -- ;
                        break ;
                    }
                
                } // for.

                i_curentWeight -- ;
            }

        }

        static private bool _SearchNeghbourNodes ( in Node startNode, in Node targetNode, in Dictionary <Vector3, List <Node>> dic_nodesNetwork, int i_nodesCount, ref Dictionary <Vector3, MappedNode> dic_nodePositionAndWeight, ref Dictionary <Vector3, Node> dic_mappedNodesForWeight, int i_weight )
        {

            if ( ( targetNode.f3_position - startNode.f3_position ).sqrMagnitude < 1 ) 
            {
                // Debug.LogWarning ( "Node reached." ) ;
                return true ;
            }

            // Expected to always be valid.
            List <Node> l_linkedNodes ;
            dic_nodesNetwork.TryGetValue ( startNode.f3_position, out l_linkedNodes ) ;

            i_weight ++ ;

            // Debug.Log ( "links nodes count: " + l_linkedNodes.Count ) ;

            for ( int i = 0; i < l_linkedNodes.Count; i ++ )
            {
                Node linkedNode = l_linkedNodes [i] ;
                MappedNode mappedNode ;
                if ( !dic_nodePositionAndWeight.TryGetValue ( linkedNode.f3_position, out mappedNode ) )
                {
                    // Does not exists yet.
                    List <Vector3> l_previousNodes = new List <Vector3> ( 10 ) ;
                    l_previousNodes.Add ( startNode.f3_position ) ;
                    dic_nodePositionAndWeight.Add ( linkedNode.f3_position, new MappedNode () { i_weight = i_weight, l_previousNodes = l_previousNodes } ) ;
                    dic_mappedNodesForWeight.Add ( linkedNode.f3_position, linkedNode ) ;
                }
                else if ( mappedNode.i_weight == i_weight )
                {
                    // Already exists.
                    mappedNode.l_previousNodes.Add ( startNode.f3_position ) ;
                    dic_nodePositionAndWeight [linkedNode.f3_position] = mappedNode ; // Set back.
                }

            } // for

            return false ;

        }
    
        static private bool _SearchAStart ( in Node targetNode, in Dictionary <Vector3, List <Node>> dic_nodesNetwork, int i_nodesCount, ref Dictionary <Vector3, MappedNode> dic_nodePositionAndWeight, ref Dictionary <Vector3, Node> dic_mappedNodesForWeight, ref int i_weight )
        {
        
            i_weight ++ ;

            bool canProceed          = false ;
            bool isTargetNodeReached = false ;

            Dictionary <Vector3, Node> dic_mappedNodesForWeightNext = new Dictionary <Vector3, Node> ( i_nodesCount ) ;
            // Recursive search.
            foreach ( Node nextNode in dic_mappedNodesForWeight.Values )
            {
                isTargetNodeReached = _SearchNeghbourNodes ( nextNode, targetNode, dic_nodesNetwork, i_nodesCount, ref dic_nodePositionAndWeight, ref dic_mappedNodesForWeightNext, i_weight ) ;
                canProceed = true && !isTargetNodeReached ;
            }
        
            if ( !canProceed ) { return true ; }
        
            i_weight ++ ;

            canProceed = false ; // Reset.
            dic_mappedNodesForWeight = new Dictionary <Vector3, Node> ( i_nodesCount ) ; // Clear
        
            foreach ( Node nextNode in dic_mappedNodesForWeightNext.Values )
            {
                isTargetNodeReached = _SearchNeghbourNodes ( nextNode, targetNode, dic_nodesNetwork, i_nodesCount, ref dic_nodePositionAndWeight, ref dic_mappedNodesForWeight, i_weight ) ;
                canProceed = true ;
            }

            if ( !canProceed ) { return true ; }

            return false ;

        }
        */
    }

}