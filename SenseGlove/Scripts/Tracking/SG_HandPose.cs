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


        /// <summary> The position of the wrist in world space. </summary>
        public Vector3 wristPosition;
        /// <summary> The rotation of the wrist in world space. </summary>
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


        /// <summary> Mirrors this hand pose around the z-axis, to that of the opposite hand. It does not mirror the wrist location, though! </summary>
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
            return new SG_HandPose(mirrorAngles, mirrorJoints, mirrorPos, newHandedness, this.wristPosition, this.wristRotation, this.normalizedFlexion);
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

        public override string ToString()
        {
            string res = this.rightHanded ? "Right Hand Pose" : "Left Hand Pose";
            res += " [";
            for (int f = 0; f < this.normalizedFlexion.Length; f++)
            {
                res += SG.Util.SG_Util.UniLengthStr(this.normalizedFlexion[f], 2);
                if (f < normalizedFlexion.Length - 1)
                {
                    res += ", ";
                }
            }
            return res + "]";
        }

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

        /// <summary> Convert this SG_HandPose back into an internal SGCore Pose. Useful when you're passing data back and forth, or for generating Unit Test Data. </summary>
        /// <returns></returns>
        public SGCore.HandPose ToInternalPose()
        {
            SGCore.Kinematics.Vect3D[][] angles = Util.SG_Conversions.ToEuler(this.jointAngles);
            SGCore.Kinematics.Quat[][] rotations = Util.SG_Conversions.ToQuaternions(this.jointRotations);
            SGCore.Kinematics.Vect3D[][] positions = Util.SG_Conversions.ToPosition(this.jointPositions);
            return new SGCore.HandPose(this.rightHanded, positions, rotations, angles);
        }

        //----------------------------------------------------------------------------------------------
        // Sreialize / Deserialize - Recording

        public string Serialize()
        {
            string serialized = SGCore.Util.Serializer.Enclose( this.rightHanded ? "R" : "L" );
            serialized += SG.Util.SG_Conversions.Serialize( wristPosition );
            serialized += SG.Util.SG_Conversions.Serialize( wristRotation );
            serialized += SG.Util.SG_Conversions.Serialize( jointPositions );
            serialized += SG.Util.SG_Conversions.Serialize( jointRotations );
            serialized += SG.Util.SG_Conversions.Serialize( jointAngles );
            serialized += SGCore.Util.Serializer.Serialize( normalizedFlexion );

            return serialized;
        }

        public static bool Deserialize(string serialized, out SG_HandPose deserializedPose)
        {
            string[] split = SGCore.Util.Serializer.SplitBlocks(serialized);
            if (split.Length > 6) //7 entries.
            {
                bool right = !(split[0].Length > 0 && split[0][0] == 'L');
                Vector3 wristPos = SG.Util.SG_Conversions.DeserializeVector3(split[1]);
                Quaternion wristRot = SG.Util.SG_Conversions.DeserializeQuaternion(split[2]);
                Vector3[][] jointPos = SG.Util.SG_Conversions.DeserializeVector3s2D(split[3]);
                Quaternion[][] jointRots = SG.Util.SG_Conversions.DeserializeQuaternions2D(split[4]);
                Vector3[][] jointAngls = SG.Util.SG_Conversions.DeserializeVector3s2D(split[5]);
                float[] normalizedFlexes = SGCore.Util.Serializer.DeserializeFloats(split[6]);
                deserializedPose = new SG_HandPose(jointAngls, jointRots, jointPos, right, wristPos, wristRot, normalizedFlexes);
                return true;
            }
            deserializedPose = null;
            return false;
        }

        /// <summary> Returns true if the otherPose has the same values as this pose. </summary>
        /// <param name="otherPose"></param>
        /// <returns></returns>
        public bool Equals(SG_HandPose otherPose)
        {
            if (otherPose.rightHanded != this.rightHanded
                || !SG.Util.SG_Conversions.RoughlyEqual(otherPose.wristPosition, this.wristPosition)
                || !SG.Util.SG_Conversions.RoughlyEqual(otherPose.wristRotation, this.wristRotation)
                || otherPose.normalizedFlexion.Length != this.normalizedFlexion.Length)
            {
                return false;
            }
            return Util.SG_Conversions.RoughlyEqual(this.normalizedFlexion, otherPose.normalizedFlexion)
                && Util.SG_Conversions.RoughlyEqual(this.jointAngles, otherPose.jointAngles)
                && Util.SG_Conversions.RoughlyEqual(this.jointPositions, otherPose.jointPositions)
                && Util.SG_Conversions.RoughlyEqual(this.jointRotations, otherPose.jointRotations);
        }

    }

}