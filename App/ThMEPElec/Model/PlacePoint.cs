using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Model
{
    // 记录插入点，以及这个点是否修正过
    public class PlacePoint
    {
        public Point3d InsertPt;

        public bool IsMoved = false;

        public PlacePoint(Point3d pt, bool moved)
        {
            InsertPt = pt;
            IsMoved = moved;
        }
    }
}
