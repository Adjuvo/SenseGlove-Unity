using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> A Detector that, when activated, triggers a series of in-game effects. </summary>
public class SenseGlove_Trigger : SenseGlove_Detector
{

    //------------------------------------------------------------------------------------------------------
    //  Properties

    #region Properties

    /// <summary> Particle effects that are shown when the glove is detected </summary>
    public ParticleSystem particlesToPlay;
    
    /// <summary> (Optional) Audio to play if the glove is detected </summary>
    public AudioSource audioToPlay;

    /// <summary> (group of) game objects to show when the glove is detected. </summary>
    public GameObject effectToShow;


    /// <summary> (Optional) tells the glove to give haptic feedback </summary>
    public bool hapticFeedback = false;

    /// <summary> The magnitude of the haptic Feedback </summary>
    public int hapticForce = 100;

    /// <summary> The duration of a haptic feedback pulse. </summary>
    public int hapticDuration = 200; //don't show if looping.

    /// <summary> Which fingers to apply the Haptic feedback to </summary>
    public bool[] whichFingers = new bool[5] { true, false, false, false, false };

    /// <summary> If set to true, the haptic feedback is continuous while the glove is inside the trigger </summary>
    public bool loop = false;

    //------------------------------------------------------------------------------------------------------
    //   Private Properties

    /// <summary> The amount of gloves that are using this trigger. </summary>
    private int inUse = 0;

    /// <summary> If loop is set to true, send a new command every X seconds. </summary>
    private float buzz_CMD_Time = 1;

    /// <summary> Used to keep track of new buzz commands. </summary>
    private float buzzTimer = 0;

    #endregion Properties

    //------------------------------------------------------------------------------------------------------
    //   Class Methods

    #region ClassMethods

    protected override void FireDetectEvent(SenseGlove_HandModel model)
    {
        this.SetAudio(true);
        this.SetParticles(true);
        this.SetEffectObject(true);
        if (this.hapticFeedback)
        {
            this.FireHapticFeedback();
        }
        this.inUse++;
        base.FireDetectEvent(model); //do X, then fire
    }

    protected override void FireRemoveEvent(SenseGlove_HandModel model)
    {
        this.SetAudio(false);
        this.SetParticles(false);
        this.SetEffectObject(false);
        if (this.hapticFeedback)
        {
            this.FireHapticFeedback(true);
        }
        this.inUse--;
        base.FireRemoveEvent(model); //do Y, then fire
    }


    

    /// <summary> Check if the trigger is in use by one or more sense gloves. </summary>
    /// <returns></returns>
    public bool InUse()
    {
        return this.inUse > 0;
    }

    public void SetAudio(bool play)
    {
        if (this.audioToPlay != null)
        {
            if (play) { this.audioToPlay.Play(); }
            else { this.audioToPlay.Stop(); }
        }
    }

    /// <summary>
    /// Start / Stop the particleffect
    /// </summary>
    /// <param name="play"></param>
    public void SetParticles(bool play)
    {
        if (this.particlesToPlay != null)
        {
            if (play) { particlesToPlay.Play(); }
            else { particlesToPlay.Stop(); }
        }
    }

