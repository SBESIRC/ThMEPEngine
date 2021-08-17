using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcStair : ThIfcBuildingElement
    {
        public List<Point3d> PlatForLayout { get; set; }
        public List<Point3d> HalfPlatForLayout { get; set; }
        public BlockReference srcBlock { get; set; }
        public static ThIfcStair Create(Entity block)
        {
            return new ThIfcStair();
        }
    }
}
