using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SG
{
    public enum FingerTrackingOverrideMode
    {
        /// <summary> Finger is free to move, unconstrained </summary>
        None,
        /// <summary> Finger flexion is locked between an upper and lower limit </summary>
        Limited,
        /// <summary> Finger Pose is the same as the preview </summary>
        MatchPreview,
    }

    /// <summary> Add this to a SG_Grabable to snap the hand at a fixed location (and pose?), either on Hover or OnGrab. </summary>
    /// <remarks> It is placed in a separate behaviour so I can extend off it's GenerateGrabArgs / GetSnapPoint functionality. (For example, allowing you to assign a whole list of SnapPoints and choosing the closest. </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SG_Grabable))]
    public class SG_SnapOptions : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The Grabable this component is linked to. Since this component can only be added to SG_Grabable objects, we can autmagically Search for it. </summary>
        public SG_Grabable linkedGrabable;


        /// <summary> Global Setting. Setting this to true this reveals the rest of the settings. </summary>
        public bool controlsFingerAnimation = false;

        [SerializeField] protected FingerTrackingOverrideMode thumb_overrideMode = FingerTrackingOverrideMode.MatchPreview;
        [SerializeField] [Range(0.0f, 1.0f)] protected float thumb_minFlexion = 0.0f;
        [SerializeField] [Range(0.0f, 1.0f)] protected float thumb_maxFlexion = 1.0f;

        [SerializeField] protected FingerTrackingOverrideMode index_overrideMode = FingerTrackingOverrideMode.MatchPreview;
        [SerializeField] [Range(0.0f, 1.0f)] protected float index_minFlexion = 0.0f;
        [SerializeField] [Range(0.0f, 1.0f)] protected float index_maxFlexion = 1.0f;

        [SerializeField] protected FingerTrackingOverrideMode middle_overrideMode = FingerTrackingOverrideMode.MatchPreview;
        [SerializeField] [Range(0.0f, 1.0f)] protected float middle_minFlexion = 0.0f;
        [SerializeField] [Range(0.0f, 1.0f)] protected float middle_maxFlexion = 1.0f;

        [SerializeField] protected FingerTrackingOverrideMode ring_overrideMode = FingerTrackingOverrideMode.MatchPreview;
        [SerializeField] [Range(0.0f, 1.0f)] protected float ring_minFlexion = 0.0f;
        [SerializeField] [Range(0.0f, 1.0f)] protected float ring_maxFlexion = 1.0f;

        [SerializeField] protected FingerTrackingOverrideMode pinky_overrideMode = FingerTrackingOverrideMode.MatchPreview;
        [SerializeField] [Range(0.0f, 1.0f)] protected float pinky_minFlexion = 0.0f;
        [SerializeField] [Range(0.0f, 1.0f)] protected float pinky_maxFlexion = 1.0f;

        /// <summary> Used for iterations etc </summary>
        protected FingerTrackingOverrideMode[] overrideModes = new FingerTrackingOverrideMode[5];
        protected float[][] fingerLimits = new float[5][];

        //-----------------------------------------------------
        // All of this has to do with 'previewing' the Snap Options.

        // The Base Hand is stored as SG_TrackedHand instead of SG_HandModel, so that
        // 1) it is clearly the one used by the actual user, and not another 3d HandModel placed in the scene (e.g. by a  different script like this one)
        // 2) So we cannot accidentally 'swap' the baseHand / posedHand around when we pass them as parameters.

        [HideInInspector] public SG.SG_TrackedHand right_baseHand;
        [HideInInspector] public SG.SG_HandModelInfo right_posedHand = null;
        protected SG.SG_HandPose right_overridePose = null;
        [HideInInspector] public string right_serializedPosed = "";
        [HideInInspector] public Vector3 right_relWristPos = Vector3.zero;
        [HideInInspector] public Quaternion right_relWristRot = Quaternion.identity;

        //TODO: Also store a Vector3 and Quaternion for the relative wrist!

        [HideInInspector] public SG.SG_TrackedHand left_baseHand;
        [HideInInspector] public SG.SG_HandModelInfo left_posedHand = null;
        protected SG.SG_HandPose left_overridePose = null;
        [HideInInspector] public string left_serializedPosed = "";
        [HideInInspector] public Vector3 left_relWristPos = Vector3.zero;
        [HideInInspector] public Quaternion left_relWristRot = Quaternion.identity;


        //--------------------------------------------------------------------------------------------------------------------------------------------
        // GrabArgument Generation.

        /// <summary> Attempt to generate snapPoint arguments. Returns false if we failed for whatever reason (e.g. no snap points were assigned). </summary>
        /// <param name="objectTransf"></param>
        /// <param name="grabScript"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool GenerateGrabArgs(Transform objectTransf, SG_GrabScript grabScript, out GrabArguments args)
        {
            args = null;

            SG_HandPose overridePose = grabScript.IsRight ? right_overridePose : left_overridePose;
            if (overridePose != null)
            {
                // Where should the Wrist be in 3D Space
                Vector3 obj_wrist_Pos = grabScript.TrackedHand.TracksRightHand() ? this.right_relWristPos : this.left_relWristPos;
                Quaternion obj_wrist_Rot = grabScript.TrackedHand.TracksRightHand() ? this.right_relWristRot : this.left_relWristRot;
                Vector3 wristPos; Quaternion wristRot;
                SG.Util.SG_Util.CalculateTargetLocation(objectTransf, obj_wrist_Pos, obj_wrist_Rot, out wristPos, out wristRot); //whis is where the Wrist Pos needs to be, but I need the Grab Reference.

                // Which means: The Grab Refernce should be here.
                Vector3 wristToGrab_pos; Quaternion wristToGrab_rot;
                grabScript.GrabRefOffsets(out wristToGrab_pos, out wristToGrab_rot);
                Vector3 grabRefPosition; Quaternion grabRefRotation; //worls
                SG.Util.SG_Util.CalculateTargetLocation(wristPos, wristRot, wristToGrab_pos, wristToGrab_rot, out grabRefPosition, out grabRefRotation);

                //Which means these offsets compared to the regular one:
                Vector3 posOffset; Quaternion rotOffset; //worls
                SG.Util.SG_Util.CalculateOffsets(objectTransf.position, objectTransf.rotation, grabRefPosition, grabRefRotation, out posOffset, out rotOffset);

                args = new GrabArguments(grabScript, posOffset, rotOffset, objectTransf.position, objectTransf.rotation);
            }
            return args != null;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------
        // Tracking Overrides


        /// <summary> Loads any (Serialized) finger tracking poses into this SnapOptions' memort </summary>
        public void UnpackPoses()
        {
            UnpackSinglePose(left_serializedPosed, ref this.left_overridePose);
            UnpackSinglePose(right_serializedPosed, ref this.right_overridePose);
        }

        /// <summary> Try to extract a seralized pose. If it exists, assign it to the correct OverridePose </summary>
        /// <param name="serializedPosed"></param>
        /// <param name="overridePose"></param>
        private void UnpackSinglePose(string serializedPosed, ref SG_HandPose overridePose)
        {
            if (serializedPosed.Length > 0 && SG_HandPose.Deserialize(serializedPosed, out SG_HandPose pose))
            {
                overridePose = pose;
            }
            else
            {
                overridePose = null; //clear these for goot measure.
            }
        }


        /// <summary> Returns a new handpose with the finger tracking overriden by the override pose, if any exists. </summary>
        /// <param name="realHandPose"></param>
        /// <param name="connectedScript"></param>
        /// <returns></returns>
        public virtual SG_HandPose GetOverridePose(SG_HandPose realHandPose, SG_GrabScript connectedScript)
        {
            if (this.controlsFingerAnimation)
            {
                SG_HandPose overrideFingers = realHandPose.rightHanded ? right_overridePose : left_overridePose;
                if (overrideFingers != null)
                {
                    //now we generate the actual finger motion(s).
                    float[] nF = new float[5];
                    Quaternion[][] rots = new Quaternion[5][];
                    Vector3[][] pos = new Vector3[5][];
                    Vector3[][] hA = new Vector3[5][];
                    FingerTrackingOverrideMode[] overrides = new FingerTrackingOverrideMode[5] { thumb_overrideMode, index_overrideMode, middle_overrideMode, ring_overrideMode, pinky_overrideMode };
                    for (int f = 0; f < 5; f++)
                    {
                        if (overrideModes[f] == FingerTrackingOverrideMode.MatchPreview)
                        {
                            nF[f] = overrideFingers.normalizedFlexion[f];
                            pos[f] = overrideFingers.jointPositions[f];
                            rots[f] = overrideFingers.jointRotations[f];
                            hA[f] = overrideFingers.jointAngles[f];
                        }
                        else if (overrideModes[f] == FingerTrackingOverrideMode.Limited && (realHandPose.normalizedFlexion[f] < fingerLimits[f][0] || realHandPose.normalizedFlexion[f] > fingerLimits[f][1]))
                        {   //fingerlimits [0] is min, [1] is max. We only need to 'do something about this' when our normalized flexion gets outside of bounds. otherwise, just use the real finger tracking.
                            nF[f] = Mathf.Clamp(realHandPose.normalizedFlexion[f], fingerLimits[f][0], fingerLimits[f][1]);
                            float abdForUpdate = realHandPose.jointAngles[f][0].y;
                            SG_HandProjection.CalculateFingerTracking(f, nF[f], abdForUpdate, connectedScript.TrackedHand.handModel, out hA[f], out pos[f], out rots[f]);
                        }
                        else
                        {
                            nF[f] = realHandPose.normalizedFlexion[f];
                            pos[f] = realHandPose.jointPositions[f];
                            rots[f] = realHandPose.jointRotations[f];
                            hA[f] = realHandPose.jointAngles[f];
                        }
                    }
                    //combine the wrist tracking of the Real Hand Pose with these new rotations
                    return SG_HandPose.Combine(realHandPose, new SG_HandPose(hA, rots, pos, realHandPose.rightHanded, realHandPose.wristPosition, realHandPose.wristRotation, nF), true);
                }
            }
            return realHandPose;
        }


        //--------------------------------------------------------------------------------------------------------------------
        // Preview-Related Functions


        // Editor-only stuff.

#if UNITY_EDITOR

        //-----------------------------------------------------------------------------------------------------------
        // Putting 3D Models on the correct spot(s).

        /// <summary> Grabs the base hand model(s) from teh SG_TrackedHand and/or SG_USer. Without these, I cannot generate a proper hand model. </summary>
        public void CollectBaseHands()
        {
            //Just a ltitle validation strategy
            if (left_baseHand != null && left_baseHand.TracksRightHand())
            {
                Debug.LogError("Your left Base Hand was assigned a right hand. Removing the reference...", this);
                left_baseHand = null;
            }
            if (right_baseHand != null && !right_baseHand.TracksRightHand())
            {
                Debug.LogError("Your right Base Hand was assigned a left hand. Removing the reference...", this);
                right_baseHand = null;
            }
            // Not grab the correct one(s)!
            if (left_baseHand == null)
            {
                left_baseHand = SG.Util.SG_Util.GetHandMatchingSide(false);
            }
            if (right_baseHand == null)
            {
                right_baseHand = SG.Util.SG_Util.GetHandMatchingSide(true);
            }
            //A message in case we cannot find them.
            if (left_baseHand == null && right_baseHand == null)
            {
                Debug.Log(this.name + ": Could not find base Hand Models. Make sure you have an SG_User and/or SG_TrackedHand in your scene, and that its GameObject is enabled!", this);
            }
        }

        /// <summary> Spawns a copt of the baseHand's HandModelInfo and returns you a reference to said script. </summary>
        /// <param name="baseHand"></param>
        /// <returns></returns>
        public SG_HandModelInfo SpawnSingleHand(SG_TrackedHand baseHand)
        {
            if (baseHand.handModel == null)
            {
                Debug.LogError("Base Hand does not have a HandModelInfo assigned!");
                return null;
            }
            GameObject copy = GameObject.Instantiate(baseHand.handModel.gameObject, Vector3.zero, Quaternion.identity); //TODO: After spawning, place me at the "most likely" pose / location based on SnapOptions (if any)

            if (this.linkedGrabable == null)
            {
                this.linkedGrabable = this.GetComponent<SG_Grabable>();
            }

            if (this.linkedGrabable != null && this.linkedGrabable.MyTransform != null)
            {
                Vector3 scale = this.linkedGrabable.MyTransform.localScale;
                if (SGCore.Kinematics.Values.FloatEquals(scale.x, scale.y) && SGCore.Kinematics.Values.FloatEquals(scale.y, scale.z)) //x == y && y == z
                {
                    copy.transform.parent = this.linkedGrabable.MyTransform;
                }
                else
                {
                    Debug.Log(this.name + " does not have a Uniform scale! Spawning in Global Space instead.");
                }
            }

            copy.name = this.gameObject.name.ToUpper() + (baseHand.TracksRightHand() ? "-POSER-RIGHT" : "-POSER-LEFT");
            SG_HandModelInfo res = copy.GetComponent<SG_HandModelInfo>();
            return res;
        }

        /// <summary> If required, spawns a new 3D Model for posing and assigns it to the correct valriable. </summary>
        /// <param name="posedHand"></param>
        /// <param name="baseHand"></param>
        private void SpawnHandForPosing(ref SG_HandModelInfo posedHand, SG_TrackedHand baseHand)
        {
            if (posedHand == null && baseHand != null)
            {
                posedHand = SpawnSingleHand(baseHand);
                Debug.Log("Spawned Hand Poser for " + (baseHand.TracksRightHand() ? "Right" : "Left"));
            }
        }

        /// <summary> If a stored overridePose exists, we place our hand there. If not, spawn us in an idle handPOse at this object's origin. </summary>
        /// <param name="handmodel_forPosing"></param>
        /// <param name="stored_overridePose"></param>
        private void SetModelToHandPose(SG_HandModelInfo handmodel_forPosing, SG_HandPose stored_overridePose, Vector3 wristPosOffset, Quaternion wristRotOffset)
        {
            if (handmodel_forPosing != null)
            {
                SG_HandPose newPose = stored_overridePose != null ? stored_overridePose : SG_HandPose.Idle(handmodel_forPosing.IsRightHand);

                if (this.linkedGrabable == null)
                {
                    this.linkedGrabable = this.GetComponent<SG_Grabable>();
                }

                if (this.linkedGrabable != null)
                {
                    Vector3 posePos; Quaternion poseRot;
                    SG.Util.SG_Util.CalculateTargetLocation(this.linkedGrabable.MyTransform, wristPosOffset, wristRotOffset, out posePos, out poseRot);
                    newPose.wristPosition = posePos;
                    newPose.wristRotation = poseRot;
                }
                else
                {
                    newPose.wristPosition = this.transform.position;
                    newPose.wristRotation = this.transform.rotation;
                }
                SG_HandAnimator.SetHandToPose(handmodel_forPosing, newPose);
            }
        }

        /// <summary> Editor Button: Spawn both hands to make us ready for posing. </summary>
        public void SpawnPoserHands()
        {
            CollectBaseHands(); //make sure we have base hands to spawn from

            SpawnHandForPosing(ref left_posedHand, left_baseHand);
            SpawnHandForPosing(ref right_posedHand, right_baseHand);

            UnpackPoses(); //loads any serialized poses we have

            //When we do spawn new hands, we put them in the 'correct' pose.
            SetModelToHandPose(left_posedHand, left_overridePose, left_relWristPos, left_relWristRot);
            SetModelToHandPose(right_posedHand, right_overridePose, right_relWristPos, right_relWristRot);
        }


        //-----------------------------------------------------------------------------------------------------------
        // Storing + Cleanupo

        /// <summary> Remove both 3D Models used to pose a preview hand. </summary>
        public void DeletePoserHands()
        {
            DeletePoserHand(ref left_posedHand, this.left_baseHand);
            DeletePoserHand(ref right_posedHand, this.right_baseHand);
        }

        /// <summary> Delete a single 3D Model for posing, if it exists. When you do, remove our reference to it. </summary>
        /// <param name="posedHand"></param>
        /// <param name="baseHand"></param>
        private void DeletePoserHand(ref SG_HandModelInfo posedHand, SG_TrackedHand baseHand)
        {
            if (posedHand != null && (baseHand == null || posedHand != baseHand.handModel))
            {
                GameObject.DestroyImmediate(posedHand.gameObject);
                posedHand = null;
            }
        }

        /// <summary> Store both Handposes and clean up the 3d Model for posing while you're at it. </summary>
        public void StorePoses()
        {
            StoreSinglePose(left_posedHand, left_baseHand, ref left_overridePose, ref left_serializedPosed, ref left_relWristPos, ref left_relWristRot);
            StoreSinglePose(right_posedHand, right_baseHand, ref right_overridePose, ref right_serializedPosed, ref right_relWristPos, ref right_relWristRot);

            DeletePoserHands();
        }

        /// <summary> Stores a single (serialized) pose into memory so we can pull it out again during a build. </summary>
        /// <param name="posedHand"></param>
        /// <param name="baseHand"></param>
        /// <param name="overridePose"></param>
        /// <param name="overrideSerialized"></param>
        private void StoreSinglePose(SG_HandModelInfo posedHand, SG_TrackedHand baseHand, ref SG_HandPose overridePose, ref string overrideSerialized, ref Vector3 relWristPos, ref Quaternion relWristRot)
        {
            if (posedHand != null && baseHand != null)
            {
                overridePose = SG.Util.SG_Util.ExtractHandPose(posedHand, baseHand.handModel);
                overrideSerialized = overridePose.Serialize();

                //Now, calculate the offsets, if any exist.
                if (this.linkedGrabable == null)
                {
                    this.linkedGrabable = this.GetComponent<SG_Grabable>();
                }
                if (linkedGrabable != null)
                {
                    Vector3 wP; Quaternion wR;
                    SG.Util.SG_Util.CalculateOffsets(posedHand.transform, this.linkedGrabable.MyTransform, out wP, out wR);
                    relWristPos = wP;
                    relWristRot = wR;
                }
                Debug.Log("Collected Pose Info for the " + (posedHand.IsRightHand ? "Right" : "Left") + " Hand. We can now safely delete the 3D Model.");
            }
        }

        public void ResetLeftPose()
        {
            this.left_serializedPosed = "";
            this.left_overridePose = null;
            this.left_relWristPos = Vector3.zero;
            this.left_relWristRot = Quaternion.identity;
            SetModelToHandPose(left_posedHand, left_overridePose, left_relWristPos, left_relWristRot);
        }

        public void ResetRightPose()
        {
            this.right_serializedPosed = "";
            this.right_overridePose = null;
            SetModelToHandPose(right_posedHand, right_overridePose, right_relWristPos, right_relWristRot);
        }

        public void MirrorLeftOntoRight()
        {
            UnpackPoses();
            if (left_overridePose == null)
            {
                Debug.LogError("No Left Pose Available!");
                return;
            }
            if (right_overridePose == null)
            {
                right_overridePose = SG_HandPose.Idle(true);
            }

            SG_HandPose copiedPose = left_overridePose.Mirror();
            right_overridePose = SG_HandPose.Combine(right_overridePose, copiedPose, true);
            right_serializedPosed = right_overridePose.Serialize();
            SetModelToHandPose(right_posedHand, right_overridePose, right_relWristPos, right_relWristRot);

            Debug.Log("Mirrored Left Hand onto the Right Hand");
        }

        public void MirrorRightOntoLeft()
        {
            UnpackPoses();
            if (right_overridePose == null)
            {
                Debug.LogError("No Right Pose Available!");
                return;
            }
            if (left_overridePose == null)
            {
                left_overridePose = SG_HandPose.Idle(false);
            }

            SG_HandPose copiedPose = right_overridePose.Mirror();
            left_overridePose = SG_HandPose.Combine(left_overridePose, copiedPose, true);
            left_serializedPosed = left_overridePose.Serialize();
            SetModelToHandPose(left_posedHand, left_overridePose, left_relWristPos, left_relWristRot);

            Debug.Log("Mirrored Right Hand onto the Left Hand");
        }


#endif //#if UNITY_EDITOR

        //--------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected virtual void Reset()
        {
            this.linkedGrabable = this.GetComponent<SG_Grabable>();
        }


        protected virtual void Awake()
        {
            this.linkedGrabable = this.GetComponent<SG_Grabable>();
            this.overrideModes = new FingerTrackingOverrideMode[5] { thumb_overrideMode, index_overrideMode, middle_overrideMode, ring_overrideMode, pinky_overrideMode };
            this.fingerLimits = new float[5][]
            {
                new float[2] { thumb_minFlexion,  thumb_maxFlexion },
                new float[2] { index_minFlexion,  index_maxFlexion },
                new float[2] { middle_minFlexion, middle_maxFlexion },
                new float[2] { ring_minFlexion,   ring_maxFlexion },
                new float[2] { pinky_minFlexion,  pinky_maxFlexion },
            };
            UnpackPoses(); //or, alternatively, do this only when needed?
        }

        protected virtual void Start()
        {
            if (left_posedHand != null || right_posedHand != null)
            {
                if (left_posedHand != null) { GameObject.Destroy(left_posedHand.gameObject); }
                if (right_posedHand != null) { GameObject.Destroy(right_posedHand.gameObject); }
                Debug.LogError("You've left your hand models for posing active in the Scene. Your changes may not have been stored! Please clean it up in future builds by clicking 'Store Pose(s)' button!", this);
            }
        }

#if UNITY_EDITOR

        protected virtual void OnValidate()
        {
            this.overrideModes = new FingerTrackingOverrideMode[5] { thumb_overrideMode, index_overrideMode, middle_overrideMode, ring_overrideMode, pinky_overrideMode };
            this.fingerLimits = new float[5][]
            {
                new float[2] { thumb_minFlexion,  thumb_maxFlexion },
                new float[2] { index_minFlexion,  index_maxFlexion },
                new float[2] { middle_minFlexion, middle_maxFlexion },
                new float[2] { ring_minFlexion,   ring_maxFlexion },
                new float[2] { pinky_minFlexion,  pinky_maxFlexion },
            };
        }

#endif

    }





    //--------------------------------------------------------------------------------------------------------------------
    // Custom Inspector for the buttons etc.
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SG_SnapOptions))]
    public class SG_SnapOptionsEditor : UnityEditor.Editor
    {
        private UnityEditor.SerializedProperty m_linkedGrabable;
        private GUIContent l_linkedGrabable;

        private UnityEditor.SerializedProperty m_overrideFingerTracking;
        private GUIContent l_overrideFingerTracking;

        private UnityEditor.SerializedProperty[] m_fingerEnums = new SerializedProperty[0];
        private GUIContent[] l_fingerEnums = new GUIContent[0];

        private UnityEditor.SerializedProperty[] m_fingerMin = new SerializedProperty[0];
        private GUIContent[] l_fingerMin = new GUIContent[0];

        private UnityEditor.SerializedProperty[] m_fingerMax = new SerializedProperty[0];
        private GUIContent[] l_fingerMax = new GUIContent[0];


        void OnEnable()
        {
            m_linkedGrabable = serializedObject.FindProperty("linkedGrabable");
            l_linkedGrabable = new GUIContent("Linked Grabable", "");

            m_overrideFingerTracking = serializedObject.FindProperty("controlsFingerAnimation");
            l_overrideFingerTracking = new GUIContent("Controls Finger Animation", "");

            m_fingerEnums = new SerializedProperty[5]
            {
                serializedObject.FindProperty("thumb_overrideMode"),
                serializedObject.FindProperty("index_overrideMode"),
                serializedObject.FindProperty("middle_overrideMode"),
                serializedObject.FindProperty("ring_overrideMode"),
                serializedObject.FindProperty("pinky_overrideMode")
            };
            l_fingerEnums = new GUIContent[5]
            {
                new GUIContent("Thumb Override Mode", ""),
                new GUIContent("Index Override Mode", ""),
                new GUIContent("Middle Override Mode", ""),
                new GUIContent("Ring Override Mode", ""),
                new GUIContent("Pinky Override Mode", "")
            };

            m_fingerMin = new SerializedProperty[5]
            {
                serializedObject.FindProperty("thumb_minFlexion"),
                serializedObject.FindProperty("index_minFlexion"),
                serializedObject.FindProperty("middle_minFlexion"),
                serializedObject.FindProperty("ring_minFlexion"),
                serializedObject.FindProperty("pinky_minFlexion")
            };
            l_fingerMin = new GUIContent[5]
            {
                new GUIContent("Thumb Min Flexion", ""),
                new GUIContent("Index Min Flexion", ""),
                new GUIContent("Middle Min Flexion", ""),
                new GUIContent("Ring Min Flexion", ""),
                new GUIContent("Pinky Min Flexion", "")
            };

            m_fingerMax = new SerializedProperty[5]
            {
                serializedObject.FindProperty("thumb_maxFlexion"),
                serializedObject.FindProperty("index_maxFlexion"),
                serializedObject.FindProperty("middle_maxFlexion"),
                serializedObject.FindProperty("ring_maxFlexion"),
                serializedObject.FindProperty("pinky_maxFlexion")
            };
            l_fingerMax = new GUIContent[5]
            {
                new GUIContent("Thumb Max Flexion", ""),
                new GUIContent("Index Max Flexion", ""),
                new GUIContent("Middle Max Flexion", ""),
                new GUIContent("Ring Max Flexion", ""),
                new GUIContent("Pinky Max Flexion", "")
            };
        }


        public void DrawBaseItems(SG_SnapOptions myScript)
        {
            //DrawDefaultInspector();

            UnityEditor.EditorGUILayout.PropertyField(m_linkedGrabable, l_linkedGrabable);

            UnityEditor.EditorGUILayout.Space();
            GUILayout.Label("Finger Tracking Overrides", UnityEditor.EditorStyles.boldLabel);

            UnityEditor.EditorGUILayout.PropertyField(m_overrideFingerTracking, l_overrideFingerTracking);
            if (myScript.controlsFingerAnimation)
            {
                //Draw the rest of the finger(s).
                for (int f = 0; f < m_fingerEnums.Length; f++)
                {
                    FingerTrackingOverrideMode ovrMethod = (FingerTrackingOverrideMode)m_fingerEnums[f].intValue;

                    UnityEditor.EditorGUILayout.PropertyField(m_fingerEnums[f], l_fingerEnums[f]);
                    if (ovrMethod == FingerTrackingOverrideMode.Limited)
                    {
                        UnityEditor.EditorGUILayout.PropertyField(m_fingerMin[f], l_fingerMin[f]);
                        UnityEditor.EditorGUILayout.PropertyField(m_fingerMax[f], l_fingerMax[f]);
                    }
                }
            }

        }


        public override void OnInspectorGUI()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            SG_SnapOptions myScript = (SG_SnapOptions)target;

            DrawBaseItems(myScript);

            //Next up, all the buttons
            UnityEditor.EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                GUILayout.Label("Pose Preview (Not available during play)", UnityEditor.EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("Pose Preview", UnityEditor.EditorStyles.boldLabel);
                if (myScript.left_posedHand == null || myScript.left_posedHand == null)
                {
                    if (GUILayout.Button("Spawn Hand(s)"))
                    {
                        myScript.SpawnPoserHands();
                    }
                }

                if (myScript.left_posedHand != null || myScript.left_posedHand != null)
                {
                    if (GUILayout.Button("Store Pose(s)"))
                    {
                        myScript.StorePoses();
                    }
                }


                if (myScript.left_serializedPosed.Length > 0 || myScript.right_serializedPosed.Length > 0)
                {
                    GUILayout.Label("Controls - Be careful with these", UnityEditor.EditorStyles.boldLabel);
                }

                if (myScript.left_serializedPosed.Length > 0)
                {
                    if (GUILayout.Button("Reset Left Pose"))
                    {
                        myScript.ResetLeftPose();
                    }

                }
                if (myScript.right_serializedPosed.Length > 0)
                {
                    if (GUILayout.Button("Mirror Fingers: Right -> Left"))
                    {
                        myScript.MirrorRightOntoLeft();
                    }
                }

                if (myScript.right_serializedPosed.Length > 0)
                {
                    if (GUILayout.Button("Reset Right Fingers"))
                    {
                        myScript.ResetRightPose();
                    }
                }
                if (myScript.left_serializedPosed.Length > 0)
                {
                    if (GUILayout.Button("Mirror Fingers: Left -> Right"))
                    {
                        myScript.MirrorLeftOntoRight();
                    }
                }
            }


            serializedObject.ApplyModifiedProperties(); //call this at the end to apply the changed properties.
        }
    }
#endif

}