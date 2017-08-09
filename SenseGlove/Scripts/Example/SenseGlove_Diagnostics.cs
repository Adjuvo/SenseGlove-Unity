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
            //this.SetText(this.trackedGlove.GloveData().packetsPerSecond.ToString());
            //Quaternion Qwrist = trackedGlove.GloveData().relativeWrist;
            //Vector3 unityEuler = Qwrist.eulerAngles;
            //float[] DLLeuler = SenseGloveCs.Quaternions.ToEuler(SenseGlove_Util.ToQuaternion(Qwrist));
            //Vector3 DLLconv = SenseGlove_Util.ToUnityEuler(DLLeuler);


            //Vector3 Xunity = Qwrist * new Vector3(1, 0, 0);
            //Vector3 Xdll = SenseGlove_Util.ToUnityPosition( SenseGloveCs.Quaternions.Rotate(new float[] { 1, 0, 0 }, SenseGlove_Util.ToQuaternion(Qwrist)) );

            //this.SetText("Converted " + Qwrist.ToString() + "\r\n" 
            //    + unityEuler.ToString() + "\t\t-\t\t" + Xunity.ToString() + "\r\n"
            //    + DLLconv.ToString() + "\t\t-\t\t" + Xdll.ToString() );

            float[][] sensors = this.trackedGlove.GloveData().gloveValues;
            string msg = "GloveAngles:\r\n";
            for (int i=0; i<sensors.Length; i++)
            {
                msg += SenseGlove_Util.ToString( SenseGloveCs.Values.Degrees( sensors[i] ) ) + "\r\n";
            }
            SetText("");


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
