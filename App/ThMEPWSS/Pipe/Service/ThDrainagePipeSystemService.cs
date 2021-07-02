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
    using ThMEPWSS.DebugNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Model;

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
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
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
            sideFloorDrains.AddRange(ents.Distinct().OfType<BlockReference>().Where(e => e.ObjectId.IsValid && e.GetEffectiveName() == "侧排地漏").Select(e => e.Bounds.ToGRect().Center));
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
                CreateDrawingDatas(geoData, cadDataMain, cadDatas, out StringBuilder sb, out List<DrainageDrawingData> drDatas);

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

            DrawVer1(new DrawingOption()
            {
                basePoint = basePoint,
                OFFSET_X = OFFSET_X,
                SPAN_X = SPAN_X,
                HEIGHT = HEIGHT,
                COUNT = COUNT,
                storeys = storeys,
                groups = groups
            });
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
        public class DrawingOption
        {
            public int maxStoreyIndex;
            public string layer;
            public Point2d basePoint;
            public double OFFSET_X;
            public double SPAN_X;
            public double HEIGHT;
            public double COUNT;
            public List<string> storeys;
            public bool drawStoreyLine = true;
            public bool test;
            public List<ThwPipeLineGroup> groups;
            public DrawingOption Clone()
            {
                return (DrawingOption)MemberwiseClone();
            }
        }
        public static void Start()
        {
            var file = @"D:\DATA\temp\637602373354770648.json";
            var drDatas = Dbg.LoadFromJsonFile<List<DrainageDrawingData>>(file);

            Dbg.FocusMainWindow();
            //var range = Dbg.TrySelectRange();
            //if (range == null) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //var storeys = ThRainSystemService.GetStoreys(range, adb);
                //ThRainSystemService.FixStoreys(storeys);
                ////Console.WriteLine(storeys.ToCadJson());
                //var storeysItems = new List<StoreysItem>();
                //foreach (var s in storeys)
                //{
                //    var item = new StoreysItem();
                //    storeysItems.Add(item);
                //    switch (s.StoreyType)
                //    {
                //        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                //            break;
                //        case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                //            {
                //                item.Labels = new List<string>() { "RF" };
                //            }
                //            break;
                //        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                //            break;
                //        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                //        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                //            {
                //                item.Ints = s.Storeys.OrderBy(x => x).ToList();
                //                item.Labels = item.Ints.Select(x => x + "F").ToList();
                //            }
                //            break;
                //        default:
                //            break;
                //    }
                //}
                //Console.WriteLine(storeysItems.ToCadJson());
                //Dbg.SaveToJsonFile(storeysItems);
                //return;

                var storeysItems = Dbg.LoadFromJsonFile<List<StoreysItem>>(@"D:\DATA\temp\637604918755722721.json");
                var pt = Dbg.SelectPoint().ToPoint2d();
                Draw(drDatas, pt, storeysItems);
            }
        }
        public class StoreysItem
        {
            public List<int> Ints;
            public List<string> Labels;
            public void Init()
            {
                Ints ??= new List<int>();
                Labels ??= new List<string>();
            }
        }
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetScore(x) - GetScore(y));
        }
        public static int GetScore(string label)
        {
            if (label == null) return 0;
            switch (label)
            {
                case "RF": return ushort.MaxValue;
                case "RF+1": return ushort.MaxValue + 1;
                case "RF+2": return ushort.MaxValue + 2;
                default:
                    {
                        int.TryParse(label.Replace("F", ""), out int ret);
                        return ret;
                    }
            }
        }
        public static void draw10()
        {
            //3号图纸比较好
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            if (!Dbg.TrySelectPoint(out Point3d point3D)) return;

            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;

            var pt = point3D.ToPoint2d();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                ThRainSystemService.FixStoreys(storeys);
                var storeysItems = GetStoreysItem(storeys);
                var geoData = new DrainageGeoData();
                geoData.Init();
                DrainageService.CollectGeoData(range, adb, geoData);
                ThDrainageService.PreFixGeoData(geoData);
                ThDrainageService.ConnectLabelToLabelLine(geoData);
                geoData.FixData();
                var cadDataMain = DrainageCadData.Create(geoData);
                var cadDatas = cadDataMain.SplitByStorey();
                var roomData = DrainageService.CollectRoomData(adb);
                DrainageService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out StringBuilder sb, out List<DrainageDrawingData> drDatas, roomData: roomData);
                DU.Dispose();
                if (!NoDraw)
                {
                    Draw(drDatas, pt, storeysItems);
                }
                else
                {
                    Dbg.SaveToTempJsonFile(drDatas);
                    Dbg.SaveToTempJsonFile(storeysItems);
                }
            }
        }
        static bool NoDraw;
        static bool YesDraw;
        public static void draw13()
        {
            try
            {
                NoDraw = true;
                draw11();
            }
            finally
            {
                NoDraw = false;
            }
        }

       
      
        public static void draw12()
        {
            try
            {
                NoDraw = true;
                draw10();
            }
            finally
            {
                NoDraw = false;
            }
        }

        static bool isWantedStorey(string storey)
        {
            return GetScore(storey) < ushort.MaxValue;
        }

        public static void draw11()
        {
            var drDatas = Dbg.LoadFromJsonFile<List<DrainageDrawingData>>(@"Y:\637607490188931855.json");
            var storeysItems = Dbg.LoadFromJsonFile<List<StoreysItem>>(@"Y:\637607490188941561.json");
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d point3D)) return;
            var pt = point3D.ToPoint2d();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                if (NoDraw)
                {
                    //Draw(drDatas, pt, storeysItems);
                    Console.WriteLine(storeysItems.ToCadJson());
                    var allFls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => x.StartsWith("FL")));
                    var allTls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => x.StartsWith("TL")));
                    var allPls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => x.StartsWith("PL")));

                    var storeys = new List<string>();
                    foreach (var item in storeysItems)
                    {
                        item.Init();
                        storeys.AddRange(item.Labels.Where(isWantedStorey));
                    }
                    storeys = storeys.Distinct().OrderBy(GetScore).ToList();
                    var flstoreylist = new List<KeyValuePair<string, string>>();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            var item = storeysItems[i];
                            foreach (var fl in drDatas[i].VerticalPipeLabels.Where(x => x.StartsWith("FL")))
                            {
                                foreach (var s in item.Labels)
                                {
                                    flstoreylist.Add(new KeyValuePair<string, string>(fl, s));
                                }
                            }
                        }
                    }
                    Console.WriteLine(flstoreylist.ToCadJson());
                    var pls = new HashSet<string>();
                    var tls = new HashSet<string>();
                    var pltlList = new List<PlTlStoreyItem>();
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        var item = storeysItems[i];
                        foreach (var gp in drDatas[i].toiletGroupers)
                        {
                            if (gp.WLType == ThDrainageService.WLType.PL_TL && gp.PLs.Count == 1 && gp.TLs.Count == 1)
                            {
                                var pl = gp.PLs[0];
                                var tl = gp.TLs[0];
                                foreach (var s in item.Labels.Where(x => storeys.Contains(x)))
                                {
                                    var x = new PlTlStoreyItem() { pl = pl, tl = tl, storey = s };
                                    pls.Add(pl);
                                    tls.Add(tl);
                                    pltlList.Add(x);
                                }
                            }
                        }
                    }
                    var plstoreylist = new List<KeyValuePair<string, string>>();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            var item = storeysItems[i];
                            foreach (var pl in drDatas[i].VerticalPipeLabels.Where(x => x.StartsWith("PL")))
                            {
                                if (pls.Contains(pl)) continue;
                                foreach (var s in item.Labels)
                                {
                                    flstoreylist.Add(new KeyValuePair<string, string>(pl, s));
                                }
                            }
                        }
                    }
                    Console.WriteLine(plstoreylist.ToCadJson());
                    Console.WriteLine(flstoreylist.ToCadJson());
                    var tlMinMaxStoreyScoreDict = new Dictionary<string, KeyValuePair<int, int>>();
                    foreach (var tl in tls)
                    {
                        int min = int.MaxValue;
                        int max = int.MinValue;
                        foreach (var item in pltlList)
                        {
                            if (item.tl == tl)
                            {
                                var s = GetScore(item.storey);
                                if (s < min) min = s;
                                if (s > max) max = s;
                                tlMinMaxStoreyScoreDict[tl] = new KeyValuePair<int, int>(min, max);
                            }
                        }
                    }
                    Console.WriteLine(tlMinMaxStoreyScoreDict.ToCadJson());
                    Console.WriteLine(pltlList.OrderBy(x => GetScore(x.storey)).ThenBy(x => x.pl).ToCadJson());
                    var plLongTransList = new List<KeyValuePair<string, string>>();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            var item = storeysItems[i];
                            foreach (var s in item.Labels)
                            {
                                foreach (var pl in drDatas[i].LongTranslatorLabels.Where(x => x.StartsWith("PL")))
                                {
                                    plLongTransList.Add(new KeyValuePair<string, string>(pl, s));
                                }
                            }
                        }
                    }
                    var flLongTransList = new List<KeyValuePair<string, string>>();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            var item = storeysItems[i];
                            foreach (var s in item.Labels)
                            {
                                foreach (var fl in drDatas[i].LongTranslatorLabels.Where(x => x.StartsWith("FL")))
                                {
                                    flLongTransList.Add(new KeyValuePair<string, string>(fl, s));
                                }
                            }
                        }
                    }
                    var plShortTransList = new List<KeyValuePair<string, string>>();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            var item = storeysItems[i];
                            foreach (var s in item.Labels)
                            {
                                foreach (var pl in drDatas[i].ShortTranslatorLabels.Where(x => x.StartsWith("PL")))
                                {
                                    plLongTransList.Add(new KeyValuePair<string, string>(pl, s));
                                }
                            }
                        }
                    }
                    var flShortTransList = new List<KeyValuePair<string, string>>();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            var item = storeysItems[i];
                            foreach (var s in item.Labels)
                            {
                                foreach (var fl in drDatas[i].ShortTranslatorLabels.Where(x => x.StartsWith("FL")))
                                {
                                    flLongTransList.Add(new KeyValuePair<string, string>(fl, s));
                                }
                            }
                        }
                    }
                    var minS = storeys.Select(x => GetScore(x)).Min();
                    var maxS = storeys.Select(x => GetScore(x)).Max();
                    var countS = maxS - minS + 1;
                    {
                        var allstoreys = new List<int>();
                        for (int storey = minS; storey <= maxS; storey++)
                        {

                        }
                        bool hasLong(string label, string storey, PipeType pipeType)
                        {
                            if (pipeType == PipeType.FL)
                            {
                                return flLongTransList.Any(x => x.Key == label && x.Value == storey);
                            }
                            if (pipeType == PipeType.PL)
                            {
                                return plLongTransList.Any(x => x.Key == label && x.Value == storey);
                            }
                            throw new Exception();
                        }
                        bool hasShort(string label, string storey, PipeType pipeType)
                        {
                            if (pipeType == PipeType.FL)
                            {
                                return flShortTransList.Any(x => x.Key == label && x.Value == storey);
                            }
                            if (pipeType == PipeType.PL)
                            {
                                return plShortTransList.Any(x => x.Key == label && x.Value == storey);
                            }
                            throw new Exception();
                        }
                        var flGroupingItems = new List<DrainageGroupingItem>();

                    }
                }
                else
                {
                    Draw(drDatas, pt, storeysItems);
                }
            }
        }
        public class DrainageGroupingItem : IEquatable<DrainageGroupingItem>
        {
            public string Label;
            public struct ValueItem
            {
                public bool HasLong;
                public bool HasShort;
            }
            public ValueItem Value;
            public bool Equals(DrainageGroupingItem other)
            {
                return this.Value.Equals(other.Value);
            }
        }
        public enum PipeType
        {
            FL, PL,
        }
        public class Dict<K, V> where V : new()
        {
            public Dictionary<K, V> d = new Dictionary<K, V>();
            public V this[K k]
            {
                get
                {
                    if (!d.TryGetValue(k, out V v))
                    {
                        v = new V();
                        d[k] = v;
                    }
                    return v;
                }
                set
                {
                    d[k] = value;
                }
            }
        }
        public class PlTlStoreyItem
        {
            public string pl;
            public string tl;
            public string storey;
        }

        public static List<StoreysItem> GetStoreysItem(List<ThStoreysData> storeys)
        {
            var storeysItems = new List<StoreysItem>();
            foreach (var s in storeys)
            {
                var item = new StoreysItem();
                storeysItems.Add(item);
                switch (s.StoreyType)
                {
                    case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                        {
                            item.Labels = new List<string>() { "RF" };
                        }
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                    case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                        {
                            item.Ints = s.Storeys.OrderBy(x => x).ToList();
                            item.Labels = item.Ints.Select(x => x + "F").ToList();
                        }
                        break;
                    default:
                        break;
                }
            }

            return storeysItems;
        }

        public static void Draw(List<DrainageDrawingData> drDatas, Point2d basePoint, List<StoreysItem> storeysItems)
        {
            var allLabels = drDatas.SelectMany(drData => drData.VerticalPipeLabels).Where(lb => ThDrainageService.IsWantedLabelText(lb)).Distinct().ToList();
            //var pipeLabels = allLabels.Where(lb => lb.StartsWith("FL")).OrderBy(x => x).ToList();
            var pipeLabels = allLabels.Where(lb => lb.StartsWith("FL") || lb.StartsWith("PL")).OrderBy(x => x).ToList();

            var pipesCmpInfos = new List<PipeCmpInfo>();
            foreach (var label in pipeLabels)
            {
                var info = new PipeCmpInfo();
                pipesCmpInfos.Add(info);
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
            var list = pipesCmpInfos.GroupBy(x => x).ToList();
            //Dbg.Log(lst.GroupBy(x => x).Select(x => new { k = x.Key, lst = x.ToList() }));
            //return;

            var OFFSET_X = 2500.0;
            var SPAN_X = 6000.0;
            //var HEIGHT = 1800.0;
            var HEIGHT = 3000.0;
            var COUNT = list.Count;
            var STOREY_COUNT = drDatas.Count;



            var pipeLineGroups = new List<ThwPipeLineGroup>();
            var storeys = storeysItems.Where(x => x.Labels != null).SelectMany(x => x.Labels).ToList();
            SortStoreys(storeys);
            foreach (var g in list)
            {
                var info = g.Key;
                var _labels = g.Select(x => x.label).ToList();
                var label = g.Key.label;
                var group = new ThwPipeLineGroup();
                pipeLineGroups.Add(group);
                var ppl = group.PL = new ThwPipeLine();
                ppl.Label1 = label;
                ppl.Comments = g.Select(x => x.label).ToList();
                var runs = ppl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var drData = drDatas[i];

                    foreach (var storey in storeysItems[i].Labels.Yield())
                    {
                        if (!isWantedStorey(storey)) continue;
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
                        //如果有转管，那就加个清扫口
                        if (run.HasLongTranslator || run.HasShortTranslator)
                        {
                            run.HasCleaningPort = true;
                        }
                        //针对pl，设置清扫口
                        if (label.StartsWith("PL"))
                        {
                            run.HasCleaningPort = true;
                            //只要有通气立管，就一定有横管
                            run.HasHorizontalShortLine = true;
                        }
                        runs.Add(run);

                        {
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
                                if (label.StartsWith("PL") || label.StartsWith("FL"))
                                {
                                    b.MiddleLeftRun = true;
                                }
                                else if (label.StartsWith("TL"))
                                {
                                    b.BlueToLeftMiddle = true;
                                }
                            }
                            {
                                if (storey == "1F")
                                {
                                    var o = ppl.Output = new ThwOutput();
                                    o.DN1 = "DN100";
                                    o.HasWrappingPipe1 = g.Key.PipeRuns.FirstOrDefault().HasWrappingPipe;
                                    var hs = new HashSet<string>();
                                    foreach (var _label in g.Select(x => x.label))
                                    {
                                        if (drData.Outlets.TryGetValue(_label, out string well))
                                        {
                                            hs.Add(well);
                                        }
                                    }
                                    o.DirtyWaterWellValues = hs.OrderBy(x => { long.TryParse(x, out long v); return v; }).ToList();
                                    ppl.Comments.Add("===");
                                    ppl.Comments.Add("outlets:");
                                    ppl.Comments.AddRange(o.DirtyWaterWellValues);
                                }

                            }
                        }
                    }



                }
            }
            var pipeLineGroups2 = new List<ThwPipeLineGroup>();


            var maxStorey = storeys.Where(isWantedStorey).FindByMax(GetScore);
            //var minStorey = storeys.Where(x => GetScore(x) < ).FindByMin(GetScore);
            var minStorey = "3F";

            Console.WriteLine(drDatas.Select(x => x.toiletGroupers).ToCadJson());


            for (int j = 0; j < pipeLineGroups.Count; j++)
            {
                var group = new ThwPipeLineGroup();
                pipeLineGroups2.Add(group);
                var ttl = group.TL = new ThwPipeLine();
                var runs = ttl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < pipeLineGroups[j].PL.PipeRuns.Count; i++)
                {
                    var run = new ThwPipeRun();
                    runs.Add(run);
                    run.Storey = pipeLineGroups[j].PL.PipeRuns[i].Storey;
                    run.HasLongTranslator = pipeLineGroups[j].PL.PipeRuns[i].HasLongTranslator;
                    run.IsLongTranslatorToLeftOrRight = pipeLineGroups[j].PL.PipeRuns[i].IsLongTranslatorToLeftOrRight;
                    run.HasShortTranslator = pipeLineGroups[j].PL.PipeRuns[i].HasShortTranslator;
                    run.IsShortTranslatorToLeftOrRight = pipeLineGroups[j].PL.PipeRuns[i].IsShortTranslatorToLeftOrRight;
                }
                for (int i = 0; i < runs.Count; i++)
                {
                    var run = runs[i];
                    {
                        var s = GetScore(run.Storey);
                        var s1 = GetScore(maxStorey);
                        var s2 = GetScore(minStorey);
                        if (!(s1 >= s && s >= s2))
                        {
                            runs[i] = null;
                            continue;
                        }
                    }
                    var bi = run.BranchInfo = new BranchInfo();
                    if (run.Storey == maxStorey)
                    {
                        bi.BlueToLeftFirst = true;
                    }
                    else if (run.Storey == minStorey)
                    {
                        bi.BlueToLeftLast = true;
                    }
                    else
                    {
                        bi.BlueToLeftMiddle = true;
                    }
                }
            }
            var maxStoreyIndex = storeys.IndexOf(maxStorey);

            DrawVer1(new DrawingOption()
            {
                basePoint = basePoint,
                OFFSET_X = OFFSET_X,
                SPAN_X = SPAN_X,
                HEIGHT = HEIGHT,
                COUNT = COUNT,
                storeys = storeys,
                groups = pipeLineGroups,
                layer = "W-DRAI-DOME-PIPE",
                maxStoreyIndex = maxStoreyIndex,
            });

            if (pipeLineGroups2.Count > 0)
            {
                DrawVer1(new DrawingOption()
                {
                    basePoint = basePoint,
                    OFFSET_X = OFFSET_X,
                    SPAN_X = SPAN_X,
                    HEIGHT = HEIGHT,
                    COUNT = COUNT,
                    storeys = storeys,
                    groups = pipeLineGroups2,
                    layer = "W-DRAI-EQPM",
                    drawStoreyLine = false,
                    test = true,
                    maxStoreyIndex = maxStoreyIndex,
                });
            }
        }
        public static void draw8(List<DrainageDrawingData> drDatas, Point2d basePoint)
        {
            var q = drDatas.SelectMany(drData => drData.VerticalPipeLabels).Distinct();
            //q = q.Where(lb => lb.StartsWith("PL"));
            //q = q.Where(lb => lb.StartsWith("FL"));
            //q = q.Where(lb => lb.StartsWith("TL"));
            q = q.Where(lb => lb.StartsWith("PL") || lb.StartsWith("FL"));
            q = q.Where(lb => ThDrainageService.IsWantedLabelText(lb));
            var pipeLabels = q.OrderBy(x => x).ToList();
            var pipesCmpInfos = new List<PipeCmpInfo>();
            foreach (var label in pipeLabels)
            {
                var info = new PipeCmpInfo();
                pipesCmpInfos.Add(info);
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
            var list = pipesCmpInfos.GroupBy(x => x).ToList();
            //Dbg.Log(lst.GroupBy(x => x).Select(x => new { k = x.Key, lst = x.ToList() }));
            //return;

            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            var COUNT = list.Count;
            var STOREY_COUNT = drDatas.Count;
            var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();


            var pipeLineGroups = new List<ThwPipeLineGroup>();

            foreach (var g in list)
            {
                var info = g.Key;
                var label = g.Key.label;
                var group = new ThwPipeLineGroup();
                pipeLineGroups.Add(group);
                var ppl = group.PL = new ThwPipeLine();
                ppl.Label1 = label;
                ppl.Comments = g.Select(x => x.label).ToList();
                var runs = ppl.PipeRuns = new List<ThwPipeRun>();
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
                    runs.Add(run);
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
                        if (label.StartsWith("PL") || label.StartsWith("FL"))
                        {
                            b.MiddleLeftRun = true;
                        }
                        else if (label.StartsWith("TL"))
                        {
                            b.BlueToLeftMiddle = true;
                        }
                    }
                    {
                        if (storey == "1F")
                        {
                            var o = ppl.Output = new ThwOutput();
                            o.DN1 = "DN100";
                            o.HasWrappingPipe1 = g.Key.PipeRuns.FirstOrDefault().HasWrappingPipe;
                            var hs = new HashSet<string>();
                            foreach (var _label in g.Select(x => x.label))
                            {
                                if (drData.Outlets.TryGetValue(_label, out string well))
                                {
                                    hs.Add(well);
                                }
                            }
                            o.DirtyWaterWellValues = hs.OrderBy(x => { long.TryParse(x, out long v); return v; }).ToList();
                            ppl.Comments.Add("===");
                            ppl.Comments.Add("outlets:");
                            ppl.Comments.AddRange(o.DirtyWaterWellValues);
                        }

                    }

                }
            }

            var pipeLineGroups2 = new List<ThwPipeLineGroup>();
            for (int j = 0; j < pipeLineGroups.Count; j++)
            {
                var group = new ThwPipeLineGroup();
                pipeLineGroups2.Add(group);
                var ttl = group.TL = new ThwPipeLine();
                var runs = ttl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var storey = storeys[i];
                    var drData = drDatas[i];
                    var run = new ThwPipeRun();
                    runs.Add(run);
                    run.HasLongTranslator = pipeLineGroups[j].PL.PipeRuns[i].HasLongTranslator;
                    run.IsLongTranslatorToLeftOrRight = pipeLineGroups[j].PL.PipeRuns[i].IsLongTranslatorToLeftOrRight;
                    run.HasShortTranslator = pipeLineGroups[j].PL.PipeRuns[i].HasShortTranslator;
                    run.IsShortTranslatorToLeftOrRight = pipeLineGroups[j].PL.PipeRuns[i].IsShortTranslatorToLeftOrRight;
                }
                for (int i = 0; i < runs.Count; i++)
                {
                    var run = runs[i];
                    var bi = run.BranchInfo = new BranchInfo();
                    if (i == runs.Count - 1)
                    {
                        bi.BlueToLeftFirst = true;
                    }
                    else if (i == 0)
                    {
                        bi.BlueToLeftLast = true;
                    }
                    else
                    {
                        bi.BlueToLeftMiddle = true;
                    }
                }
            }


            DrawVer1(new DrawingOption()
            {
                basePoint = basePoint,
                OFFSET_X = OFFSET_X,
                SPAN_X = SPAN_X,
                HEIGHT = HEIGHT,
                COUNT = COUNT,
                storeys = storeys,
                groups = pipeLineGroups,
                layer = "W-DRAI-DOME-PIPE",
            });

            if (pipeLineGroups2.Count > 0)
            {
                DrawVer1(new DrawingOption()
                {
                    basePoint = basePoint,
                    OFFSET_X = OFFSET_X,
                    SPAN_X = SPAN_X,
                    HEIGHT = HEIGHT,
                    COUNT = COUNT,
                    storeys = storeys,
                    groups = pipeLineGroups2,
                    layer = "W-DRAI-EQPM",
                    drawStoreyLine = false,
                    test = true,
                });
            }
        }
    }
}