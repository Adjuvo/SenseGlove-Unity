using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Link to a Sense Glove Device. </summary>
    public class SG_DeviceLink : MonoBehaviour
    {
        protected SenseGloveCs.IODevice linkedDevice = null;

        public int DeviceIndex
        {
            get; protected set;
        }

        public virtual SenseGloveCs.IODevice GetInternalObject()
        {
            return linkedDevice;
        }

        public virtual bool IsConnected
        {
            get { return linkedDevice != null && linkedDevice.IsConnected(); }
        }

        public bool LinkDevice(SenseGloveCs.IODevice device, int index)
        {
            if (!this.IsLinked && this.CanLinkTo(device))
            {
                this.linkedDevice = device;
                this.DeviceIndex = index;
                this.SetupDevice();
                //Debug.Log("Linked " + this.name + " to I\\O Device " + device.DeviceID());
                return true;
            }
            return false;
        }

        public void UnlinkDevice()
        {
            if (this.IsLinked)
            {
                this.DisposeDevice();
                //if (this.linkedDevice != null)
                //    Debug.Log("Unlinked " + this.name + " from I\\O Device " + this.linkedDevice.DeviceID());
                this.linkedDevice = null;
                this.DeviceIndex = -1;
            }
        }


        public virtual bool IsLinked
        {
            get { return linkedDevice != null; }
        }

        protected virtual bool CanLinkTo(SenseGloveCs.IODevice device)
        {
            return device != null;
        }

        /// <summary> When linked, this function is run for first time setup. </summary>
        protected virtual void SetupDevice() { }

        /// <summary> Run when the device is unliked, a.k.a. when the DeviceList shuts down / during OnDestroy </summary>
        protected virtual void DisposeDevice() { }



        protected virtual void OnDestroy()
        {
            this.DisposeDevice();
        }
    }
}