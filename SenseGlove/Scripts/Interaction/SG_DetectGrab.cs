using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    /// <summary> Attach this to any GameObject with a collider to have SG_Grabscript(s) detect it, but not actually change anything. 
    /// Use this if you already have a Interaction System, but wish to use our Grab Detection. </summary>
    public class SG_DetectGrab : SG_Interactable
    {
        /// <summary> Let this script known it is being interacted with. </summary>
        /// <param name="grabScript"></param>
        /// <param name="fromExternal"></param>
        /// <returns></returns>
        protected override bool InteractionBegin(SG_GrabScript grabScript, bool fromExternal)
        {
            this._grabScript = grabScript;
            return true;
        }

        /// <summary> End the interaction of this script. </summary>
        /// <param name="grabScript"></param>
        /// <param name="fromExternal"></param>
        /// <returns></returns>
        protected override bool InteractionEnd(SG_GrabScript grabScript, bool fromExternal)
        {
            if (this._grabScript != null && GameObject.ReferenceEquals(grabScript.gameObject, _grabScript.gameObject))
            {
                this._grabScript = null; //grabScript reference can only be removed by the GrabScript that is currently holding it.
            }
            return true;
        }
    }
}