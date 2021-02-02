using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public class LanePolyline
    {
        public Polyline Poly
        {
            get;
            set;
        }

        public Point3d StartPoint
        {
            get;
            set;
        }

        public Point3d EndPoint
        {
            get;
            set;
        }

        public List<Line> ExtendLines
        {
            get;
            set;
        } = new List<Line>();

        public LanePolyline Sym;

        public bool IsUsed = false;

        public LanePolyline(Polyline polyline, Point3d ptStart, Point3d ptEnd)
        {
            Poly = polyline;
            StartPoint = ptStart;
            EndPoint = ptEnd;
        }
    }
}
