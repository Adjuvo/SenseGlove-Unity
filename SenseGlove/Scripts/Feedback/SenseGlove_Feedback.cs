using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses (predictive) colliders to detect force feedback level(s) based on material properties. </summary> 
[RequireComponent(typeof(SphereCollider))]
public class SenseGlove_Feedback : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties   

    [Tooltip("The collider used to determine which object is currently being touched")]
    public SphereCollider touch;

    /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
    [Tooltip("The object that is currently touched by this SenseGlove_Touch script.")]
    public GameObject touchedObject;

    private SenseGlove_Material touchedMaterial;
    private SenseGlove_MeshDeform touchedDeform;
    private SenseGlove_Interactable touchedScript;

    /// <summary> The grabscript using these colliders for its logic. </summary>
    public SenseGlove_HandModel handModel;

    /// <summary> The position of the collider the moment it entered a new object.  Used to determine collider normal. </summary>
    private Vector3 entryOrigin = Vector3.zero;
    /// <summary> A point of the collider of the touchedObject on the moment that collision was detected. Used to determine collider normal. </summary>
    private Vector3 entryPoint = Vector3.zero;

    /// <summary> The distance [in m] that the finger collider has penetrated into the object. </summary>
    public float dist = 0;

    /// <summary> The current force-feedback level as determined by the material properties of the object we are touching. </summary>
    private int motorLevel = 0;

    /// <summary> The current buzz-motor level as determined by the material properties of the object we are touching. </summary>
    private int buzzLevel = 0;

    /// <summary> The haptic pulse duration determined by the material properties of the object we are touching. </summary>
    private int buzzTime = 0;

    /// <summary> The (finger) index of this feedback script in its appropriate handmodel [0...4] [Thumb...Pinky] </summary>
    private int scriptIndex = -1;

    #endregion Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Get / Set attributes for Construction

    #region Setup

    /// <summary> Setup the Feedback Collider to use the chosen handmodel as a parent, using its force feedback type. </summary>
    /// <param name="parentModel"></param>
    public void Setup(SenseGlove_HandModel parentModel, int index)
    {
        this.handModel = parentModel;
        this.scriptIndex = index;

        Rigidbody RB = this.gameObject.GetComponent<Rigidbody>();
        if (RB == null)
        {
            RB = this.gameObject.AddComponent<Rigidbody>();
        }
        RB.isKinematic = true;
        RB.useGravity = false;
    }

    /// <summary> Set the scriptIndex of this feedback_script. Reserved for parentModel </summary>
    /// <param name="newIndex"></param>
    public void SetIndex(int newIndex)
    {
        this.scriptIndex = newIndex;
    }

    /// <summary>
    /// Retrieve the index of this script, allowing one to determine which finger it belongs to.
    /// </summary>
    public int GetIndex()
    {
        return this.scriptIndex;
    }

    #endregion Setup

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region MonoBehaviour

    //Colled when the application starts
    void Start()
    {
        if (this.touch == null) { this.touch = this.GetComponent<SphereCollider>(); }
        this.motorLevel = 0;
    }

    // Called once per frame
    void Update()
    {
        this.touch.isTrigger = true;
    }

    // Called during a physics update.
    void FixedUpdate()
    {
        if (touch != null) { touch.isTrigger = true; } //ensure the touch collider is always kinematic.

        if (this.touchedObject != null && !this.touchedObject.activeInHierarchy)
        {
            this.touchedObject = null;
            this.touchedMaterial = null;
            this.touchedDeform = null;
            this.motorLevel = 0;
            this.dist = 0;
        }
    }

    #endregion MonoDevelop

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection / Force Feedback 

    #region Collision

    // Called when this object enters the collider of another object
    void OnTriggerEnter(Collider col)
    {
       
        SenseGlove_Material material = col.GetComponent<SenseGlove_Material>();
        SenseGlove_Interactable interactable = col.GetComponent<SenseGlove_Interactable>();
        if (material || interactable)
        {
            // SenseGlove_Debugger.Log("Touching " + col.name + "; material = " + (material != null) + ", interactable = " + (interactable != null));
            this.touchedObject = col.gameObject;
            this.touchedScript = interactable;
            this.touchedMaterial = material;
            this.touchedDeform = col.GetComponent<SenseGlove_MeshDeform>();

            if (this.handModel.forceFeedback == ForceFeedbackType.Simple && material)
            {
                this.motorLevel = material.maxForce;
            }
            else if (this.handModel.forceFeedback == ForceFeedbackType.MaterialBased)
            {
                this.FindForceDirection(col);
                this.motorLevel = 0; //still 0 since OP == EO
            }

            if (material && material.hapticFeedback)
            {
                this.buzzLevel = material.hapticMagnitude;
                this.buzzTime = material.hapticDuration;
            }
        }
    }

    // Called every FixedUpdate while this collider is inside another collider.
    void OnTriggerStay(Collider col)
    {
        if (this.IsTouching(col.gameObject)) //Check if we're still on the same object?
        {
            //any object that we are touching has either an Interactable and/or a material

            //Calculate Motor Level
            if (this.handModel.forceFeedback == ForceFeedbackType.Simple)
            {
                if (this.touchedMaterial)
                {
                    this.motorLevel = this.touchedMaterial.maxForce;
                }
            }
            else if (this.handModel.forceFeedback == ForceFeedbackType.MaterialBased)
            {
                if (this.entryPoint.Equals(this.entryOrigin))
                {
                    this.FindForceDirection(col); //only during FixedUpdate will the collider have a new position for us to check.
                }
                else
                {
                    this.CalculateMaterialBased(col.gameObject, this.touchedMaterial, this.touchedScript, true);
                }
            }
        }
    }

    // Called when this object exits the collider of another object
    void OnTriggerExit(Collider col)
    {
        if (this.touchedObject != null && this.IsTouching(col.gameObject))
        {
            if (this.touchedDeform != null) { this.touchedDeform.ResetMesh(); }
            this.touchedObject = null;
            this.motorLevel = 0;
            this.buzzLevel = 0;
            this.dist = 0;
        }
    }

    #endregion Collision

    //--------------------------------------------------------------------------------------------------------------------------
    // Feedback Logic

    #region Feedback

    private void FindForceDirection(Collider col)
    {
        //collect the absolute coordinates
        Vector3 Oa = this.transform.position;
        Vector3 Ea = col.ClosestPoint(Oa); //if something went wrong with ClosestPoint, it returns the entryPos.

        //transform these to the objects local space.
        this.entryOrigin = col.gameObject.transform.InverseTransformPoint(Oa);
        this.entryPoint = col.gameObject.transform.InverseTransformPoint(Ea);

        this.dist = 0;
    }

    /// <summary> Calculate the force feedback levels based on material properties. </summary>
    /// <param name="col"></param>
    /// <remarks>Placed in a separate method so that one can control when it is called in Unity's execution order.</remarks>
    private void CalculateMaterialBased(GameObject obj, SenseGlove_Material material, SenseGlove_Interactable interactable, bool showLines = false)
    {
        Vector3 O = obj.transform.TransformPoint(this.entryOrigin);  //O origin of collider on touch
        Vector3 E = obj.transform.TransformPoint(this.entryPoint);   //E point where the collider touched the object
        Vector3 P = this.transform.position;                         //P current collider position

        if (showLines)
        {
            Debug.DrawLine(O, E);
            Debug.DrawLine(O, P);
        }

        Vector3 OE = (E - O).normalized;
        Vector3 OP = P - O;

        if (OP.magnitude > 0 && OE.magnitude > 0)
        {
            float cos = Vector3.Dot(OE, OP) / (/*OE.magnitude */ OP.magnitude); //removed OE.magnitude since it is normalized now.
            this.dist = OP.magnitude * cos;
        }
        else
        {
            this.dist = 0;
        }

        //we have calculated the distance, now for the material (if any is present)
        if (material != null)
        {
            this.motorLevel = material.CalculateForce(this.dist, this.scriptIndex);

            if (dist > 0 && this.touchedDeform != null)
            {
                Vector3 deformPoint = this.transform.position;
                if (dist > this.touchedDeform.maxDisplacement)
                {
                    deformPoint = O + (OE * this.touchedDeform.maxDisplacement);
                }
                this.touchedDeform.AddDeformation(-OE, deformPoint, this.dist);
            }
        }
         
    }

    /// <summary> Returns the brake level as indicated by the material that we are currently touching. </summary>
    /// <returns></returns>
    public int BrakeLevel() { return this.motorLevel; }

    /// <summary> Returns the buzzmotor level as indicated by the material that we are currently touching. </summary>
    /// <returns></returns>
    public int BuzzLevel() { return this.motorLevel; }

    /// <summary> Returns the time (in ms) that the buzzMotor will virbate for. </summary>
    /// <returns></returns>
    public int BuzzTime() { return this.buzzTime; }

    /// <summary> Check whether or not this collider should send buzz motor commands </summary>
    /// <returns></returns>
    public bool CanBuzz() { return this.buzzLevel > 0 && this.buzzTime > 0; }

    /// <summary> Reset the haptic feedback of this collider, preventing us from sending the command twice. </summary>
    public void ResetHaptics()
    {
        this.buzzLevel = 0;
        this.buzzTime = 0;
    }

    #endregion Feedback

    //--------------------------------------------------------------------------------------------------------------------------
    // Touch Logic

    #region Touch

    /// <summary> Check if this SenseGlove_Touch is touching object obj. </summary>
    /// <param name="obj"></param>
    public bool IsTouching(GameObject obj)
    {
        if (this.touchedObject != null && obj != null)
        {
            bool t = GameObject.ReferenceEquals(this.touchedObject, obj);
            return t;
        }
        return false;
    }

    /// <summary> Returns the object that this collider is touching. Usually to check if it is also touched by other colliders. </summary>
    /// <returns></returns>
    public GameObject TouchObject()
    {
        return this.touchedObject;
    }

    #endregion Touch

}
