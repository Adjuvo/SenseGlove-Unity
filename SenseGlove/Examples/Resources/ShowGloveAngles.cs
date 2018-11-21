using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SenseGlove_Examples
{
    public class ShowGloveAngles : MonoBehaviour
    {

        public SenseGlove_Object glove;

        public TextMesh text;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            this.UpdateAngles();
        }


        void UpdateAngles()
        {
            if (this.glove != null && glove.GloveReady)
            {

                float[][] angles = this.glove.GloveData.gloveValues;
                SenseGloveCs.GloveData data = SenseGlove_DeviceManager.GetSenseGlove(this.glove.GloveIndex).gloveData;

                string msg = "";
                for (int f = 0; f < angles.Length; f++)
                {
                    for (int j = 0; j < angles[f].Length; j++)
                    {
                        msg += (int)Mathf.Round(SenseGloveCs.Values.Degrees(angles[f][j]));


                        msg += "/";
                        if (j == 0)
                        {
                            msg += (int)Mathf.Round(SenseGloveCs.Values.Degrees(data.kinematics.gloveLinks[f].joints[j].relativeAngle.z));
                        }
                        else
                        {
                            msg += (int)Mathf.Round(SenseGloveCs.Values.Degrees(data.kinematics.gloveLinks[f].joints[j + 1].relativeAngle.y));

                        }

                        if (j < angles[f].Length - 1)
                        {
                            msg += " \t";
                        }
                    }
                    msg += "\r\n";
                }
                this.text.text = msg;
            }
        }

    }

}