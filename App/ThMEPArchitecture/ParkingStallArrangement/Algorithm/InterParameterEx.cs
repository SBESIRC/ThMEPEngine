using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public static class InterParameterEx
    {
        public static List<LineSegment> CombineEmptyToNoneEmptyArea(this List<LineSegment> segLines,bool BoundEmptyAreaToMid)
        {
            var newSegLines = segLines.Select(l => l.Clone()).ToList();
            var SegLineStrings = newSegLines.ToLineStrings();
            var subAreas = SegLineStrings.GetSubAreas();

            var segLineToRemove = new HashSet<LineString>();
            var occupiedDic = new Dictionary<int,bool>();//int代表序号，bool代表纵向合并过还是横向
            var AreaToCombineIdx = new List<int>();
            var RestAreaIdx = new List<int>();
            for (int i = 0; i < subAreas.Count; i++)
            {
                var subArea = subAreas[i];
                if (subArea.Buildings.Count == 0) AreaToCombineIdx.Add(i);
                else RestAreaIdx.Add(i);
            }
            bool NewToFind = true;
            while (NewToFind)//空区合并到非空
            {
                NewToFind = false;
                for(int n = AreaToCombineIdx.Count-1;n >= 0;n--)
                {
                    var i = AreaToCombineIdx[n];
                    var subArea = subAreas[i];
                    var HasBoundary = subArea.Walls.Count != 0;
                    var wallCoors = new List<Coordinate>();
                    if (HasBoundary)
                    {
                        subArea.Walls.ForEach(wall => wallCoors.AddRange(wall.Coordinates));
                    }
                    //找到周围的区域
                    var neighbors = new List<(int, int)>();

                    if (subArea.SegLines.Any(l => segLineToRemove.Contains(l))) continue;

                    for (int k = 0; k < subArea.SegLines.Count; k++)
                    {
                        var segLine = subArea.SegLines[k];
                        bool HasBoundaryAndMiddleLine = HasBoundary && (segLine.Distance(InterParameter.TotalArea.Shell) > 1);
                        if (!BoundEmptyAreaToMid && HasBoundaryAndMiddleLine) continue;
                        foreach(int j in RestAreaIdx)
                        {
                            if (i == j) continue;
                            var othersubArea = subAreas[j];
                            foreach (var segLine2 in othersubArea.SegLines)
                            {
                                if (segLine.EqualsTopologically(segLine2))
                                {
                                    if (BoundEmptyAreaToMid && HasBoundaryAndMiddleLine && othersubArea.Walls.Count != 0) continue;
                                    neighbors.Add((k, j));
                                }
                            }
                        }
                    }
                    bool founded = false;
                    //先找横向合并的
                    foreach (var neighbor in neighbors)
                    {
                        if (founded) break;
                        var SegIndex = neighbor.Item1;
                        var SegLine = subArea.SegLines[SegIndex];
                        var NeighborIdx = neighbor.Item2;
                        if (SegLine.IsVertical())//纵线
                        {
                            if (occupiedDic.ContainsKey(NeighborIdx))
                            {
                                if (occupiedDic[NeighborIdx] == true)//可以合并
                                {
                                    occupiedDic.Add(i, true);
                                    segLineToRemove.Add(SegLine);
                                    founded = true;
                                }
                            }
                            else
                            {
                                occupiedDic.Add(NeighborIdx, true);
                                occupiedDic.Add(i, true);
                                segLineToRemove.Add(SegLine);
                                founded = true;
                            }
                        }
                    }
                    //再找纵向
                    foreach (var neighbor in neighbors)
                    {
                        if (founded) break;
                        var SegIndex = neighbor.Item1;
                        var SegLine = subArea.SegLines[SegIndex];
                        var NeighborIdx = neighbor.Item2;
                        if (!SegLine.IsVertical())//横线
                        {
                            if (occupiedDic.ContainsKey(NeighborIdx))
                            {
                                if (occupiedDic[NeighborIdx] == false)//可以合并
                                {
                                    segLineToRemove.Add(SegLine);
                                    occupiedDic.Add(i, false);
                                    founded = true;
                                }
                            }
                            else
                            {
                                occupiedDic.Add(NeighborIdx, false);
                                occupiedDic.Add(i, false);
                                segLineToRemove.Add(SegLine);
                                founded = true;
                            }
                        }
                    }
                    if(founded)
                    {
                        AreaToCombineIdx.RemoveAt(n);
                        RestAreaIdx.Add(i);
                        NewToFind = true;
                    }  
                }
                if (BoundEmptyAreaToMid) break;
            }
            var orgSegLines = new MultiLineString(SegLineStrings.Where(lstr => lstr != null).ToArray());
            var PartToRemove = new MultiLineString(segLineToRemove.ToArray());
            return orgSegLines.Difference(PartToRemove).Get<LineString>().ToLineSegments().Merge();
        }
        public static List<LineSegment> CombineEmptyToEmptyArea(this List<LineSegment> segLines, bool BoundEmptyAreaToMid, out int AreaCnt)
        {
            var newSegLines = segLines.Select(l => l.Clone()).ToList();
            var SegLineStrings = newSegLines.ToLineStrings();
            var subAreas = SegLineStrings.GetSubAreas();
            AreaCnt = subAreas.Count;
            var segLineToRemove = new HashSet<LineString>();
            var occupiedDic = new Dictionary<int, bool>();//int代表序号，bool代表纵向合并过还是横向
            var AreaToCombineIdx = new List<int>();
            //var RestAreaIdx = new List<int>();
            for (int i = 0; i < subAreas.Count; i++)
            {
                var subArea = subAreas[i];
                if (subArea.Buildings.Count == 0) AreaToCombineIdx.Add(i);
                //else RestAreaIdx.Add(i);
            }
            for (int n = AreaToCombineIdx.Count - 1; n >= 0; n--)
            {
                var i = AreaToCombineIdx[n];
                var subArea = subAreas[i];
                var HasBoundary = subArea.Walls.Count != 0;
                var wallCoors = new List<Coordinate>();
                if (HasBoundary)
                {
                    subArea.Walls.ForEach(wall => wallCoors.AddRange(wall.Coordinates));
                }
                //找到周围的区域
                var neighbors = new List<(int, int)>();

                if (subArea.SegLines.Any(l => segLineToRemove.Contains(l))) continue;

                for (int k = 0; k < subArea.SegLines.Count; k++)
                {
                    var segLine = subArea.SegLines[k];
                    bool HasBoundaryAndMiddleLine = HasBoundary && (segLine.Distance(InterParameter.TotalArea.Shell) > 1);
                    if (!BoundEmptyAreaToMid && HasBoundaryAndMiddleLine) continue;
                    foreach (int j in AreaToCombineIdx)
                    {
                        if (i == j) continue;
                        var othersubArea = subAreas[j];
                        foreach (var segLine2 in othersubArea.SegLines)
                        {
                            if (segLine.EqualsTopologically(segLine2))
                            {
                                if (BoundEmptyAreaToMid && HasBoundaryAndMiddleLine && othersubArea.Walls.Count != 0) continue;
                                neighbors.Add((k, j));
                            }
                        }
                    }
                }
                bool founded = false;
                //先找横向合并的
                foreach (var neighbor in neighbors)
                {
                    if (founded) break;
                    var SegIndex = neighbor.Item1;
                    var SegLine = subArea.SegLines[SegIndex];
                    var NeighborIdx = neighbor.Item2;
                    if (SegLine.IsVertical())//纵线
                    {
                        if (occupiedDic.ContainsKey(NeighborIdx))
                        {
                            if (occupiedDic[NeighborIdx] == true)//可以合并
                            {
                                occupiedDic.Add(i, true);
                                segLineToRemove.Add(SegLine);
                                founded = true;
                            }
                        }
                        else
                        {
                            occupiedDic.Add(NeighborIdx, true);
                            occupiedDic.Add(i, true);
                            segLineToRemove.Add(SegLine);
                            founded = true;
                        }
                    }
                }
                //再找纵向
                foreach (var neighbor in neighbors)
                {
                    if (founded) break;
                    var SegIndex = neighbor.Item1;
                    var SegLine = subArea.SegLines[SegIndex];
                    var NeighborIdx = neighbor.Item2;
                    if (!SegLine.IsVertical())//横线
                    {
                        if (occupiedDic.ContainsKey(NeighborIdx))
                        {
                            if (occupiedDic[NeighborIdx] == false)//可以合并
                            {
                                segLineToRemove.Add(SegLine);
                                occupiedDic.Add(i, false);
                                founded = true;
                            }
                        }
                        else
                        {
                            occupiedDic.Add(NeighborIdx, false);
                            occupiedDic.Add(i, false);
                            segLineToRemove.Add(SegLine);
                            founded = true;
                        }
                    }
                }


                if (BoundEmptyAreaToMid) break;
            }
            var orgSegLines = new MultiLineString(SegLineStrings.Where(lstr => lstr != null).ToArray());
            var PartToRemove = new MultiLineString(segLineToRemove.ToArray());
            return orgSegLines.Difference(PartToRemove).Get<LineString>().ToLineSegments().Merge();
        }

        public static List<LineSegment> GridLinesRemoveEmptyAreas(this List<LineSegment> SegLines)
        {
            var segLines = SegLines.Select(l => l.Clone()).ToList();
            int subAreaCnt_pre = -1;
            int subAreaCnt_new = 0;
            while(subAreaCnt_pre!= subAreaCnt_new)
            {
                segLines = CombineEmptyToNoneEmptyArea(segLines, false);
                subAreaCnt_pre = subAreaCnt_new;
                segLines = CombineEmptyToEmptyArea(segLines, false,out subAreaCnt_new);
            }
            segLines = CombineEmptyToNoneEmptyArea(segLines, true);
            segLines = CombineEmptyToEmptyArea(segLines, true, out subAreaCnt_new);
            var grouped = segLines.GroupSegLines().OrderBy(g => g.Count).Last();
            grouped.CleanLineWithOneIntSecPt(InterParameter.TotalArea);
            return grouped;
        }
        public static List<SubArea> GetSubAreas(this List<LineString> SegLineStrings)
        {
            var subAreas = new List<SubArea>();//分割出的子区域
            var areas = InterParameter.TotalArea.Shell.GetPolygons(SegLineStrings.Where(lstr => lstr != null));//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                var subSegLines = subSegLineStrings.GetVaildLstrs(area);
                Geometry geoWalls = area.Shell;
                foreach (var subSegLine in subSegLineStrings)
                {
                    if (subSegLine.PartInCommon(geoWalls))
                    {
                        geoWalls = geoWalls.Difference(subSegLine);
                    }
                }
                var walls = geoWalls.Get<LineString>();
                var subBuildings = InterParameter.BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subRamps = InterParameter.Ramps.Where(ramp => area.Contains(ramp.InsertPt)).ToList();
                var subBoundingBoxes = InterParameter.BoundingBoxSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subArea = new SubArea(area, subSegLines, walls, subBuildings, subRamps, subBoundingBoxes);
                subAreas.Add(subArea);
            }
            return subAreas;
        }
    }
}
