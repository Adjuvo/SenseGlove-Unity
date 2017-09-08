using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_BasicHand : MonoBehaviour
{
    public SenseGlove_Object trackedGlove;

    public GameObject handBase;

    private Transform[][] handPositions;

    // Use this for initialization
    void Start()
    {
        this.DetectJoints(this.handBase);
        if (!this.trackedGlove)
        {
            this.trackedGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        }

        if (this.trackedGlove)
        {
            this.trackedGlove.OnGloveLoaded += TrackedGlove_OnGloveLoaded;
            this.trackedGlove.OnCalibrationFinished += TrackedGlove_OnCalibrationFinished;
        }

        if (this.GetComponent<SenseGlove_PhysGrab>() && this.handPositions != null)
        {
            this.SetupColliders();
        }
    }

    private void TrackedGlove_OnCalibrationFinished(object source, CalibrationArgs args)
    {
        this.RescaleHand(args.newFingerLengths);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.trackedGlove && this.trackedGlove.GloveReady())
        {
            this.UpdateHand(this.trackedGlove.GloveData());
        }
    }

    private void TrackedGlove_OnGloveLoaded(object source, System.EventArgs args)
    {
        this.RescaleHand(this.trackedGlove.GetFingerLengths());
    }

    public void DetectJoints(GameObject fingerBase)
    {
        handPositions = new Transform[5][];
        for (int f = 0; f < handPositions.Length && f < fingerBase.transform.childCount; f++)
        {
            List<Transform> joints = new List<Transform>();
            Transform temp = fingerBase.transform.GetChild(f);
            bool reachedLast = false;
            while (joints.Count < 4 && !reachedLast)
            {
                joints.Add(temp);
                if (temp.childCount > 0) { temp = temp.GetChild(0); }
                else { reachedLast = true; }
            }

            handPositions[f] = new Transform[joints.Count];
            for (int i = 0; i < joints.Count; i++)
            {
                handPositions[f][i] = joints[i];
            }
        }
    }

    public void SetupColliders()
    {
        try
        {
            Collider[] tipColliders = new Collider[this.handPositions.Length];
            for (int i = 0; i < handPositions.Length; i++)
            {
                tipColliders[i] = handPositions[i][handPositions[i].Length - 1].GetChild(0).GetComponent<Collider>();
            }
            this.GetComponent<SenseGlove_PhysGrab>().SetupColliders(tipColliders, this.handBase.transform.GetChild(5).GetComponent<Collider>());
        }
        catch (System.Exception ex)
        {
            SenseGlove_Debugger.Log("Could not set up colliders:");
            SenseGlove_Debugger.Log(ex.Message);
        }
    }

    public void RescaleHand(float[][] handLengths)
    {
        Debug.Log("Rescaling");
        if (this.handPositions != null && handLengths != null)
        {
            for (int f = 0; f < handPositions.Length && f < handLengths.Length; f++)
            {
                if (handPositions[f] != null && handLengths[f] != null)
                {
                    for (int i = 1; i < handPositions[f].Length && (i - 1) < handLengths[f].Length; i++)
                    {
                        handPositions[f][i].localPosition = new Vector3(handLengths[f][i - 1], 0, 0);
                        if (handPositions[f][i - 1].childCount > 1)
                        {
                            Vector3 scale = handPositions[f][i - 1].GetChild(1).localScale;
                            handPositions[f][i - 1].GetChild(1).localPosition = new Vector3(handLengths[f][i - 1] / 2.0f, 0, 0);
                            handPositions[f][i - 1].GetChild(1).localScale = new Vector3(scale.x, handLengths[f][i - 1] / 2.0f, scale.z);
                        }
                    }
                }
            }
        }
    }

    public void UpdateHand(SenseGlove_Data data)
    {
        if (this.handPositions != null)
        {
            for (int f = 0; f < handPositions.Length && f < data.handAngles.Length; f++)
            {
                if (handPositions[f] != null)
                {
                    for (int i = 0; i < handPositions[f].Length && i < data.handAngles[f].Length; i++)
                    {
                        if (handPositions[f][i] != null)
                        {
                            if (f == 0 && i == 0)
                            {
                                handPositions[f][i].localRotation = data.handRotations[0][1];
                            }
                            else
                            {
                                handPositions[f][i].localRotation = Quaternion.Euler(data.handAngles[f][i]);
                            }
                        }
                    }
                }
            }
        }
    }

}
