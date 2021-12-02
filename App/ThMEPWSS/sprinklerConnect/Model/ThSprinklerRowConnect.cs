

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerRowConnect
    {
        public Dictionary<int, List<Point3d>> OrderDict { get; set; } = new Dictionary<int, List<Point3d>>();
        public int Count { get; set; } = 0;
        public bool IsStallArea { get; set; }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        public Line Base
        {
            get{ return new Line(StartPoint, EndPoint);}
        }
    }
}
