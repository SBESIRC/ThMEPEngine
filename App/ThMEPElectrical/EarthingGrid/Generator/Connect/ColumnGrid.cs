using System;
using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThMEPElectrical.EarthingGrid.Generator.Data;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class ColumnGrid
    {
        public List<Polyline> allOutlines = new List<Polyline>();
        public Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts = new Dictionary<Polyline, HashSet<Point3d>>();
        public Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts = new Dictionary<Polyline, HashSet<Point3d>>();
        public HashSet<Polyline> buildingOutline = new HashSet<Polyline>();
        public HashSet<Point3d> columnPts = new HashSet<Point3d>();

        private Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph = new Dictionary<Point3d, HashSet<Point3d>>();
        private Dictionary<Point3d, HashSet<Point3d>> graph = new Dictionary<Point3d, HashSet<Point3d>>();
        private Point3dCollection allPts = new Point3dCollection();
        private Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();

        public ColumnGrid(PreProcess preProcessData)
        {
            outlinewithBorderPts = preProcessData.outlinewithBorderPts;
            allOutlines = preProcessData.outlines;
            outlinewithNearPts = preProcessData.outlinewithNearPts;
            buildingOutline = preProcessData.buildingOutline;
            columnPts = preProcessData.columnPts;
        }

        public Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> Genterate()
        {
            //1、近点与墙点、墙点墙点之间的连接
            BorderNearConnect.ConnectBorderNear(outlinewithBorderPts, outlinewithNearPts, columnPts, allOutlines, buildingOutline, ref nearBorderGraph);

            //1.5、处理数据
            var nearAndBorderPts = nearBorderGraph.Keys.ToList();
            nearAndBorderPts.ForEach(pt => allPts.Add(pt));
            columnPts.ForEach(pt => allPts.Add(pt));
            allPts = PointsDealer.PointsDistinct(allPts, 10);

            //2、生成初始网格
            GenerateOriGrid();

            //3、网格优化
            ModifyGrid();

            //4、分割
            Split();

            return findPolylineFromLines;
        }

        private void GenerateOriGrid()
        {
            var borderPts = new List<Point3d>();
            outlinewithBorderPts.Values.ForEach(pts => { pts.ForEach(pt => borderPts.Add(pt)); });
            //生成网格
            StructureDealer.VoronoiDiagramConnect(allPts, ref graph);

            GraphDealer.RemoveSameClassLine(borderPts, ref graph);

            GraphDealer.MergeGraphAToB(nearBorderGraph, ref graph);

            GraphDealer.SimplifyGraph(ref graph, allPts.Cast<Point3d>().ToList());
        }

        private void ModifyGrid()
        {
            var itcBorderPts = PointsDealer.FindIntersectBorderPt(allOutlines, columnPts);
            GraphDealer.AddConnectUpToFour(ref graph, columnPts, itcBorderPts, 5000); //MaxBeamLength

            GraphDealer.DeleteConnectUpToFour(ref graph, ref nearBorderGraph);

            GraphDealer.SimplifyGraph(ref graph, columnPts.ToList());
        }

        private void Split()
        {
            //生成一个Polyline对应一堆其上的线的结构
            //在后面分割的时候不处理这堆东西
            var outlineWithBorderLine = new Dictionary<Polyline, List<Tuple<Point3d, Point3d>>>();
            StructureDealer.CloseBorder(allOutlines, graph.Keys.ToHashSet(), ref outlineWithBorderLine);

            outlineWithBorderLine.Values.ForEach(tups=> tups.ForEach(o => GraphDealer.AddLineToGraph(o.Item1, o.Item2, ref graph)));

            AreaDealer.BuildPolygonsCustom(graph, ref findPolylineFromLines);

            ////此处加到StructureDealer里一个函数：从findPolylineFromLines删除掉outlineWithBorderLine
            //AreaDealer.DeleteBuildingLines(ref findPolylineFromLines, outlineWithBorderLine);

            AreaDealer.SplitBlock(ref findPolylineFromLines);
        }
    }
}
