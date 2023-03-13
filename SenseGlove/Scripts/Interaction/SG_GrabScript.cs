using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Unity Event to handle object Grabbing / Releasing. </summary>
    [System.Serializable] public class SG_GrabbedObjectEvent : UnityEngine.Events.UnityEvent<SG_Interactable, SG_GrabScript> { }

    /// <summary> A layer to hover over and grab SG_Interactables. Can override hand tracking if a held object does. </summary>
	public abstract class SG_GrabScript : SG_HandComponent
	{
        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Cooldown parameters

        /// <summary> Used to keep track of interactables that we cannot grab for a few frames </summary>
        public class CooldownParams
        {
            public float cooldownTimer = 0;

            public SG_Interactable interactable;

            public CooldownParams(SG_Interactable obj)
            {
                interactable = obj;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> The GameObject from which to collect and IHandPoseProvider. Used for intention checking (does to user actually want to grab onto something?). </summary>
        [Header("Base GrabScript Components")]
        public GameObject handPoseSource;

		/// <summary> The Source of Hand Tracking for this GrabScript - used for intention detection (a.k.a. do I want to grab something). </summary>
		public IHandPoseProvider handPoseProvider;


		/// <summary> The real-world "MidPoint" of the hand; used to determine where to move the virtual hand while grabbing onto an object </summary>
		public Transform realGrabRefrence;

        /// <summary> The "MidPoint" of the virtual hand. Objects we grab will attempt to move so that this matches the real world refrence. 
        /// Should correspond to the virtual pose (affected by OnHover/Physics) location! </summary>
		public Transform virtualGrabRefrence;

        /// <summary> Required to keep the refrences at the correct location relative to their respective wrists. </summary>
        protected SG_SimpleTracking realRefTracking = null, virtualRefTracking = null;


        /// <summary> Collider used to determine hovering behaviour - overrides when you're hovering over something. Specifically linked to the virtual pose, not the real world one. Hence the name. </summary>
        public SG_HoverCollider virtualHoverCollider;

        /// <summary> A list of objects that are being held by this script. Try not to influence it directly, but use the TryGrab/TryRelease functions. </summary>
        [SerializeField] protected List<SG_Interactable> heldObjects = new List<SG_Interactable>();

        /// <summary> How many objects a GrabScript can hold onto at the same time. Ideally, it should stay at 1, because holding two objects that determine position could result in undefined behaviour. </summary>
        /// <remarks> If set to less than 0, it's ignored. If set to 0, you won't be able to grab something. </remarks>
        public int heldObjectLimit = 1;

		/// <summary> Optional Debug Element, which is used to show the heldObjects array. </summary>
		public TextMesh debugTxt;

        /// <summary> When linked to a TrackedHand, siad script detemines exactly when an update occurs. When unlinked, this script updates itself. </summary>
        protected bool updateSelf = true;

        /// <summary> Use the location of this Transform to determine which object is closest to the hand. </summary>
        protected Transform proximitySensor = null;

        /// <summary> The last object that was determined to be closest to this script. Used to unHighlight it. </summary>
        private SG_Interactable lastClosest = null;

        protected bool grabbingAllowed = true;

        /// <summary> Time after releasing an object, and before it can be grabbed again. </summary>
        protected static float objCooldownTime = 0.3f;
        /// <summary> Objects that we've just released and can therefore not be grabbed for a few frames. </summary>
        protected List<CooldownParams> objsOnCoolDown = new List<CooldownParams>();

        /// <summary> Fires just after this GrabScript grabs an object </summary>
        public SG_GrabbedObjectEvent GrabbedObject = new SG_GrabbedObjectEvent();

        /// <summary> Fires just after this GrabScript released an object </summary>
        public SG_GrabbedObjectEvent ReleasedObject = new SG_GrabbedObjectEvent();



        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // SG_HandComponent Overrides

        /// <summary> Retrieve the GrabScript linked to this hand.  </summary>
        /// <remarks> Tiny optimization as it would normally go through this.TrackedHand.grabScript. Now you can call GrabScript.GrabScript to your heart's content. </remarks>
        public override SG_GrabScript GrabScript
        {
            get { return this; }
        }

        /// <summary> Create any missing components if you haven't already. </summary>
        protected override void CreateComponents()
        {
            // Just in case you haven''t assigned one via code yet.
			if (handPoseProvider == null && this.handPoseSource != null)
            {
				handPoseProvider = this.handPoseSource.GetComponent<IHandPoseProvider>();
            }
            // Ensure a real hand refrence exists. Ideally, you've assigned this via the in
            if (realGrabRefrence == null)
            {
                GameObject refr = new GameObject("RealGrabReference");
                realGrabRefrence = refr.transform;
                realGrabRefrence.parent = this.transform;
                realGrabRefrence.localRotation = Quaternion.identity;
                realGrabRefrence.localPosition = Vector3.zero;
            }
            else
            {
                realGrabRefrence.parent = this.transform; //ensure it's part fo this layer.
            }
            realRefTracking = Util.SG_Util.TryAddComponent<SG_SimpleTracking>(this.realGrabRefrence.gameObject);

            // Ensure a virtual hand ref exists
            if (virtualGrabRefrence == null)
            {
                GameObject refr = new GameObject("VirtualGrabReference");
                realGrabRefrence = refr.transform;
            }
            virtualGrabRefrence.parent = this.transform;
            virtualGrabRefrence.rotation = realGrabRefrence.rotation;
            virtualGrabRefrence.position = realGrabRefrence.position;
            virtualRefTracking = Util.SG_Util.TryAddComponent<SG_SimpleTracking>(this.virtualGrabRefrence.gameObject);
            virtualRefTracking.SetTrackingTarget(virtualRefTracking.TrackingTarget, true); //re-calculate the offsets
        }

        /// <summary> Collect all PhysicsColliders that are part of this layer. Used to ignore any inwanted physics behaviours. </summary>
        /// <returns></returns>
        protected override List<Collider> CollectPhysicsColliders()
        {
            List<Collider> myColliders = base.CollectPhysicsColliders();
            if (this.virtualHoverCollider != null)
            {
                SG.Util.SG_Util.GetAllColliders(this.virtualHoverCollider.gameObject, ref myColliders);
            }
            return myColliders;
        }

        /// <summary> Collect all Debug Components relevant to this GrabScript. Disabled by default. </summary>
        /// <param name="objects"></param>
        /// <param name="renderers"></param>
        protected override void CollectDebugComponents(out List<GameObject> objects, out List<MeshRenderer> renderers)
        {
            base.CollectDebugComponents(out objects, out renderers);
			Util.SG_Util.CollectGameObject(this.debugTxt, ref objects);
            if (this.virtualHoverCollider != null)
            {
                Util.SG_Util.CollectComponent(virtualHoverCollider, ref renderers);
                Util.SG_Util.CollectGameObject(virtualHoverCollider.debugTxt, ref objects);
            }
        }

        /// <summary> Link this GrabScript to a new TrackedHand. </summary>
        /// <param name="newHand"></param>
        /// <param name="firstLink"></param>
        protected override void LinkToHand_Internal(SG_TrackedHand newHand, bool firstLink)
        {
			// links this newHand.
            base.LinkToHand_Internal(newHand, firstLink);

            // The hand will call my update from now on.
            this.updateSelf = false;

			//this is where I'm getting the HandPose from
			this.handPoseProvider = newHand;
            
            //// Link the Real GrabReference to the Real Hand Tracking.
            realRefTracking.updateTime = SG_SimpleTracking.UpdateDuring.Off; //TrackedHand will update me
            Transform RhandTarget = newHand.GetTransform(SG_TrackedHand.TrackingLevel.RealHandPose, HandJoint.Wrist); //follow the real wrist. 
            realRefTracking.SetTrackingTarget(RhandTarget, true); //Only recaluclate offsets the first time I linked to this script?

            //// Link the Virtual GrabReference to the virtual hand pose.
            virtualRefTracking.updateTime = SG_SimpleTracking.UpdateDuring.Off; //TrackedHand will update me
            Transform VhandTarget = newHand.GetTransform(SG_TrackedHand.TrackingLevel.VirtualPose, HandJoint.Wrist); //follow the real wrist. 
            virtualRefTracking.SetTrackingTarget(VhandTarget, true); //Only recaluclate offsets the first time I linked to this script?

            // Finally, ensure I control the hoverColliders, and that it's properly linked to the wrist.
            if (virtualHoverCollider != null)
            {
                //Link the Collider to the virtual hand pose wrist
                virtualHoverCollider.updateTime = SG_SimpleTracking.UpdateDuring.Off;
                Transform virtualWrist = newHand.GetTransform(SG_TrackedHand.TrackingLevel.VirtualPose, HandJoint.Wrist);
                virtualHoverCollider.SetTrackingTarget(virtualWrist, true);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Utility Stuff

        public void PutOnCooldown(SG_Interactable obj)
        {
            if (!OnCooldown(obj))
            {
                this.objsOnCoolDown.Add(new CooldownParams(obj));
            }
        }

        public bool OnCooldown(SG_Interactable obj)
        {
            for (int i=0; i<this.objsOnCoolDown.Count; i++)
            {
                if (objsOnCoolDown[i].interactable == obj)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateCooldowns(float dT)
        {
            for (int i=0; i<this.objsOnCoolDown.Count;)
            {
                this.objsOnCoolDown[i].cooldownTimer += dT;
                if (objsOnCoolDown[i].cooldownTimer >= objCooldownTime)
                {
                    objsOnCoolDown.RemoveAt(i);
                }
                else { i++; }
            }
        }

        /// <summary> Whether or not this hand can grab (new) objects. </summary>
        public bool GrabEnabled
        {
            get { return grabbingAllowed; }
            set
            {
                grabbingAllowed = value;
            }
        }


        /// <summary> Returns the offset between this GrabScript's GrabReference and whatever it's connected to. Required to calculate the wrist location from my grabreference's location. </summary>
        /// <param name="posOffset"></param>
        /// <param name="rotOffset"></param>
        /// <returns></returns>
        public void GrabRefOffsets(out Vector3 wristToGrab_pos, out Quaternion wristToGrab_rot)
        {
			if (this.realRefTracking != null)
			{
				wristToGrab_pos = realRefTracking.PosOffset;
				wristToGrab_rot = realRefTracking.RotOffset;
			}
			else
            {
				wristToGrab_pos = Vector3.zero;
				wristToGrab_rot = Quaternion.identity;
			}
        }


		/// <summary> Debug text that shows the grabbed objects </summary>
		public string GrabbedText
        {
			get { return this.debugTxt != null ? debugTxt.text : ""; }
			set { if (debugTxt != null) { debugTxt.text = value; } }
        }

        /// <summary> Set the current GrabbedText to show the objects grabbed by this Script. </summary>
		public virtual void UpdateDebugger()
        {
			if (debugTxt != null)
            {
				GrabbedText = PrintHeldObjects();
            }
        }

        /// <summary> Returns a list of all objects held by this GrabScript. </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
		public string PrintHeldObjects(string delim = "\n")
        {
			return Util.SG_Util.PrintContents(this.heldObjects, delim);
        }




        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Control / Positioning

        /// <summary> Returns true if this GrabScript (or whatever it is holding) has control over the position / rotation of the hand.
        /// Can happen either through hovering or though holding Objects. </summary>
        /// <returns></returns>
        public virtual bool ControlsHandLocation()
        {
			for (int i=0; i<this.heldObjects.Count; i++)
            {
				if (this.heldObjects[i].ControlsHandLocation()) { return true; }
            }
			return false;
        }

        /// <summary> Updates the positions / rotations of held objects so we can draw the hand on the most up-to-date location. </summary>
        public virtual void UpdateGrabbedObjects()
        {
			this.realRefTracking.UpdateLocation(); //makes sure these is always up to date and not a frame behind.
			this.virtualRefTracking.UpdateLocation();
			for (int i = 0; i < this.heldObjects.Count; i++)
            {
                heldObjects[i].UpdateInteractable();
            }
        }

        /// <summary> Retrieve the desired hand position- and rotation as determined by the grabable(s) held by this script. </summary>
        /// <param name="handRealPose"></param>
        /// <param name="wristPosition"></param>
        /// <param name="wristRotation"></param>
        public virtual void GetHandLocation(SG_HandPose handRealPose, out Vector3 wristPosition, out Quaternion wristRotation)
        {
			//UpdateGrabbedObjects(); //ensure the grabbed objects have their latest position
			for (int i = 0; i < this.heldObjects.Count; i++)
			{
				if (this.heldObjects[i].ControlsHandLocation())
				{
					this.heldObjects[i].GetHandLocation(handRealPose, this, out wristPosition, out wristRotation);
					return;
				}
			}
			wristRotation = handRealPose.wristRotation;
			wristPosition = handRealPose.wristPosition;
		}



		/// <summary> Returns true if this GrabScript is holding- or hovering over something that overrides finger tracking. </summary>
		/// <returns></returns>
		public virtual bool ControlsFingerTracking()
		{
			for (int i = 0; i < this.heldObjects.Count; i++)
			{
				if (this.heldObjects[i].ControlsFingerTracking()) { return true; }
			}
			return false;
		}

        /// <summary> Retrieve the finger tracking overrides from this grabable. You pass the HandDimensions of the 3D model so as to return the proper values. </summary>
        /// <param name="realHandPose"></param>
        /// <param name="handDimensions"></param>
        /// <param name="overridePose"></param>
		public virtual void GetFingerTracking(SG_HandPose realHandPose, SGCore.Kinematics.BasicHandModel handDimensions, out SG_HandPose overridePose)
        {
			for (int i = 0; i < this.heldObjects.Count; i++)
			{
				if (this.heldObjects[i].ControlsHandLocation())
				{
					this.heldObjects[i].GetFingerTracking(realHandPose, this, out overridePose);
					return; //the first object to control the HandLocation
				}
			}
			overridePose = realHandPose;
		}



        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Hover Behaviour

        /// <summary> The Transform used to determine which objects are closest to the hand. </summary>
        public Transform ProximitySource
        {
            get
            {
                if (this.proximitySensor == null)
                {
                    this.proximitySensor = this.virtualGrabRefrence;
                }
                return proximitySensor;
            }
        }

        /// <summary> Returns true if this GrabScript is hovering over elligible Interactables. </summary>
        /// <returns></returns>
        public bool IsHovering()
        {
            return false;
        }

        /// <summary> Returns the object that we should be highlighting at the moment. Returns null when no object should be highlighted. </summary>
        /// <returns></returns>
        public SG_Interactable GetObjectToHighLight()
        {
            if (!this.IsGrabbing) //not allowed to highlight while I'm grabbing
            {
                SG_Interactable[] objs = this.virtualHoverCollider.GetTouchedObjects(this.ProximitySource);
                for (int i=0; i<objs.Length; i++)
                {
                    if (objs[i].GrabAllowed())
                    {
                        return objs[i]; //return the first object that can be grabbed.
                    }
                }
            }
            return null;
        }
        
        /// <summary> Updates the Hover Logic. This is where we determine OnHover/OnUnHover behaviour. </summary>
        /// <param name="dT"></param>
        public virtual void UpdateHoverLogic(float dT) //why dT? Because I might do something link ; if hovered over for X amount of time.
        {
            //Update hover collider
            if (this.virtualHoverCollider != null)
            {
                this.virtualHoverCollider.UpdateLocation();

                SG_Interactable newClosest = GetObjectToHighLight();
                if (newClosest != lastClosest)
                {
                    if (lastClosest != null) { lastClosest.SetHightLight(false, this); } //unhighlight
                    if (newClosest != null) { newClosest.SetHightLight(true, this); }
                    lastClosest = newClosest;
                }
            }
        }
        


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Grab / Release Behaviour

        /// <summary> Returns true if this GrabScript is currently holding an Object. </summary>
        public bool IsGrabbing
        {
            get { return this.heldObjects.Count > 0; }
        }

        /// <summary> Returns true if this GrabScript is allowed to grba onto more objects. </summary>
        public bool CanGrabNewObjects
        {
			get { return GrabEnabled && ( heldObjectLimit < 0 || heldObjects.Count < heldObjectLimit ); }
        }


        /// <summary> Update the Grab Logic. Determine if new objects must be grabbed or if held objects must be released. </summary>
        /// <param name="dT"></param>
        public virtual void UpdateGrabLogic(float dT)
        {
            //makes sure these is always up to date and not a frame behind.
            this.realRefTracking.UpdateLocation();
            this.virtualRefTracking.UpdateLocation();
        }

        /// <summary> Attempt to grab an SG_Interactable. Returns true if succesful. Can only fail if the object doesn't allow grabbing or if it's already being held. </summary>
        /// <param name="grabable"></param>
        /// <param name="forceGrab"></param>
        /// <returns></returns>
		public bool TryGrab(SG_Interactable grabable, bool forceGrab = false)
        {
			if ( CanGrabNewObjects && grabable.GrabAllowed() )
			{
				int index = Util.SG_Util.ListIndex(heldObjects, grabable);
				if (index < 0 && (forceGrab || !OnCooldown(grabable)) ) //We have not grabbed it yet and it's not on cooldown
				{
					// TODO; TryGrab the Object
					bool grabbed = grabable.TryGrab(this, forceGrab);
					if (grabbed)
					{
						heldObjects.Add(grabable);
                        //Debug.Log(this.name + " Grabbed " + grabable.name);
                        this.GrabbedObject.Invoke(grabable, this);
						UpdateDebugger();
						return true;
					}
					//else
     //               {
					//	Debug.Log("Attempted to grab " + grabable.name + " but failed.");
     //               }
				}
			}
			return false;
        }

        /// <summary> Attempt to release an Interactable if it's being held by this GrabScript </summary>
        /// <param name="grabable"></param>
        /// <param name="forceRelease"></param>
        /// <returns></returns>
        public bool TryRelease(SG_Interactable grabable, bool forceRelease = false)
        {
            int index = Util.SG_Util.ListIndex(heldObjects, grabable);
            if (index > -1) //it exists
            {
                return ReleaseAt(index, forceRelease);
            }
            //Debug.Log(grabable.name + " is not held by " + this.name);
            return false;
        }

        /// <summary> Actually Removes an element from the list, and calls a release event. </summary>
        /// <param name="heldIndex"></param>
        /// <returns></returns>
        protected virtual bool ReleaseAt(int heldIndex, bool forceRelease = false)
        {
			SG_Interactable toRelease = heldObjects[heldIndex];
			bool canRelease = toRelease.TryRelease(this, forceRelease);
			if (canRelease || forceRelease) //if we're forcing a release, I don't care if you couldn't do it.
            {
				heldObjects.RemoveAt(heldIndex);
                PutOnCooldown(toRelease);
                //TODO: Fire Event
                //Debug.Log(this.name + " Released " + removed.name);
                this.GrabbedObject.Invoke(toRelease, this);
                UpdateDebugger();
                return true;
			}
			return false;
		}


   

		/// <summary> Attempt to release all objects currently held by this GrabScript. It might immedeately pick if back up, though, if they're still interactable. </summary>
		/// <param name="forceRelease">If true, they'll be unstuck no matter what the object wants. </param>
		/// <returns></returns>
		public bool ReleaseAll(bool forceRelease = true)
        {
            int toRelease = this.heldObjects.Count; //all to release
            int released = 0;
            for (int i = 0; i < this.heldObjects.Count;)
            {
				bool objReleased = ReleaseAt(i, forceRelease);
                if (!objReleased) { i++; } //if we did not remove it from the list, increase the iterator, otherwise we end up in a loop.
                else { released++; }
            }
			UpdateDebugger();
			return released >= toRelease; //return true if everything is released.
        }




        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected virtual void Start()
        {
			UpdateDebugger();
        }

        protected virtual void FixedUpdate()
        {
            UpdateHoverLogic(Time.deltaTime);
            if (updateSelf)
            {
                this.UpdateGrabLogic(Time.fixedDeltaTime);
            }
        }

        protected virtual void Update()
        {
            UpdateCooldowns(Time.deltaTime);
        }

		protected virtual void OnDestroy()
        {
			// When we're not quitting
			// TODO: Remove Grabable's reference to me
		}

	}
}