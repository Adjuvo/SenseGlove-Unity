/*
 * A hand model that visualizes the kinematic model of the Sense Glove in a wireframe.
 * Its purpose is to debug the Sense Glove Hardware and Software models, and therefore has Grabable or Feedback layers
 *  
 * @Author: Max Lammers of Sense Glove
 */

using UnityEngine;

namespace SG.Util
{

	public class SG_WireFrame : MonoBehaviour
	{
        //---------------------------------------------------------------------------------------------
        // Properties

        #region Properties

        //  Public Properties

        [Tooltip("The Haptic Glove from which to grab data.")]
        [SerializeField] protected SG_HapticGlove glove;

        [Header("Animation Components")]
        [Tooltip("The GameObject representing the forearm. Most likely this gameObject.")]
        public Transform foreArmTransform;

        [Tooltip("The Transfrom containing both glove and hand components.")]
        public Transform wristTransform;

        [Tooltip("Whether to use the IMU to animate the wrist of this wireframe")]
        public bool useIMU = false;


        /// <summary> The GameObject that will contain the glove sections. </summary>
        [Header("Wireframe Components")]
        [Tooltip("The GameObject that will contain the glove sections.")]
        public GameObject gloveBase;

        /// <summary> A GameObject with four children: Three cylinders representing dX dY dZ, and a sphere representing the point itself. </summary>
        [Tooltip("A GameObject with four children: Three cylinders representing dX dY dZ, and a sphere representing the point itself.")]
        public GameObject gloveSectionModel;

        /// <summary> The GameObject that will contain the finger sections (phalange models). </summary>
        [Tooltip("The GameObject that will contain the finger sections (phalange models).")]
        public GameObject handBase;

        /// <summary> A GameObject with two children, One Cylinder representing the Phalange Lengths, and a sphere representing the joint. </summary>
        [Tooltip("A GameObject with two children, One Cylinder representing the Phalange Lengths, and a sphere representing the joint.")]
        public GameObject phalangeModel;

    

        /// <summary> Key Code to manually toggle hand model rendering. </summary>
        [Header("Keybinds")]
        [Tooltip("Key Code to manually toggle hand model rendering.")]
        public KeyCode toggleHandKey = KeyCode.None;

        /// <summary> Key Code to manually toggle glove model rendering. </summary>
        [Tooltip("Key Code to manually toggle glove model rendering.")]
        public KeyCode toggleGloveKey = KeyCode.None;

        /// <summary> Key Code to manually calibrate the glove.. </summary>
        [Tooltip("Key Code to manually toggle glove model rendering.")]
        public KeyCode resetWristKey = KeyCode.None;


        // Internal Properties.

        /// <summary> Quaternion that aligns the lower arm with the wrist at the moment of calibration. </summary>
        protected Quaternion wristCalibration = Quaternion.identity;

        /// <summary> The relative angles between wrist and lower arm transforms. </summary>
        protected Quaternion wristAngles = Quaternion.identity;

        ///// <summary> Do not run the setups more than once. </summary>
        //private bool setupComplete = false;

        /// <summary> Glove joints to which the gloveAngles are applied. </summary>
        private Transform[][] gloveJoints = null;

        /// <summary> Finger joints to which the gloveAngles are applied. </summary>
        private Transform[][] fingerJoints = null;

        /// <summary> UNFINSHED - Optional Carpal bones for the hand. </summary>
        private Transform[] carpalBones = null;


        ///// <summary> The HandModel that was used to generate the wireframe. </summary>
        //protected SGCore.Kinematics.BasicHandModel iHandModel = null;

        /// <summary> Rotation offset between wrist and forearm </summary>
        protected Quaternion iWristCorrection = Quaternion.identity;
        /// <summary> Rotation offsets between wrist and finger joints. </summary>
        protected Quaternion[][] iFingerCorrections = null;


        #endregion Properties



        //------------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Enable / Disable the Glove wireframe </summary>
        public bool GloveVisible
        {
            get { return this.gloveBase != null && this.gloveBase.activeSelf; }
            set { if (this.gloveBase != null) { this.gloveBase.SetActive(value); } }
        }

