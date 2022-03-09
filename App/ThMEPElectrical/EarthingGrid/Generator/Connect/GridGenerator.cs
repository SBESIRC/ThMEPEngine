using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class GridGenerator
    {
        public Dictionary<Polyline, List<Polyline>> outlineWithWalls = new Dictionary<Polyline, List<Polyline>>();
        public List<Polyline> outlines = new List<Polyline>();
        public HashSet<Polyline> buildingOutline = new HashSet<Polyline>();
        public HashSet<Point3d> columnPts = new HashSet<Point3d>();

        private Dictionary<Polyline, HashSet<Point3d>> outlinewithBorderPts = new Dictionary<Polyline, HashSet<Point3d>>();
        private Dictionary<Polyline, HashSet<Point3d>> outlinewithNearPts = new Dictionary<Polyline, HashSet<Point3d>>();
        //private double[,,] faceSizes = new double[2, 3, 2] { { {10000, 10000}, {12000, 8000}, {20000, 5000} }, { {20000, 20000}, {24000, 16000}, {40000, 10000} } };

        public GridGenerator(Dictionary<Polyline, List<Polyline>> _outlineWithWalls, List<Polyline> _outlines, HashSet<Polyline> _buildingOutline, HashSet<Point3d> _columnPts)
        {
            outlineWithWalls = _outlineWithWalls;
            outlines = _outlines;
            buildingOutline = _buildingOutline;
            columnPts = _columnPts;
        }

        public void Genterator()
        {
            //0、预处理数据
            //输入墙、外边框，获得外边框对应的墙点，获得外边框对应的引下线
            DataProcess.ProcessData(outlineWithWalls, outlines, columnPts, ref outlinewithBorderPts, ref outlinewithNearPts);

            //1、生成柱网
            var columnGrid = new ColumnGrid(outlinewithBorderPts,outlines, outlinewithNearPts, buildingOutline, columnPts);
            var findPolylineFromLines = columnGrid.Genterate();

            List<Tuple<double, double>> faceSize = new List<Tuple<double, double>>();
            //2、生成地网
            var earthGrid = new EarthGrid(findPolylineFromLines, faceSize);
            var grid = earthGrid.Genterate();
        }
    }
}
