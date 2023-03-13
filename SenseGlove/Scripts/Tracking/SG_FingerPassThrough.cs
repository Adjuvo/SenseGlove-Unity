using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG
{
	/// <summary> A layer that prevents fingers from flexing through non-trigger colliders. </summary>
	public class SG_FingerPassThrough : SG_HandComponent
	{
        //------------------------------------------------------------------------------------------------------------
        // Member variables

        /// <summary> All colliders that  </summary>
        public SG_PassThroughCollider[] fingerColliders = new SG_PassThroughCollider[0];


        protected bool[] locked = new bool[5];
        protected float[][] lockedFlexions; 
        protected float[] lockedTotalFlex; //used to evaluate if we still need to lock
        protected float[] lockedNormals;

        protected SG_HandPose constrainedPose = null;

        //------------------------------------------------------------------------------------------------------------
        // HandComponent Functions

        ///// <summary> Access the Passthrough Layer linked to this layer's TrackedHand </summary>
        //public override SG_FingerPassThrough PassthroughLayer
        //{
        //    get { return this; }
        //}

        /// <summary> Collect all physics colliders from this Script </summary>
        /// <returns></returns>
        protected override List<Collider> CollectPhysicsColliders()
        {
            List<Collider> myColliders = new List<Collider>();
            for (int f = 0; f < this.fingerColliders.Length; f++)
            {
                SG.Util.SG_Util.GetAllColliders(this.fingerColliders[f].gameObject, ref myColliders);
            }
            return myColliders;
        }

        /// <summary> Collect al debug components </summary>
        /// <param name="objects"></param>
        /// <param name="renderers"></param>
        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers);
            for (int i = 0; i < this.fingerColliders.Length; i++)
            {
                Util.SG_Util.CollectComponent(this.fingerColliders[i], ref renderers);
                Util.SG_Util.CollectGameObject(this.fingerColliders[i].debugTextElement, ref objects);
            }
        }


        /// <summary> Create and set up all required variables </summary>
        protected override void CreateComponents()
        {
            base.CreateComponents();
            //make a 5x3 array to contain the flexions of the finger when a lock is detected.
            lockedFlexions = new float[5][];
            for (int f=0; f<lockedFlexions.Length; f++)
            {
                lockedFlexions[f] = new float[3];
            }
            lockedTotalFlex = new float[5];
            lockedNormals = new float[5];
        }


        /// <summary> Link this layer to a new SG_TrackedHand </summary>
        /// <param name="newHand"></param>
        /// <param name="firstLink"></param>
        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            base.LinkToHand_Internal(newHand, firstLink);
            //Link finger colliders to the collider poser.
            SG_HandPoser3D colliderPoser = newHand.GetPoser(SG_TrackedHand.TrackingLevel.VirtualPose);  //might as well. We're mainly using it for the offsets.
            for (int f = 0; f < this.fingerColliders.Length; f++)
            {
                colliderPoser.ParentObject(this.fingerColliders[f].transform, fingerColliders[f].linkMeTo); //make these children as opposed to follwing them, so they're no longer a frame behind.
                this.fingerColliders[f].updateTime = SG_SimpleTracking.UpdateDuring.Off; //I still need the script to validate the hand
            }
        }


        //------------------------------------------------------------------------------------------------------------
        // FingerPassThrough Functions

        /// <summary> Returns the latest constrained pose as determined by this PassThrough Layer. </summary>
        public SG_HandPose LatestPose
        {
            get { return this.constrainedPose; }
        }


        /// <summary> In one move, collect the individual and total flexions of a hand pose, as Unity (degrees, lhs). </summary>
        /// <param name="pose"></param>
        /// <param name="flexions"></param>
        /// <param name="totalFlexions"></param>
        public static void CollectFlexions(SG_HandPose pose, out float[][] flexions, out float[] totalFlexions)
        {
            flexions = new float[pose.jointAngles.Length][];
            totalFlexions = new float[pose.jointAngles.Length];
            for (int f=0; f<flexions.Length; f++)
            {
                flexions[f] = new float[pose.jointAngles[f].Length];
                for (int j=0; j<flexions[f].Length; j++)
                {
                    float flex = pose.jointAngles[f][j].z; //flexion is Z-axis in Unity here.
                    totalFlexions[f] += flex;
                    flexions[f][j] = flex; 
                }
            }
        }

        /// <summary> Updates the Constrained pose based on this layer's PassThroughColliders, and the 'real' hand tracking. Get the newConstrainedPose to do things with. </summary>
        /// <param name="colliderPose"></param>
        /// <param name="handDimensions"></param>
        /// <param name="newConstrainedPose"></param>
        public virtual void UpdateConstrainedPose(SG_HandPose colliderPose, SGCore.Kinematics.BasicHandModel handDimensions, out SG_HandPose newConstrainedPose)
        {
            UpdateConstrainedPose(colliderPose, handDimensions);
            newConstrainedPose = this.constrainedPose;
        }

        /// <summary> Updates the LastPose of this layer to have the wrist position of the colliderPose, but use the 'angles at lock-time' for the fingers that shouldn't passthrough. </summary>
        /// <param name="colliderPose"></param>
        /// <param name="handDimensions"></param>
        public virtual void UpdateConstrainedPose(SG_HandPose colliderPose, SGCore.Kinematics.BasicHandModel handDimensions)
        {
            float[][] currentFlex; float[] currentTotals;
            CollectFlexions(colliderPose, out currentFlex, out currentTotals);


            bool[] deformingMesh = new bool[5] { false, false, false, false, false }; //if we're still deforming a mesh and are therefore allowed to move.x
            if (this.TrackedHand != null && this.FeedbackLayer != null)
            {
                deformingMesh = this.FeedbackLayer.DeformingMesh;
            }

            for (int i = 0; i < this.fingerColliders.Length; i++)
            {
                int finger = (int)this.fingerColliders[i].locksFinger;
                bool shouldLock = fingerColliders[i].HoveredCount > 0 && !deformingMesh[finger];
                if (!locked[finger] && shouldLock) //we are currently not locked, but we are touching something!
                {
                    locked[finger] = true;
                    lockedFlexions[finger] = currentFlex[finger];
                    lockedTotalFlex[finger] = currentTotals[finger];
                    lockedNormals[finger] = colliderPose.normalizedFlexion[finger];

                    //ToDO: recalculate normals
                    //Debug.Log("Flexion for " + finger + " locked at " + Util.SG_Util.ToString(lockedFlexions[finger]) + " (" + lockedTotalFlex[finger] + ")");
                }
                locked[finger] = shouldLock;
            }

            Vector3[][] handAngles = new Vector3[5][];

            //these next 3 will either be re-calculated, or just assigned by refrence (no calculation required).
            Quaternion[][] jointRotations = new Quaternion[5][];
            Vector3[][] jointPositions = new Vector3[5][];

            float[] newNormalized = new float[5];
            for (int f = 0; f < 5; f++)
            {
                bool noChange = true; //if true, just use the basic JointPosition/Rotations/Angles etc.
                if (locked[f]) //we are locked, and we are passing though the maximum flexion.
                {
                    if (currentTotals[f] < lockedTotalFlex[f]) // < because in Unity, flexion is negative. This is what happens if your flexion is greater than where we first touched.
                    {
                        // Setap 1: set all fingers flexion back to the ideal one.
                        Vector3[] fingerAngles = colliderPose.jointAngles[f];
                        for (int j = 0; j < fingerAngles.Length; j++)
                        {
                            fingerAngles[j].z = lockedFlexions[f][j]; //flexion is z in Unity.
                        }

                        //Step 2: Convert back into Internal coords for fwd kinematics
                        SGCore.Kinematics.Vect3D[] iAngles = SG.Util.SG_Conversions.ToEuler(fingerAngles);

                        // Step 2 - Forward Kinematics using these new angles.
                        SGCore.Kinematics.Quat[] iRotations;
                        SGCore.Kinematics.Vect3D[] iPositions;
                        //TODO: Integrate into C# API
                        if (f == 0)
                        {
                            int LR = handDimensions.IsRight ? 1 : -1; //default is created for right hands.
                            SGCore.Kinematics.Quat cmcStart = SGCore.Kinematics.Quat.FromAngleAxis(SGCore.Kinematics.Values.Deg2Rad * (-90 * LR), 1, 0, 0);
                            SGCore.Kinematics.JointKinematics.ForwardKinematics(handDimensions.GetJointPosition(SGCore.Finger.Thumb), cmcStart,
                                handDimensions.Get3DLengths(SGCore.Finger.Thumb), iAngles, out iPositions, out iRotations);
                        }
                        else
                        {
                            SGCore.Finger finger = (SGCore.Finger)f;
                            SGCore.Kinematics.JointKinematics.ForwardKinematics(handDimensions.GetJointPosition(finger), handDimensions.GetJointRotation(finger),
                                handDimensions.Get3DLengths(finger), iAngles, out iPositions, out iRotations);
                        }

                        //Step 3- converting these back into Unity values for SG_HandPose.
                        handAngles[f] = fingerAngles;
                        jointPositions[f] = SG.Util.SG_Conversions.ToUnityPositions(iPositions);
                        jointRotations[f] = SG.Util.SG_Conversions.ToUnityQuaternions(iRotations);
                        newNormalized[f] = lockedNormals[f];
                        noChange = false;
                    }
                    else if (currentTotals[f] > lockedTotalFlex[f]) //we've extended above it, but are still locked. Prevent the fingers from passing back through by updating this.
                    {
                        lockedFlexions[f] = currentFlex[f];
                        lockedTotalFlex[f] = currentTotals[f];
                        lockedNormals[f] = colliderPose.normalizedFlexion[f];
                    }
                }

                if (noChange)
                {
                    handAngles[f] = colliderPose.jointAngles[f];
                    jointPositions[f] = colliderPose.jointPositions[f];
                    jointRotations[f] = colliderPose.jointRotations[f];
                    newNormalized[f] = colliderPose.normalizedFlexion[f];
                }
            }
            constrainedPose = new SG_HandPose(handAngles, jointRotations, jointPositions, colliderPose.rightHanded,
                colliderPose.wristPosition, colliderPose.wristRotation, //wrist position / location stays the same here.
                newNormalized);
        }





        /// <summary> Updates the collider locations based on the SG_SimpleTracking scripts. Not needed if linked to an SG_TrackedHand poser. </summary>
        /// <param name="pose"></param>
        /// <param name="deltaTime"></param>
        public virtual void UpdateColliderLocatons(SG_HandPose pose, float deltaTime)
        {
            for (int i = 0; i < this.fingerColliders.Length; i++)
            {
                SG_HandComponent.UpdateColliderLocation(pose, fingerColliders[i]);
            }
        }





    }
}