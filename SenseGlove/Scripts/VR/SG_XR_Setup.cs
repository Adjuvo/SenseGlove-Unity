using UnityEngine;

namespace SG.XR
{
	//--------------------------------------------------------------------------------------------------------------------------
	// Unity VR Event

	public class SG_XREvent : UnityEngine.Events.UnityEvent<SG_XR_Rig> { }


	/// <summary> An optional component that selects which CameraRig to use, based on what UnityXR detects. </summary>
	public class SG_XR_Setup : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Fires when the VR setup is identified. </summary>
		public SG_XREvent vrSetDetected = new SG_XREvent();

		/// <summary> The possible VR Setups to check for </summary>
		public SG_XR_Rig[] vrSetups = new SG_XR_Rig[0];

		/// <summary> If UnityXR fails to find a headset name, this will be the fallback value. Use this if you'd like to default to a specific SG_XR_Rig when none can be found.  </summary>
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
			string headset = "";
#if UNITY_2021 || UNITY_2020 || UNITY_2019
			//Needs both XR Plug-in Management and OpenVR for the appropriate Headset name detection.
            UnityEngine.XR.InputDevice hmd = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head); //this takes a few seconds to initialize!
            if (hmd != null && hmd.name != null) { headset = hmd.name; }
            else
            {
                Debug.LogError("SG_XR_Setup could not find a headset name. Please ensure that XR Plug-in Management and the appropriate SDK are installed.");
            }
            if (headset.Contains("OpenXR"))
            {
                Debug.LogError("Looks like you're using OpenXR, which does not allow us to detect which headset is used (yet). It's therefore not possible to use this script. :(");
            }
#elif UNITY_2017 || UNITY_2018       //Unity 2017 & Unity 2018

			headset = UnityEngine.XR.XRDevice.model;
			if (headset.Length == 0)
			{
				Debug.LogError("SG_XR_Setup could not find a headset name. Please ensure that 'XR enabled' is set 'true' in Player Settings.");
			}
#endif
			//Debug.Log("Name = \"" + headset + "\"");
			return headset.ToLower();
		}

        /// <summary> Checks which SG_XR_Rig to activate, based on UnityEngine.XR.XRDevice.model </summary>
        public void CheckForHeadset()
        {
            string headsetName = GetCurrentHeadsetName();
            if (headsetName.Length == 0)
            {
				headsetName = fallbackHeadsetName;
			}
			CheckForHeadset(headsetName);
		}

		/// <summary> Checks which SG_XR_Rig to activate, based on a custom variable. </summary>
		/// <param name="headsetName"></param>
		public void CheckForHeadset(string headsetName)
        {
			if (!HeadsetDetected)
			{
				//At this point, two things can happen: we either have a relevant rig, or we do not.
				if (debugTxt != null) { debugTxt.text = "Detected \"" + headsetName + "\""; }

				//Stap 0 - If we have but one Rig, we'll always use that one to avoid confusion.
				if (this.vrSetups.Length == 1)
				{
					this.AssignHeadset(0);
					return;
				}
				if (headsetName.Length > 0)
				{
					headsetName = headsetName.ToLower();

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
					Debug.LogError("Detected " + headsetName + ", which is not in our list. Defaulting back to the first one");
					AssignHeadset(0);
				}
				else
				{
					Debug.LogError("Could not find a headsetName, and no fallback is specified.");
				}
				HeadsetDetected = true; //we're done.
			}
		}


		/// <summary> Assign the SG_XR_Rig at a specific Index. Returns true if succesful.. </summary>
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
			if (this.vrSetDetected == null) { vrSetDetected = new SG_XREvent(); }
			for (int i=0; i<this.vrSetups.Length; i++)
            {
				vrSetups[i].RigEnabled = false;
            }
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