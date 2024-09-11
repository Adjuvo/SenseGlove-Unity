/************************************************************************************
Filename    :   SG_SettingsEditor.cs
Content     :   Allows one to edit a ScriptableObject via a nice UI
Created     :   07/03/2023
Copyright   :   Copyright SenseGlove  All rights reserved.
Author      :   Max Lammers

Changes to this file may be lost when updating the SenseGlove Plugin
************************************************************************************/

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Reflection;
using SG;

public class SG_SettingsEditor : EditorWindow
{
    private static SG_SettingsEditor _window;
    private static SerializedObject _serializedSettings;

    /// <summary> "BlackList" for private variables </summary>
    private static string[] dontShowList = new string[]
    {
        "leftQuat",
        "customLeftQuat",
        "rightQuat",
        "customRightQuat",
        "lastWristHW",
    };

    /// <summary> Opening the window </summary>
    [MenuItem("SenseGlove/Settings")]
    private static void Init()
    {
        // Get existing open window or if none, make a new one:
        GetWindow();
    }

    private static void GetWindow()
    {
        _window = (SG_SettingsEditor)EditorWindow.GetWindow(typeof(SG_SettingsEditor), false, "SenseGlove Settings");
        _window.Show();
    }


    private bool OnBlackList(string name)
    {
        foreach (string bl in dontShowList)
        {
            if (bl.Equals(name))
            {
                return true;
            }
        }
        return false;
    }

    private void OnGUI()
    {
        //TODO: Render a proper menu.
        SG_UnitySettings sett = SG_Core.Settings;
        if (sett == null)
        {
            Debug.LogError("Could not draw Settings Box as the file is missing! Please re-import your SenseGlove plugin and try again");
            return;
        }

        // Proper Settings field
        _serializedSettings = new SerializedObject(sett);

        FieldInfo[] variables = typeof(SG_UnitySettings).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); //Retrieve all public, non-public, non-static variables 

        foreach (FieldInfo member in variables)
        {
            if (member.Name.Equals("SGCommunications"))
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty(member.Name), false);
                if (sett.SGCommunications == CommunicationSetup.Disabled)
                {
                    EditorGUILayout.HelpBox("This disables any kind of initialization! You will be responsible for Initializing / Disposing of SenseGlove Resources. Only use this if you know what you're doing", MessageType.Warning);
                }
            }
            else if (member.Name.Equals("WristTrackingMethod"))
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty(member.Name), false); //Draw as normal.
                if (sett.WristTrackingMethod == GlobalWristTracking.UnityXR)
                {
                    EditorGUILayout.HelpBox("You will need to have an SG_XR_SceneTrackingLinks component active in your Scene, and have at least the XR Rig assigned.", MessageType.Info);
                }
                else if (sett.WristTrackingMethod == GlobalWristTracking.UseGameObject)
                {
                    EditorGUILayout.HelpBox("You will need to have an SG_XR_SceneTrackingLinks component active in your Scene, and have the left- and right hand tracking device assigned." +
                        " Make sure these objects are also moving relative to your XR Rig.", MessageType.Info);
                }
            }
            else if (member.Name.Equals("GlobalWristTrackingOffsets"))
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty(member.Name), false);
                if (sett.GlobalWristTrackingOffsets == TrackingHardware.Unknown)
                {
                    EditorGUILayout.HelpBox("No offsets will be applied. You will be directly using the detected Tracked Device / GameObject location.", MessageType.Warning);
                }
                else if (sett.GlobalWristTrackingOffsets == TrackingHardware.AutoDetect)
                {
                    EditorGUILayout.HelpBox("SenseGlove will attempt to detect what Tracking Device you are using based on information available through Unity.XR.InputDevice." +
                        " Due to the vast amount of devices and XR packages through which to access them, we cannot guarantee this detection will work 100% of the time. When possible, please specify a specific device or hardware family.", MessageType.Info);
                }
                else if (sett.GlobalWristTrackingOffsets == TrackingHardware.Custom)
                {
                    EditorGUILayout.HelpBox("You've opted to use a custom tracking offset from whichever source you are using. Use the values below to use across your project.", MessageType.Info);
                }
            }
            else if (member.Name.Contains("customPos") || member.Name.Contains("customRot"))
            {
                if (sett.GlobalWristTrackingOffsets == TrackingHardware.Custom)
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty(member.Name), false);
            }
            else if (!OnBlackList(member.Name))
            {
                // Safety in case I change someting about my Settings and wonder why shiz is broken.
                if (_serializedSettings == null)
                {
                    Debug.LogError("_serializedSettings == null");
                }
                else if (member == null)
                {
                    Debug.LogError("member == null");
                }
                else if (_serializedSettings.FindProperty(member.Name) == null)
                {
                    Debug.LogError("_serializedSettings.FindProperty(" + member.Name + ") == null");
                }
                else
                {
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty(member.Name), false); //Draw as normal.
                }
            }
        }

        sett.RecalculateOffsets();

        //Update the settings of the SerializedObject
        if (_serializedSettings.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(sett);
        }

    }
}

#endif