using System.Collections.Generic;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Geom;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorCompositeRoomService
    {
        private List<ThWCompositeRoom> CompositeRoom { get; set; }
        private ThIfcRoom Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorCompositeRoomService(
           ThIfcRoom space,
           List<ThWCompositeRoom> compositeRoom)
        {
            CompositeRoom = compositeRoom;
            Space = space;
            var objs = new DBObjectCollection();      
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWCompositeRoom> Find(
            ThIfcRoom space,
            List<ThWCompositeRoom> compositeRoom)
        {          
            return Findspace(space, compositeRoom);
        }
        private static List<ThWCompositeRoom> Findspace(ThIfcRoom FirstFloorSpace, List<ThWCompositeRoom> compositeRoom)
        {
            var compositeroom_ = new List<ThWCompositeRoom>();
            foreach (var room in compositeRoom)
            {
                var bboundary = FirstFloorSpace.Boundary as Polyline;
                if (GeomUtils.PtInLoop(bboundary, room.Toilet.Boundary.GetCenter()))
                {
                    compositeroom_.Add(room);
                }
            }
            return compositeroom_;
        }

    }
}

