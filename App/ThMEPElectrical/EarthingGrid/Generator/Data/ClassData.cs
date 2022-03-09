using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Geometries;

namespace ThMEPElectrical.EarthingGrid.Generator.Data
{
    class ClassData
    {
        //接闪网规格（m）	5x5或6x4	10x10或12x8	20x20或24x16
        //建筑轮廓线内缩距离（m）	0.6	0.6	0.6
        //建筑轮廓线外扩距离（m）	0.6	0.6	0.6
        //接地网建议规格（m）	10x10或12x8或20x5	10x10或12x8或20x5	20x20或24x16或40x10

    }
}
