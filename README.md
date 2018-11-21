# SenseGlove-Unity
The latest stable Unity SDK for the SenseGlove, built using Unity 2017.2.4f1.

Current version is 0.21, released on the 21st of November 2018.

When importing a new version of the Sense Glove SDK, the best practice is to delete the SenseGlove folder before re-importing, since some files may have been removed. If you don't wish to interfere with your scripts, delete only the Examples folder before importing.


## Backwards Compatability
Starting from v0.20 and onwards, the SDK is no longer compatible out of the box with the old, white, lasercut prototypes (released before DK1). If you have such a device and wish to continue using it with the latest SDK, contact the Sense Glove team.

## Getting Started
The first thing one should do is ensure that their Sense Glove is working.

1.	Download / clone the latest version of the SenseGlove SDK.
2.	Import the unitypackage into your Unity project.
3.	Ensure your Sense Glove is connected to and recognized by your computer.
4.	Open the diagnostics scene in the examples folder and press play. You should now see a virtual hand and glove moving.
5.  Verify that the glove model's movement matches that of its real-life counterpart. If it moves correctly, you're all set to go! If not, please contact the Sense Glove team.

## Before you build

The SenseGlove SDK relies on the `System.IO.Ports` assembly to communicate with the device. In order to successfully run your executable, you must change the `API Compatibility Level` from `.Net 2.0 Subset` to `.Net 2.0` in your project settings (Edit > Project Settings > Player).
