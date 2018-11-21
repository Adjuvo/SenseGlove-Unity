using UnityEngine;

namespace SenseGlove_Examples
{

    public class GloveTester : MonoBehaviour
    {

        public SenseGlove_WireFrame wireframe;
        public SenseGlove_Object glove;
        public SenseGlove_PhysGrab grabScript;

        public GameObject Tracker;

        public TextMesh debugText;

        public float spd = 0.25f;

        public bool gloveActive = true;
        public bool handActive = true;

        public SenseGlove_Detector detector;

        // Use this for initialization
        void Start()
        {
            if (detector)
            {
                this.detector.GloveDetected += Detector_GloveDetected;
                this.detector.GloveRemoved += Detector_GloveRemoved;
            }
        }

        private void Detector_GloveRemoved(object source, System.EventArgs args)
        {
            Debug.Log("Glove was removed...");
        }

        private void Detector_GloveDetected(object source, System.EventArgs args)
        {
            Debug.Log("Detected a new glove!");
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

                if (Input.GetKey(KeyCode.Q))
                {
                    pos = new Vector3(pos.x, pos.y + (spd * Time.deltaTime), pos.z);
                }
                else if (Input.GetKey(KeyCode.E))
                {
                    pos = new Vector3(pos.x, pos.y - (spd * Time.deltaTime), pos.z);
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

            if (debugText != null && glove.GloveData != null)
            {
                //debugText.text = "";


                //debugText.text = "Samples / Second = " + glove.GetSenseGlove().communicator.samplesPerSecond;

                debugText.text = "IMU: " + SenseGlove_Util.ToString(glove.GloveData.imuCalibration);
                //debugText.text = "Angles: " + SenseGlove_Util.ToString(glove.GloveData().handAngles[1][0]);

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

            if (this.glove != null && this.glove.GloveReady)
            {
                if (Input.GetKeyDown(KeyCode.V))
                {
                    /*
                    float[][] handLengths = new float[5][]
                    {
                        new float[3] { 43, 33, 33 },   //thumb
                        new float[3] { 40, 22, 22 },   //index
                        new float[3] { 45, 28, 22 },   //middle
                        new float[3] { 47, 24, 23 },   //ring
                        new float[3] { 33, 18, 21 },   //pinky
                    };
                //    this.glove.CalculateJointPositions(handLengths);
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
               //     this.glove.TestCalibration();
               */
                }

            }

        }


    }

}