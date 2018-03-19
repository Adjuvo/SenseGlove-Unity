using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Determines if the Breakable resets back to the whole object after the desired timeframe.")]
    public bool resets = false;

    /// <summary> The time after which the breakable resets, if resets is set to true. </summary>
    [Tooltip("The time after which the breakable resets, if resets is set to true.")]
    public float resetAfter = 1.0f;

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
    // Monobehaviour 

    #region Monobehaviour

    // Use this for initialization
    void Start()
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
    void Update()
    {
        if (this.resets && this.IsBroken())
        {
            if (this.resetTime < this.resetAfter)
            {
                this.resetTime += Time.deltaTime;
            }
            else
            {
                this.UnBreak();
            }
        }
    }

    #endregion Monobehaviour


    //--------------------------------------------------------------------------------------------
    // Break Logic

    #region BreakLogic

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


    /// <summary> Break the object: Hide the whole object, show the broken one and play the particle effect(s) </summary>
    public virtual void Break()
    {
        SenseGlove_Interactable senseScript = this.wholeObject.GetComponent<SenseGlove_Interactable>();
        if (senseScript) { senseScript.EndInteraction(senseScript.GrabScript()); }

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

        if (this.resets)
        {
            this.resetTime = 0;
        }
    }

    /// <summary> Reset the object to before its broken state. </summary>
    public virtual void UnBreak()
    {
        //remove the broken object if we have one.
        if (this.brokenObject != null)
        {
            SenseGlove_Interactable senseScript = this.brokenObject.GetComponent<SenseGlove_Interactable>();
            if (senseScript) { senseScript.EndInteraction(senseScript.GrabScript()); }
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
    }

    /// <summary> Reset this objects position and materials. </summary>
    public virtual void ResetObject()
    {
        SenseGlove_Interactable senseScript = this.wholeObject.GetComponent<SenseGlove_Interactable>(); //TODO: Move this to init.
        if (senseScript != null)
        {
            senseScript.EndInteraction(senseScript.GrabScript());
            senseScript.ResetObject();
        }

        if (this.brokenObject != null)
        {
            senseScript = this.brokenObject.GetComponent<SenseGlove_Interactable>();
            if (senseScript != null)
            {
                senseScript.EndInteraction(senseScript.GrabScript());
                senseScript.ResetObject();
            }
        }
        this.UnBreak();
    }

    #endregion BreakLogic;

}
