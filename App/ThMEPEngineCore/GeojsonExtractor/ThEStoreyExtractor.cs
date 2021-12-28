using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Electrical;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThEStoreyExtractor : ThExtractorBase, IPrint
    {
        public List<ThEStoreyInfo> Storeys { get; set; }        
        public ThEStoreyExtractor()
        {
            UseDb3Engine = true;
            TesslateLength = 500.0;
            Storeys = new List<ThEStoreyInfo>();
            Category = BuiltInCategory.StoreyBorder.ToString();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var engine = new ThEStoreysRecognitionEngine();
            engine.Recognize(database, pts);
            Storeys = engine.Elements.Cast<ThEStoreys>().Select(o => new ThEStoreyInfo(o)).ToList();
            for(int i = 0; i < Storeys.Count; i++)
            {
                var curve = ThTesslateService.Tesslate(Storeys[i].Boundary, TesslateLength);
                Storeys[i].Boundary = curve as Polyline;
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
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, o.StoreyNumber);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Id);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BasePointPropertyName, o.BasePoint);
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Print(Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                Storeys.Select(o => o.Boundary)
                    .Cast<Entity>()
                    .ToList()
                    .CreateGroup(database, ColorIndex);
            }
        }

        public Dictionary<Entity, string> StoreyIds
        {
            get
            {
                var result = new Dictionary<Entity, string>();
                Storeys.ForEach(o => result.Add(o.Boundary, o.Id));
                return result;
            }
        }
    }
}
