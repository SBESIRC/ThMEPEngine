using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.Temp
{
    public class ThSanitarywareExtractionEngine : ThDistributionElementExtractionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThSanitarywareExtractionEngine()
        {
        }

        public override void Extract(Database database)
        {
            var visitor = new ThSanitarywareExtractionVisitor();
            if (CheckQualifiedLayer != null)
            {
                visitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }
            if (CheckQualifiedBlockName != null)
            {
                visitor.CheckQualifiedBlockName = this.CheckQualifiedBlockName;
            }
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThSanitarywareExtractionVisitor();
            if (CheckQualifiedLayer != null)
            {
                visitor.CheckQualifiedLayer = this.CheckQualifiedLayer;
            }
            if (CheckQualifiedBlockName != null)
            {
                visitor.CheckQualifiedBlockName = this.CheckQualifiedBlockName;
            }
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }
    }
    public class ThSanitarywareRecognitionEngine : ThDistributionElementRecognitionEngine
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThSanitarywareExtractionEngine()
            {
                CheckQualifiedLayer = this.CheckQualifiedLayer,
                CheckQualifiedBlockName = this.CheckQualifiedBlockName,
            };
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var engine = new ThSanitarywareExtractionEngine()
            {
                CheckQualifiedLayer = this.CheckQualifiedLayer,
                CheckQualifiedBlockName = this.CheckQualifiedBlockName,
            };
            engine.ExtractFromMS(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcDistributionElementData> datas,
            Point3dCollection polygon)
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
            datas.Where(o => objs.Contains(o.Geometry)).ForEach(o =>
            {
                if (o.Geometry is Polyline polyline && polyline.Area > 0.0)
                {
                    var data = o.Data as BlockInfo;
                    Elements.Add(new ThIfcDistributionFlowElement
                    {
                        Outline = polyline,
                        Matrix = data.Matrix,
                        Name = o.Data.ToString(),
                    });
                }
            });
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
