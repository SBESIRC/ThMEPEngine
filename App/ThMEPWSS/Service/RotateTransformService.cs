using AcHelper;
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
        }

        public static void RotatePolyline(List<Polyline> polylines)
        {
            polylines.ForEach(x => x.Rotate(Point3d.Origin, -CalRotateAngle()));
        }

        public static void RotateInversePolyline(Polyline polyline)
        {
            polyline.Rotate(Point3d.Origin, CalRotateAngle());
        }

        public static void RotateInverseLines(List<Line> lines)
        {
            lines.ForEach(x => x.Rotate(Point3d.Origin, CalRotateAngle()));
        }

        public static Point3d RotateInversePoint(Point3d point)
        {
            return point.RotateBy(CalRotateAngle(), Vector3d.ZAxis, Point3d.Origin);
        }

        public static Vector3d RotateInverseVecter(Vector3d vec)
        {
            return vec.RotateBy(CalRotateAngle(), Vector3d.ZAxis);
        }

        private static double CalRotateAngle()
        {
            var angle = xDir.GetAngleTo(Vector3d.XAxis);
            if (Active.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d.Xaxis.Y < 0)
            {
                angle = -angle;
            }
            return angle;
        }
    }
}
