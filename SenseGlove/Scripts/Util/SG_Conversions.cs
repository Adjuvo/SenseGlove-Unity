using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{
    /// <summary> Utility class specifically for converting between SenseGlove's internal coordinate system and Unity's coordinate system. </summary>
	public static class SG_Conversions
	{

        //-------------------------------------------------------------------------------------------------------------------------
        // Positions

        /// <summary>  Convert a Vect3D position taken from SGCore into a Vector3 Unity Position. </summary>
        /// <param name="pos"></param>
        /// <param name="scale">Scale from mm to m</param>
        /// <returns></returns>
        public static Vector3 ToUnityPosition(SGCore.Kinematics.Vect3D pos, bool scale = true)
        {
            Vector3 res = new Vector3(pos.x, pos.z, pos.y);
            if (scale) { res /= 1000f; }
            return res;
        }

        /// <summary> Convert an array of Vect3D positions taken from SGCore into a Vector3 Unity Position array. </summary>
        /// <param name="pos"></param>
        /// <param name="scale">Scale from mm to m</param>
        /// <returns></returns>
        public static Vector3[] ToUnityPositions(SGCore.Kinematics.Vect3D[] pos, bool scale = true)
        {
            if (pos != null)
            {
                Vector3[] res = new Vector3[pos.Length];
                for (int f = 0; f < pos.Length; f++)
                {
                    res[f] = SG_Conversions.ToUnityPosition(pos[f], scale);
                }
                return res;
            }
            return new Vector3[] { };
        }

        /// <summary> Convert a 2D array of positions from SGCore into a Unity 2D array of positions </summary>
        /// <param name="vector"></param>
        /// <param name="scale">Scale from mm to m</param>
        /// <returns></returns>
        public static Vector3[][] ToUnityPositions(SGCore.Kinematics.Vect3D[][] vector, bool scale = true)
        {
            Vector3[][] res = new Vector3[vector.Length][];
            for (int f = 0; f < vector.Length; f++)
            {
                res[f] = ToUnityPositions(vector[f], scale);
            }
            return res;
        }


        /// <summary> Convert from a Unity Vector3 position to a Vect3D used by SGCore. </summary>
        /// <param name="pos"></param>
        /// <param name="scale">scale m to mm</param>
        /// <returns></returns>
        public static SGCore.Kinematics.Vect3D ToPosition(Vector3 pos, bool scale = true)
        {
            SGCore.Kinematics.Vect3D res = new SGCore.Kinematics.Vect3D(pos.x, pos.z, pos.y);
            if (scale) { res.Scale(1000); } //from m to mm
            return res;
        }


        /// <summary> Convert an array of Unity Vector3 positions into a Vect3D Position array used by SGCore. </summary>
        /// <param name="pos"></param>
        /// <param name="scale">scale m to mm</param>
        /// <returns></returns>
        public static SGCore.Kinematics.Vect3D[] ToPosition(Vector3[] pos, bool scale = true)
        {
            SGCore.Kinematics.Vect3D[] res = new SGCore.Kinematics.Vect3D[pos.Length];
            for (int f = 0; f < pos.Length; f++)
            {
                res[f] = SG_Conversions.ToPosition(pos[f], scale);
            }
            return res;
        }



        //-------------------------------------------------------------------------------------------------------------------------
        // Quaternion Rotations


        /// <summary> Convert a Quaternion rotation taken from SGCore into a Unity Quaternion rotation. </summary>
        /// <param name="quat"></param>
        /// <returns></returns>
        public static Quaternion ToUnityQuaternion(SGCore.Kinematics.Quat quat)
        {
            return new Quaternion(-quat.x, -quat.z, -quat.y, quat.w);
        }


        /// <summary> Covert an array of Quaternions from SGCore into a Unity Quaternion array. </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Quaternion[] ToUnityQuaternions(SGCore.Kinematics.Quat[] vector)
        {
            Quaternion[] res = new Quaternion[vector.Length];
            for (int f = 0; f < vector.Length; f++)
            {
                res[f] = ToUnityQuaternion(vector[f]);
            }
            return res;
        }

        /// <summary> Covert a 2D array of Quaternions from SGCore into a 2D Unity Quaternion array. </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Quaternion[][] ToUnityQuaternions(SGCore.Kinematics.Quat[][] vector)
        {
            Quaternion[][] res = new Quaternion[vector.Length][];
            for (int f = 0; f < vector.Length; f++)
            {
                res[f] = ToUnityQuaternions(vector[f]);
            }
            return res;
        }



        /// <summary> Convert a Unity Quaternion into a Quaternion rotation used by SGCore. </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static SGCore.Kinematics.Quat ToQuaternion(Quaternion Q)
        {
            return new SGCore.Kinematics.Quat(-Q.x, -Q.z, -Q.y, Q.w);
        }



        //-------------------------------------------------------------------------------------------------------------------------
        // Euler Rotations


        /// <summary> Convert a set of euler angles from SGCore into one that can be used by Unity. </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static Vector3 ToUnityEuler(SGCore.Kinematics.Vect3D euler)
        {
            euler = SGCore.Kinematics.Values.Degrees(euler);
            return new Vector3(-euler.x, -euler.z, -euler.y);
        }


        /// <summary> Convert an array of euler angles from SGCore into an array of Unity Euler angles. </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3[] ToUnityEulers(SGCore.Kinematics.Vect3D[] vector)
        {
            Vector3[] res = new Vector3[vector.Length];
            for (int f = 0; f < vector.Length; f++)
            {
                res[f] = ToUnityEuler(vector[f]);
            }
            return res;
        }

        /// <summary> Convert a 2D array of euler angles from SGCore into a 2D array of Unity Euler angles </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3[][] ToUnityEulers(SGCore.Kinematics.Vect3D[][] vector)
        {
            Vector3[][] res = new Vector3[vector.Length][];
            for (int f = 0; f < vector.Length; f++)
            {
                res[f] = ToUnityEulers(vector[f]);
            }
            return res;
        }


        /// <summary> Convert a set of euler angles from Unity into one used by SGCore. </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static SGCore.Kinematics.Vect3D ToEuler(Vector3 unityEuler)
        {
            return SGCore.Kinematics.Values.Radians(new SGCore.Kinematics.Vect3D(-unityEuler.x, -unityEuler.z, -unityEuler.y));
        }

        /// <summary> Convert a couple of sets of Unity Euler Angles </summary>
        /// <param name="unityEuler"></param>
        /// <returns></returns>
        public static SGCore.Kinematics.Vect3D[] ToEuler(Vector3[] unityEuler)
        {
            SGCore.Kinematics.Vect3D[] res = new SGCore.Kinematics.Vect3D[unityEuler.Length];
            for (int i=0; i<unityEuler.Length; i++)
            {
                res[i] = ToEuler(unityEuler[i]);
            }
            return res;
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Quaternion Mirrors

        /// <summary> Mirror a Question over the X-Axis </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static Quaternion MirrorX(Quaternion Q)
        {
            return new Quaternion(-Q.x, Q.y, Q.z, -Q.w);
        }

        /// <summary> Mirror a quaternion over a Y-axis </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static Quaternion MirrorY(Quaternion Q)
        {
            return new Quaternion(Q.x, -Q.y, Q.z, -Q.w);
        }

        /// <summary> Mirro a quaternion around the Z axis </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static Quaternion MirrorZ(Quaternion Q)
        {
            return new Quaternion(Q.x, Q.y, -Q.z, -Q.w);
        }

    }
}