using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// A Generic Script that can be extended to work with most hand models. 
/// It requires the developer to assign the correct transforms for each joint. 
/// All of its methods can be overrided to create custom solutions.
/// </summary>
public abstract class SenseGlove_HandModel : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Properties Variables

    #region Properties

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Public Variables

    /// <summary> The Sense Glove that controls this hand model. /summary>
    [Header("Settings")]
    [Tooltip("The Sense Glove that controls this hand model. If no glove is assigned, the script will attempt to GetComponent from this object.")]
    public SenseGlove_Object senseGlove;

    /// <summary> Whether or not to update the fingers of this Hand Model. </summary>
    [Tooltip("Whether or not to update the fingers of this Hand Model.")]
    public bool updateFingers = true;

    /// <summary> Whether or not to update the wrist of this Hand Model. </summary>
    [Tooltip("Whether or not to update the wrist of this Hand Model.")]
    public bool updateWrist = true;

    /// <summary> Determines which type of force feedback to use for this model. </summary>
    [Tooltip("Determines which type of force feedback to use for this model.")]
    public ForceFeedbackType forceFeedback = ForceFeedbackType.MaterialBased;

    /// <summary> Whether or not to resize the fingers after calibration completes. </summary>
    [Tooltip("Whether or not to resize the fingers after calibration completes.")]
    public bool resizeFingers = false;

    /// <summary> The GameObject representing the Forearm. </summary>
    [Header("Model Components")]
    [Tooltip("The GameObject representing the Forearm.")]
    public Transform foreArmTransfrom;

    /// <summary> The GameObject representing the Wrist, moves relative to the foreArm. </summary>
    [Tooltip("The GameObject representing the Wrist, moves relative to the foreArm.")]
    public Transform wristTransfrom;

    /// <summary>The touch colliders in this hand model, that are used to create force Feedback.</summary>
    [Header("Force Feedback Components")]
    [Tooltip("The touch colliders in this hand model, which are used to create force Feedback.")]
    public List<SphereCollider> touchColliders = new List<SphereCollider>();

    /// <summary> The last sent motor levels of the SenseGlove. </summary>
    [Tooltip("The determined motor levels of the SenseGlove.")]
    public int[] motorLevels = new int[5] { 0, 0, 0, 0, 0 };

    /// <summary> Debug the motor levels </summary>
    [Tooltip("Text apprears on the touchColliders, showing the current motor level.")]
    public bool debugMotorLevels = false;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Internal Variables.

    /// <summary> The force feedback scripts with which to determine fore feedback. </summary>
    protected SenseGlove_Feedback[] feedbackScripts;

    /// <summary> Optional grabscript connected to this hand model </summary>
    protected SenseGlove_GrabScript grabScript;


    /// <summary> The list of finger joint transforms, used to manipulate the angles. Assigned in the CollectFingerJoints() function. </summary>
    protected List<List<Transform>> fingerJoints = new List<List<Transform>>();

    /// <summary> The initial angles of the hand model, corresponding to (0, 0, 0) rotation of the fingers. </summary>
    protected List<List<Quaternion>> fingerCorrection = new List<List<Quaternion>>();

    /// <summary> Offset between the wrist and lower arm, used when updating the wrist transfrom. </summary>
    protected Quaternion wristCorrection = Quaternion.identity;

    /// <summary> A container for the motor level debug texts to easily toggle it on/off. </summary>
    protected GameObject debugGroup;

    /// <summary> Show the motor levels as determine by the feedback colliders on the fingers. </summary>
    protected TextMesh[] debugText;

    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    void Start()
    {
        if (senseGlove == null)
        {
            senseGlove = this.GetComponent<SenseGlove_Object>();
        }
        if (senseGlove != null)
        {
            if (senseGlove.foreArm == null) { senseGlove.foreArm = this.foreArmTransfrom.gameObject; }
            senseGlove.OnGloveLoaded += SenseGlove_OnGloveLoaded;
            senseGlove.OnCalibrationFinished += SenseGlove_OnCalibrationFinished;
        }

        this.CollectFingerJoints();
        this.CollectCorrections();

        this.SetupGrabScript();
        this.SetupFeedbackColliders();
    }

    // Update is called once per frame
    void Update()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady())
        {
            this.UpdateWrist(this.senseGlove.GloveData());

            if (this.updateFingers &&  !(this.senseGlove != null && this.senseGlove.solver == SenseGloveCs.Solver.Custom) )
            {   //in case of a custom solver, the model waits for an external UpdateHand call
                this.UpdateHand(this.senseGlove.GloveData());
            }
        }
    }

    //called at the end of all Update functions
    void LateUpdate()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady())
        {
            this.UpdateForceFeedback();
            this.UpdateDebugText();
            this.UpdateHapticFeedback();
        }
    }

    #endregion Monobehaviour

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Glove Events

    /// <summary> Utility method when the Sense Glove finishes loading. Determine left / right, for example. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    protected virtual void SenseGlove_OnGloveLoaded(object source, System.EventArgs args)
    {
        //If no joints or corrections were added yet, retry.
        if (this.fingerJoints.Count == 0)
        {
            this.CollectFingerJoints();
        }
        if (this.fingerCorrection.Count == 0)
        {
            this.CollectCorrections();
        }
        this.SetupDebugText();
    }

    /// <summary> Call the ResizeFingers function. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    protected virtual void SenseGlove_OnCalibrationFinished(object source, GloveCalibrationArgs args)
    {
        if (this.resizeFingers)
        {
            this.ResizeHand(args.newData.GetFingerLengths());
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Setup Methods

    #region Setup

    /// <summary> Collect a proper (finger x joint) array, and assign it to this.fingerJoints(). Use the handRoot variable to help you iterate. </summary>
    protected abstract void CollectFingerJoints(); 

    /// <summary> Setup a grabScript if one is attached to this handModel. </summary>
    protected virtual void SetupGrabScript()
    {
        if (this.grabScript == null)
        {
            this.grabScript = this.GetComponent<SenseGlove_GrabScript>();
        }

        if (this.grabScript != null)
        {
            this.grabScript.handModel = this;
            if (this.grabScript.grabReference == null) { this.grabScript.grabReference = this.wristTransfrom.gameObject; }
            this.grabScript.Setup();
        }
    }

    /// <summary> Set up the touchColliders to send force feedback back to the hand. </summary>
    protected virtual void SetupFeedbackColliders()
    {
        if (this.touchColliders != null && this.touchColliders.Count > 0)
        {
            int n = this.touchColliders.Count > 5 ? 5 : this.touchColliders.Count; //ensure we have enough colliders.
            this.feedbackScripts = new SenseGlove_Feedback[n];
            if (this.touchColliders.Count > 0)
            {
                for (int f = 0; f < this.feedbackScripts.Length; f++)
                {
                    SenseGlove_Feedback feedbackCollider = this.touchColliders[f].GetComponent<SenseGlove_Feedback>();
                    if (feedbackCollider == null)
                    {
                        feedbackCollider = this.touchColliders[f].gameObject.AddComponent<SenseGlove_Feedback>();
                    }
                    feedbackCollider.touch = this.touchColliders[f];
                    feedbackCollider.Setup(this, f);
                    this.feedbackScripts[f] = feedbackCollider;
                }
            }
        }
    }

    /// <summary> Collect the absolute angles of the fingers in their 'calibration' pose, correct these with the current wrist orientation. </summary>
    protected virtual void CollectCorrections() 
    {
        if (this.fingerJoints != null && this.fingerJoints.Count > 0)
        {
            this.fingerCorrection.Clear();
            for (int f = 0; f < this.fingerJoints.Count; f++)
            {
                List<Quaternion> fingerAngles = new List<Quaternion>();
                for (int j = 0; j < this.fingerJoints[f].Count; j++)
                {
                    fingerAngles.Add(Quaternion.Inverse(this.wristTransfrom.rotation) * this.fingerJoints[f][j].rotation);
                }
                this.fingerCorrection.Add(fingerAngles);
            }
        }
        else
        {
            //SenseGlove_Debugger.Log("Warning: No finger joints were collected...");
        }

        this.wristCorrection = Quaternion.Inverse(this.foreArmTransfrom.rotation) * this.wristTransfrom.rotation;
    }

    /// <summary> Setup the debug texts for the motor levels </summary>
    protected virtual void SetupDebugText()
    {
        if (this.touchColliders != null)
        {
            //creating a new group to house the debug text
            this.debugGroup = new GameObject("DebugContainer");

            int scale = this.senseGlove.GloveData().gloveSide == GloveSide.RightHand ? -1 : 1;

            int n = this.touchColliders.Count > 5 ? 5 : this.touchColliders.Count;
            this.debugText = new TextMesh[n];
            for (int f = 0; f < n; f++)
            {
                GameObject meshObject = new GameObject("MotorLevel_" + f);
                meshObject.transform.parent = this.debugGroup.transform;

                TextMesh newMesh = meshObject.AddComponent<TextMesh>();
                newMesh.alignment = TextAlignment.Center;
                newMesh.anchor = TextAnchor.MiddleCenter;

                newMesh.text = "000";
                newMesh.fontSize = 50;
                newMesh.transform.localScale = new Vector3(scale, 1, 1);

                this.debugText[f] = newMesh;
            }

            this.debugGroup.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f); //done before assigning parent, so not it is world scale.
            this.debugGroup.transform.parent = this.transform;
            this.debugGroup.transform.localPosition = Vector3.zero;

            this.debugGroup.SetActive(this.debugMotorLevels);
        }
    }

    #endregion Setup

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Update Methods

    #region Update

    /// <summary> 
    /// Update the (absolute) finger orientations, which move realtive to the (absolute) wrist transform. 
    /// Note: This method is called after UpdateWrist() is called. 
    /// </summary>
    /// <param name="data"></param>
    public virtual void UpdateHand(SenseGlove_Data data)
    {
        if (data.dataLoaded)
        {
            Quaternion[][] angles = data.handRotations;
            for (int f = 0; f < this.fingerJoints.Count; f++)
            {
                for (int j = 0; j < this.fingerJoints[f].Count; j++)
                {
                    this.fingerJoints[f][j].rotation = this.wristTransfrom.rotation * (angles[f][j] * this.fingerCorrection[f][j]);
                }
            }
        }
    }

    /// <summary> 
    /// Update the (absolute) wrist orientation, which moves realtive to the (absolute) lower arm transform. 
    /// Note: This method is called before UpdateFingers() is called.  
    /// </summary>
    /// <param name="data"></param>
    public virtual void UpdateWrist(SenseGlove_Data data)
    {
        if (this.updateWrist && data.dataLoaded)
        {
            if (data.dataLoaded)
            {
                this.wristTransfrom.rotation = this.foreArmTransfrom.rotation * this.wristCorrection * data.relativeWrist;
            }
        }
        else
        {
            this.wristTransfrom.rotation = this.wristCorrection * this.foreArmTransfrom.rotation;
        }
    }

    /// <summary> Resize the finger lengths of this hand model to reflect that of the current user. </summary>
    /// <param name="newLengths"></param>
    public virtual void ResizeHand(float[][] newLengths)
    {
        //return the hand to a position where the handAngles are 0


        //
        for (int f=0; f<newLengths.Length; f++)
        {
            if (newLengths.Length > f && newLengths[f].Length > this.fingerJoints[f].Count)
            {
                Vector3 MCP = this.fingerJoints[f][0].position;
                Vector3 P = MCP; // struct
                for (int i=1; i<this.fingerJoints[f].Count; i++)
                {
                    P = MCP + new Vector3(newLengths[f][i], 0, 0);
                    this.fingerJoints[f][i].position = P;
                }
            }
        }

        //reset the hand back to a normal position
    }

    #endregion Update


    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Feedback Methods

    #region Feedback


    /// <summary> Collect force feedback levels from the feedbackScripts and send these to the Sense Glove. </summary>
    protected virtual void UpdateForceFeedback()
    {
        this.motorLevels = new int[5] { 0, 0, 0, 0, 0 };
        if (this.forceFeedback != ForceFeedbackType.None && this.feedbackScripts != null)
        {
            //for now, fingertips only
            for (int f = 0; f < this.feedbackScripts.Length; f++)
            {
                this.motorLevels[f] = this.feedbackScripts[f].BrakeLevel();
            }
            this.senseGlove.SendBrakeCmd(this.motorLevels);
        }
    }

    /// <summary> Collect the haptic feedback commands from the feedback colliders and send these to the Sense Glove. </summary>
    protected virtual void UpdateHapticFeedback()
    {

        bool[] fingers = new bool[5];       //all false
        int[] magnitudes = new int[5];  //all 0
        int[] durations = new int[5];

        bool atLeastOne = false;
        if (this.feedbackScripts != null)
        {
            for (int f = 0; f < this.feedbackScripts.Length; f++)
            {
                if (this.feedbackScripts[f].CanBuzz())
                {
                    atLeastOne = true;
                    fingers[f] = true;
                    magnitudes[f] = this.feedbackScripts[f].BuzzLevel();
                    durations[f] = this.feedbackScripts[f].BuzzTime();

                    this.feedbackScripts[f].ResetHaptics();
                }
            }
        }
        if (atLeastOne)
        {
            this.senseGlove.SendBuzzCmd(fingers, durations, magnitudes); //advanced controls
                                                                         //this.senseGlove.GetSenseGlove().SimpleBuzzCmd(magnitudes); //simple controls
                                                                         //Debug.Log("Sent " + SenseGlove_Util.ToString(magnitudes) + " + " + SenseGlove_Util.ToString(durations));
                                                                         // SenseGlove_Debugger.Log(SenseGlove_Util.ToString(magnitudes));
        }
    }

    /// <summary> Update the motor level texts and se them to the appropriate position. </summary>
    protected virtual void UpdateDebugText()
    {
        if (this.debugMotorLevels)
        {
            if (!this.debugGroup.activeInHierarchy) { this.debugGroup.SetActive(true); }

            for (int f = 0; f < this.debugText.Length; f++)
            {
                this.debugText[f].transform.position = this.touchColliders[f].transform.position;
                this.debugText[f].text = "" + this.motorLevels[f];
            }
        }
        else if (this.debugGroup != null && this.debugGroup.activeInHierarchy)
        {
            this.debugGroup.SetActive(false);
        }
    }

    public void ClearFeedback()
    {
        if (this.feedbackScripts != null)
        {
            for (int i=0; i<this.feedbackScripts.Length; i++)
            {
                this.feedbackScripts[i].Detach();
            }
        }
    }

    #endregion Feedback


}

/// <summary> The way that the Force-Feedback is calculated. </summary>
public enum ForceFeedbackType
{
    /// <summary> No Force feedback is calculated for this SenseGlove. </summary>
    None = 0,
    /// <summary> On/Off style force feedback using the Material's 'passive force'  </summary>
    Simple,
    /// <summary> Force feedback is calculated based on how far the fingers have collided within the object. </summary>
    MaterialBased
}