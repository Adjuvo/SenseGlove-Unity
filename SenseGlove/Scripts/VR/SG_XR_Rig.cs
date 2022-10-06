using UnityEngine;

namespace SG.XR
{
	/// <summary> A Script to indicate the components of a VR Prefab, such as left and right hands.  </summary>
	public class SG_XR_Rig : MonoBehaviour
	{
		//--------------------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> The full name of this Headset as indicated through Unity XR </summary>
		[Header("Headset Properties")]
		public string xrDeviceName = "Quest 2";
		/// <summary> The Headset family, used as a secondary check in case we don't match a full name. </summary>
		public string xrDeviceFamily = "Oculus";

		/// <summary> The main VR rig object, used to recenter. Disabled on Start(). </summary>
		public GameObject rigRoot;

		/// <summary> The head's transform, used to place the user's head at a specific location.  </summary>
		public Transform headTransfrom;

		/// <summary> Controller reference for the left hand. </summary>
		[Header("Controller Tracking")]
		public Transform leftHandReference;
		/// <summary> Controller reference for the right hand.  </summary>
		public Transform rightHandReference;

		/// <summary> Controller family that this VR Rig uses for positional Tracking. </summary>
		public SGCore.PosTrackingHardware hardwareFamily = SGCore.PosTrackingHardware.Custom;

		/// <summary> Controller models that will be turned off once the respective HapticGlove connects </summary>
		public GameObject leftControllerModel, rightControllerModel;

		/// <summary> So that Initialize() is called only once. </summary>
		protected bool init = true;



		//--------------------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> Enable / Disable this VR VR rig. </summary>
		public bool RigEnabled
        {
			get { if (rigRoot == null) { this.rigRoot = this.gameObject; } return rigRoot.activeSelf; }
			set { if (rigRoot == null) { this.rigRoot = this.gameObject; } rigRoot.SetActive(value); }
		}

		/// <summary> Show / Hide the Left Controller Model </summary>
		public bool ShowLeftController
        {
			get { return leftControllerModel != null && leftControllerModel.activeSelf; }
			set { if (leftControllerModel != null) { leftControllerModel.SetActive(value); } }
		}

		/// <summary> Show / Hide the Right Controller Model </summary>
		public bool ShowRightController
		{
			get { return rightControllerModel != null && rightControllerModel.activeSelf; }
			set { if (rightControllerModel != null) { rightControllerModel.SetActive(value); } }
		}


		//--------------------------------------------------------------------------------------------------------------------------
		// Functions 

		/// <summary> Initalize this VR Rig. </summary>
		public void Initialize()
        {
			if (init)
            {
				init = false;
				ShowLeftController = true;
				ShowRightController = true;
			}
        }


		/// <summary> Check if this VR Set has controller events </summary>
		public bool HasControllerEvents
		{
			get { return hardwareFamily != SGCore.PosTrackingHardware.ViveTracker; }
		}


		//--------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		void Awake()
		{
			if (rigRoot == null) { rigRoot = this.gameObject; }
			Initialize();
		}


	}
}
