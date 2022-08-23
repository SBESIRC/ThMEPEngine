using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
namespace ThParkingStall.Core.InterProcess
{
    [Serializable]
    public class DataWraper
    {
        public InterParamWraper interParamWraper = null;//正交数据
        public OParamWraper oParamWraper = null;//斜交数据
        #region ViewModel Parameters
        //平行车位尺寸,长度
        public int ParallelSpotLength = 6000; //mm
        //平行车位尺寸,宽度
        public int ParallelSpotWidth = 2400; //mm
        //垂直车位尺寸, 长度
        public int VerticalSpotLength = 5100; //mm
        //垂直车位尺寸, 宽度
        public int VerticalSpotWidth = 2400; //mm
        //道路宽
        public int RoadWidth = 5500; //mm
        //平行于车道方向柱子尺寸
        public int ColumnSizeOfParalleToRoad = 500; //mm
        //垂直于车道方向柱子尺寸
        public int ColumnSizeOfPerpendicularToRoad = 500; //mm
        //柱子完成面尺寸
        public int ColumnAdditionalSize = 50; //mm
        //柱子完成面是否影响车道净宽
        public bool ColumnAdditionalInfluenceLaneWidth = true;
        //最大柱间距,需要改成柱间距
        public int ColumnWidth = 7800; //mm
        //背靠背模块：缩进200
        public bool DoubleRowModularDecrease200 = true;
        //尽端环通
        public bool AllowLoopThroughEnd = false;
        //背靠背长度限制
        public int DisAllowMaxLaneLength = 50000;
        //背靠背模块：柱子沿车道法向偏移距离
        public int ColumnShiftDistanceOfDoubleRowModular = 550; //mm
        //背靠背模块是否使用中柱
        public bool MidColumnInDoubleRowModular = true;
        //单排模块：柱子沿车道法向偏移距离
        public int ColumnShiftDistanceOfSingleRowModular = 550; //mm
        //车位碰撞参数D1（侧面）
        public int D1 = 300;
        //车位碰撞参数D2（尾部）
        public int D2 = 200;
        //迭代次数
        public int IterationCount = -1;
        public int RunMode;
        //横向优先_纵向车道计算长度调整_背靠背模块
        public double LayoutScareFactor_Intergral = 0.7;
        //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
        public double LayoutScareFactor_Adjacent = 0.7;
        //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
        public double LayoutScareFactor_betweenBuilds = 0.7;
        //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
        public double LayoutScareFactor_SingleVert = 0.7;
        //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
        public double SingleVertModulePlacementFactor = 1.0;
        //加速运算
        public bool SpeedUpMode = false;
        #endregion
        public Chromosome chromosome = null;//正交基因记录
        public Genome genome = null;//斜交基因记录
    }

    [Serializable]
    public class InterParamWraper
    {
        public Polygon TotalArea;//总区域
        public List<LineSegment> SegLines;//初始分区线
        public List<Polygon> Buildings;//建筑物，包含坡道
        public List<int> OuterBuildingIdxs = new List<int>(); //可穿建筑物（外围障碍物）的index,包含坡道
        public List<Polygon> BoundingBoxes;//建筑外包框
        public List<Ramp> Ramps;//坡道
        public List<(double, double)> LowerUpperBound; // 基因的上下边界，绝对值
        public List<List<int>> SeglineIndexList;// 分区线相交关系
        public List<(bool, bool)> SeglineConnectToBound;//分区线（负，正）方向是否与边界连接
        public List<(int, int, int, int)> SegLineIntSecNode;//四岔节点关系，上下左右的分区线index
    }

    [Serializable]
    public class OParamWraper
    {
        public Polygon TotalArea;//总区域
        public List<SegLine> SegLines;//初始分区线
        public List<Polygon> Buildings;//所有建筑物，包含坡道
        public List<ORamp> Ramps;//坡道
        public List<(List<int>, List<int>)> seglineIndex;//每根分区线初始以及终点接到哪
        public List<LineSegment> borderLines = null;//可动边界
        //缺一个可动边界的连接关系
    }
    public class ProgramDebug
    {
        public static List<int> TestMain(string[] ProcessInfo, ChromosomeCollection chromosomeCollection)
        {

            var ProcessCount = Int32.Parse(ProcessInfo[0]);
            var ProcessIndex = Int32.Parse(ProcessInfo[1]);
            var Result = new List<int>();
            var Chromosomes = chromosomeCollection.Chromosomes;
            for (int i = 0; i <= Chromosomes.Count / ProcessCount; i++)
            {
                int j = i * ProcessCount + ProcessIndex;
                if (j >= Chromosomes.Count) break;
                var chromosome = Chromosomes[j];
                var subAreas = InterParameter.GetSubAreas(chromosome);
                List<MParkingPartitionPro> mParkingPartitionPros = new List<MParkingPartitionPro>();
                MParkingPartitionPro mParkingPartition = new MParkingPartitionPro();
                var ParkingCount = CalculateTheTotalNumOfParkingSpace(subAreas, ref mParkingPartitionPros,ref mParkingPartition);
                Result.Add(ParkingCount);
            }
            return Result;
        }
    }
}
