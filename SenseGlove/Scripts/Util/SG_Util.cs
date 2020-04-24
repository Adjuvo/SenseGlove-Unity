using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SG
{

    [Serializable]
    public class SGEvent : UnityEvent { }

    /// <summary> Contains methods that make the SenseGloveCs library work with Unity. </summary>
    public static class SG_Util
    {
        public enum MoveAxis
        {
            X = 0, Y, Z, NegativeX, NegativeY, NegativeZ
        }

        public static bool keyBindsEnabled = true;

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
            for (int i = 0; i < V.Length; i++)
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
                    res[f] = SG_Util.ToUnityPosition(pos[f]);
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
            return new SenseGloveCs.Kinematics.Vect3D(pos.x, pos.z, pos.y);
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
                res[f] = SG_Util.ToPosition(pos[f]);
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
            return new SenseGloveCs.Kinematics.Quat(-Q.x, -Q.z, -Q.y, Q.w);
        }





        /// <summary>
        /// Convert a unity eulerAngles notation into one used by the DLL.
        /// </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static SenseGloveCs.Kinematics.Vect3D ToEuler(Vector3 euler)
        {
            return SenseGloveCs.Values.Radians(new SenseGloveCs.Kinematics.Vect3D(-euler.x, -euler.z, -euler.y));
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
        // Values 

        #region Values

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

        /// <summary> Normalize an angle (in degrees) such that it is within the -180...180 range. </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static float NormalizeAngle(float angle, float minAngle, float maxAngle)
        {
            float N = angle % 360; //convert angle to a value between 0...359
                                   //Convert it to a -180 ... 180 notation
            if (N <= minAngle)
            {
                N += 360;
            }
            else if (N > maxAngle)
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
                SG_Util.NormalizeAngle(angles.x),
                SG_Util.NormalizeAngle(angles.y),
                SG_Util.NormalizeAngle(angles.z)
            );
        }

        /// <summary> Map a value from one range to another. </summary>
        /// <param name="value"></param>
        /// <param name="inMin"></param>
        /// <param name="inMax"></param>
        /// <param name="outMin"></param>
        /// <param name="outMax"></param>
        /// <returns></returns>
        public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return SenseGloveCs.Values.Interpolate(value, inMin, inMax, outMin, outMax);
        }

        /// <summary> Calculates the average between a list of Vector3 values </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Vector3 Average(List<Vector3> values)
        {
            if (values.Count > 0)
            {
                if (values.Count > 1)
                {
                    Vector3 sum = new Vector3(0, 0, 0);
                    for (int i = 0; i < values.Count; i++)
                    {
                        sum += values[i];
                    }
                    return sum / values.Count;
                }
                return values[0];
            }
            return Vector3.zero;
        }


        /// <summary> Calculates the average between a list of integer values </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static int Average(int[] values)
        {
            if (values.Length > 0)
            {
                if (values.Length > 1)
                {
                    int sum = 0;
                    for (int i = 0; i < values.Length; i++)
                    {
                        sum += values[i];
                    }
                    return sum / values.Length;
                }
                return values[0];
            }
            return 0;
        }

        #endregion

        //-------------------------------------------------------------------------------------------------------------------------
        // Velocities / Transforms 

        #region Transforms

        /// <summary> Calculate the angular velocity of a GameObject, using its current rotation and that of the previous frame. </summary>
        /// <param name="currentRot"></param>
        /// <param name="previousRot"></param>
        /// <remarks>Placed here because it may be used by other scripts as well.</remarks>
        /// <returns></returns>
        public static Vector3 CalculateAngularVelocity(Quaternion currentRot, Quaternion previousRot, float deltaTime)
        {
            Quaternion dQ = currentRot * Quaternion.Inverse(previousRot);
            Vector3 dE = dQ.eulerAngles;
            Vector3 res = new Vector3
            (
                SG_Util.NormalizeAngle(dE.x),
                SG_Util.NormalizeAngle(dE.y),
                SG_Util.NormalizeAngle(dE.z)
            );
            return (res * Mathf.Deg2Rad) / deltaTime; //convert from deg to rad / sec
        }

        /// <summary> Calculate a position and rotation difference between two transforms. </summary>
        /// <param name="obj"></param>
        /// <param name="reference"></param>
        /// <param name="posOffset"></param>
        /// <param name="rotOffset"></param>
        public static void CalculateOffsets(Transform obj, Transform reference, out Vector3 posOffset, out Quaternion rotOffset)
        {
            rotOffset = Quaternion.Inverse(reference.rotation) * obj.rotation;
            posOffset = Quaternion.Inverse(reference.rotation) * (obj.position - reference.position);
        }


        /// <summary> Add a velocity / angularVelocity to a rigidbody to move towards a targetPosition and rotation  </summary>
        /// <param name="obj"></param>
        /// <param name="targetPosition"></param>
        /// <param name="targetRotation"></param>
        /// <param name="rotationSpeed"></param>
        public static void TransformRigidBody(ref Rigidbody obj, Vector3 targetPosition, Quaternion targetRotation, float rotationSpeed)
        {
            Quaternion qTo = Quaternion.Slerp(obj.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // rotation
            obj.transform.rotation = qTo;

            //translation
            Vector3 dPos = (targetPosition - obj.transform.position);
            float velocity = dPos.magnitude / Time.fixedDeltaTime;
            obj.velocity = dPos.normalized * velocity;
        }

        /// <summary> Add a rigidbody to a GameObject if one does not exist yet and apply the desired parameters. </summary>
        /// <param name="obj"></param>
        /// <param name="useGrav"></param>
        /// <param name="isKinematic"></param>
        /// <returns></returns>
        public static Rigidbody TryAddRB(GameObject obj, bool useGrav = false, bool isKinematic = false)
        {
            Rigidbody objBody = obj.GetComponent<Rigidbody>();
            if (objBody == null)
            {
                objBody = obj.AddComponent<Rigidbody>();
            }
            if (objBody != null)
            {
                objBody.useGravity = useGrav;
                objBody.isKinematic = isKinematic;
            }
            return objBody;
        }

        /// <summary> Remove the rigidbody from a gameObject, if one exists. </summary>
        /// <param name="obj"></param>
        public static void TryRemoveRB(GameObject obj)
        {
            Rigidbody objBody = obj.GetComponent<Rigidbody>();
            if (objBody != null)
            {
                GameObject.Destroy(objBody);
            }
        }

        #endregion

        //-------------------------------------------------------------------------------------------------------------------------
        // Linked Scripts 

        #region LinkedScripts

        /// <summary> Check if an object has a SG_HandModelInfo component and assign it to the info parameter. </summary>
        /// <param name="obj"></param>
        /// <param name="info"></param>
        public static void CheckForHandInfo(Transform obj, ref SG_HandModelInfo info)
        {
            if (info == null) { info = obj.gameObject.GetComponent<SG_HandModelInfo>(); } //check selft
            if (info == null && obj.parent != null) //check children
            {
                info = obj.parent.GetComponentInChildren<SG_HandModelInfo>();
            }
        }

        /// <summary> Try to get a SG_TrackedHand that this script is attached to. </summary>
        /// <param name="obj"></param>
        /// <param name="handScript"></param>
        public static SG_TrackedHand CheckForTrackedHand(Transform obj)
        {
            SG_TrackedHand handScript = obj.gameObject.GetComponent<SG_TrackedHand>();
            if (handScript == null)
            {
                handScript = obj.GetComponentInParent<SG_TrackedHand>();
            }
            return handScript;
        }

        /// <summary> Set all the children of the following Transform to active/inactive </summary>
        /// <param name="obj"></param>
        /// <param name="active"></param>
        public static void SetChildren(Transform obj, bool active)
        {
            for (int i = 0; i < obj.childCount; i++)
            {
                obj.GetChild(i).gameObject.SetActive(active);
            }
        }

        #endregion

        //-------------------------------------------------------------------------------------------------------------------------
        // Misc 

        #region Misc

        public static string SenseGloveDir
        {
            get { return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/SenseGlove/"; }
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

        /// <summary> Returns true if this axis is negative </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static bool IsNegative(MoveAxis axis)
        {
            return axis >= MoveAxis.NegativeX;
        }

        /// <summary> Returns an index (0, 1, 2) to access a Vector3 </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static int AxisIndex(MoveAxis axis)
        {
            return IsNegative(axis) ? (int)axis - 3 : (int)axis;
        }

        /// <summary> Returns a normalized Vector repesenting this axis in 3D space. </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static Vector3 GetVector(MoveAxis axis)
        {
            Vector3 res = Vector3.zero;
            if (IsNegative(axis))
                res[(int)axis - 3] = -1;
            else
                res[(int)axis] = 1;
            return res;
        }

        /// <summary> Spawn a sphere and make it a child of parent. </summary>
        /// <param name="worldDiameter"></param>
        /// <param name="parent"></param>
        /// <param name="withCollider"></param>
        /// <returns></returns>
        public static GameObject SpawnSphere(float worldDiameter, Transform parent, bool withCollider = true)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(worldDiameter, worldDiameter, worldDiameter);

            if (!withCollider)
            {
                Collider coll = sphere.GetComponent<Collider>();
                if (coll != null) { GameObject.Destroy(coll); }
            }

            sphere.transform.parent = parent;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localPosition = Vector3.zero;

            return sphere;
        }


        /// <summary> Append texts to existing button text (used to add hotkey info to buttons) </summary>
        /// <param name="button"></param>
        /// <param name="addedText"></param>
        public static void AppendButtonText(UnityEngine.UI.Button button, string addedText)
        {
            if (button != null)
            {
                UnityEngine.UI.Text btnTxt = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (btnTxt != null) { btnTxt.text = btnTxt.text + addedText; }
            }
        }

        #endregion

    }

}