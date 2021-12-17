using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;


namespace ThMEPArchitecture.PartitionLayout
{
    public static class GeoUtilitiesOptimized
    {
        public static Point3d GetRecCentroid(this Polyline rec)
        {
            var ext = rec.GeometricExtents;
            var min = ext.MinPoint;
            var max = ext.MaxPoint;
            return new Point3d((min.X + max.X) / 2, (min.Y + max.Y) / 2, 0);
        }

       

    }
}
