using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;

namespace ThMEPEngineCore.Engine
{
    public class ThTCHVPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();

        public override void Extract(Database database)
        {
            throw new NotSupportedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThTCHVPipeExtractionVisitor()
            {
                LayerFilter = LayerFilter,
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);

            visitor.Results.ForEach(x =>
           {
               Results.Add(new ThRawIfcFlowSegmentData() { Data = x.Data, Geometry = x.Geometry });
           });
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            //using (AcadDatabase acadDatabase = AcadDatabase.Active())
            //{
            //    var dxfNames = new string[] {
            //        ThMEPTCHService.DXF_PIPE,
            //    };
            //    var filters = ThSelectionFilterTool.Build(dxfNames, LayerName.ToArray());
            //    var psResult = Active.Editor.SelectCrossingPolygon(frame, filters);

            //    if (psResult.Status == PromptStatus.OK)
            //    {
            //        psResult.Value.GetObjectIds().ForEach(o =>
            //        {
            //            var geom = HandleTCHVPipe(acadDatabase.Element<Entity>(o));
            //            Results.Add(new ThRawIfcFlowSegmentData()
            //            {
            //                Data = o,
            //                Geometry = geom
            //            });
            //        });
            //    }
            //}

            throw new NotSupportedException();
        }

    }
}
