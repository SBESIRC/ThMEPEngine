using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class ThDrainageVPipeExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public List<string> LayerFilter { get; set; } = new List<string>();
        public override void Extract(Database database)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new System.NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            var visitor = new ThDrainageVPipeExtractionVisitor()
            {
                LayerFilter = LayerFilter,
            };
            var extractor = new ThFlowSegmentExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results.AddRange(visitor.Results);
        }

    }
}
