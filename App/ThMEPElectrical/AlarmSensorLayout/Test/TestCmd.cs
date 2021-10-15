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
using DotNetARX;
using NetTopologySuite.Geometries;

using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Sensorlayout;

namespace ThMEPElectrical.AlarmSensorLayout.Test
{
    class TestCmd
    {
        //public BeamSensorLayout.BeamSensorLayout sensorLayout;

        public BeamSensorOpt sensorOpt;
        public List<ObjectId> lineId_list { get; set; } = new List<ObjectId>();
        public List<ObjectId> pointId_list { get; set; } = new List<ObjectId>();
        public List<ObjectId> UCS_List { get; set; } = new List<ObjectId>();
        public List<ObjectId> blind_List { get; set; } = new List<ObjectId>();
        public List<ObjectId> detect_List { get; set; } = new List<ObjectId>();
        public Point3d center { get; set; }
        public double angle { get; set; }

        [CommandMethod("TIANHUACAD", "THASLT", CommandFlags.Modal)]
        public void THASLT()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //选择点
                var per = Active.Editor.GetEntity("请选择Mpolygon");
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }
                var frame = acadDatabase.Element<MPolygon>(per.ObjectId);
                Polygon polygon = frame.ToNTSPolygon();
                Polyline shell = polygon.Shell.ToDbPolyline();
                acadDatabase.ModelSpace.Add(shell);
                foreach (var hole in polygon.Holes)
                    acadDatabase.ModelSpace.Add(hole.ToDbPolyline());
            }
        }

        [CommandMethod("TIANHUACAD", "THASLR", CommandFlags.Modal)]
        public void THASLR()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
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
                    return;
                }

                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acadDatabase.Element<Polyline>(obj);
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

                var layoutHoleList = getPoly(frame, "AI-可布置区域洞", transformer, false);

                List<MPolygon> layouts = new List<MPolygon>();
                foreach (var layout in layoutList)
                {
                    Polygon polygon = layout.ToNTSPolygon();
                    foreach (var layouthole in layoutHoleList)
                        polygon = polygon.Difference(layouthole.ToNTSPolygon()) as Polygon;
                    layouts.Add(polygon.ToDbMPolygon());
                }

                var rst = Active.Editor.GetDouble(new PromptDoubleOptions("Input protection radius:"));
                if (rst.Status != PromptStatus.OK)
                    return;
                var radius = rst.Value;

                var layoutCmd = new AlarmSensorLayoutCmd();
                layoutCmd.frame = frame;
                layoutCmd.holeList = holeList;
                layoutCmd.layoutList = layouts;
                layoutCmd.wallList = wallList;
                layoutCmd.protectRadius = radius;
                layoutCmd.equipmentType = BlindType.VisibleArea;
                layoutCmd.Execute();
            }
        }
        [CommandMethod("TIANHUACAD", "THASLM", CommandFlags.Modal)]
        public void THASLM()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
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
                    return;
                }

                var ptOri = new Point3d();
                var transformer = new ThMEPOriginTransformer(ptOri);
                var frameList = new List<Polyline>();

                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acadDatabase.Element<Polyline>(obj);
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

                var layoutHoleList = getPoly(frame, "AI-可布置区域洞", transformer, false);

                List<MPolygon> layouts = new List<MPolygon>();
                foreach (var layout in layoutList)
                {
                    Polygon polygon = layout.ToNTSPolygon();
                    foreach (var layouthole in layoutHoleList)
                        polygon = polygon.Difference(layouthole.ToNTSPolygon()) as Polygon;
                    layouts.Add(polygon.ToDbMPolygon());
                }

                SpaceDivider groupOpt = new SpaceDivider();
                groupOpt.Compute(frame, layouts);

                foreach (var id in UCS_List)
                {
                    id.Erase();
                }
                UCS_List.Clear();

                //foreach (var layout in groupOpt.layouts)
                //{
                //    var dblayout = layout.ent;
                //    dblayout.ColorIndex = layout.GroupID;
                //    var id = acadDatabase.ModelSpace.Add(dblayout);
                //    circle_List.Add(id);

                //    var dbline = new Line(layout.ent.GetCentroidPoint(), layout.ent.GetCentroidPoint() + new Vector3d(300, 0, 0));
                //    dbline.Rotate(dbline.StartPoint, layout.angle / 180 * Math.PI);
                //    dbline.ColorIndex = layout.GroupID;
                //    id = acadDatabase.ModelSpace.Add(dbline);
                //    circle_List.Add(id);
                //}
                foreach (var group in groupOpt.UCSs)
                {
                    var dbucs = group.Key;
                    dbucs.ColorIndex = 5;

                    //var dbline = new Line(dbucs.GetCentroidPoint(), dbucs.GetCentroidPoint() + new Vector3d(3000, 0, 0));
                    //dbline.Rotate(dbline.StartPoint, group.Value / 180 * Math.PI);
                    //dbline.ColorIndex = 5;
                    //var id = acadDatabase.ModelSpace.Add(dbline);
                    //UCS_List.Add(id);

                    //dbucs.Rotate(dbucs.GetCentroidPoint(),-group.Value / 180 * Math.PI);
                    var id = acadDatabase.ModelSpace.Add(dbucs);
                    UCS_List.Add(id);
                }
            }
        }

        //private void ShowLines()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        foreach (var id1 in lineId_list)
        //        {
        //            id1.Erase();
        //        }
        //        lineId_list.Clear();
        //        Point3d p0, p1;
        //        //画上边线
        //        for (int i = 0; i < sensorOpt.vLines.Count; i++)
        //        {
        //            p0 = new Point3d(sensorOpt.Positions[0][i].X, sensorOpt.Positions[0][i].Y + 5800, 0);
        //            p1 = new Point3d(sensorOpt.Positions[0][i].X, sensorOpt.Positions[0][i].Y, 0);
        //            var line = new Line(p0, p1);
        //            line.ColorIndex = 1;
        //            var id = acadDatabase.ModelSpace.Add(line);
        //            id.Rotate(center, -angle);
        //            lineId_list.Add(id);
        //        }
        //        //画竖线
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //        {
        //            for (int j = 0; j < sensorOpt.vLines.Count; j++)
        //            {
        //                p0 = new Point3d(sensorOpt.Positions[i][j].X, sensorOpt.Positions[i][j].Y, 0);
        //                if (i != sensorOpt.hLines.Count - 1)
        //                    p1 = new Point3d(sensorOpt.Positions[i + 1][j].X, sensorOpt.Positions[i + 1][j].Y, 0);
        //                else
        //                    p1 = new Point3d(sensorOpt.Positions[i][j].X, sensorOpt.Positions[i][j].Y - 5800, 0);
        //                var line = new Line(p0, p1);
        //                line.ColorIndex = 1;
        //                var id = acadDatabase.ModelSpace.Add(line);
        //                id.Rotate(center, -angle);
        //                lineId_list.Add(id);
        //            }
        //        }
        //        //画左边线
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //        {
        //            p0 = new Point3d(sensorOpt.Positions[i][0].X-5800, sensorOpt.Positions[i][0].Y, 0);
        //            p1 = new Point3d(sensorOpt.Positions[i][0].X, sensorOpt.Positions[i][0].Y, 0);
        //            var line = new Line(p0, p1);
        //            line.ColorIndex = 1;
        //            var id = acadDatabase.ModelSpace.Add(line);
        //            id.Rotate(center, -angle);
        //            lineId_list.Add(id);
        //        }
        //        //画横线
        //        for (int i = 0; i < sensorOpt.hLines.Count; i++)
        //        {
        //            for (int j = 0; j < sensorOpt.vLines.Count; j++)
        //            {
        //                p0 = new Point3d(sensorOpt.Positions[i][j].X, sensorOpt.Positions[i][j].Y, 0);
        //                if (j != sensorOpt.vLines.Count - 1)
        //                    p1 = new Point3d(sensorOpt.Positions[i][j + 1].X, sensorOpt.Positions[i][j + 1].Y, 0);
        //                else
        //                    p1 = new Point3d(sensorOpt.Positions[i][j].X+5800, sensorOpt.Positions[i][j].Y, 0);
        //                var line = new Line(p0, p1);
        //                line.ColorIndex = 1;
        //                var id = acadDatabase.ModelSpace.Add(line);
        //                id.Rotate(center, -angle);
        //                lineId_list.Add(id);
        //            }
        //        }
        //    }
        //}

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

        [CommandMethod("TIANHUACAD", "THAreaGridLayoutTest", CommandFlags.Modal)]
        public void THAreaGridLayoutTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractBlkList = FireAlarm.ThFaCommon.BlkNameList;
                var cleanBlkName = new List<string>() { FireAlarm.ThFaCommon.BlkName_Smoke, FireAlarm.ThFaCommon.BlkName_Heat, FireAlarm.ThFaCommon.BlkName_Smoke_ExplosionProf, FireAlarm.ThFaCommon.BlkName_Heat_ExplosionProf };
                var avoidBlkName = FireAlarm.ThFaCommon.BlkNameList.Where(x => cleanBlkName.Contains(x) == false).ToList();

                //画框，提数据，转数据
                var pts = FireAlarm.Service.ThFireAlarmUtils.getFrame();
                if (pts.Count == 0)
                {
                    return;
                }

                //保护半径
                var rst = Active.Editor.GetDouble(new PromptDoubleOptions("Input protection radius:"));
                if (rst.Status != PromptStatus.OK)
                    return;
                var radius = rst.Value;

                //提取梁（对可布区域有影响。默认为true）
                var _referBeam = true;
                //图块比例默认100
                var _scale = 100;
                //提取数据
                var geos = FireAlarm.Service.ThFireAlarmUtils.getSmokeData(pts, extractBlkList, _referBeam);
                if (geos.Count == 0)
                {
                    return;
                }

                //转回原点
                var transformer = FireAlarm.Service.ThFireAlarmUtils.transformToOrig(pts, geos);
                ////不转回原点
                //var newPts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
                //newPts.Add(new Autodesk.AutoCAD.Geometry.Point3d());
                //var transformer = ThFireAlarmUtils.transformToOrig(newPts, geos);

                var dataQuery = new FireAlarmSmokeHeat.Data.ThSmokeDataQueryService(geos, cleanBlkName, avoidBlkName);
                //洞,必须先做找到框线
                dataQuery.analysisHoles();

                //墙，柱，可布区域，避让分配到房间框线
                dataQuery.ClassifyData();

                //扩大避让区域防止最终块重叠
                var priorityExtend = FireAlarmSmokeHeat.Service.ThFaAreaLayoutParamterCalculationService.getPriorityExtendValue(cleanBlkName, _scale);
                dataQuery.extendPriority(priorityExtend);

                //debug 打图纸用
                foreach (var frame in dataQuery.FrameList)
                {
                    FireAlarm.Service.DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
                    FireAlarm.Service.DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
                    FireAlarm.Service.DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), "l0PlaceCoverage", 200);
                }

                for (int i = 0; i < dataQuery.FrameList.Count; i++)
                {
                    var frame = dataQuery.FrameList[i];

                    var layoutCmd = new AlarmSensorLayoutCmd();
                    layoutCmd.frame = frame;
                    layoutCmd.holeList = dataQuery.FrameHoleList[frame];
                    layoutCmd.layoutList = dataQuery.FrameLayoutList[frame];
                    layoutCmd.wallList = dataQuery.FrameWallList[frame];
                    layoutCmd.columns = dataQuery.FrameColumnList[frame];
                    layoutCmd.prioritys = dataQuery.FramePriorityList[frame];
                    layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
                    layoutCmd.protectRadius = radius;
                    layoutCmd.equipmentType = BlindType.CoverArea;

                    layoutCmd.Execute();
                }
            }
        }
    }
}
