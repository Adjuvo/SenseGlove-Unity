using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> This script collects the Force Feedback from the hand and sends these to its connected Hardware. </summary>
    public class SG_HandFeedback : MonoBehaviour
    {
        /// <summary> The hardware that this script will send its Force-Feedback commands to </summary>
        [Header("Linked Scripts")]
        public SG_SenseGloveHardware connectedGlove;
        /// <summary> Information about the 3D model this script is connected to. Used to set up tracking for the fingers/wrist. </summary>
        public SG_HandModelInfo handModel;

        /// <summary> Impact script for the wrist, should be linked to this connectedGlove. </summary>
        [Header("Feedback Components")]
        public SG_BasicFeedback wristFeedbackScript;
        /// <summary> Feedback colliders on each of the fingers, sorted from thumb to pinky. </summary>
        public SG_FingerFeedback[] fingerFeedbackScripts;



        /// <summary> The TrackedHand this FeedbackScript takes its data from, used to access other components like grabscript, hardware, etc. </summary>
        public SG_TrackedHand Hand
        {
            get; protected set;
        }

        /// <summary> Returns true if this FeedbackScript is connected to Hardware that is ready to go </summary>
        public bool HardwareReady
        {
            get { return this.connectedGlove != null && this.connectedGlove.GloveReady; }
        }

        /// <summary> Returns true if this FeedbackScript is connected to Sense Glove Hardware and returns a link to it. Used in an if statement for safety </summary>
        /// <param name="hardware"></param>
        /// <returns></returns>
        public bool GetHardware(out SG_SenseGloveHardware hardware)
        {
            if (HardwareReady)
            {
                hardware = connectedGlove;
                return hardware != null;
            }
            hardware = null;
            return false;
        }


        /// <summary> Used to show/hide the feedback colliders of this hand. </summary>
        public bool DebugEnabled
        {
            set
            {
                wristFeedbackScript.DebugEnabled = value;
                for (int f = 0; f < fingerFeedbackScripts.Length; f++)
                {
                    fingerFeedbackScripts[f].DebugEnabled = value;
                }
            }
        }


        /// <summary> Returns true if at least one collider is touching a material. </summary>
        /// <returns></returns>
        public bool TouchingMaterial()
        {
            for (int f = 0; f < this.fingerFeedbackScripts.Length; f++)
            {
                if (this.fingerFeedbackScripts[f].IsTouching()) { return true; }
            }
            return false;
        }

        /// <summary> returns the distance (in m) of the fingers inside a SG_Material collider, provided they are touching one. </summary>
        public float[] ColliderDistances
        {
            get
            {
                float[] res = new float[this.fingerFeedbackScripts.Length];
                for (int f = 0; f < fingerFeedbackScripts.Length; f++)
                {
                    res[f] = fingerFeedbackScripts[f].DistanceInCollider;
                }
                return res;
            }
        }

        /// <summary> Set ignoreCollision between this layer and another set of rigidbodies. </summary>
        /// <param name="otherLayer"></param>
        /// <param name="ignoreCollision"></param>
        public void SetIgnoreCollision(SG_HandRigidBodies otherLayer, bool ignoreCollision)
        {
            if (otherLayer != null)
            {
                GameObject wrist = otherLayer.wristObj != null ? otherLayer.wristObj.gameObject : null;
                SetIgnoreCollision(wrist, ignoreCollision);
                for (int f = 0; f < otherLayer.fingerObjs.Length; f++)
                {
                    SetIgnoreCollision(otherLayer.fingerObjs[f].gameObject, ignoreCollision);
                }
            }
        }


        public void SetIgnoreCollision(GameObject obj, bool ignoreCollision)
        {
            Collider[] colliders = obj != null ? obj.GetComponents<Collider>() : new Collider[0];
            for (int i = 0; i < colliders.Length; i++)
            {
                this.SetIgnoreCollision(colliders[i], ignoreCollision);
            }
        }

        public void SetIgnoreCollision(Collider col, bool ignoreCollision)
        {
            if (this.wristFeedbackScript != null) { wristFeedbackScript.SetIgnoreCollision(col, ignoreCollision); }
            for (int f = 0; f < this.fingerFeedbackScripts.Length; f++)
            {
                this.fingerFeedbackScripts[f].SetIgnoreCollision(col, ignoreCollision);
            }
        }


        /// <summary> Sets up this script's components to link to the same glove and the appropriate hand section. </summary>
        public void SetupScripts()
        {
            if (fingerFeedbackScripts.Length < 5)
            {
                Debug.LogWarning(this.name + " has only " + fingerFeedbackScripts.Length + "/5 finger scripts connected," +
                    " and will not provide feedback to all fingers.");
            }
            for (int f = 0; f < fingerFeedbackScripts.Length && f < 5; f++)
            {
                fingerFeedbackScripts[f].linkedGlove = this.connectedGlove;
                fingerFeedbackScripts[f].handLocation = (SG_HandSection)f;
                if (handModel != null)
                {
                    Transform target;
                    if (handModel.GetFingerTip(fingerFeedbackScripts[f].handLocation, out target))
                    {
                        fingerFeedbackScripts[f].SetTrackingTarget(target, true);
                    }
                }
            }
            if (wristFeedbackScript != null)
            {
                wristFeedbackScript.linkedGlove = this.connectedGlove;
                wristFeedbackScript.handLocation = SG_HandSection.Wrist;
                if (handModel != null)
                {
                    wristFeedbackScript.SetTrackingTarget(handModel.wristTransform, true);
                }
            }
        }


        /// <summary> Retrieve the forces for each finger and send these to the glove. </summary>
        public void UpdateForces()
        {
            if (connectedGlove != null)
            {
                int[] forceLevels = new int[5];
                for (int f = 0; f < forceLevels.Length; f++)
                {
                    if (fingerFeedbackScripts.Length > f)
                    {
                        forceLevels[f] = fingerFeedbackScripts[f].ForceLevel;
                    }
                }
                connectedGlove.SendBrakeCmd(forceLevels);
            }
        }

        /// <summary> Checks for scripts that might be connected to this GameObject. Used in editor and during startup. </summary>
        public virtual void CheckForScripts()
        {
            if (connectedGlove == null)
            {
                connectedGlove = this.gameObject.GetComponent<SG_SenseGloveHardware>();
                if (connectedGlove == null && this.transform.parent != null) //still nothing
                {
                    connectedGlove = this.transform.parent.GetComponent<SG_SenseGloveHardware>();
                }
            }
            SG_Util.CheckForHandInfo(this.transform, ref this.handModel);
        }




        void Awake()
        {
            SetupScripts();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateForces();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            this.Hand = null;
            CheckForScripts();
        }
#endif

    }

}