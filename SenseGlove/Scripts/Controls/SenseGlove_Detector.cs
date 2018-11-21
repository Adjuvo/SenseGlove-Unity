using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>A class to detect a SenseGlove_HandModel based on its SenseGlove_Feedback colliders </summary>
[RequireComponent(typeof(Collider))]
public class SenseGlove_Detector : MonoBehaviour 
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    // Public Properties.

    /// <summary>  General Colliders or Specific fingers. </summary>
    [Tooltip("The method for detection")]
    public DetectionType detectionMethod = DetectionType.AnyFinger;

    /// <summary> How many SenseGlove_Feedback colliders must enter the Detector before the GloveDetected event is raised. </summary>
    [Tooltip("How many SenseGlove_Feedback colliders can enter the Detector before the GloveDetected event is raised.")]
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

    /// <summary> All of the grabscripts currently interacting with this detector, in order of appearance. </summary>
    protected List<SenseGlove_HandModel> detectedGloves = new List<SenseGlove_HandModel>();

    /// <summary> The amount of SenseGlove_Touch colliders of each grabscript that are currently in the detection area </summary>
    private List<int> detectedColliders = new List<int>();

    /// <summary> Used to keep track of the time that each glove have been inside this detector. </summary>
    private List<float> detectionTimes = new List<float>();

    /// <summary> Used to determine if the activationtheshold had been reached before. Prevents the scipt from firing multiple times. </summary>
    protected List<bool> eventFired = new List<bool>();

    /// <summary> The collider of this detection area. Assigned on startup </summary>
    private Collider myCollider;

    /// <summary> The rigidbody of this detection area. Assigned on StartUp </summary>
    private Rigidbody myRigidbody;

    #endregion Properties

    //--------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start () 
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
        for (int i = 0; i < this.detectedGloves.Count; i++)
        {
            if (this.detectedColliders[i] >= this.activationThreshold)
            {
                this.detectionTimes[i] += Time.deltaTime;
                if (this.detectionTimes[i] >= this.activationTime && !(eventFired[i]) && !(this.singleGlove && this.detectedGloves.Count > 1))
                {
                    this.FireDetectEvent(this.detectedGloves[i]);
                    this.eventFired[i] = true;
                }
            }
        }
    }

    #endregion Monobehaviour

    //--------------------------------------------------------------------------------------------------------------------------
    // Collision Detection

    #region Collision

    void OnTriggerEnter(Collider col)
    {
        SenseGlove_Feedback touch = col.GetComponent<SenseGlove_Feedback>();
        if (touch && touch.handModel) //needs to have a grabscript attached.
        {
            int scriptIndex = this.HandModelIndex(touch.handModel);

            //#1 - Check if it belongs to a new or existing detected glove.
            if (scriptIndex < 0)
            {
                if (ValidScript(touch))
                {
                    //SenseGlove_Debugger.Log("New Grabscript entered.");
                    this.AddEntry(touch.handModel);
                    scriptIndex = this.detectedGloves.Count - 1;
                }
            }
            else
            {
                if (ValidScript(touch))
                {
                    //SenseGlove_Debugger.Log("Another collider for grabscript " + scriptIndex);
                    this.detectedColliders[scriptIndex]++;
                }
            }
            
            //if no time constraint is set, raise the event immediately!
            if (this.activationTime <= 0 && this.detectedColliders[scriptIndex] == this.activationThreshold)
            {
                //SenseGlove_Debugger.Log("ActivationThreshold Reached!");
                if (!(eventFired[scriptIndex]) && !(this.singleGlove && this.detectedGloves.Count > 1))
                {
                    this.eventFired[scriptIndex] = true;
                    this.FireDetectEvent(this.detectedGloves[scriptIndex]);
                }
            }

        }
    }

    void OnTriggerExit(Collider col)
    {
        SenseGlove_Feedback touch = col.GetComponent<SenseGlove_Feedback>();
        if (touch && touch.handModel) //must have a grabscript attached.
        {
            //SenseGlove_Debugger.Log("Collider Exits");
            int scriptIndex = this.HandModelIndex(touch.handModel);
            if (scriptIndex < 0)
            {
                //SenseGlove_Debugger.Log("Something went wrong with " + this.gameObject.name);
                //it is likely the palm collider.
            }
            else
            {   //belongs to an existing SenseGlove.
                if (ValidScript(touch))
                {
                    this.detectedColliders[scriptIndex]--;
                    if (this.detectedColliders[scriptIndex] <= 0)
                    {
                        //raise release event.
                        //SenseGlove_Debugger.Log("Escape!");
                        if (eventFired[scriptIndex] && !(this.singleGlove && this.detectedGloves.Count > 1)) //only fire if the last glove has been removed.
                        {
                            this.FireRemoveEvent(this.detectedGloves[scriptIndex]);
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

    /// <summary> Returns the index of the SenseGlove_Handmodel in this detector's detectedGloves. Returns -1 if it is not in the list. </summary>
    /// <param name="grab"></param>
    /// <returns></returns>
    private int HandModelIndex(SenseGlove_HandModel model)
    {
        for (int i = 0; i < this.detectedGloves.Count; i++)
        {
            if (GameObject.ReferenceEquals(model, this.detectedGloves[i])) { return i; }
        }
        return -1;
    }

    /// <summary> Add a newly detected SenseGlove to the list of detected gloves. </summary>
    /// <param name="model"></param>
    private void AddEntry(SenseGlove_HandModel model)
    {
        this.detectedGloves.Add(model);
        this.detectionTimes.Add(0);
        this.detectedColliders.Add(1); //already add one.
        this.eventFired.Add(false);
    }

    /// <summary> Remove a handmodel at the specified index from the list of detected gloves. </summary>
    /// <param name="scriptIndex"></param>
    private void RemoveEntry(int scriptIndex)
    {
        if (scriptIndex > -1 && scriptIndex < detectedGloves.Count)
        {
            this.detectedColliders.RemoveAt(scriptIndex);
            this.detectedGloves.RemoveAt(scriptIndex);
            this.detectionTimes.RemoveAt(scriptIndex);
            this.eventFired.RemoveAt(scriptIndex);
        }
    }


    /// <summary> Returns true if there is a Sense Glove contained within this detector. </summary>
    /// <returns></returns>
    public bool ContainsSenseGlove()
    {
        return this.detectedGloves.Count > 0;
    }

    /// <summary> Get a list of all gloves within this detection area. </summary>
    /// <returns></returns>
    public SenseGlove_HandModel[] GlovesInside()
    {
        return this.detectedGloves.ToArray();
    }

    /// <summary> Check if this scriptIndex is detectable by this Detector. </summary>
    /// <param name="scriptIndex"></param>
    /// <returns></returns>
    private bool ValidScript(int scriptIndex)
    {
        return (this.detectThumb && scriptIndex == 0) 
            || (this.detectIndex && scriptIndex == 1) 
            || (this.detectMiddle && scriptIndex == 2) 
            || (this.detectRing && scriptIndex == 3) 
            || (this.detectPinky && scriptIndex == 4);
    }

    private bool ValidScript(SenseGlove_Feedback touch)
    {
        return this.detectionMethod == DetectionType.AnyFinger
                    || (this.detectionMethod == DetectionType.SpecificFingers && this.ValidScript(touch.GetIndex()));
    }

  
    #endregion Logic

    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    #region Events
    
    /// <summary> A step in between events that can be overridden by sub-classes of the SenseGlove_Detector </summary>
    /// <param name="model"></param>
    protected virtual void FireDetectEvent(SenseGlove_HandModel model)
    {
        this.OnGloveDetected(model);
    }

    /// <summary> A step in between events that can be overridden by sub-classes of the SenseGlove_Detector </summary>
    /// <param name="model"></param>
    protected virtual void FireRemoveEvent(SenseGlove_HandModel model)
    {
        this.OnGloveRemoved(model);
    }


    public delegate void GloveDetectedEventHandler(object source, GloveDetectionArgs args);
    /// <summary> Fires when a new SenseGlove_Grabscript enters this detection zone and fullfils the detector's conditions. </summary>
    public event GloveDetectedEventHandler GloveDetected;

    protected void OnGloveDetected(SenseGlove_HandModel model)
    {
        if (GloveDetected != null)
        {
            GloveDetected(this, new GloveDetectionArgs(model));
        }
    }

    public delegate void OnGloveRemovedEventHandler(object source, GloveDetectionArgs args);
    /// <summary>Fires when a SenseGlove_Grabscript exits this detection zone and fullfils the detector's conditions.  </summary>
    public event OnGloveRemovedEventHandler GloveRemoved;

    protected void OnGloveRemoved(SenseGlove_HandModel model)
    {
        if (GloveRemoved != null)
        {
            GloveRemoved(this, new GloveDetectionArgs(model));
        }
    }

    #endregion Events

}


/// <summary> EventArgs fired when a glove is detected in or removed from a SenseGlove_Detector. </summary>
public class GloveDetectionArgs : System.EventArgs
{
    /// <summary> The Grabscript that caused the event to fire. </summary>
    public SenseGlove_HandModel handModel;

    /// <summary> Create a new instance of the SenseGlove Detection Arguments </summary>
    /// <param name="grab"></param>
    public GloveDetectionArgs(SenseGlove_HandModel model)
    {
        this.handModel = model;
    }

}



namespace Util
{
    /// <summary> How a Sense Glove is detected through its Feedback scripts. </summary>
    public enum DetectionType
    {
        AnyFinger = 0,
        SpecificFingers
    }

}



#if UNITY_EDITOR //used to prevent crashing while building the solution.

#region Interface

[CustomEditor(typeof(SenseGlove_Detector))]
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
    private static readonly GUIContent l_detectType       = new GUIContent("Detection Type\t", "The method for detection: General Colliders or Specific fingers.");
    private static readonly GUIContent l_activationThresh = new GUIContent("Activation Threshold\t", " How many SenseGlove_Feedback colliders must enter the Detector before the GloveDetected event is raised.");

    private static readonly GUIContent l_detectThumb    = new GUIContent("Detects Thumb\t", "Whether or not this detector is activated by a thumb when detecting specific fingers only.");
    private static readonly GUIContent l_detectIndex    = new GUIContent("Detects Index\t", "Whether or not this detector is activated by an index finger when detecting specific fingers only.");
    private static readonly GUIContent l_detectMiddle   = new GUIContent("Detects Middle\t", "Whether or not this detector is activated by a middle finger when detecting specific fingers only.");
    private static readonly GUIContent l_detectRing     = new GUIContent("Detects Ring\t", "Whether or not this detector is activated by a ring finger when detecting specific fingers only.");
    private static readonly GUIContent l_detectPinky    = new GUIContent("Detects Pinky\t", "Whether or not this detector is activated by a pinky finger when detecting specific fingers only.");

    private static readonly GUIContent l_activationTime = new GUIContent("Activation Time\t", "Optional: The time in seconds that the Sense Glove must be inside the detector for before the GloveDetected event is called");
    private static readonly GUIContent l_singleGlove    = new GUIContent("Single Glove\t", " If set to true, the detector will not raise additional events if a second handModel joins in.");
    private static readonly GUIContent l_highlight      = new GUIContent("HighLight\t", "An optional Highlight of this Detector that can be enabled / disabled.");

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

        var detectorClass = target as SenseGlove_Detector;
        var origFontStyle = EditorStyles.label.fontStyle;

        ///// Always show the dropdown menu
        ////show as an enum dropdown with the selected option matching the one we've chosen.

        EditorGUI.BeginChangeCheck();
        SetRenderMode(_detectType.hasMultipleDifferentValues);
        detectorClass.detectionMethod = (DetectionType)EditorGUILayout.EnumPopup(l_detectType, detectorClass.detectionMethod);
        SetRenderMode(false, origFontStyle);
        if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
        {
            _detectType.enumValueIndex = (int)detectorClass.detectionMethod;
        }

        //Actually show everything.
        if (!_detectType.hasMultipleDifferentValues)
        {
            CreateIntField(ref detectorClass.activationThreshold, ref _activationThresh, l_activationThresh);

            if (detectorClass.detectionMethod == DetectionType.SpecificFingers)
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

