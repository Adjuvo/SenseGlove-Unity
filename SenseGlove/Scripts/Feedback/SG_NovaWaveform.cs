using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SG
{
    /// <summary> Wrapper around Custom Waveforms created specifically for the Nova Glove. </summary>
    [CreateAssetMenu(fileName = "CustomWaveform", menuName = "SenseGlove/Nova Waveform", order = 1)]
    public class SG_NovaWaveform : ScriptableObject
    {
        /// <summary> The actuator for which this waveform is meant. Useful for conversions, backups and testing. </summary>
        public SGCore.Nova.Nova_VibroMotor intendedMotor = SGCore.Nova.Nova_VibroMotor.BackOfHand;

        [Header("Waveform Parameters")]
        [ Range(0.0f, 1.0f) ] public float amplitude = 1.0f;
        public SGCore.Nova.WaveformType waveformType = SGCore.Nova.WaveformType.Sine;

        [Header("Timing Parameters")]
        [ Range(SGCore.Nova.Nova_Waveform.minAttackTime,  SGCore.Nova.Nova_Waveform.maxAttackTime) ]  public float attackTime = 0.0f;
        [ Range(SGCore.Nova.Nova_Waveform.minSustainTime, SGCore.Nova.Nova_Waveform.maxSustainTime) ] public float sustainTime = 1.0f;
        [ Range(SGCore.Nova.Nova_Waveform.minDecayTime,   SGCore.Nova.Nova_Waveform.maxDecayTime) ]   public float decayTime = 0.0f;

        [Range(1, SGCore.Nova.Nova_Waveform.maxRepeatAmount)] public int RepeatAmount = 1;
        public bool RepeatInfinite = false;

        [Header("Frequency Parameters")]
        [ Range(SGCore.Nova.Nova_Waveform.freqRangeMin, SGCore.Nova.Nova_Waveform.freqRangeMax) ] public int startFrequency = 60;
        [ Range(SGCore.Nova.Nova_Waveform.freqRangeMin, SGCore.Nova.Nova_Waveform.freqRangeMax) ] public int endFrequency = 60;
        
        [ Range(0.0f, 1.0f) ] public float frequencySwitchTime = 0.0f;
        [ Range(SGCore.Nova.Nova_Waveform.minFreqFactor, SGCore.Nova.Nova_Waveform.maxFreqFactor) ] public float frequencySwitchMultiplier = 1.0f;


        protected SGCore.Nova.Nova_Waveform iWaveForm = new SGCore.Nova.Nova_Waveform();

        /// <summary> using this for OnValidate to check when a (new) file is linked. </summary>
        private TextAsset previousInputFile = null; 


        [Header("Importing from Haptic Generator")]
        [SerializeField] protected TextAsset hapticGeneratorFile;

        /// <summary> FileType(s) Supported for Custom Waveforms. </summary>
        public const string wfFileType = ".json";


        /// <summary> Returns a (copy of) a waveform defined by the parameters in this class. </summary>
        /// <returns></returns>
        public virtual SGCore.Nova.Nova_Waveform GetWaveform()
        {
            this.RegenerateWaveform(); //just in case something has changed in the meantime. though I'm sure there's smarter ways to go about this...
            return new SGCore.Nova.Nova_Waveform(this.iWaveForm); //copy
        }


        /// <summary> Recreate a new Nova_Waveform based on the parameters set inside the inspector. </summary>
        public virtual void RegenerateWaveform()
        {
            iWaveForm = new SGCore.Nova.Nova_Waveform();
            iWaveForm.Amplitude = this.amplitude;
            iWaveForm.AttackTime = this.attackTime;
            iWaveForm.DecayTime = this.decayTime;
            iWaveForm.FrequencyEnd = this.endFrequency;
            iWaveForm.FrequencyStart = this.startFrequency;
            iWaveForm.FrequencySwitchFactor = this.frequencySwitchMultiplier;
            iWaveForm.FrequencySwitchTime = this.frequencySwitchTime;
            iWaveForm.Infinite = this.RepeatInfinite;
            iWaveForm.RepeatAmount = this.RepeatAmount;
            iWaveForm.SustainTime = this.sustainTime;
            iWaveForm.WaveType = this.waveformType;

            //Debug.Log("Regenerated Waveform " + iWaveForm.ToString());
        }





        protected void LoadFromFileContents(string fileContents)
        {
            if (fileContents.Length == 0)
            {
                Debug.LogError(this.name + " failed to load from file: File is empty!", this);
                return;
            }

            Parsing.HapticGeneratorOutput parsedOutput = JsonUtility.FromJson<Parsing.HapticGeneratorOutput>(fileContents);

            if (parsedOutput.Envelope == null || parsedOutput.Frequency == null || parsedOutput.Duties == null || parsedOutput.Basics == null)
            {
                Debug.LogError(this.name + " failed to load from file: Could not parse JSON Data", this);
                return;
            }

            //Actually apply the data
            this.amplitude = parsedOutput.Basics.Master;
            this.waveformType = (SGCore.Nova.WaveformType)(parsedOutput.Type + 1); //+1 because it uses the online index, and not a valid value.
            this.RepeatInfinite = parsedOutput.Infinite;
            this.RepeatAmount = parsedOutput.Basics.Repeat;

            this.attackTime = SG.Util.SG_Util.Map(parsedOutput.Envelope.Attack, 0, 8000, 0.0f, 1.0f); //LEGACY: The Envelopes are given in frames, not in seconds?
            this.sustainTime = SG.Util.SG_Util.Map(parsedOutput.Envelope.Sustain, 0, 8000, 0.0f, 1.0f); //LEGACY: The Envelopes are given in frames, not in seconds?
            this.decayTime = SG.Util.SG_Util.Map(parsedOutput.Envelope.Decay, 0, 8000, 0.0f, 1.0f); //LEGACY: The Envelopes are given in frames, not in seconds?

            this.startFrequency = parsedOutput.Frequency.Start;
            this.endFrequency = parsedOutput.Frequency.End;
            this.frequencySwitchMultiplier = parsedOutput.Duties.FrequencySwitch;
            this.frequencySwitchTime = parsedOutput.Duties.SwitchTime;

            this.intendedMotor = (SGCore.Nova.Nova_VibroMotor)parsedOutput.Actuator;
        }


        public void LoadFromFile()
        {
            this.LoadFromFile(this.hapticGeneratorFile);
        }

        public void LoadFromFile(TextAsset jsonFile)
        {
            if (jsonFile == null)
            {
                Debug.LogError(this.name + " failed to load from file: TextAsset is not assigned!", this);
                return;
            }
            LoadFromFileContents(jsonFile.text);
        }


        public void LoadFromFile(string filePath)
        {
            if (filePath.Length == 0)
            {
                Debug.LogError(this.name + " failed to load from file: Empty file path", this);
                return;
            }
            if ( !System.IO.File.Exists(filePath) )
            {
                Debug.LogError(this.name + " failed to load from file: No such file exists...", this);
                return;
            }

            string ext = System.IO.Path.GetExtension(filePath);
            if ( !ext.Equals(wfFileType, System.StringComparison.OrdinalIgnoreCase) )
            {
                Debug.LogError(this.name + " failed to load from file: Expected " + wfFileType + " but was given " + ext, this);
                return;
            }

            string rawContents;
            try
            {
                rawContents = System.IO.File.ReadAllText(filePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(this.name + " failed to load from file: " + ex.Message + "\n" + ex.StackTrace, this);
                return;
            }
            LoadFromFileContents(rawContents);
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (this.hapticGeneratorFile !=  null && this.hapticGeneratorFile != this.previousInputFile) //it's changed!
            {
                this.previousInputFile = hapticGeneratorFile;
                //Debug.Log("File link changed! Reloading!");
                this.LoadFromFile(this.hapticGeneratorFile);
            }
        }
#endif
    }


    //Utility Class to read Haptic Generator Output.

    namespace Parsing
    {
        //a bit of JSON voodoo to be able to read the output from the Hapitc Generator

        [System.Serializable]
        public class Frequency
        {
            public int Start;
            public int End;

            public Frequency() { }
        }

        [System.Serializable]
        public class Envelope
        {
            public int Attack;
            public int Sustain;
            public int Decay;

            public Envelope() { }
        }

        [System.Serializable]
        public class Duties
        {
            public float FrequencySwitch;
            public float SwitchTime;

            public Duties() { }
        }

        [System.Serializable]
        public class Basics
        {
            public int Repeat;
            public float Master;

            public Basics() { }
        }

        [System.Serializable]
        public class HapticGeneratorOutput
        {
            public Frequency Frequency;
            public Envelope Envelope;
            public Duties Duties;
            public Basics Basics;
            public bool Infinite;
            public int Actuator;
            public int Type;
            public int Presets;

            public HapticGeneratorOutput() 
            {
                Frequency = new Frequency();
                Envelope = new Envelope();
                Duties = new Duties();
                Basics = new Basics();
            } //base constructor for me.
        }


    }



    
    // Custom Inspector for this class for button(s).
#if UNITY_EDITOR


    [CustomEditor(typeof(SG_NovaWaveform))]
    public class LevelScriptEditor : Editor
    {

        SerializedProperty fileStreamAsset;


        private void OnEnable()
        {
            fileStreamAsset = serializedObject.FindProperty("hapticGeneratorFile");
        }

        public override void OnInspectorGUI()
        {
            SG_NovaWaveform myTarget = (SG_NovaWaveform)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Reset from file"))
            {
                myTarget.LoadFromFile();
            }

            if (GUILayout.Button("Save to file"))
            {
                TrySaveFile(myTarget, fileStreamAsset);
            }

            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.Space();
            GUILayout.Label("Live Testing", UnityEditor.EditorStyles.boldLabel);
            if (GUILayout.Button("Test Waveform"))
            {
                TryTestWaveform(myTarget);
            }

        }

        public static void TrySaveFile(SG_NovaWaveform target, SerializedProperty fileAsset)
        {
            if (fileAsset.objectReferenceValue == null)
            {
                Debug.LogError(target.name + " failed to load from file: No hapticGeneratorFile was assigned.", target);
                return;
            }
            //a file was certainly assigned. So we should be able to pass it on without checking for length(s).
            string assetPath = AssetDatabase.GetAssetPath(fileAsset.objectReferenceValue.GetInstanceID());



            Parsing.HapticGeneratorOutput output = new Parsing.HapticGeneratorOutput();
            output.Actuator = (int) target.intendedMotor;
            output.Basics.Master = target.amplitude;
            output.Basics.Repeat = target.RepeatAmount;
            output.Duties.FrequencySwitch = target.frequencySwitchMultiplier;
            output.Duties.SwitchTime = target.frequencySwitchTime;
            output.Envelope.Attack = Mathf.RoundToInt( SG.Util.SG_Util.Map(target.attackTime, 0.0f, 1.0f, 0, 8000, true) ); //LEGACY: The Envelopes are given in frames, not in seconds?
            output.Envelope.Sustain = Mathf.RoundToInt( SG.Util.SG_Util.Map(target.sustainTime, 0.0f, 1.0f, 0, 8000, true) ); //LEGACY: The Envelopes are given in frames, not in seconds?
            output.Envelope.Decay = Mathf.RoundToInt( SG.Util.SG_Util.Map(target.decayTime, 0.0f, 1.0f, 0, 8000, true) ); //LEGACY: The Envelopes are given in frames, not in seconds?
            output.Frequency.Start = target.startFrequency;
            output.Frequency.End = target.endFrequency;
            output.Infinite = target.RepeatInfinite;
            output.Presets = 0;
            output.Type = ((int)target.waveformType) - 1;

            string serialized = JsonUtility.ToJson(output, true).Replace("    ", "\t"); //PrettryPrint uses four spaces. I prefer a tab


            Parsing.HapticGeneratorOutput test = JsonUtility.FromJson<Parsing.HapticGeneratorOutput>(serialized);

            if (test.Envelope == null || test.Frequency == null || test.Duties == null || test.Basics == null)
            {
                Debug.LogError(target.name + " failed to save file: Could not parse JSON Data", target);
                return;
            }

            try
            {
                System.IO.File.WriteAllText(assetPath, serialized);
                AssetDatabase.ImportAsset(assetPath); // "Touch the file so Unity acknowledges the changes."
                Debug.Log("Save " + target.name + " to " + assetPath, target);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(target.name + " failed to save to file: " + ex.Message + "\n" + ex.StackTrace, target);
                return;
            }


        }

        public static void TryTestWaveform(SG_NovaWaveform target)
        {
            if (!SGCore.SenseCom.ScanningActive())
            {
                Debug.Log("Cannot test " + target.name + ": SenseCom isn't running!", target);
                SGCore.SenseCom.StartupSenseCom();
                return;
            }


            SGCore.Nova.NovaGlove[] gloves = SGCore.Nova.NovaGlove.GetNovaGloves(); //give me all Nova Gloves that are CONNECTED
            if (gloves.Length == 0)
            {
                Debug.Log("Cannot test " + target.name + ": No Nova Gloves connected!", target);
                return;
            }

            Debug.Log("Playing " + target.name, target);
            SGCore.Nova.Nova_Waveform wave = target.GetWaveform();
            for (int i=0; i<gloves.Length; i++)
            {
                gloves[i].SendCustomWaveform(wave, target.intendedMotor);
            }

        }


    }
#endif








    

}