    /// <summary>
    /// Enable/disable the "effectToShow" Gameobject
    /// </summary>
    /// <param name="active"></param>
    public void SetEffectObject(bool active)
    {
        if (this.effectToShow != null)
        {
            this.effectToShow.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// Fire the haptic feedback pulse or loop.
    /// </summary>
    /// <param name="stopAll"></param>
    private void FireHapticFeedback(bool stopAll = false)
    {
        for (int i = 0; i < this.eventFired.Count; i++)
        {
            if (this.eventFired[i] && this.detectedGloves.Count > i)
            {
                if (this.detectedGloves[i].senseGlove != null)
                {
                    if (!stopAll)
                    {
                        this.detectedGloves[i].senseGlove.SendBuzzCmd(this.whichFingers, this.hapticForce, this.hapticDuration);
                    }
                    else
                    {
                        this.detectedGloves[i].senseGlove.StopBuzzMotors();
                    }
                }
            }
        }
        //Debug.Log("Send BuzzMotorCommands");
        this.buzzTimer = 0;
    }

    #endregion ClassMethods

    //------------------------------------------------------------------------------------------------------
    //   Monobehaviour

    #region Monobehaviour

    protected override void Start()
    {
        this.SetAudio(false);
        this.SetParticles(false);
        this.SetEffectObject(false);

        if (this.loop)
        {
            this.hapticDuration = (int)(this.buzz_CMD_Time * 1000);
        }
        
    }

    protected virtual void Update()
    {
        if (this.loop)
        {
            if (this.hapticFeedback) //split from loop in case we add other functions that require timing.
            {
                if (this.buzzTimer < this.buzz_CMD_Time)
                {
                    this.buzzTimer += Time.deltaTime;
                }
                else if (this.detectedGloves.Count > 0)
                {
                    this.FireHapticFeedback();
                }
            }
        }
    }

    #endregion Monobehaviour

}

//------------------------------------------------------------------------------------------------------
//   Custom Editor

#region CustomInspector

#if UNITY_EDITOR

[CustomEditor(typeof(SenseGlove_Trigger))]
[CanEditMultipleObjects]
public class SenseGloveTriggerEditor : SenseGloveDetectorEditor
{

    protected SerializedProperty _particles, _audio, _effectObj, _hapticFB, _loop;

    protected SerializedProperty _fingers, _magn, _dur;

    private static readonly GUIContent l_particles  = new GUIContent("Particles To Play\t", "Particle effects that are shown when the glove is detected.");
    private static readonly GUIContent l_audio      = new GUIContent("Audio To Play\t", "(Optional) Audio to play if the glove is detected");
    private static readonly GUIContent l_effectObj  = new GUIContent("Effect To Show\t", "(group of) game objects to show when the glove is detected.");

    private static readonly GUIContent l_hapticFB   = new GUIContent("Haptic Feedback\t", "(Optional) tells the glove to give haptic feedback ");
    private static readonly GUIContent l_fingers = new GUIContent("Which Fingers\t", "Which fingers to apply the Haptic feedback to");
    private static readonly GUIContent l_magn = new GUIContent("Magnitude [%]\t", " The magnitude of the haptic Feedback [0..100%]");
    private static readonly GUIContent l_dur = new GUIContent("Duration [ms]\t", "The duration of a haptic feedback pulse.");

    private static readonly GUIContent l_loop       = new GUIContent("Loop\t", "If set to true, the haptic feedback is continuous while the glove is inside the trigger");

    /// <summary> 
    /// Runs once when the script's inspector is opened. 
    /// Caches all variables to save processing power.
    /// </summary>
    void OnEnable()
    {
        this.CollectBaseAttributes();

        this._particles = serializedObject.FindProperty("particlesToPlay");
        this._audio     = serializedObject.FindProperty("audioToPlay");
        this._effectObj = serializedObject.FindProperty("effectToShow");

        this._hapticFB = serializedObject.FindProperty("hapticFeedback");
        this._fingers = serializedObject.FindProperty("whichFingers");
        this._magn = serializedObject.FindProperty("hapticForce");
        this._dur = serializedObject.FindProperty("hapticDuration");
        this._loop = serializedObject.FindProperty("loop");
    }


    /// <summary> Called when the inspector is (re)drawn. </summary>
    public override void OnInspectorGUI()
    {
        var triggerClass = target as SenseGlove_Trigger;

        DrawDefault();

        //show a header
        SetRenderMode(false, headerStyle);
        EditorGUILayout.LabelField("Trigger Effects");
        SetRenderMode(false, FontStyle.Normal);

        //particles
        EditorGUILayout.PropertyField(_particles, l_particles);
        //audio
        EditorGUILayout.PropertyField(_audio, l_audio);
        //effects
        EditorGUILayout.PropertyField(_effectObj, l_effectObj);

        //haptic feedback
        CreateToggle(ref triggerClass.hapticFeedback, ref this._hapticFB, l_hapticFB);
        //show options.
        if (triggerClass.hapticFeedback && !_hapticFB.hasMultipleDifferentValues)
        {
            //show haptic feedback options


            //loop
            CreateToggle(ref triggerClass.loop, ref this._loop, l_loop);


            //intensity
            CreateIntSlider(ref triggerClass.hapticForce, ref _magn, 0, 100, l_magn);

            if (!triggerClass.loop && !_loop.hasMultipleDifferentValues)
            {
                //duration (if loop is not checked)
                CreateIntSlider(ref triggerClass.hapticDuration, ref _dur, 0, 1500, l_dur);
            }

            //which fingers to apply it to
            CreateArray(ref triggerClass.whichFingers, ref _fingers, l_fingers);

        }

        serializedObject.ApplyModifiedProperties();

    }


    protected void CreateArray(ref bool[] value, ref SerializedProperty valueProp, GUIContent label)
    {
        bool restoreWideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = true;

        //EditorGUIUtility.LookLikeInspector();
        //EditorGUIUtility.labelWidth = 0;
        //EditorGUIUtility.fieldWidth = 0;
        this.SetRenderMode(valueProp.hasMultipleDifferentValues);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(valueProp, label, true);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        this.SetRenderMode(false);
        //EditorGUIUtility.LookLikeControls();
        //EditorGUIUtility.labelWidth = 25;
        //EditorGUIUtility.fieldWidth = 50;

        EditorGUIUtility.wideMode = restoreWideMode;
    }

    protected void CreateIntSlider(ref int value, ref SerializedProperty valueProp, int min, int max, GUIContent label)
    {
        this.SetRenderMode(valueProp.hasMultipleDifferentValues);
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.IntSlider(label, value, min, max);
        if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
        {
            valueProp.intValue = value;
        }
        this.SetRenderMode(false);
    }

}

#endif

#endregion CustomInspector