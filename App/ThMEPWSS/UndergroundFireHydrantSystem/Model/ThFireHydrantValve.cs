using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

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
            else
            {

            }
            return new Line(pt1, pt2);
        }
    }

    class ThTZFireHydrantValve
    {
        private BlockReference Valve { get; set; }

        public Point3dEx CenterPt { get; set; }

        public ThTZFireHydrantValve(object valve)
        {

        }
    }
}


