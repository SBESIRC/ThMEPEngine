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

namespace ThMEPElectrical.EarthingGrid.Engine
{
    public class ThDownConductorExtractionEngine : ThDistributionElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThDownConductorExtractionVisitor();
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);         
            Results = visitor.Results;
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var blkNames = ThEarthingGridCommon.DOWNCONDUCTORBLOCKNAMES;
                var filter = ThSelectionFilterTool.BuildBlockFilter(blkNames);
                var psr = Active.Editor.SelectCrossingPolygon(frame, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    var visitor = new ThDownConductorExtractionVisitor();
                    var elements = new List<ThRawIfcDistributionElementData>();
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.Element<Entity>(o);
                        if (visitor.CheckLayerValid(e) && visitor.IsDistributionElement(e))
                        {
                            visitor.DoExtract(elements, e, Matrix3d.Identity);
                        }
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
