using UnityEngine;
using UnityEngine.UI;
using SenseGloveCs;

namespace SG.Examples
{

    /// <summary> Allows one to access certain Sense Glove fucntions using the keys on the keyboard. </summary>
    public class SGEx_Diagnostics : MonoBehaviour
    {
        //------------------------------------------------------------------------------------------------
        // Properties

        [Header("Components")]
        public SG_SenseGloveHardware senseGlove;

        public Text instructText;

        [Header("Keybinds")]
        public bool hotKeysEnabled = true;
        public KeyCode toggleAllFFBKey = KeyCode.Return;
        public KeyCode toggleAllBuzzKey = KeyCode.B;

        public KeyCode testThumperKey = KeyCode.T;

        public KeyCode fullLoadKey = KeyCode.F;
        public KeyCode calibrateWristKey = KeyCode.P;


        private bool firstLink = false;
        private float thumperTimer = 0;
        private float thumpTime = 1.2f;
        private SenseGloveCs.ThumperEffect thumperToTest = SenseGloveCs.ThumperEffect.Impact_Thump_100;

        private string baseInst = "";
        public GameObject[] disableUntilFound = new GameObject[0];

        //------------------------------------------------------------------------------------------------
        // UI

        public string Instructions
        {
            set
            {
                if (instructText != null)
                {
                    instructText.text = value;
                }
            }
        }



        void UpdateDiagnostics()
        {
            if (senseGlove.IsConnected)
            {
                string instr = (baseInst.Length > 0 ? baseInst + "\r\n" : "");
                instr += senseGlove.GloveData.packetsPerSecond + " Packets / Second";

                Instructions = instr;
                instructText.color = Color.white;
            }
            else
            {
                Instructions = "DISCONNECTED";
                instructText.color = Color.red;
            }
        }

        public void CalibrateWrist()
        {
            SG_HandAnimator handAnimation = this.senseGlove.GetComponent<SG_HandAnimator>();
            if (handAnimation != null) { handAnimation.CalibrateWrist(); }
        }



        //------------------------------------------------------------------------------------------------
        // Force-Feedback Testing

        public bool CanTestFFB
        {
            get { return senseGlove.HasFunction(GloveFunctions.Brakes); }
        }

        public int[] FFBLvls
        {
            get; private set;
        }

        public bool AllFFBOn
        {
            get
            {
                for (int f = 0; f < FFBLvls.Length; f++)
                {
                    if (FFBLvls[f] == 0) { return false; }
                }
                return true;
            }
        }

        public void SetFFB(int finger, int level)
        {
            if (finger > -1 && finger < 5)
            {
                int[] FFBcmd = FFBLvls;
                FFBcmd[finger] = level;
                SetFFB(FFBcmd);
            }
        }

        public void SetFFB(int[] levels)
        {
            if (CanTestFFB)
            {
                int[] FFBcmd = FFBLvls;
                for (int f = 0; f < 5 && f < levels.Length; f++) //ensures it works even if levels is not large enough
                {
                    FFBcmd[f] = Mathf.Clamp(levels[f], 0, 100);
                }
                FFBLvls = FFBcmd; //updates latest

                SenseGlove iGlove = (SenseGlove)senseGlove.GetInternalObject();
                if (iGlove != null)
                {
                    iGlove.BrakeCmd(FFBcmd);
                }
                else
                {
                    Debug.Log("Could not send FFB command because we have no Glove");
                }
            }
        }

        public void ToggleFFB()
        {
            if (AllFFBOn) { SetFFB(new int[] { 0, 0, 0, 0, 0 }); } //turn off if all FFB is on
            else { SetFFB(new int[] { 100, 100, 100, 100, 100 }); } //turn all ffb on if not already done
        }



        //------------------------------------------------------------------------------------------------
        // Buzz Motor Testing

        public bool CanTestBuzzMotors
        {
            get { return senseGlove.HasFunction(GloveFunctions.BuzzMotors); }
        }


        public int[] BuzzMotorLvls
        {
            get; private set;
        }


        public bool AllBuzzOn
        {
            get
            {
                for (int f = 0; f < BuzzMotorLvls.Length; f++)
                {
                    if (BuzzMotorLvls[f] == 0) { return false; }
                }
                return true;
            }
        }

        public void SetBuzz(int finger, int level)
        {
            if (finger > -1 && finger < 5)
            {
                int[] buzzCmd = BuzzMotorLvls;
                buzzCmd[finger] = level;
                SetBuzz(buzzCmd);
            }
        }

        public void SetBuzz(int[] levels)
        {
            if (CanTestBuzzMotors)
            {
                int[] buzzCmd = BuzzMotorLvls;
                for (int f = 0; f < 5 && f < levels.Length; f++) //ensures it works even if levels is not large enough
                {
                    buzzCmd[f] = Mathf.Clamp(levels[f], 0, 100);
                }
                BuzzMotorLvls = buzzCmd; //updates latest
                SenseGlove iGlove = (SenseGlove)senseGlove.GetInternalObject();
                if (iGlove != null)
                {
                    iGlove.SendBuzzCmd(buzzCmd);
                }
                else
                {
                    Debug.Log("Could not send Buzz command because we have no Glove");
                }
            }
        }

        public void ToggleBuzz()
        {
            if (AllBuzzOn) { SetBuzz(new int[] { 0, 0, 0, 0, 0 }); } //turn off if all FFB is on
            else { SetBuzz(new int[] { 100, 100, 100, 100, 100 }); } //turn all ffb on if not already done
        }




        //------------------------------------------------------------------------------------------------
        // Thumper Testing

        public bool CanTestThumper
        {
            get { return senseGlove.HasFunction(GloveFunctions.ThumperModule); }
        }


        public bool ThumperOn
        {
            get; private set;
        }


