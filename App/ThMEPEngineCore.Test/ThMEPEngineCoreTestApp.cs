using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.BeamInfo.Utils;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.BeamInfo.Business;

namespace ThMEPEngineCore.Test
{
    public class ThMEPEngineCoreTestApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THBE", CommandFlags.Modal)]
        public void ThBuildElement()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var hyperlinks = acadDatabase.Element<Entity>(result.ObjectId).Hyperlinks;
                var buildElement = ThPropertySet.CreateWithHyperlink(hyperlinks[0].Description);
            }
        }

        [CommandMethod("TIANHUACAD", "ThPreprocess", CommandFlags.Modal)]
        public void ThPreprocess()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Polyline segment in pline.Preprocess())
                {
                    segment.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(segment);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThArcBeamOutline", CommandFlags.Modal)]
        public void ThArcBeamOutline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new List<Arc>();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Arc>(obj));
                }

                Polyline polyline = ThArcBeamOutliner.TessellatedOutline(objs[0], objs[1]);
                polyline.ColorIndex = 1;
                acadDatabase.ModelSpace.Add(polyline);
            }
        }

        [CommandMethod("TIANHUACAD", "TestFrame", CommandFlags.Modal)]
        public void TestFrame()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var modelManager = new ThMEPModelManager(Active.Database))
            {
                var result = Active.Editor.GetEntity("请选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);

                modelManager.Acquire(BuildElement.All);
                modelManager.CreateSpatialIndex();

                var frameService = new ThMEPFrameService(modelManager);
                var result_element = frameService.RegionsFromFrame(frame);
                foreach (Entity item in result_element)
                {
                    item.ColorIndex = 2;
                    acadDatabase.ModelSpace.Add(item);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThArcBeamExtraction", CommandFlags.Modal)]
        public void ThArcBeamExtraction()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }

                var arcs = new List<Arc>();
                foreach (var item in objs)
                {
                    if (item is Arc) arcs.Add(item as Arc);
                }

                foreach (var item in CalBeamStruService.ArcBeamPairsExtract(arcs))
                {
                    Polyline polyline = ThArcBeamOutliner.TessellatedOutline(item.Item1, item.Item2);
                    polyline.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(polyline);
                }
            }
        }
    }
}

