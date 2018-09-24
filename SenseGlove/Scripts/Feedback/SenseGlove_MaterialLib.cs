using System.Collections.Generic;
using UnityEngine;

using SenseGloveMats;

namespace SenseGloveMats
{

    /// <summary> Contains the editable Material Properties of a single SenseGlove_Material </summary>
    public struct MaterialProps
    {
        /// <summary> The maximum force that this material can put on the Sense Glove. </summary>
        public int maxForce;

        /// <summary> The distance [m] where the maximum force has been reached. Setting it to 0 will instantly send maxForce on touch </summary>
        public float maxForceDist;

        /// <summary> The distance [m] at which the material breaks. </summary>
        public float yieldDist;

        /// <summary> The magnitude [0..100%] of the buzz motor pulse </summary>
        public int hapticForce;

        /// <summary> The duration of the Haptic Feedback, in miliseconds </summary>
        public int hapticDur;

        /// <summary> Convert a SenseGlove_Material into a MaterialProps, which can be passed between scripts or stored later on. </summary>
        /// <param name="material"></param>
        public MaterialProps(SenseGlove_Material material)
        {
            this.maxForce = material.maxForce;
            this.maxForceDist = material.maxForceDist;
            this.yieldDist = material.yieldDistance;
            this.hapticForce = material.hapticMagnitude;
            this.hapticDur = material.hapticDuration;
        }

        /// <summary> Retrieve a 'default' material. </summary>
        /// <returns></returns>
        public static MaterialProps Default()
        {
            MaterialProps res = new MaterialProps();
            res.maxForce = 100;
            res.maxForceDist = 0;
            res.yieldDist = float.MaxValue;
            res.hapticDur = 100;
            res.hapticForce = 100;
            return res;
        }

        /// <summary> Parse a DataBlock into a MaterialProps. Any missing variables will be set to their default value. </summary>
        /// <param name="dataBlock"></param>
        /// <returns></returns>
        public static MaterialProps Parse(List<string> dataBlock)
        {
            MaterialProps res = MaterialProps.Default();
            if (dataBlock.Count > 1)
            {
                float parsedValue;
                if (dataBlock.Count > (int)MatProp.maxForce && TryGetFloat(dataBlock[(int)MatProp.maxForce], out parsedValue))
                {
                    res.maxForce = (int)parsedValue;
                }
                if (dataBlock.Count > (int)MatProp.maxForceDist && TryGetFloat(dataBlock[(int)MatProp.maxForceDist], out parsedValue))
                {
                    res.maxForceDist = parsedValue;
                }
                if (dataBlock.Count > (int)MatProp.yieldDist && TryGetFloat(dataBlock[(int)MatProp.yieldDist], out parsedValue))
                {
                    if (float.IsNaN(parsedValue)) { parsedValue = float.MaxValue; }
                    res.yieldDist = parsedValue;
                }
                if (dataBlock.Count > (int)MatProp.hapticMagn && TryGetFloat(dataBlock[(int)MatProp.hapticMagn], out parsedValue))
                {
                    res.hapticForce = (int)parsedValue;
                }
                if (dataBlock.Count > (int)MatProp.hapticDur && TryGetFloat(dataBlock[(int)MatProp.hapticDur], out parsedValue))
                {
                    res.hapticDur = (int)parsedValue;
                }
            }
            return res;
        }
    
        /// <summary> Attempt to retieve the (raw) value of this material property. </summary>
        /// <param name="line"></param>
        /// <param name="raw"></param>
        /// <returns></returns>
        private static bool TryGetRawValue(string line, out string raw)
        {
            try
            {
                raw = line.Split(new char[] { '\t' }, System.StringSplitOptions.RemoveEmptyEntries)[1];
                return true;
            }
            catch
            {
                raw = "";
                return false;
            }
        }

