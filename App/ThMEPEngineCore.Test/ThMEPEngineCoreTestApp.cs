using System;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.BeamInfo.Utils;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

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
            {
                var result = Active.Editor.GetEntity("\n请选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var engine = ThBeamConnectRecogitionEngine.ExecuteRecognize(
                    acadDatabase.Database, new Point3dCollection());
                var frameService = new ThMEPFrameService(engine);
                var frame = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Entity item in frameService.RegionsFromFrame(frame))
                {
                    item.ColorIndex = 2;
                    acadDatabase.ModelSpace.Add(item);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPOLYGONPARTITION", CommandFlags.Modal)]
        public void THPolygonPartition()
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
                    objs.Add(acadDatabase.Element<Polyline>(obj));
                }
                foreach (Polyline item in objs)
                {
                    var polylines = ThMEPPolygonPartitioner.PolygonPartition(item);
                    //polylines.ColorIndex = 1;
                    //acadDatabase.ModelSpace.Add(polylines);
                    foreach (var obj in polylines)
                    {
                        obj.ColorIndex = 1;
                        acadDatabase.ModelSpace.Add(obj);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThLineProcessing", CommandFlags.Modal)]
        public void ThLineProcessing()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new DBObjectCollection();
                result.Value.GetObjectIds().ForEach(o => objs.Add(acadDatabase.Element<Curve>(o)));
                var lines = ThMEPLineExtension.LineSimplifier(objs, 20.0, 2.0, Math.PI / 180.0);
                lines.ForEach(o =>
                {
                    o.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(o);
                });
            }
        }
    }
}