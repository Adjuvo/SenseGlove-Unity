using UnityEngine;

namespace SG.XR
{
    /// <summary> VR Room setup to recenter our user. Can be linked directly to a SG_XR_Rig, or to a SG_XR_Setup script whcih chooses the right VR Rig for you. </summary>
    public class SG_XR_RoomSetup : MonoBehaviour
	{
        //--------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Optional component to have the user assign the correct variables of a VR headset. Automatically populated if the headsetDetection finds one. </summary>
        public SG_XR_Rig vrRig;

        /// <summary> Optional component that automatically detects vr headsets. </summary>
        public SG_XR_Setup headsetDetection;


        [Header("RecenterOptions")]
        public Transform roomCenter;

        /// <summary> If true, the camerarig is rotated such that the user is facing the forward (Z axis) of the targetLocation. </summary>
        public bool matchRotation = true;

        /// <summary> If true, places the camerarig so that the camera height is that of the targetLocation. Otherwise, it keeps the CameraRig at the same location. </summary>
        public bool matchTargetHeight = false;

        /// <summary> If true, the last rig location is loaded during Start() </summary>
        public bool keepBetweenSessions = false;

        /// <summary> If set to true, the rotation is stored exclusively for this scene (a.k.a. we add the scene name to the Key). </summary>
        public bool exclusiveToScene = false;

        /// <summary> Optional Hotkey for debugging / easy access </summary>
        public KeyCode recenterHotKey = KeyCode.None;

        /// <summary> Whether or not we should still be checking for VR assets.  </summary>
        protected bool vrInit = true;

        /// <summary> Base keys to access the Rig Position in PlayerPrefs. </summary>
        public const string bRigXpos = "rigXp", bRigYpos = "rigYp", bRigZpos = "rigZp";
        /// <summary> Base keys to access the Rig Rotation in PlayerPrefs. </summary>
        public const string bRigXrot = "rigZr", bRigYrot = "rigYr",  bRigZrot = "rigZr",  bRigWrot = "rigWr";


        //--------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Places the current headset at the roomCenter (based on this script's parameters) </summary>
        public void Recenter()
        {
            Debug.Log("Recentering user to " + (this.roomCenter != null ? roomCenter.name : "N\\A"));
            Recenter(this.roomCenter);
        }

        /// <summary> Places the current headset at the target (based on this script's parameters) </summary>
        public void Recenter(Transform target)
        {
            if (this.vrRig != null && target != null)
            {
                CalculateRigLocation(vrRig.rigRoot.transform, vrRig.headTransfrom, target);
            }
        }



        /// <summary> Caliculate the position of a VR Rig such that its child at headLocation matches the headTarget (based on this script's parameters). </summary>
        /// <param name="vrRig"></param>
        /// <param name="headLocation"></param>
        /// <param name="headTarget"></param>
        public void CalculateRigLocation(Transform vrRig, Transform headLocation, Transform headTarget)
        {
            if (matchRotation)
            {
                Quaternion oldRotation = vrRig.rotation;

                float yTarget = SG.Util.SG_Util.NormalizeAngle(headTarget.rotation.eulerAngles.y);
                float yRig = SG.Util.SG_Util.NormalizeAngle(vrRig.rotation.eulerAngles.y);
                float yCam = SG.Util.SG_Util.NormalizeAngle(headLocation.rotation.eulerAngles.y);

                float newYrot = yTarget + yRig - yCam; //Actually it's yRig - (yRig - yTarget) + (yRig - yCam), but simplified.
                Quaternion newRotation = Quaternion.Euler(0, newYrot, 0);
                vrRig.rotation = newRotation;
            }

            //always match position. But it depends on the Y
            Vector3 dPos = headLocation.position - vrRig.position;
            Vector3 newpos = new Vector3
            (
                headTarget.position.x - dPos.x,
                matchTargetHeight ? headTarget.position.y - dPos.y : vrRig.position.y,
                headTarget.position.z - dPos.z
            );
            vrRig.position = newpos;

            //finally, srtie variables
            StoreRigVariables(vrRig);
        }


