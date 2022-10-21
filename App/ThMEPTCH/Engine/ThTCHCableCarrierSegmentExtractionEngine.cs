using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Services;
using ThMEPEngineCore.Service;

namespace ThMEPTCH.Engine
{
    public class ThTCHCableCarrierSegmentExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThTCHCableCarrierSegmentExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThTCHCableCarrierSegmentExtractionVisitor()
            {
                LayerFilter = ThDbLayerManager.Layers(database),
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    ThMEPTCHService.DXF_CABLETRY,
                };
                var layerNames = new string[]
                {
                    "E-UNIV-EL2",
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var psr = Active.Editor.SelectCrossingPolygon(frame, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    var visitor = new ThTCHCableCarrierSegmentExtractionVisitor();
                    var elements = new List<ThRawIfcFlowSegmentData>();
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.Element<Entity>(o);
                        if (visitor.CheckLayerValid(e) && visitor.IsFlowSegment(e))
                        {
                            visitor.DoExtract(elements, e, Matrix3d.Identity);
                        }
                    });
                    Results = elements;
                }
            }
        }
    }
}
