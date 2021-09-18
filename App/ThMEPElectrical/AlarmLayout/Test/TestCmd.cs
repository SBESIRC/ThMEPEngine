using AcHelper.Commands;
using System;
using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using System.Collections.Generic;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.AlarmLayout.Command;
using ThMEPElectrical.AlarmSensorLayout.Data;

namespace ThMEPElectrical.AlarmLayout.Test
{
    class TestCmd
    {
        [CommandMethod("TIANHUACAD", "THFALCL", CommandFlags.Modal)]
        public void THFALCL()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择布置区域框线",
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
                    Application.ShowAlertDialog("请选择正确的带洞多边形！");
                    return;
                }

                Point3d ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acdb.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp, transformer);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }

                var frame = frameList.OrderByDescending(x => x.Area).First();
                var holeList = getPoly(frame, "AI-房间框线", transformer, true);
                var layoutList = getPoly(frame, "AI-可布区域", transformer, false);
                var wallList = getPoly(frame, "AI-墙", transformer, false);
                var columns = new List<Polyline>();
                var prioritys = new List<Polyline>();

                PromptDoubleResult equipRadius = Active.Editor.GetDistance("\n设备覆盖半径");
                if (equipRadius.Status != PromptStatus.OK)
                {
                    return;
                }
                PromptDoubleResult straightLineMode = Active.Editor.GetDistance("\n是否为灯（0/1）");
                if (straightLineMode.Status != PromptStatus.OK)
                {
                    return;
                }
                var layoutCmd = new FireAlarmSystemLayoutCommand();
                layoutCmd.radius = equipRadius.Value;
                layoutCmd.frame = frame;
                layoutCmd.holeList = holeList;
                layoutCmd.layoutList = layoutList;
                layoutCmd.wallList = wallList;
                layoutCmd.columns = columns;
                layoutCmd.prioritys = prioritys;
                layoutCmd.equipmentType = straightLineMode.Value == 0 ? BlindType.CoverArea : BlindType.VisibleArea;
                layoutCmd.Execute();
            }
        }

        private static List<Polyline> getPoly(Polyline frame, string sLayer, ThMEPOriginTransformer transformer, bool onlyContains)
        {
            var layoutArea = ExtractPolyline(frame, sLayer, transformer, onlyContains);
            var layoutList = layoutArea.Select(x => x.Value).ToList();
            return layoutList;
        }
        private static Dictionary<Polyline, Polyline> ExtractPolyline(Polyline bufferFrame, string LayerName, ThMEPOriginTransformer transformer, bool onlyContain)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line = acadDatabase.ModelSpace
                      .OfType<Polyline>()
                      .Where(o => o.Layer == LayerName);

                List<Polyline> lineList = line.Select(x => x.WashClone() as Polyline).ToList();

                var plInFrame = new Dictionary<Polyline, Polyline>();

                foreach (Polyline pl in lineList)
                {
                    if (pl != null)
                    {
                        var plTrans = pl.Clone() as Polyline;

                        transformer.Transform(plTrans);
                        plInFrame.Add(pl, plTrans);
                    }
                }
                if (onlyContain == false)
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value) || bufferFrame.Intersects(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                else
                {
                    plInFrame = plInFrame.Where(o => bufferFrame.Contains(o.Value)).ToDictionary(x => x.Key, x => x.Value);
                }
                return plInFrame;
            }
        }
        private static Polyline processFrame(Polyline frame, ThMEPOriginTransformer transformer)
        {
            var tol = 1000;
            //获取外包框
            var frameClone = frame.WashClone() as Polyline;
            //处理外包框
            transformer.Transform(frameClone);
            Polyline nFrame = ThMEPFrameService.NormalizeEx(frameClone, tol);

            return nFrame;
        }
    }
}