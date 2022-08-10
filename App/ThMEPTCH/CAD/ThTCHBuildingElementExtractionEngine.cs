using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public class ThTCHBuildingElementExtractionEngine : ThBuildingElementExtractionEngine
    {
        public ThTCHBuildingElementExtractionEngine()
        {
            Results = new List<ThRawIfcBuildingElementData>();
        }

        public override void Extract(Database database)
        {
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new ThTCHDoorExtractionVisitor(),
                new ThTCHWindowExtractionVisitor(),
                new ThTCHArchWallExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.Extract(database);
            visitors.ForEach(v => Results.AddRange(v.Results));
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new ThTCHDoorExtractionVisitor(),
                new ThTCHWindowExtractionVisitor(),
                new ThTCHArchWallExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.ExtractFromEditor(frame, GetSelectionFilter());
            visitors.ForEach(v => Results.AddRange(v.Results));
        }

        public override void ExtractFromMS(Database database)
        {
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new ThTCHDoorExtractionVisitor(),
                new ThTCHWindowExtractionVisitor(),
                new ThTCHArchWallExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.ExtractFromMS(database);
            visitors.ForEach(v => Results.AddRange(v.Results));
        }

        private SelectionFilter GetSelectionFilter()
        {
            var dxfNames = new string[]
            {
                ThMEPTCHService.DXF_WALL,
                ThMEPTCHService.DXF_OPENING,
            };
            return ThSelectionFilterTool.Build(dxfNames);
        }
    }
}
