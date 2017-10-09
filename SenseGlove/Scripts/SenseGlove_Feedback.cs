using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses predictive colliders to detect force feedback level(s) based on material properties. </summary> 
[RequireComponent(typeof(SphereCollider))]
public class SenseGlove_Feedback : MonoBehaviour
{

    //--------------------------------------------------------------------------------------------------------------------------
    // Publicly visible attributes

    [Tooltip("The collider used to determine which object is currently being touched")]
    public SphereCollider touch;

    /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
    [Tooltip("The object that is currently touched by this SenseGlove_Touch script.")]
    public GameObject touchedObject;

    /// <summary> The grabscript using these colliders for its logic. </summary>
    public SenseGlove_HandModel handModel;

    /// <summary> To be moved to its SenseGlove_Physgrab Script. </summary>
    private ForceFeedbackType forceFeedback = ForceFeedbackType.None;

    /// <summary> The position of the collider the moment it entered a new object.  Used to determine collider normal. </summary>
    private Vector3 entryPos = Vector3.zero;
    /// <summary> A point of the collider of the touchedObject on the moment that collision was detected. Used to determine collider normal. </summary>
    private Vector3 entryPoint = Vector3.zero;

    /// <summary> The distance [in m] that the finger collider has penetrated into the object. </summary>
    public float dist = 0;

    /// <summary> The force-feedback level as determined by the material properties of the object we are touching. </summary>
    public int motorLevel = 0;

    //--------------------------------------------------------------------------------------------------------------------------
    // Get / Set attributes for Construction

    /// <summary> Set teh forcefeedbackType of this collider. </summary>
    /// <param name="type"></param>
    public void SetForceFeedback(ForceFeedbackType type)
    {
        this.forceFeedback = type;
    }


    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

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
        if (touch != null) { touch.isTrigger = true; } //enure the touch collider is always kinematic.

        if (this.touchedObject != null && !this.touchedObject.activeInHierarchy)
        {
            this.touchedObject = null;
            this.motorLevel = 0;
            this.dist = 0;
        }
    }


    public void Setup(SenseGlove_HandModel parentModel)
    {
        this.handModel = parentModel;

        Rigidbody RB = this.gameObject.GetComponent<Rigidbody>();
        if (RB == null)
        {
            RB = this.gameObject.AddComponent<Rigidbody>();
        }
        RB.isKinematic = true;
        RB.useGravity = false;
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection / Force Feedback 

    // Called when this object enters the collider of another object
    void OnTriggerEnter(Collider col)
    {
        SenseGlove_Interactable interactable = col.GetComponent<SenseGlove_Interactable>();
        if (interactable != null && interactable.forceFeedback)
        {
            if (!this.IsTouching(col.gameObject))
            {
                //touching a new object!
            }
            this.touchedObject = col.gameObject;
            if (this.forceFeedback == ForceFeedbackType.Simple)
            {
                SenseGlove_Material material = col.GetComponent<SenseGlove_Material>();
                if (this.forceFeedback == ForceFeedbackType.Simple)
                {
                    if (material)
                    {
                        this.motorLevel = material.passiveForce;
                    }
                    else
                    {
                        this.motorLevel = SenseGlove_Material.defaultPassiveForce;
                    }
                }
            }
            else if (this.forceFeedback == ForceFeedbackType.MaterialBased)
            {
                this.entryPos = this.touch.transform.position;
                Vector3 closest = col.ClosestPoint(this.entryPos); //if something went wrong with ClosestPoint, it returns the entryPos.
                if (!closest.Equals(this.entryPos)) { this.entryPoint = closest; }
                else
                {
                    Debug.Log("WARNING: ClosestPoint == Origin, resulting in a DIV0 exception. Use an alterantive method?");
                    this.entryPoint = closest;
                }
                this.motorLevel = 0; //still 0 since OP == EO
            }
        }
    }

    // Called every FixedUpdate while this collider is inside another collider.
    void OnTriggerStay(Collider col)
    {
        if (this.IsTouching(col.gameObject)) //Check if we're still on the same object?
        {
            //Calculate Motor Level
            SenseGlove_Material material = col.GetComponent<SenseGlove_Material>();
            if (this.forceFeedback == ForceFeedbackType.Simple)
            {
                if (material)
                {
                    this.motorLevel = material.passiveForce;
                }
                else
                {
                    this.motorLevel = SenseGlove_Material.defaultPassiveForce;
                }
            }
            else if (this.forceFeedback == ForceFeedbackType.MaterialBased)
            {
                //transform the position of the SenseGlove_Touch to the 

                //O touch point when it was created
                //E touch point on the collider
                //P current finger position

                Vector3 OE = this.entryPoint - this.entryPos;
                Vector3 OP = this.transform.position - this.entryPos;

                Debug.DrawLine(this.entryPos, this.transform.position);
                Debug.DrawLine(this.entryPos, this.entryPoint);


                if (OE.magnitude == 0) // If OE.magnitude is 0, then something went wrong with Collider.ClosestPoint, which returns O.
                {
                    //check if we are outside of the collider now...
                    Vector3 thisPos = this.transform.position;
                    Vector3 clostest = col.ClosestPoint(thisPos);
                    if (!thisPos.Equals(clostest))
                    {
                        this.entryPoint = clostest;
                        this.entryPos = thisPos;
                    }
                    return; //try again next frame
                }

                if (OP.magnitude == 0) // If OP.magnitude is 0, then then 0 == P, meaning we are back onto the entry position. in that case this.dist = 0.
                {
                    this.dist = 0;
                }
                else
                {
                    float cos = Vector3.Dot(OE, OP) / (OE.magnitude * OP.magnitude); ;
                    this.dist = OP.magnitude * cos;
                }

                if (material)
                {
                    this.motorLevel = material.CalculateForce(this.dist);
                }
                else
                {
                    this.motorLevel = SenseGlove_Material.CalculateDefault(this.dist);
                }

            }
        }
    }

    // Called when this object exits the collider of another object
    void OnTriggerExit(Collider col)
    {
        if (this.touchedObject != null && this.IsTouching(col.gameObject))
        {
            this.touchedObject = null;
            this.motorLevel = 0;
            this.dist = 0;
        }
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Touch Logic

    /// <summary> Check if this SenseGlove_Touch is touching object obj. </summary>
    /// <param name="obj"></param>
    public bool IsTouching(GameObject obj)
    {
        if (this.touchedObject != null && obj != null)
        {
            bool t = GameObject.ReferenceEquals(this.touchedObject, obj);
            //return this.touchedObject.Equals(obj);
            //return this.touchedObject == obj;
            //SenseGlove_Debugger.Log("The thumb is touching " + touchedObject.name + " and Index is touching " + obj.name + ". Equals = " + t);
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

}
