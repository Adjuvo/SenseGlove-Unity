using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SG.Util
{
    /// <summary> A basic Unity Event that is re-used for simple UNity calls </summary>
    [Serializable] public class SGEvent : UnityEvent { }


    /// <summary> Contains methods we use in verious locations to make our life easier int Unity. </summary>
    public static class SG_Util
    {
        /// <summary> 2*Pi </summary>
        public static readonly float PI_2 = Mathf.PI * 2;

        //--------------------------------------------------------------------------------------------------------------------
        // Translations / Rotations around an axis

        /// <summary> An enumerator to have someone choose movement axes. Used in certain interactables. </summary>
        public enum MoveAxis
        {
            X = 0, Y, Z, NegativeX, NegativeY, NegativeZ
        }



        /// <summary> Returns a unit vector representing the chosen movement axis. </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static Vector3 GetAxis(MoveAxis axis)
        {
            Vector3 res = Vector3.zero;
            int ax = (int)axis;
            if (ax > 2) { ax -= 3; }
            res[ax] = 1;
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
            return SGCore.Kinematics.Values.Map(value, inMin, inMax, outMin, outMax);
        }

        public static Vector3 Map(float x, float x0, float x1, Vector3 y0, Vector3 y1)
        {
            return new Vector3
            (
                SG.Util.SG_Util.Map(x, x0, x1, y0.x, y1.x),
                SG.Util.SG_Util.Map(x, x0, x1, y0.y, y1.y),
                SG.Util.SG_Util.Map(x, x0, x1, y0.z, y1.z)
            );
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

        /// <summary> Generate a sine signal </summary>
        /// <param name="frequency"></param>
        /// <param name="amplitude"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        public static float GetSine(float frequency, float amplitude, float atTime)
        {
            // a normal sine wave completes a full period in 2Pi / s.
            // y = a * sin(b * x), where x is 0..2Pi (mod).
            float b = PI_2 * frequency;
            return amplitude * Mathf.Sin(b * atTime);
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


        public static Vector3 CalculateTargetPosition(Transform refrence, Vector3 posOffset, Quaternion rotOffset)
        {
            return refrence != null ? refrence.position + (refrence.rotation * posOffset) : Vector3.zero;
        }

        public static Quaternion CalculateTargetRotation(Transform refrence, Quaternion rotOffset)
        {
            return refrence != null ? refrence.transform.rotation * rotOffset : Quaternion.identity;
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

        /// <summary> Set the text of an existing button text </summary>
        /// <param name="button"></param>
        /// <param name="addedText"></param>
        public static void SetButtonText(UnityEngine.UI.Button button, string text)
        {
            if (button != null)
            {
                UnityEngine.UI.Text btnTxt = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (btnTxt != null) { btnTxt.text = text; }
            }
        }

        /// <summary> Set button text, and optionally add a hotkey indicator on the next line. </summary>
        /// <param name="button"></param>
        /// <param name="baseTxt"></param>
        /// <param name="keyCode"></param>
        public static void SetButtonText(UnityEngine.UI.Button button, string baseTxt, KeyCode keyCode)
        {
            if (keyCode != KeyCode.None) { SG.Util.SG_Util.SetButtonText(button, baseTxt + "\r\n[" + keyCode + "]"); }
            else { SG.Util.SG_Util.SetButtonText(button, baseTxt); }
        }

        #endregion


        /// <summary> get an absulute position relative to a transform. </summary>
        /// <param name="objPosWorld"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Vector3 CaluclateLocalPos(Vector3 objPosWorld, Vector3 refPosWorld, Quaternion refRotWorld)
        {
            Vector3 diff = objPosWorld - refPosWorld;
            //now we have the absolute differemce, and should rotate it to match
            return Quaternion.Inverse(refRotWorld) * diff;
        }

        public static Vector3 CalculateAbsWithOffset(Vector3 objWorldPos, Quaternion objWorldRot, Vector3 worldOffsets)
        {
            Vector3 dWorld = objWorldRot * worldOffsets;
            return objWorldPos + dWorld;
        }

        /// <summary> Get an absolure position's coordinates relative to another transform (without scaling) </summary>
        /// <param name="absPos"></param>
        /// <returns></returns>
        public static Vector3 ProjectOnTranform(Vector3 absPos, Transform relativeTo)
        {
            return Quaternion.Inverse(relativeTo.rotation) * (absPos - relativeTo.position);
        }

        /// <summary> Project an absolute position onto the plane formed by the Hing Joint's normal. </summary>
        /// <returns></returns>
        public static Vector3 ProjectOnTranform2D(Vector3 absPos, Transform relativeTo, MovementAxis normal)
        {
            Vector3 res = ProjectOnTranform(absPos, relativeTo);
            res[(int)normal] = 0;
            return res;
        }
        
    }

}