using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SenseGlove_Examples
{
    public class SGEx_HandLayerUI : MonoBehaviour
    {
        public enum ShowingLayer
        {
            None,
            HandModelLayer,
            AnimationLayer,
            FeedbackLayer,
            GrabLayer,
            RigidbodyLayer,
            PhysicsLayer,
            All
        }



        public Text instructionsUI;
        public Button prevBtn, nextBtn, calibrateWristBtn;
        protected ShowingLayer showing = ShowingLayer.All;
        public SG_TrackedHand leftHand;
        public SG_TrackedHand rightHand;

        public KeyCode nextKey = KeyCode.D;
        public KeyCode prevKey = KeyCode.A;
        public KeyCode wristKey = KeyCode.P;


        public int currStep = -1;
        public int mustConnectStep = 6; //starting from this layer, our SG should be connected

        public Button nextButton, prevButton;

        protected SG_TrackedHand activeHand = null;

        public Text[] overviewTexts = new Text[0];
        public Color textHLColor = Color.white;
        public Color textDisabledColor = Color.gray;


        protected string[] instructionTexts = new string[]
        {
            "This example will run you through the SenseGlove hand prefab and its different 'layers'",

            "The SenseGlove hand consists of 7 layers: A HandModel, Animator, Feedback Layer, Grab Layer, Rigidbody Layer and PhysicsTracking layer.",

            "Each of these layers can be enabled/disabled by turning their gameobjects on/off, either code or through the inspector. Nearly all of them can be safely deleted in their entirety if their functionality is not required.",

            "The TrackedHand script, attached to the root of the prefab, is your main access point to all layers. It can be set to follow a specific GameObject, with preprogrammed offsets for certain tracking hardware.",

            "The HandModel layer contains the 3D assets to draw and position the hand. The SG_HandInfo script tells the other SenseGlove Scripts where the joints are located.",
            
            "One can swap out the hand model for another by replacing the HandModel's children, and assigning the proper transforms in the SG_HandInfo script via code or the inspector.",
            
            "Unless you want to manually set Tracking targets for the colliders of the other layers, the SG_HandModelInfo script is the only one that should not be deleted.",

            "The animation layer is responsible for animating the hand using the SG_HandAnimation script. It can be disabled if you wish to animate the hand model yourself.",

            "The Feedback layer contains colliders that respond to impacts and to SenseGlove_Material Scripts. Each frame, the SG_HandFeedback script collects the appropriate forces and sends these to the SenseGlove.",

            "The Grab Layer allows one to pick up and manipulate objects with SG_Interactable scripts. If you already have manipulation scripts (such as through VRTK), you can disable this layer and replace it with your own.",

            "The Rigidbody layer adds rigidbodies that allow one to push and hold other rigidbody objects. This gameobject and its children can be placed on their own layer, or be told to ignore certain colliders.",

            "The PhysicsTracking layer contains non-trigger colliders that prevent the SG_TrackedHand from passing through non-grabable objects, provided that its 'trackingMethod' property is set to be 'PhysicsBased'.",

             "This separation of layers allows for a hand model that can be adjusted to your needs, and which allows different physics behaviours wihout touching the actual 3D Model.",
        };

        protected ShowingLayer[] linkedLayers = new ShowingLayer[]
        {
            ShowingLayer.None,
            ShowingLayer.None,
            ShowingLayer.None,
            ShowingLayer.None,
            ShowingLayer.HandModelLayer,
            ShowingLayer.HandModelLayer,
            ShowingLayer.HandModelLayer,
            ShowingLayer.AnimationLayer,
            ShowingLayer.FeedbackLayer,
            ShowingLayer.GrabLayer,
            ShowingLayer.RigidbodyLayer,
            ShowingLayer.PhysicsLayer,
            ShowingLayer.All,
        };

        public GameObject[] feedbackObjects = new GameObject[0];
        public GameObject[] grabLayerObjects = new GameObject[0];
        public GameObject[] rigidBodyObjects = new GameObject[0];
        public GameObject[] physicsObjects = new GameObject[0];

        private GameObject[][] layerObjects = new GameObject[0][];


        public void NextStep()
        {
            currStep++;
            if (currStep >= instructionTexts.Length) { currStep = instructionTexts.Length - 1; }
            GoToStep(currStep);
        }

        public void PreviousStep()
        {
            currStep--;
            if (currStep < 0) { currStep = 0; }
            GoToStep(currStep);
        }

        public void GoToStep(int index)
        {
            currStep = index;
            SetLayer(index);
            SetInstructions(index);
            nextButton.interactable = index < instructionTexts.Length - 1 && (index < mustConnectStep || activeHand != null); //we can still go one more step
            prevBtn.interactable = index > 0; //we can still go back one
        }


        public void SetInstructions(int index)
        {
            if (index < this.instructionTexts.Length)
            {
                if (index > -1)
                {
                    instructionsUI.text = this.instructionTexts[index];
                }
                else { instructionsUI.text = ""; }
            }
        }

        public void SetLayer(int index)
        {
            if (index < this.linkedLayers.Length)
            {
                if (index > -1)
                {
                    ShowLayer(linkedLayers[index]);
                }
                else { ShowLayer(ShowingLayer.None); }
            }
        }

        public void CalibrateWrist()
        {
            if (this.activeHand != null)
            {
                this.activeHand.handAnimation.CalibrateWrist();
            }
        }
        

        public void SetLayerObjects(int L, bool active)
        {
            if (L > 0 && layerObjects.Length > L && layerObjects[L] != null)
            {
                for (int i = 0; i < layerObjects[L].Length; i++)
                {
                    if (layerObjects[L][i] != null) { layerObjects[L][i].SetActive(active); }
                }
            }
        }

        public void SetAllObjects(bool active)
        {
            if (layerObjects != null)
            {
                for (int l=0; l<layerObjects.Length; l++)
                {
                    SetLayerObjects(l, active);
                }
            }
        }

        public void ShowLayer(ShowingLayer layer)
        {
            if (this.showing != layer)
            {
                this.showing = layer;
                if (activeHand != null)
                {
                    SG_Util.SetChildren(activeHand.transform, false); //all layers are off
                    SetAllObjects(false);
                    if (layer >= ShowingLayer.None)
                    {
                        activeHand.handModel.gameObject.SetActive(true);
                    }
                    if (layer >= ShowingLayer.AnimationLayer)
                    {
                        activeHand.handAnimation.gameObject.SetActive(true);
                    }
                    activeHand.handModel.DebugEnabled = layer < ShowingLayer.FeedbackLayer && layer > ShowingLayer.None;

                    bool updateWrist = layer >= ShowingLayer.AnimationLayer;

                    if (updateWrist && !activeHand.handAnimation.updateWrist)
                    {
                        activeHand.handAnimation.updateWrist = true;
                        activeHand.handAnimation.CalibrateWrist();
                        activeHand.handAnimation.UpdateWrist(activeHand.hardware.GloveData);
                    }
                    if (!updateWrist)
                    {
                        activeHand.handAnimation.CalibrateWrist();
                        activeHand.handAnimation.UpdateWrist(activeHand.hardware.GloveData);
                    }
                    activeHand.handAnimation.updateWrist = updateWrist;

                    if (layer == ShowingLayer.FeedbackLayer || layer == ShowingLayer.All) { activeHand.feedbackScript.gameObject.SetActive(true); }
                    if (layer == ShowingLayer.GrabLayer || layer == ShowingLayer.All) { activeHand.grabScript.gameObject.SetActive(true); }
                    if (layer == ShowingLayer.RigidbodyLayer || layer == ShowingLayer.All) { activeHand.rigidBodyLayer.gameObject.SetActive(true); }
                    if (layer == ShowingLayer.PhysicsLayer) { activeHand.physicsTrackingLayer.gameObject.SetActive(true); }
                }
                SetLayerObjects((int)layer, true);
                UpdateOverview(layer);
                
            }
        }


        public void UpdateOverview(ShowingLayer layer)
        {
            Debug.Log("Showing " + layer.ToString());
            for (int i=0; i<this.overviewTexts.Length; i++)
            {
                if (overviewTexts[i] != null)
                {
                    Color textColor;
                    if (layer == ShowingLayer.All) { textColor = this.textHLColor; }
                    else if (layer == ShowingLayer.None) { textColor = this.textDisabledColor; }
                    else
                    {
                        textColor = ((ShowingLayer)i) == layer ? textHLColor : textDisabledColor;
                    }
                    overviewTexts[i].color = textColor;
                }
            }
        }



        // Use this for initialization
        void Start()
        {
            prevBtn.gameObject.SetActive(true);
            nextBtn.gameObject.SetActive(true);
            calibrateWristBtn.gameObject.SetActive(false);

            layerObjects = new GameObject[(int)ShowingLayer.All][];
            layerObjects[(int)ShowingLayer.FeedbackLayer] = this.feedbackObjects;
            layerObjects[(int)ShowingLayer.RigidbodyLayer] = this.rigidBodyObjects;
            layerObjects[(int)ShowingLayer.PhysicsLayer] = this.physicsObjects;
            layerObjects[(int)ShowingLayer.GrabLayer] = this.grabLayerObjects;

            SetAllObjects(false);
            GoToStep(0);

            //allow both hands to at least call Awake, then turn them off for now.
            SG_Util.SetChildren(leftHand.transform, true);
            SG_Util.SetChildren(leftHand.transform, false);
            SG_Util.SetChildren(rightHand.transform, true);
            SG_Util.SetChildren(rightHand.transform, false);

            SG_Util.AppendButtonText(prevBtn, "\r\n(" + prevKey.ToString() + ")");
            SG_Util.AppendButtonText(nextBtn, "\r\n(" + nextKey.ToString() + ")");
            SG_Util.AppendButtonText(calibrateWristBtn, "\r\n(" + wristKey.ToString() + ")");


        }

        // Update is called once per frame
        void Update()
        {
            if (activeHand == null)
            {
                if (leftHand.hardware.GloveReady || rightHand.hardware.GloveReady)
                {
                    activeHand = rightHand.hardware.GloveReady ? rightHand : leftHand;

                    activeHand.handModel.gameObject.SetActive(true);
                    activeHand.handAnimation.updateWrist = true;
                    activeHand.handAnimation.CalibrateWrist();
                    activeHand.handAnimation.UpdateWrist(activeHand.hardware.GloveData);
                    activeHand.handAnimation.UpdateHand(activeHand.hardware.GloveData);
                    activeHand.feedbackScript.DebugEnabled = true;
                    activeHand.grabScript.DebugEnabled = true;
                    activeHand.rigidBodyLayer.DebugEnabled = true;
                    activeHand.physicsTrackingLayer.DebugEnabled = true;
                    calibrateWristBtn.gameObject.SetActive(true);
                    if (this.showing != ShowingLayer.PhysicsLayer)
                    {
                        activeHand.physicsTrackingLayer.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (Input.GetKeyDown(nextKey))
                {
                    NextStep();
                }
                else if (Input.GetKeyDown(prevKey))
                {
                    PreviousStep();
                }
                if (Input.GetKeyDown(wristKey))
                {
                    CalibrateWrist();
                }
            }
        }
    }
}