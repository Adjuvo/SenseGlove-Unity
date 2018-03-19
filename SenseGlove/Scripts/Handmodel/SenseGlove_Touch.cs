using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Uses colliders to detect which object(s) can be touched (and ultimately picked up) by the SenseGlove_PhysGrab. </summary> 
[RequireComponent(typeof(Collider))] //ensure the other component has a collider.
public class SenseGlove_Touch : MonoBehaviour
{

    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    [Tooltip("The collider used to determine which object is currently being touched")]
    public Collider touch;

    /// <summary> The object that is currently touched by this SenseGlove_Touch script. </summary>
    [Tooltip("The object that is currently touched by this SenseGlove_Touch script.")]
    public GameObject touchedObject;

    /// <summary> The grabscript using these colliders for its logic. Only PhysGrab uses these. </summary>
    private SenseGlove_GrabScript grabScript;
    
    /// <summary> Whether or not the collider 'should'be shown. </summary>
    private bool showCollider = false;

    /// <summary> A visual representation of the Collider. </summary>
    private GameObject debugVisual;

    /// <summary> The method to determine when to show the debugVisual. </summary>
    private PickupDebug debugLvl = PickupDebug.Off;

    #endregion Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Get / Set attributes for Construction

    /// <summary> Tell the SenseGlove_Touch that is is being used by the specified SenseGlove_PhysGrab. Copy its ForceFeedbackType. </summary>
    /// <param name="parentScript"></param>
    public void SetSourceScript(SenseGlove_GrabScript parentScript)
    {
        this.grabScript = parentScript;
    }

    /// <summary> Get a reference to the grabscript that this SenseGlove_Touch is attached to. </summary>
    /// <returns></returns>
    public SenseGlove_GrabScript GrabScript()
    {
        return this.grabScript;
    }

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    //Colled when the application starts
    void Start()
    {
        if (this.touch == null) { this.touch = this.GetComponent<Collider>(); }
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
            //SenseGlove_Debugger.Log("Object no longer exists. Releasing.");
            this.SetDebug(false);
            this.touchedObject = null;
        }

        //check debug logic
        if (this.grabScript != null && this.grabScript is SenseGlove_PhysGrab)
        {
            if (((SenseGlove_PhysGrab)this.grabScript).debugMode != this.debugLvl)
            {
                this.debugLvl = ((SenseGlove_PhysGrab)this.grabScript).debugMode;
                if (this.debugLvl == PickupDebug.Off)
                {
                    this.SetDebug(false);
                }
                else if (this.debugLvl == PickupDebug.AlwaysOn)
                {
                    this.SetDebug(true);
                }
                else if (this.debugLvl == PickupDebug.ToggleOnTouch)
                {
                    this.SetDebug(this.touchedObject != null);
                }
            }
        }

    }

   

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection / Force Feedback 

    // Called when this object enters the collider of another object
    void OnTriggerEnter(Collider col)
    {
        if (col.GetComponent<SenseGlove_Interactable>() != null && this.touchedObject == null)
        {
            //if (!this.IsTouching(col.gameObject))
            //{
            //    //touching a new object!
            //}
            this.touchedObject = col.gameObject;
            if (this.debugLvl == PickupDebug.ToggleOnTouch) { this.SetDebug(true); }
        }
    }

    // Called every FixedUpdate while this collider is inside another collider.
    void OnTriggerStay(Collider col)
    {
    }

    // Called when this object exits the collider of another object
    void OnTriggerExit(Collider col)
    {
        if (this.IsTouching(col.gameObject))
        {
            this.touchedObject = null;
            if (this.debugLvl == PickupDebug.ToggleOnTouch) { this.SetDebug(false); }
        }
    }

    #endregion Monobehaviour

    //--------------------------------------------------------------------------------------------------------------------------
    // Touch Logic

    #region TouchLogic

    /// <summary> Check if this SenseGlove_Touch is touching any objects </summary>
    /// <returns></returns>
    public bool IsTouching()
    {
        return this.touchedObject != null;
    }

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

    /// <summary> Check if two SenseGlove_Touch scripts are touching the same object </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsTouching(SenseGlove_Touch other)
    {
        if (other == null)
        {
            SenseGlove_Debugger.Log("Other is Null");
        } else
        if (this.touchedObject != null && other.touchedObject != null)
        {
            return GameObject.ReferenceEquals(this.touchedObject, other.touchedObject);
        }
        return false;
    }

    /// <summary> Returns the object that this collider is touching. Usually to check if it is also touched by other colliders. </summary>
    /// <returns></returns>
    public GameObject TouchObject()
    {
        return this.touchedObject;
    }

    /// <summary> Retrieve the Interactable script if an object is currently being touched. </summary>
    /// <returns></returns>
    public SenseGlove_Interactable TouchedScript()
    {
        if (this.touchedObject != null)
        {
            return this.touchedObject.GetComponent<SenseGlove_Interactable>();
        }
        return null;
    }

    #endregion TouchLogic

    //--------------------------------------------------------------------------------------------------------------------------
    // Debug

    #region Debug
    
    /// <summary> Create a primitive object representing this collider, that canbe turned on or off. </summary>
    /// <param name="debugColor"></param>
    public void CreateDebugObject(Color debugColor)
    {
        if (this.touch != null && this.debugVisual == null)
        {
            if (this.touch is CapsuleCollider)
            {
                this.debugVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }
            else if (this.touch is BoxCollider)
            {
                this.debugVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            else if (this.touch is SphereCollider)
            {
                this.debugVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            }

            if (this.debugVisual != null)
            {
                this.debugVisual.transform.parent = null;
                this.debugVisual.transform.position = this.touch.transform.position;
                this.debugVisual.transform.rotation = this.touch.transform.rotation;
                this.debugVisual.transform.localScale = this.touch.transform.lossyScale;
                this.debugVisual.transform.parent = this.touch.transform;
                this.debugVisual.GetComponent<Renderer>().material.color = debugColor;
                this.debugVisual.name = "Debug Collider";

                Collider C = this.debugVisual.GetComponent<Collider>();
                if (C != null)
                {
                    GameObject.Destroy(C);
                }
                this.debugVisual.SetActive(this.showCollider);
            }
        }
    }

    /// <summary> Set the debug collider to be active or inactive. </summary>
    /// <param name="active"></param>
    public void SetDebug(bool active)
    {
        this.showCollider = active;
        this.debugVisual.SetActive(this.showCollider);
       
    }

    /// <summary> Edit the Debug level of this SenseGlove_Touch </summary>
    /// <param name="debugLevel"></param>
    public void SetDebugLevel(PickupDebug debugLevel)
    {
        this.debugLvl = debugLevel;
    }

    #endregion Debug
}
