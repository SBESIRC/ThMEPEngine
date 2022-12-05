using System;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.DCL.Data
{
    public class ThDclEStoreyExtractor : ThEStoreyExtractor
    {
        public Dictionary<string, string> StoreyNumberMap { get; set; }
        public ThDclEStoreyExtractor()
        {
            StoreyNumberMap = new Dictionary<string, string>();
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            base.Extract(database, pts);
            Sort();
            for (int i = 1; i <= Storeys.Count; i++)
            {
                if (Storeys[i - 1].StoreyNumber != "")
                    StoreyNumberMap.Add(Storeys[i - 1].StoreyNumber, i + "F");
            }
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorTypePropertyName, o.StoreyType);
                if (o.StoreyNumber == "")
                    geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, o.StoreyNumber);
                else
                    geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, StoreyNumberMap[o.StoreyNumber]);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Id);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BasePointPropertyName, o.BasePoint);
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }
        private void Sort()
        {
            Storeys = Storeys.Where(o => !(o.StoreyNumber.Contains('-') || o.StoreyNumber=="")).ToList();
            Storeys = Storeys.OrderBy(o => double.Parse(o.StoreyNumber.Split(',')[0])).ToList();            
        }
    }
}
