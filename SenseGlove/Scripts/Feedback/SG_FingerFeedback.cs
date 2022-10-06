using UnityEngine;

namespace SG
{

    /// <summary> Extends impact feedback to also take into account force feedback from SG_Material's.
    /// These scripts calculate their distance into a collider. </summary>
    public class SG_FingerFeedback : SG_SimpleTracking
    {

        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables


        [Header("Force-Feedback Parameters")]
        public SGCore.Finger finger;


        /// <summary> If true, the force vectors of this script are rendered into the Scene view. </summary>    
        public bool debugDirections = false;

        public TextMesh debugTextElement;

        public string DebugText
        {
            get { return debugTextElement != null ? debugTextElement.text : ""; }
            set { if (debugTextElement != null) { debugTextElement.text = value; } }
        }

        /// <summary> Flexible detector list that will handle most of the detection work. We're only interested in the Interactable scripts it provides. </summary>
		public SG_ScriptDetector<SG_Material> materialsTouched = new SG_ScriptDetector<SG_Material>();

        public void UpdateDebugger()
        {
            DebugText = (this.TouchedMaterialScript != null ? TouchedMaterialScript.name : "-") + "\n" + System.Math.Round(this.DistanceInCollider, 3) + "m\n" + this.ForceLevel + "%";
        }



        /// <summary> The position of the collider the moment it entered a new object.  Used to determine collider normal. </summary>
        protected Vector3 entryOrigin = Vector3.zero;
        /// <summary> A point of the collider of the touchedObject on the moment that collision was detected. Used to determine collider normal. </summary>
        protected Vector3 entryPoint = Vector3.zero;

        /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
        public GameObject TouchedObject
        {
            get; protected set;
        }

        /// <summary> The Material of the last touched object. If set to null, it may have been deleted. </summary>
        public SG_Material TouchedMaterialScript
        {
            get; protected set;
        }

        /// <summary> The Mesh Deform of the last touched object, if available. Used to deform an object based on its SenseGlove-Material Properties. </summary>
        public SG_MeshDeform TouchedDeformScript
        {
            get; protected set;
        }

        /// <summary> The collider that activated the feedback </summary>
        public Collider TouchedCollider
        {
            get; protected set;
        }

        /// <summary> The distance [in m] that the finger collider has penetrated into the object. </summary>
        public float DistanceInCollider
        {
            get; protected set;
        }

