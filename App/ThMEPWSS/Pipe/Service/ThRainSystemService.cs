namespace ThMEPWSS.ReleaseNs.RainSystemNs
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
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
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
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
    using ThMEPWSS.ReleaseNs;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using static ThMEPWSS.Assistant.DrawUtils;
    using static THRainService;
    using ThMEPEngineCore.Model.Common;
    using NetTopologySuite.Operation.Buffer;
    using StoreyContext = Pipe.Model.StoreyContext;
    public class TempGeoFac
    {
        public static IEnumerable<GLineSegment> GetMinConnSegs(List<GLineSegment> segs)
        {
            if (segs.Count <= ADRENOCORTICOTROPHIC) yield break;
            var geos = segs.Select(x => x.Extend(THESAURUSFACTOR).ToLineString()).ToList();
            var gs = GeoFac.GroupGeometries(geos);
            if (gs.Count >= PHOTOGONIOMETER)
            {
                for (int i = NARCOTRAFICANTE; i < gs.Count - ADRENOCORTICOTROPHIC; i++)
                {
                    foreach (var seg in GetMinConnSegs(gs[i], gs[i + ADRENOCORTICOTROPHIC]))
                    {
                        yield return seg;
                    }
                }
            }
        }
        public static IEnumerable<GLineSegment> GetMinConnSegs(List<LineString> g1, List<LineString> g2)
        {
            var lines1 = g1.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).Distinct().ToList();
            var lines2 = g2.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).Distinct().ToList();
            if (lines1.Count > NARCOTRAFICANTE && lines2.Count > NARCOTRAFICANTE)
            {
                var dis = TempGeoFac.GetMinDis(lines1, lines2, out LineString ls1, out LineString ls2);
                if (dis > NARCOTRAFICANTE && ls1 != null && ls2 != null)
                {
                    foreach (var seg in TempGeoFac.TryExtend(GeoFac.GetLines(ls1).First(), GeoFac.GetLines(ls2).First(), THESAURUSDEPOSIT))
                    {
                        if (seg.IsValid) yield return seg;
                    }
                }
            }
        }
        public static IEnumerable<GLineSegment> TryExtend(GLineSegment s1, GLineSegment s2, double extend)
        {
            var v1 = s1.ToVector2d();
            var v2 = s2.ToVector2d();
            var dg = v1.GetAngleTo(v2).AngleToDegree();
            if (dg <= THESAURUSFACTOR)
            {
                var dis = s1.ToLineString().Distance(s2.ToLineString());
                if (dis <= NARCOTRAFICANTE) yield break;
                if (s1.StartPoint.GetDistanceTo(s2.StartPoint).EqualsTo(dis, THESAURUSCONSUL)) yield return new GLineSegment(s1.StartPoint, s2.StartPoint);
                if (s1.StartPoint.GetDistanceTo(s2.EndPoint).EqualsTo(dis, THESAURUSCONSUL)) yield return new GLineSegment(s1.StartPoint, s2.EndPoint);
                if (s1.EndPoint.GetDistanceTo(s2.StartPoint).EqualsTo(dis, THESAURUSCONSUL)) yield return new GLineSegment(s1.EndPoint, s2.StartPoint);
                if (s1.EndPoint.GetDistanceTo(s2.EndPoint).EqualsTo(dis, THESAURUSCONSUL)) yield return new GLineSegment(s1.EndPoint, s2.EndPoint);
                yield break;
            }
            var pt = s1.Extend(extend).ToLineString().Intersection(s2.Extend(extend).ToLineString()) as Point;
            if (pt != null)
            {
                var bf1 = s1.ToLineString().Buffer(THESAURUSEMBASSY);
                var bf2 = s2.ToLineString().Buffer(THESAURUSEMBASSY);
                if (!bf1.Intersects(pt))
                {
                    var s3 = new GLineSegment(pt.ToPoint2d(), s1.StartPoint);
                    var s4 = new GLineSegment(pt.ToPoint2d(), s1.EndPoint);
                    yield return s3.Length < s4.Length ? s3 : s4;
                }
                if (!bf2.Intersects(pt))
                {
                    var s3 = new GLineSegment(pt.ToPoint2d(), s2.StartPoint);
                    var s4 = new GLineSegment(pt.ToPoint2d(), s2.EndPoint);
                    yield return s3.Length < s4.Length ? s3 : s4;
                }
            }
        }
        public static double GetMinDis(List<LineString> geos1, List<LineString> geos2, out LineString m1, out LineString m2)
        {
            var minDis = double.MaxValue;
            m1 = null;
            m2 = null;
            foreach (var ls1 in geos1)
            {
                foreach (var ls2 in geos2)
                {
                    var dis = ls1.Distance(ls2);
                    if (minDis > dis)
                    {
                        minDis = dis;
                        m1 = ls1;
                        m2 = ls2;
                    }
                }
            }
            return minDis;
        }
    }
#pragma warning disable
    public class RainGeoData
    {
        public List<Point2d> StoreyContraPoints;
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
        public List<GRect> RainPortSymbols;
        public List<GRect> WaterSealingWells;
        public List<Point2d> CleaningPorts;
        public List<GRect> Ditches;
        public List<Point2d> SideFloorDrains;
        public List<GRect> PipeKillers;
        public List<KeyValuePair<Point2d, string>> WrappingPipeRadius;
        public List<GLineSegment> WrappingPipeLabelLines;
        public List<CText> WrappingPipeLabels;
        public List<GRect> AiringMachine_Hanging;
        public List<GRect> AiringMachine_Vertical;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            StoreyContraPoints ??= new List<Point2d>();
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
            RainPortSymbols ??= new List<GRect>();
            WaterSealingWells ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            GravityWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
            WrappingPipeRadius ??= new List<KeyValuePair<Point2d, string>>();
            WrappingPipeLabelLines ??= new List<GLineSegment>();
            WrappingPipeLabels ??= new List<CText>();
            Ditches ??= new List<GRect>();
            AiringMachine_Hanging ??= new List<GRect>();
            AiringMachine_Vertical ??= new List<GRect>();
        }
        public void FixData()
        {
            Init();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.IsValid).Distinct().ToList();
            DLines = DLines.Where(x => x.IsValid).Distinct().ToList();
            VLines = VLines.Where(x => x.IsValid).Distinct().ToList();
            WLines = WLines.Where(x => x.IsValid).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterPorts = WaterPorts.Where(x => x.IsValid).Distinct().ToList();
            CondensePipes = CondensePipes.Where(x => x.IsValid).Distinct().ToList();
            {
                var d = new Dictionary<GRect, string>();
                for (int i = NARCOTRAFICANTE; i < WaterWells.Count; i++)
                {
                    var well = WaterWells[i];
                    var label = WaterWellLabels[i];
                    if (!string.IsNullOrWhiteSpace(label) || !d.ContainsKey(well))
                    {
                        d[well] = label;
                    }
                }
                WaterWells.Clear();
                WaterWellLabels.Clear();
                foreach (var kv in d)
                {
                    WaterWells.Add(kv.Key);
                    WaterWellLabels.Add(kv.Value);
                }
            }
            SideWaterBuckets = SideWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            GravityWaterBuckets = GravityWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            _87WaterBuckets = _87WaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            RainPortSymbols = RainPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            WaterSealingWells = WaterSealingWells.Where(x => x.IsValid).Distinct().ToList();
            PipeKillers = PipeKillers.Where(x => x.IsValid).Distinct().ToList();
            SideFloorDrains = SideFloorDrains.Distinct().ToList();
            WrappingPipeRadius = WrappingPipeRadius.Distinct().ToList();
            WrappingPipeLabelLines = WrappingPipeLabelLines.Distinct().ToList();
            Ditches = Ditches.Where(x => x.IsValid).Distinct().ToList();
            AiringMachine_Hanging = AiringMachine_Hanging.Where(x => x.IsValid).Distinct().ToList();
            AiringMachine_Vertical = AiringMachine_Vertical.Where(x => x.IsValid).Distinct().ToList();
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
    public class AloneFloorDrainInfo
    {
        public bool IsSideFloorDrain;
        public string WaterWellLabel;
    }
    public class ExtraInfo
    {
        public class Item
        {
            public int Index;
            public List<Tuple<Geometry, string>> LabelDict;
        }
        public List<RainCadData> CadDatas;
        public List<Item> Items;
        public List<RainDiagram.StoreysItem> storeysItems;
        public RainGeoData geoData;
        public List<RainDrawingData> drDatas;
        public RainSystemDiagramViewModel vm;
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
        public List<Geometry> RainPortSymbols;
        public List<Geometry> WaterSealingWells;
        public List<Geometry> SideWaterBuckets;
        public List<Geometry> GravityWaterBuckets;
        public List<Geometry> _87WaterBuckets;
        public List<Geometry> Ditches;
        public List<Geometry> AiringMachine_Hanging;
        public List<Geometry> AiringMachine_Vertical;
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
            RainPortSymbols ??= new List<Geometry>();
            WaterSealingWells ??= new List<Geometry>();
            SideWaterBuckets ??= new List<Geometry>();
            GravityWaterBuckets ??= new List<Geometry>();
            _87WaterBuckets ??= new List<Geometry>();
            Ditches ??= new List<Geometry>();
            AiringMachine_Hanging ??= new List<Geometry>();
            AiringMachine_Vertical ??= new List<Geometry>();
        }
        public static RainCadData Create(RainGeoData data)
        {
            var o = new RainCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WLines.AddRange(data.WLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(x => x.ToGCircle(UNTRACEABLENESS).ToCirclePolygon(THESAURUSSOMETIMES, THESAURUSSEMBLANCE)));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.CondensePipes.AddRange(data.CondensePipes.Select(ConvertWashingMachinesF()));
            o.WaterWells.AddRange(data.WaterWells.Select(ConvertWashingMachinesF()));
            o.RainPortSymbols.AddRange(data.RainPortSymbols.Select(ConvertWashingMachinesF()));
            o.WaterSealingWells.AddRange(data.WaterSealingWells.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            o.PipeKillers.AddRange(data.PipeKillers.Select(ConvertVerticalPipesF()));
            o.SideWaterBuckets.AddRange(data.SideWaterBuckets.Select(ConvertVerticalPipesF()));
            o.GravityWaterBuckets.AddRange(data.GravityWaterBuckets.Select(ConvertVerticalPipesF()));
            o._87WaterBuckets.AddRange(data._87WaterBuckets.Select(ConvertVerticalPipesF()));
            o.Ditches.AddRange(data.Ditches.Select(ConvertVerticalPipesF()));
            o.AiringMachine_Hanging.AddRange(data.AiringMachine_Hanging.Select(ConvertVerticalPipesF()));
            o.AiringMachine_Vertical.AddRange(data.AiringMachine_Vertical.Select(ConvertVerticalPipesF()));
            return o;
        }
        private static Func<GRect, Polygon> ConvertWrappingPipesF()
        {
            return x => x.ToPolygon();
        }
        private static Func<GLineSegment, Geometry> ConvertLabelLinesF(int bfSize)
        {
            return x => x.Buffer(bfSize);
        }
        public static Func<Point2d, Point> ConvertSideFloorDrains()
        {
            return x => x.ToNTSPoint();
        }
        public static Func<Point2d, Polygon> ConvertCleaningPortsF()
        {
            return x => new GCircle(x, THESAURUSSENILE).ToCirclePolygon(THESAURUSSOMETIMES);
        }
        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(ELECTROMYOGRAPH).ToCirclePolygon(QUOTATIONLENTIFORM);
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
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(THESAURUSSOMETIMES);
        }
        public static Func<GLineSegment, LineString> ConvertVLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GLineSegment, LineString> ConvertDLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GLineSegment, LineString> ConvertWLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GLineSegment, LineString> ConvertLabelLinesF()
        {
            return x => x.Extend(THESAURUSEMBASSY).ToLineString();
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(PHOTOSENSITIZING);
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
            ret.AddRange(RainPortSymbols);
            ret.AddRange(WaterSealingWells);
            ret.AddRange(SideWaterBuckets);
            ret.AddRange(GravityWaterBuckets);
            ret.AddRange(_87WaterBuckets);
            ret.AddRange(Ditches);
            ret.AddRange(AiringMachine_Hanging);
            ret.AddRange(AiringMachine_Vertical);
            return ret;
        }
        public List<RainCadData> SplitByStorey()
        {
            var lst = new List<RainCadData>(this.Storeys.Count);
            if (this.Storeys.Count == NARCOTRAFICANTE) return lst;
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
                o.RainPortSymbols.AddRange(objs.Where(x => this.RainPortSymbols.Contains(x)));
                o.WaterSealingWells.AddRange(objs.Where(x => this.WaterSealingWells.Contains(x)));
                o.SideWaterBuckets.AddRange(objs.Where(x => this.SideWaterBuckets.Contains(x)));
                o.GravityWaterBuckets.AddRange(objs.Where(x => this.GravityWaterBuckets.Contains(x)));
                o._87WaterBuckets.AddRange(objs.Where(x => this._87WaterBuckets.Contains(x)));
                o.Ditches.AddRange(objs.Where(x => this.Ditches.Contains(x)));
                o.AiringMachine_Hanging.AddRange(objs.Where(x => this.AiringMachine_Hanging.Contains(x)));
                o.AiringMachine_Vertical.AddRange(objs.Where(x => this.AiringMachine_Vertical.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public RainCadData Clone()
        {
            return (RainCadData)MemberwiseClone();
        }
    }
    public class ThRainSystemServiceGeoCollector3
    {
        public AcadDatabase adb;
        public RainGeoData geoData;
        bool isInXref;
        static bool HandleGroupAtCurrentModelSpaceOnly = UNTRACEABLENESS;
        public void CollectEntities()
        {
            {
                var dict = ThMEPWSS.ViewModel.BlockConfigService.GetBlockNameListDict();
                dict.TryGetValue(THESAURUSLETTERED, out List<string> lstVertical);
                if (lstVertical != null) this.VerticalAiringMachineNames = new HashSet<string>(lstVertical);
                dict.TryGetValue(DISTEMPEREDNESS, out List<string> lstHanging);
                if (lstHanging != null) this.HangingAiringMachineNames = new HashSet<string>(lstHanging);
                this.VerticalAiringMachineNames ??= new HashSet<string>();
                this.HangingAiringMachineNames ??= new HashSet<string>();
            }
            if (HandleGroupAtCurrentModelSpaceOnly)
            {
                foreach (var g in adb.Groups)
                {
                    var lst = new List<GLineSegment>();
                    foreach (var id in g.GetAllEntityIds())
                    {
                        var entity = adb.Element<Entity>(id);
                        var dxfName = entity.GetRXClass().DxfName.ToUpper();
                        if (dxfName is PALAEOPATHOLOGIST && GetEffectiveLayer(entity.Layer) is THESAURUSCOMMOTION)
                        {
                            dynamic o = entity;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                            lst.Add(seg);
                        }
                    }
                    geoData.WLines.AddRange(TempGeoFac.GetMinConnSegs(lst.Where(x => x.IsValid).Distinct().ToList()));
                }
            }
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                if (entity is BlockReference br)
                {
                    if (!br.BlockTableRecord.IsValid) continue;
                    var btr = adb.Blocks.Element(br.BlockTableRecord);
                    var _fs = new List<KeyValuePair<Geometry, Action>>();
                    Action f = null;
                    try
                    {
                        isInXref = btr.XrefStatus != XrefStatus.NotAnXref;
                        handleBlockReference(br, Matrix3d.Identity, _fs);
                    }
                    finally
                    {
                        isInXref = UNTRACEABLENESS;
                    }
                    {
                        var info = br.XClipInfo();
                        if (info.IsValid)
                        {
                            info.TransformBy(br.BlockTransform);
                            var gf = info.PreparedPolygon;
                            foreach (var kv in _fs)
                            {
                                if (gf.Intersects(kv.Key))
                                {
                                    f += kv.Value;
                                }
                            }
                        }
                        else
                        {
                            foreach (var kv in _fs)
                            {
                                f += kv.Value;
                            }
                        }
                        f?.Invoke();
                    }
                }
                else
                {
                    var _fs = new List<KeyValuePair<Geometry, Action>>();
                    handleEntity(entity, Matrix3d.Identity, _fs);
                    foreach (var kv in _fs)
                    {
                        kv.Value();
                    }
                }
            }
        }
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<GLineSegment> wLines => geoData.WLines;
        List<CText> cts => geoData.Labels;
        List<GRect> waterWells => geoData.WaterWells;
        List<GRect> waterSealingWells => geoData.WaterSealingWells;
        List<string> waterWellLabels => geoData.WaterWellLabels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> condensePipes => geoData.CondensePipes;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        List<GRect> washingMachines => geoData.CondensePipes;
        List<GRect> pipeKillers => geoData.PipeKillers;
        List<GRect> sideWaterBuckets => geoData.SideWaterBuckets;
        List<GRect> gravityWaterBuckets => geoData.GravityWaterBuckets;
        List<GRect> _87WaterBuckets => geoData._87WaterBuckets;
        List<GRect> rainPortSymbols => geoData.RainPortSymbols;
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        public void CollectStoreys(CommandContext ctx)
        {
            foreach (var br in GetStoreyBlockReferences(adb))
            {
                var bd = br.Bounds.ToGRect();
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        public void CollectStoreys(Point3dCollection range)
        {
            var geo = range?.ToGRect().ToPolygon();
            foreach (var br in GetStoreyBlockReferences(adb))
            {
                var bd = br.Bounds.ToGRect();
                if (geo != null)
                {
                    if (!geo.Contains(bd.ToPolygon()))
                    {
                        continue;
                    }
                }
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        const int distinguishDiameter = THESAURUSGOBLIN;
        private static bool IsWantedBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return UNTRACEABLENESS;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return UNTRACEABLENESS;
            }
            if (!blockTableRecord.Explodable)
            {
                return UNTRACEABLENESS;
            }
            return THESAURUSSEMBLANCE;
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, List<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, List<GLineSegment> lst)
        {
            if (seg.IsValid) reg(fs, seg, () => { lst.Add(seg); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, List<GRect> lst)
        {
            if (r.IsValid) reg(fs, r, () => { lst.Add(r); });
        }
        static bool isRainLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSPERFECT);
        HashSet<Handle> ok_group_handles;
        private void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!IsLayerVisible(entity)) return;
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            if (isInXref)
            {
                return;
            }
            var entityLayer = entity.Layer;
            entityLayer = GetEffectiveLayer(entityLayer);
            if (!HandleGroupAtCurrentModelSpaceOnly)
            {
                ok_group_handles ??= new HashSet<Handle>();
                var groups = entity.GetPersistentReactorIds().OfType<ObjectId>()
                    .SelectNotNull(id => adb.ElementOrDefault<DBObject>(id)).OfType<Autodesk.AutoCAD.DatabaseServices.Group>().ToList();
                foreach (var g in groups)
                {
                    if (ok_group_handles.Contains(g.Handle)) continue;
                    ok_group_handles.Add(g.Handle);
                    var lst = new List<GLineSegment>();
                    foreach (var id in g.GetAllEntityIds())
                    {
                        var e = adb.Element<Entity>(id);
                        var _dxfName = e.GetRXClass().DxfName.ToUpper();
                        if (_dxfName is PALAEOPATHOLOGIST && GetEffectiveLayer(e.Layer) is THESAURUSCOMMOTION)
                        {
                            dynamic o = e;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                            lst.Add(seg);
                        }
                    }
                    lst = lst.Where(x => x.IsValid).Distinct().ToList();
                    geoData.WLines.AddRange(TempGeoFac.GetMinConnSegs(lst));
                }
            }
            {
                if (entityLayer is THESAURUSACCEDE or THESAURUSPRELIMINARY)
                {
                    if (entity is Line line)
                    {
                        if (line.Length > NARCOTRAFICANTE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, geoData.WrappingPipeLabelLines);
                        }
                        return;
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            if (ln.Length > NARCOTRAFICANTE)
                            {
                                var seg = ln.ToGLineSegment().TransformBy(matrix);
                                reg(fs, seg, geoData.WrappingPipeLabelLines);
                            }
                        }
                        return;
                    }
                    else if (entity is DBText dbt)
                    {
                        var text = dbt.TextString;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = text, Boundary = bd };
                            reg(fs, ct, geoData.WrappingPipeLabels);
                        }
                        return;
                    }
                }
            }
            {
                if (entityLayer is TRANYLCYPROMINE)
                {
                    if (entity is Spline)
                    {
                        var bd = entity.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, rainPortSymbols);
                        return;
                    }
                }
            }
            {
                if (entityLayer is THESAURUSCOMMOTION)
                {
                    if (entity is Line line && line.Length > NARCOTRAFICANTE)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, wLines);
                        return;
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            var seg = ln.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, wLines);
                        }
                        return;
                    }
                }
            }
            {
                if (isRainLayer(entityLayer))
                {
                    if (entity is Line line && line.Length > NARCOTRAFICANTE)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, labelLines);
                        return;
                    }
                    else if (entity is DBText dbt && !string.IsNullOrWhiteSpace(dbt.TextString))
                    {
                        var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                        var ct = new CText()
                        {
                            Text = dbt.TextString,
                            Boundary = bd,
                        };
                        reg(fs, ct, cts);
                        return;
                    }
                    else if (entity is Circle c)
                    {
                        if (distinguishDiameter <= c.Radius && c.Radius <= THESAURUSINDUSTRY)
                        {
                            if (isRainLayer(c.Layer))
                            {
                                var r = c.Bounds.ToGRect().TransformBy(matrix);
                                reg(fs, r, pipes);
                                return;
                            }
                        }
                        else if (c.Layer is THESAURUSSANCTITY && STEPMOTHERLINESS < c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                    else if (dxfName is PALAEOPATHOLOGIST && entityLayer is THESAURUSSANCTITY)
                    {
                        var lines = entity.ExplodeToDBObjectCollection().OfType<Line>().Where(x => x.Length > THESAURUSINDUSTRY).ToList();
                        if (lines.Count > NARCOTRAFICANTE)
                        {
                            foreach (var ln in lines)
                            {
                                var seg = ln.ToGLineSegment().TransformBy(matrix);
                                reg(fs, seg, labelLines);
                            }
                        }
                    }
                }
            }
            if (dxfName == THESAURUSEGOTISM)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSHEARTLESS + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>().Where(IsLayerVisible))
                {
                    if (e is Line line)
                    {
                        if (line.Length > NARCOTRAFICANTE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, labelLines);
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSSHAMBLE)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
                        continue;
                    }
                }
                if (ts.Count > NARCOTRAFICANTE)
                {
                    GRect bd;
                    if (ts.Count == ADRENOCORTICOTROPHIC) bd = ts[NARCOTRAFICANTE].Bounds.ToGRect();
                    else
                    {
                        bd = GeoFac.CreateGeometry(ts.Select(x => x.Bounds.ToGRect()).Where(x => x.IsValid).Select(x => x.ToPolygon())).EnvelopeInternal.ToGRect();
                    }
                    bd = bd.TransformBy(matrix);
                    var ct = new CText() { Text = text, Boundary = bd };
                    reg(fs, ct, cts);
                }
                return;
            }
            else if (dxfName == PALAEOPATHOLOGIST)
            {
                if (entityLayer is THESAURUSSANCTITY)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                    {
                        if (c.Radius >= distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, pipes);
                        }
                        else if (STEPMOTHERLINESS <= c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                }
                if (entityLayer is THESAURUSCOMMOTION)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wLines);
                    return;
                }
            }
            else if (dxfName == THESAURUSSUPERVISE && entityLayer is TRANYLCYPROMINE)
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                reg(fs, r, rainPortSymbols);
            }
            else if (dxfName is THESAURUSSHAMBLE)
            {
                if (!isRainLayer(entityLayer)) return;
                dynamic o = entity.AcadObject;
                string text = o.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    var ct = new CText() { Text = text, Boundary = r };
                    reg(fs, ct, cts);
                    return;
                }
            }
            else if (dxfName is INHOMOGENEOUSLY)
            {
                foreach (var dbText in entity.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                {
                    var text = dbText.TextString;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var bd = dbText.Bounds.ToGRect().TransformBy(matrix);
                        var ct = new CText() { Text = text, Boundary = bd };
                        reg(fs, ct, cts);
                    }
                }
            }
            else if (dxfName == THESAURUSINOFFENSIVE)
            {
                if (entityLayer is THESAURUSACCEDE or THESAURUSPRELIMINARY)
                {
                    dynamic o = entity.AcadObject;
                    string UpText = o.UpText;
                    string DownText = o.DownText;
                    var ents = entity.ExplodeToDBObjectCollection();
                    var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                    var points = GeoFac.GetAlivePoints(segs, ADRENOCORTICOTROPHIC);
                    var pts = points.Select(x => x.ToNTSPoint()).ToList();
                    points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(ADRENOCORTICOTROPHIC)).Select(x => x.Extend(PHOTOGONIOMETER).Buffer(ADRENOCORTICOTROPHIC)).ToList())).Select(pts).ToList(points)).ToList();
                    foreach (var pt in points)
                    {
                        var t = TryParseWrappingPipeRadiusText(DownText);
                        if (!string.IsNullOrWhiteSpace(t))
                        {
                            wrappingPipeRadius.Add(new KeyValuePair<Point2d, string>(pt, t));
                        }
                    }
                    return;
                }
                if (isRainLayer(entityLayer))
                {
                    dynamic o = entity.AcadObject;
                    string UpText = o.UpText;
                    string DownText = o.DownText;
                    var t = (UpText + DownText) ?? THESAURUSREDOUND;
                    if (t.Contains(THESAURUSCAPRICIOUS) && t.Contains(NANOPHANEROPHYTE))
                    {
                        var ents = entity.ExplodeToDBObjectCollection();
                        var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                        var points = GeoFac.GetAlivePoints(segs, ADRENOCORTICOTROPHIC);
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(ADRENOCORTICOTROPHIC)).Select(x => x.Extend(PHOTOGONIOMETER).Buffer(ADRENOCORTICOTROPHIC)).ToList())).Select(pts).ToList(points)).ToList();
                        if (points.Count > NARCOTRAFICANTE)
                        {
                            var r = new MultiPoint(points.Select(p => p.ToNTSPoint()).ToArray()).Envelope.ToGRect().Expand(THESAURUSINDUSTRY);
                            geoData.Ditches.Add(r);
                        }
                        return;
                    }
                    if (t.Contains(THESAURUSCAPRICIOUS) && t.Contains(POLYSOMNOGRAPHY))
                    {
                        var ents = entity.ExplodeToDBObjectCollection();
                        var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                        var points = GeoFac.GetAlivePoints(segs, ADRENOCORTICOTROPHIC);
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(ADRENOCORTICOTROPHIC)).Select(x => x.Extend(PHOTOGONIOMETER).Buffer(ADRENOCORTICOTROPHIC)).ToList())).Select(pts).ToList(points)).ToList();
                        if (points.Count > NARCOTRAFICANTE)
                        {
                            foreach (var pt in points)
                            {
                                geoData.RainPortSymbols.Add(GRect.Create(pt, UNDERACHIEVEMENT));
                            }
                        }
                        return;
                    }
                }
                {
                    if (!isRainLayer(entityLayer)) return;
                    var colle = entity.ExplodeToDBObjectCollection();
                    {
                        foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSSHAMBLE or INHOMOGENEOUSLY).Where(x => isRainLayer(x.Layer)).Where(IsLayerVisible))
                        {
                            foreach (var dbText in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                            {
                                var text = dbText.TextString;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    var bd = dbText.Bounds.ToGRect().TransformBy(matrix);
                                    var ct = new CText() { Text = text, Boundary = bd };
                                    reg(fs, ct, cts);
                                }
                            }
                        }
                        foreach (var seg in colle.OfType<Line>().Where(x => x.Length > NARCOTRAFICANTE).Where(x => isRainLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                        {
                            reg(fs, seg, labelLines);
                        }
                    }
                }
                return;
            }
        }
        public static string GetEffectiveLayer(string entityLayer)
        {
            return GetEffectiveName(entityLayer);
        }
        public static string GetEffectiveName(string str)
        {
            str ??= THESAURUSREDOUND;
            var i = str.LastIndexOf(THESAURUSSUFFICE);
            if (i >= NARCOTRAFICANTE && !str.EndsWith(THESAURUSRANSACK))
            {
                str = str.Substring(i + ADRENOCORTICOTROPHIC);
            }
            i = str.LastIndexOf(POLYCRYSTALLINE);
            if (i >= NARCOTRAFICANTE && !str.EndsWith(HENDECASYLLABUS))
            {
                str = str.Substring(i + ADRENOCORTICOTROPHIC);
            }
            return str;
        }
        public static string GetEffectiveBRName(string brName)
        {
            return GetEffectiveName(brName);
        }
        HashSet<string> VerticalAiringMachineNames;
        HashSet<string> HangingAiringMachineNames;
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var _name = br.GetEffectiveName();
                var name = GetEffectiveBRName(_name);
                if (name is THERMODYNAMICIST or KNICKERBOCKERED || VerticalAiringMachineNames.Contains(name))
                {
                    reg(fs, br.Bounds.ToGRect().TransformBy(matrix), geoData.AiringMachine_Vertical);
                    return;
                }
                if (name is MACHAIRODONTINAE || HangingAiringMachineNames.Contains(name))
                {
                    reg(fs, br.Bounds.ToGRect().TransformBy(matrix), geoData.AiringMachine_Hanging);
                    return;
                }
                if (name.Contains(INCOMPATIBILITY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    waterSealingWells.Add(bd);
                    return;
                }
                if (name.Contains(UNPALATABLENESS) || name.Contains(QUOTATIONNOXIOUS))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSHEARTLESS) ?? THESAURUSREDOUND;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(lb);
                    });
                    return;
                }
                if (name.ToUpper() is PSYCHOPROPHYLAXIS || name.ToUpper().EndsWith(THESAURUSSTUDENT))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.ToUpper() is NEUROPHYSIOLOGY or THESAURUSCUSTOM || name.ToUpper().EndsWith(MICROMANIPULATIONS) || name.ToUpper().EndsWith(CIRCUMCONVOLUTION))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is THESAURUSPROSPECTUS)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is THESAURUSEJECTION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.Contains(UNIDIOMATICALLY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, _87WaterBuckets);
                    return;
                }
                if (!isInXref)
                {
                    if ((name.Contains(THESAURUSBACTERIA) || name is INAPPREHENSIBILIS) && _name.Count(x => x == POLYCRYSTALLINE) < PHOTOGONIOMETER)
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, floorDrains);
                        if (Regex.IsMatch(name, THESAURUSEXHUME, RegexOptions.Compiled))
                        {
                            if (br.IsDynamicBlock)
                            {
                                var props = br.DynamicBlockReferencePropertyCollection;
                                foreach (DynamicBlockReferenceProperty prop in props)
                                {
                                    if (prop.PropertyName == CRYSTALLIZATIONS)
                                    {
                                        var propValue = prop.Value.ToString();
                                        {
                                            if (propValue is THESAURUSUNCOUTH)
                                            {
                                                var center = bd.Center;
                                                geoData.SideFloorDrains.Add(center);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        return;
                    }
                }
                if (name.Contains(THESAURUSPRESENTABLE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < QUOTATIONTRILINEAR && bd.Height < QUOTATIONTRILINEAR)
                        {
                            reg(fs, bd, wrappingPipes);
                        }
                    }
                    return;
                }
                if (isRainLayer(br.Layer))
                {
                    if (name is THESAURUSUNITED or THESAURUSWRONGFUL)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), UNDERACHIEVEMENT);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name.Contains(ENTERCOMMUNICATE))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is COSTERMONGERING || name.Contains(COSTERMONGERING))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                }
            }
            var btr = adb.Element<BlockTableRecord>(br.BlockTableRecord);
            if (!IsWantedBlock(btr)) return;
            var _fs = new List<KeyValuePair<Geometry, Action>>();
            foreach (var objId in btr)
            {
                var dbObj = adb.Element<Entity>(objId);
                if (dbObj is BlockReference b)
                {
                    handleBlockReference(b, br.BlockTransform.PreMultiplyBy(matrix), _fs);
                }
                else
                {
                    handleEntity(dbObj, br.BlockTransform.PreMultiplyBy(matrix), _fs);
                }
            }
            {
                var lst = new List<KeyValuePair<Geometry, Action>>();
                var info = br.XClipInfo();
                if (info.IsValid)
                {
                    info.TransformBy(br.BlockTransform.PreMultiplyBy(matrix));
                    var gf = info.PreparedPolygon;
                    foreach (var kv in _fs)
                    {
                        if (gf.Intersects(kv.Key))
                        {
                            lst.Add(kv);
                        }
                    }
                }
                else
                {
                    foreach (var kv in _fs)
                    {
                        lst.Add(kv);
                    }
                }
                fs.AddRange(lst);
            }
        }
    }
    public class ThRainSystemServiceGeoCollector2
    {
        public AcadDatabase adb;
        public RainGeoData geoData;
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
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        List<GRect> washingMachines => geoData.CondensePipes;
        List<GRect> pipeKillers => geoData.PipeKillers;
        List<GRect> sideWaterBuckets => geoData.SideWaterBuckets;
        List<GRect> gravityWaterBuckets => geoData.GravityWaterBuckets;
        List<GRect> _87WaterBuckets => geoData._87WaterBuckets;
        List<GRect> rainPortSymbols => geoData.RainPortSymbols;
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        public void CollectStoreys(Point3dCollection range)
        {
            var geo = range?.ToGRect().ToPolygon();
            foreach (var br in GetStoreyBlockReferences(adb))
            {
                var bd = br.Bounds.ToGRect();
                if (geo != null)
                {
                    if (!geo.Contains(bd.ToPolygon()))
                    {
                        continue;
                    }
                }
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        void handleBlockReference(BlockReference br, Matrix3d matrix)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            {
            }
            {
                var name = br.GetEffectiveName();
                if (name.Contains(UNPALATABLENESS) || name.Contains(THESAURUSSTUPEFACTION) || name.Contains(QUOTATIONNOXIOUS))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(br.GetAttributesStrValue(THESAURUSHEARTLESS) ?? THESAURUSREDOUND);
                        return;
                    }
                }
                if (ThMEPEngineCore.Service.ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        gravityWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name is THESAURUSPROSPECTUS)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        sideWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name is THESAURUSEJECTION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        gravityWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name.Contains(UNIDIOMATICALLY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        _87WaterBuckets.Add(bd);
                        return;
                    }
                }
                if (ThMEPEngineCore.Service.ThSideEntryWaterBucketLayerManager.IsSideEntryWaterBucketBlockName(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        sideWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name.Contains(THESAURUSBACTERIA) || name is INAPPREHENSIBILIS)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        floorDrains.Add(bd);
                    }
                    return;
                }
                if (name.Contains(THESAURUSPRESENTABLE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < QUOTATIONTRILINEAR && bd.Height < QUOTATIONTRILINEAR)
                        {
                            wrappingPipes.Add(bd);
                        }
                    }
                    return;
                }
                {
                    if (name is THESAURUSUNITED or THESAURUSWRONGFUL)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), UNDERACHIEVEMENT);
                        pipes.Add(bd);
                        return;
                    }
                    if (name.Contains(ENTERCOMMUNICATE))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        if (bd.IsValid) pipes.Add(bd);
                        return;
                    }
                }
            }
            var btr = adb.Element<BlockTableRecord>(br.BlockTableRecord);
            if (!IsBuildElementBlock(btr)) return;
            foreach (var objId in btr)
            {
                var dbObj = adb.Element<Entity>(objId);
                if (dbObj is BlockReference b)
                {
                    {
                        handleBlockReference(b, br.BlockTransform.PreMultiplyBy(matrix));
                    }
                }
                else
                {
                    handleEntity(dbObj, br.BlockTransform.PreMultiplyBy(matrix));
                }
            }
        }
        const int distinguishDiameter = THESAURUSGOBLIN;
        static bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return UNTRACEABLENESS;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return UNTRACEABLENESS;
            }
            if (!blockTableRecord.Explodable)
            {
                return UNTRACEABLENESS;
            }
            return THESAURUSSEMBLANCE;
        }
        void handleEntity(Entity entity, Matrix3d matrix)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            {
                if (entity.Layer is THESAURUSCOMMOTION)
                {
                    if (entity is Line line && line.Length > NARCOTRAFICANTE)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        if (seg.IsValid)
                        {
                            wLines.Add(seg);
                        }
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            var seg = ln.ToGLineSegment().TransformBy(matrix);
                            if (seg.IsValid)
                            {
                                wLines.Add(seg);
                            }
                        }
                    }
                }
                else if (entity.Layer is THESAURUSOVERWHELM)
                {
                    if (entity is Line line && line.Length > NARCOTRAFICANTE)
                    {
                        var seg = line.ToGLineSegment().TransformBy(matrix);
                        if (seg.IsValid)
                        {
                            vlines.Add(seg);
                        }
                    }
                    else if (entity is Polyline pl)
                    {
                        foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                        {
                            var seg = ln.ToGLineSegment().TransformBy(matrix);
                            if (seg.IsValid)
                            {
                                vlines.Add(seg);
                            }
                        }
                    }
                }
            }
            {
                static bool isRainLayer(string layer) => layer is VERGELTUNGSWAFFE or THESAURUSSANCTITY or TRANYLCYPROMINE;
                if (isRainLayer(entity.Layer))
                {
                    if (entity is Line line && line.Length > NARCOTRAFICANTE)
                    {
                        labelLines.Add(line.ToGLineSegment().TransformBy(matrix));
                        return;
                    }
                    else if (entity is DBText dbt && !string.IsNullOrWhiteSpace(dbt.TextString))
                    {
                        var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                        if (bd.IsValid)
                        {
                            cts.Add(new CText()
                            {
                                Text = dbt.TextString,
                                Boundary = bd,
                            });
                            return;
                        }
                    }
                    else if (entity is Circle c)
                    {
                        if (distinguishDiameter <= c.Radius && c.Radius <= THESAURUSINDUSTRY)
                        {
                            if (isRainLayer(c.Layer))
                            {
                                var r = c.Bounds.ToGRect().TransformBy(matrix);
                                if (r.IsValid)
                                {
                                    pipes.Add(r);
                                    return;
                                }
                            }
                        }
                        else if (c.Layer is THESAURUSSANCTITY && STEPMOTHERLINESS < c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            if (r.IsValid)
                            {
                                condensePipes.Add(r);
                                return;
                            }
                        }
                    }
                }
            }
            if (dxfName == THESAURUSEGOTISM)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSHEARTLESS + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>())
                {
                    if (e is Line line)
                    {
                        if (line.Length > NARCOTRAFICANTE)
                        {
                            labelLines.Add(line.ToGLineSegment().TransformBy(matrix));
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSSHAMBLE)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>());
                        continue;
                    }
                }
                if (ts.Count > NARCOTRAFICANTE)
                {
                    GRect bd;
                    if (ts.Count == ADRENOCORTICOTROPHIC) bd = ts[NARCOTRAFICANTE].Bounds.ToGRect();
                    else
                    {
                        bd = GeoFac.CreateGeometry(ts.Select(x => x.Bounds.ToGRect()).Where(x => x.IsValid).Select(x => x.ToPolygon())).EnvelopeInternal.ToGRect();
                    }
                    bd = bd.TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        cts.Add(new CText() { Text = text, Boundary = bd });
                    }
                }
                return;
            }
            if (dxfName == PALAEOPATHOLOGIST)
            {
                if (entity.Layer is THESAURUSSANCTITY)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>())
                    {
                        if (c.Radius > distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            if (bd.IsValid) pipes.Add(bd);
                        }
                    }
                }
                else if (entity.Layer is THESAURUSCOMMOTION)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    if (seg.IsValid)
                    {
                        wLines.Add(seg);
                    }
                }
                return;
            }
            if (dxfName == THESAURUSSUPERVISE)
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                if (r.IsValid)
                {
                    rainPortSymbols.Add(r);
                }
                return;
            }
            if (dxfName == THESAURUSSHAMBLE)
            {
                dynamic o = entity.AcadObject;
                string text = o.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    if (r.IsValid)
                    {
                        cts.Add(new CText() { Text = text, Boundary = r });
                    }
                }
                return;
            }
            if (dxfName == THESAURUSINOFFENSIVE)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var ee in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSSHAMBLE or INHOMOGENEOUSLY))
                    {
                        foreach (var dbText in ee.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)))
                        {
                            var bd = dbText.Bounds.ToGRect().TransformBy(matrix);
                            if (bd.IsValid)
                            {
                                cts.Add(new CText() { Text = dbText.TextString, Boundary = bd });
                            }
                        }
                    }
                    labelLines.AddRange(colle.OfType<Line>().Where(x => x.Length > NARCOTRAFICANTE).Select(x => x.ToGLineSegment().TransformBy(matrix)));
                }
                return;
            }
        }
        public void CollectEntities()
        {
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                if (entity is BlockReference br)
                {
                    handleBlockReference(br, Matrix3d.Identity);
                }
                else
                {
                    handleEntity(entity, Matrix3d.Identity);
                }
            }
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
                return THESAURUSIDENTICAL + label;
            }
            if (IsFL(label))
            {
                return QUOTATIONIMPERIUM + label;
            }
            return label;
        }
        public static int GetStoreyScore(string label)
        {
            if (label == null) return NARCOTRAFICANTE;
            switch (label)
            {
                case THESAURUSADHERE: return ushort.MaxValue;
                case IMMUNOGENETICALLY: return ushort.MaxValue + ADRENOCORTICOTROPHIC;
                case THESAURUSNATURALIST: return ushort.MaxValue + PHOTOGONIOMETER;
                default:
                    {
                        int.TryParse(label.Replace(QUOTATIONHOUSEMAID, THESAURUSREDOUND), out int ret);
                        return ret;
                    }
            }
        }
        public static (ExtraInfo, bool) CollectRainData(Point3dCollection range, AcadDatabase adb, out List<StoreyInfo> storeysItems, out List<RainDrawingData> drDatas)
        {
            CollectRainGeoData(range, adb, out storeysItems, out RainGeoData geoData);
            return CreateRainDrawingData(adb, out drDatas, geoData);
        }
        public static (ExtraInfo, bool) CollectRainData(AcadDatabase adb, out List<StoreysItem> storeysItems, out List<RainDrawingData> drDatas, CommandContext ctx)
        {
            CollectRainGeoData(adb, out storeysItems, out RainGeoData geoData, ctx);
            return CreateRainDrawingData(adb, out drDatas, geoData);
        }
        public static (ExtraInfo, bool) CreateRainDrawingData(AcadDatabase adb, out List<RainDrawingData> drDatas, RainGeoData geoData)
        {
            ThRainService.PreFixGeoData(geoData);
            var (_drDatas, exInfo) = _CreateRainDrawingData(adb, geoData, THESAURUSSEMBLANCE);
            drDatas = _drDatas;
            return (exInfo, THESAURUSSEMBLANCE);
        }
        public static (List<RainDrawingData>, ExtraInfo) CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.PreFixGeoData(geoData);
            ThRainService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            GetCadDatas(geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas);
            var roomData = RainService.CollectRoomData(adb);
            var exInfo = RainService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out List<RainDrawingData> drDatas, roomData);
            if (noDraw) Dispose();
            return (drDatas, exInfo);
        }
        private static (List<RainDrawingData>, ExtraInfo) _CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            GetCadDatas(geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas);
            var roomData = RainService.CollectRoomData(adb);
            var exInfo = RainService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out List<RainDrawingData> drDatas, roomData);
            if (noDraw) Dispose();
            return (drDatas, exInfo);
        }
        public static void GetCadDatas(RainGeoData geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas)
        {
            cadDataMain = RainCadData.Create(geoData);
            cadDatas = cadDataMain.SplitByStorey();
        }
        public static List<StoreyInfo> GetStoreys(AcadDatabase adb, CommandContext ctx)
        {
            return ctx.StoreyContext.StoreyInfos;
        }
        public static void CollectRainGeoData(AcadDatabase adb, out List<StoreysItem> storeysItems, out RainGeoData geoData, CommandContext ctx)
        {
            var storeys = GetStoreys(adb, ctx);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(adb, geoData, ctx);
        }
        public static List<StoreyInfo> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var geo = range?.ToGRect().ToPolygon();
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSSEMBLANCE).ToList();
            FixStoreys(storeys);
            return storeys;
        }
        public static void CollectRainGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreyInfo> storeys, out RainGeoData geoData)
        {
            storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(range, adb, geoData);
        }
        public static List<StoreysItem> GetStoreysItem(List<StoreyInfo> thStoreys)
        {
            var storeysItems = new List<StoreysItem>();
            foreach (var s in thStoreys)
            {
                var item = new StoreysItem();
                storeysItems.Add(item);
                switch (s.StoreyType)
                {
                    case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                        {
                            item.Labels = new List<string>() { THESAURUSADHERE };
                        }
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                    case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                        {
                            item.Ints = s.Numbers.OrderBy(x => x).ToList();
                            item.Labels = item.Ints.Select(x => x + QUOTATIONHOUSEMAID).ToList();
                        }
                        break;
                    default:
                        break;
                }
            }
            return storeysItems;
        }
        public static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }
        public static void SwapBy2DSpace<T>(ref T v1, ref T v2, GRect bd1, GRect bd2)
        {
            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
            if (deltaY > deltaX)
            {
                if (bd2.MaxY < bd1.MaxY)
                {
                    Swap(ref v1, ref v2);
                }
            }
            else
            {
                if (bd2.MinX < bd1.MinX)
                {
                    Swap(ref v1, ref v2);
                }
            }
        }
        public class ThwSDStoreyItem
        {
            public string Storey;
            public GRect Boundary;
            public List<string> VerticalPipes;
        }
        public static bool IsGravityWaterBucketDNText(string text)
        {
            return re.IsMatch(text);
        }
        static readonly Regex re = new Regex(PALAEONTOLOGICAL);
        public static IEnumerable<KeyValuePair<string, string>> EnumerateSmallRoofVerticalPipes(List<ThwSDStoreyItem> wsdStoreys, string vpipe)
        {
            foreach (var s in wsdStoreys)
            {
                if (s.Storey == THESAURUSNATURALIST || s.Storey == IMMUNOGENETICALLY)
                {
                    if (s.VerticalPipes.Contains(vpipe))
                    {
                        yield return new KeyValuePair<string, string>(s.Storey, vpipe);
                    }
                }
            }
        }
        public static List<ThwSDStoreyItem> CollectStoreys(List<StoreyInfo> thStoreys, List<RainDrawingData> drDatas)
        {
            var wsdStoreys = new List<ThwSDStoreyItem>();
            HashSet<string> GetVerticalPipeNotes(StoreyInfo storey)
            {
                var i = thStoreys.IndexOf(storey);
                if (i < NARCOTRAFICANTE) return new HashSet<string>();
                return new HashSet<string>(drDatas[i].VerticalPipeLabels.Where(IsRainLabel));
            }
            {
                var largeRoofVPTexts = new HashSet<string>();
                foreach (var storey in thStoreys)
                {
                    var bd = storey.Boundary;
                    switch (storey.StoreyType)
                    {
                        case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                            {
                                var vps = GetVerticalPipeNotes(storey).ToList();
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSADHERE, Boundary = bd, VerticalPipes = vps });
                                largeRoofVPTexts.AddRange(vps);
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Numbers.ForEach(i => wsdStoreys.Add(new ThwSDStoreyItem() { Storey = i + QUOTATIONHOUSEMAID, Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = thStoreys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == ADRENOCORTICOTROPHIC)
                    {
                        var storey = storeys[NARCOTRAFICANTE];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        {
                            var rf2vps = smallRoofVPTexts.Except(largeRoofVPTexts).ToList();
                            if (rf2vps.Count == NARCOTRAFICANTE)
                            {
                                var rf1Storey = new ThwSDStoreyItem() { Storey = IMMUNOGENETICALLY, Boundary = bd, };
                                wsdStoreys.Add(rf1Storey);
                            }
                            else
                            {
                                var rf1vps = smallRoofVPTexts.Except(rf2vps).ToList();
                                var rf1Storey = new ThwSDStoreyItem() { Storey = IMMUNOGENETICALLY, Boundary = bd, VerticalPipes = rf1vps };
                                wsdStoreys.Add(rf1Storey);
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSNATURALIST, Boundary = bd, VerticalPipes = rf2vps });
                            }
                        }
                    }
                    else if (storeys.Count == PHOTOGONIOMETER)
                    {
                        var s1 = storeys[NARCOTRAFICANTE];
                        var s2 = storeys[ADRENOCORTICOTROPHIC];
                        var bd1 = s1.Boundary;
                        var bd2 = s2.Boundary;
                        var vpts1 = GetVerticalPipeNotes(s1);
                        var vpts2 = GetVerticalPipeNotes(s2);
                        var vps1 = vpts1.ToList();
                        var vps2 = vpts2.ToList();
                        {
                            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
                            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
                            if (deltaY > deltaX)
                            {
                                if (bd2.MaxY < bd1.MaxY)
                                {
                                    Swap(ref bd1, ref bd2);
                                }
                            }
                            else
                            {
                                if (bd2.MinX < bd1.MinX)
                                {
                                    Swap(ref bd1, ref bd2);
                                }
                            }
                        }
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = IMMUNOGENETICALLY, Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSNATURALIST, Boundary = bd2, VerticalPipes = vps2 });
                    }
                }
            }
            return wsdStoreys;
        }
