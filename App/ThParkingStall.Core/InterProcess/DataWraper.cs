﻿using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
namespace ThParkingStall.Core.InterProcess
{
    [Serializable]
    public class DataWraper
    {
        #region LayoutParameter(InterParameter)
        public Polygon TotalArea;//总区域
        public List<LineSegment> SegLines;//初始分割线
        public List<Polygon> Buildings;//建筑物，包含坡道
        public List<Polygon> BoundingBoxes;//建筑外包框
        public List<Ramp> Ramps;//坡道

        public List<(double, double)> LowerUpperBound; // 基因的上下边界，绝对值
        public Dictionary<int, List<int>> SegLineIntsecDic ;// 分割线相交关系
        #endregion
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

        #endregion
    }

    public class ProgramDebug
    {
        public static string TestMain(string[] ProcessInfo, ChromosomeCollection chromosomeCollection)
        {

            var ProcessCount = Int32.Parse(ProcessInfo[0]);
            var ProcessIndex = Int32.Parse(ProcessInfo[1]);
            var StrResult = "";
            var Chromosomes = chromosomeCollection.Chromosomes;
            for (int i = 0; i < Chromosomes.Count / ProcessCount; i++)
            {
                int j = i * ProcessCount + ProcessIndex;
                if (j >= Chromosomes.Count) break;
                var chromosome = Chromosomes[j];
                var subAreas = InterParameter.GetSubAreas(chromosome);
                List<MParkingPartitionPro> mParkingPartitionPros = new List<MParkingPartitionPro>();
                var ParkingCount = CalculateTheTotalNumOfParkingSpace(subAreas, ref mParkingPartitionPros);
                StrResult += ParkingCount.ToString() + ' ';
            }
            return StrResult;
        }
    }
}