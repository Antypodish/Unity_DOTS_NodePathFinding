using System.Collections ;
using System.Collections.Generic ;
using UnityEngine ;

using Antypodish.Hove ;

namespace Antypodish.Hove.OOP
{

    public class HoverCraft
    {

        public float f_force ;
        public float f_throtleForce ;
        
        public Transform tr_craft ;
        private Transform tr ;
        private Rigidbody rb ;

        private Transform tr_turret ;
        private HingeJoint hj_turret ;

        private float f_currentTurretAngle ;

        public Vector3 f3_turretLookAt ;
        
        PathFinding.PathFinding pathFinding ;
        // PathFinding.PathNodes path ;

        /// <summary>
        /// Neural network based movement controller.
        /// </summary>
        NEAT_HoverCraftMovementController NEAT_HoverCraftMovementController ;

        public void _Initialization ()
        {

            tr                                = tr_craft.FindChild ( "Body" ) ;
            rb                                = tr.GetComponent <Rigidbody> () ;
            hj_turret                         = tr.GetComponent <HingeJoint> () ;
            tr_turret                         = hj_turret.connectedBody.transform ;
            f_currentTurretAngle              = 0 ;

            pathFinding                       = new PathFinding.PathFinding () ;

            NEAT_HoverCraftMovementController = new NEAT_HoverCraftMovementController () ;

            NEAT_HoverCraftMovementController._Initialize () ;
            NEAT_HoverCraftMovementController.f3_lookAtTarget    = Vector3.right * 5 ; 
            NEAT_HoverCraftMovementController.f3_facingDirection = tr.forward ;
            NEAT_HoverCraftMovementController.canTrain           = true ;

        }
    
