using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Projects the motion of a finger forward by 1 DoF, and estimates at which normalized flexion it hits any object(s). </summary>
    public class SG_FingerProjector1DoF : MonoBehaviour
    {

        public enum ProjectionDebugMethod
        {
            /// <summary> No Unity Debug info </summary>
            Off,
            /// <summary> Shows all Phalanges / collison steps that were checked </summary>
            ShowAllPhalanges,
            /// <summary> Show only the steps for phalanges that we actually check. </summary>
            ShowAllActivePhalanges,
            /// <summary>Show only the step at which we hit something... </summary>
            ShowHitFlexion,
            /// <summary> Show only the Phalange </summary>
            ShowHitPhalange,
        }

        /// <summary> Represents a 'hit' on one of the objects we are checking for. </summary>
        public class ProjectedHit
        {
            /// <summary> The collider that was hit </summary>
            public Collider Collider { get; set; }

            /// <summary> The normalized flexion value where this hit occured. </summary>
            public float FlexionValue { get; set; }

            /// <summary> THe Phalange that registered the hit (0 = Proximal Phalange / 1 = Medial Phalange / 2 = Distal Phalange </summary>
            public int PhalangeIndex { get; set; }

            /// <summary> The material attached to the collider that was hit. summary>
            public SG_Material HitMaterial { get; set; }

            /// <summary> The index of this collider in the CollidersInZone array </summary>
            public int ColliderIndex { get; set; }

            /// <summary> The index of flexion value. Useful for quick evaluations, but mostly useless outside our functions </summary>
            public int FlexIndex { get; set; }


            /// <summary> Indices for Phalanges. Usefulffor consistent access </summary>
            public const int ppIndex = 0, mpIndex = 1, dpIndex = 2;


            /// <summary> Create a new projection hit instance. </summary>
            /// <param name="col"></param>
            /// <param name="index"></param>
            public ProjectedHit(Collider col, int index)
            {
                Collider = col;
                ColliderIndex = index;
            }
        }




        [SerializeField] protected SGCore.Finger finger = SGCore.Finger.Thumb;

        public int collisionSteps = 4; //between 0 and 1
        public float fingerBoneRadius = 0.01f;

        public bool liveAbduction = true;

        public bool proximalCollision = true;
        public bool medialCollision = true;
        public bool distalCollision = true;


        public ProjectionDebugMethod debugMethod = ProjectionDebugMethod.Off;

        protected Vector3[][] localFlexPoints = new Vector3[0][]; //first index is for a specific text flexion, while second index is the location from proximal to distal [CMC/MCP, MCP/PIP, IP/DIP, Tip].
        protected float[] flexValues = new float[0]; //parallel to localFlexPoints. Index indicates whihc flexion is used.

        protected Vector3[][] lastWorldPositions = new Vector3[0][]; //calculated during last frame.
        protected ProjectedHit lastHit = null; //calculated during last frame.

        public bool CheckProjectionHit(SG_HandPose referencePose, SG_HandModelInfo handModel, List<Collider> colliderInZone, LayerMask projectionLayers,  out ProjectedHit flexionHit)
        {
            bool res = CheckProjectionHit(this.finger, this.collisionSteps, this.fingerBoneRadius, projectionLayers,
                this.liveAbduction, this.proximalCollision, this.medialCollision, this.distalCollision, this.flexValues, this.localFlexPoints,
                referencePose, handModel, colliderInZone, ref this.lastWorldPositions, out flexionHit);


            lastHit = flexionHit;
            return res;
        }

        public void GenerateVariables(SGCore.Finger forFinger, SG_HandModelInfo handModel)
        {
            this.finger = forFinger;
            SGCore.Kinematics.Vect3D[] fingerLengths = handModel.HandKinematics.Get3DLengths(this.finger);
            fingerLengths[2].x -= (this.fingerBoneRadius * 1000.0f); //subtract my radius (in mm) so that the bone capsule iself ends exactly at the fingertip.
            GenerateLocalFlexions(this.finger, this.collisionSteps, fingerLengths, out this.flexValues, out this.localFlexPoints);
           // Debug.Log(this.name + ": " + SG.Util.SG_Util.ToString( this.flexValues ) + ". Its elements are of size " + this.localFlexPoints[0].Length + " [0][1] = " + SG.Util.SG_Util.ToString(this.localFlexPoints[0][1]) );
        }






        //Return true if a collision has occured. If not, don't. 
        public static bool CheckProjectionHit(SGCore.Finger finger, int pathSteps, float colliderRadius, LayerMask projectionLayers,
            bool useLiveAbduction, bool proximalCollisions, bool medialCollisions, bool distalCollisions, float[] flexValues, Vector3[][] localPositions,
            SG_HandPose referencePose, SG_HandModelInfo handModel, List<Collider> collidersInZone, ref Vector3[][] worldPositions, out ProjectedHit flexionHit)
        {
            // Step 1: Calculate the proximal joint location. From there, we calculate the kinematics.
            Vector3 proximalJointPos; Quaternion proximalJointRot;
            CalculateProximalJoint(finger, useLiveAbduction, referencePose, handModel, out proximalJointPos, out proximalJointRot);

            // Step 2: Check for the first collision on the following points
            worldPositions = new Vector3[flexValues.Length][];

            //Checking per flexion step, as we can then re-use the world positions of particular joints AND we always have the highest flexion for this finger.
            for (int i=0; i<flexValues.Length; i++)
            {
                //with the proximal joint location known and having a set of flexion points local to it, I can calculate their position in 3D space, and do a capsule overlap check...
                //[CMC/ MCP, MCP / PIP, IP / DIP, Tip]

                //j0 would be proximalPos
                Vector3 j1 = proximalJointPos + (proximalJointRot * localPositions[i][1]);
                Vector3 j2 = proximalJointPos + (proximalJointRot * localPositions[i][2]);
                Vector3 j3 = proximalJointPos + (proximalJointRot * localPositions[i][3]);
                worldPositions[i] = new Vector3[] { proximalJointPos, j1, j2, j3 }; //store these so you can use them later for debug purposes.

                //Check from distal to proximal, if for no other reason than that Distal has the highest range, and is therefore most likely to encounter something...
                if (distalCollisions && CheckHits(collidersInZone, j2, j3, colliderRadius, projectionLayers, out flexionHit))
                {
                    flexionHit.FlexIndex = i;
                    flexionHit.FlexionValue = flexValues[i];
                    flexionHit.PhalangeIndex = ProjectedHit.dpIndex;
                    return true;
                }
                if (medialCollisions && CheckHits(collidersInZone, j1, j2, colliderRadius, projectionLayers, out flexionHit))
                {
                    flexionHit.FlexIndex = i;
                    flexionHit.FlexionValue = flexValues[i];
                    flexionHit.PhalangeIndex = ProjectedHit.mpIndex;
                    return true;
                }
                if (proximalCollisions && CheckHits(collidersInZone, proximalJointPos, j1, colliderRadius, projectionLayers, out flexionHit))
                {
                    flexionHit.FlexIndex = i;
                    flexionHit.FlexionValue = flexValues[i];
                    flexionHit.PhalangeIndex = ProjectedHit.ppIndex;
                    return true;
                }

            }
            flexionHit = null;
            return false;
        }


        public static bool CheckHits(List<Collider> collidersInZone, Vector3 jointPosition0, Vector3 jointPosition1, float colliderRadius, LayerMask projectionLayers, out ProjectedHit hit)
        {
            Collider[] phalangeTouch = Physics.OverlapCapsule(jointPosition0, jointPosition1, colliderRadius, projectionLayers.value, QueryTriggerInteraction.Ignore); //ignore trigger colliders, always.
            for (int j = 0; j < phalangeTouch.Length; j++) //check for hits.
            {
                int matches = SG.Util.SG_Util.ListIndex(collidersInZone, phalangeTouch[j]); //find which of my colliders this is
                if (matches > -1) //this is an object we scan for, that we have yet to touch
                {
                    hit = new ProjectedHit(phalangeTouch[j], matches); //2 becasue this is a distal phalange;
                    return true;
                }
            }
            hit = null;
            return false;
        }


        public static void CalculateProximalJoint(SGCore.Finger finger, bool useLiveAbduction, SG_HandPose referencePose, SG_HandModelInfo handModel, out Vector3 proximalJointPos, out Quaternion proximalJointRot)
        {
            int f = (int)finger;
            Vector3 wristToJoint_pos = SG.Util.SG_Conversions.ToUnityPosition(handModel.HandKinematics.GetJointPosition(finger));
            float abd = useLiveAbduction ? referencePose.jointAngles[f][0].y : 0.0f;
            //Calculate the current rotation & position of the CMC / MCP joint in 3D Space.
            proximalJointPos = referencePose.wristPosition + (referencePose.wristRotation * wristToJoint_pos);
            if (f == 0)
            {
                int LR = handModel.IsRightHand ? 1 : -1;
                Quaternion wristToJoint_rot = Quaternion.Euler(LR * 90, 0, 0); //90 degrees as this is a left handed coordinate system.
                proximalJointRot = useLiveAbduction ? referencePose.wristRotation * (wristToJoint_rot * Quaternion.Euler(0.0f, abd, 0.0f)) : (referencePose.wristRotation * wristToJoint_rot);
            }
            else
            {
                Quaternion wristToJoint_rot = SG.Util.SG_Conversions.ToUnityQuaternion(handModel.HandKinematics.GetJointRotation(finger));
                proximalJointRot = useLiveAbduction ? Quaternion.Euler(0.0f, abd, 0.0f) * (referencePose.wristRotation * wristToJoint_rot) : (referencePose.wristRotation * wristToJoint_rot);
            }
        }








        /// <summary> I'm lazy, so we're using SG's internal forward kinematics system. I can afford to do so because this supposedly only happens once (or when the handModel resizes) </summary>
        /// <param name="angles"></param>
        /// <param name="fingerLengths"></param>
        /// <returns></returns>
        public static Vector3[] CalculateLocalPositions(SGCore.Kinematics.Vect3D[] angles, SGCore.Kinematics.Vect3D[] fingerLengths)
        {
            SGCore.Kinematics.Vect3D[] positions;
            SGCore.Kinematics.Quat[] rotations;
            SGCore.Kinematics.JointKinematics.ForwardKinematics(SGCore.Kinematics.Vect3D.zero, SGCore.Kinematics.Quat.identity,
                fingerLengths, angles, out positions, out rotations);
            return SG.Util.SG_Conversions.ToUnityPositions(positions);
        }

        public static void GenerateLocalFlexions(SGCore.Finger finger, int inBetweenSteps, SGCore.Kinematics.Vect3D[] fingerLengths, out float[] flexValues, out Vector3[][] flexPoints )
        {
            int allSteps = inBetweenSteps + 2; // +2 because we also include 0.0f and 1.0f
            float flexStep = 1.0f / (inBetweenSteps + 1); //+1 because the one at 1.0f is also there. this turns 1 in between step to 0.5f, or two inbetweens to [0.33, 0.66]. etc.

            flexValues = new float[allSteps];
            flexPoints = new Vector3[allSteps][];

            for (int i=0; i < allSteps; i++)
            {
                flexValues[i] = i * flexStep;
                SGCore.Kinematics.Vect3D[] angles = SGCore.Kinematics.Anatomy.FingerAngles_FromNormalized(finger, flexValues[i], 0.0f, true);
                flexPoints[i] = CalculateLocalPositions(angles, fingerLengths);
            }
        }





        private void DrawGizmoLines()
        {
            if (this.lastWorldPositions.Length == 0 || this.debugMethod == ProjectionDebugMethod.Off)
                return;

            if (this.debugMethod == ProjectionDebugMethod.ShowHitFlexion || this.debugMethod == ProjectionDebugMethod.ShowHitPhalange)
            {
                if (lastHit != null)
                {
                    bool showPP = debugMethod == ProjectionDebugMethod.ShowHitFlexion || this.lastHit.PhalangeIndex == 0;
                    bool showMP = debugMethod == ProjectionDebugMethod.ShowHitFlexion || this.lastHit.PhalangeIndex == 1;
                    bool showDP = debugMethod == ProjectionDebugMethod.ShowHitFlexion || this.lastHit.PhalangeIndex == 2;

                    if (showPP) { SG.Util.SG_Util.Gizmo_DrawWireCapsule(lastWorldPositions[lastHit.FlexIndex][0], lastWorldPositions[lastHit.FlexIndex][1], Color.white, this.fingerBoneRadius); }
                    if (showMP) { SG.Util.SG_Util.Gizmo_DrawWireCapsule(lastWorldPositions[lastHit.FlexIndex][1], lastWorldPositions[lastHit.FlexIndex][2], Color.white, this.fingerBoneRadius); }
                    if (showDP) { SG.Util.SG_Util.Gizmo_DrawWireCapsule(lastWorldPositions[lastHit.FlexIndex][2], lastWorldPositions[lastHit.FlexIndex][3], Color.white, this.fingerBoneRadius); }
                }
            }

            if (debugMethod == ProjectionDebugMethod.ShowAllPhalanges || debugMethod == ProjectionDebugMethod.ShowAllActivePhalanges)
            {
                bool showPP = debugMethod == ProjectionDebugMethod.ShowAllPhalanges || this.proximalCollision;
                bool showMP = debugMethod == ProjectionDebugMethod.ShowAllPhalanges || this.medialCollision;
                bool showDP = debugMethod == ProjectionDebugMethod.ShowAllPhalanges || this.distalCollision;

                for (int i = 0; i < this.lastWorldPositions.Length; i++)
                {
                    if (lastWorldPositions[i] == null) //we swept as far as this value...
                    {
                        break;
                    }
                    if (showPP) { SG.Util.SG_Util.Gizmo_DrawWireCapsule(lastWorldPositions[i][0], lastWorldPositions[i][1], Color.white, this.fingerBoneRadius); }
                    if (showMP) { SG.Util.SG_Util.Gizmo_DrawWireCapsule(lastWorldPositions[i][1], lastWorldPositions[i][2], Color.white, this.fingerBoneRadius); }
                    if (showDP) { SG.Util.SG_Util.Gizmo_DrawWireCapsule(lastWorldPositions[i][2], lastWorldPositions[i][3], Color.white, this.fingerBoneRadius); }
                }
            }
        }



        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            this.DrawGizmoLines();
        }
    }
}
