using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A Grabscript that uses a number of the Sense Glove's data to start and end interactions. </summary>
public abstract class SenseGlove_GrabScript : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Grabscript Options

    #region Properties

    /// <summary> A SenseGlove_Object for gloveData related shenanigans. </summary>
    [Tooltip("A SenseGlove_Object for gloveData related shenanigans.")]
    public SenseGlove_Object senseGlove;

    /// <summary> The handModel connected to this GrabScript. </summary>
    [Tooltip("The handModel connected to this GrabScript")]
    public SenseGlove_HandModel handModel;

    /// <summary> When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement / parent / fixedJoint. </summary>
    [Header("Settings")]
    [Tooltip("When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement.")]
    public GameObject grabReference;

    /// <summary> A Rigidbody that is used as an anchor when interacting with an object via a FixedJoint. </summary>
    [Tooltip("A Rigidbody that is used as an anchor when interacting with an object via a FixedJoint.")]
    public Rigidbody grabAnchor;

    /// <summary> If set to true, the position and rotation of the grabscript must be stable before one can start / end interaction. </summary>
    public bool checkStability = false;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Interaction Variables

    /// <summary> Becomes true after the colliders have been succesfully assigned. </summary>
    protected bool setupFinished = false;

    /// <summary> The object(s) that are being held by this script. </summary>
    protected List<SenseGlove_Interactable> heldObjects = new List<SenseGlove_Interactable>(2);

    /// <summary> The grabReference's position during the last frame Update() </summary>
    protected Vector3 lastPosition;

    /// <summary> The grabReference's rotation during the last frame Update() </summary>
    protected Quaternion lastRotation;

    /// <summary> The xyz velocity of the grabreference, in m/s </summary>
    protected Vector3 velocity = Vector3.zero;

    /// <summary> Angilar velocity in rad/sec </summary>
    protected Vector3 angularVelocity = Vector3.zero;

    /// <summary> Whether there is consistent tracking on the grabreference (with regards to the positional tracking messing up) </summary>
    protected bool isStable = true;

    /// <summary> If the glove's velocity reaches above this number, the tracking is considered unstable. </summary>
    protected float velocityThreshold = 5;

    /// <summary> If the glove's angularVelocity reaches above this number, the tracking is considered unstable.  </summary>
    protected float angleVelocityThreshold = 25;



    /// <summary> If paused, the GrabScript will no longer raise events or grab objects untill the pauseTime has elapsed. </summary>
    protected bool paused = false;

    /// <summary> The time [s] that needs to elapse before the GrabScript can pick up another object. </summary>
    protected float pauseTime = 1.0f;

    /// <summary> The amount of time that has elpased since the Manual Release function was called. </summary>
    protected float elapsedTime = 0;

    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Dynamics Methods

    #region Dynamics

    /// <summary> Update the dynamics (velocity, angular velocity) of the grabreference. </summary>
    protected void UpdateDynamics()
    {
        if (this.grabReference != null)
        {
            Vector3 currentPos = this.grabReference.transform.position;
            this.velocity = (currentPos - lastPosition) / Time.deltaTime;

            Quaternion currentRot = this.grabReference.transform.rotation;
            this.angularVelocity = SenseGlove_Util.CalculateAngularVelocity(currentRot, this.lastRotation);

            if (this.checkStability)
            {
                this.isStable = (this.velocity.magnitude <= this.velocityThreshold || angularVelocity.magnitude <= this.angleVelocityThreshold); 
            }
            else
            {
                this.isStable = true;
            }
            this.lastPosition = currentPos;
            this.lastRotation = currentRot;
        }
    }

    /// <summary> Manually set this grabscript to be stable </summary>
    /// <param name="stable"></param>
    public void SetStable(bool stable)
    {
        this.isStable = stable;
    }

    /// <summary> Check whether or not the grabscript's grabreference is considered 'stable'. </summary>
    /// <returns></returns>
    public bool IsStable()
    {
        return this.isStable;
    }

    /// <summary> Retrieve the Velocity of this GameObject </summary>
    /// <returns></returns>
    public Vector3 GetVelocity()
    {
        return this.velocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return this.angularVelocity;
    }

    #endregion Dynamics

    

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Grabscript Methods

    #region GrabMethods

    // Internal

    /// <summary> Run setup on this grabscript; creating and/or resizing the proper colliders etc. </summary>
    /// <returns></returns>
    public abstract bool Setup();

    // External

    /// <summary> Manually force the SenseGlove_PhysGrab to drop whatever it is holding. </summary>
    /// <param name="time">The amount of time before the Grabscript can pick up objects again </param>
    public virtual void ManualRelease(float timeToReactivate = 1.0f)
    {
        //release all objects
        for (int i = 0; i < this.heldObjects.Count; i++)
        {
            this.heldObjects[i].EndInteraction(this);
        }
        this.heldObjects.Clear();
    }

    /// <summary> Check if this GrabScript is allowed to release an object, based on its release parameters. </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected virtual bool CanRelease(SenseGlove_Interactable obj)
    {
        return true;
    }

    /// <summary> Returns true if this grabscript can currently pickup an object </summary>
    /// <returns></returns>
    public abstract bool CanInteract();

    /// <summary> Return a list of GameObjects that this script is Currently Interacting with. </summary>
    /// <returns></returns>
    public virtual SenseGlove_Interactable[] HeldObjects()
    {
        SenseGlove_Interactable[] objects = new SenseGlove_Interactable[this.heldObjects.Count];
        for (int i = 0; i < this.heldObjects.Count; i++)
        {
            objects[i] = this.heldObjects[i];
        }
        return objects;
    }

    /// <summary> Returns true if this grabscript is currently holding an object </summary>
    public abstract bool IsGrabbing();

    /// <summary>  Returns true if the grabscript is touching an object </summary>
    /// <returns></returns>
    public abstract bool IsTouching();

    /// <summary> Remove any references to held objects, restoring the GrabScript as though it has not touched anything yet. </summary>
    public abstract void ClearHeldObjects();

    /// <summary> Update the Grabscript logic; called automatically every Update() frame </summary>
    public abstract void UpdateGrabScript();

    /// <summary> If this grabscript is holding obj, end its interaction with it. </summary>
    /// <param name="obj"></param>
    /// <param name="callEvent">Call the EndInteraction on this object.</param>
    public virtual void EndInteraction(SenseGlove_Interactable obj)
    {
        if (obj != null)
        {
            for (int i=0; i<this.heldObjects.Count; i++)
            {
                if (this.heldObjects[i].Equals(obj))
                {
                    //we have this object
                    this.heldObjects.RemoveAt(i); //remove refrences to this.
                    this.heldObjects[i].EndInteraction(this, true);
                    break;
                }
            }
        }
    }



    #endregion GrabMethods



    /// <summary>
    /// Get the SenseGlove_Touch of a specified collider. If none is present, create a new one.
    /// Then apply the desired settings. Returns null if no collider exists.
    /// </summary>
    /// <param name="C"></param>
    /// <returns></returns>
    protected static SenseGlove_Touch GetTouchScript(Collider C, SenseGlove_GrabScript grabScript)
    {
        if (C != null)
        {
            C.isTrigger = true;
            SenseGlove_Touch script = C.gameObject.GetComponent<SenseGlove_Touch>();
            if (script == null)
            {
                script = C.gameObject.AddComponent<SenseGlove_Touch>();
            }
            script.touch = C;
            script.SetSourceScript(grabScript);

            //also add a rigidbody
            Rigidbody RB = C.gameObject.GetComponent<Rigidbody>();
            if (RB == null)
            {
                RB = C.gameObject.AddComponent<Rigidbody>();
            }
            RB.useGravity = false;
            RB.isKinematic = true;

            return script;
        }
        return null;
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    //Load Resources before Start() function is called
    protected virtual void Awake()
    {
        if (this.senseGlove == null)
        {
            this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        }
        if (this.handModel == null)
        {
            this.handModel = this.gameObject.GetComponent<SenseGlove_HandModel>();
        }
        if (this.grabReference != null)
        {
            this.lastPosition = grabReference.transform.position;
        }
    }

    //Runs once after Awake
    protected virtual void Start()
    {
        if (this.senseGlove == null)
        {
            this.senseGlove = this.GetComponent<SenseGlove_Object>();
        }
        this.Setup();
    }

    //Runs once every frame
    protected virtual void Update()
    {
        this.UpdateDynamics();
        if (this.setupFinished)
        {
            if (this.paused)
            {
                if (this.elapsedTime < this.pauseTime) { this.elapsedTime += Time.deltaTime; }
                else { this.paused = false; }
            }
            else
            {
                this.UpdateGrabScript();
            }
        }
    }

    //runs after all trasforms have been updated
    protected virtual void LateUpdate()
    {
        for (int f = 0; f < heldObjects.Count; f++)
        {
            //follow interactions on all follow interaction
            if (this.heldObjects[f] != null)
            {
                this.heldObjects[f].GetComponent<SenseGlove_Interactable>().UpdateInteraction();
            }
        }
    }

    // When the script is disabled, release objects.
    protected virtual void OnDisable()
    {
        if (this.setupFinished)
        {
            this.ManualRelease(0.1f);
        }
    }

}


