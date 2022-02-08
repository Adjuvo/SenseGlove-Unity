using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    [System.Serializable]
    public class DropZoneEvent : UnityEngine.Events.UnityEvent<SG_Grabable> { }

    /// <summary> Collider(s) that detects SenseGlove_Grabable objects within its volume. Use it to detect if your Grabables are in the correct location.
    /// At least one of its colliders should be marked as IsTrigger. </summary>
    public class SG_DropZone : MonoBehaviour
    {
        //-----------------------------------------------------------------------------------------------
        // Drop Zone Parameters

        /// <summary> Special DropZone parameters for each object detected in this volume. </summary>
        /// <remarks> Contained within this class since it'll be rarely used outside of it. </remarks>
        public class DropZoneArgs
        {
            /// <summary> The object that was detected inside the zone. </summary>
            public SG_Grabable grabable;

            /// <summary> How long since the object was inside the zone and < detectionTime. </summary>
            public float eventTimer = 0;

            /// <summary> Whether we fired the Detection Event for this grabable yet. </summary>
            public bool eventFired = false;

            /// <summary> Default Constructor for override </summary>
            protected DropZoneArgs() { }


            /// <summary> Create a new Instance of a DropZoneArgs. </summary>
            /// <param name="detectedScript"></param>
            public DropZoneArgs(SG_Grabable detectedScript)
            {
                grabable = detectedScript;
            }


            /// <summary> Prints the grabable name, the amount of colliders, and how much time before it is detected. </summary>
            /// <remarks> Different from ToString(), because it also takes parameters </remarks>
            /// <param name="collidersInside">The amount of colliders of this Object inside the zone, detected by the ScriptDetector class.</param>
            /// <param name="zoneScript"></param>
            /// <returns></returns>
            public virtual string Print(int collidersInside, SG_DropZone zoneScript)
            {
                return grabable.name + " (" + collidersInside + ") "
                    + (grabable.IsGrabbed() ? "Being Held " : " ")
                    + (eventFired ? "Detected!" : System.Math.Round(eventTimer, 2) + "/" + zoneScript.detectionTime + "s");
            }

        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Targets that must be inside this DropZone. If assigned, this Zone will ony respond to those objects. If empty, it will register any object. </summary>
        [Header("DropZone Components")]
        public List<SG_Grabable> objectsToGet = new List<SG_Grabable>();

        //Splitting the functionality into two: One script (colliderDetection) is solely responsible for decting "in zone or not". Another set is responsible for the event arguments / logic.
        //Both run parallel to each other (the sizes/indices should be the same).

        /// <summary> Flexible detector list that will handle most of the detection work. We're only interested in the Grabable scripts it provides. </summary>
        protected SG_ScriptDetector<SG_Grabable> colliderDetection = new SG_ScriptDetector<SG_Grabable>();
        /// <summary> Runs parallel to the scripts in the zone. Custom arguments related to event firing and snapping. </summary>
        protected List<DropZoneArgs> detectionArguments = new List<DropZoneArgs>();


        /// <summary> The time (in s) that a Grabable must be inside this zone before it is detected. </summary>
        [Tooltip("The time (in s) that a Grabable must be inside this zone before it is detected.")]
        public float detectionTime = 0.2f;

        /// <summary> Determines if objects that are still being held are detected. </summary>
        [Tooltip("Determines if objects that are still being grabbed are detected [true], or if they must be released first [false].")]
        public bool detectHeldObjects = true;

        /// <summary> Optional Highlight to toggle zone indicators / object previes / etc. Access via HighLightEnabled. </summary>
        public SG_Activator highlighter;

        /// <summary> Optional Component to display all objects touched by this collider. </summary>
        public TextMesh debugTxt;

        /// <summary> Physics Colliders attached to this Zone. Used to disable collision with fingers. </summary>
        /// <remarks>They are cached here after requesting them once, so that we don't need to look for them again.</remarks>
        private Collider[] myColliders = null;

        /// <summary> Fires when an object is detected by this zone (it's in there for at least the detectionTime). This also indicates when snapping Starts </summary>
        public DropZoneEvent ObjectDetected = new DropZoneEvent();
        /// <summary> Fires when an object is removed from this zone. </summary>
        public DropZoneEvent ObjectRemoved = new DropZoneEvent();

        private bool setup = true;

        //-------------------------------------------------------------------------------------------------------------------------
        // Basic DropZone Functions


        /// <summary> Returns the number of objects currently inside this DropZone </summary>
        /// <returns></returns>
        public int ObjectsInZoneCount()
        {
            return this.colliderDetection.DetectedCount();
        }

        /// <summary> Returns all SG_Grabable objects within this DropZone. </summary>
        /// <returns></returns>
        public SG_Grabable[] ObjectsInZone()
        {
            return this.colliderDetection.GetDetectedScripts();
        }

        /// <summary> Returns true if this Zone is allwed to detect a certain Grabable. </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool CanDetect(SG_Grabable obj)
        {
            return this.objectsToGet.Count == 0 || this.objectsToGet.Contains(obj);
        }

        /// <summary> Returns true if obj is in this zone, and has fired its ObjectDetected event. It must, of course be detectable by this zone in the firts place. (check using CanDetect(obj). </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool IsDetecting(SG_Grabable obj)
        {
            int detectionIndex = this.colliderDetection.DetectionIndex(obj);
            return detectionIndex > -1 && detectionArguments[detectionIndex].eventFired; //fully detected 
        }

        /// <summary> Returns true if all ObjectsToGet for this zone have been officially detected. </summary>
        /// <returns></returns>
        public virtual bool AllObjectsDetected()
        {
            if (this.objectsToGet.Count == 0 || this.objectsToGet.Count != this.detectionArguments.Count) { return false; }
            //Don't go here unless we have at least the correct abount of objects inside.
            for (int i=0; i<this.detectionArguments.Count; i++)
            {
                if (!detectionArguments[i].eventFired) //there's at least one whose event has not yet fired.
                {
                    return false;
                }
            }
            return true;
        }
        


        //-------------------------------------------------------------------------------------------------------------------------
        // Collision / Trigger Logic

        /// <summary> Returns the index of a specific Grabable within our detectionArguments. Used to find a maching index to collisionDetection. </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected int ArgumentIndex(SG_Grabable obj)
        {
            for (int i=0; i<this.detectionArguments.Count; i++)
            {
                if (detectionArguments[i].grabable == obj)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary> Generates new DropZoneArgs to add to the list. Called as soon as the first collider enters the volume. Override this if you want to add custom DropZoneArgs. </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        protected virtual DropZoneArgs GenerateZoneArguments(SG_Grabable script)
        {
            return new DropZoneArgs(script);
        }

        /// <summary> Called OnTirggerEnter. Attempts to add a collider to our ColliderDetection script. If succesful, we also generate Detection Arguments. </summary>
        /// <param name="col"></param>
        protected virtual void TryAddCollider(Collider col)
        {
            //Step1 : Check if it has an Interactable Scrip attached.
            SG_Grabable interactable;
            if ( Util.SG_Util.GetScript(col, out interactable) )
            {
                if (!interactable.KinematicChanged)
                {
                   // Debug.Log(this.name + " (" + Time.timeSinceLevelLoad + ") Colliding with " + interactable.name + "(" + col.name + ") without changing Kinematicisim");
                    if (this.CanDetect(interactable)) //this is an object relevant to us. Hurray!
                    {
                        //Step 2 : Check if this is one relevant to us
                        int collidersInZone = this.colliderDetection.AddToList(interactable, col, this); //returns the (new) number of colliders associated with this script
                        if (collidersInZone == 1) //this is the first collider of this script we've found. Do something!
                        {
                            DropZoneArgs args = GenerateZoneArguments(interactable);
                            AddToList(args);
                            UpdateDetections(0); //check them at t=0;
                            UpdateDebugger();
                            //Debug.Log(args.grabable.name + " entered " + this.name + " with its first collider.");
                        }
                    }
                }
            }
        }

        /// <summary> Actually adds a DropzoneArg to the list. Override this if you wish to intercept it. It's in a separate function in case you wish to force detection. </summary>
        /// <param name="args"></param>
        protected virtual void AddToList(DropZoneArgs args)
        {
            int index = ArgumentIndex(args.grabable);
            if (index < 0)
            {
                this.detectionArguments.Add(args);
            }
        }

        /// <summary> Called during OnTriggerExit. Attempt to remove a collider from colliderDetection. If that's the last collider, remove the matching DropZoneArgs. </summary>
        /// <param name="col"></param>
        protected virtual void TryRemoveCollider(Collider col)
        {
            SG_Grabable interactable;
            if (Util.SG_Util.GetScript(col, out interactable)) // a Grabable just left us
            {
                if (!interactable.KinematicChanged)
                {
                    int removeCode = this.colliderDetection.RemoveFromList(col, interactable);
                    if (removeCode == 2) //returns 2 if the script has been completely removed - last collider exited.
                    {
                        //Debug.Log(interactable.name + " was completely removed from the zone.");
                        int argIndex = ArgumentIndex(interactable); //find out my detectionArgs linked to that Grabable. As they run parallel, I should NEVER have an outofRangeException. Leaving this here to warn me if it does.
                        DropZoneArgs args = detectionArguments[argIndex];
                        this.detectionArguments.RemoveAt(argIndex);
                        this.OnObjectRemoved(args); //let the script know it's gone.
                        UpdateDebugger();
                    }
                }
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Detection Logic

        /// <summary> Returns true if a particular DropZoneArgs is elligible to be detected. Can be overriden to include proximity / rotation matching. </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool CanFireEvent(DropZoneArgs args)
        {
            return this.detectHeldObjects || !args.grabable.IsGrabbed();
        }

        /// <summary> Returns true if the detectionTimer should be reset when CanFireEvent() is false. e.g. If I grab back onto the object for a dropzone that doesn't detect held objects. </summary>
        /// <returns></returns>
        public virtual bool ResetsTimer()
        {
            return false;
        }

        /// <summary> Check all DropZoneArgs to see if we can fire and event or simply update the object itself. </summary>
        /// <returns></returns>
        protected virtual void UpdateDetections(float dT)
        {
            for (int i=0; i<this.detectionArguments.Count; i++)
            {
                CheckObject(detectionArguments[i], dT);
            }
        }

        /// <summary> Checks activation / event firing of a single object within this zone. </summary>
        /// <remarks> Placed in a single function so I can override it in subclasses without doubling the for loops. </remarks>
        /// <param name="args"></param>
        /// <param name="dT"></param>
        protected virtual void CheckObject(DropZoneArgs args, float dT)
        {
            if (!args.eventFired) //this one has not been 'detected' yet
            {
                if (args.eventTimer < this.detectionTime)
                {
                    args.eventTimer += dT; 
                }
                else // capping the value at detectionTime so as to avoid any overflow. I know, that would require someone to hover for weeks. But I'm paranoid.
                { args.eventTimer = detectionTime; }

                if (CanFireEvent(args)) //I'm in the correct location at the moment
                {
                    if (args.eventTimer >= this.detectionTime)
                    {
                        this.OnObjectDetected(args);
                    }
                }
                else if (this.ResetsTimer())
                {
                    args.eventTimer = 0;
                }
            }
        }

        /// <summary> Fires when we have determined that this object is 'detected' as by our base rules (it's not held and in the zone for X amount of time. </summary>
        /// <param name="args"></param>
        protected virtual void OnObjectDetected(DropZoneArgs args)
        {
            //Fire the event
            args.eventFired = true; //let it know the event should fire.
            //Debug.Log("FIRING DETECT EVENT for " + args.grabable);
            ObjectDetected.Invoke(args.grabable);
        }


        /// <summary> Fires when an object is removed from this zone </summary>
        /// <param name="args"></param>
        protected virtual void OnObjectRemoved(DropZoneArgs args)
        {
            if (args.eventFired) //at was removed AFTER wewere able to officially detect it.
            {
                //Debug.Log("FIRING REMOVED EVENT for " + args.grabable + " evenfired = " + args.eventFired + ", grabableEnabled = " + args.grabable.enabled);
                ObjectRemoved.Invoke(args.grabable);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Utility Functions


        /// <summary> Runs initial SetupZone() function, but only ever runs it once. So you can safely call this function 100x. </summary>
        public void Setup()
        {
            if (setup)
            {
                setup = false;
                this.SetupZone();
            }
        }

        /// <summary> Actually sets up the zone and its components. </summary>
        protected virtual void SetupZone()
        {
            Util.SG_PhysicsHelper.AddCollidersToIgnore(this.GetColliders()); //ignore any HandBones from the SG_HandPhysics script, which may become part of the object.
            if (this.highlighter == null)
            {
                this.highlighter = this.GetComponent<SG_Activator>();
            }
            UpdateDebugger(); //clears the text
        }

        /// <summary> Collect the colliders attached to this GameObject and its children. Fires once. </summary>
        /// <remarks> Using List<> instead of Array for this one since it's easier to add to in overrided functions </remarks>
        /// <returns></returns>
        protected virtual List<Collider> CollectColliders()
        {
            List<Collider> res = new List<Collider>();
            Util.SG_Util.GetAllColliders(this.gameObject, ref res);
            return res;
        }

        /// <summary> Retrieve a list of all colliders attached to this GameObject and its direct children. </summary>
        /// <returns></returns>
        public Collider[] GetColliders()
        {
            if (this.myColliders == null)
            {
                this.myColliders = CollectColliders().ToArray();
            }
            return this.myColliders;
        }

        /// <summary> Safely set the highlight / preview of this zone on or off. </summary>
        /// <param name="active"></param>
        public void SetHighlight(bool active)
        {
            if (this.highlighter != null)
            {
                this.highlighter.Activated = active;
            }
        }

        /// <summary> Print all objects and their amount of colliders as a string. </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
        public virtual string PrintDetections(string delim = "\n")
        {
            //inZone and detectionArgumens should run parallel. 
            string res = "";
            for (int i = 0; i < detectionArguments.Count; i++)
            {
                int collidersIndex = colliderDetection.ColliderCount(detectionArguments[i].grabable); //make sure to look for it as opposed to copying the index.
                res += detectionArguments[i].Print(collidersIndex, this);
                if (i < detectionArguments.Count - 1) { res += delim; }
            }
            return res;
        }

        /// <summary> Debug Text to show all detected colliders. </summary>
        public string ContentText
        {
            get { return debugTxt != null ? debugTxt.text : ""; }
            set { if (debugTxt != null) { debugTxt.text = value; } }
        }

        /// <summary> Update the Debug Text to represent the number of objects within this zone, and their paramaters. </summary>
        public void UpdateDebugger()
        {
            if (debugTxt != null) //Don't even start the for loop if we don't have a text assigned.
            {
                ContentText = PrintDetections();
            }
        }

        /// <summary> Ensure that if scripts have been deleted, removed or no longer detectable, that we throw them out of our lists. </summary>
        protected virtual void ValidateObjects()
        {
            int deletedScripts;
            this.colliderDetection.ValidateDetectedObjects(this, out deletedScripts, true, true); //if colliders go from >0 to 0, remove it. But is it was already 0, we're moving towards it.
            if (deletedScripts > 0) //we've removed at least one script(!) and it's not a collider that already had 0?
            {
                for (int i = 0; i < this.detectionArguments.Count;)
                {
                    if (detectionArguments[i].grabable == null ////Grabable was deleted.
                        || !colliderDetection.IsDetected(detectionArguments[i].grabable)) // OR it no longer occus in detectedColliders because a collider was deleted.
                    {
                        detectionArguments.RemoveAt(i);
                      //  Debug.Log("Cleaning a deleted script at index " + i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        protected virtual void Awake()
        {
            this.Setup();
        }

        protected virtual void FixedUpdate()
        {
            if (detectionArguments.Count > 0) //it's really only relelevant if we are holding on to anything.
            {
                ValidateObjects();
                UpdateDetections(Time.deltaTime);
                UpdateDebugger(); //because we also show detection parameters, we update all the time.
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            TryAddCollider(other);
        }


        protected virtual void OnTriggerExit(Collider other)
        {
            TryRemoveCollider(other);
        }

    }

}