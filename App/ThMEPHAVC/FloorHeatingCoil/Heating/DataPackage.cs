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
        public List<ThFloorHeatingBathRadiator> BathRadiators { get; set; } = new List<ThFloorHeatingBathRadiator>();
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

            if (roomSet.BathRadiators != null && roomSet.BathRadiators.Count > 0)
            {
                Parameter.HaveRadiator = true;
                BathRadiators = roomSet.BathRadiators;
            }
            else 
            {
                Parameter.HaveRadiator = false;
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
        static public Dictionary<Tuple<int, int>, PipePoint> DoorPipeToPointMap = new Dictionary<Tuple<int, int>, PipePoint>();

        //散热器
        public static int RadiatorRegion = -1;
        public static List<Point3d> RadiatorPointList = new List<Point3d>();
        public static Vector3d RadiatorDir = new Vector3d();

        //集水器
        public static bool LeftToRight = false;
        public static Vector3d WaterDir = new Vector3d();
        public static int WaterDirIndex = -10;
        static public Vector3d WaterOffset = new Vector3d(0, 0, 0);

        //虚拟管道
        public static bool HaveVirtualPipe = false;
        public static Point3d VirtualPOut = new Point3d();
        public static Polyline VirtualPlNow = new Polyline();


        public ProcessedData()
        {

        }

        public static void Clear() 
        {
            RegionIndex = null;
            RegionList = null;
            DoorList = null;
            DoorToDoorDistanceMap = null;
            RegionConnection = null;
            PipeList = null;
            DoorPipeToPointMap = null;
        }


        public static void ClearVirtualPipe() 
        {
            HaveVirtualPipe = false;
            VirtualPOut = new Point3d();
            VirtualPlNow = new Polyline();
        }
    }

    class PublicValue
    {
        static public List<TopoTreeNode> RegionTree = new List<TopoTreeNode>();

    }

    class Parameter
    {
        //用户输入设定
        static public double TotalLength = 115000;

        static public double SuggestDistanceWall = 200;      //推荐靠墙间距
        static public double SuggestDistanceRoom = 250;      //推荐管道间距
        static public double PipeSpaceing = 50;              //集水器管间距
        static public double KeyRoomShortSide = 2000;        //什么样的房间被认定为单独只出一根管道

        //用户输入模式
        static public bool IndependentRoomConstraint = true;  //独立房间回路
        static public bool PublicRegionConstraint = true;     //公区约束
        static public bool AuxiliaryRoomConstraint = true;    //附属房间经过主房间之后是否能够和其他回路相连
        static public int PrivatePublicMode = 0;    // 0：自动/公建  1：住宅  2：自动

        //识别得到状态
        static public bool HaveRadiator = false;

        //
        static public double SuggestDistancePass = 600;

        //连接关系
        static public double ConnectionThresholdArea = 50;
        static public double ConnectionThresholdLength = 300;
        
        //清理框线
        static public double ClearThreshold = 280;
        static public double ClearExtendLength = 40000;    //清理耳朵时的延长长度
        
        //寻找门
        static public double DoorBufferValue = 500;       //门找区域时的单方向Buffer长度

        //寻找集水器
        static public double WaterSeparatorDis = 1000;

        //分配用全局变量
        static public double OptimizationThreshold = 0.8;   //长度到达最长的百分之多少后就不再优化

        static public double SmallPassingThreshold = 2000;

        //用于找出入口点位
        static public double IsLongSide = 1000;             //判断一面墙是否够长

        //构建管线之间的插头时，插头向外延伸的长度
        static public double ConnectorLength = 200;
        

        //判断点是否在直线上
        static public double SmallTolerance = 0.1;
    }

    class TestData
    {
        public static double SuggestWallDis = 100;
        public static double SuggestPipeDis = 400;
    }
}
