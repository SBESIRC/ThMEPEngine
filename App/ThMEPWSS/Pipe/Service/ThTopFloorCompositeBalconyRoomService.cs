using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Geom;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public class ThTopFloorCompositeBalconyRoomService
    {
        private List<ThWCompositeBalconyRoom> CompositeBalconyRoom { get; set; }
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThTopFloorCompositeBalconyRoomService(
           ThIfcSpace space,
           List<ThWCompositeBalconyRoom> compositeBalconyRoom)
        {
            CompositeBalconyRoom = compositeBalconyRoom;
            Space = space;
            var objs = new DBObjectCollection();
            //Pipes.ForEach(o => objs.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWCompositeBalconyRoom> Find(
            ThIfcSpace space,
            List<ThWCompositeBalconyRoom> compositeBalconyRoom)
        {
            var service = new ThTopFloorCompositeBalconyRoomService(space, compositeBalconyRoom);
            return service.Find(space);
        }
        private List<ThWCompositeBalconyRoom> Find(ThIfcSpace FirstFloorSpace)
        {
            var engine = new ThWCompositeRoomRecognitionEngine();
            foreach (var room in engine.FloorDrainRooms)
            {
                var bboundary = FirstFloorSpace.Boundary as Polyline;
                if (GeomUtils.PtInLoop(bboundary, room.Balcony.Balcony.Boundary.GetCenter()))
                {
                    CompositeBalconyRoom.Add(room);
                }
            }
            return CompositeBalconyRoom;
        }
    
    }
}

