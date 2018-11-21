using System.Collections.Generic;
using UnityEngine;

namespace SenseGlove_Examples
{
    /// <summary> Turns Sense Glove_Interactables on and off manually. Used to showcase materials. </summary>
    public class CycleObjects : MonoBehaviour
    {

        public SenseGlove_Object senseGlove;

        private SenseGlove_KeyBinds keyBinds;

        /// <summary> Text mesh to give instructions... </summary>
        public TextMesh objectText;

        public TextMesh instrText;

        private KeyCode nextObjKey = KeyCode.D;

        private KeyCode prevObjKey = KeyCode.A;

        public KeyCode quitKey = KeyCode.Escape;

        /// <summary> The current index in the objectsToSwap list. </summary>
        [Tooltip("The current index in the objectsToSwap list.")]
        public int index = -1;


        /// <summary> The list of objects to cycle through. </summary>
        [Tooltip("The list of objects to cycle through.")]
        public List<GameObject> objectsToSwap = new List<GameObject>();

        // Use this for initialization
        void Awake()
        {
            this.WriteObj("");
            for (int i = 0; i < this.objectsToSwap.Count; i++)
            {
                bool beActive = i == this.index;
                this.objectsToSwap[i].SetActive(beActive);
            }
        }

        void Start()
        {
            if (this.senseGlove != null)
            {
                this.keyBinds = this.senseGlove.gameObject.GetComponent<SenseGlove_KeyBinds>();
            }
            this.WriteInstr(this.GetInstructions());
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(this.nextObjKey))
            {
                this.ShowNextObject(true);
            }
            else if (Input.GetKeyDown(this.prevObjKey))
            {
                this.ShowNextObject(false);
            }

            if (Input.GetKeyDown(this.quitKey))
                Application.Quit();

            if (this.instrText != null)
            {
                this.WriteInstr(this.GetInstructions());
            }
        }

        /// <summary> Retrieve a a string containing the instructions to operate this demo. </summary>
        /// <returns></returns>
        private string GetInstructions()
        {
            if (this.senseGlove != null && this.senseGlove.GloveReady)
            {
                string res = this.prevObjKey.ToString() + " / " + this.nextObjKey.ToString() + " to cycle through objects" + "\r\n";

                if (this.senseGlove != null && this.senseGlove.IsCalibrating)
                {
                    SenseGlove_Data data = this.senseGlove.GloveData;
                    res += "Calibrating: Gathered " + data.calibrationStep + " / " + data.totalCalibrationSteps + " points.\r\n";
                    if (this.keyBinds != null)
                    {
                        res += this.keyBinds.cancelCalibrationKey.ToString() + " to cancel.";
                    }
                    return res;
                }
                else if (this.keyBinds != null)
                {
                    res += this.keyBinds.calibrateHandKey + " to start calibration.\r\n";
                    return res + this.keyBinds.calibrateWristKey.ToString() + " to calibrate wrist.";
                }
                return res + "LeftShift / T(humb) to start calibration.";
            }
            return "Waiting to connect to a\r\nSense Glove";
        }

        /// <summary> Show the next object in the list of GameObjects </summary>
        /// <param name="forward">whether to go forward or backward</param>
        private void ShowNextObject(bool forward)
        {
            //hide the current object
            this.SetObject(index, false);

            //move to the next one.
            index = forward ? index + 1 : index - 1;
            if (index >= this.objectsToSwap.Count) { index = -1; }
            if (index < -1) { index = this.objectsToSwap.Count - 1; }

            //show the next.
            this.SetObject(index, true);

            //show its name on the debugText
            if (index >= 0) { this.WriteObj(this.objectsToSwap[index].name); }
            else { this.WriteObj(""); }
        }

        /// <summary> Set the object at the specified index active or inactive. </summary>
        /// <param name="i"></param>
        /// <param name="active"></param>
        private void SetObject(int i, bool active)
        {
            if (index >= 0 && index < this.objectsToSwap.Count)
            {
                this.objectsToSwap[i].SetActive(active);
                SenseGlove_Material mat = this.objectsToSwap[i].GetComponent<SenseGlove_Material>();
                if (mat) { mat.UnBreak(); }

                SenseGlove_Breakable breakable = this.objectsToSwap[i].GetComponent<SenseGlove_Breakable>();
                if (breakable) { breakable.UnBreak(); }
            }
        }

        private void WriteObj(string msg)
        {
            if (this.objectText != null)
            {
                this.objectText.text = msg;
            }
        }

        private void WriteInstr(string msg)
        {
            if (this.instrText != null)
            {
                this.instrText.text = msg;
            }
        }

    }

}
