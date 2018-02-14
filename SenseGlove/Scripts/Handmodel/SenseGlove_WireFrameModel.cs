using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_WireFrameModel : SenseGlove_HandModel
{
    [Header("Wireframe Components")]

    [Tooltip("")]
    public GameObject gloveBase;
    [Tooltip("")]
    public GameObject gloveSectionModel;

    [Tooltip("")]
    public GameObject handBase;
    [Tooltip("")]
    public GameObject phalangeModel;

    [Tooltip("")]
    public GameObject handPalmModel;

    [Tooltip("")]
    public GameObject previewGroup;

    /// <summary> Do not run the setups more than once. </summary>
    private bool setupComplete = false;

    /// <summary> To apply glove angles to. </summary>
    private Transform[][] gloveJoints;



    protected override void CollectFingerJoints()
    {
        //not needed as this is done on StartUp.
    }

    private void SetupGlove(SenseGlove_Data gloveData)
    {
        if (!this.setupComplete && gloveData != null)
        {
            gloveBase.SetActive(true);
            gloveSectionModel.SetActive(false);

            int x = 0, y = 1, z = 2;

            if (gloveData.dataLoaded)
            {
                this.gloveBase.transform.localPosition = gloveData.commonOriginPos;
                this.gloveBase.transform.localRotation = gloveData.commonOriginRot;

                Vector3[][] glovePos = gloveData.glovePositions;
                Vector3[][] gloveLengths = gloveData.gloveLengths;
                this.gloveJoints = new Transform[glovePos.Length][];
                for (int f = 0; f < gloveJoints.Length; f++)
                {
                    gloveJoints[f] = new Transform[glovePos[f].Length];
                    for (int i = 0; i < gloveJoints[f].Length; i++)
                    {
                        GameObject gloveJoint = GameObject.Instantiate(gloveSectionModel, this.gloveBase.gameObject.transform);
                        gloveJoint.name = "GlovePostion" + f + "" + i;
                        gloveJoint.transform.localRotation = new Quaternion();
                        gloveJoint.transform.localPosition = glovePos[f][0];
                        if (i < gloveJoints[f].Length - 1)
                        {
                            if (gloveJoint.transform.childCount > 2)
                            {
                                Transform dX = gloveJoint.transform.GetChild(2);
                                Transform dY = gloveJoint.transform.GetChild(1);
                                Transform dZ = gloveJoint.transform.GetChild(0);

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
                            for (int j = 0; j < gloveJoint.transform.childCount - 1; j++)
                            {
                                gloveJoint.transform.GetChild(j).gameObject.SetActive(false);
                            }
                        }
                        gloveJoint.SetActive(true);
                        gloveJoints[f][i] = gloveJoint.transform;

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

    private void SetupFingers(SenseGlove_Data gloveData)
    {
        if (!this.setupComplete && gloveData != null)
        {
            int x = 0;
            if (phalangeModel != null && handBase != null)
            {
                if (gloveData.dataLoaded)
                {
                    this.handBase.transform.localPosition = gloveData.commonOriginPos;
                    this.handBase.transform.localRotation = gloveData.commonOriginRot;

                    Vector3[][] handPos = gloveData.handPositions;
                    Vector3[][] handLengths = gloveData.handLengths;

                    this.fingerJoints = new List<List<Transform>>();

                    for (int f = 0; f < handPos.Length; f++)
                    {
                        List<Transform> joints = new List<Transform>();
                        
                        for (int i = 0; i < handPos[f].Length; i++)
                        {
                            GameObject handPosition = GameObject.Instantiate(phalangeModel, this.handBase.transform);
                            handPosition.name = "HandPostion" + f + "" + i;
                            handPosition.transform.localRotation = new Quaternion();
                            if (i < handPos[f].Length - 1)
                            {
                                if (handPosition.transform.childCount > 0)
                                {
                                    Transform dX = handPosition.transform.GetChild(0);
                                    //Setup correct sizes.
                                    if (handLengths[f][i][x] != 0) { dX.localScale = new Vector3(dX.localScale.x, handLengths[f][i][x] / 2.0f, dX.localScale.z); }
                                    else { dX.gameObject.SetActive(false); }
                                    dX.localPosition = new Vector3(handLengths[f][i][x] / 2.0f, 0, 0);
                                }
                            }
                            else
                            {
                                Transform dX = handPosition.transform.GetChild(0);
                                dX.gameObject.SetActive(false);
                            }
                            handPosition.SetActive(true);
                            joints.Add(handPosition.transform);
                        }

                        this.fingerJoints.Add(joints);
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

    private void SetupHandPalm(bool right)
    {
        handPalmModel.SetActive(true); //activate the palm model.
        if (!right)
        {
            handPalmModel.gameObject.name = "Palm (L)";
            Vector3 pos = handPalmModel.transform.localPosition;
            handPalmModel.transform.localPosition = new Vector3(pos.x, pos.y, -pos.z); //invert Z if its a left hand.
        }
    }

    protected override void SenseGlove_OnGloveLoaded(object source, EventArgs args)
    {
        //setup the glove model.
        if (this.previewGroup != null) { Destroy(this.previewGroup); }

        SenseGlove_Data data = this.senseGlove.GloveData();

        this.SetupFingers(data);
        this.SetupGlove(data);
        this.SetupHandPalm(data.isRight);

        this.setupComplete = true;
        this.resizeFingers = true;
        base.SenseGlove_OnGloveLoaded(source, args);
    }



    public override void UpdateHand(SenseGlove_Data data)
    {
        if (data.dataLoaded)
        {
            //Update the glove
            {
                Quaternion[][] gloveAngles = data.gloveRotations;
                Vector3[][] glovePositions = data.glovePositions;
                for (int f = 0; f < this.gloveJoints.Length; f++)
                {
                    for (int j = 0; j < this.gloveJoints[f].Length; j++)
                    {
                        this.gloveJoints[f][j].localPosition = glovePositions[f][j];
                        this.gloveJoints[f][j].rotation = this.wristTransfrom.rotation * (gloveAngles[f][j] /* * this.fingerCorrection[f][j]*/);
                    }
                }
            }

            //Update the Hand
            {
                Quaternion[][] handAngles = data.handRotations;
                Vector3[][] handPositions = data.handPositions;
                for (int f = 0; f < this.fingerJoints.Count; f++)
                {
                    for (int j = 0; j < this.fingerJoints[f].Count; j++)
                    {
                        this.fingerJoints[f][j].localPosition = handPositions[f][j];
                        this.fingerJoints[f][j].rotation = this.wristTransfrom.rotation * (handAngles[f][j] * this.fingerCorrection[f][j]);
                    }
                }
            }
        }

        
    }

    /// <summary> Resizes the (white) cylinders that connect to the hand.  </summary>
    /// <param name="newLengths"></param>
    public override void ResizeHand(float[][] newLengths)
    {
        if (this.setupComplete && newLengths != null && newLengths.Length > 4)
        {
            for (int f = 0; f < newLengths.Length && f < this.fingerJoints.Count; f++)
            {
                for (int i = 0; i < newLengths[f].Length && i < this.fingerJoints[f].Count; i++)
                {
                    Transform dX = fingerJoints[f][i].GetChild(0);
                    if (newLengths[f][i] != 0) { dX.localScale = new Vector3(dX.localScale.x, newLengths[f][i] / 2.0f, dX.localScale.z); }
                    dX.localPosition = new Vector3(newLengths[f][i] / 2.0f, 0, 0);
                }
            }
        }
    }

    protected override void SenseGlove_OnCalibrationFinished(object source, CalibrationArgs args)
    {
        //resize so that the index finger position remains on the same place
        Vector3 dIndex = args.newJointPositions[1] - args.oldJointPositions[1];
        this.handBase.transform.localPosition = this.handBase.transform.localPosition - dIndex;
        this.gloveBase.transform.localPosition = this.gloveBase.transform.localPosition - dIndex;

        base.SenseGlove_OnCalibrationFinished(source, args);
    }


    //------------------------------------------------------------------------------------------------------------------------------------
    // Render methods

    /// <summary> Enable / Disable the drawing of the Glove. </summary>
    /// <param name="active"></param>
    public void SetGlove(bool active)
    {
        if (gloveBase != null)
        {
            gloveBase.SetActive(active);
        }
    }

    /// <summary> Enable / Disable the drawing of the Hand Model. </summary>
    /// <param name="active"></param>
    public void SetHand(bool active)
    {
        if (handBase != null)
        {
            handBase.SetActive(active);
        }
        if (this.handPalmModel != null)
        {
            this.handPalmModel.gameObject.SetActive(active);
        }
    }


}
