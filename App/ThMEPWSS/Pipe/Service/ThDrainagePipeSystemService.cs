namespace ThMEPWSS.Pipe.Service
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using ThMEPWSS.JsonExtensionsNs;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using Autodesk.AutoCAD.DatabaseServices;
    using Dreambuild.AutoCAD;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using NFox.Cad;
    using ThCADExtension;
    using ThMEPEngineCore.Engine;
    using System.Text;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using NetTopologySuite.Geometries;
    public class ThDrainageSystemServiceGeoCollector
    {
        public AcadDatabase adb;
        public DrainageGeoData geoData;
        public List<Entity> entities;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<CText> cts => geoData.Labels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GLineSegment> vlines => geoData.VLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;

        public void CollectStoreys(Point3dCollection range)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
            {
                var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                storeys.Add(bd);
            }
        }
        public void CollectEntities()
        {
            IEnumerable<Entity> GetEntities()
            {
                foreach (var ent in adb.ModelSpace.OfType<Entity>())
                {
                    if (ent is BlockReference br)
                    {
                        if (br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 20000 && r.Width < 80000 && r.Height > 5000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        else if (br.Layer == "块")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        //else if (br.Layer == "C-XREF-INT")
                        //{
                        //    if (br.ObjectId.IsValid && br.GetEffectiveName() == "__附着_W20-8-提资文件_SEN23WUB_设计区")
                        //    {
                        //        foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                        //        {
                        //            yield return e;
                        //        }
                        //    }
                        //}
                        else
                        {
                            yield return ent;
                        }
                    }
                    else
                    {
                        yield return ent;
                    }
                }
            }
            var entities = GetEntities().ToList();
            this.entities = entities;
        }
        public void CollectCleaningPorts()
        {
            //obb
            {
                static bool f(string layer) => layer == "W-DRAI-EQPM";
                Point3d? pt = null;
                foreach (var e in entities.OfType<BlockReference>().Where(x => f(x.Layer) && x.ObjectId.IsValid && x.GetEffectiveName() == "清扫口系统"))
                {
                    if (!pt.HasValue)
                    {
                        var blockTableRecord = adb.Blocks.Element(e.BlockTableRecord);
                        var r = blockTableRecord.GeometricExtents().ToGRect();
                        pt = GeoAlgorithm.MidPoint(r.LeftButtom, r.RightButtom).ToPoint3d();
                    }
                    cleaningPorts.Add(pt.Value.TransformBy(e.BlockTransform).ToPoint2d());
                }
            }

            foreach (var e in entities.OfType<BlockReference>().Where(e => e.Layer == "W-DRAI-EQPM" && e.ObjectId.IsValid && e.GetEffectiveName() == "$TwtSys$00000136"))
            {
                cleaningPorts.Add(e.Bounds.ToGRect().Center);
            }
        }
        public void CollectLabelLines()
        {
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-FRPT-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE";
            foreach (var e in entities.OfType<Line>().Where(e => f(e.Layer) && e.Length > 0))
            {
                labelLines.Add(e.ToGLineSegment());
            }
        }
        public void CollectDLines()
        {
            dlines.AddRange(GetLines(entities, layer => layer == "W-DRAI-DOME-PIPE"));
        }
        public void CollectVLines()
        {
            vlines.AddRange(GetLines(entities, layer => layer == "W-DRAI-VENT-PIPE"));
        }
        public static IEnumerable<GLineSegment> GetLines(IEnumerable<Entity> entities, Func<string, bool> f)
        {
            foreach (var e in entities.OfType<Entity>().Where(e => f(e.Layer)).ToList())
            {
                if (e is Line line && line.Length > 0)
                {
                    //wLines.Add(line.ToGLineSegment());
                    yield return line.ToGLineSegment();
                }
                else if (ThRainSystemService.IsTianZhengElement(e))
                {
                    //有些天正线炸开是两条，看上去是一条，这里当成一条来处理

                    //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                    //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                    //{
                    //    wLines.Add(ln.ToGLineSegment());
                    //}

                    if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                    {
                        //wLines.Add(seg);
                        if (seg.Length > 0) yield return seg;
                    }
                    else foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            if (ln.Length > 0)
                            {
                                //wLines.Add(ln.ToGLineSegment());
                                yield return ln.ToGLineSegment();
                            }
                        }
                }
            }
        }
        int distinguishDiameter = 35;
        public void CollectWrappingPipes()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
            wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
        }
        public void CollectVerticalPipes()
        {
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-RAIN-EQPM";
            {
                var pps = new List<Entity>();
                pps.AddRange(entities.OfType<BlockReference>()
                .Where(e => f(e.Layer))
                .Where(e => e.ObjectId.IsValid && e.GetEffectiveName() is "立管编号" or "带定位立管"));
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                    var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                    if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                    return GeoAlgorithm.GetBoundaryRect(ent);
                }

                foreach (var pp in pps)
                {
                    pipes.Add(getRealBoundaryForPipe(pp));
                }
            }
            {
                var pps = new List<Circle>();
                pps.AddRange(entities.OfType<Circle>()
                .Where(x => f(x.Layer))
                .Where(c => distinguishDiameter <= c.Radius && c.Radius <= 100));
                static GRect getRealBoundaryForPipe(Circle c)
                {
                    return c.Bounds.ToGRect();
                }
                foreach (var pp in pps.Distinct())
                {
                    pipes.Add(getRealBoundaryForPipe(pp));
                }
            }
            {
                var pps = new List<Entity>();
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer is "W-DRAI-PIEP-RISR" or "W-DRAI-EQPM" && e.ObjectId.IsValid && e.GetEffectiveName() == "A$C58B12E6E"));
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer == "PIPE-喷淋" && e.ObjectId.IsValid && e.GetEffectiveName() == "A$C5E4A3C21"));
                pps.AddRange(entities.OfType<Entity>().Where(e => e.Layer == "W-DRAI-EQPM" && e.GetRXClass().DxfName.ToUpper() == "TCH_PIPE"));//bounds是一个点，炸开后才能获取正确的bounds
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer == "W-DRAI-EQPM" && e.ObjectId.IsValid && e.GetEffectiveName().Contains("$LIGUAN")));
                //pps.AddRange(entities.OfType<Entity>().Where(e => e.Layer == "__附着_W20-8-提资文件_SEN23WUB_设计区$0$W-DRAI-EQPM" && e.GetRXClass().DxfName.ToUpper() == "TCH_PIPE"));
                //pps.AddRange(entities.OfType<Circle>().Where(e => e.Layer == "__附着_W20-8-提资文件_SEN23WUB_设计区$0$W-DRAI-EQPM"));
                static GRect getRealBoundaryForPipe(Entity c)
                {
                    var r = c.Bounds.ToGRect();
                    if (!r.IsValid) r = GRect.Create(r.Center, 50);
                    return r;
                }
                foreach (var pp in pps.Distinct())
                {
                    pipes.Add(getRealBoundaryForPipe(pp));
                }
            }
        }

        public void CollectCTexts()
        {
            {
                foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-DRAI-DIMS" && e.GetRXClass().DxfName.ToUpper() == "TCH_VPIPEDIM"))
                {
                    var colle = e.ExplodeToDBObjectCollection();
                    {
                        var ee = colle.OfType<Entity>().FirstOrDefault(e => e.GetRXClass().DxfName.ToUpper() == "TCH_TEXT");
                        var dbText = ((DBText)ee.ExplodeToDBObjectCollection()[0]);
                        cts.Add(new CText() { Text = dbText.TextString, Boundary = dbText.Bounds.ToGRect() });
                        labelLines.AddRange(colle.OfType<Line>().Where(x => x.Length > 0).Select(x => x.ToGLineSegment()));
                    }
                }
            }
            {
                static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE";
                foreach (var e in entities.OfType<DBText>().Where(e => f(e.Layer)))
                {
                    cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                }
                foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => f(e.Layer) && ThRainSystemService.IsTianZhengElement(e)))
                {
                    foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                    {
                        var ct = new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() };
                        if (!ct.Boundary.IsValid)
                        {
                            var p = e.Position.ToPoint2d();
                            var h = e.Height;
                            var w = h * e.WidthFactor * e.WidthFactor * e.TextString.Length;
                            var r = new GRect(p, p.OffsetXY(w, h));
                            ct.Boundary = r;
                        }
                        cts.Add(ct);
                    }
                }
            }
        }
        public void CollectWaterPorts()
        {
            var ents = new List<BlockReference>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetEffectiveName() == "污废合流井编号"));
            waterPorts.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            waterPortLabels.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
        }
        public void CollectFloorDrains()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && (x.GetEffectiveName()?.Contains("地漏") ?? false)));
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
            //ents.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer == "__附着_W20-8-提资文件_SEN23WUB_设计区$0$W-FRPT-HYDT" && e.ObjectId.IsValid && e.GetEffectiveName() == "__附着_W20-8-提资文件_SEN23WUB_设计区$0$地漏平面"));
            floorDrains.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
        }
    }
    public partial class DrainageSystemDiagram
    {
        private static void setValues(List<ThwPipeLineGroup> groups)
        {
            setValues1(groups);
            var r = groups[2].PL.PipeRuns[8];
            r.HasLongTranslator = true;
            r.HasCleaningPort = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r = groups[2].PL.PipeRuns[9];
            r.BranchInfo = new BranchInfo() { FirstLeftRun = true };
            r = groups[2].PL.PipeRuns[10];
            r.BranchInfo = new BranchInfo() { FirstRightRun = true };
            r = groups[2].PL.PipeRuns[11];
            r.BranchInfo = new BranchInfo() { LastLeftRun = true };
            r = groups[2].PL.PipeRuns[12];
            r.BranchInfo = new BranchInfo() { LastRightRun = true };
            r = groups[2].PL.PipeRuns[13];
            r.BranchInfo = new BranchInfo() { MiddleLeftRun = true };
            r = groups[2].PL.PipeRuns[14];
            r.BranchInfo = new BranchInfo() { MiddleRightRun = true };
            r = groups[2].PL.PipeRuns[15];
            r.BranchInfo = new BranchInfo() { BlueToLeftFirst = true };
            r = groups[2].PL.PipeRuns[16];
            r.BranchInfo = new BranchInfo() { BlueToLeftLast = true };
            r = groups[2].PL.PipeRuns[17];
            r.BranchInfo = new BranchInfo() { BlueToLeftMiddle = true };
            r = groups[2].PL.PipeRuns[18];
            r.BranchInfo = new BranchInfo() { BlueToRightFirst = true };
            r = groups[2].PL.PipeRuns[19];
            r.BranchInfo = new BranchInfo() { BlueToRightLast = true };
            r = groups[2].PL.PipeRuns[20];
            r.BranchInfo = new BranchInfo() { BlueToRightMiddle = true };
            r = groups[2].PL.PipeRuns[21];
            r.BranchInfo = new BranchInfo() { HasLongTranslatorToLeft = true };
            r = groups[2].PL.PipeRuns[22];
            r.BranchInfo = new BranchInfo() { HasLongTranslatorToRight = true };
        }

        private static void setValues1(List<ThwPipeLineGroup> groups)
        {
            var r = groups[1].PL.PipeRuns[7];
            r.HasLongTranslator = true;
            r.HasCleaningPort = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r = groups[1].PL.PipeRuns[8];
            r.HasCleaningPort = true;
            r = groups[1].PL.PipeRuns[9];
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasSCurve = true,
            };
            r = groups[1].PL.PipeRuns[10];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasSCurve = true,
            };
            r = groups[1].PL.PipeRuns[11];
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 1,
                HasSCurve = true,
            };
            r = groups[1].PL.PipeRuns[12];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 1,
                HasSCurve = true,
            };
            r = groups[1].PL.PipeRuns[13];
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 1,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[14];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 1,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[15];
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[16];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[17];
            r.RightHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[18];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = false;
            r.RightHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[19];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r.RightHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[20];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = false;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[21];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = false;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r.RightHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r = groups[1].PL.PipeRuns[22];
            r.HasLongTranslator = true;
            r.IsLongTranslatorToLeftOrRight = true;
            r.LeftHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
            r.RightHanging = new Hanging()
            {
                FloorDrainsCount = 2,
                HasDoubleSCurve = true,
            };
        }
    }
