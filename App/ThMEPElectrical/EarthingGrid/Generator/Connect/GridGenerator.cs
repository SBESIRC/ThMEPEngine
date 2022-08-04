using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.EarthingGrid.Generator.Data;
using ThMEPElectrical.EarthingGrid.Generator.Utils;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class GridGenerator
    {
        public static HashSet<Tuple<Point3d, Point3d>> Genterate(PreProcess preProcessData, List<Tuple<double, double>> faceSize, bool beMerge, double findLength)
        {
            //1、生成柱网
            var columnGrid = new ColumnGrid(preProcessData, findLength);
            var findPolylineFromLines = columnGrid.Genterate();

            //1.5 删除禁区外的线
            RangeConfine.RemoveOuterForbiddenLines(ref findPolylineFromLines, preProcessData);

            var earthGrid = new Dictionary<Point3d, HashSet<Point3d>>();
            if (beMerge == true)
            {
                //2、生成地网
                var grid = new EarthGrid(findPolylineFromLines, faceSize);
                grid.Genterate(preProcessData, ref earthGrid);
            }
            else
            {
                foreach(var line in findPolylineFromLines.Keys)
                {
                    GraphDealer.AddLineToGraph(line.Item1, line.Item2, ref earthGrid);
                }
            }
            //3、后处理
            var postPress = new PostProcess(preProcessData, earthGrid);
            return postPress.Process();
        }
    }
}
