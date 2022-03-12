using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSDBText
    {
        public Point3d FirstPosition { get; set; }
        public Vector3d Direction { get; set; }
        public List<string> Texts { get; set; }
    }
}
