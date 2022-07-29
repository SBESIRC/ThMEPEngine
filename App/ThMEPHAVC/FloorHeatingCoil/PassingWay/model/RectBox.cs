using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class RectBox
    {
        public double xmin, xmax, ymin, ymax;
        public double height, width;
        public Point3d center;
        public int i, j;
        public RectBox(double xmin, double xmax, double ymin, double ymax)
        {
            this.xmin = xmin;
            this.xmax = xmax;
            this.ymin = ymin;
            this.ymax = ymax;
        }
        public RectBox(double xmin, double xmax, double ymin, double ymax, int i, int j)
        {
            this.xmin = xmin;
            this.xmax = xmax;
            this.ymin = ymin;
            this.ymax = ymax;
            this.i = i;
            this.j = j;
            height = ymax - ymin;
            width = xmax - xmin;
            center = new Point3d((xmax + xmin) / 2, (ymax + ymin) / 2, 0);
        }
        public double DistanceTo(RectBox b)
        {
            if (i == b.i && j == b.j)
                return 0;
            if (Math.Abs(i - b.i) + Math.Abs(j - b.j) != 1)
                return 120000;
            var dv = b.center - center;
            return Math.Abs(dv.X) + Math.Abs(dv.Y);
        }
        public Polyline ToPolyline()
        {
            var rect = new Polyline();
            rect.Closed = true;
            rect.AddVertexAt(0, new Point2d(xmin, ymax), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(xmin, ymin), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(xmax, ymin), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(xmax, ymax), 0, 0, 0);
            rect.AddVertexAt(4, new Point2d(xmin, ymax), 0, 0, 0);
            return rect;
        }
    }
}
