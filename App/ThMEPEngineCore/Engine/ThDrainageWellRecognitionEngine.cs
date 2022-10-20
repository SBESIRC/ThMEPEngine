using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPEngineCore.Engine
{
    public class ThDrainageWellExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDrainageWellExtractionVisitor()
            {
                LayerFilter = ThDrainageWellLayerManager.CurveXrefLayers(database).ToHashSet(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
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
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var center = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            if (newPts.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(newPts);
                objs = spatialIndex.SelectCrossingPolygon(pline);
            }
            transformer.Reset(objs);
            objs.Cast<Entity>().ForEach(o => Geos.Add(o));
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }

    public class ThDrainageWellBlockExtractionEngine : ThDistributionElementExtractionEngine
    {
        public ThDrainageWellBlkExtractionVisitor Visitor { get; set; }
        
        public ThDrainageWellBlockExtractionEngine()
        {
            Visitor = new ThDrainageWellBlkExtractionVisitor();
        }
        public override void Extract(Database database)
        {            
            if (Visitor.LayerFilter.Count==0)
            {
                Visitor.LayerFilter = ThDrainageWellLayerManager.CurveXrefLayers(database).ToHashSet();
            }
            Visitor.Results = new List<ThRawIfcDistributionElementData>();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(Visitor);
            extractor.Extract(database);
            Results.AddRange(Visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {            
            if (Visitor.LayerFilter.Count == 0)
            {
                Visitor.LayerFilter = ThDrainageWellLayerManager.CurveXrefLayers(database).ToHashSet();
            }
            Visitor.Results = new List<ThRawIfcDistributionElementData>();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(Visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(Visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotSupportedException();
        }
    }

    public class ThDrainageWellBlockRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public List<Entity> Geos { get; set; }
        public ThDrainageWellBlkExtractionVisitor Visitor { get; set; }
        public ThDrainageWellBlockRecognitionEngine()
        {
            Geos = new List<Entity>();
            Visitor = new ThDrainageWellBlkExtractionVisitor();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {            
            var engine = new ThDrainageWellBlockExtractionEngine()
            {
                Visitor = this.Visitor,
            };
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {            
            var engine = new ThDrainageWellBlockExtractionEngine()
            {
                Visitor=this.Visitor,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }
        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotSupportedException();
        }
        public override void Recognize(List<ThRawIfcDistributionElementData> datas, Point3dCollection polygon)
        {
            var objs = datas.Select(o => o.Geometry).ToCollection();
            var center = polygon.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            transformer.Transform(objs);
            var newPts = transformer.Transform(polygon);
            if (newPts.Count > 0)
            {
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                objs = spatialIndex.SelectCrossingPolygon(newPts);
            }          
            transformer.Reset(objs);
            objs.Cast<Entity>().ForEach(o => Geos.Add(o));
        }
    }
}
