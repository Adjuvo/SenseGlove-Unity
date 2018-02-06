using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Door : SenseGlove_Hinge
{
    //--------------------------------------------------------------------------------------------
    // Attributes

    #region Attributes
        

    #endregion Attributes

    //--------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour
        

    #endregion Monobehaviour
        

    //--------------------------------------------------------------------------------------------
    // Door Methods

    #region DoorMethods
        



    #endregion DoorMethods

    //--------------------------------------------------------------------------------------------
    // Door Events

    #region DoorEvents

    //DoorClosed
    public delegate void DoorClosedEventHandler(object source, EventArgs args);
    /// <summary> Fires the door returns to its initial position. </summary>
    public event DoorClosedEventHandler DoorClosed;
    /// <summary> Raise the DoorClosed event </summary>
    protected void OnDoorClosed()
    {
        if (DoorClosed != null)
        {
            DoorClosed(this, null);
        }
    }


    //DoorOpened
    public delegate void DoorOpenedEventHandler(object source, EventArgs args);
    /// <summary> Fires the Door returns to its maxLimit position? </summary>
    public event DoorOpenedEventHandler DoorOpened;
    /// <summary> Raise the DoorOpened Event </summary>
    protected void OnDoorOpened()
    {
        if (DoorOpened != null)
        {
            DoorOpened(this, null);
        }
    }

    #endregion DoorEvents

}
