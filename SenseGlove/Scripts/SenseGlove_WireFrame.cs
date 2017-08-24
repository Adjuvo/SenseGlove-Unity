using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SenseGloveCs;
using System;

/// <summary> A prefab script useful for debugging purposes. </summary>
public class SenseGlove_WireFrame : MonoBehaviour
{
    //--------------------------------------------------------------------------------------
    // Public global variables.

    [Header("Tracking")]

    /// <summary> The SenseGlove_Object that this Wireframe takes its data from. </summary>
    [Tooltip("The SenseGlove_Object that this Wireframe takes its data from.")]
    public SenseGlove_Object trackedGlove;

    /// <summary> The Object that the WireFrame will connect to on Awake() </summary>
    [Tooltip("The Object that the WireFrame will connect to on Awake()")]
    public GameObject trackedObject;

    /// <summary> Which of the models the trackedObject connects to. </summary>
    [Tooltip("Where the trackedObject connects to. Use ForeArm when the tracker is connected via a wrist strap. Use Wrist when the Tracker is attached to the SenseGlove directy.")]
    public AnchorPoint anchor = AnchorPoint.ForeArm;
    
    [Header("Wireframe Objects")]

    /// <summary> The GameObject containing the Hand- and Glove models. </summary>
    [Tooltip("The GameObject containing the Hand- and Glove models. The IMU rotation is applied here if the Wrist AnchorPoint is selected.")]
    public GameObject wrist;

    /// <summary> The GameObject containing the foreArm model(s). </summary>
    [Tooltip("The GameObject containing the ForeArm models. The IMU rotation is applied here if the ForeArm AnchorPoint is selected.")]
    public GameObject foreArm;

    /// <summary> The parent of the glovePositions, used to turn the hand model on- or off. </summary>
    [Tooltip("The parent of the glovePositions, used to turn the hand model on- or off.")]
    public GameObject gloveGroup;

    /// <summary> The parent of the handPositions, used to turn the hand model on-or off. </summary>
    [Tooltip("The parent of the handPositions, used to turn the hand model on-or off.")]
    public GameObject handGroup;

    /// <summary> The base model which is duplicated and adapted to represent a section of the Glove. </summary>
    [Tooltip("The base model which is duplicated and adapted to represent a section of the Glove.")]
    public GameObject gloveBase;
    /// <summary> The base model which is duplicated and adpted to represent a phalange of the hand. </summary>
    [Tooltip("The base model which is duplicated and adpted to represent a phalange of the hanger.")]
    public GameObject handBase;

    /// <summary> The preview model(s), which will be destroyed on startup. </summary>
    [Tooltip("The preview model(s) that will be destroyed on startup.")]
    public GameObject preview;

    /// <summary> Collider for the hand palm </summary>
    [Tooltip("An optional collider for the hand palm")]
    public Collider palmCollider;

    /// <summary> Determines if the fingers of this wireframe model will have a rigidBody or not. </summary>
    [Tooltip("Determines if the fingers of this wireframe model will have a rigidBody or not.")]
    public bool rigidFingers = false;

    //--------------------------------------------------------------------------------------
    // internal global variables.


    /// <summary>  Variable used for array indexing. </summary>
    private static int x = 0, y = 1, z = 2; //used to easily assign the correct coordinates.

    /// <summary> The Glove Positions for five fingers. The Positions and Rotations of each point are applied to these objects. </summary>
    private GameObject[][] glovePositions;
    /// <summary> The Hand Positions for five fingers. The Positions and Rotations of each joint are applied to these objects. </summary>
    private GameObject[][] handPositions;

    /// <summary> The offset between the trackedObject and the Wireframe Model. </summary>
    private Vector3 trackOffset = new Vector3(0, 0, 0);
    /// <summary> The rotation difference between the trackedObject and the Wireframe Model </summary>
    private Quaternion trackRotation = Quaternion.identity;

    /// <summary> Determines if the glovePositions have been created, which can only be done after the trackedGlove receives its glove data. </summary>
    private bool setupComplete = false;

    /// <summary> Used to calibrate the wrist once after setup. </summary>
    private bool wristCalibrated = false;
    
    /// <summary> Used to ensure that the wrist is properly calibrated, a set time after setup completes. </summary>
    private float calibrationTime = 0, calibrationTimer = 0.5f;

