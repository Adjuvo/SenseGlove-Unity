using SGCore.Haptics;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    //-------------------------------------------------------------------------------------------------------------------------------------
    // Hover Arguments

	/// <summary> Contains any hovering arguments, which happens when a hand hovers above an object. Implements IFeedback device to send Haptics directly to the TrackedHand involved </summary>
	public class HoverArguments : IHandFeedbackDevice
	{
		/// <summary> The GrabScript that is hovering above this Interactable. </summary>
		public SG_GrabScript GrabScript { get; set; }

        /// <summary> The trackedHand linked to the Grabscript. </summary>
        public SG_TrackedHand TrackedHand
        {
            get { return this.GrabScript.TrackedHand; }
        }

        /// <summary> Protected default constructor for extension properties. </summary>
		protected HoverArguments() { }

        /// <summary> Create new Hover Arguments </summary>
        /// <param name="hoveredBy"></param>
		public HoverArguments(SG_GrabScript hoveredBy)
        {
			GrabScript = hoveredBy;
        }


        /// <summary> Returns the name of the TrackedHand involved in these Arguments </summary>
        /// <returns></returns>
        public string Name()
        {
            return TrackedHand != null ? ((IHandFeedbackDevice)TrackedHand).Name() : "";
        }

        /// <summary> Returns true if the TrackedHand involved in these aguments is linked and connected. </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return TrackedHand != null ? ((IHandFeedbackDevice)TrackedHand).IsConnected() : false;
        }

        /// <summary> End all vibrations on the TrackedHand involved in these arguments. </summary>
        public void StopAllVibrations()
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).StopAllVibrations(); }
        }

        /// <summary> Stop all vibration- and force haptics on the TrackedHand linked to these args </summary>
        public void StopHaptics()
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).StopHaptics(); }
        }

        /// <summary> Send a Force-Feedback command to the TrackedHand linked to these Args </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SG_FFBCmd ffb)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SendCmd(ffb); }
        }

        /// <summary> Send a Vibration command to the TrackedHand linked to these Args </summary>
        /// <param name="fingerCmd"></param>
        public void SendCmd(SG_TimedBuzzCmd fingerCmd)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SendCmd(fingerCmd); }
        }

        /// <summary> Send a Thumper command to the TrackedHand linked to these Args </summary>
        /// <param name="wristCmd"></param>
        public void SendCmd(TimedThumpCmd wristCmd)
        {
            if (TrackedHand != null) {  ((IHandFeedbackDevice)TrackedHand).SendCmd(wristCmd); }
        }

        /// <summary> Send a Thumper command to the TrackedHand linked to these Args </summary>
        /// <param name="waveform"></param>
        public void SendCmd(ThumperWaveForm waveform)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SendCmd(waveform); }
        }

        /// <summary> Send a WaveForm command to the TrackedHand linked to these Args </summary>
        /// <param name="waveform"></param>
        public void SendCmd(SG_Waveform waveform)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SendCmd(waveform); }
        }


        /// <summary> Send an Impact Vibration command to the TrackedHand linked to these Args </summary>
        /// <param name="location"></param>
        /// <param name="normalizedVibration"></param>
        public void SendImpactVibration(SG_HandSection location, float normalizedVibration)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SendImpactVibration(location, normalizedVibration); }
        }

        public void SendCmd(SG_NovaWaveform customWaveform, SGCore.Nova.Nova_VibroMotor location)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SendCmd(customWaveform, location); }
        }

        public bool FlexionLockSupported()
        {
            return TrackedHand != null && ((IHandFeedbackDevice)TrackedHand).FlexionLockSupported();
        }

        public void SetFlexionLocks(bool[] fingers, float[] fingerFlexions)
        {
            if (TrackedHand != null) { ((IHandFeedbackDevice)TrackedHand).SetFlexionLocks(fingers, fingerFlexions); }
        }

        public bool TryGetBatteryLevel(out float value01)
        {
            if (TrackedHand != null) 
            { 
                return ((IHandFeedbackDevice)TrackedHand).TryGetBatteryLevel(out value01);
            }
            value01 = 1.0f;
            return false;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------------
    // Grab Arguments

    /// <summary> Contains any grabbing arguments, which happens when an SG_GrabScript actually grabs an object </summary>
    public class GrabArguments : HoverArguments
    {
		/// <summary> The position of the GrabScript's "Grab Refrence" relative to this GameObject when the grabbing occured </summary>
		public Vector3 GrabOffset_Position { get; set; }
		/// <summary> The rotation of the GrabScript's "Grab Refrence" relative to this GameObject when the grabbing occured </summary>
		public Quaternion GrabOffset_Rotation { get; set; }

        /// <summary> The position of the grabbed object when the grba occured </summary>
        public Vector3 MyPosAtGrab { get; set; }
        /// <summary> The rotation of the grabbed object when the grba occured </summary>
        public Quaternion MyRotAtGrab { get; set; }

		/// <summary> Calculates the target position of an object that was grabbed by this grabScript. A.k.a. The current GrabAnchor Location + offsets at grab </summary>
		/// <param name="obj"></param>
		/// <param name="objTargetPos"></param>
		/// <param name="objTargetRot"></param>
		public virtual void CalculateObjectTarget(Transform obj, out Vector3 objTargetPos, out Quaternion objTargetRot)
        {
            //TODO: Confirm if this is correct.
			SG.Util.SG_Util.CalculateTargetLocation(this.GrabScript.realGrabRefrence, GrabOffset_Position, GrabOffset_Rotation, out objTargetPos, out objTargetRot);
        }

		/// <summary> Calculates the location of a GrabReference based on the grabbed location a.k.a. The object position - offsets at grab </summary>
		/// <param name="obj"></param>
		/// <param name="objTargetPos"></param>
		/// <param name="objTargetRot"></param>
		public virtual void CalculateRefrenceLocation(Transform obj, out Vector3 objTargetPos, out Quaternion objTargetRot)
        {
			SG.Util.SG_Util.CalculateRefrenceLocation(obj.position, obj.rotation, GrabOffset_Position, GrabOffset_Rotation, out objTargetPos, out objTargetRot);
        }

        public Transform GetRealGrabRefrence()
        {
            return this.GrabScript != null && this.GrabScript.realGrabRefrence != null ? this.GrabScript.realGrabRefrence : null;
        }

        /// <summary> Safely retrieve the current (world) position of the real hand's GrabReference. </summary>
        /// <returns></returns>
        public Vector3 GetRealRefPosition()
        {
            return this.GrabScript != null && this.GrabScript.realGrabRefrence != null ? this.GrabScript.realGrabRefrence.position : Vector3.zero;
        }



        /// <summary> Default constructor for extension purposes. </summary>
		protected GrabArguments() { }


		/// <summary> Create a new GrabArguments object </summary>
		/// <param name="hoveredBy"></param>
		/// <param name="relativePos"></param>
		/// <param name="relativeRot"></param>
		public GrabArguments(SG_GrabScript hoveredBy, Vector3 relativePos, Quaternion relativeRot, Vector3 objPosition, Quaternion objRotation)
		{
			GrabScript = hoveredBy;
			GrabOffset_Position = relativePos;
			GrabOffset_Rotation = relativeRot;
            MyPosAtGrab = objPosition;
            MyRotAtGrab = objRotation;
        }
	}



    //-------------------------------------------------------------------------------------------------------------------------------------
    // Interactable Script.

    /// <summary> A base class that can be overriden. It fires events and keeps track of hands, but does not manipulate the object it's attached to. </summary>
    /// <remarks> Use this script to use make an object recognizeable by an SG_GrabScript. </remarks>
    [DisallowMultipleComponent]
    public class SG_Interactable : MonoBehaviour, IHandFeedbackDevice //using this as a Feebdack Device will send commands to all hands that holding this GrabScript.
	{
        /// <summary> Grab conditions for an Interactable, restricting grabbing based on certain contitions </summary>
        public enum GrabMethod
        {
            /// <summary> This object can be grabbed by one or two hands </summary>
            Any,
            /// <summary> This object can only be grabbed by one hand. </summary>
            OneHandOnly,
            /// <summary> This object can only be grabbed by the right hand </summary>
            RightHandOnly,
            /// <summary> This object can only be grabbed by the left hand </summary>
            LeftHandOnly,
            /// <summary> This object cannot be grabbed by itself. However, its GrabPoints can still be grabbed. </summary>
            None,
        }


        /// <summary> Auto-assigned. The "base" of this Interactable, on which to apply Translation/Rotation when no RigidBody is attached.  </summary>
        [Header("Interactable Options")]
        public Transform baseTransform;

        /// <summary> Auto-assigned. Optional RigidBody attached to this Interactable. Translation/Rotation will be applied to this Body if assigned. </summary>
        public Rigidbody physicsBody;

        /// <summary> Which hands are allowed to Hover and Grab this Interactable. </summary>
        public GrabMethod allowedHands = GrabMethod.Any;


        ///// <summary> All GrabScripts currently hovering over this interactable. </summary>
        //public List<HoverArguments> hoveredBy = new List<HoverArguments>();


        /// <summary> All GrabScripts currenly (attempting to) hold onto this Interactable. </summary>
        [SerializeField] protected List<GrabArguments> grabbedBy = new List<GrabArguments>();

        /// <summary> The last simulation time that this object was updated for. Used to only call Update() once, even if multiple hands call said function. Starts at -1 so it fires even at t=0. </summary>
		protected float lastUpdateTime = -1;

        /// <summary> Ensures we only search for components on this script once, even if we get multiple setup calls. </summary>
        protected bool setup = true;

        /// <summary> Optional Component to toggle when this object should be highlighted. </summary>
        private SG_Activator highlighter = null; //TOD: Make this into a more universal script.
        /// <summary> The Grabscripts that wish to Highlight me. Used to ensure that HighLights work properly. </summary>
        protected List<SG_GrabScript> highlightedBy = new List<SG_GrabScript>();

        /// <summary> Cached colliders linked to this Interactable, which are used to toggle physics collision between it and the hand physics. </summary>
        protected Collider[] myColliders = null;

        /// <summary> Optional Debug Element that shows which grabables are being held. </summary>
        public TextMesh debugTxt;

        /// <summary> List of linked grabPoints So I can let go of them as well. </summary>
        protected List<SG_GrabPoint> linkedGrabPoints = new List<SG_GrabPoint>();

        /// <summary> The frame wherein we last changed out IsKinematic Value. </summary>
        protected int safeguardFrame = -1;

        /// <summary> Fires just before this object is officially grabbed by a GrabScript. </summary>
        public SG_GrabbedObjectEvent ObjectGrabbed = new SG_GrabbedObjectEvent();
        /// <summary> Fires just before this object is officially released by a GrabScript. </summary>
        public SG_GrabbedObjectEvent ObjectReleased = new SG_GrabbedObjectEvent();


        /// <summary> The last Grab Arguments that were applied to this Interactable. Useful if you wish to send Haptics just after a hand just releases. Is NULL when no hand has ever grabbed or released this object </summary>
        public GrabArguments LastGrabbedBy
        {
            get; protected set;
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Utility Functions


        /// <summary> If we set the isKinematic value of our RigidBody, it causes PhysX in Unity 2019+ to have an anyeurism and fire OnTriggerExit/OnTriggerEnter again. If this value is true, you shouldn't need ot affect your logic </summary>
        public bool KinematicChanged
        {
            get { return safeguardFrame > 0; } // > 0 so the first frame (Setup) doesn't influence detection.
        }


        public bool IsKinematic
        {
            get { return this.physicsBody == null || this.physicsBody.isKinematic; }
            set
            {
                if (physicsBody != null)
                {
                    if (value != physicsBody.isKinematic)
                    {
                        //Debug.Log("SGKin: IsKinematic value changed on " + Time.frameCount);
                        safeguardFrame = Time.frameCount;
                    }
                    physicsBody.isKinematic = value;
                }
            }
        }

        /// <summary> Sets up this Script with it's appropriate links and behaviours. Ensures they are performed only once. </summary>
        public void Setup()
        {
            if (setup)
            {
                setup = false;
                SetupScript();
            }
        }

        /// <summary> Properly link this script to all of its components. </summary>
        protected virtual void SetupScript()
        {
            if (this.baseTransform == null)
            {
                this.baseTransform = this.physicsBody != null ? this.physicsBody.transform : this.transform;
            }
            if (this.physicsBody == null)
            {
                this.physicsBody = this.baseTransform.GetComponent<Rigidbody>();
            }
            this.LastGrabbedBy = null;
            this.highlighter = this.GetComponent<SG_Activator>(); //attempt to collect hover highlight.
        }


        /// <summary> Let this Grabable know that it's linked to another point, so it may be released when I am </summary>
        /// <param name="point"></param>
        public void LinkGrabPoint(SG_GrabPoint point)
        {
            if (point != this)
            {
                SG.Util.SG_Util.SafelyAdd(point, linkedGrabPoints);
                point.enabled = this.enabled; //match my settings
            }
            else
            {
                Debug.LogError("ERROR: Cannot link a SnapPoint to itself!");
            }
        }

        /// <summary> Let this interactable know this is no longer connected to this point. </summary>
        /// <param name="point"></param>
        public void UnlinkGrabPoint(SG_GrabPoint point)
        {
            SG.Util.SG_Util.SafelyRemove(point, linkedGrabPoints);
        }

        //---------------------------------------------------------------------------------------------------------------------------------
        // Tracking Stuff

        /// <summary> The transform on which to apply movement (positioning / location) if no RigidBody can be found. </summary>
        public virtual Transform MyTransform
        {
            get 
            {
                Setup(); //ensures baseTransform is assigned
                return this.baseTransform; 
            }
        }


        /// <summary> Updates the interactable's position / rotation. This script will ensure it's only called once per frame, and that is has the proper dT. </summary>
        public void UpdateInteractable()
        {
            float currTime = Time.timeSinceLevelLoad; //Doing it per level 
            if (currTime != lastUpdateTime)
            {
                float dT = currTime - lastUpdateTime;
                lastUpdateTime = currTime;
                UpdateLocation(dT); //calculate DT first so we can't call UpdateInteractable from this gameobject.
            }
        }

        /// <summary> Update the location of this Interactable. Override this to implement behaviour that can be called from multiple scripts, but which only needs to run once. </summary>
        /// <param name="dT"></param>
        protected virtual void UpdateLocation(float dT) { }



        /// <summary> If you return true, this Interactable overrides the Hand Location while grabbed. At that point, GetHandlocation will be called to determine the wrist position.  </summary>
        /// <returns></returns>
        public virtual bool ControlsHandLocation()
        {
			return false;
        }


        /// <summary> Returns the Hand position for a particular GrabScript (mainly used to distinguish left / right). A.k.a: I'm here, and the grabScript held me there, so it need to be at the following location... </summary>
        /// <param name="handRealPose">Optional. Is returned if this Interactable is not being held by the connectedScript.</param>
        /// <param name="connectedScript">The script from which the function was called.</param>
        /// <param name="wristPosition"></param>
        /// <param name="wristRotation"></param>
        /// <returns></returns>
        public virtual void GetHandLocation(SG_HandPose handRealPose, SG_GrabScript connectedScript, out Vector3 wristPosition, out Quaternion wristRotation)
        {
			int grabIndex = ListIndex(connectedScript, this.grabbedBy);
			if (grabIndex > -1) //we are being held by this hand.
			{
                Transform myTransf = this.MyTransform;

				//Step 1: Calculate GrabRefrence location 
				Vector3 grabRefPos; Quaternion grabRefRot;
				grabbedBy[grabIndex].CalculateRefrenceLocation(myTransf, out grabRefPos, out grabRefRot); //todo: save this for later as a separate value?

                //Step 2: Get offsets between GrabRefrence and whatever it's following a.k.a. "the wrist"
                Vector3 wristToGrab_pos; Quaternion wristToGrab_rot;
                connectedScript.GrabRefOffsets(out wristToGrab_pos, out wristToGrab_rot);

                //Step3: Calculate the "wrist" location based on these offsets 
                Util.SG_Util.CalculateRefrenceLocation(grabRefPos, grabRefRot, wristToGrab_pos, wristToGrab_rot, out wristPosition, out wristRotation);
            }
			else //this is not a GrabScrip that holds me.
			{
				//else there isn't anything special going on.
				wristRotation = handRealPose.wristRotation;
				wristPosition = handRealPose.wristPosition;
			}
        }


		/// <summary> If you return true, this interactable forces the hand pose into a particular position. GetFingerTracking will then be called to determine what said pose will be. </summary>
		/// <returns></returns>
		public virtual bool ControlsFingerTracking()
		{
			return false;
		}

        /// <summary> Get the hand pose as determined by this grabable. Override it if you're making something that forces the hand in a particular pose. </summary>
        /// <param name="realHandPose"></param>
        /// <param name="overridePose"></param>
        /// <param name="connectedScript"> The GrabScript from which the HandPose is coming.</param>
        public virtual void GetFingerTracking(SG_HandPose realHandPose, SG_GrabScript connectedScript, out SG_HandPose overridePose)
        {
			overridePose = realHandPose;
        }


        //---------------------------------------------------------------------------------------------------------------------------------
        // Utility Stuff

        /// <summary> Debug text that shows the grabbed objects </summary>
        public string GrabbedText
        {
            get { return this.debugTxt != null ? debugTxt.text : ""; }
            set { if (debugTxt != null) { debugTxt.text = value; } }
        }

        /// <summary> Get a list of all GrabScripts that are holding onto this SG_Interactable. </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
        public string PrintGrabbedBy(string delim = "\n")
        {
            string res = "";
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                res += this.grabbedBy[i].GrabScript.name;
                if (i < grabbedBy.Count - 1) { res += delim; }
            }
            return res;
        }

        /// <summary> Set the GrabbedText to show the list of GrabScripts holding on to me. </summary>
        public void UpdateDebugger()
        {
            if (debugTxt != null)
            {
                GrabbedText = PrintGrabbedBy();
            }
        }



        /// <summary> Access all Colliders connected to this Interactable, to Get/Set their physics behaviour. </summary>
        /// <returns></returns>
        public Collider[] GetPhysicsColliders()
        {
            if (myColliders == null)
            {
                myColliders = CollectPhysicsColliders().ToArray();
            }
            return myColliders;
        }

        /// <summary> Collect the Physics Colliders connected to this Interactable. Only done once. Passing it as a List so it's easier to add to it in override implementations. </summary>
        /// <returns></returns>
        protected virtual List<Collider> CollectPhysicsColliders()
        {
            List<Collider> res =  new List<Collider>();
            SG.Util.SG_Util.GetAllColliders(this.gameObject, ref res, true);
            return res;
        }


		/// <summary> Returns the index of a GrabScript in a list of Grab/Hover Arguments. </summary>
		/// <param name="grabScript"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static int ListIndex<T>(SG_GrabScript grabScript, List<T> args) where T : HoverArguments
        {
			for (int i=0; i<args.Count; i++)
            {
				if (grabScript == args[i].GrabScript)
                {
					return i;
                }
            }
			return -1;
        }

        /// <summary> Check if any of the Arguments in listArgs has the same GrabScript as args. </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <param name="listArgs"></param>
        /// <returns></returns>
        public static int ListIndex<T>(T args, List<T> listArgs) where T : HoverArguments
        {
            for (int i = 0; i < listArgs.Count; i++)
            {
                if (args.GrabScript == listArgs[i].GrabScript)
                {
                    return i;
                }
            }
            return -1;
        }


        public Vector3 ToWorldPosition(Vector3 posRelativeToMe)
        {
            Transform transf = this.MyTransform;
            return transf.position + (transf.rotation * posRelativeToMe);
        }



        //---------------------------------------------------------------------------------------------------------------------------------
        // Hover Behaviour

        /// <summary> Get/Set Highlight on or off. </summary>
        public virtual bool HighlightEnabled
        {
            get { return this.highlighter != null && this.highlighter.Activated; }
        }

        /// <summary> Enable / Disable the highlighting of this Object, if it has any options for that. </summary>
        /// <param name="active"></param>
        public void SetHighLight(bool active)
        {
            if (this.highlighter != null)
            {
                this.highlighter.Activated = active; //keep it active if enough scripts want it to be highlighted.
            }
        }

        /// <summary> If this interactable has any highlight components, set them to active. </summary>
        /// <param name="active"></param>
        /// <param name="script">The GrabScpt from which the function is called.</param>
        public void SetHightLight(bool active, SG_GrabScript script)
        {
            //add to the list / remove from the list
            if (active) { Util.SG_Util.SafelyAdd(script, highlightedBy); }
            else { Util.SG_Util.SafelyRemove(script, highlightedBy); }
            SetHighLight(highlightedBy.Count > 0); //only turn if off of we have no script hovering over me
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Grabbing / Releasing functions

        /// <summary> Returns true if this Interactable can be grabbed. </summary>
        public virtual bool GrabAllowed()
        {
			return this.enabled;
        }

		/// <summary> Returns true if this object is currenly being grabbed by any Grabscript. </summary>
		/// <returns></returns>
		public virtual bool IsGrabbed()
        {
			return this.grabbedBy.Count > 0;
        }

        /// <summary> Returns true if this Object is being Grabbed by a GrabScript at the moment. </summary>
        /// <param name="grabScript"></param>
        /// <returns></returns>
        public virtual bool GrabbedBy(SG_GrabScript grabScript)
        {
			return ListIndex(grabScript, this.grabbedBy) > -1;
        }

        /// <summary> Returns a list of all scripts grabbing on to this object and their GrabArguments. </summary>
        /// <returns></returns>
        public virtual List<GrabArguments> ScriptsGrabbingMe()
        {
            return SG.Util.SG_Util.ArrayCopy(this.grabbedBy);
        }


        /// <summary> Return true if this object is allowed to be grabbed by a GrabScript. When returning false, it will only grab if forced. 
        /// This should not affect behaviour - only determine if it's possible or not. </summary>
        /// <param name="grabScript"></param>
        /// <returns></returns>
        public virtual bool CanBeGrabbed(SG_GrabScript grabScript)
        {
            switch (this.allowedHands)
            {
                case GrabMethod.Any:
                    return true;
                case GrabMethod.OneHandOnly:
                    return this.grabbedBy.Count == 0;
                case GrabMethod.RightHandOnly:
                    return grabScript.IsRight;
                case GrabMethod.LeftHandOnly:
                    return !grabScript.IsRight;
                case GrabMethod.None:
                    return false;
            }
            return true;
        }

        //------------------------------------------------------------------------
        // Grab Behaviour



        /// <summary> Step 1: Returns true if this object was succesfully grabbed by a GrabScript. Grabbing can fail because for many reasons (e.g. already being grabbed by this script,
        /// this is a one-handed object, etc). </summary>
        /// <param name="grabScript"></param>
        /// <param name="forcedGrab">If the object says no we will still grab it.</param>
        /// <returns></returns>
        public virtual bool TryGrab(SG_GrabScript grabScript, bool forcedGrab = false)
        {
			int grabbedIndex = ListIndex(grabScript, this.grabbedBy);
			if (grabbedIndex < 0) //We are not yet grabbed by this GrabScript
            {
				bool canGrab = CanBeGrabbed(grabScript);
				if (canGrab || forcedGrab)
                {
					GrabArguments args;
					if (StartGrab(grabScript, out args))
					{
						this.grabbedBy.Add(args);
                        this.OnGrabComplete(args);
                        return true;
					}
                }
            }
			return false;
        }

        /// <summary> Attempt to grab with pre-generated arguments. Used from GrabPoint to notify the Interactable from a different script. Only use call this when you know what you're doing </summary>
        /// <param name="args"></param>
        public virtual bool TryGrab(GrabArguments grabArgs)
        {
            int grabbedIndex = ListIndex(grabArgs, this.grabbedBy);
            //is there already arguments for that grabscript in here?
            if (grabbedIndex < 0) //We are not yet grabbed by this GrabScript
            {
                GrabArguments args;
                if (StartGrab(grabArgs.GrabScript, out args)) //we call this to simulate proper behaviour (rigidBody manipulation etc).
                {
                    this.grabbedBy.Add(grabArgs); //add the pre-generated Grab Args as opposed to the one made by this script.
                    this.OnGrabComplete(grabArgs);
                    return true;
                }
            }
            return false;
        }


		/// <summary> Gets called when we determined that the Grab can happen, but before we add the GrabScript to the list.
		/// If true is returned, the grabscript is actually added to our list. </summary>
		/// <param name="grabScript"></param>
		/// <param name="grabArgs">The GrabArguments as indictaed by GenerateGrabArgs. If all you're after is changing this output, it might be easier to override said function.</param>
		/// <returns></returns>
		protected virtual bool StartGrab(SG_GrabScript grabScript, out GrabArguments grabArgs)
        {
			grabArgs = GenerateGrabArgs(grabScript);
			return grabArgs != null;
        }

        /// <summary> Fires after a grab is succesful and we can begin. Invokes event and updates degbugger. </summary>
        protected virtual void OnGrabComplete(GrabArguments grabArgs)
        {
            //Debug.Log(this.name + " Grabbed by " + grabScript.name);
            this.ObjectGrabbed.Invoke(this, grabArgs.GrabScript);
            this.LastGrabbedBy = grabArgs; //updates the last hand to initiate a grab event.
            UpdateDebugger();
        }

		/// <summary> Put in a separate function so we can add more in-depth GrabArguments in later classes. </summary>
		/// <param name="grabScript"></param>
		/// <returns></returns>
		public virtual GrabArguments GenerateGrabArgs(SG_GrabScript grabScript)
        {
            Transform myTransf = this.MyTransform; //collect transform component.
			Vector3 posOffset; Quaternion rotOffset;
			SG.Util.SG_Util.CalculateOffsets(myTransf, grabScript.virtualGrabRefrence, out posOffset, out rotOffset); //virtualGrabReference. That's where we need to go.
			return new GrabArguments(grabScript, posOffset, rotOffset, myTransf.position, myTransf.rotation);
		}





		//------------------------------------------------------------------------
		// Release Behaviour


		/// <summary> Returns true if this object is released by a GrabScript </summary>
		/// <param name="grabScript"></param>
		/// <param name="forcedRelease">if set to true, we will release no matter what the object says</param>
		/// <returns></returns>
		public virtual bool TryRelease(SG_GrabScript grabScript, bool forcedRelease = false)
		{
			int grabbedIndex = ListIndex(grabScript, this.grabbedBy);
			if (grabbedIndex > -1) //We are actually grabbed by this GrabScript
			{
				GrabArguments beReleasedBy = grabbedBy[grabbedIndex];
				bool canEnd = CanBeReleased(beReleasedBy);
				if (canEnd || forcedRelease)
                {
					if (StartRelease(beReleasedBy))
                    {
                        // Actually release it.
                        this.grabbedBy.RemoveAt(grabbedIndex);
                        this.OnReleaseComplete(beReleasedBy);
                        return true;
                    }
				}
			}
			return false;
		}


        /// <summary> Attempt to release with pre - generated arguments.Only use this when you know what you're doing  </summary>
        /// <param name="grabArgs"></param>
        public virtual bool TryRelease(GrabArguments grabArgs)
        {
            int grabbedIndex = ListIndex(grabArgs, this.grabbedBy);
            if (grabbedIndex > -1) //We are actually grabbed by this GrabScript
            {
                GrabArguments beReleasedBy = grabbedBy[grabbedIndex];
                if (StartRelease(beReleasedBy))
                {
                    // Actually release it.
                    this.grabbedBy.RemoveAt(grabbedIndex);
                    OnReleaseComplete(beReleasedBy); //call event while the args are still alive
                    return true;
                }
            }
            return false;
        }

        /// <summary> Releases this SG_Interactable from any GrabScripts that may be grabbing it. </summary>
        public virtual void ReleaseSelf()
        {
            if (this.grabbedBy.Count > 0)
            {
                Debug.Log("Manual Release from " + this.name + ". State = " + this.enabled);

                int sanity = this.grabbedBy.Count;
                int removals = 0;
                do
                {
                    //string debug = this.name + ": Attempting to get " + this.grabbedBy[0].GrabScript.name + " (" + this.grabbedBy.Count + ") to release. Result = ";
                    bool removed = this.grabbedBy[0].GrabScript.TryRelease(this, true); //remove the first one, thich changes grabbedBy.Count(!)
                    if (!removed) //the grabscript doesn't think it's holding me, but I sure do!
                    {
                        GrabArguments beReleasedBy = this.grabbedBy[0];
                        this.StartRelease(beReleasedBy); //Call the release neatly
                        this.grabbedBy.RemoveAt(0); //then remove this one-sided love.
                        OnReleaseComplete(beReleasedBy); //call event while the args are still alive
                    }
                    //debug += removed + ", newCount = " + this.grabbedBy.Count;
                    removals++;
                }
                while (this.grabbedBy.Count > 0 && removals < sanity);

                //afterward, remove any refrences. they should be gone.
                this.grabbedBy.Clear();
            }
            for (int i=0; i<this.linkedGrabPoints.Count; i++)
            {
                this.linkedGrabPoints[i].ReleaseSelf();
            }
        }

        protected virtual void OnReleaseComplete(GrabArguments beReleasedBy)
        {
            //Debug.Log(this.name + " Released by " + beReleasedBy.GrabScript.name);
            this.LastGrabbedBy = this.grabbedBy.Count == 0 ? beReleasedBy : this.grabbedBy[this.grabbedBy.Count - 1];
            this.ObjectReleased.Invoke(this, beReleasedBy.GrabScript);
            UpdateDebugger();
        }

        /// <summary> Returns true if this object is allowed to be released by a specific GrabScript. Should not affecr behaviour, only deterimen if it's allowed.
        /// Returning false will cancel the operation. </summary>
        /// <param name="grabbedScript"></param>
        /// <returns></returns>
        protected virtual bool CanBeReleased(GrabArguments grabbedScript)
        {
			return true;
        }

		/// <summary> Called when an object is released, but before we remove it from the list. After this, it will be removed from the list. Used to perform actions such as restore RigidBody parameters. </summary>
		/// <param name="grabScript"></param>
		/// <returns></returns>
		protected virtual bool StartRelease(GrabArguments grabbedScript)
		{
			return true;
		}


        //--------------------------------------------------------------------------------------
        // IHandFeedbackDevice Imnterface - Pass Cmds to all hands touching me.

        /// <summary> Returns this object's name. </summary>
        /// <returns></returns>
        public string Name()
        {
            return this.name;
        }

        /// <summary> Attempt to retrieve the battery level of the device connected to this hand. </summary>
        /// <param name="value01"></param>
        /// <returns></returns>
        public bool TryGetBatteryLevel(out float value01)
        {
            value01 = -1.0f;
            return false;
        }

        /// <summary> Returns true onless overrided. </summary>
        /// <returns></returns>
        public virtual bool IsConnected()
        {
            return true;
        }

        /// <summary> Stop All vibrations on the hands that are holding on to this object. </summary>
        public void StopAllVibrations()
        {
            for (int i=0; i<this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.StopAllVibrations(); }
            }
        }

        /// <summary> Stop All Haptics on the hands that are holding on to this object. </summary>
        public void StopHaptics()
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.StopHaptics(); }
            }
        }

        /// <summary> Send a force-feedback command to all hands holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SG_FFBCmd ffb)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendCmd(ffb); }
            }
        }

        /// <summary> Send a buzz command to all hands holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SG_TimedBuzzCmd fingerCmd)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendCmd(fingerCmd); }
            }
        }

        /// <summary> Send a wrist command to all hands holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendCmd(TimedThumpCmd wristCmd)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendCmd(wristCmd); }
            }
        }

        /// <summary> Send a waveform command to all hands holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendCmd(ThumperWaveForm waveform)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendCmd(waveform); }
            }
        }

        /// <summary> Send a waveform command to all hands holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SG_Waveform waveform)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendCmd(waveform); }
            }
        }

        /// <summary> Send an impact vibration command to all hands holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendImpactVibration(SG_HandSection location, float normalizedVibration)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendImpactVibration(location, normalizedVibration); }
            }
        }

        public void SendCmd(SG_NovaWaveform customWaveform, SGCore.Nova.Nova_VibroMotor location)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SendCmd(customWaveform, location); }
            }
        }

        public bool FlexionLockSupported()
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null && grabbedBy[i].TrackedHand.FlexionLockSupported()) { return true; }
            }
            return false;
        }

        public void SetFlexionLocks(bool[] fingers, float[] fingerFlexions)
        {
            for (int i = 0; i < this.grabbedBy.Count; i++)
            {
                if (grabbedBy[i] != null && grabbedBy[i].TrackedHand != null) { this.grabbedBy[i].TrackedHand.SetFlexionLocks(fingers, fingerFlexions); }
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected virtual void Awake()
        {
            Setup();
        }

		protected virtual void Start()
        {
			UpdateDebugger();
        }


        protected virtual void OnDisable()
        {
            if (!SG.Util.SG_Util.IsQuitting) //otherwise Unity will cry when we change parenting etc
            {
                this.ReleaseSelf();
            
                //also disable the linked grabpoints
                if (linkedGrabPoints.Count > 0)
                {
                    //Debug.Log(this.name + " Disabled. Disabling Handles");
                    for (int i = 0; i < this.linkedGrabPoints.Count; i++)
                    {
                        this.linkedGrabPoints[i].enabled = false;
                    }
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (linkedGrabPoints.Count > 0)
            {
                //Debug.Log(this.name + " Enabled. Enabling Handles");
                for (int i = 0; i < this.linkedGrabPoints.Count; i++)
                {
                    this.linkedGrabPoints[i].enabled = true;
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            SG.Util.SG_Util.IsQuitting = true;
        }
    }
}