# SenseGlove-Unity
The latest stable Unity SDK for the SenseGlove.

Current version is 0.14, released on the 8th of September 2017.

## Getting Started
The first thing one should do is ensure that their Sense Glove is working.

1.	Download / clone the latest version of the SenseGlove SDK.
2.	Import the unitypackage into your Unity project.
3.	Ensure your Sense Glove is connected to and recognized by your computer.
4.	Open the diagnostics scene in the examples folder and press play. You should now see a virtual hand and glove moving.
5.  Verify that the glove model's movement matches that of its real-life counterpart. If it moves correctly, you're all set to go! If not, please contact the Sense Glove team.

## Before you build

The SenseGlove SDK relies on the `System.IO.Ports` assembly to communicate with the device. In order to successfully run your executable, you must change the `API Compatibility Level` from `.Net 2.0 Subset` to `.Net 2.0` in your project settings (Edit > Project Settings > Player).
