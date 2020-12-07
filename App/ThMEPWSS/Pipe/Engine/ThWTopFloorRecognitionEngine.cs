using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWTopFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWTopFloorRoom> Rooms { get; set; }
        public ThWTopFloorRecognitionEngine()
        {
            Rooms = new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWTopFloorRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                var basepoint = new List<ThIfcSpace>();
                var compositeroom = Getcompositeroom(database, pts);
                var compositebalconyroom = Getcompositebalconyroom(database, pts);
                Rooms = ThTopFloorRoomService.Build(this.Spaces, basepoint, compositeroom, compositebalconyroom);
            }
        }
        private List<ThWCompositeRoom> Getcompositeroom(Database database, Point3dCollection pts)
        {
            using (ThWCompositeRoomRecognitionEngine compositeRoomRecognitionEngine = new ThWCompositeRoomRecognitionEngine())
            {
                compositeRoomRecognitionEngine.Recognize(database, pts);
                return compositeRoomRecognitionEngine.Rooms;
            }
        }
        private List<ThWCompositeBalconyRoom> Getcompositebalconyroom(Database database, Point3dCollection pts)
        {
            using (ThWCompositeRoomRecognitionEngine compositeRoomRecognitionEngine = new ThWCompositeRoomRecognitionEngine())
            {
                compositeRoomRecognitionEngine.Recognize(database, pts);
                return compositeRoomRecognitionEngine.FloorDrainRooms;
            }
        }
    }
}
