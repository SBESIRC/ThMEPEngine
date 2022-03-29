using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.EarthingGrid.Generator.Utils;
using ThMEPElectrical.EarthingGrid.Data;
using ThMEPElectrical.EarthingGrid.Generator.Data;

namespace ThMEPElectrical.EarthingGrid.Generator.Connect
{
    class GridGenerator
    {
        public static void Genterate(PreProcess preProcessData, List<Tuple<double, double>> faceSize)
        {
            //1、生成柱网
            var columnGrid = new ColumnGrid(preProcessData);
            var findPolylineFromLines = columnGrid.Genterate();

            //findPolylineFromLines 需要对它进行处理 包括 1、由于前文的split函数缺陷，造成线会有交叉情况，去交叉，在这或者在split处处理

            //2、生成地网
            var grid = new EarthGrid(findPolylineFromLines, faceSize);
            var earthGrid = grid.Genterate();

            //3、连接引下线
            DownConductor.AddDownConductorToEarthGrid(preProcessData, ref earthGrid);

            ShowInfo.ShowGraph(earthGrid, 4);
        }
    }
}
