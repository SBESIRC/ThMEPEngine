using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSDistBoxFrameExtraction
    {
        public static List<Polyline> GetDistBoxFrame(Database database, Polyline frame, string layer)
        {
            var polyList = new List<Polyline>();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var extractService = new ThExtractPolylineService()
                {
                    ElementLayer = layer,
                };
                extractService.Extract(acadDatabase.Database, frame.Vertices());
                polyList.AddRange(extractService.Polys);
            }

            return polyList;
        }
    }
}
