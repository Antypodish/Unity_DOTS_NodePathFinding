using UnityEngine;
using System.Collections;
using System.Collections.Generic ;

namespace Antypodish.Hove.OOP
{
    public class VehiclesManager : MonoBehaviour
    {

        public Transform hoverCraftPrefab ;
        public int i_spawnCount ;

        public float force ;
        public float throtleForce ;

        private List <HoverCraft> l_hoverCrafts ;
        private List <HoverCraft> l_hoverCrafts2Destroy ;
        
        public Transform tr_debugPoiner ;
        // public PathNodes pathNodes ;

        public Cinemachine.CinemachineFreeLook cinemachine ;
        private float f_cinemachineMaxSpeed ;

        // Use this for initialization
        void Start ( )
        {
            f_cinemachineMaxSpeed = 0 ;

            l_hoverCrafts = new List <HoverCraft> ( i_spawnCount ) ;
            l_hoverCrafts.Add ( new HoverCraft () ) ;

            l_hoverCrafts2Destroy = new List<HoverCraft> ( 100 ) ;

            for ( int i = 0; i < l_hoverCrafts.Count; i++ )
            {
                Transform tr = GameObject.Instantiate ( hoverCraftPrefab ) ;

                HoverCraft hoverCraft = l_hoverCrafts [i] ;

                hoverCraft.tr_craft = tr ;

                hoverCraft.f_force        = force ;
                hoverCraft.f_throtleForce = throtleForce ;

                hoverCraft._Initialization () ;
            }

        }

        // Update is called once per frame
        void Update ( )
        {
            
            
            Ray pointerRay = Camera.main.ScreenPointToRay ( Input.mousePosition ) ;

            RaycastHit pointerHit ;
        
            if ( Physics.Raycast ( pointerRay, out pointerHit, 150 ) )
            {
                Debug.DrawLine ( pointerRay.origin, pointerHit.point, Color.blue ) ;

                tr_debugPoiner.position        = pointerHit.point ;


            }


            for ( int i = 0; i < l_hoverCrafts.Count; i++ )
            {
                HoverCraft hoverCraft      = l_hoverCrafts [i] ;
                hoverCraft.f3_turretLookAt = tr_debugPoiner.position ;

                if ( !hoverCraft._Update () )
                {
                    l_hoverCrafts2Destroy.Add ( hoverCraft ) ;
                }
            }

            for ( int i = 0; i < l_hoverCrafts2Destroy.Count; i++ )
            {
                HoverCraft hoverCraft      = l_hoverCrafts2Destroy [i] ;
                GameObject.Destroy ( hoverCraft.tr_craft.gameObject ) ;

                l_hoverCrafts.Remove ( hoverCraft ) ;
            }

            l_hoverCrafts2Destroy.Clear () ;

            
            if ( Input.GetKeyDown ( KeyCode.LeftShift ) && f_cinemachineMaxSpeed == 0 )
            {
                // Store.
                f_cinemachineMaxSpeed = cinemachine.m_XAxis.m_MaxSpeed;
                cinemachine.m_XAxis.m_MaxSpeed = 0;

                // Debug.Log ( "Store" ) ;
            }
        
            if ( Input.GetKeyUp ( KeyCode.LeftShift ) && f_cinemachineMaxSpeed != 0 )
            {
                // Restore.
                cinemachine.m_XAxis.m_MaxSpeed = f_cinemachineMaxSpeed ;
                f_cinemachineMaxSpeed          = 0 ;

                // Debug.Log ( "ReStore" ) ;
            }


        }
        void FixedUpdate ( )
        {
            
            for ( int i = 0; i < l_hoverCrafts.Count; i++ )
            {
                HoverCraft hoverCraft = l_hoverCrafts [i] ;
                hoverCraft._FixedUpdate () ;
                
            
                if ( Input.GetKey ( KeyCode.A ) )
                {
                    hoverCraft._TurnLeft () ;
                }
                else if ( Input.GetKey ( KeyCode.D ) )
                {
                    hoverCraft._TurnRight () ;
                }

                if ( Input.GetKey ( KeyCode.W ) )
                {
                    hoverCraft._MoveForward () ;
                }
                else if ( Input.GetKey ( KeyCode.S ) )
                {
                    hoverCraft._MoveBack () ;
                }

                if ( Input.GetKey ( KeyCode.Z ) )
                {
                    hoverCraft._StrafeLeft () ;
                } 
                else if ( Input.GetKey ( KeyCode.C ) )
                {
                    hoverCraft._StrafeRight () ;
                }
            }

        }

    }
}