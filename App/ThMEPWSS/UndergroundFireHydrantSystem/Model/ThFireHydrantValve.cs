using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class ThFireHydrantValve
    {
        private BlockReference Valve { get; set; }
        private double Width { get; set; }
        private double Height { get; set; }

        public ThFireHydrantValve(BlockReference valve)
        {
            Valve = valve;
            Width = GetBlockSize(valve)[0];
            Height = GetBlockSize(valve)[1];
        }

        private double[] GetBlockSize(BlockReference br)//获取block尺寸
        {
            var extent = br.GeometricExtents;
            var Length = extent.MaxPoint.X - extent.MinPoint.X;
            var Hight = extent.MaxPoint.Y - extent.MinPoint.Y;
            var Size = new double[] { Length, Hight };

            return Size;
        }

        public Point3dCollection GetRect()
        {
            double gap = 300;
            var pt1 = (Valve as Entity).GeometricExtents.MaxPoint;
            var pt2 = (Valve as Entity).GeometricExtents.MinPoint;
            var pts = new Point3d[5];
            pts[0] = new Point3d(pt2.X - gap, pt1.Y + gap, 0);
            pts[1] = new Point3d(pt1.X + gap, pt1.Y + gap, 0);
            pts[2] = new Point3d(pt1.X + gap, pt2.Y - gap, 0);
            pts[3] = new Point3d(pt2.X - gap, pt2.Y - gap, 0);
            pts[4] = pts[0];
            return new Point3dCollection(pts);
        }

        public Line GetLine(bool isBkRe)
        {
            var pt1 = new Point3d();
            var pt2 = new Point3d();
            if (isBkRe)
            {
                pt1 = new Point3d(Valve.Position.X, Valve.Position.Y, 0);
                if (Math.Abs(Valve.Rotation - Math.PI / 2) < 1e-5)
                {
                    pt2 = new Point3d(pt1.X, pt1.Y + Valve.ScaleFactors.X * Height, 0);
                }
                if (Math.Abs(Valve.Rotation) < 1e-5)
                {
                    pt2 = new Point3d(pt1.X + Valve.ScaleFactors.X * Width, pt1.Y, 0);
                }
                if (Math.Abs(Valve.Rotation - Math.PI) < 1e-5)
                {
                    pt2 = new Point3d(pt1.X - Valve.ScaleFactors.X * Width, pt1.Y, 0);
                }
                if (Math.Abs(Valve.Rotation - Math.PI * 3 / 2) < 1e-5)
                {
                    pt2 = new Point3d(pt1.X, pt1.Y - Valve.ScaleFactors.X * Height, 0);
                }
            }
            return new Line(pt1, pt2);
        }
    }
}


