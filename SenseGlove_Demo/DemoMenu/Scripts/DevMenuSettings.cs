using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DevMenu_Settings", menuName = "SenseGlove/DevMenuSettings")]
public class DevMenuSettings : ScriptableObject 
{
	[Header("Menu Title")]
	public bool autoTitle = true; //autmoagically add product name.
	public bool autoVersionInfo = true; //autmoagically add version info.

	[Header("Regular Buttons")]
	public Color defaultBtnColor = Color.white;
	public Color defaultTextColor = new Color(50f/255f, 50f/255f, 50f/255f, 1.0f); //default Unity text color.

	[Header("Toggled Buttons")]
	public Color toggledBtnColor = new Color(35f/255f, 169f/255f, 225f/255f, 1.0f); // Soft SenseCom Blue
	public Color toggledTextColor = Color.white;






}
