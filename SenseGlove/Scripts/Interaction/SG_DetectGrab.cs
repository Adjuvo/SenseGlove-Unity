using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Attach this to any GameObject with a collider to have SG_Grabscripts detect it. Does not add any manipulation. </summary>
public class SG_DetectGrab : SG_Interactable
{
    protected override bool InteractionBegin(SG_GrabScript grabScript, bool fromExternal)
    {
        this._grabScript = grabScript;
        return true;
    }

    protected override bool InteractionEnd(SG_GrabScript grabScript, bool fromExternal)
    {
        if (this._grabScript != null && GameObject.ReferenceEquals(grabScript.gameObject, _grabScript.gameObject))
        {
            this._grabScript = null; //grabScript reference can only be removed by the GrabScript that is currently holding it.
        }
        return true;
    }
}
