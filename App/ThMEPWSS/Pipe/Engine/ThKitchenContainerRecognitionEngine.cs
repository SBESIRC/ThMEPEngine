using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThKitchenContainerRecognitionEngine : ThContainerRecognitionEngine
    {
        public List<ThKitchenContainer> KitchenContainers { get; set; }
        public ThKitchenContainerRecognitionEngine()
        {
            KitchenContainers = new List<ThKitchenContainer>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var spaces = GetSpaces(database, pts);
                var basintools = GetBasintools(database, pts);
                var kitchenContainerService = ThKitchenContainerService.Build(spaces, basintools);
                KitchenContainers = kitchenContainerService.KitchenContainers;
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
    }
}
