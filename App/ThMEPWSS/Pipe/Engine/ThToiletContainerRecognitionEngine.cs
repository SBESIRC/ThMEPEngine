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
    public class ThToiletContainerRecognitionEngine : ThContainerRecognitionEngine
    {
        public List<ThToiletContainer> ToiletContainers { get; set; }
        public ThToiletContainerRecognitionEngine()
        {
            ToiletContainers = new List<ThToiletContainer>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            ToiletContainers = new List<ThToiletContainer>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var spaces = GetSpaces(database, pts);
                var closestools = GetClosestools(database, pts);
                var floorDrains = GetFloorDrains(database, pts);
                var toiletContainerService = ThToiletContainerService.Build(spaces, closestools, floorDrains);
                ToiletContainers = toiletContainerService.ToiletContainers;
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
