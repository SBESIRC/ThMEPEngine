using NetTopologySuite.Geometries;
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

        public static List<LineSegment> _SegLines;

        private static List<Polygon> _Buildings;// 所有障碍物，包含坡道
        private static List<Polygon> Buildings { get { return _Buildings; } }// 所有障碍物，包含坡道

        public static List<ORamp> _Ramps;// 坡道
        public static List<ORamp> Ramps { get{return _Ramps;} }// 坡道

        public static Polygon _BaseLineBoundary;
        public static Polygon BaseLineBoundary { get { return _BaseLineBoundary; } } //基线边界（包含内部孔），基线边界内的分割线的部分用来求基线

        private static MNTSSpatialIndex _BuildingSpatialIndex;//所有障碍物，包含坡道的spatialindex
        public static MNTSSpatialIndex BuildingSpatialIndex { get { return _BuildingSpatialIndex; } }//所有障碍物，包含坡道的spatialindex


        private static MNTSSpatialIndex _BoundarySpatialIndex;//边界打成断线 + 障碍物 + 坡道的spatialindex(所有边界）
        public static MNTSSpatialIndex BoundarySpatialIndex { get { return _BoundarySpatialIndex; } }//边界打成断线 + 障碍物 + 坡道的spatialindex(所有边界）

        public static List<(List<int>, List<int>)> SeglineIndex;//分区线（起始终止点连接关系），数量为0则连到边界，其余为其他分区线的index
        public static void Init(DataWraper dataWraper)
        {
            _TotalArea = dataWraper.TotalArea;//总区域
            _SegLines = dataWraper.SegLines;//初始分区线
            _Buildings = dataWraper.Buildings;//所有障碍物，包含坡道
            _BuildingSpatialIndex = new MNTSSpatialIndex(dataWraper.Buildings);

            var allObjs = TotalArea.Shell.ToLineStrings().Cast<Geometry>().ToList();
            allObjs.AddRange(Buildings);
            _BoundarySpatialIndex = new MNTSSpatialIndex(allObjs);
        }
        public static void Init(Polygon totalArea,List<SegLine> segLines,List<Polygon> buildings,List<ORamp> ramps,Polygon baseLineBoundary, List<(List<int>, List<int>)> seglineIndex)
        {
            _TotalArea = totalArea;
            _InitSegLines = segLines;
            _Buildings = buildings;
            _BuildingSpatialIndex = new MNTSSpatialIndex(buildings);
            _Ramps = ramps;
            _BaseLineBoundary = baseLineBoundary;
            SeglineIndex = seglineIndex;
            var allObjs = TotalArea.Shell.ToLineStrings().Cast<Geometry>().ToList();
            allObjs.AddRange(Buildings);
            _BoundarySpatialIndex = new MNTSSpatialIndex(allObjs);
        }
        //返回长度为0则为不合理解
        public static List<OSubArea> GetSubAreas()
        {
            var subAreas = new List<OSubArea>();
            var SegLineStrings = _SegLines.ToLineStrings();
            var areas = TotalArea.Shell.GetPolygons(SegLineStrings);//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            //var vaildSegSpatialIndex = new MNTSSpatialIndex(SegLineStrings.Cast<Geometry>().ToList());
            //var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                //var subLaneLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();// 分区线
                //var subLanes = subLaneLineStrings.GetVaildParts(area);

                var subLanes = SegLineStrings.GetCommonParts(area.Shell);
                //var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                var walls = SegLineStrings.GetWalls(area.Shell);
                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subArea = new OSubArea(area,subLanes,walls,subBuildings);
                subAreas.Add(subArea);
            }
            return subAreas;
        }

        public static List<OSubArea> GetOSubAreas(List<double> gene = null)
        {
            var subAreas = new List<OSubArea>();
            var newSegs = ProcessToSegLines(gene);

            var SegLineStrings = newSegs.Select(l =>l.Splitter).ToList().ToLineStrings();
            var vaildLanes = newSegs.Select(l => l.VaildLane).ToList().ToLineStrings();
            var areas = TotalArea.Shell.GetPolygons(SegLineStrings);//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            //var vaildSegSpatialIndex = new MNTSSpatialIndex(SegLineStrings.Cast<Geometry>().ToList());
            //var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                var subLanes = vaildLanes.GetCommonParts(area.Shell);
                //var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                var walls = SegLineStrings.GetWalls(area.Shell);
                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subRamps = Ramps.Where(r => area.Contains(r.InsertPt)).ToList();
                var subArea = new OSubArea(area, subLanes, walls, subBuildings,subRamps);
                subAreas.Add(subArea);
            }
            return subAreas;
        }
        //输出的分区线数量一致，需要求最大全连接组
        public static List<SegLine> ProcessToSegLines(List<double> gene)
        {
            var newSegLines = new List<SegLine>();
            if (gene == null)
            {
                newSegLines = InitSegLines.Select(segLine => segLine.Clone()).ToList();
            }
            else
            {
                for (int i = 0; i < InitSegLines.Count; i++)
                {
                    newSegLines.Add(InitSegLines[i].GetMovedLine(gene[i]));
                }
            }
            newSegLines.UpdateSegLines(SeglineIndex, TotalArea, BoundarySpatialIndex,BaseLineBoundary);

            newSegLines = newSegLines.Where(l => l.VaildLane != null).ToList();
            //获取最大全连接组,存在其他组标记 + 报错
            var groups = newSegLines.GroupSegLines().OrderBy(g => g.Count).ToList();
            newSegLines = newSegLines.Slice(groups.Last());
            return newSegLines;
        }
    }
}
