
using SGCore;
using SGCore.Haptics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Examples
{

	public class SGEx_GloveDiagnostics : MonoBehaviour
	{

		//[Header("Visual Components")]
		//public SG_HapticGlove glove;
		public SG.Util.SG_WireFrame wireFrame;
		//      public SG_CalibrationSequence calibrationSequence;
		//public SG_TrackedHand leftHandModel, rightHandModel;

		public SGEx_SelectHandModel handSelector;

		[Header("UI Components")]
		public Text titleText;
		public SG_InputSlider[] fingerFFB = new SG_InputSlider[0];
		public SG_InputSlider[] fingerVibration = new SG_InputSlider[0];
		public SG_InputSlider thumperVibration;
		public GameObject SGThumperVibration;
		public SGEx_ShowGloveAngles gloveAngleWindow;

		public Button ffbOn, ffbOff, toggleFFB, buzzOn, buzzOff, toggleBuzz;
		public Button sgThumpTest;
		public Dropdown sgThumpDDn;


		[Header("KeyBinds")]
		public KeyCode resetWristKey = KeyCode.P;
		public KeyCode testThumperKey = KeyCode.T;
		public KeyCode resetCalibrKey = KeyCode.C;
		public KeyCode testBuzzKey = KeyCode.B;
		public KeyCode testFFbKey = KeyCode.F;


		SGCore.HapticGlove hapticGlove = null;
		SGCore.Haptics.SG_FFBCmd ffbCmd = SG_FFBCmd.Off;
		SGCore.Haptics.SG_BuzzCmd buzzCmd = SG_BuzzCmd.Off;
		SGCore.Haptics.ThumperCmd thumprCmd = ThumperCmd.Off;
		//bool setup = false;
		bool sComRuns = false;
		int sgThump = 0;
		string thumpKey = "sgThump";

		private static readonly SGCore.Haptics.SG_ThumperCmd[] thumpersAvailable = new SG_ThumperCmd[]
		{
			SGCore.Haptics.SG_ThumperCmd.Impact_Thump_100,
			SGCore.Haptics.SG_ThumperCmd.Impact_Thump_30,
			SGCore.Haptics.SG_ThumperCmd.Impact_Thump_10,
			SGCore.Haptics.SG_ThumperCmd.Button_Double_100,
			SGCore.Haptics.SG_ThumperCmd.Button_Double_60,
			SGCore.Haptics.SG_ThumperCmd.Object_Grasp_100,
			SGCore.Haptics.SG_ThumperCmd.Object_Grasp_60,
			SGCore.Haptics.SG_ThumperCmd.Object_Grasp_30,
			SGCore.Haptics.SG_ThumperCmd.Cue_Game_Over
		};


		public bool FFBEnabled
        {
			get
            {
				for (int f = 0; f < this.fingerFFB.Length; f++)
				{
					if (fingerFFB[f].SlideValue < 100) { return false; }
				}
				return true;
			}
			set
            {
				int magn = value ? 100 : 0;
				for (int f = 0; f < this.fingerFFB.Length; f++)
				{
					fingerFFB[f].SlideValue = magn;
				}
			}
        }

		public bool VibroEnabled
        {
			get
			{
				for (int f = 0; f < this.fingerVibration.Length; f++)
				{
					if (fingerVibration[f].SlideValue < 100) { return false; }
				}
				return thumperVibration.SlideValue >= 100; //also check these
			}
			set
			{
				int magn = value ? 100 : 0;
				for (int f = 0; f < this.fingerVibration.Length; f++)
				{
					fingerVibration[f].SlideValue = magn;
				}
				thumperVibration.SlideValue = magn;
			}
		}


		public void EnableAllFFB()
        {
			FFBEnabled = true;
        }

		public void DisableAllFFB()
        {
			FFBEnabled = false;
		}

		public void EnableAllVibro()
        {
			VibroEnabled = true;
		}

		public void DisableAllVibro()
        {
			VibroEnabled = false;
		}

		public void ToggleFFB()
        {
			FFBEnabled = !FFBEnabled;
        }

		public void ToggleBuzz()
        {
			VibroEnabled = !VibroEnabled;
        }


		public void CalibrateIMU()
        {
			SG_HapticGlove glove = this.handSelector.ActiveGlove;
			if (glove != null)
			{
				Quaternion IMU;
				if (glove.GetIMURotation(out IMU))
				{
					this.handSelector.rightHand.handAnimation.CalibrateWrist(IMU);
					this.handSelector.leftHand.handAnimation.CalibrateWrist(IMU);
				}
			}
			if (wireFrame != null) { this.wireFrame.CalibrateWrist(); }
        }

		private void SetupAfterConnect()
        {
            //Show the right HandModels

            if (wireFrame != null)
            {
				wireFrame.SetTrackedGlove(this.handSelector.ActiveGlove);
                //wireFrame.ResizeFingers(this.handSelector.ActiveGlove.GetKinematics().FingerLengths);
                wireFrame.HandVisible = true;
            }

            SG_HapticGlove glove = this.handSelector.ActiveGlove;
            hapticGlove = (SGCore.HapticGlove)glove.InternalGlove;
			if (hapticGlove != null)
            {
				if (hapticGlove.GetDeviceType() == SGCore.DeviceType.NOVA)
                {   //disable some fino things because I am a bastard
					if (this.fingerFFB.Length > 4) { this.fingerFFB[4].gameObject.SetActive(false); }
					
					if (this.fingerVibration.Length > 2) { this.fingerVibration[2].gameObject.SetActive(false); }
					if (this.fingerVibration.Length > 3) { this.fingerVibration[3].gameObject.SetActive(false); }
					if (this.fingerVibration.Length > 4) { this.fingerVibration[4].gameObject.SetActive(false); }
                }
				else if (hapticGlove.GetDeviceType() == SGCore.DeviceType.SENSEGLOVE)
                {
					this.thumperVibration.gameObject.SetActive(false);
					this.SGThumperVibration.SetActive(true);
                }
            }
			titleText.text = "Connected to " + hapticGlove.GetDeviceID() + " running firmware v"
				+ hapticGlove.GetFirmwareVersion() + "." + hapticGlove.GetSubFirmwareVersion();

			//Hardware
			this.CalibrateIMU();
		}

		private void DDValueChanged(int value)
		{
			this.sgThump = value;
			PlayerPrefs.SetInt(thumpKey, sgThump);
		}


		public void TestThumper() //when someone presses T
		{
			if (hapticGlove != null)
			{

			}
		}

		public void TestThumperSG()
		{
			if (hapticGlove != null && hapticGlove is SGCore.SG.SenseGlove)
			{
				SGCore.Haptics.SG_ThumperCmd cmd = thumpersAvailable[sgThump];
				((SGCore.SG.SenseGlove)hapticGlove).QueueWristCommand(cmd);
				hapticGlove.SendHaptics(); //immedeately sedn
			}
		}

		public void ResetCalibration()
        {
			//if (this.calibrationSequence != null)
			//{
			//	this.calibrationSequence.StartCalibration(true);
			//}
			if (this.handSelector.ActiveHand != null)
            {
				this.handSelector.ActiveHand.calibration.StartCalibration(false);
            }
        }


		public void ActiveHandConnects()
        {
			SetupAfterConnect();
			gloveAngleWindow.senseGlove = this.handSelector.ActiveGlove;
		}

		public void ActiveHandDisconnects()
		{
			//Set Sliders to 0
			for (int f = 0; f < fingerFFB.Length; f++)
			{
				this.fingerFFB[f].SlideValue = 0;
			}
			for (int f = 0; f < fingerVibration.Length; f++)
			{
				this.fingerVibration[f].SlideValue = 0;
			}
			gloveAngleWindow.senseGlove = null;
		}


		// Use this for initialization
		void Start()
		{

			if (wireFrame != null) { wireFrame.HandVisible = false; }

			this.handSelector.leftHand.overrideWristLocation = true;
			this.handSelector.leftHand.handAnimation.imuForWrist = true;
			this.handSelector.rightHand.overrideWristLocation = true;
			this.handSelector.rightHand.handAnimation.imuForWrist = true;

			this.handSelector.ActiveHandConnect.AddListener(ActiveHandConnects);
			this.handSelector.ActiveHandDisconnect.AddListener(ActiveHandDisconnects);

			for (int f = 0; f < fingerFFB.Length; f++)
            {
				this.fingerFFB[f].Title = ((SGCore.Finger)f).ToString();
            }
			for (int f = 0; f < fingerVibration.Length; f++)
			{
				this.fingerVibration[f].Title = ((SGCore.Finger)f).ToString();
			}
			this.thumperVibration.Title = "Wrist";

			ffbOn.onClick.AddListener(EnableAllFFB);
			ffbOff.onClick.AddListener(DisableAllFFB);
			toggleFFB.onClick.AddListener(ToggleFFB);

			buzzOn.onClick.AddListener(EnableAllVibro);
			buzzOff.onClick.AddListener(DisableAllVibro);
			toggleBuzz.onClick.AddListener(ToggleBuzz);

			SGThumperVibration.SetActive(true);
			sgThumpDDn.ClearOptions();
			for (int i=0; i<thumpersAvailable.Length; i++)
            {
				sgThumpDDn.options.Add(new Dropdown.OptionData( thumpersAvailable[i].ToString() ) );
			}
			sgThump = PlayerPrefs.GetInt(thumpKey, 0);
			sgThumpDDn.value = 9;
			sgThumpDDn.value = sgThump;
			sgThumpDDn.onValueChanged.AddListener(DDValueChanged);
			sgThumpTest.onClick.AddListener(TestThumperSG);
			SGThumperVibration.SetActive(false);

			sComRuns = SGCore.DeviceList.SenseCommRunning();
			titleText.text = sComRuns ? "Awaiting connection with glove..." : "SenseCom isn't running, so no glove will be detected!";
			titleText.color = sComRuns ? Color.white : Color.red;

			gloveAngleWindow.senseGlove = null;
		}

		// Update is called once per frame
		void Update()
		{
			if (!sComRuns)
            {
				bool nowRunning = SGCore.DeviceList.SenseCommRunning();
				if (!sComRuns && nowRunning)
                {
					titleText.color = Color.white;
					titleText.text = "Awaiting connection to glove...";
                }
				sComRuns = nowRunning;
			}
			else
            {
				if (hapticGlove != null)
				{
					if (hapticGlove.IsConnected())
					{
						titleText.text = "Connected to " + hapticGlove.GetDeviceID() + " running firmware v"
							+ hapticGlove.GetFirmwareVersion() + "." + hapticGlove.GetSubFirmwareVersion() + "\r\n"
							+ "Receiving " + hapticGlove.PacketsPerSecondReceived() + " packets/s";


						//collect ffb and send.
						float[] ffb = new float[this.fingerFFB.Length];
						for (int f = 0; f < this.fingerFFB.Length; f++)
						{
							ffb[f] = fingerFFB[f].SlideValue / 100.0f;
						}
						//ffbCmd = new SG_FFBCmd(ffb);

						float[] buzz = new float[this.fingerVibration.Length];
						for (int f = 0; f < this.fingerVibration.Length; f++)
						{
							buzz[f] = (int)fingerVibration[f].SlideValue / 100.0f;
						}
						//buzzCmd = new SG_BuzzCmd(buzz);

						//thumprCmd = new ThumperCmd((int)thumperVibration.SlideValue);
						float wrist = thumperVibration.SlideValue / 100.0f;
						hapticGlove.QueueFFBLevels(ffb);
						hapticGlove.QueueVibroLevels(buzz);
						if (hapticGlove is SGCore.Nova.NovaGlove)
                        {
							((SGCore.Nova.NovaGlove)hapticGlove).QueueWristLevel(wrist);
                        }
						hapticGlove.SendHaptics();

						//update wrist IMU
						if (handSelector.ActiveHand != null && handSelector.ActiveGlove != null)
                        {
							Quaternion imu;
							if (handSelector.ActiveGlove.GetIMURotation(out imu))
                            {
								handSelector.ActiveHand.handAnimation.UpdateWrist(imu);
							}
                        }
					}
					else
                    {
						titleText.text = hapticGlove.GetDeviceID() + " is no longer connected...";
                    }
				}

				//KeyBinds
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
				if (Input.GetKeyDown(this.resetWristKey))
                {
					this.CalibrateIMU();
                }
				if (Input.GetKeyDown(this.testBuzzKey))
				{
					this.VibroEnabled = !this.VibroEnabled;
				}
				if (Input.GetKeyDown(this.testFFbKey))
				{
					this.FFBEnabled = !this.FFBEnabled;
				}
				if (Input.GetKeyDown(this.testThumperKey))
				{
					this.TestThumperSG();
				}
				if (Input.GetKeyDown(this.resetCalibrKey))
				{
					this.ResetCalibration();
				}
#endif
			}
		}

		void OnApplicationQuit()
        {
			if (this.hapticGlove != null)
            {
				this.hapticGlove.StopHaptics(); //end Haptics(!)
            }
        }
		
	}
}