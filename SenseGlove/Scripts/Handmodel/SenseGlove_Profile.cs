using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GloveProfiles;
using System.IO;

/// <summary> Manages Calibration Profiles of different users. </summary>
[RequireComponent(typeof(SenseGlove_Object))]
[DisallowMultipleComponent]
public class SenseGlove_Profile : MonoBehaviour
{
    //--------------------------------------------------------------------------------------
    //  Properties
    
    /// <summary> The name of the profile that is assigned to this glove. </summary>
    [Tooltip("The name of the profile that is assigned to this glove.")]
    public string userName = "Default";

    /// <summary> SenseGlove_Object connected to this Sense Glove. </summary>
    private SenseGlove_Object senseGlove;

    /// <summary> Checks if the profile can be updated at this time (prevents spamming) </summary>
    private bool canUpdate = false;

    //--------------------------------------------------------------------------------------
    //  Profile / Class Methods

    /// <summary> If this user exists within the database, apply their hand profile to this SenseGlove_Object. </summary>
    /// <param name="userName"></param>
    public void SetProfile(string name)
    {
        if (userName != null && userName.Length > 0)
        {
            this.canUpdate = false;
            this.userName = name;
            UserProfiles.SetLastUser(this.userName, this.senseGlove.GloveData().deviceID);
            HandProfile newProfile = UserProfiles.GetProfile(name);
            this.SetProfile(newProfile);
            SenseGlove_Debugger.Log("Set Profile for " + this.userName);
            this.canUpdate = true; //prevent additional calls to fingerCalibrationFinished while we update the profile.
        }
        else
        {
            SenseGlove_Debugger.LogWarning("Invalid Username");
        }
    }

    /// <summary> Apply the desired hand profile to the Sense Glove connected to this Profile </summary>
    /// <param name="fingerLengths"></param>
    private void SetProfile(float[][] fingerLengths)
    {
        if (this.senseGlove != null)
        {
            this.senseGlove.SetFingerLengths(fingerLengths);
        }
    }

    /// <summary> Apply the desired hand profile to the Sense Glove connected to this Profile </summary>
    /// <param name="profile"></param>
    private void SetProfile(HandProfile profile)
    {
        this.SetProfile(profile.fingerLengths);
    }

    /// <summary> Apply the profile when the Sense Glove has loaded </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_OnGloveLoaded(object source, System.EventArgs args)
    {
        //check if a profile is already established for this glove. If not assign the name of this glove.
        string ID = this.senseGlove.GloveData().deviceID;
        string lastUsedName = UserProfiles.GetLastUser(ID);
        if (lastUsedName.Length > 0)
        {   //a lastUsedName exits
            this.userName = lastUsedName;
        }
        else
        {   //not been assigned yet, tell the database this is the new lastUser
            UserProfiles.SetLastUser(this.userName, ID);
        }
        this.SetProfile(UserProfiles.GetProfile(this.userName));
        SenseGlove_Debugger.Log("Applied profile for " + userName);
        this.canUpdate = true;
    }

    /// <summary> Fires when the Sense Glove has calculated new finger Lengths. </summary>
    /// <param name="source"></param>
    /// <param name="args"></param>
    private void SenseGlove_OnCalibrationFinished(object source, CalibrationArgs args)
    {
        if (this.canUpdate)
        {
            //the new gloveLengths have already been applied to the SenseGlove-Object, but should be updated in the database.
            HandProfile updatedProfile = new HandProfile(args.newFingerLengths);
            UserProfiles.AddEntry(this.userName, updatedProfile); //update / add entry
            SenseGlove_Debugger.Log("Updated " + this.userName);
        }
    }


    //--------------------------------------------------------------------------------------
    // Monobehaviour

    #region Monobehaviour

    // Use this for initialization
    void Start()
    {
        UserProfiles.LoadDB(Application.dataPath + UserProfiles.datafolder);

        this.senseGlove = this.gameObject.GetComponent<SenseGlove_Object>();
        if (this.senseGlove != null)
        {
            this.senseGlove.OnGloveLoaded += SenseGlove_OnGloveLoaded;
            this.senseGlove.OnCalibrationFinished += SenseGlove_OnCalibrationFinished;
        }
    }

