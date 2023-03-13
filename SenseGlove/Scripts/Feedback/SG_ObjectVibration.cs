using SGCore.Haptics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Util
{

    /// <summary> Sends a Vibration to each hand that is holding on to an object. If not object is holding on to it, send it to the lost object to hold it instead.  </summary>
    public class SG_ObjectVibration : MonoBehaviour, IHandFeedbackDevice
    {
        //------------------------------------------------------------------------------------------------------------------------------
        // Member Variables


        /// <summary> The interactable to send the Vibration through. </summary>
        public SG_Interactable sendThroughObject;

        /// <summary> The waveform to be sent through the Interactable to the IHandFeedbackDevice. </summary>
        public SG_Waveform waveformToSend;

        public bool fallBackToLastGrabbed = true;

        //------------------------------------------------------------------------------------------------------------------------------
        // Send Through Object Functions

        public IHandFeedbackDevice[] DevicesLinkedToObject
        {
            get
            {
                List<GrabArguments> args = this.sendThroughObject.ScriptsGrabbingMe();
                if (args.Count > 0)
                {
                    IHandFeedbackDevice[] res = new IHandFeedbackDevice[args.Count];
                    for (int i=0; i<args.Count; i++)
                    {
                        res[i] = (IHandFeedbackDevice)args[i];
                    }
                    return res;
                }
                else if (this.fallBackToLastGrabbed && sendThroughObject.LastGrabbedBy != null)
                {
                    return new IHandFeedbackDevice[1] { (IHandFeedbackDevice) sendThroughObject.LastGrabbedBy }; //added an explicit case for redundancy's sake.
                }
                return new IHandFeedbackDevice[0];
            }
        }


        /// <summary> Utility method that can be called from anywhere. </summary>
        /// <param name="waveform"></param>
        /// <param name="interactable"></param>
        /// <param name="fallBackToLastGrab">If true, we fall back to the last GrabScript that was holding on to me. </param>
        public static void SendThroughObject(SG.SG_Waveform waveform, SG.SG_Interactable interactable, bool fallBackToLastGrab = true)
        {
            if (interactable.IsGrabbed())
            {
                interactable.SendCmd(waveform);
            }
            else if (fallBackToLastGrab && interactable.LastGrabbedBy != null)
            {
                interactable.LastGrabbedBy.SendCmd(waveform);
            }
        }


        /// <summary> Send the Waveform attached to this script to the object attached to this script. </summary>
        public void SendWaveForm()
        {
            SendThroughObject(this.waveformToSend, this.sendThroughObject, this.fallBackToLastGrabbed);
        }


        /// <summary> Send a custom waveform to the object linked tothis script. </summary>
        /// <param name="customWaveform"></param>
        /// <param name="fallBackToLastGrab"></param>
        public void SendWaveForm(SG_Waveform customWaveform)
        {
            SendThroughObject(customWaveform, this.sendThroughObject, this.fallBackToLastGrabbed);
        }




        //------------------------------------------------------------------------------------------------------------
        // IHandFeedbackDevice implementation


        public bool IsConnected()
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            return linkedDevices.Length > 0 ? linkedDevices[0].IsConnected() : false;
        }

        public string Name()
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            return linkedDevices.Length > 0 ? linkedDevices[0].Name() : "";
        }

        /// <summary> Sends a command to the object that is holding on to this object </summary>
        /// <param name="ffb"></param>
        public void SendCmd(SG_FFBCmd ffb)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i=0; i<linkedDevices.Length; i++)
            {
                linkedDevices[i].SendCmd(ffb);
            }
        }

        public void SendCmd(SG_TimedBuzzCmd fingerCmd)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SendCmd(fingerCmd);
            }
        }

        public void SendCmd(TimedThumpCmd wristCmd)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SendCmd(wristCmd);
            }
        }

        public void SendCmd(ThumperWaveForm waveform)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SendCmd(waveform);
            }
        }

        public void SendCmd(SG_Waveform waveform)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SendCmd(waveform);
            }
        }

        public void SendImpactVibration(SG_HandSection location, float normalizedVibration)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SendImpactVibration(location, normalizedVibration);
            }
        }



        public void StopAllVibrations()
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].StopAllVibrations();
            }
        }

        public void StopHaptics()
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].StopHaptics();
            }
        }

        public void SendCmd(SG_NovaWaveform customWaveform, SGCore.Nova.Nova_VibroMotor location)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SendCmd(customWaveform, location);
            }
        }


        public bool FlexionLockSupported()
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                if (linkedDevices[i].FlexionLockSupported())
                {
                    return true;
                }
            }
            return false;
        }

        public void SetFlexionLocks(bool[] fingers, float[] fingerFlexions)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                linkedDevices[i].SetFlexionLocks(fingers, fingerFlexions);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        private void Start()
        {
            if (this.sendThroughObject == null)
            {
                this.sendThroughObject = this.GetComponent<SG_Interactable>();
            }
            if (this.waveformToSend == null)
            {
                this.waveformToSend = this.GetComponent<SG_Waveform>();
            }
        }

        public bool TryGetBatteryLevel(out float value01)
        {
            IHandFeedbackDevice[] linkedDevices = this.DevicesLinkedToObject;
            for (int i = 0; i < linkedDevices.Length; i++)
            {
                if (linkedDevices[i].TryGetBatteryLevel(out value01))
                {
                    return true;
                }
            }
            value01 = -1.0f;
            return false;
        }
    }
}