using System.Collections;
using UnityEngine;

/*
 * Spawns itself a set distance from the main camera rig on start.
 * Calling this a persistent panel because it wants you to keep looking at it >:(
 * 
 * author:
 * max@senseglove.com
 */
public class SG_PersistantPanel : MonoBehaviour
{
    public Transform mainCameraTransform;

    public Transform playAreaTransform;

    /// <summary> Snaps 'in front on you' when you move this far away </summary>
    public float movedAngle = 45;
    public float movedDistance = 1.5f;
    
    //camera Forwards vs camera right

    public float debugValue;


    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    

    public void TryLinkCamera()
    {
        if (mainCameraTransform == null)
        {
            mainCameraTransform = SG_XR_SceneTrackingLinks.GetHeadTransform();
            if (mainCameraTransform == null)
            {
                Camera cam = Camera.main;
                mainCameraTransform = cam != null ? cam.transform : null;
            }
        }
        if (playAreaTransform == null)
        {
            playAreaTransform = SG_XR_SceneTrackingLinks.GetXRRigTransform();
            if (playAreaTransform == null)
            {
                GameObject obj = GameObject.Find("XR Rig");
                playAreaTransform = obj != null ? obj.transform : null;
            }
        }
    }

    public void CheckPositionUpdate()
    {
        if (mainCameraTransform == null)
            return;
        //We're using right and not forward to remove the effects of looking up/down.
        Vector3 myRight = this.transform.right;
        Vector3 camRight = mainCameraTransform.right;
        float angleDiff = Vector3.Angle(myRight, camRight);

        float camDist = (this.transform.position - mainCameraTransform.position).magnitude;

        if (angleDiff > movedAngle || camDist > this.movedDistance)
            MoveToFront();
    }



    /// <summary> Places this Gameobject's Transform back 'in front' of the camera. Perhaps 'just' to the closest 45 degree angle? </summary>
    public void MoveToFront()
    {
        if (mainCameraTransform == null)
            return;

        Vector3 camRight = mainCameraTransform.right;
        float cameraWorldAngle = Mathf.Atan2(camRight.x, camRight.z) - (Mathf.Deg2Rad * 90.0f);
        Vector3 camForward = new Vector3(
            Mathf.Sin(cameraWorldAngle),
            0f,
            Mathf.Cos(cameraWorldAngle)
            );

        debugValue = cameraWorldAngle * Mathf.Rad2Deg;

        Debug.DrawLine(Vector3.zero, camForward, Color.yellow);

        //If the XR Rig is not Zero, then we go from there. Otherwise, use the Camera Location?
        //Vector3 center = this.playAreaTransform != null ? this.playAreaTransform.position : mainCameraTransform.position - new Vector3(0.0f, positionOffset.y, 0.0f);
        Vector3 center = mainCameraTransform.position;

        Quaternion baseRotation = Quaternion.LookRotation(camForward, Vector3.up); //global up and my forward.

        Debug.DrawLine(center, center + camForward, Color.green);

        Vector3 endPosition = center + (baseRotation * positionOffset);
        Quaternion endRotation = baseRotation * Quaternion.Euler(rotationOffset);


        this.transform.rotation = endRotation;
        this.transform.position = endPosition;
    }


    private IEnumerator RecenterAfter(float time)
    {
        yield return new WaitForSeconds(time);
        MoveToFront();
    }


    // Start is called before the first frame update
    void Start()
    {
        TryLinkCamera();
        MoveToFront();

        movedDistance = Mathf.Max(movedDistance, positionOffset.magnitude + 0.01f); //if my moveDistance is set lower than the position offset, we'll alwasy teleport away
        StartCoroutine(RecenterAfter(0.5f)); //TODO: Always do this uping rgaining XR focus?
    }

    // Update is called once per frame
    void Update()
    {
        CheckPositionUpdate();
    }
}
