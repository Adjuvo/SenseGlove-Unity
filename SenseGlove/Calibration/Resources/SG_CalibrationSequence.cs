using UnityEngine;
using UnityEngine.UI;
using SenseGloveCs.Kinematics;

namespace SG.Calibration
{
    /// <summary> Manobehaviour meant to run the user though two general calibration steps, and then allows them to refine their calibration </summary>
    public class SG_CalibrationSequence : MonoBehaviour
    {
        /// <summary> Stage of the Calibration sequence </summary>
        public enum CalStage
        {
            AwaitConnection, //waiting for SG etc
            GlobalCalibration, //open hand, closed fist
            LastRefinement, //try it out, refine if needed
            Saved //done & actually applied
        }

        /// <summary> Calibration poses, used to access SG_CalPoses in an array. </summary>
        public enum CalPose
        {
            FingersExt = 0,
            FingersFlexed,
            ThumbUp,
            ThumbFlex,
            AbdOut,
            HandOpen,
            HandClosed,
            NoThumbAbd,
            All
        }


        /// <summary> Wireframe model used to show the glove and access hardware. </summary>
        public SG_WireFrame wireFrame;
        /// <summary> Stage of calibration we are currently in </summary>
        public CalStage stage = CalStage.AwaitConnection;
        /// <summary> Sub-stage of calibration. Used for general calibration. </summary>
        public int subStage = 0;



        /// <summary> UI element for general instructions. </summary>
        [Header("UI Elements")]
        public Text instrText;
        /// <summary> Menu containing refinement steps for calibration, which is disabled at start. </summary>
        public GameObject refinementMenu;
        /// <summary> Text of a "Calibrate Current Step" button, that can be altered </summary>
        public Text generalButtonTxt;
        /// <summary> Button to skip the current calibration step, only available during general calibration </summary>
        public Button skipButton;
        /// <summary> Button to save calibration. Is disabled until changes are detected. </summary>
        public Button saveButton;

        /// <summary> Popup to show if there are unsavedChanges </summary>
        public GameObject endPopup;

        /// <summary> Groups of GameObjects that show example poses of the hand </summary>
        public GameObject openHandEx, closedHandEx;

        /// <summary> HotKey for calibrating the current global step. </summary>
        [Header("KeyBinds")]
        public KeyCode nextStepKey = KeyCode.Space;


        /// <summary> GameObject hidden until the SenseGlove is connected </summary>
        [Header("3D Models")]
        public GameObject[] hiddenBeforeStart = new GameObject[0];
        /// <summary> HandModels for either a left or right hand, to move along the wireframe </summary>
        public GameObject leftAnimation, rightAnimation;
        /// <summary> All GameObjects that represent a right hand, to be mirrored when a left hand is detected. </summary>
        public Transform[] rightHands = new Transform[0];

        /// <summary> True if changes are detected, used to check when exiting. </summary>
        private bool changes = false;
        /// <summary> Interpolator clone of the Glove, which is updated and applied when calibrating. </summary>
        private InterpolationSet_IMU interpolator = null;
        /// <summary> Base instruction text to add on top of other instructions </summary>
        private string baseTxt = "";
        /// <summary> Calibration poses used to calibate the interpolator </summary>
        private CalibrationPose[] poses = new CalibrationPose[0];



        /// <summary> Generates SG_CalibrationPoses for this interpolator. </summary>
        /// <param name="intepolator"></param>
        private void LoadProfiles(InterpolationSet_IMU intepolator)
        {
            poses = new CalibrationPose[(int)CalPose.All];
            poses[(int)CalPose.HandOpen] = CalibrationPose.GetFullOpen(ref intepolator);
            poses[(int)CalPose.HandClosed] = CalibrationPose.GetFullFist(ref intepolator);
            poses[(int)CalPose.FingersExt] = CalibrationPose.GetOpenHand(ref intepolator);
            poses[(int)CalPose.FingersFlexed] = CalibrationPose.GetFist(ref intepolator);
            poses[(int)CalPose.ThumbUp] = CalibrationPose.GetThumbsUp(ref intepolator);
            poses[(int)CalPose.ThumbFlex] = CalibrationPose.GetThumbFlexed(ref intepolator);
            poses[(int)CalPose.AbdOut] = CalibrationPose.GetThumbAbd(ref intepolator);
            poses[(int)CalPose.NoThumbAbd] = CalibrationPose.GetThumbNoAbd(ref intepolator);
        }


