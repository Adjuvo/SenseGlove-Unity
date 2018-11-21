using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Util
{
    /// <summary> Ensures that .txt files are properly handled by Unity. Used by the Materials and UserProfile Libraries. </summary>
    public static class FileIO
    {
        
        /// <summary> Attempt to save a string[] to a filename within a desired directory. Returns true if succesful. </summary>
        /// <remarks> Directory is added as a separate variable so we can more easily check for its existence. </remarks>
        /// <param name="dir"></param>
        /// <param name="fileName"></param>
        /// <param name="lines"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public static bool SaveTxtFile(string dir, string fileName, string[] lines, bool append = false)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string path = dir + "/" + fileName;

                if (!File.Exists(path)) { append = true; } //always append if the file does not exist so that it will be automatically created (without IOExceptions)

                using (StreamWriter file = new StreamWriter(path, append)) //using keyword to ensu
                {
                    for (int i=0; i<lines.Length; i++)
                    {
                        file.WriteLine(lines[i]);
                    }
                    file.Close();
                }

                return true;
            }
            catch (System.Exception Ex)
            {
                Debug.LogError(Ex.Message);
            }
            return false;
        }

        /// <summary> Attempt to read all lines from a file and place them in the string[]. Returns true if succesful. If unable to open the file, the string[] will be empty.</summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool ReadTxtFile(string path, out string[] lines)
        {
            try
            {
                if (File.Exists(path))
                {
                    lines = File.ReadAllLines(path);
                    return true;
                }
                else
                {
                    Debug.Log(path + " does not exist.");
                }
            }
            catch (System.Exception Ex)
            {
                Debug.LogError(Ex.Message);
            }
            lines = new string[0];
            return false;
        }

    }

}
