using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.FlushPoint
{
    public class ThFlushPointUtils
    {
        public static void SortWashPoints(List<Point3d> washPoints)
        {
            washPoints = washPoints.OrderBy(o => o.X).ThenBy(o => o.Y).ToList();
        }
    }
}
