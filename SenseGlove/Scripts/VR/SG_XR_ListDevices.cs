using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SG_XR_ListDevices : MonoBehaviour
{
    public float updateTime = 1.0f;
    public SG.SG_HapticGlove leftGlove, rightGlove;
    
    public TextMesh textElement;

    private bool updating = false;
    private Coroutine updateRoutine = null;

    public string DeviceText
    {
        get { return textElement != null ? textElement.text : ""; }
        set { if (textElement != null) { textElement.text = value; } }
    }



    public static string CollectDeviceText()
    {
        return CollectDeviceText(null, null);
    }
    public static string CollectDeviceText(SG.SG_HapticGlove right, SG.SG_HapticGlove left)
    {
        List<UnityEngine.XR.InputDevice> devices = SG.SG_XR_Devices.GetDevices();
        string report = Time.timeSinceLevelLoad.ToString(); 
        report += "\nUnityEngine.XR.InputDevices.GetDevices has found " + devices.Count + " device(s)";
        if (devices.Count == 0)
        {
            report += ".\nSenseGlove is therefore unable to determine what offsets to use...";
        }
        else
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].name.Length > 0)
                {
                    report += "\nName: \"" + devices[i].name + "\", Manufacturer: \"" + devices[i].manufacturer + "\", Characteristics: \"" + devices[i].characteristics.ToString() + "\"";
                }
            }
        }


#if UNITY_ANDROID

        string pString = "\nN\\A"; ;
        int pairedCount = 0;
        if (SG.Util.SG_IAndroid.Andr_GetPairedDevices(out string paired))
        {
            string[] addresses = paired.Split('\n');
            pString = "";
            for (int i=0; i< addresses.Length; i++)
            {
                if (addresses[i].Length > 0)
                {
                    pString += "\n" + addresses[i];
                    pairedCount++;
                }
            }
            report += "\n\nPaired Devices: (" + pairedCount.ToString() + ")" + pString;
        }
        else
        {
            report += "\n\nPaired Devices:\nN\\A"; //Not available
        }

        report += "\n";

        SGCore.HapticGlove[] allGloves = SGCore.HapticGlove.GetHapticGloves(true);
        if (pairedCount != allGloves.Length) //we have a different amount of gloves compared to our paired ones...
        {
            report += "Connection States:";
            if (SG.Util.SG_IAndroid.Andr_GetConnectionStates(out string states))
            {
                string[] state = states.Split('|');
                for (int i = 0; i < state.Length; i++)
                {
                    SGCore.Util.ConnectionStatus cState = SGCore.Util.ConnectionStatus.Parse(state[i]);
                    report += "\n" + ( cState.IsConnected ? "Connected" : "Disconnected")
                        + "/ State: " + cState.LastConnectionCode.ToString();
                    if (cState.LastExitCode != SGCore.Util.SC_ExitCode.E_UNKNOW)
                    {
                        report += " / Exit Code: " + cState.LastExitCode.ToString();
                    }

                    //report += "\nConnected: " + cState.IsConnected.ToString() + " / Test Code: " + cState.LastTestState + " / Connection Code: " + cState.LastConnectionCode + " / Exit Code: " + cState.LastExitCode;
                }
            }
            else
            {
                report += "\nN\\A";
            }
        }
        report += "\n\nDetected " + allGloves.Length + " Haptic Glove(s)";
        if (allGloves.Length == 0)
        {
            report += ". Make sure they are turned on!";
        }
        else
        {
            for (int i = 0; i < allGloves.Length; i++)
            {
                report += "\nName: \"" + allGloves[i].GetDeviceID() + "\" : " + (allGloves[i].IsConnected() ? "Connected" : "Not Connected");
            }
        }

#else
        report += "\n";
        SGCore.HapticGlove[] allGloves = SGCore.HapticGlove.GetHapticGloves(true);
        report += "\nDetected " + allGloves.Length + " Haptic Glove(s)";
        if (allGloves.Length == 0)
        {
            report += ". Make sure they are turned on!";
        }
        else
        {
            for (int i = 0; i < allGloves.Length; i++)
            {
                report += "\nName: \"" + allGloves[i].GetDeviceID() + "\" : " + (allGloves[i].IsConnected() ? "Connected" : "Not Connected");
            }
        }
#endif

        report += "\n";
        //report += "\nTracking offsets set to:\t" + SG_Core.Settings.TrackingHardwareName;

        SGCore.PosTrackingHardware leftHw, rightHw;
        SG.SG_XR_Devices.GetTrackingHardware(true, out rightHw);
        SG.SG_XR_Devices.GetTrackingHardware(false, out leftHw);
        report += "\nSG_XR_Devices detects:\tL = " + leftHw.ToString() + ", R = " + rightHw.ToString();

        if (left != null || right != null)
        {
            string uLeft = TrackingString(left);
            string uRight = TrackingString(right);
            report += "\nHaptic Gloves:\t L = " + uLeft + ", R = " + uRight;
        }

        return report;
    }

    private static string TrackingString(SG.SG_HapticGlove glove)
    {
        if (glove != null)
        {
            return glove.wristTrackingMethod.ToString() + "/" + glove.wristTrackingOffsets.ToString();
        }
        return "N\\A";
    }

    private IEnumerator UpdateDeviceList()
    {
        updating = true;
        while (updating)
        {
            DeviceText = CollectDeviceText(this.leftGlove, this.rightGlove);
            yield return new WaitForSeconds(updateTime);
        }
    }

    private void OnEnable()
    {
        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
        }
        updateRoutine = StartCoroutine( UpdateDeviceList() );
    }

    private void OnDisable()
    {
        updating = false;
        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
        }
        updateRoutine = null;
    }
}
