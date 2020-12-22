using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Model
{
    public class ThRacewayParameter
    {
        public ThEntityParameter SideLineParameter { get; set; }
        public ThEntityParameter PortLineParameter { get; set; }
        public ThEntityParameter CenterLineParameter { get; set; }
        public ThEntityParameter NumberTextParameter { get; set; }
        public ThEntityParameter LaneLineBlockParameter { get; set; }
        public ThRacewayParameter()
        {
            SideLineParameter = new ThEntityParameter
            {
                Layer = "E-UNIV-EL2",
                ColorIndex = 40,
                LineType = "HIDDEN"
            };
            PortLineParameter = new ThEntityParameter
            {
                Layer= "E-UNIV-EL2",
                ColorIndex= 40,
                LineType= "HIDDEN"
            };
            CenterLineParameter = new ThEntityParameter
            {
                Layer= "E-LITE-CENTER",
                ColorIndex= 5,
                LineType= "DASHDOT"
            };
            NumberTextParameter = new ThEntityParameter
            {
                Layer= "E-UNIV-NOTE",
                ColorIndex=6,
                LineType="Continuous"
            };
            LaneLineBlockParameter = new ThEntityParameter
            {
                Layer= "E-LITE-LITE",
                ColorIndex=3,
                LineType= "Continuous"
            };
        }
    }

    public class ThRacewayGroupParameter
    {
        public ThRacewayParameter RacewayParameter { get; set; }
        public Line Center { get; set; }
        public List<Line> Sides { get; set; }
        public List<Line> Ports { get; set; }

        public List<Line> GetAll()
        {
            var lines = new List<Line>();
            lines.Add(Center);
            lines.AddRange(Sides);
            lines.AddRange(Ports);
            return lines;
        }
    }
}