#pragma warning disable
        static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
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
                return NARCOTRAFICANTE;
            }
            public bool Equals(PipeCmpInfo other)
            {
                return this.IsWaterPortOutlet == other.IsWaterPortOutlet && PipeRuns.SequenceEqual(other.PipeRuns);
            }
        }
        public class WaterBucketInfo
        {
            public string WaterBucket;
            public string Pipe;
            public string Storey;
        }
        public static bool ShowWaterBucketHitting;
        public static List<RainGroupedPipeItem> GetRainGroupedPipeItems(List<RainDrawingData> drDatas, List<StoreyInfo> thStoreys, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo)
        {
            otherInfo = new OtherInfo()
            {
                AloneFloorDrainInfos = new List<AloneFloorDrainInfo>(),
            };
            var thwSDStoreys = RainDiagram.CollectStoreys(thStoreys, drDatas);
            var storeysItems = new List<StoreysItem>(drDatas.Count);
            for (int i = NARCOTRAFICANTE; i < drDatas.Count; i++)
            {
                var bd = drDatas[i].Boundary;
                var item = new StoreysItem();
                item.Init();
                foreach (var sd in thwSDStoreys)
                {
                    if (sd.Boundary.EqualsTo(bd, THESAURUSCONSUL))
                    {
                        item.Ints.Add(GetStoreyScore(sd.Storey));
                        item.Labels.Add(sd.Storey);
                    }
                }
                storeysItems.Add(item);
            }
            var alllabels = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels));
            var allY1L = alllabels.Where(x => IsY1L(x)).ToList();
            var allY2L = alllabels.Where(x => IsY2L(x)).ToList();
            var allNL = alllabels.Where(x => IsNL(x)).ToList();
            var allYL = alllabels.Where(x => IsYL(x)).ToList();
            var allFL0 = alllabels.Where(x => IsFL0(x)).ToList();
            var storeys = thwSDStoreys.Select(x => x.Storey).ToList();
            storeys = storeys.Distinct().OrderBy(GetStoreyScore).ToList();
            var minS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Where(x => x > NARCOTRAFICANTE).Min();
            var maxS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Where(x => x > NARCOTRAFICANTE).Max();
            var countS = maxS - minS + ADRENOCORTICOTROPHIC;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            var allNumStoreyLabels = allNumStoreys.Select(x => x + QUOTATIONHOUSEMAID).ToList();
            allRfStoreys = storeys.Where(x => !IsNumStorey(x)).OrderBy(GetStoreyScore).ToList();
            bool existStorey(string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return THESAURUSSEMBLANCE;
                }
                return UNTRACEABLENESS;
            }
            int getStoreyIndex(string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return i;
                }
                return -ADRENOCORTICOTROPHIC;
            }
            var waterBucketsInfos = new List<WaterBucketInfo>();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).OrderBy(GetStoreyScore).ToList();
            string getLowerStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i >= ADRENOCORTICOTROPHIC) return allStoreys[i - ADRENOCORTICOTROPHIC];
                return null;
            }
            string getHigherStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i < NARCOTRAFICANTE) return null;
                if (i + ADRENOCORTICOTROPHIC < allStoreys.Count) return allStoreys[i + ADRENOCORTICOTROPHIC];
                return null;
            }
            {
                var toCmp = new List<int>();
                for (int i = NARCOTRAFICANTE; i < allStoreys.Count - ADRENOCORTICOTROPHIC; i++)
                {
                    var s1 = allStoreys[i];
                    var s2 = allStoreys[i + ADRENOCORTICOTROPHIC];
                    if ((GetStoreyScore(s2) - GetStoreyScore(s1) == ADRENOCORTICOTROPHIC) || (GetStoreyScore(s1) == maxS && GetStoreyScore(s2) == GetStoreyScore(THESAURUSADHERE)))
                    {
                        toCmp.Add(i);
                    }
                }
                foreach (var j in toCmp)
                {
                    var storey = allStoreys[j];
                    var i = getStoreyIndex(storey);
                    if (i < NARCOTRAFICANTE) continue;
                    var _drData = drDatas[i];
                    var item = storeysItems[i];
                    var higherStorey = getHigherStorey(storey);
                    if (higherStorey == null) continue;
                    var i1 = getStoreyIndex(higherStorey);
                    if (i1 < NARCOTRAFICANTE) continue;
                    var drData = drDatas[i1];
                    var v = drData.ContraPoint - _drData.ContraPoint;
                    var bkExpand = THESAURUSALCOVE;
                    var gbks = drData.GravityWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var sbks = drData.SideWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var _87bks = drData._87WaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    if (ShowWaterBucketHitting)
                    {
                        foreach (var bk in gbks.Concat(sbks).Concat(_87bks))
                        {
                            DrawRectLazy(bk);
                            Dr.DrawSimpleLabel(bk.LeftTop, THESAURUSDEVOUR);
                        }
                    }
                    var gbkgeos = gbks.Select(x => x.ToPolygon()).ToList();
                    var sbkgeos = sbks.Select(x => x.ToPolygon()).ToList();
                    var _87bkgeos = _87bks.Select(x => x.ToPolygon()).ToList();
                    var gbksf = GeoFac.CreateIntersectsSelector(gbkgeos);
                    var sbksf = GeoFac.CreateIntersectsSelector(sbkgeos);
                    var _87bksf = GeoFac.CreateIntersectsSelector(_87bkgeos);
                    for (int k = NARCOTRAFICANTE; k < _drData.Y1LVerticalPipeRectLabels.Count; k++)
                    {
                        var label = _drData.Y1LVerticalPipeRectLabels[k];
                        var vp = _drData.Y1LVerticalPipeRects[k];
                        {
                            var _gbks = gbksf(vp.ToPolygon());
                            if (_gbks.Count > NARCOTRAFICANTE)
                            {
                                var bk = drData.GravityWaterBucketLabels[gbkgeos.IndexOf(_gbks[NARCOTRAFICANTE])];
                                bk ??= QUOTATIONDEFLAGRATING;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _sbks = sbksf(vp.ToPolygon());
                            if (_sbks.Count > NARCOTRAFICANTE)
                            {
                                var bk = drData.SideWaterBucketLabels[sbkgeos.IndexOf(_sbks[NARCOTRAFICANTE])];
                                bk ??= THESAURUSSUSTAINED;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _8bks = _87bksf(vp.ToPolygon());
                            if (_8bks.Count > NARCOTRAFICANTE)
                            {
                                var bk = drData._87WaterBucketLabels[_87bkgeos.IndexOf(_8bks[NARCOTRAFICANTE])];
                                bk ??= THESAURUSSLIPSHOD;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                    }
                }
            }
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Count == ADRENOCORTICOTROPHIC)
                    {
                        var storey = storeysItems[i].Labels[NARCOTRAFICANTE];
                        var _s = getHigherStorey(storey);
                        if (_s != null)
                        {
                            var drData = drDatas[i];
                            for (int i1 = NARCOTRAFICANTE; i1 < drData.RoofWaterBuckets.Count; i1++)
                            {
                                var kv = drData.RoofWaterBuckets[i1];
                                if (kv.Value == THESAURUSINSCRIPTION)
                                {
                                    drData.RoofWaterBuckets[i1] = new KeyValuePair<string, string>(kv.Key, _s);
                                }
                            }
                        }
                    }
                }
            }
            bool testExist(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return UNTRACEABLENESS;
            }
            bool hasLong(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.LongTranslatorLabels.Contains(label))
                            {
                                var tmp = storeysItems[i].Labels.Where(IsNumStorey).ToList();
                                if (tmp.Count > ADRENOCORTICOTROPHIC)
                                {
                                    var floor = tmp.Select(GetStoreyScore).Max() + QUOTATIONHOUSEMAID;
                                    if (storey != floor) return UNTRACEABLENESS;
                                }
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                {
                    var _storey = getHigherStorey(storey);
                    if (_storey != null)
                    {
                        for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == _storey)
                                {
                                    var drData = drDatas[i];
                                    if (drData.ConnectedToGravityWaterBucket.Contains(label))
                                    {
                                        return THESAURUSSEMBLANCE;
                                    }
                                    if (drData.ConnectedToSideWaterBucket.Contains(label))
                                    {
                                        return THESAURUSSEMBLANCE;
                                    }
                                }
                            }
                        }
                        foreach (var drData in drDatas)
                        {
                            foreach (var kv in drData.RoofWaterBuckets)
                            {
                                if (kv.Value == _storey && kv.Key == label)
                                {
                                    return THESAURUSSEMBLANCE;
                                }
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasShort(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.ShortTranslatorLabels.Contains(label))
                            {
                                {
                                    var tmp = storeysItems[i].Labels.Where(IsNumStorey).ToList();
                                    if (tmp.Count > ADRENOCORTICOTROPHIC)
                                    {
                                        var floor = tmp.Select(GetStoreyScore).Max() + QUOTATIONHOUSEMAID;
                                        if (storey != floor) return UNTRACEABLENESS;
                                    }
                                }
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            List<AloneFloorDrainInfo> getAloneFloorDrainInfos()
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            return drData.AloneFloorDrainInfos;
                        }
                    }
                }
                return new List<AloneFloorDrainInfo>();
            }
            otherInfo.AloneFloorDrainInfos.AddRange(getAloneFloorDrainInfos());
            bool getHasSideFloorDrain(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSideFloorDrain.Contains(label)) return THESAURUSSEMBLANCE;
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool getIsDitch(string label)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSALCOHOLIC or THESAURUSLADYLIKE)
                        {
                            var drData = drDatas[i];
                            if (drData.HasDitch.Contains(label)) return THESAURUSSEMBLANCE;
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            string getWaterWellLabel(string label)
            {
                if (getIsDitch(label)) return null;
                string _getWaterWellLabel(string label, string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.WaterWellLabels.TryGetValue(label, out string value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                    return null;
                }
                var ret = _getWaterWellLabel(label, THESAURUSALCOHOLIC);
                ret ??= _getWaterWellLabel(label, THESAURUSLADYLIKE);
                return ret;
            }
            int getWaterWellId(string label)
            {
                if (getIsDitch(label)) return -ADRENOCORTICOTROPHIC;
                int _getWaterWellId(string label, string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.WaterWellIds.TryGetValue(label, out int value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                    return -ADRENOCORTICOTROPHIC;
                }
                var ret = _getWaterWellId(label, THESAURUSALCOHOLIC);
                if (ret == -ADRENOCORTICOTROPHIC)
                {
                    ret = _getWaterWellId(label, THESAURUSLADYLIKE);
                }
                return ret;
            }
            int getWaterSealingWellId(string label)
            {
                if (getIsDitch(label)) return -ADRENOCORTICOTROPHIC;
                int _getWaterSealingWellId(string label, string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.WaterSealingWellIds.TryGetValue(label, out int value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                    return -ADRENOCORTICOTROPHIC;
                }
                var ret = _getWaterSealingWellId(label, THESAURUSALCOHOLIC);
                if (ret == -ADRENOCORTICOTROPHIC)
                {
                    ret = _getWaterSealingWellId(label, THESAURUSLADYLIKE);
                }
                return ret;
            }
            int getRainPortId(string label)
            {
                if (getIsDitch(label)) return -ADRENOCORTICOTROPHIC;
                int _getRainPortId(string label, string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.RainPortIds.TryGetValue(label, out int value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                    return -ADRENOCORTICOTROPHIC;
                }
                var ret = _getRainPortId(label, THESAURUSALCOHOLIC);
                if (ret == -ADRENOCORTICOTROPHIC)
                {
                    ret = _getRainPortId(label, THESAURUSLADYLIKE);
                }
                return ret;
            }
            bool getHasRainPort(string label)
            {
                if (getIsDitch(label)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            return drData.HasRainPortSymbols.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool getHasWaterSealingWell(string label)
            {
                if (getIsDitch(label)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            return drData.HasWaterSealingWell.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool getHasWaterWell(string label)
            {
                if (getIsDitch(label)) return UNTRACEABLENESS;
                if (getHasRainPort(label) || getHasWaterSealingWell(label)) return UNTRACEABLENESS;
                return getWaterWellLabel(label) != null;
            }
            bool getIsSpreading(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.Spreadings.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasSingleFloorDrainDrainageForRainPort(string label)
            {
                if (IsY1L(label)) return UNTRACEABLENESS;
                var id = getRainPortId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForRainPort.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasSingleFloorDrainDrainageForWaterSealingWell(string label)
            {
                if (IsY1L(label)) return UNTRACEABLENESS;
                var id = getWaterSealingWellId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterSealingWell.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasSingleFloorDrainDrainageForWaterWell(string label)
            {
                if (IsY1L(label)) return UNTRACEABLENESS;
                var waterWellLabel = getWaterWellLabel(label);
                if (waterWellLabel == null) return UNTRACEABLENESS;
                var id = getWaterWellId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterWell.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForRainPort(string label)
            {
                if (!hasSingleFloorDrainDrainageForRainPort(label)) return UNTRACEABLENESS;
                var id = getRainPortId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForRainPort.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterWell(label)) return UNTRACEABLENESS;
                var id = getWaterWellId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterWell.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterSealingWell(label)) return UNTRACEABLENESS;
                var id = getWaterSealingWellId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            int getDitchId(string label)
            {
                int _getDitchId(string label, string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.DitchIds.TryGetValue(label, out int value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                    return -ADRENOCORTICOTROPHIC;
                }
                var ret = _getDitchId(label, THESAURUSALCOHOLIC);
                if (ret == -ADRENOCORTICOTROPHIC)
                {
                    ret = _getDitchId(label, THESAURUSLADYLIKE);
                }
                return ret;
            }
            bool hasSingleFloorDrainDrainageForDitch(string label)
            {
                if (IsY1L(label)) return UNTRACEABLENESS;
                var id = getDitchId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForDitch.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForDitch(string label)
            {
                if (!hasSingleFloorDrainDrainageForDitch(label)) return UNTRACEABLENESS;
                var id = getDitchId(label);
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForDitch.Contains(id))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            int getFDCount(string label, string storey)
            {
                if (IsY1L(label)) return NARCOTRAFICANTE;
                int _getFDCount()
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                    return NARCOTRAFICANTE;
                }
                var ret = _getFDCount();
                return ret;
            }
            int getFDWrappingPipeCount(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            drData.FloorDrainWrappingPipes.TryGetValue(label, out int v);
                            return v;
                        }
                    }
                }
                return NARCOTRAFICANTE;
            }
            bool hasCondensePipe(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.HasCondensePipe.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.HasBrokenCondensePipes.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasNonBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.HasNonBrokenCondensePipes.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool getPlsDrawCondensePipeHigher(string label, string storey)
            {
                if (hasBrokenCondensePipes(label, storey)) return UNTRACEABLENESS;
                if (!hasCondensePipe(label, storey)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.PlsDrawCondensePipeHigher.Contains(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool hasOutletlWrappingPipe(string label)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            return drData.OutletWrappingPipeDict.ContainsValue(label);
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            string getOutletWrappingPipeRadius(string label)
            {
                if (!hasOutletlWrappingPipe(label)) return null;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            foreach (var kv in drData.OutletWrappingPipeDict)
                            {
                                if (kv.Value == label)
                                {
                                    var id = kv.Key;
                                    drData.OutletWrappingPipeRadiusStringDict.TryGetValue(id, out string v);
                                    return v;
                                }
                            }
                        }
                    }
                }
                return null;
            }
            bool hasSolidPipe(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            string getWaterBucketLabel(string pipe, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.ConnectedToGravityWaterBucket.Contains(pipe)) return QUOTATIONRESPIRATORY;
                            if (drData.ConnectedToSideWaterBucket.Contains(pipe)) return ELECTRODEPOSITION;
                        }
                    }
                }
                foreach (var drData in drDatas)
                {
                    foreach (var kv in drData.RoofWaterBuckets)
                    {
                        if (kv.Value == storey && kv.Key == pipe)
                        {
                            return QUOTATIONRESPIRATORY;
                        }
                    }
                }
                return waterBucketsInfos.FirstOrDefault(x => x.Pipe == pipe && x.Storey == storey)?.WaterBucket;
            }
            WaterBucketItem getWaterBucketItem(string pipe, string storey)
            {
                var bkLabel = getWaterBucketLabel(pipe, storey);
                if (bkLabel == null) return null;
                return WaterBucketItem.TryParse(bkLabel);
            }
            var pipeInfoDict = new Dictionary<string, RainGroupingPipeItem>();
            var y1lGroupingItems = new List<RainGroupingPipeItem>();
            var y2lGroupingItems = new List<RainGroupingPipeItem>();
            var ylGroupingItems = new List<RainGroupingPipeItem>();
            var nlGroupingItems = new List<RainGroupingPipeItem>();
            var fl0GroupingItems = new List<RainGroupingPipeItem>();
            foreach (var lb in allY1L)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.Y1L;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        WaterBucket = getWaterBucketItem(lb, storey),
                        Storey = storey,
                    });
                }
                y1lGroupingItems.Add(item);
                pipeInfoDict[lb] = item;
            }
            foreach (var lb in allYL)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.YL;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        Storey = storey,
                    });
                }
                ylGroupingItems.Add(item);
                pipeInfoDict[lb] = item;
            }
            foreach (var lb in allY2L)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.Y2L;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        HasCondensePipe = hasCondensePipe(lb, storey),
                        HasBrokenCondensePipes = hasBrokenCondensePipes(lb, storey),
                        HasNonBrokenCondensePipes = hasNonBrokenCondensePipes(lb, storey),
                        FloorDrainWrappingPipesCount = getFDWrappingPipeCount(lb, storey),
                        Storey = storey,
                    });
                }
                y2lGroupingItems.Add(item);
                pipeInfoDict[lb] = item;
            }
            foreach (var lb in allNL)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.NL;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        HasCondensePipe = hasCondensePipe(lb, storey),
                        HasBrokenCondensePipes = hasBrokenCondensePipes(lb, storey),
                        HasNonBrokenCondensePipes = hasNonBrokenCondensePipes(lb, storey),
                        FloorDrainWrappingPipesCount = getFDWrappingPipeCount(lb, storey),
                        Storey = storey,
                    });
                }
                nlGroupingItems.Add(item);
                pipeInfoDict[lb] = item;
            }
            foreach (var lb in allFL0)
            {
                var item = new RainGroupingPipeItem();
                item.Label = lb;
                item.PipeType = PipeType.FL0;
                item.Items = new List<RainGroupingPipeItem.ValueItem>();
                item.Hangings = new List<RainGroupingPipeItem.Hanging>();
                foreach (var storey in allStoreys)
                {
                    var _hasLong = hasLong(lb, storey);
                    item.Items.Add(new RainGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(lb, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(lb, storey),
                    });
                    item.Hangings.Add(new RainGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(lb, storey),
                        HasCondensePipe = hasCondensePipe(lb, storey),
                        HasBrokenCondensePipes = hasBrokenCondensePipes(lb, storey),
                        HasNonBrokenCondensePipes = hasNonBrokenCondensePipes(lb, storey),
                        FloorDrainWrappingPipesCount = getFDWrappingPipeCount(lb, storey),
                        Storey = storey,
                    });
                }
                fl0GroupingItems.Add(item);
                pipeInfoDict[lb] = item;
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    foreach (var h in item.Hangings)
                    {
                        h.PlsDrawCondensePipeHigher = getPlsDrawCondensePipeHigher(label, h.Storey);
                    }
                }
            }
            var iRF = allStoreys.IndexOf(THESAURUSADHERE);
            var iRF1 = allStoreys.IndexOf(IMMUNOGENETICALLY);
            var iRF2 = allStoreys.IndexOf(THESAURUSNATURALIST);
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    item.WaterWellLabel = getWaterWellLabel(label);
                    item.HasOutletWrappingPipe = hasOutletlWrappingPipe(label);
                    item.HasSingleFloorDrainDrainageForWaterWell = hasSingleFloorDrainDrainageForWaterWell(label);
                    item.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = isFloorDrainShareDrainageWithVerticalPipeForWaterWell(label);
                    item.HasSingleFloorDrainDrainageForRainPort = hasSingleFloorDrainDrainageForRainPort(label);
                    item.IsFloorDrainShareDrainageWithVerticalPipeForRainPort = isFloorDrainShareDrainageWithVerticalPipeForRainPort(label);
                    item.HasSingleFloorDrainDrainageForWaterSealingWell = hasSingleFloorDrainDrainageForWaterSealingWell(label);
                    item.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell = isFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell(label);
                    item.HasSingleFloorDrainDrainageForDitch = hasSingleFloorDrainDrainageForDitch(label);
                    item.IsFloorDrainShareDrainageWithVerticalPipeForDitch = isFloorDrainShareDrainageWithVerticalPipeForDitch(label);
                    item.OutletWrappingPipeRadius = getOutletWrappingPipeRadius(label);
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (item.Hangings.All(x => x.WaterBucket == null))
                    {
                        for (int i = item.Items.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; i--)
                        {
                            if (item.Items[i].Exist)
                            {
                                var _m = item.Items[i];
                                _m.Exist = UNTRACEABLENESS;
                                if (Equals(_m, default(RainGroupingPipeItem.ValueItem)))
                                {
                                    if (i < iRF - ADRENOCORTICOTROPHIC && i > NARCOTRAFICANTE)
                                    {
                                        item.Items[i] = default;
                                    }
                                    if (i < iRF - ADRENOCORTICOTROPHIC)
                                    {
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = item.Items.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; i--)
                    {
                        if (item.Hangings[i].WaterBucket != null)
                        {
                            {
                                var j = i;
                                var _m = item.Items[j];
                                if (j == iRF)
                                {
                                    if (_m.Exist)
                                    {
                                        item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(NARCOTRAFICANTE, j).Select(k => item.Items[k]).Any(x => x.Exist);
                                    }
                                }
                                item.Items[j] = default;
                            }
                            {
                                for (int j = NARCOTRAFICANTE; j < i; j++)
                                {
                                    item.Hangings[j].WaterBucket = null;
                                }
                                for (int j = i + ADRENOCORTICOTROPHIC; j < item.Items.Count; j++)
                                {
                                    item.Items[j] = default;
                                }
                            }
                            break;
                        }
                    }
                    if (item.Items.Count > NARCOTRAFICANTE)
                    {
                        var lst = Enumerable.Range(NARCOTRAFICANTE, item.Items.Count).Where(i => item.Items[i].Exist).ToList();
                        if (lst.Count > NARCOTRAFICANTE)
                        {
                            var maxi = lst.Max();
                            var mini = lst.Min();
                            var hasWaterBucket = item.Hangings.Any(x => x.WaterBucket != null);
                            if (hasWaterBucket && (maxi == iRF || maxi == iRF - ADRENOCORTICOTROPHIC))
                            {
                                item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(NARCOTRAFICANTE, iRF).Select(k => item.Items[k]).Any(x => x.Exist);
                            }
                            else
                            {
                                var (ok, m) = item.Items.TryGetValue(iRF);
                                item.HasLineAtBuildingFinishedSurfice = ok && m.Exist && iRF > NARCOTRAFICANTE && item.Items[iRF - ADRENOCORTICOTROPHIC].Exist;
                            }
                        }
                    }
                    if (IsNL((item.Label)))
                    {
                        if (!item.Items.TryGet(iRF).Exist)
                            item.HasLineAtBuildingFinishedSurfice = UNTRACEABLENESS;
                    }
                    if (iRF >= PHOTOGONIOMETER)
                    {
                        if (!item.Items[iRF].Exist && item.Items[iRF - ADRENOCORTICOTROPHIC].Exist && item.Items[iRF - PHOTOGONIOMETER].Exist)
                        {
                            var hanging = item.Hangings[iRF - ADRENOCORTICOTROPHIC];
                            if (hanging.FloorDrainsCount > NARCOTRAFICANTE && !hanging.HasCondensePipe)
                            {
                                item.Items[iRF - ADRENOCORTICOTROPHIC] = default;
                            }
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (iRF >= ADRENOCORTICOTROPHIC && iRF1 > NARCOTRAFICANTE)
                    {
                        if (!item.Items[iRF1].Exist && item.Items[iRF].Exist && item.Items[iRF - ADRENOCORTICOTROPHIC].Exist)
                        {
                            item.Items[iRF] = default;
                        }
                    }
                    if (item.PipeType == PipeType.Y1L)
                    {
                        if (item.Hangings.All(x => x.WaterBucket is null))
                        {
                            for (int i = item.Items.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; --i)
                            {
                                if (item.Items[i].Exist)
                                {
                                    var hanging = item.Hangings.TryGet(i + ADRENOCORTICOTROPHIC);
                                    if (hanging != null)
                                    {
                                        hanging.WaterBucket = WaterBucketItem.TryParse(QUOTATIONGOFFERING);
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    if (!IsY1L(label)) continue;
                    var item = kv.Value;
                    foreach (var hanging in item.Hangings)
                    {
                        if (GetStoreyScore((hanging.Storey)) >= ushort.MaxValue && hanging.WaterBucket is not null)
                        {
                            item.HasLineAtBuildingFinishedSurfice = THESAURUSSEMBLANCE;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                    {
                        var hanging = item.Hangings[i];
                        if (hanging.Storey is THESAURUSALCOHOLIC)
                        {
                            if (item.Items[i].HasShort)
                            {
                                var m = item.Items[i];
                                m.HasShort = UNTRACEABLENESS;
                                item.Items[i] = m;
                            }
                        }
                    }
                    for (int i = NARCOTRAFICANTE; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                        if (m.HasLong && m.HasShort)
                        {
                            m.HasShort = UNTRACEABLENESS;
                            item.Items[i] = m;
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (getHasWaterSealingWell(label))
                    {
                        item.OutletType = OutletType.WaterSealingWell;
                        item.OutletFloor = THESAURUSALCOHOLIC;
                    }
                    else if (getHasRainPort(label))
                    {
                        item.OutletType = OutletType.RainPort;
                        item.OutletFloor = THESAURUSALCOHOLIC;
                    }
                    else if (getHasWaterWell(label))
                    {
                        item.OutletType = OutletType.RainWell;
                        item.OutletFloor = THESAURUSALCOHOLIC;
                    }
                    else
                    {
                        var ok = UNTRACEABLENESS;
                        if (testExist(label, THESAURUSALCOHOLIC))
                        {
                            if (getIsSpreading(label, THESAURUSALCOHOLIC))
                            {
                                item.OutletType = OutletType.Spreading;
                                item.OutletFloor = THESAURUSALCOHOLIC;
                                ok = THESAURUSSEMBLANCE;
                            }
                            else if (getIsDitch(label))
                            {
                                item.OutletType = OutletType.Ditch;
                                item.OutletFloor = THESAURUSALCOHOLIC;
                                ok = THESAURUSSEMBLANCE;
                            }
                        }
                        if (!ok)
                        {
                            item.OutletType = OutletType.Spreading;
                            for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                            {
                                if (item.Items[i].Exist)
                                {
                                    var hanging = item.Hangings[i];
                                    item.OutletFloor = hanging.Storey;
                                    break;
                                }
                            }
                            ok = THESAURUSSEMBLANCE;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.OutletType == OutletType.Spreading && item.OutletFloor == THESAURUSADHERE)
                {
                    item.HasLineAtBuildingFinishedSurfice = UNTRACEABLENESS;
                }
                if (item.OutletType == OutletType.Spreading)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == item.OutletFloor)
                        {
                            h.HasCheckPoint = UNTRACEABLENESS;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                    {
                        var storey = allStoreys.TryGet(i);
                        if (storey == THESAURUSALCOHOLIC)
                        {
                            var hanging = item.Hangings[i];
                            hanging.HasCheckPoint = THESAURUSSEMBLANCE;
                            break;
                        }
                    }
                    for (int i = NARCOTRAFICANTE; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                        if (m.HasShort)
                        {
                            item.Hangings[i].HasCheckPoint = THESAURUSSEMBLANCE;
                        }
                        if (m.HasLong)
                        {
                            var h = item.Hangings.TryGet(i + ADRENOCORTICOTROPHIC);
                            if (h != null && (i + ADRENOCORTICOTROPHIC) != iRF)
                            {
                                h.HasCheckPoint = THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = NARCOTRAFICANTE; i < item.Items.Count; i++)
                {
                    var x = item.Items[i];
                    if (!x.Exist) item.Items[i] = default;
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.FloorDrainsCountAt1F == NARCOTRAFICANTE)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == THESAURUSALCOHOLIC)
                        {
                            item.FloorDrainsCountAt1F = h.FloorDrainsCount;
                            h.FloorDrainsCount = NARCOTRAFICANTE;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.OutletType is OutletType.Spreading)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == item.OutletFloor)
                        {
                            h.HasCheckPoint = UNTRACEABLENESS;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = NARCOTRAFICANTE; i < item.Items.Count; i++)
                {
                    var m = item.Items[i];
                    if (m.HasLong)
                    {
                        var h = item.Hangings[i];
                        if (!h.HasCondensePipe && (item.Hangings.TryGet(i + ADRENOCORTICOTROPHIC)?.FloorDrainsCount ?? NARCOTRAFICANTE) == NARCOTRAFICANTE && (item.Hangings.TryGet(i + ADRENOCORTICOTROPHIC)?.WaterBucket != null))
                        {
                            h.LongTransHigher = THESAURUSSEMBLANCE;
                        }
                    }
                    {
                        var h = item.Hangings[i];
                        if (h.FloorDrainsCount > NARCOTRAFICANTE)
                        {
                            h.HasSideFloorDrain = getHasSideFloorDrain(item.Label, h.Storey);
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.OutletFloor is THESAURUSALCOHOLIC && item.OutletType != OutletType.Spreading)
                {
                    item.OutletWrappingPipeRadius ??= THESAURUSDISCLOSE;
                }
                if (item.OutletFloor is IMMUNOGENETICALLY or THESAURUSNATURALIST && item.OutletType == OutletType.Spreading)
                {
                    item.HasLineAtBuildingFinishedSurfice = UNTRACEABLENESS;
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                item.FloorDrainsCountAt1F = NARCOTRAFICANTE;
            }
            var pipeGroupItems = new List<RainGroupedPipeItem>();
            var y1lPipeGroupItems = new List<RainGroupedPipeItem>();
            var y2lPipeGroupItems = new List<RainGroupedPipeItem>();
            var nlPipeGroupItems = new List<RainGroupedPipeItem>();
            var ylPipeGroupItems = new List<RainGroupedPipeItem>();
            var fl0PipeGroupItems = new List<RainGroupedPipeItem>();
            foreach (var g in ylGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.RainWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasOutletWrappingPipe = g.Key.HasOutletWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    HasSingleFloorDrainDrainageForWaterSealingWell = g.Key.HasSingleFloorDrainDrainageForWaterSealingWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell,
                    HasSingleFloorDrainDrainageForDitch = g.Key.HasSingleFloorDrainDrainageForDitch,
                    IsFloorDrainShareDrainageWithVerticalPipeForDitch = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForDitch,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                    OutletType = g.Key.OutletType,
                    OutletFloor = g.Key.OutletFloor,
                    FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                };
                ylPipeGroupItems.Add(item);
            }
            foreach (var g in y1lGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.RainWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasOutletWrappingPipe = g.Key.HasOutletWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    HasSingleFloorDrainDrainageForWaterSealingWell = g.Key.HasSingleFloorDrainDrainageForWaterSealingWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell,
                    HasSingleFloorDrainDrainageForDitch = g.Key.HasSingleFloorDrainDrainageForDitch,
                    IsFloorDrainShareDrainageWithVerticalPipeForDitch = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForDitch,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                    OutletType = g.Key.OutletType,
                    OutletFloor = g.Key.OutletFloor,
                    FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                };
                y1lPipeGroupItems.Add(item);
            }
            foreach (var g in y2lGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.RainWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasOutletWrappingPipe = g.Key.HasOutletWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    HasSingleFloorDrainDrainageForWaterSealingWell = g.Key.HasSingleFloorDrainDrainageForWaterSealingWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell,
                    HasSingleFloorDrainDrainageForDitch = g.Key.HasSingleFloorDrainDrainageForDitch,
                    IsFloorDrainShareDrainageWithVerticalPipeForDitch = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForDitch,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                    OutletType = g.Key.OutletType,
                    OutletFloor = g.Key.OutletFloor,
                    FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                };
                y2lPipeGroupItems.Add(item);
            }
            foreach (var g in nlGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.RainWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasOutletWrappingPipe = g.Key.HasOutletWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    HasSingleFloorDrainDrainageForWaterSealingWell = g.Key.HasSingleFloorDrainDrainageForWaterSealingWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell,
                    HasSingleFloorDrainDrainageForDitch = g.Key.HasSingleFloorDrainDrainageForDitch,
                    IsFloorDrainShareDrainageWithVerticalPipeForDitch = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForDitch,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                    OutletType = g.Key.OutletType,
                    OutletFloor = g.Key.OutletFloor,
                    FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                };
                nlPipeGroupItems.Add(item);
            }
            foreach (var g in fl0GroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.RainWell).Select(x => x.WaterWellLabel).Distinct().ToList();
                var labels = g.Select(x => x.Label).Distinct().OrderBy(x => x).ToList();
                var item = new RainGroupedPipeItem()
                {
                    Labels = labels,
                    HasOutletWrappingPipe = g.Key.HasOutletWrappingPipe,
                    WaterWellLabels = waterWellLabels,
                    Items = g.Key.Items.ToList(),
                    PipeType = g.Key.PipeType,
                    Hangings = g.Key.Hangings.ToList(),
                    HasLineAtBuildingFinishedSurfice = g.Key.HasLineAtBuildingFinishedSurfice,
                    HasSingleFloorDrainDrainageForWaterWell = g.Key.HasSingleFloorDrainDrainageForWaterWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell,
                    HasSingleFloorDrainDrainageForRainPort = g.Key.HasSingleFloorDrainDrainageForRainPort,
                    IsFloorDrainShareDrainageWithVerticalPipeForRainPort = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForRainPort,
                    HasSingleFloorDrainDrainageForWaterSealingWell = g.Key.HasSingleFloorDrainDrainageForWaterSealingWell,
                    IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell,
                    HasSingleFloorDrainDrainageForDitch = g.Key.HasSingleFloorDrainDrainageForDitch,
                    IsFloorDrainShareDrainageWithVerticalPipeForDitch = g.Key.IsFloorDrainShareDrainageWithVerticalPipeForDitch,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                    OutletType = g.Key.OutletType,
                    OutletFloor = g.Key.OutletFloor,
                    FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                };
                fl0PipeGroupItems.Add(item);
            }
            y1lPipeGroupItems = y1lPipeGroupItems.OrderBy(x => x.Labels.First()).ToList();
            y2lPipeGroupItems = y2lPipeGroupItems.OrderBy(x => x.Labels.First()).ToList();
            nlPipeGroupItems = nlPipeGroupItems.OrderBy(x => x.Labels.First()).ToList();
            ylPipeGroupItems = ylPipeGroupItems.OrderBy(x => x.Labels.First()).ToList();
            fl0PipeGroupItems = fl0PipeGroupItems.OrderBy(x => x.Labels.First()).ToList();
            pipeGroupItems.AddRange(ylPipeGroupItems);
            pipeGroupItems.AddRange(y1lPipeGroupItems);
            pipeGroupItems.AddRange(y2lPipeGroupItems);
            pipeGroupItems.AddRange(nlPipeGroupItems);
            pipeGroupItems.AddRange(fl0PipeGroupItems);
            return pipeGroupItems;
        }
        public static void DrawRainDiagram(RainSystemDiagramViewModel viewModel, bool focus)
        {
            if (focus) FocusMainWindow();
            if (ThRainService.commandContext == null) return;
            if (ThRainService.commandContext.StoreyContext == null) return;
            if (ThRainService.commandContext.StoreyContext.StoreyInfos == null) return;
            if (!TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSSEMBLANCE))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { FINNLANDISIERUNG, THESAURUSSANCTITY, VERGELTUNGSWAFFE, THESAURUSUNDERSTATE, TRANYLCYPROMINE, THESAURUSPRELIMINARY, THESAURUSCOMMOTION, THESAURUSPROCEEDING });
                var storeys = ThRainService.commandContext.StoreyContext.StoreyInfos;
                List<RainDrawingData> drDatas;
                var range = ThRainService.commandContext.range;
                List<StoreyInfo> storeysItems;
                ExtraInfo exInfo;
                if (range != null)
                {
                    var (_exInfo, ok) = CollectRainData(range, adb, out storeysItems, out drDatas);
                    exInfo = _exInfo;
                    if (!ok) return;
                }
                else
                {
                    var (_exInfo, ok) = CollectRainData(adb, out _, out drDatas, ThRainService.commandContext);
                    exInfo = _exInfo;
                    if (!ok) return;
                    storeysItems = storeys;
                }
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + QUOTATIONHOUSEMAID).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - ADRENOCORTICOTROPHIC;
                var end = NARCOTRAFICANTE;
                var OFFSET_X = THESAURUSWOMANLY;
                var SPAN_X = THESAURUSCONTINUATION;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSINFERENCE;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSINFERENCE;
                Dispose();
                DrawRainDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel, otherInfo, exInfo);
                FlushDQ(adb);
            }
        }
        public static void DrawRainDiagram()
        {
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return;
            if (!TrySelectPoint(out Point3d point3D)) return;
            var basePoint = point3D.ToPoint2d();
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSSEMBLANCE))
            {
                List<StoreyInfo> storeysItems;
                List<RainDrawingData> drDatas;
                var (exInfo, ok) = CollectRainData(range, adb, out storeysItems, out drDatas);
                if (!ok) return;
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo);
                Dispose();
                DrawRainDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys, otherInfo, null, exInfo);
                FlushDQ(adb);
            }
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof, bool showText)
        {
            var name = showText ? (canPeopleBeOnRoof ? ADVERTISEMENTAL : ORTHONORMALIZING) : PERPENDICULARITY;
            DrawAiringSymbol(pt, name);
        }
        public static void DrawAiringSymbol(Point2d pt, string name)
        {
            DrawBlockReference(blkName: UNSATISFACTORINESS, basePt: pt.ToPoint3d(), layer: THESAURUSCOMMOTION, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(PHOTOCONDUCTING, name);
            });
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: STAPHYLORRHAPHY, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: THESAURUSCOMMOTION, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(THERMOREGULATORY, offsetY);
                br.ObjectId.SetDynBlockValue(PHOTOCONDUCTING, THESAURUSSURFACE);
            });
        }
        public static void DrawDimLabel(Point2d pt1, Point2d pt2, Vector2d v, string text, string layer)
        {
            var dim = new AlignedDimension();
            dim.XLine1Point = pt1.ToPoint3d();
            dim.XLine2Point = pt2.ToPoint3d();
            dim.DimLinePoint = (GeoAlgorithm.MidPoint(pt1, pt2) + v).ToPoint3d();
            dim.DimensionText = text;
            dim.Layer = layer;
            ByLayer(dim);
            DrawEntityLazy(dim);
        }
        private static void DrawDimLabelRight(Point3d basePt, double dy)
        {
            var pt1 = basePt;
            var pt2 = pt1.OffsetY(dy);
            var dim = new AlignedDimension();
            dim.XLine1Point = pt1;
            dim.XLine2Point = pt2;
            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(QUOTATIONTRILINEAR);
            dim.DimensionText = QUOTATIONOPHTHALMIA;
            dim.Layer = TRANYLCYPROMINE;
            ByLayer(dim);
            DrawEntityLazy(dim);
        }
        public static double CHECKPOINT_OFFSET_Y = THESAURUSINEVITABLE;
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSPROCEEDING;
                ByLayer(line);
            });
        }
        public static void DrawStoreyLine(string label, Point2d _basePt, double lineLen, string text)
        {
            var basePt = _basePt.ToPoint3d();
            {
                var line = DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, NARCOTRAFICANTE));
                Dr.SetLabelStylesForWNote(line, dbt);
                DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: basePt.OffsetX(QUINALBARBITONE), layer: FINNLANDISIERUNG, props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, text } });
            }
            if (label == THESAURUSADHERE)
            {
                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, NARCOTRAFICANTE), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, NARCOTRAFICANTE));
                var dbt = DrawTextLazy(THESAURUSAFFRONT, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, NARCOTRAFICANTE));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
        }
        static double RF_OFFSET_Y => ThWSDStorey.RF_OFFSET_Y;
        public static void DrawRainDiagram(Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, RainSystemDiagramViewModel viewModel, OtherInfo otherInfo, ExtraInfo exInfo)
        {
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                new Cmd()
                {
                    basePoint = basePoint,
                    pipeGroupItems = pipeGroupItems,
                    allNumStoreyLabels = allNumStoreyLabels,
                    allStoreys = allStoreys,
                    start = start,
                    end = end,
                    OFFSET_X = OFFSET_X,
                    SPAN_X = SPAN_X,
                    HEIGHT = HEIGHT,
                    COUNT = COUNT,
                    dy = dy,
                    viewModel = viewModel,
                    adb = adb,
                    otherInfo = otherInfo,
                    exInfo = exInfo,
                }.Run();
            }
        }
        public class Cmd
        {
            public Point2d basePoint;
            public List<RainGroupedPipeItem> pipeGroupItems;
            public List<string> allNumStoreyLabels;
            public List<string> allStoreys;
            public int start;
            public int end;
            public double OFFSET_X;
            public double SPAN_X;
            public double HEIGHT;
            public int COUNT;
            public double dy;
            public RainSystemDiagramViewModel viewModel;
            public AcadDatabase adb;
            public double _dy;
            public double __dy;
            public double h0;
            public double h1;
            public OtherInfo otherInfo;
            public double h2 => h0 - h1;
            public static bool SHOWLINE;
            public ExtraInfo exInfo;
            List<Vector2d> vecs0 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -THESAURUSINSTEAD - dy + _dy) };
            List<Vector2d> vecs1 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -h1), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy + _dy - h2) };
            List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -h1 - __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy + __dy + _dy - h2) };
            List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -h1 + __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy - __dy + _dy - h2) };
            List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -THESAURUSESTRANGE - dy + _dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            List<Vector2d> vecs3 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -h1), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSHORRENDOUS - dy + _dy - h2), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -h1 - __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSHORRENDOUS - h2 - dy + __dy + _dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -h1 + __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSHORRENDOUS - h2 - dy - __dy + _dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            List<Vector2d> vecs4 => vecs1.GetYAxisMirror();
            List<Vector2d> vecs5 => vecs2.GetYAxisMirror();
            List<Vector2d> vecs6 => vecs3.GetYAxisMirror();
            List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
            List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
            List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
            public void Run()
            {
                exInfo.vm = viewModel;
                var db = adb.Database;
                static void DrawSegs(List<GLineSegment> segs) { for (int k = NARCOTRAFICANTE; k < segs.Count; k++) DrawTextLazy(k.ToString(), segs[k].StartPoint); }
                var storeyLines = new List<KeyValuePair<string, GLineSegment>>();
                void _DrawStoreyLine(string storey, Point2d p, double lineLen, string text)
                {
                    DrawStoreyLine(storey, p, lineLen, text);
                    storeyLines.Add(new KeyValuePair<string, GLineSegment>(storey, new GLineSegment(p, p.OffsetX(lineLen))));
                }
                var vec7 = new Vector2d(-THESAURUSITINERANT, THESAURUSITINERANT);
                {
                    var heights = new List<int>(allStoreys.Count);
                    var s = NARCOTRAFICANTE;
                    var _vm = FloorHeightsViewModel.Instance;
                    static bool test(string x, int t)
                    {
                        var m = Regex.Match(x, MUSCULOTENDINOUS);
                        if (m.Success)
                        {
                            if (int.TryParse(m.Groups[ADRENOCORTICOTROPHIC].Value, out int v1) && int.TryParse(m.Groups[PHOTOGONIOMETER].Value, out int v2))
                            {
                                var min = Math.Min(v1, v2);
                                var max = Math.Max(v1, v2);
                                for (int i = min; i <= max; i++)
                                {
                                    if (i == t) return THESAURUSSEMBLANCE;
                                }
                            }
                            else
                            {
                                return UNTRACEABLENESS;
                            }
                        }
                        m = Regex.Match(x, THESAURUSCONSUME);
                        if (m.Success)
                        {
                            if (int.TryParse(x, out int v))
                            {
                                if (v == t) return THESAURUSSEMBLANCE;
                            }
                        }
                        return UNTRACEABLENESS;
                    }
                    for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                    {
                        heights.Add(s);
                        var v = _vm.GeneralFloor;
                        if (_vm.ExistsSpecialFloor) v = _vm.Items.FirstOrDefault(m => test(m.Floor, GetStoreyScore(allStoreys[i])))?.Height ?? v;
                        s += v;
                    }
                    var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                    for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        string getStoreyHeightText()
                        {
                            if (storey is THESAURUSALCOHOLIC) return THESAURUSLUXURIANT;
                            var ret = (heights[i] / PSYCHOHISTORICAL).ToString(THESAURUSFLIGHT); ;
                            if (ret == THESAURUSFLIGHT) return THESAURUSLUXURIANT;
                            return ret;
                        }
                        _DrawStoreyLine(storey, bsPt1, lineLen, getStoreyHeightText());
                        if (i == NARCOTRAFICANTE && otherInfo.AloneFloorDrainInfos.Count > NARCOTRAFICANTE)
                        {
                            var dome_lines = new List<GLineSegment>(PHOTOSENSITIZING);
                            var dome_layer = THESAURUSCOMMOTION;
                            void drawDomePipe(GLineSegment seg)
                            {
                                if (seg.IsValid) dome_lines.Add(seg);
                            }
                            void drawDomePipes(IEnumerable<GLineSegment> segs)
                            {
                                dome_lines.AddRange(segs.Where(x => x.IsValid));
                            }
                            var pt = bsPt1.OffsetX(lineLen + SPAN_X);
                            {
                                static void _DrawRainWaterWells(Point2d pt, List<string> values)
                                {
                                    if (values == null) return;
                                    values = values.OrderBy(x =>
                                    {
                                        long.TryParse(x, out long v);
                                        return v;
                                    }).ThenBy(x => x).ToList();
                                    if (values.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        DrawRainWaterWell(pt.OffsetX(-THESAURUSALCOVE), values[NARCOTRAFICANTE]);
                                    }
                                    else if (values.Count >= PHOTOGONIOMETER)
                                    {
                                        var pts = GetBasePoints(pt.OffsetX(-THESAURUSORNAMENT - THESAURUSALCOVE), PHOTOGONIOMETER, values.Count, THESAURUSORNAMENT, THESAURUSORNAMENT).ToList();
                                        for (int i = NARCOTRAFICANTE; i < values.Count; i++)
                                        {
                                            DrawRainWaterWell(pts[i], values[i]);
                                        }
                                    }
                                }
                                {
                                    var lst = otherInfo.AloneFloorDrainInfos.Where(x => !x.IsSideFloorDrain).Select(x => x.WaterWellLabel).Distinct().ToList();
                                    if (lst.Count > NARCOTRAFICANTE)
                                    {
                                        {
                                            var line = DrawLineLazy(pt.OffsetX(QUOTATIONTRILINEAR), pt.OffsetX(-SPAN_X));
                                            Dr.SetLabelStylesForWNote(line);
                                            var p = pt.OffsetY(-QUINALBARBITONE);
                                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -HYDROSTATICALLY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE) };
                                            drawDomePipes(vecs.ToGLineSegments(p));
                                            _DrawFloorDrain((p + new Vector2d(AUTHORITARIANISM, THESAURUSANCILLARY)).ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY, THESAURUSREDOUND);
                                            DrawNoteText(ENDONUCLEOLYTIC, p + new Vector2d(AUTHORITARIANISM + THESAURUSARTISAN, THESAURUSANCILLARY) + new Vector2d(-SACCHAROMYCETACEAE, -PHOTOCONVERSION));
                                            {
                                                _DrawRainWaterWells(vecs.GetLastPoint(p), lst.OrderBy(x =>
                                                {
                                                    if (x == THESAURUSHEARTLESS) return int.MaxValue;
                                                    int.TryParse(x, out int v);
                                                    return v;
                                                }).ThenBy(x => x).ToList());
                                            }
                                        }
                                        pt = pt.OffsetX(SPAN_X);
                                    }
                                }
                                {
                                    var lst = otherInfo.AloneFloorDrainInfos.Where(x => x.IsSideFloorDrain).Select(x => x.WaterWellLabel).Distinct().ToList();
                                    if (lst.Count > NARCOTRAFICANTE)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -HYDROSTATICALLY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE) };
                                        drawDomePipes(vecs.ToGLineSegments(pt));
                                        _DrawFloorDrain((pt + new Vector2d(AUTHORITARIANISM, THESAURUSANCILLARY)).ToPoint3d(), THESAURUSSEMBLANCE, THESAURUSUNCOUTH, THESAURUSREDOUND);
                                        {
                                            _DrawRainWaterWells(vecs.GetLastPoint(pt), lst.OrderBy(x =>
                                            {
                                                if (x == THESAURUSHEARTLESS) return int.MaxValue;
                                                int.TryParse(x, out int v);
                                                return v;
                                            }).ThenBy(x => x).ToList());
                                        }
                                        pt = pt.OffsetX(SPAN_X);
                                    }
                                }
                            }
                            foreach (var dome_line in dome_lines)
                            {
                                var line = DrawLineSegmentLazy(dome_line);
                                line.Layer = dome_layer;
                                ByLayer(line);
                            }
                        }
                    }
                }
                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, string value, string shadow)
                {
                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                    {
                        Dr.DrawSimpleLabel(basePt.ToPoint2d(), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                    }
                    if (Testing) return;
                    DrawFloorDrain(basePt, leftOrRight, value);
                }
                var gaps = storeyLines.Select(kv =>
                {
                    var geo = kv.Value.Buffer(THESAURUSCONSUL);
                    geo.UserData = kv.Key;
                    return geo;
                }).ToList();
                var gapsf = GeoFac.CreateIntersectsSelector(gaps);
                var storeySpaces = storeyLines.Select(kv =>
                {
                    var seg1 = kv.Value.Offset(NARCOTRAFICANTE, ADRENOCORTICOTROPHIC);
                    var seg2 = kv.Value.Offset(NARCOTRAFICANTE, HEIGHT - ADRENOCORTICOTROPHIC);
                    var geo = new GRect(seg1.StartPoint, seg2.EndPoint).ToPolygon();
                    geo.UserData = kv.Key;
                    return geo;
                }).ToList();
                var storeySpacesf = GeoFac.CreateIntersectsSelector(storeySpaces);
                for (int j = NARCOTRAFICANTE; j < COUNT; j++)
                {
                    pipeGroupItems.Add(new RainGroupedPipeItem());
                }
                var dx = NARCOTRAFICANTE;
                for (int j = NARCOTRAFICANTE; j < COUNT; j++)
                {
                    var dome_lines = new List<GLineSegment>(PHOTOSENSITIZING);
                    var dome_layer = THESAURUSCOMMOTION;
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid) dome_lines.Add(seg);
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs)
                    {
                        dome_lines.AddRange(segs.Where(x => x.IsValid));
                    }
                    var linesKillers = new HashSet<Geometry>();
                    var iRF = allStoreys.IndexOf(THESAURUSADHERE);
                    var gpItem = pipeGroupItems[j];
                    string getHDN()
                    {
                        const string dft = ENDONUCLEOLYTIC;
                        if (gpItem.PipeType == PipeType.Y2L) return viewModel?.Params.BalconyFloorDrainDN ?? dft;
                        if (gpItem.PipeType == PipeType.NL) return viewModel?.Params.CondensePipeHorizontalDN ?? dft;
                        if (gpItem.PipeType == PipeType.FL0) return viewModel?.Params.WaterWellFloorDrainDN ?? dft;
                        return dft;
                    }
                    string getPipeDn()
                    {
                        const string dft = SPLANCHNOPLEURE;
                        var dn = gpItem.PipeType switch
                        {
                            PipeType.Y2L => viewModel?.Params.BalconyRainPipeDN,
                            PipeType.NL => viewModel?.Params.CondensePipeVerticalDN,
                            _ => dft,
                        };
                        dn ??= dft;
                        return dn;
                    }
                    var shouldDrawAringSymbol = gpItem.PipeType != PipeType.Y1L && (gpItem.PipeType == PipeType.NL ? (viewModel?.Params?.HasAiringForCondensePipe ?? THESAURUSSEMBLANCE) : THESAURUSSEMBLANCE);
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                        {
                            HasLongTranslator = gpItem.Items[i].HasLong,
                            HasShortTranslator = gpItem.Items[i].HasShort,
                        } : null;
                        runs.Add(run);
                    }
                    {
                        {
                            bool? flag = null;
                            for (int i = runs.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasLongTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSSEMBLANCE;
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
                            for (int i = runs.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasShortTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSSEMBLANCE;
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
                    PipeRunLocationInfo[] getPipeRunLocationInfos(Point2d basePoint)
                    {
                        var arr = new PipeRunLocationInfo[allStoreys.Count];
                        for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                        {
                            arr[i] = new PipeRunLocationInfo() { Visible = THESAURUSSEMBLANCE, Storey = allStoreys[i], };
                        }
                        {
                            var tdx = QUOTATIONEXPANDING;
                            for (int i = start; i >= end; i--)
                            {
                                h0 = QUOTATION1BRICKETY;
                                h1 = THESAURUSSACRIFICE;
                                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                                var basePt = bsPt1.OffsetX(OFFSET_X + (j + ADRENOCORTICOTROPHIC) * SPAN_X) + new Vector2d(tdx, NARCOTRAFICANTE);
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                var storey = allStoreys[i];
                                if (storey == THESAURUSADHERE)
                                {
                                    _dy = ThWSDStorey.RF_OFFSET_Y;
                                }
                                else
                                {
                                    _dy = THESAURUSTATTLE;
                                }
                                PipeRunLocationInfo drawNormal()
                                {
                                    {
                                        var vecs = vecs0;
                                        var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                        arr[i].HangingEndPoint = arr[i].EndPoint;
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSINTENTIONAL, NARCOTRAFICANTE)).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        segs[NARCOTRAFICANTE] = new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, segs[ADRENOCORTICOTROPHIC].StartPoint);
                                        arr[i].RightSegsLast = segs;
                                    }
                                    {
                                        var pt = arr[i].Segs.First().StartPoint.OffsetX(THESAURUSINTENTIONAL);
                                        var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS))) };
                                        arr[i].RightSegsFirst = segs;
                                        segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSINTENTIONAL)));
                                    }
                                    return arr[i];
                                }
                                if (i == start)
                                {
                                    drawNormal().Visible = UNTRACEABLENESS;
                                    continue;
                                }
                                if (run == null)
                                {
                                    drawNormal().Visible = UNTRACEABLENESS;
                                    continue;
                                }
                                if (run.HasLongTranslator && gpItem.Hangings[i].LongTransHigher)
                                {
                                    h1 = THESAURUSINDUSTRY;
                                }
                                else if (run.HasLongTranslator && !gpItem.Hangings[i].HasCondensePipe && (gpItem.Hangings.TryGet(i + ADRENOCORTICOTROPHIC)?.FloorDrainsCount ?? NARCOTRAFICANTE) == NARCOTRAFICANTE)
                                {
                                    h1 = THESAURUSINDUSTRY;
                                }
                                if (run.HasLongTranslator && run.HasShortTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs3;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSTITTER);
                                            segs.Add(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSPOSTSCRIPT].EndPoint.OffsetXY(-THESAURUSINTENTIONAL, -THESAURUSINTENTIONAL)));
                                            segs.Add(new GLineSegment(segs[PHOTOGONIOMETER].EndPoint, new Point2d(segs[THESAURUSFACTOR].EndPoint.X, segs[PHOTOGONIOMETER].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSPOSTSCRIPT], new GLineSegment(segs[THESAURUSPOSTSCRIPT].StartPoint, segs[NARCOTRAFICANTE].StartPoint) };
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSINTENTIONAL)));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs6;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(-CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))).Offset(ELECTROMYOGRAPH, NARCOTRAFICANTE));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSTITTER);
                                            segs.Add(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSTITTER].StartPoint));
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSINTENTIONAL)));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    arr[i].HangingEndPoint = arr[i].Segs[THESAURUSTITTER].EndPoint;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs1;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs = segs.Take(THESAURUSTITTER).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSPOSTSCRIPT].EndPoint.OffsetXY(-THESAURUSINTENTIONAL, -THESAURUSINTENTIONAL))).ToList();
                                            segs.Add(new GLineSegment(segs[PHOTOGONIOMETER].EndPoint, new Point2d(segs[THESAURUSFACTOR].EndPoint.X, segs[PHOTOGONIOMETER].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSPOSTSCRIPT], new GLineSegment(segs[THESAURUSPOSTSCRIPT].StartPoint, segs[NARCOTRAFICANTE].StartPoint) };
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs4;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))).Offset(ELECTROMYOGRAPH, NARCOTRAFICANTE));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle;
                                            arr[i].RightSegsLast = segs.Take(THESAURUSTITTER).YieldAfter(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSFACTOR].StartPoint)).YieldAfter(segs[THESAURUSFACTOR]).ToList();
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
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
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSINTENTIONAL, NARCOTRAFICANTE)).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, segs[PHOTOGONIOMETER].StartPoint), segs[PHOTOGONIOMETER] };
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[PHOTOGONIOMETER].StartPoint, segs[PHOTOGONIOMETER].EndPoint);
                                            segs[PHOTOGONIOMETER] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(NARCOTRAFICANTE);
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, r.RightButtom));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs5;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSINTENTIONAL, NARCOTRAFICANTE)).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(-CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, segs[PHOTOGONIOMETER].StartPoint), segs[PHOTOGONIOMETER] };
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[PHOTOGONIOMETER].StartPoint, segs[PHOTOGONIOMETER].EndPoint);
                                            segs[PHOTOGONIOMETER] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(NARCOTRAFICANTE);
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, r.RightButtom));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    arr[i].HangingEndPoint = arr[i].Segs[NARCOTRAFICANTE].EndPoint;
                                }
                                else
                                {
                                    drawNormal();
                                }
                            }
                        }
                        for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                        {
                            var info = arr.TryGet(i);
                            if (info != null)
                            {
                                info.StartPoint = info.BasePoint.OffsetY(HEIGHT);
                            }
                        }
                        return arr;
                    }
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] infos)
                    {
                        var couldHavePeopleOnRoof = viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSSEMBLANCE;
                        var hasDrawedAiringSymbol = UNTRACEABLENESS;
                        void _DrawAiringSymbol(Point2d basePt)
                        {
                            if (!shouldDrawAringSymbol) return;
                            if (hasDrawedAiringSymbol) return;
                            var showText = THESAURUSSEMBLANCE;
                            {
                                var info = infos.FirstOrDefault(x => x.Storey == THESAURUSADHERE);
                                if (info != null)
                                {
                                    var pt = info.BasePoint;
                                    if (basePt.Y < pt.Y)
                                    {
                                        showText = UNTRACEABLENESS;
                                    }
                                }
                            }
                            DrawAiringSymbol(basePt, couldHavePeopleOnRoof, showText);
                            hasDrawedAiringSymbol = THESAURUSSEMBLANCE;
                        }
                        {
                            if (shouldDrawAringSymbol)
                            {
                                if (gpItem.HasLineAtBuildingFinishedSurfice)
                                {
                                    var info = infos.FirstOrDefault(x => x.Storey == THESAURUSADHERE);
                                    if (info != null)
                                    {
                                        var pt = info.BasePoint;
                                        var seg = new GLineSegment(pt, pt.OffsetY(ThWSDStorey.RF_OFFSET_Y));
                                        drawDomePipe(seg);
                                        _DrawAiringSymbol(seg.EndPoint);
                                    }
                                }
                            }
                        }
                        void _DrawLabel(string text, Point2d basePt, bool leftOrRight, double height)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, height), new Vector2d(leftOrRight ? -PLURALISTICALLY : PLURALISTICALLY, NARCOTRAFICANTE) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForRainNote(lines.ToArray());
                            var t = DrawTextLazy(text, THESAURUSDETEST, segs.Last().EndPoint.OffsetY(UNDERACHIEVEMENT));
                            Dr.SetLabelStylesForRainNote(t);
                        }
                        for (int i = end; i <= start; i++)
                        {
                            var storey = allStoreys.TryGet(i);
                            if (storey == null) continue;
                            var run = thwPipeLine.PipeRuns.TryGet(i);
                            if (run == null) continue;
                            var info = infos[i];
                            if (info == null) continue;
                            if (gpItem.OutletType == OutletType.Spreading)
                            {
                                static void DrawLabel(Point3d basePt, string text, double lineYOffset)
                                {
                                    var height = THESAURUSDETEST;
                                    var width = height * THESAURUSDISCOUNT * text.Length;
                                    var yd = new YesDraw();
                                    yd.OffsetXY(NARCOTRAFICANTE, lineYOffset);
                                    yd.OffsetX(-width);
                                    var pts = yd.GetPoint3ds(basePt).ToList();
                                    var lines = DrawLinesLazy(pts);
                                    Dr.SetLabelStylesForRainNote(lines.ToArray());
                                    var t = DrawTextLazy(text, height, pts.Last().OffsetXY(UNDERACHIEVEMENT, UNDERACHIEVEMENT));
                                    Dr.SetLabelStylesForRainNote(t);
                                }
                                if (gpItem.OutletFloor == storey)
                                {
                                    var basePt = info.EndPoint;
                                    if (storey == THESAURUSADHERE) basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                    var seg = new GLineSegment(basePt, basePt.OffsetY(THESAURUSLEVITATE));
                                    var p = seg.EndPoint;
                                    DrawDimLabel(seg.StartPoint, p, new Vector2d(QUOTATIONTRILINEAR, NARCOTRAFICANTE), THESAURUSCOMMODIOUS, TRANYLCYPROMINE);
                                    DrawLabel(p.ToPoint3d(), THESAURUSVILLAINY + storey, storey == THESAURUSADHERE ? -THESAURUSSACRIFICE - ThWSDStorey.RF_OFFSET_Y : -THESAURUSSACRIFICE);
                                    {
                                        var shadow = THESAURUSREDOUND;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                        {
                                            Dr.DrawSimpleLabel(p, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                        }
                                    }
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var segs = info.DisplaySegs = info.Segs.ToList();
                                        segs[NARCOTRAFICANTE] = new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, segs[NARCOTRAFICANTE].StartPoint.OffsetY(-(segs[NARCOTRAFICANTE].Length - THESAURUSLEVITATE)));
                                    }
                                }
                            }
                        }
                        var list = new List<Point2d>();
                        for (int i = start; i >= end; i--)
                        {
                            var storey = allStoreys.TryGet(i);
                            if (storey == null) continue;
                            {
                                var info = infos[i];
                                if (info == null) continue;
                                {
                                    var bk = gpItem.Hangings[i].WaterBucket;
                                    if (bk != null)
                                    {
                                        var basePt = info.EndPoint;
                                        if (storey == THESAURUSADHERE)
                                        {
                                            basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        switch (bk.WaterBucketType)
                                        {
                                            case WaterBucketEnum.Gravity:
                                            case WaterBucketEnum._87:
                                                {
                                                    Dr.DrawGravityWaterBucket(basePt.ToPoint3d());
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, EVERLASTINGNESS), new Vector2d(-THESAURUSUNBELIEVABLE, THESAURUSUNBELIEVABLE), new Vector2d(-THESAURUSASSISTANT + PARADEIGMATIKOS, NARCOTRAFICANTE) };
                                                    var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                                    Dr.SetLabelStylesForRainNote(DrawLineSegmentsLazy(segs).ToArray());
                                                    var pt1 = segs.Last().EndPoint;
                                                    var pt2 = pt1.OffsetXY(UNDERACHIEVEMENT, -THESAURUSALCOVE);
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.GetWaterBucketChName(), THESAURUSDETEST, pt1));
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.DN, THESAURUSDETEST, pt2));
                                                }
                                                break;
                                            case WaterBucketEnum.Side:
                                                {
                                                    var relativeYOffsetToStorey = -THESAURUSREQUISITION;
                                                    var pt = basePt.OffsetY(relativeYOffsetToStorey);
                                                    Dr.DrawSideWaterBucket(basePt.ToPoint3d());
                                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSDULCET, THESAURUSGLOSSY), new Vector2d(-THESAURUSPULSATE, THESAURUSPULSATE), new Vector2d(-THESAURUSDESTRUCTIVE + PARADEIGMATIKOS, NARCOTRAFICANTE) };
                                                    var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                                    Dr.SetLabelStylesForRainNote(DrawLineSegmentsLazy(segs).ToArray());
                                                    var pt1 = segs.Last().EndPoint;
                                                    var pt2 = pt1.OffsetXY(UNDERACHIEVEMENT, -THESAURUSALCOVE);
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.GetWaterBucketChName(), THESAURUSDETEST, pt1));
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.DN, THESAURUSDETEST, pt2));
                                                }
                                                break;
                                            default:
                                                throw new System.Exception();
                                        }
                                    }
                                }
                                {
                                    if (storey == THESAURUSALCOHOLIC)
                                    {
                                        var basePt = info.EndPoint;
                                        var text = gpItem.OutletWrappingPipeRadius;
                                        if (text != null)
                                        {
                                            var p1 = basePt + new Vector2d(-THESAURUSWAYWARD, -THESAURUSINIQUITOUS);
                                            var p2 = p1.OffsetY(-QUOTATIONCAPABLE);
                                            var p3 = p2.OffsetX(HYDROSTATICALLY);
                                            var layer = VERGELTUNGSWAFFE;
                                            DrawLine(layer, new GLineSegment(p1, p2));
                                            DrawLine(layer, new GLineSegment(p3, p2));
                                            DrawStoreyHeightSymbol(p3, VERGELTUNGSWAFFE, text);
                                            {
                                                var shadow = THESAURUSREDOUND;
                                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                {
                                                    Dr.DrawSimpleLabel(p3, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                }
                                            }
                                        }
                                        if (gpItem.HasWaterWell)
                                        {
                                            static void _DrawRainWaterWells(Point2d pt, List<string> values)
                                            {
                                                if (values == null) return;
                                                values = values.OrderBy(x =>
                                                {
                                                    long.TryParse(x, out long v);
                                                    return v;
                                                }).ThenBy(x => x).ToList();
                                                if (values.Count == ADRENOCORTICOTROPHIC)
                                                {
                                                    DrawRainWaterWell(pt, values[NARCOTRAFICANTE]);
                                                }
                                                else if (values.Count >= PHOTOGONIOMETER)
                                                {
                                                    var pts = GetBasePoints(pt.OffsetX(-THESAURUSORNAMENT), PHOTOGONIOMETER, values.Count, THESAURUSORNAMENT, THESAURUSORNAMENT).ToList();
                                                    for (int i = NARCOTRAFICANTE; i < values.Count; i++)
                                                    {
                                                        DrawRainWaterWell(pts[i], values[i]);
                                                    }
                                                }
                                            }
                                            {
                                                var fixY = -HYDROSTATICALLY;
                                                var v = new Vector2d(-THESAURUSDIRECTIVE - THESAURUSALCOVE, -VENTRILOQUISTIC + IRREMUNERABILIS + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                {
                                                    var shadow = THESAURUSREDOUND;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                    {
                                                        Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                    }
                                                }
                                                _DrawRainWaterWells(pt, values);
                                                var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, fixY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSARTISAN + THESAURUSLEVITATE, UNDERACHIEVEMENT);
                                                        DrawNoteText(getPipeDn(), segs[PHOTOGONIOMETER].EndPoint + v1);
                                                    }
                                                    drawDomePipes(segs);
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSUSABLE);
                                                        if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                                        {
                                                            if (!gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(THESAURUSSACRIFICE).ToPoint3d());
                                                                {
                                                                    var shadow = THESAURUSREDOUND;
                                                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                                    {
                                                                        Dr.DrawSimpleLabel(p, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(THESAURUSSACRIFICE).ToPoint3d());
                                                                {
                                                                    var shadow = THESAURUSREDOUND;
                                                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                                    {
                                                                        Dr.DrawSimpleLabel(p, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            DrawWrappingPipe(p.ToPoint3d());
                                                            {
                                                                var shadow = THESAURUSREDOUND;
                                                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                                {
                                                                    Dr.DrawSimpleLabel(p, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                            {
                                                var fixX = -THESAURUSARTISAN - THESAURUSPURPORT;
                                                var fixY = THESAURUSORNAMENT;
                                                var fixV = new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY);
                                                var p = basePt + new Vector2d(TRINITROTOLUENE + fixX, -THESAURUSWRANGLE);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY, THESAURUSREDOUND);
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSCOTERIE), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-ELECTROMYOGRAPH, NARCOTRAFICANTE), new Vector2d(-KHRUSELEPHANTINOS, THESAURUSSILENT) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(p, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -CONSERVATIVENESS + fixY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-AEROTHERMODYNAMICS - fixX, NARCOTRAFICANTE) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(p, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.RainPort)
                                        {
                                            {
                                                var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -HYDROSTATICALLY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE) };
                                                var segs = vecs.ToGLineSegments(basePt);
                                                drawDomePipes(segs);
                                                {
                                                    var v1 = new Vector2d(THESAURUSARTISAN + THESAURUSLEVITATE, UNDERACHIEVEMENT);
                                                    DrawNoteText(getPipeDn(), segs[PHOTOGONIOMETER].EndPoint + v1);
                                                }
                                                var pt = segs.Last().EndPoint.ToPoint3d();
                                                {
                                                    Dr.DrawRainPort(pt.OffsetX(THESAURUSALCOVE));
                                                    Dr.DrawRainPortLabel(pt.OffsetX(-UNDERACHIEVEMENT));
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSUSABLE);
                                                        DrawWrappingPipe(p.ToPoint3d());
                                                        if (gpItem.HasOutletWrappingPipe)
                                                        {
                                                            if (gpItem.HasSingleFloorDrainDrainageForRainPort && !gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort)
                                                            {
                                                                DrawWrappingPipe((basePt + new Vector2d(-THESAURUSGAINSAY, -THESAURUSINIQUITOUS - QUINALBARBITONE)).ToPoint3d());
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                    }
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt.ToPoint2d(), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForRainPort)
                                            {
                                                var fixX = -THESAURUSARTISAN - THESAURUSPURPORT;
                                                var fixY = THESAURUSORNAMENT;
                                                var fixV = new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY);
                                                var p = basePt + new Vector2d(TRINITROTOLUENE + fixX, -THESAURUSWRANGLE);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY, THESAURUSREDOUND);
                                                DrawNoteText(getHDN(), p + new Vector2d(-SACCHAROMYCETACEAE + (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort ? THESAURUSLEVITATE : NARCOTRAFICANTE), -PHOTOCONVERSION));
                                                var pt = p + fixV;
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSCOTERIE), new Vector2d(-INTERPRETATIVELY, -INTERPRETATIVELY), new Vector2d(-QUOTATIONIXIONIAN, NARCOTRAFICANTE), new Vector2d(-PROFESSIONALISM, THESAURUSEMBRACE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSFEASIBILITY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSCRITICISM, NARCOTRAFICANTE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.WaterSealingWell)
                                        {
                                            {
                                                if (gpItem.HasOutletWrappingPipe)
                                                {
                                                    DrawWrappingPipe((basePt + new Vector2d(-THESAURUSGAINSAY, -THESAURUSINIQUITOUS)).ToPoint3d());
                                                    if (gpItem.HasSingleFloorDrainDrainageForWaterSealingWell && !gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell)
                                                    {
                                                        DrawWrappingPipe((basePt + new Vector2d(-THESAURUSGAINSAY, -THESAURUSINIQUITOUS - QUINALBARBITONE)).ToPoint3d());
                                                    }
                                                }
                                                var fixY = -HYDROSTATICALLY;
                                                var v = new Vector2d(-THESAURUSDIRECTIVE - THESAURUSALCOVE, -VENTRILOQUISTIC + IRREMUNERABILIS + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                DrawWaterSealingWell(pt.OffsetY(-THESAURUSENTREAT));
                                                var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, fixY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSARTISAN + THESAURUSLEVITATE, UNDERACHIEVEMENT);
                                                        DrawNoteText(getPipeDn(), segs[PHOTOGONIOMETER].EndPoint + v1);
                                                    }
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterSealingWell)
                                            {
                                                var fixX = -THESAURUSARTISAN - THESAURUSPURPORT;
                                                var fixY = THESAURUSORNAMENT;
                                                var fixV = new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY);
                                                var p = basePt + new Vector2d(TRINITROTOLUENE + fixX, -THESAURUSWRANGLE);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY, THESAURUSREDOUND);
                                                DrawNoteText(getHDN(), p + new Vector2d(-SACCHAROMYCETACEAE + (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort ? THESAURUSLEVITATE : NARCOTRAFICANTE), -PHOTOCONVERSION));
                                                var pt = p + fixV;
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSCOTERIE), new Vector2d(-INTERPRETATIVELY, -INTERPRETATIVELY), new Vector2d(-QUOTATIONIXIONIAN, NARCOTRAFICANTE), new Vector2d(-PROFESSIONALISM, THESAURUSEMBRACE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSFEASIBILITY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSCRITICISM, NARCOTRAFICANTE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.Ditch)
                                        {
                                            if (storey != null)
                                            {
                                                {
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        DrawWrappingPipe((basePt + new Vector2d(-THESAURUSGAINSAY, -THESAURUSINIQUITOUS)).ToPoint3d());
                                                        if (gpItem.HasSingleFloorDrainDrainageForDitch && !gpItem.IsFloorDrainShareDrainageWithVerticalPipeForDitch)
                                                        {
                                                            DrawWrappingPipe((basePt + new Vector2d(-THESAURUSGAINSAY, -THESAURUSINIQUITOUS - QUINALBARBITONE)).ToPoint3d());
                                                        }
                                                    }
                                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -HYDROSTATICALLY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE) };
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    drawDomePipes(segs);
                                                    var pt = segs.Last().EndPoint.ToPoint3d();
                                                    Dr.DrawLabel(pt.OffsetX(-UNDERACHIEVEMENT), THESAURUSFRANTIC);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSARTISAN + THESAURUSLEVITATE, UNDERACHIEVEMENT);
                                                        DrawNoteText(getPipeDn(), segs[PHOTOGONIOMETER].EndPoint + v1);
                                                    }
                                                    {
                                                        var shadow = THESAURUSREDOUND;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                        {
                                                            Dr.DrawSimpleLabel(pt.ToPoint2d(), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                        }
                                                    }
                                                }
                                                if (gpItem.HasSingleFloorDrainDrainageForDitch)
                                                {
                                                    var fixX = -THESAURUSARTISAN - THESAURUSPURPORT;
                                                    var fixY = THESAURUSORNAMENT;
                                                    var fixV = new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY);
                                                    var p = basePt + new Vector2d(TRINITROTOLUENE + fixX, -THESAURUSWRANGLE);
                                                    _DrawFloorDrain(p.ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY, THESAURUSREDOUND);
                                                    DrawNoteText(getHDN(), p + new Vector2d(-THESAURUSACCOMPLISHMENT, -THESAURUSSLOBBER) + new Vector2d(-IMPLICATIONALLY + (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort ? THESAURUSLEVITATE : NARCOTRAFICANTE), -THESAURUSCESSATION));
                                                    var pt = p + fixV;
                                                    if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForDitch)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSCOTERIE), new Vector2d(-INTERPRETATIVELY, -INTERPRETATIVELY), new Vector2d(-QUOTATIONIXIONIAN, NARCOTRAFICANTE), new Vector2d(-PROFESSIONALISM, THESAURUSEMBRACE) };
                                                        var segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        {
                                                            var shadow = THESAURUSREDOUND;
                                                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                            {
                                                                Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSFEASIBILITY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSCRITICISM, NARCOTRAFICANTE) };
                                                        var segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        {
                                                            var shadow = THESAURUSREDOUND;
                                                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                                            {
                                                                Dr.DrawSimpleLabel(pt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _dy = THESAURUSINDUSTRY;
                                                var vecs = vecs0;
                                                var p1 = info.StartPoint;
                                                var p2 = p1 + new Vector2d(NARCOTRAFICANTE, -THESAURUSINSTEAD - dy + _dy);
                                                var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                info.DisplaySegs = segs;
                                                var p = basePt.OffsetY(THESAURUSINDUSTRY);
                                                drawLabel(p, THESAURUSFRANTIC, null, UNTRACEABLENESS);
                                                static void DrawDimLabelRight(Point3d basePt, double dy)
                                                {
                                                    var pt1 = basePt;
                                                    var pt2 = pt1.OffsetY(dy);
                                                    var dim = new AlignedDimension();
                                                    dim.XLine1Point = pt1;
                                                    dim.XLine2Point = pt2;
                                                    dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-THESAURUSALCOVE);
                                                    dim.DimensionText = THESAURUSCOMMODIOUS;
                                                    dim.Layer = TRANYLCYPROMINE;
                                                    ByLayer(dim);
                                                    DrawEntityLazy(dim);
                                                }
                                                DrawDimLabelRight(p.ToPoint3d(), -THESAURUSINDUSTRY);
                                            }
                                        }
                                        else
                                        {
                                            var ditchDy = THESAURUSINDUSTRY;
                                            var _run = runs.TryGet(i);
                                            if (_run != null)
                                            {
                                                if (gpItem == null)
                                                {
                                                    if (_run != null)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -HYDROSTATICALLY), new Vector2d(-THESAURUSENTREAT, -THESAURUSENTREAT), new Vector2d(-THESAURUSFRIENDLY, NARCOTRAFICANTE) };
                                                        var p = info.EndPoint;
                                                        var segs = vecs.ToGLineSegments(p);
                                                        drawDomePipes(segs);
                                                        segs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSSACRIFICE), new Vector2d(-THESAURUSWAYWARD, NARCOTRAFICANTE) }.ToGLineSegments(segs.Last().EndPoint);
                                                        foreach (var line in DrawLineSegmentsLazy(segs))
                                                        {
                                                            Dr.SetLabelStylesForRainNote(line);
                                                        }
                                                        {
                                                            var t = DrawTextLazy(THESAURUSFRANTIC, THESAURUSDETEST, segs.Last().EndPoint.OffsetXY(UNDERACHIEVEMENT, UNDERACHIEVEMENT));
                                                            Dr.SetLabelStylesForRainNote(t);
                                                        }
                                                    }
                                                    else if (!_run.HasLongTranslator && !_run.HasShortTranslator)
                                                    {
                                                        _dy = THESAURUSINDUSTRY;
                                                        var vecs = vecs0;
                                                        var p1 = info.StartPoint;
                                                        var p2 = p1 + new Vector2d(NARCOTRAFICANTE, -THESAURUSINSTEAD - dy + _dy);
                                                        var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                        info.DisplaySegs = segs;
                                                        var p = basePt.OffsetY(THESAURUSINDUSTRY);
                                                        drawLabel(p, THESAURUSFRANTIC, null, UNTRACEABLENESS);
                                                        static void DrawDimLabelRight(Point3d basePt, double dy)
                                                        {
                                                            var pt1 = basePt;
                                                            var pt2 = pt1.OffsetY(dy);
                                                            var dim = new AlignedDimension();
                                                            dim.XLine1Point = pt1;
                                                            dim.XLine2Point = pt2;
                                                            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-THESAURUSALCOVE);
                                                            dim.DimensionText = THESAURUSCOMMODIOUS;
                                                            dim.Layer = TRANYLCYPROMINE;
                                                            ByLayer(dim);
                                                            DrawEntityLazy(dim);
                                                        }
                                                        DrawDimLabelRight(p.ToPoint3d(), -THESAURUSINDUSTRY);
                                                    }
                                                    else
                                                    {
                                                        Dr.DrawLabel(basePt.ToPoint3d(), THESAURUSFRANTIC);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                void _DrawCondensePipe(Point2d basePt)
                                {
                                    list.Add(basePt.OffsetY(THESAURUSINDUSTRY));
                                    Dr.DrawCondensePipe(basePt.OffsetXY(-THESAURUSINDUSTRY, UNDERACHIEVEMENT));
                                }
                                void _drawFloorDrain(Point2d basePt, bool leftOrRight, string shadow)
                                {
                                    list.Add(basePt.OffsetY(QUINALBARBITONE));
                                    var value = TRANSUBSTANTIALLY;
                                    if (gpItem.Hangings[i].HasSideFloorDrain) value = THESAURUSUNCOUTH;
                                    if (leftOrRight)
                                    {
                                        _DrawFloorDrain(basePt.OffsetXY(THESAURUSANCILLARY + STEPMOTHERLINESS, THESAURUSANCILLARY).ToPoint3d(), leftOrRight, value, shadow);
                                    }
                                    else
                                    {
                                        _DrawFloorDrain(basePt.OffsetXY(THESAURUSANCILLARY + STEPMOTHERLINESS - THESAURUSDEFERENCE, THESAURUSANCILLARY).ToPoint3d(), leftOrRight, value, shadow);
                                    }
                                    return;
                                }
                                {
                                    void drawDN(string dn, Point2d pt)
                                    {
                                        var t = DrawTextLazy(dn, THESAURUSDETEST, pt);
                                        Dr.SetLabelStylesForRainDims(t);
                                    }
                                    var hanging = gpItem.Hangings[i];
                                    var fixW2 = gpItem.Hangings.All(x => (x?.FloorDrainWrappingPipesCount ?? NARCOTRAFICANTE) == NARCOTRAFICANTE) ? HYDROSTATICALLY : NARCOTRAFICANTE;
                                    var fixW = VENTRILOQUISTIC - fixW2;
                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSTROUPE, THESAURUSTROUPE), new Vector2d(-QUOTATIONMAXWELL - fixW, NARCOTRAFICANTE) };
                                    if (getHasAirConditionerFloorDrain(i))
                                    {
                                        var p1 = info.StartPoint.OffsetY(-THESAURUSORNAMENT);
                                        var p2 = p1.OffsetX(THESAURUSDIRECTIVE);
                                        var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                                        Dr.SetLabelStylesForWNote(line);
                                        var segs = vecs.GetYAxisMirror().ToGLineSegments(p1.OffsetY(-THESAURUSOBEISANCE - THESAURUSENCOURAGEMENT));
                                        drawDomePipes(segs);
                                        var p = segs.Last().EndPoint;
                                        _drawFloorDrain(p, THESAURUSSEMBLANCE, THESAURUSREDOUND);
                                        drawDN(ENDONUCLEOLYTIC, segs[ADRENOCORTICOTROPHIC].StartPoint.OffsetXY(THESAURUSINDUSTRY + fixW, THESAURUSINDUSTRY));
                                    }
                                    if (hanging.FloorDrainsCount > NARCOTRAFICANTE)
                                    {
                                        var bsPt = info.EndPoint;
                                        var wpCount = hanging.FloorDrainWrappingPipesCount;
                                        void tryDrawWrappingPipe(Point2d pt)
                                        {
                                            if (wpCount <= NARCOTRAFICANTE) return;
                                            DrawWrappingPipe(pt.ToPoint3d());
                                            --wpCount;
                                        }
                                        if (hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC)
                                        {
                                            if (!(storey == THESAURUSALCOHOLIC && (gpItem.HasSingleFloorDrainDrainageForWaterWell || gpItem.HasSingleFloorDrainDrainageForRainPort)))
                                            {
                                                var v = default(Vector2d);
                                                var ok = UNTRACEABLENESS;
                                                if (gpItem.Items.TryGet(i - ADRENOCORTICOTROPHIC).HasLong)
                                                {
                                                    if (runs[i - ADRENOCORTICOTROPHIC].IsLongTranslatorToLeftOrRight)
                                                    {
                                                        var p = vecs.GetLastPoint(bsPt.OffsetY(-THESAURUSOBEISANCE - THESAURUSENCOURAGEMENT) + v);
                                                        var _vecs = new List<Vector2d> { new Vector2d(-THESAURUSENTREAT, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSEXPLETIVE), new Vector2d(CONSTITUTIONALLY, -THESAURUSFRIGHT) };
                                                        var segs = _vecs.ToGLineSegments(p);
                                                        _drawFloorDrain(p, THESAURUSSEMBLANCE, THESAURUSREDOUND);
                                                        drawDomePipes(segs);
                                                        tryDrawWrappingPipe(p.OffsetX(THESAURUSARTISAN));
                                                        var __vecs = new List<Vector2d> { new Vector2d(-ELECTROMYOGRAPH, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -SUBORDINATIONISTS), new Vector2d(NARCOTRAFICANTE, -THESAURUSINTENTIONAL) };
                                                        var seg = __vecs.ToGLineSegments(info.EndPoint).Last();
                                                        static void DrawDimLabel(Point2d pt1, Point2d pt2, Vector2d v, string text, string layer)
                                                        {
                                                            var dim = new AlignedDimension();
                                                            dim.XLine1Point = pt1.ToPoint3d();
                                                            dim.XLine2Point = pt2.ToPoint3d();
                                                            dim.DimLinePoint = (GeoAlgorithm.MidPoint(pt1, pt2) + v).ToPoint3d();
                                                            dim.DimensionText = text;
                                                            dim.Layer = layer;
                                                            ByLayer(dim);
                                                            DrawEntityLazy(dim);
                                                        }
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(-QUOTATIONTRILINEAR, NARCOTRAFICANTE), BASIDIOMYCOTINA, TRANYLCYPROMINE);
                                                        ok = THESAURUSSEMBLANCE;
                                                    }
                                                }
                                                if (!ok)
                                                {
                                                    var segs = vecs.ToGLineSegments(bsPt.OffsetY(-THESAURUSOBEISANCE - THESAURUSENCOURAGEMENT) + v);
                                                    drawDomePipes(segs);
                                                    var p = segs.Last().EndPoint;
                                                    _drawFloorDrain(p, THESAURUSSEMBLANCE, THESAURUSREDOUND);
                                                    tryDrawWrappingPipe(p.OffsetX(THESAURUSARTISAN));
                                                    drawDN(getHDN(), segs[ADRENOCORTICOTROPHIC].EndPoint.OffsetXY(THESAURUSINDUSTRY + fixW, THESAURUSINDUSTRY));
                                                    ok = THESAURUSSEMBLANCE;
                                                }
                                            }
                                        }
                                        else if (hanging.FloorDrainsCount == PHOTOGONIOMETER)
                                        {
                                            {
                                                var segs = vecs.ToGLineSegments(bsPt.OffsetY(-THESAURUSOBEISANCE - THESAURUSENCOURAGEMENT));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _drawFloorDrain(p, THESAURUSSEMBLANCE, THESAURUSREDOUND);
                                                tryDrawWrappingPipe(p.OffsetX(THESAURUSARTISAN));
                                                drawDN(getHDN(), segs[ADRENOCORTICOTROPHIC].EndPoint.OffsetXY(THESAURUSINDUSTRY + fixW, THESAURUSINDUSTRY));
                                            }
                                            {
                                                var segs = vecs.GetYAxisMirror().ToGLineSegments(info.EndPoint.OffsetY(-THESAURUSOBEISANCE - THESAURUSENCOURAGEMENT));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _drawFloorDrain(p, THESAURUSSEMBLANCE, THESAURUSREDOUND);
                                                tryDrawWrappingPipe(p.OffsetX(-THESAURUSENTREAT));
                                                drawDN(getHDN(), segs[ADRENOCORTICOTROPHIC].StartPoint.OffsetXY(THESAURUSINDUSTRY + fixW, THESAURUSINDUSTRY));
                                            }
                                        }
                                    }
                                    if (hanging.HasCondensePipe)
                                    {
                                        string getCondensePipeDN()
                                        {
                                            return viewModel?.Params.CondensePipeHorizontalDN ?? ENDONUCLEOLYTIC;
                                        }
                                        var h = THESAURUSLEVITATE;
                                        var w = HYDROSTATICALLY;
                                        if (hanging.HasBrokenCondensePipes)
                                        {
                                            var v = new Vector2d(NARCOTRAFICANTE, -UNDERACHIEVEMENT);
                                            void f(double offsetY)
                                            {
                                                var segs = vecs.ToGLineSegments((info.StartPoint + v).OffsetY(offsetY));
                                                var p1 = segs.Last().EndPoint;
                                                var p3 = p1.OffsetY(h);
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3.OffsetXY(-THESAURUSINDUSTRY, THESAURUSINDUSTRY));
                                                drawDomePipes(segs);
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(THESAURUSINDUSTRY + fixW, -UNDERACHIEVEMENT));
                                            }
                                            f(-THESAURUSCONDITIONAL);
                                            f(-THESAURUSCONDITIONAL - THESAURUSFLUENT);
                                        }
                                        else
                                        {
                                            double fixY = -THESAURUSINTENTIONAL;
                                            var higher = hanging.PlsDrawCondensePipeHigher;
                                            if (higher)
                                            {
                                                fixY += HYDROSTATICALLY;
                                            }
                                            var v = new Vector2d(NARCOTRAFICANTE, fixY);
                                            var segs = vecs.ToGLineSegments((info.StartPoint + v).OffsetY(-THESAURUSCONDITIONAL));
                                            var p1 = segs.Last().EndPoint;
                                            var p2 = p1.OffsetX(w);
                                            var p3 = p1.OffsetY(h);
                                            var p4 = p2.OffsetY(h);
                                            drawDomePipes(segs);
                                            if (hanging.HasNonBrokenCondensePipes)
                                            {
                                                double _fixY = higher ? -THESAURUSOBEISANCE : NARCOTRAFICANTE;
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(THESAURUSINDUSTRY + fixW, UNDERACHIEVEMENT + UNDERACHIEVEMENT + _fixY));
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3);
                                                drawDomePipe(new GLineSegment(p2, p4));
                                                _DrawCondensePipe(p4);
                                            }
                                            else
                                            {
                                                double _fixY = higher ? -THESAURUSOBEISANCE + THESAURUSINDUSTRY : NARCOTRAFICANTE;
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(THESAURUSINDUSTRY + fixW, -UNDERACHIEVEMENT + _fixY));
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3);
                                            }
                                        }
                                    }
                                }
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                if (run == null) continue;
                                if (run.HasShortTranslator)
                                {
                                    DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                                }
                                if (gpItem.Hangings[i].HasCheckPoint)
                                {
                                    var h = Math.Round(HEIGHT / THESAURUSCONTINGENT * THESAURUSFACTOR);
                                    var p = run.HasShortTranslator ? info.Segs.Last().StartPoint.OffsetY(h).ToPoint3d() : info.EndPoint.OffsetY(h).ToPoint3d();
                                    DrawCheckPoint(p, THESAURUSSEMBLANCE);
                                    var seg = info.Segs.Last();
                                    var fixDy = run.HasShortTranslator ? -seg.Height : NARCOTRAFICANTE;
                                    DrawDimLabelRight(p, fixDy - h);
                                }
                            }
                        }
                        if (list.Count > NARCOTRAFICANTE)
                        {
                            var my = list.Select(x => x.Y).Max();
                            foreach (var pt in list)
                            {
                                if (pt.Y == my)
                                {
                                }
                            }
                            var h = QUOTATIONTRILINEAR - THESAURUSDEMONSTRATION;
                            foreach (var pt in list)
                            {
                                if (pt.Y != my) continue;
                                var ok = UNTRACEABLENESS;
                                {
                                    var storey = gapsf(pt.ToNTSPoint()).FirstOrDefault()?.UserData as string;
                                    if (storey != null)
                                    {
                                        var i = allStoreys.IndexOf(storey);
                                        var p = infos[i].BasePoint;
                                        if (shouldDrawAringSymbol)
                                        {
                                            if (!hasDrawedAiringSymbol) drawDomePipe(new GLineSegment(p, p.OffsetY(h)));
                                            _DrawAiringSymbol(p.OffsetY(h));
                                        }
                                        else
                                        {
                                            linesKillers.Add(new GLineSegment(pt.OffsetY(-THESAURUSEMBASSY), pt.OffsetY(-THESAURUSEMBASSY).OffsetX(COMPREHENSIBILITY)).ToLineString());
                                        }
                                        ok = THESAURUSSEMBLANCE;
                                    }
                                }
                                if (!ok)
                                {
                                    var storey = storeySpacesf(pt.ToNTSPoint()).FirstOrDefault()?.UserData as string;
                                    if (storey != null)
                                    {
                                        var i = allStoreys.IndexOf(storey);
                                        var p = infos[i].BasePoint;
                                        if (shouldDrawAringSymbol)
                                        {
                                            if (!hasDrawedAiringSymbol)
                                            {
                                                var line = DrawLineSegmentLazy(new GLineSegment(p, p.OffsetY(h)));
                                                line.Layer = dome_layer;
                                                ByLayer(line);
                                                linesKillers.Add(new GLineSegment(pt, pt.OffsetX(COMPREHENSIBILITY)).ToLineString());
                                            }
                                            _DrawAiringSymbol(p.OffsetY(h));
                                        }
                                        else
                                        {
                                            linesKillers.Add(new GLineSegment(pt, pt.OffsetX(COMPREHENSIBILITY)).ToLineString());
                                        }
                                        ok = THESAURUSSEMBLANCE;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (shouldDrawAringSymbol && !hasDrawedAiringSymbol)
                            {
                                var info = infos.Where(x => IsNumStorey(x.Storey)).LastOrDefault(x => x.Visible);
                                if (info != null)
                                {
                                    var pt = info.BasePoint;
                                    linesKillers.Add(new GLineSegment(pt.OffsetXY(ADRENOCORTICOTROPHIC, ADRENOCORTICOTROPHIC + THESAURUSARTISAN), pt.OffsetXY(-ADRENOCORTICOTROPHIC, ADRENOCORTICOTROPHIC + THESAURUSARTISAN)).ToLineString());
                                    _DrawAiringSymbol(pt.OffsetY(THESAURUSARTISAN));
                                    drawDomePipe(new GLineSegment(pt, pt.OffsetY(THESAURUSARTISAN)));
                                }
                            }
                        }
                    }
                    bool getHasAirConditionerFloorDrain(int i)
                    {
                        if (gpItem.PipeType != PipeType.Y2L) return UNTRACEABLENESS;
                        var hanging = gpItem.Hangings[i];
                        if (hanging.HasBrokenCondensePipes || hanging.HasNonBrokenCondensePipes)
                        {
                            return viewModel?.Params.HasAirConditionerFloorDrain ?? UNTRACEABLENESS;
                        }
                        return UNTRACEABLENESS;
                    }
                    var infos = getPipeRunLocationInfos(basePoint.OffsetX(dx));
                    handlePipeLine(thwPipeLine, infos);
                    static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight, double height = THESAURUSDETEST)
                    {
                        var gap = UNDERACHIEVEMENT;
                        var factor = LYMPHANGIOMATOUS;
                        var (w1, _) = GetDBTextSize(text1, THESAURUSDETEST, LYMPHANGIOMATOUS, THESAURUSTRAFFIC);
                        var (w2, _) = GetDBTextSize(text2, THESAURUSDETEST, LYMPHANGIOMATOUS, THESAURUSTRAFFIC);
                        var width = Math.Max(w1, w2);
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSALCOVE, THESAURUSALCOVE), new Vector2d(width, NARCOTRAFICANTE) };
                        if (isLeftOrRight == THESAURUSSEMBLANCE)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[ADRENOCORTICOTROPHIC].EndPoint : segs[ADRENOCORTICOTROPHIC].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(gap);
                        if (text1 != null)
                        {
                            var t = DrawTextLazy(text1, height, txtBasePt);
                            Dr.SetLabelStylesForRainNote(t);
                        }
                        if (text2 != null)
                        {
                            var t = DrawTextLazy(text2, height, txtBasePt.OffsetY(-height - gap));
                            Dr.SetLabelStylesForRainNote(t);
                        }
                    }
                    static void drawLabel2(Point2d basePt, string text1, string text2, bool isLeftOrRight)
                    {
                        var height = THESAURUSDETEST;
                        var gap = UNDERACHIEVEMENT;
                        var factor = LYMPHANGIOMATOUS;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? NARCOTRAFICANTE, text2?.Length ?? NARCOTRAFICANTE);
                        if (width < THESAURUSWAYWARD) width = THESAURUSWAYWARD;
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSLOADED, NARCOTRAFICANTE), new Vector2d(width, NARCOTRAFICANTE) };
                        if (isLeftOrRight == THESAURUSSEMBLANCE)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[ADRENOCORTICOTROPHIC].EndPoint : segs[ADRENOCORTICOTROPHIC].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(gap);
                        if (text1 != null)
                        {
                            var t = DrawTextLazy(text1, height, txtBasePt);
                            Dr.SetLabelStylesForRainNote(t);
                        }
                        if (text2 != null)
                        {
                            var t = DrawTextLazy(text2, height, txtBasePt.OffsetY(-height - gap));
                            Dr.SetLabelStylesForRainNote(t);
                        }
                    }
                    for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            var segs = info.DisplaySegs ?? info.Segs;
                            if (segs != null)
                            {
                                drawDomePipes(segs);
                            }
                        }
                        var storey = allStoreys[i];
                        if (storey == THESAURUSADHERE && gpItem.HasLineAtBuildingFinishedSurfice)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                            drawDomePipe(new GLineSegment(p1, p2));
                        }
                    }
                    {
                        var has_label_storeys = new HashSet<string>();
                        {
                            var _storeys = new string[] { allNumStoreyLabels.GetAt(PHOTOGONIOMETER), allNumStoreyLabels.GetLastOrDefault(THESAURUSPOSTSCRIPT) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == NARCOTRAFICANTE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            _storeys = _storeys.Where(storey =>
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = infos.TryGet(i);
                                return info != null && info.Visible;
                            }).ToList();
                            if (_storeys.Count == NARCOTRAFICANTE)
                            {
                                _storeys = allNumStoreyLabels.Where(storey =>
                                {
                                    var i = allNumStoreyLabels.IndexOf(storey);
                                    var info = infos.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(ADRENOCORTICOTROPHIC).ToList();
                            }
                            if (_storeys.Count == NARCOTRAFICANTE)
                            {
                                _storeys = allStoreys.Where(storey =>
                                {
                                    var i = allStoreys.IndexOf(storey);
                                    var info = infos.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(ADRENOCORTICOTROPHIC).ToList();
                            }
                            foreach (var storey in _storeys)
                            {
                                has_label_storeys.Add(storey);
                                var i = allStoreys.IndexOf(storey);
                                var info = infos[i];
                                {
                                    string label1, label2;
                                    var labels = RainLabelItem.ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x)).ToList()).OrderBy(x => x).ToList();
                                    if (labels.Count == PHOTOGONIOMETER)
                                    {
                                        label1 = labels[NARCOTRAFICANTE];
                                        label2 = labels[ADRENOCORTICOTROPHIC];
                                    }
                                    else
                                    {
                                        label1 = labels.JoinWith(THESAURUSPOSITION);
                                        label2 = null;
                                    }
                                    var isLeftOrRight = (gpItem.Hangings.TryGet(i)?.FloorDrainsCount ?? NARCOTRAFICANTE) == NARCOTRAFICANTE && !(gpItem.Hangings.TryGet(i)?.HasCondensePipe ?? UNTRACEABLENESS);
                                    {
                                        var run = runs.TryGet(i);
                                        if (run != null)
                                        {
                                            if (run.HasLongTranslator)
                                            {
                                                isLeftOrRight = run.IsLongTranslatorToLeftOrRight;
                                            }
                                        }
                                        var (ok, item) = gpItem.Items.TryGetValue(i);
                                        if (ok)
                                        {
                                            if (item.HasShort)
                                            {
                                                isLeftOrRight = UNTRACEABLENESS;
                                            }
                                        }
                                    }
                                    if (getHasAirConditionerFloorDrain(i) && isLeftOrRight == UNTRACEABLENESS)
                                    {
                                        var pt = info.EndPoint.OffsetY(THESAURUSLEVITATE);
                                        if (storey == THESAURUSADHERE)
                                        {
                                            pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        drawLabel2(pt, label1, label2, isLeftOrRight);
                                    }
                                    else
                                    {
                                        var pt = info.PlBasePt;
                                        if (storey == THESAURUSADHERE)
                                        {
                                            pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        drawLabel(pt, label1, label2, isLeftOrRight);
                                    }
                                }
                            }
                        }
                        {
                            var _allSmoothStoreys = new List<string>();
                            {
                                bool isMinFloor = THESAURUSSEMBLANCE;
                                for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
                                {
                                    var (ok, item) = gpItem.Items.TryGetValue(i);
                                    if (!(ok && item.Exist)) continue;
                                    if (item.Exist && isMinFloor)
                                    {
                                        isMinFloor = UNTRACEABLENESS;
                                        continue;
                                    }
                                    var storey = allNumStoreyLabels[i];
                                    if (has_label_storeys.Contains(storey)) continue;
                                    var run = runs.TryGet(i);
                                    if (run == null) continue;
                                    if (!run.HasLongTranslator && !run.HasShortTranslator && (!(gpItem.Hangings.TryGet(i)?.HasCheckPoint ?? UNTRACEABLENESS)))
                                    {
                                        _allSmoothStoreys.Add(storey);
                                    }
                                }
                            }
                            var _storeys = new string[] { _allSmoothStoreys.GetAt(NARCOTRAFICANTE), _allSmoothStoreys.GetLastOrDefault(PHOTOGONIOMETER) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == NARCOTRAFICANTE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.GetAt(ADRENOCORTICOTROPHIC), allNumStoreyLabels.GetLastOrDefault(PHOTOGONIOMETER) }.SelectNotNull().Distinct().ToList();
                            }
                            if (_storeys.Count == NARCOTRAFICANTE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            var dn = getPipeDn();
                            foreach (var storey in _storeys)
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = infos.TryGet(i);
                                if (info != null && info.Visible)
                                {
                                    var run = runs.TryGet(i);
                                    if (run != null)
                                    {
                                        Dr.DrawDN_2(info.EndPoint.OffsetX(THESAURUSSACRIFICE), VERGELTUNGSWAFFE, dn);
                                    }
                                }
                            }
                        }
                    }
                    if (linesKillers.Count > NARCOTRAFICANTE)
                    {
                        dome_lines = GeoFac.ToNodedLineSegments(dome_lines);
                        var geos = dome_lines.Select(x => x.ToLineString()).ToList();
                        dome_lines = geos.Except(GeoFac.CreateIntersectsSelector(geos)(GeoFac.CreateGeometryEx(linesKillers.ToList()))).Cast<LineString>().SelectMany(x => x.ToGLineSegments()).ToList();
                    }
                    {
                        var auto_conn = UNTRACEABLENESS;
                        if (auto_conn)
                        {
                            foreach (var g in GeoFac.GroupParallelLines(dome_lines, ADRENOCORTICOTROPHIC, QUOTATIONEXOPHTHALMIC))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: QUOTATIONNAPIERIAN));
                                line.Layer = dome_layer;
                                ByLayer(line);
                            }
                        }
                        else
                        {
                            foreach (var dome_line in dome_lines)
                            {
                                var line = DrawLineSegmentLazy(dome_line);
                                line.Layer = dome_layer;
                                ByLayer(line);
                            }
                        }
                    }
                }
            }
            public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value)
            {
                if (leftOrRight)
                {
                    if (value == THESAURUSUNCOUTH)
                    {
                        basePt += new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY).ToVector3d();
                    }
                    DrawBlockReference(RECONSTRUCTIONAL, basePt, br =>
                    {
                        br.Layer = THESAURUSSANCTITY;
                        ByLayer(br);
                        br.ScaleFactors = new Scale3d(PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, value);
                        }
                    });
                }
                else
                {
                    DrawBlockReference(RECONSTRUCTIONAL, basePt,
                   br =>
                   {
                       br.Layer = THESAURUSSANCTITY;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, value);
                       }
                   });
                }
            }
        }
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = VERGELTUNGSWAFFE;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = LYMPHANGIOMATOUS;
                    SetTextStyleLazy(t, THESAURUSTRAFFIC);
                }
            }
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSALCOVE, HYDROSTATICALLY), new Vector2d(-THESAURUSIGNORE, NARCOTRAFICANTE) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[ADRENOCORTICOTROPHIC].EndPoint : segs[ADRENOCORTICOTROPHIC].StartPoint;
            var text = THESAURUSINGENUOUS;
            var height = THESAURUSDETEST;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DrawBlockReference(blkName: ENTREPRENEURISM, basePt: basePt,
        cb: br =>
        {
            if (leftOrRight)
            {
                br.ScaleFactors = new Scale3d(-ADRENOCORTICOTROPHIC, ADRENOCORTICOTROPHIC, ADRENOCORTICOTROPHIC);
            }
            ByLayer(br);
            br.Layer = THESAURUSUNDERSTATE;
        });
        }
        public static void DrawRainDiagram(List<RainDrawingData> drDatas, List<StoreyInfo> storeysItems, Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys, OtherInfo otherInfo, RainSystemDiagramViewModel vm, ExtraInfo exInfo)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + QUOTATIONHOUSEMAID).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - ADRENOCORTICOTROPHIC;
            var end = NARCOTRAFICANTE;
            var OFFSET_X = THESAURUSWOMANLY;
            var SPAN_X = THESAURUSCONTINUATION;
            var HEIGHT = vm?.Params?.StoreySpan ?? THESAURUSINFERENCE;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSINFERENCE;
            DrawRainDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel: vm, otherInfo, exInfo);
        }
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, THESAURUSDETEST, pt);
            SetLabelStylesForRainNote(t);
        }
        public static bool Testing;
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DrawBlockReference(blkName: THESAURUSCELEBRATE, basePt: basePt.OffsetXY(-THESAURUSFLUENT, NARCOTRAFICANTE), cb: br =>
            {
                SetLayerAndByLayer(THESAURUSPRELIMINARY, br);
                if (br.IsDynamicBlock)
                {
                    br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, THESAURUSNOVICE);
                }
            });
        }
        public static IEnumerable<Point2d> GetBasePoints(Point2d basePoint, int maxCol, int num, double width, double height)
        {
            int i = NARCOTRAFICANTE, j = NARCOTRAFICANTE;
            for (int k = NARCOTRAFICANTE; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = NARCOTRAFICANTE;
                }
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = NARCOTRAFICANTE, j = NARCOTRAFICANTE;
            for (int k = NARCOTRAFICANTE; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, NARCOTRAFICANTE);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = NARCOTRAFICANTE;
                }
            }
        }
        public static void DrawRainWaterWell(Point2d basePt, string value)
        {
            DrawRainWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawWaterSealingWell(Point2d basePt)
        {
            DrawBlockReference(blkName: INCOMPATIBILITY, basePt: basePt.ToPoint3d(),
         cb: br =>
         {
             br.Layer = THESAURUSSANCTITY;
             ByLayer(br);
         });
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            value ??= THESAURUSREDOUND;
            DrawBlockReference(blkName: THESAURUSBALLAST, basePt: basePt.OffsetY(-THESAURUSALCOVE),
            props: new Dictionary<string, string>() { { THESAURUSHEARTLESS, value } },
            cb: br =>
            {
                br.Layer = THESAURUSSANCTITY;
                ByLayer(br);
            });
        }
        public static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = THESAURUSSCAVENGER)
        {
            DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, label } }, cb: br => { ByLayer(br); });
        }
        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = THESAURUSOVERWHELM;
            ByLayer(line);
        }
        public static void SetRainPipeLineStyle(Line line)
        {
            line.Layer = THESAURUSCOMMOTION;
            ByLayer(line);
        }
        public static void DrawLine(string layer, params GLineSegment[] segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            foreach (var line in lines)
            {
                line.Layer = layer;
                ByLayer(line);
            }
        }
        public static void DrawRainPipes(params GLineSegment[] segs)
        {
            DrawRainPipes((IEnumerable<GLineSegment>)segs);
        }
        public static void DrawRainPipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line => SetRainPipeLineStyle(line));
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
        static readonly Regex re = new Regex(THESAURUSSOOTHE);
        public static RainLabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new RainLabelItem()
            {
                Label = label,
                Prefix = m.Groups[ADRENOCORTICOTROPHIC].Value,
                D1S = m.Groups[PHOTOGONIOMETER].Value,
                D2S = m.Groups[THESAURUSPOSTSCRIPT].Value,
                Suffix = m.Groups[THESAURUSTITTER].Value,
            };
        }
        public static IEnumerable<string> ConvertLabelStrings(List<string> pipeIds)
        {
            {
                var labels = pipeIds.Where(x => Regex.IsMatch(x, THESAURUSRESTFUL)).ToList();
                pipeIds = pipeIds.Except(labels).ToList();
                foreach (var s in ConvertLabelString(labels))
                {
                    yield return s;
                }
                static IEnumerable<string> ConvertLabelString(IEnumerable<string> strs)
                {
                    var kvs = new List<KeyValuePair<string, string>>();
                    foreach (var str in strs)
                    {
                        var m = Regex.Match(str, THESAURUSRESTFUL);
                        if (m.Success)
                        {
                            kvs.Add(new KeyValuePair<string, string>(m.Groups[ADRENOCORTICOTROPHIC].Value, m.Groups[PHOTOGONIOMETER].Value));
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                    return kvs.GroupBy(x => x.Key).OrderBy(x => x.Key).Select(x => x.Key + THESAURUSHEARTLESS + string.Join(THESAURUSAUTOGRAPH, GetLabelString(x.Select(y => y.Value[NARCOTRAFICANTE]))));
                }
                static IEnumerable<string> GetLabelString(IEnumerable<char> chars)
                {
                    foreach (var kv in GetPairs(chars.Select(x => (int)x).OrderBy(x => x)))
                    {
                        if (kv.Key == kv.Value)
                        {
                            yield return Convert.ToChar(kv.Key).ToString();
                        }
                        else
                        {
                            yield return Convert.ToChar(kv.Key) + THESAURUSRELIGIOUS + Convert.ToChar(kv.Value);
                        }
                    }
                }
            }
            {
                var labels = pipeIds.Where(x => Regex.IsMatch(x, THESAURUSPROPHETIC)).ToList();
                pipeIds = pipeIds.Except(labels).ToList();
                foreach (var s in ConvertLabelString(labels))
                {
                    yield return s;
                }
                static IEnumerable<string> ConvertLabelString(IEnumerable<string> strs)
                {
                    var kvs = new List<ValueTuple<string, string, int>>();
                    foreach (var str in strs)
                    {
                        var m = Regex.Match(str, QUOTATIONDIAMONDBACK);
                        if (m.Success)
                        {
                            kvs.Add(new ValueTuple<string, string, int>(m.Groups[ADRENOCORTICOTROPHIC].Value, m.Groups[PHOTOGONIOMETER].Value, int.Parse(m.Groups[THESAURUSPOSTSCRIPT].Value)));
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                    return kvs.GroupBy(x => x.Item1).OrderBy(x => x.Key).Select(x => x.Key + string.Join(THESAURUSAUTOGRAPH, GetLabelString(x.First().Item2, x.Select(y => y.Item3))));
                }
                static IEnumerable<string> GetLabelString(string prefix, IEnumerable<int> nums)
                {
                    foreach (var kv in GetPairs(nums.OrderBy(x => x)))
                    {
                        if (kv.Key == kv.Value)
                        {
                            yield return prefix + kv.Key;
                        }
                        else
                        {
                            yield return prefix + kv.Key + THESAURUSRELIGIOUS + prefix + kv.Value;
                        }
                    }
                }
            }
            var items = pipeIds.Select(id => RainLabelItem.Parse(id)).Where(m => m != null).ToList();
            var rest = pipeIds.Except(items.Select(x => x.Label)).ToList();
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2S?.Length ?? NARCOTRAFICANTE).ThenBy(x => x.D2).ThenBy(x => x.D2S).ToList());
            foreach (var g in gs)
            {
                if (g.Count == ADRENOCORTICOTROPHIC)
                {
                    yield return g.First().Label;
                }
                else if (g.Count > PHOTOGONIOMETER && g.Count == g.Last().D2 - g.First().D2 + ADRENOCORTICOTROPHIC)
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
                    for (int i = NARCOTRAFICANTE; i < g.Count; i++)
                    {
                        var m = g[i];
                        sb.Append($"{m.D2S}{m.Suffix}");
                        if (i != g.Count - ADRENOCORTICOTROPHIC)
                        {
                            sb.Append(THESAURUSAUTOGRAPH);
                        }
                    }
                    yield return sb.ToString();
                }
            }
            foreach (var r in rest)
            {
                yield return r;
            }
        }
        public static IEnumerable<KeyValuePair<int, int>> GetPairs(IEnumerable<int> ints)
        {
            int st = int.MinValue;
            int ed = int.MinValue;
            foreach (var i in ints)
            {
                if (st == int.MinValue)
                {
                    st = i;
                    ed = i;
                }
                else if (ed + ADRENOCORTICOTROPHIC == i)
                {
                    ed = i;
                }
                else
                {
                    yield return new KeyValuePair<int, int>(st, ed);
                    st = i;
                    ed = i;
                }
            }
            if (st != int.MinValue)
            {
                yield return new KeyValuePair<int, int>(st, ed);
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
        public int LinesCount = ADRENOCORTICOTROPHIC;
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
        public int HangingCount = NARCOTRAFICANTE;
        public Hanging Hanging1;
        public Hanging Hanging2;
    }
    public class ThwPipeRun
    {
        public string Storey;
        public bool ShowStoreyLabel;
        public bool HasShortTranslator;
        public bool HasLongTranslator;
        public bool IsShortTranslatorToLeftOrRight;
        public bool IsLongTranslatorToLeftOrRight;
        public bool ShowShortTranslatorLabel;
        public bool IsFirstItem;
        public bool IsLastItem;
        public Hanging LeftHanging;
        public Hanging RightHanging;
    }
    public class ThwPipeLine
    {
        public List<string> Labels;
        public bool? IsLeftOrMiddleOrRight;
        public double AiringValue;
        public List<ThwPipeRun> PipeRuns;
        public ThwOutput Output;
    }
    public class OtherInfo
    {
        public List<AloneFloorDrainInfo> AloneFloorDrainInfos;
    }
    public class RainDrawingData
    {
        public GRect Boundary;
        public Point2d ContraPoint;
        public List<GRect> Y1LVerticalPipeRects;
        public List<string> Y1LVerticalPipeRectLabels;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> SideWaterBuckets;
        public List<GRect> _87WaterBuckets;
        public List<string> GravityWaterBucketLabels;
        public List<string> SideWaterBucketLabels;
        public List<string> _87WaterBucketLabels;
        public HashSet<string> HasWaterSealingWell;
        public HashSet<string> HasSideFloorDrain;
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public Dictionary<string, int> FloorDrainWrappingPipes;
        public HashSet<string> CleaningPorts;
        public HashSet<string> Spreadings;
        public Dictionary<string, string> WaterWellLabels;
        public Dictionary<string, int> WaterWellIds;
        public Dictionary<string, int> RainPortIds;
        public Dictionary<string, int> WaterSealingWellIds;
        public Dictionary<string, int> DitchIds;
        public Dictionary<int, string> OutletWrappingPipeDict;
        public HashSet<string> HasCondensePipe;
        public HashSet<string> HasBrokenCondensePipes;
        public HashSet<string> HasNonBrokenCondensePipes;
        public HashSet<string> PlsDrawCondensePipeHigher;
        public HashSet<string> HasRainPortSymbols;
        public HashSet<string> HasDitch;
        public HashSet<string> ConnectedToGravityWaterBucket;
        public HashSet<string> ConnectedToSideWaterBucket;
        public List<string> Comments;
        public List<KeyValuePair<string, string>> RoofWaterBuckets;
        public HashSet<int> HasSingleFloorDrainDrainageForWaterWell;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public HashSet<int> HasSingleFloorDrainDrainageForRainPort;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForRainPort;
        public HashSet<int> HasSingleFloorDrainDrainageForWaterSealingWell;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell;
        public HashSet<int> HasSingleFloorDrainDrainageForDitch;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForDitch;
        public Dictionary<int, string> OutletWrappingPipeRadiusStringDict;
        public List<AloneFloorDrainInfo> AloneFloorDrainInfos;
        public void Init()
        {
            Y1LVerticalPipeRects ??= new List<GRect>();
            Y1LVerticalPipeRectLabels ??= new List<string>();
            GravityWaterBuckets ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
            GravityWaterBucketLabels ??= new List<string>();
            SideWaterBucketLabels ??= new List<string>();
            _87WaterBucketLabels ??= new List<string>();
            VerticalPipeLabels ??= new HashSet<string>();
            HasSideFloorDrain ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            FloorDrainWrappingPipes ??= new Dictionary<string, int>();
            CleaningPorts ??= new HashSet<string>();
            WaterWellLabels ??= new Dictionary<string, string>();
            WaterWellIds ??= new Dictionary<string, int>();
            RainPortIds ??= new Dictionary<string, int>();
            WaterSealingWellIds ??= new Dictionary<string, int>();
            HasDitch ??= new HashSet<string>();
            DitchIds ??= new Dictionary<string, int>();
            OutletWrappingPipeDict ??= new Dictionary<int, string>();
            Comments ??= new List<string>();
            HasCondensePipe ??= new HashSet<string>();
            Spreadings ??= new HashSet<string>();
            HasBrokenCondensePipes ??= new HashSet<string>();
            HasNonBrokenCondensePipes ??= new HashSet<string>();
            PlsDrawCondensePipeHigher ??= new HashSet<string>();
            HasRainPortSymbols ??= new HashSet<string>();
            ConnectedToGravityWaterBucket ??= new HashSet<string>();
            ConnectedToSideWaterBucket ??= new HashSet<string>();
            RoofWaterBuckets ??= new List<KeyValuePair<string, string>>();
            OutletWrappingPipeRadiusStringDict ??= new Dictionary<int, string>();
            HasSingleFloorDrainDrainageForWaterWell ??= new HashSet<int>();
            FloorDrainShareDrainageWithVerticalPipeForWaterWell ??= new HashSet<int>();
            HasSingleFloorDrainDrainageForRainPort ??= new HashSet<int>();
            FloorDrainShareDrainageWithVerticalPipeForRainPort ??= new HashSet<int>();
            HasSingleFloorDrainDrainageForWaterSealingWell ??= new HashSet<int>();
            FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell ??= new HashSet<int>();
            HasSingleFloorDrainDrainageForDitch ??= new HashSet<int>();
            FloorDrainShareDrainageWithVerticalPipeForDitch ??= new HashSet<int>();
            HasWaterSealingWell ??= new HashSet<string>();
            AloneFloorDrainInfos ??= new List<AloneFloorDrainInfo>();
        }
    }
    public partial class RainService
    {
        public AcadDatabase adb;
        public RainDiagram RainDiagram;
        public List<StoreyInfo> Storeys;
        public RainGeoData GeoData;
        public RainCadData CadDataMain;
        public List<RainCadData> CadDatas;
        public List<RainDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static RainGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return null;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = new RainGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }
        public static void CollectGeoData(AcadDatabase adb, RainGeoData geoData, CommandContext ctx)
        {
            var cl = new ThRainSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(ctx);
            cl.CollectEntities();
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, RainGeoData geoData)
        {
            var cl = new ThRainSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(range);
            cl.CollectEntities();
        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == NARCOTRAFICANTE) return UNTRACEABLENESS;
            }
            return THESAURUSSEMBLANCE;
        }
        public static ThRainSystemService.CommandContext commandContext => ThRainSystemService.commandContext;
        static List<GLineSegment> ExplodeGLineSegments(Geometry geo)
        {
            static IEnumerable<GLineSegment> enumerate(Geometry geo)
            {
                if (geo is LineString ls)
                {
                    if (ls.NumPoints == PHOTOGONIOMETER) yield return new GLineSegment(ls[NARCOTRAFICANTE].ToPoint2d(), ls[ADRENOCORTICOTROPHIC].ToPoint2d());
                    else if (ls.NumPoints > PHOTOGONIOMETER)
                    {
                        for (int i = NARCOTRAFICANTE; i < ls.NumPoints - ADRENOCORTICOTROPHIC; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + ADRENOCORTICOTROPHIC].ToPoint2d());
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
            foreach (var s in geoData.Storeys) DrawRectLazy(s).ColorIndex = ADRENOCORTICOTROPHIC;
            foreach (var o in geoData.LabelLines) DrawLineSegmentLazy(o).ColorIndex = ADRENOCORTICOTROPHIC;
            foreach (var o in geoData.Labels)
            {
                DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = PHOTOGONIOMETER;
                DrawRectLazy(o.Boundary).ColorIndex = PHOTOGONIOMETER;
            }
            foreach (var o in geoData.VerticalPipes) DrawRectLazy(o).ColorIndex = THESAURUSPOSTSCRIPT;
            foreach (var o in geoData.FloorDrains)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSTITTER;
                Dr.DrawSimpleLabel(o.LeftTop, QUOTATION1BMIDDLE);
            }
            foreach (var o in geoData.GravityWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSTITTER;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSALLUSION);
            }
            foreach (var o in geoData.SideWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSTITTER;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSCOMMANDER);
            }
            foreach (var o in geoData._87WaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSTITTER;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSUMBRAGE);
            }
            foreach (var o in geoData.WaterPorts) DrawRectLazy(o).ColorIndex = ARCHAEOLOGICALLY;
            foreach (var o in geoData.WaterWells) DrawRectLazy(o).ColorIndex = ARCHAEOLOGICALLY;
            {
                var cl = Color.FromRgb(THESAURUSTITTER, COUNTERFEISANCE, INTERVOCALICALLY);
                foreach (var o in geoData.DLines) DrawLineSegmentLazy(o).Color = cl;
            }
            foreach (var o in geoData.WLines) DrawLineSegmentLazy(o).ColorIndex = QUOTATIONLENTIFORM;
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = THESAURUSALCOVE;
        public static ExtraInfo CreateDrawingDatas(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas, out string logString, out List<RainDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData)
        {
            _DrawingTransaction.Current.AbleToDraw = UNTRACEABLENESS;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = ADRENOCORTICOTROPHIC;
            }
            var sb = new StringBuilder(THESAURUSVOLITION);
            drDatas = new List<RainDrawingData>();
            var extraInfo = new ExtraInfo() { Items = new List<ExtraInfo.Item>(), CadDatas = cadDatas, drDatas = drDatas, geoData = geoData, };
            for (int storeyI = NARCOTRAFICANTE; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new RainDrawingData();
                drData.Init();
                drData.Boundary = geoData.Storeys[storeyI];
                drData.ContraPoint = geoData.StoreyContraPoints[storeyI];
                var item = cadDatas[storeyI];
                var exItem = new ExtraInfo.Item();
                extraInfo.Items.Add(exItem);
                exItem.Index = storeyI;
                {
                    var maxDis = THESAURUSOVERFLOW;
                    var angleTolleranceDegree = ADRENOCORTICOTROPHIC;
                    var waterPortCvt = RainCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.WLines.Where(x => x.Length > NARCOTRAFICANTE).Distinct().ToList().Select(cadDataMain.WLines).ToList(geoData.WLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.CondensePipes).Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
                        maxDis, angleTolleranceDegree).ToList();
                    geoData.WLines.AddRange(lines);
                    var wlineCvt = RainCadData.ConvertWLinesF();
                    var _lines = lines.Select(wlineCvt).ToList();
                    cadDataMain.WLines.AddRange(_lines);
                    item.WLines.AddRange(_lines);
                }
                var lbDict = new Dictionary<Geometry, string>();
                var notedPipesDict = new Dictionary<Geometry, string>();
                var labelLinesGroup = GG(item.LabelLines);
                var labelLinesGeos = GeosGroupToGeos(labelLinesGroup);
                var labellinesGeosf = F(labelLinesGeos);
                var shortTranslatorLabels = new HashSet<string>();
                var longTranslatorLabels = new HashSet<string>();
                var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, THESAURUSNETHER).ToList();
                var wrappingPipesf = F(item.WrappingPipes);
                var sfdsf = F(item.SideFloorDrains);
                {
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (text.Contains(THESAURUSCAPRICIOUS) && text.Contains(POLYSOMNOGRAPHY))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == ADRENOCORTICOTROPHIC)
                            {
                                var labelLineGeo = lst[NARCOTRAFICANTE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, THESAURUSFACTOR);
                                var _pts = pts.Select(x => x.ToNTSPoint()).ToList();
                                var ptsf = GeoFac.CreateIntersectsSelector(_pts);
                                {
                                    var __pts = _pts.Except(ptsf(label)).Where(pt => item.RainPortSymbols.All(x => !x.Intersects(pt.Buffer(UNDERACHIEVEMENT)))).ToList();
                                    if (__pts.Count > NARCOTRAFICANTE)
                                    {
                                        foreach (var r in __pts.Select(pt => GRect.Create(pt.ToPoint2d(), THESAURUSDERISION)))
                                        {
                                            geoData.RainPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.RainPortSymbols.Add(pl);
                                            item.RainPortSymbols.Add(pl);
                                            DrawTextLazy(COMPOSITIONALLY, pl.GetCenter());
                                        }
                                    }
                                }
                            }
                        }
                        else
                        if (text.Contains(THESAURUSCAPRICIOUS) && text.Contains(NANOPHANEROPHYTE))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == ADRENOCORTICOTROPHIC)
                            {
                                var labelLineGeo = lst[NARCOTRAFICANTE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, THESAURUSFACTOR);
                                var _pts = pts.Select(x => x.ToNTSPoint()).ToList();
                                var ptsf = GeoFac.CreateIntersectsSelector(_pts);
                                _pts = _pts.Except(ptsf(label)).Where(pt => item.Ditches.All(x => !x.Intersects(pt.Buffer(UNDERACHIEVEMENT)))).ToList();
                                if (_pts.Count > NARCOTRAFICANTE)
                                {
                                    foreach (var r in _pts.Select(pt => GRect.Create(pt.ToPoint2d(), UNDERACHIEVEMENT)))
                                    {
                                        geoData.Ditches.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.Ditches.Add(pl);
                                        item.Ditches.Add(pl);
                                        DrawTextLazy(COMPOSITIONALLY, pl.GetCenter());
                                    }
                                }
                            }
                        }
                    }
                }
                {
                    var portsf = F(item.RainPortSymbols);
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (text.Contains(THESAURUSCAPRICIOUS) && text.Contains(POLYSOMNOGRAPHY))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == ADRENOCORTICOTROPHIC)
                            {
                                var labelLineGeo = lst[NARCOTRAFICANTE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, THESAURUSFACTOR);
                                var sel = new MultiPoint(pts.Select(x => x.ToNTSPoint()).ToArray());
                                var guid = Guid.NewGuid().ToString(MANIFESTATIONAL);
                                foreach (var port in portsf(sel))
                                {
                                    var id = cadDataMain.RainPortSymbols.IndexOf(port);
                                    port.UserData ??= new Tuple<int, string>(id, guid);
                                }
                            }
                        }
                    }
                }
                var fixPortDict = new Dictionary<int, int>();
                var logicalRainPorts = new List<Geometry>();
                {
                    foreach (var port in item.RainPortSymbols)
                    {
                        if (port.UserData is null)
                        {
                            var id = cadDataMain.RainPortSymbols.IndexOf(port);
                            port.UserData = new Tuple<int, string>(id, Guid.NewGuid().ToString(MANIFESTATIONAL));
                        }
                    }
                    foreach (var g in item.RainPortSymbols.Select(x => x.UserData).Cast<Tuple<int, string>>().GroupBy(x => x.Item2))
                    {
                        var id = g.First().Item1;
                        foreach (var m in g)
                        {
                            fixPortDict[m.Item1] = id;
                        }
                        var port = new MultiLineString(g.Select(x => geoData.RainPortSymbols[x.Item1].ToPolygon().Shell).ToArray());
                        port.UserData = id;
                        logicalRainPorts.Add(port);
                    }
                }
                foreach (var port in logicalRainPorts)
                {
                    DrawRectLazy(port.EnvelopeInternal.ToGRect());
                }
                {
                    var labelsf = F(item.Labels);
                    {
                        var pipesf = F(item.VerticalPipes);
                        foreach (var label in item.Labels)
                        {
                            if (!IsRainLabel(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                            var lst = labellinesGeosf(label);
                            if (lst.Count == ADRENOCORTICOTROPHIC)
                            {
                                var labelline = lst[NARCOTRAFICANTE];
                                if (pipesf(GeoFac.CreateGeometry(label, labelline)).Count == NARCOTRAFICANTE)
                                {
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSFACTOR)).ToList(), label, radius: THESAURUSFACTOR);
                                    if (points.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        var pt = points[NARCOTRAFICANTE];
                                        if (!labelsf(pt.ToNTSPoint()).Any())
                                        {
                                            var r = GRect.Create(pt, UNANSWERABLENESS);
                                            geoData.VerticalPipes.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.VerticalPipes.Add(pl);
                                            item.VerticalPipes.Add(pl);
                                            DrawTextLazy(COMPOSITIONALLY, pl.GetCenter());
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        var pipesf = F(item.VerticalPipes);
                        foreach (var label in item.Labels)
                        {
                            var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                            if (!IsRainLabel(text)) continue;
                            var lst = labellinesGeosf(label);
                            if (lst.Count == ADRENOCORTICOTROPHIC)
                            {
                                var labellinesGeo = lst[NARCOTRAFICANTE];
                                if (labelsf(labellinesGeo).Count != ADRENOCORTICOTROPHIC) continue;
                                var lines = ExplodeGLineSegments(labellinesGeo).Where(x => x.IsValid).Distinct().ToList();
                                var geos = lines.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                                var f = F(geos);
                                var tmp = f(label).ToList();
                                if (tmp.Count == ADRENOCORTICOTROPHIC)
                                {
                                    var l1 = tmp[NARCOTRAFICANTE];
                                    tmp = f(l1).Where(x => x != l1).ToList();
                                    if (tmp.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        var l2 = tmp[NARCOTRAFICANTE];
                                        if (lines[geos.IndexOf(l2)].IsHorizontal(THESAURUSFACTOR))
                                        {
                                            tmp = f(l2).Where(x => x != l1 && x != l2).ToList();
                                            if (tmp.Count == ADRENOCORTICOTROPHIC)
                                            {
                                                var l3 = tmp[NARCOTRAFICANTE];
                                                var seg = lines[geos.IndexOf(l3)];
                                                var pts = new List<Point>() { seg.StartPoint.ToNTSPoint(), seg.EndPoint.ToNTSPoint() };
                                                var _tmp = pts.Except(GeoFac.CreateIntersectsSelector(pts)(l2.Buffer(THESAURUSCONSUL, EndCapStyle.Square))).ToList();
                                                if (_tmp.Count == ADRENOCORTICOTROPHIC)
                                                {
                                                    var ptGeo = _tmp[NARCOTRAFICANTE];
                                                    var pipes = pipesf(ptGeo);
                                                    if (pipes.Count == ADRENOCORTICOTROPHIC)
                                                    {
                                                        var pipe = pipes[NARCOTRAFICANTE];
                                                        if (!lbDict.ContainsKey(pipe))
                                                        {
                                                            lbDict[pipe] = text;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        {
                            var f = F(item.GravityWaterBuckets);
                            foreach (var label in item.Labels)
                            {
                                var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                                if (IsGravityWaterBucketLabel(text))
                                {
                                    var lst = labellinesGeosf(label);
                                    if (lst.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        var labelline = lst[NARCOTRAFICANTE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == NARCOTRAFICANTE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSFACTOR)).ToList(), label, radius: THESAURUSFACTOR);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, THESAURUSINDUSTRY);
                                                geoData.GravityWaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain.GravityWaterBuckets.Add(pl);
                                                item.GravityWaterBuckets.Add(pl);
                                                DrawTextLazy(COMPOSITIONALLY, pl.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var f = F(item.SideWaterBuckets);
                            foreach (var label in item.Labels)
                            {
                                var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                                if (IsSideWaterBucketLabel(text))
                                {
                                    var lst = labellinesGeosf(label);
                                    if (lst.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        var labelline = lst[NARCOTRAFICANTE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == NARCOTRAFICANTE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSFACTOR)).ToList(), label, radius: THESAURUSFACTOR);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, THESAURUSINDUSTRY);
                                                geoData.SideWaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain.SideWaterBuckets.Add(pl);
                                                item.SideWaterBuckets.Add(pl);
                                                DrawTextLazy(COMPOSITIONALLY, pl.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var f = F(item._87WaterBuckets);
                            foreach (var label in item.Labels)
                            {
                                var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                                if (Is87WaterBucketLabel(text))
                                {
                                    var lst = labellinesGeosf(label);
                                    if (lst.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        var labelline = lst[NARCOTRAFICANTE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == NARCOTRAFICANTE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSFACTOR)).ToList(), label, radius: THESAURUSFACTOR);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, THESAURUSINDUSTRY);
                                                geoData._87WaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain._87WaterBuckets.Add(pl);
                                                item._87WaterBuckets.Add(pl);
                                                DrawTextLazy(COMPOSITIONALLY, pl.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                DrawGeoData(geoData, cadDataMain, storeyI, item);
                {
                    {
                        var ok_ents = new HashSet<Geometry>();
                        for (int i = NARCOTRAFICANTE; i < THESAURUSPOSTSCRIPT; i++)
                        {
                            var ok = UNTRACEABLENESS;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == ADRENOCORTICOTROPHIC && pipes.Count == ADRENOCORTICOTROPHIC)
                                {
                                    var lb = labels[NARCOTRAFICANTE];
                                    var pp = pipes[NARCOTRAFICANTE];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSREDOUND;
                                    if (IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = THESAURUSSEMBLANCE;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        for (int i = NARCOTRAFICANTE; i < THESAURUSPOSTSCRIPT; i++)
                        {
                            var ok = UNTRACEABLENESS;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > NARCOTRAFICANTE)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSREDOUND).ToList();
                                    if (labelsTxts.All(txt => IsMaybeLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = NARCOTRAFICANTE; k < pipes.Count; k++)
                                        {
                                            var pp = pipes[k];
                                            var lb = labels[k];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp] = label;
                                        }
                                        ok_ents.AddRange(pipes);
                                        ok_ents.AddRange(labels);
                                        ok = THESAURUSSEMBLANCE;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        {
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? THESAURUSREDOUND;
                                if (!IsMaybeLabelText(lb)) continue;
                                var lst = labellinesGeosf(label);
                                if (lst.Count == ADRENOCORTICOTROPHIC)
                                {
                                    var labelline = lst[NARCOTRAFICANTE];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == ADRENOCORTICOTROPHIC)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[NARCOTRAFICANTE].ToNTSPoint());
                                        if (pipes.Count == ADRENOCORTICOTROPHIC)
                                        {
                                            var pp = pipes[NARCOTRAFICANTE];
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
                        bool recognise1()
                        {
                            var ok = UNTRACEABLENESS;
                            for (int i = NARCOTRAFICANTE; i < THESAURUSPOSTSCRIPT; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in wlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == ADRENOCORTICOTROPHIC && lst2.Count > NARCOTRAFICANTE)
                                    {
                                        var pp1 = lst1[NARCOTRAFICANTE];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (THESAURUSCONSUL < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSSEMBLANCE;
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSSEMBLANCE;
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
                            var ok = UNTRACEABLENESS;
                            for (int i = NARCOTRAFICANTE; i < THESAURUSPOSTSCRIPT; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(THESAURUSFACTOR).ToGCircle(UNTRACEABLENESS).ToCirclePolygon(QUOTATIONLENTIFORM));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > ADRENOCORTICOTROPHIC)
                                        {
                                            fs.Add(() =>
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSSEMBLANCE;
                                            });
                                        }
                                    }
                                    if (fs.Count == ADRENOCORTICOTROPHIC) fs[NARCOTRAFICANTE]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = NARCOTRAFICANTE; i < THESAURUSPOSTSCRIPT; i++)
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
                        foreach (var label in d.Where(x => x.Value > ADRENOCORTICOTROPHIC).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == PHOTOGONIOMETER)
                            {
                                var dis = pps[NARCOTRAFICANTE].GetCenter().GetDistanceTo(pps[ADRENOCORTICOTROPHIC].GetCenter());
                                if (THESAURUSCONSUL < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
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
                {
                    var ok_fds = new HashSet<Geometry>();
                    var floorDrainD = new Dictionary<string, int>();
                    var floorDrainWrappingPipeD = new Dictionary<string, int>();
                    {
                        var wpsf = F(item.WrappingPipes);
                        var wlinesGeosf = F(wlinesGeos);
                        {
                            var fdsf = F(item.FloorDrains);
                            foreach (var pipe in item.VerticalPipes)
                            {
                                var label = getLabel(pipe);
                                if (label != null)
                                {
                                    var wlines = wlinesGeosf(pipe);
                                    if (wlines.Any())
                                    {
                                        var wlinesGeo = GeoFac.CreateGeometry(wlines);
                                        var fds = fdsf(wlinesGeo);
                                        ok_fds.AddRange(fds);
                                        floorDrainD[label] = fds.Count;
                                        if (fds.Count > NARCOTRAFICANTE)
                                        {
                                            var wps = wpsf(wlinesGeo);
                                            floorDrainWrappingPipeD[label] = wps.Count;
                                            foreach (var fd in fds)
                                            {
                                                if (sfdsf(fd).Any())
                                                {
                                                    drData.HasSideFloorDrain.Add(label);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                    }
                    drData.FloorDrains = floorDrainD;
                    drData.FloorDrainWrappingPipes = floorDrainWrappingPipeD;
                }
                {
                    var ok_cps = new HashSet<Geometry>();
                    var ok_airing_machines = new HashSet<Geometry>();
                    var todoD = new Dictionary<Geometry, string>();
                    var pipesf = F(getOkPipes(label => IsY2L(label) || IsNL(label)).ToList());
                    var _cpsf = F(item.CondensePipes);
                    var _wlinesGeos = wlinesGeos.Where(x => _cpsf(x).Count > NARCOTRAFICANTE).ToList();
                    var _wlinesGeosf = F(_wlinesGeos);
                    var gs = GeoFac.GroupGeometries(item.CondensePipes.Concat(_wlinesGeos).ToList());
                    List<Geometry> GetNearAringMachines(Geometry cp)
                    {
                        return F(item.AiringMachine_Vertical.Concat(item.AiringMachine_Hanging).Except(ok_airing_machines).ToList())(cp.Envelope.Buffer(THESAURUSDIRECTIVE));
                    }
                    foreach (var g in gs)
                    {
                        var hs = new HashSet<Geometry>(item.CondensePipes.Except(ok_cps));
                        if (hs.Count == NARCOTRAFICANTE) continue;
                        var cps = g.Where(pl => hs.Contains(pl)).ToList();
                        var wlines = g.Where(pl => _wlinesGeos.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        var pipes = pipesf(G(cps.Cast<Polygon>().Select(x => x.Shell).Concat(wlines)));
                        if (pipes.Count == ADRENOCORTICOTROPHIC)
                        {
                            var pipe = pipes[NARCOTRAFICANTE];
                            var label = getLabel(pipe);
                            if (label != null)
                            {
                                if (cps.Count == PHOTOGONIOMETER)
                                {
                                    drData.HasNonBrokenCondensePipes.Add(label);
                                    var airingMachine = GeoFac.NearestNeighbourGeometryF(GetNearAringMachines(GeoFac.CreateGeometryEx(cps)))(GeoFac.CreateGeometryEx(cps));
                                    if (airingMachine != null)
                                    {
                                        if (item.AiringMachine_Hanging.Contains(airingMachine))
                                        {
                                            drData.PlsDrawCondensePipeHigher.Add(label);
                                            ok_airing_machines.Add(airingMachine);
                                        }
                                    }
                                }
                                if (cps.Count == ADRENOCORTICOTROPHIC)
                                {
                                    var _cps = F(item.CondensePipes.Except(ok_cps).ToList())(G(_wlinesGeosf(pipe)));
                                    if (_cps.Count == PHOTOGONIOMETER)
                                    {
                                        var a1 = GeoFac.NearestNeighbourGeometryF(GetNearAringMachines(_cps[NARCOTRAFICANTE]))(_cps[NARCOTRAFICANTE]);
                                        if (a1 != null) ok_airing_machines.Add(a1);
                                        var a2 = GeoFac.NearestNeighbourGeometryF(GetNearAringMachines(_cps[ADRENOCORTICOTROPHIC]))(_cps[ADRENOCORTICOTROPHIC]);
                                        if (a2 != null) ok_airing_machines.Add(a2);
                                        if (item.AiringMachine_Hanging.Contains(a1) && item.AiringMachine_Hanging.Contains(a2))
                                        {
                                            drData.PlsDrawCondensePipeHigher.Add(label);
                                            drData.HasNonBrokenCondensePipes.Add(label);
                                        }
                                        else
                                        {
                                            if (item.AiringMachine_Hanging.Contains(a1) || item.AiringMachine_Hanging.Contains(a2))
                                            {
                                                drData.HasBrokenCondensePipes.Add(label);
                                            }
                                            else
                                            {
                                                drData.HasNonBrokenCondensePipes.Add(label);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var cp = cps[NARCOTRAFICANTE];
                                        todoD[cp] = label;
                                        var airingMachine = GeoFac.NearestNeighbourGeometryF(GetNearAringMachines(cp))(cp);
                                        if (airingMachine != null)
                                        {
                                            if (item.AiringMachine_Hanging.Contains(airingMachine))
                                            {
                                                drData.PlsDrawCondensePipeHigher.Add(label);
                                                ok_airing_machines.Add(airingMachine);
                                            }
                                        }
                                    }
                                }
                                if (cps.Count > NARCOTRAFICANTE)
                                {
                                    drData.HasCondensePipe.Add(label);
                                    ok_cps.AddRange(cps);
                                }
                            }
                        }
                    }
                    {
                        var cpsf = F(item.CondensePipes.Except(ok_cps).ToList());
                        foreach (var kv in todoD)
                        {
                            var cp = kv.Key;
                            var label = kv.Value;
                            var pt = cp.GetCenter();
                            var cps = cpsf(new GLineSegment(pt.OffsetX(-THESAURUSSACRIFICE), pt.OffsetX(THESAURUSSACRIFICE)).ToLineString()).Except(ok_cps).ToList();
                            if (cps.Count == ADRENOCORTICOTROPHIC)
                            {
                                ok_cps.AddRange(cps);
                                drData.HasBrokenCondensePipes.Add(label);
                            }
                        }
                    }
                }
                IEnumerable<Geometry> getOkPipes(Predicate<string> predicate = null)
                {
                    foreach (var kv in lbDict)
                    {
                        if (IsRainLabel(kv.Value) && (predicate?.Invoke(kv.Value) ?? THESAURUSSEMBLANCE))
                        {
                            yield return kv.Key;
                        }
                    }
                }
                {
                    var gbksf = F(item.GravityWaterBuckets);
                    var pipesf = F(getOkPipes().ToList());
                    foreach (var wlineGeo in wlinesGeos)
                    {
                        var gbks = gbksf(wlineGeo);
                        if (gbks.Count == ADRENOCORTICOTROPHIC)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == ADRENOCORTICOTROPHIC)
                            {
                                drData.ConnectedToGravityWaterBucket.Add(lbDict[pipes[NARCOTRAFICANTE]]);
                            }
                        }
                    }
                }
                {
                    var sbksf = F(item.SideWaterBuckets);
                    var pipesf = F(getOkPipes().ToList());
                    foreach (var wlineGeo in wlinesGeos)
                    {
                        var sbks = sbksf(wlineGeo);
                        if (sbks.Count == ADRENOCORTICOTROPHIC)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == ADRENOCORTICOTROPHIC)
                            {
                                drData.ConnectedToSideWaterBucket.Add(lbDict[pipes[NARCOTRAFICANTE]]);
                            }
                        }
                    }
                }
                {
                    var ok_vpipes = new HashSet<Geometry>();
                    var waterwellLabelDict = new Dictionary<string, string>();
                    var waterWellIdDict = new Dictionary<string, int>();
                    var rainPortIdDict = new Dictionary<string, int>();
                    var waterSealingWellIdDict = new Dictionary<string, int>();
                    var ditchIdDict = new Dictionary<string, int>();
                    var waterWellsIdDict = new Dictionary<Geometry, int>();
                    var waterWellsLabelDict = new Dictionary<Geometry, string>();
                    var outletWrappingPipe = new Dictionary<int, string>();
                    {
                        var hasWaterSealingWell = new HashSet<string>();
                        var waterSealingWellsf = F(item.WaterSealingWells);
                        var pipesf = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var wpfsf = F(item.WrappingPipes);
                        foreach (var wlinesGeo in wlinesGeos)
                        {
                            var wells = waterSealingWellsf(wlinesGeo);
                            foreach (var well in wells)
                            {
                                var pipes = pipesf(wlinesGeo);
                                foreach (var pipe in pipesf(wlinesGeo))
                                {
                                    var label = getLabel(pipe);
                                    if (label != null)
                                    {
                                        hasWaterSealingWell.Add(label);
                                        waterSealingWellIdDict[label] = cadDataMain.WaterSealingWells.IndexOf(well);
                                        ok_vpipes.Add(pipe);
                                        foreach (var wp in wpfsf(wlinesGeo))
                                        {
                                            outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                        }
                                    }
                                }
                            }
                        }
                        drData.HasWaterSealingWell = hasWaterSealingWell;
                    }
                    {
                        var hasRainPortSymbols = new HashSet<string>();
                        var rainPortsf = F(item.RainPortSymbols);
                        var pipesf = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var wpfsf = F(item.WrappingPipes);
                        foreach (var wlinesGeo in wlinesGeos)
                        {
                            var rainPorts = rainPortsf(wlinesGeo);
                            foreach (var rainPort in rainPorts)
                            {
                                var pipes = pipesf(wlinesGeo);
                                foreach (var pipe in pipesf(wlinesGeo))
                                {
                                    var label = getLabel(pipe);
                                    if (label != null)
                                    {
                                        hasRainPortSymbols.Add(label);
                                        ok_vpipes.Add(pipe);
                                        var id = cadDataMain.RainPortSymbols.IndexOf(rainPort);
                                        id = fixPortDict[id];
                                        rainPortIdDict[label] = id;
                                        foreach (var wp in wpfsf(wlinesGeo))
                                        {
                                            outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                        }
                                    }
                                }
                            }
                        }
                        drData.HasRainPortSymbols = hasRainPortSymbols;
                    }
                    {
                        var hasDitch = new HashSet<string>();
                        var ditchsf = F(item.Ditches);
                        var pipesf = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var wpfsf = F(item.WrappingPipes);
                        foreach (var wlinesGeo in wlinesGeos)
                        {
                            var wells = ditchsf(wlinesGeo);
                            foreach (var well in wells)
                            {
                                var pipes = pipesf(wlinesGeo);
                                foreach (var pipe in pipesf(wlinesGeo))
                                {
                                    var label = getLabel(pipe);
                                    if (label != null)
                                    {
                                        hasDitch.Add(label);
                                        ditchIdDict[label] = cadDataMain.Ditches.IndexOf(well);
                                        ok_vpipes.Add(pipe);
                                        foreach (var wp in wpfsf(wlinesGeo))
                                        {
                                            outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                        }
                                    }
                                }
                            }
                        }
                        drData.HasDitch = hasDitch;
                    }
                    {
                        var hasWaterSealingWell = drData.HasWaterSealingWell;
                        var wellExs = item.WaterSealingWells.Select(x =>
                        {
                            var geo = x.Buffer(THESAURUSENTREAT); geo.UserData = cadDataMain.WaterSealingWells.IndexOf(x);
                            return geo;
                        }).ToList();
                        var waterSealingWellsf = F(wellExs);
                        var pipesf = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var wpfsf = F(item.WrappingPipes);
                        foreach (var wlinesGeo in wlinesGeos)
                        {
                            var wells = waterSealingWellsf(wlinesGeo);
                            foreach (var well in wells)
                            {
                                var pipes = pipesf(wlinesGeo);
                                foreach (var pipe in pipesf(wlinesGeo))
                                {
                                    var label = getLabel(pipe);
                                    if (label != null)
                                    {
                                        hasWaterSealingWell.Add(label);
                                        waterSealingWellIdDict[label] = (int)well.UserData;
                                        ok_vpipes.Add(pipe);
                                        foreach (var wp in wpfsf(wlinesGeo))
                                        {
                                            outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        void collect(Func<Geometry, List<Geometry>> waterWellsf, Func<Geometry, string> getWaterWellLabel, Func<Geometry, int> getWaterWellId)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                            foreach (var wlinesGeo in wlinesGeos)
                            {
                                var waterWells = waterWellsf(wlinesGeo);
                                if (waterWells.Count == ADRENOCORTICOTROPHIC)
                                {
                                    var waterWell = waterWells[NARCOTRAFICANTE];
                                    var waterWellLabel = getWaterWellLabel(waterWell);
                                    waterWellsLabelDict[wlinesGeo] = waterWellLabel;
                                    waterWellsIdDict[wlinesGeo] = getWaterWellId(waterWell);
                                    var pipes = f2(wlinesGeo);
                                    ok_vpipes.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            waterwellLabelDict[label] = waterWellLabel;
                                            waterWellIdDict[label] = getWaterWellId(waterWell);
                                            var wrappingpipes = wrappingPipesf(wlinesGeo);
                                            foreach (var wp in wrappingpipes)
                                            {
                                                outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                            }
                                            foreach (var wp in wrappingpipes)
                                            {
                                                waterWellsLabelDict[wp] = waterWellLabel;
                                                waterWellsIdDict[wp] = getWaterWellId(waterWell);
                                                DrawTextLazy(waterWellLabel, wp.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        collect(F(item.WaterWells), waterWell => geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(waterWell)], well => cadDataMain.WaterWells.IndexOf(well));
                        {
                            var spacialIndex = item.WaterWells.Select(cadDataMain.WaterWells).ToList();
                            var waterWells = spacialIndex.ToList(geoData.WaterWells).Select(x => x.Expand(THESAURUSALCOVE).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterWellLabels[spacialIndex[waterWells.IndexOf(waterWell)]], waterWell => spacialIndex[waterWells.IndexOf(waterWell)]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var radius = THESAURUSCONSUL;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterWells);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(QUOTATIONLENTIFORM, UNTRACEABLENESS)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterWells).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= ELECTROMYOGRAPH)
                                    {
                                        var waterWellLabel = geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(waterWell)];
                                        waterWellsLabelDict[dlinesGeo] = waterWellLabel;
                                        waterWellsIdDict[dlinesGeo] = cadDataMain.WaterWells.IndexOf(waterWell);
                                        foreach (var pipe in f2(dlinesGeo))
                                        {
                                            if (lbDict.TryGetValue(pipe, out string label))
                                            {
                                                waterwellLabelDict[label] = waterWellLabel;
                                                waterWellIdDict[label] = cadDataMain.WaterWells.IndexOf(waterWell);
                                                ok_vpipes.Add(pipe);
                                                var wrappingpipes = wrappingPipesf(dlinesGeo);
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    waterWellsLabelDict[wp] = waterWellLabel;
                                                    waterWellsIdDict[wp] = cadDataMain.WaterWells.IndexOf(waterWell);
                                                    DrawTextLazy(waterWellLabel, wp.GetCenter());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        var pipesf = F(item.VerticalPipes);
                        foreach (var wp in item.WrappingPipes)
                        {
                            if (waterWellsLabelDict.TryGetValue(wp, out string v))
                            {
                                var pipes = pipesf(wp);
                                foreach (var pipe in pipes)
                                {
                                    if (lbDict.TryGetValue(pipe, out string label))
                                    {
                                        if (!waterwellLabelDict.ContainsKey(label))
                                        {
                                            waterwellLabelDict[label] = v;
                                            waterWellIdDict[label] = waterWellsIdDict[wp];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
                        var wpf = F(item.WrappingPipes.Where(wp => !waterWellsLabelDict.ContainsKey(wp)).ToList());
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            if (!waterWellsLabelDict.TryGetValue(dlinesGeo, out string v)) continue;
                            foreach (var wp in wpf(dlinesGeo))
                            {
                                if (!waterWellsLabelDict.ContainsKey(wp))
                                {
                                    waterWellsLabelDict[wp] = v;
                                    waterWellsIdDict[wp] = waterWellsIdDict[dlinesGeo];
                                }
                            }
                        }
                        {
                            var points = geoData.WrappingPipeRadius.Select(x => x.Key).ToList();
                            var pts = points.Select(x => x.ToNTSPoint()).ToList();
                            var ptsf = GeoFac.CreateIntersectsSelector(pts);
                            foreach (var wp in item.WrappingPipes)
                            {
                                var _pts = ptsf(wp.Buffer(UNDERACHIEVEMENT));
                                if (_pts.Count > NARCOTRAFICANTE)
                                {
                                    var kv = geoData.WrappingPipeRadius[pts.IndexOf(_pts[NARCOTRAFICANTE])];
                                    var radiusText = kv.Value;
                                    if (string.IsNullOrWhiteSpace(radiusText)) radiusText = QUOTATIONPERUVIAN;
                                    drData.OutletWrappingPipeRadiusStringDict[cadDataMain.WrappingPipes.IndexOf(wp)] = radiusText;
                                }
                            }
                        }
                    }
                    {
                        drData.WaterWellIds = waterWellIdDict;
                        drData.RainPortIds = rainPortIdDict;
                        drData.WaterSealingWellIds = waterSealingWellIdDict;
                        drData.DitchIds = ditchIdDict;
                        drData.WaterWellLabels = waterwellLabelDict;
                        waterwellLabelDict.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                        {
                            var num = kv1.Value;
                            var pipe = kv2.Key;
                            DrawTextLazy(num, pipe.ToGRect().RightButtom);
                            return THESAURUSBREEDING;
                        }).Count();
                    }
                    {
                        drData.OutletWrappingPipeDict = outletWrappingPipe;
                    }
                }
                {
                    var wlinesf = F(wlinesGeos);
                    foreach (var kv in lbDict)
                    {
                        {
                            var m = Regex.Match(kv.Value, CONTRADICTIVELY);
                            if (m.Success)
                            {
                                var floor = m.Groups[ADRENOCORTICOTROPHIC].Value;
                                var pipe = kv.Key;
                                var pipes = getOkPipes().ToList();
                                var wlines = wlinesf(pipe);
                                if (wlines.Count == ADRENOCORTICOTROPHIC)
                                {
                                    foreach (var pp in F(pipes)(wlines[NARCOTRAFICANTE]))
                                    {
                                        drData.RoofWaterBuckets.Add(new KeyValuePair<string, string>(lbDict[pp], floor));
                                    }
                                }
                                continue;
                            }
                        }
                        if (kv.Value.Contains(QUOTATIONVARANGIAN))
                        {
                            var floor = THESAURUSINSCRIPTION;
                            var pipe = kv.Key;
                            var pipes = getOkPipes().ToList();
                            var wlines = wlinesf(pipe);
                            if (wlines.Count == ADRENOCORTICOTROPHIC)
                            {
                                foreach (var pp in F(pipes)(wlines[NARCOTRAFICANTE]))
                                {
                                    drData.RoofWaterBuckets.Add(new KeyValuePair<string, string>(lbDict[pp], floor));
                                }
                            }
                            continue;
                        }
                    }
                }
                {
                    var pipesf = F(item.VerticalPipes);
                    var rainpipesf = F(lbDict.Where(x => IsRainLabel(x.Value)).Select(x => x.Key).ToList());
                    if (item.WaterWells.Count + item.RainPortSymbols.Count + item.WaterSealingWells.Count + item.WaterPorts.Count > NARCOTRAFICANTE)
                    {
                        var ok_fds = new HashSet<Geometry>();
                        var wlinesf = F(wlinesGeos);
                        var alone_fds = new HashSet<Geometry>();
                        var side = new MultiPoint(geoData.SideFloorDrains.Select(x => x.ToNTSPoint()).ToArray());
                        var fdsf = F(item.FloorDrains.Where(x => !x.Intersects(side)).ToList());
                        var aloneFloorDrainInfos = new List<AloneFloorDrainInfo>();
                        var bufSize = VICISSITUDINOUS;
                        foreach (var ditch in item.Ditches)
                        {
                            foreach (var wline in wlinesf(ditch.EnvelopeInternal.ToGRect().Expand(THESAURUSENTREAT).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > NARCOTRAFICANTE)
                                {
                                    ok_fds.AddRange(fds);
                                    var id = cadDataMain.Ditches.IndexOf(ditch);
                                    drData.HasSingleFloorDrainDrainageForDitch.Add(id);
                                    if (pipesf(wline).Any(pipe => IsRainLabel(getLabel(pipe))))
                                    {
                                        drData.FloorDrainShareDrainageWithVerticalPipeForDitch.Add(id);
                                    }
                                }
                            }
                        }
                        foreach (var port in logicalRainPorts)
                        {
                            foreach (var wline in wlinesf(port))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > NARCOTRAFICANTE)
                                {
                                    ok_fds.AddRange(fds);
                                    var id = (int)port.UserData;
                                    drData.HasSingleFloorDrainDrainageForRainPort.Add(id);
                                    if (pipesf(wline).Any(pipe => IsRainLabel(getLabel(pipe))))
                                    {
                                        drData.FloorDrainShareDrainageWithVerticalPipeForRainPort.Add(id);
                                    }
                                }
                            }
                        }
                        foreach (var well in item.WaterWells)
                        {
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(THESAURUSENTREAT).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                {
                                    var __fds = _fds.Except(fds).ToList();
                                    alone_fds.AddRange(__fds);
                                    foreach (var fd in __fds)
                                    {
                                        var o = new AloneFloorDrainInfo()
                                        {
                                            IsSideFloorDrain = sfdsf(fd).Any(),
                                            WaterWellLabel = geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(well)],
                                        };
                                        aloneFloorDrainInfos.Add(o);
                                    }
                                }
                                if (fds.Count > NARCOTRAFICANTE)
                                {
                                    ok_fds.AddRange(fds);
                                    var id = cadDataMain.WaterWells.IndexOf(well);
                                    drData.HasSingleFloorDrainDrainageForWaterWell.Add(id);
                                    if (pipesf(wline).Any(pipe => IsRainLabel(getLabel(pipe))))
                                    {
                                        drData.FloorDrainShareDrainageWithVerticalPipeForWaterWell.Add(id);
                                    }
                                }
                            }
                        }
                        foreach (var well in item.WaterSealingWells)
                        {
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(THESAURUSENTREAT).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > NARCOTRAFICANTE)
                                {
                                    ok_fds.AddRange(fds);
                                    var id = cadDataMain.WaterSealingWells.IndexOf(well);
                                    drData.HasSingleFloorDrainDrainageForWaterSealingWell.Add(id);
                                    if (pipesf(wline).Any(pipe => IsRainLabel(getLabel(pipe))))
                                    {
                                        drData.FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell.Add(id);
                                    }
                                }
                            }
                        }
                        drData.AloneFloorDrainInfos = aloneFloorDrainInfos;
                    }
                }
                {
                }
                foreach (var pp in item.VerticalPipes)
                {
                    lbDict.TryGetValue(pp, out string label);
                    if (label != null)
                    {
                        DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                    }
                }
                exItem.LabelDict = lbDict.Select(x => new Tuple<Geometry, string>(x.Key, x.Value)).ToList();
                {
                    foreach (var kv in lbDict)
                    {
                        if (IsY1L(kv.Value))
                        {
                            drData.Y1LVerticalPipeRectLabels.Add(kv.Value);
                            drData.Y1LVerticalPipeRects.Add(kv.Key.EnvelopeInternal.ToGRect());
                        }
                    }
                    drData.GravityWaterBuckets.AddRange(item.GravityWaterBuckets.Select(x => geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(x)]).Distinct());
                    drData.SideWaterBuckets.AddRange(item.SideWaterBuckets.Select(x => geoData.SideWaterBuckets[cadDataMain.SideWaterBuckets.IndexOf(x)]).Distinct());
                    drData._87WaterBuckets.AddRange(item._87WaterBuckets.Select(x => geoData._87WaterBuckets[cadDataMain._87WaterBuckets.IndexOf(x)]).Distinct());
                    for (int i = NARCOTRAFICANTE; i < drData.GravityWaterBuckets.Count; i++)
                    {
                        drData.GravityWaterBucketLabels.Add(null);
                    }
                    for (int i = NARCOTRAFICANTE; i < drData.SideWaterBuckets.Count; i++)
                    {
                        drData.SideWaterBucketLabels.Add(null);
                    }
                    for (int i = NARCOTRAFICANTE; i < drData._87WaterBuckets.Count; i++)
                    {
                        drData._87WaterBucketLabels.Add(null);
                    }
                    {
                        var bklbDict = new Dictionary<Geometry, string>();
                        var labelsf = F(item.Labels);
                        var bksf = F(item.GravityWaterBuckets.Concat(item.SideWaterBuckets).Concat(item._87WaterBuckets).ToList());
                        foreach (var labelLinesGeo in labelLinesGeos)
                        {
                            var labels = labelsf(labelLinesGeo);
                            var bks = bksf(labelLinesGeo);
                            if (labels.Count == ADRENOCORTICOTROPHIC && bks.Count == ADRENOCORTICOTROPHIC)
                            {
                                var lb = labels[NARCOTRAFICANTE];
                                var bk = bks[NARCOTRAFICANTE];
                                var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSREDOUND;
                                if (IsWaterBucketLabel(label))
                                {
                                    if (!label.ToUpper().Contains(PSYCHOGEOGRAPHY))
                                    {
                                        var pt = lb.GetCenter().OffsetY(-HYDROSTATICALLY);
                                        var lst = labelsf(GRect.Create(pt, HYDROSTATICALLY, THESAURUSDERISION).ToPolygon());
                                        lst.Remove(lb);
                                        if (lst.Count == ADRENOCORTICOTROPHIC)
                                        {
                                            var _label = geoData.Labels[cadDataMain.Labels.IndexOf(lst[NARCOTRAFICANTE])].Text ?? THESAURUSREDOUND;
                                            if (_label.Contains(PSYCHOGEOGRAPHY))
                                            {
                                                label += _label;
                                            }
                                        }
                                    }
                                    bklbDict[bk] = label;
                                }
                            }
                        }
                        foreach (var kv in bklbDict)
                        {
                            int i;
                            i = cadDataMain.GravityWaterBuckets.IndexOf(kv.Key);
                            if (i >= NARCOTRAFICANTE)
                            {
                                i = drData.GravityWaterBuckets.IndexOf(geoData.GravityWaterBuckets[i]);
                                if (i >= NARCOTRAFICANTE)
                                {
                                    drData.GravityWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain.SideWaterBuckets.IndexOf(kv.Key);
                            if (i >= NARCOTRAFICANTE)
                            {
                                i = drData.SideWaterBuckets.IndexOf(geoData.SideWaterBuckets[i]);
                                if (i >= NARCOTRAFICANTE)
                                {
                                    drData.SideWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain._87WaterBuckets.IndexOf(kv.Key);
                            if (i >= NARCOTRAFICANTE)
                            {
                                i = drData._87WaterBuckets.IndexOf(geoData._87WaterBuckets[i]);
                                if (i >= NARCOTRAFICANTE)
                                {
                                    drData._87WaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                        }
                    }
                }
                {
                    drData.VerticalPipeLabels.AddRange(lbDict.Values.Distinct());
                }
                {
                    var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                    _longTranslatorLabels.Sort();
                    drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);
                }
                {
                    var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                    _shortTranslatorLabels.Sort();
                    drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);
                }
                {
                }
                drDatas.Add(drData);
            }
            logString = sb.ToString();
            _DrawingTransaction.Current.AbleToDraw = THESAURUSSEMBLANCE;
            return extraInfo;
        }
        public static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas)
        {
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = ADRENOCORTICOTROPHIC;
            }
            for (int storeyI = NARCOTRAFICANTE; storeyI < cadDatas.Count; storeyI++)
            {
                var item = cadDatas[storeyI];
                DrawGeoData(geoData, cadDataMain, storeyI, item);
            }
        }
        private static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, int storeyI, RainCadData item)
        {
            foreach (var o in item.LabelLines)
            {
                DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = ADRENOCORTICOTROPHIC;
            }
            foreach (var pl in item.Labels)
            {
                var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                var e = DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                e.ColorIndex = PHOTOGONIOMETER;
                var _pl = DrawRectLazy(m.Boundary);
                _pl.ColorIndex = PHOTOGONIOMETER;
            }
            foreach (var o in item.PipeKillers)
            {
                DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(THESAURUSTRAGEDY, THESAURUSTRAGEDY, UNANSWERABLENESS);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)], THESAURUSCONSUL);
            }
            foreach (var o in item.VerticalPipes)
            {
                DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = THESAURUSPOSTSCRIPT;
            }
            foreach (var o in item.FloorDrains)
            {
                DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = QUOTATIONLENTIFORM;
            }
            foreach (var o in item.WLines)
            {
                DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = QUOTATIONLENTIFORM;
            }
            foreach (var o in item.WaterPorts)
            {
                DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = ARCHAEOLOGICALLY;
                DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.WaterWells)
            {
                DrawRectLazy(geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ColorIndex = THESAURUSTITTER;
                DrawTextLazy(geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.RainPortSymbols)
            {
                DrawRectLazy(geoData.RainPortSymbols[cadDataMain.RainPortSymbols.IndexOf(o)]).ColorIndex = ARCHAEOLOGICALLY;
            }
            foreach (var o in item.WaterSealingWells)
            {
                DrawRectLazy(geoData.WaterSealingWells[cadDataMain.WaterSealingWells.IndexOf(o)]).ColorIndex = ARCHAEOLOGICALLY;
            }
            foreach (var o in item.GravityWaterBuckets)
            {
                var r = geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = ARCHAEOLOGICALLY;
                Dr.DrawSimpleLabel(r.LeftTop, ULTRACREPIDATION);
            }
            foreach (var o in item.SideWaterBuckets)
            {
                var r = geoData.SideWaterBuckets[cadDataMain.SideWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = ARCHAEOLOGICALLY;
                Dr.DrawSimpleLabel(r.LeftTop, THESAURUSTIRADE);
            }
            foreach (var o in item._87WaterBuckets)
            {
                var r = geoData._87WaterBuckets[cadDataMain._87WaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = ARCHAEOLOGICALLY;
                Dr.DrawSimpleLabel(r.LeftTop, THESAURUSUMBRAGE);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)]).ColorIndex = ADRENOCORTICOTROPHIC;
            }
            foreach (var o in item.CleaningPorts)
            {
                var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                if (UNTRACEABLENESS) DrawGeometryLazy(new GCircle(m, UNDERACHIEVEMENT).ToCirclePolygon(THESAURUSSOMETIMES), ents => ents.ForEach(e => e.ColorIndex = ARCHAEOLOGICALLY));
                DrawRectLazy(GRect.Create(m, THESAURUSSENILE));
            }
            foreach (var o in item.Ditches)
            {
                var m = geoData.Ditches[cadDataMain.Ditches.IndexOf(o)];
                DrawRectLazy(m);
            }
            {
                var cl = Color.FromRgb(THESAURUSINDICT, UNCONSTITUTIONALISM, DIHYDROXYSTILBENE);
                foreach (var o in item.WrappingPipes)
                {
                    DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(THESAURUSTITTER, COUNTERFEISANCE, INTERVOCALICALLY);
                foreach (var o in item.DLines)
                {
                    DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(THESAURUSBELOVED, INSTITUTIONALIZATION, OBOEDIENTIARIUS);
                foreach (var o in item.VLines)
                {
                    DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                }
            }
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
            return new List<KeyValuePair<string, Geometry>>();
        }
    }
    public class WaterBucketItem : IEquatable<WaterBucketItem>
    {
        public WaterBucketEnum WaterBucketType;
        public string DN;
        public bool Equals(WaterBucketItem other)
        {
            return this.WaterBucketType == other.WaterBucketType
                && this.DN == other.DN;
        }
        public static bool operator ==(WaterBucketItem x, WaterBucketItem y)
        {
            return (x is null && y is null) || (x is not null && y is not null && x.Equals(y));
        }
        public static bool operator !=(WaterBucketItem x, WaterBucketItem y) => !(x == y);
        public static WaterBucketItem TryParse(string text)
        {
            WaterBucketEnum bkType;
            if (text == null)
            {
                throw new System.Exception();
            }
            else if (IsGravityWaterBucketLabel(text))
            {
                bkType = WaterBucketEnum.Gravity;
            }
            else if (IsSideWaterBucketLabel(text))
            {
                bkType = WaterBucketEnum.Side;
            }
            else if (Is87WaterBucketLabel(text))
            {
                bkType = WaterBucketEnum._87;
            }
            else
            {
                bkType = WaterBucketEnum.Gravity;
            }
            var dn = GetDN(text);
            if (string.IsNullOrEmpty(dn))
            {
                if (bkType == WaterBucketEnum.Side)
                {
                    dn = THESAURUSOVERCHARGE;
                }
                else
                {
                    dn = SPLANCHNOPLEURE;
                }
            }
            return new WaterBucketItem() { DN = dn, WaterBucketType = bkType, };
        }
        public string GetWaterBucketChName()
        {
            switch (WaterBucketType)
            {
                case WaterBucketEnum.Gravity:
                    return THESAURUSLIBERTINE;
                case WaterBucketEnum.Side:
                    return THESAURUSBEATITUDE;
                case WaterBucketEnum._87:
                    return SUPERHETERODYNE;
                default:
                    return THESAURUSREDOUND;
            }
        }
        public string GetDisplayString()
        {
            switch (WaterBucketType)
            {
                case WaterBucketEnum.None:
                    throw new System.Exception();
                case WaterBucketEnum.Gravity:
                    return THESAURUSLIBERTINE + DN;
                case WaterBucketEnum.Side:
                    return THESAURUSBEATITUDE + DN;
                case WaterBucketEnum._87:
                    return SUPERHETERODYNE + DN;
                default:
                    throw new System.Exception();
            }
        }
        public override int GetHashCode()
        {
            return NARCOTRAFICANTE;
        }
    }
    public enum OutletType
    {
        RainWell,
        RainPort,
        WaterSealingWell,
        Ditch,
        Spreading,
    }
    public class RainGroupedPipeItem
    {
        public OutletType OutletType;
        public string OutletFloor;
        public List<string> Labels;
        public List<string> WaterWellLabels;
        public bool HasOutletWrappingPipe;
        public bool HasWaterWell => WaterWellLabels != null && WaterWellLabels.Count > NARCOTRAFICANTE;
        public List<RainGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public bool HasTl => TlLabels != null && TlLabels.Count > NARCOTRAFICANTE;
        public PipeType PipeType;
        public List<RainGroupingPipeItem.Hanging> Hangings;
        public int FloorDrainsCountAt1F;
        public bool HasLineAtBuildingFinishedSurfice;
        public bool HasSingleFloorDrainDrainageForWaterWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public bool HasSingleFloorDrainDrainageForRainPort;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForRainPort;
        public string OutletWrappingPipeRadius;
        public bool HasSingleFloorDrainDrainageForWaterSealingWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell;
        public bool HasSingleFloorDrainDrainageForDitch;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForDitch;
    }
    public class RainGroupingPipeItem : IEquatable<RainGroupingPipeItem>
    {
        public string Label;
        public bool HasOutletWrappingPipe;
        public bool IsSingleOutlet;
        public string WaterWellLabel;
        public bool HasLineAtBuildingFinishedSurfice;
        public List<ValueItem> Items;
        public List<Hanging> Hangings;
        public string OutletWrappingPipeRadius;
        public PipeType PipeType;
        public OutletType OutletType;
        public int FloorDrainsCountAt1F;
        public string OutletFloor;
        public bool HasSingleFloorDrainDrainageForWaterWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public bool HasSingleFloorDrainDrainageForRainPort;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForRainPort;
        public bool HasSingleFloorDrainDrainageForWaterSealingWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell;
        public bool HasSingleFloorDrainDrainageForDitch;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForDitch;
        public bool Equals(RainGroupingPipeItem other)
        {
            return this.HasOutletWrappingPipe == other.HasOutletWrappingPipe
                && this.HasLineAtBuildingFinishedSurfice == other.HasLineAtBuildingFinishedSurfice
                && this.PipeType == other.PipeType
                && this.IsSingleOutlet == other.IsSingleOutlet
                && this.FloorDrainsCountAt1F == other.FloorDrainsCountAt1F
                && this.HasSingleFloorDrainDrainageForWaterWell == other.HasSingleFloorDrainDrainageForWaterWell
                && this.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell == other.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell
                && this.HasSingleFloorDrainDrainageForRainPort == other.HasSingleFloorDrainDrainageForRainPort
                && this.IsFloorDrainShareDrainageWithVerticalPipeForRainPort == other.IsFloorDrainShareDrainageWithVerticalPipeForRainPort
                && this.HasSingleFloorDrainDrainageForWaterSealingWell == other.HasSingleFloorDrainDrainageForWaterSealingWell
                && this.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell == other.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell
                 && this.HasSingleFloorDrainDrainageForDitch == other.HasSingleFloorDrainDrainageForDitch
                && this.IsFloorDrainShareDrainageWithVerticalPipeForDitch == other.IsFloorDrainShareDrainageWithVerticalPipeForDitch
                && this.OutletWrappingPipeRadius == other.OutletWrappingPipeRadius
                && this.OutletType == other.OutletType
                && this.OutletFloor == other.OutletFloor
                && this.Items.SeqEqual(other.Items)
                && this.Hangings.SeqEqual(other.Hangings);
        }
        public class Hanging : IEquatable<Hanging>
        {
            public string Storey;
            public int FloorDrainsCount;
            public int FloorDrainWrappingPipesCount;
            public bool HasCondensePipe;
            public bool HasBrokenCondensePipes;
            public bool HasNonBrokenCondensePipes;
            public bool PlsDrawCondensePipeHigher;
            public bool HasCheckPoint;
            public bool LongTransHigher;
            public bool HasSideFloorDrain;
            public WaterBucketItem WaterBucket;
            public override int GetHashCode()
            {
                return NARCOTRAFICANTE;
            }
            public bool Equals(Hanging other)
            {
                return this.FloorDrainsCount == other.FloorDrainsCount
                    && this.HasCondensePipe == other.HasCondensePipe
                    && this.HasBrokenCondensePipes == other.HasBrokenCondensePipes
                    && this.HasNonBrokenCondensePipes == other.HasNonBrokenCondensePipes
                    && this.PlsDrawCondensePipeHigher == other.PlsDrawCondensePipeHigher
                    && this.FloorDrainWrappingPipesCount == other.FloorDrainWrappingPipesCount
                    && this.HasCheckPoint == other.HasCheckPoint
                    && this.HasSideFloorDrain == other.HasSideFloorDrain
                    && this.Storey == other.Storey
                    && this.LongTransHigher == other.LongTransHigher
                    && this.WaterBucket == other.WaterBucket
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
            return NARCOTRAFICANTE;
        }
    }
    public enum PipeType
    {
        Y1L, Y2L, NL, YL, FL0
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
    public class CommandContext
    {
        public Point3dCollection range;
        public StoreyContext StoreyContext;
        public RainSystemDiagramViewModel ViewModel;
    }
    public class ThRainService
    {
        public static CommandContext commandContext;
        public static void ConnectLabelToLabelLine(RainGeoData geoData)
        {
            static bool f(string s)
            {
                if (s == null) return UNTRACEABLENESS;
                if (IsMaybeLabelText(s)) return THESAURUSSEMBLANCE;
                return UNTRACEABLENESS;
            }
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Where(x => f(x.Text)).Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(THESAURUSCONSUL)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-THESAURUSCONSUL).OffsetY(-THESAURUSDEMONSTRATION), ELECTROMYOGRAPH, THESAURUSDEMONSTRATION);
                var _lineHGs = f1(g.ToPolygon());
                var geo = GeoFac.NearestNeighbourGeometryF(_lineHGs)(bd.Center.ToNTSPoint());
                if (geo == null) continue;
                {
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(THESAURUSINDUSTRY, THESAURUSALCOVE) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(THESAURUSEMBASSY, THESAURUSALCOVE))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(THESAURUSEMBASSY));
                    }
                }
            }
        }
        public static void PreFixGeoData(RainGeoData geoData)
        {
            geoData.FixData();
            foreach (var ct in geoData.Labels)
            {
                ct.Boundary = ct.Boundary.Expand(-THESAURUSSENILE);
            }
            geoData.Labels = geoData.Labels.Where(x => IsMaybeLabelText(x.Text)).ToList();
            for (int i = NARCOTRAFICANTE; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(THESAURUSFACTOR))
                {
                    geoData.LabelLines[i] = seg.Extend(QUOTATIONLENTIFORM);
                }
                else if (seg.IsVertical(THESAURUSFACTOR))
                {
                    geoData.LabelLines[i] = seg.Extend(ADRENOCORTICOTROPHIC);
                }
            }
            for (int i = NARCOTRAFICANTE; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(THESAURUSFACTOR);
            }
            for (int i = NARCOTRAFICANTE; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(THESAURUSFACTOR);
            }
            for (int i = NARCOTRAFICANTE; i < geoData.WLines.Count; i++)
            {
                geoData.WLines[i] = geoData.WLines[i].Extend(THESAURUSFACTOR);
            }
            for (int i = NARCOTRAFICANTE; i < geoData.VerticalPipes.Count; i++)
            {
                geoData.VerticalPipes[i] = geoData.VerticalPipes[i].Expand(THESAURUSCONSUL);
            }
            {
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < THESAURUSSACRIFICE && x.Height < THESAURUSSACRIFICE).ToList();
            }
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSCONSUL)).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, THESAURUSCONSUL))).ToList();
            }
            {
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.GravityWaterBuckets = GeoFac.GroupGeometries(geoData.GravityWaterBuckets.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.RainPortSymbols = GeoFac.GroupGeometries(geoData.RainPortSymbols.Select(x => x.ToPolygon().Buffer(THESAURUSENTREAT)).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Buffer(-THESAURUSENTREAT).Envelope.ToGRect()).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(THESAURUSEMBASSY);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.CondensePipes = geoData.CondensePipes.Distinct(cmp).ToList();
                geoData.PipeKillers = geoData.PipeKillers.Distinct(cmp).ToList();
            }
            {
                for (int i = NARCOTRAFICANTE; i < geoData.WaterWellLabels.Count; i++)
                {
                    var label = geoData.WaterWellLabels[i];
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        geoData.WaterWellLabels[i] = THESAURUSHEARTLESS;
                    }
                }
            }
            {
                var okPts = new HashSet<Point2d>(geoData.WrappingPipeRadius.Select(x => x.Key));
                var lbs = geoData.WrappingPipeLabels.Select(x => x.ToPolygon()).ToList();
                var lbsf = GeoFac.CreateIntersectsSelector(lbs);
                var lines = geoData.WrappingPipeLabelLines.Select(x => x.ToLineString()).ToList();
                var gs = GeoFac.GroupLinesByConnPoints(lines, THESAURUSNETHER);
                foreach (var geo in gs)
                {
                    var segs = GeoFac.GetLines(geo).ToList();
                    var buf = segs.Where(x => x.IsHorizontal(THESAURUSFACTOR)).Select(x => x.Buffer(THESAURUSINDUSTRY)).FirstOrDefault();
                    if (buf == null) continue;
                    var pts = GeoFac.GetLabelLineEndPoints(segs, Geometry.DefaultFactory.CreatePolygon());
                    foreach (var lb in lbsf(buf))
                    {
                        var label = TryParseWrappingPipeRadiusText(lb.UserData as string);
                        if (!string.IsNullOrWhiteSpace(label))
                        {
                            foreach (var pt in pts)
                            {
                                if (okPts.Contains(pt)) continue;
                                geoData.WrappingPipeRadius.Add(new KeyValuePair<Point2d, string>(pt, label));
                                okPts.Add(pt);
                            }
                        }
                    }
                }
            }
            {
                var kvs = new HashSet<KeyValuePair<Point2d, string>>(geoData.WrappingPipeRadius);
                var okKvs = new HashSet<KeyValuePair<Point2d, string>>();
                geoData.WrappingPipeRadius.Clear();
                foreach (var wp in geoData.WrappingPipes)
                {
                    var gf = wp.ToPolygon().ToIPreparedGeometry();
                    var _kvs = kvs.Except(okKvs).Where(x => gf.Intersects(x.Key.ToNTSPoint())).ToList();
                    var strs = _kvs.Select(x => x.Value).ToList();
                    var nums = strs.Select(x => double.TryParse(x, out double v) ? v : double.NaN).Where(x => !double.IsNaN(x)).ToList();
                    if (nums.Count > ADRENOCORTICOTROPHIC)
                    {
                        var min = nums.Min();
                        var str = strs.First(x => double.Parse(x) == min);
                        foreach (var kv in _kvs)
                        {
                            kvs.Remove(kv);
                        }
                        foreach (var kv in _kvs)
                        {
                            if (kv.Value == str)
                            {
                                kvs.Add(kv);
                                okKvs.Add(kv);
                                break;
                            }
                        }
                    }
                }
                geoData.WrappingPipeRadius.AddRange(kvs);
            }
            {
                var v = UNDERACHIEVEMENT;
                for (int i = NARCOTRAFICANTE; i < geoData.WrappingPipes.Count; i++)
                {
                    var wp = geoData.WrappingPipes[i];
                    if (wp.Width > v * PHOTOGONIOMETER)
                    {
                        geoData.WrappingPipes[i] = wp.Expand(-v);
                    }
                }
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
            {
                foreach (var ct in geoData.Labels)
                {
                    if (ct.Text.StartsWith(THESAURUSDUDGEON))
                    {
                        ct.Text = ct.Text.Substring(THESAURUSPOSTSCRIPT);
                    }
                    else if (ct.Text.StartsWith(QUOTATIONAMNIOTIC))
                    {
                        ct.Text = ct.Text.Substring(PHOTOGONIOMETER);
                    }
                    else if (ct.Text.StartsWith(THESAURUSTITULAR))
                    {
                        ct.Text = OPISTHOBRANCHIATA + ct.Text.Substring(THESAURUSPOSTSCRIPT);
                    }
                }
            }
            geoData.FixData();
        }
        public static void CollectFloorListDatasEx(bool focus)
        {
            if (focus) FocusMainWindow();
            ThMEPWSS.Common.FramedReadUtil.SelectFloorFramed(out _, () =>
            {
                using (DocLock)
                {
                    var range = TrySelectRangeEx();
                    TryUpdateByRange(range, UNTRACEABLENESS);
                }
            });
        }
        public static void TryUpdateByRange(Point3dCollection range, bool _lock)
        {
            void f()
            {
                if (range == null) return;
                using var adb = AcadDatabase.Active();
                var (ctx, brs) = GetStoreyContext(range, adb);
                commandContext.StoreyContext = ctx;
                InitFloorListDatas(adb, brs);
                CadCache.SetCache(CadCache.CurrentFile, THESAURUSALTOGETHER, range);
                CadCache.UpdateByRange(range);
            }
            if (_lock)
            {
                using (DocLock)
                {
                    f();
                }
            }
            else
            {
                f();
            }
        }
        public static (StoreyContext, List<BlockReference>) GetStoreyContext(Point3dCollection range, AcadDatabase adb)
        {
            var ctx = new StoreyContext();
            var geo = range?.ToGRect().ToPolygon();
            var brs = GetStoreyBlockReferences(adb);
            var _brs = new List<BlockReference>();
            var storeys = new List<StoreyInfo>();
            foreach (var br in brs)
            {
                var info = GetStoreyInfo(br);
                if (geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSSEMBLANCE)
                {
                    _brs.Add(br);
                    storeys.Add(info);
                }
            }
            FixStoreys(storeys);
            ctx.StoreyInfos = storeys;
            return (ctx, _brs);
        }
        public static void InitFloorListDatas(AcadDatabase adb, List<BlockReference> brs)
        {
            var ctx = commandContext.StoreyContext;
            var storeys = brs.Select(x => x.ObjectId).ToObjectIdCollection();
            var service = new ThReadStoreyInformationService();
            service.Read(storeys);
            commandContext.ViewModel.FloorListDatas = service.StoreyNames.Select(o => o.Item2).ToList();
        }
    }
    public static class THRainService
    {
        public static string TryParseWrappingPipeRadiusText(string text)
        {
            if (text == null) return null;
            var t = Regex.Replace(text, HYPERPOLARIZATION, THESAURUSREDOUND, RegexOptions.IgnoreCase);
            t = Regex.Replace(t, THESAURUSFOSTER, THESAURUSREDOUND);
            t = Regex.Replace(t, THESAURUSINDOCTRINATE, THESAURUSHEARTLESS);
            return t;
        }
        public static StoreyInfo GetStoreyInfo(BlockReference br)
        {
            var props = br.DynamicBlockReferencePropertyCollection;
            return new StoreyInfo()
            {
                StoreyType = GetStoreyType((string)props.GetValue(THESAURUSHORIZON)),
                Numbers = ParseFloorNums(GetStoreyNumberString(br)),
                ContraPoint = GetContraPoint(br),
                Boundary = br.Bounds.ToGRect(),
            };
        }
        public static StoreyType GetStoreyType(string s)
        {
            return s switch
            {
                ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_TOP_ROOF_FLOOR => StoreyType.SmallRoof,
                ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ROOF_FLOOR => StoreyType.LargeRoof,
                ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_STANDARD_FLOOR => StoreyType.StandardStorey,
                ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NON_STANDARD_FLOOR => StoreyType.NonStandardStorey,
                ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NOT_STANDARD_FLOOR => StoreyType.NonStandardStorey,
                _ => StoreyType.Unknown,
            };
        }
        public static List<int> ParseFloorNums(string floorStr)
        {
            if (string.IsNullOrWhiteSpace(floorStr)) return new List<int>();
            floorStr = floorStr.Replace(THESAURUSPROFIT, TRANSMIGRATIONIST).Replace(QUOTATIONIMPERIUM, THESAURUSPENETRATING).Replace(QUOTATIONHOUSEMAID, THESAURUSREDOUND).Replace(THESAURUSADVERSARY, THESAURUSREDOUND).Replace(THESAURUSSUCKLE, THESAURUSREDOUND);
            var hs = new HashSet<int>();
            foreach (var s in floorStr.Split(TRANSMIGRATIONIST))
            {
                if (string.IsNullOrEmpty(s)) continue;
                var m = Regex.Match(s, THESAURUSTHRILL);
                if (m.Success)
                {
                    var v1 = int.Parse(m.Groups[ADRENOCORTICOTROPHIC].Value);
                    var v2 = int.Parse(m.Groups[PHOTOGONIOMETER].Value);
                    var min = Math.Min(v1, v2);
                    var max = Math.Max(v1, v2);
                    for (int i = min; i <= max; i++)
                    {
                        hs.Add(i);
                    }
                    continue;
                }
                m = Regex.Match(s, THESAURUSGLISTEN);
                if (m.Success)
                {
                    hs.Add(int.Parse(m.Value));
                }
            }
            hs.Remove(NARCOTRAFICANTE);
            return hs.OrderBy(x => x).ToList();
        }
        public static string GetStoreyNumberString(BlockReference br)
        {
            var d = br.ObjectId.GetAttributesInBlockReference(THESAURUSSEMBLANCE);
            d.TryGetValue(THESAURUSAPPLICANT, out string ret);
            return ret;
        }
        public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() is THESAURUSCLEARANCE && x.IsDynamicBlock).ToList();
        public static Point2d GetContraPoint(BlockReference br)
        {
            double dx = double.NaN;
            double dy = double.NaN;
            Point2d pt;
            foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
            {
                if (p.PropertyName == THESAURUSDOWNPOUR)
                {
                    dx = Convert.ToDouble(p.Value);
                }
                else if (p.PropertyName == UNOBJECTIONABLY)
                {
                    dy = Convert.ToDouble(p.Value);
                }
            }
            if (!double.IsNaN(dx) && !double.IsNaN(dy))
            {
                pt = br.Position.ToPoint2d() + new Vector2d(dx, dy);
            }
            else
            {
                throw new System.Exception(THESAURUSBUDGET);
            }
            return pt;
        }
        public static void FixStoreys(List<StoreyInfo> storeys)
        {
            var lst1 = storeys.Where(s => s.Numbers.Count == ADRENOCORTICOTROPHIC).Select(s => s.Numbers[NARCOTRAFICANTE]).ToList();
            foreach (var s in storeys.Where(s => s.Numbers.Count > ADRENOCORTICOTROPHIC).ToList())
            {
                var hs = new HashSet<int>(s.Numbers);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Numbers.Clear();
                s.Numbers.AddRange(hs.OrderBy(i => i));
            }
        }
        public const int ADRENOCORTICOTROPHIC = 1;
        public const int THESAURUSFACTOR = 5;
        public const int PHOTOGONIOMETER = 2;
        public const int NARCOTRAFICANTE = 0;
        public const double THESAURUSDEPOSIT = 10e5;
        public const double THESAURUSEMBASSY = .1;
        public const bool UNTRACEABLENESS = false;
        public const int THESAURUSSOMETIMES = 36;
        public const bool THESAURUSSEMBLANCE = true;
        public const int THESAURUSSENILE = 40;
        public const int ELECTROMYOGRAPH = 1500;
        public const int QUOTATIONLENTIFORM = 6;
        public const int PHOTOSENSITIZING = 4096;
        public const string THESAURUSLETTERED = "空调内机--柜机";
        public const string DISTEMPEREDNESS = "空调内机--挂机";
        public const string PALAEOPATHOLOGIST = "TCH_PIPE";
        public const string THESAURUSCOMMOTION = "W-RAIN-PIPE";
        public const int THESAURUSGOBLIN = 35;
        public const string THESAURUSPERFECT = "-RAIN-";
        public const string THESAURUSACCEDE = "W-BUSH-NOTE";
        public const string TRANYLCYPROMINE = "W-RAIN-DIMS";
        public const int THESAURUSINDUSTRY = 100;
        public const string THESAURUSSANCTITY = "W-RAIN-EQPM";
        public const int STEPMOTHERLINESS = 20;
        public const string THESAURUSEGOTISM = "TCH_VPIPEDIM";
        public const string THESAURUSHEARTLESS = "-";
        public const string THESAURUSSHAMBLE = "TCH_TEXT";
        public const string THESAURUSSUPERVISE = "TCH_EQUIPMENT";
        public const string INHOMOGENEOUSLY = "TCH_MTEXT";
        public const string THESAURUSINOFFENSIVE = "TCH_MULTILEADER";
        public const string THESAURUSREDOUND = "";
        public const string THESAURUSCAPRICIOUS = "接";
        public const string NANOPHANEROPHYTE = "排水沟";
        public const string POLYSOMNOGRAPHY = "雨水口";
        public const int UNDERACHIEVEMENT = 50;
        public const char THESAURUSSUFFICE = '|';
        public const string THESAURUSRANSACK = "|";
        public const char POLYCRYSTALLINE = '$';
        public const string HENDECASYLLABUS = "$";
        public const string THERMODYNAMICIST = "C7C497AC6";
        public const string KNICKERBOCKERED = "H-AC-1";
        public const string MACHAIRODONTINAE = "H-AC-4";
        public const string INCOMPATIBILITY = "水封井";
        public const string UNPALATABLENESS = "雨水井编号";
        public const string QUOTATIONNOXIOUS = "13#雨水口";
        public const string PSYCHOPROPHYLAXIS = "W-DRAIN-1";
        public const string THESAURUSSTUDENT = "$W-DRAIN-1";
        public const string NEUROPHYSIOLOGY = "W-DRAIN-2";
        public const string THESAURUSCUSTOM = "W-DRAIN-5";
        public const string MICROMANIPULATIONS = "$W-DRAIN-2";
        public const string CIRCUMCONVOLUTION = "$W-DRAIN-5";
        public const string THESAURUSPROSPECTUS = "CYSD";
        public const string THESAURUSEJECTION = "重力流雨水斗";
        public const string UNIDIOMATICALLY = "$QYSD";
        public const string THESAURUSBACTERIA = "地漏";
        public const string INAPPREHENSIBILIS = "DL";
        public const string THESAURUSEXHUME = @"\$?地漏平面$";
        public const string CRYSTALLIZATIONS = "可见性";
        public const string THESAURUSUNCOUTH = "侧排地漏";
        public const string THESAURUSPRESENTABLE = "套管";
        public const int QUOTATIONTRILINEAR = 1000;
        public const string THESAURUSUNITED = "带定位立管";
        public const string THESAURUSWRONGFUL = "立管编号";
        public const string ENTERCOMMUNICATE = "$LIGUAN";
        public const string COSTERMONGERING = "A$C6BDE4816";
        public const string THESAURUSSTUPEFACTION = "污废合流井编号";
        public const string THESAURUSOVERWHELM = "W-DRAI-DOME-PIPE";
        public const string VERGELTUNGSWAFFE = "W-RAIN-NOTE";
        public const char THESAURUSIDENTICAL = 'A';
        public const char QUOTATIONIMPERIUM = 'B';
        public const string THESAURUSADHERE = "RF";
        public const string IMMUNOGENETICALLY = "RF+1";
        public const string THESAURUSNATURALIST = "RF+2";
        public const string QUOTATIONHOUSEMAID = "F";
        public const string PALAEONTOLOGICAL = @"^重力型雨水斗(DN\d+)$";
        public const int THESAURUSCONSUL = 10;
        public const int THESAURUSALCOVE = 400;
        public const string THESAURUSDEVOUR = "bk对位";
        public const string QUOTATIONRESPIRATORY = "重力型雨水斗DN100";
        public const string THESAURUSINSCRIPTION = "楼上";
        public const string THESAURUSALCOHOLIC = "1F";
        public const string THESAURUSLADYLIKE = "-1F";
        public const string ELECTRODEPOSITION = "侧入式雨水斗DN100";
        public const string QUOTATIONGOFFERING = "重力雨水斗DN100";
        public const string THESAURUSDISCLOSE = "-0.XX";
        public const string FINNLANDISIERUNG = "W-NOTE";
        public const string THESAURUSUNDERSTATE = "W-DRAI-EQPM";
        public const string THESAURUSPRELIMINARY = "W-BUSH";
        public const string THESAURUSPROCEEDING = "W-DRAI-NOTE";
        public const double THESAURUSWOMANLY = 2500.0;
        public const double THESAURUSCONTINUATION = 5500.0;
        public const double THESAURUSINFERENCE = 1800.0;
        public const string ADVERTISEMENTAL = "伸顶通气2000";
        public const string ORTHONORMALIZING = "伸顶通气500";
        public const string PERPENDICULARITY = "本层通气";
        public const string UNSATISFACTORINESS = "通气帽系统-AI2";
        public const string PHOTOCONDUCTING = "可见性1";
        public const string STAPHYLORRHAPHY = "通气帽系统";
        public const string THERMOREGULATORY = "距离1";
        public const string THESAURUSSURFACE = "伸顶通气管";
        public const string QUOTATIONOPHTHALMIA = "1000";
        public const int THESAURUSINEVITABLE = 580;
        public const string UNEXCEPTIONABLE = "标高";
        public const int QUINALBARBITONE = 550;
        public const string THESAURUSAFFRONT = "建筑完成面";
        public const int THESAURUSINSTEAD = 1800;
        public const int CONSTITUTIONALLY = 121;
        public const int THESAURUSPRONOUNCED = 1258;
        public const int THESAURUSFRIGHT = 120;
        public const int THESAURUSINFILTRATE = 779;
        public const int THESAURUSESTRANGE = 1679;
        public const int THESAURUSHORRENDOUS = 658;
        public const int THESAURUSITINERANT = 90;
        public const string MUSCULOTENDINOUS = @"^(\-?\d+)\-(\-?\d+)$";
        public const string THESAURUSCONSUME = @"^\-?\d+$";
        public const string THESAURUSLUXURIANT = "±0.00";
        public const double PSYCHOHISTORICAL = 1000.0;
        public const string THESAURUSFLIGHT = "0.00";
        public const int THESAURUSORNAMENT = 800;
        public const int HYDROSTATICALLY = 500;
        public const int THESAURUSAPOCRYPHAL = 1879;
        public const int AUTHORITARIANISM = 180;
        public const int THESAURUSANCILLARY = 160;
        public const string TRANSUBSTANTIALLY = "普通地漏无存水弯";
        public const string ENDONUCLEOLYTIC = "DN25";
        public const int THESAURUSARTISAN = 750;
        public const int SACCHAROMYCETACEAE = 2076;
        public const int PHOTOCONVERSION = 659;
        public const string INCOMBUSTIBLENESS = "*";
        public const double QUOTATIONEXPANDING = 0.0;
        public const int QUOTATION1BRICKETY = 780;
        public const int THESAURUSSACRIFICE = 700;
        public const double THESAURUSTATTLE = .0;
        public const int THESAURUSINTENTIONAL = 300;
        public const int QUOTATIONMUCOUS = 24;
        public const int EXTRAJUDICIALIS = 9;
        public const int THESAURUSTITTER = 4;
        public const int THESAURUSPOSTSCRIPT = 3;
        public const int PLURALISTICALLY = 3600;
        public const int THESAURUSDETEST = 350;
        public const double THESAURUSDISCOUNT = .8;
        public const int THESAURUSLEVITATE = 150;
        public const string THESAURUSCOMMODIOUS = "150";
        public const string THESAURUSVILLAINY = "散排至";
        public const int EVERLASTINGNESS = 125;
        public const int THESAURUSREQUISITION = 83;
        public const int THESAURUSWAYWARD = 1400;
        public const int THESAURUSINIQUITOUS = 621;
        public const int QUOTATIONCAPABLE = 1200;
        public const int THESAURUSDIRECTIVE = 2000;
        public const int VENTRILOQUISTIC = 600;
        public const int IRREMUNERABILIS = 479;
        public const string SPLANCHNOPLEURE = "DN100";
        public const int THESAURUSUSABLE = 950;
        public const int THESAURUSPURPORT = 431;
        public const int TRINITROTOLUENE = 2161;
        public const int THESAURUSWRANGLE = 387;
        public const int CONSERVATIVENESS = 1106;
        public const int AEROTHERMODYNAMICS = 3860;
        public const int THESAURUSGAINSAY = 1050;
        public const int THESAURUSCOTERIE = 306;
        public const int INTERPRETATIVELY = 269;
        public const int QUOTATIONIXIONIAN = 1481;
        public const int PROFESSIONALISM = 281;
        public const int THESAURUSEMBRACE = 501;
        public const int THESAURUSFEASIBILITY = 453;
        public const int THESAURUSCRITICISM = 2693;
        public const int THESAURUSENTREAT = 200;
        public const string THESAURUSFRANTIC = "接至排水沟";
        public const int THESAURUSACCOMPLISHMENT = 173;
        public const int THESAURUSSLOBBER = 187;
        public const int IMPLICATIONALLY = 1907;
        public const int THESAURUSCESSATION = 499;
        public const int THESAURUSFRIENDLY = 1600;
        public const int THESAURUSDEFERENCE = 360;
        public const int THESAURUSTROUPE = 130;
        public const int QUOTATIONMAXWELL = 1070;
        public const int THESAURUSOBEISANCE = 650;
        public const int THESAURUSENCOURAGEMENT = 30;
        public const int THESAURUSEXPLETIVE = 331;
        public const int SUBORDINATIONISTS = 821;
        public const string BASIDIOMYCOTINA = "≥600";
        public const int THESAURUSCONDITIONAL = 1280;
        public const int THESAURUSFLUENT = 450;
        public const int THESAURUSCONTINGENT = 18;
        public const int THESAURUSDEMONSTRATION = 250;
        public const int COMPREHENSIBILITY = 3000;
        public const double LYMPHANGIOMATOUS = .7;
        public const int THESAURUSLOADED = 2200;
        public const string THESAURUSPOSITION = ";";
        public const double QUOTATIONEXOPHTHALMIC = .01;
        public const double QUOTATIONNAPIERIAN = 10e6;
        public const string RECONSTRUCTIONAL = "地漏系统";
        public const string THESAURUSTRAFFIC = "TH-STYLE3";
        public const int THESAURUSIGNORE = 745;
        public const string THESAURUSINGENUOUS = "乙字弯";
        public const string ENTREPRENEURISM = "立管检查口";
        public const string THESAURUSCELEBRATE = "套管系统";
        public const string THESAURUSNOVICE = "防水套管水平";
        public const string THESAURUSBALLAST = "重力流雨水井编号";
        public const string THESAURUSSCAVENGER = "666";
        public const string THESAURUSSOOTHE = @"^(Y1L|Y2L|NL)(\w*)\-(\d*)([a-zA-Z]*)$";
        public const string THESAURUSAUTOGRAPH = ",";
        public const string THESAURUSRELIGIOUS = "~";
        public const string QUOTATION1BMIDDLE = "FloorDrains";
        public const string THESAURUSALLUSION = "GravityWaterBuckets";
        public const string THESAURUSCOMMANDER = "SideWaterBuckets";
        public const string THESAURUSUMBRAGE = "87WaterBuckets";
        public const int ARCHAEOLOGICALLY = 7;
        public const int COUNTERFEISANCE = 229;
        public const int INTERVOCALICALLY = 230;
        public const int THESAURUSVOLITION = 8192;
        public const int THESAURUSOVERFLOW = 8000;
        public const int THESAURUSNETHER = 15;
        public const string COMPOSITIONALLY = "FromImagination";
        public const int UNANSWERABLENESS = 55;
        public const string THESAURUSUNMITIGATED = "ConnectedToGravityWaterBucket";
        public const string THESAURUSFETCHING = "ConnectedToSideWaterBucket";
        public const string QUOTATIONPERUVIAN = "X.XX";
        public const string DISTINGUISHABLE = "WaterWellIds:";
        public const string THESAURUSPARENT = "RainPortIds:";
        public const string THESAURUSPATRONAGE = "WaterSealingWellIds:";
        public const string THESAURUSDISSONANCE = "DitchIds:";
        public const string PRESIDENTIALIST = "排出：";
        public const int THESAURUSBREEDING = 666;
        public const string THESAURUSINSOMNIA = "排出套管：";
        public const string CONTRADICTIVELY = @"接(\d+F)屋面雨水斗";
        public const string QUOTATIONVARANGIAN = "屋面雨水斗";
        public const string THESAURUSDEPRAVE = "RoofWaterBuckets:";
        public const double VICISSITUDINOUS = 1500.0;
        public const string THESAURUSINGRATIATE = "WaterWellWrappingPipeRadiusStringDict:";
        public const string THESAURUSDISTORTION = "HasSingleFloorDrainDrainage:";
        public const string QUOTATIONPERFIDIOUS = "FloorDrainShareDrainageWithVerticalPipe:";
        public const string SIPHONODONTACEAE = "HasSingleFloorDrainDrainageForRainPort:";
        public const string THESAURUSTRANSVERSE = "FloorDrainShareDrainageWithVerticalPipeForRainPort:";
        public const string THESAURUSFIRSTHAND = "HasSingleFloorDrainDrainageForWaterSealingWell:";
        public const string THESAURUSPROSTITUTION = "FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell:";
        public const string CONFORMATIONALLY = "HasSingleFloorDrainDrainageForDitch:";
        public const string THESAURUSPARVENU = "FloorDrainShareDrainageWithVerticalPipeForDitch:";
        public const string QUOTATIONGENERAL = "AloneFloorDrainInfos:";
        public const string PSYCHOGEOGRAPHY = "DN";
        public const string THESAURUSEXODUS = "立管：";
        public const string THESAURUSDEPICT = "长转管:";
        public const string SEDENTARIZATION = "短转管:";
        public const string THESAURUSEXCLAMATION = "地漏：";
        public const string UNREALISTICALLY = "地漏套管：";
        public const string THESAURUSNIGGARDLY = "HasCondensePipe：";
        public const string SCHOPENHAUERIST = "HasBrokenCondensePipes：";
        public const string THESAURUSHUSTLE = "HasNonBrokenCondensePipes：";
        public const string THESAURUSUNACCOMPLISHED = "HasRainPortSymbols：";
        public const int THESAURUSTRAGEDY = 255;
        public const string ULTRACREPIDATION = "GravityWaterBucket";
        public const string THESAURUSTIRADE = "SideWaterBucket";
        public const int THESAURUSINDICT = 0x91;
        public const int UNCONSTITUTIONALISM = 0xc7;
        public const int DIHYDROXYSTILBENE = 0xae;
        public const int THESAURUSBELOVED = 211;
        public const int INSTITUTIONALIZATION = 213;
        public const int OBOEDIENTIARIUS = 111;
        public const string THESAURUSLIBERTINE = "重力雨水斗";
        public const string THESAURUSBEATITUDE = "侧入式雨水斗";
        public const string SUPERHETERODYNE = "87雨水斗";
        public const string THESAURUSDUDGEON = "73-";
        public const string QUOTATIONAMNIOTIC = "1-";
        public const string THESAURUSTITULAR = "LN-";
        public const string OPISTHOBRANCHIATA = "NL-";
        public const string THESAURUSFOSTER = @"[^\d\.\-]";
        public const string THESAURUSINDOCTRINATE = @"\d+\-";
        public const string THESAURUSHORIZON = "楼层类型";
        public const char THESAURUSPROFIT = '，';
        public const char TRANSMIGRATIONIST = ',';
        public const char THESAURUSPENETRATING = '-';
        public const string THESAURUSADVERSARY = "M";
        public const string THESAURUSSUCKLE = " ";
        public const string THESAURUSTHRILL = @"(\-?\d+)-(\-?\d+)";
        public const string THESAURUSGLISTEN = @"\-?\d+";
        public const string THESAURUSAPPLICANT = "楼层编号";
        public const string THESAURUSCLEARANCE = "楼层框定";
        public const string THESAURUSDOWNPOUR = "基点 X";
        public const string UNOBJECTIONABLY = "基点 Y";
        public const string THESAURUSBUDGET = "error occured while getting baseX and baseY";
        public const string THESAURUSFORTIFY = "雨水斗";
        public const string THESAURUSDEMOLISH = "重力";
        public const string KLEPTOPARASITIC = "侧入";
        public const string THESAURUSCONSTITUTE = "87";
        public const string GIGANTOPITHECUS = @"(DN\d+)";
        public const string PARASYMPATHOMIMETIC = "卫生间";
        public const string UNWHOLESOMENESS = "主卫";
        public const string CONSENTANEOUSNESS = "公卫";
        public const string THESAURUSGOODBYE = "次卫";
        public const string COMPUTERIZATION = "客卫";
        public const string THESAURUSINADVERTENT = "洗手间";
        public const string CYTOGENETICALLY = "卫";
        public const string QUOTATIONPOYNTING = @"^[卫]\d$";
        public const string THESAURUSAGHAST = "厨房";
        public const string ANTHROPOMORPHIZE = "西厨";
        public const string QUOTATION1ASEGMENT = "厨";
        public const string CONGREGATIONIST = "阳台";
        public const string QUOTATIONPURKINJE = "连廊";
        public const string THESAURUSTERMINATION = "Y1L";
        public const string INHERITABLENESS = "Y2L";
        public const string BIOSYSTEMATICALLY = "NL";
        public const string THESAURUSHOOKED = "YL";
        public const string QUOTATIONBITUMINOUS = @"^W\d?L";
        public const string THESAURUSPROPITIOUS = @"^F\d?L";
        public const string THESAURUSINTUMESCENCE = "-0";
        public const string THESAURUSOBLOQUY = @"^P\d?L";
        public const string HYPERPARASITISM = @"^T\d?L";
        public const string ALSOPORCELLANIC = @"^D\d?L";
        public const string THESAURUSOVERCHARGE = "DN75";
        public const int THESAURUSDERISION = 60;
        public const string THESAURUSRESTFUL = @"^([^\-]*)\-([A-Za-z])$";
        public const string THESAURUSPROPHETIC = @"^([^\-]*\-[A-Za-z])(\d+)$";
        public const string QUOTATIONDIAMONDBACK = @"^([^\-]*\-)([A-Za-z])(\d+)$";
        public const string MANIFESTATIONAL = "N";
        public const string THESAURUSALTOGETHER = "SelectedRange";
        public const string INTOLERABLENESS = @"^DN\d+$";
        public const string ALSOCONCOMITANCY = "FL-O";
        public const string QUOTATIONDEFLAGRATING = "重力型雨水斗";
        public const string THESAURUSSUSTAINED = "侧入型雨水斗";
        public const string THESAURUSSLIPSHOD = "87型雨水斗";
        public const int THESAURUSUNBELIEVABLE = 357;
        public const int THESAURUSASSISTANT = 2650;
        public const int PARADEIGMATIKOS = 1410;
        public const int THESAURUSDULCET = 220;
        public const int THESAURUSGLOSSY = 240;
        public const int THESAURUSPULSATE = 352;
        public const int THESAURUSDESTRUCTIVE = 2895;
        public const string HYPERPOLARIZATION = @"DN\d+";
        public const int KHRUSELEPHANTINOS = 198;
        public const int THESAURUSSILENT = 353;
        public static bool IsRainLabel(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label) || IsFL0(label);
        }
        public static bool IsWaterBucketLabel(string label)
        {
            return label.Contains(THESAURUSFORTIFY);
        }
        public static bool IsGravityWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(THESAURUSDEMOLISH);
        }
        public static bool IsSideWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(KLEPTOPARASITIC);
        }
        public static bool Is87WaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && !IsGravityWaterBucketLabel(label) && !IsSideWaterBucketLabel(label) && label.Contains(THESAURUSCONSTITUTE);
        }
        public static string GetDN(string label)
        {
            if (label == null) return null;
            var m = Regex.Match(label, GIGANTOPITHECUS, RegexOptions.IgnoreCase);
            if (m.Success) return m.Value;
            return null;
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return IsRainLabel(label) || label.Contains(THESAURUSFORTIFY) || label.Contains(POLYSOMNOGRAPHY) || label.Contains(NANOPHANEROPHYTE) || Regex.IsMatch(label.Trim(), INTOLERABLENESS);
        }
        public static bool IsToilet(string roomName)
        {
            var roomNameContains = new List<string>
            {
                PARASYMPATHOMIMETIC,UNWHOLESOMENESS,CONSENTANEOUSNESS,
                THESAURUSGOODBYE,COMPUTERIZATION,THESAURUSINADVERTENT,
            };
            if (string.IsNullOrEmpty(roomName))
                return UNTRACEABLENESS;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSSEMBLANCE;
            if (roomName.Equals(CYTOGENETICALLY))
                return THESAURUSSEMBLANCE;
            return Regex.IsMatch(roomName, QUOTATIONPOYNTING);
        }
        public static bool IsKitchen(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSAGHAST, ANTHROPOMORPHIZE };
            if (string.IsNullOrEmpty(roomName))
                return UNTRACEABLENESS;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSSEMBLANCE;
            if (roomName.Equals(QUOTATION1ASEGMENT))
                return THESAURUSSEMBLANCE;
            return UNTRACEABLENESS;
        }
        public static bool IsBalcony(string roomName)
        {
            var roomNameContains = new List<string> { CONGREGATIONIST };
            if (string.IsNullOrEmpty(roomName))
                return UNTRACEABLENESS;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSSEMBLANCE;
            return UNTRACEABLENESS;
        }
        public static bool IsCorridor(string roomName)
        {
            var roomNameContains = new List<string> { QUOTATIONPURKINJE };
            if (string.IsNullOrEmpty(roomName))
                return UNTRACEABLENESS;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSSEMBLANCE;
            return UNTRACEABLENESS;
        }
        public static bool IsY1L(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return label.StartsWith(THESAURUSTERMINATION);
        }
        public static bool IsY2L(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return label.StartsWith(INHERITABLENESS);
        }
        public static bool IsNL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return label.StartsWith(BIOSYSTEMATICALLY);
        }
        public static bool IsYL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return label.StartsWith(THESAURUSHOOKED);
        }
        public static bool IsWL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return Regex.IsMatch(label, QUOTATIONBITUMINOUS);
        }
        public static bool IsFL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return Regex.IsMatch(label, THESAURUSPROPITIOUS);
        }
        public static bool IsFL0(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            if (label == ALSOCONCOMITANCY) return THESAURUSSEMBLANCE;
            return IsFL(label) && label.Contains(THESAURUSINTUMESCENCE);
        }
        public static bool IsPL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return Regex.IsMatch(label, THESAURUSOBLOQUY);
        }
        public static bool IsTL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return Regex.IsMatch(label, HYPERPARASITISM);
        }
        public static bool IsDL(string label)
        {
            if (string.IsNullOrEmpty(label)) return UNTRACEABLENESS;
            return Regex.IsMatch(label, ALSOPORCELLANIC);
        }
    }
}