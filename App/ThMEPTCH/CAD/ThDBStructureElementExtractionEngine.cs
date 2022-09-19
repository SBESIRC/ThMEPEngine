using System;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public class ThDBStructureElementExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            //墙,柱,梁,板
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new THDBWallExtractionVisitor(),
                new THDBColumnExtractionVisitor(),
                new THDBBeamExtractionVisitor(),
                new THDBMarkExtractionVisitor(),
                new THDBSlabPLExtractionVisitor(),
                new THDBSlabHatchExtractionVisitor(),
                new THDBSlabBTHExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = new List<ThRawIfcBuildingElementData>();
            visitors.ForEach(v => Results.AddRange(v.Results));
        }

        public void ExtractFromMS(Database database, List<Polyline> outLines)
        {
            //墙,柱,梁,板
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new THDBWallExtractionVisitor(),
                new THDBColumnExtractionVisitor(),
                new THDBBeamExtractionVisitor(),
                new THDBMarkExtractionVisitor(),
                new THDBSlabPLExtractionVisitor(),
                new THDBSlabHatchExtractionVisitor(),
                new THDBSlabBTHExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            Results = new List<ThRawIfcBuildingElementData>();
            var AllBuildingElements = new Dictionary<Entity, ThRawIfcBuildingElementData>();
            visitors.ForEach(v => v.Results.ForEach(e => AllBuildingElements.Add(e.Geometry, e)));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(AllBuildingElements.Keys.ToCollection());
            outLines.ForEach(outLine =>
            {
                var objs = spatialIndex.SelectCrossingPolygon(outLine);
                foreach (Entity obj in objs)
                {
                    if (!Results.Contains(AllBuildingElements[obj]))
                    {
                        Results.Add(AllBuildingElements[obj]);
                    }
                }
            });
        }
    }
}
