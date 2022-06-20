using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThMEPEngineCore.Engine;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCircuitMarkExtractionEngine : ThAnnotationElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThCircuitMarkExtractionVisitor
            {
                LayerFilter = ThPDSLayerService.CircuitMarkLayers(),
            };
            var extractor = new ThAnnotationElementExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results = visitor.Results;
        }
    }

    public class ThCircuitMarkRecognitionEngine : ThAnnotationElementRecognitionEngine
    {
        public ThCircuitMarkRecognitionEngine()
        {
            Results = new List<ThPDSEntityInfo>();
        }

        public List<ThPDSEntityInfo> Results { get; protected set; }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcAnnotationElementData> datas, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            var extractionEngine = new ThCircuitMarkExtractionEngine();
            extractionEngine.ExtractFromMS(database);
            extractionEngine.Results.Select(o => o.Data as Entity).ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    AddPolyline(polyline);
                }
                else if (o is Arc arc)
                {
                    var arcToPline = arc.TessellateArcWithArc(100.0);
                    AddPolyline(arcToPline);
                }
                else
                {
                    Results.Add(new ThPDSEntityInfo(o, true));
                }
            });
        }

        private void AddPolyline(Polyline pline)
        {
            var polyInfo = new ThPDSEntityInfo(pline, true);
            var curves = new DBObjectCollection();
            polyInfo.Entity.Explode(curves);
            var lines = new List<Line>();
            foreach (var curve in curves)
            {
                if (curve is Line line)
                {
                    lines.Add(line);
                }
                else if (curve is Arc arc)
                {
                    var arcToPolyline = arc.TessellateArcWithArc(100.0);
                    var arcCurves = new DBObjectCollection();
                    arcToPolyline.Explode(arcCurves);
                    lines.AddRange(arcCurves.OfType<Line>());
                }
            }
            lines.OfType<Line>().Where(e => e.Length > 1.0)
                .ForEach(e => Results.Add(new ThPDSEntityInfo(e, polyInfo)));
        }

        /// <summary>
        /// 若视觉闭合则返回true
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private bool IsClosed(Polyline polyline)
        {
            return polyline.Closed || polyline.StartPoint.DistanceTo(polyline.EndPoint) < 10.0;
        }
    }
}
