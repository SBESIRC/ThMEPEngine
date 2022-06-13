using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;
using ThMEPArchitecture.ViewModel;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPArchitecture.MultiProcess;
namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public static class InterParameterEx
    {
        #region 空区域合并
        public static List<LineSegment> CombineEmptyToNoneEmptyArea(this List<LineSegment> segLines,bool BoundEmptyAreaToMid,bool HorizontalFirst)
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
                        if (!(SegLine.IsVertical()^ HorizontalFirst))//纵线
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
                        if (!(!SegLine.IsVertical() ^ HorizontalFirst))//横线
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
            return orgSegLines.Difference(PartToRemove).Get<LineString>().ToLineSegments().MergeLineWithSharedPts();
        }
        public static List<LineSegment> CombineEmptyToEmptyArea(this List<LineSegment> segLines, bool BoundEmptyAreaToMid, bool HorizontalFirst, out int AreaCnt)
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
                    if (!(SegLine.IsVertical() ^ HorizontalFirst))//纵线
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
                    if (!(!SegLine.IsVertical() ^ HorizontalFirst))//横线
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
            return orgSegLines.Difference(PartToRemove).Get<LineString>().ToLineSegments().MergeLineWithSharedPts();
        }

        public static List<LineSegment> GridLinesRemoveEmptyAreas(this List<LineSegment> SegLines, bool HorizontalFirst)
        {
            var segLines = SegLines.Select(l => l.Clone()).ToList();
            int subAreaCnt_pre = -1;
            int subAreaCnt_new = 0;
            while(subAreaCnt_pre!= subAreaCnt_new)
            {
                segLines = CombineEmptyToNoneEmptyArea(segLines, false, HorizontalFirst);
                subAreaCnt_pre = subAreaCnt_new;
                segLines = CombineEmptyToEmptyArea(segLines, false, HorizontalFirst, out subAreaCnt_new);
            }
            segLines = CombineEmptyToNoneEmptyArea(segLines, true, HorizontalFirst);
            segLines = CombineEmptyToEmptyArea(segLines, true, HorizontalFirst, out subAreaCnt_new);
            var grouped = segLines.GroupSegLines().OrderBy(g => g.Count).Last();
            grouped.CleanLineWithOneIntSecPt(InterParameter.TotalArea);
            return grouped;
        }
        public static List<LineSegment> DefineSegLinePriority(this List<LineSegment> segLines)
        {
            var newSegLines = segLines.Select(l => l.Clone()).ToList();
            var SegLineStrings = newSegLines.ToLineStrings();
            var subAreas = SegLineStrings.GetSubAreas();
            var subAreaSPIdx = new MNTSSpatialIndex(subAreas.Select(s => s.Area));
            var IntSecPts = segLines.GetAllIntSecPs();
            //找到被4个非空区域公用的pt
            var PtToDefine = new List<Point>();
            foreach(var pt in IntSecPts)
            {
                var neighbors = subAreaSPIdx.SelectCrossingGeometry(pt.Buffer(1));
                if(neighbors.Count == 4)
                {
                    PtToDefine.Add(pt);
                }
            }
            var PtAndDir = new List<(Point, bool)>();
            foreach(var pt in PtToDefine)
            {
                var rst = pt.HorizontalAsPrior(subAreas);
                bool HorzPrior;
                if(rst != -1)
                {
                    HorzPrior = rst == 1;
                    PtAndDir.Add((pt, HorzPrior));
                }
            }
            return newSegLines.BreakBaseOnPriority(PtAndDir);
        }
        #endregion
        #region 定义拉通关系
        private static List<LineSegment> BreakBaseOnPriority(this List<LineSegment> SegLines, List<(Point, bool)> PtAndDirs)
        {
            var segLines = SegLines.Select(l => l.Clone()).ToList();
            foreach(var ptAndDir in PtAndDirs)
            {
                var pt = ptAndDir.Item1;
                var HorzPrior = ptAndDir.Item2;
                var queryed = segLines.Where(l => l.Distance(pt.Coordinate) < 1).ToList();
                foreach (var l in queryed)
                {
                    if (!(l.IsVertical()^ HorzPrior))
                    {
                        var splitted = l.Split(pt.Coordinate);
                        segLines.Add(splitted.Item1);
                        segLines.Add(splitted.Item2);
                    }
                }
                queryed.ForEach(l => { if (!(l.IsVertical() ^ HorzPrior)) segLines.Remove(l); });
            }
            return segLines;
        }
        private static int HorizontalAsPrior(this Point point,List<SubArea> subAreas)
        {
            var neighbors = new List<SubArea>();
            foreach(SubArea subArea in subAreas)
            {
                if (point.Distance(subArea.Area) < 1)
                {
                    neighbors.Add(subArea);
                }
            }
            if (neighbors.Count != 4) return -1;//不合理
            SubArea TopLeft = null;
            SubArea TopRight = null;
            SubArea BottomLeft = null;
            SubArea BottomRight = null;
            foreach (SubArea subArea in neighbors)
            {
                var areaCenter = subArea.Area.Centroid;
                if (areaCenter.X <= point.X && areaCenter.Y > point.Y) { TopLeft = subArea; }
                else if (areaCenter.X > point.X && areaCenter.Y >= point.Y) { TopRight = subArea; }
                else if (areaCenter.X <= point.X && areaCenter.Y < point.Y) { BottomLeft = subArea; }
                else { BottomRight = subArea; }
            }
            ;
            if (TopLeft.IsAlignedWith(TopRight, false)) return 1;
            if (BottomLeft.IsAlignedWith(BottomRight, true)) return 1;
            return 0;
        }
        #endregion
        private static bool IsAlignedWith(this SubArea subArea1,SubArea subArea2,bool CompareTop)
        {
            double difference;
            double tol = ParameterStock.VerticalSpotWidth / 2;
            double mod = ParameterStock.RoadWidth + ParameterStock.VerticalSpotLength * 2;
            if (subArea1.Buildings.Count == 0 || subArea2.Buildings.Count == 0) return true;

            var bottom1 = subArea1.Buildings.GetBound(CompareTop, true);
            var bottom2 = subArea2.Buildings.GetBound(CompareTop, true);
            difference = Math.Abs(bottom1 - bottom2);
            var remainer = difference % mod;
            if (remainer < tol) return true;
            else return false;
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
        #region 补充分割线
        public static List<LineSegment> AddSegLines()
        {
            var SegLineStrings = InterParameter.InitSegLines.Select(l => l.Clone()).ToList().ToLineStrings();
            var result = new List<LineSegment>();
            for (int i = 0; i < 3; i++)
            {
                var newSegLines = GetSegLinesOnce(SegLineStrings);
                if (newSegLines.Count == 0) break;
                SegLineStrings.AddRange(newSegLines);
                result.AddRange(newSegLines.ToLineSegments());
            }
            return result;
        }
        private static List<LineString> GetSegLinesOnce(this List<LineString> SegLineStrings)
        {
            var newSegs = new List<LineString>();
            var subAreas = SegLineStrings.GetSubAreas();
            foreach (var area in subAreas)
            {
                var newSeg = area.GetNewSegLine(SegLineStrings);
                if(newSeg != null) newSegs.Add(newSeg);
            }
            return newSegs;
        }
        public static LineString GetNewSegLine(this SubArea subArea, List<LineString> SegLineStrings)
        {
            LineString newSeg = null;
            if(subArea.Buildings.Count == 0) return newSeg;
            if(subArea.Walls.Count == 0) return newSeg;
            var BoundingBox = subArea.Buildings.Cast<Geometry>().GetEnvelope();
            var segLinesGeo = new MultiLineString(subArea.SegLines.ToArray()) ;
            var segLineSPIndex = new MNTSSpatialIndex(subArea.SegLines);
            var maxLength = ((Polygon) subArea.Area.Envelope).Shell.ToLineSegments().Max(l=>l.Length);
            var edges = BoundingBox.Shell.ToLineSegments();
            var wallLine = InterParameter.TotalArea;
            var Center = BoundingBox.Centroid;
            double w1 = VMStock.RoadWidth + VMStock.VerticalSpotLength + VMStock.D2;
            double w2 = VMStock.RoadWidth + 2*( VMStock.VerticalSpotLength + VMStock.D2);
            double h1 = 0.5 * VMStock.RoadWidth + (VMStock.ColumnSizeOfParalleToRoad + 2* VMStock.ColumnAdditionalSize) *2 + VMStock.VerticalSpotWidth * 5 + VMStock.D1;
            double h2 = h1 - VMStock.VerticalSpotWidth;
            foreach (var edge in edges)
            {
                //edge.ToDbLine().AddToCurrentSpace();
                var extended = edge.ExtendToBound(subArea.Area, (true, true));
                //extended.ToDbLine().AddToCurrentSpace();
                var IsVerticle = extended.IsVertical();
                var extendFlags = (3, 2);
                if(!IsVerticle) extendFlags = (0,1);
                var pts = (new List<Coordinate> { extended.P0,extended.P1}).Where(pt => segLineSPIndex.SelectCrossingGeometry(pt.ToPoint().Buffer(1)).Count >0);
                foreach(var pt in pts)
                {
                    var Buffer = extended.GetHalfBuffer(maxLength,true);
                    var extendFlag = extendFlags.Item1;
                    bool positiveDir = true;
                    if (Buffer.Contains(Center))
                    {
                        Buffer = extended.GetHalfBuffer(maxLength, false);
                        extendFlag = extendFlags.Item2;
                        positiveDir = false;
                    }
                    bool NeedExtraSegLine = segLineSPIndex.SelectCrossingGeometry(Buffer).
                        Cast<LineString>().Where(l => l.IsVertical() == IsVerticle).Count() == 0;
                    if (NeedExtraSegLine)//需要补线
                    {
                        var baseLine = new LineSegment(pt, pt.Move(w1, extendFlag));
                        var rect = baseLine.GetHalfBuffer(h1+1, true).Buffer(-1);
                        var rectVaild = true;
                        if (!rect.Within(subArea.Area))
                        {
                            rect = baseLine.GetHalfBuffer(h1 + 1, false).Buffer(-1);
                            rectVaild = rect.Within(subArea.Area);
                        }
                        if (rectVaild)
                        {
                            if (positiveDir) extended =  extended.Move(0.5 * (1+VMStock.RoadWidth));
                            else extended = extended.Move(-0.5 * (1 + VMStock.RoadWidth));
                            var newSegLine = extended.ExtendToBound(subArea.Area, (true, true));
                            var newSegLines = SegLineStrings.ToLineSegments();
                            newSegLines.Add(newSegLine);
                            var IsVaild = SegLineIsVaild(newSegLines,newSegLines.Count-1);
                            if(IsVaild) return newSegLine.ToLineString();
                        }

                        baseLine = new LineSegment(pt, pt.Move(w2, extendFlag));
                        rect = baseLine.GetHalfBuffer(h2 + 1, true).Buffer(-1);
                        rectVaild = true;
                        if (!rect.Within(subArea.Area))
                        {
                            rect = baseLine.GetHalfBuffer(h2 + 1, false).Buffer(-1);
                            rectVaild = rect.Within(subArea.Area);
                        }
                        if (rectVaild)
                        {
                            if (positiveDir) extended = extended.Move(0.5 * (1 + VMStock.RoadWidth));
                            else extended = extended.Move(-0.5 * (1 + VMStock.RoadWidth));
                            var newSegLine = extended.ExtendToBound(subArea.Area, (true, true));
                            var newSegLines = SegLineStrings.ToLineSegments();
                            newSegLines.Add(newSegLine);
                            var IsVaild = SegLineIsVaild(newSegLines, newSegLines.Count - 1);
                            if (IsVaild) return newSegLine.ToLineString();
                        }
                    }
                    
                }
            }
            return newSeg;
        }

        public static bool SegLineIsVaild(List<LineSegment> SegLines,int idx)
        {
            var validLane = SegLineEx.GetVaildLane(idx, SegLines, InterParameter.TotalArea, InterParameter.BoundaryObjectsSPIDX);
            double tol = VMStock.RoadWidth - 0.1;// 5500 -0.1
            if (validLane == null) return true;
            var rect = validLane.GetRect(tol);
            var rst = InterParameter.BoundarySpatialIndex.SelectCrossingGeometry(rect);
            return rst.Count == 0;
        }
        #endregion
    }
}
