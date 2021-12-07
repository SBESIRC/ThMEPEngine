using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.UCSDivisionService;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreGeCmds
    {
        [CommandMethod("TIANHUACAD", "THKJZX", CommandFlags.Modal)]
        public void THKJZX()
        {
#if (ACAD2016 || ACAD2018)
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

                ThMEPEngineCoreLayerUtils.CreateAICenterLineLayer(acadDatabase.Database);
                objs.BuildArea()
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        ThMEPPolygonService.CenterLine(e)
                        .ToCollection()
                        .LineMerge()
                        .OfType<Entity>()
                        .ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.Layer = ThMEPEngineCoreLayerUtils.CENTERLINE;
                        });
                    });
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THPSKELETON", CommandFlags.Modal)]
        public void THPSKELETON()
        {
#if (ACAD2016 || ACAD2018)
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
                objs.BuildArea().OfType<Entity>().ForEach(e =>
                {
                    ThMEPPolygonService.StraightSkeleton(e).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                    });
                });
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THPPARTITION", CommandFlags.Modal)]
        public void THPPARTITION()
        {
#if (ACAD2016 || ACAD2018)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var psr = Active.Editor.GetSelection();
                if (psr.Status != PromptStatus.OK)
                {
                    return;
                }

                var pko = new PromptKeywordOptions("\n请指定分割方式")
                {
                    AllowNone = true
                };
                pko.Keywords.Add("UCS", "UCS", "UCS(U)");
                pko.Keywords.Add("RADIUS", "RADIUS", "RADIUS(R)");
                pko.Keywords.Default = "RADIUS";
                var pe = Active.Editor.GetKeywords(pko);
                if (pe.Status != PromptStatus.OK)
                {
                    return;
                }

                var pdr = Active.Editor.GetDistance("\n请输入参数");
                if (pdr.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in psr.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }

                objs = objs.BuildArea();
                foreach (Entity obj in objs)
                {
                    if (pe.StringResult == "RADIUS")
                    {
                        ThMEPPolygonService.Partition(obj, pdr.Value).Keys.OfType<Polyline>().ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.ColorIndex = 1;
                        });

                    }
                    else if (pe.StringResult == "UCS")
                    {
                        ThMEPPolygonService.PartitionUCS(obj, pdr.Value).Keys.OfType<Polyline>().ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.ColorIndex = 1;
                        });
                    }
                }
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }
    }
}
