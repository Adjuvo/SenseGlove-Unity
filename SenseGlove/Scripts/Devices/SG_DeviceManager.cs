using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SenseGloveCs;

/// <summary> Arguments for the GloveDetected Event. </summary>
public class DeviceDetectedArgs : System.EventArgs
{
    /// <summary> The unique hardware ID of the detected glove. </summary>
    public string DeviceID { get; private set; }

    /// <summary> The index of the detected glove within the SenseGlove_DeviceManager memory. </summary>
    public int DeviceIndex { get; private set; }

    /// <summary> The DeviceType that has been found </summary>
    public SenseGloveCs.DeviceType Type { get; private set; }

    /// <summary> Create a new instance of the GloveDetectedArgs. </summary>
    /// <param name="glove"></param>
    public DeviceDetectedArgs(string id, int gloveIndex, SenseGloveCs.DeviceType deviceType)
    {
        DeviceID = id;
        DeviceIndex = gloveIndex;
        Type = deviceType;
    }
}


public class SG_DeviceManager : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------
    // Properties

    /// <summary> Sense Glove related object to link. </summary>
    [SerializeField] private List<SG_DeviceLink> devicesToLink = new List<SG_DeviceLink>();

    protected int lastAvailable = 0;
    protected List<bool> linked = new List<bool>();
    protected int objectsLinked = 0;

    public bool debug = false;
    public KeyCode clearDevicesKey = KeyCode.None;

    //--------------------------------------------------------------------------------------------------------
    // Internal DeviceScanning Stuff

    /// <summary> Reports all internal connections for debugging purposes. </summary>
    /// <returns></returns>
    public static string ReportConnections()
    {
        string msg = "";
        string[] reports = SenseGloveCs.DeviceScanner.Instance.ReportConnections();
        if (reports.Length > 0)
        {
            msg = reports[0];
            for (int i = 1; i < reports.Length; i++)
            {
                msg += "\r\n" + reports[i];
            }
        }
        else
            msg = "Plug in at least one device";
        return msg;
    }

    protected void SetupScanner()
    {
        if (!SenseGloveCs.DeviceScanner.IsScanning)
        {
            Log("Setting up DeviceScanner Resources");
            SenseGloveCs.DeviceScanner.ResponseTime = 500;
            SenseGloveCs.DeviceScanner.StartScanning(2.0f); //tells system to used old COM ports.
        }
    }

    protected void DisposeScanner()
    {
        if (SenseGloveCs.DeviceScanner.Instance != null && SenseGloveCs.DeviceScanner.IsScanning)
        {
            Log("Disposing of DeviceScanner Resources");
            SenseGloveCs.DeviceScanner.StopScanning();
            SenseGloveCs.DeviceScanner.CleanUp();
        }
    }


    //--------------------------------------------------------------------------------------------------------
    // Class Methods

    protected void Log(string msg)
    {
        if (debug)
            Debug.Log("[Sense Glove]: " + msg);
    }


    /// <summary> Check if any new connections have come in, and should be linked. </summary>
    public void CheckConnections()
    {
        if (objectsLinked < devicesToLink.Count) //we've not linked all devices yet...
        {
            IODevice[] availableDevices = SenseGloveCs.DeviceScanner.AllDevices;
            for (int d = lastAvailable; d < availableDevices.Length; d++) //add linked = false to any new devices.
            {
                Log("Connected to " + availableDevices[d].DeviceID());
                linked.Add(false);
            }
            lastAvailable = availableDevices.Length;

            for (int i = 0; i < devicesToLink.Count; i++)
            {
                if (devicesToLink[i] != null && devicesToLink[i].gameObject.activeInHierarchy && !devicesToLink[i].IsLinked)
                {
                    for (int d = 0; d < availableDevices.Length; d++)
                    {
                        if (!linked[d] && availableDevices[d].IsConnected() 
                            && devicesToLink[i].LinkDevice(availableDevices[d], d))
                        {
                            objectsLinked++;
                            linked[d] = true;
                            break;//no need to check other IODevices.
                        }
                    }
                }
            }

        }
    }


    /// <summary>  </summary>
    /// <param name="link"></param>
    /// <remarks> Using Index as opposed to bool because it might be useful later on </remarks>
    /// <returns></returns>
    public int ListIndex(SG_DeviceLink link)
    {
        for (int i=0; i<devicesToLink.Count; i++)
        {
            if (GameObject.ReferenceEquals(link, devicesToLink[i]))
                return i;
        }
        return -1;
    }


    public void AddToWatchList(SG_DeviceLink link)
    {
        if (ListIndex(link) < 0) //using listIndex since we might use that function later on
            this.devicesToLink.Add(link);
    }

    /// <summary> Clear the current connections to devices so we can try again... </summary>
    public void ClearConnections()
    {
        SenseGloveCs.DeviceScanner.ClearConnections();
    }

    /// <summary> Unlink devices from the list so they can be connected again in other scenes. </summary>
    protected void UnlinkAll()
    {
        //Unlink devices so they can cease all haptic activity.
        for (int i = 0; i < devicesToLink.Count; i++)
        {
            if (devicesToLink[i] != null)
            {
                devicesToLink[i].UnlinkDevice();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------------
    // Monobehaviour

    protected void Start()
    {
        SetupScanner();
    }

    protected void Update()
    {
        CheckConnections();
        if (SG_Util.keyBindsEnabled && Input.GetKeyDown(clearDevicesKey))
            this.ClearConnections();
    }

    
    protected void OnDestroy()
    {
        UnlinkAll(); //done twice because OnApplicationQuit is called before OnDestroy().
        SG_SenseGloveHardware.deviceScannerPresent = false; //tell the hardwares we are no longer present
    }
    

    protected void OnApplicationQuit()
    {
        UnlinkAll();
        DisposeScanner();
    }

}