        /// <summary> Enable / Disable the hand wireframe </summary>
        public bool HandVisible
        {
            get { return this.handBase != null && this.handBase.activeSelf; }
            set { if (this.handBase != null) { this.handBase.SetActive(value); } }
        }

        ///// <summary> Returns true if the hand we're linked to is Right Handed.s </summary>
        //public bool IsRight
        //{
        //    get
        //    {
        //        if (this.glove != null && this.glove.IsConnected())
        //        {
        //            return this.glove.TracksRightHand();
        //        }
        //        return true;
        //        //return this.glove.connectsTo != HandSide.LeftHand;
        //    }
        //}

        /// <summary> Retrieve the Quaterion rotation between this model's foreArm and Wrist. </summary>
        public Quaternion RelativeWrist
        {
            get { return this.wristAngles; }
        }

        /// <summary> Retrive the euler angles between this model's foreArm and Wrist.  </summary>
        public Vector3 WristAngles
        {
            get { return SG.Util.SG_Util.NormalizeAngles(this.wristAngles.eulerAngles); }
        }

        /// <summary> Get the Quaternion angle between forearm and wrist. </summary>
        public Quaternion WristRotation
        {
            get { return Quaternion.identity; }
        }

        /// <summary> Determined the geometry of the hand wireframe </summary>
        public SGCore.Kinematics.BasicHandModel HandGeometry
        {
            get
            {
                return this.glove.GetKinematics();
            }
            set
            {
                //Debug.Log("Set HandGeometry");
                this.glove.SetKinematics(value);
                this.ResizeFingers();
            }
        }

