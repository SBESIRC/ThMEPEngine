using System;
using System.Collections.Generic;
using System.Linq;
using AcHelper;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class DataProcess
    {
        private DBObjectCollection Columns = new DBObjectCollection();
        private Dictionary<Polyline, List<Polyline>> buildingWithWalls = new Dictionary<Polyline, List<Polyline>>();
        private DBObjectCollection Conductors = new DBObjectCollection();
        private DBObjectCollection ConductorWires = new DBObjectCollection();

        public List<Polyline> outlines = new List<Polyline>();

        public DataProcess(Dictionary<Polyline, List<Polyline>> _buildingWithWalls, List<Polyline> _outlines,
            DBObjectCollection _Columns, DBObjectCollection _Conductors, DBObjectCollection _ConductorWires)
        {
            buildingWithWalls = _buildingWithWalls;
            outlines = _outlines;
            Columns = _Columns;
            Conductors = _Conductors;
            ConductorWires = _ConductorWires;
        }

        public void Process(ref Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts, ref Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts,
            ref HashSet<Point3d> columnPts, ref Dictionary<Point3d, HashSet<Point3d>> conductorGraph)
        {
            //Columns -> columnPts
            AddCrossPtInPts(Columns.Cast<Polyline>(), ref columnPts);
            var dbPoints = columnPts.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);

            //outlineWithWalls -> outlinewithWallPts
            FindOutlineWallpts(spatialIndex, ref outlinewithBorderPts, 500, 600);

            //outlines & columnPts -> outline with neaerPt && outline with columnPt
            HashSet<Point3d> residentPts = columnPts.ToHashSet();
            foreach (var pt in columnPts)
            {
                foreach (var pts in outlinewithBorderPts.Values)
                {
                    foreach (var ptb in pts)
                    {
                        if (residentPts.Contains(ptb))
                        {
                            residentPts.Remove(ptb);
                        }
                    }
                }
            }
            var spatialIndexB = new ThCADCoreNTSSpatialIndex(residentPts.Select(p => new DBPoint(p)).ToCollection());
            FindOutline2Pts(spatialIndexB, ref outlinewithNearPts, 8000);

            //Conductors & ConductorWires -> conductorGraph
            CreateDownConductorConnects(ref conductorGraph);
        }

        /// <summary>
        /// 生成带有墙点的轮廓线集合
        /// </summary>
        private void FindOutlineWallpts(ThCADCoreNTSSpatialIndex spatialIndex,
            ref Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts, double outBufferLength = 500, double inBufferLength = 600)
        {
            foreach(var buildingWithWall in buildingWithWalls)
            {
                var curOutline = buildingWithWall.Key;

                var shell = curOutline.Buffer(outBufferLength).OfType<Polyline>().OrderByDescending(p => p.Area).First();
                var holes = curOutline.Buffer(-inBufferLength).OfType<Polyline>().Where(o => o.Area > 1.0).ToList();
                var mPolygon = ThMPolygonTool.CreateMPolygon(shell, holes.OfType<Curve>().ToList());
                var innerColumnPoints = spatialIndex.SelectWindowPolygon(mPolygon).OfType<DBPoint>().Select(d => d.Position).Distinct().ToHashSet();

                var wallPts = new HashSet<Point3d>();
                AddCrossPtInPts(buildingWithWall.Value, ref wallPts);

                //选取合适的墙点加入数据集
                if (!outlinewithBorderPts.ContainsKey(curOutline))
                {
                    outlinewithBorderPts.Add(curOutline, innerColumnPoints.Where(pt => curOutline.GetClosePoint(pt).DistanceTo(pt) < inBufferLength).ToHashSet());
                    foreach(var pt in wallPts.Where(pt => curOutline.GetClosePoint(pt).DistanceTo(pt) < inBufferLength).ToHashSet())
                    {
                        outlinewithBorderPts[curOutline].Add(pt);
                    }
                }
            }
        }
        //添加墙的转折点
        private void AddCrossPts(Dictionary<Point3d, HashSet<Point3d>> graph, ref HashSet<Point3d> wallPts)
        {
            foreach (var pt2pts in graph)
            {
                var pt = pt2pts.Key;
                foreach (var ptA in graph[pt])
                {
                    var vecA = ptA - pt;
                    foreach (var ptB in graph[pt])
                    {
                        var vecB = ptB - pt;
                        var angel = vecA.GetAngleTo(vecB);
                        if (angel > Math.PI / 6 && angel < Math.PI / 6 * 5 && !wallPts.Contains(pt))
                        {
                            wallPts.Add(pt);
                        }
                    }
                }
            }
        }

        private void FindOutline2Pts(ThCADCoreNTSSpatialIndex spatialIndex, ref Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts, double bufferLength = 8000)
        {
            foreach(var outline in outlines)
            {
                var shell = outline.Buffer(bufferLength).OfType<Polyline>().OrderByDescending(p => p.Area).First();
                var holes = outline.Buffer(-bufferLength).OfType<Polyline>().Where(o => o.Area > 1.0).ToList();
                var mPolygon = ThMPolygonTool.CreateMPolygon(shell, holes.OfType<Curve>().ToList());    
                outlinewithNearPts.Add(outline, spatialIndex.SelectWindowPolygon(mPolygon).OfType<DBPoint>().Select(d => d.Position).Distinct().ToHashSet());
            }
        }

        /// <summary>
        /// 找到一组多边形中的转折点或者中心点
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="points"></param>
        private void AddCrossPtInPts(IEnumerable<Polyline> polylines, ref HashSet<Point3d> points)
        {
            foreach (var polyline in polylines)
            {
                var lines = ThMEPPolygonService.CenterLine(polyline.ToNTSPolygon().ToDbMPolygon());
                var graph = new Dictionary<Point3d, HashSet<Point3d>>();
                foreach (var line in lines)
                {
                    GraphDealer.AddLineToGraph(line.StartPoint, line.EndPoint, ref graph);
                }
                //对块进行分割
                var walls = new DBObjectCollection();
                var columns = new DBObjectCollection();
                ThVStructuralElementSimplifier.Classify(polyline.ToNTSPolygon().ToDbCollection(), columns, walls);
                //提取柱点
                foreach (var ent in columns)
                {
                    if (ent is Polyline column)
                    {
                        points.Add(column.GetCentroidPoint());
                    }
                }
                //提取墙点
                AddCrossPts(graph, ref points);
            }
        }

        /// <summary>
        /// 生成引下线图
        /// Conductors & ConductorWires -> conductorGraph
        /// </summary>
        private void CreateDownConductorConnects(ref Dictionary<Point3d, HashSet<Point3d>> conductorGraph)
        {
            var basePts = new HashSet<Point3d>();
            foreach(var obj in Conductors)
            {
                if(obj is BlockReference br)
                {
                    basePts.Add(br.Position); //此处可能会出现重复，出现了就修改一下
                }
                else if(obj is Point3d pt)
                {
                    basePts.Add(pt);
                }
            }

            foreach(var obj in ConductorWires)
            {
                if(obj is Polyline pl)
                {
                    GraphDealer.AddLineToGraph(pl.StartPoint, pl.EndPoint, ref conductorGraph);
                }
                else if (obj is Arc arc)
                {
                    GraphDealer.AddLineToGraph(arc.StartPoint, arc.EndPoint, ref conductorGraph);
                }
                else if (obj is Line line)
                {
                    GraphDealer.AddLineToGraph(line.StartPoint, line.EndPoint, ref conductorGraph);
                }
            }

            foreach(var basePt in basePts)
            {
                if (!conductorGraph.ContainsKey(basePt))
                {
                    GraphDealer.AddLineToGraph(basePt, basePt, ref conductorGraph);
                }
            } 
        }
    }
}
