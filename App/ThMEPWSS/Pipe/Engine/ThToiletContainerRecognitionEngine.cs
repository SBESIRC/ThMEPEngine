using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThToiletContainerRecognitionEngine : IDisposable
    {
        public List<ThToiletContainer> ToiletContainer { get; set; }
        public void Recognize(Database database,Point3dCollection pts)
        {
            ToiletContainer = new List<ThToiletContainer>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var spaces = GetSpaces(database, pts);
                var closestools = GetClosestools(database, pts);
                var floorDrains = GetFloorDrains(database, pts);
                var toiletContainerService = ThToiletContainerService.Build(spaces, closestools, floorDrains);
                ToiletContainer = toiletContainerService.ToiletContainers;
            }
        }
        private List<ThIfcSpace> GetSpaces(Database database, Point3dCollection pts)
        {            
            using (ThSpaceRecognitionEngine spaceEngine = new ThSpaceRecognitionEngine())
            {
                spaceEngine.Recognize(database, pts);
                return spaceEngine.Spaces;
            }
        }
        private List<ThIfcClosestool> GetClosestools(Database database, Point3dCollection pts)
        {
            using (ThClosetoolRecognitionEngine closetoolEngine = new ThClosetoolRecognitionEngine())
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

        public void Dispose()
        {           
        }
    }
}
