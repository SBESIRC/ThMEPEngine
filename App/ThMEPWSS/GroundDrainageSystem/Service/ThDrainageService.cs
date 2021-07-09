﻿namespace ThMEPWSS.Pipe.Service
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using static ThMEPWSS.DebugNs.ThPublicMethods;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.DebugNs;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;
    using ThMEPWSS.DebugNs;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThUtilExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;

    public class DrainageDrawingData
    {
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public HashSet<string> CleaningPorts;
        public Dictionary<string, string> Outlets;
        public HashSet<string> WrappingPipes;
        public List<ThDrainageService.WLGrouper.ToiletGrouper> toiletGroupers;

        //1)	必然负担一个洗涤盆下水点。不用读图上任何信息；
        //2)	若厨房内存在任何地漏图块或洗衣机图块（图块名称包含A-Toilet-9），则必然负担一个地漏下水点。
        //最多同时负担一个洗涤盆下水店和一个地漏下水点。
        public HashSet<string> KitchenOnlyFls;

        //1)	图层名称包含“W-DRAI-EPQM”且半径大于40的圆视为洗手台下水点
        //2)	地漏图块视为地漏下水点
        public HashSet<string> BalconyOnlyFLs;

        //综合厨房和阳台的点位
        public HashSet<string> KitchenAndBalconyFLs;

        //FL+TL
        //若FL附近300的范围内存在TL且该TL不属于PL，则将FL与TL设为一组。系统图上和PL+TL的唯一区别是废水管要表达卫生洁具。
        public HashSet<string> MustHaveCleaningPort;

        //6.3.8	水管井的FL
        //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
        //水管井的判断：
        //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
        public HashSet<string> WaterPipeWellFLs;

        public HashSet<string> MustHaveFloorDrains;


        public void Init()
        {
            VerticalPipeLabels ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            CleaningPorts ??= new HashSet<string>();
            Outlets ??= new Dictionary<string, string>();
            WrappingPipes ??= new HashSet<string>();
            toiletGroupers ??= new List<ThDrainageService.WLGrouper.ToiletGrouper>();
            KitchenOnlyFls ??= new HashSet<string>();
            BalconyOnlyFLs ??= new HashSet<string>();
            KitchenAndBalconyFLs ??= new HashSet<string>();
            MustHaveCleaningPort ??= new HashSet<string>();
            MustHaveFloorDrains ??= new HashSet<string>();
            WaterPipeWellFLs ??= new HashSet<string>();
        }
    }

    public partial class DrainageService
    {
        public AcadDatabase adb;
        public DrainageSystemDiagram DrainageSystemDiagram;
        public List<ThStoreysData> Storeys;
        public DrainageGeoData GeoData;
        public DrainageCadData CadDataMain;
        public List<DrainageCadData> CadDatas;
        public List<DrainageDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static void TestDrawingDatasCreation(DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            TestDrawingDatasCreation(geoData, cadDataMain, cadDatas);
        }
        public static DrainageGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return null;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = new DrainageGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }
        public static void DrawDrainageSystemDiagram2()
        {
            if (commandContext != null) return;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            //if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = new DrainageGeoData();
                    geoData.Init();
                    CollectGeoData(range, adb, geoData);
                    ThDrainageService.PreFixGeoData(geoData);
                    ThDrainageService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = DrainageCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new DrainageService()
                    {
                        adb = adb,
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    //DU.Draw();
                    //DU.Dispose();
                    if (sv.DrainageSystemDiagram == null) { }

                    //DU.Dispose();
                    //sv.RainSystemDiagram.Draw(basePt);
                    //DU.Draw(adb);
                    //Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                }
                finally
                {
                    DU.Dispose();
                }
            }
        }
        public static void CollectGeoData(Geometry range, AcadDatabase adb, DrainageGeoData geoData, ThMEPWSS.Pipe.Service.ThDrainageService.CommandContext ctx)
        {
            var cl = NewMethod(adb, geoData);
            cl.CollectStoreys(range, ctx);
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = NewMethod(adb, geoData);
            cl.CollectStoreys(range);
        }

        private static ThDrainageSystemServiceGeoCollector NewMethod(AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = new ThDrainageSystemServiceGeoCollector() { adb = adb, geoData = geoData };
            cl.CollectEntities();
            cl.CollectDLines();
            cl.CollectVLines();
            cl.CollectLabelLines();
            cl.CollectCTexts();
            cl.CollectVerticalPipes();
            cl.CollectWrappingPipes();
            cl.CollectWaterPorts();
            cl.CollectFloorDrains();
            cl.CollectCleaningPorts();
            cl.CollectWashingMachines();
            return cl;
        }

        public static void DrawDrainageSystemDiagram3()
        {
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            DU.Dispose();
            if (commandContext == null) return;
            if (commandContext.StoreyContext == null) return;
            if (commandContext.range == null) return;
            if (commandContext.StoreyContext.thStoreysDatas == null) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var range = commandContext.range;
                    var storeys = commandContext.StoreyContext.thStoreysDatas;
                    var geoData = new DrainageGeoData();
                    geoData.Init();
                    CollectGeoData(range, adb, geoData);
                    ThDrainageService.PreFixGeoData(geoData);
                    ThDrainageService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = DrainageCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new DrainageService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();

                }
                finally
                {
                    DU.Dispose();
                }

            }

        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == 0) return false;
            }
            return true;
        }
        public static ThRainSystemService.CommandContext commandContext => ThRainSystemService.commandContext;
        public void CreateDrawingDatas()
        {
            //roomData ??= CollectRoomData(adb);
            TestDrawingDatasCreation(GeoData, CadDataMain, CadDatas);
        }
        static List<GLineSegment> ExplodeGLineSegments(Geometry geo)
        {
            static IEnumerable<GLineSegment> enumerate(Geometry geo)
            {
                if (geo is LineString ls)
                {
                    if (ls.NumPoints == 2) yield return new GLineSegment(ls[0].ToPoint2d(), ls[1].ToPoint2d());
                    else if (ls.NumPoints > 2)
                    {
                        for (int i = 0; i < ls.NumPoints - 1; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + 1].ToPoint2d());
                        }
                    }
                }
                else if (geo is GeometryCollection colle)
                {
                    foreach (var _geo in colle.Geometries)
                    {
                        foreach (var __geo in enumerate(_geo))
                        {
                            yield return __geo;
                        }
                    }
                }
            }
            return enumerate(geo).ToList();
        }
