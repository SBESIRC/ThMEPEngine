using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeFloorRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWRoofDeviceFloorRoom> RoofDeviceFloors { get; set; }

        public List<ThWRoofFloorRoom> RoofFloors { get; set; }
        public List<ThWTopFloorRoom> TopFloors { get; set; }

        public ThWCompositeFloorRecognitionEngine()
        {
            RoofDeviceFloors = new List<ThWRoofDeviceFloorRoom>();
            RoofFloors = new List<ThWRoofFloorRoom>();
            TopFloors = new List<ThWTopFloorRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var RoofDeviceEngine = new ThWRoofDeviceFloorRecognitionEngine();
                RoofDeviceEngine.Recognize(database, pts);
                RoofDeviceFloors = RoofDeviceEngine.Rooms;
                var RoofEngine = new ThWRoofFloorRecognitionEngine()
                {
                    Spaces = RoofDeviceEngine.Spaces
                };
                RoofEngine.Recognize(database, pts);
                RoofFloors = RoofEngine.Rooms;
                var FirstEngine = new ThWTopFloorRecognitionEngine()
                {
                    Spaces = RoofEngine.Spaces
                };
                FirstEngine.Recognize(database, pts);
                TopFloors = FirstEngine.Rooms;
            }
        }
    }
}
