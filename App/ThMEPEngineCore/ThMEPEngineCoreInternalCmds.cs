using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreInternalCmds
    {
        [CommandMethod("TIANHUACAD", "THBUFFER", CommandFlags.Modal)]
        public void ThBuffer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }

                foreach (Entity obj in objs.Buffer(result2.Value))
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THLINEMERGE", CommandFlags.Modal)]
        public void ThLineMerge()
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
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }

                foreach (Entity obj in objs.LineMerge())
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THCENTERLINE", CommandFlags.Modal)]
        public void ThCenterline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n请输入差值距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(pline, result2.Value);
                foreach (Entity centerline in centerlines)
                {
                    acadDatabase.ModelSpace.Add(centerline);
                    centerline.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThSimplify", CommandFlags.Modal)]
        public void ThSimplify()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n请输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var options = new PromptKeywordOptions("\n请指定简化方式")
                {
                    AllowNone = true
                };
                options.Keywords.Add("DP", "DP", "DP(D)");
                options.Keywords.Add("VW", "VW", "VW(V)");
                options.Keywords.Add("TP", "TP", "TP(T)");
                options.Keywords.Default = "DP";
                var result3 = Active.Editor.GetKeywords(options);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline pline = null;
                double distanceTolerance = result2.Value;
                var obj = acadDatabase.Element<Polyline>(result.ObjectId);
                if (result3.StringResult == "DP")
                {
                    pline = obj.DPSimplify(distanceTolerance);
                }
                else if (result3.StringResult == "VW")
                {
                    pline = obj.VWSimplify(distanceTolerance);
                }
                else if (result3.StringResult == "TP")
                {
                    pline = obj.TPSimplify(distanceTolerance);
                }
                acadDatabase.ModelSpace.Add(pline);
                pline.SetDatabaseDefaults();
            }
        }
    }
}
