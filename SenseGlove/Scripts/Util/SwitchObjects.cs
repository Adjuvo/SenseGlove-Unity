using UnityEngine;

/// <summary> Used to switch children of two objects, with an optional keybind and run-on-start function </summary>
/// <remarks> Can be added to trackers to switch left and right hand. </remarks>
public class SwitchObjects : MonoBehaviour
{
    //-----------------------------------------------------------------------------------------------
    // Swap Method

    /// <summary> Options for swapping the two objects </summary>
    public enum SwapMethod
    {
        Objects = 0,
        Children
    }

    //-----------------------------------------------------------------------------------------------
    // Properties

    /// <summary> One of the GameObjects to swap. </summary>
    [Tooltip("One of the GameObjects to swap. Both objects must be assigned for this script to work.")]
    public GameObject objectA, objectB;

    /// <summary> How the GameObjects are swapped. (The objects themselves, or their children) </summary>
    [Tooltip("How the GameObjects are swapped. (The objects themselves, or their children)")]
    public SwapMethod swapMethod = SwapMethod.Objects;

    /// <summary> Optional HotKey to call the Swap() funtion with the currently selected swapMethod. </summary>
    [Tooltip("Optional HotKey to call the Swap() funtion with the currently selected swapMethod.")]
    public KeyCode swapKey = KeyCode.None;

    /// <summary> Determines if the Swap() funtion runs on startup.  </summary>
    [Tooltip("Determines if the Swap() funtion runs on startup.")]
    public bool runOnStart = false;

    /// <summary> Detemines if this script can output to the console. </summary>
    [Tooltip("Detemines if this script can output to the console.")]
    public bool debug = false;

    /// <summary> utility value to run the SwapObjects just after the Start() of other objects has completed. </summary>
    protected bool init = false;

    //-----------------------------------------------------------------------------------------------
    // Class Methods

    /// <summary> Check if the two objects have been assigned and can swap. </summary>
    /// <returns></returns>
    public bool CanSwap()
    {
        return this.objectA != null && this.objectB != null;
    }

    /// <summary> Output to the console, but only if this scripts debugging is enabled. </summary>
    /// <param name="msg"></param>
    protected void Log(string msg)
    {
        if (this.debug)
            Debug.Log(msg);
    }

    /// <summary> Swap object A and B using the chosen SwapMethod. </summary>
    /// <param name="swapWhich"></param>
    public void Swap(SwapMethod swapWhich)
    {
        if (this.CanSwap())
        {
            if (swapWhich == SwapMethod.Objects)
            {
                this.Log("Swapping " + this.objectA.name + " and " + this.objectB.name + ".");
                this.SwapObjects();
            }
            else if (swapWhich == SwapMethod.Children)
            {
                this.Log("Swapping children of " + this.objectA.name + " and " + this.objectB.name + ".");
                this.SwapChildren();
            }
        }
    }

   
    /// <summary> Set the new parent of another transform, keepign the same local position- and rotation. </summary>
    /// <param name="obj"></param>
    /// <param name="newParent"></param>
    protected void SetParent(Transform obj, Transform newParent)
    {
        if (obj != null && !GameObject.ReferenceEquals(obj.parent, newParent))
        {
            Quaternion localRot = obj.localRotation;
            Vector3 localPos = obj.localPosition;
            obj.parent = newParent;
            obj.localPosition = localPos;
            obj.localRotation = localRot;
        }
    }

    /// <summary> Swap ObjectA and ObjectB themselves. </summary>
    protected void SwapObjects()
    {
        Transform A_parent = objectA.transform.parent;
        SetParent(objectA.transform, objectB.transform.parent);
        SetParent(objectB.transform, A_parent);
    }

    /// <summary> Swap the children of Object A and Object B </summary>
    protected void SwapChildren()
    {
        //Store A's original children
        Transform[] transforms_A = new Transform[this.objectA.transform.childCount];
        for (int i=0; i<transforms_A.Length; i++)
        {
            transforms_A[i] = this.objectA.transform.GetChild(i);
        }

        //Store B's original children
        Transform[] transforms_B = new Transform[this.objectB.transform.childCount];
        for (int i = 0; i < transforms_B.Length; i++)
        {
            transforms_B[i] = this.objectB.transform.GetChild(i);
        }

        //Assign B's original children to A
        for (int i = 0; i < transforms_B.Length; i++)
        {
            this.SetParent(transforms_B[i], this.objectA.transform);
        }

        //Assign A's original children to B
        for (int i = 0; i < transforms_A.Length; i++)
        {
            this.SetParent(transforms_A[i], this.objectB.transform);
        }
    }

    //-----------------------------------------------------------------------------------------------
    // Monobehaviour
	
	// Update is called once per frame
	void Update ()
    {
		if (!this.init)
        {
            if (this.runOnStart)
                this.Swap(this.swapMethod);
            this.init = true;
        }


        if (Input.GetKeyDown(this.swapKey))
        {
            this.Swap(this.swapMethod);
        }

	}
}
