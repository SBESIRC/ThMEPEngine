using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPLighting.EmgLight.Assistant;

namespace ThMEPLighting.EmgLight.Service
{
    public class LayoutEmgLightService
    {
        private List<List<Polyline>> usefulColumns;
        private List<List<Polyline>> usefulWalls;
        private List<List<Polyline>> usefulStruct;
        private List<Line> lane;
        private Dictionary<Polyline, Point3d> dictStructureCenter = new Dictionary<Polyline, Point3d>();
        private Dictionary<Polyline, Point3d> dictStructureCenterInLaneCoor = new Dictionary<Polyline, Point3d>();
        private List<Line> laneTrans = new List<Line>();
        private Polyline frame;

        public LayoutEmgLightService(List<List<Polyline>> usefulColumns, List<List<Polyline>> usefulWalls, List<Line> lane, Polyline frame, int TolLightRangeMin, int TolLightRangeMax)
        {
            this.usefulColumns = usefulColumns;
            this.usefulWalls = usefulWalls;
            this.lane = lane;
            this.frame = frame;

            usefulStruct = new List<List<Polyline>>();
            usefulStruct.Add(new List<Polyline>());
            usefulStruct[0].AddRange(usefulColumns[0]);
            usefulStruct[0].AddRange(usefulWalls[0]);
            usefulStruct.Add(new List<Polyline>());
            usefulStruct[1].AddRange(usefulColumns[1]);
            usefulStruct[1].AddRange(usefulWalls[1]);

            //必须先构建中心点
            BuildStructCenter(usefulStruct);
            BuildStructCenterInLaneCoor(usefulStruct);

            this.usefulColumns[0] = OrderingStruct(this.usefulColumns[0]);
            this.usefulColumns[1] = OrderingStruct(this.usefulColumns[1]);

            this.usefulWalls[0] = OrderingStruct(this.usefulWalls[0]);
            this.usefulWalls[1] = OrderingStruct(this.usefulWalls[1]);

            usefulStruct[0] = OrderingStruct(usefulStruct[0]);
            usefulStruct[1] = OrderingStruct(usefulStruct[1]);

        }

        public List<List<Polyline>> UsefulColumns
        {
            get
            {
                return usefulColumns;
            }
        }

        public List<List<Polyline>> UsefulWalls
        {
            get
            {
                return usefulWalls;
            }
        }

        public List<List<Polyline>> UsefulStruct
        {
            get
            {
                return usefulStruct;
            }
        }

        /// <summary>
        /// 将构建中点当布置点,计算布置方向,加入最后的list
        /// </summary>
        /// <param name="layoutList"></param>
        /// <param name="lane"></param>
        /// <param name="layoutPtInfo"></param>
        public void AddLayoutStructPt(List<Polyline> layoutList, List<Line> lane, ref Dictionary<Polyline, (Point3d, Vector3d)> layoutPtInfo)
        {
            (Point3d, Vector3d) layoutInfo;
            foreach (var structure in layoutList)
            {
                if (structure != null && layoutPtInfo.ContainsKey(structure) == false)
                {
                    layoutInfo = GetLayoutPoint(structure);
                    layoutPtInfo.Add(structure, layoutInfo);
                }
            }
        }

        /// <summary>
        /// 计算构建上排布和方向
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public (Point3d, Vector3d) GetLayoutPoint(Polyline structure)
        {

            //计算排布点
            var layoutPt = getCenter(structure);

            //计算排布方向
            var StructDir = (structure.EndPoint - structure.StartPoint).GetNormal();
            var layoutDir = Vector3d.ZAxis.CrossProduct(StructDir);

            prjPtToLine(structure, out var prjPt);
            var compareDir = (prjPt - layoutPt).GetNormal();

            if (layoutDir.DotProduct(compareDir) < 0)
            {
                layoutDir = -layoutDir;
            }

            return (layoutPt, layoutDir);
        }

        /// <summary>
        /// 计算构建在车道线坐标系下的x坐标差
        /// </summary>
        /// <param name="structList"></param>
        /// <returns></returns>
        public List<double> GetColumnDistList(List<Polyline> structList)
        {
            List<double> distX = new List<double>();
            for (int i = 0; i < structList.Count - 1; i++)
            {
                distX.Add(Math.Abs(getCenterInLaneCoor(structList[i]).X - getCenterInLaneCoor(structList[i + 1]).X));
            }

            return distX;

        }

        /// <summary>
        /// 将构建沿车道线坐标系排序
        /// </summary>
        /// <param name="structList"></param>
        /// <returns></returns>
        private List<Polyline> OrderingStruct(List<Polyline> structList)
        {
            var orderedStruct = structList.OrderBy(x => getCenterInLaneCoor(x).X).ToList();
            return orderedStruct;
        }

