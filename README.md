# SenseGlove-Unity
The latest stable Unity SDK for the SenseGlove, built using Unity 2017.4.30f1. Can be imported into Unity 2017.4 and up, which will automatically update the assets for you. Using older Unity versions may cause issues.

Current version is v2.3.1, released on the 13th of June 2022.

## Upgrade Guide
When importing a new version of the Sense Glove SDK, the best practice is to delete the SenseGlove folder before re-importing, since some files may have been removed or placed in a more suitable folder.

## Backwards Compatability
Starting from v0.20 and onwards, the SDK is no longer compatible out of the box with the old, white, lasercut prototypes (released before DK1). If you have such a device and wish to continue using it with the latest SDK, contact the Sense Glove team.

## Platform Compatability
The SenseGlove Unity API is compatible with Windows, with Linux support coming soon. It is also compatible with Android devices, such as the Oculus Quest and Pico Neo 2.

## Getting Started
The first thing one should do is ensure that their Sense Glove is working.

1.	Download / clone the latest version of the SenseGlove SDK.
2.	Ensure your SenseGlove is connected to and recognized by your computer.
3.  Extract the relevant .zip file from the SenseCom folder, and run the program with the same name.
4.  A small UI should show up, indicating your connection states. If the hand is blue, you're good to go!
5.	Import the unitypackage into your Unity project.
6.	Open the 00_HardWareDiagnostics scene in the Examples folder and press play. You should now see a virtual hand and glove moving.
7.  Verify that the glove model's movement matches that of its real-life counterpart, and test out some of the haptic functions. If it moves correctly, you're all set to go! If not, please contact the Sense Glove team.
