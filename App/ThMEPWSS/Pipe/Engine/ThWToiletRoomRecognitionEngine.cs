using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWToiletRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWToiletRoom> Rooms { get; set; }
        public List<ThWFloorDrain> FloorDrains { get; set; }
        public List<ThWCondensePipe> CondensePipes { get; set; }
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        public List<ThWClosestool> closestools { get; set; }
        public ThWToiletRoomRecognitionEngine()
        {
            Rooms = new List<ThWToiletRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWToiletRoom>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var spaces = new List<ThIfcSpace>();
                if (this.Spaces.Count == 0)
                {
                    this.Spaces = GetSpaces(database, pts);
                }
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(this.Spaces.Select(o => o.Boundary).ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    spaces = this.Spaces.Where(o => objs.Contains(o.Boundary)).ToList();
                }
                else
                {
                    spaces = this.Spaces;
                }
                Rooms = ThToiletRoomService.Build(spaces, closestools, FloorDrains, CondensePipes, RoofRainPipes);
            }
        }
        private List<ThWClosestool> GetClosestools(Database database, Point3dCollection pts)
        {
            using (ThWClosestoolRecognitionEngine closetoolEngine = new ThWClosestoolRecognitionEngine())
            {
                closetoolEngine.Recognize(database, pts);
                return closetoolEngine.Elements.Cast<ThWClosestool>().ToList();
            }
        }


    }
}
