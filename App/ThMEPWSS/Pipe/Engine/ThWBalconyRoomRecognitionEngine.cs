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
                var floorDrains = GetFloorDrains(database, pts);
                var washmachines= GetWashmachines(database, pts);
                var rainPipes = GetRainPipes(database, pts);
                var basinTools = GetBasinTools(database, pts);
                Rooms = ThBalconyRoomService.Build(this.Spaces, washmachines, floorDrains, rainPipes, basinTools);
            }
        }
        private List<ThIfcFloorDrain> GetFloorDrains(Database database, Point3dCollection pts)
        {
            using (ThFloorDrainRecognitionEngine floorDrainsEngine = new ThFloorDrainRecognitionEngine())
            {
                floorDrainsEngine.Recognize(database, pts);
                return floorDrainsEngine.Elements.Cast<ThIfcFloorDrain>().ToList();
            }
        }
        private List<ThIfcWashMachine> GetWashmachines(Database database, Point3dCollection pts)
        {
            using (ThWashMachineRecognitionEngine washmachinesEngine = new ThWashMachineRecognitionEngine())
            {
                washmachinesEngine.Recognize(database, pts);
                return washmachinesEngine.Elements.Cast<ThIfcWashMachine>().ToList();
            }
        }
        private List<ThIfcRainPipe> GetRainPipes(Database database, Point3dCollection pts)
        {
            using (ThRainPipeRecognitionEngine rainPipesEngine = new ThRainPipeRecognitionEngine())
            {
                rainPipesEngine.Recognize(database, pts);
                return rainPipesEngine.Elements.Cast<ThIfcRainPipe>().ToList();
            }
        }
        private List<ThIfcBasin> GetBasinTools(Database database, Point3dCollection pts)
        {
            using (ThBasinRecognitionEngine basinToolsEngine = new ThBasinRecognitionEngine())
            {
                basinToolsEngine.Recognize(database, pts);
                return basinToolsEngine.Elements.Cast<ThIfcBasin>().ToList();
            }
        }
    }
}
