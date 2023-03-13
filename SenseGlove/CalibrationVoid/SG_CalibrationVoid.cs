using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SG
{
	/// <summary> This Script runs up to two calibration sequences at a time, for a left and right hand. After completing, the void will transition to another scene. </summary>
	public class SG_CalibrationVoid : MonoBehaviour
	{
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Void Stage Enum

		public enum VoidStage
		{
			/// <summary> Waits for "Begin()" to be called before launching into calibration  </summary>
			WaitForStart,

			/// <summary> Wait until you've got the first glove. </summary>
			WaitingForFirstGlove,

			/// <summary> 1-2 gloves are calibration </summary>
			GlovesCalibrating,

			/// <summary>  1-2 gloves are done calibrating. Go to the next phase. </summary>
			Done,
			/// <summary> When something goes wrong </summary>
			NoCalibrationError,
		}



		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Single-Glove Calibration

		public enum GloveStage
		{
			/// <summary> This Glove is not (yet) connected. </summary>
			NotConnected,
			/// <summary> We're displaying the introduction for X seconds. </summary>
			Introduction,
			/// <summary> Collect data while the user moved their finger. Give them some extra encouragement </summary>
			MoveFingers,
			/// <summary> We have enough data to compile profiles, but are waiting for the user to confirm with a thumbs up </summary>
			ConfirmCalibration,
			/// <summary>  </summary>
			CalibrationDone,
		}



		/// <summary> Keeps track of a Calibration Sequence for one hand </summary>
		public class SG_IntegratedCalibration
		{
			/// <summary> Used for haptics and device recognition. </summary>
			public SG_TrackedHand Hand { get; set; }

			/// <summary> Calibration Algorithm linked to a single hand </summary>
			public SG_CalibrationSequence Algorithm { get; set; }

			/// <summary> Animation component of an example hand. </summary>
			public SG_HandAnimator ExampleHand { get; set; }

			/// <summary> The Glove Calibration Stage </summary>
			public GloveStage CalibrationStage { get; set; }

			/// <summary> Wafeform to play when the glove is first linked to the simulation. </summary>
			public SG_Waveform wf_connected { get; set; }

			/// <summary> Waveform that plays when the user has moved their hand enough  </summary>
			public SG_Waveform wf_movedEnough { get; set; }

			/// <summary> Fired when the user has their thumbs up for a set amount of time. </summary>
			public SG_Waveform wf_confirmed { get; set; }

			/// <summary> The amount of time we will show the introduction for. </summary>
			public float IntroTime { get; set; }

			/// <summary> How long one must keep their thumb up before we consider it 'done' </summary>
			public float ConfirmTime { get; set; }


			/// <summary> Introduction Timer - Maybe skipped if the hand has moved enough. </summary>
			protected float timer_intro;
			/// <summary> Timer to keep track of how long one's thumb is up. </summary>
			protected float timer_confirm;
			/// <summary> Frequency at which our example hand open/closes </summary>
			protected float openClose_freq;

			/// <summary> Keeps track whether or not the fingers have made a correct gesture for the thumb sup. If they have, we add an offset to our thresholds. </summary>
			protected bool[] confirmGest = new bool[5];



			/// <summary> Create a new instance of IntegratedCalibration. Extracts the hand and applies any required changes to the compeonents </summary>
			/// <param name="hand"></param>
			public SG_IntegratedCalibration(SG_TrackedHand hand, SG_HandAnimator example, float introTime, float confirmTime, float handOpenCloseFreq)
			{
				Hand = hand;
				Algorithm = hand.calibration;

				Algorithm.calibrationType = SG_CalibrationSequence.CalibrationType.Quick;
				Algorithm.startCondition = SG_CalibrationSequence.StartCondition.Manual;

				this.IntroTime = introTime;
				this.ConfirmTime = confirmTime;
				this.openClose_freq = handOpenCloseFreq;

				this.ExampleHand = example;
				SG_CalibrationVoid.SetHandExample(this.ExampleHand, false); //turn it off for now...



				GoToStage(GloveStage.NotConnected);
			}

			/// <summary> Send a waveform to the hand, but check if it's NULL just in case. </summary>
			/// <param name="wf"></param>
			public void SendWaveform(SG_Waveform wf)
			{
				if (wf != null) { Hand.SendCmd(wf); }
			}


			/// <summary> Go to your own internal stage </summary>
			/// <param name="stage"></param>
			public void GoToStage(GloveStage stage)
			{
				//Debug.Log(this.Hand.name + " going from " + this.CalibrationStage.ToString() + " to " + stage.ToString());
				this.CalibrationStage = stage;

				bool exampleEnabled = stage == GloveStage.MoveFingers || stage == GloveStage.ConfirmCalibration;
				SG_CalibrationVoid.SetHandExample(this.ExampleHand, exampleEnabled);

				if (stage == GloveStage.Introduction)
				{
					timer_intro = 0.0f;
				}
				else if (stage == GloveStage.ConfirmCalibration)
				{
					timer_confirm = 0.0f;

					//Set example to a nice enough thumbs up.

					SGCore.Kinematics.BasicHandModel handModel = this.Hand.GetHandModel();
					float[] flexions = new float[5] { 0.0f, 1.0f, 1.0f, 1.0f, 1.0f };
					float abd = 0.3f;
					SGCore.Kinematics.Vect3D[][] handAngles = SGCore.Kinematics.Anatomy.HandAngles_FromNormalized(handModel.IsRight, flexions, abd, 0.0f);
					SGCore.HandPose iPose = SGCore.HandPose.FromHandAngles(handAngles, handModel.IsRight, handModel);
					SG_HandPose confirmPose = new SG_HandPose(iPose);
					this.ExampleHand.UpdateHand(confirmPose, true);
				}
			}


			/// <summary> Update Glove State Logic </summary>
			/// <param name="dT"></param>
			public void UpdateGloveState(float dT)
			{
				if (CalibrationStage == GloveStage.CalibrationDone)
				{
					return; //no need to update anymore...
				}


				if (CalibrationStage == GloveStage.NotConnected)
				{
					//Check for a connection....
					if (Hand.IsConnected())
					{
						this.GoToStage(GloveStage.Introduction);
						SendWaveform(wf_connected);
						SG_CalibrationVoid.StartCalibration(this.Algorithm);
						return;
					}
				}

				if (this.Algorithm == null || this.Algorithm.internalSequence == null)
				{
					return;
				}

				if (CalibrationStage == GloveStage.Introduction || CalibrationStage == GloveStage.MoveFingers)
				{
					if (this.Algorithm.CanAnimate)
					{
						GoToStage(GloveStage.ConfirmCalibration);
						SendWaveform(wf_movedEnough);
					}
					if (CalibrationStage == GloveStage.Introduction)
					{
						timer_intro += dT;
						if (timer_intro >= IntroTime) //we've waited long enough. Let's move on to the MoveHands
						{
							GoToStage(GloveStage.MoveFingers);
						}
					}
					if (CalibrationStage == GloveStage.MoveFingers)
					{
						UpdateMoveFingersAnimation(); //update the example hand open/close position
					}
				}
				else if (CalibrationStage == GloveStage.ConfirmCalibration)
				{
					bool thumbsUp = SG_CalibrationVoid.CheckConfirm(true, Hand, Algorithm, ref this.confirmGest);
					if (thumbsUp)
					{
						timer_confirm += dT;
						if (timer_confirm >= this.ConfirmTime)
						{
							this.GoToStage(GloveStage.CalibrationDone);
							this.SendWaveform(wf_confirmed);
							Algorithm.internalSequence.ManualCompleted = true; //this will mark it as complete, and our Sequence will work fine.
						}
					}
					else
					{
						timer_confirm = 0.0f; //reset the timer.
					}
				}
			}

			/// <summary> Update the Example Hands animation so smoothly open/close. </summary>
			public void UpdateMoveFingersAnimation()
			{
				float animationEval = 0.5f + SG.Util.SG_Util.GetSine(openClose_freq, 0.5f, Time.timeSinceLevelLoad/* + (openCloseTime * 0.75f)*/);
				float[] allFlex = new float[5] { animationEval, animationEval, animationEval, animationEval, animationEval }; //all finger flexion is equal
				float abd = 0.6f;

				SGCore.Kinematics.BasicHandModel handModel = this.Hand.GetHandModel();
				SGCore.Kinematics.Vect3D[][] handAngles = SGCore.Kinematics.Anatomy.HandAngles_FromNormalized(handModel.IsRight, allFlex, abd, 0.0f);
				SGCore.HandPose iPose = SGCore.HandPose.FromHandAngles(handAngles, handModel.IsRight, handModel);

				SG_HandPose newPose = new SG_HandPose(iPose);
				this.ExampleHand.UpdateHand(newPose, true);
			}


			/// <summary> Skip the calibration step for this glove, cancelling anything active and setting the Stage to Done. </summary>
			public void SkipCalibration()
			{
				if (this.Algorithm.CalibrationActive)
				{
					this.Algorithm.CancelCalibration(); //cancel the calibration
				}
				Debug.Log(this.Hand.name + ": Skipping Calibration");
				this.GoToStage(GloveStage.CalibrationDone);
				this.SendWaveform(wf_confirmed); //let the user know we acknowledge their request
			}

			/// <summary> If we're not done yet, allow us to retry the calibration. </summary>
			public void RetryCalibration()
			{
				if (this.CalibrationStage != GloveStage.ConfirmCalibration) //we're only allowed to retry if the user still needs to confirm
				{
					Debug.Log("Cannot reset (yet) in the current Calibration Stage");
					return;
				}
				if (this.Algorithm.internalSequence != null)
				{
					Debug.Log(this.Hand.name + ": Resetting Calibration");
					this.Algorithm.internalSequence.Reset();
					this.GoToStage(GloveStage.MoveFingers);
				}
			}

		}


		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Member Variabeles


		/// <summary> Used to grab a lef- and right hand reference from. </summary>
		[Header("Calibration Components")]
		public SG_User user;
		/// <summary> The main Instructions to show to the user. </summary>
		public TextMesh mainInstructions;

		/// <summary> Example Handmodels that will be used to show the user how to move their hand. </summary>
		public SG_HandAnimator exampleHandLeft, exampleHandRight;

		/// <summary> (Optional) Sphere to reset the calibration with. </summary>
		public SG_ConfirmZone resetSphere;

		/// <summary> The "Main Stage" of the Calibration Void </summary>
		[Header("Control Parameters")]
		public VoidStage currStage = VoidStage.WaitingForFirstGlove;
		/// <summary> If false, we must wait for a controller press to begin calibration - provided the device you're using has Controllers. </summary>
		public bool beginImmedeately = true;

		/// <summary> The amount of time were we'll show a 'welcome' message, before one is requested to move their fingers. </summary>
		public float introTime = 1.5f;
		/// <summary> The amount of time one must hold a thumbs up before we confirm and close calibration. </summary>
		public float confirmTime = 0.5f;

		/// <summary> How Long it takes for the example hand to open and close again </summary>
		public float exampleOpenCloseTime = 2.0f;
		/// <summary> The Frequency (in Hz) of the example hand - how often per second does it open and close </summary>
		protected float openClose_freq = 1;

		/// <summary> We load the next scene in a Unity CoRoutine, but we're not opening it until this flag is set to true. </summary>
		protected bool proceedToNextScene = false;
		/// <summary> Timer to keep track of us going to the next Scene. </summary>
		protected float timer_toNextScene = 0.0f;

		/// <summary> Normalized flexion thresholds to make a 'thumbs up' gesture. For thumb (0), flexion must be smaller than. For fingers (1-4) flexion must be greater than </summary>
		public static float[] thumbsUpThresholds = new float[5] { 0.2f, 0.7f, 0.7f, 0.7f, 0.7f };
		/// <summary> Once your fingers pass above/below the threshold, it needs to move back over it by this much to cancel. </summary>
		public static float thumbPassThreshold = 0.1f;


		/// <summary> Once completed, the scene will automatically change after this much time has finished. </summary>
		[Header("Completion Settings")]
		public float changeSceneAfter = 1.5f;

		/// <summary> If the index > -1, we will change to this Scene after Calibration completes, unless a specific name is provided. </summary>
		public int goToSceneIndex = -1;
		/// <summary> If it's not empty, we will change to this scene after calibration compltes. </summary>
		public string goToSceneName = "";

		/// <summary> This Event fires when the Calibration is completed, but before the next scene is loaded. </summary>
		public UnityEvent CalibrationCompleted = new UnityEvent();

		/// <summary> Calibration algorithm for each glove. </summary>
		protected SG_IntegratedCalibration leftHandCal = null, rightHandCal = null;

		/// <summary> Waveform to send when the glove connects. </summary>
		[Header("Haptics")]
		public SG_Waveform wf_connected;
		/// <summary> Waveform to send when user has moved their fingers enough. </summary>
		public SG_Waveform wf_movedEnough;
		/// <summary> Waveform to send when the user confirms their calibration. </summary>
		public SG_Waveform wf_confirmed;


		/// <summary> Audio that will play once the first Glove connects </summary>
		[Header("Audio Assets")]
		public AudioSource introAudio;
		/// <summary> Audio that will play when the user is prompted to move their fingers. </summary>
		public AudioSource moveFingersAudio;
		/// <summary> Audio that playes when the user needs to give a thumb up. </summary>
		public AudioSource confirmAudio;
		/// <summary> Plays when all hands have been calibrated. </summary>
		public AudioSource completionAudio;

		/// <summary> Used to play Move Audio only once. </summary>
		protected bool playMoveAudio = true;
		/// <summary> Used to play confirm audio only once </summary>
		protected bool playConfirmAudio = true;


		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> Returns true if the CalibrationVoid is done. </summary>
		public bool Finished
		{
			get { return currStage >= VoidStage.Done; }
		}


		/// <summary> Access the main instructions text </summary>
		public string MainInstr
		{
			get { return mainInstructions != null ? mainInstructions.text : ""; }
			set { if (mainInstructions != null) { mainInstructions.text = value; } }
		}



		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// CalibrationVoid Functions

		// Setup / Utility

		/// <summary> Ensure the dev did not forget to turn off the playOnAwake </summary>
		/// <param name="audio"></param>
		public static void DisableAwake(AudioSource audio)
		{
			if (audio != null) { audio.playOnAwake = false; }
		}


		/// <summary> Enable / Disable an animator and its associated hand model. </summary>
		/// <param name="animator"></param>
		/// <param name="enabled"></param>
		public static void SetHandExample(SG_HandAnimator animator, bool enabled)
		{
			if (animator != null)
			{
				animator.enabled = false;
				if (animator.handModelInfo != null)
				{
					animator.handModelInfo.gameObject.SetActive(enabled);
				}
			}
		}

		/// <summary> Ensure that we are the ones checking a Hand's Calibration State, not the glvoe itself! </summary>
		/// <param name="hand"></param>
		/// <param name="checkOnStart"></param>
		public static void SetCalibrationChecks(SG_TrackedHand hand, bool checkOnStart)
		{
			if (hand != null && hand.calibration != null && hand.calibration.linkedGlove != null) { hand.calibration.linkedGlove.checkCalibrationOnConnect = checkOnStart; }
		}

		/// <summary> Safely set a ConfirmZone as active </summary>
		/// <param name="zone"></param>
		/// <param name="active"></param>
		public static void SetZone(SG_ConfirmZone zone, bool active)
		{
			if (zone != null)
			{
				zone.SetZone(active);
			}
		}

		/// <summary>  </summary>
		/// <param name="shouldCheck"></param>
		/// <param name="hand"></param>
		/// <param name="sequence"></param>
		/// <param name="gest"></param>
		/// <returns></returns>
		public static bool CheckConfirm(bool shouldCheck, SG_TrackedHand hand, SG_CalibrationSequence sequence, ref bool[] gest)
		{
			if (shouldCheck)
			{
				float[] normalizedFlexion;
				if (hand != null && hand.GetNormalizedFlexion(out normalizedFlexion))
				{
					bool allGood = true;
					//Debug.Log(SG.Util.SG_Util.ToString(normalizedFlexion));
					for (int f = 0; f < normalizedFlexion.Length && f < gest.Length && f < thumbsUpThresholds.Length; f++)
					{
						if (gest[f]) //finger is currently making a gesture
						{
							if (f == 0) { gest[f] = normalizedFlexion[f] < thumbsUpThresholds[f] + thumbPassThreshold; }
							else { gest[f] = normalizedFlexion[f] > thumbsUpThresholds[f] - thumbPassThreshold; }
						}
						else //finger is not yet in the right spot
						{
							if (f == 0) { gest[f] = normalizedFlexion[f] < thumbsUpThresholds[f]; }
							else { gest[f] = normalizedFlexion[f] > thumbsUpThresholds[f]; }
						}
						if (!gest[f]) { allGood = false; }
					}
					return allGood;
				}
				return false;
			}
			return true;
		}


		/// <summary> Attempt to retrieve the Calibration Layer from one of the hands </summary>
		/// <param name="hand"></param>
		/// <param name="calibrationLayer"></param>
		/// <returns></returns>
		public static bool GetCalibrationLayer(SG_TrackedHand hand, out SG_CalibrationSequence calibrationLayer)
		{
			calibrationLayer = hand != null && hand.calibration != null ? hand.calibration : null;
			return calibrationLayer != null;
		}



		public void CollectComponents()
		{
			DisableAwake(introAudio);
			DisableAwake(moveFingersAudio);
			DisableAwake(confirmAudio);
			DisableAwake(completionAudio);

			if (user == null)
			{
				user = GameObject.FindObjectOfType<SG_User>();
			}

			//disable calibration on start. We're going to be the one's to control it.
			SetCalibrationChecks(user.leftHand, false);
			SetCalibrationChecks(user.rightHand, false);

			openClose_freq = 1 / exampleOpenCloseTime;
		}




		public void SetupSequence()
		{
			//Do this regardless
			if (introAudio != null) //the introduction time should be at least as long as thea audio clip accompanying it (though you can override it).
			{
				introTime = Mathf.Max(introTime, introAudio.clip.length + 0.5f);
			}

			if (this.resetSphere != null)
			{
				this.resetSphere.instructionsStayVisible = false;
				this.resetSphere.SetZone(false); //zone it turned off at the start...
				this.resetSphere.InstructionText = "Retry\r\nCalibration";
			}

			//Check Scene Setup

			if (GetCalibrationLayer(user.leftHand, out SG_CalibrationSequence leftCalibration)) //if true, a calibration algorithm has been assigned,
			{
				this.leftHandCal = new SG_IntegratedCalibration(user.leftHand, this.exampleHandLeft, this.introTime, this.confirmTime, openClose_freq);
				leftHandCal.wf_connected = this.wf_connected;
				leftHandCal.wf_confirmed = this.wf_confirmed;
				leftHandCal.wf_movedEnough = this.wf_movedEnough;

				user.leftHand.feedbackLayer.gameObject.SetActive(false);
				user.leftHand.grabScript.gameObject.SetActive(false);
			}
			else
			{
				Debug.LogError("Missing Calibration Layer for left hand!");
			}

			if (GetCalibrationLayer(user.rightHand, out SG_CalibrationSequence rightCalibration))
			{
				this.rightHandCal = new SG_IntegratedCalibration(user.rightHand, this.exampleHandRight, this.introTime, this.confirmTime, openClose_freq);
				rightHandCal.wf_connected = this.wf_connected;
				rightHandCal.wf_confirmed = this.wf_confirmed;
				rightHandCal.wf_movedEnough = this.wf_movedEnough;

				user.rightHand.feedbackLayer.gameObject.SetActive(false);
				user.rightHand.grabScript.gameObject.SetActive(false);
			}
			else
			{
				Debug.LogError("Missing Calibration Layer for right hand!");
			}


			// Check for Errors
			if (leftHandCal == null || rightHandCal == null)
			{
				GoToMainStage(VoidStage.NoCalibrationError);
				return;
			}

			// Now that we know we're definitely calibrating...

			if (exampleHandLeft != null && user.leftHand != null)
			{
				Transform exHand = exampleHandLeft.transform.parent != null ? exampleHandLeft.transform.parent : exampleHandLeft.transform;
				SG_HandPoser3D poser = user.leftHand.GetPoser(SG_TrackedHand.TrackingLevel.RenderPose);
				exHand.parent = poser.GetTransform(HandJoint.Wrist);
				exHand.localRotation = Quaternion.identity;
				exHand.localPosition = Vector3.zero;
			}
			if (exampleHandRight != null && user.rightHand != null)
			{
				Transform exHand = exampleHandRight.transform.parent != null ? exampleHandRight.transform.parent : exampleHandRight.transform;
				SG_HandPoser3D poser = user.rightHand.GetPoser(SG_TrackedHand.TrackingLevel.RenderPose);
				exHand.parent = poser.GetTransform(HandJoint.Wrist);
				exHand.localRotation = Quaternion.identity;
				exHand.localPosition = Vector3.zero;
			}



			// Finally, go to the correct stage
			if (beginImmedeately)
			{
				GoToMainStage(VoidStage.WaitingForFirstGlove);
			}
			else
			{
				GoToMainStage(VoidStage.WaitForStart);
			}
		}


		// Calibration Controls


		/// <summary> Safely start a calibration scene </summary>
		/// <param name="sequence"></param>
		public static void StartCalibration(SG_CalibrationSequence sequence)
		{
			if (sequence != null)
			{
				sequence.StartCalibration(true);
				if (sequence.internalSequence != null && sequence.internalSequence is SGCore.Calibration.HG_QuickCalibration)
				{
					((SGCore.Calibration.HG_QuickCalibration)sequence.internalSequence).autoEndAfter = -1; //disable auto ending.
				}
			}
		}


		/// <summary> Event that fires when a user puts their hand inside the ResetSphere </summary>
		/// <param name="hand"></param>
		private void ResetSphere_Activated(SG_TrackedHand hand)
		{
			if (this.resetSphere != null && this.resetSphere.isActiveAndEnabled)
			{
				RetryCalibration();
			}
		}



		public void RetryCalibration()
		{
			RetryCalibration(true);
			RetryCalibration(false);
		}

		public void RetryCalibration(bool ofRightHand)
		{
			//Debug.Log("Re-Trying " + (ofRightHand ? "Right" : "Left") + " hand calibration");
			SG_IntegratedCalibration calibr = ofRightHand ? rightHandCal : leftHandCal;
			calibr.RetryCalibration();
		}


		public void SkipCalibration()
		{
			SkipCalibration(true);
			SkipCalibration(false);
		}


		public void SkipCalibration(bool ofRightHand)
		{
			//Debug.Log("Skipping " + (ofRightHand ? "Right" : "Left") + " hand calibration");
			SG_IntegratedCalibration calibr = ofRightHand ? rightHandCal : leftHandCal;
			calibr.SkipCalibration();
		}


		// Calibration Void Stages



		/// <summary> Reset the Calibration void Scene in its entirety. </summary>
		public void ResetVoid()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		/// <summary> Check the furthest stage out of both gloves, to update the MainInstr properly </summary>
		/// <returns></returns>
		public GloveStage GetFurthestStage()
		{
			int max = Mathf.Max((int)leftHandCal.CalibrationStage, (int)rightHandCal.CalibrationStage);
			return (GloveStage)max;
		}


		/// <summary> Go to the next stage of the CalibrationVoid. </summary>
		/// <param name="nextStage"></param>
		protected void GoToMainStage(VoidStage nextStage)
		{
			currStage = nextStage;

			SetZone(this.resetSphere, false); //turned off by default.

			switch (currStage)
			{
				case VoidStage.WaitForStart:
					MainInstr = "When you're ready to begin\r\npress the trigger on your controller.";
					break;

				case VoidStage.WaitingForFirstGlove:
					MainInstr = "Awaiting Connection to glove(s)...";
					break;

				case VoidStage.GlovesCalibrating: //when the first gloves have connected

					LoadNextScene_InBG(); //at this stage, we can start loading the next scene...

					//MainInstr = "Welcome to SenseGlove! It's time to calibrate";

					if (introAudio != null) { introAudio.Play(); }

					break;

				case VoidStage.Done:

					if (completionAudio != null) { completionAudio.Play(); }

					if (goToSceneName.Length > 0 || goToSceneIndex > -1)
					{
						MainInstr = "Done! Bringing you to the next stage...";
					}
					else
					{
						MainInstr = "Done!";
					}
					CalibrationCompleted.Invoke();

					break;
			}
		}



		/// <summary> Update the main CalibrationVoid Logic </summary>
		public void UpdateMainStage(float dT)
		{
			switch (currStage)
			{
				case VoidStage.WaitForStart: //we are currently waiting for the user to confirm button...

					bool isController;
					if (SG_XR_Devices.TryCheckForController(out isController) && !isController)
					{
						Debug.Log("VRHeadset has no controller buttons to push, so we're launching straight in.");
						GoToMainStage(VoidStage.WaitingForFirstGlove);
					}
					break;

				case VoidStage.WaitingForFirstGlove:

					//In this stage, we are waiting for any glove to actually connect
					if (this.GetFurthestStage() > GloveStage.NotConnected)
					{
						GoToMainStage(VoidStage.GlovesCalibrating);
						Debug.Log("There is at least one glove connected. Let's go!");
					}
					break;

				case VoidStage.GlovesCalibrating:

					//You're allowed to Complete, but only if Both Gloves are no longer calibrating...
					if ((leftHandCal.CalibrationStage == GloveStage.CalibrationDone || leftHandCal.CalibrationStage == GloveStage.NotConnected)
						&& (rightHandCal.CalibrationStage == GloveStage.CalibrationDone || rightHandCal.CalibrationStage == GloveStage.NotConnected))
					{
						if (!leftHandCal.Algorithm.CalibrationActive && !rightHandCal.Algorithm.CalibrationActive)
						{
							Debug.Log("Both Gloves are either done calibrating, or were not connected. Let's Finish this!");
							GoToMainStage(VoidStage.Done);
						}
					}
					else
					{
						GloveStage furthestStage = this.GetFurthestStage();
						//if at least one of the hands is moving. otherwise, it stays off.
						SetZone(this.resetSphere, furthestStage > GloveStage.MoveFingers);

						if (playConfirmAudio && furthestStage == GloveStage.ConfirmCalibration)
						{
							playConfirmAudio = false;
							playMoveAudio = false;
							if (confirmAudio != null) { confirmAudio.Play(); }
						}
						else if (playMoveAudio && furthestStage == GloveStage.MoveFingers)
						{
							playMoveAudio = false;
							if (moveFingersAudio != null) { moveFingersAudio.Play(); }
						}
					}
					break;

				case VoidStage.Done:

					timer_toNextScene += dT;
					if (timer_toNextScene >= changeSceneAfter)
					{
						this.proceedToNextScene = true; //Allows the next level to be loaded (if one is queued. Otherwise, we don't care).
					}
					break;
			}
		}



		/// <summary> Update the current states of the void and individual Glove calibration. </summary>
		/// <param name="dT"></param>
		protected void UpdateCurrentState(float dT)
		{
			if (leftHandCal == null || rightHandCal == null)
			{
				Debug.Log("No Calibration Algorithms for the left/right hands!");
				return;
			}

			this.leftHandCal.UpdateGloveState(dT);
			this.rightHandCal.UpdateGloveState(dT);

			UpdateMainStage(dT);

			if (currStage == VoidStage.GlovesCalibrating) //UpdateInstructions
			{
				//UpdateMainUI
				GloveStage furthestStage = GetFurthestStage();
				if (furthestStage == GloveStage.Introduction)
				{
					MainInstr = "Welcome to SenseGlove! It's time to calibrate";
				}
				else if (furthestStage == GloveStage.MoveFingers)
				{
					MainInstr = "Open and close your real hands until the virtual hands begin to move. Don't forget your Thumb!";
				}
				else if (furthestStage == GloveStage.ConfirmCalibration)
				{
					MainInstr = "When you are satified with the results, confirm with a Thumbs Up!";
				}
			}
		}

		/// <summary> Start loading the next scene in the background, if one is desired. </summary>
		void LoadNextScene_InBG()
		{
			if (goToSceneName.Length > 0 || goToSceneIndex > -1)
			{
				StartCoroutine(LoadSceneAsynch());
			}
			else
			{
				Debug.Log("No Next Scene is specified, so no background loading in progress.");
			}
		}


		// Asynch / Background Loading

		/// <summary> Worker routine to begin loading the next scene. </summary>
		/// <returns></returns>
		IEnumerator LoadSceneAsynch()
		{
			yield return null;

			AsyncOperation asyncOperation = this.goToSceneName.Length > 0 ? SceneManager.LoadSceneAsync(goToSceneName) : SceneManager.LoadSceneAsync(goToSceneIndex);
			if (asyncOperation != null)
			{
				//Don't let the Scene activate until you allow it to
				asyncOperation.allowSceneActivation = false;
				//When the load is still in progress, output the Text and progress bar
				while (!asyncOperation.isDone)
				{
					// Check if the load has finished
					if (asyncOperation.progress >= 0.9f)
					{
						//Wait to you press the space key to activate the Scene
						if (proceedToNextScene)
							//Activate the Scene
							asyncOperation.allowSceneActivation = true;
					}
					yield return null;
				}
			}
		}


		/// <summary> Brings you to the next scene regardless of Calibration stages. </summary>
		public void GoToNextSceneNow()
		{
			proceedToNextScene = true;
		}




		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Monobehaviour

		void Awake()
		{
			CollectComponents();
		}

		// Use this for initialization
		void Start()
		{
			SetupSequence();
		}

		// Update is called once per frame
		void LateUpdate()
		{
			UpdateCurrentState(Time.deltaTime);
		}

		private void OnEnable()
		{
			if (resetSphere != null)
			{
				resetSphere.OnConfirm.AddListener(ResetSphere_Activated);
			}
		}


		private void OnDisable()
		{
			if (resetSphere != null)
			{
				resetSphere.OnConfirm.RemoveListener(ResetSphere_Activated);
			}
		}

	}
}