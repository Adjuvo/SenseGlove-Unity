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

        public static SGCore.Kinematics.Vect3D[][] ToPosition(Vector3[][] pos, bool scale = true)
        {
            SGCore.Kinematics.Vect3D[][] res = new SGCore.Kinematics.Vect3D[pos.Length][];
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

        public static SGCore.Kinematics.Quat[] ToQuaternions(Quaternion[] Q)
        {
            SGCore.Kinematics.Quat[] res = new SGCore.Kinematics.Quat[Q.Length];
            for (int i=0; i<res.Length; i++)
            {
                res[i] = ToQuaternion(Q[i]);
            }
            return res;
        }


        public static SGCore.Kinematics.Quat[][] ToQuaternions(Quaternion[][] Q)
        {
            SGCore.Kinematics.Quat[][] res = new SGCore.Kinematics.Quat[Q.Length][];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = ToQuaternions(Q[i]);
            }
            return res;
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
            for (int i = 0; i < unityEuler.Length; i++)
            {
                res[i] = ToEuler(unityEuler[i]);
            }
            return res;
        }

        /// <summary> Convert a couple of sets of Unity Euler Angles </summary>
        /// <param name="unityEuler"></param>
        /// <returns></returns>
        public static SGCore.Kinematics.Vect3D[][] ToEuler(Vector3[][] unityEuler)
        {
            SGCore.Kinematics.Vect3D[][] res = new SGCore.Kinematics.Vect3D[unityEuler.Length][];
            for (int i = 0; i < unityEuler.Length; i++)
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

        // -------------------------------------------------------------------------------------
        // Serialization, Deserialization.

        /// <summary> Store a Vector3 as a string representation, so it may be stored on disk and retrieved later. </summary>
        /// <param name="vect"></param>
        /// <returns></returns>
        public static string Serialize(Vector3 vect, bool enclosed = true)
        {
            string res = vect.x.ToString() + SGCore.Util.Serializer.valueDelim + vect.y.ToString() + SGCore.Util.Serializer.valueDelim + vect.z.ToString();
            return enclosed ? SGCore.Util.Serializer.openChar + res + SGCore.Util.Serializer.closeChar : res;
        }

        /// <summary> Store a Vector3[] as a string representation, so it may be stored on disk and retrieved later. </summary>
        /// <param name="vects"></param>
        /// <param name="enclosed"></param>
        /// <returns></returns>
        public static string Serialize(Vector3[] vects, bool enclosed = true)
        {
            string res = "";
            for (int i = 0; i < vects.Length; i++)
            {
                res += Serialize(vects[i]);
            }
            return enclosed ? SGCore.Util.Serializer.openChar + res + SGCore.Util.Serializer.closeChar : res;
        }

        /// <summary> Store a Vector3[][] as a string representation, so it may be stored on disk and retrieved later </summary>
        /// <param name="vects"></param>
        /// <param name="enclosed"></param>
        /// <returns></returns>
        public static string Serialize(Vector3[][] vects, bool enclosed = true)
        {
            string res = "";
            for (int i = 0; i < vects.Length; i++)
            {
                res += Serialize(vects[i]);
            }
            return enclosed ? SGCore.Util.Serializer.openChar + res + SGCore.Util.Serializer.closeChar : res;
        }



        /// <summary> Convert a string representation of a Vector3 back into useable values </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static Vector3 DeserializeVector3(string serialized)
        {
            if (serialized.Length > 0)
            {
                string[] parsed = serialized.Split(SGCore.Util.Serializer.valueDelim);
                return new Vector3
                (
                    parsed.Length > 0 ? SGCore.Util.StrStuff.ToFloat(parsed[0]) : 0,
                    parsed.Length > 1 ? SGCore.Util.StrStuff.ToFloat(parsed[1]) : 0,
                    parsed.Length > 2 ? SGCore.Util.StrStuff.ToFloat(parsed[2]) : 0
                );
            }
            return Vector3.zero;
        }

        /// <summary> Convert a string representation of a Vector3[] back into useable values </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static Vector3[] DeserializeVector3s(string serialized)
        {
            string[] split = SGCore.Util.Serializer.SplitBlocks(serialized);
            Vector3[] res = new Vector3[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                res[i] = DeserializeVector3(split[i]);
            }
            return res;
        }

        /// <summary> Convert a string representation of a Vector3[][] back into useable values  </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static Vector3[][] DeserializeVector3s2D(string serialized)
        {
            string[] split = SGCore.Util.Serializer.SplitBlocks(serialized);
            Vector3[][] res = new Vector3[split.Length][];
            for (int i = 0; i < split.Length; i++)
            {
                res[i] = DeserializeVector3s(split[i]);
            }
            return res;
        }





        /// <summary> Store a Quanternion as a string representation, so it may be stored on disk and retrieved later. </summary>
        /// <param name="quat"></param>
        /// <returns></returns>
        public static string Serialize(Quaternion quat, bool enclosed = true)
        {
            string res= quat.x.ToString() + SGCore.Util.Serializer.valueDelim + quat.y.ToString() + SGCore.Util.Serializer.valueDelim + quat.z.ToString() + SGCore.Util.Serializer.valueDelim + quat.w.ToString();
            return enclosed ? SGCore.Util.Serializer.openChar + res + SGCore.Util.Serializer.closeChar : res;
        }


        /// <summary> Store a Quaternion[] as a string representation, so it may be stored on disk and retrieved later. </summary>
        /// <param name="vects"></param>
        /// <param name="enclosed"></param>
        /// <returns></returns>
        public static string Serialize(Quaternion[] quats, bool enclosed = true)
        {
            string res = "";
            for (int i = 0; i < quats.Length; i++)
            {
                res += Serialize(quats[i]);
            }
            return enclosed ? SGCore.Util.Serializer.openChar + res + SGCore.Util.Serializer.closeChar : res;
        }

        /// <summary> Store a Quaternion[][] as a string representation, so it may be stored on disk and retrieved later </summary>
        /// <param name="vects"></param>
        /// <param name="enclosed"></param>
        /// <returns></returns>
        public static string Serialize(Quaternion[][] quats, bool enclosed = true)
        {
            string res = "";
            for (int i = 0; i < quats.Length; i++)
            {
                res += Serialize(quats[i]);
            }
            return enclosed ? SGCore.Util.Serializer.openChar + res + SGCore.Util.Serializer.closeChar : res;
        }







        /// <summary>  Convert a string representation of a Quaternion back into useable values </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static Quaternion DeserializeQuaternion(string serialized)
        {
            if (serialized.Length > 0)
            {
                string[] parsed = serialized.Split(SGCore.Util.Serializer.valueDelim);
                return new Quaternion
                (
                    parsed.Length > 0 ? SGCore.Util.StrStuff.ToFloat(parsed[0]) : 0,
                    parsed.Length > 1 ? SGCore.Util.StrStuff.ToFloat(parsed[1]) : 0,
                    parsed.Length > 2 ? SGCore.Util.StrStuff.ToFloat(parsed[2]) : 0,
                    parsed.Length > 3 ? SGCore.Util.StrStuff.ToFloat(parsed[3]) : 0
                );
            }
            return Quaternion.identity;
        }



        /// <summary> Convert a string representation of a Vector3[] back into useable values </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static Quaternion[] DeserializeQuaternions(string serialized)
        {
            string[] split = SGCore.Util.Serializer.SplitBlocks(serialized);
            Quaternion[] res = new Quaternion[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                res[i] = DeserializeQuaternion(split[i]);
            }
            return res;
        }

        /// <summary> Convert a string representation of a Vector3[][] back into useable values  </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static Quaternion[][] DeserializeQuaternions2D(string serialized)
        {
            string[] split = SGCore.Util.Serializer.SplitBlocks(serialized);
            Quaternion[][] res = new Quaternion[split.Length][];
            for (int i = 0; i < split.Length; i++)
            {
                res[i] = DeserializeQuaternions(split[i]);
            }
            return res;
        }

        /// <summary> Returns true if this Vector3 is roughly equal to another. </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(Vector3 v1, Vector3 v2, float acceptableOffset = 0.01f)
        {
            bool xOK = SGCore.Kinematics.Values.FloatEquals(v1.x, v2.x, acceptableOffset);
            bool yOK = SGCore.Kinematics.Values.FloatEquals(v1.y, v2.y, acceptableOffset);
            bool zOK = SGCore.Kinematics.Values.FloatEquals(v1.z, v2.z, acceptableOffset);
            return xOK && yOK && zOK;
        }

        /// <summary> Returns true if this Vector3[] is roughly equal to another. </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(Vector3[] v1, Vector3[] v2, float acceptableOffset = 0.01f)
        {
            if (v1.Length != v2.Length)
            {
                return false;
            }
            for (int i = 0; i < v1.Length; i++)
            {
                if (!RoughlyEqual(v1[i], v2[i], acceptableOffset))
                { 
                    return false; 
                }
            }
            return true;
        }

        /// <summary> Returns true if this Vector3[][] is roughly equal to another. </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(Vector3[][] v1, Vector3[][] v2, float acceptableOffset = 0.01f)
        {
            if (v1.Length != v2.Length)
            {
                return false;
            }
            for (int i = 0; i < v1.Length; i++)
            {
                if (!RoughlyEqual(v1[i], v2[i], acceptableOffset)) 
                { 
                    return false;
                }
            }
            return true;
        }

        /// <summary> Returns true if this Quaternion is roughly equal to another. </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(Quaternion q1, Quaternion q2, float acceptableOffset = 0.0001f)
        {
            return 
                SGCore.Kinematics.Values.FloatEquals(q1.x, q2.x, acceptableOffset)
                && SGCore.Kinematics.Values.FloatEquals(q1.y, q2.y, acceptableOffset)
                && SGCore.Kinematics.Values.FloatEquals(q1.z, q2.z, acceptableOffset)
                && SGCore.Kinematics.Values.FloatEquals(q1.w, q2.w, acceptableOffset);
        }

        /// <summary> Returns true if this Quaternion[] is roughly equal to another. </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(Quaternion[] q1, Quaternion[] q2, float acceptableOffset = 0.0001f)
        {
            if (q1.Length != q2.Length)
            {
                return false;
            }
            for (int i=0; i<q1.Length; i++)
            {
                if ( !RoughlyEqual(q1[i], q2[i], acceptableOffset) ) { return false; }
            }
            return true;
        }

        /// <summary> Returns true if this Quaternion[][] is roughly equal to another. </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(Quaternion[][] q1, Quaternion[][] q2, float acceptableOffset = 0.0001f)
        {
            if (q1.Length != q2.Length)
            {
                return false;
            }
            for (int i = 0; i < q1.Length; i++)
            {
                if (!RoughlyEqual(q1[i], q2[i], acceptableOffset)) { return false; }
            }
            return true;
        }

        /// <summary> Returns true if two float[] contain the same values. </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <returns></returns>
        public static bool RoughlyEqual(float[] f1, float[] f2, float acceptableOffset = 0.001f)
        {
            if (f1.Length != f2.Length)
            {
                return false;
            }
            for (int i = 0; i < f1.Length; i++)
            {
                if (!SGCore.Kinematics.Values.FloatEquals(f1[i], f2[i], acceptableOffset)) { return false; }
            }
            return true;
        }

    }
}