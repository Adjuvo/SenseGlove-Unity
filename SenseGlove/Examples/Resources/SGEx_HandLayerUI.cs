using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.Examples
{
    public class SGEx_HandLayerUI : MonoBehaviour
    {
        public enum ShowingLayer //determines the order in which to show them.
        {
            None,
            HandModelLayer,
            CalibrationLayer,
            AnimationLayer,
            PassThroughLayer,
            FeedbackLayer,
            GrabLayer,
            PhysicsLayer,
            GestureLayer,
            All
        }

        /// <summary> Contains all elements to toggle / visualize a layer of one instructionStep. Generated at the start </summary>
        public class HandLayerExample
        {
            public ShowingLayer layer;

            public Text treeText;

            public SG_Activator layerObjects;

            public void SetExamples(bool active)
            {
                if (layerObjects != null)
                {
                    layerObjects.Activated = active;
                }
            }

            public void SetText(Color newColor)
            {
                if (treeText != null)
                {
                    treeText.color = newColor;
                }
            }

            public HandLayerExample(ShowingLayer forLayer, Text titleText, SG_Activator objects)
            {
                this.layer = forLayer;
                this.treeText = titleText;
                this.layerObjects = objects;
            }
        }

        /// <summary> Instructions that we cycle through by pressing next / previous. </summary>
        public class InstructionStep
        {
            public ShowingLayer activeLayer;

            public string instructions;

            public bool mustBeConnected;

            public bool mustBeCalibrated;

            public InstructionStep(ShowingLayer layer, bool mustConnect, bool mustCalibrate, string instr)
            {
                instructions = instr;
                activeLayer = layer;
                mustBeConnected = mustConnect;
                mustBeCalibrated = mustCalibrate;
            }
        }



        public SGEx_SelectHandModel handSelector;

        public Text instructionsUI;
        public Text nextBtnInstr;
        public Button prevBtn, nextBtn, calibrateWristBtn;
        public Text treeTextTemplate;
        public Color textHLColor = Color.white;
        public Color textDisabledColor = Color.gray;

        protected ShowingLayer showing = ShowingLayer.All;

        public KeyCode nextKey = KeyCode.D;
        public KeyCode prevKey = KeyCode.A;
        public KeyCode wristKey = KeyCode.P;


        public int currStep = -1;
        protected int mustConnectAt = 1000;
        protected int mustCalibrateAt = 1000;

        protected int blockCode = 0;
        //public Button nextButton, prevButton;

        protected SG_TrackedHand activeHand = null;
        protected SG_HapticGlove activeGlove = null;

        [Header("Feedback Layer Components")]
        public SG_Activator feedbackObjects;

        [Header("Grab Layer Components")]
        public SG_Activator grabLayerObjects;

        [Header("Physics Layer Components")]
        public SG_Activator physicsLayerObjects;

        [Header("PassThrough Layer Components")]
        public SG_Activator passThroughObjects;

        [Header("Feedback Layer")]
        public SG_Activator gestureObjects;
        public SGEx_DebugGesture gestureUI;

        protected HandLayerExample[] layerElements = new HandLayerExample[0];
        protected InstructionStep[] instructions = new InstructionStep[0];

        protected bool calibrationStarted = false;
        protected bool calibratedOnce = false;

        protected void SetupLayers()
        {
            if (layerElements.Length == 0)
            {
                //pre-setup
                if (gestureObjects) { gestureObjects.Add(gestureUI.gameObject); }

                //layer creation
                layerElements = new HandLayerExample[(int)ShowingLayer.All];
                layerElements[(int)ShowingLayer.None] = new HandLayerExample(ShowingLayer.None, null, null);
                layerElements[(int)ShowingLayer.HandModelLayer] = new HandLayerExample(ShowingLayer.HandModelLayer,     SpawnTreeText("Hand Model"),        null);
                layerElements[(int)ShowingLayer.CalibrationLayer] = new HandLayerExample(ShowingLayer.CalibrationLayer, SpawnTreeText("Calibration Layer"), null);
                layerElements[(int)ShowingLayer.AnimationLayer] = new HandLayerExample(ShowingLayer.AnimationLayer,     SpawnTreeText("Animation Layer"),   null);
                layerElements[(int)ShowingLayer.PassThroughLayer] = new HandLayerExample(ShowingLayer.PassThroughLayer, SpawnTreeText("PassThrough Layer"), passThroughObjects);
                layerElements[(int)ShowingLayer.FeedbackLayer] = new HandLayerExample(ShowingLayer.FeedbackLayer,       SpawnTreeText("Feedback Layer"),    feedbackObjects);
                layerElements[(int)ShowingLayer.GrabLayer] = new HandLayerExample(ShowingLayer.GrabLayer,               SpawnTreeText("Grab Layer"),        grabLayerObjects);
                layerElements[(int)ShowingLayer.PhysicsLayer] = new HandLayerExample(ShowingLayer.PhysicsLayer,         SpawnTreeText("Physics Layer"),     physicsLayerObjects);
                layerElements[(int)ShowingLayer.GestureLayer] = new HandLayerExample(ShowingLayer.GestureLayer,         SpawnTreeText("Gesture Layer"),     gestureObjects);

                for (int i=0; i<this.layerElements.Length; i++)
                {
                    if (layerElements[i] == null)
                    {
                        Debug.LogError("Missing elements for " + ((ShowingLayer)i).ToString() + "!");
                    }
                    else
                    {
                        SetLayer(((ShowingLayer)i), true); //enable once for init.
                        SetLayer(((ShowingLayer)i), false); //they;re disabled at the start
                    }
                }
            }
        }

        protected Text SpawnTreeText(string text = "")
        {
            GameObject cloned = GameObject.Instantiate(this.treeTextTemplate.gameObject, treeTextTemplate.transform.parent, false);
            cloned.SetActive(true);
            Text element = cloned.GetComponent<Text>();
            if (text.Length > 0)
            {
                element.text = text;
            }
            return element;
        }

        /// <summary> Activates / Deactivates layer assets </summary>
        /// <param name="layer"></param>
        public void SetLayer(ShowingLayer layer, bool active)
        {
            int index = (int)layer;
            if (index < this.layerElements.Length && layerElements[index] != null)
            {
                layerElements[index].SetExamples(active);
                layerElements[index].SetText(active ? this.textHLColor : this.textDisabledColor);
            }
        }


        public void SetupInstructions()
        {
            if (instructions.Length == 0)
            {
                instructions = new InstructionStep[] //contains all instructions I want to be showing to users.
                {
                    new InstructionStep(ShowingLayer.None, false, false, "This example will run you through the SenseGlove hand prefab and its different 'layers'"),
                    new InstructionStep(ShowingLayer.None, false, false, "The SenseGlove hand consists of 8 layers: A HandModel, Animator, Feedback Layer, Grab Layer, Physics layer, Gesture Layer and Calibration Layer."),
                    new InstructionStep(ShowingLayer.None, false, false, "Each of these layers can be enabled/disabled by turning their gameobjects on/off, either code or through the inspector. Nearly all of them can be safely deleted in their entirety if their functionality is not required."),
                    new InstructionStep(ShowingLayer.None, false, false, "The TrackedHand script, attached to the root of the prefab, is your main access point to all layers. It can take input from any script that implements the IHandPoseProvider interface."),
                    
                    new InstructionStep(ShowingLayer.HandModelLayer, true, false, "The HandModel layer contains the 3D assets to draw and position the hand. The SG_HandModelInfo script tells the other SenseGlove Scripts where the joints are located."),
                    new InstructionStep(ShowingLayer.HandModelLayer, true, false, "One can swap out the hand model for another by replacing the HandModel's children, and assigning the proper transforms in the SG_HandModelInfo script via code or the inspector."),
                    
                    new InstructionStep(ShowingLayer.CalibrationLayer, true, false, "The Calibration Layer will start a calibration procedure to map your fingers to the digital glove."),
                    new InstructionStep(ShowingLayer.CalibrationLayer, true, false, "Follow the instructions below then hand. Once the hand is able to move, the calibration will automatically stop in 10s."),

                    new InstructionStep(ShowingLayer.AnimationLayer, true, true, "The animation layer is responsible for animating the hand using the SG_HandAnimation script. It can be disabled if you wish to animate the hand model yourself."),

                    new InstructionStep(ShowingLayer.PassThroughLayer, true, true, "The PassThrough Layer ensures that your hand doesn't pass though non-trigger collders. Turning it off disables this behaviour."),
                    
                    new InstructionStep(ShowingLayer.FeedbackLayer, true, true, "The Feedback layer contains colliders that respond to SenseGlove_Material Scripts. Each frame, the SG_HandFeedback script collects the appropriate forces and sends these to the HapitcGlove."),
                    
                    new InstructionStep(ShowingLayer.GrabLayer, true, true, "The Grab Layer allows one to pick up and manipulate objects with SG_Interactable scripts. If you already have manipulation scripts (such as through VRTK), you can disable this layer and replace it with your own."),
                    new InstructionStep(ShowingLayer.GrabLayer, true, true, "A Grab is registered when at least one finger and either the thumb or the hand palm touches the same object, and the hand is not \"open\"."),
                    
                    new InstructionStep(ShowingLayer.PhysicsLayer, true, true, "The Physics Layer generates RigidBody colliders for the hand, to push objects and to prevent it from passing through solid walls."),
                    new InstructionStep(ShowingLayer.PhysicsLayer, true, true, "These Hand Bones merge with the RigidBody of any object you are holding, so both cannot pass through walls and tables."),
                    
                    new InstructionStep(ShowingLayer.GestureLayer, true, true, "The Gesture layer can recognize different static hand poses, used to activate certain functions using just the hand based on finger flexion."),
                    
                    new InstructionStep(ShowingLayer.All, true, true, "All these layers combine to create a full immersive experience."),
                   
                   
                };

                //now lets determine if the glove must be connected / calilbrated.
                int connIndex = -1;
                int calIndex = -1;
                for (int i=0; i<this.instructions.Length; i++)
                {
                    if (connIndex < 0 && this.instructions[i].mustBeConnected)
                    {
                        connIndex = i;
                    }
                    if (calIndex < 0 && this.instructions[i].mustBeCalibrated)
                    {
                        calIndex = i;
                    }

                    //Check if both have been assigned.
                    if (connIndex > -1 && calIndex > -1)
                    {
                        break; 
                    }
                }
                if (connIndex > -1)
                {
                    mustConnectAt = connIndex - 1;
                }
                if (calIndex > -1)
                {
                    mustCalibrateAt = calIndex - 1;
                }
                Debug.Log("Glove must be connected by instruction " + mustConnectAt + ", and calibrated at instruction " + mustCalibrateAt);
            }
        }



        
        public bool ProceedConnection()
        {
            return currStep < mustConnectAt || activeHand != null;
        }

        public bool ProceedCalibration()
        {
            bool res = currStep < mustCalibrateAt || calibratedOnce;
            if (!calibratedOnce && activeHand != null && activeHand.calibration.CalibrationActive)
            {
                bool canAnimate = ((SGCore.Calibration.HG_QuickCalibration)activeHand.calibration.internalSequence).CanAnimate;
                Debug.Log("Animate : " + canAnimate);
                res = res || canAnimate;
            }
            return res;
        }

        public void DisableAllNoneEssentialsExcept(ShowingLayer whichLayer)
        {
            if (this.activeHand != null)
            {
                this.activeHand.handModel.gameObject.SetActive(true);
                this.activeHand.handModel.DebugEnabled = whichLayer == ShowingLayer.HandModelLayer;

                this.activeHand.calibration.gameObject.SetActive(true);

                this.activeHand.handAnimation.gameObject.SetActive(whichLayer >= ShowingLayer.CalibrationLayer);

                this.activeHand.feedbackLayer.gameObject.SetActive(whichLayer == ShowingLayer.FeedbackLayer);
                this.activeHand.feedbackLayer.DebugEnabled = (whichLayer == ShowingLayer.FeedbackLayer);

                this.activeHand.grabScript.gameObject.SetActive(whichLayer == ShowingLayer.GrabLayer);
                this.activeHand.grabScript.DebugEnabled = (whichLayer == ShowingLayer.GrabLayer);

                this.activeHand.gestureLayer.gameObject.SetActive(whichLayer == ShowingLayer.GestureLayer);
                this.activeHand.gestureLayer.DebugEnabled = (whichLayer == ShowingLayer.GestureLayer);

                this.activeHand.handPhysics.gameObject.SetActive(whichLayer == ShowingLayer.PhysicsLayer);
                this.activeHand.handPhysics.DebugEnabled = (whichLayer == ShowingLayer.PhysicsLayer);

                this.activeHand.projectionLayer.gameObject.SetActive(whichLayer == ShowingLayer.PassThroughLayer);
                this.activeHand.projectionLayer.DebugEnabled = (whichLayer == ShowingLayer.PassThroughLayer);

            }
        }


        public void NextInstruction()
        {
            int next = currStep + 1;
            if (next >= instructions.Length) { next = instructions.Length - 1; }
            GoToStep(next);
        }

        public void PreviousInstruction()
        {
            int prev = currStep -1;
            if (prev < 0) { prev = 0; }
            GoToStep(prev);
        }

        protected void GoToStep(int index)
        {
            if (currStep != index)
            {
                //these show a different layer
                if (currStep == -1 || instructions[index].activeLayer != instructions[currStep].activeLayer) //at currset == -1, we're at Start()
                {
                    if (currStep > -1)
                    {
                        SetLayer(instructions[currStep].activeLayer, false); //disable current
                    }
                    SetLayer(instructions[index].activeLayer, true); //enable next
                }
                currStep = index;
                this.instructionsUI.text = instructions[index].instructions;

                if ( !ProceedConnection() )
                {
                    nextBtnInstr.text = "The Glove must be connected before proceeding...";
                    blockCode = 1;
                }
                else if ( !ProceedCalibration() )
                {
                    nextBtnInstr.text = "The Glove must be calibrated before proceeding...";
                    blockCode = 2;
                }
                else
                {
                    blockCode = 0;
                    nextBtnInstr.text = ""; //nothing's wrong.
                }

                nextBtn.interactable = index < instructions.Length - 1 && blockCode == 0;
                prevBtn.interactable = index > 0; //we can still go back one

                DisableAllNoneEssentialsExcept(currStep < 0 ? ShowingLayer.None : instructions[currStep].activeLayer);
            }
        }



        public void TrackedHandAssigned()
        {
            if (activeHand == null)
            {
                this.activeHand = this.handSelector.ActiveHand;
                this.activeGlove = this.handSelector.ActiveGlove;
                Debug.Log("Connected to a hand. Yay!");

                activeHand.feedbackLayer.DebugEnabled = false;
                activeHand.handPhysics.DebugEnabled = false;
                activeHand.grabScript.DebugEnabled = false;
                activeHand.projectionLayer.DebugEnabled = false;
                activeHand.gestureLayer.DebugEnabled = false;

                this.gestureUI.gestureLayer = activeHand.gestureLayer;
                this.gestureUI.gestureToCheck = activeHand.gestureLayer.gestures.Length > 0 ? activeHand.gestureLayer.gestures[0] : null;
                handSelector.ActiveHand.calibration.CalibrationFinished.AddListener(CalibrationFinished);
                DisableAllNoneEssentialsExcept(this.currStep < 0 ? ShowingLayer.None : this.instructions[currStep].activeLayer);
            }
        }



        void Start()
        {
            this.treeTextTemplate.gameObject.SetActive(false);
            this.SetupLayers();
            this.SetupInstructions();

            SG.Util.SG_Util.AppendButtonText(prevBtn, "\r\n(" + prevKey.ToString() + ")");
            SG.Util.SG_Util.AppendButtonText(nextBtn, "\r\n(" + nextKey.ToString() + ")");
            SG.Util.SG_Util.AppendButtonText(calibrateWristBtn, "\r\n(" + wristKey.ToString() + ")");

            prevBtn.gameObject.SetActive(true);
            nextBtn.gameObject.SetActive(true);
            calibrateWristBtn.gameObject.SetActive(false);

            nextBtn.onClick.AddListener(NextInstruction);
            prevBtn.onClick.AddListener(PreviousInstruction);

            GoToStep(0);


            handSelector.leftHand.calibration.startCondition = SG_CalibrationSequence.StartCondition.Manual;
            handSelector.leftGlove.checkCalibrationOnConnect = false;
            handSelector.rightHand.calibration.startCondition = SG_CalibrationSequence.StartCondition.Manual;
            handSelector.rightGlove.checkCalibrationOnConnect = false;
            handSelector.ActiveHandConnect.AddListener(TrackedHandAssigned);

        }

        public void CalibrationFinished()
        {
            this.calibratedOnce = true;
        }

        void Update()
        {
            if (this.activeHand != null) //we have one assinged. Yey
            {
                if (!calibrationStarted && this.currStep > -1 && this.instructions[currStep].activeLayer == ShowingLayer.CalibrationLayer)
                {
                    calibrationStarted = true;
                    this.activeHand.calibration.StartCalibration(true);
                }


                if (this.nextBtnInstr.text.Length > 0) //some form of warning given about proceeding.
                {
                    bool canProceedNow = true;
                    if (blockCode == 1 && !ProceedConnection()) //we were blocked by connection
                    {
                        canProceedNow = false;
                    }
                    else if (blockCode == 2 && !ProceedCalibration() ) //we were blocked by calibration
                    {
                        canProceedNow = false;
                    }
                    if (canProceedNow)
                    {
                        nextBtnInstr.text = "";
                        this.nextBtn.interactable = this.currStep < this.instructions.Length - 1;
                        Debug.Log("Unlock the next button, we are " + (blockCode == 1 ? "connected" : "calibrated"));
                        blockCode = 0;
                    }

                }
            }
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
            if (Input.GetKeyDown(nextKey))
            {
                if (this.ProceedConnection() && this.ProceedCalibration())
                {
                    NextInstruction();
                }
            }
            else if (Input.GetKeyDown(prevKey))
            {
                PreviousInstruction();
            }
#endif
        }

    }
}