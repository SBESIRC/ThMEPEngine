using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThCollectingWellNearbyService
    {
        //排水沟的中心线外扩4000范围内视为“附近”
        public double NearbyDistance { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThCollectingWellNearbyService(List<Polyline> wells)
        {
            NearbyDistance = 3000;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(wells.ToCollection());
        }
        public bool Find(Point3d pt)
        {
            var circle = new Circle(pt, Vector3d.ZAxis, NearbyDistance);
            var poly = circle.Tessellate(5.0);
            var objs = SpatialIndex.SelectCrossingPolygon(poly);
            return objs.Cast<AcPolygon>().Where(o =>
            {
                return pt.DistanceTo(o.GetCentroidPoint()) <= NearbyDistance;
            }).Any();               
        }
    }
}
