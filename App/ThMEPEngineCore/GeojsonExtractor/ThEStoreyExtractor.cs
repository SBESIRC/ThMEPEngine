using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model.Electrical;
using ThMEPEngineCore.IO;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThEStoreyExtractor : ThExtractorBase, IPrint
    {
        public List<ThEStoreyInfo> Storeys { get; private set; }
        public Dictionary<string, string> StoreyNumberMap { get; set; }
        public ThEStoreyExtractor()
        {
            UseDb3Engine = true;
            TesslateLength = 200.0;
            Storeys = new List<ThEStoreyInfo>();
            Category = BuiltInCategory.StoreyBorder.ToString();
            StoreyNumberMap = new Dictionary<string, string>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var engine = new ThEStoreysRecognitionEngine();
            engine.Recognize(database, pts);
            Storeys = engine.Elements.Cast<ThEStoreys>().Select(o => new ThEStoreyInfo(o)).ToList();
            Storeys.ForEach(o =>
            {
                var curve = ThTesslateService.Tesslate(o.Boundary, TesslateLength);
                o.Boundary = curve as Polyline;
            });
            Sort();
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

        private void Sort()
        {
            //ToDO
            Storeys = Storeys.Where(o => !(o.StoreyNumber.Contains('B'))).ToList();
            Storeys.OrderBy(o => double.Parse(o.StoreyNumber));
            for(int i=1;i<=Storeys.Count;i++)
            {
                if(Storeys[i-1].StoreyNumber!="")
                    StoreyNumberMap.Add(Storeys[i-1].StoreyNumber, i + "F");
            }
        }
    }
}
