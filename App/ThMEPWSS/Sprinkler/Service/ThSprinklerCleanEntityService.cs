﻿using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerCleanEntityService
    {
        public double BufferLength { get; set; }
        public double TesslateLength { get; set; }
        public ThSprinklerCleanEntityService()
        {
            BufferLength = 25.0;
            TesslateLength = 10.0;
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
    }
}
