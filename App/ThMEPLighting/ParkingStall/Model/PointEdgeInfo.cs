using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public class PointEdgeInfo
    {
        public Point3d Point;

        public List<LanePolyline> Polylines;

        public int Degree;
        public PointEdgeInfo(Point3d point, List<LanePolyline> polylines, int degree)
        {
            Point = point;
            Polylines = polylines;
            Degree = degree;
        }
    }
}
