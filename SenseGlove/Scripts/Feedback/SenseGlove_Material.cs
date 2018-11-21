using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SenseGloveMats;

#if UNITY_EDITOR //prevents crashing when building.
using UnityEditor;
#endif

/// <summary> Determines how the material properties are loaded. </summary>
public enum VirtualMaterial
{
    /// <summary> Material Properties can be assigned via the inspector. </summary>
    Custom = 0,

    /// <summary> Material properties are loaded from a .txt file. </summary>
    FromDataBase,

    /// <summary> Assigns properties of the hardest material. </summary>
    Steel,
    /// <summary> Assigns properties of a medium-soft material.  </summary>
    Rubber,
    /// <summary> Assigns properties of a soft material that is breakable.  </summary>
    Egg
}


/// <summary> A class that contains material properties for a virtual objects, which can be customized, hard-coded or loaded during runtime. </summary>
[HelpURL("https://github.com/Adjuvo/SenseGlove-Unity/wiki/SenseGlove_Material")]
[DisallowMultipleComponent]
public class SenseGlove_Material : MonoBehaviour
{
    //----------------------------------------------------------------------------------
    // Properties

    #region Properties

    //---------------------------------------------------------------------
    // Material options, enabled by default.

    /// <summary> The material-type of the SenseGlove_Material.  </summary>
    public VirtualMaterial material = VirtualMaterial.Custom;

    //---------------------------------------------------------------------
    //  Database options, used when loading from custom libraries

    /// <summary> 
    /// Path to the material database, relative to the project- or _Data folder. 
    /// Leave it empty to use the default library file: "SenseGlove/Data/MaterialsLibrary.txt"
    /// </summary>
    public string materialDataBase = "";

    private static readonly string baseFileName = "SenseGlove/Data/MaterialsLibrary.txt";

    /// <summary> The name of the material to retrieve from the Material Database (case-insensitive). </summary>
    public string materialName = "";

    //---------------------------------------------------------------------
    //  The actual material properties, hidden to all but custom materials

    // Force Feedback

    /// <summary> The maximum brake force [0..100%] that the material provides at maxForceDist. </summary>
    public int maxForce = 100;

    /// <summary> The distance [in m] before the maximum force is reached. </summary>
    public float maxForceDist = 0.00f;

    /// <summary> The distance [in m] before the material calls an OnBreak event. </summary>
    public float yieldDistance = 0.03f;


    // Haptic Feedback

    /// <summary> Whether or not the material should give any haptic feedback through the buzzMotors. </summary>
    public bool hapticFeedback = false;

    /// <summary> The magnitude of the haptic pulse [0..100%] </summary>
    public int hapticMagnitude = 100;

    /// <summary> (maximum) duration in ms of the haptic pulse </summary>
    public int hapticDuration = 100;


    //---------------------------------------------------------------------
    //  Breakable properties

    /// <summary> Indicates that this material can raise an OnBreak event. </summary>
    public bool breakable = false;

    /// <summary> this object must first be picked up before it can be broken. </summary>
    public bool mustBeGrabbed = false;

    /// <summary> This object must be crushed by the thumb before it can be broken </summary>
    public bool requiresThumb = false;

    /// <summary> The minimum amount of fingers (not thumb) that 'break' this object before it actually breaks. </summary>
    public int minimumFingers = 1;

    /// <summary> Check whether or not this object is broken. </summary>
    private bool isBroken = false;


    /// <summary> My (optional) interactable script </summary>
    private SenseGlove_Interactable myInteractable;

    /// <summary> (Optional) Connected Material Deformation Script, used to pass deformation paraeters? </summary>
    protected SenseGlove_MeshDeform deformScript;

    /// <summary> [thumb/palm, index, middle, pinky, ring] </summary>
    private bool[] raisedBreak = new bool[5];

    /// <summary> How many fingers [not thumb] have raised break events. </summary>
    private int brokenBy = 0;

    #endregion Properties

    //----------------------------------------------------------------------------------
    // Material Methods

    #region MaterialMethods

    /// <summary> Check if this material is broken </summary>
    /// <returns></returns>
    public bool IsBroken()
    {
        return this.isBroken;
    }

    /// <summary> Unbreak the material, allowing it to give feedback and raise the break event again. </summary>
    public void UnBreak()
    {
        this.isBroken = false;
        this.brokenBy = 0;
        this.raisedBreak = new bool[5];

        if (this.deformScript != null)
            this.deformScript.ResetMesh();
    }
    

