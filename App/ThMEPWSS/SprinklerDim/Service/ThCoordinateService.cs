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

        public static bool IsVertical(Vector3d l, Vector3d v)
        {
            if (Math.Abs(l.GetNormal().DotProduct(v.GetNormal())) < Math.Cos(Math.PI / 2 - Math.PI / 180))
            {
                return true;
            }

            return false;
        }

        public static bool IsTheSameDirrection(Vector3d v1, Vector3d v2)
        {
            if(v1.DotProduct(v2) > 0)
            {
                return true;
            }
            return false;
        }


        public static Polyline GetDimWholePolyline(Point3d pts1, Point3d pts2, Vector3d ldir, double distance)
        {
            double Length = pts1.DistanceTo(pts2);
            Vector3d dir = ldir.RotateBy(Math.PI / 2, new Vector3d(0, 0, 1)).GetNormal();
            Point3d CentralPt = new Point3d();

            double x1 = Math.Round(pts1.X, 1);
            double x2 = Math.Round(pts2.X, 1);

            if (x1 < x2)
            {
                CentralPt = new Point3d((pts1.X + pts2.X) / 2.0 + (275 + distance) * dir.X, (pts1.Y + pts2.Y) / 2.0 + (275 + distance) * dir.Y, 0);
            }
            else if (x1 > x2)
            {
                CentralPt = new Point3d((pts1.X + pts2.X) / 2.0 + (-275 + distance) * dir.X, (pts1.Y + pts2.Y) / 2.0 + (-275 + distance) * dir.Y, 0);
            }
            else
            {
                if (pts1.Y < pts2.Y) CentralPt = new Point3d((pts1.X + pts2.X) / 2.0 + (275 + distance) * dir.X, (pts1.Y + pts2.Y) / 2.0 + (275 + distance) * dir.Y, 0);
                else CentralPt = new Point3d((pts1.X + pts2.X) / 2.0 + (-275 + distance) * dir.X, (pts1.Y + pts2.Y) / 2.0 + (-275 + distance) * dir.Y, 0);
            }
            Polyline box = GenerateBox(CentralPt, ldir.GetNormal(), Length / 2.0, 275);

            return box;
        }

        public static int IsTextBoxOverlap(Point3d pts1, Point3d pts2, double distance)
        {
            double x1 = Math.Round(pts1.X, 1);
            double x2 = Math.Round(pts2.X, 1);

            if (x1 < x2)
            {
                if (distance > 0) return 0;
                else return 1;
            }
            else if(x1 > x2)
            {
                if (distance < 0) return 0;
                else return 1;
            }
            else
            {
                if (pts1.Y < pts2.Y)
                {
                    if (distance > 0) return 0;
                    else return 1;
                }
                else
                {
                    if (distance < 0) return 0;
                    else return 1;
                }
            }
        }

        public static Polyline GenerateBox(Point3d pt, Vector3d dir, double sTol = 2000.0, double dTol = 1500.0)
        {
            Polyline box = new Polyline();
            Vector3d tDir = dir.RotateBy(Math.PI / 2, new Vector3d(0, 0, 1));

            Point3d a = pt - sTol * dir + dTol * tDir;
            Point3d b = pt + sTol * dir + dTol * tDir;
            Point3d c = pt + sTol * dir - dTol * tDir;
            Point3d d = pt - sTol * dir - dTol * tDir;

            box.AddVertexAt(0, a.ToPoint2D(), 0, 0, 0);
            box.AddVertexAt(1, b.ToPoint2D(), 0, 0, 0);
            box.AddVertexAt(2, c.ToPoint2D(), 0, 0, 0);
            box.AddVertexAt(3, d.ToPoint2D(), 0, 0, 0);

            box.Closed = true;
            return box;
        }


    }
}
