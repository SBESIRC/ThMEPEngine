namespace ThMEPWSS.ReleaseNs.RainSystemNs
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using Autodesk.AutoCAD.DatabaseServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using Autodesk.AutoCAD.Colors;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using NetTopologySuite.Geometries;
    using ThMEPEngineCore.Algorithm;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
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
            if (segs.Count <= THESAURUSHOUSING) yield break;
            var geos = segs.Select(x => x.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList();
            var gs = GeoFac.GroupGeometries(geos);
            if (gs.Count >= THESAURUSPERMUTATION)
            {
                for (int i = THESAURUSSTAMPEDE; i < gs.Count - THESAURUSHOUSING; i++)
                {
                    foreach (var seg in GetMinConnSegs(gs[i], gs[i + THESAURUSHOUSING]))
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
            if (lines1.Count > THESAURUSSTAMPEDE && lines2.Count > THESAURUSSTAMPEDE)
            {
                var dis = TempGeoFac.GetMinDis(lines1, lines2, out LineString ls1, out LineString ls2);
                if (dis > THESAURUSSTAMPEDE && ls1 != null && ls2 != null)
                {
                    foreach (var seg in TempGeoFac.TryExtend(GeoFac.GetLines(ls1).First(), GeoFac.GetLines(ls2).First(), ELECTROLUMINESCENT))
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
            if (dg <= THESAURUSCOMMUNICATION)
            {
                var dis = s1.ToLineString().Distance(s2.ToLineString());
                if (dis <= THESAURUSSTAMPEDE) yield break;
                if (s1.StartPoint.GetDistanceTo(s2.StartPoint).EqualsTo(dis, THESAURUSACRIMONIOUS)) yield return new GLineSegment(s1.StartPoint, s2.StartPoint);
                if (s1.StartPoint.GetDistanceTo(s2.EndPoint).EqualsTo(dis, THESAURUSACRIMONIOUS)) yield return new GLineSegment(s1.StartPoint, s2.EndPoint);
                if (s1.EndPoint.GetDistanceTo(s2.StartPoint).EqualsTo(dis, THESAURUSACRIMONIOUS)) yield return new GLineSegment(s1.EndPoint, s2.StartPoint);
                if (s1.EndPoint.GetDistanceTo(s2.EndPoint).EqualsTo(dis, THESAURUSACRIMONIOUS)) yield return new GLineSegment(s1.EndPoint, s2.EndPoint);
                yield break;
            }
            var pt = s1.Extend(extend).ToLineString().Intersection(s2.Extend(extend).ToLineString()) as Point;
            if (pt != null)
            {
                var bf1 = s1.ToLineString().Buffer(ASSOCIATIONISTS);
                var bf2 = s2.ToLineString().Buffer(ASSOCIATIONISTS);
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
        public HashSet<GLineSegment> OWLines;
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
        public List<List<Geometry>> Groups;
        public List<GCircle> FloorDrainRings;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            StoreyContraPoints ??= new List<Point2d>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            OWLines ??= new HashSet<GLineSegment>();
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
            Groups ??= new List<List<Geometry>>();
            FloorDrainRings ??= new List<GCircle>();
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
                for (int i = THESAURUSSTAMPEDE; i < WaterWells.Count; i++)
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
        public List<StoreyItem> storeysItems;
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
            o.FloorDrains.AddRange(data.FloorDrains.Select(x => x.ToGCircle(INTRAVASCULARLY).ToCirclePolygon(THESAURUSDISINGENUOUS, THESAURUSOBSTINACY)));
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
            return x => new GCircle(x, DISPENSABLENESS).ToCirclePolygon(THESAURUSDISINGENUOUS);
        }
        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(THESAURUSDICTATORIAL).ToCirclePolygon(SUPERLATIVENESS);
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
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(THESAURUSDISINGENUOUS);
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
            return x => x.Extend(ASSOCIATIONISTS).ToLineString();
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(THESAURUSREPERCUSSION);
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
            if (this.Storeys.Count == THESAURUSSTAMPEDE) return lst;
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
        static bool HandleGroupAtCurrentModelSpaceOnly = INTRAVASCULARLY;
        public void CollectEntities()
        {
            {
                var dict = ThMEPWSS.ViewModel.BlockConfigService.GetBlockNameListDict();
                dict.TryGetValue(THESAURUSMARSHY, out List<string> lstVertical);
                if (lstVertical != null) this.VerticalAiringMachineNames = new HashSet<string>(lstVertical);
                dict.TryGetValue(THESAURUSPROFANITY, out List<string> lstHanging);
                if (lstHanging != null) this.HangingAiringMachineNames = new HashSet<string>(lstHanging);
                this.VerticalAiringMachineNames ??= new HashSet<string>();
                this.HangingAiringMachineNames ??= new HashSet<string>();
            }
            {
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(x => x.BlockTableRecord.IsValid && x.GetEffectiveName() is THESAURUSCONFRONTATION or PARATHYROIDECTOMY))
                {
                    var c = EntityTool.GetCircles(br).Where(x => x.Visible).Select(x => x.ToGCircle()).Where(x => x.Radius > THESAURUSINCOMPLETE).FirstOrDefault();
                    if (c.IsValid)
                    {
                        geoData.FloorDrains.Add(c.ToCirclePolygon(SUPERLATIVENESS).ToGRect());
                        geoData.FloorDrainRings.Add(c);
                    }
                }
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
                        if (dxfName is DISORGANIZATION && GetEffectiveLayer(entity.Layer) is INSTRUMENTALITY)
                        {
                            dynamic o = entity;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                            lst.Add(seg);
                        }
                    }
                    geoData.WLines.AddRange(TempGeoFac.GetMinConnSegs(lst.Where(x => x.IsValid).Distinct().ToList()));
                }
            }
            foreach (var group in adb.Groups)
            {
                var lst = new List<Geometry>();
                foreach (var id in group.GetAllEntityIds())
                {
                    var entity = adb.Element<Entity>(id);
                    var dxfName = entity.GetRXClass().DxfName.ToUpper();
                    if (entity.Layer is INSTRUMENTALITY)
                    {
                        if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
                        {
                            var seg = line.ToGLineSegment();
                            if (seg.IsValid) lst.Add(seg.ToLineString());
                            continue;
                        }
                        else if (entity is Polyline pl)
                        {
                            foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                var seg = ln.ToGLineSegment();
                                if (seg.IsValid) lst.Add(seg.ToLineString());
                            }
                            continue;
                        }
                    }
                    if (dxfName is DISORGANIZATION && GetEffectiveLayer(entity.Layer) is INSTRUMENTALITY)
                    {
                        dynamic o = entity;
                        var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                        if (seg.IsValid) lst.Add(seg.ToLineString());
                        continue;
                    }
                    if (!entity.Visible || !isRainLayer(entity.Layer)) continue;
                    var bd = entity.Bounds.ToGRect();
                    if (bd.IsValid)
                    {
                        lst.Add(bd.ToPolygon());
                        continue;
                    }
                    else
                    {
                        var ext = new Extents3d();
                        try
                        {
                            foreach (var e in entity.ExplodeToDBObjectCollection().OfType<Entity>().Where(x => x.Visible))
                            {
                                if (e.Bounds.HasValue)
                                {
                                    var v = e.Bounds.Value;
                                    var r = v.ToGRect();
                                    if (r.IsValid)
                                    {
                                        ext.AddExtents(v);
                                    }
                                }
                            }
                        }
                        catch { }
                        bd = ext.ToGRect();
                        if (bd.IsValid && bd.Width < ALSOMONOSIPHONIC && bd.Height < ALSOMONOSIPHONIC)
                        {
                            lst.Add(bd.ToPolygon());
                            continue;
                        }
                    }
                }
                if (lst.Count > THESAURUSSTAMPEDE) geoData.Groups.Add(lst);
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
                        isInXref = INTRAVASCULARLY;
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
        const int distinguishDiameter = TELEPHOTOGRAPHIC;
        private static bool IsWantedBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return INTRAVASCULARLY;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return INTRAVASCULARLY;
            }
            if (!blockTableRecord.Explodable)
            {
                return INTRAVASCULARLY;
            }
            return THESAURUSOBSTINACY;
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
        static bool isRainLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSABJURE);
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
                        if (_dxfName is DISORGANIZATION && GetEffectiveLayer(e.Layer) is INSTRUMENTALITY)
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
                if (entityLayer is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
                {
                    if (entity is Line line)
                    {
                        if (line.Length > THESAURUSSTAMPEDE)
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
                            if (ln.Length > THESAURUSSTAMPEDE)
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
                if (entityLayer is THESAURUSINVOICE)
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
                if (entityLayer is INSTRUMENTALITY)
                {
                    if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                    if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                        if (distinguishDiameter <= c.Radius && c.Radius <= HYPERDISYLLABLE)
                        {
                            if (isRainLayer(c.Layer))
                            {
                                var r = c.Bounds.ToGRect().TransformBy(matrix);
                                reg(fs, r, pipes);
                                return;
                            }
                        }
                        else if (c.Layer is DENDROCHRONOLOGIST && THESAURUSINCOMPLETE < c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                    else if (dxfName is DISORGANIZATION && entityLayer is DENDROCHRONOLOGIST)
                    {
                        var lines = entity.ExplodeToDBObjectCollection().OfType<Line>().Where(x => x.Length > HYPERDISYLLABLE).ToList();
                        if (lines.Count > THESAURUSSTAMPEDE)
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
            if (dxfName == THESAURUSWINDFALL)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSSPECIFICATION + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>().Where(IsLayerVisible))
                {
                    if (e is Line line)
                    {
                        if (line.Length > THESAURUSSTAMPEDE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, labelLines);
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSDURESS)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
                        continue;
                    }
                }
                if (ts.Count > THESAURUSSTAMPEDE)
                {
                    GRect bd;
                    if (ts.Count == THESAURUSHOUSING) bd = ts[THESAURUSSTAMPEDE].Bounds.ToGRect();
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
            else if (dxfName == DISORGANIZATION)
            {
                if (entityLayer is DENDROCHRONOLOGIST)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                    {
                        if (c.Radius >= distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, pipes);
                        }
                        else if (THESAURUSINCOMPLETE <= c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                }
                if (entityLayer is INSTRUMENTALITY)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wLines);
                    return;
                }
            }
            else if (dxfName == QUOTATIONSWALLOW && isRainLayer(entityLayer))
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                reg(fs, r, rainPortSymbols);
            }
            else if (dxfName is THESAURUSDURESS)
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
            else if (dxfName is THESAURUSFACILITATE)
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
            else if (dxfName == THESAURUSINHARMONIOUS)
            {
                if (entityLayer is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
                {
                    dynamic o = entity.AcadObject;
                    string UpText = o.UpText;
                    string DownText = o.DownText;
                    var ents = entity.ExplodeToDBObjectCollection();
                    var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                    var points = GeoFac.GetAlivePoints(segs, THESAURUSHOUSING);
                    var pts = points.Select(x => x.ToNTSPoint()).ToList();
                    points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(THESAURUSHOUSING)).Select(x => x.Extend(THESAURUSPERMUTATION).Buffer(THESAURUSHOUSING)).ToList())).Select(pts).ToList(points)).ToList();
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
                    var t = (UpText + DownText) ?? THESAURUSDEPLORE;
                    if (t.Contains(THESAURUSELIGIBLE) && t.Contains(QUOTATIONMALTESE))
                    {
                        var ents = entity.ExplodeToDBObjectCollection();
                        var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList();
                        var points = GeoFac.GetAlivePoints(segs, THESAURUSHOUSING);
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(THESAURUSHOUSING)).Select(x => x.Extend(THESAURUSPERMUTATION).Buffer(THESAURUSHOUSING)).ToList())).Select(pts).ToList(points)).ToList();
                        if (points.Count > THESAURUSSTAMPEDE)
                        {
                            var r = new MultiPoint(points.Select(p => p.ToNTSPoint()).ToArray()).Envelope.ToGRect().Expand(HYPERDISYLLABLE);
                            geoData.Ditches.Add(r);
                        }
                        return;
                    }
                    if (t.Contains(THESAURUSELIGIBLE) && t.Contains(THESAURUSADVENT))
                    {
                        var ents = entity.ExplodeToDBObjectCollection();
                        var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList();
                        var points = GeoFac.GetAlivePoints(segs, THESAURUSHOUSING);
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(THESAURUSHOUSING)).Select(x => x.Extend(THESAURUSPERMUTATION).Buffer(THESAURUSHOUSING)).ToList())).Select(pts).ToList(points)).ToList();
                        if (points.Count > THESAURUSSTAMPEDE)
                        {
                            foreach (var pt in points)
                            {
                                geoData.RainPortSymbols.Add(GRect.Create(pt, THESAURUSENTREPRENEUR));
                            }
                        }
                        return;
                    }
                }
                {
                    if (!isRainLayer(entityLayer)) return;
                    var colle = entity.ExplodeToDBObjectCollection();
                    {
                        foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSDURESS or THESAURUSFACILITATE).Where(x => isRainLayer(x.Layer)).Where(IsLayerVisible))
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
                        foreach (var seg in colle.OfType<Line>().Where(x => x.Length > THESAURUSSTAMPEDE).Where(x => isRainLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
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
            str ??= THESAURUSDEPLORE;
            var i = str.LastIndexOf(THESAURUSCONTEND);
            if (i >= THESAURUSSTAMPEDE && !str.EndsWith(MULTIPROCESSING))
            {
                str = str.Substring(i + THESAURUSHOUSING);
            }
            i = str.LastIndexOf(SUPERREGENERATIVE);
            if (i >= THESAURUSSTAMPEDE && !str.EndsWith(THESAURUSCOURIER))
            {
                str = str.Substring(i + THESAURUSHOUSING);
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
                if (name is THESAURUSSPECIFIC or THESAURUSOSSIFY || VerticalAiringMachineNames.Contains(name))
                {
                    reg(fs, br.Bounds.ToGRect().TransformBy(matrix), geoData.AiringMachine_Vertical);
                    return;
                }
                if (name is THESAURUSDISAGREEMENT || HangingAiringMachineNames.Contains(name))
                {
                    reg(fs, br.Bounds.ToGRect().TransformBy(matrix), geoData.AiringMachine_Hanging);
                    return;
                }
                if (name.Contains(THESAURUSINTRICATE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    waterSealingWells.Add(bd);
                    return;
                }
                if (name.Contains(THESAURUSCEILING) || name.Contains(PRETERNATURALLY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSSPECIFICATION) ?? THESAURUSDEPLORE;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(lb);
                    });
                    return;
                }
                if (name.ToUpper() is THESAURUSAMATEUR || name.ToUpper().EndsWith(THESAURUSREQUISITION) || name.Contains(THESAURUSTOPICAL))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.ToUpper() is THESAURUSINEXPRESSIBLE or THESAURUSWONTED || name.ToUpper().EndsWith(THESAURUSTAUTOLOGY) || name.ToUpper().EndsWith(THESAURUSCONSEQUENCE) || name.Contains(THESAURUSOVERLY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is THESAURUSBATTER)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is THESAURUSPROMONTORY)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.Contains(THESAURUSINTERMENT))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, _87WaterBuckets);
                    return;
                }
                if (name.Contains(BROKENHEARTEDNESS) && name.Contains(THESAURUSLECHER))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.Contains(QUOTATIONSPENSERIAN) && name.Contains(THESAURUSLECHER))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, _87WaterBuckets);
                    return;
                }
                if (name.Contains(THESAURUSPROLONG) && name.Contains(THESAURUSLECHER))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (!isInXref)
                {
                    if ((name.Contains(THESAURUSINDULGENT) || name is INCORRESPONDENCE) && _name.Count(x => x == SUPERREGENERATIVE) < THESAURUSPERMUTATION)
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, floorDrains);
                        if (Regex.IsMatch(name, QUOTATIONCREEPING, RegexOptions.Compiled))
                        {
                            if (br.IsDynamicBlock)
                            {
                                var props = br.DynamicBlockReferencePropertyCollection;
                                foreach (DynamicBlockReferenceProperty prop in props)
                                {
                                    if (prop.PropertyName == THESAURUSENTERPRISE)
                                    {
                                        var propValue = prop.Value.ToString();
                                        {
                                            if (propValue is PHARYNGEALIZATION)
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
                if (name.Contains(THESAURUSTHOROUGHBRED))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < POLYOXYMETHYLENE && bd.Height < POLYOXYMETHYLENE)
                        {
                            reg(fs, bd, wrappingPipes);
                        }
                    }
                    return;
                }
                if (isRainLayer(br.Layer))
                {
                    if (name is SUPERINDUCEMENT or THESAURUSURBANITY || name.Contains(DISCOMPOSEDNESS) || name.Contains(THESAURUSADULTERATE) || name.Contains(INTERROGATORIUS))
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), THESAURUSENTREPRENEUR);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (_name.Contains(THESAURUSSPECIMEN))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (_name.EndsWith(THESAURUSMANIKIN) || _name.EndsWith(THESAURUSPITILESS))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                }
                {
                    if (_name.EndsWith(THESAURUSEMPHASIS))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, rainPortSymbols);
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
                if (name.Contains(THESAURUSCEILING) || name.Contains(THESAURUSLANDMARK) || name.Contains(PRETERNATURALLY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(br.GetAttributesStrValue(THESAURUSSPECIFICATION) ?? THESAURUSDEPLORE);
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
                if (name is THESAURUSBATTER)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        sideWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name is THESAURUSPROMONTORY)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        gravityWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name.Contains(THESAURUSINTERMENT))
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
                if (name.Contains(THESAURUSINDULGENT) || name is INCORRESPONDENCE)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        floorDrains.Add(bd);
                    }
                    return;
                }
                if (name.Contains(THESAURUSTHOROUGHBRED))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < POLYOXYMETHYLENE && bd.Height < POLYOXYMETHYLENE)
                        {
                            wrappingPipes.Add(bd);
                        }
                    }
                    return;
                }
                {
                    if (name is SUPERINDUCEMENT or THESAURUSURBANITY)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), THESAURUSENTREPRENEUR);
                        pipes.Add(bd);
                        return;
                    }
                    if (name.Contains(THESAURUSSPECIMEN))
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
                    handleBlockReference(b, br.BlockTransform.PreMultiplyBy(matrix));
                }
                else
                {
                    handleEntity(dbObj, br.BlockTransform.PreMultiplyBy(matrix));
                }
            }
        }
        const int distinguishDiameter = TELEPHOTOGRAPHIC;
        static bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return INTRAVASCULARLY;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return INTRAVASCULARLY;
            }
            if (!blockTableRecord.Explodable)
            {
                return INTRAVASCULARLY;
            }
            return THESAURUSOBSTINACY;
        }
        void handleEntity(Entity entity, Matrix3d matrix)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            {
                if (entity.Layer is INSTRUMENTALITY)
                {
                    if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                else if (entity.Layer is THESAURUSCONTROVERSY)
                {
                    if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                static bool isRainLayer(string layer) => layer is CIRCUMCONVOLUTION or DENDROCHRONOLOGIST or THESAURUSINVOICE;
                if (isRainLayer(entity.Layer))
                {
                    if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                        if (distinguishDiameter <= c.Radius && c.Radius <= HYPERDISYLLABLE)
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
                        else if (c.Layer is DENDROCHRONOLOGIST && THESAURUSINCOMPLETE < c.Radius && c.Radius < distinguishDiameter)
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
            if (dxfName == THESAURUSWINDFALL)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSSPECIFICATION + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>())
                {
                    if (e is Line line)
                    {
                        if (line.Length > THESAURUSSTAMPEDE)
                        {
                            labelLines.Add(line.ToGLineSegment().TransformBy(matrix));
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSDURESS)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>());
                        continue;
                    }
                }
                if (ts.Count > THESAURUSSTAMPEDE)
                {
                    GRect bd;
                    if (ts.Count == THESAURUSHOUSING) bd = ts[THESAURUSSTAMPEDE].Bounds.ToGRect();
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
            if (dxfName == DISORGANIZATION)
            {
                if (entity.Layer is DENDROCHRONOLOGIST)
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
                else if (entity.Layer is INSTRUMENTALITY)
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
            if (dxfName == QUOTATIONSWALLOW)
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                if (r.IsValid)
                {
                    rainPortSymbols.Add(r);
                }
                return;
            }
            if (dxfName == THESAURUSDURESS)
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
            if (dxfName == THESAURUSINHARMONIOUS)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var ee in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSDURESS or THESAURUSFACILITATE))
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
                    labelLines.AddRange(colle.OfType<Line>().Where(x => x.Length > THESAURUSSTAMPEDE).Select(x => x.ToGLineSegment().TransformBy(matrix)));
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
    public partial class RainDiagram
    {
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
        }
        public static string GetLabelScore(string label)
        {
            if (label == null) return null;
            if (IsPL(label))
            {
                return THESAURUSHYSTERICAL + label;
            }
            if (IsFL(label))
            {
                return CHROMATOGRAPHER + label;
            }
            return label;
        }
        public static int GetStoreyScore(string label)
        {
            if (label == null) return THESAURUSSTAMPEDE;
            switch (label)
            {
                case THESAURUSARGUMENTATIVE: return ushort.MaxValue;
                case ANTHROPOMORPHICALLY: return ushort.MaxValue + THESAURUSHOUSING;
                case THESAURUSSCUFFLE: return ushort.MaxValue + THESAURUSPERMUTATION;
                default:
                    {
                        int.TryParse(label.Replace(THESAURUSASPIRATION, THESAURUSDEPLORE), out int ret);
                        return ret;
                    }
            }
        }
        public static (ExtraInfo, bool) CollectRainData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out List<RainDrawingData> drDatas)
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
            geoData.OWLines.AddRange(geoData.WLines);
            ThRainService.PreFixGeoData(geoData);
            var (_drDatas, exInfo) = _CreateRainDrawingData(adb, geoData, THESAURUSOBSTINACY);
            drDatas = _drDatas;
            return (exInfo, THESAURUSOBSTINACY);
        }
        static void _ConnectLabelWithLongText(List<CText> cts, List<GLineSegment> labellines)
        {
            var pls = cts.Where(x => x.Text.Contains(THESAURUSLECHER)).Select(x => x.Boundary.ToPolygon().Tag(x));
            var lines = labellines.Select(x => x.ToLineString()).ToList();
            var linest = GeoFac.CreateIntersectsTester(lines);
            var linesf = GeoFac.CreateIntersectsSelector(lines);
            var hs = new HashSet<GLineSegment>();
            foreach (var pl in pls)
            {
                if (!linest(pl))
                {
                    var p = pl.GetCenter();
                    var seg = new GLineSegment(p, p.OffsetY(-QUOTATIONWITTIG));
                    var lns = GeoFac.GetManyLines(linesf(seg.ToLineString())).Where(x => x.IsValid && x.IsHorizontal(THESAURUSCOMMUNICATION)).Select(x => x.ToLineString()).ToList();
                    if (lns.Count > THESAURUSSTAMPEDE)
                    {
                        var hl = GeoFac.GetLines(GeoFac.NearestNeighbourGeometryF(lns)(p.ToNTSPoint())).First();
                        hs.Add(new GLineSegment(p, hl.Center).Extend(ASSOCIATIONISTS));
                    }
                }
            }
            labellines.AddRange(hs.Except(labellines).ToList());
        }
        public static (List<RainDrawingData>, ExtraInfo) CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            geoData.OWLines.AddRange(geoData.WLines);
            ThRainService.PreFixGeoData(geoData);
            ThRainService.ConnectLabelToLabelLine(geoData);
            _ConnectLabelWithLongText(geoData.Labels, geoData.LabelLines);
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
            _ConnectLabelWithLongText(geoData.Labels, geoData.LabelLines);
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
        public static List<StoreyItem> GetStoreys(AcadDatabase adb, CommandContext ctx)
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
        public static List<StoreyItem> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var geo = range?.ToGRect().ToPolygon();
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY).ToList();
            FixStoreys(storeys);
            return storeys;
        }
        public static void CollectRainGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeys, out RainGeoData geoData)
        {
            storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(range, adb, geoData);
        }
        public static List<StoreysItem> GetStoreysItem(List<StoreyItem> thStoreys)
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
                            item.Labels = new List<string>() { THESAURUSARGUMENTATIVE };
                        }
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                    case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                        {
                            item.Ints = s.Numbers.OrderBy(x => x).ToList();
                            item.Labels = item.Ints.Select(x => x + THESAURUSASPIRATION).ToList();
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
        public static bool IsGravityWaterBucketDNText(string text)
        {
            return re.IsMatch(text);
        }
        static readonly Regex re = new Regex(THESAURUSNATIONWIDE);
        public static IEnumerable<KeyValuePair<string, string>> EnumerateSmallRoofVerticalPipes(List<ThwSDStoreyItem> wsdStoreys, string vpipe)
        {
            foreach (var s in wsdStoreys)
            {
                if (s.Storey == THESAURUSSCUFFLE || s.Storey == ANTHROPOMORPHICALLY)
                {
                    if (s.VerticalPipes.Contains(vpipe))
                    {
                        yield return new KeyValuePair<string, string>(s.Storey, vpipe);
                    }
                }
            }
        }
        public static List<ThwSDStoreyItem> CollectStoreys(List<StoreyItem> thStoreys, List<RainDrawingData> drDatas)
        {
            var wsdStoreys = new List<ThwSDStoreyItem>();
            HashSet<string> GetVerticalPipeNotes(StoreyItem storey)
            {
                var i = thStoreys.IndexOf(storey);
                if (i < THESAURUSSTAMPEDE) return new HashSet<string>();
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
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSARGUMENTATIVE, Boundary = bd, VerticalPipes = vps });
                                largeRoofVPTexts.AddRange(vps);
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Numbers.ForEach(i => wsdStoreys.Add(new ThwSDStoreyItem() { Storey = i + THESAURUSASPIRATION, Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = thStoreys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == THESAURUSHOUSING)
                    {
                        var storey = storeys[THESAURUSSTAMPEDE];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        {
                            var rf2vps = smallRoofVPTexts.Except(largeRoofVPTexts).ToList();
                            if (rf2vps.Count == THESAURUSSTAMPEDE)
                            {
                                var rf1Storey = new ThwSDStoreyItem() { Storey = ANTHROPOMORPHICALLY, Boundary = bd, };
                                wsdStoreys.Add(rf1Storey);
                            }
                            else
                            {
                                var rf1vps = smallRoofVPTexts.Except(rf2vps).ToList();
                                var rf1Storey = new ThwSDStoreyItem() { Storey = ANTHROPOMORPHICALLY, Boundary = bd, VerticalPipes = rf1vps };
                                wsdStoreys.Add(rf1Storey);
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSSCUFFLE, Boundary = bd, VerticalPipes = rf2vps });
                            }
                        }
                    }
                    else if (storeys.Count == THESAURUSPERMUTATION)
                    {
                        var s1 = storeys[THESAURUSSTAMPEDE];
                        var s2 = storeys[THESAURUSHOUSING];
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
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = ANTHROPOMORPHICALLY, Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSSCUFFLE, Boundary = bd2, VerticalPipes = vps2 });
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
        public class WaterBucketInfo
        {
            public string WaterBucket;
            public string Pipe;
            public string Storey;
        }
        public static bool ShowWaterBucketHitting;
        public static List<RainGroupedPipeItem> GetRainGroupedPipeItems(List<RainDrawingData> drDatas, List<StoreyItem> thStoreys, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo)
        {
            otherInfo = new OtherInfo()
            {
                AloneFloorDrainInfos = new List<AloneFloorDrainInfo>(),
            };
            var thwSDStoreys = RainDiagram.CollectStoreys(thStoreys, drDatas);
            var storeysItems = new List<StoreysItem>(drDatas.Count);
            for (int i = THESAURUSSTAMPEDE; i < drDatas.Count; i++)
            {
                var bd = drDatas[i].Boundary;
                var item = new StoreysItem();
                item.Init();
                foreach (var sd in thwSDStoreys)
                {
                    if (sd.Boundary.EqualsTo(bd, THESAURUSACRIMONIOUS))
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
            var minS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Where(x => x > THESAURUSSTAMPEDE).Min();
            var maxS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Where(x => x > THESAURUSSTAMPEDE).Max();
            var countS = maxS - minS + THESAURUSHOUSING;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            var allNumStoreyLabels = allNumStoreys.Select(x => x + THESAURUSASPIRATION).ToList();
            allRfStoreys = storeys.Where(x => !IsNumStorey(x)).OrderBy(GetStoreyScore).ToList();
            bool existStorey(string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return THESAURUSOBSTINACY;
                }
                return INTRAVASCULARLY;
            }
            int getStoreyIndex(string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return i;
                }
                return -THESAURUSHOUSING;
            }
            var waterBucketsInfos = new List<WaterBucketInfo>();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).OrderBy(GetStoreyScore).ToList();
            string getLowerStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i >= THESAURUSHOUSING) return allStoreys[i - THESAURUSHOUSING];
                return null;
            }
            string getHigherStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i < THESAURUSSTAMPEDE) return null;
                if (i + THESAURUSHOUSING < allStoreys.Count) return allStoreys[i + THESAURUSHOUSING];
                return null;
            }
            {
                var toCmp = new List<int>();
                for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count - THESAURUSHOUSING; i++)
                {
                    var s1 = allStoreys[i];
                    var s2 = allStoreys[i + THESAURUSHOUSING];
                    if ((GetStoreyScore(s2) - GetStoreyScore(s1) == THESAURUSHOUSING) || (GetStoreyScore(s1) == maxS && GetStoreyScore(s2) == GetStoreyScore(THESAURUSARGUMENTATIVE)))
                    {
                        toCmp.Add(i);
                    }
                }
                foreach (var j in toCmp)
                {
                    var storey = allStoreys[j];
                    var i = getStoreyIndex(storey);
                    if (i < THESAURUSSTAMPEDE) continue;
                    var _drData = drDatas[i];
                    var item = storeysItems[i];
                    var higherStorey = getHigherStorey(storey);
                    if (higherStorey == null) continue;
                    var i1 = getStoreyIndex(higherStorey);
                    if (i1 < THESAURUSSTAMPEDE) continue;
                    var drData = drDatas[i1];
                    var v = drData.ContraPoint - _drData.ContraPoint;
                    var bkExpand = THESAURUSDOMESTIC;
                    var gbks = drData.GravityWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var sbks = drData.SideWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var _87bks = drData._87WaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    if (ShowWaterBucketHitting)
                    {
                        foreach (var bk in gbks.Concat(sbks).Concat(_87bks))
                        {
                            DrawRectLazy(bk);
                            Dr.DrawSimpleLabel(bk.LeftTop, THESAURUSCONSUMPTION);
                        }
                    }
                    var gbkgeos = gbks.Select(x => x.ToPolygon()).ToList();
                    var sbkgeos = sbks.Select(x => x.ToPolygon()).ToList();
                    var _87bkgeos = _87bks.Select(x => x.ToPolygon()).ToList();
                    var gbksf = GeoFac.CreateIntersectsSelector(gbkgeos);
                    var sbksf = GeoFac.CreateIntersectsSelector(sbkgeos);
                    var _87bksf = GeoFac.CreateIntersectsSelector(_87bkgeos);
                    for (int k = THESAURUSSTAMPEDE; k < _drData.Y1LVerticalPipeRectLabels.Count; k++)
                    {
                        var label = _drData.Y1LVerticalPipeRectLabels[k];
                        var vp = _drData.Y1LVerticalPipeRects[k];
                        {
                            var _gbks = gbksf(vp.ToPolygon());
                            if (_gbks.Count > THESAURUSSTAMPEDE)
                            {
                                var bk = drData.GravityWaterBucketLabels[gbkgeos.IndexOf(_gbks[THESAURUSSTAMPEDE])];
                                bk ??= ARKHIPRESBUTEROS;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _sbks = sbksf(vp.ToPolygon());
                            if (_sbks.Count > THESAURUSSTAMPEDE)
                            {
                                var bk = drData.SideWaterBucketLabels[sbkgeos.IndexOf(_sbks[THESAURUSSTAMPEDE])];
                                bk ??= THESAURUSFLOURISH;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _8bks = _87bksf(vp.ToPolygon());
                            if (_8bks.Count > THESAURUSSTAMPEDE)
                            {
                                var bk = drData._87WaterBucketLabels[_87bkgeos.IndexOf(_8bks[THESAURUSSTAMPEDE])];
                                bk ??= THESAURUSCELEBRATION;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                    }
                }
            }
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Count == THESAURUSHOUSING)
                    {
                        var storey = storeysItems[i].Labels[THESAURUSSTAMPEDE];
                        var _s = getHigherStorey(storey);
                        if (_s != null)
                        {
                            var drData = drDatas[i];
                            for (int i1 = THESAURUSSTAMPEDE; i1 < drData.RoofWaterBuckets.Count; i1++)
                            {
                                var kv = drData.RoofWaterBuckets[i1];
                                if (kv.Value == THESAURUSSECRETE)
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
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool hasLong(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.LongTranslatorLabels.Contains(label))
                            {
                                var tmp = storeysItems[i].Labels.Where(IsNumStorey).ToList();
                                if (tmp.Count > THESAURUSHOUSING)
                                {
                                    var floor = tmp.Select(GetStoreyScore).Max() + THESAURUSASPIRATION;
                                    if (storey != floor) return INTRAVASCULARLY;
                                }
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                {
                    var _storey = getHigherStorey(storey);
                    if (_storey != null)
                    {
                        for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == _storey)
                                {
                                    var drData = drDatas[i];
                                    if (drData.ConnectedToGravityWaterBucket.Contains(label))
                                    {
                                        return THESAURUSOBSTINACY;
                                    }
                                    if (drData.ConnectedToSideWaterBucket.Contains(label))
                                    {
                                        return THESAURUSOBSTINACY;
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
                                    return THESAURUSOBSTINACY;
                                }
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool hasShort(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                                    if (tmp.Count > THESAURUSHOUSING)
                                    {
                                        var floor = tmp.Select(GetStoreyScore).Max() + THESAURUSASPIRATION;
                                        if (storey != floor) return INTRAVASCULARLY;
                                    }
                                }
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            List<AloneFloorDrainInfo> getAloneFloorDrainInfos()
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSREGION)
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
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSideFloorDrain.Contains(label)) return THESAURUSOBSTINACY;
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool getHasNonSideFloorDrain(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.HasNonSideFloorDrain.Contains(label)) return THESAURUSOBSTINACY;
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool getHasSideFloorDrainAndNonSideFloorDrain(string label, string storey)
            {
                return getHasSideFloorDrain(label, storey) && getHasNonSideFloorDrain(label, storey);
            }
            bool getIsDitch(string label)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasDitch.Contains(label)) return THESAURUSOBSTINACY;
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            string getWaterWellLabel(string label)
            {
                if (getIsDitch(label)) return null;
                string _getWaterWellLabel(string label, string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                var ret = _getWaterWellLabel(label, THESAURUSREGION);
                ret ??= _getWaterWellLabel(label, THESAURUSTABLEAU);
                return ret;
            }
            int getWaterWellId(string label)
            {
                if (getIsDitch(label)) return -THESAURUSHOUSING;
                int _getWaterWellId(string label, string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return -THESAURUSHOUSING;
                }
                var ret = _getWaterWellId(label, THESAURUSREGION);
                if (ret == -THESAURUSHOUSING)
                {
                    ret = _getWaterWellId(label, THESAURUSTABLEAU);
                }
                return ret;
            }
            int getWaterSealingWellId(string label)
            {
                if (getIsDitch(label)) return -THESAURUSHOUSING;
                int _getWaterSealingWellId(string label, string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return -THESAURUSHOUSING;
                }
                var ret = _getWaterSealingWellId(label, THESAURUSREGION);
                if (ret == -THESAURUSHOUSING)
                {
                    ret = _getWaterSealingWellId(label, THESAURUSTABLEAU);
                }
                return ret;
            }
            int getRainPortId(string label)
            {
                if (getIsDitch(label)) return -THESAURUSHOUSING;
                int _getRainPortId(string label, string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return -THESAURUSHOUSING;
                }
                var ret = _getRainPortId(label, THESAURUSREGION);
                if (ret == -THESAURUSHOUSING)
                {
                    ret = _getRainPortId(label, THESAURUSTABLEAU);
                }
                return ret;
            }
            bool getHasRainPort(string label)
            {
                if (getIsDitch(label)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasRainPortSymbols.Contains(label)) return THESAURUSOBSTINACY;
                            if (getRainPortId(label) >= THESAURUSSTAMPEDE) return THESAURUSOBSTINACY;
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool getHasWaterSealingWell(string label)
            {
                if (getIsDitch(label)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasWaterSealingWell.Contains(label)) return THESAURUSOBSTINACY;
                            if (getWaterSealingWellId(label) >= THESAURUSSTAMPEDE) return THESAURUSOBSTINACY;
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool getHasWaterWell(string label)
            {
                if (getIsDitch(label)) return INTRAVASCULARLY;
                if (getHasRainPort(label) || getHasWaterSealingWell(label)) return INTRAVASCULARLY;
                return getWaterWellLabel(label) != null;
            }
            bool getIsSpreading(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool hasSingleFloorDrainDrainageForRainPort(string label)
            {
                if (IsY1L(label)) return INTRAVASCULARLY;
                var id = getRainPortId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForRainPort.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool hasSingleFloorDrainDrainageForWaterSealingWell(string label)
            {
                if (IsY1L(label)) return INTRAVASCULARLY;
                var id = getWaterSealingWellId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterSealingWell.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool hasSingleFloorDrainDrainageForWaterWell(string label)
            {
                if (IsY1L(label)) return INTRAVASCULARLY;
                var waterWellLabel = getWaterWellLabel(label);
                if (waterWellLabel == null) return INTRAVASCULARLY;
                var id = getWaterWellId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterWell.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForRainPort(string label)
            {
                if (!hasSingleFloorDrainDrainageForRainPort(label)) return INTRAVASCULARLY;
                var id = getRainPortId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForRainPort.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterWell(label)) return INTRAVASCULARLY;
                var id = getWaterWellId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterWell.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterSealingWell(label)) return INTRAVASCULARLY;
                var id = getWaterSealingWellId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            int getDitchId(string label)
            {
                int _getDitchId(string label, string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return -THESAURUSHOUSING;
                }
                var ret = _getDitchId(label, THESAURUSREGION);
                if (ret == -THESAURUSHOUSING)
                {
                    ret = _getDitchId(label, THESAURUSTABLEAU);
                }
                return ret;
            }
            bool hasSingleFloorDrainDrainageForDitch(string label)
            {
                if (IsY1L(label)) return INTRAVASCULARLY;
                var id = getDitchId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForDitch.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForDitch(string label)
            {
                if (!hasSingleFloorDrainDrainageForDitch(label)) return INTRAVASCULARLY;
                var id = getDitchId(label);
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForDitch.Contains(id))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            int getFDCount(string label, string storey)
            {
                if (IsY1L(label)) return THESAURUSSTAMPEDE;
                int _getFDCount()
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return THESAURUSSTAMPEDE;
                }
                var ret = _getFDCount();
                return ret;
            }
            int getFDWrappingPipeCount(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return THESAURUSSTAMPEDE;
            }
            bool hasCondensePipe(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool hasBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool hasNonBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool getPlsDrawCondensePipeHigher(string label, string storey)
            {
                if (hasBrokenCondensePipes(label, storey)) return INTRAVASCULARLY;
                if (!hasCondensePipe(label, storey)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool hasOutletlWrappingPipe(string label)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            if (drData.OutletWrappingPipeDict.ContainsValue(label)) return THESAURUSOBSTINACY;
                            if (drData.HasWrappingPipe.Contains(label)) return THESAURUSOBSTINACY;
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            string getOutletWrappingPipeRadius(string label)
            {
                if (!hasOutletlWrappingPipe(label)) return null;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSREGION or THESAURUSTABLEAU)
                        {
                            var drData = drDatas[i];
                            foreach (var kv in drData.WrappingPipeRadius220115)
                            {
                                if (kv.Key == label)
                                {
                                    return kv.Value;
                                }
                            }
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
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            string getWaterBucketLabel(string pipe, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.TouchGravityWaterBuckets.Contains(pipe)) return UNPICTURESQUENESS;
                            if (drData.TouchSideWaterBuckets.Contains(pipe)) return QUOTATIONPARISIAN;
                            if (drData.ConnectedToGravityWaterBucket.Contains(pipe)) return UNPICTURESQUENESS;
                            if (drData.ConnectedToSideWaterBucket.Contains(pipe)) return QUOTATIONPARISIAN;
                        }
                    }
                }
                foreach (var drData in drDatas)
                {
                    foreach (var kv in drData.RoofWaterBuckets)
                    {
                        if (kv.Value == storey && kv.Key == pipe)
                        {
                            return UNPICTURESQUENESS;
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
                        h.HasSideAndNonSideFloorDrain = getHasSideFloorDrainAndNonSideFloorDrain(label, h.Storey);
                    }
                }
            }
            var iRF = allStoreys.IndexOf(THESAURUSARGUMENTATIVE);
            var iRF1 = allStoreys.IndexOf(ANTHROPOMORPHICALLY);
            var iRF2 = allStoreys.IndexOf(THESAURUSSCUFFLE);
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
                        for (int i = item.Items.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; i--)
                        {
                            if (item.Items[i].Exist)
                            {
                                var _m = item.Items[i];
                                _m.Exist = INTRAVASCULARLY;
                                if (Equals(_m, default(RainGroupingPipeItem.ValueItem)))
                                {
                                    if (i < iRF - THESAURUSHOUSING && i > THESAURUSSTAMPEDE)
                                    {
                                        item.Items[i] = default;
                                    }
                                    if (i < iRF - THESAURUSHOUSING)
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
                    for (int i = item.Items.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; i--)
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
                                        item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(THESAURUSSTAMPEDE, j).Select(k => item.Items[k]).Any(x => x.Exist);
                                    }
                                }
                                item.Items[j] = default;
                            }
                            {
                                for (int j = THESAURUSSTAMPEDE; j < i; j++)
                                {
                                    item.Hangings[j].WaterBucket = null;
                                }
                                for (int j = i + THESAURUSHOUSING; j < item.Items.Count; j++)
                                {
                                    item.Items[j] = default;
                                }
                            }
                            break;
                        }
                    }
                    if (item.Items.Count > THESAURUSSTAMPEDE)
                    {
                        var lst = Enumerable.Range(THESAURUSSTAMPEDE, item.Items.Count).Where(i => item.Items[i].Exist).ToList();
                        if (lst.Count > THESAURUSSTAMPEDE)
                        {
                            var maxi = lst.Max();
                            var mini = lst.Min();
                            var hasWaterBucket = item.Hangings.Any(x => x.WaterBucket != null);
                            if (hasWaterBucket && (maxi == iRF || maxi == iRF - THESAURUSHOUSING))
                            {
                                item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(THESAURUSSTAMPEDE, iRF).Select(k => item.Items[k]).Any(x => x.Exist);
                            }
                            else
                            {
                                var (ok, m) = item.Items.TryGetValue(iRF);
                                item.HasLineAtBuildingFinishedSurfice = ok && m.Exist && iRF > THESAURUSSTAMPEDE && item.Items[iRF - THESAURUSHOUSING].Exist;
                            }
                        }
                    }
                    if (IsNL((item.Label)))
                    {
                        if (!item.Items.TryGet(iRF).Exist)
                            item.HasLineAtBuildingFinishedSurfice = INTRAVASCULARLY;
                    }
                    if (iRF >= THESAURUSPERMUTATION)
                    {
                        if (!item.Items[iRF].Exist && item.Items[iRF - THESAURUSHOUSING].Exist && item.Items[iRF - THESAURUSPERMUTATION].Exist)
                        {
                            var hanging = item.Hangings[iRF - THESAURUSHOUSING];
                            if (hanging.FloorDrainsCount > THESAURUSSTAMPEDE && !hanging.HasCondensePipe)
                            {
                                item.Items[iRF - THESAURUSHOUSING] = default;
                            }
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (iRF >= THESAURUSHOUSING && iRF1 > THESAURUSSTAMPEDE)
                    {
                        if (!item.Items[iRF1].Exist && item.Items[iRF].Exist && item.Items[iRF - THESAURUSHOUSING].Exist)
                        {
                            item.Items[iRF] = default;
                        }
                    }
                    if (item.PipeType == PipeType.Y1L)
                    {
                        if (item.Hangings.All(x => x.WaterBucket is null))
                        {
                            for (int i = item.Items.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; --i)
                            {
                                if (item.Items[i].Exist)
                                {
                                    var hanging = item.Hangings.TryGet(i + THESAURUSHOUSING);
                                    if (hanging != null)
                                    {
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
                            item.HasLineAtBuildingFinishedSurfice = THESAURUSOBSTINACY;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                    {
                        var hanging = item.Hangings[i];
                        if (hanging.Storey is THESAURUSREGION)
                        {
                            if (item.Items[i].HasShort)
                            {
                                var m = item.Items[i];
                                m.HasShort = INTRAVASCULARLY;
                                item.Items[i] = m;
                            }
                        }
                    }
                    for (int i = THESAURUSSTAMPEDE; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                        if (m.HasLong && m.HasShort)
                        {
                            m.HasShort = INTRAVASCULARLY;
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
                        item.OutletFloor = THESAURUSREGION;
                    }
                    else if (getHasRainPort(label))
                    {
                        item.OutletType = OutletType.RainPort;
                        item.OutletFloor = THESAURUSREGION;
                    }
                    else if (getHasWaterWell(label))
                    {
                        item.OutletType = OutletType.RainWell;
                        item.OutletFloor = THESAURUSREGION;
                    }
                    else
                    {
                        var ok = INTRAVASCULARLY;
                        if (testExist(label, THESAURUSREGION))
                        {
                            if (getIsSpreading(label, THESAURUSREGION))
                            {
                                item.OutletType = OutletType.Spreading;
                                item.OutletFloor = THESAURUSREGION;
                                ok = THESAURUSOBSTINACY;
                            }
                            else if (getIsDitch(label))
                            {
                                item.OutletType = OutletType.Ditch;
                                item.OutletFloor = THESAURUSREGION;
                                ok = THESAURUSOBSTINACY;
                            }
                        }
                        if (!ok)
                        {
                            item.OutletType = OutletType.Spreading;
                            for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                            {
                                if (item.Items[i].Exist)
                                {
                                    var hanging = item.Hangings[i];
                                    item.OutletFloor = hanging.Storey;
                                    break;
                                }
                            }
                            ok = THESAURUSOBSTINACY;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.OutletType == OutletType.Spreading && item.OutletFloor == THESAURUSARGUMENTATIVE)
                {
                    item.HasLineAtBuildingFinishedSurfice = INTRAVASCULARLY;
                }
                if (item.OutletType == OutletType.Spreading)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == item.OutletFloor)
                        {
                            h.HasCheckPoint = INTRAVASCULARLY;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (IsNL(label))
                    {
                        for (int i = THESAURUSSTAMPEDE; i < item.Items.Count; i++)
                        {
                            var m = item.Items[i];
                            if (m.HasShort)
                            {
                                m.HasShort = INTRAVASCULARLY;
                                item.Items[i] = m;
                            }
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                    {
                        var storey = allStoreys.TryGet(i);
                        if (storey == THESAURUSREGION)
                        {
                            var hanging = item.Hangings[i];
                            hanging.HasCheckPoint = THESAURUSOBSTINACY;
                            break;
                        }
                    }
                    for (int i = THESAURUSSTAMPEDE; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = THESAURUSSTAMPEDE; i < item.Items.Count; i++)
                {
                    var x = item.Items[i];
                    if (!x.Exist) item.Items[i] = default;
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.FloorDrainsCountAt1F == THESAURUSSTAMPEDE)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == THESAURUSREGION)
                        {
                            item.FloorDrainsCountAt1F = h.FloorDrainsCount;
                            h.FloorDrainsCount = THESAURUSSTAMPEDE;
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
                            h.HasCheckPoint = INTRAVASCULARLY;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = THESAURUSSTAMPEDE; i < item.Items.Count; i++)
                {
                    var m = item.Items[i];
                    if (m.HasLong)
                    {
                        var h = item.Hangings[i];
                        if (!h.HasCondensePipe && (item.Hangings.TryGet(i + THESAURUSHOUSING)?.FloorDrainsCount ?? THESAURUSSTAMPEDE) == THESAURUSSTAMPEDE && (item.Hangings.TryGet(i + THESAURUSHOUSING)?.WaterBucket != null))
                        {
                            h.LongTransHigher = THESAURUSOBSTINACY;
                        }
                    }
                    {
                        var h = item.Hangings[i];
                        if (h.FloorDrainsCount > THESAURUSSTAMPEDE)
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
                if (item.OutletFloor is THESAURUSREGION && item.OutletType != OutletType.Spreading)
                {
                    item.OutletWrappingPipeRadius ??= THESAURUSLEGACY;
                }
                if (item.OutletFloor is ANTHROPOMORPHICALLY or THESAURUSSCUFFLE && item.OutletType == OutletType.Spreading)
                {
                    item.HasLineAtBuildingFinishedSurfice = INTRAVASCULARLY;
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                item.FloorDrainsCountAt1F = THESAURUSSTAMPEDE;
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { COSTERMONGERING, DENDROCHRONOLOGIST, CIRCUMCONVOLUTION, THESAURUSJUBILEE, THESAURUSINVOICE, THESAURUSDEFAULTER, INSTRUMENTALITY, THESAURUSSTRIPED });
                var storeys = ThRainService.commandContext.StoreyContext.StoreyInfos;
                List<RainDrawingData> drDatas;
                var range = ThRainService.commandContext.range;
                List<StoreyItem> storeysItems;
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
                var allNumStoreyLabels = allNumStoreys.Select(i => i + THESAURUSASPIRATION).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - THESAURUSHOUSING;
                var end = THESAURUSSTAMPEDE;
                var OFFSET_X = QUOTATIONLETTERS;
                var SPAN_X = BALANOPHORACEAE;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSINCOMING;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSINCOMING;
                Dispose();
                DrawRainDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel, otherInfo, exInfo);
                FlushDQ(adb);
            }
        }
        public static void DrawRainDiagram()
        {
            FocusMainWindow();
            var range = TrySelectRangeEx();
            if (range == null) return;
            if (!TrySelectPoint(out Point3d point3D)) return;
            var basePoint = point3D.ToPoint2d();
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
            {
                List<StoreyItem> storeysItems;
                List<RainDrawingData> drDatas;
                var (exInfo, ok) = CollectRainData(range, adb, out storeysItems, out drDatas);
                if (!ok) return;
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo);
                Dispose();
                DrawRainDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys, otherInfo, RainSystemDiagramViewModel.Singleton, exInfo);
                FlushDQ(adb);
            }
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof, bool showText)
        {
            var name = showText ? (canPeopleBeOnRoof ? THESAURUSPARTNER : THESAURUSINEFFECTUAL) : HYPERVENTILATION;
            DrawAiringSymbol(pt, name);
        }
        public static void DrawAiringSymbol(Point2d pt, string name)
        {
            DrawBlockReference(blkName: THESAURUSNARCOTIC, basePt: pt.ToPoint3d(), layer: INSTRUMENTALITY, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(QUINQUAGENARIAN, name);
            });
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: THESAURUSCONFUSION, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: INSTRUMENTALITY, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(THESAURUSKILTER, offsetY);
                br.ObjectId.SetDynBlockValue(QUINQUAGENARIAN, QUOTATIONSPRENGEL);
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
            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(POLYOXYMETHYLENE);
            dim.DimensionText = THESAURUSDUBIETY;
            dim.Layer = THESAURUSINVOICE;
            ByLayer(dim);
            DrawEntityLazy(dim);
        }
        public static double CHECKPOINT_OFFSET_Y = THESAURUSNECESSITOUS;
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSSTRIPED;
                ByLayer(line);
            });
        }
        public static void DrawStoreyLine(string label, Point2d _basePt, double lineLen, string text)
        {
            var basePt = _basePt.ToPoint3d();
            {
                var line = DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
                Dr.SetLabelStylesForWNote(line, dbt);
                DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.OffsetX(QUOTATIONPITUITARY), layer: COSTERMONGERING, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, text } });
            }
            if (label == THESAURUSARGUMENTATIVE)
            {
                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, THESAURUSSTAMPEDE), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, THESAURUSSTAMPEDE));
                var dbt = DrawTextLazy(THESAURUSSHADOWY, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
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
            public ExtraInfo exInfo;
            List<Vector2d> vecs0 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONBASTARD - dy + _dy) };
            List<Vector2d> vecs1 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -h1), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy + _dy - h2) };
            List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -h1 - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy + __dy + _dy - h2) };
            List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -h1 + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy - __dy + _dy - h2) };
            List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -COOPERATIVENESS - dy + _dy) };
            List<Vector2d> vecs3 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -h1), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - dy + _dy - h2), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -h1 - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - h2 - dy + __dy + _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -h1 + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - h2 - dy - __dy + _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            List<Vector2d> vecs4 => vecs1.GetYAxisMirror();
            List<Vector2d> vecs5 => vecs2.GetYAxisMirror();
            List<Vector2d> vecs6 => vecs3.GetYAxisMirror();
            List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
            List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
            List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
            public void Run()
            {
                if (exInfo != null) exInfo.vm = viewModel;
                var db = adb.Database;
                static void DrawSegs(List<GLineSegment> segs) { for (int k = THESAURUSSTAMPEDE; k < segs.Count; k++) DrawTextLazy(k.ToString(), segs[k].StartPoint); }
                var storeyLines = new List<KeyValuePair<string, GLineSegment>>();
                void _DrawStoreyLine(string storey, Point2d p, double lineLen, string text)
                {
                    DrawStoreyLine(storey, p, lineLen, text);
                    storeyLines.Add(new KeyValuePair<string, GLineSegment>(storey, new GLineSegment(p, p.OffsetX(lineLen))));
                }
                var vec7 = new Vector2d(-THESAURUSQUAGMIRE, THESAURUSQUAGMIRE);
                {
                    var heights = new List<int>(allStoreys.Count);
                    var s = THESAURUSSTAMPEDE;
                    var _vm = FloorHeightsViewModel.Instance;
                    static bool test(string x, int t)
                    {
                        var m = Regex.Match(x, QUOTATIONSTYLOGRAPHIC);
                        if (m.Success)
                        {
                            if (int.TryParse(m.Groups[THESAURUSHOUSING].Value, out int v1) && int.TryParse(m.Groups[THESAURUSPERMUTATION].Value, out int v2))
                            {
                                var min = Math.Min(v1, v2);
                                var max = Math.Max(v1, v2);
                                for (int i = min; i <= max; i++)
                                {
                                    if (i == t) return THESAURUSOBSTINACY;
                                }
                            }
                            else
                            {
                                return INTRAVASCULARLY;
                            }
                        }
                        m = Regex.Match(x, TETRAIODOTHYRONINE);
                        if (m.Success)
                        {
                            if (int.TryParse(x, out int v))
                            {
                                if (v == t) return THESAURUSOBSTINACY;
                            }
                        }
                        return INTRAVASCULARLY;
                    }
                    for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                    {
                        heights.Add(s);
                        var v = _vm.GeneralFloor;
                        if (_vm.ExistsSpecialFloor) v = _vm.Items.FirstOrDefault(m => test(m.Floor, GetStoreyScore(allStoreys[i])))?.Height ?? v;
                        s += v;
                    }
                    var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                    for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        string getStoreyHeightText()
                        {
                            if (storey is THESAURUSREGION) return MULTINATIONALLY;
                            var ret = (heights[i] / LAUTENKLAVIZIMBEL).ToString(THESAURUSINFINITY); ;
                            if (ret == THESAURUSINFINITY) return MULTINATIONALLY;
                            return ret;
                        }
                        _DrawStoreyLine(storey, bsPt1, lineLen, getStoreyHeightText());
                        if (i == THESAURUSSTAMPEDE && otherInfo.AloneFloorDrainInfos.Count > THESAURUSSTAMPEDE)
                        {
                            var dome_lines = new List<GLineSegment>(THESAURUSREPERCUSSION);
                            var dome_layer = INSTRUMENTALITY;
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
                                    if (values.Count == THESAURUSHOUSING)
                                    {
                                        DrawRainWaterWell(pt.OffsetX(-THESAURUSDOMESTIC), values[THESAURUSSTAMPEDE]);
                                    }
                                    else if (values.Count >= THESAURUSPERMUTATION)
                                    {
                                        var pts = GetBasePoints(pt.OffsetX(-THESAURUSDISAGREEABLE - THESAURUSDOMESTIC), THESAURUSPERMUTATION, values.Count, THESAURUSDISAGREEABLE, THESAURUSDISAGREEABLE).ToList();
                                        for (int i = THESAURUSSTAMPEDE; i < values.Count; i++)
                                        {
                                            DrawRainWaterWell(pts[i], values[i]);
                                        }
                                    }
                                }
                                {
                                    var lst = otherInfo.AloneFloorDrainInfos.Where(x => !x.IsSideFloorDrain).Select(x => x.WaterWellLabel).Distinct().ToList();
                                    if (lst.Count > THESAURUSSTAMPEDE)
                                    {
                                        {
                                            var line = DrawLineLazy(pt.OffsetX(POLYOXYMETHYLENE), pt.OffsetX(-SPAN_X));
                                            Dr.SetLabelStylesForWNote(line);
                                            var p = pt.OffsetY(-QUOTATIONPITUITARY);
                                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE) };
                                            drawDomePipes(vecs.ToGLineSegments(p));
                                            _DrawFloorDrain((p + new Vector2d(THESAURUSCAVERN, THESAURUSINTRACTABLE)).ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                            DrawNoteText(THESAURUSDISREPUTABLE, p + new Vector2d(THESAURUSCAVERN + THESAURUSATTACHMENT, THESAURUSINTRACTABLE) + new Vector2d(-THESAURUSAPPLICANT, -INCOMMODIOUSNESS));
                                            {
                                                _DrawRainWaterWells(vecs.GetLastPoint(p), lst.OrderBy(x =>
                                                {
                                                    if (x == THESAURUSSPECIFICATION) return int.MaxValue;
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
                                    if (lst.Count > THESAURUSSTAMPEDE)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE) };
                                        drawDomePipes(vecs.ToGLineSegments(pt));
                                        _DrawFloorDrain((pt + new Vector2d(THESAURUSCAVERN, THESAURUSINTRACTABLE)).ToPoint3d(), THESAURUSOBSTINACY, PHARYNGEALIZATION);
                                        {
                                            _DrawRainWaterWells(vecs.GetLastPoint(pt), lst.OrderBy(x =>
                                            {
                                                if (x == THESAURUSSPECIFICATION) return int.MaxValue;
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
                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, string value)
                {
                    DrawFloorDrain(basePt, leftOrRight, value);
                }
                var gaps = storeyLines.Select(kv =>
                {
                    var geo = kv.Value.Buffer(THESAURUSACRIMONIOUS);
                    geo.UserData = kv.Key;
                    return geo;
                }).ToList();
                var gapsf = GeoFac.CreateIntersectsSelector(gaps);
                var storeySpaces = storeyLines.Select(kv =>
                {
                    var seg1 = kv.Value.Offset(THESAURUSSTAMPEDE, THESAURUSHOUSING);
                    var seg2 = kv.Value.Offset(THESAURUSSTAMPEDE, HEIGHT - THESAURUSHOUSING);
                    var geo = new GRect(seg1.StartPoint, seg2.EndPoint).ToPolygon();
                    geo.UserData = kv.Key;
                    return geo;
                }).ToList();
                var storeySpacesf = GeoFac.CreateIntersectsSelector(storeySpaces);
                for (int j = THESAURUSSTAMPEDE; j < COUNT; j++)
                {
                    pipeGroupItems.Add(new RainGroupedPipeItem());
                }
                var dx = THESAURUSSTAMPEDE;
                for (int j = THESAURUSSTAMPEDE; j < COUNT; j++)
                {
                    var dome_lines = new List<GLineSegment>(THESAURUSREPERCUSSION);
                    var dome_layer = INSTRUMENTALITY;
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid) dome_lines.Add(seg);
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs)
                    {
                        dome_lines.AddRange(segs.Where(x => x.IsValid));
                    }
                    var linesKillers = new HashSet<Geometry>();
                    var iRF = allStoreys.IndexOf(THESAURUSARGUMENTATIVE);
                    var gpItem = pipeGroupItems[j];
                    string getFDDN()
                    {
                        const string dft = QUOTATIONBREWSTER;
                        if (gpItem.PipeType is PipeType.NL)
                        {
                            return viewModel?.Params.CondenseFloorDrainDN ?? dft;
                        }
                        else if (gpItem.PipeType is PipeType.FL0)
                        {
                            return viewModel?.Params.WaterWellFloorDrainDN ?? dft;
                        }
                        else
                        {
                            return viewModel?.Params.BalconyFloorDrainDN ?? dft;
                        }
                    }
                    string getHDN()
                    {
                        const string dft = THESAURUSDISREPUTABLE;
                        if (gpItem.PipeType == PipeType.Y2L) return viewModel?.Params.BalconyFloorDrainDN ?? dft;
                        if (gpItem.PipeType == PipeType.NL) return viewModel?.Params.CondensePipeHorizontalDN ?? dft;
                        if (gpItem.PipeType == PipeType.FL0) return viewModel?.Params.WaterWellFloorDrainDN ?? dft;
                        return dft;
                    }
                    string getPipeDn()
                    {
                        const string dft = IRRESPONSIBLENESS;
                        var dn = gpItem.PipeType switch
                        {
                            PipeType.Y2L => viewModel?.Params.BalconyRainPipeDN,
                            PipeType.NL => viewModel?.Params.CondensePipeVerticalDN,
                            PipeType.FL0 => viewModel?.Params.WaterWellPipeVerticalDN,
                            _ => dft,
                        };
                        dn ??= dft;
                        return dn;
                    }
                    var shouldDrawAringSymbol = gpItem.PipeType != PipeType.Y1L && (gpItem.PipeType == PipeType.NL ? (viewModel?.Params?.HasAiringForCondensePipe ?? THESAURUSOBSTINACY) : THESAURUSOBSTINACY);
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
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
                            for (int i = runs.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasLongTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSOBSTINACY;
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
                            for (int i = runs.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasShortTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSOBSTINACY;
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
                        for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                        {
                            arr[i] = new PipeRunLocationInfo() { Visible = THESAURUSOBSTINACY, Storey = allStoreys[i], };
                        }
                        {
                            var tdx = UNDENOMINATIONAL;
                            for (int i = start; i >= end; i--)
                            {
                                h0 = SUBCATEGORIZING;
                                h1 = THESAURUSFORMULATE;
                                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                                var basePt = bsPt1.OffsetX(OFFSET_X + (j + THESAURUSHOUSING) * SPAN_X) + new Vector2d(tdx, THESAURUSSTAMPEDE);
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                var storey = allStoreys[i];
                                if (storey == THESAURUSARGUMENTATIVE)
                                {
                                    _dy = ThWSDStorey.RF_OFFSET_Y;
                                }
                                else
                                {
                                    _dy = QUOTATIONTRANSFERABLE;
                                }
                                PipeRunLocationInfo drawNormal()
                                {
                                    {
                                        var vecs = vecs0;
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                        arr[i].HangingEndPoint = arr[i].EndPoint;
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        segs[THESAURUSSTAMPEDE] = new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, segs[THESAURUSHOUSING].StartPoint);
                                        arr[i].RightSegsLast = segs;
                                    }
                                    {
                                        var pt = arr[i].Segs.First().StartPoint.OffsetX(THESAURUSHYPNOTIC);
                                        var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE))) };
                                        arr[i].RightSegsFirst = segs;
                                        segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                                    }
                                    return arr[i];
                                }
                                if (i == start)
                                {
                                    drawNormal().Visible = INTRAVASCULARLY;
                                    continue;
                                }
                                if (run == null)
                                {
                                    drawNormal().Visible = INTRAVASCULARLY;
                                    continue;
                                }
                                if (run.HasLongTranslator && gpItem.Hangings[i].LongTransHigher)
                                {
                                    h1 = HYPERDISYLLABLE;
                                }
                                else if (run.HasLongTranslator && !gpItem.Hangings[i].HasCondensePipe && (gpItem.Hangings.TryGet(i + THESAURUSHOUSING)?.FloorDrainsCount ?? THESAURUSSTAMPEDE) == THESAURUSSTAMPEDE)
                                {
                                    h1 = HYPERDISYLLABLE;
                                }
                                if (run.HasLongTranslator && run.HasShortTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs3;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(QUOTATIONEDIBLE);
                                            segs.Add(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[INTROPUNITIVENESS].EndPoint.OffsetXY(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC)));
                                            segs.Add(new GLineSegment(segs[THESAURUSPERMUTATION].EndPoint, new Point2d(segs[THESAURUSCOMMUNICATION].EndPoint.X, segs[THESAURUSPERMUTATION].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(INTROPUNITIVENESS);
                                            segs = new List<GLineSegment>() { segs[INTROPUNITIVENESS], new GLineSegment(segs[INTROPUNITIVENESS].StartPoint, segs[THESAURUSSTAMPEDE].StartPoint) };
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs6;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(-THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))).Offset(THESAURUSDICTATORIAL, THESAURUSSTAMPEDE));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(QUOTATIONEDIBLE);
                                            segs.Add(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[QUOTATIONEDIBLE].StartPoint));
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, arr[i].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    arr[i].HangingEndPoint = arr[i].Segs[QUOTATIONEDIBLE].EndPoint;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs1;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs = segs.Take(QUOTATIONEDIBLE).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[INTROPUNITIVENESS].EndPoint.OffsetXY(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC))).ToList();
                                            segs.Add(new GLineSegment(segs[THESAURUSPERMUTATION].EndPoint, new Point2d(segs[THESAURUSCOMMUNICATION].EndPoint.X, segs[THESAURUSPERMUTATION].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(INTROPUNITIVENESS);
                                            segs = new List<GLineSegment>() { segs[INTROPUNITIVENESS], new GLineSegment(segs[INTROPUNITIVENESS].StartPoint, segs[THESAURUSSTAMPEDE].StartPoint) };
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs4;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))).Offset(THESAURUSDICTATORIAL, THESAURUSSTAMPEDE));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle;
                                            arr[i].RightSegsLast = segs.Take(QUOTATIONEDIBLE).YieldAfter(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[THESAURUSCOMMUNICATION].StartPoint)).YieldAfter(segs[THESAURUSCOMMUNICATION]).ToList();
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
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
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, segs[THESAURUSPERMUTATION].StartPoint), segs[THESAURUSPERMUTATION] };
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[THESAURUSPERMUTATION].StartPoint, segs[THESAURUSPERMUTATION].EndPoint);
                                            segs[THESAURUSPERMUTATION] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(THESAURUSSTAMPEDE);
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, r.RightButtom));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs5;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(-THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, segs[THESAURUSPERMUTATION].StartPoint), segs[THESAURUSPERMUTATION] };
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[THESAURUSPERMUTATION].StartPoint, segs[THESAURUSPERMUTATION].EndPoint);
                                            segs[THESAURUSPERMUTATION] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(THESAURUSSTAMPEDE);
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, r.RightButtom));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    arr[i].HangingEndPoint = arr[i].Segs[THESAURUSSTAMPEDE].EndPoint;
                                }
                                else
                                {
                                    drawNormal();
                                }
                            }
                        }
                        for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                        {
                            var info = arr.TryGet(i);
                            if (info != null)
                            {
                                info.StartPoint = info.BasePoint.OffsetY(HEIGHT);
                            }
                        }
                        return arr;
                    }
                    var vkills = new List<Point3d>();
                    var vdrills = new List<GRect>();
                    var vsels = new List<Point3d>();
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] infos)
                    {
                        var couldHavePeopleOnRoof = viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSOBSTINACY;
                        var hasDrawedAiringSymbol = INTRAVASCULARLY;
                        void _DrawAiringSymbol(Point2d basePt)
                        {
                            if (!shouldDrawAringSymbol) return;
                            if (hasDrawedAiringSymbol) return;
                            var showText = THESAURUSOBSTINACY;
                            {
                                var info = infos.FirstOrDefault(x => x.Storey == THESAURUSARGUMENTATIVE);
                                if (info != null)
                                {
                                    var pt = info.BasePoint;
                                    if (basePt.Y < pt.Y)
                                    {
                                        showText = INTRAVASCULARLY;
                                    }
                                }
                            }
                            vsels.Add(basePt.ToPoint3d());
                            vkills.Add(basePt.OffsetY(ASSOCIATIONISTS).ToPoint3d());
                            DrawAiringSymbol(basePt, couldHavePeopleOnRoof, showText);
                            hasDrawedAiringSymbol = THESAURUSOBSTINACY;
                        }
                        {
                            if (shouldDrawAringSymbol)
                            {
                                if (gpItem.HasLineAtBuildingFinishedSurfice)
                                {
                                    var info = infos.FirstOrDefault(x => x.Storey == THESAURUSARGUMENTATIVE);
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
                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, height), new Vector2d(leftOrRight ? -THESAURUSEXECRABLE : THESAURUSEXECRABLE, THESAURUSSTAMPEDE) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForRainNote(lines.ToArray());
                            var t = DrawTextLazy(text, THESAURUSENDANGER, segs.Last().EndPoint.OffsetY(THESAURUSENTREPRENEUR));
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
                                    var height = THESAURUSENDANGER;
                                    var width = height * THESAURUSUPKEEP * text.Length;
                                    var yd = new YesDraw();
                                    yd.OffsetXY(THESAURUSSTAMPEDE, lineYOffset);
                                    yd.OffsetX(-width);
                                    var pts = yd.GetPoint3ds(basePt).ToList();
                                    var lines = DrawLinesLazy(pts);
                                    Dr.SetLabelStylesForRainNote(lines.ToArray());
                                    var t = DrawTextLazy(text, height, pts.Last().OffsetXY(THESAURUSENTREPRENEUR, THESAURUSENTREPRENEUR));
                                    Dr.SetLabelStylesForRainNote(t);
                                }
                                if (gpItem.OutletFloor == storey)
                                {
                                    var basePt = info.EndPoint;
                                    if (storey == THESAURUSARGUMENTATIVE) basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                    var seg = new GLineSegment(basePt, basePt.OffsetY(THESAURUSSURPRISED));
                                    var p = seg.EndPoint;
                                    DrawDimLabel(seg.StartPoint, p, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), THESAURUSILLUMINATION, THESAURUSINVOICE);
                                    DrawLabel(p.ToPoint3d(), THESAURUSEXECUTIVE + storey, storey == THESAURUSARGUMENTATIVE ? -THESAURUSFORMULATE - ThWSDStorey.RF_OFFSET_Y : -THESAURUSFORMULATE);
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var segs = info.DisplaySegs = info.Segs.ToList();
                                        segs[THESAURUSSTAMPEDE] = new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, segs[THESAURUSSTAMPEDE].StartPoint.OffsetY(-(segs[THESAURUSSTAMPEDE].Length - THESAURUSSURPRISED)));
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
                                        if (storey == THESAURUSARGUMENTATIVE)
                                        {
                                            basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        switch (bk.WaterBucketType)
                                        {
                                            case WaterBucketEnum.Gravity:
                                            case WaterBucketEnum._87:
                                                {
                                                    Dr.DrawGravityWaterBucket(basePt.ToPoint3d());
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, PHOSPHORYLATION), new Vector2d(-THESAURUSMISUNDERSTANDING, THESAURUSMISUNDERSTANDING), new Vector2d(-THESAURUSINTEND + THESAURUSFLAGSTONE, THESAURUSSTAMPEDE) };
                                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                                    Dr.SetLabelStylesForRainNote(DrawLineSegmentsLazy(segs).ToArray());
                                                    var pt1 = segs.Last().EndPoint;
                                                    var pt2 = pt1.OffsetXY(THESAURUSENTREPRENEUR, -THESAURUSDOMESTIC);
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.GetWaterBucketChName(), THESAURUSENDANGER, pt1));
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.DN, THESAURUSENDANGER, pt2));
                                                }
                                                break;
                                            case WaterBucketEnum.Side:
                                                {
                                                    var relativeYOffsetToStorey = -ALSOLATREUTICAL;
                                                    var pt = basePt.OffsetY(relativeYOffsetToStorey);
                                                    Dr.DrawSideWaterBucket(pt.ToPoint3d());
                                                    vsels.Add(pt.ToPoint3d());
                                                    vkills.Add(pt.OffsetY(ASSOCIATIONISTS).ToPoint3d());
                                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSMAGNETIC, THESAURUSSTRIPE), new Vector2d(-THESAURUSFOREGONE, THESAURUSFOREGONE), new Vector2d(-ADMINISTRATIVELY + THESAURUSFLAGSTONE, THESAURUSSTAMPEDE) };
                                                    var segs = vecs.ToGLineSegments(basePt.OffsetXY(DISSOCIABLENESS, -DISSOCIABLENESS)).Skip(THESAURUSHOUSING).ToList();
                                                    Dr.SetLabelStylesForRainNote(DrawLineSegmentsLazy(segs).ToArray());
                                                    var pt1 = segs.Last().EndPoint;
                                                    var pt2 = pt1.OffsetXY(THESAURUSENTREPRENEUR, -THESAURUSDOMESTIC);
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.GetWaterBucketChName(), THESAURUSENDANGER, pt1));
                                                    Dr.SetLabelStylesForRainNote(DrawTextLazy(bk.DN, THESAURUSENDANGER, pt2));
                                                }
                                                break;
                                            default:
                                                throw new System.Exception();
                                        }
                                    }
                                }
                                {
                                    if (storey == THESAURUSREGION)
                                    {
                                        var basePt = info.EndPoint;
                                        var text = gpItem.OutletWrappingPipeRadius;
                                        if (text != null)
                                        {
                                            var p1 = basePt + new Vector2d(-REPRESENTATIONAL, -THESAURUSBELLOW);
                                            var p2 = p1.OffsetY(-DOCTRINARIANISM);
                                            if (gpItem.HasOutletWrappingPipe && gpItem.HasSingleFloorDrainDrainageForWaterWell && gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                            {
                                                p2 = p2.OffsetY(-THESAURUSEUPHORIA);
                                            }
                                            var p3 = p2.OffsetX(QUOTATIONWITTIG);
                                            var layer = CIRCUMCONVOLUTION;
                                            DrawLine(layer, new GLineSegment(p1, p2));
                                            DrawLine(layer, new GLineSegment(p3, p2));
                                            DrawStoreyHeightSymbol(p3, CIRCUMCONVOLUTION, text);
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
                                                if (values.Count == THESAURUSHOUSING)
                                                {
                                                    DrawRainWaterWell(pt, values[THESAURUSSTAMPEDE]);
                                                }
                                                else if (values.Count >= THESAURUSPERMUTATION)
                                                {
                                                    var pts = GetBasePoints(pt.OffsetX(-THESAURUSDISAGREEABLE), THESAURUSPERMUTATION, values.Count, THESAURUSDISAGREEABLE, THESAURUSDISAGREEABLE).ToList();
                                                    for (int i = THESAURUSSTAMPEDE; i < values.Count; i++)
                                                    {
                                                        DrawRainWaterWell(pts[i], values[i]);
                                                    }
                                                }
                                            }
                                            {
                                                var fixY = -QUOTATIONWITTIG;
                                                var v = new Vector2d(-THESAURUSINHERIT - THESAURUSDOMESTIC, -THESAURUSDERELICTION + ACANTHORHYNCHUS + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                _DrawRainWaterWells(pt, values);
                                                var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, fixY), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSATTACHMENT + THESAURUSSURPRISED, THESAURUSENTREPRENEUR);
                                                        DrawNoteText(getPipeDn(), segs[THESAURUSPERMUTATION].EndPoint + v1);
                                                    }
                                                    drawDomePipes(segs);
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSLOITER);
                                                        if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                                        {
                                                            if (!gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(THESAURUSFORMULATE).ToPoint3d());
                                                            }
                                                            else
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(THESAURUSFORMULATE).ToPoint3d());
                                                                {
                                                                    var seg = new List<Vector2d> { new Vector2d(THESAURUSPLEASING, THESAURUSSTAMPEDE), new Vector2d(THESAURUSIMPOSING, THESAURUSSTAMPEDE) }.ToGLineSegments(p).Last();
                                                                    var pt1 = seg.StartPoint.ToPoint3d();
                                                                    var pt2 = seg.EndPoint.ToPoint3d();
                                                                    var dim = new AlignedDimension();
                                                                    dim.XLine1Point = pt1;
                                                                    dim.XLine2Point = pt2;
                                                                    dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetY(-THESAURUSEUPHORIA);
                                                                    dim.DimensionText = METACOMMUNICATION;
                                                                    dim.Layer = THESAURUSINVOICE;
                                                                    ByLayer(dim);
                                                                    DrawEntityLazy(dim);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            DrawWrappingPipe(p.ToPoint3d());
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                            {
                                                var fixX = -THESAURUSATTACHMENT - THESAURUSLITTER;
                                                var fixY = THESAURUSDISAGREEABLE;
                                                var fixV = new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE);
                                                var p = basePt + new Vector2d(THESAURUSCORRECTIVE + fixX, -PORTMANTOLOGISM);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDEPLETION), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSMATHEMATICAL, MORPHOPHONOLOGY) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSENTIRETY + fixY), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSPILGRIM - fixX, THESAURUSSTAMPEDE) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                    {
                                                        var seg = new List<Vector2d> { new Vector2d(THESAURUSPLEASING, THESAURUSSTAMPEDE), new Vector2d(THESAURUSIMPOSING, THESAURUSSTAMPEDE) }.ToGLineSegments(p).Last();
                                                        var pt1 = seg.StartPoint.ToPoint3d();
                                                        var pt2 = seg.EndPoint.ToPoint3d();
                                                        var dim = new AlignedDimension();
                                                        dim.XLine1Point = pt1;
                                                        dim.XLine2Point = pt2;
                                                        dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetY(-THESAURUSEUPHORIA);
                                                        dim.DimensionText = METACOMMUNICATION;
                                                        dim.Layer = THESAURUSINVOICE;
                                                        ByLayer(dim);
                                                        DrawEntityLazy(dim);
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.RainPort)
                                        {
                                            {
                                                var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE) };
                                                var segs = vecs.ToGLineSegments(basePt);
                                                drawDomePipes(segs);
                                                {
                                                    var v1 = new Vector2d(THESAURUSATTACHMENT + THESAURUSSURPRISED, THESAURUSENTREPRENEUR);
                                                    DrawNoteText(getPipeDn(), segs[THESAURUSPERMUTATION].EndPoint + v1);
                                                }
                                                var pt = segs.Last().EndPoint.ToPoint3d();
                                                {
                                                    Dr.DrawRainPort(pt.OffsetX(THESAURUSDOMESTIC + THESAURUSENTREPRENEUR));
                                                    Dr.DrawRainPortLabel(pt.OffsetX(-THESAURUSENTREPRENEUR + THESAURUSENTREPRENEUR));
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSLOITER);
                                                        DrawWrappingPipe(p.ToPoint3d());
                                                        if (gpItem.HasOutletWrappingPipe)
                                                        {
                                                            if (gpItem.HasSingleFloorDrainDrainageForRainPort && !gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort)
                                                            {
                                                                DrawWrappingPipe((basePt + new Vector2d(-QUOTATIONCOLERIDGE, -THESAURUSBELLOW - QUOTATIONPITUITARY)).ToPoint3d());
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForRainPort)
                                            {
                                                var fixX = -THESAURUSATTACHMENT - THESAURUSLITTER;
                                                var fixY = THESAURUSDISAGREEABLE;
                                                var fixV = new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE);
                                                var p = basePt + new Vector2d(THESAURUSCORRECTIVE + fixX, -PORTMANTOLOGISM);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                DrawNoteText(getHDN(), p + new Vector2d(-THESAURUSAPPLICANT + (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort ? THESAURUSSURPRISED : THESAURUSSTAMPEDE), -INCOMMODIOUSNESS));
                                                var pt = p + fixV;
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDEPLETION), new Vector2d(-THESAURUSEXPERIMENT, -THESAURUSEXPERIMENT), new Vector2d(-DISAFFORESTATION, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSVARIABLE, QUOTATIONZYGOMATIC) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var pt1 = pt.OffsetX(-THESAURUSCOSTLY).ToPoint3d();
                                                        var pt2 = pt1.OffsetX(THESAURUSFORBIDDEN);
                                                        var dim = new AlignedDimension();
                                                        dim.XLine1Point = pt1;
                                                        dim.XLine2Point = pt2;
                                                        dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetY(-THESAURUSAROUND);
                                                        dim.DimensionText = METACOMMUNICATION;
                                                        dim.Layer = THESAURUSINVOICE;
                                                        ByLayer(dim);
                                                        DrawEntityLazy(dim);
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -COMMONPLACENESS), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSLEGATE + THESAURUSENTREPRENEUR - THESAURUSDISINGENUOUS, THESAURUSSTAMPEDE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    Dr.DrawRainPort(segs.Last().EndPoint.ToPoint3d());
                                                    DrawBlockReference(THESAURUSEMPHASIS, segs.Last().EndPoint.ToPoint3d(), br => { br.Layer = DENDROCHRONOLOGIST; });
                                                    {
                                                        var seg = new List<Vector2d> { new Vector2d(THESAURUSPLEASING, THESAURUSSTAMPEDE), new Vector2d(THESAURUSIMPOSING, THESAURUSSTAMPEDE) }.ToGLineSegments(p).Last();
                                                        var pt1 = seg.StartPoint.ToPoint3d();
                                                        var pt2 = seg.EndPoint.ToPoint3d();
                                                        var dim = new AlignedDimension();
                                                        dim.XLine1Point = pt1;
                                                        dim.XLine2Point = pt2;
                                                        dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetY(-THESAURUSEUPHORIA);
                                                        dim.DimensionText = METACOMMUNICATION;
                                                        dim.Layer = THESAURUSINVOICE;
                                                        ByLayer(dim);
                                                        DrawEntityLazy(dim);
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.WaterSealingWell)
                                        {
                                            {
                                                if (gpItem.HasOutletWrappingPipe)
                                                {
                                                    DrawWrappingPipe((basePt + new Vector2d(-QUOTATIONCOLERIDGE, -THESAURUSBELLOW)).ToPoint3d());
                                                    if (gpItem.HasSingleFloorDrainDrainageForWaterSealingWell && !gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell)
                                                    {
                                                        DrawWrappingPipe((basePt + new Vector2d(-QUOTATIONCOLERIDGE, -THESAURUSBELLOW - QUOTATIONPITUITARY)).ToPoint3d());
                                                    }
                                                }
                                                var fixY = -QUOTATIONWITTIG;
                                                var v = new Vector2d(-THESAURUSINHERIT - THESAURUSDOMESTIC, -THESAURUSDERELICTION + ACANTHORHYNCHUS + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                DrawWaterSealingWell(pt.OffsetY(-MISAPPREHENSIVE));
                                                var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, fixY), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSATTACHMENT + THESAURUSSURPRISED, THESAURUSENTREPRENEUR);
                                                        DrawNoteText(getPipeDn(), segs[THESAURUSPERMUTATION].EndPoint + v1);
                                                    }
                                                    drawDomePipes(segs);
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterSealingWell)
                                            {
                                                var fixX = -THESAURUSATTACHMENT - THESAURUSLITTER;
                                                var fixY = THESAURUSDISAGREEABLE;
                                                var fixV = new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE);
                                                var p = basePt + new Vector2d(THESAURUSCORRECTIVE + fixX, -PORTMANTOLOGISM);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                DrawNoteText(getHDN(), p + new Vector2d(-THESAURUSAPPLICANT + (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort ? THESAURUSSURPRISED : THESAURUSSTAMPEDE), -INCOMMODIOUSNESS));
                                                var pt = p + fixV;
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDEPLETION), new Vector2d(-THESAURUSEXPERIMENT, -THESAURUSEXPERIMENT), new Vector2d(-DISAFFORESTATION, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSVARIABLE, QUOTATIONZYGOMATIC) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -COMMONPLACENESS), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSLEGATE, THESAURUSSTAMPEDE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
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
                                                        DrawWrappingPipe((basePt + new Vector2d(-QUOTATIONCOLERIDGE, -THESAURUSBELLOW)).ToPoint3d());
                                                        if (gpItem.HasSingleFloorDrainDrainageForDitch && !gpItem.IsFloorDrainShareDrainageWithVerticalPipeForDitch)
                                                        {
                                                            DrawWrappingPipe((basePt + new Vector2d(-QUOTATIONCOLERIDGE, -THESAURUSBELLOW - QUOTATIONPITUITARY)).ToPoint3d());
                                                        }
                                                    }
                                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE) };
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    drawDomePipes(segs);
                                                    var pt = segs.Last().EndPoint.ToPoint3d();
                                                    Dr.DrawLabel(pt.OffsetX(-THESAURUSENTREPRENEUR), QUOTATIONSECOND);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSATTACHMENT + THESAURUSSURPRISED, THESAURUSENTREPRENEUR);
                                                        DrawNoteText(getPipeDn(), segs[THESAURUSPERMUTATION].EndPoint + v1);
                                                    }
                                                }
                                                if (gpItem.HasSingleFloorDrainDrainageForDitch)
                                                {
                                                    var fixX = -THESAURUSATTACHMENT - THESAURUSLITTER;
                                                    var fixY = THESAURUSDISAGREEABLE;
                                                    var fixV = new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE);
                                                    var p = basePt + new Vector2d(THESAURUSCORRECTIVE + fixX, -PORTMANTOLOGISM);
                                                    _DrawFloorDrain(p.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                    DrawNoteText(getHDN(), p + new Vector2d(-ELECTRONEGATIVE, -QUOTATIONETHIOPS) + new Vector2d(-THESAURUSABLUTION + (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort ? THESAURUSSURPRISED : THESAURUSSTAMPEDE), -OTHERWORLDLINESS));
                                                    var pt = p + fixV;
                                                    if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForDitch)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDEPLETION), new Vector2d(-THESAURUSEXPERIMENT, -THESAURUSEXPERIMENT), new Vector2d(-DISAFFORESTATION, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSVARIABLE, QUOTATIONZYGOMATIC) };
                                                        var segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        {
                                                        }
                                                        {
                                                            var seg = new List<Vector2d> { new Vector2d(THESAURUSPLEASING, THESAURUSSTAMPEDE), new Vector2d(THESAURUSIMPOSING, THESAURUSSTAMPEDE) }.ToGLineSegments(p).Last();
                                                            var pt1 = seg.StartPoint.ToPoint3d().OffsetXY(-CRYSTALLOGRAPHER, -COMPANIABLENESS);
                                                            var pt2 = pt1.OffsetX(THESAURUSSCINTILLATE);
                                                            var dim = new AlignedDimension();
                                                            dim.XLine1Point = pt1;
                                                            dim.XLine2Point = pt2;
                                                            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetY(-THESAURUSINHERIT);
                                                            dim.DimensionText = METACOMMUNICATION;
                                                            dim.Layer = THESAURUSINVOICE;
                                                            ByLayer(dim);
                                                            DrawEntityLazy(dim);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -COMMONPLACENESS), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSLEGATE, THESAURUSSTAMPEDE) };
                                                        var segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        {
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _dy = HYPERDISYLLABLE;
                                                var vecs = vecs0;
                                                var p1 = info.StartPoint;
                                                var p2 = p1 + new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONBASTARD - dy + _dy);
                                                var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                info.DisplaySegs = segs;
                                                var p = basePt.OffsetY(HYPERDISYLLABLE);
                                                drawLabel(p, QUOTATIONSECOND, null, INTRAVASCULARLY);
                                                static void DrawDimLabelRight(Point3d basePt, double dy)
                                                {
                                                    var pt1 = basePt;
                                                    var pt2 = pt1.OffsetY(dy);
                                                    var dim = new AlignedDimension();
                                                    dim.XLine1Point = pt1;
                                                    dim.XLine2Point = pt2;
                                                    dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-THESAURUSDOMESTIC);
                                                    dim.DimensionText = THESAURUSILLUMINATION;
                                                    dim.Layer = THESAURUSINVOICE;
                                                    ByLayer(dim);
                                                    DrawEntityLazy(dim);
                                                }
                                                DrawDimLabelRight(p.ToPoint3d(), -HYPERDISYLLABLE);
                                            }
                                        }
                                        else
                                        {
                                            var ditchDy = HYPERDISYLLABLE;
                                            var _run = runs.TryGet(i);
                                            if (_run != null)
                                            {
                                                if (gpItem == null)
                                                {
                                                    if (_run != null)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-MISAPPREHENSIVE, -MISAPPREHENSIVE), new Vector2d(-THESAURUSPRETTY, THESAURUSSTAMPEDE) };
                                                        var p = info.EndPoint;
                                                        var segs = vecs.ToGLineSegments(p);
                                                        drawDomePipes(segs);
                                                        segs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSFORMULATE), new Vector2d(-REPRESENTATIONAL, THESAURUSSTAMPEDE) }.ToGLineSegments(segs.Last().EndPoint);
                                                        foreach (var line in DrawLineSegmentsLazy(segs))
                                                        {
                                                            Dr.SetLabelStylesForRainNote(line);
                                                        }
                                                        {
                                                            var t = DrawTextLazy(QUOTATIONSECOND, THESAURUSENDANGER, segs.Last().EndPoint.OffsetXY(THESAURUSENTREPRENEUR, THESAURUSENTREPRENEUR));
                                                            Dr.SetLabelStylesForRainNote(t);
                                                        }
                                                    }
                                                    else if (!_run.HasLongTranslator && !_run.HasShortTranslator)
                                                    {
                                                        _dy = HYPERDISYLLABLE;
                                                        var vecs = vecs0;
                                                        var p1 = info.StartPoint;
                                                        var p2 = p1 + new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONBASTARD - dy + _dy);
                                                        var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                        info.DisplaySegs = segs;
                                                        var p = basePt.OffsetY(HYPERDISYLLABLE);
                                                        drawLabel(p, QUOTATIONSECOND, null, INTRAVASCULARLY);
                                                        static void DrawDimLabelRight(Point3d basePt, double dy)
                                                        {
                                                            var pt1 = basePt;
                                                            var pt2 = pt1.OffsetY(dy);
                                                            var dim = new AlignedDimension();
                                                            dim.XLine1Point = pt1;
                                                            dim.XLine2Point = pt2;
                                                            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-THESAURUSDOMESTIC);
                                                            dim.DimensionText = THESAURUSILLUMINATION;
                                                            dim.Layer = THESAURUSINVOICE;
                                                            ByLayer(dim);
                                                            DrawEntityLazy(dim);
                                                        }
                                                        DrawDimLabelRight(p.ToPoint3d(), -HYPERDISYLLABLE);
                                                    }
                                                    else
                                                    {
                                                        Dr.DrawLabel(basePt.ToPoint3d(), QUOTATIONSECOND);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                void _DrawCondensePipe(Point2d basePt)
                                {
                                    list.Add(basePt.OffsetY(HYPERDISYLLABLE));
                                    Dr.DrawCondensePipe(basePt.OffsetXY(-HYPERDISYLLABLE, THESAURUSENTREPRENEUR));
                                }
                                void _drawFloorDrain(Point2d basePt, bool leftOrRight, bool isAirFloorDrain)
                                {
                                    list.Add(basePt.OffsetY(QUOTATIONPITUITARY));
                                    var value = ADENOHYPOPHYSIS;
                                    if (gpItem.Hangings[i].HasSideFloorDrain)
                                    {
                                        if (gpItem.Hangings[i].HasSideAndNonSideFloorDrain && leftOrRight == INTRAVASCULARLY)
                                        {
                                        }
                                        else
                                        {
                                            value = PHARYNGEALIZATION;
                                        }
                                    }
                                    if (isAirFloorDrain) value = ADENOHYPOPHYSIS;
                                    if (leftOrRight)
                                    {
                                        _DrawFloorDrain(basePt.OffsetXY(THESAURUSINTRACTABLE + THESAURUSINCOMPLETE, THESAURUSINTRACTABLE).ToPoint3d(), leftOrRight, value);
                                    }
                                    else
                                    {
                                        _DrawFloorDrain(basePt.OffsetXY(THESAURUSINTRACTABLE + THESAURUSINCOMPLETE - THESAURUSDIFFICULTY, THESAURUSINTRACTABLE).ToPoint3d(), leftOrRight, value);
                                    }
                                    return;
                                }
                                {
                                    void drawDN(string dn, Point2d pt)
                                    {
                                        var t = DrawTextLazy(dn, THESAURUSENDANGER, pt);
                                        Dr.SetLabelStylesForRainDims(t);
                                    }
                                    var hanging = gpItem.Hangings[i];
                                    var fixW2 = gpItem.Hangings.All(x => (x?.FloorDrainWrappingPipesCount ?? THESAURUSSTAMPEDE) == THESAURUSSTAMPEDE) ? QUOTATIONWITTIG : THESAURUSSTAMPEDE;
                                    var fixW = THESAURUSDERELICTION - fixW2;
                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSDETERMINED, THESAURUSDETERMINED), new Vector2d(-APOPHTHEGGESTHAI - fixW, THESAURUSSTAMPEDE) };
                                    if (getHasAirConditionerFloorDrain(i))
                                    {
                                        var p1 = info.StartPoint.OffsetY(-THESAURUSDISAGREEABLE);
                                        var p2 = p1.OffsetX(THESAURUSINHERIT);
                                        var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                                        Dr.SetLabelStylesForWNote(line);
                                        var segs = vecs.GetYAxisMirror().ToGLineSegments(p1.OffsetY(-CONSCRIPTIONIST - VÖLKERWANDERUNG));
                                        drawDomePipes(segs);
                                        var p = segs.Last().EndPoint;
                                        _drawFloorDrain(p, THESAURUSOBSTINACY, THESAURUSOBSTINACY);
                                        drawDN(THESAURUSDISREPUTABLE, segs[THESAURUSHOUSING].StartPoint.OffsetXY(HYPERDISYLLABLE + fixW, HYPERDISYLLABLE));
                                    }
                                    if (hanging.FloorDrainsCount > THESAURUSSTAMPEDE)
                                    {
                                        var bsPt = info.EndPoint;
                                        var wpCount = hanging.FloorDrainWrappingPipesCount;
                                        void tryDrawWrappingPipe(Point2d pt)
                                        {
                                            if (wpCount <= THESAURUSSTAMPEDE) return;
                                            DrawWrappingPipe(pt.ToPoint3d());
                                            --wpCount;
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                                        {
                                            if (!(storey == THESAURUSREGION && (gpItem.HasSingleFloorDrainDrainageForWaterWell || gpItem.HasSingleFloorDrainDrainageForRainPort)))
                                            {
                                                var v = default(Vector2d);
                                                var ok = INTRAVASCULARLY;
                                                if (gpItem.Items.TryGet(i - THESAURUSHOUSING).HasLong)
                                                {
                                                    if (runs[i - THESAURUSHOUSING].IsLongTranslatorToLeftOrRight)
                                                    {
                                                        var fixV = new Vector2d(QUOTATIONWITTIG, THESAURUSSTAMPEDE);
                                                        var p = vecs.GetLastPoint(bsPt.OffsetY(-CONSCRIPTIONIST - VÖLKERWANDERUNG) + v + fixV);
                                                        if (gpItem.PipeType is PipeType.NL or PipeType.Y2L)
                                                        {
                                                            p = p.OffsetX(-QUOTATIONWITTIG);
                                                        }
                                                        var _vecs = new List<Vector2d> { new Vector2d(-MISAPPREHENSIVE, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -DISCOURTEOUSNESS), new Vector2d(THESAURUSPERVADE, -THESAURUSUNCOMMITTED) };
                                                        var segs = _vecs.ToGLineSegments(p);
                                                        _drawFloorDrain(p, THESAURUSOBSTINACY, INTRAVASCULARLY);
                                                        drawDomePipes(segs);
                                                        tryDrawWrappingPipe(p.OffsetX(THESAURUSATTACHMENT));
                                                        var __vecs = new List<Vector2d> { new Vector2d(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -OVERWHELMINGNESS), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC) };
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
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE), INTERNALIZATION, THESAURUSINVOICE);
                                                        ok = THESAURUSOBSTINACY;
                                                    }
                                                }
                                                if (!ok)
                                                {
                                                    var dy = QUOTATIONTRANSFERABLE;
                                                    if (gpItem.Hangings[i].HasSideFloorDrain)
                                                    {
                                                        dy = -MECHANOCHEMISTRY - (HEIGHT - QUOTATIONBASTARD);
                                                    }
                                                    var segs = vecs.ToGLineSegments(bsPt.OffsetY(-CONSCRIPTIONIST - VÖLKERWANDERUNG + dy) + v);
                                                    drawDomePipes(segs);
                                                    var p = segs.Last().EndPoint;
                                                    _drawFloorDrain(p, THESAURUSOBSTINACY, INTRAVASCULARLY);
                                                    tryDrawWrappingPipe(p.OffsetX(THESAURUSATTACHMENT));
                                                    drawDN(getFDDN(), segs[THESAURUSHOUSING].EndPoint.OffsetXY(HYPERDISYLLABLE + fixW, HYPERDISYLLABLE));
                                                    ok = THESAURUSOBSTINACY;
                                                }
                                            }
                                        }
                                        else if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                        {
                                            {
                                                if (hanging.HasSideAndNonSideFloorDrain)
                                                {
                                                    var segs = vecs.ToGLineSegments(bsPt.OffsetY(-CONSCRIPTIONIST - VÖLKERWANDERUNG) + new Vector2d(THESAURUSSTAMPEDE, INAUSPICIOUSNESS));
                                                    drawDomePipes(segs);
                                                    var p = segs.Last().EndPoint;
                                                    _drawFloorDrain(p, THESAURUSOBSTINACY, INTRAVASCULARLY);
                                                    tryDrawWrappingPipe(p.OffsetX(THESAURUSATTACHMENT));
                                                    drawDN(getFDDN(), segs[THESAURUSHOUSING].EndPoint.OffsetXY(HYPERDISYLLABLE + fixW, HYPERDISYLLABLE));
                                                }
                                                else
                                                {
                                                    var segs = vecs.ToGLineSegments(bsPt.OffsetY(-CONSCRIPTIONIST - VÖLKERWANDERUNG));
                                                    drawDomePipes(segs);
                                                    var p = segs.Last().EndPoint;
                                                    _drawFloorDrain(p, THESAURUSOBSTINACY, INTRAVASCULARLY);
                                                    tryDrawWrappingPipe(p.OffsetX(THESAURUSATTACHMENT));
                                                    drawDN(getFDDN(), segs[THESAURUSHOUSING].EndPoint.OffsetXY(HYPERDISYLLABLE + fixW, HYPERDISYLLABLE));
                                                }
                                            }
                                            {
                                                var segs = vecs.GetYAxisMirror().ToGLineSegments(info.EndPoint.OffsetY(-CONSCRIPTIONIST - VÖLKERWANDERUNG));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _drawFloorDrain(p, INTRAVASCULARLY, INTRAVASCULARLY);
                                                tryDrawWrappingPipe(p.OffsetX(-MISAPPREHENSIVE));
                                                drawDN(getFDDN(), segs[THESAURUSHOUSING].StartPoint.OffsetXY(HYPERDISYLLABLE + fixW, HYPERDISYLLABLE));
                                            }
                                        }
                                    }
                                    if (hanging.HasCondensePipe)
                                    {
                                        string getCondensePipeDN()
                                        {
                                            return viewModel?.Params.CondensePipeHorizontalDN ?? THESAURUSDISREPUTABLE;
                                        }
                                        var h = THESAURUSSURPRISED;
                                        var w = QUOTATIONWITTIG;
                                        if (hanging.HasBrokenCondensePipes)
                                        {
                                            var v = new Vector2d(THESAURUSSTAMPEDE, -THESAURUSENTREPRENEUR);
                                            void f(double offsetY)
                                            {
                                                var segs = vecs.ToGLineSegments((info.StartPoint + v).OffsetY(offsetY));
                                                var p1 = segs.Last().EndPoint;
                                                var p3 = p1.OffsetY(h);
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3.OffsetXY(-HYPERDISYLLABLE, HYPERDISYLLABLE));
                                                drawDomePipes(segs);
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(HYPERDISYLLABLE + fixW, -THESAURUSENTREPRENEUR));
                                            }
                                            f(-HYPERCORRECTION);
                                            f(-HYPERCORRECTION - THESAURUSGETAWAY);
                                        }
                                        else
                                        {
                                            double fixY = -THESAURUSHYPNOTIC;
                                            var higher = hanging.PlsDrawCondensePipeHigher;
                                            if (higher)
                                            {
                                                fixY += QUOTATIONWITTIG;
                                            }
                                            var v = new Vector2d(THESAURUSSTAMPEDE, fixY);
                                            var segs = vecs.ToGLineSegments((info.StartPoint + v).OffsetY(-HYPERCORRECTION));
                                            var p1 = segs.Last().EndPoint;
                                            var p2 = p1.OffsetX(w);
                                            var p3 = p1.OffsetY(h);
                                            var p4 = p2.OffsetY(h);
                                            drawDomePipes(segs);
                                            if (hanging.HasNonBrokenCondensePipes)
                                            {
                                                double _fixY = higher ? -CONSCRIPTIONIST : THESAURUSSTAMPEDE;
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(HYPERDISYLLABLE + fixW, THESAURUSENTREPRENEUR + THESAURUSENTREPRENEUR + _fixY));
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3);
                                                drawDomePipe(new GLineSegment(p2, p4));
                                                _DrawCondensePipe(p4);
                                            }
                                            else
                                            {
                                                double _fixY = higher ? -CONSCRIPTIONIST + HYPERDISYLLABLE : THESAURUSSTAMPEDE;
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(HYPERDISYLLABLE + fixW, -THESAURUSENTREPRENEUR + _fixY));
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
                                    var h = Math.Round(HEIGHT / ACANTHOCEPHALANS * THESAURUSCOMMUNICATION);
                                    var p = run.HasShortTranslator ? info.Segs.Last().StartPoint.OffsetY(h).ToPoint3d() : info.EndPoint.OffsetY(h).ToPoint3d();
                                    DrawCheckPoint(p, THESAURUSOBSTINACY);
                                    var seg = info.Segs.Last();
                                    var fixDy = run.HasShortTranslator ? -seg.Height : THESAURUSSTAMPEDE;
                                    DrawDimLabelRight(p, fixDy - h);
                                }
                            }
                        }
                        if (list.Count > THESAURUSSTAMPEDE)
                        {
                            var my = list.Select(x => x.Y).Max();
                            foreach (var pt in list)
                            {
                                if (pt.Y == my)
                                {
                                }
                            }
                            var h = POLYOXYMETHYLENE - PHYSIOLOGICALLY;
                            foreach (var pt in list)
                            {
                                if (pt.Y != my) continue;
                                var ok = INTRAVASCULARLY;
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
                                            linesKillers.Add(new GLineSegment(pt.OffsetY(-ASSOCIATIONISTS), pt.OffsetY(-ASSOCIATIONISTS).OffsetX(THESAURUSNOTORIETY)).ToLineString());
                                        }
                                        ok = THESAURUSOBSTINACY;
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
                                                linesKillers.Add(new GLineSegment(pt, pt.OffsetX(THESAURUSNOTORIETY)).ToLineString());
                                            }
                                            _DrawAiringSymbol(p.OffsetY(h));
                                        }
                                        else
                                        {
                                            linesKillers.Add(new GLineSegment(pt, pt.OffsetX(THESAURUSNOTORIETY)).ToLineString());
                                        }
                                        ok = THESAURUSOBSTINACY;
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
                                    linesKillers.Add(new GLineSegment(pt.OffsetXY(THESAURUSHOUSING, THESAURUSHOUSING + THESAURUSATTACHMENT), pt.OffsetXY(-THESAURUSHOUSING, THESAURUSHOUSING + THESAURUSATTACHMENT)).ToLineString());
                                    _DrawAiringSymbol(pt.OffsetY(THESAURUSATTACHMENT));
                                    drawDomePipe(new GLineSegment(pt, pt.OffsetY(THESAURUSATTACHMENT)));
                                }
                            }
                        }
                    }
                    bool getHasAirConditionerFloorDrain(int i)
                    {
                        if (gpItem.PipeType != PipeType.Y2L) return INTRAVASCULARLY;
                        var hanging = gpItem.Hangings[i];
                        if (hanging.HasBrokenCondensePipes || hanging.HasNonBrokenCondensePipes)
                        {
                            return viewModel?.Params.HasAirConditionerFloorDrain ?? INTRAVASCULARLY;
                        }
                        return INTRAVASCULARLY;
                    }
                    var infos = getPipeRunLocationInfos(basePoint.OffsetX(dx));
                    for (int i = gpItem.Items.Count - THESAURUSHOUSING; i >= THESAURUSHOUSING; i--)
                    {
                        if (gpItem.Items[i].Exist)
                        {
                            if (gpItem.Items[i - THESAURUSHOUSING].Exist && gpItem.Hangings[i].FloorDrainsCount > THESAURUSSTAMPEDE)
                            {
                                var pt = infos[i].BasePoint;
                                if (!shouldDrawAringSymbol)
                                {
                                    vsels.Add(pt.ToPoint3d().OffsetY(HEIGHT));
                                    vkills.Add(pt.OffsetY(HEIGHT - ASSOCIATIONISTS).ToPoint3d());
                                }
                            }
                            break;
                        }
                    }
                    handlePipeLine(thwPipeLine, infos);
                    static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight, double height = THESAURUSENDANGER)
                    {
                        var gap = THESAURUSENTREPRENEUR;
                        var factor = THESAURUSDISPASSIONATE;
                        var (w1, _) = GetDBTextSize(text1, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
                        var (w2, _) = GetDBTextSize(text2, THESAURUSENDANGER, THESAURUSDISPASSIONATE, CONTROVERSIALLY);
                        var width = Math.Max(w1, w2);
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSDOMESTIC, THESAURUSDOMESTIC), new Vector2d(width, THESAURUSSTAMPEDE) };
                        if (isLeftOrRight == THESAURUSOBSTINACY)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[THESAURUSHOUSING].EndPoint : segs[THESAURUSHOUSING].StartPoint;
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
                        var height = THESAURUSENDANGER;
                        var gap = THESAURUSENTREPRENEUR;
                        var factor = THESAURUSDISPASSIONATE;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? THESAURUSSTAMPEDE, text2?.Length ?? THESAURUSSTAMPEDE);
                        if (width < REPRESENTATIONAL) width = REPRESENTATIONAL;
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSLITANY, THESAURUSSTAMPEDE), new Vector2d(width, THESAURUSSTAMPEDE) };
                        if (isLeftOrRight == THESAURUSOBSTINACY)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[THESAURUSHOUSING].EndPoint : segs[THESAURUSHOUSING].StartPoint;
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
                    for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
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
                        if (storey == THESAURUSARGUMENTATIVE && gpItem.HasLineAtBuildingFinishedSurfice)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                            drawDomePipe(new GLineSegment(p1, p2));
                        }
                    }
                    {
                        var has_label_storeys = new HashSet<string>();
                        {
                            var _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSPERMUTATION), allNumStoreyLabels.GetLastOrDefault(INTROPUNITIVENESS) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            _storeys = _storeys.Where(storey =>
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = infos.TryGet(i);
                                return info != null && info.Visible;
                            }).ToList();
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                _storeys = allNumStoreyLabels.Where(storey =>
                                {
                                    var i = allNumStoreyLabels.IndexOf(storey);
                                    var info = infos.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(THESAURUSHOUSING).ToList();
                            }
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                _storeys = allStoreys.Where(storey =>
                                {
                                    var i = allStoreys.IndexOf(storey);
                                    var info = infos.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(THESAURUSHOUSING).ToList();
                            }
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                for (int i = THESAURUSSTAMPEDE; i < gpItem.Items.Count; i++)
                                {
                                    if (gpItem.Items[i].Exist)
                                    {
                                        var s = gpItem.Hangings[i].Storey;
                                        _storeys.Add(s);
                                        break;
                                    }
                                }
                            }
                            if (_storeys.Count == THESAURUSPERMUTATION)
                            {
                                if (Math.Abs(allStoreys.IndexOf(_storeys[THESAURUSSTAMPEDE]) - allStoreys.IndexOf(_storeys[THESAURUSHOUSING])) == THESAURUSHOUSING)
                                {
                                    _storeys.RemoveAt(THESAURUSHOUSING);
                                }
                            }
                            {
                                string label1, label2;
                                var labels = RainLabelItem.ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x)).ToList()).OrderBy(x => x).ToList();
                                if (labels.Count == THESAURUSPERMUTATION)
                                {
                                    label1 = labels[THESAURUSSTAMPEDE];
                                    label2 = labels[THESAURUSHOUSING];
                                }
                                else
                                {
                                    label1 = labels.JoinWith(THESAURUSCAVALIER);
                                    label2 = null;
                                }
                                if (_storeys.Count == THESAURUSSTAMPEDE)
                                {
                                    if (iRF >= THESAURUSSTAMPEDE)
                                    {
                                        drawLabel(infos[iRF].BasePoint, label1, label2, INTRAVASCULARLY);
                                    }
                                }
                                foreach (var storey in _storeys)
                                {
                                    has_label_storeys.Add(storey);
                                    var i = allStoreys.IndexOf(storey);
                                    var info = infos[i];
                                    {
                                        var isLeftOrRight = (gpItem.Hangings.TryGet(i)?.FloorDrainsCount ?? THESAURUSSTAMPEDE) == THESAURUSSTAMPEDE && !(gpItem.Hangings.TryGet(i)?.HasCondensePipe ?? INTRAVASCULARLY);
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
                                                    isLeftOrRight = INTRAVASCULARLY;
                                                }
                                            }
                                        }
                                        if (getHasAirConditionerFloorDrain(i) && isLeftOrRight == INTRAVASCULARLY)
                                        {
                                            var pt = info.EndPoint.OffsetY(THESAURUSSURPRISED);
                                            if (storey == THESAURUSARGUMENTATIVE)
                                            {
                                                pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                            }
                                            drawLabel2(pt, label1, label2, isLeftOrRight);
                                        }
                                        else
                                        {
                                            var pt = info.PlBasePt;
                                            if (storey == THESAURUSARGUMENTATIVE)
                                            {
                                                pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                            }
                                            drawLabel(pt, label1, label2, isLeftOrRight);
                                        }
                                    }
                                }
                            }
                        }
                        {
                            var _allSmoothStoreys = new List<string>();
                            {
                                bool isMinFloor = THESAURUSOBSTINACY;
                                for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
                                {
                                    var (ok, item) = gpItem.Items.TryGetValue(i);
                                    if (!(ok && item.Exist)) continue;
                                    if (item.Exist && isMinFloor)
                                    {
                                        isMinFloor = INTRAVASCULARLY;
                                        continue;
                                    }
                                    var storey = allNumStoreyLabels[i];
                                    if (has_label_storeys.Contains(storey)) continue;
                                    var run = runs.TryGet(i);
                                    if (run == null) continue;
                                    if (!run.HasLongTranslator && !run.HasShortTranslator && (!(gpItem.Hangings.TryGet(i)?.HasCheckPoint ?? INTRAVASCULARLY)))
                                    {
                                        _allSmoothStoreys.Add(storey);
                                    }
                                }
                            }
                            var _storeys = new string[] { _allSmoothStoreys.GetAt(THESAURUSHOUSING), _allSmoothStoreys.GetLastOrDefault(THESAURUSPERMUTATION) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSHOUSING), allNumStoreyLabels.GetLastOrDefault(THESAURUSPERMUTATION) }.SelectNotNull().Distinct().ToList();
                            }
                            if (_storeys.Count == THESAURUSSTAMPEDE)
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
                                        Dr.DrawDN_2(info.EndPoint.OffsetXY(THESAURUSFORMULATE, THESAURUSSTAMPEDE), CIRCUMCONVOLUTION, dn);
                                    }
                                }
                            }
                        }
                    }
                    if (linesKillers.Count > THESAURUSSTAMPEDE)
                    {
                        dome_lines = GeoFac.ToNodedLineSegments(dome_lines);
                        var geos = dome_lines.Select(x => x.ToLineString()).ToList();
                        dome_lines = geos.Except(GeoFac.CreateIntersectsSelector(geos)(GeoFac.CreateGeometryEx(linesKillers.ToList()))).Cast<LineString>().SelectMany(x => x.ToGLineSegments()).ToList();
                    }
                    {
                        var _vlines = dome_lines.Where(x => x.IsVertical(THESAURUSHOUSING)).ToList();
                        var others = dome_lines.Except(_vlines).ToHashSet();
                        var vlines = _vlines.Select(x => x.ToLineString()).ToList();
                        if (vsels.Count > THESAURUSSTAMPEDE && vkills.Count > THESAURUSSTAMPEDE)
                        {
                            var kill = GeoFac.CreateGeometryEx(vkills.Distinct().Select(x => GRect.Create(x.ToPoint2D(), UNCONSEQUENTIAL, UNCONSEQUENTIAL).ToPolygon()).ToList());
                            var lines = GeoFac.CreateIntersectsSelector(vlines)(GeoFac.CreateGeometryEx(vsels.Distinct().Select(x => GRect.Create(x.ToPoint2D(), UNCONSEQUENTIAL, UNCONSEQUENTIAL).ToPolygon()).ToList()));
                            vlines = vlines.Except(lines).ToList();
                            lines.AddRange(vsels.Distinct().Select(x => GRect.Create(x.ToPoint2D(), UNCONSEQUENTIAL, UNCONSEQUENTIAL)).Select(r => new GLineSegment(r.LeftTop, r.RightButtom).ToLineString()));
                            var lst = GeoFac.ToNodedLineSegments(GeoFac.GetLines(new MultiLineString(lines.ToArray())).ToList()).Where(x => x.Length > THESAURUSHOUSING).ToList();
                            vlines.AddRange(lst.Select(x => x.ToLineString()).Where(x => !x.Intersects(kill)));
                        }
                        if (vdrills.Count > THESAURUSSTAMPEDE)
                        {
                            vlines = GeoFac.GetLines(new MultiLineString(vlines.ToArray()).Difference(GeoFac.CreateGeometryEx(vdrills.Select(x => x.ToPolygon()).ToList()))).Select(x => x.ToLineString()).ToList();
                        }
                        others.AddRange(GeoFac.GetManyLines(vlines, INTRAVASCULARLY));
                        dome_lines = others.ToList();
                    }
                    {
                        var auto_conn = INTRAVASCULARLY;
                        if (auto_conn)
                        {
                            foreach (var g in GeoFac.GroupParallelLines(dome_lines, THESAURUSHOUSING, UNCONSEQUENTIAL))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: THESAURUSGROVEL));
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
                    if (value == PHARYNGEALIZATION)
                    {
                        basePt += new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE).ToVector3d();
                    }
                    DrawBlockReference(PERSUADABLENESS, basePt, br =>
                    {
                        br.Layer = DENDROCHRONOLOGIST;
                        ByLayer(br);
                        br.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
                        }
                    });
                }
                else
                {
                    if (value == PHARYNGEALIZATION)
                    {
                        basePt += new Vector2d(THESAURUSFORESHADOW, -THESAURUSINTRACTABLE).ToVector3d();
                    }
                    DrawBlockReference(PERSUADABLENESS, basePt,
                   br =>
                   {
                       br.Layer = DENDROCHRONOLOGIST;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
                       }
                   });
                }
            }
        }
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = CIRCUMCONVOLUTION;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = THESAURUSDISPASSIONATE;
                    SetTextStyleLazy(t, CONTROVERSIALLY);
                }
            }
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSDOMESTIC, THESAURUSDOMESTIC), new Vector2d(-PROKELEUSMATIKOS, THESAURUSSTAMPEDE) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt + new Vector2d(THESAURUSHESITANCY, INCONSIDERABILIS));
            var wordPt = isLeftOrRight ? segs[THESAURUSHOUSING].EndPoint : segs[THESAURUSHOUSING].StartPoint;
            var text = THESAURUSTENACIOUS;
            var height = THESAURUSENDANGER;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DrawBlockReference(blkName: THESAURUSAGILITY, basePt: basePt,
        cb: br =>
        {
            if (leftOrRight)
            {
                br.ScaleFactors = new Scale3d(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING);
            }
            ByLayer(br);
            br.Layer = THESAURUSJUBILEE;
        });
        }
        public static void DrawRainDiagram(List<RainDrawingData> drDatas, List<StoreyItem> storeysItems, Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys, OtherInfo otherInfo, RainSystemDiagramViewModel vm, ExtraInfo exInfo)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + THESAURUSASPIRATION).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - THESAURUSHOUSING;
            var end = THESAURUSSTAMPEDE;
            var OFFSET_X = QUOTATIONLETTERS;
            var SPAN_X = BALANOPHORACEAE;
            var HEIGHT = vm?.Params?.StoreySpan ?? THESAURUSINCOMING;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSINCOMING;
            DrawRainDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel: vm, otherInfo, exInfo);
        }
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, THESAURUSENDANGER, pt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DrawBlockReference(blkName: THESAURUSSTRINGENT, basePt: basePt.OffsetXY(-THESAURUSGETAWAY, THESAURUSSTAMPEDE), cb: br =>
            {
                SetLayerAndByLayer(THESAURUSDEFAULTER, br);
                if (br.IsDynamicBlock)
                {
                    br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, THESAURUSSEQUEL);
                }
            });
        }
        public static IEnumerable<Point2d> GetBasePoints(Point2d basePoint, int maxCol, int num, double width, double height)
        {
            int i = THESAURUSSTAMPEDE, j = THESAURUSSTAMPEDE;
            for (int k = THESAURUSSTAMPEDE; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = THESAURUSSTAMPEDE;
                }
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = THESAURUSSTAMPEDE, j = THESAURUSSTAMPEDE;
            for (int k = THESAURUSSTAMPEDE; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, THESAURUSSTAMPEDE);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = THESAURUSSTAMPEDE;
                }
            }
        }
        public static void DrawRainWaterWell(Point2d basePt, string value)
        {
            DrawRainWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawWaterSealingWell(Point2d basePt)
        {
            DrawBlockReference(blkName: THESAURUSINTRICATE, basePt: basePt.ToPoint3d(),
         cb: br =>
         {
             br.Layer = DENDROCHRONOLOGIST;
             ByLayer(br);
         });
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            value ??= THESAURUSDEPLORE;
            DrawBlockReference(blkName: THESAURUSGAUCHE, basePt: basePt.OffsetY(-THESAURUSDOMESTIC),
            props: new Dictionary<string, string>() { { THESAURUSSPECIFICATION, value } },
            cb: br =>
            {
                br.Layer = DENDROCHRONOLOGIST;
                ByLayer(br);
            });
        }
        public static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = HELIOCENTRICISM)
        {
            DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, label } }, cb: br => { ByLayer(br); });
        }
        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = THESAURUSCONTROVERSY;
            ByLayer(line);
        }
        public static void SetRainPipeLineStyle(Line line)
        {
            line.Layer = INSTRUMENTALITY;
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
        static readonly Regex re = new Regex(THESAURUSPAGEANTRY);
        public static RainLabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new RainLabelItem()
            {
                Label = label,
                Prefix = m.Groups[THESAURUSHOUSING].Value,
                D1S = m.Groups[THESAURUSPERMUTATION].Value,
                D2S = m.Groups[INTROPUNITIVENESS].Value,
                Suffix = m.Groups[QUOTATIONEDIBLE].Value,
            };
        }
        public static IEnumerable<string> ConvertLabelStrings(List<string> pipeIds)
        {
            {
                var labels = pipeIds.Where(x => Regex.IsMatch(x, THESAURUSJAILER)).ToList();
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
                        var m = Regex.Match(str, THESAURUSJAILER);
                        if (m.Success)
                        {
                            kvs.Add(new KeyValuePair<string, string>(m.Groups[THESAURUSHOUSING].Value, m.Groups[THESAURUSPERMUTATION].Value));
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                    return kvs.GroupBy(x => x.Key).OrderBy(x => x.Key).Select(x => x.Key + THESAURUSSPECIFICATION + string.Join(DEMATERIALISING, GetLabelString(x.Select(y => y.Value[THESAURUSSTAMPEDE]))));
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
                            yield return Convert.ToChar(kv.Key) + THESAURUSEXCREMENT + Convert.ToChar(kv.Value);
                        }
                    }
                }
            }
            {
                var labels = pipeIds.Where(x => Regex.IsMatch(x, THESAURUSCAPRICIOUS)).ToList();
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
                        var m = Regex.Match(str, UNIMPRESSIONABLE);
                        if (m.Success)
                        {
                            kvs.Add(new ValueTuple<string, string, int>(m.Groups[THESAURUSHOUSING].Value, m.Groups[THESAURUSPERMUTATION].Value, int.Parse(m.Groups[INTROPUNITIVENESS].Value)));
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                    return kvs.GroupBy(x => x.Item1).OrderBy(x => x.Key).Select(x => x.Key + string.Join(DEMATERIALISING, GetLabelString(x.First().Item2, x.Select(y => y.Item3))));
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
                            yield return prefix + kv.Key + THESAURUSEXCREMENT + prefix + kv.Value;
                        }
                    }
                }
            }
            var items = pipeIds.Select(id => RainLabelItem.Parse(id)).Where(m => m != null).ToList();
            var rest = pipeIds.Except(items.Select(x => x.Label)).ToList();
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2S?.Length ?? THESAURUSSTAMPEDE).ThenBy(x => x.D2).ThenBy(x => x.D2S).ToList());
            foreach (var g in gs)
            {
                if (g.Count == THESAURUSHOUSING)
                {
                    yield return g.First().Label;
                }
                else if (g.Count > THESAURUSPERMUTATION && g.Count == g.Last().D2 - g.First().D2 + THESAURUSHOUSING)
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
                    for (int i = THESAURUSSTAMPEDE; i < g.Count; i++)
                    {
                        var m = g[i];
                        sb.Append($"{m.D2S}{m.Suffix}");
                        if (i != g.Count - THESAURUSHOUSING)
                        {
                            sb.Append(DEMATERIALISING);
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
                else if (ed + THESAURUSHOUSING == i)
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
        public int LinesCount = THESAURUSHOUSING;
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
        public int HangingCount = THESAURUSSTAMPEDE;
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
        public HashSet<string> HasNonSideFloorDrain;
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
        public HashSet<string> HasWrappingPipe;
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
        public HashSet<string> TouchGravityWaterBuckets;
        public HashSet<string> TouchSideWaterBuckets;
        public Dictionary<int, string> OutletWrappingPipeRadiusStringDict;
        public List<KeyValuePair<string, string>> WrappingPipeRadius220115;
        public List<AloneFloorDrainInfo> AloneFloorDrainInfos;
        public void Init()
        {
            WrappingPipeRadius220115 ??= new List<KeyValuePair<string, string>>();
            Y1LVerticalPipeRects ??= new List<GRect>();
            Y1LVerticalPipeRectLabels ??= new List<string>();
            HasWrappingPipe ??= new HashSet<string>();
            GravityWaterBuckets ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
            GravityWaterBucketLabels ??= new List<string>();
            SideWaterBucketLabels ??= new List<string>();
            _87WaterBucketLabels ??= new List<string>();
            VerticalPipeLabels ??= new HashSet<string>();
            HasSideFloorDrain ??= new HashSet<string>();
            HasNonSideFloorDrain ??= new HashSet<string>();
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
            TouchGravityWaterBuckets ??= new HashSet<string>();
            TouchSideWaterBuckets ??= new HashSet<string>();
        }
    }
    public partial class RainService
    {
        public AcadDatabase adb;
        public RainDiagram RainDiagram;
        public List<StoreyItem> Storeys;
        public RainGeoData GeoData;
        public RainCadData CadDataMain;
        public List<RainCadData> CadDatas;
        public List<RainDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static RainGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            FocusMainWindow();
            var range = TrySelectRangeEx();
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
                if (pls.Count == THESAURUSSTAMPEDE) return INTRAVASCULARLY;
            }
            return THESAURUSOBSTINACY;
        }
        public static ThRainSystemService.CommandContext commandContext => ThRainSystemService.commandContext;
        static List<GLineSegment> ExplodeGLineSegments(Geometry geo)
        {
            static IEnumerable<GLineSegment> enumerate(Geometry geo)
            {
                if (geo is LineString ls)
                {
                    if (ls.NumPoints == THESAURUSPERMUTATION) yield return new GLineSegment(ls[THESAURUSSTAMPEDE].ToPoint2d(), ls[THESAURUSHOUSING].ToPoint2d());
                    else if (ls.NumPoints > THESAURUSPERMUTATION)
                    {
                        for (int i = THESAURUSSTAMPEDE; i < ls.NumPoints - THESAURUSHOUSING; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + THESAURUSHOUSING].ToPoint2d());
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
            foreach (var s in geoData.Storeys) DrawRectLazy(s).ColorIndex = THESAURUSHOUSING;
            foreach (var o in geoData.LabelLines) DrawLineSegmentLazy(o).ColorIndex = THESAURUSHOUSING;
            foreach (var o in geoData.Labels)
            {
                DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = THESAURUSPERMUTATION;
                DrawRectLazy(o.Boundary).ColorIndex = THESAURUSPERMUTATION;
            }
            foreach (var o in geoData.VerticalPipes) DrawRectLazy(o).ColorIndex = INTROPUNITIVENESS;
            foreach (var o in geoData.FloorDrains)
            {
                DrawRectLazy(o).ColorIndex = QUOTATIONEDIBLE;
                Dr.DrawSimpleLabel(o.LeftTop, VASOCONSTRICTOR);
            }
            foreach (var o in geoData.GravityWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = QUOTATIONEDIBLE;
                Dr.DrawSimpleLabel(o.LeftTop, QUOTATIONALDOUS);
            }
            foreach (var o in geoData.SideWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = QUOTATIONEDIBLE;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSINSUFFERABLE);
            }
            foreach (var o in geoData._87WaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = QUOTATIONEDIBLE;
                Dr.DrawSimpleLabel(o.LeftTop, QUOTATIONHOFMANN);
            }
            foreach (var o in geoData.WaterPorts) DrawRectLazy(o).ColorIndex = THESAURUSDESTITUTE;
            foreach (var o in geoData.WaterWells) DrawRectLazy(o).ColorIndex = THESAURUSDESTITUTE;
            {
                var cl = Color.FromRgb(QUOTATIONEDIBLE, SYNTHLIBORAMPHUS, THESAURUSPRIVATE);
                foreach (var o in geoData.DLines) DrawLineSegmentLazy(o).Color = cl;
            }
            foreach (var o in geoData.WLines) DrawLineSegmentLazy(o).ColorIndex = SUPERLATIVENESS;
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = THESAURUSDOMESTIC;
        public static ExtraInfo CreateDrawingDatas(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas, out string logString, out List<RainDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData)
        {
            _DrawingTransaction.Current.AbleToDraw = INTRAVASCULARLY;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = THESAURUSHOUSING;
            }
            var sb = new StringBuilder(THESAURUSEXCEPTION);
            drDatas = new List<RainDrawingData>();
            var extraInfo = new ExtraInfo() { Items = new List<ExtraInfo.Item>(), CadDatas = cadDatas, drDatas = drDatas, geoData = geoData, };
            for (int si = THESAURUSSTAMPEDE; si < cadDatas.Count; si++)
            {
                var drData = new RainDrawingData();
                drData.Init();
                drData.Boundary = geoData.Storeys[si];
                drData.ContraPoint = geoData.StoreyContraPoints[si];
                var item = cadDatas[si];
                var exItem = new ExtraInfo.Item();
                extraInfo.Items.Add(exItem);
                exItem.Index = si;
                {
                    var maxDis = THESAURUSEPICUREAN;
                    var angleTolleranceDegree = THESAURUSHOUSING;
                    var waterPortCvt = RainCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.WLines.Where(x => x.Length > THESAURUSSTAMPEDE).Distinct().ToList().Select(cadDataMain.WLines).ToList(geoData.WLines).ToList(),
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
                var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, DINOFLAGELLATES).ToList();
                var wrappingPipesf = F(item.WrappingPipes);
                var sfdsf = F(item.SideFloorDrains);
                {
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (text.Contains(THESAURUSELIGIBLE) && text.Contains(THESAURUSADVENT))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == THESAURUSHOUSING)
                            {
                                var labelLineGeo = lst[THESAURUSSTAMPEDE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, THESAURUSCOMMUNICATION);
                                var _pts = pts.Select(x => x.ToNTSPoint()).ToList();
                                var ptsf = GeoFac.CreateIntersectsSelector(_pts);
                                {
                                    var __pts = _pts.Except(ptsf(label)).Where(pt => item.RainPortSymbols.All(x => !x.Intersects(pt.Buffer(THESAURUSENTREPRENEUR)))).ToList();
                                    if (__pts.Count > THESAURUSSTAMPEDE)
                                    {
                                        foreach (var r in __pts.Select(pt => GRect.Create(pt.ToPoint2d(), THESAURUSHESITANCY)))
                                        {
                                            geoData.RainPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.RainPortSymbols.Add(pl);
                                            item.RainPortSymbols.Add(pl);
                                            DrawTextLazy(THESAURUSJOBBER, pl.GetCenter());
                                        }
                                    }
                                }
                            }
                        }
                        else
                        if (text.Contains(THESAURUSELIGIBLE) && text.Contains(QUOTATIONMALTESE))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == THESAURUSHOUSING)
                            {
                                var labelLineGeo = lst[THESAURUSSTAMPEDE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, THESAURUSCOMMUNICATION);
                                var _pts = pts.Select(x => x.ToNTSPoint()).ToList();
                                var ptsf = GeoFac.CreateIntersectsSelector(_pts);
                                _pts = _pts.Except(ptsf(label)).Where(pt => item.Ditches.All(x => !x.Intersects(pt.Buffer(THESAURUSENTREPRENEUR)))).ToList();
                                if (_pts.Count > THESAURUSSTAMPEDE)
                                {
                                    foreach (var r in _pts.Select(pt => GRect.Create(pt.ToPoint2d(), THESAURUSENTREPRENEUR)))
                                    {
                                        geoData.Ditches.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.Ditches.Add(pl);
                                        item.Ditches.Add(pl);
                                        DrawTextLazy(THESAURUSJOBBER, pl.GetCenter());
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
                        if (text.Contains(THESAURUSELIGIBLE) && text.Contains(THESAURUSADVENT))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == THESAURUSHOUSING)
                            {
                                var labelLineGeo = lst[THESAURUSSTAMPEDE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, THESAURUSCOMMUNICATION);
                                var sel = new MultiPoint(pts.Select(x => x.ToNTSPoint()).ToArray());
                                var guid = Guid.NewGuid().ToString(THESAURUSTACKLE);
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
                            port.UserData = new Tuple<int, string>(id, Guid.NewGuid().ToString(THESAURUSTACKLE));
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
                            if (lst.Count == THESAURUSHOUSING)
                            {
                                var labelline = lst[THESAURUSSTAMPEDE];
                                if (pipesf(GeoFac.CreateGeometry(label, labelline)).Count == THESAURUSSTAMPEDE)
                                {
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList(), label, radius: THESAURUSCOMMUNICATION);
                                    if (points.Count == THESAURUSHOUSING)
                                    {
                                        var pt = points[THESAURUSSTAMPEDE];
                                        if (!labelsf(pt.ToNTSPoint()).Any())
                                        {
                                            var r = GRect.Create(pt, THESAURUSLUMBERING);
                                            geoData.VerticalPipes.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.VerticalPipes.Add(pl);
                                            item.VerticalPipes.Add(pl);
                                            DrawTextLazy(THESAURUSJOBBER, pl.GetCenter());
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
                            if (lst.Count == THESAURUSHOUSING)
                            {
                                var labellinesGeo = lst[THESAURUSSTAMPEDE];
                                if (labelsf(labellinesGeo).Count != THESAURUSHOUSING) continue;
                                var lines = ExplodeGLineSegments(labellinesGeo).Where(x => x.IsValid).Distinct().ToList();
                                var geos = lines.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                                var f = F(geos);
                                var tmp = f(label).ToList();
                                if (tmp.Count == THESAURUSHOUSING)
                                {
                                    var l1 = tmp[THESAURUSSTAMPEDE];
                                    tmp = f(l1).Where(x => x != l1).ToList();
                                    if (tmp.Count == THESAURUSHOUSING)
                                    {
                                        var l2 = tmp[THESAURUSSTAMPEDE];
                                        if (lines[geos.IndexOf(l2)].IsHorizontal(THESAURUSCOMMUNICATION))
                                        {
                                            tmp = f(l2).Where(x => x != l1 && x != l2).ToList();
                                            if (tmp.Count == THESAURUSHOUSING)
                                            {
                                                var l3 = tmp[THESAURUSSTAMPEDE];
                                                var seg = lines[geos.IndexOf(l3)];
                                                var pts = new List<Point>() { seg.StartPoint.ToNTSPoint(), seg.EndPoint.ToNTSPoint() };
                                                var _tmp = pts.Except(GeoFac.CreateIntersectsSelector(pts)(l2.Buffer(THESAURUSACRIMONIOUS, EndCapStyle.Square))).ToList();
                                                if (_tmp.Count == THESAURUSHOUSING)
                                                {
                                                    var ptGeo = _tmp[THESAURUSSTAMPEDE];
                                                    var pipes = pipesf(ptGeo);
                                                    if (pipes.Count == THESAURUSHOUSING)
                                                    {
                                                        var pipe = pipes[THESAURUSSTAMPEDE];
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
                                    if (lst.Count == THESAURUSHOUSING)
                                    {
                                        var labelline = lst[THESAURUSSTAMPEDE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == THESAURUSSTAMPEDE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList(), label, radius: THESAURUSCOMMUNICATION);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, HYPERDISYLLABLE);
                                                geoData.GravityWaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain.GravityWaterBuckets.Add(pl);
                                                item.GravityWaterBuckets.Add(pl);
                                                DrawTextLazy(THESAURUSJOBBER, pl.GetCenter());
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
                                    if (lst.Count == THESAURUSHOUSING)
                                    {
                                        var labelline = lst[THESAURUSSTAMPEDE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == THESAURUSSTAMPEDE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList(), label, radius: THESAURUSCOMMUNICATION);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, HYPERDISYLLABLE);
                                                geoData.SideWaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain.SideWaterBuckets.Add(pl);
                                                item.SideWaterBuckets.Add(pl);
                                                DrawTextLazy(THESAURUSJOBBER, pl.GetCenter());
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
                                    if (lst.Count == THESAURUSHOUSING)
                                    {
                                        var labelline = lst[THESAURUSSTAMPEDE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == THESAURUSSTAMPEDE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSCOMMUNICATION)).ToList(), label, radius: THESAURUSCOMMUNICATION);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, HYPERDISYLLABLE);
                                                geoData._87WaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain._87WaterBuckets.Add(pl);
                                                item._87WaterBuckets.Add(pl);
                                                DrawTextLazy(THESAURUSJOBBER, pl.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                DrawGeoData(geoData, cadDataMain, si, item);
                {
                    {
                        var ok_ents = new HashSet<Geometry>();
                        for (int i = THESAURUSSTAMPEDE; i < INTROPUNITIVENESS; i++)
                        {
                            var ok = INTRAVASCULARLY;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == THESAURUSHOUSING && pipes.Count == THESAURUSHOUSING)
                                {
                                    var lb = labels[THESAURUSSTAMPEDE];
                                    var pp = pipes[THESAURUSSTAMPEDE];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSDEPLORE;
                                    if (IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = THESAURUSOBSTINACY;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        for (int i = THESAURUSSTAMPEDE; i < INTROPUNITIVENESS; i++)
                        {
                            var ok = INTRAVASCULARLY;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > THESAURUSSTAMPEDE)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSDEPLORE).ToList();
                                    if (labelsTxts.All(txt => IsMaybeLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = THESAURUSSTAMPEDE; k < pipes.Count; k++)
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
                                        ok = THESAURUSOBSTINACY;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        {
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? THESAURUSDEPLORE;
                                if (!IsMaybeLabelText(lb)) continue;
                                var lst = labellinesGeosf(label);
                                if (lst.Count == THESAURUSHOUSING)
                                {
                                    var labelline = lst[THESAURUSSTAMPEDE];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == THESAURUSHOUSING)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[THESAURUSSTAMPEDE].ToNTSPoint());
                                        if (pipes.Count == THESAURUSHOUSING)
                                        {
                                            var pp = pipes[THESAURUSSTAMPEDE];
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
                            var ok = INTRAVASCULARLY;
                            for (int i = THESAURUSSTAMPEDE; i < INTROPUNITIVENESS; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in wlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == THESAURUSHOUSING && lst2.Count > THESAURUSSTAMPEDE)
                                    {
                                        var pp1 = lst1[THESAURUSSTAMPEDE];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (THESAURUSACRIMONIOUS < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSOBSTINACY;
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSOBSTINACY;
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
                            var ok = INTRAVASCULARLY;
                            for (int i = THESAURUSSTAMPEDE; i < INTROPUNITIVENESS; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(THESAURUSCOMMUNICATION).ToGCircle(INTRAVASCULARLY).ToCirclePolygon(SUPERLATIVENESS));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > THESAURUSHOUSING)
                                        {
                                            fs.Add(() =>
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSOBSTINACY;
                                            });
                                        }
                                    }
                                    if (fs.Count == THESAURUSHOUSING) fs[THESAURUSSTAMPEDE]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = THESAURUSSTAMPEDE; i < INTROPUNITIVENESS; i++)
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
                        foreach (var label in d.Where(x => x.Value > THESAURUSHOUSING).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == THESAURUSPERMUTATION)
                            {
                                var dis = pps[THESAURUSSTAMPEDE].GetCenter().GetDistanceTo(pps[THESAURUSHOUSING].GetCenter());
                                if (THESAURUSACRIMONIOUS < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
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
                                        if (fds.Count > THESAURUSSTAMPEDE)
                                        {
                                            var wps = wpsf(wlinesGeo);
                                            floorDrainWrappingPipeD[label] = wps.Count;
                                            foreach (var fd in fds)
                                            {
                                                if (sfdsf(fd).Any())
                                                {
                                                    drData.HasSideFloorDrain.Add(label);
                                                }
                                                else
                                                {
                                                    drData.HasNonSideFloorDrain.Add(label);
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
                    var _wlinesGeos = wlinesGeos.Where(x => _cpsf(x).Count > THESAURUSSTAMPEDE).ToList();
                    var _wlinesGeosf = F(_wlinesGeos);
                    var gs = GeoFac.GroupGeometries(item.CondensePipes.Concat(_wlinesGeos).ToList());
                    List<Geometry> GetNearAringMachines(Geometry cp)
                    {
                        return F(item.AiringMachine_Vertical.Concat(item.AiringMachine_Hanging).Except(ok_airing_machines).ToList())(cp.Envelope.Buffer(THESAURUSINHERIT));
                    }
                    foreach (var g in gs)
                    {
                        var hs = new HashSet<Geometry>(item.CondensePipes.Except(ok_cps));
                        if (hs.Count == THESAURUSSTAMPEDE) continue;
                        var cps = g.Where(pl => hs.Contains(pl)).ToList();
                        var wlines = g.Where(pl => _wlinesGeos.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        var pipes = pipesf(G(cps.Cast<Polygon>().Select(x => x.Shell).Concat(wlines)));
                        if (pipes.Count == THESAURUSHOUSING)
                        {
                            var pipe = pipes[THESAURUSSTAMPEDE];
                            var label = getLabel(pipe);
                            if (label != null)
                            {
                                if (cps.Count == THESAURUSPERMUTATION)
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
                                if (cps.Count == THESAURUSHOUSING)
                                {
                                    var _cps = F(item.CondensePipes.Except(ok_cps).ToList())(G(_wlinesGeosf(pipe)));
                                    if (_cps.Count == THESAURUSPERMUTATION)
                                    {
                                        var a1 = GeoFac.NearestNeighbourGeometryF(GetNearAringMachines(_cps[THESAURUSSTAMPEDE]))(_cps[THESAURUSSTAMPEDE]);
                                        if (a1 != null) ok_airing_machines.Add(a1);
                                        var a2 = GeoFac.NearestNeighbourGeometryF(GetNearAringMachines(_cps[THESAURUSHOUSING]))(_cps[THESAURUSHOUSING]);
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
                                        var cp = cps[THESAURUSSTAMPEDE];
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
                                if (cps.Count > THESAURUSSTAMPEDE)
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
                            var cps = cpsf(new GLineSegment(pt.OffsetX(-THESAURUSFORMULATE), pt.OffsetX(THESAURUSFORMULATE)).ToLineString()).Except(ok_cps).ToList();
                            if (cps.Count == THESAURUSHOUSING)
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
                        if (IsRainLabel(kv.Value) && (predicate?.Invoke(kv.Value) ?? THESAURUSOBSTINACY))
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
                        if (gbks.Count == THESAURUSHOUSING)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == THESAURUSHOUSING)
                            {
                                drData.ConnectedToGravityWaterBucket.Add(lbDict[pipes[THESAURUSSTAMPEDE]]);
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
                        if (sbks.Count == THESAURUSHOUSING)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == THESAURUSHOUSING)
                            {
                                drData.ConnectedToSideWaterBucket.Add(lbDict[pipes[THESAURUSSTAMPEDE]]);
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
                    var hasRainPortSymbols = new HashSet<string>();
                    {
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
                    var hasDitch = new HashSet<string>();
                    {
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
                            var geo = x.Buffer(MISAPPREHENSIVE); geo.UserData = cadDataMain.WaterSealingWells.IndexOf(x);
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
                        var storey = geoData.Storeys[si].ToPolygon();
                        var gpGeos = GeoFac.CreateEnvelopeSelector(geoData.Groups.Select(GeoFac.CreateGeometry).ToList())(storey);
                        var wlsegs = geoData.WLines.Select(x => x.Extend(-THESAURUSCOMMUNICATION)).Where(x => x.Length > THESAURUSHOUSING).ToList();
                        var vertices = GeoFac.CreateEnvelopeSelector(wlsegs.Select(x => x.StartPoint.ToNTSPoint().Tag(x)).Concat(wlsegs.Select(x => x.EndPoint.ToNTSPoint().Tag(x))).ToList())(storey);
                        var verticesf = GeoFac.CreateIntersectsSelector(vertices);
                        wlsegs = vertices.Select(x => x.UserData).Cast<GLineSegment>().Distinct().ToList();
                        var pts = lbDict.Where(x => IsRainLabel(x.Value)).Select(x => x.Key.GetCenter().ToNTSPoint().Tag(x.Value)).ToList();
                        var ptsf = GeoFac.CreateIntersectsSelector(pts);
                        foreach (var g in geoData.Groups)
                        {
                            void test()
                            {
                                var geo = GeoFac.CreateGeometry(g);
                                var pt = ptsf(geo).FirstOrDefault();
                                if (pt is null) return;
                                var label = pt.UserData as string;
                                foreach (var port in item.RainPortSymbols)
                                {
                                    if (port.Intersects(geo))
                                    {
                                        rainPortIdDict[label] = cadDataMain.RainPortSymbols.IndexOf(port);
                                        hasRainPortSymbols.Add(label);
                                        return;
                                    }
                                }
                                foreach (var ditch in item.Ditches)
                                {
                                    if (ditch.Intersects(geo))
                                    {
                                        ditchIdDict[label] = cadDataMain.Ditches.IndexOf(ditch);
                                        hasDitch.Add(label);
                                        return;
                                    }
                                }
                                foreach (var well in item.WaterWells)
                                {
                                    if (well.Buffer(MISAPPREHENSIVE).Intersects(geo))
                                    {
                                        var waterWellLabel = THESAURUSSPECIFICATION;
                                        waterwellLabelDict[label] = waterWellLabel;
                                        return;
                                    }
                                }
                                var oksegs = GeoFac.GetManyLines(g, skipPolygon: THESAURUSOBSTINACY).ToHashSet();
                                if (oksegs.Count > THESAURUSSTAMPEDE)
                                {
                                    for (int i = THESAURUSSTAMPEDE; i < THESAURUSCOMMUNICATION; i++)
                                    {
                                        var lst = verticesf(oksegs.YieldPoints().Select(x => x.ToGRect(THESAURUSPERMUTATION).ToPolygon()).ToGeometry()).Select(x => x.UserData).Cast<GLineSegment>().Except(oksegs).ToList();
                                        if (lst.Count == THESAURUSSTAMPEDE) break;
                                        oksegs.AddRange(lst);
                                    }
                                    var wlgeo = oksegs.Select(x => x.ToLineString()).ToGeometry();
                                    foreach (var port in item.RainPortSymbols)
                                    {
                                        if (port.Intersects(wlgeo))
                                        {
                                            rainPortIdDict[label] = cadDataMain.RainPortSymbols.IndexOf(port);
                                            hasRainPortSymbols.Add(label);
                                            return;
                                        }
                                    }
                                    foreach (var ditch in item.Ditches)
                                    {
                                        if (ditch.Intersects(wlgeo))
                                        {
                                            ditchIdDict[label] = cadDataMain.Ditches.IndexOf(ditch);
                                            hasDitch.Add(label);
                                            return;
                                        }
                                    }
                                    for (double bufSize = THESAURUSSTAMPEDE; bufSize < QUOTATIONWITTIG; bufSize += HYPERDISYLLABLE)
                                    {
                                        foreach (var well in item.WaterWells)
                                        {
                                            if (well.Buffer(bufSize).Intersects(wlgeo))
                                            {
                                                var waterWellLabel = THESAURUSSPECIFICATION;
                                                waterwellLabelDict[label] = waterWellLabel;
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            test();
                        }
                    }
                    {
                        void collect(Func<Geometry, List<Geometry>> waterWellsf, Func<Geometry, string> getWaterWellLabel, Func<Geometry, int> getWaterWellId)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                            foreach (var wlinesGeo in wlinesGeos)
                            {
                                var waterWells = waterWellsf(wlinesGeo);
                                if (waterWells.Count == THESAURUSHOUSING)
                                {
                                    var waterWell = waterWells[THESAURUSSTAMPEDE];
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
                            var waterWells = spacialIndex.ToList(geoData.WaterWells).Select(x => x.Expand(THESAURUSDOMESTIC).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterWellLabels[spacialIndex[waterWells.IndexOf(waterWell)]], waterWell => spacialIndex[waterWells.IndexOf(waterWell)]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var radius = THESAURUSACRIMONIOUS;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterWells);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(SUPERLATIVENESS, INTRAVASCULARLY)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterWells).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= THESAURUSDICTATORIAL)
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
                                var _pts = ptsf(wp.Buffer(THESAURUSENTREPRENEUR));
                                if (_pts.Count > THESAURUSSTAMPEDE)
                                {
                                    var kv = geoData.WrappingPipeRadius[pts.IndexOf(_pts[THESAURUSSTAMPEDE])];
                                    var radiusText = kv.Value;
                                    if (string.IsNullOrWhiteSpace(radiusText)) radiusText = THESAURUSCROUCH;
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
                            return THESAURUSITEMIZE;
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
                            var m = Regex.Match(kv.Value, THESAURUSANNALS);
                            if (m.Success)
                            {
                                var floor = m.Groups[THESAURUSHOUSING].Value;
                                var pipe = kv.Key;
                                var pipes = getOkPipes().ToList();
                                var wlines = wlinesf(pipe);
                                if (wlines.Count == THESAURUSHOUSING)
                                {
                                    foreach (var pp in F(pipes)(wlines[THESAURUSSTAMPEDE]))
                                    {
                                        drData.RoofWaterBuckets.Add(new KeyValuePair<string, string>(lbDict[pp], floor));
                                    }
                                }
                                continue;
                            }
                        }
                        if (kv.Value.Contains(THESAURUSPRECOCIOUS))
                        {
                            var floor = THESAURUSSECRETE;
                            var pipe = kv.Key;
                            var pipes = getOkPipes().ToList();
                            var wlines = wlinesf(pipe);
                            if (wlines.Count == THESAURUSHOUSING)
                            {
                                foreach (var pp in F(pipes)(wlines[THESAURUSSTAMPEDE]))
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
                    if (item.WaterWells.Count + item.RainPortSymbols.Count + item.WaterSealingWells.Count + item.WaterPorts.Count > THESAURUSSTAMPEDE)
                    {
                        var ok_fds = new HashSet<Geometry>();
                        var wlinesf = F(wlinesGeos);
                        var alone_fds = new HashSet<Geometry>();
                        var side = new MultiPoint(geoData.SideFloorDrains.Select(x => x.ToNTSPoint()).ToArray());
                        var fdsf = F(item.FloorDrains.Where(x => !x.Intersects(side)).ToList());
                        var aloneFloorDrainInfos = new List<AloneFloorDrainInfo>();
                        var bufSize = THESAURUSTROUPE;
                        foreach (var ditch in item.Ditches)
                        {
                            foreach (var wline in wlinesf(ditch.EnvelopeInternal.ToGRect().Expand(MISAPPREHENSIVE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > THESAURUSSTAMPEDE)
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
                                if (fds.Count > THESAURUSSTAMPEDE)
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
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(MISAPPREHENSIVE).ToPolygon()))
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
                                if (fds.Count > THESAURUSSTAMPEDE)
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
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(MISAPPREHENSIVE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > THESAURUSSTAMPEDE)
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
                {
                    var wlinesGeosf = GeoFac.CreateIntersectsSelector(wlinesGeos);
                    var wpst = GeoFac.CreateIntersectsTester(item.WrappingPipes);
                    foreach (var kv in lbDict)
                    {
                        if (!IsRainLabel(kv.Value)) continue;
                        if (wlinesGeosf(kv.Key).Any(x => wpst(x)))
                        {
                            drData.HasWrappingPipe.Add(kv.Value);
                        }
                    }
                }
                exItem.LabelDict = lbDict.Select(x => new Tuple<Geometry, string>(x.Key, x.Value)).ToList();
                {
                    var wpsf = GeoFac.CreateIntersectsSelector(item.WrappingPipes);
                    var vpsf = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                    foreach (var wl in wlinesGeos)
                    {
                        var vps = vpsf(wl);
                        foreach (var vp in vps)
                        {
                            lbDict.TryGetValue(vp, out string lb);
                            if (IsRainLabel(lb))
                            {
                                var wps = wpsf(wl);
                                var srcPt = vp.GetCenter().ToNTSPoint();
                                if (wps.Count > THESAURUSSTAMPEDE)
                                {
                                    var wppts = geoData.WrappingPipeRadius.Select(x => x.Key.ToNTSPoint().Tag(x.Value)).ToList();
                                    var pts = GeoFac.CreateIntersectsSelector(wppts)(wps.ToGeometry());
                                    if (pts.Count > THESAURUSSTAMPEDE)
                                    {
                                        var text = pts.FindByMax(x => x.Distance(srcPt)).UserData as string;
                                        drData.WrappingPipeRadius220115.Add(new KeyValuePair<string, string>(lb, text));
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                {
                    var gbkst = GeoFac.CreateIntersectsTester(item.GravityWaterBuckets);
                    var sbkst = GeoFac.CreateIntersectsTester(item.SideWaterBuckets.Concat(item._87WaterBuckets).Distinct().ToList());
                    foreach (var kv in lbDict)
                    {
                        if (IsY1L(kv.Value))
                        {
                            var pp = kv.Key;
                            if (gbkst(pp))
                            {
                                drData.TouchGravityWaterBuckets.Add(kv.Value);
                            }
                            else if (sbkst(pp))
                            {
                                drData.TouchSideWaterBuckets.Add(kv.Value);
                            }
                        }
                    }
                }
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
                    for (int i = THESAURUSSTAMPEDE; i < drData.GravityWaterBuckets.Count; i++)
                    {
                        drData.GravityWaterBucketLabels.Add(null);
                    }
                    for (int i = THESAURUSSTAMPEDE; i < drData.SideWaterBuckets.Count; i++)
                    {
                        drData.SideWaterBucketLabels.Add(null);
                    }
                    for (int i = THESAURUSSTAMPEDE; i < drData._87WaterBuckets.Count; i++)
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
                            if (labels.Count == THESAURUSHOUSING && bks.Count == THESAURUSHOUSING)
                            {
                                var lb = labels[THESAURUSSTAMPEDE];
                                var bk = bks[THESAURUSSTAMPEDE];
                                var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSDEPLORE;
                                if (IsWaterBucketLabel(label))
                                {
                                    if (!label.ToUpper().Contains(PSEUDOSCOPICALLY))
                                    {
                                        var pt = lb.GetCenter().OffsetY(-QUOTATIONWITTIG);
                                        DrawRectLazy(GRect.Create(pt, QUOTATIONWITTIG, THESAURUSHYPNOTIC));
                                        var lst = labelsf(GRect.Create(pt, QUOTATIONWITTIG, THESAURUSHYPNOTIC).ToPolygon());
                                        lst.Remove(lb);
                                        foreach (var geo in lst)
                                        {
                                            var _label = geoData.Labels[cadDataMain.Labels.IndexOf(geo)].Text ?? THESAURUSDEPLORE;
                                            if (_label.Contains(PSEUDOSCOPICALLY))
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
                            if (i >= THESAURUSSTAMPEDE)
                            {
                                i = drData.GravityWaterBuckets.IndexOf(geoData.GravityWaterBuckets[i]);
                                if (i >= THESAURUSSTAMPEDE)
                                {
                                    drData.GravityWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain.SideWaterBuckets.IndexOf(kv.Key);
                            if (i >= THESAURUSSTAMPEDE)
                            {
                                i = drData.SideWaterBuckets.IndexOf(geoData.SideWaterBuckets[i]);
                                if (i >= THESAURUSSTAMPEDE)
                                {
                                    drData.SideWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain._87WaterBuckets.IndexOf(kv.Key);
                            if (i >= THESAURUSSTAMPEDE)
                            {
                                i = drData._87WaterBuckets.IndexOf(geoData._87WaterBuckets[i]);
                                if (i >= THESAURUSSTAMPEDE)
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
            _DrawingTransaction.Current.AbleToDraw = THESAURUSOBSTINACY;
            return extraInfo;
        }
        public static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas)
        {
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = THESAURUSHOUSING;
            }
            for (int storeyI = THESAURUSSTAMPEDE; storeyI < cadDatas.Count; storeyI++)
            {
                var item = cadDatas[storeyI];
                DrawGeoData(geoData, cadDataMain, storeyI, item);
            }
        }
        private static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, int storeyI, RainCadData item)
        {
            foreach (var o in item.LabelLines)
            {
                DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = THESAURUSHOUSING;
            }
            foreach (var pl in item.Labels)
            {
                var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                var e = DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                e.ColorIndex = THESAURUSPERMUTATION;
                var _pl = DrawRectLazy(m.Boundary);
                _pl.ColorIndex = THESAURUSPERMUTATION;
            }
            foreach (var o in item.PipeKillers)
            {
                DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(THESAURUSEXCESS, THESAURUSEXCESS, THESAURUSLUMBERING);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)], THESAURUSACRIMONIOUS);
            }
            foreach (var o in item.VerticalPipes)
            {
                DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = INTROPUNITIVENESS;
            }
            foreach (var o in item.FloorDrains)
            {
                DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = SUPERLATIVENESS;
            }
            foreach (var o in item.WLines)
            {
                DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = SUPERLATIVENESS;
            }
            foreach (var o in item.WaterPorts)
            {
                DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = THESAURUSDESTITUTE;
                DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.WaterWells)
            {
                DrawRectLazy(geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ColorIndex = QUOTATIONEDIBLE;
                DrawTextLazy(geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.RainPortSymbols)
            {
                DrawRectLazy(geoData.RainPortSymbols[cadDataMain.RainPortSymbols.IndexOf(o)]).ColorIndex = THESAURUSDESTITUTE;
            }
            foreach (var o in item.WaterSealingWells)
            {
                DrawRectLazy(geoData.WaterSealingWells[cadDataMain.WaterSealingWells.IndexOf(o)]).ColorIndex = THESAURUSDESTITUTE;
            }
            foreach (var o in item.GravityWaterBuckets)
            {
                var r = geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = THESAURUSDESTITUTE;
                Dr.DrawSimpleLabel(r.LeftTop, THESAURUSGLORIOUS);
            }
            foreach (var o in item.SideWaterBuckets)
            {
                var r = geoData.SideWaterBuckets[cadDataMain.SideWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = THESAURUSDESTITUTE;
                Dr.DrawSimpleLabel(r.LeftTop, QUOTATION1CDEVIL);
            }
            foreach (var o in item._87WaterBuckets)
            {
                var r = geoData._87WaterBuckets[cadDataMain._87WaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = THESAURUSDESTITUTE;
                Dr.DrawSimpleLabel(r.LeftTop, QUOTATIONHOFMANN);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)]).ColorIndex = THESAURUSHOUSING;
            }
            foreach (var o in item.CleaningPorts)
            {
                var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                if (INTRAVASCULARLY) DrawGeometryLazy(new GCircle(m, THESAURUSENTREPRENEUR).ToCirclePolygon(THESAURUSDISINGENUOUS), ents => ents.ForEach(e => e.ColorIndex = THESAURUSDESTITUTE));
                DrawRectLazy(GRect.Create(m, DISPENSABLENESS));
            }
            foreach (var o in item.Ditches)
            {
                var m = geoData.Ditches[cadDataMain.Ditches.IndexOf(o)];
                DrawRectLazy(m);
            }
            {
                var cl = Color.FromRgb(THESAURUSDELIGHT, THESAURUSCRADLE, HYPOSTASIZATION);
                foreach (var o in item.WrappingPipes)
                {
                    DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(QUOTATIONEDIBLE, SYNTHLIBORAMPHUS, THESAURUSPRIVATE);
                foreach (var o in item.DLines)
                {
                    DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(THESAURUSDISCOLOUR, INFINITESIMALLY, THESAURUSFIASCO);
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
                    dn = IRRESPONSIBLENESS;
                }
                else
                {
                    dn = IRRESPONSIBLENESS;
                }
            }
            return new WaterBucketItem() { DN = dn, WaterBucketType = bkType, };
        }
        public string GetWaterBucketChName()
        {
            switch (WaterBucketType)
            {
                case WaterBucketEnum.Gravity:
                    return THESAURUSTOPICAL;
                case WaterBucketEnum.Side:
                    return THESAURUSBANDAGE;
                case WaterBucketEnum._87:
                    return THESAURUSCONSERVATION;
                default:
                    return THESAURUSDEPLORE;
            }
        }
        public string GetDisplayString()
        {
            switch (WaterBucketType)
            {
                case WaterBucketEnum.None:
                    throw new System.Exception();
                case WaterBucketEnum.Gravity:
                    return THESAURUSTOPICAL + DN;
                case WaterBucketEnum.Side:
                    return THESAURUSBANDAGE + DN;
                case WaterBucketEnum._87:
                    return THESAURUSCONSERVATION + DN;
                default:
                    throw new System.Exception();
            }
        }
        public override int GetHashCode()
        {
            return THESAURUSSTAMPEDE;
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
        public bool HasWaterWell => WaterWellLabels != null && WaterWellLabels.Count > THESAURUSSTAMPEDE;
        public List<RainGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public bool HasTl => TlLabels != null && TlLabels.Count > THESAURUSSTAMPEDE;
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
            public bool HasSideAndNonSideFloorDrain;
            public WaterBucketItem WaterBucket;
            public override int GetHashCode()
            {
                return THESAURUSSTAMPEDE;
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
                    && this.HasSideAndNonSideFloorDrain == other.HasSideAndNonSideFloorDrain
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
            return THESAURUSSTAMPEDE;
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
                if (s == null) return INTRAVASCULARLY;
                if (s.StartsWith(PSEUDOSCOPICALLY)) return INTRAVASCULARLY;
                if (IsMaybeLabelText(s)) return THESAURUSOBSTINACY;
                return INTRAVASCULARLY;
            }
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Where(x => f(x.Text)).Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(THESAURUSACRIMONIOUS)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-THESAURUSACRIMONIOUS).OffsetY(-PHYSIOLOGICALLY), THESAURUSDICTATORIAL, PHYSIOLOGICALLY);
                var _lineHGs = f1(g.ToPolygon());
                var geo = GeoFac.NearestNeighbourGeometryF(_lineHGs)(bd.Center.ToNTSPoint());
                if (geo == null) continue;
                {
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(HYPERDISYLLABLE, THESAURUSDOMESTIC) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(ASSOCIATIONISTS, THESAURUSDOMESTIC))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(ASSOCIATIONISTS));
                    }
                }
            }
        }
        public static void PreFixGeoData(RainGeoData geoData)
        {
            geoData.FixData();
            foreach (var ct in geoData.Labels)
            {
                ct.Text = ct.Text?.Trim() ?? THESAURUSDEPLORE;
                ct.Boundary = ct.Boundary.Expand(-DISPENSABLENESS);
            }
            {
                var cts = geoData.Labels.Where(x => IsMaybeLabelText(x.Text)).ToList();
                var pts = cts.Select(x => x.Boundary.Center.ToNTSPoint().Tag(x)).ToList();
                var ptsf = GeoFac.CreateIntersectsSelector(pts);
                foreach (var pt in pts)
                {
                    var ct = (CText)pt.UserData;
                    if (ct.Text.Contains(THESAURUSLECHER))
                    {
                        var p1 = ct.Boundary.Center;
                        foreach (var _ct in ptsf(new GRect(p1.OffsetXY(THESAURUSDICTATORIAL, MISAPPREHENSIVE), p1.OffsetY(-MISAPPREHENSIVE)).ToPolygon()).Select(x => x.UserData).Cast<CText>())
                        {
                            if (Regex.IsMatch(_ct.Text, UREDINIOMYCETES))
                            {
                                ct.Text += _ct.Text;
                                _ct.Text = THESAURUSDEPLORE;
                                break;
                            }
                        }
                    }
                }
                geoData.Labels = cts.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            }
            for (int i = THESAURUSSTAMPEDE; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(THESAURUSCOMMUNICATION))
                {
                    geoData.LabelLines[i] = seg.Extend(SUPERLATIVENESS);
                }
                else if (seg.IsVertical(THESAURUSCOMMUNICATION))
                {
                    geoData.LabelLines[i] = seg.Extend(THESAURUSHOUSING);
                }
            }
            for (int i = THESAURUSSTAMPEDE; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(THESAURUSCOMMUNICATION);
            }
            for (int i = THESAURUSSTAMPEDE; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(THESAURUSCOMMUNICATION);
            }
            for (int i = THESAURUSSTAMPEDE; i < geoData.WLines.Count; i++)
            {
                geoData.WLines[i] = geoData.WLines[i].Extend(THESAURUSCOMMUNICATION);
            }
            for (int i = THESAURUSSTAMPEDE; i < geoData.VerticalPipes.Count; i++)
            {
                geoData.VerticalPipes[i] = geoData.VerticalPipes[i].Expand(THESAURUSACRIMONIOUS);
            }
            {
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < THESAURUSFORMULATE && x.Height < THESAURUSFORMULATE).ToList();
            }
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSACRIMONIOUS)).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, THESAURUSACRIMONIOUS))).ToList();
            }
            {
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.GravityWaterBuckets = GeoFac.GroupGeometries(geoData.GravityWaterBuckets.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.RainPortSymbols = GeoFac.GroupGeometries(geoData.RainPortSymbols.Select(x => x.ToPolygon().Buffer(MISAPPREHENSIVE)).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Buffer(-MISAPPREHENSIVE).Envelope.ToGRect()).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(ASSOCIATIONISTS);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.CondensePipes = geoData.CondensePipes.Distinct(cmp).ToList();
                geoData.PipeKillers = geoData.PipeKillers.Distinct(cmp).ToList();
            }
            {
                for (int i = THESAURUSSTAMPEDE; i < geoData.WaterWellLabels.Count; i++)
                {
                    var label = geoData.WaterWellLabels[i];
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        geoData.WaterWellLabels[i] = THESAURUSSPECIFICATION;
                    }
                }
            }
            {
                var okPts = new HashSet<Point2d>(geoData.WrappingPipeRadius.Select(x => x.Key));
                var lbs = geoData.WrappingPipeLabels.Select(x => x.ToPolygon()).ToList();
                var lbsf = GeoFac.CreateIntersectsSelector(lbs);
                var lines = geoData.WrappingPipeLabelLines.Select(x => x.ToLineString()).ToList();
                var gs = GeoFac.GroupLinesByConnPoints(lines, DINOFLAGELLATES);
                foreach (var geo in gs)
                {
                    var segs = GeoFac.GetLines(geo).ToList();
                    var buf = segs.Where(x => x.IsHorizontal(THESAURUSCOMMUNICATION)).Select(x => x.Buffer(HYPERDISYLLABLE)).FirstOrDefault();
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
                    if (nums.Count > THESAURUSHOUSING)
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
                var v = THESAURUSENTREPRENEUR;
                for (int i = THESAURUSSTAMPEDE; i < geoData.WrappingPipes.Count; i++)
                {
                    var wp = geoData.WrappingPipes[i];
                    if (wp.Width > v * THESAURUSPERMUTATION)
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
                    if (ct.Text.StartsWith(THESAURUSSTUTTER))
                    {
                        ct.Text = ct.Text.Substring(INTROPUNITIVENESS);
                    }
                    else if (ct.Text.StartsWith(JUNGERMANNIALES))
                    {
                        ct.Text = ct.Text.Substring(THESAURUSPERMUTATION);
                    }
                    else if (ct.Text.StartsWith(THESAURUSFORMATION))
                    {
                        ct.Text = THESAURUSBLOCKADE + ct.Text.Substring(INTROPUNITIVENESS);
                    }
                }
            }
            {
                var lines = geoData.LabelLines.Where(x => x.IsValid).Select(x => x.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList();
                var segs = new HashSet<GLineSegment>();
                foreach (var lns in GeoFac.GroupGeometries(lines))
                {
                    var _segs = GeoFac.GetManyLines(lns).Select(x => x.Extend(-THESAURUSCOMMUNICATION)).ToList();
                    if (_segs.Count == QUOTATIONEDIBLE)
                    {
                        if (_segs.Where(x => x.IsHorizontal(THESAURUSCOMMUNICATION)).Count() == THESAURUSPERMUTATION)
                        {
                            var pts = _segs.YieldPoints().Select(x => x.ToNTSPoint()).ToList();
                            pts = GeoFac.CreateDisjointSelector(pts)(GeoFac.CreateGeometryEx(_segs.Where(x => x.IsHorizontal(THESAURUSCOMMUNICATION)).Select(x => x.Buffer(THESAURUSCOMMUNICATION, EndCapStyle.Square)).ToList()));
                            if (pts.Select(x => x.ToPoint3d()).Distinct(new Point3dComparer(THESAURUSCOMMUNICATION)).Count() >= THESAURUSHOUSING)
                            {
                                segs.AddRange(GeoFac.GetLines(new MultiLineString(lns.ToArray()).Difference(new MultiPoint(pts.ToArray()).GetCenter().ToGRect(SUPERLATIVENESS).ToPolygon())).Where(x => x.Length > THESAURUSHOUSING));
                            }
                        }
                    }
                    else
                    {
                        segs.AddRange(GeoFac.GetManyLines(lns).Select(x => x.Extend(-THESAURUSCOMMUNICATION)).Where(x => x.Length > THESAURUSHOUSING));
                    }
                }
                geoData.LabelLines = segs.ToList();
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
                    TryUpdateByRange(range, INTRAVASCULARLY);
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
                CadCache.SetCache(CadCache.CurrentFile, PERSPICACIOUSNESS, range);
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
            var storeys = new List<StoreyItem>();
            foreach (var br in brs)
            {
                var info = GetStoreyInfo(br);
                if (geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY)
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
            var t = Regex.Replace(text, UREDINIOMYCETES, THESAURUSDEPLORE, RegexOptions.IgnoreCase);
            t = Regex.Replace(t, QUOTATION3BABOVE, THESAURUSDEPLORE);
            t = Regex.Replace(t, THESAURUSMISTRUST, THESAURUSSPECIFICATION);
            return t;
        }
        public static StoreyItem GetStoreyInfo(BlockReference br)
        {
            var props = br.DynamicBlockReferencePropertyCollection;
            return new StoreyItem()
            {
                StoreyType = GetStoreyType((string)props.GetValue(ADSIGNIFICATION)),
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
            floorStr = floorStr.Replace(THESAURUSMETROPOLIS, THESAURUSPROMINENT).Replace(CHROMATOGRAPHER, NATIONALDEMOKRATISCHE).Replace(THESAURUSASPIRATION, THESAURUSDEPLORE).Replace(STEREOPHOTOGRAMMETRY, THESAURUSDEPLORE).Replace(THESAURUSPOLISH, THESAURUSDEPLORE);
            var hs = new HashSet<int>();
            foreach (var s in floorStr.Split(THESAURUSPROMINENT))
            {
                if (string.IsNullOrEmpty(s)) continue;
                var m = Regex.Match(s, THESAURUSAGITATION);
                if (m.Success)
                {
                    var v1 = int.Parse(m.Groups[THESAURUSHOUSING].Value);
                    var v2 = int.Parse(m.Groups[THESAURUSPERMUTATION].Value);
                    var min = Math.Min(v1, v2);
                    var max = Math.Max(v1, v2);
                    for (int i = min; i <= max; i++)
                    {
                        hs.Add(i);
                    }
                    continue;
                }
                m = Regex.Match(s, THESAURUSSANITY);
                if (m.Success)
                {
                    hs.Add(int.Parse(m.Value));
                }
            }
            hs.Remove(THESAURUSSTAMPEDE);
            return hs.OrderBy(x => x).ToList();
        }
        public static string GetStoreyNumberString(BlockReference br)
        {
            var d = br.ObjectId.GetAttributesInBlockReference(THESAURUSOBSTINACY);
            d.TryGetValue(THESAURUSFLAGRANT, out string ret);
            return ret;
        }
        public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(x => x.BlockTableRecord.IsValid && x.GetEffectiveName() is THESAURUSSTICKY).ToList();
        public static Point2d GetContraPoint(BlockReference br)
        {
            double dx = double.NaN;
            double dy = double.NaN;
            Point2d pt;
            foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
            {
                if (p.PropertyName == QUOTATIONJUMPING)
                {
                    dx = Convert.ToDouble(p.Value);
                }
                else if (p.PropertyName == THESAURUSEXPOSTULATE)
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
                throw new System.Exception(QUOTATIONAMNESTY);
            }
            return pt;
        }
        public static void FixStoreys(List<StoreyItem> storeys)
        {
            var lst1 = storeys.Where(s => s.Numbers.Count == THESAURUSHOUSING).Select(s => s.Numbers[THESAURUSSTAMPEDE]).ToList();
            foreach (var s in storeys.Where(s => s.Numbers.Count > THESAURUSHOUSING).ToList())
            {
                var hs = new HashSet<int>(s.Numbers);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Numbers.Clear();
                s.Numbers.AddRange(hs.OrderBy(i => i));
            }
        }
        public const int THESAURUSHOUSING = 1;
        public const int THESAURUSCOMMUNICATION = 5;
        public const int THESAURUSPERMUTATION = 2;
        public const int THESAURUSSTAMPEDE = 0;
        public const double ELECTROLUMINESCENT = 10e5;
        public const int THESAURUSACRIMONIOUS = 10;
        public const double ASSOCIATIONISTS = .1;
        public const bool INTRAVASCULARLY = false;
        public const int THESAURUSDISINGENUOUS = 36;
        public const bool THESAURUSOBSTINACY = true;
        public const int DISPENSABLENESS = 40;
        public const int THESAURUSDICTATORIAL = 1500;
        public const int SUPERLATIVENESS = 6;
        public const int THESAURUSREPERCUSSION = 4096;
        public const string THESAURUSMARSHY = "空调内机--柜机";
        public const string THESAURUSPROFANITY = "空调内机--挂机";
        public const string DISORGANIZATION = "TCH_PIPE";
        public const string INSTRUMENTALITY = "W-RAIN-PIPE";
        public const int TELEPHOTOGRAPHIC = 35;
        public const string THESAURUSABJURE = "-RAIN-";
        public const string QUOTATIONSTANLEY = "W-BUSH-NOTE";
        public const string THESAURUSDEFAULTER = "W-BUSH";
        public const string THESAURUSINVOICE = "W-RAIN-DIMS";
        public const int HYPERDISYLLABLE = 100;
        public const string DENDROCHRONOLOGIST = "W-RAIN-EQPM";
        public const int THESAURUSINCOMPLETE = 20;
        public const string THESAURUSWINDFALL = "TCH_VPIPEDIM";
        public const string THESAURUSSPECIFICATION = "-";
        public const string THESAURUSDURESS = "TCH_TEXT";
        public const string QUOTATIONSWALLOW = "TCH_EQUIPMENT";
        public const string THESAURUSFACILITATE = "TCH_MTEXT";
        public const string THESAURUSINHARMONIOUS = "TCH_MULTILEADER";
        public const string THESAURUSDEPLORE = "";
        public const string THESAURUSELIGIBLE = "接";
        public const string VICISSITUDINOUS = "排水沟";
        public const string THESAURUSADVENT = "雨水口";
        public const int THESAURUSENTREPRENEUR = 50;
        public const char THESAURUSCONTEND = '|';
        public const string MULTIPROCESSING = "|";
        public const char SUPERREGENERATIVE = '$';
        public const string THESAURUSCOURIER = "$";
        public const string THESAURUSSPECIFIC = "C7C497AC6";
        public const string THESAURUSOSSIFY = "H-AC-1";
        public const string THESAURUSDISAGREEMENT = "H-AC-4";
        public const string THESAURUSINTRICATE = "水封井";
        public const string THESAURUSCEILING = "雨水井编号";
        public const string PRETERNATURALLY = "13#雨水口";
        public const string THESAURUSAMATEUR = "W-DRAIN-1";
        public const string THESAURUSREQUISITION = "$W-DRAIN-1";
        public const string THESAURUSINEXPRESSIBLE = "W-DRAIN-2";
        public const string THESAURUSWONTED = "W-DRAIN-5";
        public const string THESAURUSTAUTOLOGY = "$W-DRAIN-2";
        public const string THESAURUSCONSEQUENCE = "$W-DRAIN-5";
        public const string THESAURUSBATTER = "CYSD";
        public const string THESAURUSPROMONTORY = "重力流雨水斗";
        public const string THESAURUSINTERMENT = "$QYSD";
        public const string THESAURUSINDULGENT = "地漏";
        public const string INCORRESPONDENCE = "DL";
        public const string QUOTATIONCREEPING = @"\$?地漏平面$";
        public const string THESAURUSENTERPRISE = "可见性";
        public const string PHARYNGEALIZATION = "侧排地漏";
        public const string THESAURUSTHOROUGHBRED = "套管";
        public const int POLYOXYMETHYLENE = 1000;
        public const string SUPERINDUCEMENT = "带定位立管";
        public const string THESAURUSURBANITY = "立管编号";
        public const string THESAURUSSPECIMEN = "$LIGUAN";
        public const string THESAURUSMANIKIN = "A$C6BDE4816";
        public const string THESAURUSLANDMARK = "污废合流井编号";
        public const string THESAURUSCONTROVERSY = "W-DRAI-DOME-PIPE";
        public const string CIRCUMCONVOLUTION = "W-RAIN-NOTE";
        public const char THESAURUSHYSTERICAL = 'A';
        public const char CHROMATOGRAPHER = 'B';
        public const string THESAURUSARGUMENTATIVE = "RF";
        public const string ANTHROPOMORPHICALLY = "RF+1";
        public const string THESAURUSSCUFFLE = "RF+2";
        public const string THESAURUSASPIRATION = "F";
        public const string THESAURUSNATIONWIDE = @"^重力型雨水斗(DN\d+)$";
        public const int THESAURUSDOMESTIC = 400;
        public const string THESAURUSCONSUMPTION = "bk对位";
        public const string ARKHIPRESBUTEROS = "重力型雨水斗";
        public const string THESAURUSFLOURISH = "侧入型雨水斗";
        public const string THESAURUSCELEBRATION = "87型雨水斗";
        public const string THESAURUSSECRETE = "楼上";
        public const string THESAURUSREGION = "1F";
        public const string THESAURUSTABLEAU = "-1F";
        public const string UNPICTURESQUENESS = "重力型雨水斗DN100";
        public const string QUOTATIONPARISIAN = "侧入式雨水斗DN100";
        public const string THESAURUSLEGACY = "-0.XX";
        public const string COSTERMONGERING = "W-NOTE";
        public const string THESAURUSJUBILEE = "W-DRAI-EQPM";
        public const string THESAURUSSTRIPED = "W-DRAI-NOTE";
        public const double QUOTATIONLETTERS = 2500.0;
        public const double BALANOPHORACEAE = 5500.0;
        public const double THESAURUSINCOMING = 1800.0;
        public const string THESAURUSPARTNER = "伸顶通气2000";
        public const string THESAURUSINEFFECTUAL = "伸顶通气500";
        public const string HYPERVENTILATION = "本层通气";
        public const string THESAURUSNARCOTIC = "通气帽系统-AI2";
        public const string QUINQUAGENARIAN = "可见性1";
        public const string THESAURUSCONFUSION = "通气帽系统";
        public const string THESAURUSKILTER = "距离1";
        public const string QUOTATIONSPRENGEL = "伸顶通气管";
        public const string THESAURUSDUBIETY = "1000";
        public const int THESAURUSNECESSITOUS = 580;
        public const string THESAURUSSUPERFICIAL = "标高";
        public const int QUOTATIONPITUITARY = 550;
        public const string THESAURUSSHADOWY = "建筑完成面";
        public const int QUOTATIONBASTARD = 1800;
        public const int THESAURUSPERVADE = 121;
        public const int THESAURUSUNEVEN = 1258;
        public const int THESAURUSUNCOMMITTED = 120;
        public const int CONTRADISTINGUISHED = 779;
        public const int COOPERATIVENESS = 1679;
        public const int THESAURUSERRAND = 658;
        public const int THESAURUSQUAGMIRE = 90;
        public const string QUOTATIONSTYLOGRAPHIC = @"^(\-?\d+)\-(\-?\d+)$";
        public const string TETRAIODOTHYRONINE = @"^\-?\d+$";
        public const string MULTINATIONALLY = "±0.00";
        public const double LAUTENKLAVIZIMBEL = 1000.0;
        public const string THESAURUSINFINITY = "0.00";
        public const int THESAURUSDISAGREEABLE = 800;
        public const int QUOTATIONWITTIG = 500;
        public const int THESAURUSMAIDENLY = 1879;
        public const int THESAURUSCAVERN = 180;
        public const int THESAURUSINTRACTABLE = 160;
        public const string ADENOHYPOPHYSIS = "普通地漏无存水弯";
        public const string THESAURUSDISREPUTABLE = "DN25";
        public const int THESAURUSATTACHMENT = 750;
        public const int THESAURUSAPPLICANT = 2076;
        public const int INCOMMODIOUSNESS = 659;
        public const string IRRESPONSIBLENESS = "DN100";
        public const double UNDENOMINATIONAL = 0.0;
        public const int SUBCATEGORIZING = 780;
        public const int THESAURUSFORMULATE = 700;
        public const double QUOTATIONTRANSFERABLE = .0;
        public const int THESAURUSHYPNOTIC = 300;
        public const int THESAURUSREVERSE = 24;
        public const int THESAURUSACTUAL = 9;
        public const int QUOTATIONEDIBLE = 4;
        public const int INTROPUNITIVENESS = 3;
        public const int THESAURUSEXECRABLE = 3600;
        public const int THESAURUSENDANGER = 350;
        public const double THESAURUSUPKEEP = .8;
        public const int THESAURUSSURPRISED = 150;
        public const string THESAURUSILLUMINATION = "150";
        public const string THESAURUSEXECUTIVE = "散排至";
        public const int PHOSPHORYLATION = 125;
        public const int THESAURUSMISUNDERSTANDING = 357;
        public const int THESAURUSINTEND = 2650;
        public const int THESAURUSFLAGSTONE = 1410;
        public const int THESAURUSMAGNETIC = 220;
        public const int THESAURUSSTRIPE = 240;
        public const int THESAURUSFOREGONE = 352;
        public const int ADMINISTRATIVELY = 2895;
        public const int REPRESENTATIONAL = 1400;
        public const int THESAURUSBELLOW = 621;
        public const int DOCTRINARIANISM = 1200;
        public const int THESAURUSINHERIT = 2000;
        public const int THESAURUSDERELICTION = 600;
        public const int ACANTHORHYNCHUS = 479;
        public const int THESAURUSLOITER = 950;
        public const int THESAURUSLITTER = 431;
        public const int THESAURUSCORRECTIVE = 2161;
        public const int PORTMANTOLOGISM = 387;
        public const int THESAURUSDEPLETION = 306;
        public const int THESAURUSMATHEMATICAL = 198;
        public const int MORPHOPHONOLOGY = 353;
        public const int THESAURUSENTIRETY = 1106;
        public const int THESAURUSPILGRIM = 3860;
        public const int QUOTATIONCOLERIDGE = 1050;
        public const int THESAURUSEXPERIMENT = 269;
        public const int DISAFFORESTATION = 1481;
        public const int THESAURUSVARIABLE = 281;
        public const int QUOTATIONZYGOMATIC = 501;
        public const int COMMONPLACENESS = 453;
        public const int THESAURUSLEGATE = 2693;
        public const int MISAPPREHENSIVE = 200;
        public const string QUOTATIONSECOND = "接至排水沟";
        public const int ELECTRONEGATIVE = 173;
        public const int QUOTATIONETHIOPS = 187;
        public const int THESAURUSABLUTION = 1907;
        public const int OTHERWORLDLINESS = 499;
        public const int THESAURUSPRETTY = 1600;
        public const int THESAURUSDIFFICULTY = 360;
        public const int THESAURUSDETERMINED = 130;
        public const int APOPHTHEGGESTHAI = 1070;
        public const int CONSCRIPTIONIST = 650;
        public const int VÖLKERWANDERUNG = 30;
        public const int DISCOURTEOUSNESS = 331;
        public const int OVERWHELMINGNESS = 821;
        public const string INTERNALIZATION = "≥600";
        public const int HYPERCORRECTION = 1280;
        public const int THESAURUSGETAWAY = 450;
        public const int ACANTHOCEPHALANS = 18;
        public const int PHYSIOLOGICALLY = 250;
        public const int THESAURUSNOTORIETY = 3000;
        public const double THESAURUSDISPASSIONATE = .7;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const int THESAURUSLITANY = 2200;
        public const string THESAURUSCAVALIER = ";";
        public const double UNCONSEQUENTIAL = .01;
        public const double THESAURUSGROVEL = 10e6;
        public const string PERSUADABLENESS = "地漏系统";
        public const int PROKELEUSMATIKOS = 745;
        public const string THESAURUSTENACIOUS = "乙字弯";
        public const string THESAURUSAGILITY = "立管检查口";
        public const string THESAURUSSTRINGENT = "套管系统";
        public const string THESAURUSSEQUEL = "防水套管水平";
        public const string THESAURUSGAUCHE = "重力流雨水井编号";
        public const string HELIOCENTRICISM = "666";
        public const string THESAURUSPAGEANTRY = @"^(Y1L|Y2L|NL)(\w*)\-(\d*)([a-zA-Z]*)$";
        public const string THESAURUSJAILER = @"^([^\-]*)\-([A-Za-z])$";
        public const string DEMATERIALISING = ",";
        public const string THESAURUSEXCREMENT = "~";
        public const string THESAURUSCAPRICIOUS = @"^([^\-]*\-[A-Za-z])(\d+)$";
        public const string UNIMPRESSIONABLE = @"^([^\-]*\-)([A-Za-z])(\d+)$";
        public const string VASOCONSTRICTOR = "FloorDrains";
        public const string QUOTATIONALDOUS = "GravityWaterBuckets";
        public const string THESAURUSINSUFFERABLE = "SideWaterBuckets";
        public const string QUOTATIONHOFMANN = "87WaterBuckets";
        public const int THESAURUSDESTITUTE = 7;
        public const int SYNTHLIBORAMPHUS = 229;
        public const int THESAURUSPRIVATE = 230;
        public const int THESAURUSEXCEPTION = 8192;
        public const int THESAURUSEPICUREAN = 8000;
        public const int DINOFLAGELLATES = 15;
        public const int THESAURUSHESITANCY = 60;
        public const string THESAURUSJOBBER = "FromImagination";
        public const string THESAURUSTACKLE = "N";
        public const int THESAURUSLUMBERING = 55;
        public const string THESAURUSCROUCH = "X.XX";
        public const int THESAURUSITEMIZE = 666;
        public const string THESAURUSANNALS = @"接(\d+F)屋面雨水斗";
        public const string THESAURUSPRECOCIOUS = "屋面雨水斗";
        public const string PSEUDOSCOPICALLY = "DN";
        public const int THESAURUSEXCESS = 255;
        public const string THESAURUSGLORIOUS = "GravityWaterBucket";
        public const string QUOTATION1CDEVIL = "SideWaterBucket";
        public const int THESAURUSDELIGHT = 0x91;
        public const int THESAURUSCRADLE = 0xc7;
        public const int HYPOSTASIZATION = 0xae;
        public const int THESAURUSDISCOLOUR = 211;
        public const int INFINITESIMALLY = 213;
        public const int THESAURUSFIASCO = 111;
        public const string THESAURUSTOPICAL = "重力雨水斗";
        public const string THESAURUSBANDAGE = "侧入式雨水斗";
        public const string THESAURUSCONSERVATION = "87雨水斗";
        public const string THESAURUSSTUTTER = "73-";
        public const string JUNGERMANNIALES = "1-";
        public const string THESAURUSFORMATION = "LN-";
        public const string THESAURUSBLOCKADE = "NL-";
        public const string PERSPICACIOUSNESS = "SelectedRange";
        public const string UREDINIOMYCETES = @"DN\d+";
        public const string QUOTATION3BABOVE = @"[^\d\.\-]";
        public const string THESAURUSMISTRUST = @"\d+\-";
        public const string ADSIGNIFICATION = "楼层类型";
        public const char THESAURUSMETROPOLIS = '，';
        public const char THESAURUSPROMINENT = ',';
        public const char NATIONALDEMOKRATISCHE = '-';
        public const string STEREOPHOTOGRAMMETRY = "M";
        public const string THESAURUSPOLISH = " ";
        public const string THESAURUSAGITATION = @"(\-?\d+)-(\-?\d+)";
        public const string THESAURUSSANITY = @"\-?\d+";
        public const string THESAURUSFLAGRANT = "楼层编号";
        public const string THESAURUSSTICKY = "楼层框定";
        public const string QUOTATIONJUMPING = "基点 X";
        public const string THESAURUSEXPOSTULATE = "基点 Y";
        public const string QUOTATIONAMNESTY = "error occured while getting baseX and baseY";
        public const string THESAURUSLECHER = "雨水斗";
        public const string THESAURUSPLUMMET = "重力";
        public const string QUOTATIONSPENSERIAN = "87";
        public const string THESAURUSBRAINY = @"(DN\d+)";
        public const string THESAURUSRESUME = @"^DN\d+$";
        public const string CIRCUMSTANTIARE = "卫生间";
        public const string THESAURUSCIPHER = "主卫";
        public const string PREMILLENNIALIST = "公卫";
        public const string THESAURUSINDIGENT = "次卫";
        public const string THESAURUSFRAMEWORK = "客卫";
        public const string THESAURUSHUMILIATION = "洗手间";
        public const string THESAURUSSTERLING = "卫";
        public const string THESAURUSSCREEN = @"^[卫]\d$";
        public const string THESAURUSPEDESTRIAN = "厨房";
        public const string THESAURUSRECLUSE = "西厨";
        public const string THESAURUSINCONTROVERTIBLE = "厨";
        public const string NATIONALIZATION = "阳台";
        public const string THESAURUSSPECIES = "连廊";
        public const string CHRISTIANIZATION = "Y1L";
        public const string UNPREMEDITATEDNESS = "Y2L";
        public const string THESAURUSFINICKY = "NL";
        public const string THESAURUSUNBEATABLE = "YL";
        public const string THESAURUSANTIDOTE = @"^W\d?L";
        public const string THESAURUSDISSOLVE = @"^F\d?L";
        public const string THESAURUSBEATITUDE = "FL-O";
        public const string AUTOLITHOGRAPHIC = "-0";
        public const string THESAURUSABUNDANT = @"^P\d?L";
        public const string THESAURUSOPTIONAL = @"^T\d?L";
        public const string DIASTEREOISOMER = @"^D\d?L";
        public const string QUOTATIONBREWSTER = "DN50";
        public const int THESAURUSEUPHORIA = 1300;
        public const int INCONSIDERABILIS = 900;
        public const string METACOMMUNICATION = ">1500";
        public const string THESAURUSCONFRONTATION = "地漏平面";
        public const string THESAURUSEMPHASIS = "$TwtSys$00000132";
        public const string THESAURUSPITILESS = "A$C01E86F30";
        public const double THESAURUSTROUPE = 1e6;
        public const string QUOTATIONMALTESE = "沟";
        public const string THESAURUSOVERLY = "侧入雨水斗";
        public const string DISCOMPOSEDNESS = "雨水立管";
        public const string THESAURUSADULTERATE = "冷凝水立管";
        public const int MECHANOCHEMISTRY = 1135;
        public const int INAUSPICIOUSNESS = 665;
        public const int THESAURUSFORESHADOW = 182;
        public const string THESAURUSPROLONG = "侧";
        public const string THESAURUSBANKRUPT = "暗沟";
        public const int THESAURUSPLEASING = 31;
        public const int THESAURUSIMPOSING = 1019;
        public const string INTERROGATORIUS = "阳台立管";
        public const double ALSOLATREUTICAL = 82.8;
        public const double DISSOCIABLENESS = 4.9;
        public const double ALSOMONOSIPHONIC = 3e4;
        public const string CONSTRUCTIONIST = "YIL";
        public const string BROKENHEARTEDNESS = "重";
        public const string PARATHYROIDECTOMY = "地漏-AI";
        public const int THESAURUSCOSTLY = 2047;
        public const int THESAURUSFORBIDDEN = 1247;
        public const int THESAURUSAROUND = 1745;
        public const int CRYSTALLOGRAPHER = 2232;
        public const int COMPANIABLENESS = 113;
        public const int THESAURUSSCINTILLATE = 1231;
        public static bool IsRainLabel(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label) || IsFL0(label);
        }
        public static bool IsWaterBucketLabel(string label)
        {
            return label.Contains(THESAURUSLECHER);
        }
        public static bool IsGravityWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(THESAURUSPLUMMET);
        }
        public static bool IsSideWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) &&
                label.Contains(THESAURUSPROLONG);
        }
        public static bool Is87WaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && !IsGravityWaterBucketLabel(label) && !IsSideWaterBucketLabel(label) && label.Contains(QUOTATIONSPENSERIAN);
        }
        public static string GetDN(string label)
        {
            if (label == null) return null;
            var m = Regex.Match(label, THESAURUSBRAINY, RegexOptions.IgnoreCase);
            if (m.Success) return m.Value;
            return null;
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsRainLabel(label) || label.Contains(THESAURUSLECHER) || label.Contains(THESAURUSADVENT)
                || label.Contains(VICISSITUDINOUS)
                || label.Contains(THESAURUSBANKRUPT)
                || label.Contains(QUOTATIONMALTESE)
                || Regex.IsMatch(label.Trim(), THESAURUSRESUME);
        }
        public static bool IsToilet(string roomName)
        {
            var roomNameContains = new List<string>
            {
                CIRCUMSTANTIARE,THESAURUSCIPHER,PREMILLENNIALIST,
                THESAURUSINDIGENT,THESAURUSFRAMEWORK,THESAURUSHUMILIATION,
            };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            if (roomName.Equals(THESAURUSSTERLING))
                return THESAURUSOBSTINACY;
            return Regex.IsMatch(roomName, THESAURUSSCREEN);
        }
        public static bool IsKitchen(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSPEDESTRIAN, THESAURUSRECLUSE };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            if (roomName.Equals(THESAURUSINCONTROVERTIBLE))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsBalcony(string roomName)
        {
            var roomNameContains = new List<string> { NATIONALIZATION };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsCorridor(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSSPECIES };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsY1L(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(CHRISTIANIZATION) || label.StartsWith(CONSTRUCTIONIST);
        }
        public static bool IsY2L(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(UNPREMEDITATEDNESS);
        }
        public static bool IsNL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(THESAURUSFINICKY);
        }
        public static bool IsYL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return label.StartsWith(THESAURUSUNBEATABLE);
        }
        public static bool IsWL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSANTIDOTE);
        }
        public static bool IsFL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSDISSOLVE);
        }
        public static bool IsFL0(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            if (label == THESAURUSBEATITUDE) return THESAURUSOBSTINACY;
            return IsFL(label) && label.Contains(AUTOLITHOGRAPHIC);
        }
        public static bool IsPL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSABUNDANT);
        }
        public static bool IsTL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSOPTIONAL);
        }
        public static bool IsDL(string label)
        {
            if (string.IsNullOrEmpty(label)) return INTRAVASCULARLY;
            return Regex.IsMatch(label, DIASTEREOISOMER);
        }
    }
    public class Point3dComparer : IEqualityComparer<Point3d>
    {
        Tolerance tol;
        public Point3dComparer(double tol)
        {
            this.tol = new Tolerance(tol, tol);
        }
        public bool Equals(Point3d x, Point3d y)
        {
            return x.IsEqualTo(y, tol);
        }
        public int GetHashCode(Point3d obj)
        {
            return THESAURUSSTAMPEDE;
        }
    }
    public class BlockInfo
    {
        public string LayerName;
        public string BlockName;
        public Point3d BasePoint;
        public double Rotate;
        public double Scale;
        public Dictionary<string, string> PropDict;
        public Dictionary<string, object> DynaDict;
        public BlockInfo(string blockName, string layerName, Point3d basePoint)
        {
            this.LayerName = layerName;
            this.BlockName = blockName;
            this.BasePoint = basePoint;
            this.PropDict = new Dictionary<string, string>();
            this.DynaDict = new Dictionary<string, object>();
            this.Rotate = THESAURUSSTAMPEDE;
            this.Scale = THESAURUSHOUSING;
        }
    }
    public class LineInfo
    {
        public GLineSegment Line;
        public string LayerName;
        public LineInfo(GLineSegment line, string layerName)
        {
            this.Line = line;
            this.LayerName = layerName;
        }
    }
    public class PriorityQueue : IDisposable
    {
        Queue<Action>[] queues;
        public PriorityQueue(int queuesCount)
        {
            queues = new Queue<Action>[queuesCount];
            for (int i = THESAURUSSTAMPEDE; i < queuesCount; i++)
            {
                queues[i] = new Queue<Action>();
            }
        }
        public void Dispose()
        {
            Execute();
        }
        public void Enqueue(int priority, Action f)
        {
            queues[priority].Enqueue(f);
        }
        public void Execute()
        {
            while (queues.Any(queue => queue.Count > THESAURUSSTAMPEDE))
            {
                foreach (var queue in queues)
                {
                    if (queue.Count > THESAURUSSTAMPEDE)
                    {
                        queue.Dequeue()();
                        break;
                    }
                }
            }
        }
    }
    public class ThwSDStoreyItem
    {
        public string Storey;
        public GRect Boundary;
        public List<string> VerticalPipes;
    }
}