﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast
{
    public class StructureLayoutService  //根据车道线上的排布点计算出构建上广播的安装位置
    {
        readonly double broadcastTol = 150;   //广播图块宽度，避免墙上安装点安装不下广播

        /// <summary>
        /// 计算布置点和方向
        /// </summary>
        /// <param name="layoutPts"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Dictionary<Point3d, Vector3d> GetLayoutStructPt(List<Point3d> layoutPts, List<Polyline> columns, List<Polyline> walls, Vector3d dir)
        {
            Dictionary<Point3d, Vector3d> ptDic = new Dictionary<Point3d, Vector3d>();
            foreach (var pt in layoutPts)
            {
                var column = columns.Distinct().ToDictionary(x => x, y => y.Distance(pt)).OrderBy(x => x.Value).ToList();
                var wall = walls.Distinct().ToDictionary(x => x, y => y.Distance(pt)).OrderBy(x => x.Value).ToList();
                if (column.Count <= 0 && wall.Count <= 0)
                {
                    return null;
                }

                (Point3d, Vector3d)? layoutInfo = null;
                if (wall.Count <= 0 || (column.Count > 0 && column.First().Value < wall.First().Value))
                {
                    layoutInfo = GetColumnLayoutPoint(column.First().Key, pt, dir);
                }
                else
                {
                    layoutInfo = GetWallLayoutPoint(wall.First().Key, pt, dir);
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
        private (Point3d, Vector3d)? GetWallLayoutPoint(Polyline wall, Point3d pt, Vector3d dir)
        {
            var layoutLine = GetLayoutStructLine(wall, pt, dir, out Point3d closetPt);
            if (layoutLine == null)
            {
                return null;
            }

            Point3d sPt = layoutLine.StartPoint;
            Point3d ePt = layoutLine.EndPoint;
            Vector3d moveDir = (ePt - sPt).GetNormal();

            //计算排布点
            var layoutPt = closetPt;
            if (sPt.DistanceTo(layoutPt) < broadcastTol)
            {
                layoutPt = layoutPt + moveDir * (broadcastTol - sPt.DistanceTo(layoutPt));
            }
            if (ePt.DistanceTo(layoutPt) < broadcastTol)
            {
                layoutPt = layoutPt - moveDir * (broadcastTol - ePt.DistanceTo(layoutPt));
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
        private (Point3d, Vector3d)? GetColumnLayoutPoint(Polyline column, Point3d pt, Vector3d dir)
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
    }
}
