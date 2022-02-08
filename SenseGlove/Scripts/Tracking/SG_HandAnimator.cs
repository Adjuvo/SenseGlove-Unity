using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    /// <summary> 
    /// A script that, while active, collects SG_HandPoses from a linked Hardware and animates a hand model based on the joints in its HandModelInfo. 
    /// Note that, in order to map joint angles properly, the fingers of the hand must be aligned along the x-axis of the WristTransform.
    /// </summary>
    public class SG_HandAnimator : SG_HandComponent
    {
        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Properties Variables

        #region Properties

        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Public Variables

        /// <summary> HandModel information. </summary>
        [Header("Animation Components")]
        public SG_HandModelInfo handModelInfo;

        /// <summary> Whether or not to update the fingers of this Hand Model. </summary>
        public bool[] updateFingers = new bool[5] { true, true, true, true, true };

        /// <summary> Whether or not to update the wrist of this Hand Model. </summary>
        [Tooltip("Whether or not to update the wrist of this Hand Model.")]
        public bool imuForWrist = false;


        /// <summary> Check for Scripts relevant for this Animator </summary>
        public void CheckForScripts()
        {
            if (handModelInfo == null)
            {
                SG.Util.SG_Util.CheckForHandInfo(this.transform, ref this.handModelInfo); ;
            }
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Internal Variables.

        /// <summary> Quaternion that aligns the lower arm with the wrist at the moment of calibration. </summary>
        protected Quaternion wristCalibration = Quaternion.identity;

        /// <summary> The relative angles between wrist and lower arm transforms. </summary>
        protected Quaternion wristAngles = Quaternion.identity;


        #endregion Properties


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // HandModel Calibration

        /// <summary> Calibrate the wrist model of this handModel. </summary>
        public virtual void CalibrateWrist(Quaternion imuRotation)
        {
            if (handModelInfo != null && handModelInfo.foreArmTransform != null)
            {
                // Debug.Log(this.name + ": Calibrated Wrist");
                this.wristCalibration = handModelInfo.foreArmTransform.rotation * Quaternion.Inverse(imuRotation);
            }
        }


        //------------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Retrieve the Quaterion rotation between this model's foreArm and Wrist. </summary>
        public Quaternion RelativeWrist
        {
            get { return this.wristAngles; }
        }

        /// <summary> Retrieve the Wrist Position as determined by this HandModel. </summary>
        public Vector3 WristPosition
        {
            get { return this.handModelInfo.wristTransform.position; }
        }

        /// <summary> Returns the world rotation of this hand's wrist. </summary>
        public Quaternion WristRotation
        {
            get { return this.handModelInfo.wristTransform.rotation; }
        }

        /// <summary> Retrive the euler angles between this model's foreArm and Wrist.  </summary>
        public Vector3 WristAngles
        {
            get { return SG.Util.SG_Util.NormalizeAngles(this.wristAngles.eulerAngles); }
        }

        /// <summary> Global accessor to enable / disable all finger animations </summary>
        public bool AnimateFingers
        {
            get 
            {
                for (int f = 0; f < this.updateFingers.Length; f++)
                {
                    if (!updateFingers[f]) { return false; }
                }
                return true;
            }
            set
            {
                for (int f = 0; f < this.updateFingers.Length; f++) { this.updateFingers[f] = value; }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Update Methods

        #region Update

        
        /// <summary> 
        /// Update the (absolute) finger orientations, which move realtive to the (absolute) wrist transform. 
        /// Note: This method is called after UpdateWrist() is called. 
        /// </summary>
        /// <param name="data"></param>
        public virtual void UpdateHand(SG_HandPose pose, bool fingersOnly = false)
        {
            if (pose != null)
            {
                if (!fingersOnly)
                {
                    handModelInfo.wristTransform.rotation = pose.wristRotation;
                    handModelInfo.wristTransform.position = pose.wristPosition;
                }

                Quaternion[][] angles = pose.jointRotations;
                Quaternion[][] corrections = handModelInfo.FingerCorrections;
                Transform[][] fingerJoints = handModelInfo.FingerJoints;
                if (corrections != null && fingerJoints != null)
                {
                    for (int f = 0; f < fingerJoints.Length; f++)
                    {
                        if (pose.jointRotations.Length > f)
                        {
                            for (int j = 0; j < fingerJoints[f].Length; j++)
                            {
                                if (pose.jointRotations[f].Length > j)
                                {
                                    fingerJoints[f][j].rotation = handModelInfo.wristTransform.rotation
                                        * (angles[f][j] * corrections[f][j]);
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary> 
        /// Update the (absolute) wrist orientation, which moves realtive to the (absolute) lower arm transform. 
        /// Note: This method is called before UpdateFingers() is called.  
        /// </summary>
        /// <param name="data"></param>
        public virtual void UpdateWrist(Quaternion imuRotation)
        {
            if (imuForWrist)
            {
                handModelInfo.wristTransform.rotation = handModelInfo.WristCorrection * (this.wristCalibration * imuRotation);
                this.wristAngles = Quaternion.Inverse(handModelInfo.foreArmTransform.rotation) * handModelInfo.wristTransform.rotation;
            }
            else
            {
                handModelInfo.wristTransform.rotation = handModelInfo.WristCorrection * handModelInfo.foreArmTransform.rotation;
                this.wristAngles = Quaternion.identity; //ignore wrist angle(s).
            }
        }


        /// <summary> Resize the finger lengths of this hand model to reflect that of the current user. </summary>
        /// <param name="newLengths"></param>
        public virtual void ResizeHand(float[][] newLengths) { }

        /// <summary> Reset this hand size back to its original sizing. </summary>
        public virtual void ResetHandSize() { }

        #endregion Update



        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        #region Monobehaviour

        // Use this for initialization
        protected virtual void OnEnable()
        {
            CheckForScripts();
        }

        protected virtual void Start()
        {
            //sets the animation in a starting pose.
            //SG_HandPose startPose = SG_HandPose.Idle(this.handModelInfo != null ? this.handModelInfo.handSide != HandSide.LeftHand : true);
            //this.UpdateHand(startPose);
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            //if (this.TrackedHand != null)
            //{
            //    this.UpdateWrist( TrackedHand.GetIMURotation() ) ;
            //    SG_HandPose handPose;
            //    if (this.TrackedHand.GetHandPose(out handPose))
            //    {   
            //        this.UpdateHand(handPose);
            //    }
            //}
        }

        #endregion Monobehaviour


    }
}