using UnityEngine;

namespace SG
{

    /// <summary> Extends impact feedback to also take into account force feedback from SG_Material's.
    /// These scripts calculate their distance into a collider. </summary>
    public class SG_FingerFeedback : SG_BasicFeedback
    {
        /// <summary> If true, the force vectors of this script are rendered into the Scene view. </summary>
        [Header("Force-Feedback Parameters")]
        public bool debugDirections = false;

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


        /// <summary> Reset the forces and distances </summary>
        public void ResetForces()
        {
            ForceLevel = 0;
            DistanceInCollider = 0;
        }



        /// <summary> Setup this collider's properties </summary>
        public override void SetupSelf()
        {
            base.SetupSelf();
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

        /// <summary> Utility function to find a SG_Material script attached to a collider. Returns true if such a script exists. </summary>
        /// <param name="col"></param>
        /// <param name="materialScript"></param>
        /// <param name="favourSpecific"></param>
        /// <returns></returns>
        public static bool GetMaterialScript(Collider col, out SG_Material materialScript, bool favourSpecific = true)
        {
            SG_Material myMat = col.gameObject.GetComponent<SG_Material>();
            if (myMat != null && favourSpecific) //we favour the 'specific' material over a global material.
            {
                materialScript = myMat;
                return true;
            }
            //myMat might exist, but we favour the connected one if possible.
            SG_Material connectedMat = col.attachedRigidbody != null ?
                col.attachedRigidbody.gameObject.GetComponent<SG_Material>() : null;

            if (connectedMat == null) { materialScript = myMat; } //the connected body does not have a material, so regardless we'll try the specific one.
            else { materialScript = connectedMat; }
            return materialScript != null;
        }

        /// <summary> Utility function to check if a collider has a specific SG_Material collider attached. </summary>
        /// <param name="col"></param>
        /// <param name="touchedMat"></param>
        /// <returns></returns>
        public static bool SameScript(Collider col, SG_Material touchedMat)
        {
            if (touchedMat != null && col != null)
            {
                if (GameObject.ReferenceEquals(col.gameObject, touchedMat.gameObject))
                    return true; //this is the touched object.
                                 // at this line, col does not have the same material, but perhaps its attachedRigidbody does.
                return col.attachedRigidbody != null && GameObject.ReferenceEquals(col.attachedRigidbody.gameObject,
                    touchedMat.gameObject);
            }
            return false;
        }


        /// <summary> Returns true if this script's touchedObject has been disabled or destroyed. </summary>
        /// <returns></returns>
        protected bool ObjectDisabled()
        {
            return (TouchedObject != null && !TouchedObject.activeInHierarchy)
                || (TouchedCollider != null && !TouchedCollider.enabled);
        }


        /// <summary> Calculated an 'entry vector' between this object and a collider. </summary>
        /// <param name="col"></param>
        protected void FindForceDirection(Collider col)
        {
            //collect the absolute coordinates
            Vector3 Oa = transform.position;
            Vector3 Ea = col.ClosestPoint(Oa); //if something went wrong with ClosestPoint, it returns the entryPos.

            //transform these to the objects local space.
            entryOrigin = col.gameObject.transform.InverseTransformPoint(Oa);
            entryPoint = col.gameObject.transform.InverseTransformPoint(Ea);

            DistanceInCollider = 0;
        }


        /// <summary> Calculate the force feedback levels based on material properties. </summary>
        private void UpdateFeedback()
        {
            if (TouchedObject != null)
            {
                Vector3 O = TouchedObject.transform.TransformPoint(entryOrigin);  //O origin of collider on touch
                Vector3 E = TouchedObject.transform.TransformPoint(entryPoint);   //E point where the collider touched the object
                Vector3 P = transform.position;                         //P current collider position

                if (debugDirections)
                {
                    Debug.DrawLine(O, E);
                    Debug.DrawLine(O, P);
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
                    ForceLevel = TouchedMaterialScript.CalculateForce(DistanceInCollider, (int)this.handLocation);
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
        }

        /// <summary> Remove this script's reference to its SG_Material so that it is free to find another </summary>
        public void DetachScript()
        {
            TouchedCollider = null;
            TouchedObject = null;
            TouchedMaterialScript = null;
            if (TouchedDeformScript != null) { TouchedDeformScript.ResetMesh(); }
            TouchedDeformScript = null;
            ResetForces();
        }


        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.IsTouching())
            {
                if (ObjectDisabled()) { DetachScript(); }
                else if (entryPoint.Equals(entryOrigin)) { FindForceDirection(TouchedCollider); }
                else { UpdateFeedback(); }
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            SG_Material material;
            if (!this.IsTouching() && GetMaterialScript(other, out material))
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