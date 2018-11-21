using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary>
/// Yet another utility class, this one keeps track of all your connected SenseGlove(s).
/// </summary>
public class SenseGlove_Manager : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------------------------
    // Private Variables

    /// <summary> List of all devices that are already connected to something. </summary>
    private static List<string> connectedIDs = new List<string>();

    //--------------------------------------------------------------------------------------------------------------------------------
    // Glove Management

    /// <summary> Tell the SenseGlove_Manager that this deviceID is now in use or no longer in use. </summary>
    /// <param name="deviceID"></param>
    /// <param name="inUse"
    public static void SetUsed(string deviceID, bool inUse)
    {
        if (SenseGlove_Manager.connectedIDs == null) { SenseGlove_Manager.connectedIDs = new List<string>(); }
        if (inUse) { SenseGlove_Manager.connectedIDs.Add(deviceID); } //this device is now in use
        else //remove this device from use.
        {
            int index = SenseGlove_Manager.UseIndex(deviceID);
            if (index > -1) { SenseGlove_Manager.connectedIDs.RemoveAt(index); }
        }
        
    }
    

    /// <summary> Check if a specific deviceID is already in use. </summary>
    /// <param name="deviceID"></param>
    /// <returns></returns>
    public static bool IsUsed(string deviceID)
    {
        return SenseGlove_Manager.UseIndex(deviceID) > -1;
    }

    /// <summary>
    /// Retrieve the index of this deviceID in the connectedIDs.
    /// </summary>
    /// <param name="deviceID"></param>
    /// <returns></returns>
    private static int UseIndex(string deviceID)
    {
        if (connectedIDs != null)
        {
            for (int i = 0; i < connectedIDs.Count; i++)
            {
                if (connectedIDs[i].Equals(deviceID, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    return i;
                }
            }
        }
        else { connectedIDs = new List<string>(); }
        return -1;
    }

    //--------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour (if added to the scene)
   
    public void Start()
    {
        if (!DeviceScanner.IsScanning)
        {
            SenseGloveCs.DeviceScanner.ResponseTime = 200;
            SenseGloveCs.DeviceScanner.ScanInterval = 500;
            DeviceScanner.StartScanning(2.0f);
        }
    }

    public void Update()
    {




    }

}
