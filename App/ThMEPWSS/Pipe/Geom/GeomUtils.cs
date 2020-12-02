using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.Pipe.Geom
{
    public class GeomUtils
    {
        public static bool PtInLoop(Polyline polyline, Point3d pt)
        {
            if (polyline.Closed == false)
                return false;
            return polyline.IndexedContains(pt);
        }
    }
}
