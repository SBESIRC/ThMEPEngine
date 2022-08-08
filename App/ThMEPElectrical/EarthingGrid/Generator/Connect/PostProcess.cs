using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Data;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThCADExtension;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    internal class PostProcess
    {
        private Dictionary<Point3d, HashSet<Point3d>> ConductorGraph { get; set; }

        private PreProcess PreProcessData { get; set; }
        private HashSet<Polyline> Outlines { get; set; }
        private Dictionary<Point3d, HashSet<Point3d>> EarthGrid { get; set; }
        public PostProcess(PreProcess _preProcessData, Dictionary<Point3d, HashSet<Point3d>> _earthGrid)
        {
            ConductorGraph = _preProcessData.conductorGraph;
            Outlines = _preProcessData.outlines.ToHashSet();
            EarthGrid = _earthGrid;
            PreProcessData = _preProcessData;
        }

        public HashSet<Tuple<Point3d, Point3d>> Process()
        {
            MergeGrid();

            //优化图
            var tmpgrid = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var kv in EarthGrid)
            {
                tmpgrid.Add(kv.Key, kv.Value);
            }
            GraphDealer.SimplifyGraph(ref tmpgrid, GetPtsOnBorders().ToList(), 1000);
            EarthGrid.Clear();
            EarthGrid = tmpgrid;

            //贴边
            StichToBorder();
            //删除outline附近的线
            DeleteLineNearOutline();
            //添加线
            AddLinesOnOutlines();
            //删除禁区内的线
            RemoveInnerForbiddenLines();
            //连接引下线
            AddDownConductorToEarthGrid();
            return LineDealer.Graph2Lines(EarthGrid);
        }

        /// <summary>
        /// 将画线附近的链接贴上画线
        /// </summary>
        private void StichToBorder()
        {
            var allPts = EarthGrid.Keys.ToHashSet();
            var allPtsSpatialIndex = new ThCADCoreNTSSpatialIndex(allPts.Select(p => new DBPoint(p)).ToCollection());
            var polylinePts = GetPtsOnBorders();
            var plPtsSpatialIndex = new ThCADCoreNTSSpatialIndex(polylinePts.Select(p => new DBPoint(p)).ToCollection());
            //1、找到需要修改的点，来减少范围（内外500）
            var nearPtToOutlines = GetPtsNearBorders(allPtsSpatialIndex);
            //2、遍历这些点，按优先级选最近，并移动
            MovePts(nearPtToOutlines, plPtsSpatialIndex, allPtsSpatialIndex);
        }

        /// <summary>
        /// 建立点到某个多边形的映射关系
        /// </summary>
        /// <returns></returns>
        private Dictionary<Point3d, HashSet<Polyline>> GetPtsNearBorders(ThCADCoreNTSSpatialIndex spatialIndex, int bufferLength = 500)
        {
            var nearPtToOutlines = new Dictionary<Point3d, HashSet<Polyline>>();
            foreach (var outline in Outlines)
            {
                bool isClose = false;
                if (outline.Closed == false)
                {
                    isClose = true;
                    outline.Closed = true;
                }
                
                var shell = outline.Buffer(bufferLength).OfType<Polyline>().OrderByDescending(p => p.Area).First();
                var holes = outline.Buffer(-bufferLength).OfType<Polyline>().Where(o => o.Area > 1.0).ToList();
                var mPolygon = ThMPolygonTool.CreateMPolygon(shell, holes.OfType<Curve>().ToList());
                var innerPts = spatialIndex.SelectWindowPolygon(mPolygon).OfType<DBPoint>().Select(d => d.Position);

                foreach (var innerPt in innerPts)
                {
                    if (!nearPtToOutlines.ContainsKey(innerPt))
                    {
                        nearPtToOutlines.Add(innerPt, new HashSet<Polyline>());
                    }
                    nearPtToOutlines[innerPt].Add(outline);
                }

                if (isClose == true)
                {
                    isClose = false;
                    outline.Closed = false;
                }
            }
            return nearPtToOutlines;
        }

        private void MovePts(Dictionary<Point3d, HashSet<Polyline>> nearPtToOutlines, ThCADCoreNTSSpatialIndex plPtsSpatialIndex, ThCADCoreNTSSpatialIndex allPtsSpatialIndex)
        {
            //EarthGrid
            foreach (var nearPtToOutline in nearPtToOutlines)
            {
                var nearPt = nearPtToOutline.Key;
                var circle = new Circle(nearPt, Vector3d.ZAxis, 1000);
                var innerPlPts = plPtsSpatialIndex.SelectWindowPolygon(ThCircleExtension.TessellateCircleWithArc(circle, 100)).OfType<DBPoint>().Select(d => d.Position).ToHashSet();
                var bestPt = nearPt;
                if (innerPlPts.Count > 0)
                {
                    bestPt = GetTheClosestPt(innerPlPts, nearPt);
                }
                else
                {
                    var innerNearPts = new HashSet<Point3d>();
                    foreach (var pl in nearPtToOutline.Value)
                    {
                        innerNearPts.Add(pl.GetClosePoint(nearPt));
                    }
                    bestPt = GetTheClosestPt(innerNearPts, nearPt);
                }
                MovePtOnGraph(nearPt, bestPt);
            }
        }

        private HashSet<Point3d> GetPtsOnBorders()
        {
            var polylinePts = new HashSet<Point3d>();
            foreach (var outline in Outlines)
            {
                for (int i = 0; i < outline.NumberOfVertices; ++i)
                {
                    polylinePts.Add(outline.GetPoint3dAt(i));
                }
            }
            return polylinePts;
        }

        private void DeleteLineNearOutline()
        {
            var allPts = EarthGrid.Keys.ToHashSet();
            var allPtsSpatialIndex = new ThCADCoreNTSSpatialIndex(allPts.Select(p => new DBPoint(p)).ToCollection());
            var nearPtToOutlines = GetPtsNearBorders(allPtsSpatialIndex);

            foreach (var nearPtToOutline in nearPtToOutlines)
            {
                var nearPtA = nearPtToOutline.Key;
                var outlines = nearPtToOutline.Value;
                if (EarthGrid.ContainsKey(nearPtA))
                {
                    foreach (var ptB in EarthGrid[nearPtA].ToList())
                    {
                        Point3d middlePt = new Point3d((nearPtA.X + ptB.X) / 2, (nearPtA.Y + ptB.Y) / 2, 0);
                        var dis = nearPtA.DistanceTo(ptB) / 4;
                        dis = dis < 500 ? 500 : dis;
                        Circle circle = new Circle(middlePt, Vector3d.ZAxis, dis);
                        foreach (var ol in outlines)
                        {
                            var pts = new Point3dCollection();
                            ol.IntersectWith(circle, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                            if (pts.Count > 0)//|| ol.Contains(middlePt))
                            //if (ol.Intersects(circle))//|| ol.Contains(middlePt))
                            {
                                DeleteLineFromGraph(nearPtA, ptB);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void AddLinesOnOutlines()
        {
            foreach (var outline in Outlines)
            {
                var cnt = outline.NumberOfVertices;
                if (cnt < 1)
                {
                    continue;
                }
                var prePt = outline.GetPoint3dAt(cnt - 1);
                for (int i = 0; i < cnt; ++i)
                {
                    var curPt = outline.GetPoint3dAt(i);
                    if(!(outline.Closed == false && i == 0))
                    {
                        AddLineToGraph(prePt, curPt);
                    }
                    prePt = curPt;
                }
            }
        }

        private Point3d GetTheClosestPt(HashSet<Point3d> pts, Point3d basePt)
        {
            var ansPt = basePt;
            double minDis = double.MaxValue;
            foreach (var pt in pts)
            {
                var curDis = pt.DistanceTo(basePt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    ansPt = pt;
                }
            }
            return ansPt;
        }

        private void MovePtOnGraph(Point3d ptFrom, Point3d ptTo)
        {
            if (!EarthGrid.ContainsKey(ptFrom))
            {
                return;
            }
            foreach (var connect in EarthGrid[ptFrom].ToHashSet())
            {
                DeleteLineFromGraph(connect, ptFrom);
                AddLineToGraph(connect, ptTo);
            }
        }

        /// <summary>
        /// 链接引下线
        /// </summary>
        public void AddDownConductorToEarthGrid()
        {
            //1、获取点组
            var ptGroups = new List<HashSet<Point3d>>();
            GetPointsGroups(ref ptGroups);

            //2、对每个组连接最近线
            ConnectWithEarthGrid(ptGroups);
        }

        //生成点集组
        private void GetPointsGroups(ref List<HashSet<Point3d>> ptGroups)
        {
            HashSet<Point3d> ptVisted = new HashSet<Point3d>();
            foreach (var curPt in ConductorGraph.Keys)
            {
                if (!ptVisted.Contains(curPt))
                {
                    HashSet<Point3d> onePtsGroup = new HashSet<Point3d>();
                    BFS(curPt, ref onePtsGroup, ref ptVisted, ConductorGraph);
                    ptGroups.Add(onePtsGroup);
                }
            }
        }

        //广度遍历
        private void BFS(Point3d basePt, ref HashSet<Point3d> onePtsGroup, ref HashSet<Point3d> ptVisted, Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            Queue<Point3d> queue = new Queue<Point3d>();
            queue.Enqueue(basePt);
            if (!onePtsGroup.Contains(basePt))
            {
                onePtsGroup.Add(basePt);
            }
            ptVisted.Add(basePt);
            while (queue.Count > 0)
            {
                Point3d topPt = queue.Dequeue();
                foreach (var pt in graph[topPt])
                {
                    if (!ptVisted.Contains(pt))
                    {
                        ptVisted.Add(pt);
                        queue.Enqueue(pt);
                        onePtsGroup.Add(pt);
                    }
                }
            }
        }

        private void ConnectWithEarthGrid(List<HashSet<Point3d>> ptGroups)
        {
            var lines = LineDealer.Graph2Lines(EarthGrid);
            foreach (var ptGroup in ptGroups)
            {
                double minDis = double.MaxValue;
                Point3d cloestPt = new Point3d();
                Point3d cloestBasePt = new Point3d();
                foreach (var pt in ptGroup)
                {
                    foreach (var line in lines)
                    {
                        Line tmpLine = new Line(line.Item1, line.Item2);
                        Point3d curCloestPt = tmpLine.GetClosestPointTo(pt, false);
                        double curDis = curCloestPt.DistanceTo(pt);
                        if (curDis < minDis)
                        {
                            minDis = curDis;
                            cloestPt = curCloestPt;
                            cloestBasePt = pt;
                        }
                    }
                }
                if (cloestBasePt != new Point3d() && cloestPt != new Point3d())
                {
                    AddLineToGraph(cloestBasePt, cloestPt);
                }
            }
        }

        public void RemoveInnerForbiddenLines()
        {
            var pt2Line = new Dictionary<Point3d, Tuple<Point3d, Point3d>>();
            foreach (var lines in EarthGrid)
            {
                foreach (var pt in lines.Value)
                {
                    var middlePt = new Point3d((pt.X + lines.Key.X) / 2, (pt.Y + lines.Key.Y) / 2, 0);
                    var curline = new Tuple<Point3d, Point3d>(pt, lines.Key);
                    if (!pt2Line.ContainsKey(middlePt))
                    {
                        pt2Line.Add(middlePt, curline);
                    }
                    if (!pt2Line.ContainsKey(pt))
                    {
                        pt2Line.Add(pt, curline);
                    }
                    if (!pt2Line.ContainsKey(lines.Key))
                    {
                        pt2Line.Add(lines.Key, curline);
                    }
                }
            }

            var dbPoints = pt2Line.Keys.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);
            var containPoints = new HashSet<Point3d>();
            foreach (var ol in PreProcessData.innOutline)
            {
                spatialIndex.SelectWindowPolygon(ol.Buffer(-500).OfType<Polyline>().Max()).OfType<DBPoint>().Select(d => d.Position).ForEach(pt => containPoints.Add(pt));
            }
            foreach (var pt in pt2Line.Keys)
            {
                if (containPoints.Contains(pt))
                    DeleteLineFromGraph(pt2Line[pt].Item1, pt2Line[pt].Item2);
            }
        }

        private void MergeGrid()
        {
            var allPts = EarthGrid.Keys.ToHashSet();
            var allPtsSpatialIndex = new ThCADCoreNTSSpatialIndex(allPts.Select(p => new DBPoint(p)).ToCollection());

            foreach(var pt in allPts.ToList())
            {
                if (allPts.Contains(pt))
                {
                    var circle = new Circle(pt, Vector3d.ZAxis, 1000);
                    var innerPlPts = allPtsSpatialIndex.SelectWindowPolygon(ThCircleExtension.TessellateCircleWithArc(circle, 100)).OfType<DBPoint>().Select(d => d.Position).ToHashSet();
                    
                    int cnt = innerPlPts.Count;
                    double xSum = 0;
                    double ySum = 0;
                    foreach(var pt2 in innerPlPts)
                    {
                        xSum+= pt2.X;
                        ySum += pt2.Y;
                    }
                    var middlePt = new Point3d(xSum / cnt, ySum/cnt, 0);
                    foreach (var closePt in innerPlPts)
                    {
                        if (EarthGrid.ContainsKey(closePt))
                        {
                            foreach (var cntPt in EarthGrid[closePt].ToList())
                            {
                                DeleteLineFromGraph(cntPt, closePt);
                                AddLineToGraph(middlePt, cntPt);
                            }
                        }
                    }
                }
            }
        }

        private void AddLineToGraph(Point3d ptA, Point3d ptB)
        {
            if (!EarthGrid.ContainsKey(ptA))
            {
                EarthGrid.Add(ptA, new HashSet<Point3d>());
            }
            if (!EarthGrid[ptA].Contains(ptB))
            {
                EarthGrid[ptA].Add(ptB);
            }
            if (!EarthGrid.ContainsKey(ptB))
            {
                EarthGrid.Add(ptB, new HashSet<Point3d>());
            }
            if (!EarthGrid[ptB].Contains(ptA))
            {
                EarthGrid[ptB].Add(ptA);
            }
        }

        public void DeleteLineFromGraph(Point3d ptA, Point3d ptB)
        {
            if (EarthGrid.ContainsKey(ptA) && EarthGrid[ptA].Contains(ptB))
            {
                EarthGrid[ptA].Remove(ptB);
                if (EarthGrid[ptA].Count == 0)
                {
                    EarthGrid.Remove(ptA);
                }
            }
            if (EarthGrid.ContainsKey(ptB) && EarthGrid[ptB].Contains(ptA))
            {
                EarthGrid[ptB].Remove(ptA);
                if (EarthGrid[ptB].Count == 0)
                {
                    EarthGrid.Remove(ptB);
                }
            }
        }
    }
}