    // End of application
    void OnApplicationQuit()
    {
        UserProfiles.SaveDB(Application.dataPath + UserProfiles.datafolder, true);
    }
    

    #endregion Monobehaviour




}

#region Database

namespace GloveProfiles //placed in a separate namespace so they do not pop up during coding.
{
    /// <summary> Database to manage user profiles  </summary>
    public class UserProfiles
    {
        /// <summary> Standard folder relative to the Application.DataPath. </summary>
        public static readonly string datafolder = "/SenseGlove/Data/";
        /// <summary> Standard Databasename. </summary>
        public static readonly string databaseName = "UserProfiles.txt";

        /// <summary> Contains the glove profiles for each user. </summary>
        private static Dictionary<string, HandProfile> profiles = new Dictionary<string, HandProfile>();


        /// <summary> Remembers gloveID's and applies these to the appropriate deviceID. [DeviceID, ProfileName] </summary>
        private static Dictionary<string, string> lastProfiles = new Dictionary<string, string>();

        /// <summary> Prevents us from loading the Database more than once at the start of the program. </summary>
        private static bool DBLoaded = false;

        /// <summary> Prevents us from saving the database more than once at the end of the program </summary>
        private static bool DBsaved = false;

        /// <summary> Load the Database, if not already done. </summary>
        /// <param name="directory"></param>
        public static void LoadDB(string directory)
        {
            if (!DBLoaded)
            {
                string[] lines;
                if (Util.FileIO.ReadTxtFile(directory + databaseName, out lines))
                {
                    DBLoaded = true;

                    List<string> block = new List<string>();

                    int line = 1; //skip the @lastProfiles

                    //fill the block with all of the "LastProfiles"
                    while (line <= lines.Length && !(lines[line].Length > 0 && lines[line][0] == '@'))
                    {
                        if (line < lines.Length)
                        {
                            block.Add(lines[line]);
                            line++;
                        }
                    }

                    //process lastProfiles
                    for (int i=0; i<block.Count; i++)
                    {
                        string[] split = block[i].Split('\t');
                        if (split.Length > 1)
                        {
                            lastProfiles.Add(split[0], split[1]);
                        }
                    }

                    block.Clear();
                    line++;
                    string lastEntry = "";
                    while (line <= lines.Length)
                    {
                        if (line >= lines.Length || (lines[line].Length > 0 && lines[line][0] == '#')) //its a new Material
                        {
                            if (block.Count > 0 && lastEntry.Length > 0) //parse & add previous Material if it has a good name
                            {
                                //UserProfiles.AddEntry(lastEntry, HandProfile.Parse(block));
                                profiles.Add(lastEntry, HandProfile.Parse(block));
                            }
                            block.Clear();

                            if (line < lines.Length)
                            {
                                try //extract name of new material
                                {
                                    lastEntry = lines[line].Split(new char[] { '\t' }, System.StringSplitOptions.RemoveEmptyEntries)[1]; //condition name
                                }
                                catch (System.Exception Ex)
                                {
                                    SenseGlove_Debugger.LogWarning(Ex.Message);
                                    lastEntry = "";
                                }
                            }
                        }
                        if (line < lines.Length)
                        {
                            block.Add(lines[line]);
                        }
                        line++;
                    }

                    DBLoaded = true;
                    //SenseGlove_Debugger.Log("Loaded Database!");
                }
                else
                {
                    //could not load this data base asset
                }
            }
        }

