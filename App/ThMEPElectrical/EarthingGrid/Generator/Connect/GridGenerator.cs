using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.EarthingGrid.Generator.Data;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class GridGenerator
    {
        public static HashSet<Tuple<Point3d, Point3d>> Genterate(PreProcess preProcessData, List<Tuple<double, double>> faceSize)
        {
            //1、生成柱网
            var columnGrid = new ColumnGrid(preProcessData);
            var findPolylineFromLines = columnGrid.Genterate();

            //2、生成地网
            var grid = new EarthGrid(findPolylineFromLines, faceSize);
            var earthGrid = grid.Genterate(preProcessData);

            //3、后处理
            var postPress = new PostProcess(preProcessData, earthGrid);
            return postPress.Process();
        }
    }
}
