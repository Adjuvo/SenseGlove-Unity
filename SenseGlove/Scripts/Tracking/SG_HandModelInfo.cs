using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    ///// <summary> Represents a segment of the hand. Unused for now because we prefer to use the Joints, but may become relevant in the future. </summary>
    //public enum HandPart
    //{
    //    Wrist,

    //    Thumb_MetaCarpal,
    //    Thumb_Proximal_Phalange,
    //    Thumb_Distal_Phalange,
    //    Thumb_FingerTip,

    //    Index_Proximal_Phalange,
    //    Index_Medial_Phalange,
    //    Index_Distal_Phalange,
    //    Index_FingerTip,

    //    Middle_Proximal_Phalange,
    //    Middle_Medial_Phalange,
    //    Middle_Distal_Phalange,
    //    Middle_FingerTip,

    //    Ring_Proximal_Phalange,
    //    Ring_Medial_Phalange,
    //    Ring_Distal_Phalange,
    //    Ring_FingerTip,

    //    Pinky_Proximal_Phalange,
    //    Pinky_Medial_Phalange,
    //    Pinky_Distal_Phalange,
    //    Pinky_FingerTip,

    //    None//util
    //}

    /// <summary> Represents a joint of the hand </summary>
    public enum HandJoint
    {
        Wrist,

        Thumb_CMC,
        Thumb_MCP,
        Thumb_IP,
        Thumb_FingerTip,

        Index_MCP,
        Index_PIP,
        Index_DIP,
        Index_FingerTip,

        Middle_MCP,
        Middle_PIP,
        Middle_DIP,
        Middle_FingerTip,

        Ring_MCP,
        Ring_PIP,
        Ring_DIP,
        Ring_FingerTip,

        Pinky_MCP,
        Pinky_PIP,
        Pinky_DIP,
        Pinky_FingerTip,

        None//util
    }

    /// <summary> A script to assign information of hand joints, used by other scripts that use hand tracking. </summary>
    public class SG_HandModelInfo : MonoBehaviour
    {
        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The side of this hand model. Used to indicate if it is a left- or right hand. </summary>
        public HandSide handSide = HandSide.AnyHand;

        /// <summary> The forearm of the hand model, usually the parent of the wrist transform. </summary>
        public Transform foreArmTransform;
        /// <summary> The transform of the wrist. Should be distinct from the foreArmTransform if wrist animation is not required. </summary>
        public Transform wristTransform;

        /// <summary> The thumb joint transforms, preferably including the fingertip. </summary>
        public Transform[] thumbJoints = new Transform[0];
        /// <summary> The index joint transforms, preferably including the fingertip. </summary>
        public Transform[] indexJoints = new Transform[0];
        /// <summary> The middle joint transforms, preferably including the fingertip. </summary>
        public Transform[] middleJoints = new Transform[0];
        /// <summary> The ring joint transforms, preferably including the fingertip. </summary>
        public Transform[] ringJoints = new Transform[0];
        /// <summary> The pinky joint transforms, preferably including the fingertip. </summary>
        public Transform[] pinkyJoints = new Transform[0];



        /// <summary> Debug objects to show the user where the finger joint transforms are. </summary>
        protected GameObject[][] fingerDebug = null;
        /// <summary> Debug objects to show the user where the wrist transform is </summary>
        protected GameObject wristDebug = null;

        /// <summary> The BasicHandModel with appropriate Geometry for this hand. </summary>
        protected SGCore.Kinematics.BasicHandModel kinematics = null;

        /// <summary> Quaterion offset foreArm -> Wrist </summary>
        protected Quaternion iWristCorrection = Quaternion.identity;
        /// <summary> Quaternion offsets wrist -> finger.  </summary>
        protected Quaternion[][] iFingerCorrections = null;

        
        //----------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Returns the Quaternion offset between the wrist- and forearm transform. Relevent when animating the wrist. </summary>
        public Quaternion WristCorrection
        {
            get
            {
                if (iFingerCorrections == null) //not collected yet
                {
                    this.CollectCorrections();
                }
                return iWristCorrection;
            }
        }

        /// <summary> Returns the quaterion offset between the wrist and each finger. Used to animate the correct joint. </summary>
        public Quaternion[][] FingerCorrections
        {
            get
            {
                if (iFingerCorrections == null) //not collected yet
                {
                    this.CollectCorrections();
                }
                return iFingerCorrections;
            }
        }

        /// <summary> Retrieve a Basic Hand Model to use for HandPOse calculation. </summary>
        public SGCore.Kinematics.BasicHandModel HandKinematics
        {
            get
            {
                if (kinematics == null) { kinematics = this.GenerateHandModel(); }
                return kinematics;
            }
        }


        /// <summary> Retreive all finger joints as an array of Transforms, sorted from thumb to pinky. </summary>
        public Transform[][] FingerJoints
        {
            get
            {
                Transform[][] res = new Transform[5][];
                res[0] = thumbJoints;
                res[1] = indexJoints;
                res[2] = middleJoints;
                res[3] = ringJoints;
                res[4] = pinkyJoints;
                return res;
            }
        }

        /// <summary> Retruns true if this source provides data fro the right hand. </summary>
        public bool IsRightHand
        {
            get { return this.handSide != HandSide.LeftHand; }
        }


        //----------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Collect joint offsets for animation. </summary>
        /// <summary> Collect the absolute angles of the fingers in their 'calibration' pose, correct these with the current wrist orientation. </summary>
        public virtual void CollectCorrections()
        {
            Transform[][] joints = this.FingerJoints;
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


        /// <summary> Returns the position of a joint relative to the wrist. Without scaling. </summary>
        /// <param name="jointTransform"></param>
        /// <param name="wristTransform"></param>
        /// <returns></returns>
        private SGCore.Kinematics.Vect3D RelativePosition(Transform jointTransform, Transform wristTransform)
        {
            Vector3 localPos = Quaternion.Inverse(wristTransform.rotation) * (jointTransform.position - wristTransform.position);
            //SG_Util.ToPosition(fingerJoints[f][0].position, true);
            return SG.Util.SG_Conversions.ToPosition(localPos, true);
        }


        /// <summary> Generates a BasicHandModel based on the Joint Transforms of this hand. </summary>
        /// <returns></returns>
        private SGCore.Kinematics.BasicHandModel GenerateHandModel()
        {
            bool right = this.handSide != HandSide.LeftHand;
            float[][] handLenghts = new float[5][];
            SGCore.Kinematics.Vect3D[] startPositions = new SGCore.Kinematics.Vect3D[5];

            Transform[][] fingerJoints = FingerJoints;
            SGCore.Kinematics.BasicHandModel defaultH = SGCore.Kinematics.BasicHandModel.Default(right);

            for (int f=0; f<5; f++)
            {
                // SGCore.Kinematics.Vect3D lastPos = fingerJoints[f].Length > 0 ? SG_Util.ToPosition(fingerJoints[f][0].position, true) 
                //     : defaultH.GetJointPosition((SGCore.Finger)f);

                SGCore.Kinematics.Vect3D lastPos = fingerJoints[f].Length > 0 ? RelativePosition(fingerJoints[f][0], this.wristTransform)
                    : defaultH.GetJointPosition((SGCore.Finger)f);

                startPositions[f] = lastPos; //the first position is the starting position.

                handLenghts[f] = new float[3];
                float[] defaultL = handLenghts[f] = defaultH.GetFingerLengths((SGCore.Finger)f);

                for (int j=1; j<4; j++)
                {
                    if (fingerJoints[f].Length > j)
                    {
                        SGCore.Kinematics.Vect3D currPos = RelativePosition(fingerJoints[f][j], this.wristTransform);
                        handLenghts[f][j - 1] = (currPos.x - lastPos.x); //converts from m to mm
                        lastPos = currPos; //update
                    }
                    else
                    {
                        handLenghts[f][j - 1] = defaultL[j - 1];
                    }
                }
            }
            SGCore.Kinematics.BasicHandModel HM = new SGCore.Kinematics.BasicHandModel(right, handLenghts, startPositions);
            //Debug.Log("Collected HandModelInfo: " + HM.ToString(true));
            return HM;
        }




        /// <summary> Retrieve the fingertip transform of this Hand Model. </summary>
        /// <param name="finger"></param>
        /// <param name="fingerTip"></param>
        /// <returns></returns>
        public bool GetFingerTip(SG_HandSection finger, out Transform fingerTip)
        {
            int f = (int)finger;
            if (f > -1 && f < 5)
            {
                Transform[][] joints = FingerJoints;
                fingerTip = joints[f].Length > 3 ? joints[f][3] : null;
            }
            else { fingerTip = null; }
            return fingerTip != null;
        }


        /// <summary> Create/Destroy a set of small spheres on each of the hand model transforms. 
        /// Shows devs exaclty where their transforms are. As sometimes, your movement set to center instead of Pivot. </summary>
        public bool DebugEnabled
        {
            get
            {
                return fingerDebug != null;
            }
            set
            {
                if (value && fingerDebug == null) //create
                {
                    Transform[][] joints = this.FingerJoints;
                    fingerDebug = new GameObject[joints.Length][];
                    for (int f = 0; f < joints.Length; f++)
                    {
                        fingerDebug[f] = new GameObject[joints[f].Length];
                        for (int j = 0; j < joints[f].Length; j++)
                        {
                            fingerDebug[f][j] = SG.Util.SG_Util.SpawnSphere(0.005f, joints[f][j], false);
                        }
                    }
                    if (wristTransform != null) { wristDebug = SG.Util.SG_Util.SpawnSphere(0.015f, wristTransform, false); }
                }
                else if (!value && fingerDebug != null) //destroy
                {
                    for (int f = 0; f < fingerDebug.Length; f++)
                    {
                        for (int j = 0; j < fingerDebug[f].Length; j++)
                        {
                            GameObject.Destroy(fingerDebug[f][j]);
                        }
                    }
                    fingerDebug = null;
                    if (wristDebug != null) { GameObject.Destroy(wristDebug); }
                }
            }
        }
        
    }
}