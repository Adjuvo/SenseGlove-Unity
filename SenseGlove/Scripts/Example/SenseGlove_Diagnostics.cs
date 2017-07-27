using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Diagnostics : MonoBehaviour
{
    /// <summary> The SenseGlove_Object on which to perform diagnostics. </summary>
    [Tooltip("The SenseGlove_Object on which to perform diagnostics.")]
    public SenseGlove_Object trackedGlove;

    /// <summary> Optional SenseGlove_Wireframe with which to show the SenseGlove model </summary>
    private SenseGlove_WireFrame wireFrame;

    /// <summary> Debug text within the scene to output to. </summary>
    public TextMesh debugText;

	// Use this for initialization
	void Start ()
    {
		if (this.trackedGlove)
        {
            this.trackedGlove.OnGloveLoaded += TrackedGlove_OnGloveLoaded;
            this.wireFrame = this.trackedGlove.GetComponent<SenseGlove_WireFrame>();
        }
        if (!this.debugText)
        {
            this.debugText = this.gameObject.GetComponent<TextMesh>();
        }
        SetText("Waiting to connect to SenseGlove");
    }

    private void TrackedGlove_OnGloveLoaded(object source, System.EventArgs args)
    {
        SetText("");
    }

    // Update is called once per frame
    void Update ()
    {
        if (this.trackedGlove && this.trackedGlove.GloveReady())
        {
            SetText(this.trackedGlove.GloveData().packetsPerSecond.ToString());
        }
        if (this.wireFrame)
        {
            this.wireFrame.SetGlove(true);
        }
    }
    
    private void SetText(string text)
    {
        if (this.debugText)
        {
            this.debugText.text = text;
        }
    }
    
    void OnApplicationQuit()
    {
        if (trackedGlove)
        {
            this.trackedGlove.OnGloveLoaded -= TrackedGlove_OnGloveLoaded;
        }
    }
}
