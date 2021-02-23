using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antypodish.Hove.OOP.PathFinding
{

    public class PathNode : MonoBehaviour
    {

        public Transform tr_elevationPathNode ;
        private int i_levelNodeIndex ;
        private float f_levelNodeElevation ;


        public int _GetLevelNodeIndex ( )
        {
            return i_levelNodeIndex ;
        }
    
        public void _SetLevelNodeIndex ( int i )
        {
            i_levelNodeIndex = i ;
        }
    
        public float _GetLevelNodeElevation ( )
        {
            return f_levelNodeElevation ;
        }
    
        public void _SetLevelNodeElevation( float f )
        {
            f_levelNodeElevation = f ;
        }
    }

}