#pragma warning disable
    public partial class DrainageService
    {
        public static void TestDrawingDatasCreation(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas)
        {
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateGeometrySelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            Dbg.AddLazyAction("画骨架", adb =>
            {
                foreach (var s in geoData.Storeys)
                {
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
                {
                    var item = cadDatas[storeyI];
                    foreach (var o in item.LabelLines)
                    {
                        var j = cadDataMain.LabelLines.IndexOf(o);
                        var m = geoData.LabelLines[j];
                        var e = DU.DrawLineSegmentLazy(m);
                        e.ColorIndex = 1;
                    }
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom);
                        e.ColorIndex = 2;
                        var _pl = DU.DrawRectLazy(m.Boundary);
                        _pl.ColorIndex = 2;
                    }
                    foreach (var o in item.VerticalPipes)
                    {
                        var j = cadDataMain.VerticalPipes.IndexOf(o);
                        var m = geoData.VerticalPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 3;
                    }
                    {
                        var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                        foreach (var o in item.WrappingPipes)
                        {
                            var j = cadDataMain.WrappingPipes.IndexOf(o);
                            var m = geoData.WrappingPipes[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                    }
                    foreach (var o in item.FloorDrains)
                    {
                        var j = cadDataMain.FloorDrains.IndexOf(o);
                        var m = geoData.FloorDrains[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 6;
                    }
                    foreach (var o in item.WaterPorts)
                    {
                        var j = cadDataMain.WaterPorts.IndexOf(o);
                        var m = geoData.WaterPorts[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                    }
                    foreach (var o in item.WashingMachines)
                    {
                        var j = cadDataMain.WashingMachines.IndexOf(o);
                        var m = geoData.WashingMachines[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 1;
                    }
                    foreach (var o in item.CleaningPorts)
                    {
                        var j = cadDataMain.CleaningPorts.IndexOf(o);
                        var m = geoData.CleaningPorts[j];
                        if (false) DU.DrawGeometryLazy(new GCircle(m, 50).ToCirclePolygon(36), ents => ents.ForEach(e => e.ColorIndex = 7));
                        DU.DrawRectLazy(GRect.Create(m, 50));
                    }
                    {
                        var cl = Color.FromRgb(4, 229, 230);
                        foreach (var o in item.DLines)
                        {
                            var j = cadDataMain.DLines.IndexOf(o);
                            var m = geoData.DLines[j];
                            var e = DU.DrawLineSegmentLazy(m);
                            e.Color = cl;
                        }
                    }
                }
            });
            Dbg.AddLazyAction("开始分析", adb =>
            {
                foreach (var s in geoData.Storeys)
                {
                    var e = DU.DrawRectLazy(s).ColorIndex = 1;
                }
                var sb = new StringBuilder(8192);
                var drDatas = new List<DrainageDrawingData>();
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
                    {
                        var f = F(item.VerticalPipes);
                        foreach (var label in item.Labels)
                        {
                            if (!ThDrainageService.IsMaybeLabelText(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                            var lst = labellinesf(label);
                            if (lst.Count == 1)
                            {
                                var labelline = lst[0];
                                if (f(GeoFac.CreateGeometry(label, labelline)).Count == 0)
                                {
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == 1)
                                    {
                                        var pt = points[0];
                                        var r = GRect.Create(pt, 50);
                                        geoData.VerticalPipes.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.VerticalPipes.Add(pl);
                                        item.VerticalPipes.Add(pl);
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

                    //关联地漏
                    {
                        var dict = new Dictionary<string, int>();
                        var pipesf = GeoFac.CreateGeometrySelector(item.VerticalPipes);
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
                        sb.AppendLine("地漏：" + dict.ToJson());
                        drData.FloorDrains = dict;
                    }

                    //关联清扫口
                    {
                        var f = GeoFac.CreateGeometrySelector(item.VerticalPipes);
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

                        var f1 = F(item.WaterPorts);

                        var ok_ents = new HashSet<Geometry>();
                        var d = new Dictionary<string, string>();
                        var has_wrappingpipes = new HashSet<string>();

                        {
                            //先提取直接连接的
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var waterPorts = f1(dlinesGeo);
                                if (waterPorts.Count == 1)
                                {
                                    var waterPort = waterPorts[0];
                                    var pipes = f2(dlinesGeo);
                                    ok_ents.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            d[label] = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                                            if (wrappingPipesf(dlinesGeo).Any())
                                            {
                                                has_wrappingpipes.Add(label);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        {
                            //再处理没直接连接的
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
                                            var waterPortLabel = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                                            foreach (var pipe in f2(dlinesGeo))
                                            {
                                                if (lbDict.TryGetValue(pipe, out string label))
                                                {
                                                    d[label] = waterPortLabel;
                                                    ok_ents.Add(pipe);
                                                    if (wrappingPipesf(dlinesGeo).Any())
                                                    {
                                                        has_wrappingpipes.Add(label);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        {
                            sb.AppendLine("排出：" + d.ToJson());
                            drData.Outlets = d;

                            d.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                            {
                                var num = kv1.Value;
                                var pipe = kv2.Key;
                                DU.DrawTextLazy(num, pipe.ToGRect().RightButtom);
                                return 666;
                            }).Count();
                        }
                        {
                            sb.AppendLine("套管：" + has_wrappingpipes.ToJson());
                            drData.WrappingPipes = has_wrappingpipes.ToList();
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
                Dbg.PrintText(sb.ToString());
                Dbg.AddButton("Dbg.PrintText(drDatas.ToJson());", () => { Dbg.PrintText(drDatas.ToJson()); });
                Dbg.AddButton("Dbg.SaveToJsonFile(drDatas);", () => { Dbg.SaveToJsonFile(drDatas); });
                Dbg.AddLazyAction("draw8", adb =>
                {
                    Dbg.FocusMainWindow();
                    var basePt = Dbg.SelectPoint();
                    DrainageSystemDiagram.draw8(drDatas, basePt.ToPoint2d());
                });
            });
        }
    }
    public partial class DrainageSystemDiagram
    {
        public static void draw7(List<DrainageDrawingData> drDatas, Point2d basePoint)
        {
            var labels = drDatas.SelectMany(drData => drData.VerticalPipeLabels).Distinct().ToList();
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            var COUNT = labels.Count;
            var STOREY_COUNT = drDatas.Count;
            var dy = HEIGHT - 1800.0;
            //var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").ToList();
            var groups = new List<ThwPipeLineGroup>();
            foreach (var label in labels)
            {
                var group = new ThwPipeLineGroup();
                groups.Add(group);
                var ppl = group.PL = new ThwPipeLine();
                ppl.Label1 = label;
                ppl.Comments = new List<string>() { label };
                ppl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var storey = storeys[i];
                    var drData = drDatas[i];
                    var run = new ThwPipeRun()
                    {
                        Storey = storey,
                        HasShortTranslator = drData.ShortTranslatorLabels.Contains(label),
                        HasLongTranslator = drData.LongTranslatorLabels.Contains(label),
                        IsShortTranslatorToLeftOrRight = true,
                        IsLongTranslatorToLeftOrRight = true,
                        HasCheckPoint = true,
                        HasCleaningPort = drData.CleaningPorts.Contains(label),
                    };
                    ppl.PipeRuns.Add(run);
                    {
                        bool? flag = null;
                        for (int i1 = ppl.PipeRuns.Count - 1; i1 >= 0; i1--)
                        {
                            var r = ppl.PipeRuns[i1];
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
                        if (drData.FloorDrains.TryGetValue(label, out int count))
                        {
                            if (count > 0)
                            {
                                var hanging = run.LeftHanging = new Hanging();
                                hanging.FloorDrainsCount = count;
                                hanging.HasSCurve = true;
                            }
                        }
                    }
                    {
                        var b = run.BranchInfo = new BranchInfo();
                        b.MiddleLeftRun = true;
                    }
                    {
                        if (storey == "1F")
                            if (drData.Outlets.TryGetValue(label, out string well))
                            {
                                var o = ppl.Output = new ThwOutput();
                                o.DirtyWaterWellValues = new List<string>();
                                o.DirtyWaterWellValues.Add(well);
                            }
                    }

                }
            }

            Dbg.Log(groups);

            NewMethod6(basePoint, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, storeys, groups);
        }

        public class PipeCmpInfo : IEquatable<PipeCmpInfo>
        {
            public string label;
            public struct PipeRunCmpInfo
            {
                public int FloorDrainsCount;
                public bool HasLongTranslator;
                public bool HasShortTranslator;
                public bool HasCleaningPort;
                public bool HasWrappingPipe;
            }
            public List<PipeRunCmpInfo> PipeRuns;
            public bool IsWaterPortOutlet;
            public override int GetHashCode()
            {
                return 0;
            }
            public bool Equals(PipeCmpInfo other)
            {
                return this.IsWaterPortOutlet == other.IsWaterPortOutlet && PipeRuns.SequenceEqual(other.PipeRuns);
            }
        }
        public static void draw8(List<DrainageDrawingData> drDatas, Point2d basePoint)
        {
            var q = drDatas.SelectMany(drData => drData.VerticalPipeLabels).Distinct();
            //q = q.Where(lb => lb.StartsWith("PL"));
            //q = q.Where(lb => lb.StartsWith("FL"));
            //q = q.Where(lb => lb.StartsWith("TL"));
            q = q.Where(lb => ThDrainageService.IsWantedLabelText(lb));
            var labels = q.ToList();
            var lst = new List<PipeCmpInfo>();
            foreach (var label in labels)
            {
                var info = new PipeCmpInfo();
                lst.Add(info);
                info.label = label;
                info.PipeRuns = new List<PipeCmpInfo.PipeRunCmpInfo>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var drData = drDatas[i];
                    var runInfo = new PipeCmpInfo.PipeRunCmpInfo();
                    runInfo.HasShortTranslator = drData.ShortTranslatorLabels.Contains(label);
                    runInfo.HasLongTranslator = drData.LongTranslatorLabels.Contains(label);
                    runInfo.HasCleaningPort = drData.CleaningPorts.Contains(label);
                    runInfo.HasWrappingPipe = drData.WrappingPipes.Contains(label);
                    info.PipeRuns.Add(runInfo);
                }
                info.IsWaterPortOutlet = drDatas.Any(x => x.Outlets.ContainsKey(label));
            }
            var list = lst.GroupBy(x => x).ToList();
            //Dbg.Log(lst.GroupBy(x => x).Select(x => new { k = x.Key, lst = x.ToList() }));
            //return;

            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            var COUNT = list.Count;
            var STOREY_COUNT = drDatas.Count;
            var dy = HEIGHT - 1800.0;
            var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            var groups = new List<ThwPipeLineGroup>();

            foreach (var g in list)
            {
                var info = g.Key;
                var label = g.Key.label;
                var group = new ThwPipeLineGroup();
                groups.Add(group);
                var ppl = group.PL = new ThwPipeLine();
                ppl.Label1 = label;
                ppl.Comments = g.Select(x => x.label).ToList();
                ppl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var storey = storeys[i];
                    var drData = drDatas[i];
                    var run = new ThwPipeRun()
                    {
                        Storey = storey,
                        HasShortTranslator = info.PipeRuns[i].HasShortTranslator,
                        HasLongTranslator = info.PipeRuns[i].HasLongTranslator,
                        IsShortTranslatorToLeftOrRight = true,
                        IsLongTranslatorToLeftOrRight = true,
                        HasCheckPoint = true,
                        HasCleaningPort = info.PipeRuns[i].HasCleaningPort,
                    };
                    ppl.PipeRuns.Add(run);
                    {
                        bool? flag = null;
                        for (int i1 = ppl.PipeRuns.Count - 1; i1 >= 0; i1--)
                        {
                            var r = ppl.PipeRuns[i1];
                            if (r.HasLongTranslator)
                            {
                                if (!flag.HasValue)
                                {
                                    flag = label.StartsWith("PL") || label.StartsWith("FL");
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
                        if (drData.FloorDrains.TryGetValue(label, out int count))
                        {
                            if (count > 0)
                            {
                                var hanging = run.LeftHanging = new Hanging();
                                hanging.FloorDrainsCount = count;
                                hanging.HasSCurve = true;
                            }
                        }
                        else if (label.StartsWith("FL"))
                        {
                            var hanging = run.LeftHanging = new Hanging();
                            hanging.FloorDrainsCount = count;
                            hanging.HasDoubleSCurve = true;
                        }
                    }
                    {
                        var b = run.BranchInfo = new BranchInfo();
                        b.MiddleLeftRun = true;
                    }
                    {
                        if (storey == "1F")
                        {
                            var o = ppl.Output = new ThwOutput();
                            o.DN1 = "DN100";
                            o.HasWrappingPipe1 = g.Key.PipeRuns.FirstOrDefault().HasWrappingPipe;
                            Dbg.Log(g.Key);
                            o.DirtyWaterWellValues = new List<string>();
                            foreach (var _label in g.Select(x => x.label))
                            {
                                if (drData.Outlets.TryGetValue(_label, out string well))
                                {
                                    o.DirtyWaterWellValues.Add(well);
                                }
                            }
                            ppl.Comments.Add("===");
                            ppl.Comments.Add("outlets:");
                            ppl.Comments.AddRange(o.DirtyWaterWellValues);
                        }

                    }

                }
            }

            Dbg.Log(groups);

            NewMethod6(basePoint, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, storeys, groups);
        }
    }
}