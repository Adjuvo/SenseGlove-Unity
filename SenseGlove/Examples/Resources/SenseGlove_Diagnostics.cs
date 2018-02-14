using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    private bool cycleFFB = false;
    private int step = 0;

    public KeyCode cycleFFBKey = KeyCode.Return;

    public KeyCode cycleFingerFFBKey = KeyCode.Space;

    public KeyCode cycleFingerBuzzKey = KeyCode.B;

    private int currentFinger = -1;

    public bool brakeToggle = false;

    public bool buzzToggle = false;

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
        SetText("Press " + this.cycleFFBKey.ToString() + " to cycle Force Feedback");
    }

    // Update is called once per frame
    void Update ()
    {
        if (this.trackedGlove && this.trackedGlove.GloveReady())
        {
            if (cycleFFB)
            {
                step++;
                if (step > 255)
                {
                    this.cycleFFB = false;
                    //SetText("Force Feedback at Maximum");
                }
                else
                {
                    SetText("Cycling : " + this.step + " / 255");
                    this.trackedGlove.SimpleBrakeCmd(step, step, step, step, step);
                }
            }
            else if (Input.GetKeyDown(this.cycleFingerFFBKey)) //toggle each individually,
            {
                //cancel buzz cmds.
                if (this.buzzToggle)
                {
                    this.trackedGlove.SimpleBuzzCmd(new bool[5] { true, true, true, true, true }, 0);
                    this.buzzToggle = false;
                    this.currentFinger = -1;
                }


                int[] breaks = new int[5];
                this.currentFinger++;
               
                if (this.currentFinger > 4)
                {
                    this.currentFinger = -1;
                    this.brakeToggle = false;
                    SetText("Press " + this.cycleFFBKey.ToString() + " to cycle Force Feedback.");
                }
                else
                {
                    this.brakeToggle = true;
                    breaks[currentFinger] = 255;
                    string ml = "00000";
                    StringBuilder sb = new StringBuilder(ml);
                    sb[currentFinger] = '1';
                    ml = sb.ToString();
                    SetText("[" + ml + "]");
                }
                this.trackedGlove.SimpleBrakeCmd(breaks);
            }
            else if (Input.GetKeyDown(this.cycleFingerBuzzKey))
            {
                //cancel ffb cmds
                if (this.brakeToggle)
                {
                    this.trackedGlove.SimpleBrakeCmd(new int[0]); //send 0, 0, 0, 0, 0.
                    this.brakeToggle = false;
                    this.currentFinger = -1;
                }

                float mag = 0;
                bool[] fingers = new bool[5];
                float t = 0.75f;

                this.currentFinger++;

                if (this.currentFinger > 4)
                {
                    fingers = new bool[5] { true, true, true, true, true }; //send to all fingers.
                    t = 0;
                    this.currentFinger = -1;
                    this.buzzToggle = false;
                    SetText("Press " + this.cycleFFBKey.ToString() + " to cycle Force Feedback.");
                }
                else
                {
                    this.buzzToggle = true;
                    mag = 1.0f;
                    fingers[currentFinger] = true;
                    string ml = "00000";
                    StringBuilder sb = new StringBuilder(ml);
                    sb[currentFinger] = '1';
                    ml = sb.ToString();
                    SetText("[" + ml + "]");
                }
                this.trackedGlove.SimpleBuzzCmd(fingers, mag, SenseGloveCs.BuzzMotorPattern.Constant, t);
            }


            if (Input.GetKeyDown(KeyCode.Return) && currentFinger < 0)
            {
                if (this.step > 255)
                {
                    this.step = 0;
                    this.trackedGlove.SimpleBrakeCmd(0, 0, 0, 0, 0);
                    SetText("Press " + this.cycleFFBKey.ToString() + " to cycle Force Feedback");
                }
                else if (this.step > 0)
                {
                    this.step = 254;
                }
                else
                {
                    this.CycleForceFeedback();
                }
               
            }

            

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

    public void CycleForceFeedback(float cycleDuration = 0.5f)
    {
        cycleFFB = true;
        step = 0;
        trackedGlove.SimpleBrakeCmd(step, step, step, step, step);
    }

}
