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
   public class ThWDevicePlatformRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWDevicePlatformRoom> Rooms { get; set; }
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        public List<ThIfcRainPipe> RainPipes { get; set; }
        public List<ThIfcCondensePipe> CondensePipes { get; set; }
        public List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        public ThWDevicePlatformRoomRecognitionEngine()
        {
            Rooms = new List<ThWDevicePlatformRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWDevicePlatformRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }         
                Rooms = ThDevicePlatformRoomService.Build(this.Spaces, FloorDrains, RainPipes, CondensePipes, RoofRainPipes);
            }
        }
    }
}
