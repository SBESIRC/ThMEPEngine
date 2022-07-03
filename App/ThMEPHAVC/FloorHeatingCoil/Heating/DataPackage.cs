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
        public List<Polyline> Door = new List<Polyline>();
        public List<ThFloorHeatingRoom> Room = new List<ThFloorHeatingRoom>();
        public ThFloorHeatingWaterSeparator WaterSeparator;
        public List<Polyline> Obstacle = new List<Polyline>();
        public List<Polyline> RoomSeparateLine = new List<Polyline>();
        //output

        //默认构造函数
        public RawData(ThRoomSetModel roomSet)
        {
            Door = roomSet.Door;
            Room = roomSet.Room;
            WaterSeparator = roomSet.WaterSeparator;
            Obstacle = roomSet.FurnitureObstacle;
            List<Line> roomSeparateLine0 = roomSet.RoomSeparateLine;
            foreach (Line line in roomSeparateLine0) 
            {
                Polyline newPl = new Polyline();
                newPl.Closed = false;
                newPl.AddVertexAt(0, line.StartPoint.ToPoint2D(), 0, 0, 0);
                newPl.AddVertexAt(1, line.EndPoint.ToPoint2D(), 0, 0, 0);
                RoomSeparateLine.Add(newPl);
            }
        }
    }

    class ProcessedData
    {
        //空间索引
        public static ThCADCoreNTSSpatialIndex RegionIndex;

        //清理后的polyline
        static public List<SingleRegion> RegionList = new List<SingleRegion>();
        static public List<SingleDoor> DoorList = new List<SingleDoor>();
        static public DoorToDoorDistance[,] DoorToDoorDistanceMap;
        static public List<List<Connection>> RegionConnection;

        static public List<SinglePipe> PipeList = new List<SinglePipe>();
        static public Dictionary<Tuple<int, int>, List<Point3d>> DoorPipeToPointMap = new Dictionary<Tuple<int, int>, List<Point3d>>();

        public ProcessedData()
        {

        }
    }

    class PublicValue
    {
        static public List<TopoTreeNode> RegionTree = new List<TopoTreeNode>();
        


    }
    class Parameter
    {
        static public double TotalLength = 115000;
        
        static public double ClearThreshold = 240;
        static public double ClearExtendLength = 40000; 
        static public double DoorBufferValue = 500;
        static public double SmallTolerance = 0.1;
        static public double WaterSeparatorDis = 3000;

        static public double ConnectionThresholdArea = 50;
        static public double ConnectionThresholdLength = 300;

        static public double SuggestDistanceWall = 100;
        static public double SuggestDistanceRoom = 300;

        //static public double IgnoreWall = 1.6;

              //

        static public double IsLongSide = 1000;

        static public double OptimizationThreshold = 0.8;   //长度到达最长的百分之多少后就不再优化

        static public double SmallPassingThreshold = 3000;
    }
}
