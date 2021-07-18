namespace ThMEPWSS.DebugNs.RainSystemNs
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
    using static THRainService;
    public class RainGeoData
    {
        public List<GRect> Storeys;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public List<GLineSegment> WLines;
        public List<GLineSegment> DLines;
        public List<GLineSegment> VLines;
        public List<GRect> VerticalPipes;
        public List<GRect> WrappingPipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterPorts;
        public List<string> WaterPortLabels;
        public List<GRect> CondensePipes;
        public List<GRect> WaterWells;
        public List<string> WaterWellLabels;
        public List<GRect> SideWaterBuckets;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> _87WaterBuckets;
        public List<GRect> WaterPortSymbols;
        public List<GRect> WaterPort13s;
        public List<Point2d> CleaningPorts;
        public List<Point2d> SideFloorDrains;
        public List<GRect> PipeKillers;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            VerticalPipes ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            CondensePipes ??= new List<GRect>();
            WaterWells ??= new List<GRect>();
            WaterWellLabels ??= new List<string>();
            CleaningPorts ??= new List<Point2d>();
            SideFloorDrains ??= new List<Point2d>();
            PipeKillers ??= new List<GRect>();
            WaterPortSymbols ??= new List<GRect>();
            WaterPort13s ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            GravityWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
        }
        public void FixData()
        {
            Init();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.Length > 0).Distinct().ToList();
            DLines = DLines.Where(x => x.Length > 0).Distinct().ToList();
            VLines = VLines.Where(x => x.Length > 0).Distinct().ToList();
            WLines = WLines.Where(x => x.Length > 0).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterPorts = WaterPorts.Where(x => x.IsValid).Distinct().ToList();
            CondensePipes = CondensePipes.Where(x => x.IsValid).Distinct().ToList();
            WaterWells = WaterWells.Where(x => x.IsValid).Distinct().ToList();
            SideWaterBuckets = SideWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            GravityWaterBuckets = GravityWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            _87WaterBuckets = _87WaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            WaterPortSymbols = WaterPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            PipeKillers = PipeKillers.Where(x => x.IsValid).Distinct().ToList();
            WaterPort13s = WaterPort13s.Where(x => x.IsValid).Distinct().ToList();
        }
        public RainGeoData Clone()
        {
            return (RainGeoData)MemberwiseClone();
        }
        public RainGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<RainGeoData>();
        }
    }
    public class RainCadData
    {
        public List<Geometry> Storeys;
        public List<Geometry> Labels;
        public List<Geometry> LabelLines;
        public List<Geometry> DLines;
        public List<Geometry> VLines;
        public List<Geometry> WLines;
        public List<Geometry> VerticalPipes;
        public List<Geometry> WrappingPipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterPorts;
        public List<Geometry> CondensePipes;
        public List<Geometry> CleaningPorts;
        public List<Geometry> SideFloorDrains;
        public List<Geometry> PipeKillers;
        public List<Geometry> WaterWells;
        public List<Geometry> WaterPortSymbols;
        public List<Geometry> WaterPort13s;
        public List<Geometry> SideWaterBuckets;
        public List<Geometry> GravityWaterBuckets;
        public List<Geometry> _87WaterBuckets;
        public void Init()
        {
            Storeys ??= new List<Geometry>();
            Labels ??= new List<Geometry>();
            LabelLines ??= new List<Geometry>();
            DLines ??= new List<Geometry>();
            VLines ??= new List<Geometry>();
            WLines ??= new List<Geometry>();
            VerticalPipes ??= new List<Geometry>();
            WrappingPipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterPorts ??= new List<Geometry>();
            CondensePipes ??= new List<Geometry>();
            CleaningPorts ??= new List<Geometry>();
            SideFloorDrains ??= new List<Geometry>();
            PipeKillers ??= new List<Geometry>();
            WaterWells ??= new List<Geometry>();
            WaterPortSymbols ??= new List<Geometry>();
            WaterPort13s ??= new List<Geometry>();
            SideWaterBuckets ??= new List<Geometry>();
            GravityWaterBuckets ??= new List<Geometry>();
            _87WaterBuckets ??= new List<Geometry>();
        }
        public static RainCadData Create(RainGeoData data)
        {
            var bfSize = 10;
            var o = new RainCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));

            if (Dbg._) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WLines.AddRange(data.WLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (Dbg._) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.CondensePipes.AddRange(data.CondensePipes.Select(ConvertWashingMachinesF()));
            o.WaterWells.AddRange(data.WaterWells.Select(ConvertWashingMachinesF()));
            o.WaterPortSymbols.AddRange(data.WaterPortSymbols.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            o.PipeKillers.AddRange(data.PipeKillers.Select(ConvertVerticalPipesF()));
            o.WaterPort13s.AddRange(data.WaterPort13s.Select(ConvertVerticalPipesF()));
            o.SideWaterBuckets.AddRange(data.SideWaterBuckets.Select(ConvertVerticalPipesF()));
            o.GravityWaterBuckets.AddRange(data.GravityWaterBuckets.Select(ConvertVerticalPipesF()));
            o._87WaterBuckets.AddRange(data._87WaterBuckets.Select(ConvertVerticalPipesF()));
            return o;
        }

        private static Func<GRect, Polygon> ConvertWrappingPipesF()
        {
            return x => x.ToPolygon();
        }

        private static Func<GLineSegment, Geometry> NewMethod(int bfSize)
        {
            return x => x.Buffer(bfSize);
        }
        public static Func<Point2d, Point> ConvertSideFloorDrains()
        {
            return x => x.ToNTSPoint();
        }
        public static Func<Point2d, Polygon> ConvertCleaningPortsF()
        {
            return x => new GCircle(x, 40).ToCirclePolygon(36);
        }

        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }

        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(1500).ToCirclePolygon(6);
        }

        private static Func<GRect, Polygon> ConvertWaterPortsF()
        {
            return x => x.ToPolygon();
        }

        public static Func<GRect, Polygon> ConvertFloorDrainsF()
        {
            return x => x.ToPolygon();
        }

        public static Func<GRect, Polygon> ConvertVerticalPipesF()
        {
            return x => x.ToPolygon();
        }

        private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
        {
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(36);
        }

        public static Func<GLineSegment, LineString> ConvertVLinesF()
        {
            return x => x.ToLineString();
        }

        public static Func<GLineSegment, LineString> ConvertDLinesF()
        {
            return x => x.ToLineString();
        }

        public static Func<GLineSegment, LineString> ConvertLabelLinesF()
        {
            return x => x.Extend(.1).ToLineString();
        }

        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(4096);
            ret.AddRange(Storeys);
            ret.AddRange(Labels);
            ret.AddRange(LabelLines);
            ret.AddRange(DLines);
            ret.AddRange(VLines);
            ret.AddRange(WLines);
            ret.AddRange(VerticalPipes);
            ret.AddRange(WrappingPipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterPorts);
            ret.AddRange(CondensePipes);
            ret.AddRange(CleaningPorts);
            ret.AddRange(SideFloorDrains);
            ret.AddRange(PipeKillers);
            ret.AddRange(WaterWells);
            ret.AddRange(WaterPortSymbols);
            ret.AddRange(WaterPort13s);
            ret.AddRange(SideWaterBuckets);
            ret.AddRange(GravityWaterBuckets);
            ret.AddRange(_87WaterBuckets);
            return ret;
        }
        public List<RainCadData> SplitByStorey()
        {
            var lst = new List<RainCadData>(this.Storeys.Count);
            if (this.Storeys.Count == 0) return lst;
            var f = GeoFac.CreateIntersectsSelector(GetAllEntities());
            foreach (var storey in this.Storeys)
            {
                var objs = f(storey);
                var o = new RainCadData();
                o.Init();
                o.Labels.AddRange(objs.Where(x => this.Labels.Contains(x)));
                o.LabelLines.AddRange(objs.Where(x => this.LabelLines.Contains(x)));
                o.DLines.AddRange(objs.Where(x => this.DLines.Contains(x)));
                o.VLines.AddRange(objs.Where(x => this.VLines.Contains(x)));
                o.WLines.AddRange(objs.Where(x => this.WLines.Contains(x)));
                o.VerticalPipes.AddRange(objs.Where(x => this.VerticalPipes.Contains(x)));
                o.WrappingPipes.AddRange(objs.Where(x => this.WrappingPipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterPorts.AddRange(objs.Where(x => this.WaterPorts.Contains(x)));
                o.CondensePipes.AddRange(objs.Where(x => this.CondensePipes.Contains(x)));
                o.CleaningPorts.AddRange(objs.Where(x => this.CleaningPorts.Contains(x)));
                o.SideFloorDrains.AddRange(objs.Where(x => this.SideFloorDrains.Contains(x)));
                o.PipeKillers.AddRange(objs.Where(x => this.PipeKillers.Contains(x)));
                o.WaterWells.AddRange(objs.Where(x => this.WaterWells.Contains(x)));
                o.WaterPortSymbols.AddRange(objs.Where(x => this.WaterPortSymbols.Contains(x)));
                o.WaterPort13s.AddRange(objs.Where(x => this.WaterPort13s.Contains(x)));
                o.SideWaterBuckets.AddRange(objs.Where(x => this.SideWaterBuckets.Contains(x)));
                o.GravityWaterBuckets.AddRange(objs.Where(x => this.GravityWaterBuckets.Contains(x)));
                o._87WaterBuckets.AddRange(objs.Where(x => this._87WaterBuckets.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public RainCadData Clone()
        {
            return (RainCadData)MemberwiseClone();
        }
    }
    public static class THRainService
    {
        public static bool IsToilet(string roomName)
        {
            //1)	包含“卫生间” //2)	包含“主卫” //3)	包含“次卫”
            //4)	包含“客卫” //5)	单字“卫” //6)	包含“洗手间” //7)	卫 + 阿拉伯数字
            var roomNameContains = new List<string>
            {
                "卫生间","主卫","公卫",
                "次卫","客卫","洗手间",
            };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            if (roomName.Equals("卫"))
                return true;
            return Regex.IsMatch(roomName, @"^[卫]\d$");
        }
        public static bool IsKitchen(string roomName)
        {
            //1)	包含“厨房” //2)	单字“厨” //3)	包含“西厨”
            var roomNameContains = new List<string> { "厨房", "西厨" };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            if (roomName.Equals("厨"))
                return true;
            return false;
        }
        public static bool IsBalcony(string roomName)
        {
            //包含阳台
            var roomNameContains = new List<string> { "阳台" };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            return false;
        }
        public static bool IsCorridor(string roomName)
        {
            var roomNameContains = new List<string> { "连廊" };
            if (string.IsNullOrEmpty(roomName))
                return false;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return true;
            return false;
        }
        public static bool IsY1L(string label)
        {
            return label.StartsWith("Y1L");
        }
        public static bool IsY2L(string label)
        {
            return label.StartsWith("Y2L");
        }
        public static bool IsNL(string label)
        {
            return label.StartsWith("NL");
        }
        public static bool IsYL(string label)
        {
            return label.StartsWith("YL");
        }
        public static bool IsWL(string label)
        {
            return Regex.IsMatch(label, @"^W\d?L");
        }
        public static bool IsFL(string label)
        {
            return Regex.IsMatch(label, @"^F\d?L");
        }
        public static bool IsFL0(string label)
        {
            return IsFL(label) && label.Contains("-0");//可能是-0a，-0b。。
        }
        public static bool IsPL(string label)
        {
            return Regex.IsMatch(label, @"^P\d?L");
        }
        public static bool IsTL(string label)
        {
            return Regex.IsMatch(label, @"^T\d?L");
        }
        public static bool IsDL(string label)
        {
            return Regex.IsMatch(label, @"^D\d?L");
        }
    }

    public class ThRainSystemServiceGeoCollector
    {
        public AcadDatabase adb;
        public RainGeoData geoData;
        public List<Entity> entities;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<GLineSegment> wLines => geoData.WLines;
        List<CText> cts => geoData.Labels;
        List<GRect> waterWells => geoData.WaterWells;
        List<string> waterWellLabels => geoData.WaterWellLabels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GLineSegment> vlines => geoData.VLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> condensePipes => geoData.CondensePipes;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPort13s => geoData.WaterPort13s;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        List<GRect> washingMachines => geoData.CondensePipes;
        List<GRect> mopPools => geoData.WaterPortSymbols;
        List<GRect> basins => geoData.WaterWells;
        List<GRect> pipeKillers => geoData.PipeKillers;
        List<GRect> waterPortSymbols => geoData.WaterPortSymbols;
        public void CollectStoreys(Geometry range, ThRainService.CommandContext ctx)
        {
            storeys.AddRange(ctx.StoreyContext.thStoreysDatas.Select(x => x.Boundary));
        }
        public void CollectKillers()
        {
            //Dbg.DoExtract(adb, (br, m) =>
            //{

            //});

            if (Dbg._)
            {
                var visitor = new BlockReferenceVisitor();
                visitor.IsTargetBlockReferenceCb = (br) =>
                {
                    var name = br.GetEffectiveName();
                    if (name.Contains("A-Toilet-") || name.Contains("A-Kitchen-"))
                    {
                        return true;
                    }
                    return false;
                };
                visitor.HandleBlockReferenceCb = (br, m) =>
                {
                    var name = br.GetEffectiveName();
                    var e = br.GetTransformedCopy(m);
                    var r = e.Bounds.ToGRect();
                    if (!r.IsValid) return;
                    pipeKillers.Add(r);
                    if (name.Contains("A-Toilet-9"))
                    {
                        washingMachines.Add(r);
                    }
                };
                var extractor = new ThDistributionElementExtractor();
                extractor.Accept(visitor);
                extractor.Extract(adb.Database);
            }
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
                        else if (
                             (br.Layer == "块" && br.ObjectId.IsValid && br.GetEffectiveName() == "A$C028429B2")
                             || (br.Layer == "W-DRAI-NOTE" && br.ObjectId.IsValid && br.GetEffectiveName() == "wwwe")
                              || (br.Layer == "0" && br.ObjectId.IsValid && br.GetEffectiveName() == "dsa"
                              || (br.Layer is "W-DRAI-DIMS" or "A-排水立管图块" && br.ObjectId.IsValid && br.GetEffectiveName() is "sadf32f43tsag"))
                             )
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
        public void CollectWLines()
        {
            wLines.AddRange(GetLines(entities, layer => layer is "W-RAIN-PIPE"));
        }
        public void CollectLabelLines()
        {
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-FRPT-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE" or "W-RAIN-DIMS" or "W-WSUP-DIMS" or "W-WSUP-NOTE";
            foreach (var e in entities.OfType<Line>().Where(e => f(e.Layer) && e.Length > 0))
            {
                labelLines.Add(e.ToGLineSegment());
            }
        }
        public void CollectDLines()
        {
            dlines.AddRange(GetLines(entities, layer => layer is "W-DRAI-DOME-PIPE" or "W-DRAI-OUT-PIPE"));
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

        int distinguishDiameter = 35;//在雨水系统图中用这个值
        //int distinguishDiameter = 20;//在排水系统图中，居然还有半径25的圆的立管。。。不管冷凝管了
        public void CollectWrappingPipes()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
            wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
        }
        public void CollectVerticalPipes()
        {
            {
                var pps = new List<Entity>();
                pps.AddRange(entities.OfType<BlockReference>()
                .Where(x => x.Layer == "W-RAIN-EQPM")
                .Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName() == "带定位立管"));
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
                .Where(x => x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM" || x.Layer == "W-RAIN-DIMS")
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
                pps.AddRange(entities
                .Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")
                && ThRainSystemService.IsTianZhengElement(x))
                .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Any())
                );
                static GRect getRealBoundaryForPipe(Entity ent)
                {
                    return ent.Bounds.ToGRect(50);
                }
                foreach (var pp in pps.Distinct())
                {
                    pipes.Add(getRealBoundaryForPipe(pp));
                }
            }
            {
                Util1.CollectTianzhengVerticalPipes(labelLines, cts, entities);
            }
            {
                var pps = new List<Entity>();
                pps.AddRange(entities.OfType<BlockReference>()
                .Where(x => x.ObjectId.IsValid ? x.Layer == "W-RAIN-EQPM" && x.GetBlockEffectiveName() == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
                );
                pps.AddRange(entities.OfType<BlockReference>()
                .Where(e =>
                {
                    return e.ObjectId.IsValid && (e.Layer == "W-RAIN-PIPE-RISR" || e.Layer == "W-DRAI-NOTE")
&& !e.GetBlockEffectiveName().Contains("井");
                }));
                foreach (var pp in pps)
                {
                    pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                }
            }
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-RAIN-EQPM" or "VPIPE-污水" or "W-DRAI-DIMS";
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
                .Where(x => f(x.Layer) || x.Layer is "VPIPE-给水")
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
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer is "W-DRAI-PIEP-RISR" or "W-DRAI-EQPM" or "W-DRAI-DOME-PIPE" && e.ObjectId.IsValid && e.GetEffectiveName() == "A$C58B12E6E"));
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer == "PIPE-喷淋" && e.ObjectId.IsValid && e.GetEffectiveName() == "A$C5E4A3C21"));
                pps.AddRange(entities.OfType<Entity>().Where(e => e.Layer is "W-DRAI-EQPM" or "W-RAIN-EQPM" or "WP_KTN_LG" && e.GetRXClass().DxfName.ToUpper() == "TCH_PIPE"));//bounds是一个点，炸开后才能获取正确的bounds
                pps.AddRange(entities.OfType<BlockReference>().Where(e => e.Layer is "W-DRAI-EQPM" or "W-RAIN-EQPM" or "P-SEWR-SILO" && e.ObjectId.IsValid && e.GetEffectiveName().Contains("$LIGUAN")));
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
        public void CollectCondensePipes()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<Circle>()
            .Where(c => c.Layer is "W-RAIN-EQPM")
            .Where(c => 20 < c.Radius && c.Radius < distinguishDiameter));
            condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
        }
        public void CollectCTexts()
        {
            {
                foreach (var e in entities.OfType<Entity>().Where(e => e.Layer is "W-DRAI-DIMS" or "W-RAIN-DIMS" or "W-RAIN-NOTE" or "W-FRPT-NOTE" && e.GetRXClass().DxfName.ToUpper() == "TCH_VPIPEDIM"))
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
                static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE" or "W-RAIN-DIMS" or "W-WSUP-DIMS" or "W-WSUP-NOTE" or "W-FRPT-NOTE";
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
        public void CollectWaterWells()
        {
            var ents = new List<BlockReference>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")).Distinct());
            waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
            waterWellLabels.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
        }
        public void CollectWaterPortSymbols()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
            ents.AddRange(entities.Where(e => FengDbgTesting.IsTianZhengWaterPort(e)));
            waterPortSymbols.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
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
        public void CollectWaterPort13s()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.GetBlockEffectiveName().Contains("雨水口")));
            waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
        }
    }
    public partial class RainDiagram
    {
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
            if (IsPL(label))
            {
                return 'A' + label;
            }
            if (IsFL(label))
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
        public static bool CollectRainData(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<RainDrawingData> drDatas, bool noWL = false)
        {
            CollectRainGeoData(range, adb, out storeysItems, out RainGeoData geoData);
            return CreateRainDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CollectRainData(Geometry range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<RainDrawingData> drDatas, ThRainService.CommandContext ctx, bool noWL = false)
        {
            CollectRainGeoData(range, adb, out storeysItems, out RainGeoData geoData, ctx);
            return CreateRainDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CreateRainDrawingData(AcadDatabase adb, out List<RainDrawingData> drDatas, bool noWL, RainGeoData geoData)
        {
            ThRainService.PreFixGeoData(geoData);
            drDatas = _CreateRainDrawingData(adb, geoData, true);
            return true;
        }
        public static List<RainDrawingData> CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.PreFixGeoData(geoData);
            return _CreateRainDrawingData(adb, geoData, noDraw);
        }
        private static List<RainDrawingData> _CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = RainCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            var roomData = RainService.CollectRoomData(adb);
            RainService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out List<RainDrawingData> drDatas, roomData: roomData);
            Dbg.PrintText(logString);
            if (noDraw) DU.Dispose();
            return drDatas;
        }

        public static void CollectRainGeoData(Geometry range, AcadDatabase adb, out List<StoreysItem> storeysItems, out RainGeoData geoData, ThRainService.CommandContext ctx)
        {
            var storeys = ThRainSystemService.GetStoreys(range, adb, ctx);
            ThRainSystemService.FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(range, adb, geoData, ctx);
        }
        public static void CollectRainGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out RainGeoData geoData)
        {
            var storeys = ThRainSystemService.GetStoreys(range, adb);
            ThRainSystemService.FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(range, adb, geoData);
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
        static bool NoDraw;



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

        public static List<RainGroupedPipeItem> GetRainGroupedPipeItems(List<RainDrawingData> drDatas, List<StoreysItem> storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys)
        {
            //👻开始写分组逻辑

            //Console.WriteLine(storeysItems.ToCadJson());

            {
                //prefix drData

                foreach (var drData in drDatas)
                {

                }

            }

            var allY1L = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsY1L(x)));
            var allY2L = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsY2L(x)));
            var allNL = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsNL(x)));

            var storeys = new List<string>();
            foreach (var item in storeysItems)
            {
                item.Init();
                //storeys.AddRange(item.Labels.Where(isWantedStorey));
                storeys.AddRange(item.Labels);
            }
            storeys = storeys.Distinct().OrderBy(GetStoreyScore).ToList();
            var minS = storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Min();
            var maxS = storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Max();
            var countS = maxS - minS + 1;
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
            bool hasLong(string label, string storey)
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.LongTranslatorLabels.Contains(label);
                        }
                    }
                }
                return false;
            }
            bool hasShort(string label, string storey)
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.ShortTranslatorLabels.Contains(label);
                        }
                    }
                }
                return false;
            }
            string getWaterWellLabel(string label)
            {
                foreach (var drData in drDatas)
                {
                    if (drData.WaterWellLabels.TryGetValue(label, out string value))
                    {
                        return value;
                    }
                }
                return null;
            }
            bool hasWaterWell(string label)
            {
                return getWaterWellLabel(label) != null;
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
                return ret;
            }
            bool hasCondensePipe(string label, string storey)
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            
                        }
                    }
                }
                return false;
            }
            bool hasBrokenCondensePipes(string label,string storey)
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];

                        }
                    }
                }
                return false;
            }
            bool hasNonBrokenCondensePipes(string label,string storey)
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];

                        }
                    }
                }
                return false;
            }
            bool hasWrappingPipe(string label,string storey)
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];

                        }
                    }
                }
                return false;
            }
            var pipeInfoDict = new Dictionary<string, RainGroupingPipeItem>();
            var y1lGroupingItems = new List<RainGroupingPipeItem>();
            var y2lGroupingItems = new List<RainGroupingPipeItem>();
            var nlGroupingItems = new List<RainGroupingPipeItem>();

            foreach (var y1l in allY1L)
            {
                var item = new RainGroupingPipeItem();
                item.Label = y1l;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allNumStoreyLabels)
                {
                    var _hasLong = hasLong(y1l, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(y1l, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(y1l, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(y1l, storey),
                    });
                    y1lGroupingItems.Add(item);
                    pipeInfoDict[y1l] = item;
                }
            }
            foreach (var y2l in allNL)
            {
                var item = new RainGroupingPipeItem();
                item.Label = y2l;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allNumStoreyLabels)
                {
                    var _hasLong = hasLong(y2l, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(y2l, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(y2l, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(y2l, storey),
                    });
                    y2lGroupingItems.Add(item);
                    pipeInfoDict[y2l] = item;
                }
            }
            foreach (var nl in allY2L)
            {
                var item = new RainGroupingPipeItem();
                item.Label = nl;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allNumStoreyLabels)
                {
                    var _hasLong = hasLong(nl, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(nl, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(nl, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(nl, storey),
                    });
                    nlGroupingItems.Add(item);
                    pipeInfoDict[nl] = item;
                }
            }


            //修复业务bug
            {

            }
            //开始分组
            var pipeGroupItems = new List<RainGroupedPipeItem>();
            foreach (var g in y1lGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterPort).Select(x => x.WaterPortLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWrappingPipe,
                    WaterPortLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = PipeType.Y1L,
                    Hangings = g.Key.Hangings.ToList(),
                };
                pipeGroupItems.Add(item);
            }
            foreach (var g in y2lGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterPort).Select(x => x.WaterPortLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWrappingPipe,
                    WaterPortLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = PipeType.Y1L,
                    Hangings = g.Key.Hangings.ToList(),
                };
                pipeGroupItems.Add(item);
            }
            foreach (var g in nlGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.HasWaterPort).Select(x => x.WaterPortLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasWrappingPipe = g.Key.HasWrappingPipe,
                    WaterPortLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = PipeType.Y1L,
                    Hangings = g.Key.Hangings.ToList(),
                };
                pipeGroupItems.Add(item);
            }
            Console.WriteLine(pipeGroupItems.ToCadJson());
            return pipeGroupItems;
        }

        public static void DrawRainDiagram(RainSystemDiagramViewModel vm)
        {
            throw new NotImplementedException();
        }

        public static void DrawRainDiagram()
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

                Dbg.BuildAndSetCurrentLayer(adb.Database);

                List<StoreysItem> storeysItems;
                List<RainDrawingData> drDatas;
                if (!CollectRainData(range, adb, out storeysItems, out drDatas, noWL: true)) return;
                Console.WriteLine(drDatas.ToCadJson());
                Console.WriteLine(storeysItems.ToCadJson());
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                DU.Dispose();
                DrawRainDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);
                DU.Draw(adb);
            }
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof)
        {
            var offsetY = canPeopleBeOnRoof ? 500.0 : 2000.0;
            DrawAiringSymbol(pt, offsetY);
            Dr.DrawDN_1(pt);
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DU.DrawBlockReference(blkName: "通气帽系统", basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: "W-DRAI-DOME-PIPE", cb: br =>
            {
                br.ObjectId.SetDynBlockValue("距离1", offsetY);
                br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
            });
        }
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs.Where(x => x.Length > 0));
            lines.ForEach(line =>
            {
                line.Layer = "W-DRAI-NOTE";
                DU.ByLayer(line);
            });
        }
        public static void DrawStoreyLine(string label, Point2d _basePt, double lineLen)
        {
            var basePt = _basePt.ToPoint3d();
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
        public static void DrawRainDiagram(Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, int __dy, RainSystemDiagramViewModel viewModel)
        {
            var dome_lines = new List<GLineSegment>(4096);
            var dome_layer = "W-RAIN-PIPE";
            void drawDomePipe(GLineSegment seg)
            {
                if (seg.Length > 0) dome_lines.Add(seg);
            }
            void drawDomePipes(IEnumerable<GLineSegment> segs)
            {
                dome_lines.AddRange(segs.Where(x => x.Length > 0));
            }

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                static void DrawSegs(List<GLineSegment> segs) { for (int k = 0; k < segs.Count; k++) DU.DrawTextLazy(k.ToString(), segs[k].StartPoint); }
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
                        DrawStoreyLine(storey, bsPt1, lineLen);
                    }
                }
                for (int j = 0; j < COUNT; j++)
                {
                    pipeGroupItems.Add(new RainGroupedPipeItem());
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
                                    arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6);
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
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6).OffsetX(121);
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
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6).OffsetX(-121);
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
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6);
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
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6);
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
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6).OffsetX(121);
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
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / 6).OffsetX(-121);
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
                var dx = 0;
                for (int j = 0; j < COUNT; j++)
                {
                    var gpItem = pipeGroupItems[j];
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = 0; i < allNumStoreyLabels.Count; i++)
                    {
                        var storey = allNumStoreyLabels[i];
                        var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                        {
                            HasLongTranslator = gpItem.Items[i].HasLong,
                            HasShortTranslator = gpItem.Items[i].HasShort,
                            HasCleaningPort = gpItem.Hangings[i].HasCleaningPort,
                            HasCheckPoint = gpItem.Hangings[i].HasCheckPoint,
                            HasHorizontalShortLine = gpItem.Hangings[i].HasDownBoardLine,
                        } : null;
                        runs.Add(run);
                    }
                    for (int i = 0; i < allNumStoreyLabels.Count; i++)
                    {
                        var FloorDrainsCount = gpItem.Hangings[i].FloorDrainsCount;
                        var hasSCurve = gpItem.Hangings[i].HasSCurve;
                        var hasDoubleSCurve = gpItem.Hangings[i].HasDoubleSCurve;
                        if (FloorDrainsCount > 0 || hasSCurve || hasDoubleSCurve)
                        {
                            var run = runs.TryGet(i - 1);
                            if (run != null)
                            {
                                var hanging = run.LeftHanging = new Hanging();
                                hanging.FloorDrainsCount = FloorDrainsCount;
                                hanging.HasSCurve = hasSCurve;
                                hanging.HasDoubleSCurve = hasDoubleSCurve;
                                hanging.IsFL0 = gpItem.Hangings[i].IsFL0;
                            }
                        }
                    }

                    {
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
                    }
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                    {
                        {
                            //draw comments
                            var info = arr.Where(x => x != null).FirstOrDefault();
                            if (info != null)
                            {
                                var dy = -3000;
                                if (thwPipeLine.Labels != null)
                                {
                                    foreach (var comment in thwPipeLine.Labels)
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
                                    //默认屋面不上人
                                    DrawAiringSymbol(seg.EndPoint, viewModel?.Params?.CouldHavePeopleOnRoof ?? false);
                                }
                            }
                        }
                        void _DrawLabel(string text, Point2d basePt, bool leftOrRight, double height)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(0, height), new Vector2d(leftOrRight ? -3600 : 3600, 0) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DU.DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForDraiNote(lines.ToArray());
                            var t = DU.DrawTextLazy(text, 350, segs.Last().EndPoint.OffsetY(50));
                            Dr.SetLabelStylesForDraiNote(t);
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
                                    if (output != null)
                                    {
                                        //DrawOutlets
                                    }
                                }
                            }
                            void handleHanging(Hanging hanging, bool isLeftOrRight)
                            {
                                void _DrawFloorDrain(Point3d basePt, bool leftOrRight)
                                {
                                    DrawFloorDrain(basePt, leftOrRight);
                                    return;
                                }




                            }
                        }
                    }


                    //修复绘图逻辑
                    {

                    }

                    var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                    handlePipeLine(thwPipeLine, arr);

                    static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(400, 400), new Vector2d(1400, 0) };
                        if (isLeftOrRight == true)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DU.DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForDraiNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[1].EndPoint : segs[1].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(50);
                        if (text1 != null)
                        {
                            var t = DU.DrawTextLazy(text1, 350, txtBasePt);
                            Dr.SetLabelStylesForDraiNote(t);
                        }
                        if (text2 != null)
                        {
                            var t = DU.DrawTextLazy(text2, 350, txtBasePt.OffsetY(-350 - 50));
                            Dr.SetLabelStylesForDraiNote(t);
                        }
                    }
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
                    //draw storey label
                    {
                        var _storeys = new string[] { allNumStoreyLabels.GetAt(2), allNumStoreyLabels.GetLastOrDefault(3) }.SelectNotNull().Distinct().ToList();
                        if (_storeys.Count == 0)
                        {
                            _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                        }
                        _storeys = _storeys.Where(storey =>
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = arr.TryGet(i);
                            return info != null && info.Visible;
                        }).ToList();
                        if (_storeys.Count == 0)
                        {
                            _storeys = allNumStoreyLabels.Where(storey =>
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = arr.TryGet(i);
                                return info != null && info.Visible;
                            }).Take(1).ToList();
                        }
                        foreach (var storey in _storeys)
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = arr[i];

                            {
                                string label1, label2;
                                var isLeftOrRight = !thwPipeLine.Labels.Any(x => IsFL(x));
                                var labels = RainLabelItem.ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).ToList();
                                if (labels.Count == 2)
                                {
                                    label1 = labels[0];
                                    label2 = labels[1];
                                }
                                else
                                {
                                    label1 = labels.JoinWith(";");
                                    label2 = null;
                                }
                                drawLabel(info.PlBasePt, label1, label2, isLeftOrRight);
                            }
                        }
                    }
                    //立管尺寸
                    {
                        var _storeys = new string[] { allNumStoreyLabels.GetAt(1), allNumStoreyLabels.GetLastOrDefault(2) }.SelectNotNull().Distinct().ToList();
                        if (_storeys.Count == 0)
                        {
                            _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                        }

                        //var _storeys = allNumStoreyLabels;
                        foreach (var storey in _storeys)
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = arr.TryGet(i);
                            if (info != null && info.Visible)
                            {
                                var run = runs.TryGet(i);
                                if (run != null)
                                {
                                    Dr.DrawDN_2(info.EndPoint);
                                    if (gpItem.HasTl)
                                    {
                                        Dr.DrawDN_3(info.EndPoint.OffsetXY(300, 0));
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = arr.TryGet(i);
                        if (info != null && info.Visible)
                        {

                        }
                    }

                    //draw outputs
                    {
                        var i = allNumStoreyLabels.IndexOf("1F");
                        if (i >= 0)
                        {
                            var storey = allNumStoreyLabels[i];
                            var info = arr.First();
                            if (info != null && info.Visible)
                            {
                                var output = new ThwOutput()
                                {
                                    DirtyWaterWellValues = gpItem.WaterPortLabels.OrderBy(x =>
                                    {
                                        long.TryParse(x, out long v);
                                        return v;
                                    }).ToList(),
                                    HasWrappingPipe1 = gpItem.HasWrappingPipe,
                                    DN1 = "DN100",
                                    HasCleaningPort1 = true,
                                };
                                var pt = info.EndPoint;

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
                            DU.ByLayer(line);
                        }
                    }
                    else
                    {
                        foreach (var dome_line in dome_lines)
                        {
                            var line = DU.DrawLineSegmentLazy(dome_line);
                            line.Layer = dome_layer;
                            DU.ByLayer(line);
                        }
                    }
                }

            }
        }
        public static void DrawRainDiagram(List<RainDrawingData> drDatas, List<StoreysItem> storeysItems, Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + "F").ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - 1;
            var end = 0;
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0 + 500 + 3500;//马力说再加个3500
            var HEIGHT = 1800.0;
            //var HEIGHT = 3000.0;
            //var HEIGHT = 5000.0;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - 1800.0;
            var __dy = 300;
            DrawRainDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, null);
        }

        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-DRAI-NOTE";
                DU.ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static bool Testing = false;
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = "普通地漏P弯")
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DU.DrawBlockReference("地漏系统", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    DU.ByLayer(br);
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", value);
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("地漏系统", basePt,
               br =>
               {
                   br.Layer = "W-DRAI-EQPM";
                   DU.ByLayer(br);
                   br.ScaleFactors = new Scale3d(-2, 2, 2);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                   }
               });
            }

        }
        public static void DrawStoreyHeightSymbol(Point2d basePt, string label = "666")
        {
            DU.DrawBlockReference(blkName: "标高", basePt: basePt.ToPoint3d(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", label } }, cb: br => { DU.ByLayer(br); });
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
                DU.ByLayer(br);
            });
        }
        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-DOME-PIPE";
            DU.ByLayer(line);
        }
        public static void DrawDomePipes(params GLineSegment[] segs)
        {
            DrawDomePipes((IEnumerable<GLineSegment>)segs);
        }
        public static void DrawDomePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs.Where(x => x.Length > 0));
            lines.ForEach(line => SetDomePipeLineStyle(line));
        }

    }
    public class RainLabelItem
    {
        public string Label;
        public string Prefix;
        public string D1S;
        public string D2S;
        public string Suffix;
        public int D1
        {
            get
            {
                int.TryParse(D1S, out int r); return r;
            }
        }
        public int D2
        {
            get
            {
                int.TryParse(D2S, out int r); return r;
            }
        }
        static readonly Regex re = new Regex(@"^(Y1L|Y2L|NL)(\w*)\-(\w*)([a-zA-Z]*)$");
        public static LabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new LabelItem()
            {
                Label = label,
                Prefix = m.Groups[1].Value,
                D1S = m.Groups[2].Value,
                D2S = m.Groups[3].Value,
                Suffix = m.Groups[4].Value,
            };
        }
        public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
        {
            var items = pipeIds.Select(id => LabelItem.Parse(id)).Where(m => m != null);
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2).ToList());
            foreach (var g in gs)
            {
                if (g.Count == 1)
                {
                    yield return g.First().Label;
                }
                else if (g.Count > 2 && g.Count == g.Last().D2 - g.First().D2 + 1)
                {
                    var m = g.First();
                    yield return $"{m.Prefix}{m.D1S}-{g.First().D2S}{m.Suffix}~{g.Last().D2S}{m.Suffix}";
                }
                else
                {
                    var sb = new StringBuilder();
                    {
                        var m = g.First();
                        sb.Append($"{m.Prefix}{m.D1S}-");
                    }
                    for (int i = 0; i < g.Count; i++)
                    {
                        var m = g[i];
                        sb.Append($"{m.D2S}{m.Suffix}");
                        if (i != g.Count - 1)
                        {
                            sb.Append(",");
                        }
                    }
                    yield return sb.ToString();
                }
            }
        }

    }
    public class Hanging
    {
        public int FloorDrainsCount;
        public bool IsSeries;
        public bool HasSCurve;
        public bool HasDoubleSCurve;
        public bool HasUnderBoardLabel;

        public bool IsFL0;
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
    }
    public class ThwPipeLine
    {
        public List<string> Labels;
        public bool? IsLeftOrMiddleOrRight;
        public double AiringValue;
        public List<ThwPipeRun> PipeRuns;
        public ThwOutput Output;
    }
    public class RainDrawingData
    {
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public HashSet<string> CleaningPorts;
        public Dictionary<string, string> WaterWellLabels;
        public HashSet<string> WrappingPipes;

        public List<string> Comments;

        public void Init()
        {
            VerticalPipeLabels ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            CleaningPorts ??= new HashSet<string>();
            WaterWellLabels ??= new Dictionary<string, string>();
            WrappingPipes ??= new HashSet<string>();
            Comments ??= new List<string>();
        }
    }
    public partial class RainService
    {
        public AcadDatabase adb;
        public RainDiagram RainDiagram;
        public List<ThStoreysData> Storeys;
        public RainGeoData GeoData;
        public RainCadData CadDataMain;
        public List<RainCadData> CadDatas;
        public List<RainDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static RainGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return null;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = new RainGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }

        public static void CollectGeoData(Geometry range, AcadDatabase adb, RainGeoData geoData, ThRainService.CommandContext ctx)
        {
            var cl = CollectGeoData(adb, geoData);
            cl.CollectStoreys(range, ctx);
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, RainGeoData geoData)
        {
            var cl = CollectGeoData(adb, geoData);
            cl.CollectStoreys(range);
        }

        public static ThRainSystemServiceGeoCollector CollectGeoData(AcadDatabase adb, RainGeoData geoData)
        {
            var cl = new ThRainSystemServiceGeoCollector() { adb = adb, geoData = geoData };
            cl.CollectEntities();
            cl.CollectLabelLines();
            cl.CollectWLines();
            cl.CollectCTexts();
            cl.CollectWaterWells();
            cl.CollectCondensePipes();
            cl.CollectVerticalPipes();
            cl.CollectWrappingPipes();
            cl.CollectWaterPorts();
            cl.CollectWaterPortSymbols();
            cl.CollectFloorDrains();
            cl.CollectWaterPort13s();
            return cl;
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
        public static void DrawGeoData(RainGeoData geoData)
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
            foreach (var o in geoData.WaterWells) DU.DrawRectLazy(o).ColorIndex = 7;
            {
                var cl = Color.FromRgb(4, 229, 230);
                foreach (var o in geoData.DLines) DU.DrawLineSegmentLazy(o).Color = cl;
            }
        }

        const double MAX_SHORTTRANSLATOR_DISTANCE = 300;//150;

        public static void CreateDrawingDatas(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas, out string logString, out List<RainDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData = null)
        {
            DrawingTransaction.Current.AbleToDraw = false;
            roomData ??= new List<KeyValuePair<string, Geometry>>();
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DU.DrawRectLazy(s).ColorIndex = 1;
            }
            var sb = new StringBuilder(8192);
            drDatas = new List<RainDrawingData>();
            for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
            {
                sb.AppendLine($"===框{storeyI}===");
                var drData = new RainDrawingData();
                drData.Init();
                var item = cadDatas[storeyI];

                {
                    var maxDis = 8000;
                    var angleTolleranceDegree = 1;
                    var waterPortCvt = RainCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > 0).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
                        maxDis, angleTolleranceDegree).ToList();
                    geoData.DLines.AddRange(lines);
                    var dlineCvt = RainCadData.ConvertDLinesF();
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
                var wlinesGroups = GG(item.WLines);
                var wlinesGeos = GeosGroupToGeos(wlinesGroups);
                var wrappingPipesf = F(item.WrappingPipes);

                //自动补上缺失的立管
                {
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        if (!ThRainService.IsMaybeLabelText(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
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
                foreach (var o in item.PipeKillers)
                {
                    DU.DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(255, 255, 55);
                }
                foreach (var o in item.CondensePipes)
                {
                    DU.DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)], 10);
                }
                foreach (var o in item.VerticalPipes)
                {
                    DU.DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = 3;
                }
                foreach (var o in item.FloorDrains)
                {
                    DU.DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = 6;
                }
                foreach (var o in item.WLines)
                {
                    DU.DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = 6;
                }
                foreach (var o in item.WaterPorts)
                {
                    DU.DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = 7;
                    DU.DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WaterWells)
                {
                    DU.DrawRectLazy(geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ColorIndex = 6;
                    DU.DrawTextLazy(geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WaterPort13s)
                {
                    DU.DrawRectLazy(geoData.WaterPort13s[cadDataMain.WaterPort13s.IndexOf(o)]).ColorIndex = 7;
                }
                foreach (var o in item.WaterPortSymbols)
                {
                    DU.DrawRectLazy(geoData.WaterPortSymbols[cadDataMain.WaterPortSymbols.IndexOf(o)]).ColorIndex = 7;
                }
                foreach (var o in item.CondensePipes)
                {
                    var e = DU.DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)]).ColorIndex = 1;
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
                                    if (ThRainService.IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
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
                                    if (labelsTxts.All(txt => ThRainService.IsMaybeLabelText(txt)))
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
                                if (!ThRainService.IsMaybeLabelText(lb)) continue;
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
                                foreach (var dlinesGeo in wlinesGeos)
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
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = true;
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
                }

                string getLabel(Geometry pipe)
                {
                    lbDict.TryGetValue(pipe, out string label);
                    return label;
                }

                {
                    var f = F(item.VerticalPipes);
                    foreach (var dlinesGeo in wlinesGeos)
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
                                    shortTranslatorLabels.Add(label);
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
                        var gs = GeoFac.GroupGeometriesEx(wlinesGeos, item.FloorDrains);
                        foreach (var g in gs)
                        {
                            var fds = g.Where(pl => item.FloorDrains.Contains(pl)).ToList();
                            var dlines = g.Where(pl => wlinesGeos.Contains(pl)).ToList();
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
                    drData.FloorDrains = dict;
                }

                //关联清扫口
                {
                    var f = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                    var hs = new HashSet<string>();
                    var gs = GeoFac.GroupGeometries(wlinesGeos.Concat(item.CleaningPorts).ToList());
                    foreach (var g in gs)
                    {
                        var dlines = g.Where(pl => wlinesGeos.Contains(pl)).ToList();
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
                        void collect(Func<Geometry, List<Geometry>> waterWellsf, Func<Geometry, string> getWaterWellLabel)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in wlinesGeos)
                            {
                                var waterWells = waterWellsf(dlinesGeo);
                                if (waterWells.Count == 1)
                                {
                                    var waterWell = waterWells[0];
                                    var waterWellLabel = getWaterWellLabel(waterWell);
                                    portd[dlinesGeo] = waterWellLabel;
                                    //DU.DrawTextLazy(waterPortLabel, dlinesGeo.GetCenter());
                                    var pipes = f2(dlinesGeo);
                                    ok_ents.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            outletd[label] = waterWellLabel;
                                            var wrappingpipes = wrappingPipesf(dlinesGeo);
                                            if (wrappingpipes.Count > 0)
                                            {
                                                has_wrappingpipes.Add(label);
                                            }
                                            foreach (var wp in wrappingpipes)
                                            {
                                                portd[wp] = waterWellLabel;
                                                DU.DrawTextLazy(waterWellLabel, wp.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //先提取直接连接的
                        collect(F(item.WaterWells), waterWell => geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(waterWell)]);
                        //再处理挨得特别近但就是没连接的
                        {
                            var spacialIndex = item.WaterWells.Select(cadDataMain.WaterWells).ToList();
                            var waterWells = spacialIndex.ToList(geoData.WaterWells).Select(x => x.Expand(400).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterWellLabels[spacialIndex[waterWells.IndexOf(waterWell)]]);
                        }
                    }
                    {
                        //再处理挨得有点远没直接连接的
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = 10;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterWells);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterWells).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= 1500)
                                    {
                                        Dbg.ShowXLabel(pt);
                                        var waterWellLabel = geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(waterWell)];
                                        portd[dlinesGeo] = waterWellLabel;
                                        foreach (var pipe in f2(dlinesGeo))
                                        {
                                            if (lbDict.TryGetValue(pipe, out string label))
                                            {
                                                outletd[label] = waterWellLabel;
                                                ok_ents.Add(pipe);
                                                var wrappingpipes = wrappingPipesf(dlinesGeo);
                                                if (wrappingpipes.Any())
                                                {
                                                    has_wrappingpipes.Add(label);
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    portd[wp] = waterWellLabel;
                                                    DU.DrawTextLazy(waterWellLabel, wp.GetCenter());
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
                        foreach (var dlinesGeo in wlinesGeos)
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
                        drData.WaterWellLabels = outletd;

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

                {
                    sb.AppendLine("地漏：" + drData.FloorDrains.ToJson());
                }
                drDatas.Add(drData);
            }
            logString = sb.ToString();
            DrawingTransaction.Current.AbleToDraw = true;
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
            var names = adb.ModelSpace.OfType<MText>().Where(x => x.Layer == "AI-空间名称").Select(x => new CText() { Text = x.Text, Boundary = x.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect() }).ToList();
            var f = GeoFac.CreateIntersectsSelector(ranges);
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == 1)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[0]));
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
    }
    public class RainGroupingPipeItem : IEquatable<RainGroupingPipeItem>
    {
        public string Label;
        public bool HasWaterPort;
        public bool HasBasinInKitchenAt1F;
        public bool HasWrappingPipe;
        public bool IsSingleOutlet;
        public string WaterPortLabel;
        public List<ValueItem> Items;
        public List<Hanging> Hangings;
        public bool HasTL;
        public string TlLabel;
        public int MaxTl;
        public int MinTl;
        public int FloorDrainsCountAt1F;
        public class Hanging : IEquatable<Hanging>
        {
            public int FloorDrainsCount;
            public bool IsSeries;
            public bool HasSCurve;
            public bool HasDoubleSCurve;
            public bool HasCleaningPort;
            public bool HasCheckPoint;
            public bool HasDownBoardLine;//降板线
            public bool IsFL0;
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
                    && this.HasCheckPoint == other.HasCheckPoint
                    && this.HasDownBoardLine == other.HasDownBoardLine
                    && this.IsFL0 == other.IsFL0
                    ;
            }
        }
        public struct ValueItem
        {
            public bool Exist;
            public bool HasLong;
            public bool HasShort;
            public bool HasKitchenWashingMachine;
            public bool HasKitchenNonWashingMachineFloorDrain;
            public bool HasKitchenWashingMachineFloorDrain;
            public bool HasBalconyWashingMachineFloorDrain;
            public bool HasBalconyNonWashingMachineFloorDrain;
        }
        public override int GetHashCode()
        {
            return 0;
        }
        public bool Equals(RainGroupingPipeItem other)
        {
            return this.HasWaterPort == other.HasWaterPort
                && this.HasWrappingPipe == other.HasWrappingPipe
                && this.HasBasinInKitchenAt1F == other.HasBasinInKitchenAt1F
                && this.MaxTl == other.MaxTl
                && this.MinTl == other.MinTl
                && this.IsSingleOutlet == other.IsSingleOutlet
                && this.FloorDrainsCountAt1F == other.FloorDrainsCountAt1F
                && this.Items.SeqEqual(other.Items)
                && this.Hangings.SeqEqual(other.Hangings);
        }
    }
    public enum PipeType
    {
        Y1L, Y2L, NL,
    }
    public class RainGroupedPipeItem
    {
        public List<string> Labels;
        public List<string> WaterPortLabels;
        public bool HasWrappingPipe;
        public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > 0;
        public List<RainGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public int MinTl;
        public int MaxTl;
        public bool HasTl => TlLabels != null && TlLabels.Count > 0;
        public PipeType PipeType;
        public List<RainGroupingPipeItem.Hanging> Hangings;
        public bool IsSingleOutlet;
        public bool HasBasinInKitchenAt1F;
        public int FloorDrainsCountAt1F;
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
        public Point2d PlBasePt;
    }
    public class ThRainService
    {
        public class CommandContext
        {
            public Point3dCollection range;
            public ThMEPWSS.Pipe.Service.ThRainSystemService.StoreyContext StoreyContext;
            public Diagram.ViewModel.RainSystemDiagramViewModel ViewModel;
            public System.Windows.Window window;
        }
        public static CommandContext commandContext;
        public static void ConnectLabelToLabelLine(RainGeoData geoData)
        {
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1500, 250);
                var _lineHGs = f1(g.ToPolygon());
                var geo = GeoFac.NearestNeighbourGeometryF(_lineHGs)(bd.Center.ToNTSPoint());
                if (geo == null) continue;
                {
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(100, 400) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(.1, 400))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(.1));
                    }
                }
            }
        }
        public static void PreFixGeoData(RainGeoData geoData)
        {
            for (int i = 0; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(5))
                {
                    geoData.LabelLines[i] = seg.Extend(6);
                }
                else if (seg.IsVertical(5))
                {
                    geoData.LabelLines[i] = seg.Extend(1);
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
            for (int i = 0; i < geoData.WLines.Count; i++)
            {
                geoData.WLines[i] = geoData.WLines[i].Extend(5);
            }
            {
                //处理立管重叠的情况
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(.1)).ToList();
            }
            {
                //处理标注重叠的情况
                geoData.Labels = geoData.Labels.Distinct(Dbg.CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, 10))).ToList();
            }
            {
                //其他的也顺手处理下好了。。。
                var cmp = new GRect.EqualityComparer(.1);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.CondensePipes = geoData.CondensePipes.Distinct(cmp).ToList();
                geoData.PipeKillers = geoData.PipeKillers.Distinct(cmp).ToList();
            }
            {
                var _pipes = geoData.VerticalPipes.Distinct().ToList();
                var pipes = _pipes.Select(x => x.Center.ToNTSPoint()).ToList();
                var tag = new object();
                var pipesf = GeoFac.CreateContainsSelector(pipes);
                foreach (var _killer in geoData.PipeKillers)
                {
                    foreach (var pipe in pipesf(_killer.ToPolygon()))
                    {
                        pipe.UserData = tag;
                    }
                }
                geoData.VerticalPipes = pipes.Where(x => x.UserData == null).Select(pipes).ToList(_pipes);
            }
            //if (Dbg._)
            {
                foreach (var ct in geoData.Labels)
                {
                    if (ct.Text.StartsWith("73-"))
                    {
                        ct.Text = ct.Text.Substring(3);
                    }
                    else if (ct.Text.StartsWith("1-"))
                    {
                        ct.Text = ct.Text.Substring(2);
                    }
                }
            }
        }
        public static void Register(Action<string, Action> register)
        {
            register("ThRainSystemService", ThRainSystemService.DrawRainSystemDiagram2);
            register("ThRainDiagram", () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var basePoint = Dbg.SelectPoint().ToPoint2d();
                    var OFFSET_X = 2500.0;
                    var SPAN_X = 5500.0;
                    var HEIGHT = 1800.0;
                    //var HEIGHT = 5000.0;
                    var COUNT = 20;
                    var start = 10;
                    var end = 0;
                    var dy = 0;
                    var __dy = 0;
                    var allNumStoreyLabels = Enumerable.Range(1, 32).Select(i => i + "F").ToList();
                    var allStoreys = allNumStoreyLabels.Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                    var pipeGroupItems = new List<RainGroupedPipeItem>();
                    RainDiagram.DrawRainDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, null);
                }
            });
            register("🔴draw byuivm", () =>
            {
                try
                {
                    var vm = new ThMEPWSS.Diagram.ViewModel.RainSystemDiagramViewModel();
                    vm.Params.CouldHavePeopleOnRoof = true;
                    ThRainService.commandContext = new ThRainService.CommandContext() { ViewModel = vm, };
                    ThRainService.CollectFloorListDatasEx();
                    RainDiagram.DrawRainDiagram(vm);
                }
                finally
                {
                    ThRainService.commandContext = null;
                }
            });
            register("🔴draw rain bycmd", () => { RainDiagram.DrawRainDiagram(); });
            register("load rain drDatas and draw", () =>
            {
                var storeysItems = Dbg.LoadFromTempJsonFile<List<RainDiagram.StoreysItem>>("rain_storeysItems");
                var drDatas = Dbg.LoadFromTempJsonFile<List<RainDrawingData>>("rain_drDatas");
                Dbg.FocusMainWindow();
                var basePoint = Dbg.SelectPoint().ToPoint2d();

                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var pipeGroupItems = RainDiagram.GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                    DU.Dispose();
                    RainDiagram.DrawRainDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);
                    DU.Draw(adb);
                }
            });
            register("load rain geoData save drDatas noDraw", () =>
            {
                var storeysItems = Dbg.LoadFromTempJsonFile<List<RainDiagram.StoreysItem>>("rain_storeysItems");
                var geoData = Dbg.LoadFromTempJsonFile<RainGeoData>("rain_geoData");

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var drDatas = RainDiagram.CreateRainDrawingData(adb, geoData, true);
                    Dbg.SaveToTempJsonFile(drDatas, "rain_drDatas");
                }
            });
            register("load rain geoData save drDatas", () =>
            {
                var storeysItems = Dbg.LoadFromTempJsonFile<List<RainDiagram.StoreysItem>>("rain_storeysItems");
                var geoData = Dbg.LoadFromTempJsonFile<RainGeoData>("rain_geoData");

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    var drDatas = RainDiagram.CreateRainDrawingData(adb, geoData, false);
                    Dbg.SaveToTempJsonFile(drDatas, "rain_drDatas");
                }
            });
            register("save rain geoData", () =>
            {
                Dbg.FocusMainWindow();
                var range = Dbg.TrySelectRange();
                if (range == null) return;
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    var db = adb.Database;
                    Dbg.BuildAndSetCurrentLayer(db);
                    RainDiagram.CollectRainGeoData(range, adb, out List<RainDiagram.StoreysItem> storeysItems, out RainGeoData geoData);
                    Dbg.SaveToTempJsonFile(storeysItems, "rain_storeysItems");
                    Dbg.SaveToTempJsonFile(geoData, "rain_geoData");
                }
            });
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return false;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsY1L(label) || IsY2L(label) || IsNL(label);
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return false;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label)
                || IsY1L(label) || IsY2L(label) || IsNL(label) || label.StartsWith("YL")
                || label.Contains("单排")
                || label.StartsWith("RML") || label.StartsWith("RMHL")
                || label.StartsWith("J1L") || label.StartsWith("J2L")
                ;
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
    }
}