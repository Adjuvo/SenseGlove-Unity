using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A static object that cannot be interacted with, but still gives force Feedback. </summary>
[RequireComponent(typeof(Collider))]
public class SenseGlove_StaticObject : SenseGlove_Interactable 
{

    void LateUpdate()
    {
        this.forceFeedback = true;
        this.isInteractable = false;
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    // Unused class methods.

    public override void BeginInteraction(SenseGlove_GrabScript grabScript)
    {
    }

    public override void EndInteraction(SenseGlove_GrabScript grabScript)
    {
    }

    public override void ResetObject()
    {
    }

    public override void UpdateInteraction()
    {
    }

}
