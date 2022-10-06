using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//A slider component that can be controlled via a textbox.
// moving the slider updates the textbox. Inputting into the textbox updates the slider
public class SG_InputSlider : MonoBehaviour
{
    public Slider slider;
    public InputField textField;
    public Text title;
    private bool alreadyEditing = false; //prevents infinte loops. 

    public float SlideValue
    {
        get { return slider.value; }
        set { slider.value = value; }
    }

    public string Title
    {
        get { return title != null ? title.text : ""; }
        set { if (title != null) { title.text = value; } }
    }


    private void UpdateTextFromSlider(float newValue)
    {
        if (!alreadyEditing)
        {
            alreadyEditing = true;
            //Debug.Log("Slider: Text should be " + newValue);
            textField.text = newValue.ToString();
            alreadyEditing = false;
        }
    }

    private void UpdateSliderFromText(string newContent)
    {
        if (!alreadyEditing)
        {
            //Debug.Log("Text: Slider should be " + newContent);
            alreadyEditing = true;
            if (newContent.Length > 0)
            {
                slider.value = SGCore.Util.StrStuff.ToFloat(newContent);
                textField.text = slider.value.ToString();
            }
            else
            {
                slider.value = 0;
            }
            alreadyEditing = false;
        }
    }


	// Use this for initialization
	void OnEnable ()
    {
        this.slider.wholeNumbers = true;
        slider.onValueChanged.AddListener(UpdateTextFromSlider);
        textField.onValueChanged.AddListener(UpdateSliderFromText);
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(UpdateTextFromSlider);
        textField.onValueChanged.RemoveListener(UpdateSliderFromText);
    }
	
    void Start()
    {
        int maxSize = Mathf.Max(slider.minValue.ToString().Length, slider.maxValue.ToString().Length);
        textField.characterLimit = maxSize; //ensures we can input the correct numbers
        UpdateTextFromSlider(slider.value);
    }

	// Update is called once per frame
	void Update ()
    {
#if ENABLE_INPUT_SYSTEM //if Unitys new Input System is enabled....
#else
        //if (Input.GetKeyDown(KeyCode.D))
        //   {
        //       slider.value = 100;
        //   }
        //   else if (Input.GetKeyDown(KeyCode.A))
        //   {
        //       slider.value = 0;
        //   }
#endif
    }
}
