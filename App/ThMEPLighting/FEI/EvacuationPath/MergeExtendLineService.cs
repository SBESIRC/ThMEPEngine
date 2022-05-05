using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class MergeExtendLineService
    {
        readonly double tol = 200;
        readonly double laneMergeDis = 15000;
        readonly double removeDis = 10000;
        public List<ExtendLineModel> MergeLines(List<List<Line>> xLanes, List<List<Line>> yLanes, List<ExtendLineModel> extendLines)
        {
            List<ExtendLineModel> resLines = extendLines.Where(x => x.priority == Priority.startExtendLine || x.priority == Priority.MergeStartLine).ToList();
            List<ExtendLineModel> otherLines = extendLines.Except(resLines).ToList();
            
            resLines.AddRange(MergeMainLines(otherLines, xLanes, yLanes));
            resLines.AddRange(MergeMainLines(otherLines, yLanes, xLanes));

            return resLines.Distinct().ToList();
        }

        /// <summary>
        /// 合并线（删除范围内多余的线）
        /// </summary>
        /// <param name="extendLines"></param>
        /// <param name="connectLanes"></param>
        /// <param name="otherLanes"></param>
        private List<ExtendLineModel> MergeMainLines(List<ExtendLineModel> extendLines, List<List<Line>> connectLanes, List<List<Line>> otherLanes)
        {
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            if (connectLanes.Count <= 0)
            {
                return resLines;
            }

            connectLanes = OrderLanes(connectLanes);
            foreach (var lane in connectLanes)
            {
                var matchLines = extendLines.Where(x => x.startLane == lane || x.endLane == lane).ToList();
                extendLines = extendLines.Except(matchLines).ToList();
                if (matchLines.Count > 0)
                {
                    RemoveAroundLanesLines(matchLines, otherLanes);      //穿过当前车道线的延伸线，用另一方向车道线判断是否离的过近需要删除
                    resLines.AddRange(RemoveSurplusExtendLine(matchLines));
                }
            }

            return resLines;
        }

        /// <summary>
        /// 去除不需要多余的线
        /// </summary>
        /// <param name="extendLines"></param>
        private List<ExtendLineModel> RemoveSurplusExtendLine(List<ExtendLineModel> extendLines)
        {
            List<ExtendLineModel> usefulLines = new List<ExtendLineModel>();
            if (extendLines.Count <= 0)
            {
                return extendLines;
            }

            //排序延申线
            extendLines = OrderExtendLine(extendLines);

            var firstLine = extendLines.First();
            extendLines.Remove(firstLine);
            usefulLines.Add(firstLine);
            var resLines = GetAroudLines(firstLine.line, extendLines.Select(x => x.line).ToList(), removeDis);
            extendLines.RemoveAll(x => resLines.Contains(x.line));
            if (extendLines.Count > 0)
            {
                var lastLine = extendLines.Last();
                extendLines.Remove(lastLine);
                usefulLines.Add(lastLine);
                resLines = GetAroudLines(lastLine.line, extendLines.Select(x => x.line).ToList(), removeDis);
                extendLines.RemoveAll(x => resLines.Contains(x.line));

                var firExtendLine = new ExtendLineTreeModel(firstLine, null);
                var resExtendLines = RemoveLines(extendLines, firExtendLine);

                return GetMostBalancedExtendLines(resExtendLines, lastLine);
            }
            else
            {
                return new List<ExtendLineModel> { firstLine };
            }
        }

        /// <summary>
        /// 求最均匀的排布方案
        /// </summary>
        /// <param name="extendLineTrees"></param>
        /// <param name="lastLine"></param>
        /// <returns></returns>
        private List<ExtendLineModel> GetMostBalancedExtendLines(List<ExtendLineTreeModel> extendLineTrees, ExtendLineModel lastLine)
        {

            List<List<ExtendLineModel>> resLinesPlan = new List<List<ExtendLineModel>>();
            foreach (var extendLineTree in extendLineTrees)
            {
                var resExLines = GetExtendLinesPlan(extendLineTree);
                resExLines.Add(lastLine);
                resLinesPlan.Add(resExLines);
            }

            var linesPlanInfo = resLinesPlan.ToDictionary(x => x, y => CalVariance(y));

            return linesPlanInfo.OrderBy(x => x.Value).First().Key;
        }

        /// <summary>
        /// 获取合并方案
        /// </summary>
        /// <param name="extendLineTree"></param>
        /// <returns></returns>
        private List<ExtendLineModel> GetExtendLinesPlan(ExtendLineTreeModel extendLineTree)
        {
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            ExtendLineTreeModel nodeLine = extendLineTree;
            while (nodeLine.parentExtendLine != null)
            {
                resLines.Insert(0, nodeLine.ExtendLine);
                nodeLine = nodeLine.parentExtendLine;
            }

            resLines.Insert(0, nodeLine.ExtendLine);
            return resLines;
        }

        /// <summary>
        /// 合并线
        /// </summary>
        /// <param name="extendLines"></param>
        /// <param name="pLine"></param>
        /// <returns></returns>
        private List<ExtendLineTreeModel> RemoveLines(List<ExtendLineModel> extendLines, ExtendLineTreeModel pLine)
        {
            if (extendLines.Count <= 0)
            {
                return new List<ExtendLineTreeModel>() { pLine };
            }

            List<ExtendLineTreeModel> resExtendLines = new List<ExtendLineTreeModel>();
            var firLine = extendLines.First();
            ExtendLineTreeModel parentLine = pLine;
            while (extendLines.Count > 0)
            {
                var resLines = GetAroudLines(firLine.line, extendLines.Select(x => x.line).ToList(), removeDis);
                var resExLines = extendLines.Where(x => resLines.Contains(x.line)).ToList();
                if (resExLines.Count > 0)
                {
                    var firLevelExLines = resExLines.Where(x => x.priority == Priority.firstLevel).ToList();
                    if (firLevelExLines.Count > 0)
                    {
                        var moveLines = resExLines.Except(firLevelExLines).ToList();
                        extendLines.RemoveAll(x => moveLines.Contains(x));
                        resExLines = firLevelExLines;
                    }

                    foreach (var line in resExLines)
                    {
                        List<ExtendLineModel> checkLines = new List<ExtendLineModel>(extendLines);
                        checkLines.Remove(line);
                        ExtendLineTreeModel extendLine = new ExtendLineTreeModel(line, parentLine);

                        resLines = GetAroudLines(line.line, checkLines.Select(x => x.line).ToList(), removeDis);
                        resExLines = checkLines.Where(x => resLines.Contains(x.line)).ToList();
                        checkLines = checkLines.Except(resExLines).ToList();

                        resExtendLines.AddRange(RemoveLines(checkLines, extendLine));
                    }

                    break;
                }
            }

            return resExtendLines;
        }

        /// <summary>
        /// 排序延伸线
        /// </summary>
        /// <param name="extendLines"></param>
        /// <returns></returns>
        private List<ExtendLineModel> OrderExtendLine(List<ExtendLineModel> extendLines)
        {
            var line = extendLines.First().line;
            Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, yDir.X, zDir.X, 0,
                xDir.Y, yDir.Y, zDir.Y, 0,
                xDir.Z, yDir.Z, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            extendLines = extendLines.OrderBy(x =>
            {
                var transLine = x.line.Clone() as Polyline;
                return transLine.StartPoint.TransformBy(matrix).Y;
            }).ToList();

            return extendLines;
        }

        /// <summary>
        /// 排序车道线
        /// </summary>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private List<List<Line>> OrderLanes(List<List<Line>> lanes)
        {
            var line = lanes.SelectMany(x => x).OrderBy(x => x.Length).Last();
            Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, yDir.X, zDir.X, 0,
                xDir.Y, yDir.Y, zDir.Y, 0,
                xDir.Z, yDir.Z, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            lanes = lanes.OrderBy(x =>
            {
                return x.First().StartPoint.TransformBy(matrix).Y;
            }).ToList();

            return lanes;
        }

        /// <summary>
        /// 删除离车道线周围离得过近的延伸线
        /// </summary>
        /// <param name="extendLines"></param>
        /// <param name="lanes"></param>
        private void RemoveAroundLanesLines(List<ExtendLineModel> extendLines, List<List<Line>> lanes)
        {
            var lines = extendLines.Select(x => x.line).ToList();
            ParkingLinesService parkingLinesService = new ParkingLinesService();
            foreach (var lane in lanes)
            {
                var polyLane = parkingLinesService.CreateParkingLineToPolyline(lane);
                var resLines = GetAroudLines(polyLane, lines, laneMergeDis);
                if (resLines.Count > 0)
                {
                    var matchLines = extendLines.Where(x => resLines.Contains(x.line)).ToList();
                    foreach (var mLine in matchLines)
                    {
                        if (IsOverloop(lane, mLine.line))
                        {
                            if (IsCheckLane(lane, mLine))
                            {
                                extendLines.Remove(mLine);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取周围线
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="otherLines"></param>
        /// <returns></returns>
        private List<Polyline> GetAroudLines(Polyline polyline, List<Polyline> otherLines, double distance)
        {
            var bufferPoly = polyline.BufferPL(distance)[0] as Polyline;
            var selectLines = SelectService.SelelctCrossing(otherLines, bufferPoly);

            List<Polyline> resLines = new List<Polyline>();
            foreach (var line in selectLines)
            {
                resLines.AddRange(otherLines.Where(x => (x.StartPoint.IsEqualTo(line.StartPoint) && x.EndPoint.IsEqualTo(line.EndPoint))
                    || (x.StartPoint.IsEqualTo(line.EndPoint) && x.EndPoint.IsEqualTo(line.StartPoint))));
            }

            return resLines;
        }

        /// <summary>
        /// 判断是否重合
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsOverloop(List<Line> lanes, Polyline line)
        {
            Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, yDir.X, zDir.X, 0,
                xDir.Y, yDir.Y, zDir.Y, 0,
                xDir.Z, yDir.Z, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            var minX = line.StartPoint.TransformBy(matrix).X;
            var maxX = line.EndPoint.TransformBy(matrix).X;
            if (minX > maxX)
            {
                maxX = line.StartPoint.TransformBy(matrix).X;
                minX = line.EndPoint.TransformBy(matrix).X;
            }
            List<Point3d> allPts = lanes.SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint })
                .Select(x => x.TransformBy(matrix))
                .OrderBy(x => x.X)
                .ToList();

            if (allPts.First().X + tol > maxX || allPts.Last().X - tol < minX)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断是否是需要比较的车道线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="extendLine"></param>
        /// <returns></returns>
        private bool IsCheckLane(List<Line> lines, ExtendLineModel extendLine)
        {
            bool startCheck = false;
            foreach (var lane in extendLine.startLane)
            {
                foreach (var line in lines)
                {
                    var sCPt = lane.GetClosestPointTo(line.StartPoint, false);
                    var eCPt = lane.GetClosestPointTo(line.EndPoint, false);
                    if (sCPt.IsEqualTo(line.StartPoint, new Tolerance(5, 5)) || eCPt.IsEqualTo(line.EndPoint, new Tolerance(5, 5)))
                    {
                        startCheck = true;
                        break;
                    }
                }
                if (startCheck)
                {
                    break;
                }
            }

            bool endCheck = false;
            foreach (var lane in extendLine.endLane)
            {
                foreach (var line in lines)
                {
                    var sCPt = lane.GetClosestPointTo(line.StartPoint, false);
                    var eCPt = lane.GetClosestPointTo(line.EndPoint, false);
                    if (sCPt.IsEqualTo(line.StartPoint, new Tolerance(5, 5)) || eCPt.IsEqualTo(line.EndPoint, new Tolerance(5, 5)))
                    {
                        endCheck = true;
                        break;
                    }
                }
                if (endCheck)
                {
                    break;
                }
            }

            return startCheck && endCheck;
        }

        /// <summary>
        /// 求车道线间距的方差
        /// </summary>
        /// <param name="extendLines"></param>
        /// <returns></returns>
        private double CalVariance(List<ExtendLineModel> extendLines)
        {
            List<double> distance = new List<double>();
            for (int i = 0; i < extendLines.Count - 1; i++)
            {
                distance.Add(extendLines[i].line.Distance(extendLines[i + 1].line));
            }

            double averageNumber = distance.Sum() / distance.Count;
            double varianceNumber = distance.Select(x=> Math.Pow(averageNumber - x, 2)).Sum() / distance.Count;

            return varianceNumber;
        }
    }
}