    /// <summary> used to force the Resize method during the next frame, as the Calibration requires a few ms to take effect.  </summary>
    private bool shouldResize = false;

    /// <summary> Used to keep the index MCP position of the WireFrame at the same location.  </summary>
    private Vector3 dJoints = Vector3.zero, oldMCPPosition = Vector3.zero;

    //------------------------------------------------------------------------------------------------------------------------------------
    // Unity / MonoDevelop


    // Awake is called when the Program starts, before SteamVR has a chance to move the contollers' relative position
    void Awake()
    {
        StartTracking(); //setup the tracking parameters.
    }

    // Use this for initialization
    void Start()
    {
        trackedGlove.OnGloveLoaded += TrackedGlove_OnGloveLoaded;
        trackedGlove.OnCalibrationFinished += TrackedGlove_OnCalibrationFinished; ;
        
        //remove the preview models, if any are available
        if (this.preview != null) { Destroy(this.preview); }
    }

    private void TrackedGlove_OnCalibrationFinished(object source, CalibrationArgs args)
    {
        SenseGlove_Debugger.Log("Resizing Model.");
        this.dJoints = args.jointPositions[1] - oldMCPPosition;
        this.shouldResize = true;
        //ShouldRescale is called because we cannot Get() transforms outside of the main thread.
    }

    private void TrackedGlove_OnGloveLoaded(object source, System.EventArgs args)
    {
        SenseGlove_Debugger.Log("Setting up WireFrame...");
        SetupGlove(trackedGlove.GloveData());
        SetupHand(trackedGlove.GloveData());
        SetGlove(false);    //hide the glove by default.
        SetHand(true);      //show the hand by default.
        SetupGrabColliders();
        this.setupComplete = true;
    }

    // Update is called once per frame
    void Update()
    {
        //Rescaling - done here because it requires access to the children of the glovepositions, which cannot be done outside the main thread.
        if (this.shouldResize && this.trackedGlove != null)
        {
            this.RescaleHand(this.trackedGlove.GetFingerLengths());
            this.handGroup.transform.localPosition = this.handGroup.transform.localPosition - dJoints;
            this.gloveGroup.transform.localPosition = this.gloveGroup.transform.localPosition - dJoints;
            this.oldMCPPosition = this.trackedGlove.GloveData().handPositions[1][0];
            this.shouldResize = false;
        }

        if (this.setupComplete) { this.setupComplete = false; } //reset the setupCOmplete "Event" back to zero.

        //update the glove.
        if (trackedGlove != null && trackedGlove.GloveReady())
        {
            if (!wristCalibrated)
            {
                if (calibrationTime < calibrationTimer)
                {
                    calibrationTime += Time.deltaTime;
                }
                else
                {
                    this.CalibrateWrist();
                    //Show the hand & lower arm only after the wrist has been calibrated.
                    int activate = (anchor != AnchorPoint.Wrist) ? 2 : 1;
                    for (int i = 0; i < activate && i < this.transform.childCount; i++)
                    {
                        this.transform.GetChild(i).gameObject.SetActive(true);
                    }
                    wristCalibrated = true;
                }
            }
            UpdateGlove(trackedGlove.GloveData());
            UpdateHand(trackedGlove.GloveData());
            UpdateWrist(trackedGlove.GloveData());
        }
    }

