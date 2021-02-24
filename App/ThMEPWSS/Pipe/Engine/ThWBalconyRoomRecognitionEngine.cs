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
    public class ThWBalconyRoomRecognitionEngine : ThWRoomRecognitionEngine
    {
        public List<ThWBalconyRoom> Rooms { get; set; }
        public List<ThWWashingMachine> Washmachines { get; set; }
        public List<ThWFloorDrain> FloorDrains { get; set; }
        public List<ThWRainPipe> RainPipes { get; set; }
        public List<ThWBasin> BasinTools { get; set; }

        public ThWBalconyRoomRecognitionEngine()
        {
            Rooms = new List<ThWBalconyRoom>();
        }
        public override void Recognize(Database database, Point3dCollection pts)
        {
            Rooms = new List<ThWBalconyRoom>();
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
                
                Rooms = ThBalconyRoomService.Build(spaces, Washmachines, FloorDrains, RainPipes, BasinTools);
            }
        }
    }
}
