using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThDrainageDitchNearbyService
    {
        public double NearbyDistance { get; set; } //排水沟的中心线外扩4000范围内视为“附近”
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThDrainageDitchNearbyService(List<Entity> ditches)
        {
            NearbyDistance = 4000;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(ditches.ToCollection());
        }
        public bool Find(Point3d pt)
        {
            var circle = new Circle(pt, Vector3d.ZAxis, NearbyDistance);
            var poly = circle.Tessellate(5.0);
            var objs = SpatialIndex.SelectCrossingPolygon(poly);
            return objs.Count > 0;
        }
    }
}
