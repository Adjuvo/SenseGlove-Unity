using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace SG
{

    /// <summary> A class to detect a SG_TrackedHand based on its SG_Feedback colliders </summary>
    [RequireComponent(typeof(Collider))]
    public class SG_HandDetector : MonoBehaviour
    {

        /// <summary> How a SG_TrackedHand is detected through its Feedback scripts. </summary>
        public enum DetectionType
        {
            /// <summary> This zone detects any finger. </summary>
            AnyFinger = 0,
            /// <summary> This zone only detects specific fingers. </summary>
            SpecificFingers
        }


        /// <summary> EventArgs fired when a glove is detected in or removed from a SenseGlove_Detector. </summary>
        public class GloveDetectionArgs : System.EventArgs
        {
            /// <summary> The TrackedHand that caused the event to fire. </summary>
            public SG_TrackedHand trackedHand;

            /// <summary> Create a new instance of the SenseGlove Detection Arguments </summary>
            /// <param name="grab"></param>
            public GloveDetectionArgs(SG_TrackedHand hand)
            {
                this.trackedHand = hand;
            }

        }

        /// <summary> Contains internal detection arguments for a single glove. </summary>
        public class HandDetectArgs
        {
            //parameters

            /// <summary> The trackedHand we've detected, grants access to all of its layers. </summary>
            public SG_TrackedHand TrackedHand
            {
                get; private set;
            }


            /// <summary> The HapticGlove linked to the detected hand. </summary>
            public SG_HapticGlove Glove
            {
                get { return TrackedHand.gloveHardware; }
            }

            /// <summary> How long this hand is inside the detection zone for. </summary>
            public float DetectionTime
            {
                get; set;
            }

            /// <summary> If true, this instance's HandDetected Event has already been fired. </summary>
            public bool EventFired
            {
                get; set;
            }


            /// <summary> The number of colliders of each finger we've detected for this TrackedHand. </summary>
            private int[] FingerColliders;

            /// <summary> The number of wrist colliders we've detected for this TrackedHand. </summary>
            public int WristColliders
            {
                get; private set;
            }

            /// <summary> The number of colliders we've detcted that don't belong to a wrist or a finger. </summary>
            public int OtherColliders
            {
                get; private set;
            }

            /// <summary> The total number of colliders of this TrackedHand inside the zone. If this reaches 0, the hand has exited. </summary>
            public int TotalColliders
            {
                get; private set;
            }



            // Accessors

            /// <summary> Which fingers of the TrackedHand are inside of this zone. </summary>
            /// <returns></returns>
            public bool[] FingersInside()
            {
                bool[] res = new bool[5];
                for (int f=0; f<FingerColliders.Length; f++)
                {
                    res[f] = FingerColliders[f] > 0;
                }
                return res;
            }

            /// <summary> Returns true if the wrist of this hand is inside the zone. </summary>
            /// <returns></returns>
            public bool WristInside()
            {
                return WristColliders > 0;
            }
            
            // Construction.

            /// <summary> Create a new instance of a detectedHand without any colliders. </summary>
            /// <param name="detectedHand"></param>
            public HandDetectArgs(SG_TrackedHand detectedHand)
            {
                TrackedHand = detectedHand;
                EventFired = false;
                DetectionTime = 0;
                FingerColliders = new int[5];
                WristColliders = 0;
                TotalColliders = 0;
            }

            /// <summary> Register a newly detcted collider to this hand. </summary>
            /// <param name="touch"></param>
            public void AddCollider(SG_BasicFeedback touch)
            {
                if (touch.handLocation == SG_HandSection.Unknown)
                {
                    OtherColliders += 1;
                }
                else if (touch.handLocation == SG_HandSection.Wrist)
                {
                    WristColliders += 1;
                }
                else
                {
                    int index = (int)touch.handLocation;
                    FingerColliders[index] += 1;
                }
                TotalColliders += 1;
            }

            /// <summary> Register that a collider of this hand has been removed from the zone. </summary>
            /// <param name="touch"></param>
            public void RemoveCollider(SG_BasicFeedback touch)
            {
                if (touch.handLocation == SG_HandSection.Unknown)
                {
                    OtherColliders -= 1;
                }
                else if (touch.handLocation == SG_HandSection.Wrist)
                {
                    WristColliders -= 1;
                }
                else
                {
                    int index = (int)touch.handLocation;
                    FingerColliders[index] -= 1;
                }
                TotalColliders -= 1;
            }

            /// <summary> Prints the objects inside of the zone, with the detection parameters. </summary>
            /// <returns></returns>
            public string PrintDetected()
            {
                string res = "Detected " + this.Glove.gameObject.name + ": dT = " + (Math.Round(DetectionTime, 2).ToString()) +  "s [";
                for (int f=0; f<this.FingerColliders.Length; f++)
                {
                    res += FingerColliders[f].ToString();
                    if (f < 4) { res += ", "; }
                }
                return res + "] + " + WristColliders + " => " + TotalColliders.ToString();
            }

        }



        //--------------------------------------------------------------------------------------------------------------------------
        // Properties

        #region Properties

        // Public Properties.

        /// <summary>  General Colliders or Specific fingers. </summary>
        [Tooltip("The method for detection")]
        public DetectionType detectionMethod = DetectionType.AnyFinger;

        /// <summary> How many SG_Feedback colliders must enter the Detector before the GloveDetected event is raised. </summary>
        [Tooltip("How many SG_Feedback colliders can enter the Detector before the GloveDetected event is raised.")]
        public int activationThreshold = 1;

        /// <summary> Whether or not this detector is activated by a thumb when detecting specific fingers only.</summary>
        [Tooltip("Whether or not this detector is activated by a thumb")]
        public bool detectThumb = true;

        /// <summary> Whether or not this detector is activated by an index finger when detecting specific fingers only. </summary>
        [Tooltip("Whether or not this detector is activated by an index finger")]
        public bool detectIndex = true;

        /// <summary> Whether or not this detector is activated by a middle finger when detecting specific fingers only. </summary>
        [Tooltip("Whether or not this detector is activated by a middle finger")]
        public bool detectMiddle = true;

        /// <summary> Whether or not this detector is activated by a ring finger when detecting specific fingers only.</summary>
        [Tooltip("Whether or not this detector is activated by a ring finger")]
        public bool detectRing = true;

        /// <summary> Whether or not this detector is activated by a pinky finger when detecting specific fingers only. </summary>
        [Tooltip("Whether or not this detector is activated by a pinky finger")]
        public bool detectPinky = true;

        /// <summary> Optional: The time in seconds that the Sense Glove must be inside the detector for before the GloveDetected event is called. </summary>
        [Tooltip("Optional: The time in seconds that the Sense Glove must be inside the detector for before the GloveDetected event is called. Set to 0 to ignore.")]
        public float activationTime = 0;

        /// <summary> If set to true, the detector will not raise events if a second handModel joins in.  </summary>
        [Tooltip("If set to true, the detector will not raise events if a second handModel joins in.")]
        public bool singleGlove = false;

        /// <summary> An optional Highlight of this Detector that can be enabled / disabled. </summary>
        [Tooltip("An optional Highlight of this Detector that can be enabled / disabled.")]
        public Renderer highLight;

        // Internal Properties.

        /// <summary> List of the detected hands inside this zone. </summary>
        protected List<HandDetectArgs> detectedHands = new List<HandDetectArgs>();

        /// <summary> The collider of this detection area. Assigned on startup </summary>
        private Collider myCollider;

        /// <summary> The rigidbody of this detection area. Assigned on StartUp </summary>
        private Rigidbody myRigidbody;

        #endregion Properties

        

        //--------------------------------------------------------------------------------------------------------------------------
        // Collision Detection

        #region Collision

        void OnTriggerEnter(Collider col)
        {
            SG_BasicFeedback touch = col.GetComponent<SG_BasicFeedback>();
            if (touch && touch.feedbackScript && touch.feedbackScript.TrackedHand) //needs to have a grabscript attached.
            {
                int scriptIndex = this.HandModelIndex(touch.feedbackScript.TrackedHand);

                //#1 - Check if it belongs to a new or existing detected glove.
                if (scriptIndex < 0)
                {
                    if (ValidScript(touch))
                    {
                        //SG_Debugger.Log("New Grabscript entered.");
                        this.AddEntry(touch.feedbackScript.TrackedHand);
                        scriptIndex = this.detectedHands.Count - 1;
                        this.detectedHands[scriptIndex].AddCollider(touch);
                    }
                }
                else
                {
                    if (ValidScript(touch))
                    {
                        //SG_Debugger.Log("Another collider for grabscript " + scriptIndex);
                        this.detectedHands[scriptIndex].AddCollider(touch);
                    }
                }

                //if no time constraint is set, raise the event immediately!
                if (this.activationTime <= 0 && scriptIndex > -1 && scriptIndex < detectedHands.Count && this.detectedHands[scriptIndex].TotalColliders == this.activationThreshold)
                {
                    //SG_Debugger.Log("ActivationThreshold Reached!");
                    if (!(detectedHands[scriptIndex].EventFired) && !(this.singleGlove && this.detectedHands.Count > 1))
                    {
                        this.detectedHands[scriptIndex].EventFired = true;
                        this.FireDetectEvent(this.detectedHands[scriptIndex].TrackedHand);
                    }
                }

            }
        }

        void OnTriggerExit(Collider col)
        {
            SG_BasicFeedback touch = col.GetComponent<SG_BasicFeedback>();
            if (touch && touch.feedbackScript && touch.feedbackScript.TrackedHand)
            {
                //SG_Debugger.Log("Collider Exits");
                int scriptIndex = this.HandModelIndex(touch.feedbackScript.TrackedHand);
                if (scriptIndex < 0)
                {
                    //SG_Debugger.Log("Something went wrong with " + this.gameObject.name);
                    //it is likely the palm collider.
                }
                else
                {   //belongs to an existing SenseGlove.
                    if (ValidScript(touch))
                    {
                        this.detectedHands[scriptIndex].RemoveCollider(touch);
                        if (this.detectedHands[scriptIndex].TotalColliders <= 0)
                        {
                            //raise release event.
                            //SG_Debugger.Log("Escape!");
                            if (detectedHands[scriptIndex].EventFired && !(this.singleGlove && this.detectedHands.Count > 1)) //only fire if the last glove has been removed.
                            {
                                this.FireRemoveEvent(this.detectedHands[scriptIndex].TrackedHand);
                            }
                            this.RemoveEntry(scriptIndex);
                        }
                    }
                }
            }
        }

        #endregion Collision

        //--------------------------------------------------------------------------------------------------------------------------
        // Logic

        #region Logic

        /// <summary> Set the highlight of this Sense Glove on or off. </summary>
        /// <param name="active"></param>
        public void SetHighLight(bool active)
        {
            if (this.highLight != null)
            {
                this.highLight.enabled = active;
            }
        }

        /// <summary> Set the highlight of this Sense Glove on or off </summary>
        public bool HighlightEnabled
        {
            get { return this.highLight != null && this.highLight.enabled; }
            set { if (this.highLight != null) { this.highLight.enabled = value; }  }
        }


        /// <summary> Returns the index of the SG_HandAnimator in this detector's detectedGloves. Returns -1 if it is not in the list. </summary>
        /// <param name="grab"></param>
        /// <returns></returns>
        private int HandModelIndex(SG_TrackedHand model)
        {
            for (int i = 0; i < this.detectedHands.Count; i++)
            {
                if (GameObject.ReferenceEquals(model.gameObject, this.detectedHands[i].TrackedHand.gameObject)) { return i; }
            }
            return -1;
        }

        /// <summary> Add a newly detected SenseGlove to the list of detected gloves. </summary>
        /// <param name="model"></param>
        private void AddEntry(SG_TrackedHand model)
        {
            this.detectedHands.Add(new HandDetectArgs(model));
        }

        /// <summary> Remove a handmodel at the specified index from the list of detected gloves. </summary>
        /// <param name="scriptIndex"></param>
        private void RemoveEntry(int scriptIndex)
        {
            if (scriptIndex > -1 && scriptIndex < detectedHands.Count)
            {
                this.detectedHands.RemoveAt(scriptIndex);
            }
        }


        /// <summary> Returns true if there is a Sense Glove contained within this detector. </summary>
        /// <returns></returns>
        public bool ContainsSenseGlove()
        {
            for (int i=0; i<this.detectedHands.Count; i++)
            {
                if (detectedHands[i].EventFired) { return true; } //returns true for the first glove that has fired its event.
            }
            return false;
        }

        /// <summary> Get a list of all Haptic Glove Hardware within this detection area. </summary>
        /// <returns></returns>
        public SG_HapticGlove[] GlovesInside()
        {
            SG_HapticGlove[] res = new SG_HapticGlove[this.detectedHands.Count];
            for (int i=0; i<this.detectedHands.Count; i++)
            {
                res[i] = this.detectedHands[i].Glove;
            }
            return res;
        }

        /// <summary> Get a list of all SG_TrackedHands within this detection area. </summary>
        /// <returns></returns>
        public SG_TrackedHand[] HandsInside()
        {
            SG_TrackedHand[] res = new SG_TrackedHand[this.detectedHands.Count];
            for (int i = 0; i < this.detectedHands.Count; i++)
            {
                res[i] = this.detectedHands[i].TrackedHand;
            }
            return res;
        }

        /// <summary> Access not only the glove within this collider, but also its detection parameters </summary>
        /// <returns></returns>
        public HandDetectArgs[] GetDetected()
        {
            return this.detectedHands.ToArray();
        }

        /// <summary> Returns true if a SG_TrackedHand is detected by this zone. </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        public bool IsDetected(SG_TrackedHand hand)
        {
            if (hand != null)
            {
                for (int i = 0; i < this.detectedHands.Count; i++)
                {
                    if (detectedHands[i].TrackedHand == hand && detectedHands[i].EventFired) { return true; } //returns true but only if the glove has fires its event.
                }
            }
            return false;
        }


        /// <summary> Check if this scriptIndex is detectable by this Detector. </summary>
        /// <param name="scriptIndex"></param>
        /// <returns></returns>
        private bool ValidScript(SG_HandSection handSection)
        {
            return (this.detectThumb && handSection == SG_HandSection.Thumb)
                || (this.detectIndex && handSection == SG_HandSection.Index)
                || (this.detectMiddle && handSection == SG_HandSection.Middle)
                || (this.detectRing && handSection == SG_HandSection.Ring)
                || (this.detectPinky && handSection == SG_HandSection.Pinky);
        }

        /// <summary> Returns true if this is a valid script. </summary>
        /// <param name="touch"></param>
        /// <returns></returns>
        private bool ValidScript(SG_BasicFeedback touch)
        {
            return this.detectionMethod == DetectionType.AnyFinger
                        || (this.detectionMethod == DetectionType.SpecificFingers && this.ValidScript(touch.handLocation));
        }


        #endregion Logic

        //--------------------------------------------------------------------------------------------------------------------------
        // Events

        #region Events

        /// <summary> A step in between events that can be overridden by sub-classes of the SenseGlove_Detector </summary>
        /// <param name="model"></param>
        protected virtual void FireDetectEvent(SG_TrackedHand hand)
        {
            this.OnGloveDetected(hand);
        }

        /// <summary> A step in between events that can be overridden by sub-classes of the SenseGlove_Detector </summary>
        /// <param name="model"></param>
        protected virtual void FireRemoveEvent(SG_TrackedHand hand)
        {
            this.OnGloveRemoved(hand);
        }

        /// <summary> Event Handler for new Glove Detections </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public delegate void GloveDetectedEventHandler(object source, GloveDetectionArgs args);
        
        /// <summary> Fires when a new SG_TrackedHand enters this detection zone and fullfils the detector's conditions. </summary>
        public event GloveDetectedEventHandler GloveDetected;

        /// <summary> Fire the GloveDetected event </summary>
        /// <param name="hand"></param>
        protected void OnGloveDetected(SG_TrackedHand hand)
        {
            if (GloveDetected != null)
            {
                GloveDetected(this, new GloveDetectionArgs(hand));
            }
        }

        /// <summary> Event Handler for Glove removals. </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public delegate void OnGloveRemovedEventHandler(object source, GloveDetectionArgs args);

        /// <summary>Fires when a SG_TrackedHand exits this detection zone and fullfils the detector's conditions.  </summary>
        public event OnGloveRemovedEventHandler GloveRemoved;

        /// <summary> Fire the GlloveRemoved event </summary>
        /// <param name="hand"></param>
        protected void OnGloveRemoved(SG_TrackedHand hand)
        {
            if (GloveRemoved != null)
            {
                GloveRemoved(this, new GloveDetectionArgs(hand));
            }
        }

        #endregion Events

        /// <summary> Remove all of the references to trackedHands inside this list. </summary>
        public void ResetParameters()
        {
            int failsafe = this.detectedHands.Count;
            int count = 0;
            while (count < failsafe && detectedHands.Count > 0)
            {
                this.RemoveEntry(0);
                count++;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        #region Monobehaviour

        // Use this for initialization
        protected virtual void Start()
        {
            //add a rigidbody if not already present?
            myCollider = this.GetComponent<Collider>();
            myRigidbody = this.GetComponent<Rigidbody>();

            if (myCollider)
            {
                myCollider.isTrigger = true;
            }
            if (myRigidbody)
            {
                myRigidbody.useGravity = false;
                myRigidbody.isKinematic = true;
            }
        }

        // Updates every frame, used to raise event(s).
        protected virtual void LateUpdate()
        {
            for (int i = 0; i < this.detectedHands.Count; i++)
            {
                if (this.detectedHands[i].TotalColliders >= this.activationThreshold)
                {
                    if (this.detectedHands[i].DetectionTime <= this.activationTime)
                    {
                        this.detectedHands[i].DetectionTime += Time.deltaTime;
                    }
                    if (this.detectedHands[i].DetectionTime >= this.activationTime && !(detectedHands[i].EventFired) && !(this.singleGlove && this.detectedHands.Count > 1))
                    {
                        this.FireDetectEvent(this.detectedHands[i].TrackedHand);
                        this.detectedHands[i].EventFired = true;
                    }
                }
                // Debug.Log(detectedHands[i].ToString());
            }
        }

        #endregion Monobehaviour

    }



#if UNITY_EDITOR //used to prevent crashing while building the solution.

    #region Interface

    [CustomEditor(typeof(SG_HandDetector))]
    [CanEditMultipleObjects]
    public class SenseGloveDetectorEditor : Editor
    {
        /// <summary> Properties to check for changes and for multi-object editing. </summary>
        protected SerializedProperty _detectType, _activationThresh, _detectThumb, _detectIndex, _detectMiddle, _detectRing, _detectPinky;

        /// <summary> Properties to check for changes and for multi-object editing. </summary>
        protected SerializedProperty _activationTime, _singleGlove, _highLight;

        /// <summary> Style for the Material, Breakable and Haptic Feedback properties. </summary>
        protected FontStyle headerStyle = FontStyle.Bold;

        //Tooltips are kept as static readonly so they are only instantialized once for the entire session, for each script.
        private static readonly GUIContent l_detectType = new GUIContent("Detection Type\t", "The method for detection: General Colliders or Specific fingers.");
        private static readonly GUIContent l_activationThresh = new GUIContent("Activation Threshold\t", " How many SG_Feedback colliders must enter the Detector before the GloveDetected event is raised.");

        private static readonly GUIContent l_detectThumb = new GUIContent("Detects Thumb\t", "Whether or not this detector is activated by a thumb when detecting specific fingers only.");
        private static readonly GUIContent l_detectIndex = new GUIContent("Detects Index\t", "Whether or not this detector is activated by an index finger when detecting specific fingers only.");
        private static readonly GUIContent l_detectMiddle = new GUIContent("Detects Middle\t", "Whether or not this detector is activated by a middle finger when detecting specific fingers only.");
        private static readonly GUIContent l_detectRing = new GUIContent("Detects Ring\t", "Whether or not this detector is activated by a ring finger when detecting specific fingers only.");
        private static readonly GUIContent l_detectPinky = new GUIContent("Detects Pinky\t", "Whether or not this detector is activated by a pinky finger when detecting specific fingers only.");

        private static readonly GUIContent l_activationTime = new GUIContent("Activation Time\t", "Optional: The time in seconds that the Sense Glove must be inside the detector for before the GloveDetected event is called");
        private static readonly GUIContent l_singleGlove = new GUIContent("Single Glove\t", " If set to true, the detector will not raise additional events if a second handModel joins in.");
        private static readonly GUIContent l_highlight = new GUIContent("HighLight\t", "An optional Highlight of this Detector that can be enabled / disabled.");

        /// <summary> 
        /// Runs once when the script's inspector is opened. 
        /// Caches all variables to save processing power.
        /// </summary>
        void OnEnable()
        {
            CollectBaseAttributes();
        }

        /// <summary> Placed in a separate method so its child classes can call it as well. </summary>
        protected virtual void CollectBaseAttributes()
        {
            this._detectType = serializedObject.FindProperty("detectionMethod");
            this._activationThresh = serializedObject.FindProperty("activationThreshold");

            this._detectThumb = serializedObject.FindProperty("detectThumb");
            this._detectIndex = serializedObject.FindProperty("detectIndex");
            this._detectMiddle = serializedObject.FindProperty("detectMiddle");
            this._detectRing = serializedObject.FindProperty("detectRing");
            this._detectPinky = serializedObject.FindProperty("detectPinky");

            this._activationTime = serializedObject.FindProperty("activationTime");
            this._singleGlove = serializedObject.FindProperty("singleGlove");
            this._highLight = serializedObject.FindProperty("highLight");
        }

        /// <summary> Called when the inspector is (re)drawn. </summary>
        public override void OnInspectorGUI()
        {
            DrawDefault();
        }

        /// <summary> Method to fully draw a detector based on the conditions set. Placed in a separate model so its children can call it as well. </summary>
        public void DrawDefault()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            var detectorClass = target as SG_HandDetector;
            var origFontStyle = EditorStyles.label.fontStyle;

            ///// Always show the dropdown menu
            ////show as an enum dropdown with the selected option matching the one we've chosen.

            EditorGUI.BeginChangeCheck();
            SetRenderMode(_detectType.hasMultipleDifferentValues);
            detectorClass.detectionMethod = (SG_HandDetector.DetectionType)EditorGUILayout.EnumPopup(l_detectType, detectorClass.detectionMethod);
            SetRenderMode(false, origFontStyle);
            if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
            {
                _detectType.enumValueIndex = (int)detectorClass.detectionMethod;
            }

            //Actually show everything.
            if (!_detectType.hasMultipleDifferentValues)
            {
                CreateIntField(ref detectorClass.activationThreshold, ref _activationThresh, l_activationThresh);

                if (detectorClass.detectionMethod == SG_HandDetector.DetectionType.SpecificFingers)
                {
                    CreateToggle(ref detectorClass.detectThumb, ref _detectThumb, l_detectThumb);
                    CreateToggle(ref detectorClass.detectIndex, ref _detectIndex, l_detectIndex);
                    CreateToggle(ref detectorClass.detectMiddle, ref _detectMiddle, l_detectMiddle);
                    CreateToggle(ref detectorClass.detectRing, ref _detectRing, l_detectRing);
                    CreateToggle(ref detectorClass.detectPinky, ref _detectPinky, l_detectPinky);
                }
            }

            //show general properties
            //activationTime
            CreateFloatField(ref detectorClass.activationTime, ref this._activationTime, l_activationTime);

            //singleglove
            CreateToggle(ref detectorClass.singleGlove, ref this._singleGlove, l_singleGlove);

            //highlight.
            EditorGUILayout.PropertyField(_highLight, l_highlight);

            EditorStyles.label.fontStyle = origFontStyle; //return it to the desired value.

            // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Set the renderer to show a mixed value or not. </summary>
        /// <param name="multipleValues"></param>
        protected void SetRenderMode(bool multipleValues)
        {
            EditorGUI.showMixedValue = multipleValues;
        }

        /// <summary> Set the renderer to show a mixed value and or a different fonr style. </summary>
        /// <param name="multipleValues"></param>
        /// <param name="style"></param>
        protected void SetRenderMode(bool multipleValues, FontStyle style)
        {
            EditorGUI.showMixedValue = multipleValues;
            EditorStyles.label.fontStyle = style;
        }

        /// <summary> Create an input field for an integer, </summary>
        /// <param name="value"></param>
        /// <param name="valueProp"></param>
        /// <param name="label"></param>
        protected void CreateIntField(ref int value, ref SerializedProperty valueProp, GUIContent label)
        {
            this.SetRenderMode(valueProp.hasMultipleDifferentValues);
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.IntField(label, value);
            if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
            {
                valueProp.intValue = value;
            }
            this.SetRenderMode(false);
        }

        /// <summary> Create an input field for a floatign point value </summary>
        /// <param name="value"></param>
        /// <param name="valueProp"></param>
        /// <param name="label"></param>
        protected void CreateFloatField(ref float value, ref SerializedProperty valueProp, GUIContent label)
        {
            this.SetRenderMode(valueProp.hasMultipleDifferentValues);
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.FloatField(label, value);
            if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
            {
                valueProp.floatValue = value;
            }
            this.SetRenderMode(false);
        }

        /// <summary> Create a boolean checkbox </summary>
        /// <param name="value"></param>
        /// <param name="valueProp"></param>
        /// <param name="label"></param>
        protected void CreateToggle(ref bool value, ref SerializedProperty valueProp, GUIContent label)
        {
            this.SetRenderMode(valueProp.hasMultipleDifferentValues);
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
            {
                valueProp.boolValue = value;
            }
            this.SetRenderMode(false);
        }

        /// <summary> Create a toggle with a specified header style. </summary>
        /// <param name="value"></param>
        /// <param name="valueProp"></param>
        /// <param name="label"></param>
        /// <param name="style"></param>
        /// <param name="originalStyle"></param>
        protected void CreateToggle(ref bool value, ref SerializedProperty valueProp, GUIContent label, FontStyle style, FontStyle originalStyle)
        {
            this.SetRenderMode(valueProp.hasMultipleDifferentValues, style);
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
            {
                valueProp.boolValue = value;
            }
            this.SetRenderMode(false, originalStyle);
        }


    }
    #endregion Interface


#endif

}

