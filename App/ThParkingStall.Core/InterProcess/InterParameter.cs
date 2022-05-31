﻿using System;
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

        public static List<(int, int, int, int)> _SegLineIntSecNode;//四岔节点关系，上下左右的分割线index

        public static List<(int, int, int, int)> SegLineIntSecNode { get { return _SegLineIntSecNode; } }//四岔节点关系，上下左右的分割线index
        //private static List<HashSet<int>> NewIdxToOrg;//动态更新，合并后分割线对应到原始分割线index
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
            _SegLineIntSecNode = dataWraper.SegLineIntSecNode;
            //NewIdxToOrg = new List<HashSet<int>>();
            //for (int i = 0; i < InitSegLines.Count; i++)
            //{
            //    NewIdxToOrg.Add(new HashSet<int> { i });
            //}
        }
        //返回长度为0则为不合理解
        public static List<SubArea> GetSubAreas(Chromosome chromosome)
        {
            var subAreas = new List<SubArea>();
            var result_segLines = ProcessToSegLines(chromosome);
            var newSegLines = result_segLines.Item1;
            var vaildSeg = result_segLines.Item2;
            if(newSegLines == null) return subAreas;
            var SegLineStrings = newSegLines.ToLineStrings(false);
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
                var key = GetSubAreaKey(area, SegLineStrings); 
                var subArea = new SubArea(area, subLanes,walls, subBuildings, subRamps, subBoundingBoxes, key);
                subAreas.Add(subArea);
            }
            return subAreas;
        }
        public static (List<LineSegment>, List<LineSegment>) ProcessToSegLines(Chromosome chromosome)
        {
            var newSegLines = new List<LineSegment>();
            foreach (var gene in chromosome.Genome)
            {
                newSegLines.Add(gene.ToLineSegment());
            }
            newSegLines = MergeSegLines(newSegLines, out List<List<int>> seglineIndexListNew, out List<(bool, bool)> seglineToBoundNew);
            newSegLines.ExtendToBound(TotalArea, seglineToBoundNew);
            newSegLines.ExtendAndIntSect(seglineIndexListNew);//延展
            newSegLines.SeglinePrecut(TotalArea);//预切割
            newSegLines.Clean();//过滤孤立的线
            if (!newSegLines.Allconnected()) return (null,null);//判断是否全部相连
            //var vaildSeg = newSegLines.GetVaildSegLines(TotalArea);//获取有效分割线
            var vaildSeg = newSegLines.GetVaildLanes(TotalArea, BoundaryObjectsSPIDX);//获取有效车道线
            if (!vaildSeg.VaildLaneWidthSatisfied(BoundarySpatialIndex)) return (null, null);//判断是否满足车道宽
            return (newSegLines, vaildSeg);
        }
        private static List<LineSegment> MergeSegLines(List<LineSegment> SegLines,out List<List<int>> seglineIndexListNew,out List<(bool, bool)> seglineToBoundNew)
        {
            var horzPrior = SegLines.HorzProir();
            var NewIdxToOrg = new List<HashSet<int>>();
            for (int i = 0; i < SegLineIntSecNode.Count; i++)
            {
                var node = SegLineIntSecNode[i];
                int id1;
                int id2;
                if(horzPrior[i])
                {
                    id1 = node.Item3;
                    id2 = node.Item4;
                }
                else
                {
                    id1 = node.Item1;
                    id2 = node.Item2;
                }
                //bool founded = false;
                var partEqual =new List<int>();
                for(int j = 0; j < NewIdxToOrg.Count; j++)
                {
                    var ids = NewIdxToOrg[j];
                    if (ids.Contains(id1) || ids.Contains(id2)) partEqual.Add(j);
                }
                var newSet = new HashSet<int> { id1, id2 };
                partEqual.ForEach(j => newSet.UnionWith(NewIdxToOrg[j]));
                partEqual.Reverse();
                partEqual.ForEach(j => NewIdxToOrg.RemoveAt(j));
                NewIdxToOrg.Add(newSet);
            }
            var newSegLines = new List<LineSegment>();
            var addedIdxs = new HashSet<int>();
            seglineIndexListNew = new List<List<int>>();
            seglineToBoundNew = new List<(bool, bool)>();
            foreach (var idxs in NewIdxToOrg)//合并需要合并的
            {
                var lineToMerge = new List<LineSegment>();
                foreach (var idx in idxs)
                {
                    lineToMerge.Add(SegLines[idx]);
                    addedIdxs.Add(idx);
                }  
                newSegLines.Add(lineToMerge.MergeLinesToMid());
            }
            for (int i = 0; i < SegLines.Count; i++)//添加剩余的
            {
                if (!addedIdxs.Contains(i))
                {
                    newSegLines.Add(SegLines[i]);
                    NewIdxToOrg.Add(new HashSet<int> {i});
                }
            }
            var OrgIdxToNew = new Dictionary<int, int>();
            for (int i = 0; i < NewIdxToOrg.Count; i++)
            {
                var orgIdxs = NewIdxToOrg[i];
                foreach (var orgIdx in orgIdxs)
                {
                    OrgIdxToNew.Add(orgIdx, i);
                }
            }
            for (int i = 0; i < NewIdxToOrg.Count; i++)
            {
                var orgIdxs = NewIdxToOrg[i];
                bool negConnect = SeglineConnectToBound.Slice(orgIdxs).Any(o => o.Item1);
                bool posConnect = SeglineConnectToBound.Slice(orgIdxs).Any(o => o.Item2);
                seglineToBoundNew.Add((negConnect,posConnect));

                var connectToOrg = SeglineIndexList.Slice(orgIdxs);
                var connectToNewIdxs = new HashSet<int>();
                //旧id =>新id
                connectToOrg.ForEach(orgids => orgids.ForEach(orgid => connectToNewIdxs.Add(OrgIdxToNew[orgid])));
                seglineIndexListNew.Add(connectToNewIdxs.ToList());

            }

            return newSegLines;
        }
        public static List<bool> HorzProir(this List<LineSegment> SegLines)
        {
            var horzPrior = new List<bool>();
            foreach (var node in SegLineIntSecNode)
            {
                var top = SegLines[node.Item1];
                var bottom = SegLines[node.Item2];
                var left = SegLines[node.Item3];
                var right = SegLines[node.Item4];
                var distVert = Math.Abs(top.GetValue() - bottom.GetValue());
                var distHorz = Math.Abs(left.GetValue() - right.GetValue());
                if (distVert <= distHorz) horzPrior.Add(false);
                else horzPrior.Add(true);
            }
            return horzPrior;
        }
        public static SubAreaKey GetSubAreaKey(Polygon area, List<LineString> SegLineStrings)
        {
            var centers = new List<Point>();
            for(int idx = 0; idx < SegLineStrings.Count; idx++)
            {
                var SegLineString = SegLineStrings[idx];
                if (SegLineString == null) continue;
                var intSection = area.Shell.Intersection(SegLineString);
                if (intSection.Length > 0)
                {
                    centers.Add(intSection.Centroid);
                }
            }
            var center = area.GetCenter();
            return new SubAreaKey(centers, center);
        }
    }
}
