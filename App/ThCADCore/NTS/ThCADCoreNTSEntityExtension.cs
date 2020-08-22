using System;
using GeoAPI.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSEntityExtension
    {
        public static IPolygon ToNTSPolygon(this Entity obj)
        {
            if (obj is Polyline polyline)
            {
                return polyline.ToNTSPolygon();
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
    }
}
