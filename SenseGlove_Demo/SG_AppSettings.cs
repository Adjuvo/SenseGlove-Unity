using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG.Util
{
	

	//------------------------------------------------------------------------------------------------------------------------------------
	// Event Args

	public class SettingChangedArgs : EventArgs
	{
		/// <summary> The script or object that made the change </summary>
		public object MadeFrom { get; set; }

		/// <summary> The Key  </summary>
		public string Key { get; set; }

		/// <summary>  </summary>
		/// <param name="theChanger"></param>
		/// <param name="changedKey"></param>
		public SettingChangedArgs(object changer, string changedKey)
		{
			MadeFrom = changer;
			Key = changedKey;
		}
	}



	//------------------------------------------------------------------------------------------------------------------------------------
	// Main Access Class

	/// <summary> Functions like PlayerPrefs, but stores the values instead in an INI file that is stored on disk. </summary>
	public static class SG_AppSettings
	{
		//------------------------------------------------------------------------------------------------------------------------------------
		// Member Variables

#if UNITY_EDITOR
		private static string iniDir = Application.dataPath + "\\";
#elif UNITY_ANDROID
		private static string iniDir = Application.persistentDataPath + "\\";
#else
		private static string iniDir = Application.dataPath + "\\..\\"; //up one folder from Data so that it's next to the .exe file.
#endif

		/// <summary> The name of the ini file. Change this if you want a more specific one. </summary>
		private static string iniName = "inifile.ini";

		/// <summary> Flexible key/value container to keep the  </summary>
		private static PortableSettings iniValues = null;

		//------------------------------------------------------------------------------------------------------------------------------------
		// Events

		/// <summary> Event delegate to send SettingChangedArgs to another script. </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public delegate void SettingChangedEventHandler(object sender, SettingChangedArgs e);

		/// <summary> This event fires when a value is changed. </summary>
		public static event SettingChangedEventHandler SettingChanged;

		/// <summary> Fire the SettingChanged event </summary>
		/// <param name="e"></param>
		private static void OnSettingChanged(string key, object source)
		{
			SettingChangedArgs e = new SettingChangedArgs(source, key);
			if (SettingChanged != null)
            {
				SettingChanged.Invoke(null, e);
            }
		}

		//------------------------------------------------------------------------------------------------------------------------------------
		// INI File Loading / Storing

		public static PortableSettings GetFileContents()
        {
			if (iniValues == null)
            {
				//TODO: Load the file or, failing that, create a new instance.
				string iniPath = iniDir + iniName;
				string[] iniContent;
				if (SGCore.Util.FileIO.ReadTxtFile(iniPath, out iniContent))
                {
					if (PortableSettings.Deserialize(iniContent, out iniValues))
                    {
						//Debug.Log("Loaded INI Settings;" + iniValues.ToString());
                    }
				}
				//if after all this it still does not exist...
				if (iniValues == null)
                {
					iniValues = new PortableSettings();
                }
			}
			return iniValues;
        }

		public static void StoreIniSettings()
		{
			if (iniValues != null)
			{
				string[] lines = iniValues.Serialize();
				if (SGCore.Util.FileIO.SaveTxtFile(iniDir, iniName, lines, false))
                {
					//Debug.Log("Saved INI settings:\n" + SG.Util.SG_Util.PrintArray(lines));
                }
				else
                {
					Debug.LogError("Could not store .ini file. Your changes will not be reflected outside of this session.");
                }
			}
		}



		//------------------------------------------------------------------------------------------------------------------------------------
		// Get / Set functions. Similar to PlayerPrefs.

		/// <summary> If a value for key exist, return it. Otherwise, return the defaultValue. </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static int GetInt(string key, int defaultValue)
		{
			return GetFileContents().GetInt(key, defaultValue);
		}

		/// <summary> Set a value in the INI settings. Will raise a SettingsChanged event. </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void SetInt(string key, int value, object source)
		{
			GetFileContents().SetInt(key, value);
			StoreIniSettings();
			OnSettingChanged(key, source); //let the other scripts know that this value has been changed, so they can update their visualization(s).
		}

		/// <summary> If a value for key exist, return it. Otherwise, return the defaultValue. </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static bool GetBool(string key, bool defaultValue)
		{
			return GetFileContents().GetBool(key, defaultValue);
		}

		/// <summary> Set a value in the INI settings. Will raise a SettingsChanged event </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void SetBool(string key, bool value, object source)
		{
			GetFileContents().SetBool(key, value);
			StoreIniSettings();
			OnSettingChanged(key, source); //let the other scripts know that this value has been changed, so they can update their visualization(s).
		}


		/// <summary> Prints the contents of all INI file settings, if it exists. </summary>
		/// <returns></returns>
		public static string PrintContents()
        {
			return GetFileContents().ToString();
		}


	}


	//------------------------------------------------------------------------------------------------------------------------------------
	// Flexible Container Class

	/// <summary> A class to contain a combination of key-value pairs. That can be serialized / deserialized on disk. </summary>
	public class PortableSettings
	{
		//-------------------------------------------------------------------------------------------------------------
		// Member Variables

		/// <summary> Contains all string-boolen pairs </summary>
		private Dictionary<string, bool> boolValues = new Dictionary<string, bool>();
		/// <summary> Contains all string-integer pairs. </summary>
		private Dictionary<string, int> intValues = new Dictionary<string, int>();

		/// <summary> Used for parsing serialized value(s). </summary>
		private const char serializeDelim = '|';
		/// <summary> Byte that indicates this is a serialized boolean </summary>
		private const char boolId = 'b';
		/// <summary> Byte that indicates this is a serialized integer value. </summary>
		private const char intId = 'i';

		//-------------------------------------------------------------------------------------------------------------
		// Constructor

		/// <summary> Creates a new, empty INI settings file </summary>
		public PortableSettings() { }


		/// <summary> Creates a new  </summary>
		/// <param name="bools"></param>
		/// <param name="ints"></param>
		public PortableSettings(Dictionary<string, bool> bools, Dictionary<string, int> ints)
		{
			this.boolValues = bools;
			this.intValues = ints;
		}


		//-------------------------------------------------------------------------------------------------------------
		// Member Functions

		/// <summary> Returns a string representation of all key/value pairs inside this object </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string res = "";
			foreach (KeyValuePair<string, bool> item in boolValues)
			{
				res += item.Key + ": " + item.Value + "\n";
			}
			foreach (KeyValuePair<string, int> item in intValues)
			{
				res += item.Key + ": " + item.Value + "\n";
			}
			return res;
		}

		/// <summary> Retrieve an integer value, if its key exists in memory. Otherwise, return thet DefaultValue. </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public int GetInt(string key, int defaultValue)
		{
			int val;
			if (intValues.TryGetValue(key, out val))
			{
				return val;
			}
			return defaultValue;
		}

		/// <summary> Stores an integer value in memory. If a key does not exist, it will be generated. </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetInt(string key, int value)
		{
			if (intValues.ContainsKey(key))
			{
				intValues[key] = value;
			}
			else
			{
				intValues.Add(key, value);
			}
		}


		/// <summary> Retrieve a boolean value, if its key exists in memory. Otherwise, return thet DefaultValue </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public bool GetBool(string key, bool defaultValue)
		{
			bool val;
			if (boolValues.TryGetValue(key, out val))
			{
				return val;
			}
			return defaultValue;
		}

		/// <summary> Stores a boolean value in memory. If a key does not exist, it will be generated. </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetBool(string key, bool value)
		{
			if (boolValues.ContainsKey(key))
			{
				boolValues[key] = value;
			}
			else
			{
				boolValues.Add(key, value);
			}
		}


		//-------------------------------------------------------------------------------------------------------------
		// Serialization / Deserialization

		/// <summary> Turn the contents of these settings into a string representation that can be stored on disk. </summary>
		/// <returns></returns>
		public string[] Serialize()
		{
			List<string> lines = new List<string>();

			foreach (KeyValuePair<string, bool> boolItem in boolValues)
			{
				string line = boolId.ToString() + serializeDelim + boolItem.Key + serializeDelim + (boolItem.Value ? "1" : "0");
				lines.Add(line);
			}
			foreach (KeyValuePair<string, int> intItem in intValues)
			{
				string line = intId.ToString() + serializeDelim + intItem.Key + serializeDelim + intItem.Value;
				lines.Add(line);
			}
			return lines.ToArray();
		}


		/// <summary> Split one line of a serialized settings file into its raw id|key|value representation </summary>
		/// <param name="line"></param>
		/// <param name="id"></param>
		/// <param name="key"></param>
		/// <param name="rawVal"></param>
		/// <returns></returns>
		public static bool InitialSplit(string line, out char id, out string key, out string rawVal)
		{
			if (line.Length > 0)
			{
				id = line[0];
				string[] split = line.Split(serializeDelim);
				if (split.Length > 2)
				{
					key = split[1];
					rawVal = split[2];
					return key.Length > 0 && rawVal.Length > 0;
				}
			}
			id = '!';
			key = "";
			rawVal = "";
			return false;
		}

		/// <summary> Convert the contents of a file back into a Settings Object. </summary>
		/// <param name="serialized"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		public static bool Deserialize(string[] serialized, out PortableSettings output)
		{
			try
			{
				output = new PortableSettings();
				for (int i = 0; i < serialized.Length; i++)
				{
					char id; string key; string rawVal;
					if (InitialSplit(serialized[i], out id, out key, out rawVal))
					{
						if (id == boolId)
						{
							output.SetBool(key, rawVal[0] == '1' ? true : false);
						}
						if (id == intId)
						{
							int parse;
							if (int.TryParse(rawVal, out parse))
							{
								output.SetInt(key, parse);
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError("Error Loading ini File: " + ex.Message);
			}
			output = new PortableSettings();
			return false;
		}




	}


}