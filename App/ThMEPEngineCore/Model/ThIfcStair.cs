using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcStair : ThIfcBuildingElement
    {
        public List<List<Point3d>> PlatForLayout { get; set; }
        public List<List<Point3d>> HalfPlatForLayout { get; set; }
        public BlockReference SrcBlock { get; set; }
        public string StairType { get; set; }
        public string Storey { get; set; }
        
        public ThIfcStair()
        {
            PlatForLayout = new List<List<Point3d>>();
            HalfPlatForLayout = new List<List<Point3d>>();
        }

        public static ThIfcStair Create(Entity block)
        {
            return new ThIfcStair();
        }
    }
}
