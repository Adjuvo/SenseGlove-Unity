using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SG_XR_ListDevices : MonoBehaviour
{
    public float updateTime = 1.0f;

    
    public UnityEngine.UI.Text textElement;

    private bool updating = false;
    private Coroutine updateRoutine = null;

    public string DeviceText
    {
        get { return textElement != null ? textElement.text : ""; }
        set { if (textElement != null) { textElement.text = value; } }
    }

    public static string CollectDeviceText()
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

        report += "\n";

        SGCore.HapticGlove[] allGloves = SGCore.HapticGlove.GetHapticGloves(false);
        report += "\nFound " + allGloves.Length + " Haptic Glove(s)";
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
        return report;
    }

    private IEnumerator UpdateDeviceList()
    {
        updating = true;
        while (updating)
        {
            DeviceText = CollectDeviceText();
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
