using System.Collections.Generic;
using UIMFLibrary;

namespace FeatureFinder.Data.Maps
{
    public static class ScanLCToFrameTypeMap
    {
        public static Dictionary<int, UIMFData.FrameType> Mapping { get; set; }

        static ScanLCToFrameTypeMap()
        {
            Mapping = new Dictionary<int, UIMFData.FrameType>();
        }
    }
}
