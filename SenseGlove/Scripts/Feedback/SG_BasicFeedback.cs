using SG.Util;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    /// <summary> Attach to a collider and it will send haptic feedback to a linked glove on impact. Optionally tracks a GameObject.
    /// Extended by SG_Finger to apply more forces. </summary>
    public class SG_BasicFeedback : SG_SimpleTracking
    {
        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Sense Glove that will receive the feedback effect </summary>
        [Header("Hand Settings")]
        public SG_HapticGlove linkedGlove;

        /// <summary> Access the TrackedHand, through which one can send/receive SenseGlove data. </summary>
        public SG_HandFeedback feedbackScript;

        /// <summary> The part of the hand that this script belongs to. </summary>
        public SG_HandSection handLocation = SG_HandSection.Unknown;

        /// <summary> If true, this script will send vibrotactile feedback on impact. </summary>
        [Header("Impact Feedback")]
        public bool impactFeedbackEnabled = true;
        /// <summary> The minimum time, in seconds, between impact vibration. </summary>
        public float impactCooldown = 0.5f;

        /// <summary> The minimum speed, in m\s, that this object must make before an impact is played. </summary>
        public float minImpactSpeed = 0.01f;
        /// <summary> The speed, in m/s, where the maxiumum vibration level is sent. </summary>
        public float maxImpactSpeed = 0.1f;
        /// <summary> A curve that determines how the impact vibration varies between the minimum and maximum impact speed. Set to constant (1) to have the same vibration no matter the speed. </summary>
        public AnimationCurve impactProfile = AnimationCurve.Linear(0, 0, 1, 1);

        /// <summary> The minimum vibration level at which an impact can be felt. </summary>
        protected static int minBuzzLevel = 50;
        /// <summary> The maximum vibration level to represent an impact. </summary>
        protected static int maxBuzzLevel = 80;
        /// <summary> The time to vibrate the buzz motors for. </summary>
        protected static float vibrationTime = 0.100f;

        /// <summary> The maximum frames for which to keep track of velocity. </summary>
        public static int maxVelocityPoints = 10;

        /// <summary> The xyz velocities during the last few frames, used to determine the average impact velocity. </summary>
        protected List<Vector3> velocities = new List<Vector3>(); //storing as xyz vector to allow for greater flexibility in the future.

        /// <summary> This object's position during the last frame, used to determine velocity. </summary>
        protected Vector3 lastPosition = Vector3.zero;

        /// <summary> Keeps track of time since last vibration </summary>
        protected float cooldownTimer = 0;


        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Used to show or hide this object's collider. </summary>
        public override bool DebugEnabled
        {
            get { return base.DebugEnabled; }
            set { base.DebugEnabled = value; }
        }

        /// <summary> Returns true if this script can send an impact vibration </summary>
        public bool CanImpact
        {
            get { return cooldownTimer >= impactCooldown; }
        }

        /// <summary> Returns the average velocity over the last few frames </summary>
        /// <returns></returns>
        public Vector3 SmoothedVelocity
        {
            get { return SG_Util.Average(this.velocities); }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Setup the SG_BasicFeedback script components  </summary>
        public virtual void SetupSelf()
        {
            //Debug.Log(this.name + "Calculated Offsets!");
            SG_Util.TryAddRB(this.gameObject, false, true);
            cooldownTimer = 0;
            SetTrackingTarget(this.trackingTarget, true); //updates offsets on start
            this.lastPosition = this.transform.position;

            if (this.linkedGlove == null && this.feedbackScript != null && this.feedbackScript.TrackedHand != null) 
            { 
                this.linkedGlove = this.feedbackScript.TrackedHand.gloveHardware; 
            }
        }


        /// <summary> Send impact feedback to the linked glove, based on an impact-velocity. </summary>
        /// <param name="impactVelocity"></param>
        public void SendImpactFeedback(float impactVelocity)
        {
            SendImpactFeedback(impactVelocity, null);
        }


        /// <summary> Send an impact vibration to this script's connected glove, based on a speed in m/s. Checks for SG_RigidBodies. </summary>
        /// <param name="impactVelocity"></param>
        public void SendImpactFeedback(float impactVelocity, Collider col)
        {
            if (handLocation != SG_HandSection.Unknown && impactVelocity >= minImpactSpeed)
            {
                SG_TrackedBody rbScript;
                if (col == null || (col != null && !SG_TrackedBody.GetTrackedBodyScript(col, out rbScript))) //we either don't have a collider, or we do, but it doesn't have a RigidBody script.
                {
                    int impactLevel;
                    //evaluate the intensity
                    if (impactVelocity > maxImpactSpeed || minImpactSpeed >= maxImpactSpeed)
                    {
                        impactLevel = maxBuzzLevel;
                    }
                    else
                    {
                        //map the impact parameters on the speed.
                        float mapped = Mathf.Clamp01(SG_Util.Map(impactVelocity, minImpactSpeed, maxImpactSpeed, 0, 1));
                        //we're actually start at minBuzzLevel; that's when you can start to feel the Sense Glove vibrations
                        impactLevel = (int)(SG_Util.Map(impactProfile.Evaluate(mapped), 0, 1, minBuzzLevel, maxBuzzLevel));
                    }
                    //actually send the effect
                    if (linkedGlove != null && vibrationTime > 0)
                    {
                        if (handLocation == SG_HandSection.Wrist) //it's a wrist.
                        {
                            linkedGlove.SendCmd(new TimedThumpCmd(impactLevel, vibrationTime));
                        }
                        else //finger
                        {
                            SGCore.Finger finger = (SGCore.Finger)handLocation; //can do this since the finger indices match
                            SGCore.Haptics.SG_TimedBuzzCmd buzzCmd = new SGCore.Haptics.SG_TimedBuzzCmd(finger, impactLevel, vibrationTime);
                            linkedGlove.SendCmd(buzzCmd);
                        }
                        cooldownTimer = 0; //reset cooldown for this finger.
                    }
                }
            }
        }


        /// <summary> Update this collider's position, and register its velocity. </summary>
        protected override void UpdatePosition()
        {
            base.UpdatePosition();
            Vector3 currPos = this.transform.position;
            Vector3 velocity = ((currPos - lastPosition) / Time.deltaTime);

            velocities.Add(velocity);
            if (velocities.Count > maxVelocityPoints) { velocities.RemoveAt(0); }

            lastPosition = currPos;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        protected override void Awake()
        {
            SetupSelf();
        }

        protected override void FixedUpdate() //physics updates
        {
            base.FixedUpdate();
            //update cooldown
            if (cooldownTimer <= impactCooldown)
            {
                cooldownTimer += Time.fixedDeltaTime;
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (impactFeedbackEnabled && CanImpact && !other.isTrigger)
            {
                Vector3 currentVelocity = this.SmoothedVelocity;
                //Debug.Log(this.name + " bumping " + other.name);
                SendImpactFeedback(this.SmoothedVelocity.magnitude, other);
            }
        }

    }
}