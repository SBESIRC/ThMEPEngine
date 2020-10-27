﻿using System;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSEntityExtension
    {
        public static Geometry ToNTSGeometry(this Entity obj)
        {
            if (obj is DBPoint point)
            {
                return point.ToNTSPoint();
            }
            else if (obj is Curve curve)
            {
                return curve.ToNTSLineString();
            }
            else if (obj is Region region)
            {
                return region.ToNTSPolygon();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public static LineString ToNTSLineString(this Curve curve)
        {
            if (curve is Line line)
            {
                return line.ToNTSLineString();
            }
            else if (curve is Polyline polyline)
            {
                return polyline.ToNTSLineString();
            }
            else if (curve is Polyline2d poly2d)
            {
                return poly2d.ToNTSLineString();
            }
            else if (curve is Polyline3d poly3d)
            {
                return poly3d.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
