using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3SlabExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDB3SlabExtractionVisitor()
            {
                LayerFilter = ThSlabLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ThDB3SlabRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3SlabExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            List<Curve> curves = new List<Curve>();
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
                    curves.Add(filterObj as Curve);
                }
            }
            else
            {
                curves = objs.Cast<Curve>().ToList();
            }
            curves.ForEach(o =>
                {
                    if (o is Polyline polyline && polyline.Length>1e-6)
                    {
                        Elements.Add(ThIfcSlab.Create(polyline));
                    }
                });
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }

        private Entity Wash(Curve curve)
        {
            var objs = new DBObjectCollection() { curve };
            var simplifer = new ThSlabSimplifier();
            objs = simplifer.Tessellate(objs);
            objs = simplifer.Simplify(objs);
            objs = simplifer.MakeValid(objs);            
            objs = simplifer.Normalize(objs);
            return objs.Count > 0 ? objs[0] as Entity : new Polyline();
        }
    }
}