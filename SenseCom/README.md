# SenseCom
SenseCom is a program (or process) that is responsible for detecting and communicating with SenseGlove Devices. When running SenseCom, the connection status of each SenseGlove device will be visible to the user. This is to ensure all hardware has been properly connected before starting a program.

## Installation
Extract the .zip file somewhere you can remember, such as in "D:\SenseGlove\" or "MyDocuments\SenseGlove". You should be able to run SenseCom.exe without any problems. However, if you're not able to connect to your glove(s), close the program and check sessionLog.txt in MyDocument\SenseGlove. If it reports a missing dll, your system is missing a Visual C++ extension, which you can download using the VC_redist.x64.exe inside the .zip file, or by downloading it from https://aka.ms/vs/16/release/vc_redist.x64.exe. 

The Windows version of SenseCom also has an install wizard available in this folder. It will also (attempt to) install the Visual C++ extension required to run SenseCom.

## Documentation

Guides on how to use the SenseCom software can be found [on our documentation page](https://senseglove.gitlab.io/SenseGloveDocs/sensecom/overview.html).

## SenseCom Git

SenseCom has [its own GitHub page](https://github.com/Adjuvo/SenseCom) now. You can use it to install the latest releases, as well as post any issues you may have.