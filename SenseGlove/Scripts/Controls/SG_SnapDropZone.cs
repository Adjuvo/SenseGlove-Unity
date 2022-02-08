using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> A special kind of DropZone that "snaps" the Grabables it detects to a fixed point in space. </summary>
    public class SG_SnapDropZone : SG_DropZone
    {
        //-----------------------------------------------------------------------------------------------
        // Snap Zone Parameters

        /// <summary> Additional arguments to create a smooth snapping after locking the object. </summary>
        /// <remarks> Placed inside SnapDropZone becuase it will likely not be used outside of it. </remarks>
        public class SnapZoneArgs : DropZoneArgs
        {
            /// <summary> Is true once snapping has completed, and the object is securely in place. </summary>
            public bool snapComplete = false;

            /// <summary> Timer for this object. Determines how far along the movement is. </summary>
            public float snapTimer = 0;

            /// <summary> Whether or not this object was Enabled before we initiated snap. If this zone doesn't lock it, set it back to this value once snapping completes. </summary>
            public bool wasEnabled;

            /// <summary> The object's position when smoothed snapping started. </summary>
            public Vector3 startPosition = Vector3.zero;
            /// <summary> The object's rotation when smoothed snapping started. </summary>
            public Quaternion startRotation = Quaternion.identity;

            /// <summary> The last known parent of this object before it became attached to this script. </summary>
            public Transform lastParent = null;

            /// <summary> Whether or not the object's parent has been restored yet. </summary>
            public bool parentRestored = false;

            /// <summary> Empty Constructor for child classes. </summary>
            protected SnapZoneArgs() { }

            /// <summary> Create a new insance of SnapZoneArgs. </summary>
            /// <param name="detectedScript"></param>
            public SnapZoneArgs(SG_Grabable detectedScript)
            {
                grabable = detectedScript;
                wasEnabled = detectedScript.enabled; //Might as well log it here for good measure. 
                lastParent = detectedScript.MyTransform.parent;
            }

            /// <summary> Stores relevant variables and locks the object for a snap operation. </summary>
            public void InitiateSmoothSnap()
            {
                this.snapTimer = 0; //reset timer either way
                this.snapComplete = false;
                this.startPosition = this.grabable.MyTransform.position;
                this.startRotation = this.grabable.MyTransform.rotation;
                this.wasEnabled = this.grabable.enabled;
                this.lastParent = this.grabable.MyTransform.parent;
                this.grabable.enabled = false; //lock the object nao.
            }

            /// <summary> Restore this object's parent back to where it was before snapping. </summary>
            /// <param name="forceRestore"></param>
            public void RestoreParent(bool forceRestore = false)
            {
                if (!parentRestored || forceRestore)
                {
                    parentRestored = true;
                    this.grabable.MyTransform.parent = this.lastParent; //restoring the parent causes glitching if you're moving in the Editor.
                }
            }

            /// <summary> During/After snapping; shows the snapping state. Before that, shows the detection state(s). </summary>
            /// <param name="collidersInside"></param>
            /// <param name="zoneScript"></param>
            /// <returns></returns>
            public override string Print(int collidersInside, SG_DropZone zoneScript)
            {
                if (!this.eventFired)
                {
                    return base.Print(collidersInside, zoneScript);
                }
                //While snapping....
                return grabable.name + " (" + collidersInside + ") "
                    + (snapComplete ? "Snapped!" : "Moving...")
                    + (grabable.enabled ? " Grabable." : " Locked.");

            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Locks Object to this zone. You're no longer allowed to grab onto it(!) until another script enables it again. </summary>
        [Header("SnapZone Components")]
        public bool locksObject = true;

        /// <summary> If true, this zone becomes the new parent of the object, until it's picked back up. Useful when you're attaching it to moving objects. </summary>
        public bool useParenting = true;

        /// <summary> If assigned, we'll snap an object's baseTransform to this origin. If not, the object stays floating in space. </summary>
        public Transform snapPoint;

        /// <summary> If true, we snap the object(s) to this zone's snapPoint position.  </summary>
        public bool snapToPosition = true;

        /// <summary> if true, we snap the object(s) to this zone's snapPoint rotation </summary>
        public bool snapToRotation = true;

        /// <summary> Optional component to enable a smooth movement from the held position to the snapped position when the object is detected. 
        /// If left unassinged, the object will snap to the location instantly. </summary>
        public SG_SmoothMovement snapSmoothing;

        /// <summary> Fires when an object finishes snapping to this zone, which occurs at most one FixedUpdate after ObjectDetected is called. 
        /// If you want to do something as soon as it starts snapping, use the ObjectDetected event. </summary>
        public DropZoneEvent ObjectSnapped = new DropZoneEvent();

        //-------------------------------------------------------------------------------------------------------------------------
        // DropZone Overrides

        /// <summary> Apply Snapping Logic to the object before calling the base ObjectDetected method (which calls the event). </summary>
        /// <param name="args"></param>
        protected override void OnObjectDetected(DropZoneArgs args) //fires once CanFirEvent has been triggered. Usually 1 frame after OnTriggerEnter.
        {

            //Ensure the object is no longer held.
            args.grabable.ReleaseSelf();

            //lock the object in place
            args.grabable.IsKinematic = true; //it it has a RigidBody, set IsKinematic to true. Otherwise, it'll just stay floating as well. This safe way will block double events.

            SnapZoneArgs snapArgs = (SnapZoneArgs)args;
            //logs some important info
            snapArgs.InitiateSmoothSnap(); //Always SmoothSnap first. if this obj has no Smoothed snapping, it will fire ObjectSnapped during the next FixedUpdate.
            //from this point on, it's locked.

            if (useParenting) 
            {
                args.grabable.MyTransform.parent = this.ParentTransform;
            }

            //fires the event
            base.OnObjectDetected(args);
        }


        /// <summary> Also restore the object's parent if we haven't already. </summary>
        /// <param name="args"></param>
        protected override void OnObjectRemoved(DropZoneArgs args)
        {
            base.OnObjectRemoved(args);
            ((SnapZoneArgs)args).RestoreParent(); //ensure it's restored
        }


        /// <summary> Fires when the object enters this zone for the first time. Generates SnapZoneArgs instead of DropZoneArgs. </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        protected override DropZoneArgs GenerateZoneArguments(SG_Grabable script)
        {
            return new SnapZoneArgs(script);
        }

        /// <summary> Updates object's location while it is snapping smoothly. Then call the base method for detection. </summary>
        /// <param name="args"></param>
        /// <param name="dT"></param>
        protected override void CheckObject(DropZoneArgs args, float dT)
        {
            if (args.eventFired) //must be detected for us to begin a smooth transition
            {
                SnapZoneArgs snapArgs = (SnapZoneArgs)args;
                if (!snapArgs.snapComplete)
                {
                    snapArgs.snapTimer += dT;
                    if (snapSmoothing == null || snapArgs.snapTimer >= this.snapSmoothing.movementTime) //if any point the smoothing is deleted
                    {
                       // Debug.Log(this.name + ": Finished snapping " + args.grabable + ". You should now be able to pick it up again");
                        this.FinishSnap(snapArgs);
                    }
                    else
                    {
                        //Ideally, I'd use the objects "MoveToLocation" - But it will be Kinematic anyway, and should hold the speed as determine by the SnapZone
                        snapSmoothing.UpdateLocation(snapArgs.grabable.MyTransform, this.snapPoint, snapArgs.startPosition, snapArgs.startRotation, snapArgs.snapTimer);
                    }
                }
                else if (snapArgs.grabable.IsGrabbed()) //this fires when the snapping is complete but the object is grabbed once more.
                {
                   // Debug.Log(this.name + ": " + snapArgs.grabable.name + " is grabbed, restoring the parenting (RB?).");
                    snapArgs.RestoreParent(); //restores if it isn't already. 
                }
            }
            base.CheckObject(args, dT); //fires the event if we haven't already.
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // SnapDropZone Functions

        /// <summary> The transform of this Zone to use as a parent for objects that we're snapping. </summary>
        /// <remarks> we prefer to snap to the snapPoint rather than this Zone, if possible? </remarks>
        public Transform ParentTransform
        {
            get
            {
                return this.snapPoint != null ? this.snapPoint : this.transform;
            }
        }


        /// <summary> Returns true if all objectsToGot in this zone have been snapped. </summary>
        /// <returns></returns>
        public virtual bool AllObjectsSnapped()
        {
            if (this.objectsToGet.Count == 0 || this.objectsToGet.Count != this.detectionArguments.Count) { return false; }
            //Don't go here unless we have at least the correct abount of objects inside.
            for (int i = 0; i < this.detectionArguments.Count; i++)
            {
                if (!((SnapZoneArgs)detectionArguments[i]).snapComplete) //there's at least one that has not yet snapped.
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary> Confirm if a specific Grabable is in the process of being snapped to this zone through a smoothedSnap </summary>
        /// <param name="objToCheck"></param>
        public virtual bool IsMovingToSnap(SG_Grabable objToCheck)
        {
            int snapIndex = ArgumentIndex(objToCheck);
            if (snapIndex > -1)
            {
                SnapZoneArgs args = (SnapZoneArgs)this.detectionArguments[snapIndex];
                return args.eventFired && !args.snapComplete;
            }
            return false;
        }

        /// <summary> Confirm if a specific Grabable is snapped to this zone </summary>
        /// <param name="objToCheck"></param>
        public virtual bool IsSnapped(SG_Grabable objToCheck)
        {
            int snapIndex = ArgumentIndex(objToCheck);
            if (snapIndex > -1)
            {
                SnapZoneArgs args = (SnapZoneArgs)this.detectionArguments[snapIndex];
                return args.snapComplete;
            }
            return false;
        }

        /// <summary> Safely snap a Transform to this SnapZone, using it's 'snap to X' parameters. Does not initate a smooth movement(!). </summary>
        /// <param name="baseTransform"></param>
        public virtual void SnapToMe(Transform baseTransform)
        {
            //now place it at the snap location(?)
            if (this.snapPoint != null && baseTransform != null)
            {
                if (snapToRotation) { baseTransform.rotation = snapPoint.rotation; }
                if (snapToPosition) { baseTransform.position = snapPoint.position; }
            }
        }

        /// <summary> Snap an object to me, and notify the args container that we're finished. Called when not smooth snapping, or when smooth snap finishes. </summary>
        /// <param name="objToSnap"></param>
        protected virtual void FinishSnap(SnapZoneArgs objToSnap)
        {
            SnapToMe(objToSnap.grabable.baseTransform);
            bool wasSnapped = objToSnap.snapComplete;
            objToSnap.snapComplete = true; //no need check for updates anymore.
            objToSnap.grabable.enabled = this.locksObject ? false : objToSnap.wasEnabled; //only lock it if we're meant to do so. Otherwise, just turn it back to the way it was.
            if (!wasSnapped) //wasn't snapped before.
            {
                ObjectSnapped.Invoke(objToSnap.grabable);
            }
        }


        /// <summary> Snaps the Grabable to this SnapZone via programming. You can choose to snap it immedeately, or use the smoothedSnap option(s). </summary>
        /// <param name="grabable"></param>
        /// <param name="snapInstantly"></param>
        /// <returns> Anything over 0 means a succesful snap
        /// -1 - This Grabable cannot be detected by this SnapZone. It did not happen
        /// 0 - This object was already snapped
        /// 1 - The object was moving to the snapPoint - It will snap now / soon
        /// 2 - This object was not detected yet. It will snap now / soon
        /// </returns>
        public virtual int SnapToMe(SG_Grabable grabable, bool snapInstantly)
        {
            if (this.CanDetect(grabable)) //it is one I can detect
            {
                int index = this.ArgumentIndex(grabable);
                SnapZoneArgs snapArgs;
                if (index > -1) //I've already detected this object
                {
                    snapArgs = (SnapZoneArgs) this.detectionArguments[index];
                }
                else //it's never been added the list a.k.a. we've never been detected before. In fact, the object may be too far away to detect (yet).
                {
                    snapArgs = (SnapZoneArgs) this.GenerateZoneArguments(grabable);
                    this.detectionArguments.Add(snapArgs); //normally I'd advide to use AddToList, but that one also checks for ArgumentIndex, which we've already detremined is -1.
                }

                bool wasSnapping = snapArgs.eventFired;
                bool wasSnapped = snapArgs.snapComplete;
                if (!snapArgs.eventFired) //it's not even snapping yet
                {
                    this.OnObjectDetected(snapArgs); //instantly fires the event. Also snaps according to my parameters
                }
                if (!wasSnapping)
                {
                    snapArgs.InitiateSmoothSnap();
                }
                if (!snapArgs.snapComplete && snapInstantly) //if that didn't instantly snap me
                {
                    this.FinishSnap(snapArgs);
                }
                UpdateDebugger(); //report the state(s).

                //Evaluate return params
                if (wasSnapped) { return 0; }
                else if (wasSnapping) { return 1; }
                else if (index < 0) { return 2; }
            }
            return -1;
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        protected override void Awake()
        {
            base.Awake();
            if (this.snapSmoothing == null)
            {
                this.snapSmoothing = this.GetComponent<SG_SmoothMovement>();
            }
        }

        //Runs once. Using it to check for the validity of settings.
        protected virtual void Start()
        {
            if (this.snapPoint == null)
            {
                Debug.LogWarning(this.name + " has no SnapPoint assigned. Objects snapping to this SnapDropZone will lock themselves in the air.", this);
            }
            if (this.objectsToGet.Count != 1)
            {
                Debug.LogWarning(this.name + ": You specified " + objectsToGet.Count + " target objects for this SnapZone. They work best with 1 specific object.", this);
            }
        }

    }
}