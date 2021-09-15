using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThCollectingWellNearbyService:ThNearbyService
    {
        //排水沟的中心线外扩4000范围内视为“附近”        
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThCollectingWellNearbyService(
            List<Polyline> wells, 
            List<Entity> rooms,
            double nearbyDistance) :base(rooms, nearbyDistance)
        {
            SpatialIndex = new ThCADCoreNTSSpatialIndex(wells.ToCollection());
        }

        public override bool Find(Point3d pt)
        {
            var rooms = FindRooms(pt);
            var collector = new DBObjectCollection();
            rooms.ForEach(r =>
            {
                foreach (Entity ent in SpatialIndex.SelectCrossingPolygon(r))
                {
                    collector.Add(ent);
                }
            });

            var spatialIndex = new ThCADCoreNTSSpatialIndex(collector);
            var circle = new Circle(pt, Vector3d.ZAxis, NearbyDistance);
            var poly = circle.Tessellate(5.0);
            var objs = SpatialIndex.SelectCrossingPolygon(poly);
            return objs.Cast<Polyline>().Where(o =>
            {
                return pt.DistanceTo(o.GetCentroidPoint()) <= NearbyDistance;
            }).Any();
        }
    }
}
