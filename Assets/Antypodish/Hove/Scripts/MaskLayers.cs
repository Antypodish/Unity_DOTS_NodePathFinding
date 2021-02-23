using UnityEngine;

namespace Antypodish.Hove
{
    public class MaskLayers
    {
        static public int i_pathNodes ;
        static public int i_vehicles ;
        
        // Initialize at initialization.
        public MaskLayers ()
        {
            _SetPathNodes ( ) ;
        }

        static private void _SetPathNodes ( )
        {
            i_pathNodes = _Name2Layer ( "PathNodes" ) ;
            i_vehicles  = _Name2Layer ( "Vehicles" ) ;
        }
        static public int _Name2Layer ( string s_layerName )
        {
            return 1 << LayerMask.NameToLayer ( s_layerName ) ;
        }

    }
}