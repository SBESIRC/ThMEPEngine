using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPEngineCore.Model
{
    public class ThIfcStair : ThIfcBuildingElement
    {
        public List<Point3d> PlatForLayout { get; set; }
        public List<Point3d> HalfPlatForLayout { get; set; }
        public static ThIfcStair Create(Entity block)
        {
            return new ThIfcStair()
            {
                Outline = block,
            };
        }
    }
}
