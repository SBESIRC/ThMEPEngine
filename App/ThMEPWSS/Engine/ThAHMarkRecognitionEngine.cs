using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Engine
{
    public class ThAHMarkRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public List<Entity> Texts { get; private set; }
        public ThAHMarkRecognitionEngine()
        {
            Texts = new List<Entity>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var visitor = new ThAHMarkExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Recognize(visitor.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var texts = new List<Entity>();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(polygon);
                foreach (var filterObj in spatialIndex.SelectCrossingPolygon(pline))
                {
                    texts.Add(filterObj as Entity);
                }
            }
            else
            {
                texts = objs.Cast<Entity>().ToList();
            }
            Texts = texts;
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}
