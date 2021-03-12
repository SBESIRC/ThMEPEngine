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

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWKitchenRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWKitchenRoom> Rooms { get; set; }
        public List<ThWRainPipe> RainPipes { get; set; }
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        public List<ThWBasin> BasinTools { get; set; }
        public List<ThWCondensePipe> CondensePipes { get; set; }
        public List<ThWFloorDrain> FloorDrains { get; set; }

        public ThWKitchenRoomRecognitionEngine()
        {
            Rooms = new List<ThWKitchenRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
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
                Rooms = ThKitchenRoomService.Build(spaces, BasinTools, RainPipes, RoofRainPipes, CondensePipes, FloorDrains);
            }
        }   
    }
}
