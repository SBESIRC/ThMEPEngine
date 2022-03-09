using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class DataProcess
    {
        public static void ProcessData(Dictionary<Polyline, List<Polyline>> buildingWithWalls, List<Polyline> outlines, HashSet<Point3d> columnPts,
            ref Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts, ref Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts)
        {
            var dbPoints = columnPts.Select(p => new DBPoint(p)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dbPoints);

            //outlineWithWalls -> outlinewithWallPts
            FindOutlineWallpts(buildingWithWalls, spatialIndex, ref outlinewithBorderPts, 500, 600);

            //outlines & columnPts -> outline with neaerPt && outline with columnPt
            HashSet<Point3d> residentPts = columnPts.ToHashSet();
            foreach(var pt in columnPts)
            {
                foreach(var pts in outlinewithBorderPts.Values)
                {
                    foreach(var ptb in pts)
                    {
                        if (residentPts.Contains(ptb))
                        {
                            residentPts.Remove(ptb);
                        }
                    }
                }
            }
            var spatialIndexB = new ThCADCoreNTSSpatialIndex(residentPts.Select(p => new DBPoint(p)).ToCollection());
            FindOutline2Pts(outlines, spatialIndexB, ref outlinewithNearPts, 8000);
        }

        /// <summary>
        /// 生成带有墙点的轮廓线集合
        /// </summary>
        private static void FindOutlineWallpts(Dictionary<Polyline, List<Polyline>> buildingWithWalls, ThCADCoreNTSSpatialIndex spatialIndex,
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
                foreach(var wall in buildingWithWall.Value)
                {
                    var lines = ThMEPPolygonService.CenterLine(wall.ToNTSPolygon().ToDbMPolygon());
                    var graph = new Dictionary<Point3d, HashSet<Point3d>>();
                    foreach(var line in lines)
                    {
                        GraphDealer.AddLineToGraph(line.StartPoint, line.EndPoint, ref graph);
                    }
                    //对块进行分割
                    var walls = new DBObjectCollection();
                    var columns = new DBObjectCollection();
                    ThVStructuralElementSimplifier.Classify(wall.ToNTSPolygon().ToDbCollection(), columns, walls);
                    //提取柱点
                    foreach(var ent in columns)
                    {
                        if (ent is Polyline column)
                        {
                            wallPts.Add(column.GetCentroidPoint());
                        }
                    }
                    //提取墙点
                    AddCrossPts(graph, ref wallPts);
                }
                //选取合适的墙点加入数据集
                if (!outlinewithBorderPts.ContainsKey(curOutline))
                {
                    outlinewithBorderPts.Add(curOutline, wallPts.Where(pt => curOutline.GetClosePoint(pt).DistanceTo(pt) < inBufferLength).ToHashSet());
                }
            }
        }
        //添加墙的转折点
        private static void AddCrossPts(Dictionary<Point3d, HashSet<Point3d>> graph, ref HashSet<Point3d> wallPts)
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

        /// <summary>
        /// 这个nearPts是临时的，后面会进行删除处理
        /// </summary>
        private static void FindOutline2Pts(List<Polyline> outlines, ThCADCoreNTSSpatialIndex spatialIndex, 
            ref Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts, double bufferLength = 8000)
        {
            foreach(var outline in outlines)
            {
                var shell = outline.Buffer(bufferLength).OfType<Polyline>().OrderByDescending(p => p.Area).First();
                var holes = outline.Buffer(-bufferLength).OfType<Polyline>().Where(o => o.Area > 1.0).ToList();
                var mPolygon = ThMPolygonTool.CreateMPolygon(shell, holes.OfType<Curve>().ToList());    
                outlinewithNearPts.Add(outline, spatialIndex.SelectWindowPolygon(mPolygon).OfType<DBPoint>().Select(d => d.Position).Distinct().ToHashSet());
            }
        }
    }
}
