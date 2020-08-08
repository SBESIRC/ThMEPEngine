using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm.Locate;

namespace ThCADCore.NTS
{
    public enum LocateStatus
    {
        Null = -1,
        //
        // 摘要:
        //     DE-9IM row index of the interior of the first point and column index of the interior
        //     of the second point. Location value for the interior of a point.
        //
        // 备注:
        //     int value = 0;
        Interior = 0,
        //
        // 摘要:
        //     DE-9IM row index of the boundary of the first point and column index of the boundary
        //     of the second point. Location value for the boundary of a point.
        //
        // 备注:
        //     int value = 1;
        Boundary = 1,
        //
        // 摘要:
        //     DE-9IM row index of the exterior of the first point and column index of the exterior
        //     of the second point. Location value for the exterior of a point.
        //
        // 备注:
        //     int value = 2;
        Exterior = 2
    }

    public static class ThCADCoreNTSLocate
    {
        public static LocateStatus PointInPolygon(this Polyline polyline, Point3d pt)
        {
            var geometry = polyline.ToNTSPolygon();
            if (geometry.IsEmpty)
            {
                return LocateStatus.Null;
            }

            return (LocateStatus)SimplePointInAreaLocator.LocatePointInPolygon(pt.ToNTSCoordinate(), geometry);
        }

        public static LocateStatus IndexedPointInPolygon(this Polyline polyline, Point3d pt)
        {
            var geometry = polyline.ToNTSPolygon();
            if (geometry.IsEmpty)
            {
                return LocateStatus.Null;
            }

            var locator = new IndexedPointInAreaLocator(geometry);
            return (LocateStatus)locator.Locate(pt.ToNTSCoordinate());
        }
    }
}