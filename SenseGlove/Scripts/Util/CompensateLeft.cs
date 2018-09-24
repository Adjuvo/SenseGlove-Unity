using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> A script needed to compensate for the mirroring of the left hand </summary>
/// <remarks> TODO: Have a separate, decent model for the left hand. </remarks>
public class CompensateLeft : MonoBehaviour
{
    /// <summary> The negatively scaled object that this object follows. </summary>
    public GameObject grabReference;
	
    public void AttachToRef()
    {
        if (this.grabReference != null)
        {
            this.transform.position = this.grabReference.transform.position;
            this.transform.rotation = this.grabReference.transform.rotation;
        }
    }

	// Update is called once per frame
	void Update ()
    {
        AttachToRef();
	}

    void LateUpdate()
    {
        AttachToRef();
    }

    void FixedUpdate()
    {
        AttachToRef();
    }

}