        /// <summary>
        /// 将点pt转到lane坐标系内
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        private static Point3d TransformPointToLine(Point3d pt, List<Line> lane)
        {
            //getAngleTo根据右手定则旋转(一般逆时针)
            var rotationangle = Vector3d.XAxis.GetAngleTo((lane.Last().EndPoint - lane.First().StartPoint), Vector3d.ZAxis);
            Matrix3d matrix = Matrix3d.Displacement(lane.First().StartPoint.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            var transedPt = pt.TransformBy(matrix.Inverse());

            return transedPt;
        }

        /// <summary>
        /// 找到给定点投影到lanes尾的多线段和距离. 如果点在起点外,则返回投影到向前延长线到最末的距离和多线段.如果点在端点外,则返回点到端点的距离(负数)和多线段
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="PolylineToEnd"></param>
        /// <returns></returns>
        public void prjPtToLineEnd(Polyline structure, out Polyline PolylineToEnd)
        {
            Point3d prjPt;
            PolylineToEnd = new Polyline();
            int timeToCheck = 0;

            Point3d centerPtTrans;
            Point3d centerPt;
            if (laneTrans.Count == 0)
            {
                laneTrans = lane.Select(x => new Line(TransformPointToLine(x.StartPoint, lane), TransformPointToLine(x.EndPoint, lane))).ToList();
            }

            centerPt = getCenter(structure);
            centerPtTrans = getCenterInLaneCoor(structure);

            if (centerPtTrans.X < laneTrans.First().StartPoint.X)
            {
                prjPt = lane[0].GetClosestPointTo(centerPt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                foreach (var l in lane)
                {
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                }
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lane.Last().EndPoint.ToPoint2D(), 0, 0, 0);
            }
            else if (centerPtTrans.X > laneTrans.Last().EndPoint.X)
            {
                prjPt = lane.Last().GetClosestPointTo(centerPt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lane.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
            }
            else
            {
                for (int i = 0; i < lane.Count; i++)
                {
                    if (timeToCheck == 0 && laneTrans[i].StartPoint.X <= centerPtTrans.X && centerPtTrans.X <= laneTrans[i].EndPoint.X)
                    {
                        prjPt = lane[i].GetClosestPointTo(centerPt, false);
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                        timeToCheck = 1;
                    }
                    else if (timeToCheck > 0)
                    {
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lane[i].StartPoint.ToPoint2D(), 0, 0, 0);
                    }
                }
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, lane.Last().EndPoint.ToPoint2D(), 0, 0, 0);
            }

        }

