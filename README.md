# SenseGlove-Unity
The latest stable Unity SDK for the SenseGlove, built using Unity 2019.4.33f1. Can be imported into Unity 2019.4 and up, which will automatically update the assets for you. Using older Unity versions will cause issues.

Current version is v2.7.0, released on the 11th of September 2024.

**Important notice**: After 4-5 years of supporting Unity 2017 and 2018, the time has come for us to raise the minimum Unity version from 2017.4 to 2019.4 (LTS version). This update will allow us to make more use of the Unity XR system, and add vr-ready examples to the plugin. This means that your Unity 2017-2018 projects will no longer be able to receive updates to the SenseGlove Unity Plugin. If you'd like to continue using Unity 2017-2018, you can still use versions 1.0.0 - 2.3.1. SenseCom operates independently from the Unity Editor, and can still be used with our plugins of v2.0 and above.

Furthermore; you can find more extensive documentation of the Unity Plugin at [docs.senseglove.com/unity](https://senseglove.gitlab.io/SenseGloveDocs/unity/overview.html)

## Upgrade Guide
When importing a new version of the Sense Glove SDK, the best practice is to delete the SenseGlove folder before re-importing, since some files may have been removed or placed in a more suitable folder.

When upgrading to v2.4.0 and above, be sure to note the parameters you've used for your SG_Waveforms and SG_Material Scripts: This version puts those parameters into ScriptableObjects, rather than Monobehaviour scripts, and will clear the one's you've defined from the inspector. On the bright side, after this update you will be able to more easily edit materials and waveforms across the project, and port them between projects more easily as well.

When upgrading to v2.6.0, there are breaking changes to calibration and haptics. An [Upgrade Guide](https://senseglove.gitlab.io/SenseGloveDocs/unity/update-to-2-6.html) is available to guide you through the process up updating older demos to v2.6.0.

## Backwards Compatability
As with all unity packages, the SenseGlove Unity Plugin is compatible with the Unity version it is built, and any Unity Editor version released after. It is not recommended to use an earier version of the Unity Editor, as this may cause compiler errors and/or cause prefabs to go missing.

The SenseGlove Unity Plugin is compatible with both the SenseGlove DK1 - exoskeleton glove and the SenseGlove Nova - the soft glove.

The SenseCom software is compatible with plugin version 2.0 and above. When using older versions of the plugin, SenseCom should not be running in the background.

## Platform Compatability
The SenseGlove Unity API is compatible with Windows, with Linux support currently only working for the DK1 blue exoskeleton gloves. It is also compatible with Android devices, such as the Oculus Quest and Pico Neo 2.

## Getting Started
The first thing one should do is ensure that their Sense Glove is working.

1.	Download / clone the latest version of the SenseGlove SDK.
2.	Ensure your SenseGlove is connected to and recognized by your computer.
3.  Extract the relevant .zip file from the SenseCom folder, and run the program with the same name.
4.  A small UI should show up, indicating your connection states. If the hand is blue, you're good to go!
5.	Import the unitypackage into your Unity project.
6.	When using a Nova 1.0 or SenseGlove DK1: Open the 00_HardWareDiagnostics scene in the Examples folder and press play. When using a Nova 2.0, please open 14_Nova2_Diagnostics instead. You should now see a virtual hand and (in the case of the DK1, a glove) moving.
7.  Verify that the virtual hand is moving, and test out some of the haptic functions. If it moves correctly, you're all set to go! If not, please contact the Sense Glove team.
