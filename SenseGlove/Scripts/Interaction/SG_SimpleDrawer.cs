using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{



    /// <summary> A SG_Interactable that moves along a single (local) axis. You define hof far you can push it in or pull it out from its starting location. </summary>
    public class SG_SimpleDrawer : SG_Grabable, IOutputs01Value, IControlledBy01Value
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        // Drawer Axis

        /// <summary> The axis along which the drawer is moved. </summary>
        public enum DrawerAxis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Local(!) axis along which to move. </summary>
        [Header("Drawer Components")]
        public DrawerAxis moveAxis = DrawerAxis.Z;

        protected Vector3 localStartPos = Vector3.zero;
        protected Quaternion localStartRot = Quaternion.identity;

        /// <summary> How far can the drawer be pushed in from it's starting position. Should be Negative </summary>
        public float pushDistance = 0.0f;
        /// <summary> How far can the drawer be pulled out from it's starting position. Should be Positive </summary>
        public float pullDistance = 1.0f;

        /// <summary> The distance that the drawer has moved in world space [m] </summary>
        public float drawerDist = 0;
        /// <summary> How far the drawer had been pushed in/slid out, as a decima; [0 = fully pushed in, 1 = fully pulles out] </summary>
        [Range(0, 1)] public float drawer_slideValue = 0;


        /// <summary> INormalizedValue provider implementation </summary>
        /// <returns></returns>
        public virtual float Get01Value()
        {
            return drawer_slideValue;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        // SG_Grabable Overrides

        protected override void SetupScript()
        {
            base.SetupScript();

            this.SetPhysicsbody(false, this.physicsBody != null ? this.physicsBody.isKinematic : true, RigidbodyConstraints.FreezeRotation); //unlock the movement, freeze the rotation.
            this.UpdateRigidbodyDefaults(); //should always return to this.

            RecalculateBaseLocation();
        }


        /// <summary> Updates the drawer's position </summary>
        /// <param name="dT"></param>
        protected override void UpdateLocation(float dT)
        {
            List<GrabArguments> heldBy = this.grabbedBy;
            if (heldBy.Count > 0) //I'm actually grabbed by something
            {
                Vector3 targetPosition; Quaternion targetRotation;
                CalculateTargetLocation(heldBy, out targetPosition, out targetRotation);
                //My targetPosition is projected onto the MovementAxis. My targetRotation is ignored (set to parent).

                Vector3 projPos; Quaternion projRot;
                CalculateDrawerTarget(this, targetPosition, out projPos, out projRot, out this.drawerDist);
                CalculateSliderValue();

                MoveToTargetLocation(projPos, projRot, dT);
            }
            else if (this.IsMovedByPhysics) //I have a physicsBody
            {
                throw new System.NotImplementedException("The SG_Drawer feature is not yet available for non-Kinematic Rigidbodies.");
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        // Drawer Functions

        /// <summary> Re-Calculates the  </summary>
        protected void CalculateSliderValue()
        {
            float push = -Mathf.Abs(this.pushDistance);
            float pull = Mathf.Abs(this.pullDistance);
            this.drawer_slideValue = SG.Util.SG_Util.Map(drawerDist, push, pull, 0, 1);  //no need to clamp, the drawerDist is locked.
        }

        /// <summary> Set the current location of the drawer as "0" for the drawerDistance. </summary>
        public void RecalculateBaseLocation()
        {
            SG.Util.SG_Util.CalculateBaseLocation(this.MyTransform, out localStartPos, out localStartRot);
        }

        /// <summary> Returns the position & rotation of the Drawer's base. </summary>
        public void GetBaseLocation(out Vector3 basePos, out Quaternion baseRot)
        {
            SG.Util.SG_Util.GetCurrentBaseLocation(this.MyTransform, localStartPos, localStartRot, out basePos, out baseRot);
        }

        /// <summary> Forward directional vector along which we should be moving now. </summary>
        public Vector3 DrawerDirection
        {
            get
            {
                Vector3 localAxis = SG.Util.SG_Util.GetAxis( (Util.MoveAxis) this.moveAxis ); //I can cast to the Utils moveAxis because the also use XYZ at the start
                return this.transform.TransformDirection(localAxis);
            }
        }

        /// <summary> Calculate a drawer target position. Placed in a static function so it can be used bo ther scripts that use sliders. </summary>
        /// <param name="drawer"></param>
        /// <param name="nextPositon"></param>
        /// <param name="targetPos"></param>
        /// <param name="targetRot"></param>
        /// <param name="drawerDist"></param>
        public static void CalculateDrawerTarget(SG_SimpleDrawer drawer, Vector3 nextPositon, out Vector3 targetPos, out Quaternion targetRot, out float drawerDist)
        {
           // Calculate the ference point for this drawer
            Vector3 basePos;
            drawer.GetBaseLocation(out basePos, out targetRot);

            // Project targetPosition onto the movement "plane" 
            Vector3 projectedPos = SG.Util.SG_Util.ProjectOnTransform(nextPositon, basePos, targetRot);
            int ax = (int)drawer.moveAxis;
            for (int i = 0; i < 3; i++)
            {
                if (i != ax) { projectedPos[i] = 0; }
            }

            //ToDO: Limit movement
            projectedPos[ax] = Mathf.Clamp(projectedPos[ax], -Mathf.Abs(drawer.pushDistance), drawer.pullDistance);
            drawerDist = projectedPos[ax];
            
            //finally: Reproject local back to abs.
            targetPos = basePos + (targetRot * projectedPos);
        }

        

        /// <summary> Sets the drawer to a certain pulled-out value (0 = fully pushed in, 1 = fully pulled out) </summary>
        /// <param name="slideValue01"></param>
        public void SetDrawerAt(float slideValue01)
        {
            slideValue01 = Mathf.Clamp01(slideValue01);
            if (slideValue01 != this.drawer_slideValue) //optimization: Don't change if you're already there.
            {
                this.drawer_slideValue = slideValue01;

                //Calculate displacement from base POsition
                float push = -Mathf.Abs(this.pushDistance);
                float pull = Mathf.Abs(this.pullDistance);
                float localDisplacement = SG.Util.SG_Util.Map(slideValue01, 0, 1, push, pull);  //no need to clamp, the drawerDist is locked.

                Vector3 localPos = Vector3.zero;
                int ax = (int)this.moveAxis;
                localPos[ax] = localDisplacement;

                //now to set this...

                Vector3 basePos; Quaternion baseRot;
                this.GetBaseLocation(out basePos, out baseRot);

                Vector3 targetDrawerPos = basePos + (baseRot * localPos);
                this.MyTransform.position = targetDrawerPos;
            }
        }

        /// <summary> Ssets the Drawer position at a specific point between its start-and end location(s). </summary>
        /// <param name="value01"></param>
        public void SetControlValue(float value01)
        {
            SetDrawerAt(value01);
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected override void Awake()
        {
            base.Awake();
            this.CalculateSliderValue();
        }

  
    }

}