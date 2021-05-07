using UnityEngine;

namespace SG.VR
{
	//--------------------------------------------------------------------------------------------------------------------------
	// Unity VR Event

	public class SG_VREvent : UnityEngine.Events.UnityEvent<SG_VR_Rig> { }


	/// <summary> An optional component that selects which CameraRig to use, based on what UnityXR detects. </summary>
	public class SG_VR_Setup : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Fires when the VR setup is identified. </summary>
		public SG_VREvent vrSetDetected = new SG_VREvent();

		/// <summary> The possible VR Setups to check for </summary>
		public SG_VR_Rig[] vrSetups = new SG_VR_Rig[0];

		/// <summary> If UnityXR fails to find a headset name, this will be the fallback value. Use this if you'd like to default to a specific SG_VR_Rig when none can be found.  </summary>
		public string fallbackHeadsetName = "";

		/// <summary> Determines if a Headset has been succesfully found by this script. </summary>
		public bool HeadsetDetected
		{
			get; private set;
		}

		/// <summary> Time in between checking for VR Hardware </summary>
		protected float checkTime = 1.0f;
		/// <summary> timer to keep track of when to check again. </summary>
		protected float hwTimer = 0;


		/// <summary> Optional debugTxt which will output the XRDeviceName. Useful if you're not sure which device you're dealing with in a build. </summary>
		public TextMesh debugTxt;


		//--------------------------------------------------------------------------------------------------------------------------
		// Functions

        /// <summary> Returnts the current headset name, according to Unity.XR </summary>
        /// <returns></returns>
        public static string GetCurrentHeadsetName()
        {
            return UnityEngine.XR.XRDevice.model;
        }

        /// <summary> Checks which SG_VR_Rig to activate, based on UnityEngine.XR.XRDevice.model </summary>
        public void CheckForHeadset()
        {
            string headsetName = GetCurrentHeadsetName();

            if (headsetName.Length == 0)
            {
				headsetName = fallbackHeadsetName;
			}
			CheckForHeadset(headsetName);
		}

		/// <summary> Checks which SG_VR_Rig to activate, based on a custom variable. </summary>
		/// <param name="headsetName"></param>
		public void CheckForHeadset(string headsetName)
        {
			if (headsetName.Length > 0)
			{
				headsetName = headsetName.ToLower();
				//At this point, two things can happen: we either have a relevant rig, or we do not.
				HeadsetDetected = true;
				if (debugTxt != null) { debugTxt.text = "Detected \"" + headsetName + "\""; }

				//Step 1 - Do we have an exact match?
				for (int i = 0; i < this.vrSetups.Length; i++)
				{
					if (this.vrSetups[i].xrDeviceName.ToLower().Equals(headsetName))
					{
						this.AssignHeadset(i, headsetName);
						return;
					}
				}

				//Step 2 - Do we at least match a family
				for (int i = 0; i < this.vrSetups.Length; i++)
				{
					if (this.vrSetups[i].xrDeviceFamily.Length > 0 && headsetName.Contains(this.vrSetups[i].xrDeviceFamily.ToLower()))
					{
						this.AssignHeadset(i, headsetName);
						return;
					}
				}

				//Step 3 - we don't have that Headset supported by SG devices... What now?
				Debug.LogError("Detected " + headsetName + ", which is not in our list.");
			}
			else
            {
				Debug.LogError("Could not find a headsetName, and no fallback is specified.");
            }
		}


		/// <summary> Assign the SG_VR_Rig at a specific Index. Returns true if succesful.. </summary>
		/// <param name="index"></param>
		public bool AssignHeadset(int index, string hsName = "")
        {
			if (index  > -1 && index < this.vrSetups.Length)
            {
				Debug.Log("Linking SenseGlove VR Scripts to " + hsName);
				HeadsetDetected = true;
				vrSetups[index].RigEnabled = true;
				this.vrSetDetected.Invoke(vrSetups[index]);
				return true;
            }
			return false;
        }


		//--------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour


		void Awake()
        {
			if (this.vrSetDetected == null) { vrSetDetected = new SG_VREvent(); }
			for (int i=0; i<this.vrSetups.Length; i++)
            {
				vrSetups[i].RigEnabled = false;
            }
        }

		// Use this for initialization
		void Start()
		{
			CheckForHeadset();
		}

		// Update is called once per frame
		void Update()
		{
			if (!HeadsetDetected)
			{
				hwTimer += Time.deltaTime;
				if (hwTimer > checkTime)
				{
					CheckForHeadset();
					hwTimer = 0;
				}
			}
		}
	}

}