        /// <summary> Attempt to convert a specific property to a floating point. </summary>
        /// <param name="line"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        private static bool TryGetFloat(string line, out float res)
        {
            string raw;
            if (TryGetRawValue(line, out raw))
            {
                res = SenseGloveCs.Values.toFloat(raw);
                return true;
            }
            res = float.NaN;
            return false;
        }

    }
    

    /// <summary> Used to parse enties within material databases. </summary>
    internal enum MatProp
    {
        Name,
        maxForce,
        maxForceDist,
        yieldDist,
        hapticMagn,
        hapticDur,
        All
    }

    /// <summary> Static class to keep track of all the material libraries that have been loaded in. </summary>
    public static class MaterialLibraries
    {
        /// <summary> Used to access the material library (props) </summary>
        private static Dictionary<string, MaterialProps> materials = new Dictionary<string, MaterialProps>();

        /// <summary> A secndary index to load multiple dictionaries into the game. </summary>
        private static List<string> libraryNames = new List<string>();

        /// <summary> Load a materials library from a TextAsset </summary>
        /// <param name="databaseFile"></param>
        public static void LoadLibrary(string dir, string file)
        {
            if (!IsLoaded(file))
            {
                string[] fLines;
                if (Util.FileIO.ReadTxtFile(dir + file, out fLines))
                {
                    //Splitup file
                    int line = 0;
                    List<string> dataBlock = new List<string>();
                    string lastName = "";
                    while (line <= fLines.Length)
                    {
                        if (line >= fLines.Length || (fLines[line].Length > 0 && fLines[line][0] == '#')) //its a new Material
                        {
                            if (dataBlock.Count > 0 && lastName.Length > 0) //parse & add previous Material if it has a good name
                            {
                                AddMaterial(lastName, MaterialProps.Parse(dataBlock));
                            }
                            dataBlock.Clear();

                            if (line < fLines.Length)
                            {
                                try //extract name of new material
                                {
                                    lastName = fLines[line].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)[1].ToLowerInvariant(); //condition name
                                }
                                catch (System.Exception Ex)
                                {
                                    SenseGlove_Debugger.LogWarning(Ex.Message);
                                    lastName = "";
                                }
                            }
                        }
                        if (line < fLines.Length)
                        {
                            dataBlock.Add(fLines[line]);
                        }
                        line++;
                    }
                    
                }
                libraryNames.Add(file); //prevents us from continuously trying to open files
            }
        }

        /// <summary> Retrieve the Material properties from the chosen Library. </summary>
        /// <param name="libName"></param>
        /// <param name="matName"></param>
        /// <returns></returns>
        public static MaterialProps GetMaterial(string matName)
        {
            matName = matName.ToLowerInvariant(); //always convert to lower case to prevent typos on Dev/User side;
            MaterialProps res;
            if (materials.TryGetValue(matName, out res))
            {
                return res;
            }
            else
            {
                SenseGlove_Debugger.Log("Error loading " + matName + ": No such material loaded.");
            }
            return MaterialProps.Default();
        }

        /// <summary> Add a material with specified properties tot he internal memory </summary>
        /// <param name="matName"></param>
        /// <param name="props"></param>
        private static void AddMaterial(string matName, MaterialProps props)
        {
            MaterialProps exist;
            if (!materials.TryGetValue(matName, out exist))
            {
                materials.Add(matName, props);
            }
        }

        /// <summary> Check if a materials library has been loaded yet. </summary>
        /// <param name="libName"></param>
        /// <returns></returns>
        private static bool IsLoaded(string libName)
        {
            return LibIndex(libName) > -1;
        }

        /// <summary> Return a chosen library's index, if it is already loaded. </summary>
        /// <param name="libName"></param>
        /// <returns></returns>
        private static int LibIndex(string libName)
        {
            for (int i = 0; i < MaterialLibraries.libraryNames.Count; i++)
            {
                if (libraryNames[i].Equals(libName)) { return i; }
            }
            return -1;
        }

        /// <summary> Clear all existing material libraries. </summary>
        public static void ClearLibraries()
        {
            materials.Clear();
            libraryNames.Clear();
        }


    }

}