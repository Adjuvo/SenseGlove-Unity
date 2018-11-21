using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SenseGloveCs;

/// <summary> Attach this object to the Sense Glove to allow it to teleport around a room. </summary>
public class SenseGlove_Teleport : MonoBehaviour
{

    //----------------------------------------------------------------------------------------------------------------------------------
    // Public Properties

    #region Properties

    /// <summary> The camerRig object that controls the position of the VR setup within the scene, including that of the camera_eye. </summary>
    [Header("Required Components")]
    [Tooltip("The camerRig object that controls the position of the VR setup within the scene, including that of the camera_eye.")]
    public GameObject cameraRig;

    /// <summary> The HMD within the cameraRig. </summary>
    [Tooltip("The HMD within the cameraRig.")]
    public GameObject camera_eye;

    /// <summary> The origin of the pointer, which extends from its forward (Z) axis. </summary>
    [Tooltip("The origin of the pointer, which extends from its forward (Z) axis.")]
    public Transform pointerOriginZ;

    /// <summary> The glove to use for teleportation. </summary>
    [Tooltip("The glove to use for teleportation.")]
    public SenseGlove_HandModel senseGlove;

    /// <summary> A collider placed in the hand that is used to detect gestures. </summary>
    [Tooltip("A collider placed in the hand that is used to detect fingers.")]
    public SenseGlove_FingerDetector gestureDetector;

    /// <summary> The cooldown (in seconds) in between teleports </summary>
    [Tooltip("The cooldown (in seconds) in between teleports")]
    public float coolDown = 1.0f;

    /// <summary> The layers that this Teleporter uses for collision. </summary>
    [Header("Collision Options")]
    [Tooltip("The layers that this Teleporter uses for collision.")]
    public LayerMask collisionLayers;

    /// <summary> The way that the teleport position is calculated </summary>
    [Tooltip("The way that the teleport position is calculated ")]
    public Util.PointerOption pointerStyle = Util.PointerOption.StraightLine;

    ///// <summary> The maximum distance (in m) that the user can teleport in one go. </summary>
    //[Tooltip("The maximum distance (in m) that the user can teleport in one go.")]
    //private float maxDistance = 10; //private since it has not been implemented yet.

    /// <summary> Whether or not the cameraRig can move in the y-direction. </summary>
    [Tooltip("Whether or not the cameraRig can move in the y-direction.")]
    public bool ignoreY = false;

    /// <summary> The color of the pointer / indicator </summary>
    [Header("Display Options (Optional)")]
    [Tooltip("The color of the pointer / indicator ")]
    public Color indicatorColor = Color.green;

    /// <summary> An optional GameObject that appears (in the hand) to indicate that teleportation is active. </summary>
    [Tooltip("An optional GameObject that appears (for example in the hand) to indicate that teleportation is active.")]
    public Renderer activeIndicator;

    /// <summary> Highlighters that are set to active while the teleporter is online. </summary>
    [Tooltip("Highlighters (to aid navigation) that are set to active while the pointer is active.")]
    public List<Renderer> teleportHighlights = new List<Renderer>();

    //----------------------------------------------------------------------------------------------------------------------------------
    // Private Properties

    /// <summary> Determines if the teleporter is currently active. </summary>
    private bool isActive = false;

    /// <summary> The gameobject representing the pointer from the origin to the endPoint. </summary>
    private GameObject pointer;

    /// <summary> A sphere that appears at the desired location, if any is available. </summary>
    private GameObject endPointTracker;

    /// <summary> Optional, used to disable the teleporter while the user is interacting with an object. </summary>
    private SenseGlove_GrabScript grabScript;

    /// <summary> Timer for the cooldown of the teleport </summary>
    private float coolDownTimer = 0;
    
    /// <summary> Timer to activate the laser </summary>
    private float activateTimer = 0; //uses isactivated

    /// <summary> Time (seconds) that the user must point with the index finger before the pointer activates. </summary>
    public static float activationTime = .2f;



    /// <summary> Timer to teleport. </summary>
    private float teleportTimer = 0;

    /// <summary> Time (seconds) that the user must press with their thumb before the teleportation activates. </summary>
    public static float teleportTime = 0f;

    /// <summary> Indicates that the user has teleported using the fingerDetector. </summary>
    private bool hasTeleported = false;

    /// <summary> Thickness of the laser if using a Straight Line style. </summary>
    private static readonly float laserThickness = 0.002f;

    /// <summary> The diameter of the sphere that indicates where the player will teleport to. </summary>
    private static readonly float endPointDiameter = 0.15f;

    #endregion Properties

    //----------------------------------------------------------------------------------------------------------------------------------
    // Utility Methods

    #region Utility

