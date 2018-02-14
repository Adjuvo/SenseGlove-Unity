using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Breakable : MonoBehaviour
{
    /// <summary> The breakable GameObject. </summary>
    public SenseGlove_Interactable wholeObject;

    /// <summary> The GameObject in its broken state. </summary>
    public SenseGlove_Interactable brokenObject;

    /// <summary> Optional Particle System that plays when the object breaks. </summary>
    public ParticleSystem breakParticles;

    /// <summary> Optional sound to play when the material breaks. </summary>
    public AudioSource breakSound;

    public bool resets = false;

    public float resetAfter = 1.0f;

    private float resetTime = 0;

    private SenseGlove_Material wholeMaterial;
    private SenseGlove_Material brokenMaterial;

    /// <summary> Returns true if the wholeObject is currently in its broken state. </summary>
    /// <returns></returns>
    public bool IsBroken()
    {
        return this.wholeMaterial.isBroken;
    }

    // Use this for initialization
    void Start()
    {
        this.wholeMaterial = this.wholeObject.GetComponent<SenseGlove_Material>();
        this.wholeMaterial.MaterialBreaks += WholeMaterial_MaterialBreaks;
        this.wholeObject.SaveTransform();

        if (this.brokenObject)
        {
            this.brokenMaterial = this.brokenObject.GetComponent<SenseGlove_Material>();
            this.brokenObject.SaveTransform();
        }
        
        this.UnBreak();
    }

    private void WholeMaterial_MaterialBreaks(object source, System.EventArgs args)
    {
        this.Break();
    }

    // Update is called once per frame
    void Update()
    {
        if (this.resets)
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

    /// <summary> Break the object: Hide the whole object, show the broken one and play the particle effect(s) </summary>
    public virtual void Break()
    {
        SenseGlove_Interactable senseScript = this.wholeObject.GetComponent<SenseGlove_Interactable>();
        if (senseScript) { senseScript.EndInteraction(senseScript.GrabScript()); }

        if (this.wholeMaterial)
        {
            this.wholeMaterial.ResetMesh();
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
                this.brokenMaterial.isBroken = false;
                this.brokenMaterial.ResetMesh();
            }

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
        if (this.wholeMaterial) { this.wholeMaterial.isBroken = false; }
        this.wholeObject.gameObject.SetActive(true);
    }

    /// <summary> Reset this objects position and materials. </summary>
    public virtual void ResetObject()
    {
        SenseGlove_Interactable senseScript = this.wholeObject.GetComponent<SenseGlove_Interactable>();
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

}
