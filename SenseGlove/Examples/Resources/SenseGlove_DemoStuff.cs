using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SenseGlove_Examples
{

    public class SenseGlove_DemoStuff : MonoBehaviour
    {

        public SenseGlove_Object handModel_L, handModel_R;

        /// <summary> Determines if calibration data is stored between sessions </summary>
        public bool saveLengths = false;

        public bool saveStartpos = false;

        public KeyCode swapHandsKey = KeyCode.S;

        // Use this for initialization
        protected virtual void Start()
        {
            if (handModel_L != null)
            {
                handModel_L.GloveLoaded += LoadLeft;
            }
            if (handModel_R != null)
            {
                handModel_R.GloveLoaded += LoadRight;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            if (this.handModel_L != null && this.handModel_L.GloveReady)
                this.SaveGloveData(this.handModel_L);
            if (this.handModel_R != null && this.handModel_R.GloveReady)
                this.SaveGloveData(this.handModel_R);
        }

        private void LoadRight(object source, System.EventArgs args)
        {
            LoadGloveData(this.handModel_R);
        }

        private void LoadLeft(object source, System.EventArgs args)
        {
            LoadGloveData(this.handModel_L);
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (Input.GetKeyDown(this.swapHandsKey))
            {
                this.SwapHands();
            }
        }

        /// <summary>
        /// switch the parent transforms of the hands
        /// </summary>
        public void SwapHands()
        {
            if (this.handModel_L && this.handModel_R)
            {
                Transform parentR = this.handModel_R.transform.parent;
                Vector3 posR = this.handModel_R.transform.localPosition;
                Quaternion rotR = this.handModel_R.transform.localRotation;

                Transform parentL = this.handModel_L.transform.parent;
                Vector3 posL = this.handModel_L.transform.localPosition;
                Quaternion rotL = this.handModel_L.transform.localRotation;


                this.handModel_R.transform.parent = parentL;
                this.handModel_R.transform.localPosition = posL;
                this.handModel_R.transform.localRotation = rotL;

                this.handModel_L.transform.parent = parentR;
                this.handModel_L.transform.localPosition = posR;
                this.handModel_L.transform.localRotation = rotR;
            }
        }





        private static string GenerateFileName(SenseGlove_Object glove)
        {
            return "" + glove.gameObject.name + ".txt";
        }

        public void LoadGloveData(SenseGlove_Object senseGlove)
        {
            if (saveLengths || saveStartpos)
            {
                try
                {
                    string[] lines = File.ReadAllLines(GenerateFileName(senseGlove));

                    if (lines.Length >= 5)
                    {
                        Vector3[] positions = new Vector3[5];
                        float[][] lengths = new float[5][];

                        for (int f = 0; f < positions.Length; f++)
                        {
                            string[] split = lines[f].Split('\t');
                            if (split.Length >= 3)
                            {
                                float x = SenseGloveCs.Values.toFloat(split[0]);
                                float y = SenseGloveCs.Values.toFloat(split[1]);
                                float z = SenseGloveCs.Values.toFloat(split[2]);
                                positions[f] = new Vector3(x, y, z);
                            }
                            if (split.Length >= 6)
                            {
                                float PP = SenseGloveCs.Values.toFloat(split[3]);
                                float MP = SenseGloveCs.Values.toFloat(split[4]);
                                float DP = SenseGloveCs.Values.toFloat(split[5]);
                                lengths[f] = new float[] { PP, MP, DP };
                            }
                        }

                        if (saveLengths && lengths.Length > 0)
                        {
                            senseGlove.FingerLengths = (lengths);
                        }
                        if (saveStartpos && positions.Length > 0)
                        {
                            senseGlove.StartJointPositions = (positions);
                        }
                    }
                }
                catch (System.Exception Ex)
                {
                    Debug.Log(Ex.Message);
                }
            }
        }



        private void SaveGloveData(SenseGlove_Object senseGlove)
        {
            Vector3[] positions = senseGlove.StartJointPositions;
            float[][] lengths = senseGlove.FingerLengths;

            if (true)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(GenerateFileName(senseGlove));

                    for (int f = 0; f < positions.Length; f++)
                    {
                        string line = "";

                        line += positions[f].x + "\t" + positions[f].y + "\t" + positions[f].z;
                        line += "\t";
                        line += lengths[f][0] + "\t" + lengths[f][1] + "\t" + lengths[f][2];

                        writer.WriteLine(line);
                    }
                    writer.Close();
                    Debug.Log("Saved Glove Lenghts of " + senseGlove.gameObject.name);
                }
                catch (System.Exception Ex)
                {
                    Debug.Log(Ex.Message);
                }
            }
        }

    }

}