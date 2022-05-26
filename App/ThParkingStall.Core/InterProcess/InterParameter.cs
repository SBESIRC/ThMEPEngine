using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;
namespace ThParkingStall.Core.InterProcess
{
    public static class InterParameter 
    {
        private static Polygon _TotalArea;//总区域，边界为外包框
        public static Polygon TotalArea { get { return _TotalArea; }}//总区域，边界为外包框

        private static List<LineSegment> _InitSegLines;//所有初始分割线
        public static List<LineSegment> InitSegLines { get { return _InitSegLines; } }//所有初始分割线

        private static List<Polygon> _Buildings;// 所有障碍物，包含坡道
        private static List<Polygon> Buildings { get { return _Buildings; } }// 所有障碍物，包含坡道

        public  static List<int> _OuterBuildingIdxs; 
        public  static List<int> OuterBuildingIdxs { get { return _OuterBuildingIdxs; } } //可穿建筑物（外围障碍物）的index,包含坡道
        private static List<Polygon> _BoundingBoxes;// 所有的建筑物的边框
        private static List<Polygon> BoundingBoxes { get { return _BoundingBoxes; } }// 所有的建筑物的边框

        private static List<List<int>> _SegLineIntsecList;//分割线临近线
        public static List<List<int>> SeglineIndexList { get { return _SegLineIntsecList; } }//分割线临近线
        private static List<(bool, bool)> _SeglineConnectToBound;//分割线（负，正）方向是否与边界连接
        public static List<(bool, bool)> SeglineConnectToBound { get { return _SeglineConnectToBound; } }//分割线（负，正）方向是否与边界连接

        private static List<Ramp> _Ramps;//坡道
        public static List<Ramp> Ramps { get { return _Ramps; } }//坡道

        private static MNTSSpatialIndex _BuildingSpatialIndex;//所有障碍物，包含坡道的spatialindex
        public static MNTSSpatialIndex BuildingSpatialIndex { get { return _BuildingSpatialIndex; } }//所有障碍物，包含坡道的spatialindex
        private static MNTSSpatialIndex _BoundaryObjectsSPIDX;
        private static MNTSSpatialIndex BoundaryObjectsSPIDX { get { return _BoundaryObjectsSPIDX; } }//边界打成断线+可忽略障碍物的spatialindex；
        private static MNTSSpatialIndex _BoundingBoxSpatialIndex;//建筑物块的外包框的spatialindex
        public static MNTSSpatialIndex BoundingBoxSpatialIndex { get { return _BoundingBoxSpatialIndex; } }//建筑物块的外包框的spatialindex

        private static MNTSSpatialIndex _BoundarySpatialIndex;// 所有边界，包含边界线，坡道，以及障碍物
        public static MNTSSpatialIndex BoundarySpatialIndex { get { return _BoundarySpatialIndex; } }// 所有边界，包含边界线，坡道，以及障碍物


        private static List<(double, double)> _LowerUpperBound;
        public static List<(double, double)> LowerUpperBound { get { return _LowerUpperBound; } } // 基因的上下边界，绝对值

