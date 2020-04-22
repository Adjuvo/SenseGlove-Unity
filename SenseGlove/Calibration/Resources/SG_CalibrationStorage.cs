
namespace SG.Calibration
{
    /// <summary> Class responsible for storing and retrieving Sense Glove calibration on disk. </summary>
    public static class SG_CalibrationStorage
    {
        /// <summary> Default location for storing calibration data. </summary>
        private static readonly string calibrDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
            + "/SenseGlove/Calibration/";


        /// <summary> Generate a new filename for this calibration profile. </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        private static string GetFilename(SenseGloveCs.DeviceType type, GloveSide side)
        {
            string subFile = "SG-"; //dynamically generated later, when more devices become available.
            return subFile + (side == GloveSide.LeftHand ? "L" : "R") + ".txt";
        }


        /// <summary> Stores a deserialized value of an interpolator onto a disk. </summary>
        /// <param name="interpolator"></param>
        /// <param name="side"></param>
        public static void StoreInterpolation(SenseGloveCs.Kinematics.InterpolationSet_IMU interpolator, SenseGloveCs.DeviceType type, GloveSide side)
        {
            if (interpolator != null)
            {
                string FN = GetFilename(type, side);
                string[] contents = new string[] { interpolator.Serialize() };
                Util.FileIO.SaveTxtFile(calibrDir, FN, contents, false);
            }
        }

        /// <summary> Stores a serialized value of an interpolator onto a disk. </summary>
        /// <param name="interpolator"></param>
        /// <param name="side"></param>
        public static void StoreInterpolation(string interpolator, SenseGloveCs.DeviceType type, GloveSide side)
        {
            if (interpolator != null)
            {
                string FN = GetFilename(type, side);
                string[] contents = new string[] { interpolator };
                Util.FileIO.SaveTxtFile(calibrDir, FN, contents, false);
            }
        }


        /// <summary> Retrieves an interpolator from the disk. Returns true if one is actually available. </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public static bool LoadInterpolation(SenseGloveCs.DeviceType type, GloveSide side, out string output)
        {
            string FN = GetFilename(type, side);
            string[] contents;
            if (Util.FileIO.ReadTxtFile(calibrDir + FN, out contents) && contents.Length > 0)
            {
                output = contents[0];
                return output != null;
            }
            output = null;
            return false;
        }

    }
}