using System.Collections.Generic;

namespace ThMEPLighting.Garage.Model
{
    public class ThCableTrayParameter
    {
        private static readonly ThCableTrayParameter instance = new ThCableTrayParameter();
        public ThEntityParameter SideLineParameter { get; set; }
        public ThEntityParameter CenterLineParameter { get; set; }
        public ThEntityParameter NumberTextParameter { get; set; }
        public ThEntityParameter LaneLineBlockParameter { get; set; }
        public ThEntityParameter JumpWireParameter { get; set; }
        static ThCableTrayParameter()
        {
        }
        internal ThCableTrayParameter() 
        {
            SideLineParameter = new ThEntityParameter
            {
                Layer = "E-UNIV-EL2",
                ColorIndex = 40,
                LineType = "HIDDEN"
            };
            CenterLineParameter = new ThEntityParameter
            {
                Layer = "E-LITE-CENTER",
                ColorIndex = 5,
                LineType = "DASHDOT"
            };
            NumberTextParameter = new ThEntityParameter
            {
                Layer = "E-UNIV-NOTE",
                ColorIndex = 6,
                LineType = "Continuous"
            };
            LaneLineBlockParameter = new ThEntityParameter
            {
                Layer = "E-LITE-LITE",
                ColorIndex = 3,
                LineType = "Continuous"
            };
            JumpWireParameter = new ThEntityParameter
            {
                Layer = "E-LITE-WIRE",
                ColorIndex = 4,
                LineType = "Continuous"
            };
        }
        public List<string> AllLayers
        {
            get
            {
                return new List<string> 
                {
                    JumpWireParameter.Layer,
                    SideLineParameter.Layer,
                    CenterLineParameter.Layer,
                    NumberTextParameter.Layer,
                    LaneLineBlockParameter.Layer,
                };
            }
        }
        public static ThCableTrayParameter Instance { get { return instance; } }
    }
}
