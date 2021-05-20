using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    class LayoutToStructure
    {
        readonly double minWidth = 500;

        /// <summary>
        /// 计算布置点和方向
        /// </summary>
        /// <param name="layoutPts"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Dictionary<Point3d, Vector3d> GetLayoutStructPt(List<Point3d> layoutPts, List<Polyline> columns, List<Polyline> walls, Vector3d dir,bool spCalc=true)
        {
            Dictionary<Point3d, Vector3d> ptDic = new Dictionary<Point3d, Vector3d>();
            if (layoutPts == null || layoutPts.Count < 1)
                return null;
            //这里认为传入的点是同一根线，且方向一致
            Point3d sp = layoutPts.First();
            Point3d ep = layoutPts.Last();
            double length = layoutPts.First().DistanceTo(layoutPts.Last());
            List<Polyline> wallsInPoints = new List<Polyline>();
            List<Polyline> columnsInPoints = new List<Polyline>();

            foreach (var pt in layoutPts)
            {
                if (!spCalc && pt.DistanceTo(sp) < 10)
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
                    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                    if (null != layoutInfo && layoutInfo.HasValue) 
                    {
                        var prjPt = EmgPilotLampUtil.PointToLine(layoutInfo.Value.Item1, new Line(sp, ep));
                        if(length>3500 && ((prjPt.DistanceTo(sp) > 500 && prjPt.DistanceTo(sp) < 3500) || (prjPt.DistanceTo(pt) > length*2/3 && pt.DistanceTo(sp)> length * 2 / 3) 
                            || (prjPt.DistanceTo(pt)>3500 &&pt.DistanceTo(sp)<100)))
                            layoutInfo = null;
                    }
                    
                }
                if (null == layoutInfo )
                {
                    if (wall.Count > 0)
                    {
                        (Point3d, Vector3d)? wallLayout = null;
                        var wallDisToLine = double.MaxValue;
                        var wallMinDis = double.MinValue;
                        var wallMaxDis = double.MaxValue;
                        foreach (var item in wall) 
                        {
                            var tempWall = GetWallLayoutPoint(item.Key, pt, dir, sp, length);
                            if (tempWall == null)
                                continue;
                            var wallPrj = EmgPilotLampUtil.PointToLine(tempWall.Value.Item1, new Line(sp, ep));
                            var disToLine = wallPrj.DistanceTo(tempWall.Value.Item1);
                            var prjDis = wallPrj.DistanceTo(sp);
                            if (pt.DistanceTo(sp) < 10)
                            {
                                //起点
                                if (prjDis  > length)
                                {
                                    tempWall = null;
                                    continue;
                                }
                                if (wallMaxDis > prjDis)
                                {
                                    wallMaxDis = prjDis;
                                    wallLayout = tempWall;
                                    //break;
                                }
                            }
                            else 
                            {
                                //终点
                                if (prjDis - length > 10)
                                {
                                    tempWall = null;
                                    continue;
                                }
                                if (Math.Abs(wallMinDis - prjDis) < 100)
                                {
                                    if (wallDisToLine > disToLine) 
                                    {
                                        wallMinDis = prjDis;
                                        wallLayout = tempWall;
                                        wallDisToLine = disToLine;
                                    }
                                }
                                else if( wallMinDis < prjDis)
                                {
                                    wallMinDis = prjDis;
                                    wallLayout = tempWall;
                                    wallDisToLine = disToLine;
                                }
                            }
                            
                        }
                        if (columns.Count > 0)
                        {
                            var dis = 0.0;
                            var colLayout = GetColumnLayoutPoint(column.First().Key, pt, dir);
                            if (wallLayout != null && colLayout != null)
                            {
                                var colPrj = EmgPilotLampUtil.PointToLine(colLayout.Value.Item1, new Line(sp, ep));
                                var wallPrj = EmgPilotLampUtil.PointToLine(wallLayout.Value.Item1, new Line(sp, ep));
                                dis = colPrj.DistanceTo(pt) - wallPrj.DistanceTo(pt);
                            }
                            if (Math.Abs(dis) < 2000)
                                layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                            else if (column.First().Value < wall.First().Value)
                                layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                            else
                            {
                                layoutInfo = wallLayout;
                                if (layoutInfo == null && column.Count > 0)
                                {
                                    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                                }
                            }
                        }
                        else
                        {
                            layoutInfo = wallLayout;//GetWallLayoutPoint(wall.First().Key, pt, dir,sp,length);
                        }
                    }
                    else if (column.Count > 0)
                    {
                        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                    }
                }

                //优先找近的
                //if (wall.Count <= 0 || (column.Count > 0 && column.First().Value < wall.First().Value))
                //{
                //    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //}
                //else
                //{
                //    layoutInfo = GetWallLayoutPoint(wall.First().Key, pt, dir);
                //    if (layoutInfo == null && column.Count > 0)
                //    {
                //        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //    }
                //}
                //优先找近的，如果近的是墙在判断两米内是否有柱子，如果有柱子，柱子优先
                //if (wall.Count > 0 && column.Count > 0)
                //{
                //    var dis = column.First().Value - wall.First().Value;
                //    if (Math.Abs(dis) < 2000)
                //        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //    else if (column.First().Value < wall.First().Value)
                //        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //    else
                //    {
                //        layoutInfo = GetWallLayoutPoint(wall.First().Key, pt, dir);
                //        if (layoutInfo == null && column.Count > 0)
                //        {
                //            layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //        }
                //    }
                //}
                //else if (wall.Count < 1)
                //{
                //    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //}
                //else 
                //{
                //    layoutInfo = GetWallLayoutPoint(wall.First().Key, pt, dir);
                //    if (layoutInfo == null && column.Count > 0)
                //    {
                //        layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                //    }
                //}
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
            var layoutLine = GetLayoutStructLine(column, pt, dir, out Point3d closetPt);
            if (layoutLine == null)
            {
                return null;
            }

            Point3d sPt = layoutLine.StartPoint;
            Point3d ePt = layoutLine.EndPoint;

            //计算排布点
            var layoutPt = new Point3d((sPt.X + ePt.X) / 2, (sPt.Y + ePt.Y) / 2, 0);

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
        /// 找到构建的布置边
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Line GetLayoutStructLine(Polyline polyline, Point3d pt, Vector3d dir, out Point3d layoutPt)
        {
            var closetPt = polyline.GetClosestPointTo(pt, false);
            layoutPt = closetPt;
            List<Line> lines = new List<Line>();
            //多段线有可以合并的线，这里如果没有合并，如果有些是多段线
            polyline = polyline.DPSimplify(2);
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                lines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(dir);
            var layoutLine = lines.Where(x => x.ToCurve3d().IsOn(closetPt, new Tolerance(1, 1)))
                .Where(x =>
                {
                    var xDir = (x.EndPoint - x.StartPoint).GetNormal();
                    return Math.Abs(otherDir.DotProduct(xDir)) < Math.Abs(dir.DotProduct(xDir));
                }).FirstOrDefault();

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
            var maxLength = lines.Max(c => c.Length);
            var prjDis = prjPoint.DistanceTo(startPoint);
            if ( prjDis> maxDis || (maxDis>prjDis && Math.Abs(prjDis - maxDis) <10))
            {
                //近点不符合要求，进一步计算
                var dis = double.MinValue;
                var disX = double.MaxValue;
                Line tempLine = null;
                Point3d? tempPoint = null;
                foreach (var li in lines) 
                {
                    var xDir = (li.EndPoint - li.StartPoint).GetNormal();
                    if (Math.Abs(otherDir.DotProduct(xDir)) > Math.Abs(dir.DotProduct(xDir)))
                        continue;
                    var outDir = Vector3d.ZAxis.CrossProduct(xDir);
                    var temp = li.GetClosestPointTo(pt, false);
                    prjPoint = EmgPilotLampUtil.PointToLine(temp, startPoint, dir);
                    var tempdis = prjPoint.DistanceTo(startPoint);
                    var tempDir = (temp - prjPoint).GetNormal();
                    var tempDot = tempDir.DotProduct(outDir);
                    if (tempDot > 0.1)
                        continue;
                    if (Math.Abs(tempdis - maxDis) > 1)
                        continue;
                    if (dis <= tempdis && disX>Math.Abs(tempDot)) 
                    {
                        tempPoint = temp;
                        tempLine = li;
                        dis = tempdis;
                    }
                }
                if (null != tempLine)
                {
                    layoutPt = tempPoint.Value;
                    layoutLine = tempLine;
                }
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
        
    }
}
