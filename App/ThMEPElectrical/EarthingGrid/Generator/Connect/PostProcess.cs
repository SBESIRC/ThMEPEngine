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

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    internal class PostProcess
    {
        private Dictionary<Point3d, HashSet<Point3d>> ConductorGraph { get; set; }
        private HashSet<Polyline> Outlines { get; set; }
        private Dictionary<Point3d, HashSet<Point3d>> EarthGrid { get; set; }
        public PostProcess(PreProcess _preProcessData, Dictionary<Point3d, HashSet<Point3d>> _earthGrid)
        {
            ConductorGraph = _preProcessData.conductorGraph;
            Outlines = _preProcessData.outlines.ToHashSet();
            EarthGrid = _earthGrid;
        }

        public HashSet<Tuple<Point3d, Point3d>> Process()
        {
            //1、贴边
            StichToBorder();

            //2、连接引下线
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
            foreach(var outline in Outlines)
            {
                var shell = outline.Buffer(bufferLength).OfType<Polyline>().OrderByDescending(p => p.Area).First();
                var holes = outline.Buffer(-bufferLength).OfType<Polyline>().Where(o => o.Area > 1.0).ToList();
                var mPolygon = ThMPolygonTool.CreateMPolygon(shell, holes.OfType<Curve>().ToList());
                var innerPts = spatialIndex.SelectWindowPolygon(mPolygon).OfType<DBPoint>().Select(d => d.Position);//.Distinct().ToHashSet();

                foreach(var innerPt in innerPts)
                {
                    if (!nearPtToOutlines.ContainsKey(innerPt))
                    {
                        nearPtToOutlines.Add(innerPt, new HashSet<Polyline>());
                    }
                    nearPtToOutlines[innerPt].Add(outline);
                }
            }
            return nearPtToOutlines;
        }

        private void MovePts(Dictionary<Point3d, HashSet<Polyline>> nearPtToOutlines, ThCADCoreNTSSpatialIndex plPtsSpatialIndex, ThCADCoreNTSSpatialIndex allPtsSpatialIndex)
        {
            //EarthGrid
            foreach(var nearPtToOutline in nearPtToOutlines)
            {
                var nearPt = nearPtToOutline.Key;
                var circle = new Circle(nearPt, Vector3d.ZAxis, 500);
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
                for(int i = 0; i < outline.NumberOfVertices; ++i)
                {
                    polylinePts.Add(outline.GetPoint3dAt(i));
                }
            }
            return polylinePts;
        }

        private Point3d GetTheClosestPt(HashSet<Point3d> pts, Point3d basePt)
        {
            var ansPt = basePt;
            double minDis = double.MaxValue;
            foreach(var pt in pts)
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
