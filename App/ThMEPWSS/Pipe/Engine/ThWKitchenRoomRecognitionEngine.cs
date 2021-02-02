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
                var basintools = GetBasintools(database, pts);
                var rainPipes = GetRainPipes(database, pts);
                var roofRainPipes = GetRoofRainPipes(database, pts);
                Rooms = ThKitchenRoomService.Build(this.Spaces, basintools, rainPipes, roofRainPipes);
            }
        }
        private List<ThIfcBasin> GetBasintools(Database database, Point3dCollection pts)
        {
            using (ThBasinRecognitionEngine basintoolEngine = new ThBasinRecognitionEngine())
            {
                basintoolEngine.Recognize(database, pts);
                return basintoolEngine.Elements.Cast<ThIfcBasin>().ToList();
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
