using System.Linq;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.Model;
using Linq2Acad;
using ThCADExtension;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.Engine
{
    public class ThTCHArchWallExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var visitor = new ThTCHArchWallExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            Results = new List<ThRawIfcBuildingElementData>();
            Results.AddRange(visitor.Results);
        }

        public void ExtractFromEditor()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var dxfNames = new string[]
                {
                    "TCH_WALL",
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var psr = Active.Editor.SelectAll(filter);
                if (psr.Status == PromptStatus.OK)
                {
                    Results = new List<ThRawIfcBuildingElementData>();
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var e = acadDatabase.Element<Entity>(o);
                        var solid3d = Explode2Solid3d(e);
                        if (solid3d != null)
                        {
                            Results.Add(new ThRawIfcBuildingElementData()
                            {
                                Geometry = solid3d,
                            });
                        }
                    });
                }
            }
        }

        private Solid3d Explode2Solid3d(Entity wall)
        {
            return wall.ExplodeTCHElement()
                .OfType<Solid3d>()
                .FirstOrDefault();
        }
    }

    public class ThTCHArchWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThTCHArchWallExtractionEngine();
            engine.ExtractFromEditor();
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> objs, Point3dCollection polygon)
        {
            Elements.AddRange(objs.Select(o => ThTCHWall.Create(o.Geometry)));
        }
    }
}
