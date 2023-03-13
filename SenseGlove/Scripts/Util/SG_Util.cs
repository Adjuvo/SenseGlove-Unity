using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace SG
{
    /// <summary> Promises the return of a single value between 0 .. 1 that can be used to drive other scripts.  </summary>
    public interface IOutputs01Value
    {
        float Get01Value();
    }

    /// <summary> Has something that can be controller by setting a value of 0...1. This can be anything from animation to transparency to timing-related variables. </summary>
    public interface IControlledBy01Value
    {
        void SetControlValue(float value01);
    }
}

namespace SG.Util
{
    /// <summary> Stats for a RigidBody that we can pass around </summary>
    public class RigidBodyStats
    {
        /// <summary> Whether or not this RigidBody was Kinematic when we created these stats </summary>
        public bool wasKinematic;
        /// <summary> Whether or not this RigidBody used GHravity when we created these stats </summary>
        public bool usedGravity;
        /// <summary> Constraints when last we checked this RigidBody </summary>
        public RigidbodyConstraints rbConstraints;

        /// <summary> Create a new RigidBody stats from a RigidBody </summary>
        /// <param name="RB"></param>
        public RigidBodyStats(Rigidbody RB)
        {
            wasKinematic = RB.isKinematic;
            usedGravity = RB.useGravity;
            rbConstraints = RB.constraints;
        }

        /// <summary> Create a new RigidBodyStats by manually setting parameters. </summary>
        /// <param name="useGravity"></param>
        /// <param name="isKinematic"></param>
        /// <param name="constraints"></param>
        public RigidBodyStats(bool useGravity, bool isKinematic, RigidbodyConstraints constraints)
        {
            wasKinematic = isKinematic;
            usedGravity = useGravity;
            rbConstraints = constraints;
        }

