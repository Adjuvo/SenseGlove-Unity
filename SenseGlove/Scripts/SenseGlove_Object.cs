/*
 * 
 * 
 * 
 * 
 */

using UnityEngine;

using SenseGloveCs;

/// <summary>
/// A SenseGlove object with Unity Wrappers and other fun stuff! Used by applications that depend on the SenseGlove.
/// This Script is responsible for ensuring a stable connection between Unity and the SenseGlove using the SenseGloveCs DLL.
/// </summary>
public class SenseGlove_Object : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------------------
    // Publicly visible attributes

    /// <summary> Determines if the DeviceScanner of Hard-Coded connection is made. </summary>
    [Header("Communication Settings")]
    [Tooltip("Set this value to true if you want the SenseGlove to look for the first L/R hand it finds. If turned off, it will automatically connect to the specified port")]
    public bool automaticMode = true;
    
    /// <summary> Indicates if this is or will be a Left or Right hand. </summary>
    [Tooltip("Indicates that this glove is a L or R hand (if automaticMode is false) or if it should automatically connect to the first left or right hand (if automaticMode is true) ")]
    public bool rightHand = true;

    /// <summary> The address of the SenseGlove's Communicator. </summary>
    [Tooltip("The (virtual) COM Port that this SenseGlove will connect to (if automaticMode is false), or the port the glove is currently conencted to (if automaticMode is true).")]
    public string COMPort = "COM3";

    /// <summary> Determines up to which level the Kinematics of the glove are Updated. </summary>
    [Header("Hand Model Settings")]
    [Tooltip("Determines up to which level the Kinematic Model is updated. Setting it to a lower level increases performance.")]
    public Kinematics updateTo = Kinematics.HandPositions;

    /// <summary> Determine wether or not the wrist is updated by the DLL. </summary>
    [Tooltip("Determines whether or not the wrist should be updated.")]
    public bool updateWrist = true;

    /// <summary> Represents the foreArm. USed for calibration & relative movement. </summary>
    [Tooltip("(Optional) The GameObject representing a foreArm. The wrist can be calibrated to align with its X-axis.")]
    public GameObject foreArm;

    /// <summary> Check for gestures within the DLL </summary>
    [Tooltip("NOT YET IMPLEMENTED - Gesture recognition")]
    private bool checkGestures = false;

    //--------------------------------------------------------------------------------------------------------------------------
    // Private attributes

    /// <summary> The SenseGlove object through which we retrieve data and send commands. </summary>
    private SenseGlove glove;
    /// <summary> A 'Briefcase' representing all of the Data that can be obtained from this SenseGlove. </summary>
    private GloveData gloveData;

    /// <summary> Used in the Update function to run a method in the DLL which parses the glove data for initialization. </summary>
    private bool finishedSetup = false;

    /// <summary> Wether or not this SenseGlove is allowed to update itself. Used to save calculation power when the glove is disconnected. </summary>
    private bool canUpdate = false;

    /// <summary>  Time that has elapsed since the Setup was called. Can be used to delay the Update Finction by setting it back to 0. </summary>
    private float elapsedTime = 0;

    /// <summary> The time it takes for one out of two steps of the Setup to complete. </summary>
    private static float setupTime = 0.5f;

    /// <summary> Only true during the frame where the setup finishes. </summary>
    private bool gloveReady = false;

    /// <summary> Determines if the manual wrist correction has been assigned. </summary>
    private bool wristCalibrated = false;

    /// <summary> Indicates if Calibration has already started. </summary>
    private bool calibrating = false;
    /// <summary> Indicates which of the calibration steps is currently being performed. </summary>
    private int calSteps = 0;

    //--------------------------------------------------------------------------------------------------------------------------
    // Events

    public delegate void GloveReadyEventHandler(object source, System.EventArgs args);
    public event GloveReadyEventHandler OnGloveLoaded;

    protected void GloveLoaded()
    {
        if (OnGloveLoaded != null)
        {
            OnGloveLoaded(this, null);
        }
    }


    public delegate void CalibrationFinishedEventHandler(object source, System.EventArgs args);
    public event CalibrationFinishedEventHandler OnCalibrationFinished;

    protected void CalibrationFinished()
    {
        if (OnCalibrationFinished != null)
        {
            OnCalibrationFinished(this, null);
        }
    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // Unity / MonoDevelop

    // Use this for initialization
    void Start()
    {
        RetryConnection();
    }

    // Update is called once per frame
    void Update()
    {
        if (gloveReady) { gloveReady = false; } //return the boolean to false, ensuring that it is only for one update, allowing us to use it like an event.

        if (canUpdate)
        {
            if (elapsedTime < setupTime)
            {
                elapsedTime += Time.deltaTime;
            }
            else if (!finishedSetup)
            {
                if (automaticMode)
                {
                    IODevice[] devices = DeviceScanner.GetDevices();
                    if (devices.Length > 0)
                    {
                        for (int i = 0; i < devices.Length; i++)
                        {
                            GloveData SD = ((SenseGlove)devices[i]).Update(Kinematics.GlovePositions, false);
                            if (devices[i] is SenseGlove && ((SenseGlove)devices[i]).IsRight() == this.rightHand)
                            {
                                glove = (SenseGlove)devices[i];
                                this.gloveData = SD;
                            }
                        }
                        if (glove == null) //the glove is still null even though we've gone through all of them
                        {
                            if (rightHand)
                            {
                                Debug.Log("WARNING : No right-handed gloves found...");
                            }
                            else
                            {
                                Debug.Log("WARNING : No left-handed gloves found...");
                            }
                            elapsedTime = 0;
                        }
                        else
                        {
                            Debug.Log("Connected to a SenseGlove with ID " + glove.communicator.deviceID + " on " + glove.communicator.Address());
                            if (!gloveReady)
                            {
                                GloveLoaded(); //raise the gloveloaded event.
                            }
                            gloveReady = true;
                            finishedSetup = true;
                        }
                    }
                    else
                    {
                        elapsedTime = 0; //no glove(s) detected....Keep searching again?
                        Debug.Log("Looking for SenseGlove...");
                    }
                }
                else
                {
                    if (glove == null)
                    {
                        if (this.COMPort.Length > 4 && this.COMPort.Length < 8) { this.COMPort = "\\\\.\\" + this.COMPort; } //Unity port name correction.
                        Communicator Teensy = new SerialCommunicator(this.COMPort);
                        Teensy.Connect();
                        if (Teensy.IsConnected())
                        {
                            //Debug.Log("Connected to a SenseGlove on " + Teensy.Address());
                            this.glove = new SenseGlove(Teensy);
                        }
                        else
                        {
                            Debug.Log("WARNING : Could not connect to the SenseGlove on " + Teensy.Address() + ".");
                        }
                    }
                    else if (glove.IsConnected())
                    {
                        gloveData = glove.Update(Kinematics.GlovePositions, false);
                        gloveReady = true;
                        finishedSetup = true;
                        this.SetupWrist();
                    }
                    else
                    {
                        //the glove has disconnected D:
                        RetryConnection();
                    }
                }
            }
            else if (glove != null)
            {
                if (glove.gloveData.dataLoaded && !wristCalibrated)
                {
                    this.SetupWrist();
                    wristCalibrated = true;
                }


                //Update the Glove every frame, using the correct input.
                float[] foreArmRotation;
                if (foreArm == null)
                {
                    foreArmRotation = Quaternions.Identity();
                }
                else
                {
                    foreArmRotation = SenseGlove_Util.ToQuaternion(this.foreArm.transform.rotation);
                }
                gloveData = glove.Update(this.updateTo, this.updateWrist, foreArmRotation, this.checkGestures);
                //Debug.Log( SenseGlove_Util.ToString( glove.gloveData.gloveValues[1] ));

                //Update the public values, based on the detection mode.
                if (automaticMode) { this.COMPort = glove.communicator.Address(); }
                else { this.rightHand = glove.IsRight(); }

                //if (glove.IsConnected())
                //{
                //    Debug.Log("The glove is still connected!");
                //}
                //else
                //{
                //    Debug.Log("The glove is no longer connected...");
                //}


            }
        }
    }

    // OnApplicationQuit is called when the game shuts down.
    void OnApplicationQuit()
    {
        this.CancelCalibration();
        if (glove != null)
        {
            glove.Disconnect();
            Debug.Log("Disconnected the SenseGlove on " + glove.communicator.Address());
        }
        DeviceScanner.CleanUp();
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Communication methods.

    /// <summary>
    /// Disconnect the glove and stop updating until the RetryConnection is called.
    /// This allows a developer to change the communication variables before calling the RetryConnection method.
    /// </summary>
    public void Disconnect()
    {
        canUpdate = false;
        gloveReady = false;
        if (glove != null)
        {
            glove = null; //if done via DeviceScanner, the destructor should not be called. Else the destructor will be called and the connection will end.
        }
    }

    /// <summary> 
    /// Disconnect and retry the connecting to the SenseGlove, 
    /// such as when a different glove is connected or when the (manual) connection is lost. 
    /// </summary>
    public void RetryConnection()
    {
        if (glove != null)
        {
            glove = null;
        }

        if (automaticMode)
        {
            if (!DeviceScanner.IsScanning())
            {
                DeviceScanner.StartScanning(true); //Start the DeviceScanner if it is not already running.
            }
        }

        elapsedTime = 0;
        gloveReady = false;
        canUpdate = true;
        finishedSetup = false;
    }

    /// <summary>
    /// Check if the GloveReady flag is up. Use this function like an event in the Update() function : if ( mySenseGloveObject.GloveReady() ) { doSomeSetupStuff; } ) 
    /// </summary>
    /// <returns></returns>
    public bool GloveReady()
    {
        return this.gloveReady;
    }

    /// <summary>
    /// Check if this glove's setup is completed. A.k.a. The glove is loaded.
    /// </summary>
    /// <returns></returns>
    public bool SetupFinished()
    {
        return this.finishedSetup;
    }

    /// <summary> Manually assign IMU Correction </summary>
    public void SetupWrist()
    {
        if (this.glove != null && this.glove.gloveData.dataLoaded)
        {
            string ID = this.gloveData.deviceID;
            string[] gloveVersion = this.gloveData.firmwareVersion.Split('.');
            if (gloveVersion[0][0] == 'v') { gloveVersion[0] = gloveVersion[0].Substring(1); } //if there is a v in front of it, remove this.
            int mainVersion = int.Parse(gloveVersion[0]);
            int subVersion = int.Parse(gloveVersion[1]);
            if (mainVersion <= 2 && subVersion <= 19)
            {
                if (ID.Contains("120101"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(Mathf.PI, 0, 0)); //correction for glove 1
                    Debug.Log("Glove Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120203"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(0, 0, Mathf.PI / 2.0f)); //correction?
                    Debug.Log("Glove Version v2.19 or earlier. Adding Hardware Compensation");
                }
                else if (ID.Contains("120307") || ID.Contains("120204") || ID.Contains("120310") || ID.Contains("120309") || ID.Contains("120311") || ID.Contains("120312"))
                {
                    this.glove.gloveData.wrist.SetHardwareOrientation(Quaternions.FromEuler(0, 0, Mathf.PI)); //correction for glove 7 & 4?  
                    Debug.Log("Glove Version v2.19 or earlier. Adding Hardware Compensation");
                }
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Wrapper methods

    /// <summary>
    /// Retrieve the GloveData from this SenseGlove, with which one can access all kinds of information.
    /// </summary>
    /// <returns></returns>
    public GloveData GetGloveData()
    {
        return this.gloveData;
    }

    /// <summary>
    /// Retrieve the SenseGlove Object, which can be used for calibration or connection purposes.
    /// </summary>
    /// <returns></returns>
    public SenseGlove GetSenseGlove()
    {
        return this.glove;
    }

    /// <summary>
    /// Initializa Calibration of the chosen fingers and the chosen complexity
    /// </summary>
    /// <param name="whichFingers"></param>
    /// <param name="simpleCalibration"></param>
    /// <returns></returns>
    public bool Calibrate(bool[] whichFingers, bool simpleCalibration = true)
    {
        if (glove != null && glove.IsConnected())
        {
            glove.Calibrate(whichFingers, simpleCalibration);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Proceed to the next calibration step of the fingers.
    /// </summary>
    /// <returns></returns>
    private bool NextCalibrationStep()
    {
        if (glove != null && glove.IsConnected())
        {
            glove.NextCalibrationStep();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reset the Calibration of the glove if, for instance, something went wrong.
    /// </summary>
    public void CancelCalibration()
    {
        if (glove != null)
        {
            glove.ResetCalibration();
            Debug.Log("Canceled Calibration");
        }
    }


    /// <summary>
    /// Perform the next calibration step of the fingers. If we are not already calibrating, start the calibration!
    /// </summary>
    /// <param name="calibrationKey">The key used to call this method, used for the debug messages.</param>
    public void NextCalibrationStep(KeyCode calibrationKey)
    {
        if (glove != null && finishedSetup)
        {

            if (!calibrating)
            {
                this.CancelCalibration();
                this.Calibrate(new bool[] { false, true, true, true, true }, true);
                calSteps = 0;
                // Debug.Log("Calibrate is called : " + calSteps);
            }
            else if (calSteps <= 3)
            {
                this.NextCalibrationStep();
                // Debug.Log("NextStep is called : " + calSteps);
            }

            if (calSteps == 0)
            {
                calibrating = true;
                Debug.Log("Started Calibration. Please stretch your fingers in front of you and press " + calibrationKey.ToString());
                calSteps++;
            }
            else if (calSteps == 1)
            {
                Debug.Log("Step 1 completed. Please bend you MCP joint to 45* and press " + calibrationKey.ToString());
                calSteps++;
            }
            else if (calSteps == 2)
            {
                Debug.Log("Step 2 completed. Please bend you MCP joint to 90* and press " + calibrationKey.ToString());
                calSteps++;
            }
            else
            {
                Debug.Log("Step 3 completed. Calibration has finished. Resizing model.");
                calibrating = false;
                calSteps = 0;
                CalibrationFinished(); //raise the calibrationFinished event.
            }
        }

    }

    /// <summary>
    /// Set the finger lengths used but this sense glove as a 5x3 array, 
    /// which contains the Proximal-, Medial-, and Distal Phalange lengths for each finger, in that order.
    /// </summary>
    /// <param name="newFingerLengths"></param>
    public void SetFingerLengths(float[][] newFingerLengths)
    {
        if (this.glove != null)
        {
            this.glove.SetHandLengths(newFingerLengths);
        }
    }

    /// <summary>
    /// Retrive the finger lengths used by this SenseGlove. 
    /// Returns a 5x3 array which contains the Proximal-, Medial-, and Distal Phalange lengths for each finger, in that order.
    /// Returns an empty array if unsuccesfull.
    /// </summary>
    /// <returns></returns>
    public float[][] GetFingerLengths()
    {
        if (this.glove != null)
        {
            return this.gloveData.handModel.GetFingerLengths();   
        }
        return new float[][] { };
    }
   

    /// <summary>
    /// Calibrate the Wrist, based on the orientatio of the foreArm.
    /// </summary>
    /// <returns></returns>
    public bool CalibrateWrist()
    {
        if (glove != null && glove.IsConnected() && foreArm != null)
        {
            glove.CalibrateWrist(null, SenseGlove_Util.ToQuaternion(this.foreArm.transform.rotation));
            Debug.Log("Calibrated Wrist");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Set the current lowerArm quaternion as the 'zero'
    /// </summary>
    /// <param name="lowerArm"></param>
    /// <returns></returns>
    public bool CalibrateWrist(Quaternion lowerArm)
    {
        if (glove != null && glove.IsConnected())
        {
            glove.CalibrateWrist(null, SenseGlove_Util.ToQuaternion(lowerArm));
            Debug.Log("Calibrated Wrist");
            return true;
        }
        return false;
    }

}
