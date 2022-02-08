using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Registers Impact on the RigidBody of the hand. When an impact happens, we determine where on the hand it occurs, and map it to an intensity. </summary>
    public class SG_ImpactFeedback : MonoBehaviour
    {
        /// <summary> The GameObject from which te retrieve the hapitcHardware. </summary>
        public GameObject hapticsObject;

        /// <summary> Hardware that can accept the SenseGlove Impact commands. </summary>
        public IHandFeedbackDevice hapticHardware;
    
        /// <summary> If true, this script will send vibrotactile feedback on impact. </summary>
        [Header("Impact Feedback")]
        public bool impactFeedbackEnabled = true;
        /// <summary> The minimum time, in seconds, between impact vibration. </summary>
        public float impactCooldown = 0.5f;
        /// <summary> Keeps track of time since last vibration </summary>
        protected float cooldownTimer = 0;

        /// <summary> The minimum speed, in m\s, that this object must make before an impact is played. </summary>
        public float minImpactSpeed = 3;
        /// <summary> The speed, in m/s, where the maxiumum vibration level is sent. </summary>
        public float maxImpactSpeed = 5f;
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


        /// <summary> Returns true if this script can send an impact vibration </summary>
        public bool CanImpact
        {
            get { return cooldownTimer >= impactCooldown; }
        }

        /// <summary> Returns the average velocity over the last few frames </summary>
        /// <returns></returns>
        public Vector3 SmoothedVelocity
        {
            get { return SG.Util.SG_Util.Average(this.velocities); }
        }


        /// <summary> Update this collider's position, and register its velocity. </summary>
        public void UpdateVelocity(float dT)
        {
            Vector3 currPos = this.transform.position;
            Vector3 velocity = ((currPos - lastPosition) / dT);

            velocities.Add(velocity);
            if (velocities.Count > maxVelocityPoints) { velocities.RemoveAt(0); }

            lastPosition = currPos;
        }

        private void Start()
        {
            if (this.hapticHardware == null && this.hapticsObject != null)
            {
                this.hapticHardware = this.hapticsObject.GetComponent<IHandFeedbackDevice>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            UpdateVelocity(Time.deltaTime);
            //update cooldown
            if (cooldownTimer <= impactCooldown)
            {
                cooldownTimer += Time.deltaTime;
            }
        }


        private void OnCollisionEnter(Collision collision)
        {
            if (CanImpact)
            {
                float magnitude = SmoothedVelocity.magnitude;
                if (magnitude >= this.minImpactSpeed)
                {
                    cooldownTimer = 0;
#if UNITY_2017
                    ContactPoint firstContact = collision.contacts[0];
#else
                    ContactPoint firstContact = collision.GetContact(0);
#endif
          //          Debug.Log(this.name + ": Collision between " + firstContact.thisCollider.name + " and " + firstContact.otherCollider.name + " v = " + magnitude + "m/s");

                    float normalizedSpeed = SGCore.Kinematics.Values.Map(magnitude, this.minImpactSpeed, this.maxImpactSpeed, 0, 1, 0, 1);
                    float normalizedVibration = this.impactProfile.Evaluate(normalizedSpeed);
                    hapticHardware.SendImpactVibration(SG_HandSection.Wrist, normalizedVibration);
                    
                }
            }
        }


    }
}