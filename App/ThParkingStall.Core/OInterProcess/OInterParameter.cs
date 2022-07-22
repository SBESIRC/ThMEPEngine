﻿using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;
using ThParkingStall.Core.OTools;
namespace ThParkingStall.Core.OInterProcess
{
    public static class OInterParameter
    {
        private static Polygon _TotalArea;//总区域，边界为外包框
        public static Polygon TotalArea { get { return _TotalArea; } }//总区域，边界为外包框

        private static List<SegLine> _InitSegLines;//所有初始分区线
        public static List<SegLine> InitSegLines { get { return _InitSegLines; } }//所有初始分区线

        private static List<Polygon> _Buildings;// 所有障碍物，包含坡道
        private static List<Polygon> Buildings { get { return _Buildings; } }// 所有障碍物，包含坡道

        private static MNTSSpatialIndex _BuildingSpatialIndex;//所有障碍物，包含坡道的spatialindex
        public static MNTSSpatialIndex BuildingSpatialIndex { get { return _BuildingSpatialIndex; } }//所有障碍物，包含坡道的spatialindex

        public static List<(List<int>, List<int>)> SeglineIndex;//分区线（起始终止点连接关系），数量为0则连到边界，其余为其他分区线的index
        public static void Init(DataWraper dataWraper)
        {
            _TotalArea = dataWraper.TotalArea;//总区域
            //_InitSegLines = dataWraper.SegLines;//初始分区线
            _Buildings = dataWraper.Buildings;//所有障碍物，包含坡道
            _BuildingSpatialIndex = new MNTSSpatialIndex(dataWraper.Buildings);
        }
        //返回长度为0则为不合理解
        public static List<OSubArea> GetSubAreas()
        {
            var subAreas = new List<OSubArea>();
            var SegLineStrings = InitSegLines.Select(seg =>seg.Splitter).ToList().ToLineStrings();
            var areas = TotalArea.Shell.GetPolygons(SegLineStrings);//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            //var vaildSegSpatialIndex = new MNTSSpatialIndex(SegLineStrings.Cast<Geometry>().ToList());
            var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                var subLaneLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();// 分区线
                var subLanes = subLaneLineStrings.GetVaildParts(area);

                //var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                Geometry geoWalls = area.Shell;
                foreach (var subSegLine in subLaneLineStrings)
                {
                    if (subSegLine.PartInCommon(geoWalls))
                    {
                        geoWalls = OverlayNGRobust.Overlay(geoWalls, subSegLine, SpatialFunction.Difference);
                    }
                }
                var walls = geoWalls.Get<LineString>();

                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subArea = new OSubArea(area,subLanes,walls,subBuildings);
                subAreas.Add(subArea);
            }
            return subAreas;
        }

        //输出的分区线数量一致
        //public static List<LineSegment> ProcessToSegLines(List<double> gene)
        //{
        //    var movedLines = new List<LineSegment>();
        //    for (int i = 0; i < InitSegLines.Count; i++)
        //    {
        //        movedLines.Add(InitSegLines[i].GetMovedLine(gene[i]));
        //    }
        //    var segLines = new List<LineSegment>();
        //    for (int i = 0; i < InitSegLines.Count; i++)
        //    {
        //        segLines.Add(movedLines.RebuildLine(i, SeglineIndex, TotalArea.Shell));
        //    }
        //    return segLines;
        //}
    }
}