        /// <summary> Report the constraints for logging. </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "useGravity = " + usedGravity + ", isKinematic = " + wasKinematic;
        }
    }

    /// <summary> How to translate a RigidBody </summary>
    public enum TranslateMode
    {
        /// <summary> Do nothing </summary>
        Off,
        /// <summary> rigidBody.position = targetPosition </summary>
        SetPosition,
        /// <summary> sets rigidBody.velocity so that it can reach targetPosition 1 frame's time. </summary>
        OldVelocity,
        /// <summary> Unity's built-in MovePosition(targetPosition) </summary>
        OfficialMovePos,
        /// <summary> Unity's built-in MovePosition(target) where target is clamped by our movementSpeed. </summary>
        CustomMovePos,
        /// <summary> Sets rigidBody.velocity towards the target, but clamp it to my speed. MovePosition(target, speed) in later versions. </summary>
        ImprovedVelocity,
    }

    /// <summary> How to rotate a RigidBody </summary>
    public enum RotateMode
    {
        /// <summary> Do Nothing </summary>
        Off,
        /// <summary> rigidBody.rotation = targetRotation </summary>
        SetRotation,
        /// <summary> SLERPs the rotation from current to targetRotation using my speed, then sets Rotation </summary>
        OldSLerp,
        /// <summary> Unity's Built-In MoveRotation(target), but target is limted by the rotation speed.  </summary>
        OfficialMoveRotation,
    }

    /// <summary> Directory to store / load files </summary>
    public enum StorageDir
    {
        /// <summary> In player: .exe folder. In Editor - Assets </summary>
        WorkingDir,
        /// <summary> MyDocuments </summary>
        MyDocuments,
        /// <summary> Where most SG data ends up. MyDocuments/SenseGlove on Win </summary>
        SGCommon,
        /// <summary> On Android: Application folder. On Windows: AppData/LocalLow/Company/ProjectName/ </summary>
        AppData,
        /// <summary> The true Executable directory. In Inspector, this is the uni.exe? </summary>
        ExeDir,
    }


    /// <summary> A basic Unity Event that is re-used for simple Unity calls </summary>
    [Serializable] public class SGEvent : UnityEvent { }

    /// <summary> An enumerator to have someone choose movement axes. Used in certain interactables. </summary>
    public enum MoveAxis
    {
        X = 0, Y, Z, NegativeX, NegativeY, NegativeZ
    }




    /// <summary> Contains methods we use in verious locations to make our life easier int Unity. </summary>
    public static class SG_Util
    {
        /// <summary> Returns true if the application is quitting, becuase Unity doesn't support this in a meaningful way. </summary>
        public static bool IsQuitting
        {
            get { return _quitting; }
            set { _quitting = value; }
        }

        private static bool _quitting = false;

        /// <summary> 2*Pi </summary>
        public static readonly float PI_2 = Mathf.PI * 2;

        //--------------------------------------------------------------------------------------------------------------------
        // Translations / Rotations around an axis

        #region MovementAxes




        /// <summary> Returns a unit vector representing the chosen movement axis. </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static Vector3 GetAxis(MoveAxis axis, bool abs = false)
        {
            Vector3 res = Vector3.zero;
            if (IsNegative(axis))
            {
                res[(int)axis - 3] = abs ? 1 : -1;
            }
            else
            {
                res[(int)axis] = 1;
            }
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

        #endregion MovementAxes

        //--------------------------------------------------------------------------------------------------------------------
        // ToString Methods

        #region ToString

        /// <summary> Create a string representation of a floating point value, guarenteed to have the amount of deicmals. (e.g. "0" -> "0.00" </summary>
        /// <param name="val"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public static string UniLengthStr(float val, int decimals)
        {
            string res = System.Math.Round(val, decimals).ToString();
            int expLength = decimals + 2; //value dot decimals
            if (res.Length < expLength)
            {
                if (res.Length < 2) //only 0
                {
                    res += "."; //value + .
                }
                for (int i = 2; i < expLength; i++)
                {
                    if (res.Length <= i)
                    {
                        res += "0"; //add more zeroes
                    }
                }
            }
            return res;
        }


        public static string ToString(float val, int decimals = -1)
        {
            if (decimals > -1)
            {
                return System.Math.Round(val, decimals).ToString();
            }
            return val.ToString();
        }

        /// <summary> Convert a Unity Vector3 to a string with a greater precision that it default method. </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static string ToString(Vector3 V, int decimals = -1)
        {
            return "[" + ToString(V.x, decimals) + ", " + ToString(V.y, decimals) + ", " + ToString(V.z, decimals) + "]";
        }

        /// <summary> Convert a Unity Quaternion to a string with a greater precision that it default method.  </summary>
        /// <param name="Q"></param>
        /// <returns></returns>
        public static string ToString(Quaternion Q, int decimals = -1)
        {
            return "[" + ToString(Q.x, decimals) + ", " + ToString(Q.y, decimals) + ", " + ToString(Q.z, decimals) + ", " + ToString(Q.w, decimals) + "]";
        }

        /// <summary> Convert a float[] to a string with a greater precision that it default Unity(?) method. </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static string ToString(float[] V, int decimals = -1)
        {
            string res = "[";
            for (int i = 0; i < V.Length; i++)
            {
                res += ToString(V[i], decimals);
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

        public static string PrintArray<T>(T[] array, string delim = ", ")
        {
            string res = "";
            for (int i = 0; i < array.Length; i++)
            {
                res += array[i].ToString();
                if (i < array.Length - 1)
                {
                    res += delim;
                }
            }
            return res;
        }

        public static string PrintArray<T>(T[][] array, string colDelim = ", ", string rowDelim = "\n")
        {
            string res = "";
            for (int i = 0; i < array.Length; i++)
            {
                res += PrintArray(array[i], colDelim);
                if (i < array.Length - 1)
                {
                    res += rowDelim;
                }
            }
            return res;
        }


        public static string PrintArray<T>(List<T> array, string delim = ", ")
        {
            string res = "";
            for (int i = 0; i < array.Count; i++)
            {
                res += array[i].ToString();
                if (i < array.Count - 1)
                {
                    res += delim;
                }
            }
            return res;
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

        public static float Map(float value, float inMin, float inMax, float outMin, float outMax, bool clampOutput = false)
        {
            return SGCore.Kinematics.Values.Map(value, inMin, inMax, outMin, outMax, clampOutput);
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
                    return Mathf.RoundToInt(sum / (float)values.Length);
                }
                return values[0];
            }
            return 0;
        }

        public static float Average(float[] values)
        {
            if (values.Length > 0)
            {
                if (values.Length > 1)
                {
                    float sum = 0;
                    for (int i = 0; i < values.Length; i++)
                    {
                        sum += values[i];
                    }
                    return sum / (float)values.Length;
                }
                return values[0];
            }
            return 0.0f;
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

        public static void CalculateTargetLocation(Transform refrence, Vector3 posOffset, Quaternion rotOffset, out Vector3 targetPos, out Quaternion targetRot)
        {
            //this.lastWristRotation = SG.Util.SG_Util.CalculateTargetRotation(this.trackedObject, this.customRotOffset);
            //this.lastWristPosition = SG.Util.SG_Util.CalculateTargetPosition(this.trackedObject, this.customPosOffset, this.customRotOffset);
            targetRot = refrence.rotation * rotOffset;
            targetPos = refrence.position + (refrence.rotation * posOffset);
        }

        public static void CalculateTargetLocation(Vector3 refrPos, Quaternion refrRot, Vector3 posOffset, Quaternion rotOffset, out Vector3 targetPos, out Quaternion targetRot)
        {
            //this.lastWristRotation = SG.Util.SG_Util.CalculateTargetRotation(this.trackedObject, this.customRotOffset);
            //this.lastWristPosition = SG.Util.SG_Util.CalculateTargetPosition(this.trackedObject, this.customPosOffset, this.customRotOffset);
            targetRot = refrRot * rotOffset;
            targetPos = refrPos + (refrRot * posOffset);
        }


        /// <summary> Add a velocity / angularVelocity to a rigidbody to move towards a targetPosition and rotation  </summary>
        /// <param name="obj"></param>
        /// <param name="targetPosition"></param>
        /// <param name="targetRotation"></param>
        /// <param name="rotationSpeed"></param>
        public static void TransformRigidBody(ref Rigidbody obj, Vector3 targetPosition, Quaternion targetRotation, float rotationSpeed, float deltaTime)
        {
            Quaternion qTo = Quaternion.Slerp(obj.transform.rotation, targetRotation, deltaTime * rotationSpeed);

            // rotation
            obj.transform.rotation = qTo;

            //translation
            Vector3 dPos = (targetPosition - obj.transform.position);
            float velocity = dPos.magnitude / deltaTime;
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

        ///// <summary> Try to get a SG_TrackedHand that this script is attached to. </summary>
        ///// <param name="obj"></param>
        ///// <param name="handScript"></param>
        //public static SG_TrackedHand CheckForTrackedHand(Transform obj)
        //{
        //    SG_TrackedHand handScript = obj.gameObject.GetComponent<SG_TrackedHand>();
        //    if (handScript == null)
        //    {
        //        handScript = obj.GetComponentInParent<SG_TrackedHand>();
        //    }
        //    return handScript;
        //}

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

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Particle Systems

        /// <summary> Starts all ParticleSystems in a list </summary>
        /// <param name="systems"></param>
        public static void StartAll(ParticleSystem[] systems)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Play();
            }
        }

        /// <summary> Stops all ParticleSystems in a list </summary>
        /// <param name="systems"></param>
        public static void StopAll(ParticleSystem[] systems)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop();
            }
        }

        /// <summary> Adds a FixedJoint to an object if it isn't there already </summary>
        /// <param name="addTo"></param>
        /// <param name="breakForce"></param>
        public static FixedJoint TryAddFixedJoint(GameObject addTo, float breakForce = float.PositiveInfinity, float breakTorque = float.PositiveInfinity)
        {
            FixedJoint joint = addTo.GetComponent<FixedJoint>();
            if (joint == null)
            {
                joint = addTo.AddComponent<FixedJoint>();
            }
            joint.breakForce = breakForce;
            joint.breakTorque = breakTorque;
            return joint;
        }

        #endregion


        //------------------------------------------------------------------------------------------------------------------------
        // Offsets, and Local Positions

        #region LocationFunctions

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
        public static Vector3 ProjectOnTransform(Vector3 absPos, Transform relativeTo)
        {
            return ProjectOnTransform(absPos, relativeTo.position, relativeTo.rotation);
        }

        public static Vector3 ProjectOnTransform(Vector3 absPos, Vector3 refWorldPosition, Quaternion refWorldRotation)
        {
            return Quaternion.Inverse(refWorldRotation) * (absPos - refWorldPosition);
        }


        /// <summary> Project an absolute position onto the plane formed by the Hing Joint's normal. </summary>
        /// <returns></returns>
        public static Vector3 ProjectOnTransform2D(Vector3 absPos, Transform relativeTo, MoveAxis normal)
        {
            Vector3 res = ProjectOnTransform(absPos, relativeTo);
            res[AxisIndex(normal)] = 0;
            return res;
        }

        /// <summary> Calculates the position of a parent (that is in a set rotation) such that its child is at a specific location - but not rotation </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        /// <param name="targetLocation"></param>
        public static Vector3 CalculateParentPosition(Transform parent, Transform child, Vector3 childTarget)
        {
            // dWorldPos = childPositon - parentPosition
            // res = childTarget - dWorldPose
            // ==> res = childTarget - childPosition + parentPosition;
            return childTarget - child.position + parent.position;
        }



        /// <summary> Given the location of a Child object and offsets from it to the parent, calculate the parent location.  </summary>
        /// <param name="parentPos"></param>
        /// <param name="parentRot"></param>
        public static void CalculateRefrenceLocation(Vector3 targetPos, Quaternion targetRot, Vector3 offset_refToTarget_pos, Quaternion offset_refToTarget_rot, out Vector3 refPos, out Quaternion refRot)
        {
            // the iverse of : 
            // targetRot = refrRot * rotOffset;
            // targetPos = refrPos + (refrRot * posOffset);
            refRot = targetRot * Quaternion.Inverse(offset_refToTarget_rot);
            refPos = targetPos - (refRot * offset_refToTarget_pos);

            //grabRefRot = myRot * Quaternion.Inverse(refToMe_rot);
            //grabRefPos = myPos - (grabRefRot * refToMe_pos);
        }

        /// <summary> Given a Start- and end Location, interpolate based on a time between 0 .. 1. </summary>
        /// <param name="t01"></param>
        /// <param name="startPosition"></param>
        /// <param name="startRotation"></param>
        /// <param name="endPosition"></param>
        /// <param name="endRotation"></param>
        /// <param name="currPosition"></param>
        /// <param name="currRotation"></param>
        public static void LerpLocation(float t01, Vector3 startPosition, Quaternion startRotation, Vector3 endPosition, Quaternion endRotation, out Vector3 currPosition, out Quaternion currRotation)
        {
            currPosition = Vector3.Lerp(startPosition, endPosition, t01);
            currRotation = Quaternion.Slerp(startRotation, endRotation, t01);
        }



        /// <summary> Calculates local or world space location at this moment. = </summary>
        /// <param name="interactable"></param>
        /// <param name="worldSpace"></param>
        /// <param name="basePosition"></param>
        /// <param name="baseRotation"></param>
        public static void CalculateBaseLocation(Transform objToCalculate, out Vector3 basePosition, out Quaternion baseRotation)
        {
            if (objToCalculate.parent == null) //we're in world space
            {
                basePosition = objToCalculate.position;
                baseRotation = objToCalculate.rotation;
            }
            else //we're in local space
            {
                CalculateOffsets(objToCalculate, objToCalculate.parent, out basePosition, out baseRotation);
            }
        }

        /// <summary> Calculates where the base position and rotation would be now - uses the object's current parent. </summary>
        /// <param name="objToCalculate"></param>
        /// <param name="basePosition_original"></param>
        /// <param name="baseRotation_original"></param>
        /// <param name="basePosition_current"></param>
        /// <param name="baseRotation_current"></param>
        public static void GetCurrentBaseLocation(Transform objToCalculate, Vector3 basePosition_original, Quaternion baseRotation_original, out Vector3 basePosition_current, out Quaternion baseRotation_current)
        {
            GetCurrentBaseLocation(objToCalculate, objToCalculate.parent, basePosition_original, baseRotation_original, out basePosition_current, out baseRotation_current);
        }

        /// <summary> Calculates where the base position and rotation would be now - uses the object's original parent. </summary>
        /// <param name="objToCalculate"></param>
        /// <param name="originalParent"></param>
        /// <param name="basePosition_original"></param>
        /// <param name="baseRotation_original"></param>
        /// <param name="basePosition_current"></param>
        /// <param name="baseRotation_current"></param>
        public static void GetCurrentBaseLocation(Transform objToCalculate, Transform originalParent, Vector3 basePosition_original, Quaternion baseRotation_original, out Vector3 basePosition_current, out Quaternion baseRotation_current)
        {
            if (originalParent == null)
            {
                basePosition_current = basePosition_original;
                baseRotation_current = baseRotation_original;
            }
            else
            {
                SG.Util.SG_Util.CalculateTargetLocation(originalParent, basePosition_original, baseRotation_original, out basePosition_current, out baseRotation_current);
            }
        }

        #endregion LocationFunctions


        //----------------------------------------------------------------------------------------------------------------------------------------
        // Array Manipulations

        #region ArrayManipulations

        /// <summary> Copy an array of whatever. Only deep copies [n] if T is a struct or base variable (char, int, bool, float, etc)  </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <returns></returns>
        public static T[] ArrayCopy<T>(T[] original)
        {
            T[] res = new T[original.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = original[i];
            }
            return res;
        }

        /// <summary> Copy a 2D araay of whatever, Only deep copies [n]m[] if T is a struct or base variable (char, int, bool, float, etc)  </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <returns></returns>
        public static T[][] ArrayCopy<T>(T[][] original)
        {
            T[][] res = new T[original.Length][];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = ArrayCopy(original[i]);
            }
            return res;
        }

        /// <summary> Copy a list of whatever. Only deep copies if T is a struct or base variable (char, int, bool, float, etc)  </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <returns></returns>
        public static List<T> ArrayCopy<T>(List<T> original)
        {
            List<T> res = new List<T>(original.Count);
            for (int i = 0; i < original.Count; i++)
            {
                res.Add(original[i]);
            }
            return res;
        }

        /// <summary> Add obj to list, but only it if does not exist there yet. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool SafelyAdd<T>(T obj, List<T> list)
        {
            if (obj != null && !list.Contains(obj))
            {
                list.Add(obj);
                return true;
            }
            return false;
        }

        /// <summary> Remove obj from List if it exists. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool SafelyRemove<T>(T obj, List<T> list) where T : Component
        {
            int index = ListIndex(list, obj);
            if (index > -1)
            {
                list.RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary> Print the contents of a list into a string with specific delimiters. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string PrintContents<T>(List<T> list, string delim = ", ")
        {
            string res = "";
            for (int i = 0; i < list.Count; i++)
            {
                res += list[i].ToString();
                if (i < list.Count - 1)
                {
                    res += delim;
                }
            }
            return res;
        }

        /// <summary> Print a 1D Array to a string with a specific delimiter. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        public static string PrintContents<T>(T[] list, string delim = ", ")
        {
            string res = "";
            for (int i = 0; i < list.Length; i++)
            {
                res += list[i].ToString();
                if (i < list.Length - 1)
                {
                    res += delim;
                }
            }
            return res;
        }


        //public static T[] Copy<T>(T[] original)
        //{
        //    T[] res = new T[original.Length];
        //    for (int i = 0; i < original.Length; i++)
        //    {
        //        res[i] = original[i];
        //    }
        //    return res;
        //}

        //public static List<T> Copy<T>(List<T> original)
        //{
        //    List<T> res = new List<T>(original.Count);
        //    for (int i = 0; i < original.Count; i++)
        //    {
        //        res.Add(original[i]);
        //    }
        //    return res;
        //}

        /// <summary> Remove ay duplicate entries (refences) from list. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T> FilterDuplicates<T>(List<T> list)
        {
            List<T> filtered = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                SafelyAdd(list[i], filtered); //safelyAdd ignores duplicates and null
            }
            return filtered;
        }


        /// <summary> Returns true if there is at lease one true value in this array of booleans. </summary>
        /// <param name="bools"></param>
        /// <returns></returns>
        public static bool OneTrue(bool[] bools)
        {
            for (int i = 0; i < bools.Length; i++)
            {
                if (bools[i]) { return true; }
            }
            return false;
        }

        /// <summary> Returns the index of objectToFind inside the list, or -1 if it does not exist. </summary>
        /// <param name="list"></param>
        /// <param name="objectToFind"></param>
        /// <param name="startIndex">Optional index</param>
        /// <returns></returns>
        public static int ListIndex<T>(List<T> list, T objectToFind, int startIndex = 0) where T : Component
        {
            for (int i = startIndex; i < list.Count; i++)
            {
                if (objectToFind == list[i]) { return i; }
            }
            return -1;
        }

        public static int ArrayIndex<T>(T[] list, T objectToFind, int startIndex = 0) where T : Component
        {
            for (int i = startIndex; i < list.Length; i++)
            {
                if (objectToFind == list[i]) { return i; }
            }
            return -1;
        }


        /// <summary> Check for overlap in lists A and B; it will return all elements that occur both in A and in B, all that occur in A and not B, all that occur in B and not A </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <param name="overLap"></param>
        /// <param name="AnotB"></param>
        /// <param name="bNotA"></param>
        public static bool OverlapArrays<T>(T[] listA, T[] listB, List<T> AandB, List<T> AnotB, List<T> bNotA) where T : MonoBehaviour
        {
            throw new System.NotImplementedException();
        }

        #endregion ArrayManipulations


        //------------------------------------------------------------------------------------------------------------------------
        // Physics Collision - What is colliding NOW

        #region CheckCollision

        /// <summary> Check which colliders are currently colliding with col. - includes Col, as it doesn't use the collider itself, only its transform / parameters. </summary>
        /// <param name="col"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static Collider[] CurrentlyCollidingWith(Collider col, int layerMask = Physics.AllLayers, bool hitTriggers = false) //col should be a HandBone... A primitive...?
        {
            if (col is BoxCollider)
            {
                return CurrentlyColliding((BoxCollider)col, layerMask, hitTriggers);
            }
            else if (col is SphereCollider)
            {
                return CurrentlyColliding((SphereCollider)col, layerMask, hitTriggers);
            }
            else if (col is CapsuleCollider)
            {
                return CurrentlyColliding((CapsuleCollider)col, layerMask, hitTriggers);
            }
            else if (col is MeshCollider)
            {
                throw new System.NotImplementedException("This feature is not available for MeshColliders. They are far too complex...");
            }
            return new Collider[0];
        }


        /// <summary> Check which colliders are currently colliding with CapsuleCollider col.  </summary>
        /// <param name="col"></param>
        /// <param name="layerMask"></param>
        /// <param name="hitTriggers"></param>
        /// <remarks> https://roundwide.com/physics-overlap-capsule/ was used to determine wher p0 and p1 are meant to be. </remarks>
        /// <returns></returns>
        public static Collider[] CurrentlyColliding(CapsuleCollider col, int layerMask = Physics.AllLayers, bool hitTriggers = false) //TODO: Should be on col's layermask(?)
        {
            Transform local = col.transform;
            Vector3 scale = local.lossyScale;

            float worldHeight = col.height * scale[col.direction]; //we can calculate this first. The worldHeight determines the total height...
            scale[col.direction] = 0; //now set it to zero so this one doesn't count. 
            float worldRadius = Mathf.Max(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)), Mathf.Abs(scale.z)) * col.radius;
            if (worldHeight <= worldRadius * 2)
            {
                //At this point, Unity treats the Capsule as a Cirlce.
                Vector3 worldCenter = local.TransformPoint(col.center);
                return Physics.OverlapSphere(worldCenter, worldRadius, layerMask, (hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore));
            }
            float hStep = (col.height / 2) - col.radius; //this is how far up & down we go (locally)
            Vector3 upDir = Vector3.zero; //define a local up direction.
            upDir[col.direction] = hStep; //direction 0 = x, 1 = y, 2 = z.

            Vector3 p0 = local.TransformPoint(col.center + (upDir));
            Vector3 p1 = local.TransformPoint(col.center - (upDir));
            //Debug.Log("Capsule: World Radus = " + worldRadius + ", world height = " + worldHeight + ". WorldSteppie = " + (worldHeight - (worldRadius * 2)) 
            //    + "\n=> [" + ToString(p0) + ", " + ToString(p1) );
            return Physics.OverlapCapsule(p0, p1, worldRadius, layerMask, (hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore));
        }

        /// <summary> Check which colliders are currently colliding with SphereCollider col. </summary>
        /// <param name="col"></param>
        /// <param name="layerMask"></param>
        /// <param name="hitTriggers"></param>
        /// <returns></returns>
        public static Collider[] CurrentlyColliding(SphereCollider col, int layerMask = Physics.AllLayers, bool hitTriggers = false)
        {
            Transform local = col.transform;
            Vector3 worldCenter = local.TransformPoint(col.center);

            //the sphere collider radius adapts to the biggest local scale
            Vector3 scale = local.lossyScale;
            float worldRadius = Mathf.Max(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)), Mathf.Abs(scale.z)) * col.radius;
            //Debug.Log("Checking overlap with " + col.name + ", Radius " + worldRadius + ", center at " + ToString(worldCenter));
            return Physics.OverlapSphere(worldCenter, worldRadius, layerMask, (hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore));
        }

        /// <summary> Check which colliders are currently colliding with BoxCollider col. </summary>
        /// <param name="col"></param>
        /// <param name="layerMask"></param>
        /// <param name="hitTriggers"></param>
        /// <returns></returns>
        public static Collider[] CurrentlyColliding(BoxCollider col, int layerMask = Physics.AllLayers, bool hitTriggers = false)
        {
            Transform local = col.transform;
            throw new System.NotImplementedException("This feature is not yet available for BoxColliders");
        }

        #endregion CheckCollision

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Adding / Removing Components

        #region AddRemoveComponents

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // HandPosers


        /// <summary> Spawns basic HandPoser with Debug Components </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="debugColor"></param>
        /// <returns></returns>
        public static SG_HandPoser3D SpawnHandPoser_WithDebugs(string name, Transform parent, Color debugColor, bool debugEnabled = false)
        {
            GameObject newObj = new GameObject(name);
            newObj.transform.parent = parent;
            SG_HandPoser3D res = newObj.AddComponent<SG_HandPoser3D>();
            res.LinesColor = debugColor;
            res.LinesEnabled = debugEnabled;
            return res;
        }


        /// <summary> Tries to add a HandPoser to component. If it does not exist, set its value. Otherwise, ensure it has the same properties </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="debugEnabled"></param>
        /// <param name="debugColor"></param>
        public static void TryAddHandPoser(ref SG_HandPoser3D component, string name, Transform parent, Color debugColor, bool debugEnabled = false)
        {
            // Link Real Life Hand Tracking
            if (component == null)
            {
                component = SG_Util.SpawnHandPoser_WithDebugs(name, parent, debugColor, debugEnabled);
            }
            else
            {
                component.gameObject.name = name;
                component.transform.parent = parent;
                component.LinesColor = debugColor;
                component.LinesEnabled = debugEnabled;
            }
            component.SetupTransforms(); //ensure it's setup and ready
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // LineRenderer


        /// <summary> MIGHT CREATE A AMEMORY LEAK SO DESTROY THE SHADER? </summary>
        /// <param name="addTo"></param>
        /// <param name="positions"></param>
        /// <param name="lineColor"></param>
        /// <param name="worldSpace"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static LineRenderer AddDebugRenderer(GameObject addTo, int positions, Color lineColor, string objName = "LineRenderer", bool worldSpace = true, float width = 0.005f)
        {
            GameObject newObj = new GameObject(objName);
            newObj.transform.parent = addTo.transform;
            LineRenderer res = newObj.AddComponent<LineRenderer>();
            res.positionCount = positions;
            res.useWorldSpace = worldSpace;
            res.startWidth = width;
            res.endWidth = width;

            //Shaders added programatically are annoying...
            Shader unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader != null)
            {
                res.material = new Material(unlitShader);
                res.material.color = lineColor;
            }

            res.receiveShadows = false;
            res.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            res.startColor = lineColor;
            res.endColor = lineColor;
            res.numCapVertices = 5;
            return res;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Any Component

        /// <summary> Attempt to add a component to a GameObject. If it already exists, don't to anything. Either way, return a reference to the new Component </summary>
        /// <param name="componentToAdd"></param>
        /// <returns></returns>
        public static T TryAddComponent<T>(GameObject obj) where T : Component
        {
            T attached = obj.GetComponent<T>();
            if (attached == null)
            {
                attached = obj.AddComponent<T>();
            }
            return attached;
        }

        /// <summary> Attempt to add a component to a GameObject. If it already exists, don't to anything. Returns true if the refrence was first created. </summary>
        /// <param name="componentToAdd"></param>
        /// <returns></returns>
        public static bool TryAddComponent<T>(GameObject obj, out T attached) where T : Component
        {
            attached = obj.GetComponent<T>();
            if (attached == null)
            {
                attached = obj.AddComponent<T>();
                return true;
            }
            return false;
        }

        /// <summary> Attempt to remove a Component of a specific type, if it exists. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void TryRemoveComponent<T>(GameObject obj) where T : Component
        {
            T firstComponent = obj.GetComponent<T>();
            if (firstComponent != null)
            {
                GameObject.Destroy(firstComponent);
            }
        }

        /// <summary> Attempot to remove all components of a specific type, if it exists. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        public static void TryRemoveComponents<T>(GameObject obj) where T : Component
        {
            T[] components = obj.GetComponents<T>();
            for (int i = 0; i < components.Length; i++)
            {
                GameObject.Destroy(components[i]);
            }
        }

        /// <summary> Collect all components of a specific type from a component refrence, and safely add them to a List </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <param name="addTo"></param>
        /// <returns></returns>
        public static bool CollectComponent<T>(Component component, ref List<T> addTo) where T : Component
        {
            if (component != null)
            {
                return CollectComponent(component.gameObject, ref addTo);
            }
            return false;
        }

        /// <summary> Collect all components of a specific type from a gameobject refrence, and safely add them to a List </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectFrom"></param>
        /// <param name="addTo"></param>
        /// <returns></returns>
        public static bool CollectComponent<T>(GameObject collectFrom, ref List<T> addTo) where T : Component
        {
            T comp = collectFrom.GetComponent<T>();
            return SafelyAdd(comp, addTo);
        }

        /// <summary> Collect the GameObject from a script, and safely add it to a list. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool CollectGameObject<T>(T script, ref List<GameObject> list) where T : Component
        {
            if (script != null)
            {
                return SafelyAdd(script.gameObject, list);
            }
            return false;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Detection

        /// <summary> Collect a specific script from a collider. If it doesn't exist, check its connected rigidbody instead. </summary>
        /// <param name="col"></param>
        /// <param name="interactable"></param>
        /// <param name="favourSpecific"></param>
        /// <returns></returns>
        public static bool GetScript<T>(Collider col, out T res, bool favourSpecific = true) where T : MonoBehaviour
        {
            T myScript = col.gameObject.GetComponent<T>();
            if (myScript != null && favourSpecific) //we favour the 'specific' script, which exists.
            {
                res = myScript;
                return true;
            }
            // we favour one of the connected rigidBody, so let's check it.
            T connectedScript = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject.GetComponent<T>() : null;
            if (connectedScript == null) { res = myScript; } //the connected body does not have a material, so regardless we'll try the specific one.
            else { res = connectedScript; }
            //Regardless of the outcome, if both are null, then we return false;
            return res != null;
        }



        /// <summary> Colliect Colliders from this object and all its children and add them to listToAdd. Returns the number of colliders that were added. </summary>
        /// <param name="obj"></param>
        /// <param name="addToList"></param>
        /// <returns></returns>
        public static int GetAllColliders(GameObject obj, ref List<Collider> listToAdd, bool ignoreTrigger = false)
        {
            int initialCount = listToAdd.Count;
            Collider[] myColliders = obj.GetComponents<Collider>();
            for (int i = 0; i < myColliders.Length; i++)
            {
                if ((!myColliders[i].isTrigger || !ignoreTrigger) && !listToAdd.Contains(myColliders[i]))
                {
                    listToAdd.Add(myColliders[i]);
                }
            }
            Collider[] myChildColliders = obj.GetComponentsInChildren<Collider>();
            for (int i = 0; i < myChildColliders.Length; i++)
            {
                if ((!myChildColliders[i].isTrigger || !ignoreTrigger) && !listToAdd.Contains(myChildColliders[i]))
                {
                    listToAdd.Add(myChildColliders[i]);
                }
            }
            return listToAdd.Count - initialCount;
        }


        #endregion AddRemoveComponents


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Rigidbody Manipulation

        #region MoveRigidBodies

        /// <summary> Move a RigidBody according to a set translation- and rotation method. Used so I can switch parameters live. </summary>
        /// <param name="rigidBody"></param>
        /// <param name="targetPosition"></param>
        /// <param name="targetRotation"></param>
        /// <param name="deltaTime"></param>
        /// <param name="RBTranslation"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="zeroVelocity"></param>
        /// <param name="RBRotation"></param>
        /// <param name="rotationSpeed"></param>
        /// <param name="zeroAngularVelocity"></param>
        public static void MoveRigidBody(Rigidbody rigidBody, Vector3 targetPosition, Quaternion targetRotation, float deltaTime,
            TranslateMode RBTranslation, float moveSpeed, bool zeroVelocity,
            RotateMode RBRotation, float rotationSpeed, bool zeroAngularVelocity
            )
        {
            RotateRigidBody(rigidBody, targetRotation, deltaTime, RBRotation, rotationSpeed, zeroAngularVelocity);
            TranslateRigidBody(rigidBody, targetPosition, deltaTime, RBTranslation, moveSpeed, zeroVelocity);
        }

        /// <summary> Translate a RigidBody to a target position using various movement parameters </summary>
        /// <param name="rigidBody"></param>
        /// <param name="targetPosition"></param>
        /// <param name="deltaTime"></param>
        /// <param name="RBTranslation"></param>
        /// <param name="moveSpeed"></param>
        /// <param name="zeroVelocity"></param>
        public static void TranslateRigidBody(Rigidbody rigidBody, Vector3 targetPosition, float deltaTime,
            TranslateMode RBTranslation, float moveSpeed, bool zeroVelocity)
        {

            // Then translate
            bool velocityBasedTranslate = RBTranslation == TranslateMode.OldVelocity || RBTranslation == TranslateMode.ImprovedVelocity;
            if (zeroVelocity && velocityBasedTranslate)
            {
                rigidBody.velocity = Vector3.zero;
            }
            Vector3 dPos = (targetPosition - rigidBody.position);
            // Vector3 dir = dPos.normalized;
            if (RBTranslation != TranslateMode.Off)
            {
                if (RBTranslation == TranslateMode.SetPosition)
                {
                    rigidBody.position = targetPosition;
                }
                else if (RBTranslation == TranslateMode.OldVelocity)
                {
                    float velocity = dPos.magnitude / deltaTime;
                    rigidBody.velocity = dPos.normalized * velocity;
                }
                else if (RBTranslation == TranslateMode.OfficialMovePos)
                {
                    //Store user input as a movement vector
                    Vector3 m_Input = dPos.normalized;
                    //Apply the movement vector to the current position, which is
                    //multiplied by deltaTime and speed for a smooth MovePosition
                    rigidBody.MovePosition(rigidBody.position + m_Input * deltaTime * moveSpeed);
                }
                else if (RBTranslation == TranslateMode.CustomMovePos)
                {
                    float maxMovementThisFrame = moveSpeed * deltaTime;
                    float pathFraction = maxMovementThisFrame / dPos.magnitude; //the amount of frames it would take me to get there.
                    Vector3 targetpos = Vector3.Lerp(rigidBody.position, targetPosition, pathFraction);
                    rigidBody.MovePosition(targetpos);
                }
                else if (RBTranslation == TranslateMode.ImprovedVelocity)
                {
                    float velocity = dPos.magnitude / deltaTime; //the speed I need to get there.
                    velocity = Mathf.Clamp(velocity, 0, moveSpeed); //clamped to my own max speed
                    rigidBody.velocity = dPos.normalized * velocity;
                }
            }
            if (zeroVelocity && velocityBasedTranslate)
            {
                rigidBody.velocity = Vector3.zero;
            }
        }

        /// <summary> Rotate a RigidBody to a targetRotation using various rotation parameters. </summary>
        /// <param name="rigidBody"></param>
        /// <param name="targetRotation"></param>
        /// <param name="deltaTime"></param>
        /// <param name="RBRotation"></param>
        /// <param name="rotationSpeed"></param>
        /// <param name="zeroAngularVelocity"></param>
        public static void RotateRigidBody(Rigidbody rigidBody, Quaternion targetRotation, float deltaTime,
           RotateMode RBRotation, float rotationSpeed, bool zeroAngularVelocity)
        {
            // Always Rotate the RigidBody first.
            bool velocityBasedRotate = RBRotation == RotateMode.OldSLerp;
            if (zeroAngularVelocity && velocityBasedRotate)
            {
                rigidBody.angularVelocity = Vector3.zero;
            }
            Quaternion dRot = targetRotation * Quaternion.Inverse(rigidBody.rotation);
            if (RBRotation != RotateMode.Off)
            {
                if (RBRotation == RotateMode.SetRotation)
                {
                    rigidBody.rotation = targetRotation;
                }
                else if (RBRotation == RotateMode.OldSLerp)
                {
                    Quaternion qTo = Quaternion.Slerp(rigidBody.rotation, targetRotation, deltaTime * rotationSpeed);
                    rigidBody.rotation = qTo;
                    rigidBody.angularVelocity = Vector3.zero;
                }
                else if (RBRotation == RotateMode.OfficialMoveRotation)
                {
                    Quaternion qTo = Quaternion.Slerp(rigidBody.rotation, targetRotation, deltaTime * rotationSpeed);
#if UNITY_2018_1_OR_NEWER
                    qTo = qTo.normalized;
#else
                    //Manual Normalization
                    float qL = Mathf.Sqrt((qTo.x * qTo.x) + (qTo.y * qTo.y) + (qTo.z * qTo.z) + (qTo.w * qTo.w));
                    if (qL != 0)
                    {
                        qTo = new Quaternion(qTo.x / qL, qTo.y / qL, qTo.z / qL, qTo.w / qL);
                    }
#endif
                    rigidBody.MoveRotation(qTo);
                }
            }
            if (zeroAngularVelocity && !velocityBasedRotate)
            {
                rigidBody.angularVelocity = Vector3.zero;
            }

        }


        /// <summary> Set the RigidBody parameters to a particular set of stats. </summary>
        /// <param name="physicsBody"></param>
        /// <param name="stats"></param>
        public static void SetRigidBody(Rigidbody physicsBody, RigidBodyStats stats)
        {
            if (physicsBody != null)
            {
                physicsBody.useGravity = stats.usedGravity;
                physicsBody.isKinematic = stats.wasKinematic;
            }
        }


        #endregion MoveRigidBodies


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Proximity Sorting

        #region ProximityDetection

        /// <summary> Returns the index of the Component in objectsToCheck whose GameObject is closest to source. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="objectsToCheck"></param>
        /// <returns></returns>
        public static int ClosestObjIndex<T>(Transform source, T[] objectsToCheck) where T : Component
        {
            if (objectsToCheck.Length > 0)
            {
                if (objectsToCheck.Length == 1) { return 0; } //it's an array of length 1. Just return the first index.
                float minDistSqrd = (objectsToCheck[0].transform.position - source.position).sqrMagnitude; //we use sqrd to avoid the sqrt function in distance.
                int res = 0;
                for (int i = 1; i < objectsToCheck.Length; i++)
                {
                    float sqrDist = (objectsToCheck[i].transform.position - source.position).sqrMagnitude;
                    if (sqrDist < minDistSqrd)
                    {
                        res = i;
                        minDistSqrd = sqrDist;
                    }
                }
                return res;
            }
            return -1;
        }


        /// <summary> Returns the object in a list that is closest to a source. Beware: Returns Null if list is empty. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="objectsToCheck"></param>
        /// <returns></returns>
        public static T GetClosestComponent<T>(Transform source, T[] objectsToCheck) where T : Component
        {
            int closestIndex = ClosestObjIndex(source, objectsToCheck);
            return closestIndex > -1 ? objectsToCheck[closestIndex] : null;
        }


        /// <summary> Returns an array of objects sorted by their distance to a reference position. T.transform is used. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="closeTo"></param>
        /// <param name="toSort"></param>
        /// <param name="copyAlways">If true, we always return a copy of an array, and not the original when L=0 or L=1. </param>
        /// <returns></returns>
        public static T[] SortByProximity<T>(Vector3 referencePos, T[] toSort) where T : Component
        {
            if (toSort.Length > 1) //there must be more than one object in there, otherwise it's no use.
            {
                List<T> sorted = new List<T>(toSort.Length);
                List<float> distances = new List<float>(toSort.Length);
                //first entry. You gotta start somewhere
                float sqrtDist = (toSort[0].transform.position - referencePos).sqrMagnitude;
                sorted.Add(toSort[0]);
                distances.Add(sqrtDist);
                //let's goo 
                for (int i = 1; i < toSort.Length; i++)
                {
                    sqrtDist = (toSort[i].transform.position - referencePos).sqrMagnitude;
                    if (sqrtDist < distances[0]) //we're closer than the closest one. add at the start
                    {
                        sorted.Insert(0, toSort[i]);
                        distances.Insert(0, sqrtDist);
                    }
                    else if (sqrtDist >= distances[distances.Count - 1]) //we're further than the furhest one one. Add at the end
                    {
                        sorted.Add(toSort[i]);
                        distances.Add(sqrtDist);
                    }
                    else //This is where the fun begins. We either go through each one, or do some sort of 'halfsies appoach. For now, I'm using the expesive operation, because you'll likely never hover over more than 12 objects at a time (famous last words)
                    {
                        bool sanityCheck = false;
                        for (int j = 0; j < distances.Count - 1; j++)
                        {
                            if (sqrtDist >= distances[j] && sqrtDist < distances[j + 1]) //we're further that j, but closer than the next one. So we should be a j+1.
                            {
                                sorted.Insert(j, toSort[i]);
                                distances.Insert(j, sqrtDist);
                                sanityCheck = true;
                                break; //no need to check
                            }
                        }
                        if (!sanityCheck)
                        {
                            Debug.LogError("Something's Not Quite Right");
                        }
                    }
                }
                return sorted.ToArray();
            }
            //If we get here there was no use sorting.
            if (toSort.Length == 1)
            {
                return new T[] { toSort[0] }; //ensure we give a new object, and not a refrence to the original
            }
            return new T[0]; //ensure we give a new object, and not a refrence to the original
        }

        #endregion ProximityDetection


        //------------------------------------------------------------------------------------------------------------------------------------------
        // Folders / IO

        /// <summary> Returns the SenseGlove directory on the system; "MyDocuments/SenseGlove/" </summary>
        public static string SenseGloveDir
        {
            get
            {
                return CombineDir(GetDirectory(StorageDir.MyDocuments), "SenseGlove\\");
            }
        }

        /// <summary> Returns a path to a commonly used directory </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static string GetDirectory(StorageDir directory)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			return Application.persistentDataPath + "\\";
#else
            switch (directory)
            {
                case StorageDir.MyDocuments:
                    return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                case StorageDir.AppData:
                    return Application.persistentDataPath + "\\";
                case StorageDir.SGCommon:
                    return SenseGloveDir;
                case StorageDir.ExeDir:
                    return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            }
            return "";
#endif
        }


        /// <summary> Ensure a directory ends in a "\", if it has any charaters. </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static string ValidateDir(string directory)
        {
            if (directory.Length > 0) //gotta check it's still there.
            {
                char last = directory[directory.Length - 1];
                if (last != '\\' && last != '/')
                {
                    directory += "\\"; //add one last subslash
                }
            }
            return directory;
        }

        /// <summary> Collect the path to a subfolder in a specific Directory  </summary>
        /// <param name="directory"></param>
        /// <param name="subDir"></param>
        /// <returns></returns>
        public static string GetDirectory(StorageDir directory, string subDir)
        {
            return CombineDir(GetDirectory(directory), subDir);
        }

        /// <summary> Combine two directories into a valid path. Either could be empty. Ensures no duplicate entries exist. </summary>
        /// <param name="startDir"></param>
        /// <param name="subDir"></param>
        /// <returns></returns>
        public static string CombineDir(string startDir, string subDir)
        {
            startDir = ValidateDir(startDir); //ensure start ends in "\"
            if (startDir.Length > 0 && subDir.Length > 0) //ensure the end one doesn't.
            {
                char first = subDir[0];
                if (first == '/' || first == '\\')
                {
                    subDir = subDir.Substring(1);
                }
            }
            return startDir + subDir;
        }

        public static void ShowDirectory(string itemPath)
        {
            itemPath = itemPath.Replace(@"/", @"\");   // explorer doesn't like front slashes
                                                       //System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);
            Application.OpenURL("file://" + itemPath);
        }

        public static void ShowFile(string itemPath)
        {
            itemPath = itemPath.Replace(@"/", @"\");   // explorer doesn't like front slashes
            System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);
        }


        public static string PrintGameObjName(Component[] array, string delim = ", ")
        {
            string res = "";
            for (int i = 0; i < array.Length; i++)
            {
                res += array[i].gameObject.name;
                if (i < array.Length - 1)
                {
                    res += delim;
                }
            }
            return res;
        }

        public static string PrintGameObjName(List<Component> array, string delim = ", ")
        {
            string res = "";
            for (int i = 0; i < array.Count; i++)
            {
                res += array[i].gameObject.name;
                if (i < array.Count - 1)
                {
                    res += delim;
                }
            }
            return res;
        }



        public static Vector3 MirrorPosition(Vector3 originalPos, Vector3 mirrorOrigin, Vector3 mirrorNormal)
        {
            Vector3 reflect = Vector3.Reflect(originalPos - mirrorOrigin, mirrorNormal);
            return reflect + mirrorOrigin;
        }




        public static Quaternion MirrorX(Quaternion Q)
        {
            return new Quaternion(-Q.x, Q.y, Q.z, -Q.w);
        }

        public static Quaternion MirrorY(Quaternion Q)
        {
            return new Quaternion(Q.x, -Q.y, Q.z, -Q.w);
        }

        public static Quaternion MirrorZ(Quaternion Q)
        {
            return new Quaternion(Q.x, Q.y, -Q.z, -Q.w);
        }


        public static Quaternion MirrorRotation(Quaternion Q, Quaternion mirrorOriginRotation, MoveAxis mirrorNormal)
        {
//            Quaternion mirrToAbs = mirrorOrigin.rotation;
            Quaternion localRot = Quaternion.Inverse(mirrorOriginRotation) * Q;
            Quaternion localMirror = Quaternion.identity;
            switch (mirrorNormal)
            {
                case MoveAxis.X:
                    localMirror = SG_Util.MirrorX(localRot);
                    break;
                case MoveAxis.Y:
                    localMirror = SG_Util.MirrorY(localRot);
                    break;
                case MoveAxis.Z:
                    localMirror = SG_Util.MirrorZ(localRot);
                    break;
            }
            return localMirror * mirrorOriginRotation;
        }




        public static void Gizmo_DrawWireCapsule(Vector3 start, Vector3 end, Color col, float radius)
        {
            Gizmo_DrawWireCapsule(start, end, radius, (end - start).magnitude, col);
        }


        public static void Gizmo_DrawWireCapsule(Vector3 _pos, Vector3 _pos2, float _radius, float _height, Color _color = default(Color))
        {
#if UNITY_EDITOR
            if (_color != default(Color)) UnityEditor.Handles.color = _color;

            var forward = _pos2 - _pos;
            var _rot = Quaternion.LookRotation(forward);
            var pointOffset = _radius / 2f;
            var length = forward.magnitude;
            var center2 = new Vector3(0f, 0, length);

            Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, UnityEditor.Handles.matrix.lossyScale);

            using (new UnityEditor.Handles.DrawingScope(angleMatrix))
            {
                UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.forward, _radius);
                UnityEditor.Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.left * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireArc(Vector3.zero, Vector3.left, Vector3.down * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireDisc(center2, Vector3.forward, _radius);
                UnityEditor.Handles.DrawWireArc(center2, Vector3.up, Vector3.right * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireArc(center2, Vector3.left, Vector3.up * pointOffset, -180f, _radius);

                Gizmo_DrawLine(_radius, 0f, length);
                Gizmo_DrawLine(-_radius, 0f, length);
                Gizmo_DrawLine(0f, _radius, length);
                Gizmo_DrawLine(0f, -_radius, length);
            }
#endif
        }

        private static void Gizmo_DrawLine(float arg1, float arg2, float forward)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.DrawLine(new Vector3(arg1, arg2, 0f), new Vector3(arg1, arg2, forward));
#endif
        }


        /// <summary> Detect an SG_TrackedHand within the scene. </summary>
        /// <param name="handSide"></param>
        /// <returns></returns>
        public static SG_TrackedHand FindHandInScene(HandSide handSide)
        {
            SG_TrackedHand[] hands = GameObject.FindObjectsOfType<SG_TrackedHand>();
            if (handSide == HandSide.AnyHand && hands.Length > 0)
            {
                return hands[0];
            }
            bool rightHand = handSide == HandSide.RightHand;
            for (int i=0; i<hands.Length; i++)
            {
                if (hands[i].TracksRightHand() == rightHand)
                {
                    return hands[i];
                }
            }
            return null;
        }



        public static Vector3 GetClosestPointOnLine(Vector3 point, Vector3 line_start, Vector3 line_end)
        {
            return line_start + Vector3.Project(point - line_start, line_end - line_start);
        }



    }

}