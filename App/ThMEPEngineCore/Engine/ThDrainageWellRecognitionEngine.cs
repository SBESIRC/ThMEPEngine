using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDrainageWellExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDrainageWellExtractionVisitor()
            {
                LayerFilter = ThDrainageWellLayerManager.CurveXrefLayers(database),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }
    }

    public class ThDrainageWellRecognitionEngine : ThBuildingElementRecognitionEngine
    { 
        public List<Entity> Geos { get; set; }
        public ThDrainageWellRecognitionEngine()
        {
            Geos = new List<Entity>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDrainageWellExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var ents = new List<Entity>();
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
                    ents.Add(filterObj as Entity);
                }
            }
            else
            {
                ents = objs.Cast<Entity>().ToList();
            }
            ents.ForEach(o => Geos.Add(o));
        }        
    }

    public class ThDrainageWellBlockExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDrainageWellBlkExtractionVisitor()
            {
                LayerFilter = new HashSet<string>(ThDrainageWellLayerManager.CurveXrefLayers(database)),
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void ExtractFromMS(Database database)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ThDrainageWellBlockRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<Entity> Geos { get; set; }
        public ThDrainageWellBlockRecognitionEngine()
        {
            Geos = new List<Entity>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDrainageWellBlockExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var ents = new List<Entity>();
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
                    ents.Add(filterObj as Entity);
                }
            }
            else
            {
                ents = objs.Cast<Entity>().ToList();
            }
            ents.ForEach(o => Geos.Add(o));
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new System.NotImplementedException();
        }
    }
}
