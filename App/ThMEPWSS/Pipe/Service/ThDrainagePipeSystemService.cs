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
        public void CollectLabelLines()
        {
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE";
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
        public void CollectVerticalPipes()
        {
            static bool f(string layer) => layer == "W-DRAI-EQPM";

            {
                var pps = new List<Entity>();
                pps.AddRange(entities.OfType<BlockReference>()
                .Where(e => f(e.Layer))
                .Where(e => e.ObjectId.IsValid && e.GetEffectiveName() == "立管编号"));
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
        }

        public void CollectCTexts()
        {
            static bool f(string layer) => layer == "W-DRAI-EQPM";
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
}