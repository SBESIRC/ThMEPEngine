using System;
using DotNetARX;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.Service;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Command;
using ThMEPWSS.Bussiness.LayoutBussiness;

namespace ThMEPWSS
{
    public class ThSprayCmds
    {
        [CommandMethod("TIANHUACAD", "THPLCD", CommandFlags.Modal)]
        public void ThCreateLayoutPtByLine()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择布置线",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Line)).DxfName,
                RXClass.GetClass(typeof(Polyline)).DxfName
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            List<Curve> lines = new List<Curve>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Curve>(frame);
                    lines.Add(plBack);
                }
            }

            //预处理线
            DBObjectCollection objs = new DBObjectCollection();
            lines.ForEach(x => objs.Add(x));
            var handleLines = ThMEPLineExtension.LineSimplifier(objs, 500, 20.0, 2.0, Math.PI / 180.0);
            objs = new DBObjectCollection();
            handleLines.ForEach(x => objs.Add(x));
            handleLines = objs.ToNTSNodedLineStrings().ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .ToList();

            //清除原有喷淋
            handleLines.ClearSprayByLine();

            //计算喷淋布置点
            LayoutSprayByLineService layoutSprayByLineService = new LayoutSprayByLineService();
            var layoutPts = layoutSprayByLineService.LayoutSprayByLine(handleLines, ThWSSUIService.Instance.Parameter.distance);

            //放置喷头
            //InsertSprinklerService.Insert(layoutPts);
            InsertSprinklerService.InsertTCHSprinkler(layoutPts, ThWSSUIService.Instance.Parameter.layoutType == Model.LayoutType.DownSpray ? 0 : 1);
            
        }

        [CommandMethod("TIANHUACAD", "THPLZX", CommandFlags.Modal)]
        public void ThPTLayout()
        {
            var polylines = ThSprinklerLayoutCmdUtils.GetFrames();
            if (polylines.Count <= 0)
            {
                return;
            }
            if (!ThSprinklerLayoutCmdUtils.CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();
                    plFrame.ClearErrorSprayMark();
                    plFrame.ClearMoveSprayMark();
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    ThSprinklerLayoutCmdUtils.GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);

                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));

                    //生成喷淋对象
                    RayLayoutService layoutDemo = new RayLayoutService();
                    layoutDemo.LayoutSpray(plFrame, columns, beams, walls, holes, matrix, true);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPLPT", CommandFlags.Modal)]
        public void ThAutomaticLayoutSpray()
        {
            using (var cmd = new ThSprinklerLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THPLMQ", CommandFlags.Modal)]
        public void ThCreateBlindArea()
        {
            using (var cmd = new ThSprinklerBlindAreaCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THPLKQ", CommandFlags.Modal)]
        public void ThCreateLayoutArea()
        {
            using (var cmd = new ThSprinklerLayoutAreaCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFGTQ", CommandFlags.Modal)]
        public void THFGTQ()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());

                var results = new DBObjectCollection();
                //天正风管
                var engine = new ThTCHDuctRecognitionEngine();
                engine.Recognize(acadDatabase.Database, frame.Vertices());
                engine.RecognizeMS(acadDatabase.Database, frame.Vertices());
                var temp = engine.Elements;
                temp.OfType<ThIfcDuctSegment>().ForEach(o => results.Add(o.Parameters.Outline));

                //天正配件
                var fittingEngine = new ThTCHFittingRecognitionEngine();
                fittingEngine.Recognize(acadDatabase.Database, frame.Vertices());
                fittingEngine.RecognizeMS(acadDatabase.Database, frame.Vertices());
                var temp1 = fittingEngine.Elbows;
                var temp2 = fittingEngine.Tees;
                var temp3 = fittingEngine.Crosses;
                var temp4 = fittingEngine.Reducings;
                temp1.ForEach(o => results.Add(o.Parameters.Outline));
                temp2.ForEach(o => results.Add(o.Parameters.Outline));
                temp3.ForEach(o => results.Add(o.Parameters.Outline));
                temp4.ForEach(o => results.Add(o.Parameters.Outline));

                //AI风管及其配件
                var thDuctEngine = new ThMEPDuctExtractor();
                thDuctEngine.Recognize(acadDatabase.Database, frame.Vertices());
                thDuctEngine.Elements.OfType<ThIfcDuctSegment>().ForEach(o =>
                {
                    results.Add(o.Parameters.Outline.ToNTSPolygon().ToDbEntity());
                });

                var list = new List<string> { "Elbow", "Tee", "Cross", "Reducing" };
                list.ForEach(o =>
                {
                    var thFittingEngine = new ThMEPFittingExtractor();
                    thFittingEngine.Category = o;
                    thFittingEngine.Recognize(acadDatabase.Database, frame.Vertices());
                    var result = thFittingEngine.Elements;
                    result.OfType<ThIfcDuctElbow>().ForEach(o =>
                    {
                        results.Add(o.Parameters.Outline.ToNTSPolygon().ToDbEntity());
                    });
                    result.OfType<ThIfcDuctTee>().ForEach(o =>
                    {
                        results.Add(o.Parameters.Outline.ToNTSPolygon().ToDbEntity());
                    });
                    result.OfType<ThIfcDuctCross>().ForEach(o =>
                    {
                        results.Add(o.Parameters.Outline.ToNTSPolygon().ToDbEntity());
                    });
                    result.OfType<ThIfcDuctReducing>().ForEach(o =>
                    {
                        results.Add(o.Parameters.Outline.ToNTSPolygon().ToDbEntity());
                    });
                });

                results.OfType<Entity>().ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
    }
}
