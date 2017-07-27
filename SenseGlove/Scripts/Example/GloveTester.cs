using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveTester : MonoBehaviour
{

    public SenseGlove_WireFrame wireframe;
    public SenseGlove_Object glove;
    public SenseGlove_PhysGrab grabScript;

    public GameObject Tracker;

    public TextMesh debugText;

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
            //    //SenseGlove_Debugger.Log("Calibrate Glove.");
            //    wireframe.CalibrateWrist();
            //}
        }

        if (debugText != null && glove.GloveData() != null)
        {
            //debugText.text = "";


            //debugText.text = "Samples / Second = " + glove.GetSenseGlove().communicator.samplesPerSecond;

            

            debugText.text = "Angles: " + SenseGlove_Util.ToString(glove.GloveData().handAngles[1][0]);

            //debugText.text = SenseGlove_Util.ToString(SenseGloveCs.Values.Degrees(glove.GetGloveData().handModel.handAngles[1][2]));

            //debugText.text = SenseGlove_Util.ToString(glove.GetData().absoluteRelativeWrist.eulerAngles);   

            //Vector3 X = new Vector3(1, 0, 0);
            //Vector3 Y = new Vector3(0, 1, 0);
            //Vector3 Z = new Vector3(0, 0, 1);

            //float xAngle = Vector3.Angle(glove.GetData().relativeWrist * X, glove.foreArm.transform.rotation * X);
            //float yAngle = Vector3.Angle(glove.GetData().relativeWrist * Y, glove.foreArm.transform.rotation * Y);
            //float zAngle = Vector3.Angle(glove.GetData().relativeWrist * Z, glove.foreArm.transform.rotation * Z);

            //  debugText.text = "X:" +  xAngle.ToString() + ". Y:" + yAngle.ToString() + ". Z:" + zAngle.ToString();

            //float[] DLLwristEuler   = SenseGloveCs.Values.Degrees(SenseGloveCs.Quaternions.ToEuler(glove.GetGloveData().wrist.Qrelative ));
            //float[] DLLforeArmEuler = SenseGloveCs.Values.Degrees(SenseGloveCs.Quaternions.ToEuler(glove.GetGloveData().wrist.QforeArm  ));
            
            //debugText.text = SenseGlove_Util.ToString(DLLwristEuler) + "\r\n" + SenseGlove_Util.ToString(DLLforeArmEuler);

            
        }

        if (debugText != null && grabScript != null) 
        {
            //debugText.text = SenseGlove_Util.ToString(grabScript.GetVelocity());
        }


    }
}
