using System.Collections.Generic;
using UIMFLibrary;

namespace FeatureFinder.Data.Maps
{
    public static class ScanLCToFrameTypeMap
    {
        public static Dictionary<int, DataReader.FrameType> Mapping { get; set; }

        static ScanLCToFrameTypeMap()
        {
            Mapping = new Dictionary<int, DataReader.FrameType>();
        }
    }
}
