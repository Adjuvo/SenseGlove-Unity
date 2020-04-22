using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 
/// A Generic Script that can be extended to work with most hand models. 
/// It requires the developer to assign the correct transforms for each joint. 
/// All of its methods can be overridden to create custom solutions.
/// </summary>
public abstract class SG_HandAnimator : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Properties Variables

    #region Properties

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Public Variables

    /// <summary> The Sense Glove that controls this hand model. /summary>
    [Header("Linked Scripts")]
    [Tooltip("The Sense Glove that controls this hand model. If no glove is assigned, the script will attempt to GetComponent from this object.")]
    public SG_SenseGloveHardware senseGlove;

    /// <summary> Whether or not to update the fingers of this Hand Model. </summary>
    protected bool updateFingers = true;

    /// <summary> Whether or not to update the wrist of this Hand Model. </summary>
    [Header("Settings")]
    [Tooltip("Whether or not to update the wrist of this Hand Model.")]
    public bool updateWrist = false;

    /// <summary> Whether or not to resize the fingers after calibration completes. </summary>
    [Tooltip("Whether or not to resize the fingers after calibration completes.")]
    protected bool resizeFingers = false;

    /// <summary> The GameObject representing the Forearm. </summary>
    [Header("Animation Components")]
    [Tooltip("The GameObject representing the Forearm.")]
    public Transform foreArmTransfrom;

    /// <summary> The GameObject representing the Wrist, moves relative to the foreArm. </summary>
    [Tooltip("The GameObject representing the Wrist, moves relative to the foreArm.")]
    public Transform wristTransfrom;



    /// <summary> The TrackedHand this Animator takes its data from, used to access grabscript, hardware, etc. </summary>
    public SG_TrackedHand Hand
    {
        get; protected set;
    }

    /// <summary> Returns true if this Animator is connected to Hardware that is ready to go </summary>
    public virtual bool HardwareReady
    {
        get { return this.senseGlove != null && this.senseGlove.GloveReady; }
    }

    /// <summary> Returns true if this Animator is connected to Sense Glove Hardware. Used in an if statement for safety </summary>
    /// <param name="hardware"></param>
    /// <returns></returns>
    public virtual bool GetHardware(out SG_SenseGloveHardware hardware)
    {
        if (HardwareReady)
        {
            hardware = this.senseGlove;
            return hardware != null;
        }
        hardware = null;
        return false;
    }

    /// <summary> Check for Scripts relevant for this Animator </summary>
    protected virtual void CheckForScripts()
    {
        if (this.Hand == null)
        {
            this.Hand = SG_Util.CheckForTrackedHand(this.transform);
        }
        if (this.senseGlove == null && this.Hand != null)
        {
            this.senseGlove = this.Hand.hardware;
        }
    }


    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Internal Variables.

    /// <summary> The list of finger joint transforms, used to manipulate the angles. Assigned in the CollectFingerJoints() function. </summary>
    protected Transform[][] fingerJoints = new Transform[0][];

    /// <summary> The initial angles of the hand model, corresponding to (0, 0, 0) rotation of the fingers. </summary>
    protected List<List<Quaternion>> fingerCorrection = new List<List<Quaternion>>();

    /// <summary> Offset between the wrist and lower arm, used when updating the wrist transfrom. </summary>
    protected Quaternion wristCorrection = Quaternion.identity;

    /// <summary> Quaternion that aligns the lower arm with the wrist at the moment of calibration. </summary>
    protected Quaternion wristCalibration = Quaternion.identity;

    /// <summary> The relative angles between wrist and lower arm transforms. </summary>
    protected Quaternion wristAngles = Quaternion.identity;

    /// <summary> A container for the motor level debug texts to easily toggle it on/off. </summary>
    protected GameObject debugGroup;

    /// <summary> Show the motor levels as determine by the feedback colliders on the fingers. </summary>
    protected TextMesh[] debugText;

    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // Glove Events

    /// <summary> Utility method when the Sense Glove finishes loading. Determine left / right, for example. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    protected virtual void SenseGlove_OnGloveLoaded(object source, System.EventArgs args)
    {
        //If no joints or corrections were added yet, retry.
        if (this.fingerJoints.Length == 0)
        {
            this.CollectFingerJoints();
        }
        if (this.fingerCorrection.Count == 0)
        {
            this.CollectCorrections();
        }

        if (this.fingerJoints.Length > 0)
        {
            if (this._jointPositions.Length < 0)
                this.CollectHandParameters();

            ////TODO: Apply this to the Sense Glove
            //if (this.senseGlove != null && senseGlove.solver == SenseGloveCs.Solver.DistanceBased)
            //    this.senseGlove.SetHandParameters(this._jointPositions, this._handLengths);
            ////else
            ////    Debug.Log("Sense Glove is null. Something is wrong?");
        }

        //this.SetupDebugText();
        this.CalibrateWrist();
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


    /// <summary> Collect the absolute angles of the fingers in their 'calibration' pose, correct these with the current wrist orientation. </summary>
    protected virtual void CollectCorrections() 
    {
        if (this.fingerCorrection.Count == 0 && this.fingerJoints != null && this.fingerJoints.Length > 0)
        {
            this.fingerCorrection.Clear();
            for (int f = 0; f < this.fingerJoints.Length; f++)
            {
                List<Quaternion> fingerAngles = new List<Quaternion>();
                for (int j = 0; j < this.fingerJoints[f].Length; j++)
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


    protected Vector3[] _jointPositions = new Vector3[0];
    protected Vector3[][] _handLengths = new Vector3[0][];

    /// <summary> collects the starting positions and rotations of the VHM, which can later be applied to Sense Glove models </summary>
    public virtual void CollectHandParameters()
    {
        if (this.fingerJoints.Length > 0)
        {
            this._jointPositions = new Vector3[this.fingerJoints.Length];
            this._handLengths = new Vector3[this.fingerJoints.Length][];
            for (int f = 0; f < this._jointPositions.Length; f++)
            {
                if (fingerJoints[f].Length > 0)
                {
                    Vector3 relPos = DifferenceFromWrist(this.wristTransfrom, this.fingerJoints[f][0].position);
                    this._jointPositions[f] = new Vector3(relPos.x * 1000, relPos.y * 1000, relPos.z * 1000);

                    this._handLengths[f] = new Vector3[this.fingerJoints[f].Length - 1];
                    for (int i = 0; i < this._handLengths[f].Length; i++)
                    {
                        Vector3 dV = DifferenceFromWrist(this.wristTransfrom, this.fingerJoints[f][i + 1].position)
                            - DifferenceFromWrist(this.wristTransfrom, this.fingerJoints[f][i].position);
                        this._handLengths[f][i] = new Vector3(dV.x * 1000, dV.y * 1000, dV.z * 1000);
                    }
                }
            }
        }
    }

    /// <summary> Calculates the difference between an absolute position and the wrist transform, without scaling. </summary>
    /// <param name="wristTransfrom"></param>
    /// <param name="absPos"></param>
    /// <returns></returns>
    public static Vector3 DifferenceFromWrist(Transform wristTransfrom, Vector3 absPos)
    {
        return Quaternion.Inverse(wristTransfrom.rotation) * (absPos - wristTransfrom.position);
    }

    #endregion Setup

    //-----------------------------------------------------------------------------------------------------------------------------------------
    // HandModel Calibration

    /// <summary> Calibrate the wrist model of this handModel. </summary>
    public void CalibrateWrist()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady && this.foreArmTransfrom != null)
        {
           // Debug.Log(this.name + ": Calibrated Wrist");
            this.wristCalibration = this.foreArmTransfrom.rotation * Quaternion.Inverse(this.senseGlove.GloveData.absoluteWrist);
        }
    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // Accessors

    /// <summary> Retrieve the Quaterion rotation between this model's foreArm and Wrist. </summary>
    public Quaternion RelativeWrist
    {
        get { return this.wristAngles; }
    }

    /// <summary> Retrive the euler angles between this model's foreArm and Wrist.  </summary>
    public Vector3 WristAngles
    {
        get { return SG_Util.NormalizeAngles(this.wristAngles.eulerAngles); }
    }

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
            for (int f = 0; f < this.fingerJoints.Length; f++)
            {
                for (int j = 0; j < this.fingerJoints[f].Length; j++)
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
                this.wristTransfrom.rotation = /*this.foreArmTransfrom.rotation */ this.wristCorrection * (this.wristCalibration * data.absoluteWrist);
                this.wristAngles = Quaternion.Inverse(this.foreArmTransfrom.rotation) * this.wristTransfrom.rotation;
            }
        }
        else
        {
            this.wristTransfrom.rotation = this.wristCorrection * this.foreArmTransfrom.rotation;
            this.wristAngles = Quaternion.identity; //ignore wrist angle(s).
        }
    }

    /// <summary> Resize the finger lengths of this hand model to reflect that of the current user. </summary>
    /// <param name="newLengths"></param>
    public virtual void ResizeHand(float[][] newLengths)
    {
        //return the hand to a position where the handAngles are 0

        for (int f=0; f<newLengths.Length; f++)
        {
            if (newLengths.Length > f && newLengths[f].Length > this.fingerJoints[f].Length)
            {
                Vector3 MCP = this.fingerJoints[f][0].position;
                Vector3 P = MCP; // struct
                for (int i=1; i<this.fingerJoints[f].Length; i++)
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
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start()
    {
        CheckForScripts();
        if (senseGlove != null)
        {
            senseGlove.GloveLoaded += SenseGlove_OnGloveLoaded;
            senseGlove.CalibrationFinished += SenseGlove_OnCalibrationFinished;
        }

        if (this.foreArmTransfrom == null) { this.foreArmTransfrom = this.transform; }

        this.CollectFingerJoints();
        this.CollectCorrections();

        if (this.fingerJoints.Length > 0)
            this.CollectHandParameters();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady)
        {
            this.UpdateWrist(this.senseGlove.GloveData);

            if (this.updateFingers /*&& !(this.senseGlove != null && this.senseGlove.solver == SenseGloveCs.Solver.Custom)*/)
            {   //in case of a custom solver, the model waits for an external UpdateHand call
                this.UpdateHand(this.senseGlove.GloveData);
            }
        }
    }



#if UNITY_EDITOR
    void OnValidate()
    {
        this.Hand = null;
        CheckForScripts();
    }
#endif



    #endregion Monobehaviour


}
