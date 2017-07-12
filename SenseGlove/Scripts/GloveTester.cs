using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveTester : MonoBehaviour
{

    public SenseGlove_WireFrame wireframe;
    public SenseGlove_Object glove;
    public SenseGlove_PhysGrab grabScript;

    public GameObject Tracker;

    public float spd = 0.5f;

    public bool gloveActive = true;
    public bool handActive = true;

    // Use this for initialization
    void Start()
    {
    }



    // Update is called once per frame
    void Update()
    {
        //if (grabScript != null)
        //{
        //    if (Input.GetKeyDown(KeyCode.LeftAlt))
        //    {
        //        grabScript.ManualRelease();
        //    }

        //}

        if (Tracker != null)
        {
            Vector3 pos = Tracker.transform.position;
            if (Input.GetKey(KeyCode.W))
            {
                pos = new Vector3(pos.x + (spd * Time.deltaTime), pos.y, pos.z);
            }
            else if (Input.GetKey(KeyCode.S))
            {
                pos = new Vector3(pos.x - (spd * Time.deltaTime), pos.y, pos.z);
            }

            if (Input.GetKey(KeyCode.A))
            {
                pos = new Vector3(pos.x, pos.y, pos.z + (spd * Time.deltaTime));
            }
            else if (Input.GetKey(KeyCode.D))
            {
                pos = new Vector3(pos.x, pos.y, pos.z - (spd * Time.deltaTime));
            }
            Tracker.transform.position = pos;
        }


        if (wireframe != null)
        {
            if (wireframe.SetupComplete())
            {
                gloveActive = false;
                wireframe.SetGlove(gloveActive);
            }


            if (UnityEngine.Input.GetKeyDown(KeyCode.G))
            {
                gloveActive = !gloveActive;
                wireframe.SetGlove(gloveActive);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                handActive = !handActive;
                wireframe.SetHand(handActive);
            }

            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    //Debug.Log("Calibrate Glove.");
            //    wireframe.CalibrateWrist();
            //}
        }



    }
}
