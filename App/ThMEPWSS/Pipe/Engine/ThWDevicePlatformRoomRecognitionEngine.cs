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
                var floorDrains = GetFloorDrains(database, pts);
                var rainPipes = GetRainPipes(database, pts);
                var condensePipes = GetCondensePipes(database, pts);
                var roofRainPipes = GetRoofRainPipes(database, pts);
                Rooms = ThDevicePlatformRoomService.Build(this.Spaces, floorDrains, rainPipes, condensePipes, roofRainPipes);
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
        private List<ThIfcRainPipe> GetRainPipes(Database database, Point3dCollection pts)
        {
            using (ThRainPipeRecognitionEngine rainPipesEngine = new ThRainPipeRecognitionEngine())
            {
                rainPipesEngine.Recognize(database, pts);
                return rainPipesEngine.Elements.Cast<ThIfcRainPipe>().ToList();
            }
        }
        private List<ThIfcCondensePipe> GetCondensePipes(Database database, Point3dCollection pts)
        {
            using (ThCondensePipeRecognitionEngine condensePipesEngine = new ThCondensePipeRecognitionEngine())
            {
                condensePipesEngine.Recognize(database, pts);
                return condensePipesEngine.Elements.Cast<ThIfcCondensePipe>().ToList();
            }
        }
        private List<ThIfcRoofRainPipe> GetRoofRainPipes(Database database, Point3dCollection pts)
        {
            using (ThRoofRainPipeRecognitionEngine roofRainPipesEngine = new ThRoofRainPipeRecognitionEngine())
            {
                roofRainPipesEngine.Recognize(database, pts);
                return roofRainPipesEngine.Elements.Cast<ThIfcRoofRainPipe>().ToList();
            }
        }
    }
}
