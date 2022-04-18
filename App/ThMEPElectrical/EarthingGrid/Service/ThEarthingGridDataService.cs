using System.Linq;
using System.Collections.Generic;

namespace ThMEPElectrical.EarthingGrid.Service
{
    public class ThEarthingGridDataService
    {
        private static readonly ThEarthingGridDataService instance = new ThEarthingGridDataService() { };
        public static ThEarthingGridDataService Instance { get { return instance; } }
        internal ThEarthingGridDataService()
        {
            EarthingGridSize = EarthingGridSizes.Count > 0 ?
                EarthingGridSizes.First() : "";
        }
        static ThEarthingGridDataService()
        {
        }
        public List<string> EarthingGridSizes
        {
            get
            {
                return new List<string>() { "10x10或12x8或20x5", "20x20或24x16或40x10" };
            }
        }
        public string EarthingGridSize { get; set; }
        public string AIAreaInternalLayer { get; private set; } = "AI-AREA-INT";
        public string AIAreaExternalLayer { get; private set; } = "AI-AREA-EXT";
        public short AIAreaInternalColorIndex { get; private set; } = 7;
        public short AIAreaExternalColorIndex { get; private set; } = 7;
    }
}