#pragma warning disable
        public static void DrawGeoData(DrainageGeoData geoData)
        {
            foreach (var s in geoData.Storeys) DU.DrawRectLazy(s).ColorIndex = 1;
            foreach (var o in geoData.LabelLines) DU.DrawLineSegmentLazy(o).ColorIndex = 1;
            foreach (var o in geoData.Labels)
            {
                DU.DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = 2;
                DU.DrawRectLazy(o.Boundary).ColorIndex = 2;
            }
            foreach (var o in geoData.VerticalPipes) DU.DrawRectLazy(o).ColorIndex = 3;
            foreach (var o in geoData.FloorDrains) DU.DrawRectLazy(o).ColorIndex = 6;
            foreach (var o in geoData.WaterPorts) DU.DrawRectLazy(o).ColorIndex = 7;
            {
                var cl = Color.FromRgb(4, 229, 230);
                foreach (var o in geoData.DLines) DU.DrawLineSegmentLazy(o).Color = cl;
            }
        }

        const double MAX_SHORTTRANSLATOR_DISTANCE = 300;//150;

        public static void CreateDrawingDatas(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas, out StringBuilder sb, out List<DrainageDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData = null)
        {
            DrawingTransaction.Cur.AbleToDraw = false;
            roomData ??= new List<KeyValuePair<string, Geometry>>();
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DU.DrawRectLazy(s).ColorIndex = 1;
            }
            sb = new StringBuilder(8192);
            drDatas = new List<DrainageDrawingData>();
            for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
            {
                sb.AppendLine($"===框{storeyI}===");
                var drData = new DrainageDrawingData();
                drData.Init();
                var item = cadDatas[storeyI];

                {
                    var maxDis = 8000;
                    var angleTolleranceDegree = 1;
                    var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > 0).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
                        maxDis, angleTolleranceDegree).ToList();
                    geoData.DLines.AddRange(lines);
                    var dlineCvt = DrainageCadData.ConvertDLinesF();
                    var _lines = lines.Select(dlineCvt).ToList();
                    cadDataMain.DLines.AddRange(_lines);
                    item.DLines.AddRange(_lines);
                }

                var lbDict = new Dictionary<Geometry, string>();
                var notedPipesDict = new Dictionary<Geometry, string>();
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesf = F(labelLinesGeos);
                var shortTranslatorLabels = new HashSet<string>();
                var longTranslatorLabels = new HashSet<string>();
                var dlinesGroups = GG(item.DLines);
                var dlinesGeos = GeosGroupToGeos(dlinesGroups);
                var vlinesGroups = GG(item.VLines);
                var vlinesGeos = GeosGroupToGeos(vlinesGroups);
                var wrappingPipesf = F(item.WrappingPipes);

                //自动补上缺失的立管
                {
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        if (!ThDrainageService.IsMaybeLabelText(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                        var lst = labellinesf(label);
                        if (lst.Count == 1)
                        {
                            var labelline = lst[0];
                            if (pipesf(GeoFac.CreateGeometry(label, labelline)).Count == 0)
                            {
                                var lines = ExplodeGLineSegments(labelline);
                                var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(5)).ToList(), label, radius: 5);
                                if (points.Count == 1)
                                {
                                    var pt = points[0];
                                    var r = GRect.Create(pt, 55);
                                    geoData.VerticalPipes.Add(r);
                                    var pl = r.ToPolygon();
                                    cadDataMain.VerticalPipes.Add(pl);
                                    item.VerticalPipes.Add(pl);
                                    DU.DrawTextLazy("脑补的", pl.GetCenter());
                                }
                            }
                        }
                    }
                }


                DU.DrawTextLazy($"===框{storeyI}===", geoData.Storeys[storeyI].LeftTop);
                foreach (var o in item.LabelLines)
                {
                    DU.DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = 1;
                }
                foreach (var pl in item.Labels)
                {
                    var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                    var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var _pl = DU.DrawRectLazy(m.Boundary);
                    _pl.ColorIndex = 2;
                }
                foreach (var o in item.VerticalPipes)
                {
                    DU.DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = 3;
                }
                foreach (var o in item.FloorDrains)
                {
                    DU.DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = 6;
                }
                foreach (var o in item.WaterPorts)
                {
                    DU.DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = 7;
                    DU.DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WashingMachines)
                {
                    var e = DU.DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = 1;
                }
                foreach (var o in item.CleaningPorts)
                {
                    var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                    if (false) DU.DrawGeometryLazy(new GCircle(m, 50).ToCirclePolygon(36), ents => ents.ForEach(e => e.ColorIndex = 7));
                    DU.DrawRectLazy(GRect.Create(m, 40));
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var o in item.WrappingPipes)
                    {
                        var e = DU.DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(4, 229, 230);
                    foreach (var o in item.DLines)
                    {
                        var e = DU.DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(211, 213, 111);
                    foreach (var o in item.VLines)
                    {
                        DU.DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                    }
                }


                //标注立管
                {
                    {
                        //通过引线进行标注
                        var ok_ents = new HashSet<Geometry>();
                        for (int i = 0; i < 3; i++)
                        {
                            //先处理最简单的case
                            var ok = false;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == 1 && pipes.Count == 1)
                                {
                                    var lb = labels[0];
                                    var pp = pipes[0];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? "";
                                    if (ThDrainageService.IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = true;
                                    }
                                    else if (ThDrainageService.IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
                                        ok_ents.Add(lb);
                                        ok = true;
                                    }
                                }
                            }
                            if (!ok) break;
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            //再处理多个一起串的case
                            var ok = false;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > 0)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? "").ToList();
                                    if (labelsTxts.All(txt => ThDrainageService.IsMaybeLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = 0; k < pipes.Count; k++)
                                        {
                                            var pp = pipes[k];
                                            var lb = labels[k];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp] = label;
                                        }
                                        //OK，识别成功
                                        ok_ents.AddRange(pipes);
                                        ok_ents.AddRange(labels);
                                        ok = true;
                                    }
                                }
                            }
                            if (!ok) break;
                        }

                        {
                            //对付擦边球case
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? "";
                                if (!ThDrainageService.IsMaybeLabelText(lb)) continue;
                                var lst = labellinesf(label);
                                if (lst.Count == 1)
                                {
                                    var labelline = lst[0];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == 1)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[0].ToNTSPoint());
                                        if (pipes.Count == 1)
                                        {
                                            var pp = pipes[0];
                                            lbDict[pp] = lb;
                                            ok_ents.Add(pp);
                                            ok_ents.Add(label);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    (List<Geometry>, List<Geometry>) getPipes()
                    {
                        var pipes1 = new List<Geometry>(lbDict.Count);
                        var pipes2 = new List<Geometry>(lbDict.Count);
                        foreach (var pipe in item.VerticalPipes) if (lbDict.ContainsKey(pipe)) pipes1.Add(pipe); else pipes2.Add(pipe);
                        return (pipes1, pipes2);
                    }
                    {
                        //识别转管，顺便进行标注

                        bool recognise1()
                        {
                            var ok = false;
                            for (int i = 0; i < 3; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in dlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == 1 && lst2.Count > 0)
                                    {
                                        var pp1 = lst1[0];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (10 < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                //通气立管没有乙字弯
                                                if (!label.StartsWith("TL"))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = true;
                                                }
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = true;
                                            }
                                        }
                                    }
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        bool recognise2()
                        {
                            var ok = false;
                            for (int i = 0; i < 3; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(5).ToGCircle(false).ToCirclePolygon(6));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        //通气立管没有乙字弯
                                        if (!label.StartsWith("TL"))
                                        {
                                            if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > 1)
                                            {
                                                fs.Add(() =>
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = true;
                                                });
                                            }
                                        }
                                    }
                                    if (fs.Count == 1) fs[0]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            if (!(recognise1() && recognise2())) break;
                        }
                    }
                    {
                        var pipes1f = F(lbDict.Where(kv => kv.Value.StartsWith("TL")).Select(kv => kv.Key).ToList());
                        var pipes2f = F(item.VerticalPipes.Where(p => !lbDict.ContainsKey(p)).ToList());
                        foreach (var vlinesGeo in vlinesGeos)
                        {
                            var lst = pipes1f(vlinesGeo);
                            if (lst.Count == 1)
                            {
                                var pp1 = lst[0];
                                lst = pipes2f(vlinesGeo);
                                if (lst.Count == 1)
                                {
                                    var pp2 = lst[0];
                                    if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > MAX_SHORTTRANSLATOR_DISTANCE)
                                    {
                                        var label = lbDict[pp1];
                                        longTranslatorLabels.Add(label);
                                        lbDict[pp2] = label;
                                    }
                                }
                            }
                        }
                    }
                }

                string getLabel(Geometry pipe)
                {
                    lbDict.TryGetValue(pipe, out string label);
                    return label;
                }

                {
                    var f = F(item.VerticalPipes);
                    foreach (var dlinesGeo in dlinesGeos)
                    {
                        var pipes = f(dlinesGeo);
                        var d = pipes.Select(getLabel).Where(x => x != null).ToCountDict();
                        foreach (var label in d.Where(x => x.Value > 1).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == 2)
                            {
                                var dis = pps[0].GetCenter().GetDistanceTo(pps[1].GetCenter());
                                if (10 < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                {
                                    //通气立管没有乙字弯
                                    if (!label.StartsWith("TL"))
                                    {
                                        shortTranslatorLabels.Add(label);
                                    }
                                }
                                else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                {
                                    longTranslatorLabels.Add(label);
                                }
                            }
                            else
                            {
                                longTranslatorLabels.Add(label);
                            }
                        }
                    }
                }

                //关联地漏
                {
                    var dict = new Dictionary<string, int>();
                    {
                        var pipesf = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                        var gs = GeoFac.GroupGeometriesEx(dlinesGeos, item.FloorDrains);
                        foreach (var g in gs)
                        {
                            var fds = g.Where(pl => item.FloorDrains.Contains(pl)).ToList();
                            var dlines = g.Where(pl => dlinesGeos.Contains(pl)).ToList();
                            if (!AllNotEmpty(fds, dlines)) continue;
                            var pipes = pipesf(GeoFac.CreateGeometry(fds.Concat(dlines).ToList()));
                            foreach (var lb in pipes.Select(getLabel).Where(lb => lb != null).Distinct())
                            {
                                dict[lb] = fds.Count;
                            }
                        }
                    }
                    {
                        //如果地漏没连dline，那么以中心500范围内对应最近的立管
                        var pipesf = F(item.VerticalPipes);
                        foreach (var fd in item.FloorDrains.Except(F(item.FloorDrains)(G(item.DLines))))
                        {
                            foreach (var pipe in pipesf(new GCircle(fd.GetCenter(), 500).ToCirclePolygon(6)))
                            {
                                if (lbDict.TryGetValue(pipe, out string label))
                                {
                                    dict.TryGetValue(label, out int count);
                                    if (count == 0)
                                    {
                                        dict[label] = 1;
                                        break;
                                    }
                                }
                            }
                        }

                    }
                    sb.AppendLine("地漏：" + dict.ToJson());
                    drData.FloorDrains = dict;
                }

                //关联清扫口
                {
                    var f = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                    var hs = new HashSet<string>();
                    var gs = GeoFac.GroupGeometries(dlinesGeos.Concat(item.CleaningPorts).ToList());
                    foreach (var g in gs)
                    {
                        var dlines = g.Where(pl => dlinesGeos.Contains(pl)).ToList();
                        var ports = g.Where(pl => item.CleaningPorts.Contains(pl)).ToList();
                        if (!AllNotEmpty(ports, dlines)) continue;
                        var pipes = f(GeoFac.CreateGeometry(ports.Concat(dlines).ToList()));
                        hs.AddRange(pipes.Select(getLabel).Where(lb => lb != null));
                    }
                    sb.AppendLine("清扫口：" + hs.ToJson());
                    drData.CleaningPorts.AddRange(hs);
                }


                //排出方式
                {
                    //获取排出编号

                    var ok_ents = new HashSet<Geometry>();
                    var outletd = new Dictionary<string, string>();
                    var has_wrappingpipes = new HashSet<string>();
                    var portd = new Dictionary<Geometry, string>();
                    {
                        void collect(Func<Geometry, List<Geometry>> waterPortsf, Func<Geometry, string> getWaterPortLabel)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var waterPorts = waterPortsf(dlinesGeo);
                                if (waterPorts.Count == 1)
                                {
                                    var waterPort = waterPorts[0];
                                    var waterPortLabel = getWaterPortLabel(waterPort);
                                    portd[dlinesGeo] = waterPortLabel;
                                    //DU.DrawTextLazy(waterPortLabel, dlinesGeo.GetCenter());
                                    var pipes = f2(dlinesGeo);
                                    ok_ents.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            outletd[label] = waterPortLabel;
                                            var wrappingpipes = wrappingPipesf(dlinesGeo);
                                            if (wrappingpipes.Count > 0)
                                            {
                                                has_wrappingpipes.Add(label);
                                            }
                                            foreach (var wp in wrappingpipes)
                                            {
                                                portd[wp] = waterPortLabel;
                                                DU.DrawTextLazy(waterPortLabel, wp.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //先提取直接连接的
                        collect(F(item.WaterPorts), waterPort => geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)]);
                        //再处理挨得特别近但就是没连接的
                        {
                            var spacialIndex = item.WaterPorts.Select(cadDataMain.WaterPorts).ToList();
                            var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(400).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
                        }
                    }
                    {
                        //再处理挨得有点远没直接连接的
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = 10;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterPort = f5(pt.ToPoint3d());
                                if (waterPort != null)
                                {
                                    if (waterPort.GetCenter().GetDistanceTo(pt) <= 1500)
                                    {
                                        Dbg.ShowXLabel(pt);
                                        var waterPortLabel = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                                        portd[dlinesGeo] = waterPortLabel;
                                        //DU.DrawTextLazy(waterPortLabel, dlinesGeo.GetCenter());
                                        foreach (var pipe in f2(dlinesGeo))
                                        {
                                            if (lbDict.TryGetValue(pipe, out string label))
                                            {
                                                outletd[label] = waterPortLabel;
                                                ok_ents.Add(pipe);
                                                var wrappingpipes = wrappingPipesf(dlinesGeo);
                                                if (wrappingpipes.Any())
                                                {
                                                    has_wrappingpipes.Add(label);
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    portd[wp] = waterPortLabel;
                                                    DU.DrawTextLazy(waterPortLabel, wp.GetCenter());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        //给套管做标记
                        var wpf = F(item.WrappingPipes.Where(wp => !portd.ContainsKey(wp)).ToList());
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            if (!portd.TryGetValue(dlinesGeo, out string v)) continue;
                            foreach (var wp in wpf(dlinesGeo))
                            {
                                if (!portd.ContainsKey(wp))
                                {
                                    portd[wp] = v;
                                }
                            }
                        }
                    }
                    {
                        var pipesf = F(item.VerticalPipes);
                        //再处理通过套管来连接的
                        foreach (var wp in item.WrappingPipes)
                        {
                            if (portd.TryGetValue(wp, out string v))
                            {
                                var pipes = pipesf(wp);
                                foreach (var pipe in pipes)
                                {
                                    if (lbDict.TryGetValue(pipe, out string label))
                                    {
                                        if (!outletd.ContainsKey(label))
                                        {
                                            outletd[label] = v;
                                        }
                                    }
                                }
                            }
                        }
                    }


                    {
                        sb.AppendLine("排出：" + outletd.ToJson());
                        drData.Outlets = outletd;

                        outletd.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                        {
                            var num = kv1.Value;
                            var pipe = kv2.Key;
                            DU.DrawTextLazy(num, pipe.ToGRect().RightButtom);
                            return 666;
                        }).Count();
                    }
                    {
                        sb.AppendLine("套管：" + has_wrappingpipes.ToJson());
                        drData.WrappingPipes.AddRange(has_wrappingpipes);
                    }

                }

                //“仅31F顶层板下设置乙字弯”的处理（😉不处理）
                //标出所有的立管编号（看看识别成功了没）
                foreach (var pp in item.VerticalPipes)
                {
                    lbDict.TryGetValue(pp, out string label);
                    if (label != null)
                    {
                        DU.DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                    }
                }


                //立管分组
                {
                    var pls = new List<Geometry>();
                    var dls = new List<Geometry>();
                    var tls = new List<Geometry>();
                    var fls = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (kv.Value.StartsWith("PL"))
                        {
                            pls.Add(kv.Key);
                        }
                        else if (kv.Value.StartsWith("DL"))
                        {
                            dls.Add(kv.Key);
                        }
                        else if (kv.Value.StartsWith("TL"))
                        {
                            tls.Add(kv.Key);
                        }
                        else if (kv.Value.StartsWith("FL"))
                        {
                            fls.Add(kv.Key);
                        }
                    }
                    var lst = ThDrainageService.WLGeoGrouper.ToiletGrouper.DoGroup(pls, tls, dls, fls);
                    var toiletGroupers = new List<ThDrainageService.WLGrouper.ToiletGrouper>();
                    foreach (var itm in lst)
                    {
                        var _pls = itm.PLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _tls = itm.TLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _dls = itm.DLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _fls = itm.FLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var m = new ThDrainageService.WLGrouper.ToiletGrouper()
                        {
                            WLType = itm.WLType,
                            PLs = _pls,
                            TLs = _tls,
                            DLs = _dls,
                            FLs = _fls,
                        };
                        m.Init();
                        toiletGroupers.Add(m);
                    }
                    drData.toiletGroupers.AddRange(toiletGroupers);
                }
                //6.3.8	水管井的FL
                {
                    //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                    //水管井的判断：
                    //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
                    var f = F(lbDict.Where(kv => kv.Value.StartsWith("FL")).Select(x => x.Key).ToList());
                    var hs = new HashSet<string>();
                    foreach (var kv in roomData)
                    {
                        if (kv.Key == "水" || kv.Key.Contains("水井") || kv.Key.Contains("水管井"))
                        {
                            hs.AddRange(f(kv.Value).Select(x => lbDict[x]));
                        }
                    }
                    drData.WaterPipeWellFLs.AddRange(hs);
                }


                {
                    var fls = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (kv.Value.StartsWith("FL"))
                        {
                            fls.Add(kv.Key);
                        }
                    }
                    var kitchens = roomData.Where(x => x.Key == "厨房").Select(x => x.Value).ToList();
                    var toilets = roomData.Where(x => x.Key == "卫生间").Select(x => x.Value).ToList();
                    var nonames = roomData.Where(x => x.Key == "").Select(x => x.Value).ToList();
                    var balconys = roomData.Where(x => x.Key == "阳台").Select(x => x.Value).ToList();

                    var pts = GeoFac.GetAlivePoints(item.DLines.Select(cadDataMain.DLines).ToList(geoData.DLines), radius: 5);

                    //1)	必然负担一个洗涤盆下水点。不用读图上任何信息；
                    //2)	若厨房内存在任何地漏图块或洗衣机图块（图块名称包含A-Toilet-9），则必然负担一个地漏下水点。
                    //最多同时负担一个洗涤盆下水店和一个地漏下水点。
                    var _fls1 = DrainageService.GetKitchenOnlyFLs(fls, kitchens, nonames, balconys, pts);
                    foreach (var fl in _fls1)
                    {
                        var label = lbDict[fl];
                        drData.KitchenOnlyFls.Add(label);
                    }
                    var _fls4 = DrainageService.GetFLsWhereSupportingFloorDrainUnderWaterPoint(fls, kitchens, item.FloorDrains, item.WashingMachines);
                    foreach (var fl in _fls4)
                    {
                        var label = lbDict[fl];
                        drData.MustHaveFloorDrains.Add(label);
                    }

                    var _fls2 = DrainageService.GetBalconyOnlyFLs(fls, kitchens, nonames, balconys, pts);
                    //1)	图层名称包含“W-DRAI-EPQM”且半径大于40的圆视为洗手台下水点
                    //2)	地漏图块视为地漏下水点
                    foreach (var fl in _fls2)
                    {
                        var label = lbDict[fl];
                        drData.BalconyOnlyFLs.Add(label);
                    }

                    //综合厨房和阳台的点位
                    var _fls3 = DrainageService.GetKitchenAndBalconyBothFLs(fls, kitchens, nonames, balconys, pts);
                    foreach (var fl in _fls3)
                    {
                        var label = lbDict[fl];
                        drData.KitchenAndBalconyFLs.Add(label);
                    }
                    foreach (var fl in _fls3)
                    {
                        var label = lbDict[fl];
                        drData.MustHaveFloorDrains.Add(label);
                    }


                }


                {
                    sb.AppendLine("立管：" + lbDict.Values.Distinct().OrderBy(x => x).ToJson());
                    drData.VerticalPipeLabels.AddRange(lbDict.Values.Distinct());
                }
                {
                    var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                    _longTranslatorLabels.Sort();
                    sb.AppendLine("长转管:" + _longTranslatorLabels.JoinWith(","));
                    drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);
                }

                {
                    var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                    _shortTranslatorLabels.Sort();
                    sb.AppendLine("短转管:" + _shortTranslatorLabels.JoinWith(","));
                    drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);
                }
                drDatas.Add(drData);
            }
            DrawingTransaction.Cur.AbleToDraw = true;
        }

        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
        {
            return source1.Concat(source2).ToList();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
        {
            return source1.Concat(source2).Concat(source3).ToList();
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == "AI-空间框线").Select(x => x.ToNTSPolygon()).Cast<Geometry>().ToList();
            var names = adb.ModelSpace.OfType<MText>().Where(x => x.Layer == "AI-空间名称").Select(x => new CText() { Text = x.Text, Boundary = x.Bounds.ToGRect() }).ToList();
            var f = GeoFac.CreateIntersectsSelector(ranges);
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == 1)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text, l[0]));
                    }
                    else
                    {
                        foreach (var geo in l)
                        {
                            ranges.Remove(geo);
                        }
                    }
                }
            }
            foreach (var range in ranges.Except(list.Select(kv => kv.Value)))
            {
                list.Add(new KeyValuePair<string, Geometry>("", range));
            }
            return list;
        }
        public static List<Geometry> GetKitchenOnlyFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies, List<Point2d> pts)
        {
            //6.3.2	只负责厨房的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	FL的500范围内有厨房空间，或
            //2）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在厨房空间内
            //且以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意所有结束点不在没有名称的空间或不在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Point> endpoints = null;
                Geometry endpointsGeo = null;
                List<Point> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter(), pts.Select(x => x.ToNTSPoint()).ToList());
                }
                bool test1()
                {
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return kitchensGeo.Intersects(endpointsGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
                }
                if ((test1() || test2()) && test3())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
        //找出负担地漏下水点的FL
        public static HashSet<Geometry> GetFLsWhereSupportingFloorDrainUnderWaterPoint(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> floorDrains, List<Geometry> washMachines)
        {
            var f = GeoFac.CreateIntersectsSelector(ToList(floorDrains, washMachines));
            var hs = new HashSet<Geometry>();
            {
                var flsf = GeoFac.CreateIntersectsSelector(FLs);
                foreach (var kitchen in kitchens)
                {
                    var lst = flsf(kitchen);
                    if (lst.Count > 0)
                    {
                        if (f(kitchen).Count > 0)
                        {
                            hs.AddRange(lst);
                        }
                    }
                }
            }
            return hs;
        }
        static List<Point> GetEndPoints(Point2d start, List<Point> points)
        {
            //以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的结束点
            return points.Except(GeoFac.CreateIntersectsSelector(points)(new GCircle(start, 5).ToCirclePolygon(36))).ToList();
        }
        public static List<Geometry> GetBalconyOnlyFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies, List<Point2d> pts)
        {
            //6.3.3	只负责阳台的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。且
            //2）	FL的500范围内没有厨房空间，且
            //3）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的所有结束点不在在厨房空间内
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Point> endpoints = null;
                Geometry endpointsGeo = null;
                List<Point> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter(), pts.Select(x => x.ToNTSPoint()).ToList());
                }
                bool test1()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
                }
                bool test2()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return !kitchensGeo.Intersects(endpointsGeo);
                }
                if (test1() && test2() && test3())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
        public static List<Geometry> GetKitchenAndBalconyBothFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies, List<Point2d> pts)
        {
            //6.3.4	厨房阳台兼用FL
            //-	判断方法
            //1)	FL的500范围内有厨房空间，且
            //2)	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Point> endpoints = null;
                Geometry endpointsGeo = null;
                List<Point> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter(), pts.Select(x => x.ToNTSPoint()).ToList());
                }
                bool test1()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
                }
                if (test1() && test2())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
    }
    public partial class DrainageSystemDiagram
    {
        public static readonly Point2d[] LONG_TRANSLATOR_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-1379, -121), new Point2d(-1500, -241) };
        public static readonly Point2d[] SHORT_TRANSLATOR_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121) };
        public static double LONG_TRANSLATOR_HEIGHT1 = 780;
        public static double CHECKPOINT_OFFSET_Y = 580;
        public static readonly Point2d[] LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 500), new Point2d(-79, 621), new Point2d(1029, 621), new Point2d(1150, 741), new Point2d(1150, 1021) };
        public static void DrawDrainageSystemDiagram()
        {
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            if (!Dbg.TrySelectPoint(out Point3d point3D)) return;
            var basePoint = point3D.ToPoint2d();

            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb, true))
            {
                List<StoreysItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: true)) return;
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                DU.Dispose();
                DrawDrainageSystemDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);
                DU.Draw(adb);
            }
        }

        public static void draw14()
        {

            static void DrawStoreys(Point2d basePoint)
            {
                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0;
                var HEIGHT = 1800.0;
                //var HEIGHT = 5000.0;
                var COUNT = 20;

                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    DrawStoreyLine(storey, bsPt1, lineLen);
                }
                var outputStartPointOffsets = new Vector2d[COUNT];
            }


            List<DrainageDrawingData> drDatas;
            List<StoreysItem> storeysItems;
            loadTestData(out drDatas, out storeysItems);
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d point3D)) return;
            var basePoint = point3D.ToPoint2d();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                if (false) Draw(drDatas, basePoint, storeysItems);
                if (false) DrawVer1(null);
                Testing = true;
                DrawDrainageSystemDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);

            }
        }




        public static void DrawStoreyLine(string label, Point2d basePt, double lineLen)
        {
            DrawStoreyLine(label, basePt.ToPoint3d(), lineLen);
        }
        public static void DrawStoreyLine(string label, Point3d basePt, double lineLen)
        {
            {
                var line = DU.DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DU.DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
                DU.DrawBlockReference(blkName: "标高", basePt: basePt.OffsetX(550), layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", "" } });
            }
            if (label == "RF")
            {
                var line = DU.DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0));
                var dbt = DU.DrawTextLazy("建筑完成面", ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
        }
        public static IEnumerable<Point2d> GetBasePoints(Point2d basePoint, int maxCol, int num, double width, double height)
        {
            int i = 0, j = 0;
            for (int k = 0; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = 0;
                }
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = 0, j = 0;
            for (int k = 0; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, 0);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = 0;
                }
            }
        }
        public static void DrawWashBasin(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("洗涤盆排水", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("洗涤盆排水", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(-2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
        }
        public static void DrawDoubleWashBasins(Point3d basePt, bool leftOrRight)
        {
            basePt = basePt.OffsetY(300);
            if (leftOrRight)
            {
                DU.DrawBlockReference("双格洗涤盆排水", basePt,
                  br =>
                  {
                      br.Layer = "W-DRAI-EQPM";
                      br.ScaleFactors = new Scale3d(2, 2, 2);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                      }
                  });
            }
            else
            {
                DU.DrawBlockReference("双格洗涤盆排水", basePt,
                  br =>
                  {
                      br.Layer = "W-DRAI-EQPM";
                      br.ScaleFactors = new Scale3d(-2, 2, 2);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                      }
                  });
            }
        }
        public void Draw(Point3d basePoint)
        {
            draw1(basePoint);
        }

        public class ThwPipeRun
        {
            public string Storey;
            public bool ShowStoreyLabel;
            public bool HasHorizontalShortLine;
            public bool HasShortTranslator;
            public bool HasLongTranslator;
            public bool IsShortTranslatorToLeftOrRight;
            public bool IsLongTranslatorToLeftOrRight;
            public bool ShowShortTranslatorLabel;
            public bool HasCheckPoint;
            public bool IsFirstItem;
            public bool IsLastItem;
            public Hanging LeftHanging;
            public Hanging RightHanging;
            public bool HasCleaningPort;
            public BranchInfo BranchInfo;
        }
        public class BranchInfo
        {
            public bool FirstLeftRun;
            public bool MiddleLeftRun;
            public bool LastLeftRun;
            public bool FirstRightRun;
            public bool MiddleRightRun;
            public bool LastRightRun;
            public bool BlueToLeftFirst;
            public bool BlueToLeftMiddle;
            public bool BlueToLeftLast;
            public bool BlueToRightFirst;
            public bool BlueToRightMiddle;
            public bool BlueToRightLast;
            public bool HasLongTranslatorToLeft;
            public bool HasLongTranslatorToRight;
            public bool IsLast;
        }
        public class Hanging
        {
            public int FloorDrainsCount;
            public bool IsSeries;
            public bool HasSCurve;
            public bool HasDoubleSCurve;
            public bool HasUnderBoardLabel;
        }
        public enum GDirection
        {
            E, W, S, N, ES, EN, WS, WN,
        }
        public class ThwOutput
        {
            public int LinesCount = 1;
            public List<string> DirtyWaterWellValues;
            public bool HasVerticalLine2;
            public bool HasWrappingPipe1;
            public bool HasWrappingPipe2;
            public bool HasWrappingPipe3;
            public string DN1;
            public string DN2;
            public string DN3;
            public bool HasCleaningPort1;
            public bool HasCleaningPort2;
            public bool HasCleaningPort3;
            public bool HasLargeCleaningPort;
            public int HangingCount = 0;
            public Hanging Hanging1;
            public Hanging Hanging2;
        }
        public class ThwPipeLine
        {
            public string Label1;
            public string Label2;
            public List<string> Comments;
            public bool? IsLeftOrMiddleOrRight;
            public double AiringValue;
            public List<ThwPipeRun> PipeRuns;
            public ThwOutput Output;
        }
        public class ThwPipeLineGroup
        {
            public ThwOutput Output;
            public ThwPipeLine TL;
            public ThwPipeLine DL;
            public ThwPipeLine PL;
            public ThwPipeLine FL;
            public int LinesCount
            {
                get
                {
                    var s = 0;
                    if (TL != null) ++s;
                    if (DL != null) ++s;
                    if (PL != null) ++s;
                    if (FL != null) ++s;
                    return s;
                }
            }
            public ThwPipeLineGroup Clone()
            {
                return this.ToCadJson().FromCadJson<ThwPipeLineGroup>();
            }
        }

        public class PipeRunLocationInfo
        {
            public string Storey;
            public Point2d BasePoint;
            public Point2d StartPoint;
            public Point2d EndPoint;
            public Point2d HangingEndPoint;
            public List<Vector2d> Vector2ds;
            public List<GLineSegment> Segs;
            public List<GLineSegment> DisplaySegs;
            public List<GLineSegment> RightSegsFirst;
            public List<GLineSegment> RightSegsMiddle;
            public List<GLineSegment> RightSegsLast;
            public bool Visible;
        }
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-RAIN-NOTE";
                e.ColorIndex = 256;
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-800, 1000), new Vector2d(-745, 0) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[1].EndPoint : segs[1].StartPoint;
            var text = "乙字弯";
            var height = 350;
            var lines = DU.DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DU.DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawWashingMachineRaisingSymbol(Point2d bsPt, bool isLeftOrRight)
        {
            if (isLeftOrRight)
            {
                var v = new Vector2d(383875.8169, -250561.9571);
                DU.DrawBlockReference("P型存水弯", (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                    }
                });
            }
            else
            {
                var v = new Vector2d(-383875.8169, -250561.9571);
                DU.DrawBlockReference("P型存水弯", (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(-2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                    }
                });
            }
        }
        public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreysItem> storeysItems, Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + "F").ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - 1;
            var end = 0;
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0 + 500;
            //var HEIGHT = 1800.0;
            var HEIGHT = 5000.0;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - 1800.0;
            var __dy = 300;
            DrawDrainageSystemDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, null);
        }

        public static void DrawDrainageSystemDiagram(Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, int __dy, DrainageSystemDiagramViewModel viewModel)
        {
            static void DrawSegs(List<GLineSegment> segs)
            {
                for (int k = 0; k < segs.Count; k++) DU.DrawTextLazy(k.ToString(), segs[k].StartPoint);
            }
            var vecs0 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -1800 - dy) };
            var vecs1 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy) };
            var vecs8 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780 - __dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy + __dy) };
            var vecs11 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780 + __dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy - __dy) };
            var vecs2 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -1679 - dy), new Vector2d(-121, -121) };
            var vecs3 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658 - dy), new Vector2d(-121, -121) };
            var vecs9 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780 - __dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658 - dy + __dy), new Vector2d(-121, -121) };
            var vecs13 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780 + __dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658 - dy - __dy), new Vector2d(-121, -121) };
            var vecs4 = vecs1.GetYAxisMirror();
            var vecs5 = vecs2.GetYAxisMirror();
            var vecs6 = vecs3.GetYAxisMirror();
            var vecs10 = vecs9.GetYAxisMirror();
            var vecs12 = vecs11.GetYAxisMirror();
            var vecs14 = vecs13.GetYAxisMirror();
            var vec7 = new Vector2d(-90, 90);





            {
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                for (int i = 0; i < allStoreys.Count; i++)
                {
                    var storey = allStoreys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
                }
            }

            var dome_lines = new List<GLineSegment>(4096);
            var vent_lines = new List<GLineSegment>(4096);
            var dome_layer = "W-DRAI-DOME-PIPE";
            var vent_layer = "W-DRAI-VENT-PIPE";
            //var vent_layer = "W-RAIN-EQPM";
            void drawDomePipe(GLineSegment seg)
            {
                dome_lines.Add(seg);
                //var line = DU.DrawLineSegmentLazy(seg);
                //line.Layer = dome_layer;
                //line.ColorIndex = 256;
            }
            void drawDomePipes(IEnumerable<GLineSegment> segs)
            {
                dome_lines.AddRange(segs);
                //var lines = DU.DrawLineSegmentsLazy(segs);
                //foreach (var line in lines)
                //{
                //    line.Layer = dome_layer;
                //    line.ColorIndex = 256;
                //}
            }


            PipeRunLocationInfo[] getPipeRunLocationInfos(Point2d basePoint, ThwPipeLine thwPipeLine, int j)
            {
                var arr = new PipeRunLocationInfo[allStoreys.Count];

                for (int i = 0; i < allStoreys.Count; i++)
                {
                    arr[i] = new PipeRunLocationInfo() { Visible = true, Storey = allStoreys[i], };
                }

                {
                    var tdx = 0.0;
                    for (int i = start; i >= end; i--)
                    {
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + new Vector2d(tdx, 0);
                        var run = thwPipeLine.PipeRuns.TryGet(i);

                        PipeRunLocationInfo drawNormal()
                        {
                            {
                                var vecs = vecs0;
                                var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                var dx = vecs.Sum(v => v.X);
                                tdx += dx;
                                arr[i].BasePoint = basePt;
                                arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                arr[i].HangingEndPoint = arr[i].EndPoint;
                                arr[i].Vector2ds = vecs;
                                arr[i].Segs = segs;
                                arr[i].RightSegsMiddle = segs.Select(x => x.Offset(300, 0)).ToList();
                            }
                            {
                                var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(24 - 9, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(24 - 6, 24))));
                            }
                            {
                                var segs = arr[i].RightSegsMiddle.ToList();
                                segs[0] = new GLineSegment(segs[0].StartPoint, segs[1].StartPoint);
                                arr[i].RightSegsLast = segs;
                            }
                            {
                                var pt = arr[i].Segs.First().StartPoint.OffsetX(300);
                                var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(24 - 6, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(24 - 9, 24))) };
                                arr[i].RightSegsFirst = segs;
                                segs.Add(new GLineSegment(segs[0].StartPoint, arr[i].EndPoint.OffsetX(300)));
                            }
                            return arr[i];
                        }

                        if (i == start)
                        {
                            drawNormal().Visible = false;
                            continue;
                        }
                        if (run == null)
                        {
                            drawNormal().Visible = false;
                            continue;
                        }


                        if (run.HasLongTranslator && run.HasShortTranslator)
                        {
                            if (run.IsLongTranslatorToLeftOrRight)
                            {
                                {
                                    var vecs = vecs3;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(300)).Skip(1).ToList();
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(6, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(9, 24))));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    segs.RemoveAt(5);
                                    segs.RemoveAt(4);
                                    segs.Add(new GLineSegment(segs[3].EndPoint, segs[3].EndPoint.OffsetXY(-300, -300)));
                                    segs.Add(new GLineSegment(segs[2].EndPoint, new Point2d(segs[5].EndPoint.X, segs[2].EndPoint.Y)));
                                    segs.RemoveAt(5);
                                    segs.RemoveAt(3);
                                    segs = new List<GLineSegment>() { segs[3], new GLineSegment(segs[3].StartPoint, segs[0].StartPoint) };
                                    arr[i].RightSegsLast = segs;
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    var pt = segs[4].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(6, 24))) };
                                    segs.Add(new GLineSegment(segs[0].EndPoint, pt.OffsetXY(-300, HEIGHT.ToRatioInt(9, 24))));
                                    segs.Add(new GLineSegment(segs[0].StartPoint, arr[i].EndPoint.OffsetX(300)));
                                    arr[i].RightSegsFirst = segs;
                                }
                            }
                            else
                            {
                                {
                                    var vecs = vecs6;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(300)).Skip(1).ToList();
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(24 - 9, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(24 - 6, 24))).Offset(1500, 0));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    segs.RemoveAt(5);
                                    segs.RemoveAt(4);
                                    segs.Add(new GLineSegment(segs[3].EndPoint, segs[4].StartPoint));
                                    arr[i].RightSegsLast = segs;
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    var pt = segs[4].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(6, 24))) };
                                    segs.Add(new GLineSegment(segs[0].EndPoint, pt.OffsetXY(-300, HEIGHT.ToRatioInt(9, 24))));
                                    segs.Add(new GLineSegment(segs[0].StartPoint, arr[i].EndPoint.OffsetX(300)));
                                    arr[i].RightSegsFirst = segs;
                                }
                            }
                            arr[i].HangingEndPoint = arr[i].Segs[4].EndPoint;
                        }
                        else if (run.HasLongTranslator)
                        {
                            if (run.IsLongTranslatorToLeftOrRight)
                            {
                                {
                                    var vecs = vecs1;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(300)).Skip(1).ToList();
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(6, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(9, 24))));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    segs = segs.Take(4).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[3].EndPoint, segs[3].EndPoint.OffsetXY(-300, -300))).ToList();
                                    segs.Add(new GLineSegment(segs[2].EndPoint, new Point2d(segs[5].EndPoint.X, segs[2].EndPoint.Y)));
                                    segs.RemoveAt(5);
                                    segs.RemoveAt(3);
                                    segs = new List<GLineSegment>() { segs[3], new GLineSegment(segs[3].StartPoint, segs[0].StartPoint) };
                                    arr[i].RightSegsLast = segs;
                                }
                                {
                                    //var segs = arr[i].RightSegsMiddle.ToList();
                                    //segs.RemoveAt(0);
                                    //var r = new GRect(segs[4].StartPoint, segs[4].EndPoint);
                                    //segs[4] = new GLineSegment(r.LeftTop, r.RightButtom);
                                    //segs.Add(new GLineSegment(r.RightButtom, segs[0].StartPoint));
                                    //arr[i].RightSegsFirst = segs;
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    var pt = segs[4].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(6, 24))) };
                                    segs.Add(new GLineSegment(segs[0].EndPoint, pt.OffsetXY(-300, HEIGHT.ToRatioInt(9, 24))));
                                    arr[i].RightSegsFirst = segs;
                                }
                            }
                            else
                            {
                                {
                                    var vecs = vecs4;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(300)).Skip(1).ToList();
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(24 - 9, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(24 - 6, 24))).Offset(1500, 0));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle;
                                    arr[i].RightSegsLast = segs.Take(4).YieldAfter(new GLineSegment(segs[3].EndPoint, segs[5].StartPoint)).YieldAfter(segs[5]).ToList();
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    var pt = segs[4].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(6, 24))) };
                                    segs.Add(new GLineSegment(segs[0].EndPoint, pt.OffsetXY(-300, HEIGHT.ToRatioInt(9, 24))));
                                    arr[i].RightSegsFirst = segs;
                                }
                            }
                            arr[i].HangingEndPoint = arr[i].EndPoint;
                        }
                        else if (run.HasShortTranslator)
                        {
                            if (run.IsShortTranslatorToLeftOrRight)
                            {
                                {
                                    var vecs = vecs2;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = segs.Select(x => x.Offset(300, 0)).ToList();
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(24 - 9, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(24 - 6, 24))));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[0].StartPoint, segs[2].StartPoint), segs[2] };
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    var r = new GRect(segs[2].StartPoint, segs[2].EndPoint);
                                    segs[2] = new GLineSegment(r.LeftTop, r.RightButtom);
                                    segs.RemoveAt(0);
                                    segs.Add(new GLineSegment(segs[0].StartPoint, r.RightButtom));
                                    arr[i].RightSegsFirst = segs;
                                }
                            }
                            else
                            {
                                {
                                    var vecs = vecs5;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                    arr[i].RightSegsMiddle = segs.Select(x => x.Offset(300, 0)).ToList();
                                }
                                {
                                    var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                    arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(24 - 9, 24)), pt.OffsetXY(-300, -HEIGHT.ToRatioInt(24 - 6, 24))));
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[0].StartPoint, segs[2].StartPoint), segs[2] };
                                }
                                {
                                    var segs = arr[i].RightSegsMiddle.ToList();
                                    var r = new GRect(segs[2].StartPoint, segs[2].EndPoint);
                                    segs[2] = new GLineSegment(r.LeftTop, r.RightButtom);
                                    segs.RemoveAt(0);
                                    segs.Add(new GLineSegment(segs[0].StartPoint, r.RightButtom));
                                    arr[i].RightSegsFirst = segs;
                                }
                            }
                            arr[i].HangingEndPoint = arr[i].Segs[0].EndPoint;
                        }
                        else
                        {
                            drawNormal();
                        }
                    }
                }

                for (int i = 0; i < allNumStoreyLabels.Count; i++)
                {
                    var info = arr.TryGet(i);
                    if (info != null)
                    {
                        info.StartPoint = info.BasePoint.OffsetY(HEIGHT);
                    }
                }

                return arr;
            }



            void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
            {
                {
                    var info = arr.Where(x => x != null).FirstOrDefault();
                    if (info != null)
                    {
                        var dy = -3000;
                        if (thwPipeLine.Comments != null)
                        {
                            foreach (var comment in thwPipeLine.Comments)
                            {
                                if (!string.IsNullOrEmpty(comment))
                                {
                                    DU.DrawTextLazy(comment, 350, info.EndPoint.OffsetY(-HEIGHT * ((IList)arr).IndexOf(info)).OffsetY(dy));
                                }
                                dy -= 350;
                            }
                        }
                    }
                }
                {
                    foreach (var info in arr)
                    {
                        if (info?.Storey == "RF")
                        {
                            var pt = info.BasePoint;
                            var seg = new GLineSegment(pt, pt.OffsetY(ThWSDStorey.RF_OFFSET_Y));
                            drawDomePipe(seg);
                            DrawAiringSymbol(seg.EndPoint, viewModel?.Params?.CouldHavePeopleOnRoof ?? true);
                        }
                    }
                }
                for (int i = start; i >= end; i--)
                {
                    var storey = allNumStoreyLabels.TryGet(i);
                    if (storey == null) continue;
                    var run = thwPipeLine.PipeRuns.TryGet(i);
                    if (run == null) continue;
                    var info = arr[i];
                    if (info == null) continue;
                    var output = thwPipeLine.Output;

                    {
                        if (storey == "1F")
                        {
                            var basePt = info.EndPoint;
                            if (thwPipeLine.Output != null) DrawOutputs1(basePt, 3600, thwPipeLine.Output);
                        }
                    }
                    void handleHanging(Hanging hanging, bool isLeftOrRight)
                    {
                        if (run.HasLongTranslator)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(-200, 200), new Vector2d(0, 479), new Vector2d(-121, 121), new Vector2d(-789, 0), new Vector2d(-1270, 0), new Vector2d(-180, 0), new Vector2d(-1090, 0) };
                            if (isLeftOrRight == false)
                            {
                                vecs = vecs.GetYAxisMirror();
                            }
                            var pt = info.Segs[4].StartPoint.OffsetY(-669).OffsetY(590 - 90);
                            if (isLeftOrRight == false && run.IsLongTranslatorToLeftOrRight == true)
                            {
                                var p1 = pt;
                                var p2 = pt.OffsetX(1700);
                                drawDomePipe(new GLineSegment(p1, p2));
                                pt = p2;
                            }
                            if (isLeftOrRight == true && run.IsLongTranslatorToLeftOrRight == false)
                            {
                                var p1 = pt;
                                var p2 = pt.OffsetX(-1700);
                                drawDomePipe(new GLineSegment(p1, p2));
                                pt = p2;
                            }
                            var segs = vecs.ToGLineSegments(pt);
                            {
                                var _segs = segs.ToList();
                                if (hanging.FloorDrainsCount == 2)
                                {
                                    _segs.RemoveAt(5);
                                }
                                else if (hanging.FloorDrainsCount == 1)
                                {
                                    _segs = segs.Take(5).ToList();
                                }
                                else if (hanging.FloorDrainsCount == 0)
                                {
                                    _segs = segs.Take(4).ToList();
                                }
                                drawDomePipes(_segs);
                            }
                            if (hanging.FloorDrainsCount >= 1)
                            {
                                DrawFloorDrain(segs.Last(3).EndPoint.ToPoint3d(), isLeftOrRight);
                            }
                            if (hanging.FloorDrainsCount >= 2)
                            {
                                DrawFloorDrain(segs.Last(1).EndPoint.ToPoint3d(), isLeftOrRight);
                                if (hanging.IsSeries)
                                {
                                    DrawDomePipes(segs.Last(2));
                                }
                            }

                            if (hanging.HasSCurve)
                            {
                                var p1 = segs.Last(3).StartPoint;
                                DrawSCurve(vec7, p1, isLeftOrRight);
                            }
                            if (hanging.HasDoubleSCurve)
                            {
                                var p1 = segs.Last(3).StartPoint;
                                DrawDSCurve(vec7, p1, isLeftOrRight);
                            }
                        }
                        else
                        {
                            var vecs = new List<Vector2d> { new Vector2d(-121, 121), new Vector2d(-789, 0), new Vector2d(-1270, 0), new Vector2d(-180, 0), new Vector2d(-1090, -15) };
                            if (isLeftOrRight == false)
                            {
                                vecs = vecs.GetYAxisMirror();
                            }
                            var segs = vecs.ToGLineSegments(info.StartPoint.OffsetY(-510));
                            {
                                var _segs = segs.ToList();
                                if (hanging.FloorDrainsCount == 2)
                                {
                                    _segs.RemoveAt(3);
                                }
                                if (hanging.FloorDrainsCount == 1)
                                {
                                    _segs.RemoveAt(4);
                                    _segs.RemoveAt(3);
                                }
                                if (hanging.FloorDrainsCount == 0)
                                {
                                    _segs = _segs.Take(2).ToList();
                                }
                                drawDomePipes(_segs);
                            }
                            if (hanging.FloorDrainsCount >= 1)
                            {
                                DrawFloorDrain(segs.Last(3).EndPoint.ToPoint3d(), isLeftOrRight);
                            }
                            if (hanging.FloorDrainsCount >= 2)
                            {
                                DrawFloorDrain(segs.Last(1).EndPoint.ToPoint3d(), isLeftOrRight);
                            }
                            if (hanging.HasSCurve)
                            {
                                var p1 = segs.Last(3).StartPoint;
                                DrawSCurve(vec7, p1, isLeftOrRight);
                            }
                            if (hanging.HasDoubleSCurve)
                            {
                                var p1 = segs.Last(3).StartPoint;
                                DrawDSCurve(vec7, p1, isLeftOrRight);
                            }
                        }
                    }
                    void handleBranchInfo(ThwPipeRun run, PipeRunLocationInfo info)
                    {
                        var bi = run.BranchInfo;
                        if (bi.FirstLeftRun)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 2));
                            var p3 = info.EndPoint.OffsetX(300);
                            var p4 = p3.OffsetY(HEIGHT.ToRatioInt(2, 3));
                            info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                        }
                        if (bi.FirstRightRun)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 6));
                            var p3 = info.EndPoint.OffsetX(-300);
                            var p4 = p3.OffsetY(HEIGHT.ToRatioInt(1, 3));
                            info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                        }
                        if (bi.LastLeftRun)
                        {

                        }
                        if (bi.LastRightRun)
                        {

                        }
                        if (bi.MiddleLeftRun)
                        {

                        }
                        if (bi.MiddleRightRun)
                        {

                        }
                        if (bi.BlueToLeftFirst)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 6));
                            var p3 = info.EndPoint.OffsetX(-300);
                            var p4 = p3.OffsetY(HEIGHT.ToRatioInt(1, 3));
                            info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                        }
                        if (bi.BlueToRightFirst)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 6));
                            var p3 = info.EndPoint.OffsetX(300);
                            var p4 = p3.OffsetY(HEIGHT.ToRatioInt(1, 3));
                            info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                        }
                        if (bi.BlueToLeftLast)
                        {
                            if (run.HasLongTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    var _dy = 300;
                                    var vs1 = new List<Vector2d> { new Vector2d(0, -780 - _dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy + _dy + 400), new Vector2d(-300, -225) };
                                    var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                    var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                    info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                }
                                else
                                {
                                    var _dy = 300;
                                    var vs1 = new List<Vector2d> { new Vector2d(0, -780 + _dy), new Vector2d(121, -121), new Vector2d(1258, 0), new Vector2d(121, -120), new Vector2d(0, -779 - dy - _dy + 400), new Vector2d(-300, -225) };
                                    var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                    var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                    info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                }
                            }
                            else if (!run.HasLongTranslator)
                            {
                                var vs = new List<Vector2d> { new Vector2d(0, -1125), new Vector2d(-300, -225) };
                                info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                            }
                        }
                        if (bi.BlueToRightLast)
                        {
                            if (!run.HasLongTranslator && !run.HasShortTranslator)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(9, 24));
                                var p3 = info.EndPoint.OffsetX(300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(6, 24));
                                var p5 = p1.OffsetY(HEIGHT);
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, p2), new GLineSegment(p2, p5) };
                            }
                        }
                        if (bi.BlueToLeftMiddle)
                        {
                            if (!run.HasLongTranslator && !run.HasShortTranslator)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(9, 24));
                                var p3 = info.EndPoint.OffsetX(-300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(6, 24));
                                var segs = info.Segs.ToList();
                                segs.Add(new GLineSegment(p2, p4));
                                info.DisplaySegs = segs;
                            }
                            else if (run.HasLongTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    var _dy = 300;
                                    var vs1 = new List<Vector2d> { new Vector2d(0, -780 - _dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy + _dy) };
                                    var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                    var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                    info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                }
                                else
                                {
                                    var _dy = 300;
                                    var vs1 = new List<Vector2d> { new Vector2d(0, -780 + _dy), new Vector2d(121, -121), new Vector2d(1258, 0), new Vector2d(121, -120), new Vector2d(0, -779 - dy - _dy) };
                                    var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                    var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                    info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                }
                            }
                        }
                        if (bi.BlueToRightMiddle)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(HEIGHT.ToRatioInt(9, 24));
                            var p3 = info.EndPoint.OffsetX(300);
                            var p4 = p3.OffsetY(HEIGHT.ToRatioInt(6, 24));
                            var segs = info.Segs.ToList();
                            segs.Add(new GLineSegment(p2, p4));
                            info.DisplaySegs = segs;
                        }
                        {
                            var vecs = new List<Vector2d> { new Vector2d(0, -900), new Vector2d(-121, -121), new Vector2d(-1479, 0), new Vector2d(0, -499), new Vector2d(-200, -200) };
                            if (bi.HasLongTranslatorToLeft)
                            {
                                var vs = vecs;
                                info.DisplaySegs = vecs.ToGLineSegments(info.StartPoint);
                                if (!bi.IsLast)
                                {
                                    var pt = vs.Take(vs.Count - 1).GetLastPoint(info.StartPoint);
                                    info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(0, -280) }.ToGLineSegments(pt));
                                }
                            }
                            if (bi.HasLongTranslatorToRight)
                            {
                                var vs = vecs.GetYAxisMirror();
                                info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                if (!bi.IsLast)
                                {
                                    var pt = vs.Take(vs.Count - 1).GetLastPoint(info.StartPoint);
                                    info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(0, -280) }.ToGLineSegments(pt));
                                }
                            }
                        }
                    }
                    if (run.LeftHanging != null)
                    {
                        handleHanging(run.LeftHanging, true);
                    }
                    if (run.RightHanging != null)
                    {
                        handleHanging(run.RightHanging, false);
                    }
                    if (run.BranchInfo != null)
                    {
                        handleBranchInfo(run, info);
                    }
                    if (run.ShowShortTranslatorLabel)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(76, 76), new Vector2d(-424, 424), new Vector2d(-1900, 0) };
                        var segs = vecs.ToGLineSegments(info.EndPoint).Skip(1).ToList();
                        DrawDraiNoteLines(segs);
                        DrawDraiNoteLines(segs);
                        var text = "DN100乙字弯";
                        var pt = segs.Last().EndPoint;
                        DrawNoteText(text, pt);
                    }
                    if (run.HasCheckPoint)
                    {
                        if (run.HasShortTranslator)
                        {
                            DrawPipeCheckPoint(info.Segs.Last().StartPoint.OffsetY(280).ToPoint3d(), false);
                        }
                        else
                        {
                            DrawPipeCheckPoint(info.EndPoint.OffsetY(280).ToPoint3d(), false);
                        }
                    }
                    if (run.HasHorizontalShortLine)
                    {
                        DrawHorizontalLineOnPipeRun(HEIGHT, info.BasePoint.ToPoint3d());
                    }
                    if (run.HasCleaningPort)
                    {
                        if (run.HasLongTranslator)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(-200, 200), new Vector2d(0, 300), new Vector2d(121, 121), new Vector2d(1109, 0), new Vector2d(121, 121), new Vector2d(0, 279) };
                            if (run.IsLongTranslatorToLeftOrRight == false)
                            {
                                vecs = vecs.GetYAxisMirror();
                            }
                            if (run.HasShortTranslator)
                            {
                                var segs = vecs.ToGLineSegments(info.Segs.Last(2).StartPoint.OffsetY(-300));
                                drawDomePipes(segs);
                                DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, 2);
                            }
                            else
                            {
                                var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-300));
                                drawDomePipes(segs);
                                DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, 2);
                            }
                        }
                        else
                        {
                            DrawCleaningPort(info.StartPoint.OffsetY(-300).ToPoint3d(), true, 2);
                        }

                    }

                    if (run.HasShortTranslator)
                    {
                        DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                    }
                    if (viewModel?.Params?.ShouldRaiseWashingMachine ?? false)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(-121, 121), new Vector2d(-2059, 0) };
                        var segs = vecs.ToGLineSegments(info.HangingEndPoint.OffsetY(300));
                        drawDomePipes(segs);
                        DrawWashingMachineRaisingSymbol(segs.Last().EndPoint, true);
                    }
                }
            }

            var dx = 0;
            for (int j = 0; j < COUNT; j++)
            {
                var gpItem = pipeGroupItems[j];
                var thwPipeLine = new ThwPipeLine();
                thwPipeLine.Comments = gpItem.Labels.Concat(gpItem.TlLabels.Yield()).ToList();


                var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();

                for (int i = 0; i < allNumStoreyLabels.Count; i++)
                {
                    var storey = allNumStoreyLabels[i];
                    var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                    {
                        HasLongTranslator = gpItem.Items[i].HasLong,
                        HasShortTranslator = gpItem.Items[i].HasShort,
                        HasCleaningPort = gpItem.Hangings[i].HasCleaningPort,
                    } : null;
                    runs.Add(run);

                }
                for (int i = 0; i < allNumStoreyLabels.Count; i++)
                {
                    var FloorDrainsCount = gpItem.Hangings[i].FloorDrainsCount;
                    var hasSCurve = gpItem.Hangings[i].HasSCurve;
                    if (FloorDrainsCount > 0 || hasSCurve)
                    {
                        var run = runs.TryGet(i - 1);
                        if (run != null)
                        {
                            var hanging = run.LeftHanging = new Hanging();
                            hanging.FloorDrainsCount = FloorDrainsCount;
                            hanging.HasSCurve = hasSCurve;
                        }
                    }
                }


                {
                    bool? flag = null;
                    for (int i = runs.Count - 1; i >= 0; i--)
                    {
                        var r = runs[i];
                        if (r == null) continue;
                        if (r.HasLongTranslator)
                        {
                            if (!flag.HasValue)
                            {
                                flag = true;
                            }
                            else
                            {
                                flag = !flag.Value;
                            }
                            r.IsLongTranslatorToLeftOrRight = flag.Value;
                        }
                    }
                }
                {
                    bool? flag = null;
                    for (int i = runs.Count - 1; i >= 0; i--)
                    {
                        var r = runs[i];
                        if (r == null) continue;
                        if (r.HasShortTranslator)
                        {
                            if (!flag.HasValue)
                            {
                                flag = true;
                            }
                            else
                            {
                                flag = !flag.Value;
                            }
                            r.IsShortTranslatorToLeftOrRight = flag.Value;
                        }
                    }
                }

                var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                handlePipeLine(thwPipeLine, arr);

                for (int i = 0; i < allNumStoreyLabels.Count; i++)
                {
                    var info = arr.TryGet(i);
                    if (info != null && info.Visible)
                    {
                        var segs = info.DisplaySegs ?? info.Segs;
                        if (segs != null)
                        {
                            drawDomePipes(segs);
                        }
                    }
                }
                for (int i = 0; i < allNumStoreyLabels.Count; i++)
                {
                    var info = arr.TryGet(i);
                    if (info != null && info.Visible)
                    {
                        void TestRightSegsMiddle()
                        {
                            var segs = info.RightSegsMiddle;
                            if (segs != null)
                            {
                                vent_lines.AddRange(segs);
                            }
                        }
                        void TestRightSegsLast()
                        {
                            var segs = info.RightSegsLast;
                            if (segs != null)
                            {
                                vent_lines.AddRange(segs);
                            }
                        }
                        void TestRightSegsFirst()
                        {
                            var segs = info.RightSegsFirst;
                            if (segs != null)
                            {
                                vent_lines.AddRange(segs);
                            }
                        }
                        void Run()
                        {
                            var storey = allNumStoreyLabels[i];
                            var maxStorey = allNumStoreyLabels.Last();
                            if (gpItem.HasTl)
                            {
                                bool isFirstTl()
                                {
                                    return (gpItem.MaxTl == GetStoreyScore(storey));
                                }
                                if (isFirstTl())
                                {
                                    var segs = info.RightSegsFirst;
                                    if (segs != null)
                                    {
                                        vent_lines.AddRange(segs);
                                    }
                                }
                                else if (gpItem.MinTl + "F" == storey)
                                {
                                    var segs = info.RightSegsLast;
                                    if (segs != null)
                                    {
                                        vent_lines.AddRange(segs);
                                    }
                                }
                                else if (GetStoreyScore(storey).InRange(gpItem.MinTl, gpItem.MaxTl))
                                {
                                    var segs = info.RightSegsMiddle;
                                    if (segs != null)
                                    {
                                        vent_lines.AddRange(segs);
                                    }
                                }

                            }
                        }
                        Run();
                    }
                }

                {
                    var i = allNumStoreyLabels.IndexOf("1F");
                    if (i >= 0)
                    {
                        var storey = allNumStoreyLabels[i];
                        var info = arr.First();
                        if (info != null && info.Visible)
                        {
                            DrawOutputs1(info.EndPoint, 3600, new ThwOutput()
                            {
                                DirtyWaterWellValues = gpItem.WaterPortLabels.ToList(),
                                HasWrappingPipe1 = gpItem.HasWrappingPipe,
                            });
                        }
                    }
                }

            }


            {
                var auto_conn = false;
                if (auto_conn)
                {
                    foreach (var g in GeoFac.GroupParallelLines(dome_lines, 1, .01))
                    {
                        var line = DU.DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: 10e6));
                        line.Layer = dome_layer;
                        line.ColorIndex = 256;
                    }
                }
                else
                {
                    foreach (var dome_line in dome_lines)
                    {
                        var line = DU.DrawLineSegmentLazy(dome_line);
                        line.Layer = dome_layer;
                        line.ColorIndex = 256;
                    }
                    foreach (var _line in vent_lines)
                    {
                        var line = DU.DrawLineSegmentLazy(_line);
                        line.Layer = vent_layer;
                        line.ColorIndex = 256;
                    }

                }

            }
        }

        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-DRAI-NOTE";
                e.ColorIndex = 256;
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static void DrawDomePipes(params GLineSegment[] segs)
        {
            DrawDomePipes((IEnumerable<GLineSegment>)segs);
        }
        public static void DrawDomePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
        }
        public static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line =>
            {
                line.Layer = "W-RAIN-PIPE";
                line.ColorIndex = 256;
            });
        }
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line =>
            {
                line.Layer = "W-DRAI-NOTE";
                line.ColorIndex = 256;
            });
        }
        public static void qvg1qe()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var vecs = new List<Vector2d> { new Vector2d(0, -479), new Vector2d(-121, -121), new Vector2d(-1879, 0) };
                var pts = vecs.ToPoint2ds(basePt);
                var segs = vecs.ToGLineSegments(basePt);
                DrainageSystemDiagram.DrawDomePipes(segs);
                DrawWrappingPipe(pts.Last().OffsetX(300));
                DrawCleaningPort(pts[2].ToPoint3d(), false, 1);
                DrawDiryWaterWells1(basePt + new Vector2d(-2000 - 400, -600), new List<string>() { "1", "2" });
                DrawNoteText("DN100", pts[3].OffsetX(750));
            }
        }
        public static void qvg1vf()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var vecs = new List<Vector2d> { new Vector2d(0, -1479), new Vector2d(-121, -121), new Vector2d(-2379, 0), new Vector2d(0, 600), new Vector2d(1779, 0), new Vector2d(121, 121), new Vector2d(0, 579), new Vector2d(-1600, -700), new Vector2d(0, -600) };
                var pts = vecs.ToPoint2ds(basePt);
                var segs = vecs.ToGLineSegments(basePt);
                {
                    var _segs = segs.ToList();
                    _segs.RemoveAt(8);
                    _segs.RemoveAt(7);
                    _segs.RemoveAt(3);
                    DrawDomePipes(_segs);
                }
                {
                    DrawCleaningPort(pts[2].ToPoint3d(), false, 1);
                    DrawCleaningPort(pts[5].ToPoint3d(), false, 1);
                    DrawCleaningPort(pts[7].ToPoint3d(), true, 2);
                    DrawWrappingPipe(pts[8]);
                    DrawWrappingPipe(pts[9]);
                    DrawDiryWaterWells1(basePt + new Vector2d(-2000 - 400 - 500, -600 - 400 - 400), new List<string>() { "1", "2" });
                    DrawNoteText("DN100", pts[3].OffsetX(750));
                    DrawNoteText("DN100", pts[4].OffsetX(750));
                }
            }
        }
        public static void qvg7cd()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                DU.DrawLineSegmentLazy(new GLineSegment(basePt, basePt.OffsetX(550 + 3038)), "W-NOTE");
                DrawStoreyHeightSymbol(basePt.OffsetX(550), "-0.35");
                var p1 = basePt + new Vector2d(2830, -390);
                DU.DrawBlockReference("地漏系统", p1.ToPoint3d(), br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏S弯");
                    }
                });
                var vecs = new List<Vector2d> { new Vector2d(180, -460), new Vector2d(0, -94), new Vector2d(141, -141), new Vector2d(1359, 0) };
                var segs = vecs.ToGLineSegments(p1).Skip(1).ToList();
                DrawDomePipes(segs);
                DrawWrappingPipe(segs[1].EndPoint.OffsetX(150));
                DrawNoteText("DN100", segs[1].EndPoint.OffsetX(450));
                var p2 = segs.Last().EndPoint;
                DrawDiryWaterWells2(p2, new List<string>() { "1", "2" });
            }
        }
        public static void qvg7cs()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                //DrawStoreyHeightSymbol(basePt, "-0.75");
                //DU.DrawLineSegmentLazy(new GLineSegment(basePt, basePt.OffsetX(2450 )), "W-NOTE");
                DU.DrawBlockReference("侧排地漏", basePt.ToPoint3d(), cb: br => br.Layer = "W-DRAI-FLDR");
            }
        }

        public static void DrawStoreyHeightSymbol(Point2d basePt, string label = "666")
        {
            DU.DrawBlockReference(blkName: "标高", basePt: basePt.ToPoint3d(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", label } });
        }

        public static void DrawWrappingPipe(Point2d basePt)
        {
            DrawWrappingPipe(basePt.ToPoint3d());
        }
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DU.DrawBlockReference("套管系统", basePt, br =>
            {
                br.Layer = "W-BUSH";
            });
        }

        public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * .7;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + 150;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-200);
            var p3 = p1.OffsetX(200);
            var line = DU.DrawLineLazy(p2, p3);
            line.Layer = "W-DRAI-NOTE";
        }

        public static Point2d NewMethod5(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), false);
            return lastPt;
        }

        public static Point2d NewMethod4(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS;
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), true);
            return lastPt;
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight)
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DU.DrawBlockReference("地漏系统", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("地漏系统", basePt,
               br =>
               {
                   br.Layer = "W-DRAI-EQPM";
                   br.ScaleFactors = new Scale3d(-2, 2, 2);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                   }
               });
            }

        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("S型存水弯", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("S型存水弯", basePt,
                   br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                       br.ScaleFactors = new Scale3d(-2, 2, 2);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                       }
                   });
            }
        }
        public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                });
            }
            else
            {
                DU.DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90 + 180);
                });
            }
        }
        public static void DrawPipeCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DU.DrawBlockReference(blkName: "立管检查口", basePt: basePt,
cb: br =>
{
    if (leftOrRight)
    {
        br.ScaleFactors = new Scale3d(-1, 1, 1);
    }
    br.Layer = "W-DRAI-EQPM";
});
        }

        public static void NewMethod2(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2, double height1)
        {

            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var v = new Vector2d(lastPt1.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
            {
                var height2 = HEIGHT + lastPt2.Y;
                var segs = points2.ToGLineSegments(basePt.OffsetY(HEIGHT - height2));
                DrawDomePipes(segs);
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        public static void NewMethod3(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2)
        {
            var height1 = LONG_TRANSLATOR_HEIGHT1;
            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var v = new Vector2d(lastPt1.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
            {
                var height2 = HEIGHT + lastPt2.Y;
                var segs = points2.ToGLineSegments(basePt.OffsetY(HEIGHT - height2));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        public static void NewMethod1(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = HEIGHT + lastPt.Y;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
                v += new Vector2d(lastPt.X, 0);
            }
        }

        public static void NewMethod(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = LONG_TRANSLATOR_HEIGHT1;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));

            {
                var p1 = basePt.OffsetX(points.Last().X);
                var p2 = p1.OffsetY(HEIGHT - height + lastPt.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            v += new Vector2d(lastPt.X, 0);
        }
        public static void DrawDirtyWaterWell(Point2d basePt, string value)
        {
            DrawDirtyWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawDirtyWaterWell(Point3d basePt, string value)
        {
            DU.DrawBlockReference(blkName: "污废合流井编号", basePt: basePt.OffsetY(-400),
            props: new Dictionary<string, string>() { { "-", value } },
            cb: br =>
            {
                br.Layer = "W-DRAI-EQPM";
            });
        }
        public static void SetVentPipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-VENT-PIPE";
            line.ColorIndex = 256;
        }

        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-DOME-PIPE";
            line.ColorIndex = 256;
        }
    }
    public class ThDrainageService
    {
        public class CommandContext
        {
            public Point3dCollection range;
            public ThMEPWSS.Pipe.Service.ThRainSystemService.StoreyContext StoreyContext;
            public Diagram.ViewModel.DrainageSystemDiagramViewModel ViewModel;
            public System.Windows.Window window;
        }
        public static CommandContext commandContext;
        public static void ConnectLabelToLabelLine(DrainageGeoData geoData)
        {
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1500, 250);
                {
                    var e = DU.DrawRectLazy(g);
                    e.ColorIndex = 2;
                }
                var _lineHGs = f1(g.ToPolygon());
                var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                if (geo == null) continue;
                {
                    var ents = geo.ToDbObjects().OfType<Entity>().ToList();
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(100, 400) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(.1, 400))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(.1));
                    }
                }
            }
        }

        public static void PreFixGeoData(DrainageGeoData geoData)
        {
            for (int i = 0; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(5))
                {
                    geoData.LabelLines[i] = seg.Extend(6);
                }
            }
            for (int i = 0; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(5);
            }
            for (int i = 0; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(5);
            }
        }
        public static bool HasWL(string label)
        {
            //暂不支持污废分流
            return label.StartsWith("WL");
        }
        public static double GetAiringValue()
        {
            //伸顶通气的值，上人屋面伸顶2000，不上人屋面伸顶500。从面板读取。
            return GetCanPeopleBeOnRoof() ? 2000 : 500;
        }
        public static bool GetCanPeopleBeOnRoof()
        {
            return false;
        }
        public static bool HasAiringHorizontalPipe(string storeyLabel, List<Geometry> PLs, List<Geometry> FLs, List<Geometry> toilets)
        {
            //6.4	通气立管的横管
            //在通气立管出现的每一层都和PL（+DL）或FL进行连通，直至有排水点位的最高层为止。
            //出现通气立管的最高层的判断：
            //在普通楼层（数字+F）找到PL或FL附近300范围内有卫生间的最高楼层即可，这个楼层就是连接通气立管的最高楼层。
            bool test3()
            {
                if (FLs.Count == 0 || toilets.Count == 0) return false;
                var toiletsGeo = GeoFac.CreateGeometry(toilets);
                foreach (var fl in FLs)
                {
                    if (GeoFac.CreateCirclePolygon(fl.GetCenter(), 300, 36).Intersects(toiletsGeo)) return true;
                }
                return false;
            }
            return Regex.IsMatch(storeyLabel, @"^\d+F$") && ((PLs.Count > 0) || test3());
        }
        public static bool IsNotedLabel(string label)
        {
            //接至厨房单排、接至卫生间单排。。。
            return label.Contains("单排") || label.Contains("设置乙字弯");
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return false;
            return label.StartsWith("FL") || label.StartsWith("PL") || label.StartsWith("TL") || label.StartsWith("DL");
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return false;
            return label.StartsWith("FL") || label.StartsWith("PL") || label.StartsWith("TL") || label.StartsWith("DL") || label.StartsWith("Y1L") || label.StartsWith("Y2L") || label.StartsWith("NL") || label.StartsWith("YL") || label.Contains("单排");
        }
        public static void CollectFloorListDatasEx()
        {
            static ThRainSystemService.StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb, List<ThMEPWSS.Model.FloorFramed> resFloors)
            {
                var ctx = new ThRainSystemService.StoreyContext();

                if (range != null)
                {
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    storeysRecEngine.Recognize(adb.Database, range);
                    ctx.thStoreys = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
                }
                else
                {
                    ctx.thStoreys = resFloors.Select(x => new ThMEPEngineCore.Model.Common.ThStoreys(x.blockId)).ToList();
                }


                var storeys = new List<ThStoreysData>();
                foreach (var s in ctx.thStoreys)
                {
                    var e = adb.Element<Entity>(s.ObjectId);
                    var data = new ThStoreysData()
                    {
                        Boundary = e.Bounds.ToGRect(),
                        Storeys = s.Storeys,
                        StoreyType = s.StoreyType,
                    };
                    storeys.Add(data);
                }
                ThRainSystemService.FixStoreys(storeys);
                ctx.thStoreysDatas = storeys;
                return ctx;
            }

            Dbg.FocusMainWindow();
            if (ThMEPWSS.Common.FramedReadUtil.SelectFloorFramed(out List<ThMEPWSS.Model.FloorFramed> resFloors))
            {
                var ctx = commandContext;
                using var adb = AcadDatabase.Active();
                ctx.StoreyContext = GetStoreyContext(null, adb, resFloors);
                InitFloorListDatas(adb);
            }
        }
        public static void CollectFloorListDatas()
        {
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            var ctx = commandContext;
            ctx.range = range;
            using var adb = AcadDatabase.Active();
            ctx.StoreyContext = GetStoreyContext(range, adb);
            InitFloorListDatas(adb);
        }
        public static ThRainSystemService.StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb)
        {
            var ctx = new ThRainSystemService.StoreyContext();
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            ctx.thStoreys = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().ToList();
            foreach (var s in ctx.thStoreys)
            {
                var e = adb.Element<Entity>(s.ObjectId);
                var data = new ThStoreysData()
                {
                    Boundary = e.Bounds.ToGRect(),
                    Storeys = s.Storeys,
                    StoreyType = s.StoreyType,
                };
                storeys.Add(data);
            }
            ThRainSystemService.FixStoreys(storeys);
            ctx.thStoreysDatas = storeys;
            return ctx;
        }
        public static void InitFloorListDatas(AcadDatabase adb)
        {
            var ctx = commandContext.StoreyContext;
            var storeys = ctx.GetObjectIds()
            .Select(o => adb.Element<BlockReference>(o))
            .Where(o => o.ObjectId.IsValid && o.GetBlockEffectiveName() == ThWPipeCommon.STOREY_BLOCK_NAME)
            .Select(o => o.ObjectId)
            .ToObjectIdCollection();
            var service = new ThReadStoreyInformationService();
            service.Read(storeys);
            commandContext.ViewModel.FloorListDatas = service.StoreyNames.Select(o => o.Item2).ToList();
        }
        public class WLGrouper
        {
            public class ToiletGrouper
            {
                public List<string> PLs;//污废合流立管
                public List<string> TLs;//通气立管
                public List<string> DLs;//沉箱立管
                public List<string> FLs;//废水立管
                public WLType WLType;
                public void Init()
                {
                    PLs ??= new List<string>();
                    TLs ??= new List<string>();
                    DLs ??= new List<string>();
                    FLs ??= new List<string>();
                }
            }

        }
        public enum WLType
        {
            PL, PL_TL, PL_DL, PL_TL_DL, FL, FL_TL,
        }
        public class WLGeoGrouper
        {
            public class ToiletGrouper
            {
                public List<Geometry> PLs;//污废合流立管
                public List<Geometry> TLs;//通气立管
                public List<Geometry> DLs;//沉箱立管
                public List<Geometry> FLs;//废水立管
                public WLType WLType;
                public void Init()
                {
                    PLs ??= new List<Geometry>();
                    TLs ??= new List<Geometry>();
                    DLs ??= new List<Geometry>();
                    FLs ??= new List<Geometry>();
                }
                public static List<ToiletGrouper> DoGroup(List<Geometry> PLs, List<Geometry> TLs, List<Geometry> DLs, List<Geometry> FLs)
                {
                    var list = new List<ToiletGrouper>();
                    var ok_pipes = new HashSet<Geometry>();
                    foreach (var pl in PLs)
                    {
                        ok_pipes.Add(pl);
                        var range = GeoFac.CreateCirclePolygon(pl.GetCenter(), 300, 12);
                        //在每一根PL的每一层300范围内找TL
                        var tls = GeoFac.CreateIntersectsSelector(TLs.Except(ok_pipes).ToList())(range);
                        ok_pipes.AddRange(tls);
                        //在每一根PL的每一层300范围内找DL
                        var dls = GeoFac.CreateIntersectsSelector(DLs.Except(ok_pipes).ToList())(range);
                        ok_pipes.AddRange(dls);
                        var o = new ToiletGrouper();
                        list.Add(o);
                        o.Init();
                        o.PLs.Add(pl);
                        o.TLs.AddRange(tls);
                        o.DLs.AddRange(dls);
                        if (tls.Count == 0 && dls.Count == 0)
                        {
                            o.WLType = WLType.PL;
                        }
                        else if (tls.Count > 0 && dls.Count > 0)
                        {
                            o.WLType = WLType.PL_TL_DL;
                        }
                        else if (tls.Count > 0 && dls.Count == 0)
                        {
                            o.WLType = WLType.PL_TL;
                        }
                        else if (tls.Count == 0 && dls.Count > 0)
                        {
                            o.WLType = WLType.PL_DL;
                        }
                        else
                        {
                            throw new System.Exception(nameof(ToiletGrouper));
                        }
                    }
                    foreach (var fl in FLs)
                    {
                        ok_pipes.Add(fl);
                        var o = new ToiletGrouper();
                        list.Add(o);
                        o.Init();
                        o.FLs.Add(fl);
                        //在每一根FL的每一层300范围内找TL
                        var range = GeoFac.CreateCirclePolygon(fl.GetCenter(), 300, 12);
                        var tls = GeoFac.CreateIntersectsSelector(TLs.Except(ok_pipes).ToList())(range);
                        ok_pipes.AddRange(tls);
                        if (tls.Count == 0)
                        {
                            o.WLType = WLType.FL;
                        }
                        else
                        {
                            o.WLType = WLType.FL_TL;
                        }
                    }
                    return list;
                }
            }
        }


    }
}


