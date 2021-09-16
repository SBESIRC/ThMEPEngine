using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Service;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Command;
using ThMEPWSS.Bussiness.LayoutBussiness;
using ThMEPWSS.Sprinkler.Analysis;

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
            InsertSprinklerService.Insert(layoutPts);
        }

        [CommandMethod("TIANHUACAD", "THPLZX", CommandFlags.Modal)]
        public void ThPTLayout()
        {
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

            if (!ThSprinklerLayoutCmdUtils.CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    polylines.Add(plFrame);
                }

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

        [CommandMethod("TIANHUACAD", "THPTJH", CommandFlags.Modal)]
        public void THPTJH()
        {
            using (var cmd = new ThSprinklerCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THPL18", CommandFlags.Modal)]
        public void THPL18()
        {
            using (var cmd = new ThSprinklerDistanceCheckCmd())
            {
                cmd.Execute();
            }
        }
    }
}
