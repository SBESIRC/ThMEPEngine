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
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        public List<ThIfcCondensePipe> CondensePipes { get; set; }
        public List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
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
                Rooms = ThToiletRoomService.Build(this.Spaces, closestools, FloorDrains, CondensePipes, RoofRainPipes);
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
    
     
    }
}