    /// <summary> Create new Pointer objects based on the chosen style. </summary>
    protected void CreatePointer()
    {
        Material ptrMaterial = new Material(Shader.Find("Unlit/Color"));
        ptrMaterial.SetColor("_Color", indicatorColor);

        //create the pointer
        if (this.pointerStyle == Util.PointerOption.StraightLine)
        {
            this.pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            this.pointer.transform.parent = null;
            this.pointer.transform.localScale = new Vector3(laserThickness, laserThickness, 20f);

            this.pointer.transform.position = this.pointerOriginZ.position + (this.pointerOriginZ.rotation * new Vector3(0, 0, 10f));

            this.pointer.transform.parent = this.pointerOriginZ;
            this.pointer.transform.localRotation = Quaternion.identity;

            GameObject.Destroy(pointer.GetComponent<BoxCollider>()); //remove boxCollider so no collision calculation is done.
            
            pointer.GetComponent<MeshRenderer>().material = ptrMaterial;

            //Setup the end point
            this.endPointTracker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.endPointTracker.name = "TeleportLocation";
            GameObject.Destroy(endPointTracker.GetComponent<SphereCollider>());
            endPointTracker.transform.parent = null;
            endPointTracker.transform.localScale = new Vector3(endPointDiameter, endPointDiameter, endPointDiameter);
            endPointTracker.GetComponent<MeshRenderer>().material = ptrMaterial;
        }
        

    }

    /// <summary> Set the pointer and its active indicator to true/false. </summary>
    /// <param name="active"></param>
    public void SetPointer(bool active)
    {
        this.pointer.SetActive(active);
        if (this.endPointTracker != null)
        {
            this.endPointTracker.SetActive(active);
        }
        if (this.activeIndicator != null)
        {
            this.activeIndicator.enabled = active;
        }
    }

