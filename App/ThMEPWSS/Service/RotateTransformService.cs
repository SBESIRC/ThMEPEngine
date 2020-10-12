using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Service
{
    public static class RotateTransformService
    {
        public static Vector3d xDir = Vector3d.XAxis;

        public static void RotatePolyline(Polyline polyline)
        {
            polyline.Rotate(Point3d.Origin, -CalRotateAngle());
            //using (AcadDatabase acdb = AcadDatabase.Active())
            //{
            //    acdb.ModelSpace.Add(polyline.Clone() as Polyline);
            //}
        }

        public static void RotatePolyline(List<Polyline> polylines)
        {
            polylines.ForEach(x => x.Rotate(Point3d.Origin, -CalRotateAngle()));
            //using (AcadDatabase acdb = AcadDatabase.Active())
            //{
            //    polylines.ForEach(x=> {
            //            var s = x.Clone() as Polyline;
            //            acdb.ModelSpace.Add(s);
            //        });
            //}
        }

        public static void RotateInversePolyline(Polyline polyline)
        {
            polyline.Rotate(Point3d.Origin, CalRotateAngle());
        }

        public static void RotateInverseLines(List<Line> lines)
        {
            lines.ForEach(x => x.Rotate(Point3d.Origin, CalRotateAngle()));
        }

        private static double CalRotateAngle()
        {
            return xDir.GetAngleTo(Vector3d.XAxis);
        }
    }
}
