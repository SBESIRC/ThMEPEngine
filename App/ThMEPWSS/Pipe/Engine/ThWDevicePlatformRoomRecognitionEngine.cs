using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Service;
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
                Rooms = ThDevicePlatformRoomService.Build(spaces, FloorDrains, RainPipes, CondensePipes, RoofRainPipes);
            }
        }
    }
}
