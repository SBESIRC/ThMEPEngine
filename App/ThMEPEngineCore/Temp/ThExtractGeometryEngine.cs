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
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractGeometryEngine : IDisposable
    {
        public ThExtractParameter ExtractParameter { get; set; }
        public Dictionary<Polyline, List<Polyline>> Obstructs { get; private set; }
        public List<ThIfcSpace> Spaces { get; private set; }
        public List<Polyline> Doors { get; private set; }
        public List<Polyline> Walls { get; private set; }
        public List<Curve> CenterLines { get; private set; }
        public List<Polyline> Columns { get; private set; }
        public List<Curve> DrainageFacilities { get; private set; }
        public List<ThIfcSpace> ParkingStalls { get; private set; }
        public List<Line> LaneLines { get; private set; }
        public Dictionary<string, List<Polyline>> Equipments {get; private set;}
        public Dictionary<Polyline, string> ConnectPorts { get; private set; }
        
        public ThExtractGeometryEngine()
        {
        }
        public void Dispose()
        {            
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            Spaces = ExtractParameter.IsExtractSpace ? BuildSpaces(database, pts) : new List<ThIfcSpace>();
            Walls = ExtractParameter.IsExtractWall ? BuildWalls(database, pts) : new List<Polyline>();
            Doors = ExtractParameter.IsExtractDoor ? BuildDoors(database, pts) : new List<Polyline>();
            Equipments = ExtractParameter.IsExtractEquipment ? BuildEquipments(database, pts) : new Dictionary<string, List<Polyline>>();
            ConnectPorts = ExtractParameter.IsExtractConnectPort ? BuildConnectPorts(database, pts) : new Dictionary<Polyline, string>();
            Columns = ExtractParameter.IsExtractColumn ? BuildColumns(database, pts) : new List<Polyline>();
            DrainageFacilities = ExtractParameter.IsExtractDrainageFacility ? BuildDrainageFacilities(database, pts) : new List<Curve>();
            ParkingStalls = ExtractParameter.IsExtractParkingStall ? BuildParkingStalls(database, pts) : new List<ThIfcSpace>();
            LaneLines = ExtractParameter.IsExtractLaneLine ? BuildLaneLines(database, pts) : new List<Line>();
            Obstructs = ExtractParameter.IsExtractObstruct ? BuildObstructs() : new Dictionary<Polyline, List<Polyline>>();
            CenterLines = ExtractParameter.IsExtractCenterLine ? BuildCenterLine(database, pts) : new List<Curve>();
        }      
        private List<ThIfcSpace> BuildSpaces(Database HostDb, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            using (var spaceEngine = new ThExtractSpaceRecognitionEngine())
            {
                spaceEngine.SpaceLayer = "AD-AREA-OUTL";
                spaceEngine.NameLayer = "AD-NAM";
                //spaceEngine.NameLayer = "空间名称";

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
        private List<Polyline> BuildWalls(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractWallService()
            {
                WallLayer = "墙",
            };
            instance.Extract(HostDb, pts);
            return instance.Walls;
        }
        private List<Curve> BuildCenterLine(Database HostDb, Point3dCollection pts)
        {
            var instance = new ThExtractCenterLineService()
            {
                CenterLineLayer = "中心线示意",
            };
            instance.Extract(HostDb, pts);
            return instance.CenterLines;
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
                return lines.Where(o=>o.Length>=3000).ToList();
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
    public class ThExtractParameter
    {
        public bool IsExtractObstruct { get; set; } = false;
        public bool IsExtractSpace { get; set; } = false;
        public bool IsExtractDoor { get; set; } = false;
        public bool IsExtractWall { get; set; } = false;
        public bool IsExtractCenterLine { get; set; } = false;
        public bool IsExtractColumn { get; set; } = false;
        public bool IsExtractDrainageFacility { get; set; } = false;
        public bool IsExtractParkingStall { get; set; } = false;
        public bool IsExtractLaneLine { get; set; } = false;
        public bool IsExtractEquipment { get; set; } = false;
        public bool IsExtractConnectPort { get; set; } = false;
        public double ArcLength { get; set; } = 50.0;
    }
}
