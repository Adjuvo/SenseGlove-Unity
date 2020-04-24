using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    /// <summary> A Grabscript that uses a number of the Sense Glove's data to start and end interactions. </summary>
    public abstract class SG_GrabScript : MonoBehaviour
    {
        /// <summary> Event Arguments for grabbing/releasing of objects. </summary>
        public class SG_GrabEventArgs : System.EventArgs
        {
            /// <summary> The object that is being grabbed or released </summary>
            public SG_Interactable Interactable { get; protected set; }

            public SG_GrabEventArgs(SG_Interactable obj)
            {
                Interactable = obj;
            }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Grabscript Options

        #region Properties


        /// <summary> A SG_SenseGloveHardware for gloveData related shenanigans. </summary>
        [Header("Linked Scripts")]
        [Tooltip("A SG_SenseGloveHardware for gloveData related shenanigans.")]
        public SG_SenseGloveHardware hardware;

        /// <summary> When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement / parent / fixedJoint. </summary>
        [Header("Settings")]
        [Tooltip("When an object is picked up, this GameObject (Typically the wrist) is used as a reference for its movement.")]
        public GameObject grabReference;

        /// <summary> A Rigidbody that is used as an anchor when interacting with an object via a FixedJoint. </summary>
        [Tooltip("A Rigidbody that is used as an anchor when interacting with an object via a FixedJoint.")]
        public Rigidbody grabAnchor;

        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Interaction Variables

        /// <summary> Becomes true after the colliders have been succesfully assigned. </summary>
        protected bool setupFinished = false;

        /// <summary> The object(s) that are being held by this script. </summary>
        protected List<SG_Interactable> heldObjects = new List<SG_Interactable>(2);


        /// <summary> The velocity during the previous frames. </summary>
        protected List<Vector3> velocities = new List<Vector3>();
        /// <summary> The angular velocity during the previous frames. </summary>
        protected List<Vector3> angularVelocities = new List<Vector3>();

        /// <summary> The grabReference's position during the last frame. </summary>
        protected Vector3 lastPosition = Vector3.zero;
        /// <summary> The grabReference's rotation during the last frame. </summary>
        protected Quaternion lastRotation = Quaternion.identity;

        /// <summary> The maximum frames for which to keep track of velocities. </summary>
        protected static int maxDataPoints = 5;


        /// <summary> If paused, the GrabScript will no longer raise events or grab objects untill the pauseTime has elapsed. </summary>
        protected bool paused = false;

        /// <summary> The time [s] that needs to elapse before the GrabScript can pick up another object. </summary>
        protected float pauseTime = 1.0f;

        /// <summary> The amount of time that has elpased since the Manual Release function was called. </summary>
        protected float elapsedTime = 0;

        #endregion Properties

        /// <summary> Show/Hide the debug elements (colliders, DrawLines) of this GrabScript. </summary>
        public virtual bool DebugEnabled
        {
            set { }
        }


        /// <summary> The TrackedHand this GrabScript is connected to, used to access animation, hardware, etc. </summary>
        public SG_TrackedHand Hand
        {
            get; protected set;
        }


        /// <summary> Returns true if this GrabScript is connected to Hardware that is ready to go </summary>
        public virtual bool HardwareReady
        {
            get { return this.hardware != null && this.hardware.GloveReady; }
        }


        /// <summary> Returns true if this GrabScript is connected to Sense Glove Hardware and returns a refernce to it. Used in an if statement for safety </summary>
        /// <param name="hardware"></param>
        /// <returns></returns>
        public virtual bool GetHardware(out SG_SenseGloveHardware hardware)
        {
            if (HardwareReady)
            {
                hardware = this.hardware;
                return hardware != null;
            }
            hardware = null;
            return false;
        }


        /// <summary> Check for relevant scripts connected to this one, that may not yet have been assigned. </summary>
        public virtual void CheckForScripts()
        {
            if (this.Hand == null)
            {
                if (this.hardware != null) { this.Hand = this.hardware.GetComponent<SG_TrackedHand>(); }
                if (this.Hand == null) { this.Hand = this.GetComponentInParent<SG_TrackedHand>(); }
            }
            if (this.hardware == null)
            {
                if (this.Hand != null && this.Hand.hardware != null) { this.hardware = this.Hand.hardware; }
                else if (this.Hand != null) { this.hardware = this.Hand.gameObject.GetComponent<SG_SenseGloveHardware>(); }
                if (this.hardware == null)
                {
                    this.hardware = this.gameObject.GetComponent<SG_SenseGloveHardware>();
                }
            }
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Dynamics Methods

        #region Dynamics

        /// <summary> Update the dynamics (velocity, angular velocity) of the grabreference. </summary>
        protected virtual void UpdateDynamics()
        {
            Vector3 currPos = this.grabReference != null ? this.grabReference.transform.position : this.transform.position;
            Quaternion currRot = this.grabReference != null ? this.grabReference.transform.rotation : this.transform.rotation;

            Vector3 velocity = (currPos - lastPosition) / Time.deltaTime;
            Vector3 angularVelocity = SG_Util.CalculateAngularVelocity(currRot, lastRotation, Time.deltaTime);

            this.velocities.Add(velocity);
            this.angularVelocities.Add(angularVelocity);
            if (velocities.Count > SG_GrabScript.maxDataPoints)
            {
                this.velocities.RemoveAt(0);
                this.angularVelocities.RemoveAt(0);
            }

            lastPosition = currPos;
            lastRotation = currRot;
        }


        /// <summary> Retrieve the Velocity of this Grabscript in m/s </summary>
        /// <returns></returns>
        public Vector3 GetVelocity()
        {
            return SG_Util.Average(this.velocities);
        }

        /// <summary> Retrieve the angular velocity of this Grabscript in rad/s </summary>
        /// <returns></returns>
        public Vector3 GetAngularVelocity()
        {
            return SG_Util.Average(this.angularVelocities);
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
        protected virtual bool CanRelease(SG_Interactable obj)
        {
            return true;
        }

        /// <summary> Returns true if this grabscript can currently pickup an object </summary>
        /// <returns></returns>
        public abstract bool CanInteract();

        /// <summary> Return a list of GameObjects that this script is Currently Interacting with. </summary>
        /// <returns></returns>
        public virtual SG_Interactable[] HeldObjects()
        {
            SG_Interactable[] objects = new SG_Interactable[this.heldObjects.Count];
            for (int i = 0; i < this.heldObjects.Count; i++)
            {
                objects[i] = this.heldObjects[i];
            }
            return objects;
        }

        /// <summary> Returns true if this grabscript is currently holding an object </summary>
        public virtual bool IsGrabbing()
        {
            return heldObjects.Count > 0;
        }

        /// <summary> Returns true if this GrabScript is grabbing a specific SG_Interactable. </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool IsGrabbing(SG_Interactable obj)
        {
            for (int i = 0; i < this.heldObjects.Count; i++)
            {
                if (GameObject.ReferenceEquals(obj.gameObject, heldObjects[i].gameObject))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>  Returns true if the grabscript is touching an object </summary>
        /// <returns></returns>
        public abstract bool IsTouching();

        /// <summary> Remove any references to held objects, restoring the GrabScript as though it has not touched anything yet. </summary>
        public virtual void ClearHeldObjects()
        {
            for (int i = 0; i < heldObjects.Count; i++)
            {
                heldObjects[i].EndInteraction();
            }
            heldObjects.Clear();
        }

        /// <summary> Update the Grabscript logic; called automatically every Update() frame </summary>
        public abstract void UpdateGrabScript();

        /// <summary> If this grabscript is holding obj, end its interaction with it. </summary>
        /// <param name="obj"></param>
        /// <param name="callEvent">Call the EndInteraction on this object.</param>
        public virtual void EndInteraction(SG_Interactable obj)
        {
            if (obj != null)
            {
                for (int i = 0; i < this.heldObjects.Count; i++)
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


        /// <summary> Event Handler for grabbing/releasing objects </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public delegate void GrabEventHandler(object source, SG_GrabEventArgs args);

        /// <summary> Fires when a SG_GrabScript's grabs an object. </summary>
        public event GrabEventHandler ObjectGrabbed;
        /// <summary> Fires when a SG_GrabScript's releases an object. </summary>
        public event GrabEventHandler ObjectReleased;

        /// <summary> Calls the ObjectGrabbed event </summary>
        /// <param name="obj"></param>
        protected void OnGrabbedObject(SG_Interactable obj)
        {
            if (ObjectGrabbed != null) { ObjectGrabbed(this, new SG_GrabEventArgs(obj)); }
        }

        /// <summary> Calls an ObjectReleased event. </summary>
        /// <param name="obj"></param>
        protected void OnReleasedObject(SG_Interactable obj)
        {
            if (ObjectReleased != null) { ObjectReleased(this, new SG_GrabEventArgs(obj)); }
        }



        /// <summary> Attempt to grab an Interactable. If succesful, fire the ObjectGrabbed event. </summary>
        /// <param name="obj"></param>
        protected virtual void TryGrabObject(SG_Interactable obj)
        {
            if (!obj.MustBeReleased() && !IsGrabbing(obj))
            {
                bool grabbed = obj.BeginInteraction(this);
                if (grabbed)
                {
                    this.heldObjects.Add(obj);
                    this.OnGrabbedObject(obj);
                }
            }
        }

        /// <summary> Attempt to release an Interactable in heldObjects. If succesful, fire the ObjectReleased event. </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected virtual int ReleaseObjectAt(int index) //returns new index;
        {
            bool released = false;
            if (heldObjects[index] != null)
            {
                if (heldObjects[index].MustBeReleased() || (this.CanRelease(heldObjects[index]) && heldObjects[index].EndInteractAllowed()))
                {
                    released = heldObjects[index].EndInteraction(this);
                }
            }
            else { released = true; } //the object is null, so remove it from the list anyway

            if (released)
            {
                SG_Interactable hObj = heldObjects[index]; //grab a ref brefore it is removed?
                heldObjects.RemoveAt(index);
                if (hObj != null) { this.OnReleasedObject(hObj); } //only fire if the object is not null
            }
            else { index++; }

            return index;
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        //Load Resources before Start() function is called
        protected virtual void Awake()
        {
            if (this.Hand == null)
            {
                this.Hand = SG_Util.CheckForTrackedHand(this.transform);
            }
        }

        //Runs once after Awake
        protected virtual void Start()
        {
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
                    this.heldObjects[f].GetComponent<SG_Interactable>().UpdateInteraction();
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


#if UNITY_EDITOR
        void OnValidate()
        {
            this.Hand = null;
            CheckForScripts();
        }
#endif

    }


}