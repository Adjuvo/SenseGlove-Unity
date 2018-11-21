﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> An area that detects specific finger scripts. Can be used for a rudimentary gesture detection. </summary>
public class SenseGlove_FingerDetector : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------------------------------------------
    //  Properties

    #region Properties

    /// <summary> The SenseGlove_HandModel of which to track the fingers. Set to null to have it detect any feedback collider. </summary>
    /// <remarks>Avoids other hand from interfearing with each other.</remarks>
    [Tooltip("The SenseGlove_HandModel of which to track the fingers. Set to null to have it detect any feedback collider.")]
    public SenseGlove_HandModel modelToCheck;

    /// <summary> A highlight that can be turned on / off </summary>
    [Tooltip("A highlight object that can be turned on or off.")]
    public Renderer highLight;

    /// <summary> Keeps track of how many of each feedback colliders are within this object. </summary>
    private int[] finger = new int[5] { 0, 0, 0, 0, 0 };

    /// <summary> The Gameobjects to which the finger colliders of modelToCheck are connected. Used to speed up detection. </summary>
    private SenseGlove_Feedback[] fingerColliders = new SenseGlove_Feedback[0];

    /// <summary> boolean to run setup once after start (so that the modelToTrack has time to setup its colliders). </summary>
    private bool setup = false;

    #endregion Properties

    //-----------------------------------------------------------------------------------------------------------------------------------
    //  Class Methods

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start ()
    {
        //Set my colliders to istrigger
        Collider[]  myColliders = this.gameObject.GetComponents<Collider>();
        if (myColliders != null)
        {
            for (int i=0; i<myColliders.Length; i++)
            {
                myColliders[i].isTrigger = true;
            }
        }
	}

    protected virtual void Update()
    {
        //collect the colliders after startup, to make sure the handmodel has completed.
        if (!setup)
        {
            //collect my hand model's feedback colliders - speeds up collision detection
            if (modelToCheck != null)
            {
                fingerColliders = new SenseGlove_Feedback[modelToCheck.touchColliders.Count];
                for (int i = 0; i < fingerColliders.Length; i++)
                {
                    fingerColliders[i] = modelToCheck.touchColliders[i].GetComponent<SenseGlove_Feedback>();
                }
            }
            setup = true;
        }
    }




    /// <summary> A new (feedback) collider enters the detector. </summary>
    /// <param name="other"></param>
    protected virtual void OnTriggerEnter(Collider other)
    {
       // Debug.Log("Touche!");
        if (this.modelToCheck != null) //we have access specific GameObjects
        {
            int index = ColliderIndex(other.gameObject);
            //Debug.Log("Touching Index " + index);
            this.SetTouch(index, true);
        }
        else //no acces, so we need to check for scripts.
        {
            SenseGlove_Feedback script = other.GetComponent<SenseGlove_Feedback>();
            if (script != null)
            {
                this.SetTouch(script.GetIndex(), true);
            }
        }
    }

    /// <summary> A (feedback) collider exits the detector. </summary>
    /// <param name="other"></param>
    protected virtual void OnTriggerExit(Collider other)
    {
      //  Debug.Log("Letgo!");
        if (this.modelToCheck != null) //we have access specific GameObjects
        {
            int index = ColliderIndex(other.gameObject);
            //Debug.Log("Releasing Index " + index);
            this.SetTouch(index, false);
        }
        else //no acces, so we need to check for scripts.
        {
            SenseGlove_Feedback script = other.GetComponent<SenseGlove_Feedback>();
            if (script != null)
            {
                this.SetTouch(script.GetIndex(), false);
            }
        }
    }

    #endregion Monobehaviour

    //-----------------------------------------------------------------------------------------------------------------------------------
    //  Class Methods

    #region ClassMethods

    /// <summary> Check if the this detector is touched by a specific finger. </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool TouchedBy(int index)
    {
        if (index >= 0 && index < 5)
        {
            return this.finger[index] > 0;
        }
        return false;
    }

    /// <summary>
    /// Check if the this detector is touched by a specific finger.
    /// </summary>
    /// <param name="finger"></param>
    /// <returns></returns>
    public bool TouchedBy(SenseGloveCs.Finger finger)
    {
        if (finger != SenseGloveCs.Finger.All)
        {
            return this.finger[(int)finger] > 0;
        }
        else
        {
            for (int i = 0; i < this.finger.Length; i++)
            {
                if (!(this.finger[i] > 0)) { return false; }
            }
            return true;
        }
    }

    /// <summary> Set a particular finger to true/false. Catch the indices. </summary>
    /// <param name="index"></param>
    /// <param name="isTouched"></param>
    private void SetTouch(int index, bool isTouched)
    {
        if (index >= 0 & index < 5)
        {
            if (isTouched) { this.finger[index]++; }
            else
            {
                this.finger[index]--;
                if (this.finger[index] < 0) { this.finger[index] = 0; } //catch exception where we start within the collider
            }
        }
    }

    /// <summary> Retrieve the index of a collided gameobject </summary>
    /// <param name="collided"></param>
    /// <returns></returns>
    private int ColliderIndex(GameObject collided)
    {
        for (int f=0; f<this.fingerColliders.Length; f++)
        {
            if ( GameObject.ReferenceEquals(collided, fingerColliders[f].gameObject) )
            {
                return fingerColliders[f].GetIndex();
            }
        }
        return -1;
    }

    /// <summary>
    /// Change the highlight state of this detector
    /// </summary>
    /// <param name="active"></param>
    public void SetHighlight(bool active)
    {
        if (this.highLight != null)
        {
            this.highLight.enabled = active;
        }
    }

    #endregion ClassMethods

}
