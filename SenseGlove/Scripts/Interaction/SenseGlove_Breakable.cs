using UnityEngine;

/// <summary> A Gameobject that despawns an objects once its material breaks, and optionally replaces it with a 'broken' version. </summary>
public class SenseGlove_Breakable : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------
    // Properties

    #region Properties

    /// <summary> The Interactable with a material which can break. Represents the 'whole' object </summary>
    [Tooltip("The Interactable with a material which can break. Represents the 'whole' object")]
    public SenseGlove_Interactable wholeObject;

    /// <summary> The interactable in its broken state. </summary>
    [Tooltip("The interactable in its broken state.")]
    public SenseGlove_Interactable brokenObject;

    /// <summary> Optional Particle System that plays when the object breaks. </summary>
    [Tooltip("Optional Particle System that plays when the object breaks.")]
    public ParticleSystem breakParticles;

    /// <summary> Optional sound to play when the material breaks. </summary>
    [Tooltip("Optional sound to play when the material breaks.")]
    public AudioSource breakSound;

    /// <summary> Determines if the Breakable resets back to the whole object after the desired timeframe. </summary>
    [Header("Reset Options")]
    [Tooltip("Determines how the Breakable resets back to the whole object after the desired timeframe.")]
    public SenseGloveUtils.UnbreakType unbreakMethod = SenseGloveUtils.UnbreakType.None;

    /// <summary> The time after which the breakable checks if it needs to reset. </summary>
    [Tooltip("The time after which the breakable checks if it needs to reset..")]
    public float checkTime = 1.0f;

    /// <summary> Timer to keep track of when this object resets. </summary>
    private float resetTime = 0;

    /// <summary> SenseGlove_Material of the whole object. Used to catch the OnMaterialBreak event. </summary>
    private SenseGlove_Material wholeMaterial;
    /// <summary> SenseGlove_Material of the broken object. </summary>
    private SenseGlove_Material brokenMaterial;

    /// <summary> (Optional) deform script of the whole object, to reset if the material breaks </summary>
    private SenseGlove_MeshDeform wholeDeform;
    /// <summary>  (Optional) deform script of the broken object, to reset if the material unbreaks </summary>
    private SenseGlove_MeshDeform brokenDeform;

    #endregion Properties
    

    //--------------------------------------------------------------------------------------------
    // Break Logic

    #region BreakLogic
    
    /// <summary> Fired when the associated material breaks. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void WholeMaterial_MaterialBreaks(object source, System.EventArgs args)
    {
        this.Break();
    }


    /// <summary> Returns true if the wholeObject is currently in its broken state. </summary>
    /// <returns></returns>
    public bool IsBroken()
    {
        return this.wholeMaterial.IsBroken();
    }


    /// <summary> Break the object: Hide the whole object, optionally show the broken one and play the particle effect(s) </summary>
    public virtual void Break()
    {
        SenseGlove_Interactable senseScript = this.wholeObject.GetComponent<SenseGlove_Interactable>();
        if (senseScript) { senseScript.EndInteraction(); }

        if (this.wholeDeform)
        {
            this.wholeDeform.ResetMesh();
        }
        this.wholeObject.gameObject.SetActive(false);

        if (this.brokenObject)
        {
            this.brokenObject.transform.position = this.wholeObject.transform.position;
            this.brokenObject.transform.rotation = this.wholeObject.transform.rotation;
            this.brokenObject.gameObject.SetActive(true);
        }

        if (this.breakParticles)
        {
            this.breakParticles.gameObject.transform.position = this.wholeObject.transform.position;
            this.breakParticles.gameObject.transform.rotation = this.wholeObject.transform.rotation;
            this.breakParticles.Play();
        }

        if (this.breakSound)
        {
            this.breakSound.Play();
        }

        this.resetTime = 0;
        
        this.OnObjectBreaks();
    }

    /// <summary> Reset the object to before its unbroken state, at the same location of the current broken object. </summary>
    public virtual void UnBreak()
    {
        //remove the broken object if we have one.
        if (this.brokenObject != null)
        {
            this.brokenObject.EndInteraction();
            this.brokenObject.gameObject.SetActive(false);

            if (this.brokenMaterial)
            {
                this.brokenMaterial.UnBreak();
            }

            if (this.brokenDeform)
            {
                this.brokenDeform.ResetMesh();
            }

            //Debug.Log("WholeObject is now on the position of the broken object");
            this.wholeObject.transform.position = this.brokenObject.transform.position;
            this.wholeObject.transform.rotation = this.brokenObject.transform.rotation;
        }

        //stop the particle effect(s)
        if (this.breakParticles)
        {
            this.breakParticles.Stop();
        }

        if (this.breakSound)
        {
            this.breakSound.Stop();
        }

        //unbreak the whole object
        if (this.wholeMaterial) { this.wholeMaterial.UnBreak(); }

        this.wholeObject.gameObject.SetActive(true);

        this.resetTime = 0;

        this.OnObjectUnBreaks();
    }

    /// <summary> Reset this objects position and materials. </summary>
    public virtual void ResetObject()
    {
        this.UnBreak(); //every parameter is reset; the whole material is now on the position of the broken one (if a broken one exists).

        this.wholeObject.ResetObject();
        if (this.brokenObject)
        {
            this.brokenObject.ResetObject();
        }
    }

    /// <summary> Check if this objects needs to be reset, depending on the state and unbreakMethod </summary>
    public virtual void CheckUnbreak()
    {
        if (this.unbreakMethod != SenseGloveUtils.UnbreakType.None && this.IsBroken())
        {
            if (this.resetTime < this.checkTime)
            {
                this.resetTime += Time.deltaTime;
            }
            else //after the ResetTime elapses, the object
            {
                if (this.unbreakMethod == SenseGloveUtils.UnbreakType.Reset)
                    this.ResetObject();
                else if (this.unbreakMethod == SenseGloveUtils.UnbreakType.Unbreak)
                    this.UnBreak();

                this.resetTime = 0;
            }
        }
    }

    #endregion BreakLogic;


    //--------------------------------------------------------------------------------------------
    // Monobehaviour 

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start()
    {
        this.wholeDeform = this.wholeObject.GetComponent<SenseGlove_MeshDeform>();
        this.wholeMaterial = this.wholeObject.GetComponent<SenseGlove_Material>();
        this.wholeMaterial.MaterialBreaks += WholeMaterial_MaterialBreaks;
        this.wholeObject.SaveTransform();

        if (this.brokenObject)
        {
            this.brokenDeform = this.brokenObject.GetComponent<SenseGlove_MeshDeform>();
            this.brokenMaterial = this.brokenObject.GetComponent<SenseGlove_Material>();
            this.brokenObject.SaveTransform();
        }

        this.UnBreak();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        this.CheckUnbreak();
    }

    #endregion Monobehaviour


    //--------------------------------------------------------------------------------------------
    // Events

    #region Events

    /// <summary> Event delegate for the ObjectBreaks EventHandler </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void ObjectBrokenEventHandler(object source, System.EventArgs args);

    /// <summary> Fires when this objects Break() function has been called. </summary>
    public event ObjectBrokenEventHandler ObjectBreaks;

    /// <summary> Calls the ObjectBreaks event handler. </summary>
    protected void OnObjectBreaks()
    {
        if (ObjectBreaks != null)
        {
            ObjectBreaks(this, null);
        }
    }


    /// <summary> Event delegate for the ObjectUnBreaks EventHandler </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void ObjectUnBrokenEventHandler(object source, System.EventArgs args);

    /// <summary> Fires when this objects UnBreak() function has been called. </summary>
    public event ObjectUnBrokenEventHandler ObjectUnBreaks;

    /// <summary> Calls the ObjectUnBreaks event handler. </summary>
    protected void OnObjectUnBreaks()
    {
        if (ObjectUnBreaks != null)
        {
            ObjectUnBreaks(this, null);
        }
    }

    #endregion Events

}


namespace SenseGloveUtils
{
    /// <summary> How the object will respond after it breaks. </summary>
    public enum UnbreakType
    {
        /// <summary> The object stays broken, and does nothing. Default value.  </summary>
        None = 0,
        /// <summary> The object unbreaks after the timer elapses. </summary>
        Unbreak,
        /// <summary> The object fully resets after the timer elapsed. </summary>
        Reset
    }
}

