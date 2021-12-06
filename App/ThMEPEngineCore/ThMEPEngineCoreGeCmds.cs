using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using System.Collections.Generic;
using ThMEPEngineCore.UCSDivisionService;
using ThMEPEngineCore.Engine;

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

        /// <summary>
        /// ucs分区
        /// </summary>
        [CommandMethod("TIANHUACAD", "THUCSDIV", CommandFlags.Modal)]
        public void ThUcsDisivision()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                foreach (var frame in frameLst)
                {   
                    GetStructureInfo(frame, out List<Polyline> columns);

                    //区域分割
                    UCSService uCSService = new UCSService();
                    var ucsInfo = uCSService.UcsDivision(columns, frame);
                    foreach (var item in ucsInfo)
                    {
                        //acadDatabase.ModelSpace.Add(item.Key);
                    }
                }
            }
        }

        /// <summary>
        /// 获取构建
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(Polyline polyline, out List<Polyline> columns)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                ////获取柱
                var ColumnExtractEngine = new ThColumnExtractionEngine();
                ColumnExtractEngine.Extract(acdb.Database);
                //ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ColumnEngine = new ThColumnRecognitionEngine();
                ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());
                
                columns = new List<Polyline>();
                columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                var objs = new DBObjectCollection();
                columns.ForEach(x => objs.Add(x));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
            }
        }
    }
}
