using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveUtils;


/// <summary> Enumerator that determines how the SenseGlove_PhysDebug indicates when its SenseGlove_PhysGrab script touches a valid interactable.</summary>
public enum PickupDebug
{
    /// <summary> The pickup debug colliders are turned off </summary>
    Off = 0,
    /// <summary> The debug colliders are visible, whether they are touching anything or not. Used to debug the Intention Checking. </summary>
    AlwaysOn,
    /// <summary> The Debug Colliders turn on when they touch a valid interactable, and turn off when they are not. </summary>
    ToggleOnTouch
}


[RequireComponent(typeof(SenseGlove_PhysGrab))]
public class SenseGlove_PhysDebug : MonoBehaviour
{

    /// <summary> Method to debug the SenseGlove_Touch scripts that are connected to this GrabScript </summary>
    [SerializeField]
    [Tooltip("Method to debug the SenseGlove_Touch scripts that are connected to this GrabScript")]
    protected PickupDebug debugMode = PickupDebug.Off;

    /// <summary> The color of the debug colliders in their default state, when the gesture recognition determines a grasping intention. </summary>
    [Tooltip("The color of the debug colliders.")]
    [SerializeField]
    protected Color canGrabColor = new Color(49/255f, 144/255f, 1, 1);

    /// <summary> The color of the debug colliders whenever the gesture recognition detects no intention to grab. </summary>
    [Tooltip("The color of the debug colliders.")]
    [SerializeField]
    protected Color cannotGrabColor = new Color(1, 187/255f, 0, 1);

    /// <summary> The Grabscript connected to this debugger. </summary>
    protected SenseGlove_PhysGrab grabScript;

    /// <summary> The last intention of the Grabscript, used to avoid changing materials every frame. </summary>
    protected bool[] lastIntention = new bool[5] { false, false, false, false, false };

    /// <summary> Debug Colliders of the fingers </summary>
    protected DebugCollider[][] fingers = null;

    /// <summary> Debug Collider of the Wrist </summary>
    protected DebugCollider palm = null;

    //-----------------------------------------------------------------------------------------------
    // Setup

    /// <summary> Setup the Debug Colliders of the fingers and/or palm </summary>
    protected void SetupDebugger()
    {
        if (this.palm == null && this.grabScript.palmCollider != null)
        {
            this.palm = SenseGlove_PhysDebug.CreateDebugObject(this.grabScript.palmCollider);
            this.palm.SetColor(this.canGrabColor);
        }

        if (this.fingers == null) //run only once.
        {
            this.fingers = new DebugCollider[5][]
            {
                SenseGlove_PhysDebug.ExtractObjects(this.grabScript.thumbColliders),
                SenseGlove_PhysDebug.ExtractObjects(this.grabScript.indexColliders),
                SenseGlove_PhysDebug.ExtractObjects(this.grabScript.middleColliders),
                SenseGlove_PhysDebug.ExtractObjects(this.grabScript.ringColliders),
                SenseGlove_PhysDebug.ExtractObjects(this.grabScript.pinkyColliders)
            };
        }

        this.ForceIntentions(this.lastIntention);
        SetDebugMode(this.debugMode);
    }


    /// <summary> Destroy the debug colliders. </summary>
    protected void CleanupDebugger()
    {
        if (this.palm != null)
        {
            this.palm.DeleteObj();
            this.palm = null;
        }
        if (this.fingers != null)
        {
            for (int f=0; f<this.fingers.Length; f++)
            {
                for (int j=0; j<this.fingers[f].Length; j++)
                {
                    this.fingers[f][j].DeleteObj();
                }
            }
            this.fingers = null;
        }
    }


