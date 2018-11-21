using System.Text;
using UnityEngine;

namespace SenseGlove_Examples
{
    /// <summary> Allows one to access certain Sense Glove fucntions using the keys on the keyboard. </summary>
    public class SenseGlove_Diagnostics : MonoBehaviour
    {
        //------------------------------------------------------------------------------------------------
        // Properties

        /// <summary> The SenseGlove_Object on which to perform diagnostics. </summary>
        [Header("Diagnostics Components")]
        [Tooltip("The SenseGlove_Object on which to perform diagnostics.")]
        public SenseGlove_Object trackedGlove;

        /// <summary> Debug text to show instructions. </summary>
        [Tooltip("Debug text to show instructions.")]
        public TextMesh debugText;

        /// <summary> Textmesh that shows how many packets/second are recieved. </summary>
        [Tooltip("Textmesh that shows how many packets/second are recieved.")]
        public TextMesh packetsText;

        /// <summary> Key that cycles the Force Feedback from 0 to 100% </summary>
        [Header("KeyBinds")]
        [Tooltip("Key that cycles the Force Feedback from 0 to 100%")]
        public KeyCode cycleFFBKey = KeyCode.Return;

        /// <summary> Key to cycle Force Feedback between fingers. </summary>
        [Tooltip("Key to cycle Force Feedback between fingers.")]
        public KeyCode cycleFingerFFBKey = KeyCode.Space;

        /// <summary> Key to cycle buzz motors between fingers. </summary>
        [Tooltip("Key to cycle buzz motors between fingers.")]
        public KeyCode cycleFingerBuzzKey = KeyCode.B;

        /// <summary> Whether or not we are currently cycling Force Feedback </summary>
        private bool cycleFFB = false;

        /// <summary> The step of the force-feedback cycle, running from 0 to 100. </summary>
        private int step = 0;

        /// <summary> Whether or not we are currently toggling brakes. </summary>
        private bool brakeToggle = false;

        /// <summary> Whether or not we are currently toggling buzz motors. </summary>
        private bool buzzToggle = false;

        /// <summary> The finger which we are currently cycling to. If set to -1, we are not cycling. </summary>
        private int currentFinger = -1;

        //------------------------------------------------------------------------------------------------
        // Diagnostics Methods

        /// <summary> Fires when the input glove is loaded. Update text. </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void TrackedGlove_OnGloveLoaded(object source, System.EventArgs args)
        {
            SetText("Press " + this.cycleFFBKey.ToString() + " to cycle Force Feedback");
        }

        /// <summary> Set the debug text to the desired string value. </summary>
        /// <param name="text"></param>
        private void SetText(string text)
        {
            if (this.debugText)
                this.debugText.text = text;
        }

        /// <summary> Begins the cycling of force feedback. </summary>
        public void CycleForceFeedback()
        {
            cycleFFB = true;
            step = 0;
            trackedGlove.SendBrakeCmd(step, step, step, step, step);
        }

        


        //------------------------------------------------------------------------------------------------
        // Monobehaviour

        // Use this for initialization
        void Start()
        {
            if (this.trackedGlove)
                this.trackedGlove.GloveLoaded += TrackedGlove_OnGloveLoaded;
            
            if (!this.debugText)
                this.debugText = this.gameObject.GetComponent<TextMesh>();
            
            this.SetText("Waiting to connect to SenseGlove");
        }


        // Update is called once per frame
        void Update()
        {
            if (this.trackedGlove != null)
            {
                if (this.trackedGlove.IsLinked && this.packetsText != null)
                    this.packetsText.text = "Receiving " + trackedGlove.GloveData.packetsPerSecond + " packets / second";
                

                if (!this.trackedGlove.GloveReady)
                {
                    string[] reports = SenseGloveCs.DeviceScanner.Instance.ReportConnections();
                    if (reports.Length > 0)
                    {
                        string msg = reports[0];
                        for (int i = 1; i < reports.Length; i++)
                            msg += i < reports.Length - 1 ? reports[i] + "\r\n" : reports[i];
                        this.SetText(msg);
                    }
                    else
                        this.SetText("Plug in at least one device...");
                }
                else
                {
                    if (cycleFFB)
                    {
                        step++;
                        if (step > 100)
                        {
                            this.cycleFFB = false;
                            //SetText("Force Feedback at Maximum");
                        }
                        else
                        {
                            SetText("Cycling : " + this.step + " / 100");
                            this.trackedGlove.SendBrakeCmd(step, step, step, step, step);
                        }
                    }
                    else if (Input.GetKeyDown(this.cycleFingerFFBKey)) //toggle each individually,
                    {
                        //cancel buzz cmds.
                        if (this.buzzToggle)
                        {
                            this.trackedGlove.StopBuzzMotors();
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
                            breaks[currentFinger] = 100;
                            string ml = "00000";
                            StringBuilder sb = new StringBuilder(ml);
                            sb[currentFinger] = '1';
                            ml = sb.ToString();
                            SetText("[" + ml + "]");
                        }
                        this.trackedGlove.SendBrakeCmd(breaks);
                    }
                    else if (Input.GetKeyDown(this.cycleFingerBuzzKey))
                    {
                        //cancel ffb cmds
                        if (this.brakeToggle)
                        {
                            this.trackedGlove.StopBrakes(); //send 0, 0, 0, 0, 0.
                            this.brakeToggle = false;
                            this.currentFinger = -1;
                        }

                        int mag = 0;
                        bool[] fingers = new bool[5];
                        int t = 750;

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
                            mag = 100;
                            fingers[currentFinger] = true;
                            string ml = "00000";
                            StringBuilder sb = new StringBuilder(ml);
                            sb[currentFinger] = '1';
                            ml = sb.ToString();
                            SetText("[" + ml + "]");
                        }
                        this.trackedGlove.SendBuzzCmd(fingers, mag, t);
                    }


                    if (Input.GetKeyDown(KeyCode.Return) && currentFinger < 0)
                    {
                        if (this.step > 100)
                        {
                            this.step = 0;
                            this.trackedGlove.StopBrakes();
                            SetText("Press " + this.cycleFFBKey.ToString() + " to cycle Force Feedback");
                        }
                        else if (this.step > 0)
                        {
                            this.step = 99;
                        }
                        else
                        {
                            this.CycleForceFeedback();
                        }

                    }
                }
            }

            

        }

        //Fires when the application quits.
        void OnApplicationQuit()
        {
            if (trackedGlove)
                this.trackedGlove.GloveLoaded -= TrackedGlove_OnGloveLoaded;
        }


    }

}