using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyCorrectHand : MonoBehaviour
{
    public SenseGlove_Object leftGlove, rightGlove;

    private GameObject leftHandModel, rightHandModel;

    public KeyCode swapHandsKey = KeyCode.Return;
    private SolveType currSolv;

    public KeyCode switchSolver = KeyCode.S;
    private bool leftReady = false, rightReady = false;

	// Use this for initialization
	void Start ()
    {
        if (leftGlove != null)
        {
            leftGlove.gameObject.SetActive(true);
            leftHandModel = leftGlove.transform.GetChild(0).gameObject;
        }
        if (rightGlove != null)
        {
            rightGlove.gameObject.SetActive(true);
            rightHandModel = rightGlove.transform.GetChild(0).gameObject;
        }
        SetModels(false, false);
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (leftGlove != null)
        {
            if (leftGlove.GloveReady() && !leftReady)
            {
                this.leftReady = true;
                if (!rightReady)
                {
                    SetModels(true, false);
                }
            }
        }
        if (rightGlove != null)
        {
            if (rightGlove.GloveReady() && !rightReady)
            {
                this.rightReady = true;
                if (!leftReady)
                {
                    SetModels(false, true);
                }
            }
        }

        if (Input.GetKeyDown(swapHandsKey) && (leftReady || rightReady))
        {
            SetModels( !this.leftHandModel.activeInHierarchy, !this.rightHandModel.activeInHierarchy);
        }
        if (Input.GetKeyDown(switchSolver))
        {
            if (leftReady)
            {
                if (leftGlove.solver == SolveType.InterpolateAsIMU)
                {
                    leftGlove.solver = SolveType.Kinematic3D;
                }
                else if (leftGlove.solver == SolveType.Kinematic3D)
                {
                    leftGlove.solver = SolveType.InterpolateAsIMU;
                }
            }
            if (rightReady)
            {
                if (rightGlove.solver == SolveType.InterpolateAsIMU)
                {
                    rightGlove.solver = SolveType.Kinematic3D;
                }
                else if (rightGlove.solver == SolveType.Kinematic3D)
                {
                    rightGlove.solver = SolveType.InterpolateAsIMU;
                }
            }
        }
	}

    public void SetSolver(SolveType solv)
    {

    }

    public void SetModels(bool left, bool right)
    {
        if (leftHandModel != null)
        {
            leftHandModel.SetActive(left);
        }
        if (rightHandModel != null)
        {
            rightHandModel.SetActive(right);
        }
    }

}