    /// <summary> Calculates the force on the finger based on material properties. </summary>
    /// <param name="displacement"></param>
    /// <param name="fingerIndex"></param>
    /// <returns></returns>
    public int CalculateForce(float displacement, int fingerIndex)
    {
        if (this.breakable)
        {
            if (!this.isBroken)
            {
                //  SenseGlove_Debugger.Log("Disp:\t" + displacement + ",\t i:\t"+fingerIndex);
                if (!this.mustBeGrabbed || (this.mustBeGrabbed && this.myInteractable.IsInteracting()))
                {
                    // SenseGlove_Debugger.Log("mustBeGrabbed = " + this.mustBeGrabbed + ", isInteracting: " + this.myInteractable.IsInteracting());

                    if (fingerIndex >= 0 && fingerIndex < 5)
                    {
                        bool shouldBreak = displacement >= this.yieldDistance;
                        if (shouldBreak && !this.raisedBreak[fingerIndex])
                        { this.brokenBy++; }
                        else if (!shouldBreak && this.raisedBreak[fingerIndex])
                        { this.brokenBy--; }
                        this.raisedBreak[fingerIndex] = shouldBreak;

                        // SenseGlove_Debugger.Log(displacement + " --> raisedBreak[" + fingerIndex + "] = " + this.raisedBreak[fingerIndex]+" --> "+this.brokenBy);
                        if (this.brokenBy >= this.minimumFingers && (!this.requiresThumb || (this.requiresThumb && this.raisedBreak[0])))
                        {
                            this.OnMaterialBreak();
                        }
                    }
                }
            }
            else
            {
                return 0;
            }
        }
        return (int)SenseGloveCs.Values.Wrap(SenseGlove_Material.CalculateResponseForce(displacement, this.maxForce, this.maxForceDist), 0, this.maxForce);
    }

    /// <summary> Calculate the haptic pulse based on material properties. </summary>
    /// <returns></returns>
    public int CalculateHaptics()
    {
        if (this.hapticFeedback)
        {
            return this.hapticMagnitude;
        }
        return 0;
    }


    /// <summary>
    /// The actual method to calculate things, used by both default and custom materials.
    /// </summary>
    /// <returns></returns>
    public static int CalculateResponseForce(float disp, int maxForce, float maxForceDist)
    {
        if (maxForceDist > 0)
        {
            return (int)SenseGloveCs.Values.Wrap(disp * (maxForce / maxForceDist), 0, 100);
        }
        else if (disp > 0)
        {
            return maxForce;
        }
        return 0;
    }

    #endregion MaterialMethods

    //----------------------------------------------------------------------------------
    // Material Property loading

    #region MaterialProps

    /// <summary> Load material properties from an external database file. </summary>
    private void LoadMaterialProps()
    {
        if (this.materialDataBase != null && this.materialName.Length > 0)
        {
            //SenseGlove_Debugger.Log("Loading " + this.materialName + " from a local database file " + this.materialDataBase.name);
            MaterialProps props = SenseGloveMats.MaterialLibraries.GetMaterial(this.materialName);
            this.LoadMaterialProps(props);
        }
    }

    /// <summary> Load the hard-coded properties of the material </summary>
    /// <param name="ofMaterial"></param>
    public void LoadMaterialProps(VirtualMaterial ofMaterial)
    {
        //SenseGlove_Debugger.Log("Loading material props of " + ofMaterial.ToString());
        if (ofMaterial != VirtualMaterial.Custom && ofMaterial != VirtualMaterial.FromDataBase)
        {

            MaterialProps thisProps = new MaterialProps();

            switch (ofMaterial)
            {
                case VirtualMaterial.Rubber:
                    thisProps.maxForce = 65;
                    thisProps.maxForceDist = 0.02f;
                    thisProps.yieldDist = float.MaxValue;
                    thisProps.hapticForce = 60;
                    thisProps.hapticDur = 200;
                    break;

                case VirtualMaterial.Steel:
                    thisProps.maxForce = 100;
                    thisProps.maxForceDist = 0.00f;
                    thisProps.yieldDist = float.MaxValue;
                    thisProps.hapticForce = 0;
                    thisProps.hapticDur = 0;
                    break;
                case VirtualMaterial.Egg:
                    thisProps.maxForce = 90;
                    thisProps.maxForceDist = 0.01f;
                    thisProps.yieldDist = 0.02f;
                    thisProps.hapticForce = 0;
                    thisProps.hapticDur = 0;
                    break;
            }
            this.LoadMaterialProps(thisProps);
        }
    }

