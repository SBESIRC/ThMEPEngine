using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Geom;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorCompositeRoomService
    {
        private List<ThWCompositeRoom> CompositeRoom { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorCompositeRoomService(
           ThIfcSpace space,
           List<ThWCompositeRoom> compositeRoom)
        {
            CompositeRoom = compositeRoom;
            Space = space;
            var objs = new DBObjectCollection();      
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWCompositeRoom> Find(
            ThIfcSpace space,
            List<ThWCompositeRoom> compositeRoom)
        {
            var service = new ThTopFloorCompositeRoomService(space, compositeRoom);
            return service.Find(space);
        }
        private List<ThWCompositeRoom> Find(ThIfcSpace FirstFloorSpace)
        {
            var engine = new ThWCompositeRoomRecognitionEngine();
            foreach (var room in engine.Rooms)
            {
                var bboundary = FirstFloorSpace.Boundary as Polyline;
                if (GeomUtils.PtInLoop(bboundary, room.Kitchen.Boundary.GetCenter()))
                {
                    CompositeRoom.Add(room);
                }
            }
            return CompositeRoom;
        }

    }
}