        /// <summary> Store the current value of the VR Rig into PlayerPrefs. </summary>
        /// <param name="vrRig"></param>

        private void StoreRigVariables(Transform vrRig)
        {
            string sceneName = exclusiveToScene ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : "";

            PlayerPrefs.SetFloat(sceneName + bRigXpos, vrRig.position.x);
            PlayerPrefs.SetFloat(sceneName + bRigYpos, vrRig.position.y);
            PlayerPrefs.SetFloat(sceneName + bRigZpos, vrRig.position.z);

            PlayerPrefs.SetFloat(sceneName + bRigXrot, vrRig.rotation.x);
            PlayerPrefs.SetFloat(sceneName + bRigYrot, vrRig.rotation.y);
            PlayerPrefs.SetFloat(sceneName + bRigZrot, vrRig.rotation.z);
            PlayerPrefs.SetFloat(sceneName + bRigWrot, vrRig.rotation.w);

        }

        /// <summary> Apply the last stored location to the VR Rig. </summary>
        /// <param name="vrRig"></param>
        public void ApplyLastLocation(Transform vrRig)
        {
            string sceneName = exclusiveToScene ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : "";
            if (PlayerPrefs.HasKey(sceneName + bRigWrot)) //this is the last variable to be assigned. So if it exists, all is well.
            {
                Vector3 rigPos = Vector3.zero;
                rigPos.x = PlayerPrefs.GetFloat(sceneName + bRigXpos, 0);
                if (matchTargetHeight) { rigPos.y = PlayerPrefs.GetFloat(sceneName + bRigYpos, 0); }
                rigPos.z = PlayerPrefs.GetFloat(sceneName + bRigZpos, 0);

                Quaternion rigRot = Quaternion.identity;
                rigRot.x = PlayerPrefs.GetFloat(sceneName + bRigXrot, 0);
                rigRot.y = PlayerPrefs.GetFloat(sceneName + bRigYrot, 0);
                rigRot.z = PlayerPrefs.GetFloat(sceneName + bRigZrot, 0);
                rigRot.w = PlayerPrefs.GetFloat(sceneName + bRigWrot, 0);

                if (matchRotation)
                {
                    vrRig.rotation = rigRot;
                }
                vrRig.position = rigPos;
            }
        }

        /// <summary> Notify this RoomSetup that we've found a VR Rig. Called if we're linked to a SG_XR_Setup that has detected a headset. </summary>
        /// <param name="headset"></param>
        public void VRHeadsetFound(SG_XR_Rig headset)
        {
            if (vrInit && headset != null)
            {
                this.vrRig = headset;
                vrInit = false;
                if (keepBetweenSessions)
                {
                    ApplyLastLocation(vrRig.rigRoot.transform);
                    //StoreRigVariables(vrRig.rigRoot.transform); //store the new variables, in case matchRotation ect have changed since last time?
                }
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        void Awake()
        {
            if (this.roomCenter == null)
            {
                this.roomCenter = this.transform;
            }

            // Scan for VRRigs in case you load this Room Setup into an existing scene.
            if (this.vrRig == null && headsetDetection == null)
            {   // nothing's been assigned, so let's try
                this.headsetDetection = GameObject.FindObjectOfType<SG_XR_Setup>();
                if (headsetDetection == null) //still null
                {
                    this.vrRig = GameObject.FindObjectOfType<SG_XR_Rig>();
                }
            }
        }

        void OnEnable()
		{
			if (this.headsetDetection != null)
			{
				this.headsetDetection.vrSetDetected.AddListener(VRHeadsetFound);
			}
		}

		void OnDisable()
		{
			if (this.headsetDetection != null)
			{
				this.headsetDetection.vrSetDetected.AddListener(VRHeadsetFound);
			}
		}

		void Update()
        {
            if (vrInit && vrRig != null)
            {
                VRHeadsetFound(vrRig);
            }
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
            if (Input.GetKeyDown(recenterHotKey))
            {
                this.Recenter();
            }
#endif
        }

    }
}