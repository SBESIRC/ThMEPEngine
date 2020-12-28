using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
