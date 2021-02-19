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
using ThMEPLighting.EmgLight.Model;

namespace ThMEPLighting.EmgLight.Service
{
    public class LayoutService
    {
        private List<List<ThStruct>> m_usefulColumns;
        private List<List<ThStruct>> m_usefulWalls;
        private List<List<ThStruct>> m_usefulStruct;
        private ThLane m_lane;
        private Dictionary<ThStruct, Point3d> dictStructureCenterInLaneCoor = new Dictionary<ThStruct, Point3d>();

        public LayoutService(List<List<ThStruct>> usefulColumns, List<List<ThStruct>> usefulWalls, ThLane lane)
        {
            this.m_usefulColumns = usefulColumns;
            this.m_usefulWalls = usefulWalls;
            this.m_lane = lane;

            m_usefulStruct = new List<List<ThStruct>>();
            m_usefulStruct.Add(new List<ThStruct>());
            m_usefulStruct[0].AddRange(usefulColumns[0]);
            m_usefulStruct[0].AddRange(usefulWalls[0]);
            m_usefulStruct.Add(new List<ThStruct>());
            m_usefulStruct[1].AddRange(usefulColumns[1]);
            m_usefulStruct[1].AddRange(usefulWalls[1]);

            //必须先构建中心点
            //BuildStructCenter(usefulStruct);
            BuildStructCenterInLaneCoor(m_usefulStruct);

            this.m_usefulColumns[0] = OrderingStruct(this.m_usefulColumns[0]);
            this.m_usefulColumns[1] = OrderingStruct(this.m_usefulColumns[1]);

            this.m_usefulWalls[0] = OrderingStruct(this.m_usefulWalls[0]);
            this.m_usefulWalls[1] = OrderingStruct(this.m_usefulWalls[1]);

            m_usefulStruct[0] = OrderingStruct(m_usefulStruct[0]);
            m_usefulStruct[1] = OrderingStruct(m_usefulStruct[1]);

        }

        public List<List<ThStruct>> UsefulColumns
        {
            get
            {
                return m_usefulColumns;
            }
        }

        public List<List<ThStruct>> UsefulWalls
        {
            get
            {
                return m_usefulWalls;
            }
        }

        public List<List<ThStruct>> UsefulStruct
        {
            get
            {
                return m_usefulStruct;
            }
        }

        public ThLane thLane
        {
            get
            {
                return m_lane;
            }
        }

        /// <summary>
        /// 将构建中点当布置点,计算布置方向,加入最后的list
        /// </summary>
        /// <param name="layoutList"></param>
        /// <param name="lane"></param>
        /// <param name="layoutPtInfo"></param>
        public void AddLayoutStructPt(List<ThStruct> layoutList, ref Dictionary<Polyline, (Point3d, Vector3d)> layoutPtInfo)
        {

            foreach (var structure in layoutList)
            {
                if (structure != null && layoutPtInfo.ContainsKey(structure.geom) == false)
                {
                    var layoutInfo = GetLayoutDir(structure);
                    layoutPtInfo.Add(structure.geom, layoutInfo);
                }
            }
        }

        /// <summary>
        /// 计算构建上方向
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        private (Point3d, Vector3d) GetLayoutDir(ThStruct structure)
        {
            var layoutDir = Vector3d.ZAxis.CrossProduct(structure.dir );

            prjPtToLine(structure, out var prjPt);
            var compareDir = (prjPt - structure.centerPt).GetNormal();

            if (layoutDir.DotProduct(compareDir) < 0)
            {
                layoutDir = -layoutDir;
            }

            return (structure.centerPt, layoutDir);
        }

        /// <summary>
        /// 计算构建在车道线坐标系下的x坐标差
        /// </summary>
        /// <param name="structList"></param>
        /// <returns></returns>
        public List<double> GetColumnDistList(List<ThStruct> structList)
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
        public List<ThStruct> OrderingStruct(List<ThStruct> structList)
        {
            var orderedStruct = structList.OrderBy(x => getCenterInLaneCoor(x).X).ToList();
            return orderedStruct;
        }