        private void SendThumperCmd(ThumperEffect effect)
        {
            senseGlove.SendThumperCmd(effect);
        }


        public void BeginTestThumper(bool loops)
        {
            if (CanTestThumper)
            {
                SendThumperCmd(thumperToTest);
                if (loops)
                {
                    thumperTimer = 0;
                    ThumperOn = true;
                }
            }
            else
            {
                Debug.Log("This Sense Glove's firmware (v" + (senseGlove.GloveData.firmwareVersion)
                    + ") does not support a Thumper");
            }
        }

        public void EndTestThumper()
        {
            if (ThumperOn) //cant turn of if you don't have a thumper
            {
                SendThumperCmd(ThumperEffect.TurnOff);
            }
            ThumperOn = false;
        }

        void UpdateThumper()
        {
            if (ThumperOn)
            {
                if (thumperTimer < thumpTime)
                {
                    thumperTimer += Time.deltaTime;
                    if (thumperTimer >= thumpTime)
                    {
                        SendThumperCmd(thumperToTest);
                        thumperTimer = 0;
                    }
                }
            }
        }




        //------------------------------------------------------------------------------------------------
        // Full Load Testing

        public bool AllFeedbackOn
        {
            get
            {
                if (CanTestFFB && !AllFFBOn)
                {
                    return false;
                }
                if (CanTestBuzzMotors && !AllBuzzOn)
                {
                    return false;
                }
                if (CanTestThumper && !ThumperOn)
                {
                    return false;
                }

                return true;
            }
        }


        public void SetBrakeBuzz(int[] ffb, int[] buzz)
        {
            if (CanTestBuzzMotors && CanTestFFB)
            {
                int[] FFBcmd = FFBLvls;
                for (int f = 0; f < 5 && f < ffb.Length; f++) //ensures it works even if levels is not large enough
                {
                    FFBcmd[f] = Mathf.Clamp(ffb[f], 0, 100);
                }
                FFBLvls = FFBcmd; //updates latest

                int[] buzzCmd = BuzzMotorLvls;
                for (int f = 0; f < 5 && f < buzz.Length; f++) //ensures it works even if levels is not large enough
                {
                    buzzCmd[f] = Mathf.Clamp(buzz[f], 0, 100);
                }
                BuzzMotorLvls = buzzCmd; //updates latest

                SenseGlove iGlove = (SenseGlove)senseGlove.GetInternalObject();
                if (iGlove != null)
                {
                    iGlove.SendBrakeBuzz(FFBcmd, buzzCmd);
                }
                else
                {
                    Debug.Log("Could not send commands because we have no Glove");
                }
            }
        }


        public void EngageAllFeedback()
        {
            Debug.Log("Engaging all feedback");

            if (CanTestThumper)
            {
                BeginTestThumper(true);
            }
            if (CanTestBuzzMotors && CanTestFFB)
            {
                SetBrakeBuzz(new int[] { 100, 100, 100, 100, 100 }, new int[] { 100, 100, 100, 100, 100 });
            }
            else if (CanTestFFB)
            {
                SetFFB(new int[] { 100, 100, 100, 100, 100 }); //all on 
            }
            else if (CanTestBuzzMotors)
            {
                SetBuzz(new int[] { 100, 100, 100, 100, 100 }); //all on 
            }
        }

        public void EndAllFeedback()
        {
            Debug.Log("Ending all feedback");
            if (CanTestThumper)
            {
                EndTestThumper();
            }

            if (CanTestFFB && CanTestBuzzMotors)
            {
                SetBrakeBuzz(new int[5], new int[5]); //all on
            }
            else if (CanTestFFB)
            {
                SetFFB(new int[5]); //all on
            }
            else if (CanTestBuzzMotors)
            {
                SetBuzz(new int[5]); //all on
            }
        }

        public void ToggleAllFeedback()
        {
            if (AllFeedbackOn)
            {
                EndAllFeedback();
            }
            else
            {
                EngageAllFeedback();
            }
        }


        //------------------------------------------------------------------------------------------------
        // Monobehaviour

        void Awake()
        {
            FFBLvls = new int[5];
            BuzzMotorLvls = new int[5];
            ThumperOn = false;
        }

        // Use this for initialization
        void Start()
        {
            for (int i = 0; i < this.disableUntilFound.Length; i++)
            {
                this.disableUntilFound[i].SetActive(true);//allow them to perform Awake/Start
                this.disableUntilFound[i].SetActive(false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (senseGlove.GloveReady)
            {
                if (!firstLink)
                {
                    firstLink = true;
                    baseInst = "Connected to " + senseGlove.GloveData.deviceID
                        + " running firmware v" + senseGlove.GloveData.firmwareVersion;
                    Instructions = baseInst;
                    for (int i = 0; i < this.disableUntilFound.Length; i++)
                    {
                        this.disableUntilFound[i].SetActive(true);
                    }
                }

                UpdateThumper();
                UpdateDiagnostics();

                //input
                if (SG_Util.keyBindsEnabled)
                {
                    if (Input.GetKeyDown(toggleAllFFBKey))
                    {
                        ToggleFFB();
                    }
                    if (Input.GetKeyDown(toggleAllBuzzKey))
                    {
                        ToggleBuzz();
                    }
                    else if (Input.GetKeyDown(testThumperKey))
                    {
                        BeginTestThumper(false);
                    }
                    else if (Input.GetKeyDown(fullLoadKey))
                    {
                        ToggleAllFeedback();
                    }
                    else if (Input.GetKeyDown(calibrateWristKey))
                    {
                        this.CalibrateWrist();
                    }
                }
            }
            else //not yet linked
            {
                Instructions = SG_DeviceManager.ReportConnections();
            }
        }
    }
}