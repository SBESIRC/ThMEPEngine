using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class ThFireHydrantSelectArea
    {
        public static Point3dCollection CreateArea(Tuple<Point3d, Point3d> tuplePoint)
        {
            var ptNum = new Point3d[5];
            ptNum[0] = tuplePoint.Item1;
            ptNum[2] = tuplePoint.Item2;
            ptNum[4] = tuplePoint.Item1;
            ptNum[1] = new Point3d(ptNum[2].X, ptNum[0].Y, 0);
            ptNum[3] = new Point3d(ptNum[0].X, ptNum[2].Y, 0);

            var ptCollect = new Point3dCollection(ptNum);

            return ptCollect;
        }
    }
}
