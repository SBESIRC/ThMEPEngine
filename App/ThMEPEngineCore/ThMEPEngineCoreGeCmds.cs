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
using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.GridOperation;
using ThCADExtension;
using ThMEPEngineCore.GridOperation.Model;
using ThMEPEngineCore.Algorithm.ArcAlgorithm;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreGeCmds
    {
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

        [CommandMethod("TIANHUACAD", "THCLEANGRID", CommandFlags.Modal)]
        public void THTESTCleanGrid()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                // 从外参中提取房间
                var frame = acadDatabase.Element<Polyline>(result.ObjectId);

                var axisEngine = new ThAXISLineRecognitionEngine();
                axisEngine.Recognize(acadDatabase.Database, frame.Vertices());
                var retAxisCurves = new List<Curve>();
                foreach (var item in axisEngine.Elements)
                {
                    if (item == null || item.Outline == null)
                        continue;
                    if (item.Outline is Curve curve)
                    {
                        var copy = (Curve)curve.Clone();
                        retAxisCurves.Add(copy);
                    }
                }

                GetStructureInfo(acadDatabase, frame, out List<Polyline> columns);

                GridLineCleanService gridLineClean = new GridLineCleanService();
                gridLineClean.CleanGrid(retAxisCurves, columns, out List<LineGridModel> lineGirds, out List<ArcGridModel> arcGrids);

                //var curves = new List<Curve>(lineGirds.SelectMany(x => x.xLines));
                //curves.AddRange(lineGirds.SelectMany(x => x.yLines));
                //curves.AddRange(arcGrids.SelectMany(x => x.lines));
                //curves.AddRange(arcGrids.SelectMany(x => x.arcLines));
                //var polygons = ThArcPolygonize.Polygonize(curves, 500);
                //foreach (var item in polygons)
                //{
                //    acadDatabase.ModelSpace.Add(item);
                //}
                //foreach (var item in lineGirds)
                //{
                //    foreach (var line in item.xLines)
                //    {
                //        acadDatabase.ModelSpace.Add(line);
                //    }
                //    foreach (var line in item.yLines)
                //    {
                //        acadDatabase.ModelSpace.Add(line);
                //    }
                //}
                //foreach (var item in arcGrids)
                //{
                //    foreach (var line in item.lines)
                //    {
                //        acadDatabase.ModelSpace.Add(line);
                //    }
                //    foreach (var line in item.arcLines)
                //    {
                //        acadDatabase.ModelSpace.Add(line);
                //    }
                //}
            }
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, out List<Polyline> columns)
        {
            var ColumnExtractEngine = new ThColumnExtractionEngine();
            ColumnExtractEngine.Extract(acdb.Database);
            //ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

            ////获取柱
            columns = new List<Polyline>();
            columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
        }
    }
}
