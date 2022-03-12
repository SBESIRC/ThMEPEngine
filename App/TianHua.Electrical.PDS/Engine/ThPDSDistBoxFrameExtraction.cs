using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

using ThMEPEngineCore.GeojsonExtractor.Service;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSDistBoxFrameExtraction
    {
        public static List<Polyline> GetDistBoxFrame(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var results = new List<Polyline>();
                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = ThPDSLayerService.DistBoxFrameLayer(),
                };
                extractService.Extract(acadDatabase.Database, new Point3dCollection());
                results.AddRange(extractService.Polys);

                return results;
            }
        }
    }
}
