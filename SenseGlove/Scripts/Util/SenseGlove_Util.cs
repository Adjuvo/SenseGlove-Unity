using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[Serializable]
public class SGEvent : UnityEvent { }

/// <summary> Contains methods that make the SenseGloveCs library work with Unity. </summary>
public static class SenseGlove_Util
{
    public enum MoveAxis
    {
        X = 0, Y, Z, NegativeX, NegativeY, NegativeZ
    }

    //--------------------------------------------------------------------------------------------------------------------
    // ToString Methods

    #region ToString

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

    /// <summary> Convert an int[] to a string with a greater precision that it default Unity(?) method. </summary>
    /// <param name="V"></param>
    /// <returns></returns>
    public static string ToString(int[] V)
    {
        string res = "[";
        for (int i = 0; i < V.Length; i++)
        {
            res += V[i];
            if (i < V.Length - 1) { res += ", "; }
        }
        return res + "]";
    }

    #endregion ToString

    //-------------------------------------------------------------------------------------------------------------------------
    // Conversion

    #region Conversion


    /// <summary>
    /// Convert a float[3] position taken from the DLL into a Unity Position.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Vector3 ToUnityPosition(SenseGloveCs.Kinematics.Vect3D pos)
    {
        return new Vector3(pos.x, pos.z, pos.y);
    }

    /// <summary>
    /// Convert an array of float[3] positions taken from the DLL into a Vector3[].
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static Vector3[] ToUnityPosition(SenseGloveCs.Kinematics.Vect3D[] pos)
    {
        if (pos != null)
        {
            Vector3[] res = new Vector3[pos.Length];
            for (int f = 0; f < pos.Length; f++)
            {
                res[f] = SenseGlove_Util.ToUnityPosition(pos[f]);
            }
            return res;
        }
        return new Vector3[] { };
    }

    /// <summary> Convert from a unity vector3 to a float[3] used in the DLL. </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static SenseGloveCs.Kinematics.Vect3D ToPosition(Vector3 pos)
    {
        return new SenseGloveCs.Kinematics.Vect3D( pos.x, pos.z, pos.y );
    }

    /// <summary>
    /// Convert an array of unity positions back into an array used by the DLL
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static SenseGloveCs.Kinematics.Vect3D[] ToPosition(Vector3[] pos)
    {
        SenseGloveCs.Kinematics.Vect3D[] res = new SenseGloveCs.Kinematics.Vect3D[pos.Length];
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
    public static Quaternion ToUnityQuaternion(SenseGloveCs.Kinematics.Quat quat)
    {
        return new Quaternion(-quat.x, -quat.z, -quat.y, quat.w);
    }

    /// <summary> Convert a unity Quaternion into a float[4] used in the DLL. </summary>
    /// <param name="Q"></param>
    /// <returns></returns>
    public static SenseGloveCs.Kinematics.Quat ToQuaternion(Quaternion Q)
    {
        return new SenseGloveCs.Kinematics.Quat ( -Q.x, -Q.z, -Q.y, Q.w );
    }

    



    /// <summary>
    /// Convert a unity eulerAngles notation into one used by the DLL.
    /// </summary>
    /// <param name="euler"></param>
    /// <returns></returns>
    public static SenseGloveCs.Kinematics.Vect3D ToEuler(Vector3 euler)
    {
        return SenseGloveCs.Values.Radians(new SenseGloveCs.Kinematics.Vect3D ( -euler.x, -euler.z, -euler.y ));
    }

    /// <summary> Convert a set of euler angles from the DLL into the Unity notation. </summary>
    /// <param name="euler"></param>
    /// <returns></returns>
    public static Vector3 ToUnityEuler(SenseGloveCs.Kinematics.Vect3D euler)
    {
        euler = SenseGloveCs.Values.Degrees(euler);
        return new Vector3(-euler.x, -euler.z, -euler.y);
    }

    #endregion Conversion

    //-------------------------------------------------------------------------------------------------------------------------
    // Misc 

    /// <summary> Normalize an angle (in degrees) such that it is within the -180...180 range. </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static float NormalizeAngle(float angle)
    {
        float N = angle % 360; //convert angle to a value between 0...359
        //Convert it to a -180 ... 180 notation
        if (N <= -180)
        {
            N += 360;
        }
        else if (N > 180)
        {
            N -= 360;
        }
        return N;
    }

    /// <summary> Normalize a set of (euler) angles to fall within a -180... 180 range. </summary>
    /// <param name="angles"></param>
    /// <returns></returns>
    public static Vector3 NormalizeAngles(Vector3 angles)
    {
        return new Vector3
        (
            SenseGlove_Util.NormalizeAngle(angles.x),
            SenseGlove_Util.NormalizeAngle(angles.y),
            SenseGlove_Util.NormalizeAngle(angles.z)
        );
    }


    /// <summary> Calculate the angular velocity of a GameObject, using its current rotation and that of the previous frame. </summary>
    /// <param name="currentRot"></param>
    /// <param name="previousRot"></param>
    /// <remarks>Placed here because it may be used by other scripts as well.</remarks>
    /// <returns></returns>
    public static Vector3 CalculateAngularVelocity(Quaternion currentRot, Quaternion previousRot)
    {
        Quaternion dQ = currentRot * Quaternion.Inverse(previousRot);
        Vector3 dE = dQ.eulerAngles;
        Vector3 res = new Vector3
        (
            SenseGlove_Util.NormalizeAngle(dE.x),
            SenseGlove_Util.NormalizeAngle(dE.y),
            SenseGlove_Util.NormalizeAngle(dE.z)
        );
        return (res * Mathf.Deg2Rad) / Time.deltaTime; //convert from deg to rad / sec
    }



    /// <summary> Returns a unit vector representing the chosen movement axis. </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static Vector3 GetAxis(MovementAxis axis)
    {
        Vector3 res = Vector3.zero;
        res[(int)axis] = 1;
        return res;
    }

    public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return SenseGloveCs.Values.Interpolate(value, inMin, inMax, outMin, outMax);
    }




    public static bool IsNegative(MoveAxis axis)
    {
        return axis >= MoveAxis.NegativeX;
    }

    public static int AxisIndex(MoveAxis axis)
    {
        return IsNegative(axis) ? (int)axis - 3 : (int)axis;
    }

    public static Vector3 GetVector(MoveAxis axis)
    {
        Vector3 res = Vector3.zero;
        if (IsNegative(axis))
            res[(int)axis - 3] = -1;
        else
            res[(int)axis] = 1;
        return res;
    }



    public static int ListIndex(MonoBehaviour obj, List<MonoBehaviour> objects) //List<> needs standardization
    {
        return SenseGlove_Util.ListIndex(obj.gameObject, objects);
    }

    public static int ListIndex(GameObject obj, List<MonoBehaviour> objects) //List<> needs standardization
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (GameObject.ReferenceEquals(obj, objects[i].gameObject))
                return i;
        }
        return -1;
    }


}



