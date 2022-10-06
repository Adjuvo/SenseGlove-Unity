using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SG
{

	public class SG_CalibrationVoid : MonoBehaviour
	{
		public enum VoidStage
		{
			/// <summary> Waits for "Begin()" to be called before launching into calibration  </summary>
			WaitForStart,

			/// <summary> Wait until you've got the first glove. </summary>
			WaitingForFirst,
			/// <summary> Then, we wait X seconds for another Glove to connect. </summary>
			WaitingForSecond,
			/// <summary> Then, we start calibration with either 1 or 2 gloves. We start with some instructions </summary>
			Introduction,
			/// <summary> Collect data while the user moved their finger. Give them some extra encouragement </summary>
			MoveFingers,
			/// <summary> We have enough data to compile profiles, but are waiting for the user to confirm with a thumbs up </summary>
			ConfirmCalibration,
			/// <summary> We're finished. Compile profiles, congratulate the user, and move on. </summary>
			Done,
			/// <summary> When something goes wrong </summary>
			NoCalibrationError,
		}


		public SG_User user;
		public TextMesh mainInstructions;

		public VoidStage currStage = VoidStage.WaitingForFirst;
		public bool beginImmedeately = true;

		protected bool init = true;
		protected SG_CalibrationSequence leftCalibration = null, rightCalibration = null;
		protected bool calibratingLeft = false, calibratingRight = false;
		protected bool checkVRAssign = true;
		protected bool proceedToNextScene = false;

		public float timer_currStage = 0;


		public SG_XR_StablePanel[] vrUIElements = new SG_XR_StablePanel[0];
		public SG_HandAnimator exampleHandLeft, exampleHandRight;

		[Header("Init Settings")]
		public float timeForSecondGlove = 1.0f; //stop checking after X seconds.
		public SG_Waveform wf_connected;

		[Header("Intro Settings")]
		public float introTime = 2.5f;
		public AudioSource introAudio;

		[Header("MoveFingers Settings")]
		public AudioSource moveFingersAudio;
		public float openCloseTime = 1.0f; //open and close within this time(!).
		protected float openClose_freq = 1;
		protected float oc_lerp = 0; // 0  = open, 1 = closed

		SGCore.Kinematics.HandInterpolator lh_interp, rh_interp;
		public SG_Waveform wf_movedEnough;
		protected bool leftMoved = false, rightMoved = false;

		[Header("Confirmation Settings")]
		public AudioSource confirmAudio;
		public float confirmTime = 0.5f;
		public float[] thumbsUpThresholds = new float[5] { 0.2f, 0.7f, 0.7f, 0.7f, 0.7f };
		public float thumbPassThreshold = 0.1f;

		protected bool[] leftGest = new bool[5];
		protected bool[] rightGest = new bool[5];

		public SG_ConfirmZone resetSphere;

		/// <summary> ThumsUpTimes </summary>
		protected float leftTUTime = 0, rightTUTime = 0;
		public SG_Waveform wf_Confirmed;

		[Header("Completion Settings")]
		public AudioSource completionAudio;
		public float changeSceneAfter = 2.5f;

		public string goToSceneName = "";
		public int goToScene = -1;

		public UnityEvent CalibrationCompleted = new UnityEvent();


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



		public static void EnableGrasps(SG_TrackedHand hand, bool enabled)
		{
			if (hand != null && hand.grabScript != null) { hand.grabScript.GrabEnabled = enabled; }
		}

		public static void SetAnimation(SG_TrackedHand hand, bool enabled)
		{
			if (hand != null && hand.handAnimation != null)
			{
				hand.handAnimation.gameObject.SetActive(enabled);
				if (!enabled)
				{
					hand.handAnimation.UpdateHand(SG_HandPose.Idle(hand.TracksRightHand()));
				}
			}
		}

		public static void SetAssets(GameObject[] objects, bool enabled)
		{
			foreach (GameObject obj in objects)
			{
				if (obj != null) { obj.SetActive(enabled); }
			}
		}


		public static void SetCalibrationLayer(SG_TrackedHand hand, SG_CalibrationSequence.StartCondition condition)
		{
			if (hand != null && hand.calibration != null) { hand.calibration.startCondition = condition; }
		}

		public static bool GetCalibrationLayer(SG_TrackedHand hand, out SG_CalibrationSequence calibrationLayer)
		{
			calibrationLayer = hand != null && hand.calibration != null ? hand.calibration : null;
			return calibrationLayer != null;
		}

		public static void SetCalibrationChecks(SG_TrackedHand hand, bool checkOnStart)
		{
			if (hand != null && hand.calibration != null && hand.calibration.linkedGlove != null) { hand.calibration.linkedGlove.checkCalibrationOnConnect = checkOnStart; }
		}

		public static void DisableAwake(AudioSource audio)
		{
			if (audio != null) { audio.playOnAwake = false; }
		}

		/// <summary> Returns true if both calibration sequences are running </summary>
		/// <returns></returns>
		public bool MovedEnough()
		{
			int expected = 0;
			int actual = 0;
			if (leftCalibration != null && calibratingLeft)
			{
				expected++;
				//Debug.Log("Left: " + leftCalibration.CanAnimate);
				if (leftCalibration.CanAnimate)
				{
					if (!leftMoved && wf_movedEnough != null)
					{
						leftMoved = true;
						user.leftHand.SendCmd(wf_movedEnough);
					}
					actual++;
				}
			}
			if (rightCalibration != null && calibratingRight)
			{
				expected++;
				//Debug.Log("Right: " + leftCalibration.CanAnimate);
				if (rightCalibration.CanAnimate)
				{
					if (!rightMoved && wf_movedEnough != null)
					{
						rightMoved = true;
						user.rightHand.SendCmd(wf_movedEnough);
					}
					actual++;
				}
			}
			return expected > 0 && expected == actual;
		}


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

		public static void SetHandExample(SG_HandAnimator animater, bool enabled)
		{
			if (animater != null)
			{
				animater.enabled = false;
				if (animater.handModelInfo != null)
				{
					animater.handModelInfo.gameObject.SetActive(enabled);
				}
			}
		}

		public static void UpdateOpenClose(bool isRight, SG_HandAnimator animater, bool calibrating, SGCore.Kinematics.HandInterpolator poseInterp, float normalizedTime01)
		{
			if (animater != null && calibrating && poseInterp != null)
			{
				SGCore.Kinematics.Vect3D[][] handAngles = SGCore.Kinematics.Values.FillZero(5, 3);
				for (int f = 0; f < 5; f++)
				{
					if (f == 0)
					{
						handAngles[f][0].x = poseInterp.CalculateAngle(SGCore.Kinematics.ThumbMovement.T_CMC_Twist, normalizedTime01);
						handAngles[f][0].z = poseInterp.CalculateAngle(SGCore.Kinematics.ThumbMovement.T_CMC_Abd, normalizedTime01);
						handAngles[f][0].y = poseInterp.CalculateAngle(SGCore.Kinematics.ThumbMovement.T_CMC_Flex, normalizedTime01);
						handAngles[f][1].y = poseInterp.CalculateAngle(SGCore.Kinematics.ThumbMovement.T_MCP_Flex, normalizedTime01);
						handAngles[f][2].y = poseInterp.CalculateAngle(SGCore.Kinematics.ThumbMovement.T_IP_Flex, normalizedTime01);
					}
					else
					{
						SGCore.Finger finger = (SGCore.Finger)f;
						handAngles[f][0].z = poseInterp.CalculateAngle(finger, SGCore.Kinematics.FingerMovement.F_MCP_Abd, normalizedTime01);
						handAngles[f][0].y = poseInterp.CalculateAngle(finger, SGCore.Kinematics.FingerMovement.F_MCP_Flex, normalizedTime01);
						handAngles[f][1].y = poseInterp.CalculateAngle(finger, SGCore.Kinematics.FingerMovement.F_PIP_Flex, normalizedTime01);
						handAngles[f][2].y = poseInterp.CalculateAngle(finger, SGCore.Kinematics.FingerMovement.F_DIP_Flex, normalizedTime01);
					}
				}
				animater.UpdateHand(new SG_HandPose(SGCore.HandPose.FromHandAngles(handAngles, isRight)), true);
			}
		}

		public static void SetZone(SG_ConfirmZone zone, bool active)
		{
			if (zone != null)
			{
				zone.SetZone(active);
			}
		}

		public static void UpdateHand(SG_HandAnimator animater, bool calibrating, SG_HandPose pose)
		{
			if (animater != null && calibrating) { animater.UpdateHand(pose, true); }
		}

		public void SetupSequence()
		{
			if (user == null)
			{
				user = GameObject.FindObjectOfType<SG_User>();
			}

			SGCore.HandPose lh_openPose = SGCore.HandPose.FlatHand(false);
			SGCore.HandPose lh_closedPose = SGCore.HandPose.Fist(false);
			lh_interp = SGCore.Kinematics.HandInterpolator.BetweenPoses(lh_openPose, lh_closedPose);

			SGCore.HandPose rh_openPose = SGCore.HandPose.FlatHand(true);
			SGCore.HandPose rh_closedPose = SGCore.HandPose.Fist(true);
			rh_interp = SGCore.Kinematics.HandInterpolator.BetweenPoses(rh_openPose, rh_closedPose);


			//disable calibration on start. We're going to be the one's to control it.
			SetCalibrationChecks(user.leftHand, false);
			SetCalibrationChecks(user.rightHand, false);

			//They're not allowed to grab objects yet
			EnableGrasps(user.rightHand, false);
			EnableGrasps(user.leftHand, false);

			openClose_freq = 1 / openCloseTime;

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


			if (beginImmedeately)
			{
				GoToStage(VoidStage.WaitingForFirst);
			}
			else
			{
				GoToStage(VoidStage.WaitForStart);
			}

			//MainInstr = " Before we can begin, we need to calibrate your hand(s).";

			//currStage = VoidStage.WaitingForFirst;

			if (GetCalibrationLayer(user.leftHand, out leftCalibration)) //if true, a calibration algorithm has been assigned,
			{
				leftCalibration.calibrationType = SG_CalibrationSequence.CalibrationType.Quick;
				leftCalibration.startCondition = SG_CalibrationSequence.StartCondition.Manual;
			}
			else
			{
				Debug.LogError("Missing Calibration Layer for left hand!");
			}

			if (GetCalibrationLayer(user.rightHand, out rightCalibration))
			{
				rightCalibration.calibrationType = SG_CalibrationSequence.CalibrationType.Quick;
				rightCalibration.startCondition = SG_CalibrationSequence.StartCondition.Manual;
			}
			else
			{
				Debug.LogError("Missing Calibration Layer for right hand!");
				GoToStage(VoidStage.NoCalibrationError);
			}

			if (introAudio != null)
			{
				introTime = introAudio.clip.length + 0.5f;
				introAudio.playOnAwake = false;
			}
			DisableAwake(moveFingersAudio);
			DisableAwake(confirmAudio);
			DisableAwake(completionAudio);
		}


		public bool CheckConfirm(bool shouldCheck, SG_TrackedHand hand, SG_CalibrationSequence sequence, ref bool[] gest)
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

		public void CheckHeadsetAssign()
		{
			if (checkVRAssign) //do soemthign when headset is assigned.
			{
				if (user != null && user.vrRig != null)
				{
					checkVRAssign = false;
					//Debug.Log("Assigned Headset. Running init for instr");
					if (mainInstructions != null && user.vrRig.headTransfrom != null)
					{
						//TODO: place this text X m away from the head.
						for (int i = 0; i < vrUIElements.Length; i++)
						{
							vrUIElements[i].headTransform = user.vrRig.headTransfrom;
						}
					}
					//MainInstr = "LeftHand: " + (user.leftHand != null && user.leftHand.trackedObject != null ? user.leftHand.trackedObject.name : "NULL")
					//    + "\r\nRightHand: " + (user.rightHand != null && user.rightHand.trackedObject != null ? user.rightHand.trackedObject.name : "NULL");
				}
			}
		}


		public void SkipCalibration()
		{
			SkipCalibration(true);
			SkipCalibration(false);
		}


		public void SkipCalibration(bool ofRightHand)
		{
			if (currStage > VoidStage.WaitForStart) //I wont skip if we're waiting.
			{
				SG_CalibrationSequence calibrationLayer = ofRightHand ? rightCalibration : leftCalibration;
				if (calibrationLayer != null)
				{
					if (currStage == VoidStage.ConfirmCalibration && calibrationLayer.internalSequence != null
						&& calibrationLayer.internalSequence is SGCore.Calibration.HG_QuickCalibration)
					{
						Debug.Log("Attempting to still make something out of the calibration...");
						((SGCore.Calibration.HG_QuickCalibration)calibrationLayer.internalSequence).ManualCompleted = true; //bypass maually
					}
					else
					{
						calibrationLayer.CancelCalibration();
					}
				}
				if (ofRightHand)
				{
					calibratingRight = false;
				}
				else
				{
					calibratingLeft = false;
				}

				Debug.Log("Cancelled " + (ofRightHand ? "Right" : "Left") + " Hand Calibration");

				//if neither go to stage?
				if (!calibratingLeft && !calibratingRight && currStage != VoidStage.Done)
				{
					GoToStage(VoidStage.Done);
				}
			}
			else
			{
				GoToStage(VoidStage.WaitingForFirst); //starting up instead...
			}
		}

		public void ResetCalibrationRanges()
		{
			ResetCalibrationRange(true);
			ResetCalibrationRange(false);
		}

		public void ResetCalibrationRange(bool hand)
		{
			if (this.user != null)
			{
				ResetCalibrationRange(hand ? this.user.rightHand : this.user.leftHand);
			}
		}


		protected void ResetCalibrationRange(SG_TrackedHand hand)
		{
			if (this.currStage == VoidStage.ConfirmCalibration && hand != null && hand.calibration != null && hand.calibration.internalSequence != null)
			{
				Debug.Log("Resettign Range of " + hand.gameObject.name);
				hand.calibration.internalSequence.Reset();
				hand.SendCmd(this.wf_connected);
				GoToStage(VoidStage.MoveFingers);
			}
		}

		protected void GoToStage(VoidStage nextStage)
		{
			//do some cleanup(?)
			timer_currStage = 0; //reset timer
			currStage = nextStage;

			switch (currStage)
			{
				case VoidStage.WaitForStart:
					MainInstr = "When you're ready to begin\r\npress the trigger on your controller.";
					break;

				case VoidStage.WaitingForFirst:
					MainInstr = "Awaiting Connection to glove(s)...";
					break;
				case VoidStage.WaitingForSecond:
					MainInstr = "Awaiting Connection to glove(s)...";
					break;

				case VoidStage.Introduction:

					LoadNextScene_InBG();

					Debug.Log("Showing intro for " + introTime + "s");
					calibratingLeft = this.leftCalibration != null && this.user.LeftHandConnected;
					calibratingRight = this.rightCalibration != null && this.user.RightHandConnected;
					MainInstr = "Welcome to SenseGlove! It's time to calibrate";
					if (introAudio != null) { introAudio.Play(); }

					//Already initialize the sequence here already
					if (rightCalibration != null && calibratingRight)
					{
						StartCalibration(rightCalibration);
					}
					if (leftCalibration != null && calibratingLeft)
					{
						StartCalibration(leftCalibration);
					}

					SetAnimation(user.rightHand, true);
					SetAnimation(user.leftHand, true);

					break;

				case VoidStage.MoveFingers:

					SetZone(resetSphere, false);

					//turn these hands on if they aren't already
					SetHandExample(exampleHandLeft, calibratingLeft);
					SetHandExample(exampleHandRight, calibratingRight);

					//and update.
					UpdateOpenClose(false, exampleHandLeft, calibratingLeft, lh_interp, 0); // t==0
					UpdateOpenClose(true, exampleHandRight, calibratingRight, rh_interp, 0);

					if (calibratingLeft && calibratingRight) { MainInstr = "Open and close your real hands until the virtual hands begin to move"; }
					else { MainInstr = "Open and close your real hand until the virtual hand begins to move"; }

					break;

				case VoidStage.ConfirmCalibration:

					//turn these hands on if they aren't already, and have them making a thumbs up
					SetHandExample(exampleHandLeft, calibratingLeft);
					UpdateHand(exampleHandLeft, calibratingLeft, new SG_HandPose(SGCore.HandPose.ThumbsUp(false)));

					SetHandExample(exampleHandRight, calibratingRight);
					UpdateHand(exampleHandRight, calibratingRight, new SG_HandPose(SGCore.HandPose.ThumbsUp(true)));

					MainInstr = "When you are satified with the results, confirm with a Thumbs Up!";

					//Add a cancellation option

					if (!checkVRAssign && this.resetSphere != null)
					{
						Debug.Log("VR enabled, so we're adding a reset.");
						SetZone(resetSphere, true);
					}


					break;

				case VoidStage.Done:

					//turn these hands off if they aren't already
					SetHandExample(exampleHandLeft, false);
					SetHandExample(exampleHandRight, false);

					//restore original settings.
					SetAnimation(user.rightHand, true);
					SetAnimation(user.leftHand, true);
					EnableGrasps(user.rightHand, true);
					EnableGrasps(user.leftHand, true);

					SetZone(resetSphere, false);

					if (goToSceneName.Length > 0 || goToScene > -1)
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


		protected int TryLinkingGloves()
		{
			//this runs only once
			int linkedGloves = 0;
			if (user.LeftHandConnected)
			{
				if (!calibratingLeft && wf_connected != null)
				{
					user.leftHand.SendCmd(this.wf_connected); //if user's hand is connected, hand & gloveHardware is not NULL
				}
				calibratingLeft = true;
				linkedGloves++;
			}
			if (user.RightHandConnected)
			{
				if (!calibratingRight && wf_connected != null)
				{
					user.rightHand.SendCmd(this.wf_connected); //if user's hand is connected, hand & gloveHardware is not NULL
				}
				calibratingRight = true;
				linkedGloves++;
			}
			return linkedGloves;
		}

		protected void UpdateCurrentState()
		{
			switch (currStage)
			{
				case VoidStage.WaitForStart:

					CheckHeadsetAssign();
					if (user.vrRig != null && !user.vrRig.HasControllerEvents)
					{
						Debug.Log("VRHeadset has no controller buttons to push, so we're launching straight in.");
						GoToStage(VoidStage.WaitingForFirst);
					}
					break;


				case VoidStage.WaitingForFirst:

					CheckHeadsetAssign();
					if (user.ConnectedGloves > 0)
					{
						int linkedGloves = TryLinkingGloves();
						if (linkedGloves == 2)
						{
							Debug.Log("Linked two gloves, so going straight to Introduction");
							GoToStage(VoidStage.Introduction);
						}
						else if (linkedGloves == 1)
						{
							Debug.Log("Linked one gloves, checking for another for " + timeForSecondGlove.ToString() + " seconds");
							GoToStage(VoidStage.WaitingForSecond);
						}
					}

					break;
				case VoidStage.WaitingForSecond:

					CheckHeadsetAssign();
					//check for glove
					if (user.ConnectedGloves > 1)
					{
						int linkedGloves = TryLinkingGloves();
						if (linkedGloves == 2)
						{
							Debug.Log("Linked two gloves, so to Introduction");
							GoToStage(VoidStage.Introduction);
						}
					}
					else if (timer_currStage <= timeForSecondGlove)
					{
						timer_currStage += Time.deltaTime;
						if (timer_currStage >= timeForSecondGlove)
						{
							Debug.Log("Could not find a second glove. So going to the introduction");
							GoToStage(VoidStage.Introduction);
						}
					}

					break;

				case VoidStage.Introduction:

					CheckHeadsetAssign();
					//check if we've already moved enough
					if (MovedEnough())
					{
						GoToStage(VoidStage.ConfirmCalibration);
					}
					else if (timer_currStage <= introTime)
					{
						timer_currStage += Time.deltaTime;
						if (timer_currStage >= introTime)
						{
							Debug.Log("Introduction finished after. Going to moveFingers.");
							GoToStage(VoidStage.MoveFingers);
						}
					}

					break;

				case VoidStage.MoveFingers:

					timer_currStage += Time.deltaTime;
					oc_lerp = 0.5f + SG.Util.SG_Util.GetSine(openClose_freq, 0.5f, timer_currStage + (openCloseTime * 0.75f));

					//and update.
					UpdateOpenClose(false, exampleHandLeft, calibratingLeft, lh_interp, oc_lerp);
					UpdateOpenClose(true, exampleHandRight, calibratingRight, rh_interp, oc_lerp);


					//check if we've already moved enough
					if (MovedEnough())
					{
						GoToStage(VoidStage.ConfirmCalibration);
					}

					break;

				case VoidStage.ConfirmCalibration:


					//if thumbs are up for X seconds, we're off!

					bool leftConf = CheckConfirm(calibratingLeft, user.leftHand, leftCalibration, ref leftGest);
					bool rightConf = CheckConfirm(calibratingRight, user.rightHand, rightCalibration, ref rightGest);
					//MainInstr = "Confirm your calibration with a thumbs up! (" + leftConf + "/" + rightConf + ")";

					if (calibratingRight && rightCalibration.CalibrationActive)
					{
						if (rightConf)
						{
							rightTUTime += Time.deltaTime;

							//TODO: Update Right UI Element...

							if (rightTUTime >= confirmTime)
							{
								Debug.Log("Right Hand Calibration is done!");
								rightCalibration.internalSequence.ManualCompleted = true;
								if (wf_Confirmed != null) { user.rightHand.SendCmd(wf_Confirmed); }
							}
						}
						else { rightTUTime = 0; }
					}
					if (calibratingLeft && leftCalibration.CalibrationActive)
					{
						if (leftConf)
						{
							leftTUTime += Time.deltaTime;

							//TODO: Update LEFT UI Element...

							if (leftTUTime >= confirmTime)
							{
								Debug.Log("Left Hand Calibration is done!");
								leftCalibration.internalSequence.ManualCompleted = true;
								if (wf_Confirmed != null) { user.leftHand.SendCmd(wf_Confirmed); }
							}
						}
						else { leftTUTime = 0; }
					}


					bool rightFinished = calibratingRight ? !rightCalibration.CalibrationActive : true;
					bool leftFinished = calibratingLeft ? !leftCalibration.CalibrationActive : true;

					if (rightFinished && leftFinished)
					{
						GoToStage(VoidStage.Done);
					}

					break;

				case VoidStage.Done:

					if (timer_currStage <= changeSceneAfter)
					{
						timer_currStage += Time.deltaTime;
						if (timer_currStage >= changeSceneAfter)
						{
							proceedToNextScene = true;
						}
					}

					break;
			}
		}

		void LoadNextScene_InBG()
		{
			if (goToSceneName.Length > 0 || goToScene > -1)
			{
				StartCoroutine(LoadSceneAsynch());
			}
			else
			{
				Debug.Log("No Next Scene is specified, so no background loading in progress.");
			}
		}


		IEnumerator LoadSceneAsynch()
		{
			yield return null;

			AsyncOperation asyncOperation = this.goToSceneName.Length > 0 ? SceneManager.LoadSceneAsync(goToSceneName) : SceneManager.LoadSceneAsync(goToScene);
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


		public void ResetVoid()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		void Awake()
		{
			SetupSequence();
		}

		// Use this for initialization
		void Start()
		{
			if (this.resetSphere != null)
			{
				this.resetSphere.instructionsStayVisible = false;
				this.resetSphere.SetZone(false);
				this.resetSphere.InstructionText = "Retry\r\nCalibration";
			}
		}

		// Update is called once per frame
		void LateUpdate()
		{
			if (init)
			{
				init = false;
				//disable animation for now
				SetAnimation(user.leftHand, false);
				SetAnimation(user.rightHand, false);
				SetHandExample(exampleHandLeft, false);
				SetHandExample(exampleHandRight, false);
			}
			UpdateCurrentState();
		}

		private void OnEnable()
		{
			if (user != null)
			{
				if (user.leftHand != null && user.leftHand.feedbackLayer != null) { user.leftHand.feedbackLayer.gameObject.SetActive(false); }
				if (user.rightHand != null && user.rightHand.feedbackLayer != null) { user.rightHand.feedbackLayer.gameObject.SetActive(false); }
			}
			if (resetSphere != null)
			{
				resetSphere.OnConfirm.AddListener(ResetSphere_Activated);
			}
		}

		private void ResetSphere_Activated(SG_TrackedHand hand)
		{
			ResetCalibrationRange(hand);
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