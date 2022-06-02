using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{

    [System.Serializable]
    public class HandDetectionEvent : UnityEngine.Events.UnityEvent<SG_TrackedHand> { }

    /// <summary> A Zone that detects TrackedHands, rather than objects </summary>
    public class SG_HandDetector : MonoBehaviour
	{
        /// <summary> Whether a HandDetector responds to left hands, righ hands, or any hand. </summary>
		public enum DetectionType
        {
			Any,
			LeftHandOnly,
			RightHandOnly
        }

        //-----------------------------------------------------------------------------------------------
        // HandDetection Parameters

        /// <summary> Special Hand Detection parameters for each object detected in this volume. </summary>
        /// <remarks> Contained within this class since it'll be rarely used outside of it. </remarks>
        public class HandDetectionArgs
        {
            /// <summary> The hand that was detected inside the zone. </summary>
            public SG_TrackedHand hand;

            /// <summary> How long since the hand was inside the zone and < detectionTime. </summary>
            public float eventTimer = 0;

            /// <summary> Whether we fired the Detection Event for this hand yet. </summary>
            public bool eventFired = false;

            /// <summary> Default Constructor for override </summary>
            protected HandDetectionArgs() { }


            /// <summary> Create a new Instance of a HandDetectionArgs. </summary>
            /// <param name="detectedScript"></param>
            public HandDetectionArgs(SG_TrackedHand detectedHand)
            {
                hand = detectedHand;
            }


            /// <summary> Prints the grabable name, the amount of colliders, and how much time before it is detected. </summary>
            /// <remarks> Different from ToString(), because it also takes parameters </remarks>
            /// <param name="collidersInside">The amount of colliders of this Object inside the zone, detected by the ScriptDetector class.</param>
            /// <param name="zoneScript"></param>
            /// <returns></returns>
            public virtual string Print(int collidersInside, SG_HandDetector zoneScript)
            {
                return hand.name + " (" + collidersInside + ") "
                    + (eventFired ? "Detected!" : System.Math.Round(eventTimer, 2) + "/" + zoneScript.detectionTime + "s");
            }

        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> Targets that must be inside this DropZone. If assigned, this Zone will ony respond to those objects. If empty, it will register any object. </summary>
        [Header("HandDetector Components")]
        public List<SG_TrackedHand> detectableHands = new List<SG_TrackedHand>();

        //Splitting the functionality into two: One script (colliderDetection) is solely responsible for decting "in zone or not". Another set is responsible for the event arguments / logic.
        //Both run parallel to each other (the sizes/indices should be the same).

        /// <summary> Flexible detector list that will handle most of the detection work. We're only interested in the Grabable scripts it provides. </summary>
        protected SG_ScriptDetector<SG_TrackedHand> colliderDetection = new SG_ScriptDetector<SG_TrackedHand>();
        /// <summary> Runs parallel to the scripts in the zone. Custom arguments related to event firing and snapping. </summary>
        protected List<HandDetectionArgs> detectionArguments = new List<HandDetectionArgs>();


        /// <summary> The time (in s) that a Grabable must be inside this zone before it is detected. </summary>
        [Tooltip("The time (in s) that a Grabable must be inside this zone before it is detected.")]
        public float detectionTime = 0.2f;

        //TODO: Detect Specific Fingers?

        /// <summary> Optional Highlight to toggle zone indicators / object previes / etc. Access via HighLightEnabled. </summary>
        public SG_Activator highlighter;

        /// <summary> Optional Component to display all objects touched by this collider. </summary>
        public TextMesh debugTxt;

        /// <summary> Physics Colliders attached to this Zone. Used to disable collision with fingers. </summary>
        /// <remarks>They are cached here after requesting them once, so that we don't need to look for them again.</remarks>
        private Collider[] myColliders = null;

        /// <summary> Fires when a hand is detected by this zone (it's in there for at least the detectionTime).  </summary>
        public HandDetectionEvent HandDetected = new HandDetectionEvent();
        /// <summary> Fires when a hand object is removed from this zone. </summary>
        public HandDetectionEvent HandRemoved = new HandDetectionEvent();


        //-------------------------------------------------------------------------------------------------------------------------
        // Basic DropZone Functions


        /// <summary> Returns the number of objects currently inside this DropZone </summary>
        /// <returns></returns>
        public int HandsInZoneCount()
        {
            return this.colliderDetection.DetectedCount();
        }

        /// <summary> Returns all SG_Grabable objects within this DropZone. </summary>
        /// <returns></returns>
        public SG_TrackedHand[] HandsInZone()
        {
            return this.colliderDetection.GetDetectedScripts();
        }

        /// <summary> returns the amount of hands that are inside the Detection zone and have fired their Detected Events </summary>
        /// <returns></returns>
        public int FullyDetctedCount()
        {
            return this.FullyDetectedHands().Count;
        }
        
        /// <summary> Returns a list of all hands within this zone that have fired their Detected events. </summary>
        /// <returns></returns>
        public List<SG_TrackedHand> FullyDetectedHands()
        {
            List<SG_TrackedHand> hands = new List<SG_TrackedHand>();
            for (int i=0; i<this.detectionArguments.Count; i++)
            {
                if (detectionArguments[i].eventFired)
                { 
                    hands.Add(detectionArguments[i].hand); 
                }
            }
            return hands;
        }

        /// <summary> Returns true if this Zone can detect a specific Grabable. </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Detects(SG_TrackedHand hand)
        {
            return this.detectableHands.Count == 0 || this.detectableHands.Contains(hand);
        }

        /// <summary> Returns true if all DetectedHands for this zone have been officially detected. </summary>
        /// <returns></returns>
        public bool AllhandsDetected()
        {
            if (this.detectableHands.Count == 0 || this.detectableHands.Count != this.detectionArguments.Count) { return false; }
            //Don't go here unless we have at least the correct abount of objects inside.
            for (int i = 0; i < this.detectionArguments.Count; i++)
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

        /// <summary> Returns the index of a specific TrackedHand within our detectionArguments. Used to find a maching index to collisionDetection. </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected int ArgumentIndex(SG_TrackedHand hand)
        {
            for (int i = 0; i < this.detectionArguments.Count; i++)
            {
                if (detectionArguments[i].hand == hand)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary> Generates new HandDetectionArgs to add to the list. Called as soon as the first collider enters the volume. Override this if you want to add custom HandDetectionArgs. </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        protected virtual HandDetectionArgs GenerateZoneArguments(SG_TrackedHand hand)
        {
            return new HandDetectionArgs(hand);
        }

        /// <summary> Called OnTirggerEnter. Attempts to add a collider to our ColliderDetection script. If succesful, we also generate Detection Arguments. </summary>
        /// <param name="col"></param>
        protected virtual void TryAddCollider(Collider col)
        {
            //Step1 : Check if it has an Interactable Scrip attached.
            SG_TrackedHand hand;
            if (SG_HandPhysics.TryGetLinkedHand(col, out hand)) //this collider is attached to the hand
            {
                if (this.Detects(hand)) //this is an object relevant to us. Hurray!
                {
                    //Step 2 : Check if this is one relevant to us
                    int collidersInZone = this.colliderDetection.AddToList(hand, col, this); //returns the (new) number of colliders associated with this script
                    if (collidersInZone == 1) //this is the first collider of this script we've found. Do something!
                    {
                        HandDetectionArgs args = GenerateZoneArguments(hand);
                        AddToList(args);
                        UpdateDetections(0); //check them at t=0;
                        UpdateDebugger();
                        //Debug.Log(args.grabable.name + " entered " + this.name + " with its first collider.");
                    }
                }
            }
        }

        protected virtual void AddToList(HandDetectionArgs args)
        {
            int index = ArgumentIndex(args.hand);
            if (index < 0)
            {
                this.detectionArguments.Add(args);
            }
        }

        /// <summary> Called during OnTriggerExit. Attempt to remove a collider from colliderDetection. If that's the last collider, remove the matching HandDetectionArgs. </summary>
        /// <param name="col"></param>
        protected virtual void TryRemoveCollider(Collider col)
        {
            SG_TrackedHand hand;
            if (SG_HandPhysics.TryGetLinkedHand(col, out hand)) // a Grabable just left us
            {
                int removeCode = this.colliderDetection.RemoveFromList(col, hand);
                if (removeCode == 2) //returns 2 if the script has been completely removed - last collider exited.
                {
                    //Debug.Log(interactable.name + " was completely removed from the zone.");
                    int argIndex = ArgumentIndex(hand); //find out my detectionArgs linked to that Grabable. As they run parallel, I should NEVER have an outofRangeException. Leaving this here to warn me if it does.
                    HandDetectionArgs args = detectionArguments[argIndex];
                    this.detectionArguments.RemoveAt(argIndex);
                    this.OnObjectRemoved(args); //let the script know it's gone.
                    UpdateDebugger();
                }
                //if (this.name.Contains("Zap"))
                //{
                //    Debug.Log(this.name + ": Removing " + col.name + " with parent " + (col.transform.parent != null ? col.transform.parent.name : "NULL") + " with code " + removeCode + ", which was linked to " + hand.name);
                //}
            }
            //else if (this.name.Contains("Zap") && col.transform.parent != null && col.transform.parent.name.Contains("Bone"))
            //{
            //    Debug.LogError("Could not get a Hand for " + col.name + "???");
            //}
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Detection Logic

        /// <summary> Returns true if a particular HandDetection is elligible to be detected. Can be overriden to include specific fingers. </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual bool CanFireEvent(HandDetectionArgs args)
        {
            return true;
        }

        /// <summary> Returns true if the detectionTimer should be reset when CanFireEvent is false. </summary>
        /// <returns></returns>
        public virtual bool ResetsTimer()
        {
            return false;
        }

        /// <summary> Check all HandDetectionArgs to see if we can fire and event or simply update the object itself. </summary>
        /// <returns></returns>
        protected virtual void UpdateDetections(float dT)
        {
            for (int i = 0; i < this.detectionArguments.Count; i++)
            {
                CheckObject(detectionArguments[i], dT);
            }
        }

        /// <summary> Checks activation / event firing of a single object within this zone. </summary>
        /// <remarks> Placed in a single function so I can override it in subclasses without doubling the for loops. </remarks>
        /// <param name="args"></param>
        /// <param name="dT"></param>
        protected virtual void CheckObject(HandDetectionArgs args, float dT)
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
        protected virtual void OnObjectDetected(HandDetectionArgs args)
        {
            //Fire the event
            args.eventFired = true; //let it know the event should fire.
            HandDetected.Invoke(args.hand);
        }


        /// <summary> Fires when an object is removed from this zone </summary>
        /// <param name="args"></param>
        protected virtual void OnObjectRemoved(HandDetectionArgs args)
        {
            if (args.eventFired) //at was removed AFTER wewere able to officially detect it.
            {
                HandRemoved.Invoke(args.hand);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------
        // Utility Functions

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


        public virtual string PrintDetections(string delim = "\n")
        {
            //inZone and detectionArgumens should run parallel. 
            string res = "";
            for (int i = 0; i < detectionArguments.Count; i++)
            {
                int collidersIndex = colliderDetection.ColliderCount(detectionArguments[i].hand); //make sure to look for it as opposed to copying the index.
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

        /// <summary> Ensure that, if  </summary>
        protected virtual void ValidateObjects()
        {
            int deletedScripts;
            this.colliderDetection.ValidateDetectedObjects(this, out deletedScripts, true, false); //if colliders go from >0 to 0, remove it. But is it was already 0, we're moving towards it.
            if (deletedScripts > 0) //we've removed at least one script(!) and it's not a collider that already had 0?
            {
                for (int i = 0; i < this.detectionArguments.Count;)
                {
                    if (detectionArguments[i].hand == null ////Grabable was deleted.
                        || !colliderDetection.IsDetected(detectionArguments[i].hand)) // OR it no longer occus in detectedColliders because a collider was deleted.
                    {
                        detectionArguments.RemoveAt(i);
                        Debug.Log("Cleaning a deleted script at index " + i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        /// <summary> Remove a specific hand from this zone </summary>
        /// <param name="hand"></param>
        public void RemoveDetection(SG.SG_TrackedHand hand)
        {
            if (hand == null)
            {
                return;
            }

            this.colliderDetection.ClearCollisions(hand);
            for (int i=0; i<this.detectionArguments.Count;)
            {
                if (detectionArguments[i].hand == hand)
                {
                    detectionArguments.RemoveAt(i);
                }
                else { i++; }
            }
        }

        /// <summary> Removes all detected hands from this zone </summary>
        public void RemoveDetections()
        {

        }

        //-------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour


        protected virtual void Awake()
        {
            //Do not ignore physics collision with the hand this time (duh)
            if (this.highlighter == null)
            {
                this.highlighter = this.GetComponent<SG_Activator>();
            }
            UpdateDebugger(); //clears the text
        }

        protected virtual void FixedUpdate()
        {
            if (detectionArguments.Count > 0) //it's really only relelevant if we're detecting something.
            {
                ValidateObjects();
                UpdateDetections(Time.deltaTime);
                UpdateDebugger();
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            //if (this.name.Contains("Zap"))
            //{
            //    Debug.Log(this.name + ": Collided with " + other.name);
            //}
            TryAddCollider(other);
        }


        protected virtual void OnTriggerExit(Collider other)
        {
            //if (this.name.Contains("Zap"))
            //{
            //    Debug.Log(this.name + ": " + ": Collided with " + other.name);
            //}
            TryRemoveCollider(other);
        }

    }

}