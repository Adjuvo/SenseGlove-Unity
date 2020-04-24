using UnityEngine;
using UnityEngine.UI;
using SenseGloveCs;

namespace SG.Examples
{
    public class SGEx_ShowGloveAngles : MonoBehaviour
    {
        public SG_SenseGloveHardware senseGlove;

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


        void SetupAngleUI()
        {
            if (angleCanvas != null)
            {
                float[][] sensors = senseGlove.GloveData.gloveValues;
                int columns = sensors[0].Length + 1;
                angleCanvas.constraintCount = columns;

                angleBoxes = new Text[sensors.Length][];
                Font arialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
                // fingers = row, sensors = column
                for (int f = 0; f < sensors.Length; f++)
                {
                    angleBoxes[f] = new Text[sensors[f].Length];
                    CreateTextBox(((Finger)f).ToString(), arialFont, angleCanvas.transform);
                    for (int j = 0; j < sensors[f].Length; j++)
                    {
                        angleBoxes[f][j] = CreateTextBox("0", arialFont, angleCanvas.transform);
                    }
                }

            }
        }

        void UpdateAngleUI(float[][] sensors)
        {
            float[][] angles = new float[sensors.Length][];
            for (int f = 0; f < sensors.Length; f++)
            {
                angles[f] = new float[sensors[f].Length];
                for (int j = 0; j < sensors[f].Length; j++)
                {
                    float degr = Mathf.Round(sensors[f][j] * Mathf.Rad2Deg);
                    angles[f][j] = j < sensors[f].Length - 1 ? SG_Util.NormalizeAngle(degr)
                        : SG_Util.NormalizeAngle(degr, -60, 300);
                }
            }

            if (angleCanvas != null)
            {
                for (int f = 0; f < angles.Length && f < angleBoxes.Length; f++)
                {
                    for (int j = 0; j < angles[f].Length && j < angleBoxes[f].Length; j++)
                    {
                        angleBoxes[f][j].text = angles[f][j].ToString() + "°";
                    }
                }
            }
        }

        private void Update()
        {
            if (senseGlove.GloveReady)
            {
                if (!setup)
                {
                    setup = true;
                    SetupAngleUI();
                }
                UpdateAngleUI(senseGlove.GloveData.gloveValues);
            }
        }

    }
}