using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerParameter
    {
        public List<Point3d> SprinklerPt { get; set; } = new List<Point3d>();
        public List<Line> AllPipe { get; set; } = new List<Line>();
        public List<Line> MainPipe { get; set; } = new List<Line>();
        public List<Line> SubMainPipe { get; set; } = new List<Line>();
    }
}
