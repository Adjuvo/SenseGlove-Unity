using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SenseGloveCs;

/// <summary> Arguments for the GloveDetected Event. </summary>
public class GloveDetectedArgs : System.EventArgs
{
    /// <summary> The unique hardware ID of the detected glove. </summary>
    public string DeviceID { get; private set; }

    /// <summary> The index of the detected glove within the SenseGlove_DeviceManager memory. </summary>
    public int DeviceIndex { get; private set; }

    /// <summary> True if this is a righ handed Sense Glove, false if it is a left hand. </summary>
    public bool RightHanded { get; private set; }

    /// <summary> Create a new instance of the GloveDetectedArgs. </summary>
    /// <param name="glove"></param>
    public GloveDetectedArgs( SenseGlove glove, int gloveIndex )
    {
        this.DeviceID = glove.GetData(false).deviceID;
        this.RightHanded = glove.IsRight();
        this.DeviceIndex = gloveIndex;
    }
}


/// <summary> 
/// The SenseGlove_DeviceManager is responsible for detecting new Sense Glove Connections, 
/// and disposing of them once the application exits. 
/// These connections are then linked to SenseGlove_Objects within the scene, 
/// based on their connection parameters.
/// </summary>
public sealed class SenseGlove_DeviceManager : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------
    // Properties

    /// <summary> Sense Glove connections will be assigned to these SenseGlove_Objects, in this order, though dependent on their connection parameters. </summary>
    [SerializeField] private List<SenseGlove_Object> senseGloves = new List<SenseGlove_Object>();

    /// <summary> A list of newly detected Sense Gloves, which are gathered during the DeviceScanner thread. </summary>
    private List<SenseGlove> queuedGloves = new List<SenseGlove>();


    
    /// <summary> Once detected, a Sense Glove will be assigned to this list. </summary>
    private static List<SenseGlove> detectedGloves = new List<SenseGlove>();

    /// <summary> Whether or not the detectedGloves have been linked. </summary>
    private static List<bool> gloveLinked = new List<bool>();

    //--------------------------------------------------------------------------------------------------------
    // Accessors
    
    public static string ReportConnections()
    {
        string msg = "";
        string[] reports = SenseGloveCs.DeviceScanner.Instance.ReportConnections();
        if (reports.Length > 0)
        {
            msg = reports[0];
            for (int i = 1; i < reports.Length; i++)
                msg += "\r\n" + reports[i];
        }
        else
            msg = "Plug in at least one device";

        return msg;
    }


    //--------------------------------------------------------------------------------------------------------
    // Events

    /// <summary> Event delegate for the GloveDetected event. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    public delegate void GloveDetectedEventHandler(object source, GloveDetectedArgs args);

    /// <summary> Fires when a new Sense Glove connects to the system. </summary>
    public event GloveDetectedEventHandler GloveDetected;

    /// <summary> Used to call the GloveDetected event. </summary>
    private void OnGloveDetected(SenseGlove glove, int gloveIndex)
    {
        if (GloveDetected != null)
        {
            GloveDetected(this, new GloveDetectedArgs(glove, gloveIndex));
        }
    }


    //--------------------------------------------------------------------------------------------------------
    // Methods

    /// <summary> Fired when the  </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void DeviceScanner_SenseGloveDetected(object source, SenseGloveCs.IO.DeviceArgs args)
    {
        this.queuedGloves.Add((SenseGlove)args.Device);
    }


    /// <summary> Checks for any new events that have come in from the SenseGloveCs library. </summary>
    /// <remarks> Events are queued and fired here so that they are Unity-Safe: They could be fired from asynchronous worker threads, 
    /// and certain Unity functions are only accessible from this main thread. </remarks>
    private void CheckEventQueue()
    {
        //New gloves detected since the last Update
        if (this.queuedGloves.Count > 0)
        {
            for (int i=0; i<this.queuedGloves.Count; i++)
            {
                SenseGlove detectedGlove = this.queuedGloves[i]; //keep a refrence here, since the RemoveAt may make it go out of scope.
                Debug.Log("Found a new unattended Glove: " + detectedGlove.DeviceID());
                this.AssignNewGlove( detectedGlove );
            }
            this.queuedGloves.Clear(); //clear after we're done(?)
        }
    }

    /// <summary> Attempt to link a new glove to one of our assigned SenseGlove_Objects. Fire a GloveDetected event afterwards.? </summary>
    /// <param name="glove"></param>
    private void AssignNewGlove(SenseGlove glove)
    {
        if ( GetGloveIndex(glove.DeviceID()) < 0 ) //It is a new glove
        {
            SenseGlove_DeviceManager.detectedGloves.Add(glove);
            SenseGlove_DeviceManager.gloveLinked.Add(false);
            
            int index = detectedGloves.Count - 1;
            bool linked = false;
            bool isRight = glove.IsRight();

            //attempt to assign it to existing SenseGlove_Objects?
            for (int i=0; i<this.senseGloves.Count; i++)
            {
                if (this.senseGloves[i] != null)
                {
                    if (!this.senseGloves[i].IsLinked && SenseGlove_Object.MatchesConnection(isRight, senseGloves[i].connectionMethod))
                    {   //This Sense Glove is elligible for a connection and is not already connected.
                        bool succesfullLink = this.senseGloves[i].LinkToGlove(index);
                        if (succesfullLink)
                        {   //only when it is actually assigned do we break.
                            SenseGlove_DeviceManager.gloveLinked[index] = true; //we linked the glove at Index.
                            linked = true;
                            break;
                        }
                    }
                }
                else
                {   //Warn devs when their SenseGlove_Objects have not been assigned.
                    Debug.LogError("NullRefrence exception occured in " + this.name + ". You likely haven't assigned it via the inspector.");
                }
            }

            if (!linked)
                this.OnGloveDetected(glove, index); //only fire event if the glove was not assigned.
        }
        else
        {
            Debug.LogWarning("GloveDetected event was fired twice for the same device. Most likely you have two instances of DeviceManager running. " 
                + "We reccommend removing duplicate instances.");
        }
    }


    /// <summary> Clean up DeviceManager resources. </summary>
    private void Cleanup()
    {
        SenseGloveCs.DeviceScanner.Instance.SenseGloveDetected -= DeviceScanner_SenseGloveDetected;

        if (DeviceScanner.IsScanning)
            SenseGloveCs.DeviceScanner.StopScanning();

        this.queuedGloves.Clear(); //Stop recieving new gloves
        
        foreach (SenseGlove_Object glove in this.senseGloves)
        {
            glove.UnlinkGlove(); //stop all forms of feedback before ending.
        }
        this.senseGloves.Clear();

        foreach (SenseGlove glove in SenseGlove_DeviceManager.detectedGloves)
        {
            glove.StopFeedback(); //stop all forms of feedback before ending.
            glove.Disconnect();
        }
        SenseGlove_DeviceManager.detectedGloves.Clear();

        SenseGloveCs.DeviceScanner.CleanUp(); //Explicityly cleans up deviceScanner, since Unity does call finalizers / destructors.
    }

    /// <summary> Add a SenseGlove_Object to this Device Manager's watch list, which automatically connects a new Sense Glove if one is detected. </summary>
    /// <param name="obj"></param>
    public void AddToWatchList(SenseGlove_Object obj)
    {
        if (obj != null)
        {
            //todo: check if it exists
            for (int i=0; i<this.senseGloves.Count; i++)
            {
                if (System.Object.ReferenceEquals(this.senseGloves[i], obj))
                    return; //it is already in the list
            }
            //if we go here, it is not in the list.
            this.senseGloves.Add(obj);
        }
    }

    // Static Methods

    /// <summary> Retrieve the index of a glove with a specific deviceID within the detectedGloves array. </summary>
    /// <param name="deviceID"></param>
    /// <returns></returns>
    private static int GetGloveIndex(string deviceID)
    {
        for (int i=0; i<detectedGloves.Count; i++)
        {
            if (detectedGloves[i].DeviceID().Equals(deviceID))
                return i;
        }
        return -1;
    }

    /// <summary> Retrieve an internal Sense Glove object. </summary>
    /// <param name="index"></param>
    /// <returns>Returns null if the Index is invalid.</returns>
    public static SenseGlove GetSenseGlove(int index)
    {
        if (index > -1 && index < SenseGlove_DeviceManager.detectedGloves.Count)
        {
            return SenseGlove_DeviceManager.detectedGloves[index];
        }
        return null;
    }

    /// <summary> Link the SenseGlove_Object to a sense glove in DeviceManager Memory. </summary>
    /// <param name="obj"></param>
    /// <param name="gloveIndex"></param>
    /// <remarks> Possible duplicate of the SenseGlove_Object.LinkToGlove, though this one is accessible through a static method.  v</remarks>
    public static void LinkObject(SenseGlove_Object obj, int gloveIndex)
    {
        if (obj != null)
            obj.LinkToGlove(gloveIndex);
    }


    //--------------------------------------------------------------------------------------------------------
    // Monobehaviour

    // Use this for initialization
    void Start()
    {
        SenseGloveCs.DeviceScanner.ResponseTime = 500;
        SenseGloveCs.DeviceScanner.ScanInterval = 500;
        SenseGloveCs.DeviceScanner.Instance.SenseGloveDetected += DeviceScanner_SenseGloveDetected;
        SenseGloveCs.DeviceScanner.StartScanning(2.0f); //The input of .NET version 2.0 tells the DeviceScanner to use the 'old' notation for COM Ports.
    }

    // Update is called once per frame
    void Update ()
    {
        this.CheckEventQueue();	
	}

    // Called when this object is destroyed
    void OnDestroy()
    {
        this.Cleanup();
    }

    // Redundant call
    void OnApplicationQuit()
    {
        this.Cleanup();
    }

}
