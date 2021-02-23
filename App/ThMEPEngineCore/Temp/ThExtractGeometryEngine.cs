using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.LaneLine;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractGeometryEngine : IDisposable
    {
        public Dictionary<Polyline, List<Polyline>> Obstructs { get; private set; }
        public List<ThIfcSpace> Spaces { get; private set; }
        public List<Polyline> Doors { get; private set; }
        public List<Polyline> Columns { get; private set; }
        public List<Curve> DrainageFacilities { get; private set; }
        public List<ThIfcSpace> ParkingStalls { get; private set; }
        public List<Line> LaneLines { get; private set; }
        public Dictionary<string, List<Polyline>> Equipments {get; private set;}
        public Dictionary<Polyline, string> ConnectPorts { get; private set; }
        public double ArcLength { get; set; } = 50.0;
        public ThExtractGeometryEngine()
        {
            Spaces = new List<ThIfcSpace>();
            Doors = new List<Polyline>();
            Obstructs = new Dictionary<Polyline, List<Polyline>>();
            Equipments = new Dictionary<string, List<Polyline>>();
            ConnectPorts = new Dictionary<Polyline, string>();
        }
        public void Dispose()
        {            
        }
        public void Extract(Database database,Point3dCollection pts)
        {
            Spaces = BuildSpaces(database, pts);
            Doors = BuildDoors(database, pts);
            Equipments = BuildEquipments(database, pts);
            ConnectPorts = BuildConnectPorts(database, pts);
            Columns = BuildColumns(database, pts);
            DrainageFacilities = BuildDrainageFacilities(database, pts);
            ParkingStalls = BuildParkingStalls(database, pts);
            LaneLines = BuildLaneLines(database, pts);
            Obstructs = BuildObstructs();
        }       
        private List<ThIfcSpace> BuildSpaces(Database HostDb, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            using (var spaceEngine = new ThExtractSpaceRecognitionEngine())
            {
                spaceEngine.Recognize(HostDb, pts);
                return spaceEngine.Spaces;
            }
        }
        private List<Polyline> BuildDoors(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractDoorService();
            instance.Extract(HostDb, pts);
            return instance.Doors;
        }
        private Dictionary<string, List<Polyline>> BuildEquipments(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractEquipmentService();
            instance.Extract(HostDb, pts);
            return instance.Equipments;
        }
        private Dictionary<Polyline, string> BuildConnectPorts(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractConnectPortsService();
            instance.Extract(HostDb, pts);
            return instance.ConnectPorts;
        }
        private List<Polyline> BuildColumns(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractColumnService();
            instance.Extract(HostDb, pts);
            return instance.Columns;
        }
        private List<Curve> BuildDrainageFacilities(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractDrainageFacilityService();
            instance.Extract(HostDb, pts);
            return instance.Facilities;
        }
        private List<ThIfcSpace> BuildParkingStalls(Database HostDb, Point3dCollection pts)
        {
            using (var engine =new ThParkingStallRecognitionEngine())
            {
                engine.Recognize(HostDb, pts);
                return engine.Spaces;
            }
        }
        private List<Line> BuildLaneLines (Database HostDb, Point3dCollection pts)
        {
            using (var engine = new ThLaneLineRecognitionEngine())
            {
                engine.Recognize(HostDb, pts);

                // 车道中心线处理
                var curves = engine.Spaces.Select(o => o.Boundary).ToList();
                var lines = ThLaneLineSimplifier.Simplify(curves.ToCollection(), 1500);
                return lines;
            }
        }
        private Dictionary<Polyline,List<Polyline>> BuildObstructs()
        {
            //在停车区域空间
            var obstacles = new Dictionary<Polyline, List<Polyline>>();
            var spaces = Spaces
                 .Where(o => o.Tags.Where(g => g.Contains("停车区域")).Any())
                 .Select(o => o.Boundary).ToList();
            spaces.ForEach(o => obstacles.Add(o.Clone() as Polyline, ThArrangeObstacleService.Arrange(o as Polyline)));
            return obstacles;
        }
    }
}