        public static bool MultiThread = false;//是否使用进程内多线程
        public static void Init(DataWraper dataWraper)
        {
            _TotalArea = dataWraper.TotalArea;//总区域
            _InitSegLines = dataWraper.SegLines;//初始分割线
            _Buildings = dataWraper.Buildings;//所有障碍物，包含坡道

            _BoundingBoxes = dataWraper.BoundingBoxes;// 所有的建筑物的边框

            _Ramps = dataWraper.Ramps;//坡道

            _BuildingSpatialIndex = new MNTSSpatialIndex(dataWraper.Buildings);
            _BoundingBoxSpatialIndex = new MNTSSpatialIndex(dataWraper.BoundingBoxes);
            var boundaries = new List<Geometry> { dataWraper.TotalArea.Shell };
            boundaries.AddRange(dataWraper.Buildings);
            _BoundarySpatialIndex = new MNTSSpatialIndex(boundaries);
            _OuterBuildingIdxs = dataWraper.OuterBuildingIdxs;
            var ignorableBuildings = new List<Geometry>();
            foreach (int idx in OuterBuildingIdxs) ignorableBuildings.Add(Buildings[idx]);
            ignorableBuildings.AddRange(TotalArea.Shell.ToLineStrings().ToList());
            _BoundaryObjectsSPIDX = new MNTSSpatialIndex(ignorableBuildings);
            _SegLineIntsecList = dataWraper.SeglineIndexList;
            _SeglineConnectToBound = dataWraper.SeglineConnectToBound;
            _LowerUpperBound = dataWraper.LowerUpperBound;
        }
        public static bool IsValid(Chromosome chromosome)
        {
            var newSegLines = new List<LineSegment>();
            foreach (var gene in chromosome.Genome)
            {
                newSegLines.Add(gene.ToLineSegment());
            }
            newSegLines.ExtendAndIntSect(SeglineIndexList);//延展
            newSegLines.ExtendToBound(TotalArea, SeglineConnectToBound);
            newSegLines.SeglinePrecut(TotalArea);//预切割
            newSegLines.Clean();//过滤孤立的线
            if (!newSegLines.Allconnected()) return false;//判断是否全部相连
            //var vaildSeg = newSegLines.GetVaildSegLines(TotalArea);//获取有效分割线
            var vaildSeg = newSegLines.GetVaildLanes(TotalArea, BoundaryObjectsSPIDX);//获取有效车道线
            if (!vaildSeg.VaildLaneWidthSatisfied(BoundarySpatialIndex)) return false;//判断是否满足车道宽
            return true;
        }
        //返回长度为0则为不合理解
        public static List<SubArea> GetSubAreas(Chromosome chromosome)
        {
            var subAreas = new List<SubArea>();//分割出的子区域
            var newSegLines = new List<LineSegment>();
            foreach(var gene in chromosome.Genome)
            {
                newSegLines.Add(gene.ToLineSegment());
            }
            newSegLines.ExtendAndIntSect(SeglineIndexList);//延展
            newSegLines.ExtendToBound(TotalArea, SeglineConnectToBound);
            newSegLines.SeglinePrecut(TotalArea);//预切割
            // 这有个bug，影响subareakey

            newSegLines.Clean();//过滤孤立的线
            if (!newSegLines.Allconnected()) return subAreas;//判断是否全部相连
            //var vaildSeg = newSegLines.GetVaildSegLines(TotalArea);//获取有效分割线
            var SegLineStrings = newSegLines.ToLineStrings(false);

            var vaildSeg = newSegLines.GetVaildLanes(TotalArea, BoundaryObjectsSPIDX);//获取有效车道线
            if (!vaildSeg.VaildLaneWidthSatisfied(BoundarySpatialIndex)) return subAreas;//判断是否满足车道宽
            
            var areas = TotalArea.Shell.GetPolygons(SegLineStrings.Where(lstr => lstr!=null));//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            var vaildSegSpatialIndex = new MNTSSpatialIndex(vaildSeg.ToLineStrings().Cast<Geometry>().ToList());
            var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                //获取以下元素
                //Polygon area;//该区域的面域
                //LineString subBoundary;// 该区域的边界线
                // subSegLineStrings;//该区域全部分割线(linestring)
                //List<LineSegment> subSegLines;//该区域全部分割线(linesegment)
                //List<Polygon> subBuildings; //该区域全部建筑物,
                //List<Ramp> subRamps;//该区域全部的坡道
                //List<Polygon> subBoundingBoxes;//该区域所有建筑物的bounding box
                var area = areas[i];
                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                var subLaneLineStrings = vaildSegSpatialIndex.SelectCrossingGeometry(area).Cast<LineString>();// 分割线
                var subLanes = subLaneLineStrings.GetVaildParts(area);

                var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                Geometry geoWalls = area.Shell;
                foreach(var subSegLine in subSegLineStrings)
                {
                    if (subSegLine.PartInCommon(geoWalls))
                    {
                        geoWalls = geoWalls.Difference(subSegLine);
                    }
                }
                var walls = geoWalls.Get<LineString>();

                //subSegLineStrings = subSegLineStrings.Where(lstr => area.Shell.PartInCommon(lstr));//去除未构成边界的
                //var subSegLines = subSegLineStrings.ToLineSegments().Select(l => l.GetVaildPart(area)).ToList();

                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subRamps = Ramps.Where(ramp => area.Contains(ramp.InsertPt)).ToList();
                var subBoundingBoxes = BoundingBoxSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var key = GetSubAreaKey(area, chromosome, SegLineStrings);
                var subArea = new SubArea(area, subLanes,walls, subBuildings, subRamps, subBoundingBoxes, key);
                subAreas.Add(subArea);
            }
            return subAreas;
        }
        
        public static SubAreaKey GetSubAreaKey(Polygon area,Chromosome chromosome, List<LineString> SegLineStrings)
        {
            var GeneIdxs = new List<int>();
            var GeneVals = new List<double>();
            for(int idx = 0; idx < SegLineStrings.Count; idx++)
            {
                var SegLineString = SegLineStrings[idx];
                if(area.Shell.PartInCommon(SegLineString)) GeneIdxs.Add(idx);
            }
            var center = area.GetCenter();
            //var ValIncreaseDir = center.OnIncreaseDirectionOf( chromosome.Genome[GeneIdxs.First()].ToLineSegment());
            GeneIdxs.ForEach(idx => GeneVals.Add(chromosome.Genome[idx].Value));
            return new SubAreaKey(GeneIdxs, GeneVals, center);
        }
    }
}
