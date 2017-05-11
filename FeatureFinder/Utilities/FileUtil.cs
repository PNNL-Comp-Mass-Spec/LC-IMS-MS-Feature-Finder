using System;
using System.IO;

namespace FeatureFinder.Utilities
{
    public class FileUtil
    {

        public static string GetUimfFileForIsosFile(string isosFileName)
        {

            var charIndex1 = isosFileName.IndexOf("_Filtered_isos.csv", StringComparison.OrdinalIgnoreCase);
            if (charIndex1 > 0)
            {
                return isosFileName.Substring(0, charIndex1) + ".uimf";
            }

            var charIndex2 = isosFileName.IndexOf("_isos.csv", StringComparison.OrdinalIgnoreCase);
            if (charIndex2 > 0)
            {
                return isosFileName.Substring(0, charIndex2) + ".uimf";
            }

            return Path.ChangeExtension(isosFileName, "uimf");
        }
    }
}