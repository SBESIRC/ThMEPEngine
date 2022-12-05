using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThPlatform3D.WallConstruction.Data;

namespace ThPlatform3D.WallConstruction.Service
{
    internal class ThWallConstructionUtilServices
    {
        public static ThWallConstructionDataProcessService GetData(AcadDatabase acadDatabase, Point3dCollection framePts, ThMEPOriginTransformer transformer)
        {
            var dataFactory = new ThWallConstructionDataFactory()
            {
                Transformer = transformer,
            };
            dataFactory.GetElements(acadDatabase.Database, framePts);

            var dataQuery = new ThWallConstructionDataProcessService()
            {
                Transformer = transformer,
                Wall = dataFactory.Wall,
                Door = dataFactory.Door,
                Axis = dataFactory.Axis,
                FloorLevel = dataFactory.FloorLevel,
                Moldings = dataFactory.Moldings,
                BreakLine = dataFactory.BreakLine,
                FloorNum = dataFactory.FloorNum,
            };


            dataQuery.Print();

            return dataQuery;
        }

    }
}
