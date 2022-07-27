using System.Collections.Generic;

using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using TianHua.Electrical.PDS.Service;
using ThMEPEngineCore.GeojsonExtractor.Service;

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
