using System;
using AcHelper;
using Linq2Acad;
using GeoAPI.Geometries;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.BeamInfo.Utils;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Distance;

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

                Polyline polyline = ThArcBeamOutliner.Outline(objs[0], objs[1]);
                polyline.ColorIndex = 2;
                acadDatabase.ModelSpace.Add(polyline);
            }
        }

        [CommandMethod("TIANHUACAD", "TestFrame", CommandFlags.Modal)]
        public void TestFrame()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);

                ThMEPFrameService.Instance.InitializeWithDb(acadDatabase.Database);
                var result_element = ThMEPFrameService.Instance.RegionsFromFrame(frame);
                
                foreach (Entity item in result_element)
                {
                    item.ColorIndex = 2;
                    item.Highlight();
                    acadDatabase.ModelSpace.Add(item);
                }
            }
        }
    }
}

