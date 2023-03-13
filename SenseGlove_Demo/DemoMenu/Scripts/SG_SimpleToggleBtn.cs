using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary> Turns a regular button into a toggle-able one. </summary>
public class SG_SimpleToggleBtn : MonoBehaviour 
{
    /// <summary> The button that will be used to activate the toggle </summary>
	public Button linkedButton;

    /// <summary> Button Text. Optional. Granned automatically. </summary>
    [SerializeField] protected Text linkedText;

    /// <summary> Fires when clicked but after values are set. </summary>
    public UnityEvent Toggled = new UnityEvent();

    

    /// <summary> The Value of this toggle button (true / False). Used to check state changes. </summary>
    public bool Value
    { 
        get; protected set; 
    }


    public bool Interactable
    {
        get { return this.linkedButton != null ? this.linkedButton.interactable : false; }
        set { if (this.linkedButton != null) { this.linkedButton.interactable = value; } }
    }

    public string ButtonText
    {
        get { return this.linkedText != null ? this.linkedText.text : ""; }
        set { if (this.linkedText != null) { this.linkedText.text = value; } }
    }

    public Color TextColor
    {
        get { return this.linkedText != null ? this.linkedText.color : Color.magenta; }
        set { if (this.linkedText != null) { this.linkedText.color = value; } }
    }



    protected void ButtonClicked()
    {
        this.Value = !this.Value;
        this.Toggled.Invoke();
    }

	void OnEnable()
    {
        if (linkedButton == null)
        {
            linkedButton = this.GetComponent<Button>();
        }
        if (linkedText == null && linkedButton != null)
        {
            linkedText = linkedButton.GetComponentInChildren<Text>();
        }
        this.linkedButton.onClick.AddListener(ButtonClicked);
    }

	void OnDisable()
    {
        this.linkedButton.onClick.RemoveListener(ButtonClicked);
    }
}
