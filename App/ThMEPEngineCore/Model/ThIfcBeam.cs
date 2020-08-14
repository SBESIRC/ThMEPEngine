using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Model
{
    public class ThIfcBeam : ThIfcBuildingElement
    {
        public ThIfcBeam()
        {
        }
        public Point3d StartPoint { get; set; }
        public Point3d EndPoint { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
