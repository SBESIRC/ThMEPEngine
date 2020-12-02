using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class PolylineCircle
    {
        public Polyline Poly;
        public Point3d Point;

        public PolylineCircle(Polyline polyline, Point3d pt)
        {
            Poly = polyline;
            Point = pt;
        }
    }
}
