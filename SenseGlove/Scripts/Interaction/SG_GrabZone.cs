using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Creates a zone that extends its SG_Interactable methods to other objects, essentially creating a handle for (multiple) other Interactables.</summary>
public class SG_GrabZone : SG_Interactable
{
    /// <summary> The Interactables that this Grabzone is connected to.  </summary>
    public List<SG_Interactable> connectedTo = new List<SG_Interactable>();

    //--------------------------------------------------------------------------------------------------------
    // Setup

    #region Setup

    //Before anything else, verify that the connections are valid. Saves us evaltuations later on.
    void Awake()
    {
        for (int i=0; i < connectedTo.Count;)
        {
            if (connectedTo[i] == null) { connectedTo.RemoveAt(i); }
            else { i++; }
        }
    }

    /// <summary> Connect a new Interactable to this GrabZone. Returns true if succesful.</summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool ConnectTo(SG_Interactable obj)
    {
        if (obj != null)
        {
            int index = this.ConnectionIndex(obj);
            if (index < 0)
            {   //new entry
                this.connectedTo.Add(obj);
                return true;
            }
        }
        return false;
    }

    /// <summary> Check if a SG_Interactable is already connected to this GrabZone. </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private int ConnectionIndex(SG_Interactable obj)
    {
        for (int i = 0; i < this.connectedTo.Count; i++)
        {
            if (GameObject.ReferenceEquals(this.connectedTo[i].gameObject, obj.gameObject))
            {
                return i;
            }
        }
        return -1;
    }

    #endregion Setup

    //--------------------------------------------------------------------------------------------------------
    // Methods

    #region ClassMethods

    /// <summary> Pass the BeginInteraction on to all connected SenseGlove_Interactables. </summary>
    /// <param name="grabScript"></param>
    protected override bool InteractionBegin(SG_GrabScript grabScript, bool fromExternal = false)
    {
        if (this.isInteractable)
        {
            for (int i = 0; i < this.connectedTo.Count; i++)
            {
                this.connectedTo[i].BeginInteraction(grabScript, true);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Pass the EndInteraction on to all connected SenseGlove_Interactables. 
    /// </summary>
    /// <param name="grabScript"></param>
    protected override bool InteractionEnd(SG_GrabScript grabScript, bool fromExternal = false)
    {
        if (this.isInteractable)
        {
            for (int i = 0; i < this.connectedTo.Count; i++)
            {
                this.connectedTo[i].EndInteraction(grabScript, true);
            }
            return true;
        }
        return false;
    }


    /// <summary>
    /// Pass the updateInteraction on to all connected SenseGlove_Interactables. 
    /// </summary>
    public override void UpdateInteraction()
    {
        if (this.isInteractable)
        {
            for (int i = 0; i < this.connectedTo.Count; i++)
            {
                this.connectedTo[i].UpdateInteraction();
            }
        }
    }

    /// <summary>
    /// Pass the ResetObject on to all connected SenseGlove_Interactables. 
    /// </summary>
    public override void ResetObject()
    {
        for (int i = 0; i < this.connectedTo.Count; i++)
        {
            this.connectedTo[i].ResetObject();
        }
    }

    /// <summary>
    /// Pass the SaveTransform function to all connected Interactables.
    /// </summary>
    public override void SaveTransform()
    {
        for (int i = 0; i < this.connectedTo.Count; i++)
        {
            this.connectedTo[i].SaveTransform();
        }
    }

    #endregion ClassMethods
    
}
