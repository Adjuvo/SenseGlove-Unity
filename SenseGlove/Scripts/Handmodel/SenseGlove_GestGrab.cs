using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_GestGrab : SenseGlove_GrabScript
{

    


    public override bool CanInteract()
    {
        throw new NotImplementedException();
    }

    public override GameObject[] CanPickup()
    {
        throw new NotImplementedException();
    }

    public override SenseGlove_Touch GetPalm()
    {
        return null; //there is no palm collider for this Gesture-Based Script.
    }

    public override bool IsGrabbing()
    {
        throw new NotImplementedException();
    }

    public override bool IsTouching()
    {
        throw new NotImplementedException();
    }

    public override void ManualRelease(float timeToReactivate = 1)
    {
        throw new NotImplementedException();
    }

    public override bool Setup()
    {
        throw new NotImplementedException();
    }
}
