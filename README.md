# SenseGlove-Unity
The latest stable Unity SDK for the SenseGlove, built using Unity 2017.4.30f1. Can be imported into Unity 2017.4 and up, which will automatically update the assets for you. Using older Unity versions may cause issues.

Current version is 1.2, released on the 22nd of April 2020.

## Upgrade Guide
When importing a new version of the Sense Glove SDK, the best practice is to delete the SenseGlove folder before re-importing, since some files may have been removed.

## Backwards Compatability
Starting from v0.20 and onwards, the SDK is no longer compatible out of the box with the old, white, lasercut prototypes (released before DK1). If you have such a device and wish to continue using it with the latest SDK, contact the Sense Glove team.

## Getting Started
The first thing one should do is ensure that their Sense Glove is working.

1.	Download / clone the latest version of the SenseGlove SDK.
2.	Import the unitypackage into your Unity project.
3.	Ensure your SenseGlove is connected to and recognized by your computer.
4.	Open the 00_HardWareDiagnostics scene in the Examples folder and press play. You should now see a virtual hand and glove moving.
5.  Verify that the glove model's movement matches that of its real-life counterpart, and test out some of the haptic functions. If it moves correctly, you're all set to go! If not, please contact the Sense Glove team.

## Before you build

The SenseGlove SDK relies on the `System.IO.Ports` assembly to communicate with the device. In order to successfully run your executable, you must change the `API Compatibility Level` from `.Net 2.0 Subset` to `.Net 2.0` in your project settings (Edit > Project Settings > Player).
