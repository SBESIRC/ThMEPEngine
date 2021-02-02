using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Model
{
    public class ThRegionLightEdge
    {
        public Polyline RegionBorder { get; set; }
        public List<BlockReference> Lights { get; set; }
        public List<Line> Edges { get; set; }
        public ThRegionLightEdge()
        {
            RegionBorder = new Polyline();
            Lights = new List<BlockReference>();
            Edges = new List<Line>();
        }
    }
}
