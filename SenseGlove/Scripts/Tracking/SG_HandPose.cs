using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
   
    /// <summary> Data class containing all variables needed to draw or analyze a hand. Converted into Unity Coordinates from the internal SenseGlove coordinates. </summary>
    public class SG_HandPose
    {
        //----------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Whether or not this pose was made for a right or left hand. </summary>
        public bool rightHanded;

        /// <summary> The angles of each joint, in degrees, where flexing the finger creates a negative z-rotation. 
        /// The first index [0..4] determines the finger (thumb..pinky), while the second [0..2]  determines joint (CMC, MCP, IP for thumb. MCP, PIP, DIP for fingers.) </summary>
        public Vector3[][] jointAngles;

        /// <summary> The quaternion rotation of each joint, relative to a Wrist Transform: JointRotation * WristRotation = 3D Rotation. 
        /// The first index [0..4] determines the finger (thumb..pinky), while the second [0..2]  determines joint (CMC, MCP, IP for thumb. MCP, PIP, DIP for fingers.) </summary>
        public Quaternion[][] jointRotations;

        /// <summary> The position of each joint, in meters, relative to a Wrist Transform: (JointPosition * WristRotation) + WristPosition = 3D Position. 
        /// The first index [0..4] determines the finger (thumb..pinky), while the second [0..2]  determines joint (CMC, MCP, IP for thumb. MCP, PIP, DIP for fingers.) </summary>
        public Vector3[][] jointPositions;

        /// <summary> The total flexion of each finger, normalized to values between 0 (fingers fully extended) and 1 (fingers fully flexed). 
        /// The index [0..4] determines the finger (thumb..pinky). </summary>
        public float[] normalizedFlexion;


        /// <summary> The position in world space of the wrist </summary>
        public Vector3 wristPosition;
        /// <summary> The rotation in world space of the wrist. </summary>
        public Quaternion wristRotation;


        //----------------------------------------------------------------------------------------------
        // Construction

        /// <summary> Create a new SG_HandPose from an internal SenseGlove Pose. </summary>
        /// <param name="handPose"></param>
        public SG_HandPose(SGCore.HandPose handPose)
        {
            jointAngles = Util.SG_Conversions.ToUnityEulers(handPose.handAngles);
            jointRotations = Util.SG_Conversions.ToUnityQuaternions(handPose.jointRotations);
            jointPositions = Util.SG_Conversions.ToUnityPositions(handPose.jointPositions, true);
            normalizedFlexion = handPose.GetNormalizedFlexion();
            wristPosition = Vector3.zero;
            wristRotation = Quaternion.identity;
            rightHanded = handPose.isRight;
        }

        /// <summary> Manually create a new SG_HandPose. </summary>
        /// <param name="handAngles"></param>
        /// <param name="handRotations"></param>
        /// <param name="handPositions"></param>
        /// <param name="normalFlex"></param>
        public SG_HandPose(Vector3[][] handAngles, Quaternion[][] handRotations, Vector3[][] handPositions, bool rightHandedPose, float[] normalFlex = null)
        {
            jointAngles = handAngles;
            jointRotations = handRotations;
            jointPositions = handPositions;
            normalizedFlexion = normalFlex == null ? new float[5] : normalFlex;
            wristPosition = Vector3.zero;
            wristRotation = Quaternion.identity;
            rightHanded = rightHandedPose;
        }

        /// <summary> Manually create a new SG_HandPose. </summary>
        /// <param name="handAngles"></param>
        /// <param name="handRotations"></param>
        /// <param name="handPositions"></param>
        /// <param name="normalFlex"></param>
        public SG_HandPose(Vector3[][] handAngles, Quaternion[][] handRotations, Vector3[][] handPositions, bool rightHandedPose, Vector3 wristPos, Quaternion wristRot, float[] normalFlex = null)
        {
            jointAngles = handAngles;
            jointRotations = handRotations;
            jointPositions = handPositions;
            normalizedFlexion = normalFlex == null ? new float[5] : normalFlex;
            wristPosition = wristPos;
            wristRotation = wristRot;
            rightHanded = rightHandedPose;
        }



        /// <summary> Deep copies the values </summary>
        /// <param name="original"></param>
        public SG_HandPose(SG_HandPose original)
        {
            jointAngles = SG.Util.SG_Util.ArrayCopy(original.jointAngles);
            jointRotations = SG.Util.SG_Util.ArrayCopy(original.jointRotations);
            jointPositions = SG.Util.SG_Util.ArrayCopy(original.jointPositions);
            normalizedFlexion = SG.Util.SG_Util.ArrayCopy(original.normalizedFlexion);
            wristPosition = original.wristPosition;
            wristRotation = original.wristRotation;
            rightHanded = original.rightHanded;
        }

        /// <summary> Generates an 'idle' handPose for a left or right hand, where the fingers are fully extended, and the thumb is slightly abducted. </summary>
        /// <param name="right"></param>
        /// <returns></returns>
        public static SG_HandPose Idle(bool right)
        {
            return new SG_HandPose(SGCore.HandPose.DefaultIdle(right));
        }


        //----------------------------------------------------------------------------------------------
        // Mirroring


        /// <summary> Mirrors this hand pose around the z-axis, to that of the opposite hand </summary>
        /// <returns></returns>
        public SG_HandPose Mirror() //todo: make this part SGCore.HandPose as well.
        {
            bool newHandedness = !this.rightHanded;
            Quaternion[][] mirrorJoints = new Quaternion[this.jointRotations.Length][];
            for (int f = 0; f < jointRotations.Length; f++)
            {
                mirrorJoints[f] = new Quaternion[jointRotations[f].Length];
                for (int j = 0; j < jointRotations[f].Length; j++)
                {
                    mirrorJoints[f][j] = SG.Util.SG_Conversions.MirrorZ(jointRotations[f][j]);
                }
            }
            Vector3[][] mirrorPos = new Vector3[this.jointPositions.Length][];
            for (int f = 0; f < jointPositions.Length; f++)
            {
                mirrorPos[f] = new Vector3[jointPositions[f].Length];
                for (int j = 0; j < jointPositions[f].Length; j++)
                {
                    mirrorPos[f][j] = new Vector3(jointPositions[f][j].x, jointPositions[f][j].y, -jointPositions[f][j].z);
                }
            }
            Vector3[][] mirrorAngles = new Vector3[this.jointAngles.Length][];
            for (int f = 0; f < jointAngles.Length; f++)
            {
                mirrorAngles[f] = new Vector3[jointAngles[f].Length];
                for (int j = 0; j < jointAngles[f].Length; j++)
                {
                    mirrorAngles[f][j] = new Vector3(-jointAngles[f][j].x, -jointAngles[f][j].y, jointAngles[f][j].z);
                }
            }
            return new SG_HandPose(mirrorAngles, mirrorJoints, mirrorPos, newHandedness);
        }


        //----------------------------------------------------------------------------------------------
        // Combining Poses


        /// <summary> Combines the finger tracking of one HandPose with a set wrist location. Allows for deep copy-ing. </summary>
        /// <param name="wristPosition"></param>
        /// <param name="wristRotation"></param>
        /// <param name="fingerTracking"></param>
        /// <param name="deepCopy"></param>
        /// <returns></returns>
        public static SG_HandPose Combine(Vector3 wristPosition, Quaternion wristRotation, SG_HandPose fingerTracking, bool deepCopy)
        {
            if (deepCopy)
            {
                SG_HandPose res = new SG_HandPose(fingerTracking); //deep copies all finger tracking values to res.
                res.wristPosition = wristPosition;
                res.wristRotation = wristRotation;
                return res;
            }
            //a shallow copy - pass all arrays by refence(?)
            return new SG_HandPose(fingerTracking.jointAngles, fingerTracking.jointRotations, fingerTracking.jointPositions, fingerTracking.rightHanded, wristPosition, wristRotation, fingerTracking.normalizedFlexion);
        }


        /// <summary> Combine the wrist location of one pose with the finger tracking of another. </summary>
        /// <param name="wristTracking"></param>
        /// <param name="fingerTracking"></param>
        /// <param name="deepCopy">Whether or not to copy all varables, or simply pass them by refrence.</param>
        /// <returns></returns>
        public static SG_HandPose Combine(SG_HandPose wristTracking, SG_HandPose fingerTracking, bool deepCopy)
        {
            return Combine(wristTracking.wristPosition, wristTracking.wristRotation, fingerTracking, deepCopy);
        }


        //----------------------------------------------------------------------------------------------
        // Utility Functions

        /// <summary> Returns the total flexion of each finger in degrees. In Unity, flexion movements create a negative value. The index [0..4] determines the finger (thumb..pinky). </summary>
        public float[] TotalFlexions
        {
            get
            {
                float[] res = new float[this.jointAngles.Length];
                for (int f=0; f<this.jointAngles.Length; f++)
                {
                    for (int j = 0; j < this.jointAngles[f].Length; j++)
                    {
                        res[f] += jointAngles[f][j].z;
                    }
                }
                return res;
            }
        }

        ///// <summary> Returns this HandPose's Normalized Abductions (thumb abd, finger spread) between 0 and 1. </summary>
        ///// <returns></returns>
        //public float[] NormalizeAbductions()
        //{
        //    float[] res = new float[this.jointAngles.Length];
        //    for (int f=0; f<res.Length; f++)
        //    {
        //        float totalAbd = this.jointAngles[f][0].y; //it's y over here.
        //        res[f] = SG_ManualPoser.NormalizeAbduction((SGCore.Finger)f, totalAbd, this.rightHanded);
        //    }
        //    return res;
        //}

    }

}