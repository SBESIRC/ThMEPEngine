﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPLighting.EmgLight.Model;
using ThMEPLighting.EmgLight.Common;

namespace ThMEPLighting.EmgLight.Service
{
    class StructFilterService
    {
        private ThLane m_thLane;
        private List<Polyline> m_columns;
        private List<Polyline> m_walls;

        public StructFilterService(ThLane thLane, List<Polyline> columns, List<Polyline> walls)
        {
            m_thLane = thLane;
            m_columns = columns;
            m_walls = walls;
        }

        public LayoutService getStructSeg()
        {
            //获取该车道线上的构建
            var closeColumn = GetStruct(m_columns, EmgLightCommon.TolLane);
            var closeWall = GetStruct(m_walls, EmgLightCommon.TolLane);

            DrawUtils.ShowGeometry(closeColumn, EmgLightCommon.LayerGetStruct, 1, 35);
            DrawUtils.ShowGeometry(closeWall, EmgLightCommon.LayerGetStruct, 92, 35);

            foreach (Line l in m_thLane.geom)
            {
                var linePoly = GeomUtils.ExpandLine(l, EmgLightCommon.TolLane, 0, EmgLightCommon.TolLane, 0);
                DrawUtils.ShowGeometry(linePoly, EmgLightCommon.LayerSeparatePoly, 44);
            }

            //打散构建并生成数据结构
            var columnSegment = StructureService.BrakeStructToStructSeg(closeColumn);
            var wallSegment = StructureService.BrakeStructToStructSeg(closeWall);

            DrawUtils.ShowGeometry(columnSegment.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructSeg, 1, 35);
            DrawUtils.ShowGeometry(wallSegment.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructSeg, 92, 35);

            //选取构建平行车道线的边
            var parallelColmuns = getStructureParallelPart(columnSegment);
            var parallelWalls = getStructureParallelPart(wallSegment);

            //破墙
            var brokeWall = StructureService.breakWall(parallelWalls, EmgLightCommon.TolBrakeWall);

            DrawUtils.ShowGeometry(parallelColmuns.Select(x => x.geom).ToList(), EmgLightCommon.LayerParallelStruct, 5, 35);
            DrawUtils.ShowGeometry(brokeWall.Select(x => x.geom).ToList(), EmgLightCommon.LayerParallelStruct, 5, 35);


            //将构建按车道线方向分成左(0)右(1)两边
            var usefulColumns = StructureService.SeparateStructByLine(parallelColmuns, m_thLane.geom, EmgLightCommon.TolLane);
            var usefulWalls = StructureService.SeparateStructByLine(brokeWall, m_thLane.geom, EmgLightCommon.TolLane);

            StructureService.removeDuplicateStruct(usefulColumns);
            StructureService.removeDuplicateStruct(usefulWalls);

            DrawUtils.ShowGeometry(usefulColumns[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, 161, 35);
            DrawUtils.ShowGeometry(usefulColumns[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, 161, 35);
            DrawUtils.ShowGeometry(usefulWalls[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, 11, 35);
            DrawUtils.ShowGeometry(usefulWalls[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, 11, 35);

            LayoutService layoutServer = new LayoutService(usefulColumns, usefulWalls, m_thLane);

            return layoutServer;

        }

        public void FilterStruct(LayoutService layoutServer, Polyline frame)
        {
            ////过滤掉很短的墙
            filterTooShortWall(layoutServer, EmgLightCommon.TolBrakeWall - 50);
            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[0].Select(x => x.geom).ToList(), "l4tooShort", 161, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[1].Select(x => x.geom).ToList(), "l4tooShort", 161, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[0].Select(x => x.geom).ToList(), "l4tooShort", 11, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[1].Select(x => x.geom).ToList(), "l4tooShort", 11, 35);

            ////滤掉重合部分
            filterOverlapStruc(layoutServer);
            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerOverlap, 161, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerOverlap, 161, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerOverlap, 11, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerOverlap, 11, 35);


            //过滤柱与墙交叉的部分
            FilterStructIntersect(layoutServer.UsefulColumns, m_walls, layoutServer, EmgLightCommon.TolIntersect);
            FilterStructIntersect(layoutServer.UsefulWalls, m_columns, layoutServer, EmgLightCommon.TolIntersect);

            DrawUtils.ShowGeometry(layoutServer.UsefulStruct[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerNotIntersectStruct, 140, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulStruct[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerNotIntersectStruct, 140, 35);

            ////滤掉框外边的部分
            getInsideFramePart(layoutServer, frame);
            DrawUtils.ShowGeometry(layoutServer.UsefulStruct[0].Select(x => x.geom).ToList(), "l61NotIntersectFrame", 140, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulStruct[1].Select(x => x.geom).ToList(), "l61NotIntersectFrame", 140, 35);

            ////滤掉框后边的部分
            filterStrucBehindFrame(layoutServer, frame);

            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, 161, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, 161, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, 11, 35);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, 11, 35);

        }

        /// <summary>
        /// 查找柱或墙平行于车道线
        /// </summary>
        /// <param name="structrues"></param>
        /// <param name="line"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<ThStruct> getStructureParallelPart(List<ThStruct> structureSegment)
        {
            ////平行于车道线的边
            //var structureLayoutSegment = structureSegment.Where(x =>
            //{
            //    bool bAngle = Math.Abs(m_thLane.dir.DotProduct(x.dir)) / (m_thLane.dir.Length * x.dir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
            //    return bAngle;
            //}).ToList();

            var tolAngle = 20;
            var structureLayoutSegment = new List<ThStruct>();

            //平行于车道线的边
            var orderLanePts = GeomUtils.orderLineListPts(m_thLane.geom, m_thLane.matrix);

            for (int i = 0; i < orderLanePts.Count - 1; i++)
            {
                var segMatrix = GeomUtils.getLineMatrix(orderLanePts.ElementAt(i).Key, orderLanePts.ElementAt(i + 1).Key);
                var lintPtN = orderLanePts.ElementAt(i + 1).Key.TransformBy(segMatrix.Inverse());

                foreach (var s in structureSegment)
                {
                    var ptTransTemp = s.centerPt.TransformBy(segMatrix.Inverse());

                    if (0 <= ptTransTemp.X && ptTransTemp.X <= lintPtN.X)
                    {
                        var segDir = (orderLanePts.ElementAt(i + 1).Key - orderLanePts.ElementAt(i).Key).GetNormal();
                        var cosSegStruct = Math.Abs(segDir.DotProduct(s.dir)) / (segDir.Length * s.dir.Length);

                        if (cosSegStruct > Math.Cos(tolAngle * Math.PI / 180))
                        {
                            structureLayoutSegment.Add(s);
                        }
                    }
                }
            }

            return structureLayoutSegment;
        }

        /// <summary>
        /// 查找车道线附近的构建
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private List<Polyline> GetStruct(List<Polyline> structs, double tol)
        {
            var resPolys = m_thLane.geom.SelectMany(x =>
            {
                var linePoly = GeomUtils.ExpandLine(x, tol, 0, tol, 0);
                return structs.Where(y =>
                {
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            return resPolys;
        }

        /// <summary>
        /// 过滤墙柱相交的构建
        /// </summary>
        /// <param name="structSeg"></param>
        /// <param name="structure"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private void FilterStructIntersect(List<List<ThStruct>> structList, List<Polyline> structure, LayoutService layoutServer, double tol)
        {
            List<ThStruct> intersectSegList = new List<ThStruct>();
            foreach (var structSeg in structList)
            {
                foreach (var seg in structSeg)
                {
                    var bInter = false;
                    var bContain = false;

                    foreach (var poly in structure)
                    {
                        if (seg.oriStructGeo == poly)
                        {
                            continue;
                        }
                        bContain = bContain || poly.Contains(seg.geom);
                        bInter = poly.Intersects(seg.geom);
                        if (bInter == true)
                        {
                            Point3dCollection pts = new Point3dCollection();
                            seg.geom.IntersectWith(poly, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);

                            if (pts.Count > 1)
                            {
                                bInter = true;
                                break;
                            }
                            else if (pts.Count == 1)
                            {
                                Point3d ptIn = new Point3d();
                                Point3d ptOut = new Point3d();
                                if (poly.Contains(seg.geom.StartPoint) == true)
                                {
                                    ptIn = seg.geom.StartPoint;
                                    ptOut = seg.geom.EndPoint;
                                }
                                if (poly.Contains(seg.geom.EndPoint) == true)
                                {
                                    ptIn = seg.geom.EndPoint;
                                    ptOut = seg.geom.StartPoint;
                                }

                                var lIn = new Line(pts[0], ptIn);
                                var lOut = new Line(pts[0], ptOut);

                                if (ptIn.X == 0)
                                {
                                    bInter = false;
                                }

                                else if (lIn.Length < EmgLightCommon.TolIntersect || lOut.Length >= EmgLightCommon.TolInterFilter)
                                {
                                    bInter = false;
                                }
                                else
                                {
                                    bInter = true;
                                    break;
                                }
                            }
                            else
                            {
                                bInter = false;
                            }
                        }
                    }

                    if (bInter == true || bContain == true)
                    {
                        intersectSegList.Add(seg);
                    }
                }
            }
         
            for (int i = 0; i < layoutServer.UsefulStruct.Count; i++)
            {
                foreach (var removeStru in intersectSegList)
                {
                    layoutServer.UsefulColumns[i].Remove(removeStru);
                    layoutServer.UsefulWalls[i].Remove(removeStru);
                    layoutServer.UsefulStruct[i].Remove(removeStru);
                }
            }
        }

        /// <summary>
        /// 滤掉对于车道线重叠的构建
        /// </summary>
        private void filterOverlapStruc(LayoutService layoutServer)
        {
            var orderLanePts = GeomUtils.orderLineListPts(layoutServer.thLane.geom, layoutServer.thLane.matrix);

            for (int i = 0; i < layoutServer.UsefulStruct.Count; i++)
            {
                Dictionary<ThStruct, ThStruct> removeList = new Dictionary<ThStruct, ThStruct>();
                for (int iLane = 0; iLane < orderLanePts.Count - 1; iLane++)
                {
                    var segMatrix = GeomUtils.getLineMatrix(orderLanePts.ElementAt(iLane).Key, orderLanePts.ElementAt(iLane + 1).Key);
                    var lintPtN = orderLanePts.ElementAt(iLane + 1).Key.TransformBy(segMatrix.Inverse());

                    for (int curr = 0; curr < layoutServer.UsefulStruct[i].Count; curr++)
                    {
                        for (int j = curr; j < layoutServer.UsefulStruct[i].Count; j++)
                        {
                            if (j != curr)
                            {
                                if (removeList.ContainsKey(layoutServer.UsefulStruct[i][curr]) == false)
                                {
                                    var currCenter = layoutServer.UsefulStruct[i][curr].centerPt.TransformBy(segMatrix.Inverse());
                                    var closeStart = layoutServer.UsefulStruct[i][j].geom.StartPoint.TransformBy(segMatrix.Inverse());
                                    var closeEnd = layoutServer.UsefulStruct[i][j].geom.EndPoint.TransformBy(segMatrix.Inverse());
                                    var closeCenter = layoutServer.UsefulStruct[i][j].centerPt .TransformBy(segMatrix.Inverse());

                                    if ((0 <= currCenter.X && currCenter.X <= lintPtN.X) &&
                                         ((closeStart.X <= currCenter.X && currCenter.X <= closeEnd.X) || (closeEnd.X <= currCenter.X && currCenter.X <= closeStart.X)))
                                    {
                                        if (Math.Abs(currCenter.Y) > Math.Abs(closeCenter.Y))
                                        {
                                            removeList.Add(layoutServer.UsefulStruct[i][curr], layoutServer.UsefulStruct[i][j]);
                                            var keys = removeList.Where(q => q.Value == layoutServer.UsefulStruct[i][curr]).Select(q => q.Key).ToList();
                                            keys.ForEach(k => removeList[k] = layoutServer.UsefulStruct[i][j]);
                                        }
                                    }
                                }
                                if (removeList.ContainsKey(layoutServer.UsefulStruct[i][j]) == false)
                                {
                                    var currCenter = layoutServer.UsefulStruct[i][j].centerPt.TransformBy(segMatrix.Inverse());
                                    var closeStart = layoutServer.UsefulStruct[i][curr].geom.StartPoint.TransformBy(segMatrix.Inverse());
                                    var closeEnd = layoutServer.UsefulStruct[i][curr].geom.EndPoint.TransformBy(segMatrix.Inverse());
                                    var closeCenter = layoutServer.UsefulStruct[i][curr].centerPt.TransformBy(segMatrix.Inverse());

                                    if ((0 <= currCenter.X && currCenter.X <= lintPtN.X) &&
                                        ((closeStart.X <= currCenter.X && currCenter.X <= closeEnd.X) || (closeEnd.X <= currCenter.X && currCenter.X <= closeStart.X)))
                                    {
                                        if (Math.Abs(currCenter.Y) > Math.Abs(closeCenter.Y))
                                        {
                                            removeList.Add(layoutServer.UsefulStruct[i][j], layoutServer.UsefulStruct[i][curr]);
                                            var keys = removeList.Where(q => q.Value == layoutServer.UsefulStruct[i][j]).Select(q => q.Key).ToList();
                                            keys.ForEach(k => removeList[k] = layoutServer.UsefulStruct[i][curr]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var removeStru in removeList)
                {
                    if (layoutServer.UsefulColumns[i].Remove(removeStru.Key) == true)
                    {

                        layoutServer.UsefulColumns[i].Add(removeStru.Value);
                    }

                    layoutServer.UsefulWalls[i].Remove(removeStru.Key);
                    layoutServer.UsefulStruct[i].Remove(removeStru.Key);
                }
                layoutServer.UsefulColumns[i] = layoutServer.OrderingStruct(layoutServer.UsefulColumns[i]);

            }
            StructureService.removeDuplicateStruct(layoutServer.UsefulColumns);
        }

        /// <summary>
        /// 过滤对于车道线防火墙凹点外的构建
        /// </summary>
        private void filterStrucBehindFrame(LayoutService layoutServer, Polyline frame)
        {
            List<ThStruct> removeList = new List<ThStruct>();
            for (int i = 0; i < layoutServer.UsefulStruct.Count; i++)
            {
                var layoutInfo = layoutServer.UsefulStruct[i].Where(x =>
                {
                    Point3dCollection pts = new Point3dCollection();
                    //选不在防火墙凹后的
                    layoutServer.prjPtToLine(x, out var prjPt);
                    Line l = new Line(prjPt, x.centerPt);
                    l.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    return pts.Count > 0;
                }).ToList();

                foreach (var removeStru in layoutInfo)
                {
                    layoutServer.UsefulStruct[i].Remove(removeStru);
                    layoutServer.UsefulColumns[i].Remove(removeStru);
                    layoutServer.UsefulWalls[i].Remove(removeStru);
                }
            }
        }

        /// <summary>
        /// 过滤防火墙相交或不在防火墙内的构建
        /// </summary>
        private void getInsideFramePart(LayoutService layoutServer, Polyline frame)
        {
            List<ThStruct> removeList = new List<ThStruct>();

            for (int i = 0; i < layoutServer.UsefulStruct.Count; i++)
            {
                //选与防火框不相交且在防火框内
                var layoutInfo = layoutServer.UsefulStruct[i].Where(x =>
                {
                    Point3dCollection pts = new Point3dCollection();
                    x.geom.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    return pts.Count > 0 || frame.Contains(x.geom) == false;

                }).ToList();

                foreach (var removeStru in layoutInfo)
                {
                    layoutServer.UsefulStruct[i].Remove(removeStru);
                    layoutServer.UsefulColumns[i].Remove(removeStru);
                    layoutServer.UsefulWalls[i].Remove(removeStru);
                }
            }
        }

        private void filterTooShortWall(LayoutService layoutServer, double tol)
        {
            //var tooShortWall = layoutServer.UsefulWalls[0].Where(x => x.geom.Length < tol).ToList();
            //layoutServer.UsefulWalls[0].RemoveAll(x => tooShortWall.Contains(x));
            //layoutServer.UsefulStruct[0].RemoveAll(x => tooShortWall.Contains(x));

            //tooShortWall = layoutServer.UsefulWalls[1].Where(x => x.geom.Length < tol).ToList();
            //layoutServer.UsefulWalls[1].RemoveAll(x => tooShortWall.Contains(x));
            //layoutServer.UsefulStruct[1].RemoveAll(x => tooShortWall.Contains(x));

            var tooShortWall = layoutServer.UsefulStruct[0].Where(x => x.geom.Length < tol).ToList();
            layoutServer.UsefulStruct[0].RemoveAll(x => tooShortWall.Contains(x));
            layoutServer.UsefulWalls[0].RemoveAll(x => tooShortWall.Contains(x));
            layoutServer.UsefulColumns[0].RemoveAll(x => tooShortWall.Contains(x));

            tooShortWall = layoutServer.UsefulStruct[1].Where(x => x.geom.Length < tol).ToList();
            layoutServer.UsefulStruct[1].RemoveAll(x => tooShortWall.Contains(x));
            layoutServer.UsefulWalls[1].RemoveAll(x => tooShortWall.Contains(x));
            layoutServer.UsefulColumns[1].RemoveAll(x => tooShortWall.Contains(x));
        }
    }


}
