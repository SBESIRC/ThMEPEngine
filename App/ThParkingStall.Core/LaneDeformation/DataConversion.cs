using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.LaneDeformation
{
    //输入
    [Serializable]
    public class PureVector
    {
        public PureVector(double x, double y)
        {
            X = x;
            Y = y;
        }
        public double X { get; set; }
        public double Y { get; set; }
    }

    //车道
    [Serializable]
    public static class LaneDeformationParas
    {
        //车道线宽度的一半
        static public double VehicleLaneWidth;    //这个放这里和放VehicleLane类里面都可以
        static public Polygon Boundary;           //地库外框
        static public List<Polygon> Blocks;       //墙体等障碍物
    }

    //接口转换使用数据
    [Serializable]
    public class VehicleLaneData
    {
        public VehicleLaneData()
        {

        }
        public VehicleLaneData(List<VehicleLane> vehicleLanes)
        {
            VehicleLanes=vehicleLanes;
        }
        public List<VehicleLane> VehicleLanes = new List<VehicleLane>();
        public double VehicleLaneWidth;       //这个放这里和放VehicleLane类里面都可以
        public Polygon Boundary;      //地库外框
        public List<Polygon> Blocks;   //墙体等障碍物
    }

    //车道
    [Serializable]
    public class VehicleLane
    {
        public VehicleLane(LineSegment centerLine, Polygon laneObb, PureVector vec)
        {
            CenterLine=centerLine;
            LaneObb = laneObb;
            Vec=vec;
        }
        public PureVector Vec;//接口转换使用数据,目前会认为一条车道线，垂直的两个方向生成车位，其实是两条车道线对应两个相反方向——如果这一步不需要区分，接口可作调整
        public LineSegment CenterLine; //中心线
        public Polygon LaneObb;    //车道外包框线
        public List<ParkingPlaceBlock> ParkingPlaceBlockList = new List<ParkingPlaceBlock>(); //某个车道生成的车位的集合起来的块
        public bool IsAnchorLane = false;                                     //不能移动的特殊车道信息
                                                                              //public double Width;//最小车道宽度 
        //static:方便数据传递
        //车道线宽度的一半
    }
    //车位的集合块
    [Serializable]
    public class ParkingPlaceBlock
    {
        public ParkingPlaceBlock(LineSegment sourceLane, Polygon parkingPlaceBlockObb, PureVector blockDir, List<SingleParkingPlace> cars, List<LDColumn> colunmList)
        {
            SourceLane = sourceLane;
            ParkingPlaceBlockObb = parkingPlaceBlockObb;
            BlockDir = blockDir;
            Cars = cars;
            ColunmList = colunmList;
            InitColunmProperties();
            InitCarProperties();
        }
        void InitColunmProperties()
        {
            ColunmList.ForEach(e => e.FatherParkingPlaceBlock = this);
        }
        void InitCarProperties()
        {
            Cars.ForEach(e => e.FatherParkingPlaceBlock = this);
        }
        public LineSegment SourceLane { get; set; }//接口转换使用数据
        //记录从属于哪个车道
        private VehicleLane _FatherVehicleLane = null;
        public VehicleLane FatherVehicleLane
        {
            get { return _FatherVehicleLane; }
            set
            {
                _FatherVehicleLane = value;
                Cars.ForEach(e => e.FatherVehicleLane = value);
            }
        }
        public Polygon ParkingPlaceBlockObb; //外包框线
        public PureVector BlockDir;        //*生成的朝向
        public List<SingleParkingPlace> Cars;   //块内包含的车位
        public List<LDColumn> ColunmList;     //块内包含的柱子
    }

    //车位
    [Serializable]
    public class SingleParkingPlace
    {
        public SingleParkingPlace(Polygon polygon, int type, PureVector vec,Coordinate point)
        {
            ParkingPlaceObb=polygon;
            Type=type;
            ParkingPlaceDir=vec;
            Point=point;
            InitParas();
        }
        void InitParas()
        {
            var nums = GetEdges(ParkingPlaceObb).Select(e => e.Length).OrderBy(e => e).ToList();
            ShortSide = nums[0];
            LongSide = nums[3];
        }
        public Coordinate Point { get; set; }//数据转换使用,如果车位发生偏移了，请将此点偏移同向量
        public VehicleLane FatherVehicleLane { get; set; }        //记录车位从属于哪个车道
        public ParkingPlaceBlock FatherParkingPlaceBlock { get; set; }  //记录车位从属于哪个块
        public Polygon ParkingPlaceObb;   //车位外包框线
        public int Type;                    //车位类型
        public PureVector ParkingPlaceDir;   //车位朝向
        public double LongSide;            //长边
        public double ShortSide;           //短边
                                           //车位原始块是cad图形数据不好传进来，如果以后需要数据可作补充
                                           //public double original;            //*车位原始块,不知道有没有必要 
                                           //见柱子注释
                                           //public List<Column> ColunmList;  //*块内包含的柱子，暂时可有可无
        static List<LineSegment> GetEdges(Polygon polygon)
        {
            List<LineSegment> edges = new List<LineSegment>();
            for (int i = 0; i < polygon.Coordinates.Count() - 1; i++)
                edges.Add(new LineSegment(polygon.Coordinates[i], polygon.Coordinates[i + 1]));
            return edges;
        }
    }

    //柱子
    [Serializable]
    public class LDColumn
    {
        public LDColumn(Polygon colunmnObb)
        {
            ColunmnObb = colunmnObb;
        }
        public ParkingPlaceBlock FatherParkingPlaceBlock { get; set; }    //记录从属于哪一个车位块
        //note:柱子和车位之间的从属关系不是很明确，通常是柱子在两个车位中间的情况
        //public SingleParkingPlace FatherParkingPlace;        //*记录从属于哪一个车位
        public Polygon ColunmnObb;
    }
    public enum SingleParkingPlaceType : int
    {
        //后续可能有新的车位 应该暂时不重要不考虑
        VERT = 0,//垂直式，单排的一排 5300*2400
        PARALLEL = 1,//平行式，车位长边平行于车道
        VERTBACKBACK = 2,//背靠背的垂直式，两个垂直式背靠背排在一起时，尺寸可以缩短5100*2400
    }

}
