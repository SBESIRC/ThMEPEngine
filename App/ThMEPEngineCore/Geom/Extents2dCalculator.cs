using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.ImagePlot.Service;

namespace ThMEPEngineCore.Geom
{
    public class Extents2dCalculator
    {
        public double MinX = double.MaxValue;
        public double MinY = double.MaxValue;
        public double MaxX = double.MinValue;
        public double MaxY = double.MinValue;
        public bool IsValid => MinX != double.MaxValue && MinY != double.MaxValue && MaxX != double.MinValue && MaxY != double.MinValue;
        public static Extents2d Calc(IEnumerable<Point2d> points)
        {
            var o = new Extents2dCalculator();
            foreach (var pt in points)
            {
                o.Update(pt);
            }
            return o.ToExtents2d();
        }
        public static GLineSegment GetCenterLine(IEnumerable<GLineSegment> segs)
        {
            var o = new Extents2dCalculator();
            var c = 0;
            var s = .0;
            foreach (var seg in segs)
            {
                var angle = seg.ToVector2d().Angle.AngleToDegree();
                if (angle >= 180.0) angle -= 180.0;
                s += angle;
                ++c;
                o.Update(seg);
            }
            if (c == 0) throw new ArgumentException();
            var avg = s / c;
            var r = o.ToGRect();
            var center = r.Center;
            if (avg >= 90.0) avg -= 90.0;

            {
                var angle = avg.AngleFromDegree();
                Vector2d vec;
                if (0 <= avg && avg <= 45.0)
                {
                    vec = new Vector2d(r.Width / 2, Math.Tan(angle) * r.Width / 2);
                }
                else if (45.0 < avg && avg <= 90.0)
                {
                    vec = new Vector2d(r.Height / 2 / Math.Tan(angle), r.Height / 2);
                }
                else
                {
                    throw new Exception(avg.ToString());
                }

                return new GLineSegment(center + vec, center - vec);
            }
        }
        public static Extents2d Calc(IEnumerable<GLineSegment> segs)
        {
            var o = new Extents2dCalculator();
            o.Update(segs);
            return o.ToExtents2d();
        }
        public void Update(IEnumerable<GLineSegment> segs)
        {
            foreach (var seg in segs)
            {
                Update(seg);
            }
        }
        public void Update(GRect r)
        {
            Update(r.LeftTop);
            Update(r.RightButtom);
        }
        public void Update(GLineSegment seg)
        {
            Update(seg.StartPoint);
            Update(seg.EndPoint);
        }
        public void Update(Point2d pt)
        {
            if (MinX > pt.X) MinX = pt.X;
            if (MinY > pt.Y) MinY = pt.Y;
            if (MaxX < pt.X) MaxX = pt.X;
            if (MaxY < pt.Y) MaxY = pt.Y;
        }
        public void Update(Point3d pt)
        {
            if (MinX > pt.X) MinX = pt.X;
            if (MinY > pt.Y) MinY = pt.Y;
            if (MaxX < pt.X) MaxX = pt.X;
            if (MaxY < pt.Y) MaxY = pt.Y;
        }
        public void Update(Extents2d ext)
        {
            var pt = ext.MinPoint;
            if (MinX > pt.X) MinX = pt.X;
            if (MinY > pt.Y) MinY = pt.Y;
            pt = ext.MaxPoint;
            if (MaxX < pt.X) MaxX = pt.X;
            if (MaxY < pt.Y) MaxY = pt.Y;
        }
        public Extents2d ToExtents2d()
        {
            return new Extents2d(MinX, MinY, MaxX, MaxY);
        }
        public Extents3d ToExtents3d()
        {
            return new Extents3d(new Point3d(MinX, MinY, 0), new Point3d(MaxX, MaxY, 0));
        }
        public GRect ToGRect()
        {
            return new GRect(MinX, MinY, MaxX, MaxY);
        }
    }
}
