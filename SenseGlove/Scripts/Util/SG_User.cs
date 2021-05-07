﻿using SG.VR;
using UnityEngine;

namespace SG
{
	/// <summary> Utility Script that serves as the bridge between VR RIgs and up to two hands. If no VRRigs are assigned, it can be used to  </summary>
	public class SG_User : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The Left TrackedHand. Hidden until it (re)connects) </summary>
		public SG_TrackedHand leftHand;

		/// <summary> The Right TrackedHand. Hidden until it (re)connects) </summary>
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


		//--------------------------------------------------------------------------------------------------------------------------
		// Properties

		/// <summary> Accesses the left TrackedHand activation state </summary>
		public bool LeftHandEnabled
		{
			get { return leftHand != null && leftHand.HandEnabled; }
			set { if (leftHand != null) { leftHand.HandEnabled = value; } }
		}

		/// <summary> Accesses the right TrackedHand activation state </summary>
		public bool RightHandEnabled
		{
			get { return rightHand != null && rightHand.HandEnabled; }
			set { if (rightHand != null) { rightHand.HandEnabled = value; } }
		}
		

		//--------------------------------------------------------------------------------------------------------------------------
		// Properties


		/// <summary> Swaps the trackedObjects of two TrackedHands. </summary>
		public void SwapHands()
        {
            this.handsSwapped = !handsSwapped;
            PlayerPrefs.SetInt(swappedKey, handsSwapped ? 1 : 0); //let the system know we swapped before.

            if (leftHand != null && rightHand != null)
            {
				leftHand.SwapTracking(rightHand); //swaps around the tracked objects
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

				if (leftHand != null) { leftHand.SetTrackingHardware(vrRig.leftHandReference, vrRig.hardwareFamily); }
				if (rightHand != null) { rightHand.SetTrackingHardware(vrRig.rightHandReference, vrRig.hardwareFamily); }

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
			bool rightConnected = rightHand != null && rightHand.gloveHardware != null && rightHand.gloveHardware.IsConnected;
			bool leftConnected = leftHand != null && leftHand.gloveHardware != null && leftHand.gloveHardware.IsConnected;

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


		//--------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		void OnEnable()
        {
			if (this.headsetDetection != null)
			{
				this.headsetDetection.vrSetDetected.AddListener(AssignVRSet);
			}
			if (this.rightHand != null && this.rightHand.gloveHardware != null)
            {
				this.rightHand.gloveHardware.DeviceConnected.AddListener(UpdateVisuals);
				this.rightHand.gloveHardware.DeviceDisconnected.AddListener(UpdateVisuals);
            }
			if (this.leftHand != null && this.leftHand.gloveHardware != null)
			{
				this.leftHand.gloveHardware.DeviceConnected.AddListener(UpdateVisuals);
				this.leftHand.gloveHardware.DeviceDisconnected.AddListener(UpdateVisuals);
			}
		}


		void OnDisable()
        {
			if (this.headsetDetection != null)
			{
				this.headsetDetection.vrSetDetected.RemoveListener(AssignVRSet);
			}
			if (this.rightHand != null && this.rightHand.gloveHardware != null)
			{
				this.rightHand.gloveHardware.DeviceConnected.RemoveListener(UpdateVisuals);
				this.rightHand.gloveHardware.DeviceDisconnected.RemoveListener(UpdateVisuals);
			}
			if (this.leftHand != null && this.leftHand.gloveHardware != null)
			{
				this.leftHand.gloveHardware.DeviceConnected.RemoveListener(UpdateVisuals);
				this.leftHand.gloveHardware.DeviceDisconnected.RemoveListener(UpdateVisuals);
			}
		}



		// Use this for initialization
		void Awake()
		{
			//Ensure the user 
			if (leftHand != null && leftHand.gloveHardware != null) { leftHand.gloveHardware.connectsTo = HandSide.LeftHand; }
			if (rightHand != null && rightHand.gloveHardware != null) { rightHand.gloveHardware.connectsTo = HandSide.RightHand; }

			if (this.vrRig != null || this.headsetDetection != null)
			{	//leave these alone unless we're using is for checking headsets
				RightHandEnabled = false;
				LeftHandEnabled = false;
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (vrInit && vrRig != null) //if at any point we assing one
            {
				AssignVRSet(vrRig);
            }
			if (Input.GetKeyDown(swapHandsKey)) { this.SwapHands(); }
		}
	}

}