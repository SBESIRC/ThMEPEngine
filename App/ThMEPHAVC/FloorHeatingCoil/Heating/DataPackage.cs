using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{

    class RawData
    {
        //输入属性
        public List<Polyline> Door;
        public List<ThFloorHeatingRoom> Room;
        public ThFloorHeatingWaterSeparator WaterSeparator;
        public List<Polyline> Obstacle;
        public List<Polyline> RoomSeparateLine;
        //output

        //默认构造函数
        public RawData(ThFloorHeatingDataProcessService dataQuery)
        {
            Door = dataQuery.Door;
            Room = dataQuery.Room;
            WaterSeparator = dataQuery.WaterSeparator;
            Obstacle = dataQuery.FurnitureObstacle;
            RoomSeparateLine = dataQuery.RoomSeparateLine;   
        }
    }

    class ProcessedData
    {
        //空间索引
        public static ThCADCoreNTSSpatialIndex RegionIndex;

        //清理后的polyline
        static public List<SingleRegion> RegionList = new List<SingleRegion>();
        static public List<SingleDoor> DoorList = new List<SingleDoor>();
        static public  DoorToDoorDistance[,] DoorToDoorDistanceMap;


        static public List<SinglePipe> PipeList = new List<SinglePipe>();
        static public Dictionary<Tuple<int, int>, List<Point3d>> DoorPipeToPointMap = new Dictionary<Tuple<int, int>, List<Point3d>>();

        public ProcessedData()
        {

        }
    }

    class PublicValue
    {
        static public List<TopoTree> RegionTree = new List<TopoTree>();
        


    }
    class Parameter
    {
        static public double TotalLength = 130000;
        
        static public double ClearThreshold = 240;
        static public double DoorBufferValue = 500;
        static public double SmallTolerance = 0.1;

        static public double ConnectionThresholdArea = 50;
        static public double ConnectionThresholdLength = 300;

        static public double SuggestDistanceWall = 100;
        static public double SuggestDistanceRoom = 300;

        static public double IgnoreWall = 1.6;

        static public double WaterSeparatorDis = 3000;

        static public double IsLongSide = 1000;
    }
}
