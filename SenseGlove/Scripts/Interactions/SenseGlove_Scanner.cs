using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SenseGlove_Scanner : SenseGlove_Grabable 
{

    protected bool wasUsed = false, wasReleased = true;

	// Update is called once per frame
	void Update () 
	{
	    if (this.IsGrabbed())
        {
            Vector3[] angles = this._grabScript.handModel.senseGlove.GloveData().handAngles[1]; //index finger angles
            //Debug.Log(SenseGlove_Util.ToString(angles[2]));

            if (wasReleased)
            {
                Debug.Log("The trigger was released");
                bool canUse = angles[2].z < -20;
                //check if 
                if (canUse)
                {
                    Debug.Log("The trigger should be pressed");
                    this.wasUsed = true;
                    this.wasReleased = false;
                    this.OnScannerUsed();
                }
            }
            else if (wasUsed)
            {
                Debug.Log("The trigger was pressed");
                bool canRelease = angles[2].z > -10;
                if (canRelease)
                {
                    Debug.Log("The trigger should be released");
                    this.wasReleased = true;
                    this.wasUsed = false;
                    this.OnScannerUnUsed();
                }

            }
        }	
	}

    /// <summary> Check if the trigger of this scanner has been pressed. </summary>
    /// <returns></returns>
    public bool IsPressed()
    {
        return this.wasUsed;
    }


    //ScannerUsed - trigger is pulled
    public delegate void ScannerUsedEventHandler(object source, System.EventArgs args);
    /// <summary> Fires the Drawer returns to its initial position. </summary>
    public event ScannerUsedEventHandler ScannerUsed;

    protected void OnScannerUsed()
    {
        if (ScannerUsed != null)
        {
            Debug.Log("Used!");
            ScannerUsed(this, null);
        }
    }

    //ScannerUnUsed - trigger is released
    public delegate void ScannerUnUsedEventHandler(object source, System.EventArgs args);
    /// <summary> Fires the Drawer returns to its initial position. </summary>
    public event ScannerUnUsedEventHandler ScannerUnUsed;

    protected void OnScannerUnUsed()
    {
        if (ScannerUnUsed != null)
        {
            Debug.Log("UnUsed!");
            ScannerUnUsed(this, null);
        }
    }


}