        /// <summary>
        /// 找到给定点投影到lanes尾的多线段和距离. 如果点在起点外,则返回投影到向前延长线到最末的距离和多线段.如果点在端点外,则返回点到端点的距离(负数)和多线段
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="PolylineToEnd"></param>
        /// <returns></returns>
        public void prjPtToLineEnd(ThStruct structure, out Polyline PolylineToEnd)
        {
            Point3d prjPt;
            PolylineToEnd = new Polyline();
            int timeToCheck = 0;

            Point3d centerPtTrans;
            Point3d centerPt;
          
            centerPt = structure.centerPt;
            centerPtTrans = getCenterInLaneCoor(structure);

            if (centerPtTrans.X < m_lane.laneTrans.First().StartPoint.X)
            {
                prjPt = m_lane.geom.First().GetClosestPointTo(centerPt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                foreach (var l in m_lane.geom)
                {
                    PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, l.StartPoint.ToPoint2D(), 0, 0, 0);
                }
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, m_lane.geom.Last().EndPoint.ToPoint2D(), 0, 0, 0);
            }
            else if (centerPtTrans.X > m_lane.laneTrans.Last().EndPoint.X)
            {
                prjPt = m_lane.geom.Last().GetClosestPointTo(centerPt, true);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, m_lane.geom.Last().EndPoint.ToPoint2D(), 0, 0, 0);
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
            }
            else
            {
                for (int i = 0; i < m_lane.geom.Count; i++)
                {
                    if (timeToCheck == 0 && m_lane.laneTrans[i].StartPoint.X <= centerPtTrans.X && centerPtTrans.X <= m_lane.laneTrans[i].EndPoint.X)
                    {
                        prjPt = m_lane.geom[i].GetClosestPointTo(centerPt, false);
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, prjPt.ToPoint2D(), 0, 0, 0);
                        timeToCheck = 1;
                    }
                    else if (timeToCheck > 0)
                    {
                        PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, m_lane.geom[i].StartPoint.ToPoint2D(), 0, 0, 0);
                    }
                }
                PolylineToEnd.AddVertexAt(PolylineToEnd.NumberOfVertices, m_lane.geom.Last().EndPoint.ToPoint2D(), 0, 0, 0);
            }

        }

        /// <summary>
        /// 找点到线的投影点,如果点在线外,则返回延长线上的投影点
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        public void prjPtToLine(ThStruct structure, out Point3d prjPt)
        {
            var centerPtTrans = getCenterInLaneCoor(structure);
            getPrjPtToLine(structure.centerPt, centerPtTrans, out prjPt);
        }

        /// <summary>
        /// 找点到线的投影点,如果点在线外,则返回延长线上的投影点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="prjPt"></param>
        /// <returns></returns>
        public void prjPtToLine(Point3d pt, out Point3d prjPt)
        {
            var ptNew = m_lane.TransformPointToLine(pt);
            getPrjPtToLine(pt, ptNew, out prjPt);
        }

        private void getPrjPtToLine(Point3d pt, Point3d ptInLaneCoor, out Point3d prjPt)
        {
            prjPt = new Point3d();

            if (ptInLaneCoor.X < m_lane.laneTrans.First().StartPoint.X)
            {
                prjPt = m_lane.geom.First().GetClosestPointTo(pt, true);
            }
            else if (ptInLaneCoor.X > m_lane.laneTrans.Last().EndPoint.X)
            {
                prjPt = m_lane.geom.Last().GetClosestPointTo(pt, true);
            }
            else
            {
                for (int i = 0; i < m_lane.geom.Count; i++)
                {
                    if (m_lane.laneTrans[i].StartPoint.X <= ptInLaneCoor.X && ptInLaneCoor.X <= m_lane.laneTrans[i].EndPoint.X)
                    {
                        prjPt = m_lane.geom[i].GetClosestPointTo(pt, false);
                        break;
                    }
                }
            }
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
            prjPtToLine(midPoint, out prjMidPt);

        }

        /// <summary>
        /// 构建中心点在车道线坐标系的坐标
        /// </summary>
        /// <param name="structList"></param>
        private void BuildStructCenterInLaneCoor(List<List<ThStruct>> structList)
        {
            foreach (var structureSide in structList)
            {
                foreach (var s in structureSide)
                {
                    if (dictStructureCenterInLaneCoor.ContainsKey(s) == false)
                    {
                        dictStructureCenterInLaneCoor.Add(s, m_lane.TransformPointToLine(s.centerPt));
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
        public List<List<ThStruct>> BuildHeadLayout(List<ThStruct> layout, double TolExtend, double TolLane)
        {
            //车道线往前做框buffer
            var ExtendLineList = m_lane.LaneHeadExtend( TolExtend);
            var FilteredLayout = StructureService.GetStruct( layout, ExtendLineList, TolLane);
            var importLayout = StructureService.SeparateStructByLine(FilteredLayout, ExtendLineList, TolLane);

            var extendPoly = GeomUtils.ExpandLine(ExtendLineList[0], TolLane, 0, TolLane, 0);
            DrawUtils.ShowGeometry(extendPoly, EmgLightCommon.LayerLaneHead, Color.FromColorIndex(ColorMethod.ByColor,44));

            //BuildStructCenter(importLayout);
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
        public Point3d getCenterInLaneCoor(ThStruct structure)
        {
            Point3d ptTrans;

            if (dictStructureCenterInLaneCoor.TryGetValue(structure, out ptTrans) == false)
            {
                ptTrans = m_lane.TransformPointToLine(structure.centerPt);
                dictStructureCenterInLaneCoor.Add(structure, ptTrans);
            }
            return ptTrans;
        }

    }
}
