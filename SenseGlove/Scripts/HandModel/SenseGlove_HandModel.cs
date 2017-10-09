using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A Generic Script that can be extended to work with most hand models. It requires the developer to assign the correct transforms. </summary>
public abstract class SenseGlove_HandModel : MonoBehaviour
{
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
    public ForceFeedbackType forceFeedback = ForceFeedbackType.None;

    /// <summary> The GameObject representing the Forearm. </summary>
    [Header("Model Components")]
    [Tooltip("The GameObject representing the Forearm.")]
    public Transform foreArmTransfrom;

    /// <summary> The GameObject representing the Wrist, moves relative to the foreArm. </summary>
    [Tooltip("The GameObject representing the Wrist, moves relative to the foreArm.")]
    public Transform wristTransfrom;

    /// <summary> The GameObject on which the fingers are connected. Can be used to collect the finger joints. </summary>
    [Tooltip("The GameObject on which the fingers are connected. Can be used to collect the finger joints.")]
    public Transform handRoot;
    
    /// <summary> The last sent motor levels of the SenseGlove. </summary>
    [Header("Force Feedback Components")]
    [Tooltip("The determined motor levels of the SenseGlove.")]
    protected int[] motorLevels = new int[] { 0, 0, 0, 0, 0 };

    /// <summary>The touch colliders in this hand model, that are used to create force Feedback.</summary>
    [Tooltip("The touch colliders in this hand model, which are used to create force Feedback.")]
    public List<SphereCollider> touchColliders = new List<SphereCollider>();

    /// <summary> The force feedback scripts with which to determine fore feedback. </summary>
    protected SenseGlove_Feedback[] feedbackScripts;

    /// <summary> Optional grabscript connected to this hand model </summary>
    protected SenseGlove_GrabScript grabScript;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Internal Variables.

    /// <summary> The list of finger joint transforms, used to manipulate the angles. Assigned in the CollectFingerJoints() function. </summary>
    protected List<List<Transform>> fingerJoints = new List<List<Transform>>();

    /// <summary> The initial angles of the hand model, corresponding to (0, 0, 0) rotation of the fingers. </summary>
    protected List<List<Quaternion>> fingerCorrection = new List<List<Quaternion>>();

    /// <summary> Offset between the wrist and lower arm, used when updating the wrist transfrom. </summary>
    protected Quaternion wristCorrection = Quaternion.identity;

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    //As soon as the model awakens, only once.
    void Awake()
    {
        this.CollectFingerJoints();
        this.CollectCorrections();
        
        this.SetupGrabScript();
        this.SetupFeedbackColliders();
    }

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
    }

    // Update is called once per frame
    void Update()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady())
        {
            this.UpdateWrist(this.senseGlove.GloveData());

            if (this.updateFingers)
            {
                this.UpdateHand(this.senseGlove.GloveData());
            }
        }

        this.UpdateForceFeedback();

    }

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Glove Events

    /// <summary> Utility method when the Sense Glove finishes loading. Determine left / right, for example. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_OnGloveLoaded(object source, System.EventArgs args)
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
    }

    /// <summary> Call the ResizeFingers function. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_OnCalibrationFinished(object source, CalibrationArgs args)
    {
        //no resizing for now...
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Setup Methods

    /// <summary> Collect a proper (finger x joint) array, and assign it to this.fingerJoints(). Use the handRoot variable to help you iterate. </summary>
    protected abstract void CollectFingerJoints(); //Abstract

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
                feedbackCollider.Setup(this);
                feedbackCollider.SetForceFeedback(this.forceFeedback);
                this.feedbackScripts[f] = feedbackCollider;
            }
        }
    }

    /// <summary> Collect the absolute angles of the fingers in their çalibration' pose, correct these with the current wrist orientation. </summary>
    protected virtual void CollectCorrections() //virtual
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
            SenseGlove_Debugger.Log("Warning: No finger joints were collected...");
        }

        this.wristCorrection = Quaternion.Inverse(this.foreArmTransfrom.rotation) * this.wristTransfrom.rotation;
    }


    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Update Methods
    

    /// <summary> 
    /// Update the (absolute) finger orientations, which move realtive to the (absolute) wrist transform. 
    /// Note: This method is called after UpdateWrist() is called. 
    /// </summary>
    /// <param name="data"></param>
    protected virtual void UpdateHand(SenseGlove_Data data)
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
    protected virtual void UpdateWrist(SenseGlove_Data data)
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

    /// <summary> Collect force feedback levels from the feedbackScripts and send these to the Sense Glove. </summary>
    protected virtual void UpdateForceFeedback()
    {
        this.motorLevels = new int[5] { 0, 0, 0, 0, 0 };
        if (this.forceFeedback != ForceFeedbackType.None && this.feedbackScripts != null)
        {
            //for now, fingertips only
            for (int f = 0; f < this.feedbackScripts.Length; f++)
            {
                this.motorLevels[f] = this.feedbackScripts[f].motorLevel;
            }
            this.senseGlove.SimpleBrakeCmd(this.motorLevels);
        }
    }

}
