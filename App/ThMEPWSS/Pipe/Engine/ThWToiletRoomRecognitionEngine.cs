using System.Linq;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWToiletRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWToiletRoom> Rooms { get; set; }
        public ThWToiletRoomRecognitionEngine()
        {
            Rooms = new List<ThWToiletRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWToiletRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                var closestools = GetClosestools(database, pts);
                var floorDrains = GetFloorDrains(database, pts);
                Rooms = ThToiletRoomService.Build(this.Spaces, closestools, floorDrains);
            }
        }
        private List<ThIfcClosestool> GetClosestools(Database database, Point3dCollection pts)
        {
            using (ThClosestoolRecognitionEngine closetoolEngine = new ThClosestoolRecognitionEngine())
            {
                closetoolEngine.Recognize(database, pts);
                return closetoolEngine.Elements.Cast<ThIfcClosestool>().ToList();
            }
        }
        private List<ThIfcFloorDrain> GetFloorDrains(Database database, Point3dCollection pts)
        {
            using (ThFloorDrainRecognitionEngine floorDrainEngine = new ThFloorDrainRecognitionEngine())
            {
                floorDrainEngine.Recognize(database, pts);
                return floorDrainEngine.Elements.Cast<ThIfcFloorDrain>().ToList();
            }
        }
    }
}
