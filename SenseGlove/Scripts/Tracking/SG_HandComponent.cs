using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Represents different sections of the hand, used to determine feedback or tracking location. </summary>
    public enum SG_HandSection
    {
        Thumb = 0,
        Index,
        Middle,
        Ring,
        Pinky,
        Wrist,
        Unknown
    }


    /// <summary> Represents a 'Layer' of the SG_TrackedHand, which has access to the other layers. 
    /// Most, if not all 'layers' extend from this component to allow easy access all the way throughout the 'tree'. </summary>
    public class SG_HandComponent : MonoBehaviour
    {

        //--------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Access the TrackedHand, through which one can send/receive SenseGlove data. It may also be assigned via the Inspector, but is overriden when using the LinkToHand function. </summary>
        [Header("Hand Layer Components")]
        public SG_TrackedHand TrackedHand;

        /// <summary> Used to determine if this is the first time this HandComponent is linked to a TrackedHand. </summary>
        private bool firstLink = true;
        /// <summary> Whether or not this hand's components still need to be created, so that's only done once. </summary>
        private bool createComponents = true;

        /// <summary> Controls whether or not Debug elements of this layer are enabled. Serialized so you can easily access it in the editor. </summary>
        [SerializeField] protected bool debugEnabled = false;

        //These are cached from the first time they're required.

        /// <summary> Cached Physics Colliders, used to Ignore Collisions. </summary>
        protected Collider[] physicsColliders = null;
        /// <summary> Cached Objects to fully enable / disable based on debugEnabled. </summary>
        private GameObject[] debugObjects = null;
        /// <summary> Cached Renderers to fully enable / disable based on debugEnabled.</summary>
        private MeshRenderer[] debugRenderers = null;


        //--------------------------------------------------------------------------------------------------------------------------
        // Access to the other layers

        
        /// <summary> Gain access to the Physics Layer attached to this layer's TrackedHand. </summary>
        public virtual SG_HandPhysics HandPhysics
        {
            get { return this.TrackedHand != null ? TrackedHand.handPhysics : null; }
        }


        /// <summary> Gain access to the GrabScript attached to this layer's TrackedHand. </summary>
        public virtual SG_GrabScript GrabScript
        {
            get { return this.TrackedHand != null ? TrackedHand.grabScript : null; }
        }

        /// <summary> Gain access to the GrabScript attached to this layer's TrackedHand. </summary>
        public virtual SG_HandFeedback FeedbackLayer
        {
            get { return this.TrackedHand != null ? TrackedHand.feedbackLayer : null; }
        }

        /// <summary> Gain access to the GrabScript attached to this layer's TrackedHand. </summary>
        public virtual SG_HandProjection ProjectionLayer
        {
            get { return this.TrackedHand != null ? TrackedHand.projectionLayer : null; }
        }




        //--------------------------------------------------------------------------------------------------------------------------
        // Linking / Setup Functions

        /// <summary> Link this component to a (new) trackedHand. Some guarding in place to make sure we don't call this function when it's not needed. </summary>
        /// <param name="newHand"></param>
        public void LinkToHand(SG_TrackedHand newHand, bool forceLink = false)
        {
            if (this.TrackedHand == null || newHand != this.TrackedHand || forceLink) //safegaurd
            {
                SetupComponents(); //make sure the layer has it's components already.
                LinkToHand_Internal(newHand, this.firstLink);
               // Debug.Log(this.name + "(" + (this.TrackedHand != null ? (this.TrackedHand.TracksRightHand() ? "R" : "L") : "BEFORE LINK") + "): Linked to " + this.TrackedHand.name);
                firstLink = false;
            }
        }

        /// <summary> Actually (Re)Link this Hand Layer to a new Tracked Hand. Setup all tracking accordingly. Other layers override this function. </summary>
        /// <param name="newHand"></param>
        /// <param name="firstLink">Is only true the first time a link is called.</param>
        protected virtual void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
            this.TrackedHand = newHand;
        }


        /// <summary> Create any missing components if we haven't already. Called during Awake by default </summary>
        public void SetupComponents()
        {
            if (createComponents)
            {
               // Debug.Log(this.name + "(" + (this.TrackedHand != null ? ( this.TrackedHand.TracksRightHand() ? "R" : "L" ) : "BEFORE LINK") + "): Setup.");
                CreateComponents();
                createComponents = false;
            }
        }

        /// <summary> Check if all components are there and create any missing ones.
        /// Called once by SetupComponents. DO NOT call this function from MonoBehaviour. Use SetupComponents instead. </summary>
        protected virtual void CreateComponents() { }
        

        /// <summary> Updates the location of a collider based on a HandPose and its preferred tracking location. </summary>
        /// <param name="pose"></param>
        /// <param name="colliderTracking"></param>
        public static void UpdateColliderLocation(SG_HandPose pose, SG_SimpleTracking colliderTracking)
        {
            int finger; int jointIndex;
            SG_HandPoser3D.ToFinger(colliderTracking.linkMeTo, out finger, out jointIndex);
            //we now know which Transform we're linked to.
            Vector3 jointPos = pose.jointPositions[finger][jointIndex]; //relative to the wrist. Wrist is (this).
            Quaternion jointRot = pose.jointRotations[finger][jointIndex];

            Vector3 localPos; Quaternion localRot;
            Util.SG_Util.CalculateTargetLocation(jointPos, jointRot, colliderTracking.PosOffset, colliderTracking.RotOffset, out localPos, out localRot);

            //and finally, calculate the actiual position in world space? Wait, I don't have to. Not if I consider the layer to be the wrist and these are my children...
            colliderTracking.transform.rotation = pose.wristRotation * localRot;
            colliderTracking.transform.position = pose.wristPosition + (pose.wristRotation * localPos);
        }


        //--------------------------------------------------------------------------------------------------------------------------
        // Debug Elements


        /// <summary> Whether or not a debug elements are turned on/off. </summary>
        public bool DebugEnabled
        {
            get 
            {
                return debugEnabled;
            }
            set 
            {
                CheckDebugComponents();
                debugEnabled = value;
                if (debugObjects != null)
                {
                    for (int i = 0; i < debugObjects.Length; i++)
                    {
                        debugObjects[i].SetActive(debugEnabled);
                    }
                    for (int i = 0; i < debugRenderers.Length; i++)
                    {
                        debugRenderers[i].enabled = debugEnabled;
                    }
                }
            }
        }

        /// <summary> Collect Debug Components if you have not done so already. </summary>
        private void CheckDebugComponents()
        {
            if (!createComponents && (this.debugObjects == null || this.debugRenderers == null)) //we've created the components.
            {
                List<GameObject> objs; List<MeshRenderer> rends;
                CollectDebugComponents(out objs, out rends);
                this.debugObjects = objs.ToArray();
                this.debugRenderers = rends.ToArray();
                // Debug.Log(this.name + ": Collected Debugging Components (" + debugObjects.Length + " objects, " + debugRenderers.Length + " renderers).");
            }
        }

        /// <summary> Collect debug elements by adding them to the list. Called once, either during awake or when DebugEnabled is called first. </summary>
        /// <param name="objects"></param>
        /// <param name="renderers"></param>
        protected virtual void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            objects = new List<GameObject>();
            renderers = new List<MeshRenderer>();
        }


        //--------------------------------------------------------------------------------------------------------------------------
        // Accessors

        /// <summary> Check if this trackedhand is meant for a right hand. </summary>
        public virtual bool IsRight
        {
            get { return this.TrackedHand != null && this.TrackedHand.TracksRightHand(); }
        }


        //--------------------------------------------------------------------------------------------------------------------------
        // Physics - Ignoring Collisions between layers.


        /// <summary> Retrieve this layer's PhysicsColliders, so that we can affect their transforms, layers, and collision. </summary>
        /// <returns></returns>
        public Collider[] GetPhysicsColliders()
        {
            if (this.physicsColliders == null)
            {
                this.physicsColliders = CollectPhysicsColliders().ToArray();
            }
            return this.physicsColliders;
        }

        /// <summary> Expensive operation. Do this once. </summary>
        /// <returns></returns>
        protected virtual List<Collider> CollectPhysicsColliders() //List is easier to add to.
        {
            return new List<Collider>();
        }


        /// <summary> Expensive Operation. Do this once. </summary>
        /// <param name="otherLayer"></param>
        /// <param name="collisionEnabled"></param>
        public void SetIgnoreCollision(SG_HandComponent otherLayer, bool ignoreCollision)
        {
            if (otherLayer != null)
            {
                Collider[] otherColliders = otherLayer.GetPhysicsColliders();
                if (otherColliders.Length > 0)
                {
                    //Debug.Log((ignoreCollision ? "Disabling" : "Enabling") + " collision between "
                    //    + this.name + " (" + myColliders.Length + " Collider(s)) and "
                    //    + otherLayer.name + " (" + otherColliders.Length + " Collider(s))");
                    SetIgnoreCollision(otherColliders, ignoreCollision);
                }
            }
        }

        /// <summary> Ignore collision between these colliders </summary>
        /// <param name="colliders"></param>
        /// <param name="ignoreCollision"></param>
        public void SetIgnoreCollision(Collider[] colliders, bool ignoreCollision)
        {
            Collider[] myColliders = this.GetPhysicsColliders();
            if (myColliders.Length > 0)
            {
                //Tell each of my colliders to ignore each of the other colliders.
                for (int i = 0; i < myColliders.Length; i++)
                {
                    for (int j = 0; j < colliders.Length; j++)
                    {
                        if (myColliders[i] != null && colliders[j] != null) //these can be NULL if the scene is shutting down!
                        {
                            Physics.IgnoreCollision(myColliders[i], colliders[j], ignoreCollision);
                        }
                    }
                }
            }
        }
 


        //--------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        public virtual void Awake()
        {
            SetupComponents();
            DebugEnabled = this.debugEnabled; //set debug components
        }

        public virtual void OnApplicationQuit()
        {
            SG.Util.SG_Util.IsQuitting = true;
        }

#if UNITY_EDITOR
        public virtual void OnValidate()
        {

            if (Application.isPlaying)
            {
                DebugEnabled = this.debugEnabled;
            }
        }
#endif

    }
}