        /// <summary> The current force-feedback level as determined by the material properties of the object we are touching. </summary>
        public int ForceLevel
        {
            get; protected set;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Functions

        /// <summary> Reset the forces and distances </summary>
        public void ResetForces()
        {
            ForceLevel = 0;
            DistanceInCollider = 0;
        }

        /// <summary> Setup this collider's properties </summary>
        public void SetupSelf()
        {
            //Debug.Log(this.name + "Calculated Offsets!");
            Util.SG_Util.TryAddRB(this.gameObject, false, true); //ensure this one has a RigidBody
            ResetForces();
        }

        /// <summary> Check if this collider is touching a valid GameObject </summary>
        public bool IsTouching()
        {
            return TouchedObject != null;
        }

        /// <summary> Check if this script is touching a specific gameobject </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsTouching(GameObject obj)
        {
            return obj != null && TouchedObject != null && GameObject.ReferenceEquals(TouchedObject, obj);
        }

        /// <summary> Check if this collider is touching a specific collider </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public bool IsTouching(Collider collider)
        {
            return collider == TouchedCollider;
        }



        /// <summary> Returns true if this script's touchedObject has been disabled or destroyed. </summary>
        /// <returns></returns>
        protected bool ObjectDisabled()
        {
            return TouchedObject == null || (TouchedObject != null && !TouchedObject.activeInHierarchy)
                || (TouchedCollider != null && !TouchedCollider.enabled);
        }


        /// <summary> Calculated an 'entry vector' between this object and a collider. </summary>
        /// <param name="col"></param>
        protected void FindForceDirection(Collider col)
        {
            //collect the absolute coordinates
            Vector3 Oa = transform.position;
            Vector3 Ea = col.ClosestPoint(Oa); //if something went wrong with ClosestPoint, it returns the entryPos.

            Transform colT = col.gameObject.transform;
            entryOrigin = SG.Util.SG_Util.CaluclateLocalPos(Oa, colT.position, colT.rotation);
            entryPoint = SG.Util.SG_Util.CaluclateLocalPos(Ea, colT.position, colT.rotation);

            DistanceInCollider = 0;
        }


        /// <summary> Calculate the force feedback levels based on material properties. </summary>
        private void UpdateFeedback()
        {
            if (TouchedObject != null)
            {
                Transform colT = TouchedObject.transform;
                Vector3 O = SG.Util.SG_Util.CalculateAbsWithOffset(colT.position, colT.rotation, entryOrigin);
                Vector3 E = SG.Util.SG_Util.CalculateAbsWithOffset(colT.position, colT.rotation, entryPoint);
                Vector3 P = transform.position;                         //P current collider position

                if (debugDirections)
                {
                    Debug.DrawLine(O, E, Color.white);
                    Debug.DrawLine(O, P, Color.blue);
                }
                Vector3 OE = (E - O).normalized;
                Vector3 OP = P - O;
                if (OP.magnitude > 0 && OE.magnitude > 0)
                {
                    float cos = Vector3.Dot(OE, OP) / (/*OE.magnitude */ OP.magnitude); //removed OE.magnitude since it is normalized now.
                    DistanceInCollider = OP.magnitude * cos;
                }
                else { DistanceInCollider = 0; }

                //we have calculated the distance, now for the material (if any is present)
                if (TouchedMaterialScript != null)
                {
                    ForceLevel = TouchedMaterialScript.CalculateForce(DistanceInCollider, (int)this.finger);
                }
                //now for the deform script if it exists
                if (TouchedDeformScript != null && DistanceInCollider > 0)
                {
                    Vector3 deformPoint = transform.position;
                    // limits the position to go no further than the maximum.
                    if (DistanceInCollider > TouchedDeformScript.maxDisplacement)
                    {
                        deformPoint = O + (OE * TouchedDeformScript.maxDisplacement);
                    }
                    TouchedDeformScript.AddDeformation(-OE, deformPoint, DistanceInCollider);
                }
                UpdateDebugger();
            }
        }



        /// <summary> Connect this script to a SG_Material, and link any other possible components </summary>
        /// <param name="collider"></param>
        /// <param name="material"></param>
        protected void AttachScript(Collider collider, SG_Material material)
        {
            TouchedCollider = collider;
            TouchedObject = material.gameObject;
            TouchedMaterialScript = material;
            TouchedDeformScript = TouchedObject.GetComponent<SG_MeshDeform>();
            ForceLevel = 0; //still 0 since OP == EO
            material.Touch(this);
            UpdateDebugger();
        }

        /// <summary> Remove this script's reference to its SG_Material so that it is free to find another </summary>
        public void DetachScript()
        {
            if (TouchedMaterialScript != null) { TouchedMaterialScript.UnTouch(this); } 
            TouchedCollider = null;
            TouchedObject = null;
            TouchedMaterialScript = null;
            if (TouchedDeformScript != null) { TouchedDeformScript.ResetMesh(); }
            TouchedDeformScript = null;
            ResetForces();
            UpdateDebugger();
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected void Start()
        {
            SetupSelf();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (ObjectDisabled()) { DetachScript(); }
            if (this.IsTouching())
            {
                if (entryPoint.Equals(entryOrigin)) { FindForceDirection(TouchedCollider); }
                else { UpdateFeedback(); }
            }
            
        }

        protected void OnTriggerEnter(Collider other)
        {
            SG_Material material;
            if (!this.IsTouching() && SG.Util.SG_Util.GetScript(other, out material))
            {
                AttachScript(other, material);
                FindForceDirection(other);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (this.IsTouching(other))
            {
                DetachScript();
            }
        }


    }

}