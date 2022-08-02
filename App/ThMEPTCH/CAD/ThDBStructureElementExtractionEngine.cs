using System;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

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
            //墙，柱
            var visitors = new ThBuildingElementExtractionVisitor[]{
                new THDBWallExtractionVisitor(),
                new THDBColumnExtractionVisitor(),
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitors);
            extractor.ExtractFromMS(database);
            Results = new List<ThRawIfcBuildingElementData>();
            visitors.ForEach(v => Results.AddRange(v.Results));
            //梁
            ThRawBeamRecognitionEngine beamEngine = new ThRawBeamRecognitionEngine();
            beamEngine.Recognize(database, new Point3dCollection());
            beamEngine.Elements.ForEach(o => Results.Add(CreatStructureEntity(o as ThMEPEngineCore.Model.ThIfcLineBeam)));
            //板
        }

        private ThRawIfcBuildingElementData CreatStructureEntity(ThMEPEngineCore.Model.ThIfcLineBeam entity)
        {
            return new ThRawIfcBuildingElementData()
            {
                Geometry = entity.Outline,
                Data = new THStructureBeam()
                {
                    Outline = entity.Outline as Polyline,
                    Height = entity.Height,
                }
            };
        }
    }
}
