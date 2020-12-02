using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    public class PairData
    {
        public Point3d Point;
        public Polyline Polyline;

        public PairData(Point3d point, Polyline polyline)
        {
            Point = point;
            Polyline = polyline;
        }
    }
}