        public bool _Update ()
        {
            // Debug.Log ( V3_crosssForward + "; " + V3_crosssLeft + "; " + f_dotPitch + "; " + f_dotRoll ) ;

            // Turret look direction
            {
                Vector3 V3_turretLookDirection = ( f3_turretLookAt - tr_turret.position ).normalized ;

                Vector3 V3_crosssLeft          = Vector3.Cross ( V3_turretLookDirection, Vector3.up ) ;
                Vector3 V3_crosssForward       = -Vector3.Cross ( V3_crosssLeft, Vector3.up ) ;
            
                // Debug.DrawLine ( tr_turret.position, tr_turret.position + V3_crosssLeft * 10, Color.blue ) ;
                // Debug.DrawLine ( tr_turret.position, tr_turret.position + V3_crosssForward * 10, Color.yellow ) ;
                // Debug.DrawRay ( tr_turret.position, tr_turret.position + V3_turretLookDirection * 10, Color.green ) ;

                float f_angleYaw        = Vector3.SignedAngle ( tr.forward, V3_crosssForward, tr_turret.up ) ;

                // Debug.Log ( f_angleYaw + "; " + "; " + ( f_angleYaw >= 0 ? f_angleYaw : f_angleYaw + 360 ) + "; " + ( f_angleYaw >= 0 ? f_angleYaw : f_angleYaw + 360 * 0.9 ) + "") ;
                // f_angleYaw = f_angleYaw >= 0 ? f_angleYaw : f_angleYaw + 360 ;

                float f_angleDiff       = -hj_turret.angle - f_angleYaw ;
                float f_angleCorrection = f_angleDiff < -180 ? f_angleDiff + 360 : ( f_angleDiff > 180 ? f_angleDiff - 360 : f_angleDiff ) ;
                JointMotor jm           = hj_turret.motor ;
                jm.targetVelocity       = f_angleCorrection < 1 ? -40 : ( f_angleCorrection > 1 ? 40 : 0 ) ;
                hj_turret.motor         = jm ;

                // float f_angleDiff = f_currentTurretAngle - f_angleYaw ;
                // float f_angleCorrection = f_angleDiff < -180 ? f_angleDiff + 360 : ( f_angleDiff > 180 ? f_angleDiff - 360 : f_angleDiff ) ;

                // Debug.Log ( hj_turret.angle + " - " + f_angleYaw + " = " + f_angleDiff ) ;

                // f_currentTurretAngle += f_angleCorrection >= 1f ? -1f : ( f_angleCorrection < -1f ? 1f : 0 ) ;


                // tr_turret.localRotation = Quaternion.AngleAxis ( f_currentTurretAngle, Vector3.up ) ; 
            }
        
            // Debug.DrawRay ( pointerRay.origin, pointerRay.direction, Color.red ) ;

            int i_layerMask = 1 << LayerMask.NameToLayer ( "PathNodes" ) ;

            Collider [] a_collidersHit      = Physics.OverlapBox ( tr.position, new Vector3 ( 20, 1, 20 ), Quaternion.identity, i_layerMask ) ;
            // RaycastHit [] a_rayHits      = Physics.BoxCastAll ( transform.position, new Vector3 ( 4, 1, 4 ), transform.forward, transform.rotation, 25, i_layerMask ) ;

            float f_closestPathNodeDistance = 99999 ;
            Vector3 V3_closestPathNode      = Vector3.zero ;

            // Debug.Log ( a_collidersHit.Length ) ;

            for ( int i = 0; i < a_collidersHit.Length; i ++ )
            {

                Collider colliderHit = a_collidersHit [i] ;
            
                // Debug.Log ( "i " + i + "; " + a_collidersHit.Length + "; " + transform.position + "; " + V3_closestPathNode ) ;

                // Debug.Log ( i + "; " + rayHits.point ) ;

                Vector3 V3_posDiff = colliderHit.transform.position - tr.position ;
                float f_sqrMagn    = V3_posDiff.sqrMagnitude ;

                if ( f_sqrMagn > 0 && f_sqrMagn < f_closestPathNodeDistance )
                {
                    f_closestPathNodeDistance = f_sqrMagn ;
                    V3_closestPathNode        = colliderHit.transform.position ;

                    // Debug.Log ( transform.position + "; " + rayHits.point ) ;
                }

            } // for

            if ( f_closestPathNodeDistance < 99999 )
            {
                Debug.DrawLine ( tr.position, V3_closestPathNode , Color.red ) ;
                pathFinding._SetStartNode ( V3_closestPathNode ) ;

                // PathNodes.Node targetNode = pathNodes.dic_nodes [...] ;
                // List <Vector3> l_path = new List<Vector3> ( PathFinding.PathNodes._GetNodesCount () ) ;
                
                // pathFinding._PathFindingAStar () ;
                
                for ( int i = 1; pathFinding.l_path != null && i < pathFinding.l_path.Count; i ++ )
                {
                    Debug.DrawLine ( pathFinding.l_path [i], pathFinding.l_path [i -1], Color.magenta, 1 ) ;
// Debug.Log ( i + " / " + pathFinding.l_path.Count + "; -1: " + pathFinding.l_path [i-1].ToString ("F4") + "; Target: " + pathFinding.l_path [i].ToString ("F4") ) ;
                }

            }
            

            Ray pointerRay = Camera.main.ScreenPointToRay ( Input.mousePosition ) ;

            RaycastHit hit ;

            if ( Input.GetMouseButtonUp ( 0 ) && Physics.Raycast ( pointerRay, out hit, 150 ) )
            {
                pathFinding._SetNewDestination ( hit.transform.position ) ;
            }


            // Neural network brain and training.            
            return _NNBrain () ;
            // Debug.Log () ;

            // V3_mousePointer

            // float f_angle = 0 ;

            // tr_turret.transform.localRotation = Quaternion.AngleAxis ( f_angle, Vector3.up ) ; ;

            // return true ;
        }

        /// <summary>
        /// Neural network brain and training.  
        /// </summary>
        /// <returns></returns>
        public bool _NNBrain ()
        {
            NEAT_HoverCraftMovementController.f3_lookAtTarget    = Vector3.right * 5 ; 
            NEAT_HoverCraftMovementController.f3_facingDirection = tr.forward ;

            NEAT_HoverCraftMovementController._Update () ;

            if ( NEAT_HoverCraftMovementController.canTrain )
            {
                NEAT_HoverCraftMovementController._IsFinished () ;
                
                return false ;
            }

            return true ;
        }

