using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    public class LayoutToStructure
    {
        readonly double minWidth = 500;
        readonly double minLayoutLineLength = 400.0;
        private Polyline _maxPolyline;
        private List<Polyline> _innerPolylines;
        private double _maxDistanceToLine;
        private List<Line> _maxLines;
        public LayoutToStructure(Polyline outMaxPline,List<Polyline> innerPolylines,double maxDisToLine) 
        {
            _maxPolyline = outMaxPline;
            _maxDistanceToLine = maxDisToLine;
            _maxLines = new List<Line>();
            _innerPolylines = new List<Polyline>();
            if (null != outMaxPline)
            {
                var polyline = _maxPolyline.DPSimplify(10);
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    var sp = polyline.GetPoint3dAt(i);
                    var ep = polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices);
                    if (sp.DistanceTo(ep) < 0.0001)
                        continue;
                    _maxLines.Add(new Line(sp, ep));
                }
            }
            foreach (var item in innerPolylines) 
            {
                _innerPolylines.Add(item);
            }
        }
        public Dictionary<Point3d, Vector3d> GetLayoutStructPt(Point3d lineSp,Point3d lineEp, List<Polyline> columns, List<Polyline> walls, Vector3d sideDir, bool spCalc = true)
        {
            Dictionary<Point3d, Vector3d> ptDic = new Dictionary<Point3d, Vector3d>();
            double length = lineSp.DistanceTo(lineEp);
            var wallLayouts = GetAllWallPolylineLayout(walls, lineSp, lineEp, sideDir);
            var layoutPts = new List<Point3d>() { lineSp, lineEp };
            Line line = new Line(lineSp, lineEp);
            var lineDir = (lineEp - lineSp).GetNormal();
            foreach (var pt in layoutPts)
            {
                if (!spCalc && pt.DistanceTo(lineSp) < 10)
                    continue;
                var column = columns.Distinct().ToDictionary(x => x, y => y.Distance(pt)).OrderBy(x => x.Value).ToList();
                var wall = walls.Distinct().ToDictionary(x => x, y => y.Distance(pt)).OrderBy(x => x.Value).ToList();
                if (column.Count <= 0 && wall.Count <= 0)
                {
                    return null;
                }
                (Point3d, Vector3d)? layoutInfo = null;
                //优先找柱子
                if (column.Count > 0)
                {
                    //如果柱点到起点距离3500优先找柱子
                    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, lineDir);
                    if (null != layoutInfo && layoutInfo.HasValue)
                    {
                        var prjPt = EmgPilotLampUtil.PointToLine(layoutInfo.Value.Item1, line);
                        if (prjPt.DistanceTo(lineSp) > length)
                            layoutInfo = null;
                        else if ( length > 3500 && ((prjPt.DistanceTo(lineSp) > 500 && prjPt.DistanceTo(lineSp) < 3500) || (prjPt.DistanceTo(pt) > length * 2 / 3 && pt.DistanceTo(lineSp) > length * 2 / 3)
                            || (prjPt.DistanceTo(pt) > 3500 && pt.DistanceTo(lineSp) < 100)))
                            layoutInfo = null;
                    }

                }
                if (null == layoutInfo)
                {
                    if (wall.Count > 0)
                    {
                        (Point3d, Vector3d)? wallLayout = GetLayoutLineInfo(wallLayouts, pt, lineSp, lineDir);
                        if (columns.Count > 0)
                        {
                            var dis = 0.0;
                            var colLayout = GetColumnLayoutPoint(column.First().Key, pt, lineDir);
                            if (wallLayout != null && colLayout != null)
                            {
                                var colPrj = EmgPilotLampUtil.PointToLine(colLayout.Value.Item1, line);
                                var wallPrj = EmgPilotLampUtil.PointToLine(wallLayout.Value.Item1, line);
                                dis = colPrj.DistanceTo(pt) - wallPrj.DistanceTo(pt);
                            }
                            if (Math.Abs(dis) < 2000)
                                layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, lineDir);
                            else if (column.First().Value < wall.First().Value)
                                layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, lineDir);
                            else
                            {
                                layoutInfo = wallLayout;
                                if (layoutInfo == null && column.Count > 0)
                                {
                                    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, lineDir);
                                }
                            }
                        }
                        else
                        {
                            layoutInfo = wallLayout;
                        }
                    }
                    else if (column.Count > 0)
                    {
                        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, lineDir);
                    }
                }
                if (layoutInfo.HasValue)
                {
                    if (!ptDic.Keys.Contains(layoutInfo.Value.Item1))
                    {
                        ptDic.Add(layoutInfo.Value.Item1, layoutInfo.Value.Item2);
                    }
                }
            }

            return ptDic;
        }
        public Dictionary<Point3d, Vector3d> GetLayoutStructPtColumnFirst(List<Point3d> layoutPts, List<Polyline> columns, List<Polyline> walls, Vector3d dir)
        {
            Dictionary<Point3d, Vector3d> ptDic = new Dictionary<Point3d, Vector3d>();
            if (layoutPts == null || layoutPts.Count < 1)
                return null;
            Point3d sp = layoutPts.First();
            Point3d ep = layoutPts.Last();
            double length = layoutPts.First().DistanceTo(layoutPts.Last());
            List<Polyline> wallsInPoints = new List<Polyline>();
            List<Polyline> columnsInPoints = new List<Polyline>();


            foreach (var pt in layoutPts)
            {
                var column = columns.Distinct().ToDictionary(x => x, y => y.Distance(pt)).OrderBy(x => x.Value).ToList();
                var wall = walls.Distinct().ToDictionary(x => x, y => y.Distance(pt)).OrderBy(x => x.Value).ToList();
                if (column.Count <= 0 && wall.Count <= 0)
                {
                    return null;
                }
                (Point3d, Vector3d)? layoutInfo = null;
                //优先找柱子
                if (column.Count > 0 || wall.Count < 1)
                {
                    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                }
                else
                {
                    layoutInfo = GetWallLayoutPoint(wall.First().Key, pt, dir,sp,length);
                    if (layoutInfo == null && column.Count > 0)
                    {
                        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                    }
                }
                if (layoutInfo.HasValue)
                {
                    if (!ptDic.Keys.Contains(layoutInfo.Value.Item1))
                    {
                        ptDic.Add(layoutInfo.Value.Item1, layoutInfo.Value.Item2);
                    }
                }
            }

            return ptDic;
        }
        /// <summary>
        /// 计算墙上排布点和方向
        /// </summary>
        /// <param name="wall"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public (Point3d, Vector3d)? GetWallLayoutPoint(Polyline wall, Point3d pt, Vector3d dir,Point3d startPoint,double maxDis)
        {
            var layoutLine = GetLayoutStructLine(wall, pt, dir, out Point3d closetPt, startPoint,maxDis);
            if (layoutLine == null)
            {
                return null;
            }

            Point3d sPt = layoutLine.StartPoint;
            Point3d ePt = layoutLine.EndPoint;
            Vector3d moveDir = (ePt - sPt).GetNormal();
            if (layoutLine.Length < 350)
                return null;
            //计算排布点
            var layoutPt = closetPt;
            if (sPt.DistanceTo(layoutPt) < minWidth)
            {
                layoutPt = layoutPt + moveDir * (minWidth/2 - sPt.DistanceTo(layoutPt));
            }
            if (ePt.DistanceTo(layoutPt) < minWidth)
            {
                layoutPt = layoutPt - moveDir * (minWidth/2 - ePt.DistanceTo(layoutPt));
            }
            //计算排布方向
            var layoutDir = Vector3d.ZAxis.CrossProduct((ePt - sPt).GetNormal());
            var compareDir = (pt - layoutPt).GetNormal();
            if (layoutDir.DotProduct(compareDir) < 0)
            {
                layoutDir = -layoutDir;
            }

            return (layoutPt, layoutDir);
        }

        /// <summary>
        /// 计算柱上排布点和方向
        /// </summary>
        /// <param name="column"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public (Point3d, Vector3d)? GetColumnLayoutPoint(Polyline column, Point3d pt, Vector3d dir)
        {
            var layoutLine = GetLayoutStructLine(column, pt, dir, out Point3d closetPt,out Vector3d outDir);
            if (layoutLine == null)
            {
                return null;
            }

            Point3d sPt = layoutLine.StartPoint;
            Point3d ePt = layoutLine.EndPoint;

            //计算排布点
            var layoutPt = new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0);
            var lineDir = (ePt - sPt).GetNormal();
            //计算排布方向
            var layoutDir = Vector3d.ZAxis.CrossProduct((ePt - sPt).GetNormal());
            var compareDir = (pt - layoutPt).GetNormal();
            if (layoutDir.DotProduct(compareDir) < 0)
            {
                layoutDir = -layoutDir;
            }
            if (!CheckLayoutPointInMaxPolyline(layoutPt, outDir))
                return null;
            if (!CheckLayoutLineCanLayout(layoutPt, layoutLine, outDir, lineDir))
                return null;
            return (layoutPt, layoutDir);
        }

        /// <summary>
        /// 找到构建的布置边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Line GetLayoutStructLine(Polyline polyline, Point3d pt, Vector3d dir, out Point3d layoutPt,out Vector3d outDir)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            layoutPt = closetPt;
            outDir = new Vector3d();
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var lineOutDir = EmgPilotLampUtil.PolylineOutDir(polyline);
            Line layoutLine = null;
            foreach (var item in lineOutDir) 
            {
                if (!EmgPilotLampUtil.PointInLine(closetPt, item.Key))
                    continue;
                var xDir = (item.Key.EndPoint - item.Key.StartPoint).GetNormal();
                if (Math.Abs(otherDir.DotProduct(xDir)) >= Math.Abs(dir.DotProduct(xDir)))
                    continue;
                layoutLine = item.Key;
                outDir = item.Value;
                break;
            }
            return layoutLine;
        }
        private Line GetLayoutStructLine(Polyline polyline, Point3d pt, Vector3d dir, out Point3d layoutPt,Point3d startPoint,double maxDis)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            layoutPt = closetPt;
            Line layoutLine = null;
            List<Line> lines = new List<Line>();
            //多段线有可以合并的线，这里如果没有合并，如果有些是多段线
            polyline = polyline.DPSimplify(2);
            var prjPoint = EmgPilotLampUtil.PointToLine(closetPt, startPoint, dir);
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
            lines = lines.Where(c => c.Length > 100).ToList();
            var maxLength = lines.Max(c => c.Length);
            var prjDis = prjPoint.DistanceTo(startPoint);
            var dis = double.MinValue;
            var disX = double.MaxValue;
            Line tempLine = null;
            Point3d? tempPoint = null;
            foreach (var li in lines)
            {
                //if (li.Length < 500)
                //    continue;
                var xDir = (li.EndPoint - li.StartPoint).GetNormal();
                if (Math.Abs(otherDir.DotProduct(xDir)) > Math.Abs(dir.DotProduct(xDir)))
                    continue;
                var outDir = Vector3d.ZAxis.CrossProduct(xDir);
                var closePt = li.GetClosestPointTo(pt, false);
                prjPoint = EmgPilotLampUtil.PointToLine(closePt, startPoint, dir);
                var tempdis = prjPoint.DistanceTo(closePt);
                var tempDir = (closePt - prjPoint).GetNormal();
                var tempDot = tempDir.DotProduct(outDir);
                if (tempDot > 0.1)
                    continue;
                if (tempdis > maxDis || Math.Abs(tempdis-maxDis)<5)
                    continue;
                if (dis <= tempdis && disX > Math.Abs(tempDot))
                {
                    tempPoint = closePt;
                    tempLine = li;
                    dis = tempdis;
                }
            }
            if (null != tempLine)
            {
                layoutPt = tempPoint.Value;
                layoutLine = tempLine;
            }
            return layoutLine;
            if (prjDis > maxDis || (maxDis > prjDis && Math.Abs(prjDis - maxDis) < 10))
            {
                //近点不符合要求，进一步计算
                //var dis = double.MinValue;
                //var disX = double.MaxValue;
                //Line tempLine = null;
                //Point3d? tempPoint = null;
                //foreach (var li in lines)
                //{
                //    var xDir = (li.EndPoint - li.StartPoint).GetNormal();
                //    if (Math.Abs(otherDir.DotProduct(xDir)) > Math.Abs(dir.DotProduct(xDir)))
                //        continue;
                //    var outDir = Vector3d.ZAxis.CrossProduct(xDir);
                //    var temp = li.GetClosestPointTo(pt, false);
                //    prjPoint = EmgPilotLampUtil.PointToLine(temp, startPoint, dir);
                //    var tempdis = prjPoint.DistanceTo(startPoint);
                //    var tempDir = (temp - prjPoint).GetNormal();
                //    var tempDot = tempDir.DotProduct(outDir);
                //    if (tempDot > 0.1)
                //        continue;
                //    if (Math.Abs(tempdis - maxDis) > 1)
                //        continue;
                //    if (dis <= tempdis && disX > Math.Abs(tempDot))
                //    {
                //        tempPoint = temp;
                //        tempLine = li;
                //        dis = tempdis;
                //    }
                //}
                //if (null != tempLine)
                //{
                //    layoutPt = tempPoint.Value;
                //    layoutLine = tempLine;
                //}
            }
            else
            {

                layoutLine = lines.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
                    .Where(x =>
                    {
                        var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                        return Math.Abs(otherDir.DotProduct(xDir)) < Math.Abs(dir.DotProduct(xDir));
                    }).FirstOrDefault();
            }
            return layoutLine;
        }

        private (Point3d, Vector3d)? GetLayoutLineInfo(List<LineLayout> allLineLayouts, Point3d pt, Point3d lineSp, Vector3d lineDir) 
        {
            LineLayout lineLayout = null;
            bool isStart = false;
            if (pt.DistanceTo(lineSp) < 10)
                //起点找离的最近的可以布置的点。终点找最远可以布置的点
                isStart = true;
            var nearDisToLine = double.MaxValue;
            if (isStart)
            {
                var nearDis = double.MaxValue;
                
                foreach (var item in allLineLayouts) 
                {
                    var layoutPt= GetLineLayoutPoint(item.canLayoutLine, pt, lineSp);
                    if (!CheckLayoutPointInMaxPolyline(layoutPt.Value, item.sideDirection))
                        continue;
                    if (!CheckLayoutLineCanLayout(layoutPt.Value, item.canLayoutLine, item.sideDirection, item.layoutDirection))
                        continue;
                    var prjToLine = EmgPilotLampUtil.PointToLine(layoutPt.Value, lineSp, lineDir);
                    var vectorPt = prjToLine - layoutPt.Value;
                    if (vectorPt.DotProduct(item.sideDirection) <0.1)
                        continue;
                    var disToSp = prjToLine.DistanceTo(lineSp);
                    if (prjToLine.DistanceTo(layoutPt.Value) > _maxDistanceToLine)
                        continue;
                    item.layoutPoint = layoutPt.Value;
                    if (Math.Abs(nearDis - disToSp) < 1000) 
                    {
                        var tempDis = prjToLine.DistanceTo(layoutPt.Value);
                        if (tempDis < nearDisToLine) 
                        {
                            lineLayout = item;
                            nearDis = disToSp;
                            nearDisToLine = prjToLine.DistanceTo(layoutPt.Value);
                        }
                    }
                    else if (disToSp < nearDis) 
                    {
                        lineLayout = item;
                        nearDis = disToSp;
                        nearDisToLine = prjToLine.DistanceTo(layoutPt.Value);
                    }
                }
            }
            else 
            {
                var nearDis = double.MinValue;
                foreach (var item in allLineLayouts)
                {
                    var layoutPt = GetLineLayoutPoint(item.canLayoutLine, pt, lineSp);
                    if (!CheckLayoutPointInMaxPolyline(layoutPt.Value, item.sideDirection))
                        continue;
                    if (!CheckLayoutLineCanLayout(layoutPt.Value, item.canLayoutLine, item.sideDirection, item.layoutDirection))
                        continue;
                    var prjToLine = EmgPilotLampUtil.PointToLine(layoutPt.Value, lineSp, lineDir);
                    var vectorPt = prjToLine - layoutPt.Value;
                    if (vectorPt.DotProduct(item.sideDirection) < 0.1)
                        continue;
                    if (prjToLine.DistanceTo(layoutPt.Value) > _maxDistanceToLine)
                        continue;
                    var disToSp = prjToLine.DistanceTo(lineSp);
                    item.layoutPoint = layoutPt.Value;
                    if (Math.Abs(nearDis - disToSp) < 1000)
                    {
                        var tempDis = prjToLine.DistanceTo(layoutPt.Value);
                        if (tempDis < nearDisToLine)
                        {
                            lineLayout = item;
                            nearDis = disToSp;
                            nearDisToLine = prjToLine.DistanceTo(layoutPt.Value);
                        }
                    }
                    else if (disToSp > nearDis)
                    {
                        lineLayout = item;
                        nearDis = disToSp;
                        nearDisToLine = prjToLine.DistanceTo(layoutPt.Value);
                    }

                }
            }
            if (lineLayout == null)
                return null;
            return (lineLayout.layoutPoint, lineLayout.sideDirection);
        }
        private List<LineLayout> GetAllWallPolylineLayout(List<Polyline> wallPolylines,Point3d lineSp,Point3d lineEp,Vector3d lineSideDir) 
        {
            var allLineLayouts = new List<LineLayout>();
            if (wallPolylines == null || wallPolylines.Count < 1)
                return allLineLayouts;
            for (int i = 0; i < wallPolylines.Count; i++) 
            {
                var lineLayouts = GetPolylineCanLayoutLine(wallPolylines[i], lineSp, lineEp, lineSideDir);
                if (null == lineLayouts || lineLayouts.Count < 1)
                    continue;
                //进一步判断是否和其它墙线共线
                List<Line> otherWallLines = new List<Line>();
                for (int j = 0; j < wallPolylines.Count; j++)
                {
                    if (i == j)
                        continue;
                    var lineOutDir = EmgPilotLampUtil.PolylineOutDir(wallPolylines[j]);
                    otherWallLines.AddRange(lineOutDir.Select(c => c.Key).ToList());
                }
                if (otherWallLines.Count < 0) 
                {
                    allLineLayouts.AddRange(lineLayouts);
                    continue;
                }
                foreach (var item in lineLayouts)
                {
                    if (!EmgPilotLampUtil.LineIsCollinear(item.canLayoutLine.StartPoint, item.canLayoutLine.EndPoint, otherWallLines,10))
                        allLineLayouts.Add(item);
                }
            }
            return allLineLayouts;
        }
        /// <summary>
        /// 根据线，和轮廓获取可以布置的线和点的信息
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="lineSp"></param>
        /// <param name="lineEp"></param>
        /// <param name="lineSideDir"></param>
        /// <returns></returns>
        private List<LineLayout> GetPolylineCanLayoutLine(Polyline pline,Point3d lineSp,Point3d lineEp,Vector3d lineSideDir) 
        {
            List<LineLayout> lineLayouts = new List<LineLayout>();
            var lineLength = lineSp.DistanceTo(lineEp);
            var lineDir = (lineEp - lineSp).GetNormal();
            var otherDir = lineDir.CrossProduct(Vector3d.ZAxis);
            //多段线有可以合并的线，这里如果没有合并，如果有些是多段线
            var polyline = pline.DPSimplify(2);
            var lineOutDir = EmgPilotLampUtil.PolylineOutDir(polyline);
            if (null == lineOutDir)
                return lineLayouts;
            foreach (var item in lineOutDir) 
            {
                var outDir = item.Value;
                var dot = outDir.DotProduct(lineSideDir);
                if (dot > -0.3)
                    continue;
                var sp = item.Key.StartPoint;
                var ep = item.Key.EndPoint;
                var prjSp = EmgPilotLampUtil.PointToLine(sp, lineSp, lineDir);
                var prjEp = EmgPilotLampUtil.PointToLine(ep, lineSp, lineDir);
                if (prjSp.DistanceTo(prjEp) < 10)
                    continue;
                if (!EmgPilotLampUtil.LineIsCollinear(lineSp, lineEp, prjSp, prjEp,out List<Point3d> collPts))
                    continue;
                var line1 = new Line(prjSp, prjSp + otherDir);
                var line2 = new Line(prjEp, prjEp + otherDir);
                var intersectPtSp = line1.Intersection(item.Key, Intersect.ExtendBoth).ToAcGePoint3d();
                var intersectPtEp = line2.Intersection(item.Key, Intersect.ExtendBoth).ToAcGePoint3d();
                if (!EmgPilotLampUtil.PointInLine(intersectPtSp, item.Key) || !EmgPilotLampUtil.PointInLine(intersectPtEp, item.Key))
                    continue;
                if (intersectPtSp.DistanceTo(intersectPtEp) < 350)
                    continue;

                LineLayout lineLayout = new LineLayout();
                lineLayout.baseLine = item.Key;
                lineLayout.canLayoutLine = new Line(intersectPtSp, intersectPtEp);
                lineLayout.sideDirection = item.Value;
                lineLayout.layoutDirection = (intersectPtEp - intersectPtSp).GetNormal();
                lineLayouts.Add(lineLayout);
            }
            return lineLayouts;
        }

        private Point3d? GetLineLayoutPoint(Line layoutLine, Point3d pt,  Point3d startPoint)
        {
            Point3d sPt = layoutLine.StartPoint;
            Point3d ePt = layoutLine.EndPoint;
            Vector3d moveDir = (ePt - sPt).GetNormal();
            if (layoutLine.Length < 350)
                return null;
            //计算排布点
            var layoutPt = EmgPilotLampUtil.LineCloseNearPoint(layoutLine,pt);
            if (sPt.DistanceTo(layoutPt) < minWidth)
            {
                layoutPt = layoutPt + moveDir * (minWidth / 2 - sPt.DistanceTo(layoutPt));
            }
            if (ePt.DistanceTo(layoutPt) < minWidth)
            {
                layoutPt = layoutPt - moveDir * (minWidth / 2 - ePt.DistanceTo(layoutPt));
            }
            return layoutPt;
        }
        bool CheckLayoutPointInMaxPolyline(Point3d layoutPoint,Vector3d outVector,double outEx=100) 
        {
            if (_maxPolyline == null)
                return true;
            var checkPoint = layoutPoint + outVector.MultiplyBy(outEx);
            return _maxPolyline.Contains(checkPoint);
        }
        bool CheckLayoutLineCanLayout(Point3d layoutPoint,Line layoutLine, Vector3d outVector,Vector3d lightDir,double outEx = 100) 
        {
            double lineLength = layoutLine.Length;
            if (lineLength < minLayoutLineLength && Math.Abs(lineLength - minLayoutLineLength) > 10)
                return false;
            var sp = layoutPoint + lightDir.MultiplyBy(minLayoutLineLength / 2);
            var ep = layoutPoint - lightDir.MultiplyBy(minLayoutLineLength/2);
            var maxInnerLines = _maxPolyline.Trim(new Line(sp, ep)).OfType<Curve>();
            var moveSp = sp + outVector.MultiplyBy(outEx);
            var moveEp = ep + outVector.MultiplyBy(outEx);
            //进一步偏移看看是否在轮廓内
            if (maxInnerLines.Count() < 1)
                maxInnerLines = _maxPolyline.Trim(new Line(moveSp, moveEp)).OfType<Curve>();
            if (maxInnerLines.Count() < 1)
                return false;
            var length = maxInnerLines.First().GetLength();
            if (length < minLayoutLineLength && Math.Abs(length - minLayoutLineLength) > 10)
                return false; 
            foreach (var item in _innerPolylines) 
            {
                maxInnerLines = item.Trim(new Line(moveSp, moveEp),true).OfType<Curve>();
                //进一步偏移看看是否在内轮廓外
                if (maxInnerLines.Count() < 1)
                    maxInnerLines = item.Trim(new Line(moveSp, moveEp),true).OfType<Curve>();
                if (maxInnerLines.Count() < 1)
                    return false;
                length = maxInnerLines.First().GetLength();
                if (length < minLayoutLineLength && Math.Abs(length - minLayoutLineLength) > 10)
                    return false;
            }
            return true;
        }
    }
    class LineLayout
    {
        /// <summary>
        /// 轮廓原始线
        /// </summary>
        public Line baseLine { get; set; }
        /// <summary>
        /// 原始线上可布置区域线
        /// </summary>
        public Line canLayoutLine { get; set; }
        /// <summary>
        /// 线的外侧朝向
        /// </summary>
        public Vector3d sideDirection { get; set; }
        /// <summary>
        /// 计算出来的排布点
        /// </summary>
        public Point3d layoutPoint { get; set; }
        /// <summary>
        /// 实际的排布指向，和线的方向平行
        /// </summary>
        public Vector3d layoutDirection { get; set; }
    }
}