        public void SetTrackedGlove(SG_HapticGlove newGlove)
        {
            if (newGlove != null)
            {
                this.glove = newGlove;
                this.ResizeFingers(newGlove.GetKinematics().FingerLengths);
                this.CalibrateWrist();

                SG_HandPose idleHand = SG_HandPose.Idle(glove.TracksRightHand());
                this.UpdateHand(idleHand);
                if (this.glove.InternalGlove is SGCore.SG.SenseGlove)
                {
                    //  Debug.Log("Generated glove model for " + this.glove.GetInternalObject().GetDeviceID());
                    SGCore.SG.SG_GloveInfo gloveModel = ((SGCore.SG.SenseGlove)this.glove.InternalGlove).GetGloveModel();
                    GenerateGlove(gloveModel);
                    SGCore.SG.SG_GlovePose idlePose = SGCore.SG.SG_GlovePose.IdlePose(gloveModel);
                    this.UpdateGlove(idlePose);
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // HandModel Calibration

        /// <summary> Collect joint offsets for animation. </summary>
        /// <summary> Collect the absolute angles of the fingers in their 'calibration' pose, correct these with the current wrist orientation. </summary>
        public virtual void CollectCorrections()
        {
            if (this.foreArmTransform == null) { foreArmTransform = this.transform; }
            if (this.fingerJoints != null)
            {
                Transform[][] joints = this.fingerJoints;
                this.iFingerCorrections = new Quaternion[joints.Length][];
                for (int f = 0; f < joints.Length; f++)
                {
                    this.iFingerCorrections[f] = new Quaternion[joints[f].Length];
                    for (int j = 0; j < joints[f].Length; j++)
                    {
                        this.iFingerCorrections[f][j] = Quaternion.Inverse(this.wristTransform.rotation) * joints[f][j].rotation;
                    }
                }
                this.iWristCorrection = Quaternion.Inverse(this.foreArmTransform.rotation) * this.wristTransform.rotation;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Wrist Functions

        /// <summary>  Calibrate the wrist model of this handModel, based on a known IMURotation </summary>
        /// <param name="imuRotation"></param>
        public virtual void CalibrateWrist(Quaternion imuRotation)
        {
            if (this.foreArmTransform != null)
            {
                // Debug.Log(this.name + ": Calibrated Wrist");
                this.wristCalibration = this.foreArmTransform.rotation * Quaternion.Inverse(imuRotation);
            }
        }

        /// <summary> Calibrate the wrist model of this handModel by retrieving the IMU rotation of the linkedGlove. </summary>
        public virtual void CalibrateWrist()
        {
            if (this.glove != null)
            {
                Quaternion IMU;
                if (this.glove.GetIMURotation(out IMU))
                {
                    this.CalibrateWrist(IMU);
                }
            }
        }


        /// <summary> Update the wrist of this wireframe, based on an IMU rotation, retrieved from the linkedGlove. </summary>
        /// <param name="imuRotation"></param>
        public void UpdateWrist(Quaternion imuRotation)
        {
            if (useIMU)
            {
                this.wristTransform.rotation = this.iWristCorrection * (this.wristCalibration * imuRotation);
                this.wristAngles = Quaternion.Inverse(this.foreArmTransform.rotation) * this.wristTransform.rotation;
            }
            else
            {
                this.wristTransform.rotation = this.iWristCorrection * this.foreArmTransform.rotation;
                this.wristAngles = Quaternion.identity; //ignore wrist angle(s).
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Hand Model WireFrame Functions

        /// <summary> Generate a base wireframe for the hand: 5 Fingers with 4 joints each. </summary>
        private void GenerateFingers()
        {
            if (fingerJoints == null)
            {
                float baseLength = 0; //all phalanges are 20mm, just for the sake of being able to see them(?)
                if (phalangeModel != null && handBase != null)
                {
                    this.fingerJoints = new Transform[5][];
                    for (int f = 0; f < fingerJoints.Length; f++)
                    {
                        fingerJoints[f] = new Transform[4];
                        for (int i = 0; i < fingerJoints[f].Length; i++)
                        {
                            GameObject handPosition = GameObject.Instantiate(phalangeModel, this.handBase.transform);
                            handPosition.name = "HandPostion" + f + "" + i;
                            handPosition.transform.localRotation = new Quaternion();
                            fingerJoints[f][i] = handPosition.transform;
                            if (i < fingerJoints[f].Length - 1)
                            {
                                if (handPosition.transform.childCount > 0)
                                {
                                    Transform dX = handPosition.transform.GetChild(0);
                                    //Setup correct sizes.
                                    dX.localScale = new Vector3(dX.localScale.x, baseLength / 2.0f, dX.localScale.z);
                                    dX.localPosition = new Vector3(baseLength / 2.0f, 0, 0);
                                }
                            }
                            else
                            {
                                Transform dX = handPosition.transform.GetChild(0);
                                GameObject.Destroy(dX.gameObject); //remove dX
                            }
                            handPosition.SetActive(true);
                        }
                    }
                }
                else
                {
                    SG_Debugger.Log("WARNING : No base model for Hand Wireframe");
                }
            }
        }

        /// <summary> Resize the fingers based  on this glove's internal handModel </summary>
        public void ResizeFingers()
        { 
            if (this.glove != null)
            {
                this.ResizeFingers(this.HandGeometry.FingerLengths);
            }
        }

        /// <summary> Resize the white cylingers that represent the hand bones. newLengths should be in mm. </summary>
        /// <param name="newLengths"></param>
        public void ResizeFingers(float[][] newLengths)
        {
            //Debug.Log(this.name + " Resizing Fingers " + Util.SG_Util.ToString( newLengths[0] ));
            if (this.fingerJoints == null)
            {
                this.GenerateFingers();
            }
            if (newLengths != null && this.fingerJoints != null)
            {
                for (int f = 0; f < fingerJoints.Length && f < newLengths.Length; f++)
                {
                    for (int j = 0; j < fingerJoints[f].Length - 1 && j < newLengths[f].Length; j++)
                    {
                        if (fingerJoints[f][j].childCount > 0)
                        {
                            Transform dX = fingerJoints[f][j].GetChild(0);
                            dX.localScale = new Vector3(dX.localScale.x, newLengths[f][j] / 2000.0f, dX.localScale.z); // /2000 becuse its /2 and / 1000
                            dX.localPosition = new Vector3(newLengths[f][j] / 2000.0f, 0, 0);
                        }
                    }
                }
            }
        }

        /// <summary> UNFINISHED: Update the position and rotation of the Metacarpal bones. </summary>
        protected void UpdateCarpals()
        {
            if (carpalBones != null)
            {
                for (int f = 0; f < carpalBones.Length && f < fingerJoints.Length; f++)
                {
                    Vector3 mcp = fingerJoints[f][0].localPosition;
                    Vector3 crp = carpalBones[f].localPosition;

                    Vector3 diff = mcp - crp;
                    float L = diff.magnitude;
                    //Debug.Log( SG_Util.ToString(mcp) + " - " + SG_Util.ToString(crp) + " -> " + L);

                    Transform dX = carpalBones[f].transform.GetChild(0);
                    dX.localScale = new Vector3(dX.localScale.x, L / 2.0f, dX.localScale.z);
                    dX.localPosition = new Vector3(L / 2.0f, 0, 0);

                    carpalBones[f].LookAt(fingerJoints[f][0], handBase.transform.up);
                    carpalBones[f].rotation = Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)) * carpalBones[f].rotation;
                }
            }
        }

        /// <summary> Update the hand wireframe based off a HandPose taken from the LinkedGlove. </summary>
        public void UpdateHand()
        {
            SG_HandPose handPose;
            if (this.glove.GetHandPose(out handPose, true))
            {
                UpdateHand(handPose);
            }
        }
        
        /// <summary> Update the hand wireframe based off a HandPose. </summary>
        /// <param name="pose"></param>
        public void UpdateHand(SG_HandPose pose)
        {
            if (pose != null)
            {
                //Debug.Log("WireFrame; " + this.HandGeometry.ToString());
                if (this.iFingerCorrections == null)
                {
                    this.CollectCorrections();
                }
                Quaternion[][] angles = pose.jointRotations;
                Vector3[][] positions = pose.jointPositions;
                if (fingerJoints != null && this.iFingerCorrections != null)
                {
                    for (int f = 0; f < fingerJoints.Length; f++)
                    {
                        if (pose.jointRotations.Length > f)
                        {
                            for (int j = 0; j < fingerJoints[f].Length; j++)
                            {
                                if (pose.jointRotations[f].Length > j)
                                {
                                    fingerJoints[f][j].rotation = this.wristTransform.rotation
                                        * (angles[f][j] * this.iFingerCorrections[f][j]);

                                    fingerJoints[f][j].localPosition = positions[f][j];
                                }
                                //if (j > 0)
                                //{
                                //    Debug.DrawLine(fingerJoints[f][j - 1].position, fingerJoints[f][j].position, Color.red);
                                //}
                            }
                        }
                    }
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Glove Model WireFrame Functions

        /// <summary> Generate a wireframe for a glove, based on its GloveModel </summary>
        /// <param name="gloveModel"></param>
        public void GenerateGlove(SGCore.SG.SG_GloveInfo gloveModel)
        {
            if (gloveModel != null)
            {
                gloveBase.SetActive(true);
                gloveSectionModel.SetActive(true);

                Vector3[][] gloveLengths = SG.Util.SG_Conversions.ToUnityPositions(gloveModel.GloveLengths, true);
                int x = 0, y = 1, z = 2;
                this.gloveJoints = new Transform[gloveModel.GloveLengths.Length][];
                for (int f = 0; f < gloveJoints.Length; f++)
                {
                    gloveJoints[f] = new Transform[gloveModel.GloveLengths[f].Length + 1];

                    for (int i = 0; i < gloveJoints[f].Length; i++)
                    {
                        GameObject gloveJoint = GameObject.Instantiate(gloveSectionModel, this.gloveBase.gameObject.transform);
                        gloveJoint.name = "GlovePostion" + f + "" + i;
                        gloveJoint.transform.localRotation = Quaternion.identity;
                        gloveJoints[f][i] = gloveJoint.transform;
                        //gloveJoint.SetActive(true);
                        if (i < gloveJoints[f].Length - 1)
                        {
                            if (gloveJoint.transform.childCount > 2)
                            {
                                Transform dX = gloveJoint.transform.GetChild(2);
                                Transform dY = gloveJoint.transform.GetChild(1);
                                Transform dZ = gloveJoint.transform.GetChild(0);

                                //Setup correct sizes.
                                if (gloveLengths[f][i][x] != 0) { dX.localScale = new Vector3(dX.localScale.x, gloveLengths[f][i][x] / 2.0f, dX.localScale.z); }
                                else { dX.gameObject.SetActive(false); }
                                if (gloveLengths[f][i][y] != 0) { dY.localScale = new Vector3(dX.localScale.x, gloveLengths[f][i][y] / 2.0f, dX.localScale.z); }
                                else { dY.gameObject.SetActive(false); }
                                if (gloveLengths[f][i][z] != 0) { dZ.localScale = new Vector3(dX.localScale.x, gloveLengths[f][i][z] / 2.0f, dX.localScale.z); }
                                else { dZ.gameObject.SetActive(false); }

                                //set correct positions based on ZYX?
                                dY.localPosition = new Vector3(0, gloveLengths[f][i][y] / 2.0f, 0);
                                dX.localPosition = new Vector3(gloveLengths[f][i][x] / 2.0f, gloveLengths[f][i][y] / 1.0f, 0);
                                //dY ?
                            }
                        }
                        else
                        {
                            for (int j = 0; j < gloveJoint.transform.childCount - 1; j++) //the last one is just the dot
                            {
                                GameObject.Destroy(gloveJoint.transform.GetChild(j).gameObject);
                            }
                        }
                    }
                }
                gloveSectionModel.SetActive(false);
            }
            else
            {
                SG_Debugger.Log("WARNING : No base model for Glove Wireframe");
            }
        }

        /// <summary> Update the Glove wireframe based off a GlovePose taken from the linkedGlove </summary>
        public void UpdateGlove()
        {
            if (this.glove != null && this.gloveJoints != null)
            {
                SGCore.SG.SG_GlovePose glovePose;
                if (((SGCore.SG.SenseGlove)this.glove.InternalGlove).GetGlovePose(out glovePose))
                {
                    this.UpdateGlove(glovePose);
                }
            }
        }

        /// <summary> Update the Glove wireframe based off a GlovePose </summary>
        /// <param name="glovePose"></param>
        public void UpdateGlove(SGCore.SG.SG_GlovePose glovePose)
        {
            if (glovePose != null && this.gloveBase.activeInHierarchy)
            {
                Quaternion[][] angles = SG.Util.SG_Conversions.ToUnityQuaternions(glovePose.JointRotations);
                Vector3[][] positions = SG.Util.SG_Conversions.ToUnityPositions(glovePose.JointPositions);
                if (gloveJoints != null)
                {
                    for (int f = 0; f < gloveJoints.Length; f++)
                    {
                        if (angles.Length > f)
                        {
                            for (int j = 0; j < gloveJoints[f].Length; j++)
                            {
                                if (angles[f].Length > j)
                                {
                                    gloveJoints[f][j].rotation = this.wristTransform.rotation
                                        * (angles[f][j]);

                                    gloveJoints[f][j].localPosition = positions[f][j];
                                }
                            }
                        }
                    }
                }
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Monobeviour

        // Use this for initialization
        void Start()
		{
            GenerateFingers();
            //this.SetTrackedGlove(this.glove);
        }

		// Update is called once per frame
		void Update()
		{
            //Keybinds
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
            if (Input.GetKeyDown(this.toggleGloveKey))
            {
                this.GloveVisible = !GloveVisible;
            }
            if (Input.GetKeyDown(this.toggleHandKey))
            {
                this.HandVisible = !HandVisible;
            }
#endif

            //Automated Updates
            if (this.glove != null && glove.IsConnected())
            {
                if (this.wristTransform != null && this.foreArmTransform != null)
                {
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
                    if (Input.GetKeyDown(this.resetWristKey)) { this.CalibrateWrist(); }
#endif
                    Quaternion imu;
                    if (this.glove.GetIMURotation(out imu)) { this.UpdateWrist(imu); }
                }
                if (this.gloveBase.activeInHierarchy)
                {
                    this.UpdateGlove();
                }
                if (this.handBase.activeInHierarchy)
                {
                    this.UpdateHand();
                }
            }
		}
	}

}