        /// <summary>
        /// 找点到线的投影点,如果点在线外,则返回延长线上的投影点
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        public void prjPtToLine(Polyline structure, out Point3d prjPt)
        {
            prjPt = new Point3d();

            Point3d centerPtTrans;
            Point3d centerPt;
            if (laneTrans.Count == 0)
            {
                laneTrans = lane.Select(x => new Line(TransformPointToLine(x.StartPoint, lane), TransformPointToLine(x.EndPoint, lane))).ToList();
            }

            centerPt = getCenter(structure);
            centerPtTrans = getCenterInLaneCoor(structure);

            if (centerPtTrans.X < laneTrans.First().StartPoint.X)
            {
                prjPt = lane[0].GetClosestPointTo(centerPt, true);
            }
            else if (centerPtTrans.X > laneTrans.Last().EndPoint.X)
            {
                prjPt = lane.Last().GetClosestPointTo(centerPt, true);
            }
            else
            {
                for (int i = 0; i < lane.Count; i++)
                {
                    if (laneTrans[i].StartPoint.X <= centerPtTrans.X && centerPtTrans.X <= laneTrans[i].EndPoint.X)
                    {
                        prjPt = lane[i].GetClosestPointTo(centerPt, false);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 找点到线的投影点,如果点在线外,则返回延长线上的投影点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        private double distToLine(Point3d pt, out Point3d prjPt)
        {
            double distProject = -1;
            var ptNew = TransformPointToLine(pt, lane);
            prjPt = new Point3d();

            if (laneTrans.Count == 0)
            {
                laneTrans = lane.Select(x => new Line(TransformPointToLine(x.StartPoint, lane), TransformPointToLine(x.EndPoint, lane))).ToList();
            }
            if (ptNew.X < laneTrans.First().StartPoint.X)
            {
                prjPt = lane[0].GetClosestPointTo(pt, true);
            }
            else if (ptNew.X > laneTrans.Last().EndPoint.X)
            {
                prjPt = lane.Last().GetClosestPointTo(pt, true);
            }
            else
            {
                for (int i = 0; i < lane.Count; i++)
                {
                    if (laneTrans[i].StartPoint.X <= ptNew.X && ptNew.X <= laneTrans[i].EndPoint.X)
                    {
                        prjPt = lane[i].GetClosestPointTo(pt, false);
                        break;
                    }
                }
            }

            distProject = prjPt.DistanceTo(pt);
            return distProject;

        }

        /// <summary>
        /// 两点中点到线的投影点
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="prjMidPt"></param>
        public void findMidPointOnLine(Point3d pt1, Point3d pt2, out Point3d prjMidPt)
        {
            Point3d midPoint;

            midPoint = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
            distToLine(midPoint, out prjMidPt);

        }

        /// <summary>
        /// 找距离pt最近的构建
        /// </summary>
        /// <param name="structList"></param>
        /// <param name="Pt"></param>
        /// <param name="closestStruct"></param>
        private static void findClosestStruct(List<Polyline> structList, Point3d Pt, out Polyline closestStruct)
        {
            double minDist = 10000;
            closestStruct = null;

            for (int i = 0; i < structList.Count; i++)
            {
                if (structList[i].Distance(Pt) <= minDist)
                {
                    minDist = structList[i].Distance(Pt);
                    closestStruct = structList[i];
                }
            }
        }

        /// <summary>
        /// 找ExtendPoly框内且离Pt点最近的构建
        /// </summary>
        /// <param name="structList"></param>
        /// <param name="Pt"></param>
        /// <param name="ExtendPoly"></param>
        /// <param name="closestStruct"></param>
        /// <returns></returns>
        public bool FindClosestStructToPt(List<Polyline> structList, Point3d Pt, Polyline ExtendPoly, out Polyline closestStruct)
        {
            bool bReturn = false;
            closestStruct = null;

            FindPolyInExtendPoly(structList, ExtendPoly, out var inExtendStruct);
            if (inExtendStruct.Count > 0)
            {
                //框内有位置布灯
                findClosestStruct(inExtendStruct, Pt, out closestStruct);
                bReturn = true;
            }
            else
            {
                bReturn = false;
            }
            return bReturn;
        }

        /// <summary>
        /// 判断框内是否有构建
        /// </summary>
        /// <param name="structList"></param>
        /// <param name="ExtendPoly"></param>
        /// <param name="inExtendStruct"></param>
        private static void FindPolyInExtendPoly(List<Polyline> structList, Polyline ExtendPoly, out List<Polyline> inExtendStruct)
        {
            inExtendStruct = structList.Where(x =>
           {
               return (ExtendPoly.Contains(x) || ExtendPoly.Intersects(x));
           }).ToList();

        }

        /// <summary>
        /// 构建中心点坐标
        /// </summary>
        /// <param name="structList"></param>
        private void BuildStructCenter(List<List<Polyline>> structList)
        {
            foreach (var structureSide in structList)
            {
                foreach (var s in structureSide)
                {
                    if (dictStructureCenter.ContainsKey(s) == false)
                    {
                        dictStructureCenter.Add(s, StructUtils.GetStructCenter(s));
                    }
                }
            }
        }

        /// <summary>
        /// 构建中心点在车道线坐标系的坐标
        /// </summary>
        /// <param name="structList"></param>
        private void BuildStructCenterInLaneCoor(List<List<Polyline>> structList)
        {
            foreach (var structureSide in structList)
            {
                foreach (var s in structureSide)
                {
                    if (dictStructureCenterInLaneCoor.ContainsKey(s) == false)
                    {
                        dictStructureCenterInLaneCoor.Add(s, TransformPointToLine(dictStructureCenter[s], lane));
                    }
                }
            }
        }

        /// <summary>
        /// 找出车道线起点前后的已布构建,并按车道线排序
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="TolLane"></param>
        /// <returns></returns>
        public List<List<Polyline>> BuildHeadLayout(List<Polyline> layout, double TolLane)
        {
            //车道线往前做框buffer
            var ExtendLineList = StructureServiceLight.LaneHeadExtend(lane, EmgLightCommon.TolLightRangeMax);
            var FilteredLayout = StructureServiceLight.GetStruct(ExtendLineList, layout, TolLane);
            var importLayout = StructureServiceLight.SeparateColumnsByLine(FilteredLayout, ExtendLineList, TolLane);

            var extendPoly = StructUtils.ExpandLine(ExtendLineList[0], TolLane, 0, TolLane, 0);
            DrawUtils.ShowGeometry(extendPoly, EmgLightCommon.LayerLaneHead, Color.FromRgb(141, 118, 12));

            BuildStructCenter(importLayout);
            BuildStructCenterInLaneCoor(importLayout);

            importLayout[0] = OrderingStruct(importLayout[0]);
            importLayout[1] = OrderingStruct(importLayout[1]);

            return importLayout;
        }

        /// <summary>
        /// 计算构建中点在车道线坐标系下的坐标.并存入dictionary
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public Point3d getCenterInLaneCoor(Polyline structure)
        {
            Point3d ptTrans;
            Point3d centerPt;

            if (dictStructureCenterInLaneCoor.TryGetValue(structure, out ptTrans) == false)
            {
                centerPt = getCenter(structure);

                ptTrans = TransformPointToLine(centerPt, lane);
                dictStructureCenterInLaneCoor.Add(structure, ptTrans);
            }
            return ptTrans;
        }

        /// <summary>
        /// 计算构建中点并存入dictionary
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public Point3d getCenter(Polyline structure)
        {
            Point3d centerPt;

            if (dictStructureCenter.TryGetValue(structure, out centerPt) == false)
            {
                centerPt = StructUtils.GetStructCenter(structure);
                dictStructureCenter.Add(structure, centerPt);
            }

            return centerPt;
        }

        /// <summary>
        /// 滤掉对于车道线重叠的构建
        /// </summary>
        public void filterOverlapStruc()
        {

            for (int i = 0; i < usefulStruct.Count; i++)
            {
                List<Polyline> removeList = new List<Polyline>();
                for (int curr = 0; curr < usefulStruct[i].Count; curr++)
                {
                    for (int j = curr; j < usefulStruct[i].Count; j++)
                    {
                        if (j != curr && removeList.Contains(usefulStruct[i][j]) == false)
                        {
                            var currCenter = getCenterInLaneCoor(usefulStruct[i][curr]);
                            var closeStart = TransformPointToLine(usefulStruct[i][j].StartPoint, lane);
                            var closeEnd = TransformPointToLine(usefulStruct[i][j].EndPoint, lane);

                            if ((closeStart.X <= currCenter.X && currCenter.X <= closeEnd.X) || (closeEnd.X <= currCenter.X && currCenter.X <= closeStart.X))
                            {
                                if (Math.Abs(currCenter.Y) > Math.Abs(getCenterInLaneCoor(usefulStruct[i][j]).Y))
                                {
                                    removeList.Add(usefulStruct[i][curr]);
                                }
                            }

                            currCenter = getCenterInLaneCoor(usefulStruct[i][j]);
                            closeStart = TransformPointToLine(usefulStruct[i][curr].StartPoint, lane);
                            closeEnd = TransformPointToLine(usefulStruct[i][curr].EndPoint, lane);
                            if ((closeStart.X <= currCenter.X && currCenter.X <= closeEnd.X) || (closeEnd.X <= currCenter.X && currCenter.X <= closeStart.X))
                            {
                                if (Math.Abs(currCenter.Y) > Math.Abs(getCenterInLaneCoor(usefulStruct[i][curr]).Y))
                                {
                                    removeList.Add(usefulStruct[i][j]);
                                }
                            }
                        }

                    }
                }
                foreach (var removeStru in removeList)
                {
                    usefulColumns[i].Remove(removeStru);
                    usefulWalls[i].Remove(removeStru);
                    usefulStruct[i].Remove(removeStru);
                }
            }
        }

        /// <summary>
        /// 过滤对于车道线防火墙凹点外的构建
        /// </summary>
        public void filterStrucBehindFrame()
        {
            List<Polyline> removeList = new List<Polyline>();
            for (int i = 0; i < usefulStruct.Count; i++)
            {
                var layoutInfo = usefulStruct[i].Where(x =>
                {
                    Point3dCollection pts = new Point3dCollection();
                    //选不在防火墙凹后的
                    prjPtToLine(x, out var prjPt);
                    Line l = new Line(prjPt, getCenter(x));
                    l.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    return pts.Count > 0;
                }).ToList();

                foreach (var removeStru in layoutInfo)
                {
                    usefulStruct[i].Remove(removeStru);
                    usefulColumns[i].Remove(removeStru);
                    usefulWalls[i].Remove(removeStru);
                }
            }
        }

        /// <summary>
        /// 过滤防火墙相交或不在防火墙内的构建
        /// </summary>
        public void getInsideFramePart()
        {
            List<Polyline> removeList = new List<Polyline>();

            for (int i = 0; i < usefulStruct.Count; i++)
            {
                //选与防火框不相交且在防火框内
                var layoutInfo = usefulStruct[i].Where(x =>
                {
                    Point3dCollection pts = new Point3dCollection();
                    x.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    return pts.Count > 0 || frame.Contains(x) == false;

                }).ToList();

                foreach (var removeStru in layoutInfo)
                {
                    usefulStruct[i].Remove(removeStru);
                    usefulColumns[i].Remove(removeStru);
                    usefulWalls[i].Remove(removeStru);
                }
            }

        }


    }
}
