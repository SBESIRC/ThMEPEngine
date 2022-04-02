using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
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
            //findPolylineFromLines 需要对它进行处理 包括 1、由于前文的split函数缺陷，造成线会有交叉情况，去交叉，在这或者在split处处理

            //2、生成地网
            var grid = new EarthGrid(findPolylineFromLines, faceSize);
            var earthGrid = grid.Genterate(preProcessData);

            //3、连接引下线
            DownConductor.AddDownConductorToEarthGrid(preProcessData, ref earthGrid);

            return LineDealer.Graph2Lines(earthGrid);
        }
    }
}
