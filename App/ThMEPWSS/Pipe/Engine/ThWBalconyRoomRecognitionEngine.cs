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
   public class ThWBalconyRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWBalconyRoom> Rooms { get; set; }
        public List<ThIfcWashMachine> Washmachines { get; set; }
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        public List<ThIfcRainPipe> RainPipes { get; set; }
        public List<ThIfcBasin> BasinTools { get; set; }

        public ThWBalconyRoomRecognitionEngine()
        {
            Rooms = new List<ThWBalconyRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWBalconyRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }              
               
                Rooms = ThBalconyRoomService.Build(this.Spaces, Washmachines, FloorDrains, RainPipes, BasinTools);
            }
        }
    }
}
