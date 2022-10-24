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
    public class DataConversion
    {

    }
    //输入
    //车道
    [Serializable]
    public static class LaneDeformationParas
    {
        //车道线宽度的一半
        static public double VehicleLaneWidth;       //这个放这里和放VehicleLane类里面都可以
        static public Polygon Boundary;      //地库外框
        static public List<Polygon> Blocks;   //墙体等障碍物
    }

    //车位的集合块
    [Serializable]
    public class ParkingPlaceBlock
    {
        public VehicleLane FatherVehicleLane; //记录从属于哪个车道
        public Polygon ParkingPlaceBlockObb; //外包框线
        public Vector2D BlockDir;        //*生成的朝向
        public List<SingleParkingPlace> Cars;   //块内包含的车位
        public List<Column> ColunmList;     //块内包含的柱子
    }
    //车道
    [Serializable]
    public class VehicleLane
    {
        public LineSegment CenterLine; //中心线
        public Polygon LaneObb;    //车道外包框线
        public List<ParkingPlaceBlock> ParkingPlaceBlockList; //某个车道生成的车位的集合起来的块
        public bool IsAnchorLane = false;                                     //不能移动的特殊车道信息
        //public double Width;//最小车道宽度 
    }

    //车位
    [Serializable]
    public class SingleParkingPlace
    {
        public VehicleLane FatherVehicleLane;             //记录车位从属于哪个车道
        public ParkingPlaceBlock FatherParkingPlaceBlock; //记录车位从属于哪个块
        public Polygon ParkingPlaceObb;   //车位外包框线
        public int Type;                    //车位类型
        public Vector2D ParkingPlaceDir;   //车位朝向
        public double LongSide;            //长边
        public double ShortSide;           //短边
        //车位原始块是cad图形数据不好传进来，如果以后需要数据可作补充
        //public double original;            //*车位原始块,不知道有没有必要 
        //见柱子注释
        //public List<Column> ColunmList;  //*块内包含的柱子，暂时可有可无
    }

    //柱子
    [Serializable]
    public class Column
    {
        public ParkingPlaceBlock FatherParkingPlaceBlock;    //记录从属于哪一个车位块
        //
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