        /// <summary> Saves the entire DataBase to a specific path. </summary>
        public static void SaveDB(string directory, bool onExit = false)
        {
            if (!onExit || (onExit && !DBsaved))
            {

                List<string> lines = new List<string>();
                //print LastProfiles.
                lines.Add("@lastProfiles");

                foreach (KeyValuePair<string, string> pair in lastProfiles)
                {
                    lines.Add(pair.Key + "\t" + pair.Value);
                }
            
                //Print Profiles.
                lines.Add("@profiles");
                HandProfile prof;

                foreach (KeyValuePair<string, HandProfile> pair in profiles)
                {
                    prof = pair.Value;
                    if (prof.fingerLengths.Length > 0)
                    {
                        lines.Add("#\t" + pair.Key);

                        for (int f = 0; f < prof.fingerLengths.Length; f++)
                        {
                            string line = ((SenseGloveCs.Finger)(f)).ToString() + "\t";
                            for (int i = 0; i < prof.fingerLengths[f].Length; i++)
                            {
                                line += prof.fingerLengths[f][i];
                                if (i < prof.fingerLengths[f].Length - 1) { line += '\t'; }
                            }
                            lines.Add(line);
                        }
                    }
                }

                bool saved = Util.FileIO.SaveTxtFile(directory, databaseName, lines.ToArray(), false);
                if (saved)
                {
                    //SenseGlove_Debugger.Log("Saved Database!");
                    if (onExit) { DBsaved = true; }
                }
            }
        }

        /// <summary> Check if an entry has been loaded yet. </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsLoaded(string entry)
        {
            HandProfile res;
            return profiles.TryGetValue(entry, out res);
        }

        /// <summary> Add an entry to the profile if it is not loaded. </summary>
        /// <param name="userName"></param>
        /// <param name="profile"></param>
        public static void AddEntry(string userName, HandProfile profile)
        {
            //if (DBLoaded)
            {
                if (!IsLoaded(userName))
                {
                    UserProfiles.profiles.Add(userName, profile);
                }
                else //update the key-value pair
                {
                    profiles[userName] = profile;
                }
            }
        }

        /// <summary> Retrieve a Hand Profile from the database, if loaded. </summary>
        /// <param name="profileName"></param>
        /// <returns></returns>
        public static HandProfile GetProfile(string profileName)
        {
            HandProfile res;
            if (profiles.TryGetValue(profileName, out res))
            {
                return res;
            }
            return HandProfile.empty;
        }
        
        /// <summary>  Retrieve the last user to use a glove identfied by and ID. </summary>
        /// <param name="deviceID"></param>
        /// <returns></returns>
        public static string GetLastUser(string deviceID)
        {
            string res;
            if (lastProfiles.TryGetValue(deviceID, out res))
            {
                return res;
            }
            return "";
        }

        /// <summary> Tell the database that this user is the last used for this deviceID </summary>
        /// <param name="deviceID"></param>
        public static void SetLastUser(string userName, string deviceID)
        {
            if (userName.Length > 0)
            {
                if (GetLastUser(deviceID).Length > 0) //check if already loaded.
                {
                    lastProfiles[deviceID] = userName;
                }
                else //new entry
                {
                    lastProfiles.Add(deviceID, userName);
                }
            }
            else
            {
                SenseGlove_Debugger.LogWarning("Warning: Invalid Username");
            }
        }
    }


    /// <summary> Contains the hand profile of a single user. </summary>
    public class HandProfile
    {
        /// <summary> xyz finger lengths for each finger. </summary>
        public float[][] fingerLengths;
        
        /// <summary> Create a new handprofile, which is used to store profiles etc. </summary>
        /// <param name="lengths"></param>
        public HandProfile(float[][] lengths)
        {
            this.fingerLengths = lengths;
        }

        /// <summary>
        /// Parse a new hand profile from a raw string representation coming from a hand profile database.
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static HandProfile Parse(List<string> raw)
        {
            if (raw.Count > 5) //assume the first row is the name.
            {
                float[][] lengths = new float[5][];
                for (int f = 1; f < 6; f++)
                {
                    string[] split = raw[f].Split('\t');
                    if (split.Length > 3)
                    {
                        float[] L = new float[3];
                        for (int i=1; i<4; i++)
                        {
                            L[i - 1] = SenseGloveCs.Values.toFloat(split[i]);
                        }
                        lengths[f - 1] = L;
                    }
                    else
                    {
                        lengths[f - 1] = new float[0];
                    }
                }
                return new HandProfile(lengths);
            }
            return HandProfile.empty; //empty
        }

        /// <summary> A default handprofile that will not update the Sense Glove Kinematic Model. </summary>
        public static readonly HandProfile empty = new HandProfile(new float[][] { }); 

    }


}

#endregion Database