    /// <summary> Actually apply materialProps to this Material.  </summary>
    /// <param name="props"></param>
    private void LoadMaterialProps(MaterialProps props)
    {
        this.maxForce = props.maxForce;
        this.maxForceDist = props.maxForceDist;
        this.yieldDistance = props.yieldDist;
        this.hapticMagnitude = props.hapticForce;
        this.hapticDuration = props.hapticDur;
    }

    #endregion MaterialProps

    //------------------------------------------------------------------------------------
    // Events

    public delegate void MaterialBreaksEventHandler(object source, System.EventArgs args);
    /// <summary> Fires when the material breaks under the conditions set through the Material Properties. </summary>
    public event MaterialBreaksEventHandler MaterialBreaks;

    protected void OnMaterialBreak()
    {
        if (MaterialBreaks != null)
        {
            MaterialBreaks(this, null);
        }
        this.isBroken = true;
        this.brokenBy = 0;
        this.raisedBreak = new bool[this.raisedBreak.Length];
    }


    //----------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    protected virtual void Start()
    {
        //load material props, if required.
        if (this.materialDataBase != null)
        {
            if (this.materialDataBase.Length == 0)
            {
                this.materialDataBase = SenseGlove_Material.baseFileName;
            }
            SenseGloveMats.MaterialLibraries.LoadLibrary(Application.dataPath + "/", this.materialDataBase);
        }
        if (this.materialName.Length > 0)
        {
            this.materialName = this.materialName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)[0]; //remove spaces
        }

        if (this.material == VirtualMaterial.FromDataBase)
        {
            this.LoadMaterialProps();
        }
        else if (this.material != VirtualMaterial.Custom)
        {
            this.LoadMaterialProps(this.material); //don't load yield distances if the material has been edited?
        }

        //load grab options
        this.myInteractable = this.gameObject.GetComponent<SenseGlove_Interactable>();
        if (myInteractable == null && this.mustBeGrabbed)
        {
            this.mustBeGrabbed = false; //we cannot require this material to be grabbed if it's not an interactable.
        }
    }

    /// <summary> Unbreak this material if it is disabled. </summary>
    protected virtual void OnDisable()
    {
        this.UnBreak();
    }

    #endregion Monobehaviour


}

#if UNITY_EDITOR

#region CustomEditor

[CustomEditor(typeof(SenseGlove_Material))]
[CanEditMultipleObjects]
public class SenseGloveMaterialEditor : Editor
{
    /// <summary> Properties to check for changes and for multi-object editing. </summary>
    SerializedProperty _materialType, _maxForce,_maxForceDist, _yieldDist, _hapticFeedback, _hapticMagnitude, _hapticDuration,
        _materialDataBase, _materialName, _breakAble, _mustGrab, _mustThumb, _nuFingers;

    /// <summary> The previous material that was applies to this material via the inspector. Used to update hard coded properties on-the-go. </summary>
    private VirtualMaterial previousMaterial = VirtualMaterial.Custom;
    /// <summary> Style for the Material, Breakable and Haptic Feedback properties. </summary>
    private FontStyle headerStyle = FontStyle.Bold;

    //Tooltips are kept as static readonly so they are only instantialized once for the entire session, for each script.
    private static readonly GUIContent l_material = new GUIContent("Material\t", "The Type of Material, determines which options are shown.");

    private static readonly GUIContent l_maxForce = new GUIContent("Max Force [%]\t", "The maximum brake force[0..100 %] that the material provides at maxForceDist.");
    private static readonly GUIContent l_maxForceDist = new GUIContent(new GUIContent("Max Force Distance [m]\t", "The maximum brake force[0..100 %] that the material provides at maxForceDist."));
    private static readonly GUIContent l_yieldDist = new GUIContent(new GUIContent("Yield Distance [m]\t", "The distance [in m] before the material calls an OnBreak event."));

