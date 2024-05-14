using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SGEx_Nova2Diagnostics : MonoBehaviour
{

    public SG.SG_HapticGlove nova2Glove;

    public UnityEngine.UI.Text subTitle;

    public UnityEngine.UI.Dropdown connectionDropDown;

    public SG_InputSlider[] ffbSliders = new SG_InputSlider[0];
    public UnityEngine.UI.Button ffbOffBtn, ffbOnBtn, ffbToggleBtn;

    public SG_InputSlider strapSlider;
    public UnityEngine.UI.Button strapOffBtn, strapOnBtn, strapToggleBtn;

    public UnityEngine.UI.Button thumbBtn;
    public UnityEngine.UI.Button indexBtn;
    public UnityEngine.UI.Button palm_IndexBtn;
    public UnityEngine.UI.Button palm_PinkyBtn;

    public UnityEngine.UI.Button palmUp, palmDown;

    protected float[] lastFFBCmd = new float[5];

    public SG.SG_CustomWaveform testWaveform;

    public SG.SG_CustomWaveform palmWaveform01;
    public SG.SG_CustomWaveform palmWaveform02;

    public float palmDelay = 0.05f; //50ms?

    private bool pauseUpdates = false;

    [SerializeField] protected UnityEngine.UI.Text sensorDataElement;


    public UnityEngine.UI.Button resetNormalBtn;
    public UnityEngine.UI.Button endNormalBtn;
    public UnityEngine.UI.Button sendRawBtn;
    public UnityEngine.UI.Button sendNormBtn;

#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
    public KeyCode toggleFFBHotKey = KeyCode.Return;
    public KeyCode toggleStapKey = KeyCode.Space;
    public KeyCode thumbHotKey = KeyCode.Alpha1;
    public KeyCode indexHotKey = KeyCode.Alpha2;
    public KeyCode palmIndexHotKey = KeyCode.Alpha3;
    public KeyCode palmPinkyHotKey = KeyCode.Alpha4;
    public KeyCode pamUpHotKey = KeyCode.Alpha5;
    public KeyCode pamDownHotKey = KeyCode.Alpha6;
#endif

    public SG.SG_DeviceSelector leftHandInput, rightHandInput;

    public string SensorText
    {
        get { return this.sensorDataElement != null ? this.sensorDataElement.text : ""; }
        set { if (this.sensorDataElement != null) { this.sensorDataElement.text = value; } }
    }



    public void OnGloveConnected()
    {
        //Remove inputs from
        if (leftHandInput != null) { leftHandInput.ClearDevices(); }
        if (rightHandInput != null) { rightHandInput.ClearDevices(); }
        
       // Debug.Log("Glove Connected! " + nova2Glove.connectsTo.ToString() + " / " + this.nova2Glove.DeviceType.ToString());
        if (this.nova2Glove != null && this.nova2Glove.DeviceType == SGCore.DeviceType.NOVA_2_GLOVE)
        {
            if (subTitle != null) { subTitle.text = this.nova2Glove.InternalGlove.GetDeviceID() + " - " + (this.nova2Glove.InternalGlove.IsRight() ? "Right Hand" : "Left Hand"); }
            
            if (this.nova2Glove.InternalGlove.IsRight() && rightHandInput != null)
            {
                rightHandInput.AddDevice(this.nova2Glove);
            }
            else if ( !this.nova2Glove.InternalGlove.IsRight() && leftHandInput != null)
            {
                leftHandInput.AddDevice(this.nova2Glove);
            }
            
            UpdateForceFeedback();
            UpdateStrapLevel(); //make sure these are up to date(!)
        }
        else
        {
            this.SensorText = "";
            if (subTitle != null) { subTitle.text = "No Nova 2 Glove connected to the chosen hand side..."; }
        }
    }


    /// <summary> Update visualization(s) </summary>
    public void UpdateSensorData()
    {
        if (this.nova2Glove != null && this.nova2Glove.DeviceType == SGCore.DeviceType.NOVA_2_GLOVE)
        {
            SGCore.Nova.Nova2_SensorData sData;
            if (((SGCore.Nova.Nova2Glove)this.nova2Glove.InternalGlove).GetSensorData(out sData))
            {
                string sensorTxt = "State:\t" + sData.SensorState.ToString();
                SGCore.Kinematics.Vect3D[][] rawVals = sData.SensorValues;
                for (int f=0; f<rawVals.Length; f++)
                {
                    sensorTxt += "\n" + ((SGCore.Finger)f) + ":\t";
                    if (f == 0 && rawVals[f].Length > 0) //abd, then however many others
                    {
                        sensorTxt += rawVals[f][0].z.ToString() + "\t";
                    }
                    for (int i=0; i<rawVals[f].Length; i++)
                    {
                        sensorTxt += rawVals[f][i].y.ToString() + "\t";
                    }
                }
                this.SensorText = sensorTxt;
            }
        }
    }




    public void SetFFBOn()
    {
        pauseUpdates = true;
        for (int i=0; i<this.ffbSliders.Length; i++)
        {
            this.ffbSliders[i].SlideValue = 100;
        }
        pauseUpdates = false;
        this.UpdateForceFeedback();
    }

    public void SetFFBOff()
    {
        pauseUpdates = true;
        for (int i = 0; i < this.ffbSliders.Length; i++)
        {
            this.ffbSliders[i].SlideValue = 0;
        }
        pauseUpdates = false;
        this.UpdateForceFeedback();
    }


    public bool FFBActive
    {
        get
        {
            for (int i=0; i<this.ffbSliders.Length; i++)
            {
                if (this.ffbSliders[i].SlideValue > 0)
                    return true;
            }
            return false;
        }
    }

    public void ToggleFFB()
    {
        if (FFBActive)
        {
            SetFFBOff();
        }
        else
        {
            SetFFBOn();
        }
    }




    public void SetStrapOn()
    {
        this.strapSlider.SlideValue = 100;
    }

    public void SetStrapOff()
    {
        this.strapSlider.SlideValue = 0;
    }

    public void ToggleStrap()
    {
        this.strapSlider.SlideValue = this.strapSlider.SlideValue > 0 ? 0 : 100;
    }


    public void UpdateForceFeedback()
    {
        if (pauseUpdates) //don't update yet...
            return;

        for (int f=0; f<5 && f < ffbSliders.Length; f++)
        {
            this.lastFFBCmd[f] = ((int)ffbSliders[f].SlideValue) / 100.0f;
        }
       // Debug.Log("Update FFB: " + SG.Util.SG_Util.ToString(lastFFBCmd));
        this.nova2Glove.QueueFFBCmd(lastFFBCmd);
    }

    public void UpdateStrapLevel()
    {
        if (pauseUpdates)
            return;

        if (this.strapSlider == null)
            return;

        float strapLvl = this.strapSlider.SlideValue / 100.0f;
        this.nova2Glove.QueueWristSqueeze(strapLvl);
       // Debug.Log("Update Strap: " + strapLvl.ToString("0.00"));
    }


    public void SendWaveform(SG.SG_CustomWaveform waveForm, SGCore.Nova.Nova2Glove.Nova2_VibroMotors motor)
    {
     //   Debug.Log("Sending " + waveForm.name + " to " + motor);
        //this.nova2Glove.SendCustomWaveform(waveForm, motor);
        if (this.nova2Glove != null && this.nova2Glove.DeviceType == SGCore.DeviceType.NOVA_2_GLOVE)
        {
            ((SGCore.Nova.Nova2Glove)this.nova2Glove.InternalGlove).SendCustomWaveform(waveForm.GetWaveform(), motor);
        }
    }


    private IEnumerator SendCoupledCommmands(SGCore.Nova.Nova2Glove.Nova2_VibroMotors firstLocation, SGCore.Nova.Nova2Glove.Nova2_VibroMotors secondLocation, float delay)
    {
        //Debug.Log(firstLocation.ToString()); ;
        this.SendWaveform(palmWaveform01, firstLocation);
        yield return new WaitForSecondsRealtime(delay);
        this.SendWaveform(palmWaveform02, secondLocation);
       // Debug.Log(secondLocation.ToString());
    }



    public void PalmVibro_IndexToPinky()
    {
        StartCoroutine( SendCoupledCommmands(SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmIndexSide, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmPinkySide, this.palmDelay) );
    }

    public void PalmVibro_PinkyToIndex()
    {
        StartCoroutine(SendCoupledCommmands(SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmPinkySide, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmIndexSide, this.palmDelay));
    }


    public void ResetNormalization()
    {
        if (this.nova2Glove != null && this.nova2Glove.InternalGlove != null && this.nova2Glove.InternalGlove is SGCore.Nova.Nova2Glove)
        {
            ((SGCore.Nova.Nova2Glove)this.nova2Glove.InternalGlove).ResetCalibration();
        }
    }

    public void EndNormalization()
    {
        if (this.nova2Glove != null && this.nova2Glove.InternalGlove != null && this.nova2Glove.InternalGlove is SGCore.Nova.Nova2Glove)
        {
            ((SGCore.Nova.Nova2Glove)this.nova2Glove.InternalGlove).EndCalibration();
        }
    }

    public void SendRawData()
    {
        if (this.nova2Glove != null && this.nova2Glove.InternalGlove != null && this.nova2Glove.InternalGlove is SGCore.Nova.Nova2Glove)
        {
            ((SGCore.Nova.Nova2Glove)this.nova2Glove.InternalGlove).SendRawData();
        }
    }

    public void SendNormalizedData()
    {
        if (this.nova2Glove != null && this.nova2Glove.InternalGlove != null && this.nova2Glove.InternalGlove is SGCore.Nova.Nova2Glove)
        {
            ((SGCore.Nova.Nova2Glove)this.nova2Glove.InternalGlove).SendNormalizedData();
        }
    }


    public void OnConnectionUpdate(int newValue)
    {
        switch (newValue)
        {
            case 1:
                this.nova2Glove.connectsTo = SG.HandSide.LeftHand;
                break;
            case 2:
                this.nova2Glove.connectsTo = SG.HandSide.RightHand;
                break;
            default:
                this.nova2Glove.connectsTo = SG.HandSide.AnyHand;
                break;
        }
        this.OnGloveConnected(); //call this one 
    }





    private void Start()
    {
        if (strapSlider != null) { strapSlider.Title = ""; }
        for (int f = 0; f < this.ffbSliders.Length && f < 5; f++)
        {
            ffbSliders[f].Title = ((SGCore.Finger)f).ToString();
        }
        if (thumbBtn != null) { SG.Util.SG_Util.SetButtonText(thumbBtn, "Thumb"); }
        if (indexBtn != null) { SG.Util.SG_Util.SetButtonText(indexBtn, "Index"); }
        if (palm_IndexBtn != null) { SG.Util.SG_Util.SetButtonText(palm_IndexBtn, "Palm-Index"); }
        if (palm_PinkyBtn != null) { SG.Util.SG_Util.SetButtonText(palm_PinkyBtn, "Palm-Pinky"); }
        
        if (palmUp != null) { SG.Util.SG_Util.SetButtonText(palmUp, "Palm-Up"); }
        if (palmDown != null) { SG.Util.SG_Util.SetButtonText(palmDown, "Palm-Down"); }

        this.SensorText = "";
    }



    private void Update()
    {
        UpdateSensorData();

#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
        if (Input.GetKeyDown(this.toggleFFBHotKey)) { this.ToggleFFB(); }
        if (Input.GetKeyDown(this.toggleStapKey)) { this.ToggleStrap(); }

        if (Input.GetKeyDown(this.thumbHotKey)) { this.SendWaveform(testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.ThumbFingerTip); }
        if (Input.GetKeyDown(this.indexHotKey)) { this.SendWaveform(testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.IndexFingerTip); }
        if (Input.GetKeyDown(this.palmIndexHotKey)) { this.SendWaveform(testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmIndexSide); }
        if (Input.GetKeyDown(this.palmPinkyHotKey)) { this.SendWaveform(testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmPinkySide); }
        
        if (Input.GetKeyDown(this.pamUpHotKey)) { this.PalmVibro_PinkyToIndex(); }
        if (Input.GetKeyDown(this.pamDownHotKey)) { this.PalmVibro_IndexToPinky(); }
#endif
    }


    private void OnEnable()
    {
        this.nova2Glove.DeviceConnected.AddListener(OnGloveConnected);
        this.nova2Glove.DeviceDisconnected.AddListener(OnGloveConnected);

        for (int f = 0; f < this.ffbSliders.Length && f < 5; f++)
        {
            ffbSliders[f].OnValueChanged.AddListener(UpdateForceFeedback);
        }
        if (ffbOnBtn != null) { ffbOnBtn.onClick.AddListener(SetFFBOn); }
        if (ffbOffBtn != null) { ffbOffBtn.onClick.AddListener(SetFFBOff); }
        if (ffbToggleBtn!= null) { ffbToggleBtn.onClick.AddListener(ToggleFFB); }
        
        if (thumbBtn != null) { thumbBtn.onClick.AddListener(delegate { SendWaveform(this.testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.ThumbFingerTip); }); }
        if (indexBtn != null) { indexBtn.onClick.AddListener(delegate { SendWaveform(this.testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.IndexFingerTip); }); }
        if (palm_IndexBtn != null) { palm_IndexBtn.onClick.AddListener(delegate { SendWaveform(this.testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmIndexSide); }); }
        if (palm_PinkyBtn != null) { palm_PinkyBtn.onClick.AddListener(delegate { SendWaveform(this.testWaveform, SGCore.Nova.Nova2Glove.Nova2_VibroMotors.PalmPinkySide); }); }
        
        if (strapSlider != null) { strapSlider.OnValueChanged.AddListener(UpdateStrapLevel); }
        if (strapOnBtn != null) { strapOnBtn.onClick.AddListener(SetStrapOn); }
        if (strapOffBtn != null) { strapOffBtn.onClick.AddListener(SetStrapOff); }
        if (strapToggleBtn != null) { strapToggleBtn.onClick.AddListener(ToggleStrap); }

        if (palmDown != null) { palmDown.onClick.AddListener(PalmVibro_IndexToPinky); } 
        if (palmUp != null) { palmUp.onClick.AddListener(PalmVibro_PinkyToIndex); }

        if (resetNormalBtn != null) { this.resetNormalBtn.onClick.AddListener(ResetNormalization); }
        if (endNormalBtn != null) { this.endNormalBtn.onClick.AddListener(EndNormalization); }
        if (sendRawBtn != null) { this.sendRawBtn.onClick.AddListener(SendRawData); }
        if (sendNormBtn != null) { this.sendNormBtn.onClick.AddListener(SendNormalizedData); }

        if (connectionDropDown != null) { connectionDropDown.onValueChanged.AddListener(OnConnectionUpdate); }

    }


    private void OnDisable()
    {
        this.nova2Glove.DeviceConnected.RemoveListener(OnGloveConnected);

        for (int f = 0; f < this.ffbSliders.Length && f < 5; f++)
        {
            ffbSliders[f].OnValueChanged.RemoveListener(UpdateForceFeedback);
        }

        if (thumbBtn != null) { thumbBtn.onClick.RemoveAllListeners(); }
        if (indexBtn != null) { indexBtn.onClick.RemoveAllListeners(); }
        if (palm_IndexBtn != null) { palm_IndexBtn.onClick.RemoveAllListeners(); }
        if (palm_PinkyBtn != null) { palm_PinkyBtn.onClick.RemoveAllListeners(); }

        if (strapSlider != null) { strapSlider.OnValueChanged.RemoveListener(UpdateStrapLevel); }
        
        if (palmDown != null) { palmDown.onClick.RemoveListener(PalmVibro_IndexToPinky); }
        if (palmUp != null) { palmUp.onClick.RemoveListener(PalmVibro_PinkyToIndex); }

        if (resetNormalBtn != null) { this.resetNormalBtn.onClick.RemoveListener(ResetNormalization); }
        if (endNormalBtn != null) { this.endNormalBtn.onClick.RemoveListener(EndNormalization); }
        if (sendRawBtn != null) { this.sendRawBtn.onClick.RemoveListener(SendRawData); }
        if (sendNormBtn != null) { this.sendNormBtn.onClick.RemoveListener(SendNormalizedData); }

    }


}
