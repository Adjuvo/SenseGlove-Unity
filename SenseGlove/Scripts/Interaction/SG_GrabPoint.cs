using UnityEngine;

namespace SG
{
    /// <summary> An extension of an SG_Grabable -with its own grab / snap behaviours. Allows you to add 'nested' behaviours, like Handles.  </summary>
    public class SG_GrabPoint : SG_Interactable
    {
        //-----------------------------------------------------------------------------------------------------------------------------
        // Member Variables

        /// <summary> This GrabPoint will count towards this Grabable Script. It must be linked or this script will break. </summary>
        [Header("GrabPoint Options")]
        public SG_Interactable linkedToScript = null;
        
        /// <summary> Optional snap behaviour when you grab this object. </summary>
        public SG_SnapOptions snapOptions;


        //-----------------------------------------------------------------------------------------------------------------------------
        // Interactable Overrides

        /// <summary> Ensure that Snap Options are in place. </summary>
        protected override void SetupScript()
        {
            if (this.linkedToScript != this)
            {
                this.linkedToScript.Setup(); //enure this one is setup first
                this.baseTransform = this.linkedToScript.baseTransform;
                this.physicsBody = this.linkedToScript.physicsBody;
                if (this.snapOptions == null)
                {
                    this.snapOptions = this.GetComponent<SG_SnapOptions>();
                }
                //link
                this.linkedToScript.LinkGrabPoint(this);
            }
            else
            {
                Debug.LogError("ERROR: Cannot link a SnapPoint to itself!");
            }
        }

        /// <summary> If the linkedScritp controls hand behaviour, so do we. </summary>
        /// <returns></returns>
        public override bool ControlsHandLocation()
        {
            return this.linkedToScript.ControlsHandLocation();
        }

        /// <summary> Retrieve my hand location from the linked Grabable Script. </summary>
        /// <param name="handRealPose"></param>
        /// <param name="connectedScript"></param>
        /// <param name="wristPosition"></param>
        /// <param name="wristRotation"></param>
        public override void GetHandLocation(SG_HandPose handRealPose, SG_GrabScript connectedScript, out Vector3 wristPosition, out Quaternion wristRotation)
        {
            this.linkedToScript.GetHandLocation(handRealPose, connectedScript, out wristPosition, out wristRotation);
        }

        /// <summary> Generate new Scrab Args, with optional snapping. </summary>
        /// <param name="grabScript"></param>
        /// <returns></returns>
        public override GrabArguments GenerateGrabArgs(SG_GrabScript grabScript)
        {
            //If we can snap, calculate a new "ref at snap"?
            if (this.snapOptions != null)
            {
                GrabArguments res;
                if (this.snapOptions.GenerateGrabArgs(this.linkedToScript.MyTransform, grabScript, out res)) //since were passing the aguments ot the liked script, and I myself don't do anything with it.
                {
                    return res;
                }
            }
            //no snapping, so just go.
            return linkedToScript.GenerateGrabArgs(grabScript);
        }

        /// <summary> Fires when we deteremined a grab is possible, but before adding to this list. </summary>
        /// <param name="grabScript"></param>
        /// <param name="grabArgs"></param>
        /// <returns></returns>
        protected override bool StartGrab(SG_GrabScript grabScript, out GrabArguments grabArgs)
        {
            if (this.linkedToScript != null && this.linkedToScript.enabled)
            {
                bool successGrab = base.StartGrab(grabScript, out grabArgs);
                if (successGrab)
                {
                    //  Debug.Log("Grabbed " + this.name + ", attempting to add myself to " + this.linkedToScript);
                    this.linkedToScript.TryGrab(grabArgs);
                }
                return successGrab;
            }
            grabArgs = null;
            return false;
        }

        /// <summary> Fires when a release happens, but before we remove it form the list. </summary>
        /// <param name="grabbedScript"></param>
        /// <returns></returns>
        protected override bool StartRelease(GrabArguments grabbedScript)
        {
            bool successRelease = base.StartRelease(grabbedScript);
            if (successRelease)
            {
              //  Debug.Log("Grabbed " + this.name + ", attempting to add release from " + this.linkedToScript);
                this.linkedToScript.TryRelease(grabbedScript);
            }
            return successRelease;
        }


        /// <summary> I do not update my own location, but the location of my linked script. SG_Interactable will check if it's already doing that. </summary>
        /// <param name="dT"></param>
        protected override void UpdateLocation(float dT)
        {
            if (this.linkedToScript != null)
            {
                //Debug.Log("Attempting to update " + this.linkedToScript.name + " via " + this.name);
                this.linkedToScript.UpdateInteractable(); //Automatically checks if already update (for example, by another hand).
            }
        }

        



    }
}