    // Used to position the WireFrame after the TrackedObject has been moved.
    void LateUpdate()
    {
        

        //Update tracking
        if (trackedObject != null)
        {

            if (anchor == AnchorPoint.ForeArm && foreArm != null)
            {
                //follow the object to the best of our ability
                this.foreArm.transform.rotation = trackedObject.transform.rotation * trackRotation;
                this.foreArm.transform.position = trackedObject.transform.position + (this.foreArm.transform.rotation * trackOffset);

                //have the other component follow
                if (wrist != null) //ensure the wrist mimics the lower arm.
                {
                    wrist.transform.localPosition = foreArm.transform.localPosition;
                }
            }
            else if (anchor == AnchorPoint.Wrist && wrist != null)
            {
                //follow the object to the best of our ability
                this.wrist.transform.rotation = trackedObject.transform.rotation * trackRotation;
                this.wrist.transform.position = trackedObject.transform.position + (this.wrist.transform.rotation * trackOffset);

                //have the other component follow
                if (foreArm != null) //ensure the wrist mimics the lower arm.
                {
                    foreArm.transform.localPosition = wrist.transform.localPosition;
                }
            }

        }

    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // Setup methods


    /// <summary> Create and distribute the glovePositions based on the GloveData received from the trackedGlove. </summary>
    /// <param name="data"></param>
    private void SetupGlove(SenseGlove_Data data)
    {
        if (gloveGroup != null && gloveBase != null && data != null && !this.setupComplete)
        {
            gloveBase.SetActive(false);
            gloveGroup.SetActive(true);

            if (data.dataLoaded)
            {
                this.gloveGroup.transform.localPosition = data.commonOriginPos;
                this.gloveGroup.transform.localRotation = data.commonOriginRot;

                Vector3[][] glovePos = data.glovePositions;
                Vector3[][] gloveLengths = data.gloveLengths;
                glovePositions = new GameObject[glovePos.Length][];
                for (int f = 0; f < glovePositions.Length; f++)
                {
                    glovePositions[f] = new GameObject[glovePos[f].Length];
                    for (int i = 0; i < glovePositions[f].Length; i++)
                    {
                        glovePositions[f][i] = GameObject.Instantiate(gloveBase, this.gloveGroup.gameObject.transform);
                        glovePositions[f][i].name = "GlovePostion" + f + "" + i;
                        glovePositions[f][i].transform.localRotation = new Quaternion();
                        if (i < glovePositions[f].Length - 1)
                        {
                            if (glovePositions[f][i].transform.childCount > 2)
                            {
                                Transform dX = glovePositions[f][i].transform.GetChild(2);
                                Transform dY = glovePositions[f][i].transform.GetChild(1);
                                Transform dZ = glovePositions[f][i].transform.GetChild(0);
                                
                                //Setup correct sizes.
                                if (gloveLengths[f][i][x] != 0) { dX.localScale = new Vector3(dX.localScale.x, gloveLengths[f][i][x] / 2.0f, dX.localScale.z); }
                                else { dX.gameObject.SetActive(false); }
                                if (gloveLengths[f][i][y] != 0) { dY.localScale = new Vector3(dX.localScale.x, gloveLengths[f][i][y] / 2.0f, dX.localScale.z); }
                                else { dY.gameObject.SetActive(false); }
                                if (gloveLengths[f][i][z] != 0) { dZ.localScale = new Vector3(dX.localScale.x, gloveLengths[f][i][z] / 2.0f, dX.localScale.z); }
                                else { dZ.gameObject.SetActive(false); }

                                //set correct positions based on ZYX?
                                dY.localPosition = new Vector3(0, gloveLengths[f][i][y] / 2.0f, 0);
                                dX.localPosition = new Vector3(gloveLengths[f][i][x] / 2.0f, gloveLengths[f][i][y], 0);
                                //dY ?
                            }
                        }
                        else
                        {
                            for (int j = 0; j < glovePositions[f][i].transform.childCount - 1; j++)
                            {
                                glovePositions[f][i].transform.GetChild(j).gameObject.SetActive(false);
                            }
                        }
                        glovePositions[f][i].SetActive(true);
                    }
                }
            }
            else
            {
                SenseGlove_Debugger.Log("ERROR : No Glove Data was found...");
            }
        }
        else
        {
            SenseGlove_Debugger.Log("WARNING : No base model for Glove Wireframe");
        }
    }

    /// <summary> Create and distribute the handPositions based on the GloveData received from the trackedGlove. </summary>
    /// <param name="data"></param>
    private void SetupHand(SenseGlove_Data data)
    {
        if (data != null && !this.setupComplete)
        {
            this.oldMCPPosition = data.handPositions[1][0]; //store the last mcp position
            if (handBase != null && handGroup != null)
            {
                for (int i = 1; i < handGroup.transform.childCount; i++)
                {
                    handGroup.transform.GetChild(i).gameObject.SetActive(true); //activate the palm model.
                    if (i == 1 && !data.isRight)
                    {
                        handGroup.transform.GetChild(i).gameObject.name = "Palm (L)";
                        Vector3 pos = handGroup.transform.GetChild(i).localPosition;
                        handGroup.transform.GetChild(i).localPosition = new Vector3(pos.x, pos.y, -pos.z); //invert Z if its a left hand.
                    }
                }

                if (data.dataLoaded)
                {
                    this.handGroup.transform.localPosition = data.commonOriginPos;
                    this.handGroup.transform.localRotation = data.commonOriginRot;

                    Vector3[][] handPos = data.handPositions;
                    Vector3[][] handLengths = data.handLengths;
                    handPositions = new GameObject[handPos.Length][];
                    for (int f = 0; f < handPositions.Length; f++)
                    {
                        handPositions[f] = new GameObject[handPos[f].Length];
                        for (int i = 0; i < handPositions[f].Length; i++)
                        {
                            handPositions[f][i] = GameObject.Instantiate(handBase, this.handGroup.gameObject.transform);
                            handPositions[f][i].name = "HandPostion" + f + "" + i;
                            handPositions[f][i].transform.localRotation = new Quaternion();
                            if (i < handPositions[f].Length - 1)
                            {
                                if (handPositions[f][i].transform.childCount > 2)
                                {
                                    Transform dX = handPositions[f][i].transform.GetChild(2);
                                    Transform dY = handPositions[f][i].transform.GetChild(1);
                                    Transform dZ = handPositions[f][i].transform.GetChild(0);

                                    //Setup correct sizes.
                                    if (handLengths[f][i][x] != 0) { dX.localScale = new Vector3(dX.localScale.x, handLengths[f][i][x] / 2.0f, dX.localScale.z); }
                                    else { dX.gameObject.SetActive(false); }

                                    if (handLengths[f][i][y] != 0) { dY.localScale = new Vector3(dX.localScale.x, handLengths[f][i][y] / 2.0f, dX.localScale.z); }
                                    else { dY.gameObject.SetActive(false); }

                                    if (handLengths[f][i][z] != 0) { dZ.localScale = new Vector3(dX.localScale.x, handLengths[f][i][z] / 2.0f, dX.localScale.z); }
                                    else { dZ.gameObject.SetActive(false); }

                                    dX.localPosition = new Vector3(handLengths[f][i][x] / 2.0f, 0, 0);
                                    //dY.localPosition = new Vector3(handLengths[f][i][x], handLengths[f][i][y] / 2.0f, 0);
                                    dY.gameObject.SetActive(false);
                                    //set correct positions based on ZYX?
                                    //dY.localPosition = new Vector3(0, handLengths[f][i][y] / 2.0f, 0);

                                    //dZ ?
                                    if (rigidFingers && i == handPositions[f].Length - 2)
                                    {
                                        Debug.Log("Added a collider and rb to " + f + "." + i);
                                        Collider C = dX.gameObject.AddComponent<CapsuleCollider>();
                                        C.isTrigger = false;

                                        Rigidbody RB = dX.gameObject.AddComponent<Rigidbody>();
                                        RB.isKinematic = true;
                                        RB.useGravity = false;
                                    }
                                }
                            }
                            else
                            {
                                for (int j = 0; j < handPositions[f][i].transform.childCount - 1; j++)
                                {
                                    handPositions[f][i].transform.GetChild(j).gameObject.SetActive(false);
                                }
                            }
                            handPositions[f][i].SetActive(true);
                        }
                    }
                }
                else
                {
                    SenseGlove_Debugger.Log("ERROR : No Hand Data was found...");
                }
            }
            else
            {
                SenseGlove_Debugger.Log("WARNING : No base model for Hand Wireframe");
            }
        }
        else
        {
            SenseGlove_Debugger.Log("WARNING : GloveData is null?");
        }



    }

    /// <summary> Initiate the Tracking between this WireFrame and the trackedObject in their current position </summary>
    private void StartTracking()
    {
        if (trackedObject != null)
        {
            GameObject followObject = null; //Could have used Transform, but Wrist or ForeArm could be null.
            if (anchor == AnchorPoint.ForeArm)
            {
                followObject = this.foreArm;
            }
            else if (anchor == AnchorPoint.Wrist)
            {
                followObject = this.wrist;
            }

            if (followObject != null)
            {
                trackOffset = Quaternion.Inverse(trackedObject.transform.rotation) * (followObject.transform.position - trackedObject.transform.position);
                //SenseGlove_Debugger.Log("ForeArm = " + SenseGlove_Util.ToString(followObject.transform.position) + 
                //    ". TrackedObject = " + SenseGlove_Util.ToString(trackedObject.transform.position)
                //    + ". Diff = " + SenseGlove_Util.ToString(anchorPosition) );

                trackRotation = Quaternion.Inverse(trackedObject.transform.rotation) * followObject.transform.rotation;
                //SenseGlove_Debugger.Log("ForeArm = " + SenseGlove_Util.ToString(followObject.transform.rotation.eulerAngles) +
                //    ". TrackedObject = " + SenseGlove_Util.ToString(trackedObject.transform.rotation.eulerAngles)
                //    + ". Diff = " + SenseGlove_Util.ToString(anchorRotation.eulerAngles));
            }

        }
    }

    /// <summary>
    /// Attach the Wireframe model, in its current position, to another GameObject
    /// </summary>
    /// <param name="obj"></param>
    public void StartTracking(GameObject obj)
    {
        this.trackedObject = obj;
        this.StartTracking();
    }


    /// <summary>  If this WireFrame model has a grabscript, attach the appropriate colliders. </summary>
    private void SetupGrabColliders()
    {
        if (!this.setupComplete)
        {
            SenseGlove_PhysGrab grabscript = this.GetComponent<SenseGlove_PhysGrab>();
            if (grabscript != null)
            {
                //attack capsule colliders to the fingers
                SenseGlove_Debugger.Log("Grabscript detected! Attaching colliders.");
                Collider[] tipColliders = new Collider[handPositions.Length];

                for (int f = 0; f < handPositions.Length; f++) //DEBUG : Only thumb & index
                {
                    GameObject fingerTip = this.handPositions[f][handPositions[f].Length - 1];
                    SphereCollider C = fingerTip.AddComponent<SphereCollider>();
                    C.radius = fingerTip.transform.FindChild("Point").localScale.x / 1f;
                    tipColliders[f] = C;
                }
                grabscript.SetupColliders(tipColliders, this.palmCollider);
            }
            //else try other forms of grabscripts
        }
    }

    /// <summary> Check whether or not this glove has completed its setup. </summary>
    /// <returns>True if this Wireframe's setup has been completed.</returns>
    public bool SetupComplete()
    {
        return this.setupComplete;
    }

    /// <summary>
    /// Rescale the hand lengths after the last Calibration step
    /// </summary>
    public void RescaleHand(float[][][] handLengths)
    {
        if (handLengths != null && this.trackedGlove != null)
        {
            for (int f = 0; f < handLengths.Length && f < handPositions.Length; f++)
            {
                for (int i = 0; i < handLengths[f].Length && i < handPositions[f].Length; i++)
                {
                    Transform dX = handPositions[f][i].transform.GetChild(2);
                    Transform dY = handPositions[f][i].transform.GetChild(1);
                    Transform dZ = handPositions[f][i].transform.GetChild(0);

                    //Setup correct sizes.
                    if (handLengths[f][i][x] != 0) { dX.localScale = new Vector3(dX.localScale.x, handLengths[f][i][x] / 2.0f, dX.localScale.z); }
                    else { dX.gameObject.SetActive(false); }

                    if (handLengths[f][i][z] != 0) { dY.localScale = new Vector3(dX.localScale.x, handLengths[f][i][z] / 2.0f, dX.localScale.z); }
                    else { dY.gameObject.SetActive(false); }

                    if (handLengths[f][i][y] != 0) { dZ.localScale = new Vector3(dX.localScale.x, handLengths[f][i][y] / 2.0f, dX.localScale.z); }
                    else { dZ.gameObject.SetActive(false); }

                    dX.localPosition = new Vector3(handLengths[f][i][x] / 2.0f, 0, 0);
                    //dY.localPosition = new Vector3(handLengths[f][i][x], handLengths[f][i][z] / 2.0f, 0);
                    dY.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Resize the hand using new fingerLengths, as retrieved from a FingerCalibrationFinished event.
    /// </summary>
    /// <param name="newLengths"></param>
    public void RescaleHand(float[][] newLengths)
    {
        if (newLengths != null && newLengths.Length > 4)
        {
            for (int f=0; f<newLengths.Length && f<this.handPositions.Length; f++)
            {
                for (int i=0; i<newLengths[f].Length && i<this.handPositions[f].Length; i++)
                {
                    Transform dX = handPositions[f][i].transform.GetChild(2);
                    if (newLengths[f][i] != 0) { dX.localScale = new Vector3(dX.localScale.x, newLengths[f][i] / 2.0f, dX.localScale.z); }
                    dX.localPosition = new Vector3(newLengths[f][i] / 2.0f, 0, 0);
                }
            }
        }
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Transform methods


    /// <summary> Update all glove positions based on the latest data taken from the trackedGlove, but only if the gloveGroup is active in the heirarchy. </summary>
    /// <param name="data"></param>
    private void UpdateGlove(SenseGlove_Data data)
    {
        if (glovePositions != null && gloveGroup.activeInHierarchy && data.dataLoaded)
        {
            Vector3[][] pos = data.glovePositions;
            Quaternion[][] rot = data.gloveRotations;
            for (int f = 0; f < glovePositions.Length && f < data.glovePositions.Length; f++)
            {
                for (int i = 0; i < glovePositions[f].Length && i < pos[f].Length; i++)
                {
                    glovePositions[f][i].transform.localPosition = pos[f][i];
                    glovePositions[f][i].transform.localRotation = rot[f][i];
                }
            }
        }
    }

    /// <summary> Update all hand positions based on the latest data taken from the trackedGlove, but only if the handGroup is active in the heirarchy </summary>
    /// <param name="data"></param>
    private void UpdateHand(SenseGlove_Data data)
    {
        if (handPositions != null && handGroup.activeInHierarchy && data.dataLoaded)
        {
            Vector3[][] pos = data.handPositions;
            Quaternion[][] rot = data.handRotations;
            for (int f = 0; f < handPositions.Length && f < data.handPositions.Length; f++)
            {
                for (int i = 0; i < handPositions[f].Length && i < pos[f].Length; i++)
                {
                    handPositions[f][i].transform.localPosition = pos[f][i];
                    handPositions[f][i].transform.localRotation = rot[f][i];
                }
            }

            Debug.DrawLine(handPositions[0][1].transform.position, handPositions[1][0].transform.position);

        }
    }

    /// <summary> 
    /// Update the Wrist with the latest data from the trackedGlove. 
    /// Includes conversion from Right- handed coordinate system to the Unity left handed coordinate system. 
    /// </summary>
    /// <param name="data"></param>
    private void UpdateWrist(SenseGlove_Data data)
    {
        if (wrist != null && data != null)
        {
            wrist.transform.rotation = data.absoluteCalibratedWrist;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    // Wrapper Functions

    /// <summary> Access the gloveReady event of the trackedGlove. </summary>
    /// <returns> True during the frame that the GloveReady is called. </returns>
    [Obsolete("No longer used since this can be achieved by a SenseGlove_Object.")]
    public bool GloveReady()
    {
        return trackedGlove != null && trackedGlove.GloveReady();
    }

    /// <summary> Align the wrist with the orientation of the foreArm. </summary>
    public void CalibrateWrist()
    {
        if (trackedGlove != null)
        {
            if (foreArm != null)
            {
                trackedGlove.CalibrateWrist(foreArm.transform.rotation);
            }
            else
            {
                trackedGlove.CalibrateWrist(Quaternion.identity);
            }
        }
    }

    /// <summary> Set the anchor position and -rotation of the Wireframe relative to the trackedObject. </summary>
    /// <param name="offset"></param>
    /// <param name="rotation"></param>
    public void SetAnchor(Vector3 offset, Quaternion rotation)
    {
        this.trackOffset = offset;
        this.trackRotation = rotation;
    }

    /// <summary>
    /// Access the offset between the trackedObject and this wireframe.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetTrackingOffset()
    {
        return this.trackOffset;
    }

    /// <summary>
    /// Access the difference in rotation between the trackedobject and this wireframe.
    /// </summary>
    /// <returns></returns>
    public Quaternion GetTrackingRotation()
    {
        return this.trackRotation;
    }

    //------------------------------------------------------------------------------------------------------------------------------------
    // Render methods

    /// <summary> Enable / Disable the drawing of the Glove. </summary>
    /// <param name="active"></param>
    public void SetGlove(bool active)
    {
        if (gloveGroup != null)
        {
            gloveGroup.SetActive(active);
        }
    }

    /// <summary> Enable / Disable the drawing of the Hand Model. </summary>
    /// <param name="active"></param>
    public void SetHand(bool active)
    {
        if (handGroup != null)
        {
            handGroup.SetActive(active);
        }
    }

}


/// <summary>
/// Determines where the TrackedObject should connect to.
/// </summary>
public enum AnchorPoint
{
    Wrist = 0,
    ForeArm
}