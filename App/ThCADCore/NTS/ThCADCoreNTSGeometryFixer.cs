using System;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Geometries.Utilities;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSGeometryFixer
    {
        public static DBObjectCollection MakeValid(this DBObjectCollection polygons)
        {
            return GeometryFixer.Fix(polygons.ToNTSMultiPolygon()).ToDbCollection();
        }
    }
}
