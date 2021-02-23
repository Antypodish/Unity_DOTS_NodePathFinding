using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antypodish.Hove.OOP.PathFinding
{

    public class PathFinding
    {

        // private int i_nodesCount ;

        // public Dictionary <Vector3, Node> dic_nodes ;

        // Dictionary <Vector3, List <Node>> dic_nodesNetwork ;

        public List <Vector3> l_path ;

        private Node startNode ;
        private Node targetNode ;

        /*
        // Start is called before the first frame update
        void _Start ()
        {
            
            if ( i_test == 0 ) 
            {
                startNode = l_nodes [0] ;
                Debug.LogWarning ( "Test Start Node " + startNode.f3_position ) ;
                i_test ++ ;
            }
            
        }
        */

        /*
        // Update is called once per frame
        public void _Update ()
        {
        
            Ray pointerRay = Camera.main.ScreenPointToRay ( Input.mousePosition ) ;

            RaycastHit hit ;

            if ( Input.GetMouseButtonUp ( 0 ) && Physics.Raycast ( pointerRay, out hit, 150 ) )
            {

                // Node targetNode ;
                if ( PathNodes.dic_nodes.TryGetValue ( hit.transform.position, out targetNode ) )
                {
                    // Debug.Log ( "Hit: " + targetNode.f3_position ) ;

                    l_path = new List <Vector3> ( PathNodes._GetNodesCount () ) ;
                    l_path.Add  ( startNode.f3_position ) ;

                    _PathFindingAStar () ;
                }

            }

        }
        */

        public void _SetStartNode ( Vector3 V3_nodePosition )
        {
            if ( ( V3_nodePosition - startNode.f3_position ).sqrMagnitude > 1 )
            { 
                startNode = PathNodes.dic_nodes [V3_nodePosition] ;

                l_path = new List <Vector3> ( PathNodes._GetNodesCount () ) ;
                l_path.Add  ( targetNode.f3_position ) ;

                _PathFindingAStar () ;
            }
        }
        
        public void _SetNewDestination ( Vector3 V3_destinationNodePosition )
        {

            // Node targetNode ;
            if ( PathNodes.dic_nodes.TryGetValue ( V3_destinationNodePosition, out targetNode ) )
            {
                // Debug.Log ( "Hit: " + targetNode.f3_position ) ;

                l_path = new List <Vector3> ( PathNodes._GetNodesCount () ) ;
                l_path.Add  ( V3_destinationNodePosition ) ;

                _PathFindingAStar () ;
                
                /// l_path [0] = V3_destinationNodePosition ;
Debug.Log ( "Target node: " + l_path.Count + "; " + V3_destinationNodePosition.ToString ("F4") + "; -1: " + l_path [0].ToString ("F4") + "; Target: " + l_path [1].ToString ("F4")  ) ;
            }

        }

        public void _PathFindingAStar ()
        // static private void _PathFindingAStar ( in Node startNode, in Node targetNode, in Dictionary <Vector3, List <Node>> dic_nodesNetwork, int i_nodesCount, ref List <Vector3> l_path )
        {

            // Debug.Log ( targetNode.f3_position ) ;
            if ( targetNode.f3_position.sqrMagnitude < 1 ) return ;

            int i_weight     = 0 ;
            int i_nodesCount = PathNodes._GetNodesCount () ;

            Dictionary <Vector3, MappedNode> dic_nodePositionAndWeight = new Dictionary<Vector3, MappedNode> ( i_nodesCount ) ;

            MappedNode mappedNode = new MappedNode () { i_weight = i_weight, l_previousNodes = new List <Vector3> () } ;
            // mappedNode.l_previousNodes.Add ( startNode.f3_position ) ;
            dic_nodePositionAndWeight.Add ( startNode.f3_position, mappedNode ) ;

            Dictionary <Vector3, Node> dic_mappedNodesForWeight = new Dictionary <Vector3, Node> ( i_nodesCount ) ;

            // Initial search.
            _SearchNeghbourNodes ( startNode, targetNode, PathNodes.dic_nodesNetwork, i_nodesCount, ref dic_nodePositionAndWeight, ref dic_mappedNodesForWeight, i_weight ) ;

            int i_whileWatchdog = 0 ;
            while ( !_SearchAStart ( targetNode, PathNodes.dic_nodesNetwork, i_nodesCount, ref dic_nodePositionAndWeight, ref dic_mappedNodesForWeight, ref i_weight ) )
            {
                // Debug.LogError ( "Next search." ) ;

                i_whileWatchdog ++ ;
                if ( i_whileWatchdog > 1000 ) { Debug.LogError ( "While inf loop!" ) ; return ; } ;
            }


            MappedNode targetMappedNode = dic_nodePositionAndWeight [targetNode.f3_position] ;
        
            MappedNode currentMappedNode      = targetMappedNode ;
            // Vector3 currentMappedNodePosition = targetNode.f3_position ;

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
                        // currentMappedNodePosition = V3_previousNodePosition ;

// Debug.Log ( "previous node: " + l_path.Count + "; " + V3_previousNodePosition.ToString ("F4") ) ;
                        l_path.Add ( V3_previousNodePosition ) ;
                        // i_curentWeight -- ;
                        break ;
                    }
                
                } // for.

                i_curentWeight -- ;
            }

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

    }

}