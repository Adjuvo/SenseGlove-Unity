using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
	/// <summary> When enabled, this script collects Force-Feedback information from the fingers, and sends them to a HapticGlove </summary>
	public class SG_HandFeedback : SG_HandComponent
	{
        /// <summary> The Hardware this Feebdack Layer will send its commands to. </summary>
        public IHandFeedbackDevice hapticHardware;

        /// <summary> Feedback colliders on each of the fingers, sorted from thumb to pinky. </summary>
        public SG_FingerFeedback[] fingerFeedbackScripts;

        protected bool updateSelf = true;

        /// <summary> TODO: Automatically Detect this based on device </summary>
        public bool useTraditional = true;

        /// <summary> TODO: Automatically Detect this based on device </summary>
        public bool useThresholds = true;

        /// <summary> If true, we've yet to warn someone about the projection layer... </summary>
        protected bool warnProjection = true;
        protected bool warnThresholds = true;

        protected override void CreateComponents()
        {
            if (fingerFeedbackScripts.Length < 5)
            {
                Debug.LogWarning(this.name + " has only " + fingerFeedbackScripts.Length + "/5 finger scripts connected," +
                    " and will not provide feedback to all fingers.");
            }
            for (int f = 0; f < fingerFeedbackScripts.Length && f < 5; f++)
            {
                //fingerFeedbackScripts[f].feedbackScript = this;
                fingerFeedbackScripts[f].finger = (SGCore.Finger)f;
            }
        }

        public override SG_HandFeedback FeedbackLayer
        {
            get { return this; }
        }

        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            //Debug.Log("Linking " + this.name + " to " + newHand.name);
            base.LinkToHand_Internal(newHand, firstLink);
            hapticHardware = newHand;

            SG_HandPoser3D poser = newHand.GetPoser(SG_TrackedHand.TrackingLevel.VirtualPose);
            for (int f = 0; f < fingerFeedbackScripts.Length && f < 5; f++)
            {
                //Ensure my colliders have RigidBodies.
                Rigidbody fbBody = SG.Util.SG_Util.TryAddComponent<Rigidbody>(fingerFeedbackScripts[f].gameObject);
                fbBody.isKinematic = true;
                //link by childing
                HandJoint linkTo = SG_HandPoser3D.ToHandJoint((SGCore.Finger)f, 3);
                poser.ParentObject(fingerFeedbackScripts[f].transform, linkTo); //to ensure these do not lag behind, we're childing them.
                fingerFeedbackScripts[f].updateTime = SG_SimpleTracking.UpdateDuring.Off; //we cant shut down the entire script, otherwise there'd be no FFB. Instead, we stop the tracking part.

            }
            this.updateSelf = false; //the TrackedHand now determines the orde rof operations.
        }

        /// <summary> Collect all colliders from this Script </summary>
        /// <returns></returns>
        protected override List<Collider> CollectPhysicsColliders()
        {
            List<Collider> myColliders = new List<Collider>();
            for (int f=0; f<this.fingerFeedbackScripts.Length; f++)
            {
                SG.Util.SG_Util.GetAllColliders(this.fingerFeedbackScripts[f].gameObject, ref myColliders);
            }
            return myColliders;
        }


        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers); //also creates the lists.
            for (int f = 0; f < this.fingerFeedbackScripts.Length; f++)
            {
                SG.Util.SG_Util.CollectComponent(this.fingerFeedbackScripts[f].gameObject, ref renderers); //deleteable renderer of the GameObject
                Util.SG_Util.CollectGameObject(this.fingerFeedbackScripts[f].debugTextElement, ref objects); //we want to be able to turn off the test completely.
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> returns the distance (in m) of the fingers inside a SG_Material collider, provided they are touching one. </summary>
        public virtual float[] ColliderDistances
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

        /// <summary> Returns MeshDeform scripts touched by each finger. Elements can be NULL! </summary>
        public virtual SG_MeshDeform[] TouchedDeformScripts
        {
            get
            {
                SG_MeshDeform[] res = new SG_MeshDeform[this.fingerFeedbackScripts.Length];
                for (int f = 0; f < fingerFeedbackScripts.Length; f++)
                {
                    res[f] = fingerFeedbackScripts[f].TouchedDeformScript;
                }
                return res;
            }
        }

        /// <summary> Returns true for each finger that is touching a deformation script and that hasn't passed full deformation distance yet. </summary>
        public virtual bool[] DeformingMesh
        {
            get
            {
                bool[] res = new bool[this.fingerFeedbackScripts.Length];
                for (int f = 0; f < fingerFeedbackScripts.Length; f++)
                {
                    res[f] = fingerFeedbackScripts[f].TouchedDeformScript != null ? fingerFeedbackScripts[f].DistanceInCollider < fingerFeedbackScripts[f].TouchedDeformScript.maxDisplacement : false;
                }
                return res;
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Returns true if at least one collider is touching a material. </summary>
        /// <returns></returns>
        public virtual bool TouchingMaterial()
        {
            for (int f = 0; f < this.fingerFeedbackScripts.Length; f++)
            {
                if (this.fingerFeedbackScripts[f].IsTouching()) { return true; }
            }
            return false;
        }

        public virtual void UpdateColliders()
        {
            for (int f=0; f<this.fingerFeedbackScripts.Length; f++)
            {
                this.fingerFeedbackScripts[f].UpdateLocation();
            }
        }


        /// <summary> Retrieve the forces for each finger and send these to the glove. </summary>
        public virtual void UpdateForces()
        {
            if (this.isActiveAndEnabled && this.hapticHardware != null && this.hapticHardware.IsConnected()) //don't send FFB when disconnected.
            {
                if (useTraditional)
                {
                    int[] forceLevels = new int[5];
                    for (int f = 0; f < forceLevels.Length; f++)
                    {
                        if (fingerFeedbackScripts.Length > f)
                        {
                            forceLevels[f] = fingerFeedbackScripts[f].ForceLevel;
                        }
                    }
                    hapticHardware.SendCmd(new SGCore.Haptics.SG_FFBCmd(forceLevels));
                }

                if ( this.useThresholds ) //Our Device supports locking the fingers at a specific flexion!
                {
                    if (hapticHardware.FlexionLockSupported())
                    {
                        if (ProjectionLayer == null || !ProjectionLayer.isActiveAndEnabled)
                        {
                            if (warnProjection)
                            {
                                warnProjection = false;
                                Debug.LogWarning("Your device Supports Flexion Locking, but your projection layer ");
                            }
                        }
                        else
                        {
                            this.ProjectionLayer.GetTouchInformation(out bool[] isTouching, out float[] touchFlexion, true); //we ignore soft materials for now...
                            this.hapticHardware.SetFlexionLocks(isTouching, touchFlexion);
                        }
                    }
                    else if (warnProjection)
                    {
                        warnProjection = false;
                        Debug.Log( this.hapticHardware.Name() + " does not support Threshold Commands. Disabling this feature." );
                    }
                }

            }
        }

       



        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Update is called once per frame
        protected virtual void Update()
        {
            if (updateSelf)
            {
                UpdateColliders();
            }
            UpdateForces();
        }


        protected virtual void OnDisable()
        {
            if (this.hapticHardware != null)
            {
                this.hapticHardware.StopHaptics();
            }
        }

    }

}