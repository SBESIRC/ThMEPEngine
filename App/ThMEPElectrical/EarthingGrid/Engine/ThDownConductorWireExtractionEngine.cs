using System;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical.EarthingGrid.Engine
{
    public class ThDownConductorWireExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Arc)).DxfName,
                    RXClass.GetClass(typeof(Line)).DxfName,
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var layerNames = new string[]
                {
                    "E-GRND-WIRE",
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var psr = Active.Editor.SelectCrossingPolygon(frame, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    var elements = new List<ThRawIfcFlowSegmentData>();
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.Element<Entity>(o);
                        elements.Add(new ThRawIfcFlowSegmentData()
                        {
                            Geometry = e.Clone() as Entity,
                        });
                    });
                    Results.AddRange(elements);
                }
            }
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
}
