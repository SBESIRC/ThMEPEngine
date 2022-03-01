using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Engine
{
    public class ThFirstSecondRecognitionEngine:IDisposable
    {
        private Tolerance AccuracyTolerance; // 精确判断精度
        private Tolerance ApproximateTolerance; // 粗略判断精度
        private double TTypeAngleTolerance = 10.0; // T型连接时，分支与主线的的夹角和90度相减的容差
        private double ShortLinkLineLength = 100.0;
        
        /// <summary>
        /// 记录边线是1号线，还是2号线
        /// </summary>
        private Dictionary<Line, EdgePattern> SideLineNumberDict { get; set; }
        /// <summary>
        /// 记录中心线是否被用了
        /// </summary>
        private Dictionary<Line, bool> CenterLineUsedRecordDict { get; set; }
        /// <summary>
        /// 记录中心线两边的线
        /// Line.LineDirection().GetPerpendicularVector()->Value's.Item1
        /// Line.LineDirection().GetPerpendicularVector().Negate()->Value's.Item2
        /// </summary>
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDict { get; private set; }
        private ThCADCoreNTSSpatialIndex CenterSpatialIndex { get; set; }
        public List<Line> FirstLines
        {
            get
            {
                return SideLineNumberDict.Where(o => o.Value == EdgePattern.First).Select(o => o.Key).ToList();
            }
        }
        public List<Line> SecondLines
        {
            get
            {
                return SideLineNumberDict.Where(o => o.Value == EdgePattern.Second).Select(o => o.Key).ToList();
            }
        }
        public ThFirstSecondRecognitionEngine()
        {
            ApproximateTolerance = new Tolerance(1.0, 1.0);
            AccuracyTolerance = new Tolerance(1e-4, 1e-4);
        }
        public void Dispose()
        {
            //TODO
        }
        private void Init(List<Line> centerLines, double width)
        {
            CenterLineUsedRecordDict = new Dictionary<Line, bool>();
            SideLineNumberDict = new Dictionary<Line, EdgePattern>();
            CenterSpatialIndex = new ThCADCoreNTSSpatialIndex(centerLines.ToCollection());
            centerLines.ForEach(k => CenterLineUsedRecordDict.Add(k, false));
        }        
        public void Recognize(Point3d start, List<Line> centerLines, double width)
        {
            // 中心线是已经处理过的线
            if (centerLines.Count==0 || width <= 1.0)
            {
                return;
            }
            // 初始化
            Init(centerLines, width);

            // 返回的是中心线和边线的对应关系
            CenterSideDict = FindCenterPair(centerLines, width);
            //Print();

            HandleCenterSideDict(width);
            //Print();

            // 给CenterSideDict赋值
            CenterSideDict.ForEach(o =>
            {
                o.Value.Item1.ForEach(k => AddSideLineNumberDict(k, EdgePattern.Unknown));
                o.Value.Item2.ForEach(k => AddSideLineNumberDict(k, EdgePattern.Unknown));
            });

            // 根据start找到第一条边
            var centerFirst = FindCenterFirst(start, centerLines);
            var upDir = centerFirst.LineDirection().GetAlignedDimensionTextDir();
            UpdateCenterSide(centerFirst, upDir);
            SetLineNumer(centerFirst, CenterSpatialIndex);
        }

        private void SetLineNumer(Line center,ThCADCoreNTSSpatialIndex spatialIndex)
        {
            SetLineNumer(center, center.StartPoint, spatialIndex);
            SetLineNumer(center, center.EndPoint, spatialIndex);
        }
        private void SetLineNumer(Line center,Point3d portPt, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            // portPt是线的端点
            var objs = portPt.Query(spatialIndex, ApproximateTolerance.EqualPoint);
            objs.Remove(center);
            // 两直线的外角排序
            var links = objs.OfType<Line>().OrderByDescending(o => ThGarageUtils.CalculateTwoLineOuterAngle(
                center.StartPoint, center.EndPoint, o.StartPoint, o.EndPoint)).ToList();
            foreach (var link in links)
            {
                // 如果连接的边已被使用不继续传递下去
                if (CenterLineUsedRecordDict[link])
                {
                    continue;
                }
                var sideLines = GetCenterSides(link);
                if(sideLines.Count>0)
                {
                    if (PassLineNumber(center, link))
                    {
                        SetLineNumer(link, spatialIndex);
                    }
                }
                else
                {
                    UpdateCenterLineUsedRecordDict(link);
                    var linkPtRes = center.FindLinkPt(link);
                    if(linkPtRes.HasValue)
                    {
                        var nextPt = linkPtRes.Value.GetNextLinkPt(link.StartPoint,link.EndPoint);
                        var objs1 = nextPt.Query(spatialIndex, ApproximateTolerance.EqualPoint);
                        objs1.Remove(link);
                        var links1 = objs1
                            .OfType<Line>()
                            .Where(o=>ThGarageUtils.IsLessThan45Degree(link.StartPoint,link.EndPoint,o.StartPoint,o.EndPoint))
                            .OrderByDescending(o => ThGarageUtils.CalculateTwoLineOuterAngle(center.StartPoint, center.EndPoint, o.StartPoint, o.EndPoint))
                            .ToList();
                        if(links1.Count>0)
                        {
                            if(PassLineNumber(center, links1[0],true))
                            {
                                SetLineNumer(links1[0], spatialIndex);
                            }
                        }
                    }
                }
            }
        }        

        private bool PassLineNumber(Line preCenterLine, Line nextCenterLine,bool forcePass=false)
        {
            /*                   (second)  
             *                      .
             *                      .
             *                      .(center)
             *                      .
             * (first)...............(linkPt)
             *         (pre)   
             */
            bool issuccessful = false;
            var pts = forcePass? CreatePassPts(preCenterLine, nextCenterLine):CreateLinkPts(preCenterLine, nextCenterLine);
            if(pts.Count==3)
            {
                var preSideVec = pts[0].GetVectorTo(pts[1]).GetPerpendicularVector().GetNormal();
                var nextSideVec = pts[1].GetVectorTo(pts[2]).GetPerpendicularVector().GetNormal();
                issuccessful = SubPassLineNumber(preCenterLine, nextCenterLine, preSideVec, nextSideVec);
                if (issuccessful == false)
                {
                    // 让路径另一侧进行连接和传递
                    issuccessful = SubPassLineNumber(preCenterLine, nextCenterLine, preSideVec.Negate(), nextSideVec.Negate());
                }
            }
            return issuccessful;
        }

        private bool SubPassLineNumber(Line preCenterLine, Line nextCenterLine,Vector3d preSideVec,Vector3d nextSideVec)
        {
            var preSides = GetCenterSides(preCenterLine, preSideVec);
            var nextSides = GetCenterSides(nextCenterLine, nextSideVec);
            if (IsLink(preSides, nextSides))
            {
                var preSideEdgePattern = GetSideLineNumber(preSides[0]);
                if (preSideEdgePattern == EdgePattern.First)
                {
                    UpdateCenterSide(nextCenterLine, nextSideVec);
                    return true;
                }
                else if (preSideEdgePattern == EdgePattern.Second)
                {
                    UpdateCenterSide(nextCenterLine, nextSideVec.Negate());
                    return true;
                }
            }
            return false;
        }

        private bool IsLink(List<Line> sideLines1,List<Line> sideLines2)
        {
            for(int i=0;i< sideLines1.Count;i++)
            {
                for (int j = 0; j < sideLines2.Count; j++)
                {
                    if(IsLink(sideLines1[i], sideLines2[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsLink(Line line1, Line line2)
        {
            var newLine1 = line1.ExtendLine(-ApproximateTolerance.EqualPoint);
            var newLine2 = line2.ExtendLine(-ApproximateTolerance.EqualPoint);
            if(ThGeometryTool.IsOverlapEx(newLine1.StartPoint, newLine1.EndPoint, newLine2.StartPoint, newLine2.EndPoint))
            {
                return false;
            }
            var pt = line1.FindLinkPt(line2, ApproximateTolerance.EqualPoint);
            return pt.HasValue;
        }

        private void UpdateCenterSide(Line center,Vector3d vec)
        {
            // 设定vec方向的为1号边，方向为2号边
            var codirectionalSides = GetCenterSides(center, vec);
            var reverseSides = GetCenterSides(center, vec.Negate());
            UpdateSideLineNumberDict(codirectionalSides, EdgePattern.First);
            UpdateSideLineNumberDict(reverseSides, EdgePattern.Second);
            UpdateCenterLineUsedRecordDict(center);
        }

        private void UpdateSideLineNumberDict(List<Line> sideLines,EdgePattern edgePattern)
        {
            sideLines.ForEach(l => SideLineNumberDict[l] = edgePattern);
        }
        private void AddSideLineNumberDict(Line sideLine, EdgePattern edgePattern)
        {
            if(!SideLineNumberDict.ContainsKey(sideLine))
            {
                SideLineNumberDict.Add(sideLine, edgePattern);
            }
        }

        private EdgePattern GetSideLineNumber(Line sideLine)
        {
            return SideLineNumberDict[sideLine];
        }

        private void UpdateCenterLineUsedRecordDict(Line center)
        {
            CenterLineUsedRecordDict[center] = true;
        }

        private List<Line> GetCenterSides(Line center,Vector3d findDir)
        {
            var upDir = center.LineDirection().GetPerpendicularVector();
            if(upDir.IsCodirectionalTo(findDir, AccuracyTolerance))
            {
                return CenterSideDict[center].Item1;
            }
            if (upDir.Negate().IsCodirectionalTo(findDir, AccuracyTolerance))
            {
                return CenterSideDict[center].Item2;
            }
            return new List<Line>();
        }

        private List<Line> GetCenterSides(Line center)
        {
            var results = new List<Line>();
            if(CenterSideDict.ContainsKey(center))
            {
                results.AddRange(CenterSideDict[center].Item1);
                results.AddRange(CenterSideDict[center].Item2);
            }
            return results;
        }

        private Line FindCenterFirst(Point3d start, List<Line> lines, double tolerance = 5.0)
        {
            // 找出与start最近的线
            var results = lines.Where(l => l.StartPoint.DistanceTo(start) <= tolerance ||
            l.EndPoint.DistanceTo(start) <= tolerance).ToList();

            if(results.Count==1)
            {
                return results[0];
            }
            else
            {
                return lines[0];
            }
        }

        private Dictionary<Line, Tuple<List<Line>,List<Line>>> FindCenterPair(List<Line> lines,double width)
        {
            // 创建边线
            var sideLines = ThLightSideLineCreator.Create(lines,width);
            //sideLines.Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 5);

            var handler = new ThLightSideLineHandler(ShortLinkLineLength);
            var newSideLines = handler.Handle(sideLines.ToCollection()).OfType<Line>().ToList();
            //newSideLines.Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 5);

            var sideParameter = new ThFindSideLinesParameter
            {
                CenterLines = lines,
                SideLines = newSideLines,
                HalfWidth = width / 2.0
            };

            //查找合并线buffer后，获取中心线对应的两边线槽线
            var instane = new ThFindSideLinesService(sideParameter);
            return instane.FindSides();
        }

        private void HandleCenterSideDict(double width)
        {
            //目前只处理在T型处，产生的短线分配长的一边
            CenterSideDict.ForEach(c =>
            {
                var startLinks = c.Key.StartPoint.Query(CenterSpatialIndex, ApproximateTolerance.EqualPoint);
                startLinks.Remove(c.Key);
                if (startLinks.Count==2)
                {
                    if(IsTTypeLink(c.Key, startLinks[0] as Line, startLinks[1] as Line))
                    {
                        HandleTTypeLink(c.Key, startLinks[0] as Line, startLinks[1] as Line, width);
                    }
                }

                var endLinks = c.Key.EndPoint.Query(CenterSpatialIndex, ApproximateTolerance.EqualPoint);
                endLinks.Remove(c.Key);
                if (endLinks.Count==2)
                {
                    if (IsTTypeLink(c.Key, endLinks[0] as Line, endLinks[1] as Line))
                    {
                        HandleTTypeLink(c.Key, endLinks[0] as Line, endLinks[1] as Line, width);
                    }
                }
            });
        }

        private void HandleTTypeLink(Line firstLine,Line secondLine,Line thirdLine,double width)
        {
            /*           |
             *           |
             *           |(vertical)
             *           |
             *   ------------------
             *  (first)   (collinear)
             */
            var collinearLine = secondLine;
            var verticalLine = thirdLine;
            if (!IsCollinear(firstLine,secondLine))
            {
                collinearLine = thirdLine;
                verticalLine = secondLine;
            }
            var pathPts = CreateLinkPts(firstLine, verticalLine);
            var path1Pts = CreateLinkPts(collinearLine, verticalLine);
            if (pathPts.Count==0 || path1Pts.Count==0)
            {
                return;
            }
            var branchVec = pathPts[1].GetVectorTo(pathPts[2]).GetNormal();
            var firstSideVec = firstLine.LineDirection().GetPerpendicularVector();
            var collinearSideVec = collinearLine.LineDirection().GetPerpendicularVector();
            firstSideVec = firstSideVec.DotProduct(branchVec) > 0.0 ? firstSideVec.Negate() : firstSideVec;
            collinearSideVec = collinearSideVec.DotProduct(branchVec) > 0.0 ? collinearSideVec.Negate() : collinearSideVec;

            var firstSides = GetCenterSides(firstLine, firstSideVec);
            var collinearSides = GetCenterSides(collinearLine, collinearSideVec);

            var verFirst = verticalLine.GetOffsetCurves(width / 2.0)[0] as Line;
            var verSecond = verticalLine.GetOffsetCurves(-width / 2.0)[0] as Line;
            var section = GetParallelLineSection(verFirst, verSecond);
            // 缩短跨度
            Vector3d vec = section.Item1.GetVectorTo(section.Item2).GetNormal();
            Point3d sectionSp = section.Item1 + vec.MultiplyBy(10.0);
            Point3d sectionEp = section.Item2 - vec.MultiplyBy(10.0);
            var extendPairs = FindExtendPairs(firstSides, collinearSides, sectionSp, sectionEp);
            if (extendPairs.Count==0)
            {
                return;
            }
            var branchPair = SortBranchPair(firstLine, verFirst, verSecond);
            if(branchPair == null)
            {
                return;
            }
            if (firstLine.Length >= collinearLine.Length)
            {
                extendPairs.ForEach(p =>
                {
                    Line line1 = Extend(p.Item1, branchPair.Item2);
                    Line line2 = Shorten(p.Item2, branchPair.Item2, branchPair.Item1);
                    firstSides.Remove(p.Item1);
                    firstSides.Add(line1);
                    collinearSides.Remove(p.Item2);
                    collinearSides.Add(line2);
                });
            }
            else
            {
                extendPairs.ForEach(p =>
                {
                    Line line1 = Extend(p.Item2, branchPair.Item1);
                    Line line2 = Shorten(p.Item1, branchPair.Item1, branchPair.Item2);
                    collinearSides.Remove(p.Item2);
                    collinearSides.Add(line1);
                    firstSides.Remove(p.Item1);
                    firstSides.Add(line2);
                });
            }
        }

        private List<Tuple<Line,Line>> FindExtendPairs(List<Line> firstSides,List<Line> collinearSides, Point3d sectionSp, Point3d sectionEp)
        {
            var results = new List<Tuple<Line, Line>>();      
            for (int i=0;i<firstSides.Count;i++)
            {
                if(!IsSideLineCanExtend(firstSides[i], sectionSp, sectionEp))
                {
                    continue;
                }
                for (int j = 0; j < collinearSides.Count; j++)
                {
                    if (!IsSideLineCanExtend(collinearSides[j], sectionSp, sectionEp))
                    {
                        continue;
                    }
                    Point3d? linkRes = ThGarageUtils.FindLinkPt(
                        firstSides[i], collinearSides[j], ApproximateTolerance.EqualPoint);
                    if (linkRes.HasValue)
                    {
                        results.Add(Tuple.Create(firstSides[i], collinearSides[j]));
                    }
                }
            }
            return results;
        }

        private bool IsSideLineCanExtend(Line side, Point3d sectionSp,Point3d sectionEp)
        {
            // 有个点在区域内,有个点不在
            var firstSpIn = ThGeometryTool.IsProjectionPtInLine(sectionSp, sectionEp, side.StartPoint);
            var firstEpIn = ThGeometryTool.IsProjectionPtInLine(sectionSp, sectionEp, side.EndPoint);
            if (firstSpIn && firstEpIn)
            {
                return false;
            }
            if(!firstSpIn && !firstEpIn)
            {
                return false;
            }
            return true;
        }

        private Tuple<Point3d,Point3d> GetParallelLineSection(Line first,Line second)
        {
            // first 和 second 是平行线
            var firstMidPt = first.StartPoint.GetMidPt(first.EndPoint);
            var firstProjection = firstMidPt.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
            return Tuple.Create(firstMidPt, firstProjection);
        }

        private Tuple<Line,Line> SortBranchPair(Line sideLine,Line branchFirst,Line branchSecond)
        {
            var firstInters = sideLine.IntersectWithEx(branchFirst, Intersect.ExtendBoth);
            var secondInters = sideLine.IntersectWithEx(branchSecond, Intersect.ExtendBoth);
            if(firstInters.Count==0 || secondInters.Count == 0)
            {
                return null;
            }
            var pairs = new List<Tuple<Point3d, Point3d>>();
            if (!ThGeometryTool.IsPointOnLine(firstInters[0], secondInters[0], sideLine.StartPoint, ApproximateTolerance.EqualPoint))
            {
                pairs.Add(Tuple.Create(sideLine.StartPoint, firstInters[0]));
                pairs.Add(Tuple.Create(sideLine.StartPoint, secondInters[0]));
            }
            if (!ThGeometryTool.IsPointOnLine(firstInters[0], secondInters[0], sideLine.EndPoint, ApproximateTolerance.EqualPoint))
            {
                pairs.Add(Tuple.Create(sideLine.EndPoint, firstInters[0]));
                pairs.Add(Tuple.Create(sideLine.EndPoint, secondInters[0]));
            }
            if(pairs.Count>0)
            {
               var farwayIntersPt = pairs.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First().Item2;
               if(farwayIntersPt.IsEqualTo(secondInters[0],ApproximateTolerance))
                {
                    return Tuple.Create(branchFirst, branchSecond);
                }
                {
                    return Tuple.Create(branchSecond,branchFirst);
                }
            }
            return null;
        }

        private Line Extend(Line sideLine, Line branchLine)
        {
            /*                  |    |
             *      (sideLine)  |    |(branchLine)
             *  ------------------>  |
             *                  |    |
             *                  |    |
            */
            // sides 要在farwayPt的一侧,不要超出
            var inters = sideLine.IntersectWithEx(branchLine, Intersect.ExtendBoth);
            if (sideLine.StartPoint.DistanceTo(inters[0]) > sideLine.EndPoint.DistanceTo(inters[0]))
            {
                return new Line(sideLine.StartPoint, inters[0]);
            }
            else
            {
                return new Line(sideLine.EndPoint, inters[0]);
            }
        }
        private Line Shorten(Line side, Line branchFirst, Line branchSecond)
        {
            /*                  |    |
             *     (branchFirst)|    |(branchSecond)
             *  ----------------|<-- |
             *      (side)      |    |
             *                  |    |
            */
            var section = GetParallelLineSection(branchFirst, branchSecond);            
            var inters = side.IntersectWithEx(branchFirst, Intersect.ExtendBoth);
            if(ThGeometryTool.IsProjectionPtInLine(section.Item1, section.Item2, side.StartPoint))
            {
                return new Line(inters[0],side.EndPoint);
            }
            else 
            {
                return new Line(side.StartPoint,inters[0]);
            }
        }
           
        private List<Point3d> CreateLinkPts(Line first, Line second)
        {
            // 创建从first到second的路径点
            var result = new List<Point3d>();
            var linkPt = first.FindLinkPt(second, ApproximateTolerance.EqualPoint);
            if (linkPt.HasValue)
            {
                var firstPt = linkPt.Value.GetNextLinkPt(first.StartPoint, first.EndPoint);
                var secondPt = linkPt.Value.GetNextLinkPt(second.StartPoint, second.EndPoint);
                result.Add(firstPt);
                result.Add(linkPt.Value);
                result.Add(secondPt);
                return result;
            }
            return result;
        }

        private List<Point3d> CreatePassPts(Line first, Line second)
        {
            // 创建从first到second的路径点
            var result = new List<Point3d>();
            var firstPt = first.StartPoint.DistanceTo(second.StartPoint) > first.EndPoint.DistanceTo(second.StartPoint) ? 
                first.StartPoint:first.EndPoint;
            result.Add(firstPt);
            result.Add(firstPt.GetNextLinkPt(first.StartPoint,first.EndPoint));
            result.Add(second.EndPoint.DistanceTo(firstPt) > second.StartPoint.DistanceTo(firstPt) ?
                second.EndPoint : second.StartPoint);
            return result;
        }

        private bool IsTTypeLink(Line first,Line second ,Line third)
        {
            if(IsCollinear(first, second)) 
            {
                if(IsVertical(first,third))
                {
                    return true;
                }
            }
            if (first.IsCollinear(third))
            {
                if (IsVertical(first,second))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsCollinear(Line first,Line second)
        {
            return ThGeometryTool.IsCollinearEx(
                first.StartPoint, first.EndPoint,
                second.StartPoint, second.EndPoint,
                ApproximateTolerance.EqualPoint);
        }

        private bool IsVertical(Line first , Line second)
        {
            return first.LineDirection().IsVertical(second.LineDirection(), TTypeAngleTolerance);
        }

        private void Print()
        {
            var res1 = CenterSideDict.SelectMany(o => o.Value.Item1).Select(l => l.Clone() as Line).Cast<Entity>().ToList();
            res1 = res1.Distinct().ToList();
            res1.CreateGroup(AcHelper.Active.Database, 1);

            var res2 = CenterSideDict.SelectMany(o => o.Value.Item2).Select(l => l.Clone() as Line).Cast<Entity>().ToList();
            res2 = res2.Distinct().ToList();
            res2.CreateGroup(AcHelper.Active.Database, 6);
        }
    }
}
