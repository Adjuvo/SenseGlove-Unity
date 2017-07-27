using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Util
{ 
    
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ToString Methods

    /// <summary> Convert a Unity Vector3 to a string with a greater precision that it default method. </summary>
    /// <param name="V"></param>
    /// <returns></returns>
    public static string ToString(Vector3 V)
    {
        return "[" + V.x + ", " + V.y + ", " + V.z + "]";
    }

    /// <summary> Convert a Unity Quaternion to a string with a greater precision that it default method.  </summary>
    /// <param name="Q"></param>
    /// <returns></returns>
    public static string ToString(Quaternion Q)
    {
        return "[" + Q.x + ", " + Q.y + ", " + Q.z + ", " + Q.w + "]";
    }

    /// <summary> Convert a float[] to a string with a greater precision that it default Unity(?) method. </summary>
    /// <param name="V"></param>
    /// <returns></returns>
    public static string ToString(float[] V)
    {
        string res = "[";
        for (int i=0; i<V.Length; i++)
        {
            res += V[i];
            if (i < V.Length - 1) { res += ", "; }
        }
        return res + "]";
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Conversion

    /// <summary> DLL indices for the different variables. </summary>
    private static int x = 0, y = 1, z = 2, w = 3;

    /// <summary>
    /// Convert a float[3] position taken from the DLL into a Unity Position.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Vector3 ToUnityPosition(float[] pos)
    {
        return new Vector3(pos[x], pos[z], pos[y]);
    }

    /// <summary>
    /// Convert an array of float[3] positions taken from the DLL into a Vector3[].
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Vector3[] ToUnityPosition(float[][] pos)
    {
        Vector3[] res = new Vector3[pos.Length];
        for (int f=0; f<pos.Length;f++)
        {
            res[f] = SenseGlove_Util.ToUnityPosition(pos[f]);
        }
        return res;
    }

    /// <summary> Convert from a unity vector3 to a float[3] used in the DLL. </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static float[] ToPosition(Vector3 pos)
    {
        return new float[] { pos.x, pos.z, pos.y };
    }

    /// <summary>
    /// Convert an array of unity positions back into an array used by the DLL
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static float[][] ToPosition(Vector3[] pos)
    {
        float[][] res = new float[pos.Length][];
        for (int f = 0; f < pos.Length; f++)
        {
            res[f] = SenseGlove_Util.ToPosition(pos[f]);
        }
        return res;
    }



    /// <summary>
    /// Convert a float[4] quaternion taken from the DLL into a Unity Quaternion. 
    /// </summary>
    /// <param name="quat"></param>
    /// <returns></returns>
    public static Quaternion ToUnityQuaternion(float[] quat)
    {
        return new Quaternion(-quat[x], -quat[z], -quat[y], quat[w]);
    }

    /// <summary> Convert a unity Quaternion into a float[4] used in the DLL. </summary>
    /// <param name="Q"></param>
    /// <returns></returns>
    public static float[] ToQuaternion(Quaternion Q)
    {
        return new float[] { -Q.x, -Q.z, -Q.y, Q.w };
    }

    



    /// <summary>
    /// Convert a unity eulerAngles notation into one used by the DLL.
    /// </summary>
    /// <param name="euler"></param>
    /// <returns></returns>
    public static float[] ToEuler(Vector3 euler)
    {
        return SenseGloveCs.Values.Radians(new float[] { -euler.x, -euler.z, -euler.y });
    }

    /// <summary> Convert a set of euler angles from the DLL into the Unity notation. </summary>
    /// <param name="euler"></param>
    /// <returns></returns>
    public static Vector3 ToUnityEuler(float[] euler)
    {
        euler = SenseGloveCs.Values.Degrees(euler);
        return new Vector3(-euler[x], -euler[z], -euler[y]);
    }


}


/// <summary>
/// Determines where the TrackedObject should connect to.
/// </summary>
public enum AnchorPoint
{
    Wrist = 0,
    ForeArm
}


