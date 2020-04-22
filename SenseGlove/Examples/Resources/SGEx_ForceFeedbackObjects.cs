using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SGEx_ForceFeedbackObjects : MonoBehaviour
{
    public SG_TrackedHand leftHand, rightHand;
    protected SG_TrackedHand activeHand = null;

    public KeyCode nextObjKey = KeyCode.D;
    public KeyCode prevObjKey = KeyCode.A;
    public KeyCode calibrateWristKey = KeyCode.P;

    public Button nextButton, previousButton;
    public Button wristButton;

    public GameObject[] ffbObjects = new GameObject[0];
   // protected SG_SimpleTracking[] trackScripts = new SG_SimpleTracking[0];
    protected SG_Breakable[] breakables = new SG_Breakable[0];

    protected int objIndex = -1;

    protected bool allowedSwap = false;
    protected float openTime = 0.2f;
    protected float openedTimer = 0;

    protected float breakableResetTime = 1.0f;
    protected float breakableTimer = 1.0f;

    public Text objectText;

    

    protected void SetRelevantScripts(SG_TrackedHand hand, bool active)
    {
        if (hand != null)
        {
            if (hand.handAnimation != null) { hand.handAnimation.gameObject.SetActive(active); }
            if (hand.handModel != null) { hand.handModel.gameObject.SetActive(active); }
            if (hand.feedbackScript != null) { hand.feedbackScript.gameObject.SetActive(active); }
        }
    }


    public void CalibrateWrist()
    {
        if (this.activeHand != null && this.activeHand.handAnimation != null)
        {
            this.activeHand.handAnimation.CalibrateWrist();
        }
    }


    public static bool CheckHandOpen(SG_TrackedHand hand)
    {
        float[] flexions = hand.hardware.GloveData.GetFlexions();
        return flexions.Length > 3 && flexions[0] > -45 && flexions[1] > -45 && flexions[2] > -45;
    }


    protected void SetObject(int index, bool active)
    {
        if (index > -1 && index < ffbObjects.Length)
        {
            ffbObjects[index].SetActive(active);
            if (breakables[index] != null && breakables[index].IsBroken()) { breakables[index].UnBreak(); } 
            if (active && objectText != null) { objectText.text = ffbObjects[index].name; }
        }
        else if (active && objectText != null) { objectText.text = ""; }
    }

    protected int WrapIndex(int newIndex)
    {
        if (newIndex < -1) { return ffbObjects.Length - 1; }
        else if (newIndex >= ffbObjects.Length) { return -1; }
        return newIndex;
    }


    void ConnectObjects(SG_TrackedHand hand)
    {
        for (int i = 0; i < ffbObjects.Length; i++)
        {
            ffbObjects[i].SetActive(true);

            SG_SimpleTracking trackingScript = ffbObjects[i].GetComponent<SG_SimpleTracking>();
            if (trackingScript == null) { trackingScript = ffbObjects[i].AddComponent<SG_SimpleTracking>(); }
            if (trackingScript != null) { trackingScript.SetTrackingTarget(hand.handModel.wristTransform, true); }
            
            ffbObjects[i].SetActive(false);
        }
    }


    public void NextObject()
    {
        this.SetObject(objIndex, false);
        this.objIndex = WrapIndex(this.objIndex + 1);
        this.SetObject(this.objIndex, true);
    }

    public void PreviousObject()
    {
        this.SetObject(objIndex, false);
        this.objIndex = WrapIndex(this.objIndex - 1);
        this.SetObject(this.objIndex, true);
    }


    public bool ButtonsActive
    {
        get { return previousButton != null && previousButton.gameObject.activeInHierarchy; }
        set
        {
            if (previousButton != null) { previousButton.gameObject.SetActive(value); }
            if (nextButton != null) { nextButton.gameObject.SetActive(value); }
            if (wristButton != null) { wristButton.gameObject.SetActive(value); }
        }
    }

    public bool ButtonsInteractable
    {
        get { return previousButton != null && previousButton.interactable; }
        set
        {
            if (previousButton != null) { previousButton.interactable = value; }
            if (nextButton != null) { nextButton.interactable = value; }
        }
    }


    void Awake()
    {
        
    }

	// Use this for initialization
	void Start ()
    {
        SG_Util.SetChildren(leftHand.transform, false);
        SG_Util.SetChildren(rightHand.transform, false);
        if (objectText != null) { objectText.text = ""; }
        if (nextButton != null)
        {
            Text btnText = nextButton.GetComponentInChildren<Text>();
            if (btnText != null) { btnText.text = btnText.text + "\r\n(" + this.nextObjKey.ToString() + ")"; }
        }
        if (previousButton != null)
        {
            Text btnText = previousButton.GetComponentInChildren<Text>();
            if (btnText != null) { btnText.text = btnText.text + "\r\n(" + this.prevObjKey.ToString() + ")"; }
        }
        if (wristButton != null)
        {
            Text btnText = wristButton.GetComponentInChildren<Text>();
            if (btnText != null) { btnText.text = btnText.text + "\r\n(" + this.calibrateWristKey.ToString() + ")"; }
        }

        ButtonsActive = false;
        breakables = new SG_Breakable[ffbObjects.Length];
        for (int i = 0; i < ffbObjects.Length; i++)
        {
            ffbObjects[i].gameObject.SetActive(true); //allows them to call awake at least once.
            breakables[i] = ffbObjects[i].GetComponent<SG_Breakable>();


            ffbObjects[i].gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (activeHand == null)
        {
            if (rightHand.hardware.IsConnected || leftHand.hardware.IsConnected)
            {
                activeHand = rightHand.hardware.IsConnected ? rightHand : leftHand;
                SetRelevantScripts(activeHand, true);
                ConnectObjects(activeHand);
                ButtonsActive = true;
            }
        }
        else
        {
            if (Input.GetKeyDown(calibrateWristKey)) { this.CalibrateWrist(); }

            bool handOpen = this.objIndex > -1 ? !activeHand.feedbackScript.TouchingMaterial() : CheckHandOpen(this.activeHand);
            if (handOpen)
            {
                openedTimer += Time.deltaTime;
                if (openedTimer >= openTime) { allowedSwap = true; }
            }
            else
            {
                openedTimer = 0;
                allowedSwap = false;
            }

            if (ButtonsInteractable != allowedSwap) { ButtonsInteractable = allowedSwap; }
            if (allowedSwap)
            {
                if (Input.GetKeyDown(prevObjKey))
                {
                    PreviousObject();
                }
                else if (Input.GetKeyDown(nextObjKey))
                {
                    NextObject();
                }
            }

            if (objIndex > -1 && breakables[objIndex] != null && breakables[objIndex].IsBroken()) //we're dealing with a breakable
            {
                bool canRespawn = CheckHandOpen(this.activeHand);
                if (canRespawn)
                {
                    this.breakableTimer += Time.deltaTime;
                    if (this.breakableTimer >= breakableResetTime)
                    {
                        this.breakableTimer = 0;
                        breakables[objIndex].UnBreak();
                    }
                }
            }

        }

    
	}
}
