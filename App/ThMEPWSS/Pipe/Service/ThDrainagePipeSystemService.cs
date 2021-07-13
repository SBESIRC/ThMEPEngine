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
    using System.IO;
    using System.Windows.Forms;

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
        List<GRect> washingMachines => geoData.WashingMachines;
        public void CollectStoreys(Geometry range, ThMEPWSS.Pipe.Service.ThDrainageService.CommandContext ctx)
        {
            storeys.AddRange(ctx.StoreyContext.thStoreysDatas.Select(x => x.Boundary));
        }
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
                    if (ent is BlockReference br && br.ObjectId.IsValid)
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
                        else if (br.Layer == "块" && br.ObjectId.IsValid && br.GetEffectiveName() == "A$C028429B2")
                        {
                            foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                            {
                                yield return e;
                            }
                        }
                        else if (br.Layer == "块")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 30000 && r.Height > 15000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        else if (br.Layer == "C-SHET-SHET" && br.ObjectId.IsValid && br.GetEffectiveName() == "A$C066D2D80")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 30000 && r.Height > 8000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<BlockReference>())
                                {
                                    if (e.Layer == "块")
                                    {
                                        r = br.Bounds.ToGRect();
                                        if (r.Width > 30000 && r.Height > 8000)
                                        {
                                            foreach (var ee in e.ExplodeToDBObjectCollection().OfType<Entity>())
                                            {
                                                yield return ee;
                                            }
                                        }
                                    }
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
            {
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "0" && e.ObjectId.IsValid && e.GetEffectiveName() == "A$C17E546D9"))
                {
                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<BlockReference>())
                    {
                        if (e.Layer is "P-SEWR-SILO" or "W-RAIN-EQPM" or "W-DRAI-EQPM")
                        {
                            pipes.Add(e.Bounds.ToGRect().Center.ToGRect(55));
                        }
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
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-FRPT-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE" or "W-RAIN-DIMS";
            foreach (var e in entities.OfType<Line>().Where(e => f(e.Layer) && e.Length > 0))
            {
                labelLines.Add(e.ToGLineSegment());
            }
        }
        public void CollectDLines()
        {
            dlines.AddRange(GetLines(entities, layer => layer is "W-DRAI-DOME-PIPE"));
        }
        public void CollectVLines()
        {
            vlines.AddRange(GetLines(entities, layer => layer is "W-DRAI-VENT-PIPE"));
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
                else if (e is Polyline pl)
                {
                    foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                    {
                        if (ln.Length > 0)
                        {
                            yield return ln.ToGLineSegment();
                        }
                    }
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

        public void CollectWashingMachines()
        {
            var q = entities.OfType<BlockReference>().Where(e => e.ObjectId.IsValid && e.GetEffectiveName().Contains("A-Toilet-9"));
            washingMachines.AddRange(q.Select(x => x.Bounds.ToGRect()));
        }

        //int distinguishDiameter = 35;//在雨水系统图中用这个值
        int distinguishDiameter = 20;//在排水系统图中，居然还有半径25的圆的立管。。。不管冷凝管了
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
                pps.AddRange(entities.OfType<Entity>().Where(e => e.Layer is "W-DRAI-EQPM" or "W-RAIN-EQPM" or "WP_KTN_LG" && e.GetRXClass().DxfName.ToUpper() == "TCH_PIPE"));//bounds是一个点，炸开后才能获取正确的bounds
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer is "W-DRAI-EQPM" or "W-RAIN-EQPM" or "P-SEWR-SILO" && e.ObjectId.IsValid && e.GetEffectiveName().Contains("$LIGUAN")));
                //pps.AddRange(entities.OfType<Entity>().Where(e => e.Layer == "__附着_W20-8-提资文件_SEN23WUB_设计区$0$W-DRAI-EQPM" && e.GetRXClass().DxfName.ToUpper() == "TCH_PIPE"));
                //pps.AddRange(entities.OfType<Circle>().Where(e => e.Layer == "__附着_W20-8-提资文件_SEN23WUB_设计区$0$W-DRAI-EQPM"));
                static GRect getRealBoundaryForPipe(Entity c)
                {
                    var r = c.Bounds.ToGRect();
                    if (!r.IsValid) r = GRect.Create(r.Center, 55);
                    return r;
                }
                foreach (var pp in pps.Distinct())
                {
                    if (pp is BlockReference e && e.ObjectId.IsValid && e.GetEffectiveName().Contains("$LIGUAN"))
                    {
                        //旁边有一些网线，只算里面那个黄圈圈的大小
                        pipes.Add(pp.Bounds.ToGRect().Center.ToGRect(60));
                    }
                    else
                    {
                        pipes.Add(getRealBoundaryForPipe(pp));
                    }
                }
            }
        }

        public void CollectCTexts()
        {
            {
                foreach (var e in entities.OfType<Entity>().Where(e => e.Layer is "W-DRAI-DIMS" or "W-RAIN-DIMS" && e.GetRXClass().DxfName.ToUpper() == "TCH_VPIPEDIM"))
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
                static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE" or "W-RAIN-DIMS";
                foreach (var e in entities.OfType<DBText>().Where(e => f(e.Layer)).Where(x => !x.TextString.StartsWith("De")))
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
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
        }
        public static string GetLabelScore(string label)
        {
            if (label == null) return null;
            if (label.StartsWith("PL"))
            {
                return 'A' + label;
            }
            if (label.StartsWith("FL"))
            {
                return 'B' + label;
            }
            return label;
        }
        public static int GetStoreyScore(string label)
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
                List<StoreysItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                CollectDrainageData(range, adb, out storeysItems, out drDatas);
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

        private static bool CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = false)
        {
            NewMethod6(range, adb, out storeysItems, out DrainageGeoData geoData);
            return NewMethod7(adb, ref storeysItems, out drDatas, noWL, geoData);
        }
        private static bool CollectDrainageData(Geometry range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, ThMEPWSS.Pipe.Service.ThDrainageService.CommandContext ctx, bool noWL = false)
        {
            NewMethod6(range, adb, out storeysItems, out DrainageGeoData geoData, ctx);
            return NewMethod7(adb, ref storeysItems, out drDatas, noWL, geoData);
        }
        private static bool NewMethod7(AcadDatabase adb, ref List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            //考虑到污废合流的场景占比在全国范围项目的比例较高，因此优先支持污废合流。判断方式：
            //通过框选范围内的立管判断。若存在污水立管（WL开头），则程序中断，并提醒暂不支持污废分流。
            if (noWL && geoData.Labels.Any(x => x.Text.StartsWith("WL")))
            {
                MessageBox.Show("暂不支持污废分流");
                storeysItems = null;
                drDatas = null;
                return false;
            }
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            var roomData = DrainageService.CollectRoomData(adb);
            DrainageService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out StringBuilder sb, out drDatas, roomData: roomData);
            DU.Dispose();
            return true;
        }
        private static void NewMethod6(Geometry range, AcadDatabase adb, out List<StoreysItem> storeysItems, out DrainageGeoData geoData, ThMEPWSS.Pipe.Service.ThDrainageService.CommandContext ctx)
        {
            var storeys = ThRainSystemService.GetStoreys(range, adb, ctx);
            ThRainSystemService.FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(range, adb, geoData, ctx);
        }
        private static void NewMethod6(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out DrainageGeoData geoData)
        {
            var storeys = ThRainSystemService.GetStoreys(range, adb);
            ThRainSystemService.FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(range, adb, geoData);
        }

        static bool NoDraw;
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

        static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
        }
        public class RefList<T> : IEnumerable<T>
        {
            public class Ref<T> : IComparable<Ref<T>>, IEquatable<Ref<T>>
            {
                public T Value;
                public readonly int Id;
                public Ref(T value, int id)
                {
                    Value = value;
                    Id = id;
                }

                public int CompareTo(Ref<T> other)
                {
                    return this.Id - other.Id;
                }
                public override int GetHashCode()
                {
                    return Id;
                }
                public bool Equals(Ref<T> other)
                {
                    return this.Id == other.Id;
                }
            }
            List<Ref<T>> list;
            private RefList() { }
            public static RefList<T> Create(IEnumerable<T> source)
            {
                int tk = 0;
                var lst = new RefList<T>();
                lst.list = new List<Ref<T>>((source as System.Collections.IList)?.Count ?? 32);
                foreach (var item in source)
                {
                    lst.list.Add(new Ref<T>(item, ++tk));
                }
                return lst;
            }
            public T this[int index] { get => list[index].Value; }
            public Ref<T> GetAt(int index)
            {
                return list[index];
            }
            public int Count => list.Count;
            public List<T> ToList()
            {
                return list.Select(r => r.Value).ToList();
            }
            public IEnumerable<V> Select<V>(IList<V> source, IEnumerable<Ref<T>> refs)
            {
                foreach (var rf in refs)
                {
                    yield return source[IndexOf(rf)];
                }
            }
            public IEnumerable<Ref<T>> Where(Func<T, bool> f)
            {
                return list.Where(x => f(x.Value));
            }

            public bool Contains(Ref<T> item)
            {
                return IndexOf(item) >= 0;
            }
            public IEnumerator<T> GetEnumerator()
            {
                return _GetEnumerator();
            }

            private IEnumerator<T> _GetEnumerator()
            {
                return list.Select(x => x.Value).GetEnumerator();
            }

            public int IndexOf(Ref<T> item)
            {
                return list.BinarySearch(item);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _GetEnumerator();
            }
        }
        public static void draw11()
        {
            List<DrainageDrawingData> drDatas;
            List<StoreysItem> storeysItems;
            loadTestData(out drDatas, out storeysItems);
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d point3D)) return;
            var pt = point3D.ToPoint2d();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                if (NoDraw)
                {
                    GetDrainageGroupedPipeItems(drDatas, storeysItems, out _, out _);
                }
                else
                {
                    Draw(drDatas, pt, storeysItems);
                }
            }
        }

        public static void loadTestData(out List<DrainageDrawingData> drDatas, out List<StoreysItem> storeysItems)
        {
            var lines = File.ReadAllLines(@"Y:\xx.txt");
            drDatas = Dbg.LoadFromJsonFile<List<DrainageDrawingData>>($@"Y:\{lines[0]}.json");
            storeysItems = Dbg.LoadFromJsonFile<List<StoreysItem>>($@"Y:\{lines[1]}.json");
        }

        public static List<DrainageGroupedPipeItem> GetDrainageGroupedPipeItems(List<DrainageDrawingData> drDatas, List<StoreysItem> storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys)
        {
            //Draw(drDatas, pt, storeysItems);
            //👻开始写分组逻辑

            //Console.WriteLine(storeysItems.ToCadJson());
            var allFls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => x.StartsWith("FL")));
            var allTls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => x.StartsWith("TL")));
            var allPls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => x.StartsWith("PL")));

            var storeys = new List<string>();
            foreach (var item in storeysItems)
            {
                item.Init();
                //storeys.AddRange(item.Labels.Where(isWantedStorey));
                storeys.AddRange(item.Labels);
            }
            storeys = storeys.Distinct().OrderBy(GetStoreyScore).ToList();
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
            //Console.WriteLine(flstoreylist.ToCadJson());
            var pls = new HashSet<string>();
            var tls = new HashSet<string>();
            var pltlList = new List<PlTlStoreyItem>();
            var fltlList = new List<FlTlStoreyItem>();
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
                    else if (gp.WLType == ThDrainageService.WLType.FL_TL && gp.FLs.Count == 1 && gp.TLs.Count == 1)
                    {
                        var fl = gp.FLs[0];
                        var tl = gp.TLs[0];
                        foreach (var s in item.Labels.Where(x => storeys.Contains(x)))
                        {
                            var x = new FlTlStoreyItem() { fl = fl, tl = tl, storey = s };
                            tls.Add(tl);
                            fltlList.Add(x);
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
                            plstoreylist.Add(new KeyValuePair<string, string>(pl, s));
                        }
                    }
                }
            }
            //Console.WriteLine(plstoreylist.ToCadJson());
            //Console.WriteLine(flstoreylist.ToCadJson());
            var tlMinMaxStoreyScoreDict = new Dictionary<string, KeyValuePair<int, int>>();
            foreach (var tl in tls)
            {
                int min = int.MaxValue;
                int max = int.MinValue;
                foreach (var item in pltlList)
                {
                    if (item.tl == tl)
                    {
                        var s = GetStoreyScore(item.storey);
                        if (s < min) min = s;
                        if (s > max) max = s;
                        tlMinMaxStoreyScoreDict[tl] = new KeyValuePair<int, int>(min, max);
                    }
                }
            }
            //Console.WriteLine(tlMinMaxStoreyScoreDict.ToCadJson());
            //Console.WriteLine(pltlList.OrderBy(x => GetScore(x.storey)).ThenBy(x => x.pl).ToCadJson());
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
                            plShortTransList.Add(new KeyValuePair<string, string>(pl, s));
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
                            flShortTransList.Add(new KeyValuePair<string, string>(fl, s));
                        }
                    }
                }
            }
            var minS = storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Min();
            var maxS = storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Max();
            var countS = maxS - minS + 1;
            {
                allNumStoreys = new List<int>();
                for (int storey = minS; storey <= maxS; storey++)
                {
                    allNumStoreys.Add(storey);
                }
                allRfStoreys = storeys.Where(x => !IsNumStorey(x)).ToList();
                var allNumStoreyLabels = allNumStoreys.Select(x => x + "F").ToList();
                bool testExist(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.VerticalPipeLabels.Contains(label);
                            }
                        }
                    }
                    return false;
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
                string getWaterPortLabel(string label)
                {
                    foreach (var drData in drDatas)
                    {
                        if (drData.Outlets.TryGetValue(label, out string value))
                        {
                            return value;
                        }
                    }
                    return null;
                }
                bool hasWaterPort(string label)
                {
                    return getWaterPortLabel(label) != null;
                }
                int getFDCount(string label, string storey)
                {
                    int _getFDCount()
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == storey)
                                {
                                    var drData = drDatas[i];
                                    drData.FloorDrains.TryGetValue(label, out int v);
                                    return v;
                                }
                            }
                        }
                        return 0;
                    }
                    var ret = _getFDCount();
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == storey)
                                {
                                    var drData = drDatas[i];
                                    //必然负担一个地漏下水点。
                                    if (drData.MustHaveFloorDrains.Contains(label))
                                    {
                                        if (ret == 0) ret = 1;
                                    }
                                    //最多同时负担一个洗涤盆下水店和一个地漏下水点。
                                    if (drData.KitchenOnlyFls.Contains(label))
                                    {
                                        if (ret > 1) ret = 1;
                                    }
                                }
                            }
                        }
                    }
                    return ret;
                }
                bool hasSCurve(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                //1)	必然负担一个洗涤盆下水点。不用读图上任何信息；
                                if (drData.KitchenOnlyFls.Contains(label) || drData.KitchenAndBalconyFLs.Contains(label))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                bool IsWaterPipeWellFL(string label)
                {
                    if (label.StartsWith("FL"))
                    {
                        foreach (var drData in drDatas)
                        {
                            if (drData.WaterPipeWellFLs.Contains(label)) return true;
                        }
                    }
                    return false;
                }
                bool hasCleaningPort(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                //若FL附近300的范围内存在TL且该TL不属于PL，则将FL与TL设为一组。系统图上和PL+TL的唯一区别是废水管要表达卫生洁具。
                                if (drData.MustHaveCleaningPort.Contains(label))
                                {
                                    return true;
                                }

                            }
                        }
                    }
                    return false;
                }
                bool hasWrappingPipe(string label)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == "1F")
                            {
                                var drData = drDatas[i];
                                return drData.WrappingPipes.Contains(label);
                            }
                        }
                    }
                    return false;
                }
                var pipeInfoDict = new Dictionary<string, DrainageGroupingPipeItem>();
                var flGroupingItems = new List<DrainageGroupingPipeItem>();
                var plGroupingItems = new List<DrainageGroupingPipeItem>();
                foreach (var fl in allFls)
                {
                    var item = new DrainageGroupingPipeItem();
                    item.Label = fl;
                    item.HasWaterPort = hasWaterPort(fl);
                    item.WaterPortLabel = getWaterPortLabel(fl);
                    item.HasWrappingPipe = hasWrappingPipe(fl);
                    item.Items = new List<DrainageGroupingPipeItem.ValueItem>();
                    item.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
                    foreach (var storey in allNumStoreyLabels)
                    {
                        var _hasLong = hasLong(fl, storey, PipeType.FL);
                        item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                        {
                            Exist = testExist(fl, storey),
                            HasLong = _hasLong,
                            HasShort = hasShort(fl, storey, PipeType.FL),
                        });
                        item.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
                        {
                            FloorDrainsCount = getFDCount(fl, storey),
                            HasCleaningPort = hasCleaningPort(fl, storey) || _hasLong,
                            HasSCurve = hasSCurve(fl, storey),
                        });
                    }
                    if (IsWaterPipeWellFL(fl))
                    {
                        //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = 1;
                        }
                    }
                    flGroupingItems.Add(item);
                    pipeInfoDict[fl] = item;
                }
                foreach (var pl in allPls)
                {
                    var item = new DrainageGroupingPipeItem();
                    item.Label = pl;
                    item.HasWaterPort = hasWaterPort(pl);
                    item.WaterPortLabel = getWaterPortLabel(pl);
                    item.HasWrappingPipe = hasWrappingPipe(pl);
                    item.Items = new List<DrainageGroupingPipeItem.ValueItem>();
                    item.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
                    foreach (var storey in allNumStoreyLabels)
                    {
                        var _hasLong = hasLong(pl, storey, PipeType.PL);
                        item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                        {
                            Exist = testExist(pl, storey),
                            HasLong = _hasLong,
                            HasShort = hasShort(pl, storey, PipeType.PL),
                        });
                        item.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
                        {
                            FloorDrainsCount = getFDCount(pl, storey),
                            HasCleaningPort = _hasLong,
                        });
                    }
                    foreach (var x in pltlList)
                    {
                        if (x.pl == pl)
                        {
                            var tl = x.tl;
                            item.HasTL = true;
                            var kv = tlMinMaxStoreyScoreDict[tl];
                            item.MinTl = kv.Key;
                            item.MaxTl = kv.Value;
                            item.TlLabel = tl;
                        }
                    }
                    plGroupingItems.Add(item);
                    pipeInfoDict[pl] = item;
                }
                var pipeGroupItems = new List<DrainageGroupedPipeItem>();
                foreach (var g in flGroupingItems.GroupBy(x => x))
                {
                    var outlets = g.Where(x => x.HasWaterPort).Select(x => x.WaterPortLabel).Distinct().ToList();
                    var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                    var item = new DrainageGroupedPipeItem()
                    {
                        Labels = labels,
                        HasWrappingPipe = g.Key.HasWrappingPipe,
                        WaterPortLabels = outlets,
                        Items = g.Key.Items.ToList(),
                        PipeType = PipeType.FL,
                        Hangings = g.Key.Hangings.ToList(),
                    };
                    pipeGroupItems.Add(item);
                }
                foreach (var g in plGroupingItems.GroupBy(x => x))
                {
                    var outlets = g.Where(x => x.HasWaterPort).Select(x => x.WaterPortLabel).Distinct().ToList();
                    var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                    var item = new DrainageGroupedPipeItem()
                    {
                        Labels = labels,
                        HasWrappingPipe = g.Key.HasWrappingPipe,
                        WaterPortLabels = outlets,
                        Items = g.Key.Items.ToList(),
                        PipeType = PipeType.PL,
                        MinTl = g.Key.MinTl,
                        MaxTl = g.Key.MaxTl,
                        TlLabels = g.Select(x => x.TlLabel).Where(x => x != null).ToList(),
                        Hangings = g.Key.Hangings.ToList(),
                    };
                    pipeGroupItems.Add(item);
                }
                pipeGroupItems = pipeGroupItems.OrderBy(x => GetLabelScore(x.Labels.FirstOrDefault())).ToList();
                Console.WriteLine(pipeGroupItems.ToCadJson());
                return pipeGroupItems;
            }
        }

        public class DrainageGroupedPipeItem
        {
            public List<string> Labels;
            public List<string> WaterPortLabels;
            public bool HasWrappingPipe;
            public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > 0;
            public List<DrainageGroupingPipeItem.ValueItem> Items;
            public List<string> TlLabels;
            public int MinTl;
            public int MaxTl;
            public bool HasTl => TlLabels != null && TlLabels.Count > 0;
            public PipeType PipeType;
            public List<DrainageGroupingPipeItem.Hanging> Hangings;
        }
        public class DrainageGroupingPipeItem : IEquatable<DrainageGroupingPipeItem>
        {
            public string Label;
            public bool HasWaterPort;
            public bool HasWrappingPipe;
            public string WaterPortLabel;
            public List<ValueItem> Items;
            public List<Hanging> Hangings;
            public bool HasTL;
            public string TlLabel;
            public int MaxTl;
            public int MinTl;

            public class Hanging : IEquatable<Hanging>
            {
                public int FloorDrainsCount;
                public bool IsSeries;
                public bool HasSCurve;
                public bool HasDoubleSCurve;
                public bool HasCleaningPort;
                public override int GetHashCode()
                {
                    return 0;
                }
                public bool Equals(Hanging other)
                {
                    return this.FloorDrainsCount == other.FloorDrainsCount
                        && this.IsSeries == other.IsSeries
                        && this.HasSCurve == other.HasSCurve
                        && this.HasDoubleSCurve == other.HasDoubleSCurve
                        && this.HasCleaningPort == other.HasCleaningPort
                        ;
                }
            }
            public struct ValueItem
            {
                public bool Exist;
                public bool HasLong;
                public bool HasShort;
            }
            public override int GetHashCode()
            {
                return 0;
            }
            public bool Equals(DrainageGroupingPipeItem other)
            {
                return this.HasWaterPort == other.HasWaterPort
                    && this.HasWrappingPipe == other.HasWrappingPipe
                    && this.MaxTl == other.MaxTl
                    && this.MinTl == other.MinTl
                    && this.Items.SeqEqual(other.Items)
                    && this.Hangings.SeqEqual(other.Hangings);
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
        public class FlTlStoreyItem
        {
            public string fl;
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
                        if (!IsNumStorey(storey)) continue;
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


            var maxStorey = storeys.Where(IsNumStorey).FindByMax(GetStoreyScore);
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
                        var s = GetStoreyScore(run.Storey);
                        var s1 = GetStoreyScore(maxStorey);
                        var s2 = GetStoreyScore(minStorey);
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