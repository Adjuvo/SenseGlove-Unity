using UnityEngine;

/// <summary> A script to manage a set of Rigidbodies that represent the hand geometry. </summary>
public class SG_HandRigidBodies : MonoBehaviour
{
    /// <summary> The hand model information, used to assign tracking information. If left unassinged, you'll need to assing them manually. </summary>
    [Header("Linked Scripts")]
    public SG_HandModelInfo handModel;

    /// <summary> The managed rigidbody of the wrist </summary>
    [Header("RigidBody Components")]
    public SG_TrackedBody wristObj;
    /// <summary> The managed rigidbody of the fingers, from thumb to pinky. </summary>
    public SG_TrackedBody[] fingerObjs = new SG_TrackedBody[0];


    /// <summary> The TrackedHand this Animator takes its data from, used to access grabscript, hardware, etc. </summary>
    public SG_TrackedHand Hand
    {
        get; protected set;
    }

    /// <summary> Returns true if this Animator is connected to Hardware that is ready to go </summary>
    public virtual bool HardwareReady
    {
        get { return this.Hand != null && this.Hand.hardware != null && this.Hand.hardware.GloveReady; }
    }

    /// <summary> Returns true if this Animator is connected to Sense Glove Hardware. Used in an if statement for safety </summary>
    /// <param name="hardware"></param>
    /// <returns></returns>
    public virtual bool GetHardware(out SG_SenseGloveHardware hardware)
    {
        if (HardwareReady)
        {
            hardware = this.Hand.hardware;
            return hardware != null;
        }
        hardware = null;
        return false;
    }



    /// <summary> Show/Hide the the rigidbodies in this layer. </summary>
    public bool DebugEnabled
    {
        set
        {
            wristObj.DebugEnabled = value;
            for (int f = 0; f < fingerObjs.Length; f++)
            {
                fingerObjs[f].DebugEnabled = value;
            }
        }
    }

    /// <summary> Enable/Disable the overall collision of the rigidbodies in this layer. </summary>
    public bool CollisionsEnabled
    {
        set
        {
            wristObj.CollisionEnabled = value;
            for (int f = 0; f < fingerObjs.Length; f++)
            {
                fingerObjs[f].CollisionEnabled = value;
            }
        }
    }


    /// <summary> Assign scripts relevant to this script's functioning. </summary>
    protected virtual void CheckForScripts()
    {
        SG_Util.CheckForHandInfo(this.transform, ref this.handModel);
        if (this.Hand == null)
        {
            this.Hand = SG_Util.CheckForTrackedHand(this.transform);
        }
    }

    /// <summary> Setup the tracking / parameters of this script's components. </summary>
    protected void SetupSelf()
    {
        CheckForScripts();
        wristObj.SetTrackingTarget(handModel.wristTransform, true);
        for (int f = 0; f < fingerObjs.Length; f++)
        {
            Transform target;
            if (handModel.GetFingerTip((SG_HandSection)f, out target))
            {
                fingerObjs[f].SetTrackingTarget(target, true);
            }
        }
        this.SetIgnoreCollision(this, true); //ignore own colliders
    }





    /// <summary> Set ignoreCollision between this layer and another set of rigidbodies. </summary>
    /// <param name="otherLayer"></param>
    /// <param name="ignoreCollision"></param>
    public void SetIgnoreCollision(SG_HandRigidBodies otherLayer, bool ignoreCollision)
    {
        if (otherLayer != null)
        {
            GameObject wrist = otherLayer.wristObj != null ? otherLayer.wristObj.gameObject : null;
            SetIgnoreCollision(wrist, ignoreCollision);
            for (int f=0; f< otherLayer.fingerObjs.Length; f++)
            {
                SetIgnoreCollision(otherLayer.fingerObjs[f].gameObject, ignoreCollision);
            }
        }
    }

    /// <summary> Set the ignoreCollision between this layer and a specific gameobject </summary>
    /// <param name="obj"></param>
    /// <param name="ignoreCollision"></param>
    public void SetIgnoreCollision(GameObject obj, bool ignoreCollision)
    {
        Collider[] colliders = obj != null ? obj.GetComponents<Collider>() : new Collider[0];
        for (int i = 0; i < colliders.Length; i++)
        {
            this.SetIgnoreCollision(colliders[i], ignoreCollision);
        }
    }

    /// <summary> Set the ignoreCollision between this layer and a specific collider </summary>
    /// <param name="obj"></param>
    /// <param name="ignoreCollision"></param>
    public void SetIgnoreCollision(Collider col, bool ignoreCollision)
    {
        if (this.wristObj != null) { this.wristObj.SetIgnoreCollision(col, ignoreCollision); }
        for (int f = 0; f < fingerObjs.Length; f++)
        {
            if (fingerObjs[f] != null) { this.fingerObjs[f].SetIgnoreCollision(col, ignoreCollision); }
        }
    }



    /// <summary> Add Rigidbodies with proper parameters for this layer. </summary>
    /// <param name="useGrav"></param>
    /// <param name="kinematic"></param>
    public void AddRigidBodies(bool useGrav = false, bool kinematic = false)
    {
        this.wristObj.TryAddRB(useGrav, kinematic);
        for (int f = 0; f < this.fingerObjs.Length; f++)
        {
            fingerObjs[f].TryAddRB(useGrav, kinematic);
            fingerObjs[f].updateTime = SG_SimpleTracking.UpdateDuring.FixedUpdate;
        }
    }

    /// <summary> Removes rigidbodies from this layer, so their collision can become part of a different RigidBody. </summary>
    public void RemoveRigidBodies()
    {
        this.wristObj.TryRemoveRB();
        for (int f = 0; f < this.fingerObjs.Length; f++)
        {
            fingerObjs[f].TryRemoveRB();
            fingerObjs[f].updateTime = SG_SimpleTracking.UpdateDuring.LateUpdate;
        }
    }




    // Use this for initialization
    protected void Awake ()
    {
        SetupSelf();
	}

#if UNITY_EDITOR
    protected void OnValidate()
    {
        this.Hand = null;
        CheckForScripts();
    }
#endif

}