        /// <summary> Get Calibration Values from the hardware, as the interpolation solver would. </summary>
        /// <returns></returns>
        public Vector3[] GetCalibrationValues()
        {
            float[][] rawAngles = wireFrame.senseGlove.GloveData.gloveValues;
            float[][] Nsensors = Interp4Sensors.NormalizeAngles(rawAngles);
            Vect3D[][] inputAngles = SenseGloveModel.ToGloveAngles(Nsensors);

            Vector3[] res = new Vector3[5];
            for (int f = 0; f < inputAngles.Length; f++)
            {
                res[f] = Vector3.zero;
                for (int j = 0; j < inputAngles.Length; j++)
                {
                    res[f] += new Vector3(inputAngles[f][j].x, inputAngles[f][j].y, inputAngles[f][j].z);
                }
            }
            return res;
        }

        /// <summary> Calibrate the interpolator with a specified pose </summary>
        /// <param name="poseIndex"></param>
        public void CalibratePose(int poseIndex)
        {
            if (interpolator != null && poseIndex > -1 && poseIndex < poses.Length)
            {
                Vector3[] handAngles = GetCalibrationValues();
                poses[poseIndex].CalibrateParameters(handAngles, ref this.interpolator);
                wireFrame.senseGlove.SetInterpolationProfile(interpolator);
                //Debug.Log("Calibrated " + ((CalPose)poseIndex).ToString());
                changes = true;
                saveButton.interactable = true;
            }
        }

        /// <summary> Store calibration on disk so it may be used by other applications. </summary>
        public void SaveCalibration()
        {
            wireFrame.senseGlove.SaveHandCalibration();
            Debug.Log("Saved Calibration for " + (wireFrame.senseGlove.IsRight ? "right hand" : "left hand"));
            changes = false;
            saveButton.interactable = false;
        }

        /// <summary> Reset Sense Glove Calibration back to its default values. </summary>
        public void ResetCalibration()
        {
            Debug.Log("Reset Calibration");
            wireFrame.senseGlove.ResetKinematics();
            wireFrame.senseGlove.GetInterpolationProfile(out this.interpolator); //also update our own interpolator
            changes = true;
        }



        /// <summary> Start Global Calibration </summary>
        public void StartGlobal()
        {
            stage = CalStage.GlobalCalibration;
            subStage = 0;
            skipButton.gameObject.SetActive(true);
            GoToMainStage(subStage);
        }

        /// <summary> End global calibration </summary>
        public void EndGlobal()
        {
            subStage = 0;
            instrText.text = baseTxt + "Global Calibration is complete! Check the movements of the hand, and adjust them as needed.";
            generalButtonTxt.rectTransform.parent.gameObject.SetActive(false);
            stage = CalStage.LastRefinement;
            refinementMenu.SetActive(true);
            skipButton.gameObject.SetActive(false);
        }


        /// <summary> Go to a substage within the Global Calibration </summary>
        /// <param name="newStage"></param>
        protected void GoToMainStage(int newStage)
        {
            if (newStage == 0)
            {
                openHandEx.SetActive(true);
                instrText.text = baseTxt + "Ensure you have put on the glove properly.\r\nOpen your hand, then press the Calibration Button, or press "
                    + nextStepKey.ToString();
                generalButtonTxt.rectTransform.parent.gameObject.SetActive(true);
                refinementMenu.SetActive(false);
            }
            else if (newStage == 1)
            {
                openHandEx.SetActive(false);
                closedHandEx.SetActive(true);
                instrText.text = baseTxt + "Close your hand into a fist, then press the Calibration Button, or press " + nextStepKey.ToString();
            }
            else if (newStage == 2)
            {
                closedHandEx.SetActive(false);
                EndGlobal();
            }
        }

