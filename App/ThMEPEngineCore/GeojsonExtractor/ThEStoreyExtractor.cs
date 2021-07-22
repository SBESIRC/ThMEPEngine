using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Common;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThStoreyExtractor : ThExtractorBase, IPrint
    {
        public List<ThStoreyInfo> Storeys { get; private set; }
        private const double TesslateLength =200.0;
        public ThStoreyExtractor()
        {
            UseDb3Engine = true;
            Storeys = new List<ThStoreyInfo>();
            Category = BuiltInCategory.StoreyBorder.ToString();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var engine = new ThStoreysRecognitionEngine();
                engine.Recognize(database, pts);
                Storeys = engine.Elements.Cast<ThStoreys>().Select(o=>new ThStoreyInfo(o)).ToList();                
            }
            else
            {
                //
            }
            Storeys.ForEach(o =>
            {
                var curve = ThTesslateService.Tesslate(o.Boundary, TesslateLength);
                o.Boundary = curve as Polyline;
            });
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
