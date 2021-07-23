using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThDrainageDitchNearbyService
    {
        public double NearbyDistance { get; set; } //排水沟的中心线外扩4000范围内视为“附近”
        public List<Entity> Rooms { get; set; }
        private ThCADCoreNTSSpatialIndex DitchSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex RoomSpatialIndex { get; set; }
        public ThDrainageDitchNearbyService(List<Entity> ditches,List<Entity> rooms)
        {
            Rooms = rooms;
            NearbyDistance = 4000;
            DitchSpatialIndex = new ThCADCoreNTSSpatialIndex(ditches.ToCollection());
            RoomSpatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
        }
        public bool Find(Point3d pt)
        {
            var rooms = FindRooms(pt);
            var collector = new DBObjectCollection();
            rooms.ForEach(r =>
            {
                foreach(Entity ent in DitchSpatialIndex.SelectCrossingPolygon(r))
                {
                    collector.Add(ent);
                }
            });
            var spatialIndex = new ThCADCoreNTSSpatialIndex(collector);
            var circle = new Circle(pt, Vector3d.ZAxis, NearbyDistance);
            var poly = circle.Tessellate(5.0);
            var objs = spatialIndex.SelectCrossingPolygon(poly);
            return objs.Count > 0;
        }
        private List<Entity> FindRooms(Point3d pt)
        {
            var envelop = ThDrawTool.CreateSquare(pt, 5.0);
            var objs = RoomSpatialIndex.SelectCrossingPolygon(envelop);
            var containers = new List<Entity>();
            if (objs.Count == 0)
            {
                containers = Rooms.Where(r => r.IsContains(pt)).ToList();
            }
            else
            {
                containers = objs.Cast<Entity>().ToList();
            }
            return containers.OrderBy(o => o.ToNTSPolygon().Distance(pt.ToNTSPoint())).ToList();
        }
    }
}
