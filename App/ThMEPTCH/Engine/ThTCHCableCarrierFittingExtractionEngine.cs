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

namespace ThMEPTCH.Engine
{
    public class ThTCHCableCarrierFittingExtractionEngine : ThFlowFittingExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    ThMEPTCHService.DXF_EFITTING,
                };
                var layerNames = new string[]
                {
                    "E-UNIV-EL2",
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var psr = Active.Editor.SelectCrossingPolygon(frame, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    var visitor = new ThTCHCableCarrierFittingExtractionVisitor();
                    var elements = new List<ThRawIfcFlowFittingData>();
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.Element<Entity>(o);
                        if (visitor.CheckLayerValid(e) && visitor.IsFlowFitting(e))
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