    private static readonly GUIContent l_materialDB = new GUIContent(new GUIContent("Material Database", "The TextAsset containing materialData."));
    private static readonly GUIContent l_materialName = new GUIContent(new GUIContent("Material Name", "The name of the material to retrieve from the Material Database."));

    private static readonly GUIContent l_hapticFeedback = new GUIContent(new GUIContent("Haptic Feedback\t", "Whether or not the material should give any haptic feedback through the buzzMotors."));
    private static readonly GUIContent l_hapticMagn = new GUIContent(new GUIContent("Magnitude [%]\t", "The magnitude of the haptic pulse"));
    private static readonly GUIContent l_hapticDur = new GUIContent(new GUIContent("Duration [ms]\t", "The (maximum) duration in ms of the haptic pulse "));

    private static readonly GUIContent l_breakable = new GUIContent(new GUIContent("Breakable\t", "Indicates that this material can raise an OnBreak event."));
    private static readonly GUIContent l_mustGrab = new GUIContent(new GUIContent("Must Be Grabbed\t", "This object must first be picked up before it can be broken."));
    private static readonly GUIContent l_reqThumb = new GUIContent(new GUIContent("Requires Thumb\t", "This object must be crushed by the thumb before it can be broken"));
    private static readonly GUIContent l_minFingers = new GUIContent(new GUIContent("Minimum Fingers\t", "The minimum amount of fingers (not thumb) that 'break' this object before it actually breaks."));


    /// <summary> 
    /// Runs once when the script's inspector is opened. 
    /// Caches all variables to save processing power.
    /// </summary>
    void OnEnable()
    {
        this._materialType = serializedObject.FindProperty("material");

        this._maxForce = serializedObject.FindProperty("maxForce");
        this._maxForceDist = serializedObject.FindProperty("maxForceDist");
        this._yieldDist = serializedObject.FindProperty("yieldDistance");

        this._materialDataBase = serializedObject.FindProperty("materialDataBase");
        this._materialName = serializedObject.FindProperty("materialName");

        this._hapticFeedback = serializedObject.FindProperty("hapticFeedback");
        this._hapticMagnitude = serializedObject.FindProperty("hapticMagnitude");
        this._hapticDuration = serializedObject.FindProperty("hapticDuration");

        this._breakAble = serializedObject.FindProperty("breakable");
        this._mustGrab = serializedObject.FindProperty("mustBeGrabbed");
        this._mustThumb = serializedObject.FindProperty("requiresThumb");
        this._nuFingers = serializedObject.FindProperty("minimumFingers");
    }

