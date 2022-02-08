using SG.VR;
using UnityEngine;

namespace SG
{
	/// <summary> Utility Script that serves as the bridge between VR RIgs and up to two hands. If no VRRigs are assigned, it can be used to  </summary>
	public class SG_User : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The Left TrackedHand.Hidden until it(re)connects) </summary>
        public SG_TrackedHand leftHand;

		/// <summary> The Right TrackedHand.Hidden until it(re)connects) </summary>
		public SG_TrackedHand rightHand;

        /// <summary> Hotkey to swap SenseGlove hands. </summary>
        public KeyCode swapHandsKey = KeyCode.None;


		/// <summary> Optional component to have the user assign the correct variables of a VR headset </summary>
		public SG_VR_Rig vrRig;

		/// <summary> Optional component that automatically detects vr headsets. </summary>
		public SG_VR_Setup headsetDetection;

		/// <summary> Whether or nog we should still be checking for VR assets.  </summary>
		protected bool vrInit = true;

        /// <summary> If true, the hands have been swapped from their original setup. </summary>
        protected bool handsSwapped = false;


        /// <summary> Used to check whether or not we swapped last time, which becomes relevant if we're using Vives. </summary>
        public const string swappedKey = "sgSwap";

        /// <summary> HapticGlove components. Since we're no longer sure if HapticGloves are the actual TrackingSource. </summary>
        protected SG_HapticGlove leftGlove = null, rightGlove = null;

        /// <summary> How ofter SG_Users check the hardware connections to see if models need switching. </summary>
        public static float hwCheckTime = 1.0f;
        /// <summary> The timer for this specific user. </summary>
        protected float timer_hwChecks = 0;


        //--------------------------------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> Accesses the left TrackedHand activation state </summary>
        public bool LeftHandEnabled
        {
            get { return leftHand != null && leftHand.HandModelEnabled; }
            set { if (leftHand != null) { leftHand.HandModelEnabled = value; } }
        }

        /// <summary> Accesses the right TrackedHand activation state </summary>
        public bool RightHandEnabled
        {
            get { return rightHand != null && rightHand.HandModelEnabled; }
            set { if (rightHand != null) { rightHand.HandModelEnabled = value; } }
        }


        /// <summary> retruns true is this user's left hand is connected </summary>
        public bool LeftHandConnected
        {
            get { return leftHand != null && leftHand.IsConnected(); }
        }

        /// <summary> Returns true if this user's right hand is connected </summary>
        public bool RightHandConnected
        {
            get { return rightHand != null && rightHand.IsConnected(); }
        }


        /// <summary> Returns the amount of gloves connected to this user [0 .. 1] </summary>
        public int ConnectedGloves
        {
			get
            {
                int n = 0;
                if (LeftHandConnected) { n++; }
                if (RightHandConnected) { n++; }
                return n;
            }
        }
		

		//--------------------------------------------------------------------------------------------------------------------------
		// Properties


		/// <summary> Swaps the trackedObjects of two TrackedHands. </summary>
		public void SwapHands()
        {
            this.handsSwapped = !handsSwapped;
            PlayerPrefs.SetInt(swappedKey, handsSwapped ? 1 : 0); //let the system know we swapped before.
            if (leftGlove != null && rightGlove != null)
            {
                leftGlove.SwapTracking(rightGlove); //swaps around the tracked objects
            }
            UpdateVisuals();
        }


		/// <summary> Assign a VR headset to this user, which also assignes tracking parameters to both hands. </summary>
		/// <param name="detectedVRSet"></param>
		public void AssignVRSet(SG_VR_Rig detectedVRSet)
        {
			if (vrInit)
			{
				vrInit = false; //no longer need to initilaize
				vrRig = detectedVRSet;
				Debug.Log("Assgined " + detectedVRSet.name + " to the SenseGlove setup");

                if (leftGlove != null) { leftGlove.SetTrackingHardware(vrRig.leftHandReference, vrRig.hardwareFamily); }
                if (rightGlove != null) { rightGlove.SetTrackingHardware(vrRig.rightHandReference, vrRig.hardwareFamily); }

                bool swappedBefore = PlayerPrefs.GetInt(swappedKey, 0) == 1;
                if (swappedBefore)
                {
                    SwapHands(); //also calls UpdateVisuals.
                }
                else
                {
                    UpdateVisuals();
                }
            }
        }


		/// <summary> Based on our state(s) [controllers found, devices found, calibration states], update the hand visuals? </summary>
		public void UpdateVisuals()
        {
            //for every non-connected glove, just show the controller model (if any exists).
            //for every connected glove; show the hand (model) and hide the controller;
            bool rightConnected = RightHandConnected;
            bool leftConnected = LeftHandConnected;

            if (this.vrRig != null && !vrInit) //we have been assigned a VR rig. Hurray!
            {
                if (handsSwapped)
                {
                    vrRig.ShowLeftController = !rightConnected;
                    vrRig.ShowRightController = !leftConnected;
                }
                else
                {
                    vrRig.ShowLeftController = !leftConnected;
                    vrRig.ShowRightController = !rightConnected;
                }
            }

            LeftHandEnabled = leftConnected;
            RightHandEnabled = rightConnected;
        }

        /// <summary> Utility method to grab a left- or right hand when you only have a boolean value. </summary>
        /// <param name="rightHand"></param>
        /// <returns></returns>
        public SG_TrackedHand GetHand(bool rightHand)
        {
            return rightHand ? this.rightHand : leftHand;
        }


        public SG_HapticGlove CheckHapticGlove(SG_TrackedHand hand)
        {
            if (hand != null)
            {
                if (hand.handTrackingSource != null) //try HandPoseProvider
                {
                    IHandPoseProvider provider = hand.handTrackingSource.GetComponent<IHandPoseProvider>();
                    if (provider is SG_HapticGlove)
                    {
                        return (SG_HapticGlove)provider;
                    }
                }
                else if (hand.hapticsSource != null) //then try the HapticGloveSource
                {
                    IHandFeedbackDevice feedbackDevice = hand.hapticsSource.GetComponent<IHandFeedbackDevice>();
                    if (feedbackDevice is SG_HapticGlove)
                    {
                        return (SG_HapticGlove)feedbackDevice;
                    }
                }
            }
            return null;
        }


        //--------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        void OnEnable()
        {
            if (this.headsetDetection != null)
            {
                this.headsetDetection.vrSetDetected.AddListener(AssignVRSet);
            }
        }


		void OnDisable()
        {
            if (this.headsetDetection != null)
            {
                this.headsetDetection.vrSetDetected.RemoveListener(AssignVRSet);
            }
        }



		// Use this for initialization
		void Awake()
		{
            //CEnsure the hands are setup
            if (leftHand != null) { leftHand.Setup(); }
            this.leftGlove = CheckHapticGlove(this.leftHand);
            if (leftGlove != null) { leftGlove.connectsTo = HandSide.LeftHand; }

            if (rightHand != null) { rightHand.Setup(); }
            this.rightGlove = CheckHapticGlove(this.rightHand);
            if (rightGlove != null) { rightGlove.connectsTo = HandSide.RightHand; }

            if (this.vrRig != null || this.headsetDetection != null)
            {   //leave these alone unless we're using is for checking headsets
                RightHandEnabled = false;
                LeftHandEnabled = false;
            }
		}

		void Start()
        {
			// Scan for VRRigs in case you load this User into an existing scene.
			if (this.vrRig == null && headsetDetection == null)
			{   // nothing's been assigned, so let's try
				this.headsetDetection = GameObject.FindObjectOfType<SG_VR_Setup>();
				if (headsetDetection == null) //still null
                {
					this.vrRig = GameObject.FindObjectOfType<SG_VR_Rig>();
                }
            }
            //ignore collisions between perticular colliders, as determined by TrackedHand.
            if (this.leftHand != null && this.rightHand != null)
            {
                this.leftHand.SetIgnoreCollision(this.rightHand, true);
            }
        }

		// Update is called once per frame
		void Update()
		{
			if (vrInit && vrRig != null) //if at any point we assing one
            {
				AssignVRSet(vrRig);
            }

            if (timer_hwChecks <= hwCheckTime)
            {
                timer_hwChecks += Time.deltaTime;
                if (timer_hwChecks >= hwCheckTime)
                {
                    timer_hwChecks = 0;
                    UpdateVisuals();
                }
            }

			if (Input.GetKeyDown(swapHandsKey)) { this.SwapHands(); }
		}
	}

}