    /// <summary> Create a primitive object representing this collider, that canbe turned on or off. </summary>
    /// <param name="touchCollider"></param>
    protected static DebugCollider CreateDebugObject(Collider touchCollider)
    {
        SenseGlove_Touch touchScr = touchCollider.GetComponent<SenseGlove_Touch>();
        GameObject debugObj = null;
        Renderer rend = null;

        if (touchCollider != null)
        {
            if (touchCollider is CapsuleCollider)
            {
                debugObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }
            else if (touchCollider is BoxCollider)
            {
                debugObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            else if (touchCollider is SphereCollider)
            {
                debugObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            }

            if (debugObj != null)
            {
                rend = debugObj.GetComponent<Renderer>();

                debugObj.transform.parent = null;
                debugObj.transform.position = touchCollider.transform.position;
                debugObj.transform.rotation = touchCollider.transform.rotation;
                debugObj.transform.localScale = touchCollider.transform.lossyScale;
                debugObj.transform.parent = touchCollider.transform;
                debugObj.name = "Debug Collider";

                //Destory the collider, creating only a rendered object.
                Collider C = debugObj.GetComponent<Collider>();
                if (C != null)
                {
                    GameObject.Destroy(C);
                }
                debugObj.SetActive(false); //disable by default?
            }
        }

        return new DebugCollider(touchScr, rend);
    }

    /// <summary> Create new DebugColliders from a list of Colliders of the Grabscript. </summary>
    /// <param name="colliders"></param>
    /// <returns></returns>
    protected static DebugCollider[] ExtractObjects(List<Collider> colliders)
    {
        DebugCollider[] res = new DebugCollider[colliders.Count];
        for (int i=0; i<colliders.Count; i++)
        {
            res[i] = SenseGlove_PhysDebug.CreateDebugObject(colliders[i]);
        }
        return res;
    }


    //-----------------------------------------------------------------------------------------------
    // Logic


    /// <summary> Update the intention visualization (grab/ungrab) of the fingers, changing materials only if needed. </summary>
    /// <param name="fing"></param>
    protected void SetIntentions(bool[] fing)
    {
        Color toSet = this.canGrabColor;
        if (this.fingers != null)
        {
            for (int f = 0; f < this.fingers.Length; f++)
            {
                if (this.lastIntention[f] != fing[f])
                {
                    toSet = fing[f] ? this.canGrabColor : this.cannotGrabColor;
                    for (int i = 0; i < this.fingers[f].Length; i++)
                    {
                        this.fingers[f][i].SetColor(toSet);
                    }
                    this.lastIntention[f] = fing[f];
                }
            }
        }
    }

    /// <summary> Set the intention visualization (grab/ungrab) of the fingers, without checking if they are already set. </summary>
    /// <param name="fing"></param>
    protected void ForceIntentions(bool[] fing)
    {
        Color toSet = this.canGrabColor;
        if (this.fingers != null)
        {
            for (int f = 0; f < this.fingers.Length; f++)
            {
                toSet = fing[f] ? this.canGrabColor : this.cannotGrabColor;
                for (int i = 0; i < this.fingers[f].Length; i++)
                {
                    this.fingers[f][i].SetColor(toSet);
                }
                this.lastIntention[f] = fing[f];
            }
        }
    }


    /// <summary> Turn all debug visuals on or off. </summary>
    /// <param name="active"></param>
    protected void SetAllVisuals(bool active)
    {
        if (this.palm != null)
        {
            this.palm.SetActive(active);
        }
        if (this.fingers != null)
        {
            for (int f = 0; f < this.fingers.Length; f++)
            {
                for (int i = 0; i < this.fingers[f].Length; i++)
                {
                    this.fingers[f][i].SetActive(active);
                }
            }
        }


    }

    /// <summary> Update this debugger; placed in a separate method so it can be called at different times. </summary>
    public void UpdateDebugger()
    {
        this.UpdateIntention();
        this.UpdateTouch();
    }

    /// <summary> Update the Intention colors based on the Grabscript. </summary>
    public void UpdateIntention()
    {
        //No need to check what the Grabscript's intentions are, as these are already refelcted in the wantsGrab variable.
        bool[] want = this.grabScript.GrabValues;
        this.SetIntentions(want);
    }

    /// <summary> Set the debug visuals on or off based on their touchScripts. </summary>
    public void UpdateTouch()
    {
        if (this.debugMode == PickupDebug.ToggleOnTouch)
        {
            if (this.palm != null)
            {
                this.palm.CheckToggle();
            }

            if (this.fingers != null)
            {
                for (int f=0; f<this.fingers.Length; f++)
                {
                    for (int i=0; i<this.fingers[f].Length; i++)
                    {
                        this.fingers[f][i].CheckToggle();
                    }
                }
            }
        }
    }

    /// <summary> Changes this script's Debug Mode </summary>
    /// <param name="mode"></param>
    public void SetDebugMode(PickupDebug mode)
    {
        this.debugMode = mode;
        if (debugMode != PickupDebug.ToggleOnTouch)
        {
            bool setOn = mode == PickupDebug.AlwaysOn ? true : false;
            SetAllVisuals(setOn);
        }
        else
        {
            this.UpdateTouch();
        }
    }


    //-----------------------------------------------------------------------------------------------
    // Monobehaviour

    // Use this for initialization
    protected virtual void Start ()
    {
        this.grabScript = this.gameObject.GetComponent<SenseGlove_PhysGrab>();
    }

    // Update is called once per frame
    protected virtual void Update ()
    {
		if ((this.fingers == null && this.palm == null) && this.grabScript.SetupComplete()) //ensures it is not called before the physgrab has completed setup.
        {
            this.SetupDebugger();
        }
        else
        {
            this.UpdateDebugger();
        }
	}

}

namespace SenseGloveUtils
{
    /// <summary> A container class for debugger scripts. </summary>
    public class DebugCollider
    {
        /// <summary> The SenseGlove_Touch script attached to this Debug Collider </summary>
        protected SenseGlove_Touch touchScript;

        /// <summary> The Renderer attached to the debug collider </summary>
        protected Renderer renderer;
        
        /// <summary> Create a new instance of a DebugCollider. </summary>
        /// <param name="touch"></param>
        /// <param name="rend"></param>
        public DebugCollider(SenseGlove_Touch touch, Renderer rend)
        {
            this.renderer = rend;
            this.touchScript = touch;
        }
        
        /// <summary> Set the color of this renderer and its associated render state </summary>
        /// <param name="newColor"></param>
        /// <param name="renderState"></param>
        public void SetColor(Color newColor)
        {
            this.renderer.material.color = newColor;
        }

        /// <summary> Delete the GameObject associated with this DebugCollider. Used when deleting the SenseGlove_PhysDebug Script. </summary>
        public void DeleteObj()
        {
            GameObject.Destroy(this.renderer.gameObject);
        }

        /// <summary> Turn the rendering component of this DebugCollider on or off </summary>
        /// <param name="active"></param>
        public void SetActive(bool active)
        {
            this.renderer.gameObject.SetActive(active);
        }
        
        /// <summary> Check if this Debug Collider should turn on or off, depending on if it is touching something. </summary>
        public void CheckToggle()
        {
            bool isTouched = this.touchScript.IsTouching();
            bool isOn = this.renderer.gameObject.activeInHierarchy;

            if (isTouched && !isOn)
            {
                this.SetActive(true);
            }
            else if (!isTouched && isOn)
            {
                this.SetActive(false);
            }
        }

    }


}

