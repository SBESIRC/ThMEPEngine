using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThCleanEntityService
    {
        public double BufferLength { get; set; }
        public double TesslateLength { get; set; }
        public ThCleanEntityService()
        {
            BufferLength = 25.0;
            TesslateLength = 100.0;
        }
        public Polyline Clean(Polyline polygon)
        {
            var polyline = MakeValid(polygon);
            if (polyline.Area > 0.0)
            {
                polyline = Buffer(polyline, BufferLength);
            }
            return polyline;
        }
        public static Polyline Buffer(Polyline polygon, double length)
        {
            //处理狭长线
            var objs = polygon.Buffer(-length);
            objs = objs.Buffer(length);
            return objs.Count > 0 ? objs.Cast<Polyline>().OrderByDescending(p => p.Area).First() : new Polyline();
        }
        public static Polyline MakeValid(Polyline polygon)
        {
            //处理自交
            var objs = polygon.MakeValid();
            return objs.Count > 0 ? objs.Cast<Polyline>().OrderByDescending(p => p.Area).First() : new Polyline();
        }
        public static Polyline Tesslate(Polyline polyline, double length = 10.0)
        {
            //把带弧的线段打散成直线
            return polyline.Tessellate(length);
        }
    }
}
