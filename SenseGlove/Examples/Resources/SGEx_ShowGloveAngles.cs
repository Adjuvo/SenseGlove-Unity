using UnityEngine;
using UnityEngine.UI;
using SG;
using SGCore.SG;
using SGCore.Nova;

namespace SG.Examples
{
    public class SGEx_ShowGloveAngles : MonoBehaviour
    {
        public SG_HapticGlove senseGlove;

        public GridLayoutGroup angleCanvas;

        private Text[][] angleBoxes;

        private bool setup = false;

        Text CreateTextBox(string textString, Font font, Transform parent, string objName = "textBox")
        {
            GameObject txtObj = new GameObject(objName);
            txtObj.transform.SetParent(angleCanvas.transform);
            Text text = txtObj.AddComponent<Text>();
            text.font = font;
            text.material = font.material;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.text = textString;
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }


        protected bool GetSensorData(SG_HapticGlove hapticGlove, out float[][] sensors)
        {
            sensors = new float[0][];// = senseGlove.GloveData.gloveValues;
            SGCore.HapticGlove glove;
            if (SG_HapticGlove.GetGloveInstance(hapticGlove.connectsTo, out glove))  //hapticGlove.GetInternalObject();
            {
                if (glove.GetDeviceType() == SGCore.DeviceType.SENSEGLOVE)
                {
                    SG_SensorData sData;
                    if (((SGCore.SG.SenseGlove)glove).GetSensorData(out sData))
                    {
                        sensors = new float[sData.SensorAngles.Length][];
                        for (int f = 0; f < sensors.Length; f++)
                        {
                            sensors[f] = new float[sData.SensorAngles[f].Length];
                            for (int j = 0; j < sensors[f].Length; j++)
                            {
                                float degr = (float)System.Math.Round(sData.SensorAngles[f][j] * Mathf.Rad2Deg, 2);
                                sensors[f][j] = j < sensors[f].Length - 1 ? SG.Util.SG_Util.NormalizeAngle(degr)
                                    : SG.Util.SG_Util.NormalizeAngle(degr, -60, 300);
                            }
                        }
                    }
                }
                else if (glove.GetDeviceType() == SGCore.DeviceType.NOVA)
                {
                    Nova_SensorData sData;
                    if (((SGCore.Nova.NovaGlove)glove).GetSensorData(out sData))
                    {
                        sensors = new float[sData.SensorValues.Length][];
                        for (int f = 0; f < sensors.Length; f++)
                        {
                            sensors[f] = new float[3]
                            {
                            sData.SensorValues[f][0].x,
                            sData.SensorValues[f][0].y,
                            sData.SensorValues[f][0].z
                            };
                        }
                    }
                }
            }
            return sensors.Length > 0;
        }


        void SetupAngleUI(float[][] sensors)
        {
            if (angleCanvas != null)
            {
                if (sensors.Length > 0)
                {
                    int columns = sensors[0].Length + 1;
                    angleCanvas.constraintCount = columns;

                    angleBoxes = new Text[sensors.Length][];
                    Font arialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                    // fingers = row, sensors = column
                    for (int f = 0; f < sensors.Length; f++)
                    {
                        angleBoxes[f] = new Text[sensors[f].Length];
                        CreateTextBox(((SGCore.Finger)f).ToString(), arialFont, angleCanvas.transform);
                        for (int j = 0; j < sensors[f].Length; j++)
                        {
                            angleBoxes[f][j] = CreateTextBox("0", arialFont, angleCanvas.transform);
                        }
                    }
                }
                else
                {
                    Debug.LogError("This isn't a Haptic Glove, so we won't be showing glove angles.");
                }
            }
        }

        void UpdateAngleUI(float[][] sensors)
        {
            if (angleCanvas != null)
            {
                for (int f = 0; f < sensors.Length && f < angleBoxes.Length; f++)
                {
                    for (int j = 0; j < sensors[f].Length && j < angleBoxes[f].Length; j++)
                    {
                        angleBoxes[f][j].text = sensors[f][j].ToString();
                    }
                }
            }
        }

        private void Update()
        {
            if (senseGlove != null && senseGlove.IsConnected())
            {
                float[][] sData;
                if (GetSensorData(senseGlove, out sData))
                {
                    if (!setup)
                    {
                        setup = true;
                        SetupAngleUI(sData);
                    }
                    UpdateAngleUI(sData);
                }
            }
        }

    }
}