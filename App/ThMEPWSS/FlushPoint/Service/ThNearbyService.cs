using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public abstract class ThNearbyService
    {
        protected List<Entity> Rooms { get; set; }
        protected double NearbyDistance { get; set; }
        protected ThCADCoreNTSSpatialIndex RoomSpatialIndex { get; set; }
        public ThNearbyService(List<Entity> rooms,double nearbyDistance)
        {
            Rooms = rooms;
            NearbyDistance = nearbyDistance;
            RoomSpatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
        }
        public abstract bool Find(Point3d pt);
        protected List<Entity> FindRooms(Point3d pt)
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
