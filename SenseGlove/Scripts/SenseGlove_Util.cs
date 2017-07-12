using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Util
{ 

    public static string ToString(Vector3 V)
    {
        return "[" + V.x + ", " + V.y + ", " + V.z + "]";
    }

    public static string ToString(Quaternion Q)
    {
        return "[" + Q.x + ", " + Q.y + ", " + Q.z + ", " + Q.w + "]";
    }

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

    public static Vector3 ToUnityPosition(float[] pos)
    {
        return new Vector3(pos[x], pos[z], pos[y]);
    }

    private static int x = 0, y = 1, z = 2, w = 3;

    public static Quaternion ToUnityQuaternion(float[] quat)
    {
        return new Quaternion(-quat[x], -quat[z], -quat[y], quat[w]);
    }

    public static float[] ToQuaternion(Quaternion Q)
    {
        return new float[] { -Q.x, -Q.z, -Q.y, Q.w };
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

/// <summary>
/// Grab event arguments used by the physics- and gesture based grab scripts.
/// </summary>
public class GrabEventArgs : System.EventArgs
{
    public GameObject gameObject { get; set; }
}
