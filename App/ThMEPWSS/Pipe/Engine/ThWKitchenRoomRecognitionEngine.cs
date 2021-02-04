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
    public class ThWKitchenRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWKitchenRoom> Rooms { get; set; }
        public List<ThIfcRainPipe> RainPipes { get; set; }
        public List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        public List<ThIfcBasin> BasinTools { get; set; }
        public ThWKitchenRoomRecognitionEngine()
        {
            Rooms = new List<ThWKitchenRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }                                          
                Rooms = ThKitchenRoomService.Build(this.Spaces, BasinTools, RainPipes, RoofRainPipes);
            }
        }   
    }
}