        /// <summary> Calibrate the current global calibration step </summary>
        public void CalibrateCurrentStep() //for buttons
        {
            if (subStage == 0)
            {
                CalibratePose((int)CalPose.HandOpen);
                subStage++;
                GoToMainStage(subStage);
            }
            else if (subStage == 1)
            {
                CalibratePose((int)CalPose.HandClosed);
                subStage++;
                GoToMainStage(subStage);
            }
        }

        /// <summary> Skip the current calibration step, without calibrating. </summary>
        public void SkipStep()
        {
            if (stage == CalStage.GlobalCalibration)
            {
                subStage++;
                GoToMainStage(subStage);
            }
        }



       

        /// <summary> Allow exiting of the application. </summary>
        public void RegularExit()
        {
            Application.Quit();
        }

        /// <summary> Save Calibration, then exit the application </summary>
        public void SaveAndExit()
        {
            this.SaveCalibration();
            RegularExit();
        }




        // Use this for initialization
        void Start()
        {
            instrText.text = "Awaiting connection with Sense Glove...";
            refinementMenu.SetActive(false);
            generalButtonTxt.rectTransform.parent.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            endPopup.SetActive(false);
            openHandEx.SetActive(false);
            closedHandEx.SetActive(false);
            leftAnimation.gameObject.SetActive(false);
            rightAnimation.gameObject.SetActive(false);
            for (int i = 0; i < hiddenBeforeStart.Length; i++)
            {
                hiddenBeforeStart[i].SetActive(false);
            }
        }

        void Update()
        {
            if (interpolator == null)
            {
                if (wireFrame.senseGlove.IsLinked && wireFrame.senseGlove.GetInterpolationProfile(out interpolator))
                {
                    baseTxt = "Calibrating the " + (wireFrame.senseGlove.IsRight ? "Right Hand" : "Left Hand") + "\r\n";
                    instrText.text = baseTxt;
                    if (!wireFrame.senseGlove.IsRight)
                    {
                        //its a left hand!
                        for (int i = 0; i < rightHands.Length; i++)
                        {
                            Vector3 S = rightHands[i].localScale;
                            S.z = S.z * -1;
                            rightHands[i].localScale = S;
                        }
                        leftAnimation.gameObject.SetActive(true);
                    }
                    else
                    {
                        rightAnimation.gameObject.SetActive(true);
                    }
                    for (int i = 0; i < hiddenBeforeStart.Length; i++)
                    {
                        hiddenBeforeStart[i].SetActive(true);
                    }
                    LoadProfiles(interpolator);
                    StartGlobal();
                }
            }
            else
            {
                if (stage == CalStage.GlobalCalibration)
                {
                    //Vector3[] gloveAngles = wireFrame.senseGlove.GloveData.TotalGloveAngles();
                    if (Input.GetKeyDown(nextStepKey))
                    {
                        CalibrateCurrentStep();
                    }
                }
                else if (stage == CalStage.LastRefinement)
                {

                }
                else
                {
                    //do something else?
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetCalibration();
            }
        }

        void OnApplicationQuit()
        {
            if (changes)
            {
#if UNITY_EDITOR
                Debug.Log("Quitting wihout having saved changes! So we're saving just in case");
                SaveCalibration(); //just saving the calibration
#else
                //nothing for now
#endif
                if (!endPopup.activeSelf)
                {
                    endPopup.SetActive(true);
                    Application.CancelQuit();
                } //else we are already shown it
            }

        }

    }
}