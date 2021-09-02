using UnityEngine;


namespace SG
{

	/// <summary> A script with access to the latest handProfiles generated on this system. </summary>
	public class SG_HandProfiles
	{
		//-------------------------------------------------------------------------------------------------------------------------
		// Member Variables


		/// <summary> Profile for the left hand </summary>
		private static SGCore.HandProfile leftProfile = null;
		/// <summary> Profile for the right hand </summary>
		private static SGCore.HandProfile rightProfile = null;
		
		/// <summary> The location on disk where the profiles are stoed and retrieved from. Differs in Windows and Android. </summary>
		private static string profileDir = "";
		/// <summary> Name of the file containing the left-hand profile in ProfileDirectory </summary>
		private static string leftHandFile = "leftHandProfile.txt";
		/// <summary> Name of the file containing the right-hand profile in ProfileDirectory </summary>
		private static string rightHandFile = "rightHandProfile.txt";

		/// <summary> A SubDirectory of ProfileDirectory to store the last calibration ranges used for a device. </summary>
		private static string rangeDir = "Ranges/";

		/// <summary> Determines if we hava tried to load a profile from disk yet. If yes, and it still doesn't exist, we generate one. </summary>
		private static bool triedLoading = false;


		//-------------------------------------------------------------------------------------------------------------------------
		// Accessors

		/// <summary> Returns the directory where our HandProfiles are stored. </summary>
		public static string ProfileDirectory
        {
            get
            {
				if (profileDir.Length == 0) //The android path is different, and cannot be defined as static. SO we check it once.
				{
#if UNITY_ANDROID && !UNITY_EDITOR
					profileDir = Application.persistentDataPath + "/"; 
#else
					profileDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "/SenseGlove/";
#endif
				}
				return profileDir;
			}
		}

		/// <summary> Access the latest Right Hand Profile </summary>
		public static SGCore.HandProfile RightHandProfile
		{
			get
			{
				if (!triedLoading) { TryLoadFromDisk(); }
				return rightProfile;
			}
			set
			{
				if (value == null) { throw new System.ArgumentNullException(); }
				rightProfile = value;
				if (SG.Util.FileIO.SaveTxtFile(ProfileDirectory, rightHandFile, new string[] { rightProfile.Serialize() }))
				{
					triedLoading = true; //we don't need to load again, as the latest is in out memory
				}
				else
				{
					Debug.LogError("There was an error while saving the right hand Profile!");
				}
			}
		}

		/// <summary> Access the latest Left Hand Profile </summary>
		public static SGCore.HandProfile LeftHandProfile
        {
            get
            {
				if (!triedLoading) { TryLoadFromDisk(); }
				return leftProfile;
			}
            set
            {
				if (value == null) { throw new System.ArgumentNullException(); }
				leftProfile = value;
				if (SG.Util.FileIO.SaveTxtFile(ProfileDirectory, leftHandFile, new string[] { leftProfile.Serialize() }))
				{
					triedLoading = true; //we don't need to load again, as the latest is in out memory
				}
				else
                {
					Debug.LogError("There was an error while saving the left hand Profile!");
				}
			}
        }



		//-------------------------------------------------------------------------------------------------------------------------
		// Profile Accessing Functions

		/// <summary> Retrieve a left- or right handed profile. </summary>
		/// <param name="rightHand"></param>
		/// <returns></returns>
		public static SGCore.HandProfile GetProfile(bool rightHand)
		{
			return rightHand ? RightHandProfile : LeftHandProfile;
		}

		/// <summary> Store a profile in the global variables and on disk. The profile determines if this is a left- or right hand. </summary>
		/// <param name="profile"></param>
		public static void SetProfile(SGCore.HandProfile profile)
		{
			SetProfile(profile, profile.IsRight);
		}

		/// <summary> Store a profile in the global variables and on disk. You determine if this is a left- or right hand. </summary>
		/// <param name="profile"></param>
		/// <param name="rightHand"></param>
		public static void SetProfile(SGCore.HandProfile profile, bool rightHand)
		{
			//since we're working off the accessors, profiles are automatically stored.
			if (rightHand) { RightHandProfile = profile; } 
			else { LeftHandProfile = profile; }
		}




		/// <summary> Restore both profiles back to their default values. </summary>
		public static void RestoreDefaults()
		{
			RestoreDefaults(true);
			RestoreDefaults(false);
		}

		/// <summary> Restore the left- or right hand profiles back to their default values. </summary>
		/// <param name="rightHand"></param>
		public static void RestoreDefaults(bool rightHand)
		{
			SGCore.HandProfile newProfile = SGCore.HandProfile.Default(rightHand);
			Debug.Log("Restoring " + (rightHand ? "Right" : "Left") + " hand to Default Values");
			SetProfile(newProfile, rightHand);
		}




		//-------------------------------------------------------------------------------------------------------------------------
		// Store / Load functions


		/// <summary> Load the latest profiles from disk. Automatically called when you first try to access a profile.
		/// Exposed so you can force-reload profiles. </summary>
		public static void TryLoadFromDisk()
        {
			if (!LoadProfile(ProfileDirectory + leftHandFile, ref leftProfile)) //returns false if it couldn't be loaded.
            {
				if (leftProfile == null) { leftProfile = SGCore.HandProfile.Default(false); }
            }
			if (!LoadProfile(ProfileDirectory + rightHandFile, ref rightProfile)) //returns false if it couldn't be loaded.
			{
				if (rightProfile == null) { rightProfile = SGCore.HandProfile.Default(true); }
			}
			triedLoading = true;
		}

		/// <summary> Load a profile form a file, return true if it was succesfully deserialized. </summary>
		/// <param name="filePath"></param>
		/// <param name="currProfile"></param>
		/// <returns></returns>
		private static bool LoadProfile(string filePath, ref SGCore.HandProfile currProfile)
        {
			string[] lines;
			if (SG.Util.FileIO.ReadTxtFile(filePath, out lines) && lines.Length > 0)
            {
                try
                {
					currProfile = SGCore.HandProfile.Deserialize(lines[0]);
					return true;
				}
				catch (System.Exception ex)
                {
					Debug.LogError("Error attempting to collect a profile: " + ex.Message);
                }
            }
			return false;
        }




		/// <summary> Stores the last sensor range of a glove. </summary>
		/// <param name="currentRange"></param>
		/// <param name="forGlove"></param>
		/// <returns></returns>
		public static bool SaveLastRange(SGCore.Calibration.SensorRange currentRange, SGCore.HapticGlove forGlove)
		{
			if (forGlove != null)
			{
				string name = forGlove.GetDeviceID() + ".txt";
				return SG.Util.FileIO.SaveTxtFile(ProfileDirectory + rangeDir, name, new string[] { currentRange.Serialize() });
			}
			return false;
		}

		/// <summary> Loads the last sensor range of a glove. </summary>
		/// <param name="forGlove"></param>
		/// <param name="lastRange"></param>
		/// <returns></returns>
		public static bool LoadLastRange(SGCore.HapticGlove forGlove, out SGCore.Calibration.SensorRange lastRange)
		{
			if (forGlove != null)
			{
				string name = forGlove.GetDeviceID() + ".txt";
				string[] lines;
				if (SG.Util.FileIO.ReadTxtFile(ProfileDirectory + rangeDir + name, out lines) && lines.Length > 0 && lines[0].Length > 0)
				{
					lastRange = SGCore.Calibration.SensorRange.Deserialize(lines[0]);
					return true;
				}
			}
			lastRange = new SGCore.Calibration.SensorRange();
			return false;
		}


	}
}