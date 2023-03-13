using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> This layer projects forward finger movement(s) and keeps track at which (normalized) values they would hit an object tagged with an SG_Material. </summary>
    /// <remarks> This is a separate script this is not directly related to projection(s). Because we can use it for visual elements as well. </remarks>
    public class SG_HandProjection : SG_HandComponent
    {

        public LayerMask projectionLayers = ~0; //~0 is all

        /// <summary> Projectors for individual fingers. </summary>
        public SG_FingerProjector1DoF[] projectors = new SG_FingerProjector1DoF[0];

        /// <summary> We will only project only objects that occur within this zone. Used so speed up detection. </summary>
        public SG_MaterialDetector objectDetector;

        /// <summary> Used or animation </summary>
        public bool useForAnimation = true;

        /// <summary> Used to visualuze debug information. </summary>
        public TextMesh debugElement;

        protected SG_FingerProjector1DoF.ProjectedHit[] lastHits = null;


        //the previously calculated finger angles. Used for optmization purposes.

        float[] lastFlexForLocks = new float[5] { -1.0f, -1.0f, -1.0f, -1.0f, -1.0f }; //The highest extension measured while we'e under the flexion value of an object


        
        //Finger values calculated during last frame. Cached here so I don't have to do this every frame
        Vector3[][] calc_JointAngles = new Vector3[5][];
        Vector3[][] calc_JointPositions = new Vector3[5][];
        Quaternion[][] calc_JointRotations = new Quaternion[5][];
        // The last flexions / abd pair used for calculations, used to prevent re-calculating every frame.
        float[] calc_Flex = new float[5] { -1.0f, -1.0f, -1.0f, -1.0f, -1.0f }; //start these at -1 so we calculate the values the very first time...
        float[] calc_Abd = new float[5] { -1.0f, -1.0f, -1.0f, -1.0f, -1.0f }; //start these at -1 so we calculate the values the very first time...

        /// <summary> Text of the DebugElement </summary>
        public string DebugText
        {
            get { return this.debugElement != null ? this.debugElement.text : ""; }
            set { if (this.debugElement != null) { this.debugElement.text = value; } }
        }


        /// <summary> Updates the projections of this hand. </summary>
        /// <param name="referencePose">Required for the abduction / adduction of the fingers. </param>
        public void UpdateProjections(SG_HandPose referencePose)
        {
            List<Collider> collidersInZone = objectDetector.GetUnbrokenColliders();
            for (int f = 0; f < this.projectors.Length; f++)
            {
                SG_FingerProjector1DoF.ProjectedHit hit;
                projectors[f].CheckProjectionHit(referencePose, this.TrackedHand.handModel, collidersInZone, this.projectionLayers, out hit); //will be NULL or will exist.
                if (hit != null)
                {
                    if (lastHits[f] == null)
                    {
                       // Debug.Log(f + " entered range at flexion of " + referencePose.normalizedFlexion[f]);
                        lastFlexForLocks[f] = 1000.0f; //make sure we're always updating for our current flexion during the next Update...

                        //update Material
                        SG_Material touchedMaterial;
                        this.objectDetector.GetConnectedMaterial(hit.Collider, out touchedMaterial);
                        hit.HitMaterial = touchedMaterial;
                    }
                    else if ( hit.Collider != lastHits[f].Collider ) //We were hitting something before, but its a different collider!
                    {
                        //it' a different collider, so update the material!
                        SG_Material touchedMaterial;
                        this.objectDetector.GetConnectedMaterial(hit.Collider, out touchedMaterial);
                        hit.HitMaterial = touchedMaterial;
                    }
                    else //it' the same collider as lastHits, so we don't need to update...
                    {
                        hit.HitMaterial = lastHits[f].HitMaterial; //make sure to keep the material in memory...?
                    }
                }
                lastHits[f] = hit;
            }

            if (this.debugEnabled)
            {
                string txt = "";

                bool[] touching; float[] touchedFlex;
                this.GetTouchInformation(out touching, out touchedFlex, true);
                for (int i=0; i<touching.Length; i++)
                {
                    txt += ((SGCore.Finger)i).ToString() + ":\t";
                    txt += touching[i] ? touchedFlex[i].ToString("0.00") : "-";
                    txt += this.lastHits[i] != null && this.lastHits[i].HitMaterial != null ? " / " + lastHits[i].HitMaterial.name + (this.lastHits[i].HitMaterial.CanDeform() ? " (D)" : "") : " / NULL";
                    if (i < touching.Length - 1) { txt += "\n"; }
                }
                DebugText = txt;
            }
        }


        protected override List<Collider> CollectPhysicsColliders()
        {
            List<Collider> baseColliders =  base.CollectPhysicsColliders();
            List<Collider> detectionColliders = this.objectDetector.GetDetectionColliders();
            for (int i=0; i<detectionColliders.Count; i++)
            {
                SG.Util.SG_Util.SafelyAdd(detectionColliders[i], baseColliders);
            }
            return baseColliders;
        }


        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers);

            SG.Util.SG_Util.CollectGameObject(this.debugElement, ref objects);
            if (this.objectDetector != null)
            {
                SG.Util.SG_Util.CollectComponent(this.objectDetector.gameObject, ref renderers);
                SG.Util.SG_Util.CollectGameObject(this.objectDetector.debugTxt, ref objects);
            }
        }



        public static void CalculateFingerTracking(int f, float normalizedFlexion, float liveAbduction, SG_HandModelInfo handModel,
            out Vector3[] jointAngles, out Vector3[] jointPositions, out Quaternion[] jointRotations)
        {
            float SGAbd = -liveAbduction * Mathf.Deg2Rad; //because unity is both left handed and in degrees.
            CalculateFingerTracking(f, normalizedFlexion, SGAbd, handModel.HandKinematics, out SGCore.Kinematics.Vect3D[] jointAngles_sg, out SGCore.Kinematics.Vect3D[] jointPositions_sg, out SGCore.Kinematics.Quat[] jointRotations_sg);

            jointAngles = SG.Util.SG_Conversions.ToUnityEulers(jointAngles_sg);
            jointPositions = SG.Util.SG_Conversions.ToUnityPositions(jointPositions_sg);
            jointRotations = SG.Util.SG_Conversions.ToUnityQuaternions(jointRotations_sg);
        }

        private static void CalculateFingerTracking(int f, float normalizedFlexion, float liveAbduction_sg, SGCore.Kinematics.BasicHandModel handModel,
            out SGCore.Kinematics.Vect3D[] jointAngles_sg, out SGCore.Kinematics.Vect3D[] jointPositions_sg, out SGCore.Kinematics.Quat[] jointRotations_sg)
        {
            SGCore.Finger finger = (SGCore.Finger)f;

            float[] flexions = SGCore.Kinematics.Anatomy.Flexions_FromNormalized(finger, normalizedFlexion);
            jointAngles_sg = new SGCore.Kinematics.Vect3D[] 
            {
                new SGCore.Kinematics.Vect3D( 0.0f, flexions[0], liveAbduction_sg ),
                new SGCore.Kinematics.Vect3D( 0.0f, flexions[1], 0.0f ),
                new SGCore.Kinematics.Vect3D( 0.0f, flexions[2], 0.0f ) 
            };

            SGCore.Kinematics.Quat startRot;
            if (f == 0)
            {
                int LR = handModel.IsRight ? 1 : -1;
                startRot = SGCore.Kinematics.Quat.FromAngleAxis( Mathf.Deg2Rad * (-90 * LR) , 1.0f, 0.0f, 0.0f);
            }
            else
            {
                startRot = handModel.GetJointRotation(finger);
            }

            SGCore.Kinematics.JointKinematics.ForwardKinematics(handModel.GetJointPosition(finger), startRot,
                handModel.Get3DLengths(finger), jointAngles_sg, out jointPositions_sg, out jointRotations_sg);
        }


        /// <summary> Returns a pose with finger tracking limited to not pass through any material colliders </summary>
        public SG_HandPose GetConstrainedPose(SG_HandPose referencePose, SG_HandModelInfo handModel)
        {
            //TODO: Take into account soft objects...
            float[] normalized = referencePose.normalizedFlexion;

            //these ones might change. Everything else we'll grab off the referencepose etc.
            float[] resNormalized = new float[5];
            Vector3[][] jointAngles = new Vector3[5][];
            Vector3[][] jointPositions = new Vector3[5][];
            Quaternion[][] jointRotations = new Quaternion[5][];

            for (int f=0; f<5; f++)
            {
                if (lastHits[f] != null && (lastHits[f].HitMaterial == null || !lastHits[f].HitMaterial.CanDeform() ) && normalized[f] > lastHits[f].FlexionValue) //the real fingers have flexed past the threshold!
                {
                    //Check if the fingers have extended a little bit since last time...
                    if (normalized[f] < lastFlexForLocks[f])
                    {
                        lastFlexForLocks[f] = normalized[f];
                    }
                    float abdForUpdate = referencePose.jointAngles[f][0].y;
                    //Update our internal kinemtics if required
                    if ( !SGCore.Kinematics.Values.FloatEquals( lastFlexForLocks[f], calc_Flex[f] ) || !SGCore.Kinematics.Values.FloatEquals(abdForUpdate, calc_Abd[f]) ) //Either abduction or lastFlexForLocks has been updated...
                    {
                        calc_Abd[f] = abdForUpdate;
                        calc_Flex[f] = lastFlexForLocks[f];
                        CalculateFingerTracking(f, lastFlexForLocks[f], abdForUpdate, handModel, out calc_JointAngles[f], out calc_JointPositions[f], out calc_JointRotations[f]);
                    }
                    // Assign this as the values for our finger tracking.
                    resNormalized[f] = calc_Flex[f];
                    jointAngles[f] = calc_JointAngles[f];
                    jointPositions[f] = calc_JointPositions[f];
                    jointRotations[f] = calc_JointRotations[f];

                }
                else //we'e not touching anything, OR we'e above the limit. Yey!
                {
                    resNormalized[f] = normalized[f];
                    jointAngles[f] = referencePose.jointAngles[f];
                    jointPositions[f] = referencePose.jointPositions[f];
                    jointRotations[f] = referencePose.jointRotations[f];
                }
            }
            return new SG_HandPose(jointAngles, jointRotations, jointPositions, handModel.IsRightHand, referencePose.wristPosition, referencePose.wristRotation, resNormalized);
        }


        protected override void CreateComponents()
        {
            base.CreateComponents();
            this.DebugText = "";
            this.lastHits = new SG_FingerProjector1DoF.ProjectedHit[this.projectors.Length];
        }

        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            base.LinkToHand_Internal(newHand, firstLink);
            for (int f = 0; f < this.projectors.Length; f++)
            {
                projectors[f].GenerateVariables((SGCore.Finger)f, newHand.handModel);
            }
            Transform wrist = newHand.GetTransform(SG_TrackedHand.TrackingLevel.VirtualPose, HandJoint.Wrist);
            this.objectDetector.SetTrackingTarget(wrist, true);

        }

        public void GetTouchInformation(out bool[] touchingObject, out float[] touchedFlexion, bool ignoreSoftMaterials)
        {
            touchedFlexion = new float[5];
            touchingObject = new bool[5];
            for (int i=0; i<this.lastHits.Length; i++)
            {
                //for now, we ignore force-feedback commands for soft materials
                if (lastHits[i] != null && ( !ignoreSoftMaterials || lastHits[i].HitMaterial == null 
                    || ( lastHits[i].HitMaterial.MaxForce >= 0.90f && lastHits[i].HitMaterial.MaxForceDistance <= 0.001f ) //more than 1mm MaxForceDist means its soft, and thus we handle it with traditional commands
                    ) )
                {
                    //TODO: If this is a 'soft' object, we should lock a bit further into the object itself. There esists a minor 'mapping' from MaxForceDist to flexion(s).
                    //TODO: Take into account harder matertials with Threshold commands...?
                    touchingObject[i] = true;
                    touchedFlexion[i] = lastHits[i].FlexionValue;
                }
            }
        }


    }
}
