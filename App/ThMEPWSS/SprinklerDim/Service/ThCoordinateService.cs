using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThCoordinateService
    {
        public static Matrix3d GetCoordinateTransformer(Point3d fromOrigin, Vector3d fromXAxis, Point3d toOrigin, Vector3d toXAxis)
        {

            var rotationangle = fromXAxis.GetAngleTo(toXAxis, Vector3d.ZAxis);
            var matrix = Matrix3d.Displacement(toOrigin - fromOrigin) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));

            return matrix.Inverse();

        }


        public static Matrix3d GetCoordinateTransformer(Point3d fromOrigin, Point3d toOrigin, Double angle)
        {
            while(angle > Math.Abs(angle - Math.PI / 2))
            {
                angle = angle - Math.PI / 2;
            }

            var matrix = Matrix3d.Displacement(toOrigin - fromOrigin) * Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            return matrix.Inverse();

        }

        public static List<Point3d> MakeTransformation(List<Point3d> pts, Matrix3d transformer)
        {
            List<Point3d> transPts = new List<Point3d>();

            foreach(Point3d pt in pts)
            {
                transPts.Add(pt.TransformBy(transformer));
            }

            return transPts;
        }

        public static double GetOriginalValue(Point3d pt, bool isXAxis)
        {
            if (isXAxis)
                return pt.X;
            else
                return pt.Y;
        }

        public static Vector3d GetDirrection(Matrix3d transformer, bool isXAxis)
        {
            Point3d startPoint = new Point3d(0, 0, 0);
            Point3d endPoint = new Point3d();

            if (isXAxis)
            {
                endPoint = new Point3d(1, 0, 0);
            }
            else
            {
                endPoint = new Point3d(0, 1, 0);
            }


            List<Point3d> pts = new List<Point3d> { startPoint, endPoint };
            pts = ThCoordinateService.MakeTransformation(pts, transformer);

            Vector3d dir = (pts[1] - pts[0]).GetNormal();
            return dir;
        }

        public static bool IsParalleled(Line l1, Line l2)
        {
            Vector3d v1 = l1.EndPoint - l1.StartPoint;
            Vector3d v2 = l2.EndPoint - l2.StartPoint;


            if (Math.Abs(v1.GetNormal().DotProduct(v2.GetNormal())) > Math.Cos(Math.PI / 180))
                return true;

            return false;
        }


    }
}
