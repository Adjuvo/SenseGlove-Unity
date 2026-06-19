using SG.Util;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace SG
{

    /// <summary> Script that, when Spawned in, propmts the user to select an offset from a list, then to confirm it. It's not responsible (yet) for when it is spawned?
    /// In this case, it's only possible to set both hands! </summary>
    public class SG_OffsetSelector : MonoBehaviour
    {

        private enum SelectionState
        {
            Unknown,
            SelectOffset,
            Confirm
        }


        //------------------------------------------------------------------------------------------------------------------------------
        // Checking / Spawning the menu

        private static bool s_checkedResources = false;
        private static GameObject s_prefabInResources = null;

        private static SG_OffsetSelector s_instanceInScene = null;


        /// <summary> Checks if my Prefab exists inside a Resources folder. </summary>
        private static void CheckForPrefab()
        {
            if (s_checkedResources)
                return;

            s_checkedResources = true; //no need to try again
            try
            {
                s_prefabInResources = Resources.Load<GameObject>("[SG_OffsetSelection]");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }




        /// <summary> Entry point for various objects </summary>
        /// <param name="onlyIfRequired">if true, then this selector will skip selection when one is already present. If false, you will be prompted regardless</param>
        public static void SpawnSelector(bool onlyIfRequired)
        {
            if (s_instanceInScene != null)
            {
                Debug.Log("There is already an instance of SG_OffsetSelector in this scene. So no need to spawn a new one.");
                return;
            }
            if (onlyIfRequired)
            {
                if (!UnityEngine.XR.XRSettings.enabled) //it's not an XR Project (yet) so skip.
                    return;

                SG_ConfigSettings.TryLoadConfig(); //if a hard-coded offset exists in these config files, then we use it.
                
                //IF it can be loaded from the CONFIG file, we skip it
                TrackingHardware chosenHardware = SG_Core.Settings.GlobalWristTrackingOffsets;
                if (chosenHardware != TrackingHardware.PromptUser && chosenHardware != TrackingHardware.Unknown)
                    return; //the offsets are already chosen / fixed.
            }

            //If we get here, we -should- spawn an instance
            CheckForPrefab(); //does it exist in our Resources?
            if (s_prefabInResources == null)
            {
                Debug.LogError("[SG_OffsetSelection] could not be loaded from Resources");
                return;
            }

            Debug.Log("[SG_OffsetSelection] exists in resources");
            GameObject.Instantiate(s_prefabInResources);
            //should register itself as s_instanceInScene on Awake
        }



        //------------------------------------------------------------------------------------------------------------------------------
        // Instance Components

        /// <summary> Based on the chosen settings (UnityXR vs FollowGameObject). The Tracking Origin </summary>
        [SerializeField] private Transform leftTrackingOrigin, rightTrackingOrigin;

        /// <summary> Where the wrists will eventually show up at (with finger tracking?)
        [SerializeField] private SG_TrackedHand leftHandExample, rightHandExample; //where the hands end up


        [SerializeField] private SG_OffsetOption[] offsetOptions = new SG_OffsetOption[0]; //selection timer.


        private SG_User sgUser = null; //If one exists, we'll temporatily disable them for now.
        private bool userState = true;

        /// <summary> Currently selected option </summary>
        private SG_OffsetOption lastSelectedOption = null;
        private float timer_selection = 0.0f;
        private const float rayDistance = 3.0f;
        public float selectionTime = 2.0f;

        public float smoothSpeed = 10f;
        private Vector3 smoothedDirection = Vector3.forward;

        private SelectionState currState = SelectionState.Unknown;

        [SerializeField] private Transform timingTransform;
        [SerializeField] private UnityEngine.UI.Image timingImage;

        public List<GameObject> selectionObjects = new List<GameObject>();
        public List<GameObject> confirmationObjects = new List<GameObject>();

        public TextMesh confirmInstr;
        private const float minTrackerDistance = 0.25f; //minimum amount a tracker has to move from the XR rig / 0.0.0 to be considered 'active'


        [SerializeField] private Transform confirmZone, cancelZone;
        public float confirmTime = 2.0f;
        public float selectionZoneProximity = 0.25f;

        public UnityEngine.Events.UnityEvent OnSelectionMade = new UnityEngine.Events.UnityEvent();


        //------------------------------------------------------------------------------------------------------------------------------
        // Instance Behaviour


        public string ConfirmInstruction
        {
            get { return confirmInstr != null ? confirmInstr.text : ""; }
            set { if (confirmInstr) { confirmInstr.text = value; } }
        }

        private void SetupSelf()
        {
            //Set them free so they're not influenced by my own movement
            if (leftTrackingOrigin != null)
                leftTrackingOrigin.parent = null;
            if (leftHandExample != null)
            {
                leftHandExample.overrideWristLocation = true; //we will be in charge so it works even when you don't have your glove connected.
                leftHandExample.transform.parent = null;
                SG_Util.SafelyAdd(leftHandExample.gameObject, confirmationObjects); //make sure these are only shown during conf step
            }
            if (rightTrackingOrigin != null)
                rightTrackingOrigin.parent = null;
            if (rightHandExample != null)
            {
                rightHandExample.overrideWristLocation = true; //we will be in charge so it works even when you don't have your glove connected.
                rightHandExample.transform.parent = null;
                SG_Util.SafelyAdd(rightHandExample.gameObject, confirmationObjects);
            }

            if (sgUser == null)
                sgUser = GameObject.FindFirstObjectByType<SG_User>();
            if (sgUser != null)
            {
                userState = sgUser.gameObject.activeSelf;
                sgUser.gameObject.SetActive(false);
            }

            Transform cam = SG_XR_SceneTrackingLinks.GetHeadTransform();
            if (cam != null)
            {
                smoothedDirection = cam.forward;

                if (timingTransform != null)
                {
                    timingTransform.SetParent(cam.transform);
                    timingTransform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    timingTransform.localPosition = new Vector3(0.0f, 0.0f, 1.5f);
                }
            }

            SG.Util.SG_Util.SafelyAdd(confirmZone.gameObject, confirmationObjects);
            SG.Util.SG_Util.SafelyAdd(cancelZone.gameObject, confirmationObjects);

            GoToState(SelectionState.SelectOffset);
        }

        private void CleanupSelf()
        {
            //return to me so they can get cleaned up. bam!
            if (leftTrackingOrigin != null)
                leftTrackingOrigin.transform.parent = this.transform;
            if (rightTrackingOrigin != null)
                rightTrackingOrigin.transform.parent = this.transform;
            if (leftHandExample != null)
                leftHandExample.transform.parent = this.transform;
            if (rightHandExample != null)
                rightHandExample.transform.parent = this.transform;
            if (timingTransform != null)
                timingTransform.SetParent(transform);

            if (sgUser != null)
                sgUser.gameObject.SetActive(userState);

            GameObject.Destroy(this.gameObject);
        }

        private void SetOptions(bool enabled)
        {
            foreach (SG_OffsetOption option in offsetOptions)
            {
                option.Selected = false; //deselect em all regardless
                option.gameObject.SetActive(enabled);
            }
        }

        private void DeselectAll()
        {
            foreach (SG_OffsetOption option in offsetOptions)
            {
                option.Selected = false; //deselect em all regardless
            }
        }

        private static void SetGameObjects(List<GameObject> objs, bool enabled)
        {
            foreach (GameObject obj in objs)
            {
                if (obj != null)
                    obj.SetActive(enabled);
            }
        }

        private void GoToState(SelectionState state)
        {
            this.currState = state;
            TimingIndicationValue = 0.0f;
            timer_selection = 0.0f; //reset this either way

            SetGameObjects(selectionObjects, false);
            SetGameObjects(confirmationObjects, false);
            DeselectAll();
            switch (state)
            {
                case SelectionState.SelectOffset:

                    SG_Core.Settings.GlobalWristTrackingOffsets = TrackingHardware.AutoDetect;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(SG_Core.Settings);
#endif
                    SG_XR_Devices.ClearDevices(true); //Check again now that you've set the offsets

                    lastSelectedOption = null; //cleared
                    SetOptions(true);
                    SetGameObjects(selectionObjects, true);
                    break;


                case SelectionState.Confirm:

                    SetOptions(false);

                    //TODO: Re-select the current thing...
                    SG_Core.Settings.GlobalWristTrackingOffsets = lastSelectedOption.WristOffsets;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(SG_Core.Settings);
#endif
                    SG_XR_Devices.ClearDevices(true); //Check again now that you've set the offsets

                    UpdateConfirmText();

                    SetGameObjects(confirmationObjects, true);
                    break;
            }
        }


        private void UpdateConfirmText()
        {
            //"You have selected XXX\r\nPlease observe your wrist locations, and confirm your choice using your hands"
            string msg = $"You have selected {lastSelectedOption.WristOffsets.ToString()}";


            //Check if any tracking devices are currently active...
            Transform rig = SG_XR_SceneTrackingLinks.GetXRRigTransform();
            Vector3 xrZero = rig ? rig.position : Vector3.zero;

            bool rightHandActive = SG_HandTracking.GetTrackingDeviceLocation(true, out Vector3 rPos, out Quaternion rRot) && (rPos - xrZero).magnitude > minTrackerDistance;
            bool leftHandActive = SG_HandTracking.GetTrackingDeviceLocation(false, out Vector3 lPos, out Quaternion lRot) && (lPos - xrZero).magnitude > minTrackerDistance;

            bool trackingActive = rightHandActive || leftHandActive;
            if (trackingActive)
            {
                msg += "\r\nUse the preview hands to check if your tracking is working,\r\nthen confirm your choice using the spheres below.";
            }
            else
            {
                msg += "\r\nPlease activate your trackng device(s) to check if they are correctly set up.\r\nIf you don't see them moving, there might be a problem with the SenseGlove Settings.";
            }
            ConfirmInstruction = msg;
        }


        /// <summary> Spinny thingy </summary>
        public float TimingIndicationValue
        {
            get 
            {
                return timingImage != null ? timingImage.fillAmount : 0.0f;
            }
            set
            {
                if (timingImage != null) { timingImage.fillAmount = Mathf.Clamp01(value); }
            }
        }

        //it will come with some example Hands that will be turned on/off
        private void UpdateTrackingDeviceLocations()
        {
            if (leftTrackingOrigin != null && SG_HandTracking.GetTrackingDeviceLocation(false, out Vector3 LtoPos, out Quaternion LtoRot))
            {
                leftTrackingOrigin.rotation = LtoRot;
                leftTrackingOrigin.position = LtoPos;
            }
            if (rightTrackingOrigin != null && SG_HandTracking.GetTrackingDeviceLocation(true, out Vector3 RtoPos, out Quaternion RtoRot))
            {
                rightTrackingOrigin.rotation = RtoRot;
                rightTrackingOrigin.position = RtoPos;
            }
        }

        private void UpdateExampleLocation()
        {
            //we have the TrackingDevice Location. So just use that combined with offsets for (assumed Nova 2 unless a Device is connected
            if (lastSelectedOption == null)
                return;

            SGCore.PosTrackingHardware hw = SG.Util.SG_Conversions.ToInternalTracking(lastSelectedOption.WristOffsets);

            //TODO: Check if there's a left / right glove?
            bool rightPresent = SG_Core.GetGloveInstance(true, out SGCore.HapticGlove rGlove);
            bool leftPresent = SG_Core.GetGloveInstance(false, out SGCore.HapticGlove lGlove);

            if (leftPresent || rightPresent)
            {
                if (!leftPresent) //Only Right Glove
                {
                    UpdateWristFromInstance(rGlove, rightTrackingOrigin, hw, rightHandExample);
                    UpdateWristFromFallback(false, leftTrackingOrigin, hw, lGlove.GetDeviceType(), leftHandExample);
                }
                else if (!rightPresent) //Only Left Glove
                {
                    UpdateWristFromFallback(true, rightTrackingOrigin, hw, rGlove.GetDeviceType(), rightHandExample);
                    UpdateWristFromInstance(lGlove, leftTrackingOrigin, hw, leftHandExample);
                }
                else
                {
                    UpdateWristFromInstance(rGlove, rightTrackingOrigin, hw, rightHandExample);
                    UpdateWristFromInstance(lGlove, leftTrackingOrigin, hw, leftHandExample);
                }
            }
            else //fallbacks. In this case, it;s always Nova 2
            {
                UpdateWristFromFallback(true, rightTrackingOrigin, hw, SGCore.DeviceType.NOVA_2_GLOVE, rightHandExample);
                UpdateWristFromFallback(false, leftTrackingOrigin, hw, SGCore.DeviceType.NOVA_2_GLOVE, leftHandExample);
            }
        }


        private void UpdateWristFromInstance(SGCore.HapticGlove instance, Transform referenceTransfrom, SGCore.PosTrackingHardware hardware, SG_TrackedHand wristTransform)
        {
            if (wristTransform == null || instance == null || referenceTransfrom == null)
                return;

            SGCore.Kinematics.Vect3D iTrPos = SG.Util.SG_Conversions.ToPosition(referenceTransfrom.position);
            SGCore.Kinematics.Quat iTrRot = SG.Util.SG_Conversions.ToQuaternion(referenceTransfrom.rotation);

            SGCore.Kinematics.Vect3D wrPos; SGCore.Kinematics.Quat wrRot;
            instance.GetWristLocation(iTrPos, iTrRot, hardware, out wrPos, out wrRot);

            wristTransform.transform.rotation = SG.Util.SG_Conversions.ToUnityQuaternion(wrRot);
            wristTransform.transform.position = SG.Util.SG_Conversions.ToUnityPosition(wrPos);
        }

        private void UpdateWristFromFallback(bool right, Transform referenceTransfrom, SGCore.PosTrackingHardware hardware, SGCore.DeviceType fallbackDevice, SG_TrackedHand wristTransform)
        {
            if (wristTransform == null)
                return;

            SGCore.Kinematics.Vect3D iTrPos = SG.Util.SG_Conversions.ToPosition(referenceTransfrom.position);
            SGCore.Kinematics.Quat iTrRot = SG.Util.SG_Conversions.ToQuaternion(referenceTransfrom.rotation);

            SGCore.Kinematics.Vect3D wrPos;
            SGCore.Kinematics.Quat wrRot;

            switch (fallbackDevice)
            {
                case SGCore.DeviceType.NOVA: //Nova 1
                    SGCore.Nova.Nova_GloveInfo temp = new SGCore.Nova.Nova_GloveInfo("", "", 1, 3, right, SGCore.Kinematics.Quat.identity, 5);
                    SGCore.Nova.NovaGlove.CalculateWristLocation(iTrPos, iTrRot, hardware, temp, out wrPos, out wrRot);
                    break;
                default: //Nova 2
                    SGCore.Nova.Nova2Glove.CalculateWristLocation(iTrPos, iTrRot, hardware, right, "v1.0.0", out wrPos, out wrRot);
                    break;
            }
            wristTransform.transform.rotation = SG.Util.SG_Conversions.ToUnityQuaternion(wrRot);
            wristTransform.transform.position = SG.Util.SG_Conversions.ToUnityPosition(wrPos);
        }



        private bool CheckForOption(Collider other, out SG_OffsetOption option)
        {
            for (int i=0; i<this.offsetOptions.Length; i++)
            {
                if (offsetOptions[i].SameCollider(other))
                {
                    option = offsetOptions[i];
                    return true;
                }
            }
            option = null;
            return false;
        }

        private SG_OffsetOption CheckGazedOption(Vector3 camPos, Vector3 camDir)
        {
            Ray ray = new Ray(camPos, camDir);
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);
            foreach (var hit in hits)
            {
                if (CheckForOption(hit.collider, out SG_OffsetOption option))
                {
                    return option; //if we get here, you've hit one of the option(s).
                }
            }
            return null;
        }

        private void SelectOption(SG_OffsetOption option)
        {
            lastSelectedOption = option;
            Debug.Log("goSelect " + option.name);
            GoToState(SelectionState.Confirm);
        }

        private void CheckForOptionHit(float dT)
        {
            Transform cam = SG_XR_SceneTrackingLinks.GetHeadTransform();
            if (cam != null)
            {
                //Step 1 - Smooth
                Vector3 targetDirection = cam.forward;
                smoothedDirection = Vector3.Lerp(
                    smoothedDirection,
                    targetDirection,
                    Time.deltaTime * smoothSpeed
                );

                // Use smoothedDirection for raycasts, cursors, etc.
                Debug.DrawRay(cam.position, smoothedDirection * rayDistance, Color.green);

                //step 2 - Cast
                SG_OffsetOption currentOption = CheckGazedOption(cam.position, smoothedDirection);

                if (currentOption == lastSelectedOption)
                {
                    if (currentOption != null) //otherwise still nothing so meh
                    {
                        timer_selection += dT;
                        if (timer_selection >= selectionTime) //We've made a selection!
                            SelectOption(currentOption);
                    }
                }
                else //there's been a change of any kind!
                {
                    Debug.Log("Currently Selecting " + (currentOption == null ? "NULL" : currentOption.name) + ", Previous was " + (lastSelectedOption == null ? "NULL" : lastSelectedOption.name));
                    if (lastSelectedOption != null) //deselect this one
                        lastSelectedOption.Selected = false;
                    if (currentOption != null) //select this one
                        currentOption.Selected = true;

                    timer_selection = 0.0f; //reset this either way
                    lastSelectedOption = currentOption;
                }
                if (currState == SelectionState.SelectOffset)
                    TimingIndicationValue = timer_selection / selectionTime;
            }

        }


        private void CheckForConfirmation(float dT)
        {
            Vector3 confPos = confirmZone.position;
            Vector3 cancPos = cancelZone.position;
            int rightSelection = NearSelection(rightHandExample, in confPos, in cancPos);
            int leftSelection = NearSelection(leftHandExample, in confPos, in cancPos);

            if (leftSelection == 0 && rightSelection == 0)
            {
                timer_selection = 0.0f; //reset
            }
            else if (leftSelection + rightSelection != 0) //one of them is Not 0. Since it's either -1 or +1, I can use summation to check if they're in two different zones.
            {
                timer_selection += dT;
            }


            TimingIndicationValue = timer_selection / confirmTime;
            if (timer_selection >= confirmTime)
            {
                if (leftSelection + rightSelection > 0) //one or more is wanting to confirm
                    ConfirmChoice();
                else
                    CancelChoice();
            }   
        }

        private void ConfirmChoice()
        {
            SG_ConfigSettings.StoreConfigFile(); //saves the chosen offset(s) to the config.txt
            OnSelectionMade?.Invoke();
            CleanupSelf();
        }

        private void CancelChoice()
        {
            GoToState(SelectionState.SelectOffset);
        }



        
        /// <summary> false when not nearby, -1 for cancel, 1 for success. 0 for not near any </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        private int NearSelection(SG_TrackedHand exampleHand, in Vector3 confPos, in Vector3 cancelPos)
        {
            if (exampleHand == null)
                return 0;
            Vector3 worldPos = exampleHand.transform.position;
            if ((worldPos - confPos).magnitude <= selectionZoneProximity)
            {
                Debug.DrawLine(worldPos, confPos, Color.green);
                Debug.DrawLine(worldPos, cancelPos, Color.red);
                return 1;
            }
            else if ((worldPos - cancelPos).magnitude <= selectionZoneProximity)
            {
                Debug.DrawLine(worldPos, confPos, Color.red);
                Debug.DrawLine(worldPos, cancelPos, Color.green);
                return -1;
            }
            Debug.DrawLine(worldPos, confPos, Color.red);
            Debug.DrawLine(worldPos, cancelPos, Color.red);
            return 0;
        }




        //------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        private void Awake()
        {
            if (s_instanceInScene == null)
            {
                s_instanceInScene = this;
                Debug.Log("Registered instance of SG_OffsetSelector", this);
            }
            else
            {
                Debug.Log("An Instance of SG_OffsetSelector already exists within the scene. This one will be de-activated and destroyed.", this);
            }
        }

        private void OnDestroy()
        {
            if (s_instanceInScene == this)
                s_instanceInScene = null;
        }


        // Start is called before the first frame update
        void Start()
        {
            SetupSelf();
        }

        // Update is called once per frame
        void Update()
        {
            if (currState == SelectionState.SelectOffset)
            {
                CheckForOptionHit(Time.deltaTime);
            }
            else if (currState == SelectionState.Confirm)
            {
                UpdateConfirmText();
                CheckForConfirmation(Time.deltaTime);
            }
        }

        void LateUpdate()
        {
            UpdateTrackingDeviceLocations(); //we're doing this regardless.
            if (currState == SelectionState.Confirm)
                UpdateExampleLocation();
        }
    }
}