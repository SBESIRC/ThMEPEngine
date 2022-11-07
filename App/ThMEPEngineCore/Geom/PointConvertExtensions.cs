using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.ImagePlot.Service;

namespace ThMEPEngineCore.Geom
{
    public static class PointConvertExtensions
    {
        public static GRect ToGRect(this Extents3d? extents3D)
        {
            if (extents3D.HasValue) return new GRect(extents3D.Value.MinPoint, extents3D.Value.MaxPoint);
            return default;
        }
        public static GRect ToGRect(this Extents3d? extents3D, double radius)
        {
            if (extents3D is Extents3d ext)
            {
                var center = GeoAlgorithm.MidPoint(ext.MinPoint, ext.MaxPoint);
                return GRect.Create(center, radius);
            }
            return default;
        }
        public static System.Drawing.Point ToPoint(this Point2d pt) => new(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
        public static System.Drawing.Point ToPoint(this Point3d pt) => new(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
        public static PointF ToPointF(this Point3d pt) => new(Convert.ToSingle(pt.X), Convert.ToSingle(pt.Y));
    }
}