        public void _FixedUpdate ()
        {
        
            Vector3 V3_crosssLeft    = Vector3.Cross ( rb.transform.forward, Vector3.up ) ;
            Vector3 V3_crosssForward = -Vector3.Cross ( V3_crosssLeft, Vector3.up ) ;
            // Vector3 V3_forward    = Vector3.ProjectOnPlane ( rb.transform.forward, new Vector3 ( 1, 0, 1 ) ) ;
            float f_anglePitch       = Vector3.SignedAngle ( V3_crosssForward, rb.transform.forward, V3_crosssLeft ) / 180 ;
        
        
            // Debug.DrawRay ( rb.transform.position, V3_crosssLeft, Color.red ) ;
            // Debug.DrawRay ( rb.transform.position, V3_crosssForward, Color.green ) ;

            // V3_crosssForward         = Vector3.Cross ( rb.transform.right, Vector3.up ) ;
            // V3_crosssLeft            = Vector3.Cross ( V3_crosssForward, Vector3.up ) ;

            // Vector3 V3_right = Vector3.ProjectOnPlane ( rb.transform.right, new Vector3 ( 1, 0, 1) ) ;
            // float f_dotRoll = Vector3.Dot ( rb.transform.forward, V3_crosssLeft ) ;
            float f_angleRoll       = Vector3.SignedAngle ( V3_crosssLeft, -rb.transform.right, V3_crosssForward ) / 180 ;


            // Debug.DrawRay ( Vector3.zero, V3_crosssForward, Color.red ) ;
            // Debug.Log ( f_anglePitch + "; " + f_angleRoll ) ;
        

            float f_maxDistance = 2 ;

            Vector3 V3_posOffset ;

            float f_frontForce = f_anglePitch < 0 ? f_force + f_force * -f_anglePitch * 10 : f_force ;
            float f_backForce = f_anglePitch > 0 ? f_force + f_force * f_anglePitch * 10 : f_force ;

            V3_posOffset = new Vector3 ( 1, 0, 1 ) * 0.5f ;
            _Hover ( rb, V3_posOffset, f_maxDistance, f_frontForce ) ;
        
            V3_posOffset = new Vector3 ( -1, 0, 1 ) * 0.5f ;
            _Hover ( rb, V3_posOffset, f_maxDistance, f_frontForce ) ;

            V3_posOffset = new Vector3 ( 1, 0, -1 ) * 0.5f ;
            _Hover ( rb, V3_posOffset, f_maxDistance, f_backForce ) ;
        
            V3_posOffset = new Vector3 ( -1, 0, -1 ) * 0.5f ;
            _Hover ( rb, V3_posOffset, f_maxDistance, f_backForce ) ;

            int i_turnLeftOrRight = NEAT_HoverCraftMovementController._GetTurnLeftOrRight () ;
            if ( i_turnLeftOrRight < 0 ) _TurnLeft () ;
            if ( i_turnLeftOrRight > 0 ) _TurnRight () ;
        }

        public void _TurnLeft ( )
        {
            rb.AddRelativeTorque ( -Vector3.up * f_throtleForce ) ;
        }
        
        public void _TurnRight ( )
        {
            rb.AddRelativeTorque ( Vector3.up * f_throtleForce ) ;
        }
        
        public void _MoveForward ( )
        {
            rb.AddRelativeForce ( Vector3.forward * f_throtleForce ) ;
        }
        
        public void _MoveBack ( )
        {
            rb.AddRelativeForce ( -Vector3.forward * f_throtleForce ) ;
        }

        public void _StrafeLeft ( )
        {
            rb.AddRelativeForce ( Vector3.left * f_throtleForce ) ;
        }
        public void _StrafeRight ( )
        {
            rb.AddRelativeForce ( -Vector3.left * f_throtleForce ) ;
        }

        static private void _Hover ( Rigidbody rb, Vector3 V3_posOffset, float f_maxDistance, float force )
        {
        
            float f_localUp = 0 ; // force ;

            // Exclude mask layer.
            int i_maskLayer = -1 ^ MaskLayers.i_pathNodes ;
            Vector3 V3_pos = rb.transform.position + rb.rotation * V3_posOffset ;
        
            Ray ray = new Ray ( V3_pos, -rb.transform.up ) ;
            RaycastHit hit ;

            if ( Physics.Raycast ( ray, out hit, f_maxDistance, i_maskLayer ) )
            {
                float f_distance2Ground = ( f_maxDistance - hit.distance ) ;
                f_localUp = force + force * f_distance2Ground * f_distance2Ground ;
            }
        
            Vector3 V3_force = rb.rotation * Vector3.up * f_localUp ;
            rb.AddForceAtPosition ( V3_force, V3_pos ) ;

            // Debug.DrawRay ( V3_pos, V3_force.normalized, Color.red, 1 ) ;
            // Debug.Log ( V3_pos ) ;
        }

    }

}