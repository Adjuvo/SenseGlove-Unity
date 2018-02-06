using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A hinge that can be flipped to activate events or logic </summary>
public class SenseGlove_Lever : SenseGlove_Hinge
{
    //--------------------------------------------------------------------------------------------
    // Attributes

    #region Attributes

    /// <summary>  </summary>
    public float leverRatio = 0;

    public bool hasRaisedUp = false;
    public bool hasRaisedDown = false;
    
    #endregion Attributes

    //--------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    private void Start()
    {
        this.leverRatio = this.GetLeverRatio();
        this.hasRaisedUp   = leverRatio < 0.1f ? true : false;
        this.hasRaisedDown = leverRatio > 0.9f ? true : false;
    }

    private void Update()
    {
        float oldLeverRatio = this.leverRatio;
        
        this.leverRatio = this.GetLeverRatio();

        if (oldLeverRatio > 0 && this.leverRatio <= 0 && !this.hasRaisedUp)
        {
            this.hasRaisedUp = true;
            this.OnLeverUp();
        }
        else if (this.hasRaisedUp && this.leverRatio > 0.1f)
        {
            this.hasRaisedUp = false;
        }

        if (oldLeverRatio < 1 && this.leverRatio >= 1 && !this.hasRaisedDown)
        {
            this.hasRaisedDown = true;
            this.OnLeverDown();
        }
        else if (this.hasRaisedDown && this.leverRatio < 0.9f)
        {
            this.hasRaisedDown = false;
        }

    }

    #endregion Monobehaviour


    //--------------------------------------------------------------------------------------------
    // Lever Methods

    #region LeverMethods

    /// <summary> Returns how far the Lever is from its minimum (0) and maximum (1) angle. Useful when applying a certain rotation </summary>
    /// <returns></returns>
    public float GetLeverRatio()
    {
        return this.GetLeverRatio(this.GetHingeAngle());
    }

    /// <summary> Returns how far the chosen angle is from its minimum (0) and maximum (1) angle. Useful when applying a certain rotation </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public float GetLeverRatio(float angle)
    {
        float res = SenseGloveCs.Values.Interpolate(angle, this.minAngle, this.maxAngle, 0, 1);
        return SenseGloveCs.Values.Wrap(res, 0, 1);

    }


    #endregion LeverMethods

    //--------------------------------------------------------------------------------------------
    // Lever Events

    #region LeverEvents

    //LeverUp
    public delegate void LeverUpEventHandler(object source, EventArgs args);
    /// <summary> Fires when the Lever reaches its maximum limit </summary>
    public event LeverUpEventHandler LeverUp;

    protected void OnLeverUp()
    {
        if (LeverUp != null)
        {
            Debug.Log("Lever is Up!");
            LeverUp(this, null);
        }
    }


    //LeverDown
    public delegate void LeverDownEventHandler(object source, EventArgs args);
    /// <summary> Fires when the Lever reaches its minimum limit </summary>
    public event LeverDownEventHandler LeverDown;

    protected void OnLeverDown()
    {
        if (LeverDown != null)
        {
            Debug.Log("Lever is Down!");
            LeverDown(this, null);
        }
    }

    #endregion LeverEvents



}