    /// <summary> Activate / Deactivate the teleport HighLights </summary>
    /// <param name="active"></param>
    public void SetHighlights(bool active)
    {
        for (int i = 0; i < this.teleportHighlights.Count; i++)
        {
            this.teleportHighlights[i].enabled = active;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------------------
    // Teleport Methods

    /// <summary> Activate the teleporter manually, untill you call a disable function. </summary>
    public void ActivateTeleporter()
    {
        this.SetPointer(true);
        this.SetHighlights(true);
        this.isActive = true;
    }

    /// <summary> Disable the teleporter untill you call ActivateTeleport. </summary>
    public void DisableTeleporter()
    {
        this.SetPointer(false);
        this.SetHighlights(false);
        this.isActive = false;
        this.activateTimer = 0;
    }
    
    /// <summary> Calculates the desired position of the teleporter based on the chosen PointerOption </summary>
    /// <returns></returns>
    public bool CalculateDesiredPos(out Vector3 newPos)
    {
        newPos = Vector3.zero;
        bool validHit = false;

        switch (this.pointerStyle)
        {
            case Util.PointerOption.StraightLine:

                Ray raycast = new Ray(this.pointerOriginZ.position, this.pointerOriginZ.forward);
                RaycastHit hit;
                bool bHit = Physics.Raycast(raycast, out hit, 100f, this.collisionLayers);

                float d = 20f;
                
                if (bHit)
                {
                    Vector3 destination = hit.point;
                    //Vector3 rigPos = this.cameraRig.transform.position;
                    //Vector3 dir2D = new Vector3(destination.x - rigPos.x, 0, destination.z - rigPos.z);

                    //if (dir2D.magnitude > this.maxDistance)
                    //{
                    //    destination = rigPos + (dir2D.normalized * this.maxDistance);
                    //    destination.y = hit.point.y;
                    //}
                    newPos = destination;
                    d = (newPos - pointerOriginZ.position).magnitude; //size (m between points)
                    validHit = true;
                }

                //update pointer
                this.pointer.transform.parent = null;
                this.pointer.transform.localScale = new Vector3(laserThickness, laserThickness, d);
                this.pointer.transform.position = this.pointerOriginZ.position + (this.pointerOriginZ.rotation * new Vector3(0, 0, (d / 2.0f)));
                this.pointer.transform.parent = this.pointerOriginZ;

                return validHit;
        }

        newPos = Vector3.zero;
        return false;
    }

    /// <summary> Teleport to the position of the endPointTracker, but only if it is active on a valid position. </summary>
    public void Teleport()
    {
        if (this.IsActive() && this.endPointTracker.activeInHierarchy)
        {
            this.Teleport(this.endPointTracker.transform.position);
        }
    }

    /// <summary> Set the position of the CameraRig so that the HMD is at the new desired location. </summary>
    /// <param name="newHMDPos"></param>
    public void Teleport(Vector3 newHMDPos)
    {
        if (this.cameraRig != null)
        {
            if (this.camera_eye != null)
            {
                Vector3 dPos = this.camera_eye.transform.position - this.cameraRig.transform.position;

                float newY = ignoreY ? this.cameraRig.transform.position.y : newHMDPos.y;

                Vector3 newpos = new Vector3
                    (
                        newHMDPos.x - dPos.x,
                        newY,
                        newHMDPos.z - dPos.z
                    );
                this.cameraRig.transform.position = newpos;
            }
            else
            {
                this.cameraRig.transform.position = newHMDPos;
            }
            this.coolDownTimer = 0;
        }
        else
        {
            SenseGlove_Debugger.LogError(this.name + ".SenseGlove_Teleport requires access to a CameraRig.");
        }
    }

    /// <summary> Returns true if the laser should activate. </summary>
    /// <returns></returns>
    public virtual bool ShouldActivate()
    {
        if (this.gestureDetector != null && (this.senseGlove == null || this.senseGlove.senseGlove.GloveReady))
        {
            if (this.grabScript == null || (this.grabScript != null && !this.grabScript.IsTouching()))
            {
                return !this.gestureDetector.TouchedBy(Finger.Index)
                    && this.gestureDetector.TouchedBy(Finger.Middle)
                    && this.gestureDetector.TouchedBy(Finger.Ring)
                    && this.gestureDetector.TouchedBy(Finger.Little);
            }
        }
        return false;
    }

    /// <summary> Returns true if we should teleport. </summary>
    /// <returns></returns>
    public virtual bool ShouldTeleport()
    {
        if (this.gestureDetector != null) //no need to check if active, because otherwise the teleporter is not even active.
        {
            return this.gestureDetector.TouchedBy(Finger.Thumb);
        }
        return false;
    }

    #endregion Utility

    //----------------------------------------------------------------------------------------------------------------------------------
    // Variable Access

    #region Variables

    /// <summary> Check if the teleporter is currently active. </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return this.isActive;
    }
    
    /// <summary> Check if the cooldown timer is currently running </summary>
    /// <returns></returns>
    public bool IsOnCoolDown()
    {
        return this.coolDownTimer < this.coolDown;
    }

    #endregion Variables

    //----------------------------------------------------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    protected virtual void Start ()
    {
        if (this.senseGlove != null)
        {
            this.grabScript = this.senseGlove.gameObject.GetComponent<SenseGlove_GrabScript>();
        }
        CreatePointer();
        SetPointer(false);
        SetHighlights(false);
        this.coolDownTimer = this.coolDown; //set to equal so we can teleport right away.
	}

    // Update is called once per frame
    protected virtual void Update ()
    {
        if (this.coolDownTimer < this.coolDown)
        {
            this.coolDownTimer += Time.deltaTime;
            if (this.endPointTracker.activeInHierarchy) { this.endPointTracker.SetActive(false); } //disable end point (and with it, teleport)
        }
        else
        {
            //check activation gestures
            if (this.gestureDetector != null)
            {
                bool shouldActivate = this.ShouldActivate();
                if (shouldActivate && !this.isActive)
                {
                    if (this.activateTimer < SenseGlove_Teleport.activationTime)
                    {
                        this.activateTimer += Time.deltaTime;
                    }
                    else
                    {
                        this.ActivateTeleporter();
                    }
                }
                else if (!shouldActivate && this.isActive)
                {
                    this.DisableTeleporter();
                }
            }

            if (this.IsActive())
            {
                //calculate the desired position and put the endPoint tracker over there
                Vector3 newPos;
                if (CalculateDesiredPos(out newPos))
                {
                    if (!this.endPointTracker.activeInHierarchy) { this.endPointTracker.SetActive(true); }
                    this.endPointTracker.transform.position = newPos;
                }
                else
                {
                    if (this.endPointTracker.activeInHierarchy) { this.endPointTracker.SetActive(false); }
                }


                //check teleport gesture
                if (this.gestureDetector != null)
                {
                    bool shouldTeleport = this.ShouldTeleport();
                    if (shouldTeleport && !hasTeleported)
                    {
                        if (this.teleportTimer < SenseGlove_Teleport.teleportTime)
                        {
                            this.teleportTimer += Time.deltaTime;
                        }
                        else
                        {
                            this.Teleport();
                            this.hasTeleported = true;
                        }
                    }
                    else if (!shouldTeleport)
                    {
                        this.hasTeleported = false;
                    }
                }


            }
        }
        
	}

    #endregion Monobehaviour

}

namespace Util
{
    /// <summary> Which style of pointer to use for the Teleporter. </summary>
    public enum PointerOption
    {
        StraightLine = 0,
       // Bezier
    }
}