    /// <summary> Called when the inspector is (re)drawn. </summary>
    public override void OnInspectorGUI()
    {
        // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
        serializedObject.Update();

        var materialClass = target as SenseGlove_Material;
        var origFontStyle = EditorStyles.label.fontStyle;

        ///// Always show the dropdown menu
        ////show as an enum dropdown with the selected option matching the one we've chosen.

        EditorGUI.BeginChangeCheck();        
        SetRenderMode(_materialType.hasMultipleDifferentValues, headerStyle);
        materialClass.material = (VirtualMaterial)EditorGUILayout.EnumPopup(l_material, materialClass.material);
        SetRenderMode(false, origFontStyle);
        if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
        {
            _materialType.enumValueIndex = (int)materialClass.material;
            if (materialClass.material != this.previousMaterial)
            {
                //SenseGlove_Debugger.Log("Material Changed (from Editor)");
                if (materialClass.material != VirtualMaterial.Custom && materialClass.material != VirtualMaterial.FromDataBase)
                {
                    //SenseGlove_Debugger.Log("We've updated! materialClass.material = " + materialClass.material.ToString());

                    materialClass.LoadMaterialProps(materialClass.material);
                    //apply all relevant material settings.
                    _maxForce.intValue = materialClass.maxForce;
                    _maxForceDist.floatValue = materialClass.maxForceDist;
                    _yieldDist.floatValue = materialClass.yieldDistance;

                    _hapticFeedback.boolValue = materialClass.hapticFeedback;
                    _hapticDuration.intValue = materialClass.hapticDuration;
                    _hapticMagnitude.intValue = materialClass.hapticMagnitude;

                    _breakAble.boolValue = materialClass.breakable;
                }
            }
            this.previousMaterial = materialClass.material;
        }

        //end of materialType; show everything else.

        if (!_materialType.hasMultipleDifferentValues)
        {
            ///only show material properties if custom is selected.
            if (materialClass.material == VirtualMaterial.Custom)
            {
                CreateIntSlider(ref materialClass.maxForce, ref this._maxForce, 0, 100, l_maxForce);
                CreateFloatField(ref materialClass.maxForceDist, ref this._maxForceDist, l_maxForceDist);
                CreateFloatField(ref materialClass.yieldDistance, ref this._yieldDist, l_yieldDist);
                
            }
            ///Show only the Database propery
            else if (materialClass.material == VirtualMaterial.FromDataBase)
            {
                EditorGUILayout.PropertyField(_materialDataBase, l_materialDB);
                CreateTextField(ref materialClass.materialName, ref _materialName, l_materialName);
            }
        }

        //Haptic Feeback Options
        CreateToggle(ref materialClass.hapticFeedback, ref _hapticFeedback, l_hapticFeedback, headerStyle, origFontStyle);
        if (materialClass.hapticFeedback && !_hapticFeedback.hasMultipleDifferentValues)
        {
            CreateIntSlider(ref materialClass.hapticMagnitude, ref _hapticMagnitude, 0, 100, l_hapticMagn);
            CreateIntSlider(ref materialClass.hapticDuration, ref _hapticDuration, 0, 1500, l_hapticDur);
        }

        //Breakable Options
        CreateToggle(ref materialClass.breakable, ref _breakAble, l_breakable, headerStyle, origFontStyle);
        if (materialClass.breakable && !_breakAble.hasMultipleDifferentValues)
        {
            CreateToggle(ref materialClass.mustBeGrabbed, ref _mustGrab, l_mustGrab);
            CreateToggle(ref materialClass.requiresThumb, ref _mustThumb, l_reqThumb);
            CreateIntSlider(ref materialClass.minimumFingers, ref _nuFingers, 1, 4, l_minFingers);
        }

        EditorStyles.label.fontStyle = origFontStyle; //return it to the desired value.
        
        // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary> Set the inspector to draw mutiple values </summary>
    /// <param name="multipleValues"></param>
    private void SetRenderMode(bool multipleValues)
    {
        EditorGUI.showMixedValue = multipleValues;
    }

    /// <summary> Set the inspector to draw a mixed value in a specific style </summary>
    /// <param name="multipleValues"></param>
    /// <param name="style"></param>
    private void SetRenderMode(bool multipleValues, FontStyle style)
    {
        EditorGUI.showMixedValue = multipleValues;
        EditorStyles.label.fontStyle = style;
    }

    /// <summary> Create an input field for a floating point number </summary>
    /// <param name="value"></param>
    /// <param name="valueProp"></param>
    /// <param name="label"></param>
    private void CreateFloatField(ref float value, ref SerializedProperty valueProp, GUIContent label)
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

    /// <summary> Create a boolean toggle box. </summary>
    /// <param name="value"></param>
    /// <param name="valueProp"></param>
    /// <param name="label"></param>
    private void CreateToggle(ref bool value, ref SerializedProperty valueProp, GUIContent label)
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

    /// <summary> Create a boolean toggle box with a specific header style. </summary>
    /// <param name="value"></param>
    /// <param name="valueProp"></param>
    /// <param name="label"></param>
    /// <param name="style"></param>
    /// <param name="originalStyle"></param>
    private void CreateToggle(ref bool value, ref SerializedProperty valueProp, GUIContent label, FontStyle style, FontStyle originalStyle)
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

    /// <summary> Create a slider for and integer value, between two values. </summary>
    /// <param name="value"></param>
    /// <param name="valueProp"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="label"></param>
    private void CreateIntSlider(ref int value, ref SerializedProperty valueProp, int min, int max, GUIContent label)
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

    /// <summary> Create an input text field. </summary>
    /// <param name="value"></param>
    /// <param name="valueProp"></param>
    /// <param name="label"></param>
    private void CreateTextField(ref string value, ref SerializedProperty valueProp, GUIContent label)
    {
        this.SetRenderMode(valueProp.hasMultipleDifferentValues);
        EditorGUI.BeginChangeCheck();
        value = EditorGUILayout.TextField(label, value);
        if (EditorGUI.EndChangeCheck()) //update serialzed properties before showing them.
        {
            valueProp.stringValue = value;
        }
        this.SetRenderMode(false);
    }

}

#endregion CustomEditor

#endif