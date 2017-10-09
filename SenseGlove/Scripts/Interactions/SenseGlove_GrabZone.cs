using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Acts as wrapper for Handles or other simple UI elements. Basically triggers the Begin, Follow and End interaction on external objects.</summary>
public class SenseGlove_GrabZone : SenseGlove_Interactable
{

    /// <summary> The Interactables that this Grabzone is connected to.  </summary>
    public List<SenseGlove_Interactable> connectedTo = new List<SenseGlove_Interactable>();

    //Before anything else, verify that the connections are valid. Saves us evaltuations later on.
    void Awake()
    {
        for (int i=0; i < connectedTo.Count;)
        {
            if (connectedTo[i] == null) { connectedTo.RemoveAt(i); }
            else { i++; }
        }
    }

    /// <summary> Pass the BeginInteraction on to all connected SenseGlove_Interactables. </summary>
    /// <param name="grabScript"></param>
    public override void BeginInteraction(SenseGlove_GrabScript grabScript)
    {
        for (int i = 0; i < this.connectedTo.Count; i++)
        {
            this.connectedTo[i].BeginInteraction(grabScript);
        }
    }

    /// <summary>
    /// Pass the EndInteraction on to all connected SenseGlove_Interactables. 
    /// </summary>
    /// <param name="grabScript"></param>
    public override void EndInteraction(SenseGlove_GrabScript grabScript)
    {
        for (int i = 0; i < this.connectedTo.Count; i++)
        {
            this.connectedTo[i].EndInteraction(grabScript);
        }
    }


    /// <summary>
    /// Pass the updateInteraction on to all connected SenseGlove_Interactables. 
    /// </summary>
    public override void UpdateInteraction()
    {
        for (int i = 0; i < this.connectedTo.Count; i++)
        {
            this.connectedTo[i].UpdateInteraction();
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

    /// <summary> Connect a new Interactable to this GrabZone. </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool ConnectTo(SenseGlove_Interactable obj)
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

    /// <summary> Check if a SenseGlove_Interactable is already connected to this GrabZone. </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private int ConnectionIndex(SenseGlove_Interactable obj)
    {
        for (int i=0; i<this.connectedTo.Count; i++)
        {
            if (GameObject.ReferenceEquals(this.connectedTo[i].gameObject, obj.gameObject))
            {
                return i;
            }
        }
        return -1;
    }

}
