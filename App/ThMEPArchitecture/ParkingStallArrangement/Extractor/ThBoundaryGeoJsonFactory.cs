using System;
using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    internal class ThBoundaryGeoJsonFactory : ThExtractorBase
    {
        public DBObjectCollection Boudaries { get; private set; }
        public ThBoundaryGeoJsonFactory(DBObjectCollection boudaries)
        {
            Category = "Boundary";
            Boudaries = boudaries;
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var results = new List<ThGeometry>();
            Boudaries.OfType<Polyline>().ForEach(p =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, Guid.NewGuid().ToString());
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, "");
                geometry.Boundary = p;
                results.Add(geometry);
            });
            return results;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            throw new NotImplementedException();
        }
    }

}
