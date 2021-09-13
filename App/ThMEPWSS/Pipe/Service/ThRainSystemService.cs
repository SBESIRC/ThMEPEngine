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
    using ThMEPWSS.ReleaseNs;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using static ThMEPWSS.Assistant.DrawUtils;
    using static THRainService;
    using ThMEPEngineCore.Model.Common;
    using NetTopologySuite.Operation.Buffer;
    public static class TempExts
    {
        public static Point2d ToPoint2d(this Point pt)
        {
            return new Point2d(pt.X, pt.Y);
        }
    }
    public class TempGeoFac
    {
        public static IEnumerable<GLineSegment> GetMinConnSegs(List<LineString> g1, List<LineString> g2)
        {
            var lines1 = g1.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).Distinct().ToList();
            var lines2 = g2.SelectMany(x => GeoFac.GetLines(x)).Select(x => x.ToLineString()).Distinct().ToList();
            if (lines1.Count > BATHYDRACONIDAE && lines2.Count > BATHYDRACONIDAE)
            {
                var dis = TempGeoFac.GetMinDis(lines1, lines2, out LineString ls1, out LineString ls2);
                if (dis > BATHYDRACONIDAE && ls1 != null && ls2 != null)
                {
                    foreach (var seg in TempGeoFac.TryExtend(GeoFac.GetLines(ls1).First(), GeoFac.GetLines(ls2).First(), THESAURUSDISRUPTION))
                    {
                        if (seg.IsValid) yield return seg;
                    }
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
        public static IEnumerable<GLineSegment> TryExtend(GLineSegment s1, GLineSegment s2, double extend)
        {
            var pt = s1.Extend(extend).ToLineString().Intersection(s2.Extend(extend).ToLineString()) as Point;
            if (pt != null)
            {
                var bf1 = s1.ToLineString().Buffer(THESAURUSCOUNCIL);
                var bf2 = s2.ToLineString().Buffer(THESAURUSCOUNCIL);
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
        public static IEnumerable<GLineSegment> GetMinConnSegs(List<GLineSegment> segs)
        {
            if (segs.Count <= PIEZOELECTRICAL) yield break;
            var geos = segs.Select(x => x.Extend(DOLICHOCEPHALOUS).ToLineString()).ToList();
            var gs = GeoFac.GroupGeometries(geos);
            if (gs.Count >= TEREBINTHINATED)
            {
                for (int i = BATHYDRACONIDAE; i < gs.Count - PIEZOELECTRICAL; i++)
                {
                    foreach (var seg in GetMinConnSegs(gs[i], gs[i + PIEZOELECTRICAL]))
                    {
                        yield return seg;
                    }
                }
            }
        }
    }
    public class RainGeoData
    {
        public RainGeoData Clone()
        {
            return (RainGeoData)MemberwiseClone();
        }
        public List<GLineSegment> LabelLines;
        public List<CText> Labels;
        public List<string> WaterWellLabels;
        public List<GRect> PipeKillers;
        public List<GRect> VerticalPipes;
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
                for (int i = BATHYDRACONIDAE; i < WaterWells.Count; i++)
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
        public List<GRect> Storeys;
        public List<string> WaterPortLabels;
        public List<Point2d> SideFloorDrains;
        public List<GLineSegment> DLines;
        public List<GRect> SideWaterBuckets;
        public List<GRect> WaterSealingWells;
        public List<GRect> WaterPorts;
        public List<Point2d> CleaningPorts;
        public List<GRect> WrappingPipes;
        public List<GRect> RainPortSymbols;
        public List<Point2d> StoreyContraPoints;
        public List<GLineSegment> WLines;
        public List<GLineSegment> WrappingPipeLabelLines;
        public List<CText> WrappingPipeLabels;
        public List<GRect> FloorDrains;
        public List<GRect> AiringMachine_Hanging;
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
        public List<GRect> CondensePipes;
        public List<GRect> _87WaterBuckets;
        public List<GLineSegment> VLines;
        public List<GRect> WaterWells;
        public List<KeyValuePair<Point2d, string>> WrappingPipeRadius;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> AiringMachine_Vertical;
        public RainGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<RainGeoData>();
        }
        public List<GRect> Ditches;
    }
    public class AloneFloorDrainInfo
    {
        public bool IsSideFloorDrain;
        public string WaterWellLabel;
    }
    public class RainCadData
    {
        private static Func<GLineSegment, Geometry> ConvertLabelLinesF(int bfSize)
        {
            return x => x.Buffer(bfSize);
        }
        public static Func<Point2d, Point> ConvertSideFloorDrains()
        {
            return x => x.ToNTSPoint();
        }
        public List<Geometry> Ditches;
        private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
        {
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(THESAURUSADMISSION);
        }
        public List<Geometry> GravityWaterBuckets;
        public List<Geometry> AiringMachine_Hanging;
        public List<Geometry> RainPortSymbols;
        public List<Geometry> SideWaterBuckets;
        public static Func<GLineSegment, LineString> ConvertDLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<Point2d, Polygon> ConvertCleaningPortsF()
        {
            return x => new GCircle(x, DISCOMFORTABLENESS).ToCirclePolygon(THESAURUSADMISSION);
        }
        public List<Geometry> CleaningPorts;
        public static Func<GLineSegment, LineString> ConvertLabelLinesF()
        {
            return x => x.Extend(THESAURUSCOUNCIL).ToLineString();
        }
        public List<Geometry> LabelLines;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterWells;
        public List<Geometry> CondensePipes;
        private static Func<GRect, Polygon> ConvertWaterPortsF()
        {
            return x => x.ToPolygon();
        }
        public List<Geometry> DLines;
        public static Func<GRect, Polygon> ConvertFloorDrainsF()
        {
            return x => x.ToPolygon();
        }
        public List<Geometry> Labels;
        public List<Geometry> AiringMachine_Vertical;
        public List<Geometry> _87WaterBuckets;
        public static Func<GRect, Polygon> ConvertVerticalPipesF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GLineSegment, LineString> ConvertWLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(PRAETERNATURALIS).ToCirclePolygon(PARALLELOGRAMMIC);
        }
        private static Func<GRect, Polygon> ConvertWrappingPipesF()
        {
            return x => x.ToPolygon();
        }
        public List<Geometry> WLines;
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
            o.FloorDrains.AddRange(data.FloorDrains.Select(x => x.ToGCircle(THESAURUSESPECIALLY).ToCirclePolygon(THESAURUSADMISSION, THESAURUSNEGATIVE)));
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
        public List<Geometry> Storeys;
        public RainCadData Clone()
        {
            return (RainCadData)MemberwiseClone();
        }
        public List<Geometry> SideFloorDrains;
        public List<Geometry> WaterSealingWells;
        public List<Geometry> VerticalPipes;
        public List<RainCadData> SplitByStorey()
        {
            var lst = new List<RainCadData>(this.Storeys.Count);
            if (this.Storeys.Count == BATHYDRACONIDAE) return lst;
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
        public List<Geometry> VLines;
        public static Func<GLineSegment, LineString> ConvertVLinesF()
        {
            return x => x.ToLineString();
        }
        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(THESAURUSREGARDING);
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
        public List<Geometry> WrappingPipes;
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
        public List<Geometry> PipeKillers;
        public List<Geometry> WaterPorts;
    }
    public class ThRainSystemServiceGeoCollector3
    {
        List<GRect> pipes => geoData.VerticalPipes;
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        public static string GetEffectiveName(string str)
        {
            str ??= THESAURUSAMENITY;
            var i = str.LastIndexOf(THESAURUSSLOPPY);
            if (i >= BATHYDRACONIDAE && !str.EndsWith(THESAURUSMAJESTY))
            {
                str = str.Substring(i + PIEZOELECTRICAL);
            }
            i = str.LastIndexOf(THESAURUSRECKON);
            if (i >= BATHYDRACONIDAE && !str.EndsWith(THESAURUSMALICE))
            {
                str = str.Substring(i + PIEZOELECTRICAL);
            }
            return str;
        }
        List<GLineSegment> wLines => geoData.WLines;
        List<GRect> gravityWaterBuckets => geoData.GravityWaterBuckets;
        List<GRect> _87WaterBuckets => geoData._87WaterBuckets;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        HashSet<string> 挂式空调内机Names;
        static bool isRainLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSDESCEND);
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, List<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        List<GRect> storeys => geoData.Storeys;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<GLineSegment> labelLines => geoData.LabelLines;
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        List<GRect> pipeKillers => geoData.PipeKillers;
        public void CollectStoreys(CommandContext ctx)
        {
            foreach (var br in GetStoreyBlockReferences(adb))
            {
                var bd = br.Bounds.ToGRect();
                storeys.Add(bd);
                geoData.StoreyContraPoints.Add(GetContraPoint(br));
            }
        }
        public RainGeoData geoData;
        public AcadDatabase adb;
        const int distinguishDiameter = THESAURUSBELIEVE;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> waterWells => geoData.WaterWells;
        List<string> waterWellLabels => geoData.WaterWellLabels;
        bool isInXref;
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, List<GRect> lst)
        {
            if (r.IsValid) reg(fs, r, () => { lst.Add(r); });
        }
        List<GRect> washingMachines => geoData.CondensePipes;
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, List<GLineSegment> lst)
        {
            if (seg.IsValid) reg(fs, seg, () => { lst.Add(seg); });
        }
        List<GLineSegment> dlines => geoData.DLines;
        private void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!IsLayerVisible(entity)) return;
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
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
                        if (_dxfName is THESAURUSDICTIONARY && GetEffectiveLayer(e.Layer) is NEUROTRANSMITTER)
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
                if (entityLayer is THESAURUSDECORATIVE)
                {
                    if (entity is Line line)
                    {
                        if (line.Length > BATHYDRACONIDAE)
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
                            if (ln.Length > BATHYDRACONIDAE)
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
            }
            {
                if (entityLayer is NEUROTRANSMITTER)
                {
                    if (entity is Line line && line.Length > BATHYDRACONIDAE)
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
                    if (entity is Line line && line.Length > BATHYDRACONIDAE)
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
                        if (distinguishDiameter <= c.Radius && c.Radius <= QUOTATIONPATRONAL)
                        {
                            if (isRainLayer(c.Layer))
                            {
                                var r = c.Bounds.ToGRect().TransformBy(matrix);
                                reg(fs, r, pipes);
                                return;
                            }
                        }
                        else if (c.Layer is THESAURUSINSPECTOR && THESAURUSCARTOON < c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                    else if (dxfName is THESAURUSDICTIONARY && entityLayer is THESAURUSINSPECTOR)
                    {
                        var lines = entity.ExplodeToDBObjectCollection().OfType<Line>().Where(x => x.Length > QUOTATIONPATRONAL).ToList();
                        if (lines.Count > BATHYDRACONIDAE)
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
            if (dxfName == QUOTATIONVENICE)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + CONTEMPTIBILITY + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>().Where(IsLayerVisible))
                {
                    if (e is Line line)
                    {
                        if (line.Length > BATHYDRACONIDAE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, labelLines);
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSMANIFEST)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
                        continue;
                    }
                }
                if (ts.Count > BATHYDRACONIDAE)
                {
                    GRect bd;
                    if (ts.Count == PIEZOELECTRICAL) bd = ts[BATHYDRACONIDAE].Bounds.ToGRect();
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
            else if (dxfName == THESAURUSDICTIONARY)
            {
                if (entityLayer is THESAURUSINSPECTOR)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                    {
                        if (c.Radius >= distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, pipes);
                        }
                        else if (THESAURUSCARTOON <= c.Radius && c.Radius < distinguishDiameter)
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, condensePipes);
                            return;
                        }
                    }
                }
                if (entityLayer is NEUROTRANSMITTER)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wLines);
                    return;
                }
            }
            else if (dxfName is THESAURUSMANIFEST)
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
            else if (dxfName is THESAURUSELUCIDATE)
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
            else if (dxfName == THESAURUSMERRIMENT)
            {
                if (entityLayer is THESAURUSDECORATIVE)
                {
                    dynamic o = entity.AcadObject;
                    string UpText = o.UpText;
                    string DownText = o.DownText;
                    var ents = entity.ExplodeToDBObjectCollection();
                    var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                    var points = GeoFac.GetAlivePoints(segs, PIEZOELECTRICAL);
                    var pts = points.Select(x => x.ToNTSPoint()).ToList();
                    points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(PIEZOELECTRICAL)).Select(x => x.Extend(TEREBINTHINATED).Buffer(PIEZOELECTRICAL)).ToList())).Select(pts).ToList(points)).ToList();
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
                    var t = (UpText + DownText) ?? THESAURUSAMENITY;
                    if (t.Contains(THESAURUSINNOCENT) && t.Contains(THESAURUSCONSOLIDATE))
                    {
                        var ents = entity.ExplodeToDBObjectCollection();
                        var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                        var points = GeoFac.GetAlivePoints(segs, PIEZOELECTRICAL);
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(PIEZOELECTRICAL)).Select(x => x.Extend(TEREBINTHINATED).Buffer(PIEZOELECTRICAL)).ToList())).Select(pts).ToList(points)).ToList();
                        if (points.Count > BATHYDRACONIDAE)
                        {
                            var r = new MultiPoint(points.Select(p => p.ToNTSPoint()).ToArray()).Envelope.ToGRect().Expand(QUOTATIONPATRONAL);
                            geoData.Ditches.Add(r);
                        }
                        return;
                    }
                    if (t.Contains(THESAURUSINNOCENT) && t.Contains(THESAURUSINCULCATE))
                    {
                        var ents = entity.ExplodeToDBObjectCollection();
                        var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                        var points = GeoFac.GetAlivePoints(segs, PIEZOELECTRICAL);
                        var pts = points.Select(x => x.ToNTSPoint()).ToList();
                        points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(PIEZOELECTRICAL)).Select(x => x.Extend(TEREBINTHINATED).Buffer(PIEZOELECTRICAL)).ToList())).Select(pts).ToList(points)).ToList();
                        if (points.Count > BATHYDRACONIDAE)
                        {
                            foreach (var pt in points)
                            {
                                geoData.RainPortSymbols.Add(GRect.Create(pt, THESAURUSFORTIFICATION));
                            }
                        }
                        return;
                    }
                }
                {
                    if (!isRainLayer(entityLayer)) return;
                    var colle = entity.ExplodeToDBObjectCollection();
                    {
                        foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSMANIFEST or THESAURUSELUCIDATE).Where(x => isRainLayer(x.Layer)).Where(IsLayerVisible))
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
                        foreach (var seg in colle.OfType<Line>().Where(x => x.Length > BATHYDRACONIDAE).Where(x => isRainLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                        {
                            reg(fs, seg, labelLines);
                        }
                    }
                }
                return;
            }
        }
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        static bool HandleGroupAtCurrentModelSpaceOnly = THESAURUSESPECIALLY;
        HashSet<string> 立式空调内机Names;
        private static bool IsWantedBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return THESAURUSESPECIALLY;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return THESAURUSESPECIALLY;
            }
            if (!blockTableRecord.Explodable)
            {
                return THESAURUSESPECIALLY;
            }
            return THESAURUSNEGATIVE;
        }
        List<GRect> sideWaterBuckets => geoData.SideWaterBuckets;
        HashSet<Handle> ok_group_handles;
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
        public static string GetEffectiveBRName(string brName)
        {
            return GetEffectiveName(brName);
        }
        public static string GetEffectiveLayer(string entityLayer)
        {
            return GetEffectiveName(entityLayer);
        }
        public void CollectEntities()
        {
            {
                var dict = ThMEPWSS.ViewModel.BlockConfigService.GetBlockNameListDict();
                dict.TryGetValue(THESAURUSPERCHANCE, out List<string> lstVertical);
                if (lstVertical != null) this.立式空调内机Names = new HashSet<string>(lstVertical);
                dict.TryGetValue(QUOTATIONIXIONIAN, out List<string> lstHanging);
                if (lstHanging != null) this.挂式空调内机Names = new HashSet<string>(lstHanging);
                this.立式空调内机Names ??= new HashSet<string>();
                this.挂式空调内机Names ??= new HashSet<string>();
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
                        if (dxfName is THESAURUSDICTIONARY && GetEffectiveLayer(entity.Layer) is NEUROTRANSMITTER)
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
                        isInXref = THESAURUSESPECIALLY;
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
        List<CText> cts => geoData.Labels;
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var name = GetEffectiveBRName(br.GetEffectiveName());
                if (name is THESAURUSRUMPLE or THESAURUSJOCULAR || 立式空调内机Names.Contains(name))
                {
                    reg(fs, br.Bounds.ToGRect().TransformBy(matrix), geoData.AiringMachine_Vertical);
                    return;
                }
                if (name is THESAURUSCHAUVINISM || 挂式空调内机Names.Contains(name))
                {
                    reg(fs, br.Bounds.ToGRect().TransformBy(matrix), geoData.AiringMachine_Hanging);
                    return;
                }
                if (name.Contains(QUOTATIONKEELED))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    waterSealingWells.Add(bd);
                    return;
                }
                if (name.Contains(THESAURUSCHIRPY) || name.Contains(THESAURUSREPULSIVE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(CONTEMPTIBILITY) ?? THESAURUSAMENITY;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(lb);
                    });
                    return;
                }
                if (name.ToUpper() is HANDCRAFTSMANSHIP || name.ToUpper().EndsWith(SYNERGISTICALLY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.ToUpper() is THESAURUSDISPENSATION or THESAURUSREDOUBTABLE || name.ToUpper().EndsWith(QUOTATIONVITAMIN) || name.ToUpper().EndsWith(THESAURUSREMEDY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is THESAURUSSUNKEN)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, sideWaterBuckets);
                    return;
                }
                if (name is TRANSUBSTANTIATE)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, gravityWaterBuckets);
                    return;
                }
                if (name.Contains(THESAURUSBLARNEY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, _87WaterBuckets);
                    return;
                }
                if (name.Contains(THESAURUSAGNOSTIC) || name is FERROELECTRICALLY)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, floorDrains);
                    if (Regex.IsMatch(name, THESAURUSPINPOINT, RegexOptions.Compiled))
                    {
                        if (br.IsDynamicBlock)
                        {
                            var props = br.DynamicBlockReferencePropertyCollection;
                            foreach (DynamicBlockReferenceProperty prop in props)
                            {
                                if (prop.PropertyName == THESAURUSCOMPREHEND)
                                {
                                    var propValue = prop.Value.ToString();
                                    {
                                        if (propValue is TRICHOBATRACHUS)
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
                if (name.Contains(THESAURUSSUPPOSITION))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < CONSTRUCTIONISM && bd.Height < CONSTRUCTIONISM)
                        {
                            reg(fs, bd, wrappingPipes);
                        }
                    }
                    return;
                }
                if (isRainLayer(br.Layer))
                {
                    if (name is THESAURUSBALDERDASH or THESAURUSBAPTIZE)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), THESAURUSFORTIFICATION);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name.Contains(THESAURUSLOVING))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is THESAURUSCANDLE || name.Contains(THESAURUSCANDLE))
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
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        List<GRect> waterSealingWells => geoData.WaterSealingWells;
        List<GRect> condensePipes => geoData.CondensePipes;
    }
    public class ThRainSystemServiceGeoCollector2
    {
        List<CText> cts => geoData.Labels;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<GRect> _87WaterBuckets => geoData._87WaterBuckets;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<GRect> gravityWaterBuckets => geoData.GravityWaterBuckets;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> condensePipes => geoData.CondensePipes;
        List<GLineSegment> vlines => geoData.VLines;
        List<GRect> rainPortSymbols => geoData.RainPortSymbols;
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
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        List<GLineSegment> wLines => geoData.WLines;
        List<GRect> waterWells => geoData.WaterWells;
        List<GRect> washingMachines => geoData.CondensePipes;
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
        public RainGeoData geoData;
        List<GRect> storeys => geoData.Storeys;
        List<GRect> pipeKillers => geoData.PipeKillers;
        void handleBlockReference(BlockReference br, Matrix3d matrix)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            {
            }
            {
                var name = br.GetEffectiveName();
                if (name.Contains(THESAURUSCHIRPY) || name.Contains(QUOTATIONADJACENT) || name.Contains(THESAURUSREPULSIVE))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        waterWells.Add(bd);
                        waterWellLabels.Add(br.GetAttributesStrValue(CONTEMPTIBILITY) ?? THESAURUSAMENITY);
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
                if (name is THESAURUSSUNKEN)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        sideWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name is TRANSUBSTANTIATE)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        gravityWaterBuckets.Add(bd);
                        return;
                    }
                }
                if (name.Contains(THESAURUSBLARNEY))
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
                if (name.Contains(THESAURUSAGNOSTIC) || name is FERROELECTRICALLY)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        floorDrains.Add(bd);
                    }
                    return;
                }
                if (name.Contains(THESAURUSSUPPOSITION))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < CONSTRUCTIONISM && bd.Height < CONSTRUCTIONISM)
                        {
                            wrappingPipes.Add(bd);
                        }
                    }
                    return;
                }
                {
                    if (name is THESAURUSBALDERDASH or THESAURUSBAPTIZE)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), THESAURUSFORTIFICATION);
                        pipes.Add(bd);
                        return;
                    }
                    if (name.Contains(THESAURUSLOVING))
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
        public AcadDatabase adb;
        List<GRect> sideWaterBuckets => geoData.SideWaterBuckets;
        List<string> waterWellLabels => geoData.WaterWellLabels;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        static bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsDynamicBlock)
            {
                return THESAURUSESPECIALLY;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return THESAURUSESPECIALLY;
            }
            if (!blockTableRecord.Explodable)
            {
                return THESAURUSESPECIALLY;
            }
            return THESAURUSNEGATIVE;
        }
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GLineSegment> dlines => geoData.DLines;
        void handleEntity(Entity entity, Matrix3d matrix)
        {
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            {
                if (entity.Layer is NEUROTRANSMITTER)
                {
                    if (entity is Line line && line.Length > BATHYDRACONIDAE)
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
                else if (entity.Layer is THESAURUSOBJECTIVELY)
                {
                    if (entity is Line line && line.Length > BATHYDRACONIDAE)
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
                static bool isRainLayer(string layer) => layer is THESAURUSSPELLBOUND or THESAURUSINSPECTOR or THESAURUSCOURTESAN;
                if (isRainLayer(entity.Layer))
                {
                    if (entity is Line line && line.Length > BATHYDRACONIDAE)
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
                        if (distinguishDiameter <= c.Radius && c.Radius <= QUOTATIONPATRONAL)
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
                        else if (c.Layer is THESAURUSINSPECTOR && THESAURUSCARTOON < c.Radius && c.Radius < distinguishDiameter)
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
            if (dxfName == QUOTATIONVENICE)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + CONTEMPTIBILITY + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>())
                {
                    if (e is Line line)
                    {
                        if (line.Length > BATHYDRACONIDAE)
                        {
                            labelLines.Add(line.ToGLineSegment().TransformBy(matrix));
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSMANIFEST)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>());
                        continue;
                    }
                }
                if (ts.Count > BATHYDRACONIDAE)
                {
                    GRect bd;
                    if (ts.Count == PIEZOELECTRICAL) bd = ts[BATHYDRACONIDAE].Bounds.ToGRect();
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
            if (dxfName == THESAURUSDICTIONARY)
            {
                if (entity.Layer is THESAURUSINSPECTOR)
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
                else if (entity.Layer is NEUROTRANSMITTER)
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
            if (dxfName == QUOTATIONPEIRCE)
            {
                var r = entity.Bounds.ToGRect().TransformBy(matrix);
                if (r.IsValid)
                {
                    rainPortSymbols.Add(r);
                }
                return;
            }
            if (dxfName == THESAURUSMANIFEST)
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
            if (dxfName == THESAURUSMERRIMENT)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var ee in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSMANIFEST or THESAURUSELUCIDATE))
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
                    labelLines.AddRange(colle.OfType<Line>().Where(x => x.Length > BATHYDRACONIDAE).Select(x => x.ToGLineSegment().TransformBy(matrix)));
                }
                return;
            }
        }
        const int distinguishDiameter = THESAURUSBELIEVE;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
    }
    public partial class RainDiagram
    {
        public static bool CollectRainData(AcadDatabase adb, out List<StoreysItem> storeysItems, out List<RainDrawingData> drDatas, CommandContext ctx, bool noWL = THESAURUSESPECIALLY)
        {
            CollectRainGeoData(adb, out storeysItems, out RainGeoData geoData, ctx);
            return CreateRainDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static void DrawAiringSymbol(Point2d pt, string name)
        {
            DrawBlockReference(blkName: DISCRIMINATIVELY, basePt: pt.ToPoint3d(), layer: NEUROTRANSMITTER, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(THESAURUSGENTILITY, name);
            });
        }
        public static List<RainGroupedPipeItem> GetRainGroupedPipeItems(List<RainDrawingData> drDatas, List<StoreyInfo> thStoreys, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo)
        {
            otherInfo = new OtherInfo()
            {
                AloneFloorDrainInfos = new List<AloneFloorDrainInfo>(),
            };
            var thwSDStoreys = RainDiagram.CollectStoreys(thStoreys, drDatas);
            var storeysItems = new List<StoreysItem>(drDatas.Count);
            for (int i = BATHYDRACONIDAE; i < drDatas.Count; i++)
            {
                var bd = drDatas[i].Boundary;
                var item = new StoreysItem();
                item.Init();
                foreach (var sd in thwSDStoreys)
                {
                    if (sd.Boundary.EqualsTo(bd, INTERLINGUISTICS))
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
            var minS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Where(x => x > BATHYDRACONIDAE).Min();
            var maxS = storeys.Where(IsNumStorey).Select(GetStoreyScore).Where(x => x > BATHYDRACONIDAE).Max();
            var countS = maxS - minS + PIEZOELECTRICAL;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            var allNumStoreyLabels = allNumStoreys.Select(x => x + PHENYLENEDIAMINE).ToList();
            allRfStoreys = storeys.Where(x => !IsNumStorey(x)).OrderBy(GetStoreyScore).ToList();
            bool existStorey(string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return THESAURUSNEGATIVE;
                }
                return THESAURUSESPECIALLY;
            }
            int getStoreyIndex(string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Contains(storey)) return i;
                }
                return -PIEZOELECTRICAL;
            }
            var waterBucketsInfos = new List<WaterBucketInfo>();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).OrderBy(GetStoreyScore).ToList();
            string getLowerStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i >= PIEZOELECTRICAL) return allStoreys[i - PIEZOELECTRICAL];
                return null;
            }
            string getHigherStorey(string label)
            {
                var i = allStoreys.IndexOf(label);
                if (i < BATHYDRACONIDAE) return null;
                if (i + PIEZOELECTRICAL < allStoreys.Count) return allStoreys[i + PIEZOELECTRICAL];
                return null;
            }
            {
                var toCmp = new List<int>();
                for (int i = BATHYDRACONIDAE; i < allStoreys.Count - PIEZOELECTRICAL; i++)
                {
                    var s1 = allStoreys[i];
                    var s2 = allStoreys[i + PIEZOELECTRICAL];
                    if ((GetStoreyScore(s2) - GetStoreyScore(s1) == PIEZOELECTRICAL) || (GetStoreyScore(s1) == maxS && GetStoreyScore(s2) == GetStoreyScore(THESAURUSINSURANCE)))
                    {
                        toCmp.Add(i);
                    }
                }
                foreach (var j in toCmp)
                {
                    var storey = allStoreys[j];
                    var i = getStoreyIndex(storey);
                    if (i < BATHYDRACONIDAE) continue;
                    var _drData = drDatas[i];
                    var item = storeysItems[i];
                    var higherStorey = getHigherStorey(storey);
                    if (higherStorey == null) continue;
                    var i1 = getStoreyIndex(higherStorey);
                    if (i1 < BATHYDRACONIDAE) continue;
                    var drData = drDatas[i1];
                    var v = drData.ContraPoint - _drData.ContraPoint;
                    var bkExpand = PSYCHOPHYSIOLOGICAL;
                    var gbks = drData.GravityWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var sbks = drData.SideWaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    var _87bks = drData._87WaterBuckets.Select(x => x.OffsetXY(-v)).Select(x => x.Expand(bkExpand)).ToList();
                    if (ShowWaterBucketHitting)
                    {
                        foreach (var bk in gbks.Concat(sbks).Concat(_87bks))
                        {
                            DrawRectLazy(bk);
                            Dr.DrawSimpleLabel(bk.LeftTop, PRESTIDIGITATION);
                        }
                    }
                    var gbkgeos = gbks.Select(x => x.ToPolygon()).ToList();
                    var sbkgeos = sbks.Select(x => x.ToPolygon()).ToList();
                    var _87bkgeos = _87bks.Select(x => x.ToPolygon()).ToList();
                    var gbksf = GeoFac.CreateIntersectsSelector(gbkgeos);
                    var sbksf = GeoFac.CreateIntersectsSelector(sbkgeos);
                    var _87bksf = GeoFac.CreateIntersectsSelector(_87bkgeos);
                    for (int k = BATHYDRACONIDAE; k < _drData.Y1LVerticalPipeRectLabels.Count; k++)
                    {
                        var label = _drData.Y1LVerticalPipeRectLabels[k];
                        var vp = _drData.Y1LVerticalPipeRects[k];
                        {
                            var _gbks = gbksf(vp.ToPolygon());
                            if (_gbks.Count > BATHYDRACONIDAE)
                            {
                                var bk = drData.GravityWaterBucketLabels[gbkgeos.IndexOf(_gbks[BATHYDRACONIDAE])];
                                bk ??= MEGACHIROPTERAN;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _sbks = sbksf(vp.ToPolygon());
                            if (_sbks.Count > BATHYDRACONIDAE)
                            {
                                var bk = drData.SideWaterBucketLabels[sbkgeos.IndexOf(_sbks[BATHYDRACONIDAE])];
                                bk ??= STURZKAMPFFLUGZEUG;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                        {
                            var _8bks = _87bksf(vp.ToPolygon());
                            if (_8bks.Count > BATHYDRACONIDAE)
                            {
                                var bk = drData._87WaterBucketLabels[_87bkgeos.IndexOf(_8bks[BATHYDRACONIDAE])];
                                bk ??= THESAURUSACCUSTOM;
                                waterBucketsInfos.Add(new WaterBucketInfo() { WaterBucket = bk, Pipe = label, Storey = higherStorey });
                                continue;
                            }
                        }
                    }
                }
            }
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    if (storeysItems[i].Labels.Count == PIEZOELECTRICAL)
                    {
                        var storey = storeysItems[i].Labels[BATHYDRACONIDAE];
                        var _s = getHigherStorey(storey);
                        if (_s != null)
                        {
                            var drData = drDatas[i];
                            for (int i1 = BATHYDRACONIDAE; i1 < drData.RoofWaterBuckets.Count; i1++)
                            {
                                var kv = drData.RoofWaterBuckets[i1];
                                if (kv.Value == THESAURUSFESTER)
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
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return THESAURUSESPECIALLY;
            }
            bool hasLong(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.LongTranslatorLabels.Contains(label))
                            {
                                var tmp = storeysItems[i].Labels.Where(IsNumStorey).ToList();
                                if (tmp.Count > PIEZOELECTRICAL)
                                {
                                    var floor = tmp.Select(GetStoreyScore).Max() + PHENYLENEDIAMINE;
                                    if (storey != floor) return THESAURUSESPECIALLY;
                                }
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                {
                    var _storey = getHigherStorey(storey);
                    if (_storey != null)
                    {
                        for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == _storey)
                                {
                                    var drData = drDatas[i];
                                    if (drData.ConnectedToGravityWaterBucket.Contains(label))
                                    {
                                        return THESAURUSNEGATIVE;
                                    }
                                    if (drData.ConnectedToSideWaterBucket.Contains(label))
                                    {
                                        return THESAURUSNEGATIVE;
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
                                    return THESAURUSNEGATIVE;
                                }
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool hasShort(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                                    if (tmp.Count > PIEZOELECTRICAL)
                                    {
                                        var floor = tmp.Select(GetStoreyScore).Max() + PHENYLENEDIAMINE;
                                        if (storey != floor) return THESAURUSESPECIALLY;
                                    }
                                }
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            List<AloneFloorDrainInfo> getAloneFloorDrainInfos()
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
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
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSideFloorDrain.Contains(label)) return THESAURUSNEGATIVE;
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool getIsDitch(string label)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s is THESAURUSDEFILE or THESAURUSSUSTAIN)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSideFloorDrain.Contains(label)) return THESAURUSNEGATIVE;
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            string getWaterWellLabel(string label)
            {
                if (getIsDitch(label)) return null;
                string _getWaterWellLabel(string label, string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                var ret = _getWaterWellLabel(label, THESAURUSDEFILE);
                ret ??= _getWaterWellLabel(label, THESAURUSSUSTAIN);
                return ret;
            }
            int getWaterWellId(string label)
            {
                if (getIsDitch(label)) return -PIEZOELECTRICAL;
                int _getWaterWellId(string label, string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                    return -PIEZOELECTRICAL;
                }
                var ret = _getWaterWellId(label, THESAURUSDEFILE);
                if (ret == -PIEZOELECTRICAL)
                {
                    ret = _getWaterWellId(label, THESAURUSSUSTAIN);
                }
                return ret;
            }
            int getWaterSealingWellId(string label)
            {
                if (getIsDitch(label)) return -PIEZOELECTRICAL;
                int _getWaterSealingWellId(string label, string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                    return -PIEZOELECTRICAL;
                }
                var ret = _getWaterSealingWellId(label, THESAURUSDEFILE);
                if (ret == -PIEZOELECTRICAL)
                {
                    ret = _getWaterSealingWellId(label, THESAURUSSUSTAIN);
                }
                return ret;
            }
            int getRainPortId(string label)
            {
                if (getIsDitch(label)) return -PIEZOELECTRICAL;
                int _getRainPortId(string label, string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                    return -PIEZOELECTRICAL;
                }
                var ret = _getRainPortId(label, THESAURUSDEFILE);
                if (ret == -PIEZOELECTRICAL)
                {
                    ret = _getRainPortId(label, THESAURUSSUSTAIN);
                }
                return ret;
            }
            bool getHasRainPort(string label)
            {
                if (getIsDitch(label)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            return drData.HasRainPortSymbols.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool getHasWaterSealingWell(string label)
            {
                if (getIsDitch(label)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            return drData.HasWaterSealingWell.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool getHasWaterWell(string label)
            {
                if (getIsDitch(label)) return THESAURUSESPECIALLY;
                if (getHasRainPort(label) || getHasWaterSealingWell(label)) return THESAURUSESPECIALLY;
                return getWaterWellLabel(label) != null;
            }
            bool getIsSpreading(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return THESAURUSESPECIALLY;
            }
            bool hasSingleFloorDrainDrainageForRainPort(string label)
            {
                if (IsY1L(label)) return THESAURUSESPECIALLY;
                var id = getRainPortId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForRainPort.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool hasSingleFloorDrainDrainageForWaterSealingWell(string label)
            {
                if (IsY1L(label)) return THESAURUSESPECIALLY;
                var id = getWaterSealingWellId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterSealingWell.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool hasSingleFloorDrainDrainageForWaterWell(string label)
            {
                if (IsY1L(label)) return THESAURUSESPECIALLY;
                var waterWellLabel = getWaterWellLabel(label);
                if (waterWellLabel == null) return THESAURUSESPECIALLY;
                var id = getWaterWellId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForWaterWell.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForRainPort(string label)
            {
                if (!hasSingleFloorDrainDrainageForRainPort(label)) return THESAURUSESPECIALLY;
                var id = getRainPortId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForRainPort.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterWell(label)) return THESAURUSESPECIALLY;
                var id = getWaterWellId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterWell.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell(string label)
            {
                if (!hasSingleFloorDrainDrainageForWaterSealingWell(label)) return THESAURUSESPECIALLY;
                var id = getWaterSealingWellId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            int getDitchId(string label)
            {
                int _getDitchId(string label, string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                    return -PIEZOELECTRICAL;
                }
                var ret = _getDitchId(label, THESAURUSDEFILE);
                if (ret == -PIEZOELECTRICAL)
                {
                    ret = _getDitchId(label, THESAURUSSUSTAIN);
                }
                return ret;
            }
            bool hasSingleFloorDrainDrainageForDitch(string label)
            {
                if (IsY1L(label)) return THESAURUSESPECIALLY;
                var id = getDitchId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.HasSingleFloorDrainDrainageForDitch.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool isFloorDrainShareDrainageWithVerticalPipeForDitch(string label)
            {
                if (!hasSingleFloorDrainDrainageForDitch(label)) return THESAURUSESPECIALLY;
                var id = getDitchId(label);
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.FloorDrainShareDrainageWithVerticalPipeForDitch.Contains(id))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            int getFDCount(string label, string storey)
            {
                if (IsY1L(label)) return BATHYDRACONIDAE;
                int _getFDCount()
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                    return BATHYDRACONIDAE;
                }
                var ret = _getFDCount();
                return ret;
            }
            int getFDWrappingPipeCount(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return BATHYDRACONIDAE;
            }
            bool hasCondensePipe(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return THESAURUSESPECIALLY;
            }
            bool hasBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return THESAURUSESPECIALLY;
            }
            bool hasNonBrokenCondensePipes(string label, string storey)
            {
                if (!hasCondensePipe(label, storey)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return THESAURUSESPECIALLY;
            }
            bool getPlsDrawCondensePipeHigher(string label, string storey)
            {
                if (hasBrokenCondensePipes(label, storey)) return THESAURUSESPECIALLY;
                if (!hasCondensePipe(label, storey)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
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
                return THESAURUSESPECIALLY;
            }
            bool hasOutletlWrappingPipe(string label)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            return drData.OutletWrappingPipeDict.ContainsValue(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            string getOutletWrappingPipeRadius(string label)
            {
                if (!hasOutletlWrappingPipe(label)) return null;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
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
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            string getWaterBucketLabel(string pipe, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.ConnectedToGravityWaterBucket.Contains(pipe)) return MEGACHIROPTERAN;
                            if (drData.ConnectedToSideWaterBucket.Contains(pipe)) return THESAURUSSUSCEPTIBILITY;
                        }
                    }
                }
                foreach (var drData in drDatas)
                {
                    foreach (var kv in drData.RoofWaterBuckets)
                    {
                        if (kv.Value == storey && kv.Key == pipe)
                        {
                            return MEGACHIROPTERAN;
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
            var iRF = allStoreys.IndexOf(THESAURUSINSURANCE);
            var iRF1 = allStoreys.IndexOf(THESAURUSBLACKOUT);
            var iRF2 = allStoreys.IndexOf(HYDROMETALLURGY);
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
                        for (int i = item.Items.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; i--)
                        {
                            if (item.Items[i].Exist)
                            {
                                var _m = item.Items[i];
                                _m.Exist = THESAURUSESPECIALLY;
                                if (Equals(_m, default(RainGroupingPipeItem.ValueItem)))
                                {
                                    if (i < iRF - PIEZOELECTRICAL && i > BATHYDRACONIDAE)
                                    {
                                        item.Items[i] = default;
                                    }
                                    if (i < iRF - PIEZOELECTRICAL)
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
                    for (int i = item.Items.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; i--)
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
                                        item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(BATHYDRACONIDAE, j).Select(k => item.Items[k]).Any(x => x.Exist);
                                    }
                                }
                                item.Items[j] = default;
                            }
                            {
                                for (int j = BATHYDRACONIDAE; j < i; j++)
                                {
                                    item.Hangings[j].WaterBucket = null;
                                }
                                for (int j = i + PIEZOELECTRICAL; j < item.Items.Count; j++)
                                {
                                    item.Items[j] = default;
                                }
                            }
                            break;
                        }
                    }
                    if (item.Items.Count > BATHYDRACONIDAE)
                    {
                        var lst = Enumerable.Range(BATHYDRACONIDAE, item.Items.Count).Where(i => item.Items[i].Exist).ToList();
                        if (lst.Count > BATHYDRACONIDAE)
                        {
                            var maxi = lst.Max();
                            var mini = lst.Min();
                            var hasWaterBucket = item.Hangings.Any(x => x.WaterBucket != null);
                            if (hasWaterBucket && (maxi == iRF || maxi == iRF - PIEZOELECTRICAL))
                            {
                                item.HasLineAtBuildingFinishedSurfice = Enumerable.Range(BATHYDRACONIDAE, iRF).Select(k => item.Items[k]).Any(x => x.Exist);
                            }
                            else
                            {
                                var (ok, m) = item.Items.TryGetValue(iRF);
                                item.HasLineAtBuildingFinishedSurfice = ok && m.Exist && iRF > BATHYDRACONIDAE && item.Items[iRF - PIEZOELECTRICAL].Exist;
                            }
                        }
                    }
                    if (IsNL((item.Label)))
                    {
                        if (!item.Items.TryGet(iRF).Exist)
                            item.HasLineAtBuildingFinishedSurfice = THESAURUSESPECIALLY;
                    }
                    if (iRF >= TEREBINTHINATED)
                    {
                        if (!item.Items[iRF].Exist && item.Items[iRF - PIEZOELECTRICAL].Exist && item.Items[iRF - TEREBINTHINATED].Exist)
                        {
                            var hanging = item.Hangings[iRF - PIEZOELECTRICAL];
                            if (hanging.FloorDrainsCount > BATHYDRACONIDAE && !hanging.HasCondensePipe)
                            {
                                item.Items[iRF - PIEZOELECTRICAL] = default;
                            }
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (iRF >= PIEZOELECTRICAL && iRF1 > BATHYDRACONIDAE)
                    {
                        if (!item.Items[iRF1].Exist && item.Items[iRF].Exist && item.Items[iRF - PIEZOELECTRICAL].Exist)
                        {
                            item.Items[iRF] = default;
                        }
                    }
                    if (item.PipeType == PipeType.Y1L)
                    {
                        if (item.Hangings.All(x => x.WaterBucket is null))
                        {
                            for (int i = item.Items.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; --i)
                            {
                                if (item.Items[i].Exist)
                                {
                                    var hanging = item.Hangings.TryGet(i + PIEZOELECTRICAL);
                                    if (hanging != null)
                                    {
                                        hanging.WaterBucket = WaterBucketItem.TryParse(COMFORTABLENESS);
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
                            item.HasLineAtBuildingFinishedSurfice = THESAURUSNEGATIVE;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                    {
                        var hanging = item.Hangings[i];
                        if (hanging.Storey is THESAURUSDEFILE)
                        {
                            if (item.Items[i].HasShort)
                            {
                                var m = item.Items[i];
                                m.HasShort = THESAURUSESPECIALLY;
                                item.Items[i] = m;
                            }
                        }
                    }
                    for (int i = BATHYDRACONIDAE; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                        if (m.HasLong && m.HasShort)
                        {
                            m.HasShort = THESAURUSESPECIALLY;
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
                        item.OutletType = OutletType.水封井;
                        item.OutletFloor = THESAURUSDEFILE;
                    }
                    else if (getHasRainPort(label))
                    {
                        item.OutletType = OutletType.雨水口;
                        item.OutletFloor = THESAURUSDEFILE;
                    }
                    else if (getHasWaterWell(label))
                    {
                        item.OutletType = OutletType.雨水井;
                        item.OutletFloor = THESAURUSDEFILE;
                    }
                    else
                    {
                        if (testExist(label, THESAURUSDEFILE))
                        {
                            if (getIsSpreading(label, THESAURUSDEFILE))
                            {
                                item.OutletType = OutletType.散排;
                                item.OutletFloor = THESAURUSDEFILE;
                            }
                            else
                            {
                                item.OutletType = OutletType.排水沟;
                                item.OutletFloor = THESAURUSDEFILE;
                            }
                        }
                        else
                        {
                            item.OutletType = OutletType.散排;
                            for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                            {
                                if (item.Items[i].Exist)
                                {
                                    var hanging = item.Hangings[i];
                                    item.OutletFloor = hanging.Storey;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.OutletType == OutletType.散排 && item.OutletFloor == THESAURUSINSURANCE)
                {
                    item.HasLineAtBuildingFinishedSurfice = THESAURUSESPECIALLY;
                }
                if (item.OutletType == OutletType.散排)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == item.OutletFloor)
                        {
                            h.HasCheckPoint = THESAURUSESPECIALLY;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                    {
                        var storey = allStoreys.TryGet(i);
                        if (storey == THESAURUSDEFILE)
                        {
                            var hanging = item.Hangings[i];
                            hanging.HasCheckPoint = THESAURUSNEGATIVE;
                            break;
                        }
                    }
                    for (int i = BATHYDRACONIDAE; i < item.Items.Count; i++)
                    {
                        var m = item.Items[i];
                        if (m.HasShort)
                        {
                            item.Hangings[i].HasCheckPoint = THESAURUSNEGATIVE;
                        }
                        if (m.HasLong)
                        {
                            var h = item.Hangings.TryGet(i + PIEZOELECTRICAL);
                            if (h != null && (i + PIEZOELECTRICAL) != iRF)
                            {
                                h.HasCheckPoint = THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = BATHYDRACONIDAE; i < item.Items.Count; i++)
                {
                    var x = item.Items[i];
                    if (!x.Exist) item.Items[i] = default;
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.FloorDrainsCountAt1F == BATHYDRACONIDAE)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == THESAURUSDEFILE)
                        {
                            item.FloorDrainsCountAt1F = h.FloorDrainsCount;
                            h.FloorDrainsCount = BATHYDRACONIDAE;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (item.OutletType is OutletType.散排)
                {
                    foreach (var h in item.Hangings)
                    {
                        if (h.Storey == item.OutletFloor)
                        {
                            h.HasCheckPoint = THESAURUSESPECIALLY;
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = BATHYDRACONIDAE; i < item.Items.Count; i++)
                {
                    var m = item.Items[i];
                    if (m.HasLong)
                    {
                        var h = item.Hangings[i];
                        if (!h.HasCondensePipe && (item.Hangings.TryGet(i + PIEZOELECTRICAL)?.FloorDrainsCount ?? BATHYDRACONIDAE) == BATHYDRACONIDAE && (item.Hangings.TryGet(i + PIEZOELECTRICAL)?.WaterBucket != null))
                        {
                            h.LongTransHigher = THESAURUSNEGATIVE;
                        }
                    }
                    {
                        var h = item.Hangings[i];
                        if (h.FloorDrainsCount > BATHYDRACONIDAE)
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
                if (item.OutletFloor == THESAURUSDEFILE && item.OutletType != OutletType.散排)
                {
                    item.OutletWrappingPipeRadius ??= THESAURUSEXULTATION;
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                item.FloorDrainsCountAt1F = BATHYDRACONIDAE;
            }
            var pipeGroupItems = new List<RainGroupedPipeItem>();
            var y1lPipeGroupItems = new List<RainGroupedPipeItem>();
            var y2lPipeGroupItems = new List<RainGroupedPipeItem>();
            var nlPipeGroupItems = new List<RainGroupedPipeItem>();
            var ylPipeGroupItems = new List<RainGroupedPipeItem>();
            var fl0PipeGroupItems = new List<RainGroupedPipeItem>();
            foreach (var g in ylGroupingItems.GroupBy(x => x))
            {
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.雨水井).Select(x => x.WaterWellLabel).Distinct().ToList();
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
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.雨水井).Select(x => x.WaterWellLabel).Distinct().ToList();
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
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.雨水井).Select(x => x.WaterWellLabel).Distinct().ToList();
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
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.雨水井).Select(x => x.WaterWellLabel).Distinct().ToList();
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
                var waterWellLabels = g.Where(x => x.OutletType == OutletType.雨水井).Select(x => x.WaterWellLabel).Distinct().ToList();
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
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, REVOLUTIONIZATION, pt);
            SetLabelStylesForRainNote(t);
        }
        public static void GetCadDatas(RainGeoData geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas)
        {
            cadDataMain = RainCadData.Create(geoData);
            cadDatas = cadDataMain.SplitByStorey();
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSNEGATIVE))
            {
                List<StoreyInfo> storeysItems;
                List<RainDrawingData> drDatas;
                if (!CollectRainData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSNEGATIVE)) return;
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo);
                Dispose();
                DrawRainDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys, otherInfo);
                FlushDQ(adb);
            }
        }
        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = THESAURUSOBJECTIVELY;
            ByLayer(line);
        }
        public class ThwSDStoreyItem
        {
            public string Storey;
            public GRect Boundary;
            public List<string> VerticalPipes;
        }
        public static List<ThwSDStoreyItem> CollectStoreys(List<StoreyInfo> thStoreys, List<RainDrawingData> drDatas)
        {
            var wsdStoreys = new List<ThwSDStoreyItem>();
            HashSet<string> GetVerticalPipeNotes(StoreyInfo storey)
            {
                var i = thStoreys.IndexOf(storey);
                if (i < BATHYDRACONIDAE) return new HashSet<string>();
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
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSINSURANCE, Boundary = bd, VerticalPipes = vps });
                                largeRoofVPTexts.AddRange(vps);
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Numbers.ForEach(i => wsdStoreys.Add(new ThwSDStoreyItem() { Storey = i + PHENYLENEDIAMINE, Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = thStoreys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == PIEZOELECTRICAL)
                    {
                        var storey = storeys[BATHYDRACONIDAE];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        {
                            var rf2vps = smallRoofVPTexts.Except(largeRoofVPTexts).ToList();
                            if (rf2vps.Count == BATHYDRACONIDAE)
                            {
                                var rf1Storey = new ThwSDStoreyItem() { Storey = THESAURUSBLACKOUT, Boundary = bd, };
                                wsdStoreys.Add(rf1Storey);
                            }
                            else
                            {
                                var rf1vps = smallRoofVPTexts.Except(rf2vps).ToList();
                                var rf1Storey = new ThwSDStoreyItem() { Storey = THESAURUSBLACKOUT, Boundary = bd, VerticalPipes = rf1vps };
                                wsdStoreys.Add(rf1Storey);
                                wsdStoreys.Add(new ThwSDStoreyItem() { Storey = HYDROMETALLURGY, Boundary = bd, VerticalPipes = rf2vps });
                            }
                        }
                    }
                    else if (storeys.Count == TEREBINTHINATED)
                    {
                        var s1 = storeys[BATHYDRACONIDAE];
                        var s2 = storeys[PIEZOELECTRICAL];
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
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = THESAURUSBLACKOUT, Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThwSDStoreyItem() { Storey = HYDROMETALLURGY, Boundary = bd2, VerticalPipes = vps2 });
                    }
                }
            }
            return wsdStoreys;
        }
        public static void SetRainPipeLineStyle(Line line)
        {
            line.Layer = NEUROTRANSMITTER;
            ByLayer(line);
        }
        public static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }
        private static List<RainDrawingData> _CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            GetCadDatas(geoData, out RainCadData cadDataMain, out List<RainCadData> cadDatas);
            var roomData = RainService.CollectRoomData(adb);
            RainService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out List<RainDrawingData> drDatas, roomData);
            if (noDraw) Dispose();
            return drDatas;
        }
        public static bool Testing;
        public static void DrawStoreyLine(string label, Point2d _basePt, double lineLen)
        {
            var basePt = _basePt.ToPoint3d();
            {
                var line = DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, BATHYDRACONIDAE));
                Dr.SetLabelStylesForWNote(line, dbt);
                DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: basePt.OffsetX(COMMENSURATENESS), layer: THESAURUSGOODNESS, props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, THESAURUSAMENITY } });
            }
            if (label == THESAURUSINSURANCE)
            {
                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, BATHYDRACONIDAE), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, BATHYDRACONIDAE));
                var dbt = DrawTextLazy(THESAURUSVERIFICATION, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, BATHYDRACONIDAE));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
        }
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = THESAURUSSPELLBOUND;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = THESAURUSADVANCEMENT;
                    SetTextStyleLazy(t, THESAURUSEXTEMPORE);
                }
            }
        }
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
        }
        public static bool IsGravityWaterBucketDNText(string text)
        {
            return re.IsMatch(text);
        }
        static readonly Regex re = new Regex(INCONSUMPTIBILIS);
        public static void CollectRainGeoData(AcadDatabase adb, out List<StoreysItem> storeysItems, out RainGeoData geoData, CommandContext ctx)
        {
            var storeys = GetStoreys(adb, ctx);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(adb, geoData, ctx);
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
            List<Vector2d> vecs0 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSCONFECTIONERY - dy + _dy) };
            List<Vector2d> vecs1 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -h1), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy + _dy - h2) };
            List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -h1 - __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy + __dy + _dy - h2) };
            List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -h1 + __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy - __dy + _dy - h2) };
            List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSPROLONG - dy + _dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            List<Vector2d> vecs3 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -h1), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -ANTHROPOMORPHITES - dy + _dy - h2), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -h1 - __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -ANTHROPOMORPHITES - h2 - dy + __dy + _dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -h1 + __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -ANTHROPOMORPHITES - h2 - dy - __dy + _dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            List<Vector2d> vecs4 => vecs1.GetYAxisMirror();
            List<Vector2d> vecs5 => vecs2.GetYAxisMirror();
            List<Vector2d> vecs6 => vecs3.GetYAxisMirror();
            List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
            List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
            List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
            public void Run()
            {
                var db = adb.Database;
                static void DrawSegs(List<GLineSegment> segs) { for (int k = BATHYDRACONIDAE; k < segs.Count; k++) DrawTextLazy(k.ToString(), segs[k].StartPoint); }
                var storeyLines = new List<KeyValuePair<string, GLineSegment>>();
                void _DrawStoreyLine(string storey, Point2d p, double lineLen)
                {
                    DrawStoreyLine(storey, p, lineLen);
                    storeyLines.Add(new KeyValuePair<string, GLineSegment>(storey, new GLineSegment(p, p.OffsetX(lineLen))));
                }
                var vec7 = new Vector2d(-QUOTATIONSORCERER, QUOTATIONSORCERER);
                {
                    var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                    for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        _DrawStoreyLine(storey, bsPt1, lineLen);
                        if (i == BATHYDRACONIDAE && otherInfo.AloneFloorDrainInfos.Count > BATHYDRACONIDAE)
                        {
                            var dome_lines = new List<GLineSegment>(THESAURUSREGARDING);
                            var dome_layer = NEUROTRANSMITTER;
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
                                    if (values.Count == PIEZOELECTRICAL)
                                    {
                                        DrawRainWaterWell(pt.OffsetX(-PSYCHOPHYSIOLOGICAL), values[BATHYDRACONIDAE]);
                                    }
                                    else if (values.Count >= TEREBINTHINATED)
                                    {
                                        var pts = GetBasePoints(pt.OffsetX(-CHLOROFLUOROCARBONS - PSYCHOPHYSIOLOGICAL), TEREBINTHINATED, values.Count, CHLOROFLUOROCARBONS, CHLOROFLUOROCARBONS).ToList();
                                        for (int i = BATHYDRACONIDAE; i < values.Count; i++)
                                        {
                                            DrawRainWaterWell(pts[i], values[i]);
                                        }
                                    }
                                }
                                {
                                    var lst = otherInfo.AloneFloorDrainInfos.Where(x => !x.IsSideFloorDrain).Select(x => x.WaterWellLabel).Distinct().ToList();
                                    if (lst.Count > BATHYDRACONIDAE)
                                    {
                                        {
                                            var line = DrawLineLazy(pt.OffsetX(CONSTRUCTIONISM), pt.OffsetX(-SPAN_X));
                                            Dr.SetLabelStylesForWNote(line);
                                            var p = pt.OffsetY(-COMMENSURATENESS);
                                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBEFRIEND), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE) };
                                            drawDomePipes(vecs.ToGLineSegments(p));
                                            _DrawFloorDrain((p + new Vector2d(THESAURUSDOWNHEARTED, THESAURUSUNAVOIDABLE)).ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY, THESAURUSAMENITY);
                                            {
                                                _DrawRainWaterWells(vecs.GetLastPoint(p), lst.OrderBy(x =>
                                                {
                                                    if (x == CONTEMPTIBILITY) return int.MaxValue;
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
                                    if (lst.Count > BATHYDRACONIDAE)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBEFRIEND), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE) };
                                        drawDomePipes(vecs.ToGLineSegments(pt));
                                        _DrawFloorDrain((pt + new Vector2d(THESAURUSDOWNHEARTED, THESAURUSUNAVOIDABLE)).ToPoint3d(), THESAURUSNEGATIVE, TRICHOBATRACHUS, THESAURUSAMENITY);
                                        {
                                            _DrawRainWaterWells(vecs.GetLastPoint(pt), lst.OrderBy(x =>
                                            {
                                                if (x == CONTEMPTIBILITY) return int.MaxValue;
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
                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                    {
                        Dr.DrawSimpleLabel(basePt.ToPoint2d(), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                    }
                    if (Testing) return;
                    DrawFloorDrain(basePt, leftOrRight, value);
                }
                var gaps = storeyLines.Select(kv =>
                {
                    var geo = kv.Value.Buffer(INTERLINGUISTICS);
                    geo.UserData = kv.Key;
                    return geo;
                }).ToList();
                var gapsf = GeoFac.CreateIntersectsSelector(gaps);
                var storeySpaces = storeyLines.Select(kv =>
                {
                    var seg1 = kv.Value.Offset(BATHYDRACONIDAE, PIEZOELECTRICAL);
                    var seg2 = kv.Value.Offset(BATHYDRACONIDAE, HEIGHT - PIEZOELECTRICAL);
                    var geo = new GRect(seg1.StartPoint, seg2.EndPoint).ToPolygon();
                    geo.UserData = kv.Key;
                    return geo;
                }).ToList();
                var storeySpacesf = GeoFac.CreateIntersectsSelector(storeySpaces);
                for (int j = BATHYDRACONIDAE; j < COUNT; j++)
                {
                    pipeGroupItems.Add(new RainGroupedPipeItem());
                }
                var dx = BATHYDRACONIDAE;
                for (int j = BATHYDRACONIDAE; j < COUNT; j++)
                {
                    var dome_lines = new List<GLineSegment>(THESAURUSREGARDING);
                    var dome_layer = NEUROTRANSMITTER;
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid) dome_lines.Add(seg);
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs)
                    {
                        dome_lines.AddRange(segs.Where(x => x.IsValid));
                    }
                    var linesKillers = new HashSet<Geometry>();
                    var iRF = allStoreys.IndexOf(THESAURUSINSURANCE);
                    var gpItem = pipeGroupItems[j];
                    var shouldDrawAringSymbol = gpItem.PipeType != PipeType.Y1L && (gpItem.PipeType == PipeType.NL ? (viewModel?.Params?.HasAiringForCondensePipe ?? THESAURUSNEGATIVE) : THESAURUSNEGATIVE);
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
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
                            for (int i = runs.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasLongTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSNEGATIVE;
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
                            for (int i = runs.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; i--)
                            {
                                var r = runs[i];
                                if (r == null) continue;
                                if (r.HasShortTranslator)
                                {
                                    if (!flag.HasValue)
                                    {
                                        flag = THESAURUSNEGATIVE;
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
                        for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
                        {
                            arr[i] = new PipeRunLocationInfo() { Visible = THESAURUSNEGATIVE, Storey = allStoreys[i], };
                        }
                        {
                            var tdx = THESAURUSINDEMNIFY;
                            for (int i = start; i >= end; i--)
                            {
                                h0 = THESAURUSADMIRABLE;
                                h1 = CHEMOTROPICALLY;
                                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                                var basePt = bsPt1.OffsetX(OFFSET_X + (j + PIEZOELECTRICAL) * SPAN_X) + new Vector2d(tdx, BATHYDRACONIDAE);
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                var storey = allStoreys[i];
                                if (storey == THESAURUSINSURANCE)
                                {
                                    _dy = ThWSDStorey.RF_OFFSET_Y;
                                }
                                else
                                {
                                    _dy = THESAURUSDISABILITY;
                                }
                                PipeRunLocationInfo drawNormal()
                                {
                                    {
                                        var vecs = vecs0;
                                        var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        arr[i].BasePoint = basePt;
                                        arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                        arr[i].HangingEndPoint = arr[i].EndPoint;
                                        arr[i].Vector2ds = vecs;
                                        arr[i].Segs = segs;
                                        arr[i].RightSegsMiddle = segs.Select(x => x.Offset(DIETHYLSTILBOESTROL, BATHYDRACONIDAE)).ToList();
                                        arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC);
                                    }
                                    {
                                        var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                        arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))));
                                    }
                                    {
                                        var segs = arr[i].RightSegsMiddle.ToList();
                                        segs[BATHYDRACONIDAE] = new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, segs[PIEZOELECTRICAL].StartPoint);
                                        arr[i].RightSegsLast = segs;
                                    }
                                    {
                                        var pt = arr[i].Segs.First().StartPoint.OffsetX(DIETHYLSTILBOESTROL);
                                        var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE))) };
                                        arr[i].RightSegsFirst = segs;
                                        segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, arr[i].EndPoint.OffsetX(DIETHYLSTILBOESTROL)));
                                    }
                                    return arr[i];
                                }
                                if (i == start)
                                {
                                    drawNormal().Visible = THESAURUSESPECIALLY;
                                    continue;
                                }
                                if (run == null)
                                {
                                    drawNormal().Visible = THESAURUSESPECIALLY;
                                    continue;
                                }
                                if (run.HasLongTranslator && gpItem.Hangings[i].LongTransHigher)
                                {
                                    h1 = QUOTATIONPATRONAL;
                                }
                                else if (run.HasLongTranslator && !gpItem.Hangings[i].HasCondensePipe && (gpItem.Hangings.TryGet(i + PIEZOELECTRICAL)?.FloorDrainsCount ?? BATHYDRACONIDAE) == BATHYDRACONIDAE)
                                {
                                    h1 = QUOTATIONPATRONAL;
                                }
                                if (run.HasLongTranslator && run.HasShortTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs3;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSEVINCE);
                                            segs.Add(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[THESAURUSINTELLECT].EndPoint.OffsetXY(-DIETHYLSTILBOESTROL, -DIETHYLSTILBOESTROL)));
                                            segs.Add(new GLineSegment(segs[TEREBINTHINATED].EndPoint, new Point2d(segs[DOLICHOCEPHALOUS].EndPoint.X, segs[TEREBINTHINATED].EndPoint.Y)));
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSINTELLECT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSINTELLECT], new GLineSegment(segs[THESAURUSINTELLECT].StartPoint, segs[BATHYDRACONIDAE].StartPoint) };
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, arr[i].EndPoint.OffsetX(DIETHYLSTILBOESTROL)));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs6;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(-THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))).Offset(PRAETERNATURALIS, BATHYDRACONIDAE));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSEVINCE);
                                            segs.Add(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[THESAURUSEVINCE].StartPoint));
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, arr[i].EndPoint.OffsetX(DIETHYLSTILBOESTROL)));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    arr[i].HangingEndPoint = arr[i].Segs[THESAURUSEVINCE].EndPoint;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs1;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            segs = segs.Take(THESAURUSEVINCE).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[THESAURUSINTELLECT].EndPoint.OffsetXY(-DIETHYLSTILBOESTROL, -DIETHYLSTILBOESTROL))).ToList();
                                            segs.Add(new GLineSegment(segs[TEREBINTHINATED].EndPoint, new Point2d(segs[DOLICHOCEPHALOUS].EndPoint.X, segs[TEREBINTHINATED].EndPoint.Y)));
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSINTELLECT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSINTELLECT], new GLineSegment(segs[THESAURUSINTELLECT].StartPoint, segs[BATHYDRACONIDAE].StartPoint) };
                                            arr[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs4;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))).Offset(PRAETERNATURALIS, BATHYDRACONIDAE));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle;
                                            arr[i].RightSegsLast = segs.Take(THESAURUSEVINCE).YieldAfter(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[DOLICHOCEPHALOUS].StartPoint)).YieldAfter(segs[DOLICHOCEPHALOUS]).ToList();
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
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
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = segs.Select(x => x.Offset(DIETHYLSTILBOESTROL, BATHYDRACONIDAE)).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, segs[TEREBINTHINATED].StartPoint), segs[TEREBINTHINATED] };
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[TEREBINTHINATED].StartPoint, segs[TEREBINTHINATED].EndPoint);
                                            segs[TEREBINTHINATED] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(BATHYDRACONIDAE);
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, r.RightButtom));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs5;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            arr[i].BasePoint = basePt;
                                            arr[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            arr[i].Vector2ds = vecs;
                                            arr[i].Segs = segs;
                                            arr[i].RightSegsMiddle = segs.Select(x => x.Offset(DIETHYLSTILBOESTROL, BATHYDRACONIDAE)).ToList();
                                            arr[i].PlBasePt = arr[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(-THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = arr[i].RightSegsMiddle.First().StartPoint;
                                            arr[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            arr[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, segs[TEREBINTHINATED].StartPoint), segs[TEREBINTHINATED] };
                                        }
                                        {
                                            var segs = arr[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[TEREBINTHINATED].StartPoint, segs[TEREBINTHINATED].EndPoint);
                                            segs[TEREBINTHINATED] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(BATHYDRACONIDAE);
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, r.RightButtom));
                                            arr[i].RightSegsFirst = segs;
                                        }
                                    }
                                    arr[i].HangingEndPoint = arr[i].Segs[BATHYDRACONIDAE].EndPoint;
                                }
                                else
                                {
                                    drawNormal();
                                }
                            }
                        }
                        for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
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
                        var couldHavePeopleOnRoof = viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSNEGATIVE;
                        var hasDrawedAiringSymbol = THESAURUSESPECIALLY;
                        void _DrawAiringSymbol(Point2d basePt)
                        {
                            if (!shouldDrawAringSymbol) return;
                            if (hasDrawedAiringSymbol) return;
                            var showText = THESAURUSNEGATIVE;
                            {
                                var info = infos.FirstOrDefault(x => x.Storey == THESAURUSINSURANCE);
                                if (info != null)
                                {
                                    var pt = info.BasePoint;
                                    if (basePt.Y < pt.Y)
                                    {
                                        showText = THESAURUSESPECIALLY;
                                    }
                                }
                            }
                            DrawAiringSymbol(basePt, couldHavePeopleOnRoof, showText);
                            hasDrawedAiringSymbol = THESAURUSNEGATIVE;
                        }
                        {
                            if (shouldDrawAringSymbol)
                            {
                                if (gpItem.HasLineAtBuildingFinishedSurfice)
                                {
                                    var info = infos.FirstOrDefault(x => x.Storey == THESAURUSINSURANCE);
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
                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, height), new Vector2d(leftOrRight ? -QUOTATIONLUNGEING : QUOTATIONLUNGEING, BATHYDRACONIDAE) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForRainNote(lines.ToArray());
                            var t = DrawTextLazy(text, REVOLUTIONIZATION, segs.Last().EndPoint.OffsetY(THESAURUSFORTIFICATION));
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
                            if (gpItem.OutletType == OutletType.散排)
                            {
                                static void DrawLabel(Point3d basePt, string text, double lineYOffset)
                                {
                                    var height = REVOLUTIONIZATION;
                                    var width = height * THESAURUSFOREMAN * text.Length;
                                    var yd = new YesDraw();
                                    yd.OffsetXY(BATHYDRACONIDAE, lineYOffset);
                                    yd.OffsetX(-width);
                                    var pts = yd.GetPoint3ds(basePt).ToList();
                                    var lines = DrawLinesLazy(pts);
                                    Dr.SetLabelStylesForRainNote(lines.ToArray());
                                    var t = DrawTextLazy(text, height, pts.Last().OffsetXY(THESAURUSFORTIFICATION, THESAURUSFORTIFICATION));
                                    Dr.SetLabelStylesForRainNote(t);
                                }
                                if (gpItem.OutletFloor == storey)
                                {
                                    var basePt = info.EndPoint;
                                    if (storey == THESAURUSINSURANCE) basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                    var seg = new GLineSegment(basePt, basePt.OffsetY(THESAURUSTRAGEDY));
                                    var p = seg.EndPoint;
                                    DrawDimLabel(seg.StartPoint, p, new Vector2d(CONSTRUCTIONISM, BATHYDRACONIDAE), THESAURUSABRIDGE, THESAURUSCOURTESAN);
                                    DrawLabel(p.ToPoint3d(), THESAURUSCONTRADICT + storey, storey == THESAURUSINSURANCE ? -CHEMOTROPICALLY - ThWSDStorey.RF_OFFSET_Y : -CHEMOTROPICALLY);
                                    {
                                        var shadow = THESAURUSAMENITY;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                        {
                                            Dr.DrawSimpleLabel(p, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                        }
                                    }
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var segs = info.DisplaySegs = info.Segs.ToList();
                                        segs[BATHYDRACONIDAE] = new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, segs[BATHYDRACONIDAE].StartPoint.OffsetY(-(segs[BATHYDRACONIDAE].Length - THESAURUSTRAGEDY)));
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
                                        if (storey == THESAURUSINSURANCE)
                                        {
                                            basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        switch (bk.WaterBucketType)
                                        {
                                            case WaterBucketEnum.Gravity:
                                                {
                                                    Dr.DrawGravityWaterBucket(basePt.ToPoint3d());
                                                    Dr.DrawGravityWaterBucketLabel(basePt.OffsetXY(BATHYDRACONIDAE, THESAURUSFIXTURE).ToPoint3d(), bk.GetDisplayString());
                                                }
                                                break;
                                            case WaterBucketEnum.Side:
                                                {
                                                    var relativeYOffsetToStorey = -QUOTATIONEMETIC;
                                                    var pt = basePt.OffsetY(relativeYOffsetToStorey);
                                                    Dr.DrawSideWaterBucket(basePt.ToPoint3d());
                                                    Dr.DrawSideWaterBucketLabel(pt.OffsetXY(-ECHOENCEPHALOGRAM, PROCHLORPERAZINE).ToPoint3d(), bk.GetDisplayString());
                                                }
                                                break;
                                            case WaterBucketEnum._87:
                                                {
                                                    Dr.DrawGravityWaterBucket(basePt.ToPoint3d());
                                                    Dr.DrawGravityWaterBucketLabel(basePt.OffsetXY(BATHYDRACONIDAE, THESAURUSFIXTURE).ToPoint3d(), bk.GetDisplayString());
                                                }
                                                break;
                                            default:
                                                throw new System.Exception();
                                        }
                                    }
                                }
                                {
                                    if (storey == THESAURUSDEFILE)
                                    {
                                        var basePt = info.EndPoint;
                                        var text = gpItem.OutletWrappingPipeRadius;
                                        if (text != null)
                                        {
                                            var p1 = basePt + new Vector2d(-THESAURUSDEFICIT, -THESAURUSSTRETCH);
                                            var p2 = p1.OffsetY(-QUOTATIONCHOROID);
                                            var p3 = p2.OffsetX(THESAURUSBEFRIEND);
                                            var layer = THESAURUSSPELLBOUND;
                                            DrawLine(layer, new GLineSegment(p1, p2));
                                            DrawLine(layer, new GLineSegment(p3, p2));
                                            DrawStoreyHeightSymbol(p3, THESAURUSSPELLBOUND, text);
                                            {
                                                var shadow = THESAURUSAMENITY;
                                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                {
                                                    Dr.DrawSimpleLabel(p3, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
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
                                                if (values.Count == PIEZOELECTRICAL)
                                                {
                                                    DrawRainWaterWell(pt, values[BATHYDRACONIDAE]);
                                                }
                                                else if (values.Count >= TEREBINTHINATED)
                                                {
                                                    var pts = GetBasePoints(pt.OffsetX(-CHLOROFLUOROCARBONS), TEREBINTHINATED, values.Count, CHLOROFLUOROCARBONS, CHLOROFLUOROCARBONS).ToList();
                                                    for (int i = BATHYDRACONIDAE; i < values.Count; i++)
                                                    {
                                                        DrawRainWaterWell(pts[i], values[i]);
                                                    }
                                                }
                                            }
                                            {
                                                var fixY = -THESAURUSBEFRIEND;
                                                var v = new Vector2d(-THESAURUSIMAGINATIVE - PSYCHOPHYSIOLOGICAL, -THESAURUSSUCCINCT + THESAURUSLABYRINTHINE + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                {
                                                    var shadow = THESAURUSAMENITY;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                    {
                                                        Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                    }
                                                }
                                                _DrawRainWaterWells(pt, values);
                                                var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, fixY), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSAPPORTION + THESAURUSTRAGEDY, THESAURUSFORTIFICATION);
                                                        DrawNoteText(THESAURUSSPITEFUL, segs[TEREBINTHINATED].EndPoint + v1);
                                                    }
                                                    drawDomePipes(segs);
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSDEVICE);
                                                        if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                                        {
                                                            if (!gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(CHEMOTROPICALLY).ToPoint3d());
                                                                {
                                                                    var shadow = THESAURUSAMENITY;
                                                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                                    {
                                                                        Dr.DrawSimpleLabel(p, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                DrawWrappingPipe(p.OffsetX(CHEMOTROPICALLY).ToPoint3d());
                                                                {
                                                                    var shadow = THESAURUSAMENITY;
                                                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                                    {
                                                                        Dr.DrawSimpleLabel(p, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            DrawWrappingPipe(p.ToPoint3d());
                                                            {
                                                                var shadow = THESAURUSAMENITY;
                                                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                                {
                                                                    Dr.DrawSimpleLabel(p, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterWell)
                                            {
                                                var fixX = -THESAURUSAPPORTION - THESAURUSCENTRAL;
                                                var fixY = CHLOROFLUOROCARBONS;
                                                var fixV = new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE);
                                                var p = basePt + new Vector2d(THESAURUSMANAGEABLE + fixX, -QUOTATIONCESTUI);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY, THESAURUSAMENITY);
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBREAST + fixY), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSELLING, BATHYDRACONIDAE), new Vector2d(-THESAURUSSCANDAL, KONINGKLIPVISCH) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(p, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBREAST + fixY), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-INTERPRETATIONS - fixX, BATHYDRACONIDAE) };
                                                    var segs = vecs.ToGLineSegments(p + fixV);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(p, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.雨水口)
                                        {
                                            {
                                                var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBEFRIEND), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE) };
                                                var segs = vecs.ToGLineSegments(basePt);
                                                drawDomePipes(segs);
                                                var pt = segs.Last().EndPoint.ToPoint3d();
                                                {
                                                    Dr.DrawRainPort(pt.OffsetX(PSYCHOPHYSIOLOGICAL));
                                                    Dr.DrawRainPortLabel(pt.OffsetX(-THESAURUSFORTIFICATION));
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        var p = segs.Last().EndPoint.OffsetX(THESAURUSDEVICE);
                                                        DrawWrappingPipe(p.ToPoint3d());
                                                    }
                                                    else
                                                    {
                                                    }
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt.ToPoint2d(), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForRainPort)
                                            {
                                                var fixX = -THESAURUSAPPORTION - THESAURUSCENTRAL;
                                                var fixY = CHLOROFLUOROCARBONS;
                                                var fixV = new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE);
                                                var p = basePt + new Vector2d(THESAURUSMANAGEABLE + fixX, -QUOTATIONCESTUI);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY, THESAURUSAMENITY);
                                                var pt = p + fixV;
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForRainPort)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSNOVELIST), new Vector2d(-THESAURUSALCOHOL, -THESAURUSALCOHOL), new Vector2d(-THESAURUSSUNLESS, BATHYDRACONIDAE), new Vector2d(-THESAURUSPACKET, THESAURUSAUSPICIOUS) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSINDOCTRINATE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSBUBBLY, BATHYDRACONIDAE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.水封井)
                                        {
                                            {
                                                var fixY = -THESAURUSBEFRIEND;
                                                var v = new Vector2d(-THESAURUSIMAGINATIVE - PSYCHOPHYSIOLOGICAL, -THESAURUSSUCCINCT + THESAURUSLABYRINTHINE + fixY);
                                                var pt = basePt + v;
                                                var values = gpItem.WaterWellLabels;
                                                DrawWaterSealingWell(pt.OffsetY(-UNCONJECTURABLE));
                                                var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, fixY), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE), };
                                                {
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSAPPORTION + THESAURUSTRAGEDY, THESAURUSFORTIFICATION);
                                                        DrawNoteText(THESAURUSSPITEFUL, segs[TEREBINTHINATED].EndPoint + v1);
                                                    }
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                            }
                                            if (gpItem.HasSingleFloorDrainDrainageForWaterSealingWell)
                                            {
                                                var fixX = -THESAURUSAPPORTION - THESAURUSCENTRAL;
                                                var fixY = CHLOROFLUOROCARBONS;
                                                var fixV = new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE);
                                                var p = basePt + new Vector2d(THESAURUSMANAGEABLE + fixX, -QUOTATIONCESTUI);
                                                _DrawFloorDrain(p.ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY, THESAURUSAMENITY);
                                                var pt = p + fixV;
                                                if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell)
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSNOVELIST), new Vector2d(-THESAURUSALCOHOL, -THESAURUSALCOHOL), new Vector2d(-THESAURUSSUNLESS, BATHYDRACONIDAE), new Vector2d(-THESAURUSPACKET, THESAURUSAUSPICIOUS) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSINDOCTRINATE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSBUBBLY, BATHYDRACONIDAE) };
                                                    var segs = vecs.ToGLineSegments(pt);
                                                    drawDomePipes(segs);
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (gpItem.OutletType == OutletType.排水沟)
                                        {
                                            if (storey != null)
                                            {
                                                {
                                                    if (gpItem.HasOutletWrappingPipe)
                                                    {
                                                        DrawWrappingPipe((basePt + new Vector2d(-CONTRADICTORIES, -THESAURUSSTRETCH)).ToPoint3d());
                                                    }
                                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBEFRIEND), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE) };
                                                    var segs = vecs.ToGLineSegments(basePt);
                                                    drawDomePipes(segs);
                                                    var pt = segs.Last().EndPoint.ToPoint3d();
                                                    Dr.DrawLabel(pt.OffsetX(-THESAURUSFORTIFICATION), CHRONOSTRATIGRAPHIC);
                                                    {
                                                        var v1 = new Vector2d(THESAURUSAPPORTION + THESAURUSTRAGEDY, THESAURUSFORTIFICATION);
                                                        DrawNoteText(THESAURUSSPITEFUL, segs[TEREBINTHINATED].EndPoint + v1);
                                                    }
                                                    {
                                                        var shadow = THESAURUSAMENITY;
                                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                        {
                                                            Dr.DrawSimpleLabel(pt.ToPoint2d(), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                        }
                                                    }
                                                    if (gpItem.HasSingleFloorDrainDrainageForDitch)
                                                    {
                                                        DrawWrappingPipe((basePt + new Vector2d(-CONTRADICTORIES, -THESAURUSSTRETCH - COMMENSURATENESS)).ToPoint3d());
                                                    }
                                                }
                                                if (gpItem.HasSingleFloorDrainDrainageForDitch)
                                                {
                                                    var fixX = -THESAURUSAPPORTION - THESAURUSCENTRAL;
                                                    var fixY = CHLOROFLUOROCARBONS;
                                                    var fixV = new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE);
                                                    var p = basePt + new Vector2d(THESAURUSMANAGEABLE + fixX, -QUOTATIONCESTUI);
                                                    _DrawFloorDrain(p.ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY, THESAURUSAMENITY);
                                                    var pt = p + fixV;
                                                    if (gpItem.IsFloorDrainShareDrainageWithVerticalPipeForDitch)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSNOVELIST), new Vector2d(-THESAURUSALCOHOL, -THESAURUSALCOHOL), new Vector2d(-THESAURUSSUNLESS, BATHYDRACONIDAE), new Vector2d(-THESAURUSPACKET, THESAURUSAUSPICIOUS) };
                                                        var segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        {
                                                            var shadow = THESAURUSAMENITY;
                                                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                            {
                                                                Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSINDOCTRINATE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSBUBBLY, BATHYDRACONIDAE) };
                                                        var segs = vecs.ToGLineSegments(pt);
                                                        drawDomePipes(segs);
                                                        {
                                                            var shadow = THESAURUSAMENITY;
                                                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                                            {
                                                                Dr.DrawSimpleLabel(pt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                _dy = QUOTATIONPATRONAL;
                                                var vecs = vecs0;
                                                var p1 = info.StartPoint;
                                                var p2 = p1 + new Vector2d(BATHYDRACONIDAE, -THESAURUSCONFECTIONERY - dy + _dy);
                                                var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                info.DisplaySegs = segs;
                                                var p = basePt.OffsetY(QUOTATIONPATRONAL);
                                                drawLabel(p, CHRONOSTRATIGRAPHIC, null, THESAURUSESPECIALLY);
                                                static void DrawDimLabelRight(Point3d basePt, double dy)
                                                {
                                                    var pt1 = basePt;
                                                    var pt2 = pt1.OffsetY(dy);
                                                    var dim = new AlignedDimension();
                                                    dim.XLine1Point = pt1;
                                                    dim.XLine2Point = pt2;
                                                    dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-PSYCHOPHYSIOLOGICAL);
                                                    dim.DimensionText = THESAURUSABRIDGE;
                                                    dim.Layer = THESAURUSCOURTESAN;
                                                    ByLayer(dim);
                                                    DrawEntityLazy(dim);
                                                }
                                                DrawDimLabelRight(p.ToPoint3d(), -QUOTATIONPATRONAL);
                                            }
                                        }
                                        else
                                        {
                                            var ditchDy = QUOTATIONPATRONAL;
                                            var _run = runs.TryGet(i);
                                            if (_run != null)
                                            {
                                                if (gpItem == null)
                                                {
                                                    if (_run != null)
                                                    {
                                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBEFRIEND), new Vector2d(-UNCONJECTURABLE, -UNCONJECTURABLE), new Vector2d(-ULTRAMICROTOMED, BATHYDRACONIDAE) };
                                                        var p = info.EndPoint;
                                                        var segs = vecs.ToGLineSegments(p);
                                                        drawDomePipes(segs);
                                                        segs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -CHEMOTROPICALLY), new Vector2d(-THESAURUSDEFICIT, BATHYDRACONIDAE) }.ToGLineSegments(segs.Last().EndPoint);
                                                        foreach (var line in DrawLineSegmentsLazy(segs))
                                                        {
                                                            Dr.SetLabelStylesForRainNote(line);
                                                        }
                                                        {
                                                            var t = DrawTextLazy(CHRONOSTRATIGRAPHIC, REVOLUTIONIZATION, segs.Last().EndPoint.OffsetXY(THESAURUSFORTIFICATION, THESAURUSFORTIFICATION));
                                                            Dr.SetLabelStylesForRainNote(t);
                                                        }
                                                    }
                                                    else if (!_run.HasLongTranslator && !_run.HasShortTranslator)
                                                    {
                                                        _dy = QUOTATIONPATRONAL;
                                                        var vecs = vecs0;
                                                        var p1 = info.StartPoint;
                                                        var p2 = p1 + new Vector2d(BATHYDRACONIDAE, -THESAURUSCONFECTIONERY - dy + _dy);
                                                        var segs = new List<GLineSegment>() { new GLineSegment(p1, p2) };
                                                        info.DisplaySegs = segs;
                                                        var p = basePt.OffsetY(QUOTATIONPATRONAL);
                                                        drawLabel(p, CHRONOSTRATIGRAPHIC, null, THESAURUSESPECIALLY);
                                                        static void DrawDimLabelRight(Point3d basePt, double dy)
                                                        {
                                                            var pt1 = basePt;
                                                            var pt2 = pt1.OffsetY(dy);
                                                            var dim = new AlignedDimension();
                                                            dim.XLine1Point = pt1;
                                                            dim.XLine2Point = pt2;
                                                            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(-PSYCHOPHYSIOLOGICAL);
                                                            dim.DimensionText = THESAURUSABRIDGE;
                                                            dim.Layer = THESAURUSCOURTESAN;
                                                            ByLayer(dim);
                                                            DrawEntityLazy(dim);
                                                        }
                                                        DrawDimLabelRight(p.ToPoint3d(), -QUOTATIONPATRONAL);
                                                    }
                                                    else
                                                    {
                                                        Dr.DrawLabel(basePt.ToPoint3d(), CHRONOSTRATIGRAPHIC);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                void _DrawCondensePipe(Point2d basePt)
                                {
                                    list.Add(basePt.OffsetY(QUOTATIONPATRONAL));
                                    Dr.DrawCondensePipe(basePt.OffsetXY(-QUOTATIONPATRONAL, THESAURUSFORTIFICATION));
                                }
                                void _drawFloorDrain(Point2d basePt, bool leftOrRight)
                                {
                                    list.Add(basePt.OffsetY(COMMENSURATENESS));
                                    var value = PSYCHOLINGUISTICALLY;
                                    if (gpItem.Hangings[i].HasSideFloorDrain) value = TRICHOBATRACHUS;
                                    if (leftOrRight)
                                    {
                                        _DrawFloorDrain(basePt.OffsetXY(THESAURUSUNAVOIDABLE + THESAURUSCARTOON, THESAURUSUNAVOIDABLE).ToPoint3d(), leftOrRight, value, THESAURUSAMENITY);
                                    }
                                    else
                                    {
                                        _DrawFloorDrain(basePt.OffsetXY(THESAURUSUNAVOIDABLE + THESAURUSCARTOON - QUINQUARTICULARIS, THESAURUSUNAVOIDABLE).ToPoint3d(), leftOrRight, value, THESAURUSAMENITY);
                                    }
                                    return;
                                }
                                {
                                    void drawDN(string dn, Point2d pt)
                                    {
                                        var t = DrawTextLazy(dn, REVOLUTIONIZATION, pt);
                                        Dr.SetLabelStylesForRainDims(t);
                                    }
                                    var hanging = gpItem.Hangings[i];
                                    var fixW2 = gpItem.Hangings.All(x => (x?.FloorDrainWrappingPipesCount ?? BATHYDRACONIDAE) == BATHYDRACONIDAE) ? THESAURUSBEFRIEND : BATHYDRACONIDAE;
                                    var fixW = THESAURUSSUCCINCT - fixW2;
                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSPREAMBLE, THESAURUSPREAMBLE), new Vector2d(-THESAURUSPERFIDY - fixW, BATHYDRACONIDAE) };
                                    if (getHasAirConditionerFloorDrain(i))
                                    {
                                        var p1 = info.StartPoint.OffsetY(-CHLOROFLUOROCARBONS);
                                        var p2 = p1.OffsetX(THESAURUSIMAGINATIVE);
                                        var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                                        Dr.SetLabelStylesForWNote(line);
                                        var segs = vecs.GetYAxisMirror().ToGLineSegments(p1.OffsetY(-THESAURUSMANKIND - THESAURUSDISPUTE));
                                        drawDomePipes(segs);
                                        var p = segs.Last().EndPoint;
                                        _drawFloorDrain(p, THESAURUSNEGATIVE);
                                        drawDN(SYNDIOTACTICALLY, segs[PIEZOELECTRICAL].StartPoint.OffsetXY(QUOTATIONPATRONAL + fixW, QUOTATIONPATRONAL));
                                    }
                                    if (hanging.FloorDrainsCount > BATHYDRACONIDAE)
                                    {
                                        var bsPt = info.EndPoint;
                                        string getDN()
                                        {
                                            const string dft = SYNDIOTACTICALLY;
                                            if (gpItem.PipeType == PipeType.Y2L) return viewModel?.Params.BalconyFloorDrainDN ?? dft;
                                            if (gpItem.PipeType == PipeType.NL) return viewModel?.Params.CondensePipeHorizontalDN ?? dft;
                                            return dft;
                                        }
                                        var wpCount = hanging.FloorDrainWrappingPipesCount;
                                        void tryDrawWrappingPipe(Point2d pt)
                                        {
                                            if (wpCount <= BATHYDRACONIDAE) return;
                                            DrawWrappingPipe(pt.ToPoint3d());
                                            --wpCount;
                                        }
                                        if (hanging.FloorDrainsCount == PIEZOELECTRICAL)
                                        {
                                            if (!(storey == THESAURUSDEFILE && (gpItem.HasSingleFloorDrainDrainageForWaterWell || gpItem.HasSingleFloorDrainDrainageForRainPort)))
                                            {
                                                var v = default(Vector2d);
                                                var ok = THESAURUSESPECIALLY;
                                                if (gpItem.Items.TryGet(i - PIEZOELECTRICAL).HasLong)
                                                {
                                                    if (runs[i - PIEZOELECTRICAL].IsLongTranslatorToLeftOrRight)
                                                    {
                                                        var p = vecs.GetLastPoint(bsPt.OffsetY(-THESAURUSMANKIND - THESAURUSDISPUTE) + v);
                                                        var _vecs = new List<Vector2d> { new Vector2d(-UNCONJECTURABLE, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -QUOTATIONDIAMONDBACK), new Vector2d(THESAURUSSETBACK, -THESAURUSOBLITERATION) };
                                                        var segs = _vecs.ToGLineSegments(p);
                                                        _drawFloorDrain(p, THESAURUSNEGATIVE);
                                                        drawDomePipes(segs);
                                                        tryDrawWrappingPipe(p.OffsetX(THESAURUSAPPORTION));
                                                        var __vecs = new List<Vector2d> { new Vector2d(-PRAETERNATURALIS, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -ULTRACENTRIFUGE), new Vector2d(BATHYDRACONIDAE, -DIETHYLSTILBOESTROL) };
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
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(-CONSTRUCTIONISM, BATHYDRACONIDAE), THESAURUSCONSUME, THESAURUSCOURTESAN);
                                                        ok = THESAURUSNEGATIVE;
                                                    }
                                                }
                                                if (!ok)
                                                {
                                                    var segs = vecs.ToGLineSegments(bsPt.OffsetY(-THESAURUSMANKIND - THESAURUSDISPUTE) + v);
                                                    drawDomePipes(segs);
                                                    var p = segs.Last().EndPoint;
                                                    _drawFloorDrain(p, THESAURUSNEGATIVE);
                                                    tryDrawWrappingPipe(p.OffsetX(THESAURUSAPPORTION));
                                                    drawDN(getDN(), segs[PIEZOELECTRICAL].EndPoint.OffsetXY(QUOTATIONPATRONAL + fixW, QUOTATIONPATRONAL));
                                                    ok = THESAURUSNEGATIVE;
                                                }
                                            }
                                        }
                                        else if (hanging.FloorDrainsCount == TEREBINTHINATED)
                                        {
                                            {
                                                var segs = vecs.ToGLineSegments(bsPt.OffsetY(-THESAURUSMANKIND - THESAURUSDISPUTE));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _drawFloorDrain(p, THESAURUSNEGATIVE);
                                                tryDrawWrappingPipe(p.OffsetX(THESAURUSAPPORTION));
                                                drawDN(getDN(), segs[PIEZOELECTRICAL].EndPoint.OffsetXY(QUOTATIONPATRONAL + fixW, QUOTATIONPATRONAL));
                                            }
                                            {
                                                var segs = vecs.GetYAxisMirror().ToGLineSegments(info.EndPoint.OffsetY(-THESAURUSMANKIND - THESAURUSDISPUTE));
                                                drawDomePipes(segs);
                                                var p = segs.Last().EndPoint;
                                                _drawFloorDrain(p, THESAURUSNEGATIVE);
                                                tryDrawWrappingPipe(p.OffsetX(-UNCONJECTURABLE));
                                                drawDN(getDN(), segs[PIEZOELECTRICAL].StartPoint.OffsetXY(QUOTATIONPATRONAL + fixW, QUOTATIONPATRONAL));
                                            }
                                        }
                                    }
                                    if (hanging.HasCondensePipe)
                                    {
                                        string getCondensePipeDN()
                                        {
                                            return viewModel?.Params.CondensePipeHorizontalDN ?? SYNDIOTACTICALLY;
                                        }
                                        var h = THESAURUSTRAGEDY;
                                        var w = THESAURUSBEFRIEND;
                                        if (hanging.HasBrokenCondensePipes)
                                        {
                                            var v = new Vector2d(BATHYDRACONIDAE, -THESAURUSFORTIFICATION);
                                            void f(double offsetY)
                                            {
                                                var segs = vecs.ToGLineSegments((info.StartPoint + v).OffsetY(offsetY));
                                                var p1 = segs.Last().EndPoint;
                                                var p3 = p1.OffsetY(h);
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3.OffsetXY(-QUOTATIONPATRONAL, QUOTATIONPATRONAL));
                                                drawDomePipes(segs);
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(QUOTATIONPATRONAL + fixW, -THESAURUSFORTIFICATION));
                                            }
                                            f(-THESAURUSCOMPOSURE);
                                            f(-THESAURUSCOMPOSURE - THESAURUSUNDERWATER);
                                        }
                                        else
                                        {
                                            double fixY = -DIETHYLSTILBOESTROL;
                                            var higher = hanging.PlsDrawCondensePipeHigher;
                                            if (higher)
                                            {
                                                fixY += THESAURUSBEFRIEND;
                                            }
                                            var v = new Vector2d(BATHYDRACONIDAE, fixY);
                                            var segs = vecs.ToGLineSegments((info.StartPoint + v).OffsetY(-THESAURUSCOMPOSURE));
                                            var p1 = segs.Last().EndPoint;
                                            var p2 = p1.OffsetX(w);
                                            var p3 = p1.OffsetY(h);
                                            var p4 = p2.OffsetY(h);
                                            drawDomePipes(segs);
                                            if (hanging.HasNonBrokenCondensePipes)
                                            {
                                                double _fixY = higher ? -THESAURUSMANKIND : BATHYDRACONIDAE;
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(QUOTATIONPATRONAL + fixW, THESAURUSFORTIFICATION + THESAURUSFORTIFICATION + _fixY));
                                                drawDomePipe(new GLineSegment(p1, p3));
                                                _DrawCondensePipe(p3);
                                                drawDomePipe(new GLineSegment(p2, p4));
                                                _DrawCondensePipe(p4);
                                            }
                                            else
                                            {
                                                double _fixY = higher ? -THESAURUSMANKIND + QUOTATIONPATRONAL : BATHYDRACONIDAE;
                                                drawDN(getCondensePipeDN(), p3.OffsetXY(QUOTATIONPATRONAL + fixW, -THESAURUSFORTIFICATION + _fixY));
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
                                    var h = Math.Round(HEIGHT / THESAURUSYAWNING * DOLICHOCEPHALOUS);
                                    var p = run.HasShortTranslator ? info.Segs.Last().StartPoint.OffsetY(h).ToPoint3d() : info.EndPoint.OffsetY(h).ToPoint3d();
                                    DrawCheckPoint(p, THESAURUSNEGATIVE);
                                    var seg = info.Segs.Last();
                                    var fixDy = run.HasShortTranslator ? -seg.Height : BATHYDRACONIDAE;
                                    DrawDimLabelRight(p, fixDy - h);
                                }
                            }
                        }
                        if (list.Count > BATHYDRACONIDAE)
                        {
                            var my = list.Select(x => x.Y).Max();
                            foreach (var pt in list)
                            {
                                if (pt.Y == my)
                                {
                                }
                            }
                            var h = CONSTRUCTIONISM - HYPERSENSITIZED;
                            foreach (var pt in list)
                            {
                                if (pt.Y != my) continue;
                                var ok = THESAURUSESPECIALLY;
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
                                            linesKillers.Add(new GLineSegment(pt.OffsetY(-THESAURUSCOUNCIL), pt.OffsetY(-THESAURUSCOUNCIL).OffsetX(UNCOMPASSIONATE)).ToLineString());
                                        }
                                        ok = THESAURUSNEGATIVE;
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
                                            }
                                            _DrawAiringSymbol(p.OffsetY(h));
                                            linesKillers.Add(new GLineSegment(pt, pt.OffsetX(UNCOMPASSIONATE)).ToLineString());
                                        }
                                        else
                                        {
                                            linesKillers.Add(new GLineSegment(pt, pt.OffsetX(UNCOMPASSIONATE)).ToLineString());
                                        }
                                        ok = THESAURUSNEGATIVE;
                                    }
                                }
                            }
                        }
                    }
                    bool getHasAirConditionerFloorDrain(int i)
                    {
                        if (gpItem.PipeType != PipeType.Y2L) return THESAURUSESPECIALLY;
                        var hanging = gpItem.Hangings[i];
                        if (hanging.HasBrokenCondensePipes || hanging.HasNonBrokenCondensePipes)
                        {
                            return viewModel?.Params.HasAirConditionerFloorDrain ?? THESAURUSESPECIALLY;
                        }
                        return THESAURUSESPECIALLY;
                    }
                    var infos = getPipeRunLocationInfos(basePoint.OffsetX(dx));
                    handlePipeLine(thwPipeLine, infos);
                    static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight, double height = REVOLUTIONIZATION)
                    {
                        var gap = THESAURUSFORTIFICATION;
                        var factor = THESAURUSADVANCEMENT;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? BATHYDRACONIDAE, text2?.Length ?? BATHYDRACONIDAE) + QUOTATIONPATRONAL;
                        if (width < THESAURUSDEFICIT) width = THESAURUSDEFICIT;
                        var vecs = new List<Vector2d> { new Vector2d(PSYCHOPHYSIOLOGICAL, PSYCHOPHYSIOLOGICAL), new Vector2d(width, BATHYDRACONIDAE) };
                        if (isLeftOrRight == THESAURUSNEGATIVE)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[PIEZOELECTRICAL].EndPoint : segs[PIEZOELECTRICAL].StartPoint;
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
                        var height = REVOLUTIONIZATION;
                        var gap = THESAURUSFORTIFICATION;
                        var factor = THESAURUSADVANCEMENT;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? BATHYDRACONIDAE, text2?.Length ?? BATHYDRACONIDAE);
                        if (width < THESAURUSDEFICIT) width = THESAURUSDEFICIT;
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSCORRESPONDENT, BATHYDRACONIDAE), new Vector2d(width, BATHYDRACONIDAE) };
                        if (isLeftOrRight == THESAURUSNEGATIVE)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForRainNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[PIEZOELECTRICAL].EndPoint : segs[PIEZOELECTRICAL].StartPoint;
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
                    for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
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
                        if (storey == THESAURUSINSURANCE && gpItem.HasLineAtBuildingFinishedSurfice)
                        {
                            var p1 = info.EndPoint;
                            var p2 = p1.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                            drawDomePipe(new GLineSegment(p1, p2));
                        }
                    }
                    {
                        var has_label_storeys = new HashSet<string>();
                        {
                            var _storeys = new string[] { allNumStoreyLabels.GetAt(TEREBINTHINATED), allNumStoreyLabels.GetLastOrDefault(THESAURUSINTELLECT) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == BATHYDRACONIDAE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            _storeys = _storeys.Where(storey =>
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = infos.TryGet(i);
                                return info != null && info.Visible;
                            }).ToList();
                            if (_storeys.Count == BATHYDRACONIDAE)
                            {
                                _storeys = allNumStoreyLabels.Where(storey =>
                                {
                                    var i = allNumStoreyLabels.IndexOf(storey);
                                    var info = infos.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(PIEZOELECTRICAL).ToList();
                            }
                            if (_storeys.Count == BATHYDRACONIDAE)
                            {
                                _storeys = allStoreys.Where(storey =>
                                {
                                    var i = allStoreys.IndexOf(storey);
                                    var info = infos.TryGet(i);
                                    return info != null && info.Visible;
                                }).Take(PIEZOELECTRICAL).ToList();
                            }
                            foreach (var storey in _storeys)
                            {
                                has_label_storeys.Add(storey);
                                var i = allStoreys.IndexOf(storey);
                                var info = infos[i];
                                {
                                    string label1, label2;
                                    var labels = RainLabelItem.ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x)).ToList()).OrderBy(x => x).ToList();
                                    if (labels.Count == TEREBINTHINATED)
                                    {
                                        label1 = labels[BATHYDRACONIDAE];
                                        label2 = labels[PIEZOELECTRICAL];
                                    }
                                    else
                                    {
                                        label1 = labels.JoinWith(UNCOMPANIONABLE);
                                        label2 = null;
                                    }
                                    var isLeftOrRight = (gpItem.Hangings.TryGet(i)?.FloorDrainsCount ?? BATHYDRACONIDAE) == BATHYDRACONIDAE && !(gpItem.Hangings.TryGet(i)?.HasCondensePipe ?? THESAURUSESPECIALLY);
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
                                                isLeftOrRight = THESAURUSESPECIALLY;
                                            }
                                        }
                                    }
                                    if (getHasAirConditionerFloorDrain(i) && isLeftOrRight == THESAURUSESPECIALLY)
                                    {
                                        var pt = info.EndPoint.OffsetY(THESAURUSTRAGEDY);
                                        if (storey == THESAURUSINSURANCE)
                                        {
                                            pt = pt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
                                        }
                                        drawLabel2(pt, label1, label2, isLeftOrRight);
                                    }
                                    else
                                    {
                                        var pt = info.PlBasePt;
                                        if (storey == THESAURUSINSURANCE)
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
                                bool isMinFloor = THESAURUSNEGATIVE;
                                for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                                {
                                    var (ok, item) = gpItem.Items.TryGetValue(i);
                                    if (!(ok && item.Exist)) continue;
                                    if (item.Exist && isMinFloor)
                                    {
                                        isMinFloor = THESAURUSESPECIALLY;
                                        continue;
                                    }
                                    var storey = allNumStoreyLabels[i];
                                    if (has_label_storeys.Contains(storey)) continue;
                                    var run = runs.TryGet(i);
                                    if (run == null) continue;
                                    if (!run.HasLongTranslator && !run.HasShortTranslator && (!(gpItem.Hangings.TryGet(i)?.HasCheckPoint ?? THESAURUSESPECIALLY)))
                                    {
                                        _allSmoothStoreys.Add(storey);
                                    }
                                }
                            }
                            var _storeys = new string[] { _allSmoothStoreys.GetAt(BATHYDRACONIDAE), _allSmoothStoreys.GetLastOrDefault(TEREBINTHINATED) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == BATHYDRACONIDAE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.GetAt(PIEZOELECTRICAL), allNumStoreyLabels.GetLastOrDefault(TEREBINTHINATED) }.SelectNotNull().Distinct().ToList();
                            }
                            if (_storeys.Count == BATHYDRACONIDAE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                            }
                            const string dft = THESAURUSSPITEFUL;
                            var dn = gpItem.PipeType switch
                            {
                                PipeType.Y2L => viewModel?.Params.BalconyRainPipeDN,
                                PipeType.NL => viewModel?.Params.CondensePipeVerticalDN,
                                _ => dft,
                            };
                            dn ??= dft;
                            foreach (var storey in _storeys)
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = infos.TryGet(i);
                                if (info != null && info.Visible)
                                {
                                    var run = runs.TryGet(i);
                                    if (run != null)
                                    {
                                        Dr.DrawDN_2(info.EndPoint.OffsetX(CHEMOTROPICALLY), THESAURUSSPELLBOUND, dn);
                                    }
                                }
                            }
                        }
                    }
                    if (linesKillers.Count > BATHYDRACONIDAE)
                    {
                        dome_lines = GeoFac.ToNodedLineSegments(dome_lines);
                        var geos = dome_lines.Select(x => x.ToLineString()).ToList();
                        dome_lines = geos.Except(GeoFac.CreateIntersectsSelector(geos)(GeoFac.CreateGeometryEx(linesKillers.ToList()))).Cast<LineString>().SelectMany(x => x.ToGLineSegments()).ToList();
                    }
                    {
                        var auto_conn = THESAURUSESPECIALLY;
                        if (auto_conn)
                        {
                            foreach (var g in GeoFac.GroupParallelLines(dome_lines, PIEZOELECTRICAL, THESAURUSPROPRIETOR))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: PTILONORHYNCHUS));
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
                    if (value == TRICHOBATRACHUS)
                    {
                        basePt += new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE).ToVector3d();
                    }
                    DrawBlockReference(THESAURUSCOMMUTE, basePt, br =>
                    {
                        br.Layer = THESAURUSINSPECTOR;
                        ByLayer(br);
                        br.ScaleFactors = new Scale3d(TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, value);
                        }
                    });
                }
                else
                {
                    DrawBlockReference(THESAURUSCOMMUTE, basePt,
                   br =>
                   {
                       br.Layer = THESAURUSINSPECTOR;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, value);
                       }
                   });
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
                return BATHYDRACONIDAE;
            }
            public bool Equals(PipeCmpInfo other)
            {
                return this.IsWaterPortOutlet == other.IsWaterPortOutlet && PipeRuns.SequenceEqual(other.PipeRuns);
            }
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof, bool showText)
        {
            var name = showText ? (canPeopleBeOnRoof ? THESAURUSACQUIRE : THESAURUSASTUTE) : THESAURUSMACHINE;
            DrawAiringSymbol(pt, name);
        }
        public static bool CollectRainData(Point3dCollection range, AcadDatabase adb, out List<StoreyInfo> storeysItems, out List<RainDrawingData> drDatas, bool noWL = THESAURUSESPECIALLY)
        {
            CollectRainGeoData(range, adb, out storeysItems, out RainGeoData geoData);
            return CreateRainDrawingData(adb, out drDatas, noWL, geoData);
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
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSSMASHING;
                ByLayer(line);
            });
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSNEGATIVE))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { THESAURUSGOODNESS, THESAURUSINSPECTOR, THESAURUSSPELLBOUND, THESAURUSRECRIMINATION, THESAURUSCOURTESAN, THESAURUSEXCOMMUNICATE, NEUROTRANSMITTER, THESAURUSSMASHING });
                var storeys = ThRainService.commandContext.StoreyContext.StoreyInfos;
                List<RainDrawingData> drDatas;
                var range = ThRainService.commandContext.range;
                List<StoreyInfo> storeysItems;
                if (range != null)
                {
                    if (!CollectRainData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSNEGATIVE)) return;
                }
                else
                {
                    if (!CollectRainData(adb, out _, out drDatas, ThRainService.commandContext, noWL: THESAURUSNEGATIVE)) return;
                    storeysItems = storeys;
                }
                var pipeGroupItems = GetRainGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys, out OtherInfo otherInfo);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + PHENYLENEDIAMINE).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - PIEZOELECTRICAL;
                var end = BATHYDRACONIDAE;
                var OFFSET_X = HYPERCHOLESTERO;
                var SPAN_X = QUOTATIONCOLLARED;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSRECAPITULATE;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSRECAPITULATE;
                Dispose();
                DrawRainDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel, otherInfo);
                FlushDQ(adb);
            }
        }
#pragma warning disable
        static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
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
                            item.Labels = new List<string>() { THESAURUSINSURANCE };
                        }
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                    case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                        {
                            item.Ints = s.Numbers.OrderBy(x => x).ToList();
                            item.Labels = item.Ints.Select(x => x + PHENYLENEDIAMINE).ToList();
                        }
                        break;
                    default:
                        break;
                }
            }
            return storeysItems;
        }
        public static bool CreateRainDrawingData(AcadDatabase adb, out List<RainDrawingData> drDatas, bool noWL, RainGeoData geoData)
        {
            ThRainService.PreFixGeoData(geoData);
            drDatas = _CreateRainDrawingData(adb, geoData, THESAURUSNEGATIVE);
            return THESAURUSNEGATIVE;
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-PSYCHOPHYSIOLOGICAL, THESAURUSBEFRIEND), new Vector2d(-THESAURUSNOTIFY, BATHYDRACONIDAE) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[PIEZOELECTRICAL].EndPoint : segs[PIEZOELECTRICAL].StartPoint;
            var text = THESAURUSCONSENT;
            var height = REVOLUTIONIZATION;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static List<StoreyInfo> GetStoreys(AcadDatabase adb, CommandContext ctx)
        {
            return ctx.StoreyContext.StoreyInfos;
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = BATHYDRACONIDAE, j = BATHYDRACONIDAE;
            for (int k = BATHYDRACONIDAE; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, BATHYDRACONIDAE);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = BATHYDRACONIDAE;
                }
            }
        }
        static double RF_OFFSET_Y => ThWSDStorey.RF_OFFSET_Y;
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            value ??= THESAURUSAMENITY;
            DrawBlockReference(blkName: THESAURUSASSIGN, basePt: basePt.OffsetY(-PSYCHOPHYSIOLOGICAL),
            props: new Dictionary<string, string>() { { CONTEMPTIBILITY, value } },
            cb: br =>
            {
                br.Layer = THESAURUSINSPECTOR;
                ByLayer(br);
            });
        }
        public static void CollectRainGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreyInfo> storeys, out RainGeoData geoData)
        {
            storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            geoData = new RainGeoData();
            geoData.Init();
            RainService.CollectGeoData(range, adb, geoData);
        }
        public static List<StoreyInfo> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var geo = range?.ToGRect().ToPolygon();
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSNEGATIVE).ToList();
            FixStoreys(storeys);
            return storeys;
        }
        public static void DrawRainDiagram(List<RainDrawingData> drDatas, List<StoreyInfo> storeysItems, Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys, OtherInfo otherInfo)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + PHENYLENEDIAMINE).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - PIEZOELECTRICAL;
            var end = BATHYDRACONIDAE;
            var OFFSET_X = HYPERCHOLESTERO;
            var SPAN_X = QUOTATIONCOLLARED;
            var HEIGHT = THESAURUSRECAPITULATE;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSRECAPITULATE;
            DrawRainDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, viewModel: null, otherInfo);
        }
        public static double CHECKPOINT_OFFSET_Y = SUPERCILIOUSNESS;
        public static void DrawRainWaterWell(Point2d basePt, string value)
        {
            DrawRainWaterWell(basePt.ToPoint3d(), value);
        }
        private static void DrawDimLabelRight(Point3d basePt, double dy)
        {
            var pt1 = basePt;
            var pt2 = pt1.OffsetY(dy);
            var dim = new AlignedDimension();
            dim.XLine1Point = pt1;
            dim.XLine2Point = pt2;
            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(CONSTRUCTIONISM);
            dim.DimensionText = STOICHIOMETRICALLY;
            dim.Layer = THESAURUSCOURTESAN;
            ByLayer(dim);
            DrawEntityLazy(dim);
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
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: THESAURUSCENSOR, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: NEUROTRANSMITTER, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(PALAEOGEOMORPHOLOGY, offsetY);
                br.ObjectId.SetDynBlockValue(THESAURUSGENTILITY, THESAURUSREPREHENSIBLE);
            });
        }
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DrawBlockReference(blkName: THESAURUSREPRESSION, basePt: basePt.OffsetXY(-THESAURUSUNDERWATER, BATHYDRACONIDAE), cb: br =>
            {
                SetLayerAndByLayer(THESAURUSEXCOMMUNICATE, br);
                if (br.IsDynamicBlock)
                {
                    br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, AUTHORITATIVENESS);
                }
            });
        }
        public static void DrawRainDiagram(Point2d basePoint, List<RainGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, RainSystemDiagramViewModel viewModel, OtherInfo otherInfo)
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
                }.Run();
            }
        }
        public static bool ShowWaterBucketHitting;
        public static void DrawWaterSealingWell(Point2d basePt)
        {
            DrawBlockReference(blkName: QUOTATIONKEELED, basePt: basePt.ToPoint3d(),
         cb: br =>
         {
             br.Layer = THESAURUSINSPECTOR;
             ByLayer(br);
         });
        }
        public static void DrawRainPipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line => SetRainPipeLineStyle(line));
        }
        public static IEnumerable<Point2d> GetBasePoints(Point2d basePoint, int maxCol, int num, double width, double height)
        {
            int i = BATHYDRACONIDAE, j = BATHYDRACONIDAE;
            for (int k = BATHYDRACONIDAE; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = BATHYDRACONIDAE;
                }
            }
        }
        public class WaterBucketInfo
        {
            public string WaterBucket;
            public string Pipe;
            public string Storey;
        }
        public static int GetStoreyScore(string label)
        {
            if (label == null) return BATHYDRACONIDAE;
            switch (label)
            {
                case THESAURUSINSURANCE: return ushort.MaxValue;
                case THESAURUSBLACKOUT: return ushort.MaxValue + PIEZOELECTRICAL;
                case HYDROMETALLURGY: return ushort.MaxValue + TEREBINTHINATED;
                default:
                    {
                        int.TryParse(label.Replace(PHENYLENEDIAMINE, THESAURUSAMENITY), out int ret);
                        return ret;
                    }
            }
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
        public static List<RainDrawingData> CreateRainDrawingData(AcadDatabase adb, RainGeoData geoData, bool noDraw)
        {
            ThRainService.PreFixGeoData(geoData);
            return _CreateRainDrawingData(adb, geoData, noDraw);
        }
        public static void DrawCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DrawBlockReference(blkName: QUOTATIONEMBRYOID, basePt: basePt,
        cb: br =>
        {
            if (leftOrRight)
            {
                br.ScaleFactors = new Scale3d(-PIEZOELECTRICAL, PIEZOELECTRICAL, PIEZOELECTRICAL);
            }
            ByLayer(br);
            br.Layer = THESAURUSRECRIMINATION;
        });
        }
        public static void DrawRainPipes(params GLineSegment[] segs)
        {
            DrawRainPipes((IEnumerable<GLineSegment>)segs);
        }
        public static string GetLabelScore(string label)
        {
            if (label == null) return null;
            if (IsPL(label))
            {
                return UNACCUSTOMEDNESS + label;
            }
            if (IsFL(label))
            {
                return THESAURUSFILIGREE + label;
            }
            return label;
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
        public static IEnumerable<KeyValuePair<string, string>> EnumerateSmallRoofVerticalPipes(List<ThwSDStoreyItem> wsdStoreys, string vpipe)
        {
            foreach (var s in wsdStoreys)
            {
                if (s.Storey == HYDROMETALLURGY || s.Storey == THESAURUSBLACKOUT)
                {
                    if (s.VerticalPipes.Contains(vpipe))
                    {
                        yield return new KeyValuePair<string, string>(s.Storey, vpipe);
                    }
                }
            }
        }
        public static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = MICROSPECTROPHOTOMETRY)
        {
            DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, label } }, cb: br => { ByLayer(br); });
        }
    }
    public class RainLabelItem
    {
        public string D2S;
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
                else if (ed + PIEZOELECTRICAL == i)
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
        public static IEnumerable<string> ConvertLabelStrings(List<string> pipeIds)
        {
            {
                var labels = pipeIds.Where(x => Regex.IsMatch(x, QUOTATIONTELLUROUS)).ToList();
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
                        var m = Regex.Match(str, QUOTATIONTELLUROUS);
                        if (m.Success)
                        {
                            kvs.Add(new KeyValuePair<string, string>(m.Groups[PIEZOELECTRICAL].Value, m.Groups[TEREBINTHINATED].Value));
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                    return kvs.GroupBy(x => x.Key).OrderBy(x => x.Key).Select(x => x.Key + CONTEMPTIBILITY + string.Join(THESAURUSLUGGAGE, GetLabelString(x.Select(y => y.Value[BATHYDRACONIDAE]))));
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
                            yield return Convert.ToChar(kv.Key) + CHRISTIANIZATION + Convert.ToChar(kv.Value);
                        }
                    }
                }
            }
            {
                var labels = pipeIds.Where(x => Regex.IsMatch(x, THESAURUSHOODLUM)).ToList();
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
                        var m = Regex.Match(str, THESAURUSWRONGDOER);
                        if (m.Success)
                        {
                            kvs.Add(new ValueTuple<string, string, int>(m.Groups[PIEZOELECTRICAL].Value, m.Groups[TEREBINTHINATED].Value, int.Parse(m.Groups[THESAURUSINTELLECT].Value)));
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                    return kvs.GroupBy(x => x.Item1).OrderBy(x => x.Key).Select(x => x.Key + string.Join(THESAURUSLUGGAGE, GetLabelString(x.First().Item2, x.Select(y => y.Item3))));
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
                            yield return prefix + kv.Key + CHRISTIANIZATION + prefix + kv.Value;
                        }
                    }
                }
            }
            var items = pipeIds.Select(id => RainLabelItem.Parse(id)).Where(m => m != null).ToList();
            var rest = pipeIds.Except(items.Select(x => x.Label)).ToList();
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2S?.Length ?? BATHYDRACONIDAE).ThenBy(x => x.D2).ThenBy(x => x.D2S).ToList());
            foreach (var g in gs)
            {
                if (g.Count == PIEZOELECTRICAL)
                {
                    yield return g.First().Label;
                }
                else if (g.Count > TEREBINTHINATED && g.Count == g.Last().D2 - g.First().D2 + PIEZOELECTRICAL)
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
                    for (int i = BATHYDRACONIDAE; i < g.Count; i++)
                    {
                        var m = g[i];
                        sb.Append($"{m.D2S}{m.Suffix}");
                        if (i != g.Count - PIEZOELECTRICAL)
                        {
                            sb.Append(THESAURUSLUGGAGE);
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
        public string Prefix;
        public string Label;
        public string D1S;
        public int D1
        {
            get
            {
                int.TryParse(D1S, out int r); return r;
            }
        }
        static readonly Regex re = new Regex(THESAURUSMUDDLE);
        public int D2
        {
            get
            {
                int.TryParse(D2S, out int r); return r;
            }
        }
        public string Suffix;
        public static RainLabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new RainLabelItem()
            {
                Label = label,
                Prefix = m.Groups[PIEZOELECTRICAL].Value,
                D1S = m.Groups[TEREBINTHINATED].Value,
                D2S = m.Groups[THESAURUSINTELLECT].Value,
                Suffix = m.Groups[THESAURUSEVINCE].Value,
            };
        }
    }
    public class Hanging
    {
        public bool HasSCurve;
        public bool HasDoubleSCurve;
        public bool HasUnderBoardLabel;
        public bool IsSeries;
        public int FloorDrainsCount;
        public bool IsFL0;
    }
    public class ThwOutput
    {
        public bool HasCleaningPort3;
        public Hanging Hanging2;
        public bool HasWrappingPipe1;
        public Hanging Hanging1;
        public bool HasCleaningPort1;
        public int LinesCount = PIEZOELECTRICAL;
        public string DN3;
        public string DN1;
        public bool HasCleaningPort2;
        public bool HasWrappingPipe2;
        public bool HasLargeCleaningPort;
        public string DN2;
        public List<string> DirtyWaterWellValues;
        public int HangingCount = BATHYDRACONIDAE;
        public bool HasVerticalLine2;
        public bool HasWrappingPipe3;
    }
    public class ThwPipeRun
    {
        public string Storey;
        public Hanging LeftHanging;
        public bool ShowStoreyLabel;
        public bool IsFirstItem;
        public bool HasLongTranslator;
        public bool ShowShortTranslatorLabel;
        public bool HasShortTranslator;
        public bool IsLastItem;
        public bool IsLongTranslatorToLeftOrRight;
        public Hanging RightHanging;
        public bool IsShortTranslatorToLeftOrRight;
    }
    public class ThwPipeLine
    {
        public bool? IsLeftOrMiddleOrRight;
        public List<ThwPipeRun> PipeRuns;
        public List<string> Labels;
        public double AiringValue;
        public ThwOutput Output;
    }
    public class OtherInfo
    {
        public List<AloneFloorDrainInfo> AloneFloorDrainInfos;
    }
    public class RainDrawingData
    {
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public Dictionary<string, int> FloorDrains;
        public HashSet<string> CleaningPorts;
        public HashSet<string> ConnectedToSideWaterBucket;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell;
        public Dictionary<string, string> WaterWellLabels;
        public GRect Boundary;
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> PlsDrawCondensePipeHigher;
        public List<string> SideWaterBucketLabels;
        public HashSet<string> HasSideFloorDrain;
        public Dictionary<int, string> OutletWrappingPipeDict;
        public Dictionary<int, string> OutletWrappingPipeRadiusStringDict;
        public List<GRect> GravityWaterBuckets;
        public Dictionary<string, int> RainPortIds;
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
        public List<GRect> Y1LVerticalPipeRects;
        public List<string> Y1LVerticalPipeRectLabels;
        public Dictionary<string, int> WaterWellIds;
        public HashSet<int> HasSingleFloorDrainDrainageForDitch;
        public List<string> _87WaterBucketLabels;
        public HashSet<string> HasNonBrokenCondensePipes;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForRainPort;
        public Dictionary<string, int> FloorDrainWrappingPipes;
        public Point2d ContraPoint;
        public HashSet<int> HasSingleFloorDrainDrainageForWaterSealingWell;
        public List<string> Comments;
        public HashSet<int> FloorDrainShareDrainageWithVerticalPipeForDitch;
        public HashSet<string> Spreadings;
        public List<string> GravityWaterBucketLabels;
        public List<KeyValuePair<string, string>> RoofWaterBuckets;
        public HashSet<string> HasBrokenCondensePipes;
        public Dictionary<string, int> WaterSealingWellIds;
        public HashSet<int> HasSingleFloorDrainDrainageForRainPort;
        public HashSet<int> HasSingleFloorDrainDrainageForWaterWell;
        public HashSet<string> HasRainPortSymbols;
        public HashSet<string> ShortTranslatorLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ConnectedToGravityWaterBucket;
        public HashSet<string> HasDitch;
        public HashSet<string> HasCondensePipe;
        public Dictionary<string, int> DitchIds;
        public List<GRect> SideWaterBuckets;
        public HashSet<string> HasWaterSealingWell;
        public List<GRect> _87WaterBuckets;
        public List<AloneFloorDrainInfo> AloneFloorDrainInfos;
    }
    public partial class RainService
    {
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == THESAURUSHUMORIST).Select(x => x.ToNTSPolygon()).Cast<Geometry>().ToList();
            var names = adb.ModelSpace.OfType<MText>().Where(x => x.Layer == THESAURUSCOMPLEX).Select(x => new CText() { Text = x.Text, Boundary = x.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect() }).ToList();
            var f = GeoFac.CreateIntersectsSelector(ranges);
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == PIEZOELECTRICAL)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[BATHYDRACONIDAE]));
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
                list.Add(new KeyValuePair<string, Geometry>(THESAURUSAMENITY, range));
            }
            return list;
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
        {
            return source1.Concat(source2).Concat(source3).ToList();
        }
        public List<KeyValuePair<string, Geometry>> roomData;
        private static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, int storeyI, RainCadData item)
        {
            foreach (var o in item.LabelLines)
            {
                DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = PIEZOELECTRICAL;
            }
            foreach (var pl in item.Labels)
            {
                var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                var e = DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                e.ColorIndex = TEREBINTHINATED;
                var _pl = DrawRectLazy(m.Boundary);
                _pl.ColorIndex = TEREBINTHINATED;
            }
            foreach (var o in item.PipeKillers)
            {
                DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(MELANCHOLIOUSNESS, MELANCHOLIOUSNESS, STANDOFFISHNESS);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)], INTERLINGUISTICS);
            }
            foreach (var o in item.VerticalPipes)
            {
                DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = THESAURUSINTELLECT;
            }
            foreach (var o in item.FloorDrains)
            {
                DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = PARALLELOGRAMMIC;
            }
            foreach (var o in item.WLines)
            {
                DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = PARALLELOGRAMMIC;
            }
            foreach (var o in item.WaterPorts)
            {
                DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = PERCHLOROETHYLENE;
                DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.WaterWells)
            {
                DrawRectLazy(geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ColorIndex = THESAURUSEVINCE;
                DrawTextLazy(geoData.WaterWellLabels[cadDataMain.WaterWells.IndexOf(o)], o.GetCenter());
            }
            foreach (var o in item.RainPortSymbols)
            {
                DrawRectLazy(geoData.RainPortSymbols[cadDataMain.RainPortSymbols.IndexOf(o)]).ColorIndex = PERCHLOROETHYLENE;
            }
            foreach (var o in item.WaterSealingWells)
            {
                DrawRectLazy(geoData.WaterSealingWells[cadDataMain.WaterSealingWells.IndexOf(o)]).ColorIndex = PERCHLOROETHYLENE;
            }
            foreach (var o in item.GravityWaterBuckets)
            {
                var r = geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = PERCHLOROETHYLENE;
                Dr.DrawSimpleLabel(r.LeftTop, CHUCKLEHEADEDNESS);
            }
            foreach (var o in item.SideWaterBuckets)
            {
                var r = geoData.SideWaterBuckets[cadDataMain.SideWaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = PERCHLOROETHYLENE;
                Dr.DrawSimpleLabel(r.LeftTop, DISCIPLINABILIS);
            }
            foreach (var o in item._87WaterBuckets)
            {
                var r = geoData._87WaterBuckets[cadDataMain._87WaterBuckets.IndexOf(o)];
                DrawRectLazy(r).ColorIndex = PERCHLOROETHYLENE;
                Dr.DrawSimpleLabel(r.LeftTop, THESAURUSALTERATION);
            }
            foreach (var o in item.CondensePipes)
            {
                DrawRectLazy(geoData.CondensePipes[cadDataMain.CondensePipes.IndexOf(o)]).ColorIndex = PIEZOELECTRICAL;
            }
            foreach (var o in item.CleaningPorts)
            {
                var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                if (THESAURUSESPECIALLY) DrawGeometryLazy(new GCircle(m, THESAURUSFORTIFICATION).ToCirclePolygon(THESAURUSADMISSION), ents => ents.ForEach(e => e.ColorIndex = PERCHLOROETHYLENE));
                DrawRectLazy(GRect.Create(m, DISCOMFORTABLENESS));
            }
            foreach (var o in item.Ditches)
            {
                var m = geoData.Ditches[cadDataMain.Ditches.IndexOf(o)];
                DrawRectLazy(m);
            }
            {
                var cl = Color.FromRgb(THESAURUSFELLOW, THESAURUSBEDEVIL, THESAURUSTESTIMONY);
                foreach (var o in item.WrappingPipes)
                {
                    DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(THESAURUSEVINCE, THESAURUSJAGGED, THESAURUSENDING);
                foreach (var o in item.DLines)
                {
                    DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                }
            }
            {
                var cl = Color.FromRgb(HYDROXYNAPHTHALENE, THESAURUSFOLLOWER, THESAURUSACCORDANCE);
                foreach (var o in item.VLines)
                {
                    DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                }
            }
        }
        public static void CollectGeoData(AcadDatabase adb, RainGeoData geoData, CommandContext ctx)
        {
            var cl = new ThRainSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(ctx);
            cl.CollectEntities();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
        {
            return source1.Concat(source2).ToList();
        }
        public List<RainDrawingData> drawingDatas;
        public RainDiagram RainDiagram;
        static List<GLineSegment> ExplodeGLineSegments(Geometry geo)
        {
            static IEnumerable<GLineSegment> enumerate(Geometry geo)
            {
                if (geo is LineString ls)
                {
                    if (ls.NumPoints == TEREBINTHINATED) yield return new GLineSegment(ls[BATHYDRACONIDAE].ToPoint2d(), ls[PIEZOELECTRICAL].ToPoint2d());
                    else if (ls.NumPoints > TEREBINTHINATED)
                    {
                        for (int i = BATHYDRACONIDAE; i < ls.NumPoints - PIEZOELECTRICAL; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + PIEZOELECTRICAL].ToPoint2d());
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
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == BATHYDRACONIDAE) return THESAURUSESPECIALLY;
            }
            return THESAURUSNEGATIVE;
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = PSYCHOPHYSIOLOGICAL;
        public AcadDatabase adb;
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, RainGeoData geoData)
        {
            var cl = new ThRainSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(range);
            cl.CollectEntities();
        }
        public RainCadData CadDataMain;
        public List<StoreyInfo> Storeys;
        public static ThRainSystemService.CommandContext commandContext => ThRainSystemService.commandContext;
        public RainGeoData GeoData;
        public List<RainCadData> CadDatas;
#pragma warning disable
        public static void DrawGeoData(RainGeoData geoData)
        {
            foreach (var s in geoData.Storeys) DrawRectLazy(s).ColorIndex = PIEZOELECTRICAL;
            foreach (var o in geoData.LabelLines) DrawLineSegmentLazy(o).ColorIndex = PIEZOELECTRICAL;
            foreach (var o in geoData.Labels)
            {
                DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = TEREBINTHINATED;
                DrawRectLazy(o.Boundary).ColorIndex = TEREBINTHINATED;
            }
            foreach (var o in geoData.VerticalPipes) DrawRectLazy(o).ColorIndex = THESAURUSINTELLECT;
            foreach (var o in geoData.FloorDrains)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSEVINCE;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSBOILING);
            }
            foreach (var o in geoData.GravityWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSEVINCE;
                Dr.DrawSimpleLabel(o.LeftTop, DISCONTINUATION);
            }
            foreach (var o in geoData.SideWaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSEVINCE;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSLECHEROUS);
            }
            foreach (var o in geoData._87WaterBuckets)
            {
                DrawRectLazy(o).ColorIndex = THESAURUSEVINCE;
                Dr.DrawSimpleLabel(o.LeftTop, THESAURUSALTERATION);
            }
            foreach (var o in geoData.WaterPorts) DrawRectLazy(o).ColorIndex = PERCHLOROETHYLENE;
            foreach (var o in geoData.WaterWells) DrawRectLazy(o).ColorIndex = PERCHLOROETHYLENE;
            {
                var cl = Color.FromRgb(THESAURUSEVINCE, THESAURUSJAGGED, THESAURUSENDING);
                foreach (var o in geoData.DLines) DrawLineSegmentLazy(o).Color = cl;
            }
            foreach (var o in geoData.WLines) DrawLineSegmentLazy(o).ColorIndex = PARALLELOGRAMMIC;
        }
        public static void CreateDrawingDatas(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas, out string logString, out List<RainDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData)
        {
            _DrawingTransaction.Current.AbleToDraw = THESAURUSESPECIALLY;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = PIEZOELECTRICAL;
            }
            var sb = new StringBuilder(MINERALOCORTICOID);
            drDatas = new List<RainDrawingData>();
            for (int storeyI = BATHYDRACONIDAE; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new RainDrawingData();
                drData.Init();
                drData.Boundary = geoData.Storeys[storeyI];
                drData.ContraPoint = geoData.StoreyContraPoints[storeyI];
                var item = cadDatas[storeyI];
                {
                    var maxDis = THESAURUSRETAIN;
                    var angleTolleranceDegree = PIEZOELECTRICAL;
                    var waterPortCvt = RainCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.WLines.Where(x => x.Length > BATHYDRACONIDAE).Distinct().ToList().Select(cadDataMain.WLines).ToList(geoData.WLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
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
                var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, HOOGMOGENDHEIDEN).ToList();
                var wrappingPipesf = F(item.WrappingPipes);
                var sfdsf = F(item.SideFloorDrains);
                {
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (text.Contains(THESAURUSINNOCENT) && text.Contains(THESAURUSINCULCATE))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == PIEZOELECTRICAL)
                            {
                                var labelLineGeo = lst[BATHYDRACONIDAE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, DOLICHOCEPHALOUS);
                                var _pts = pts.Select(x => x.ToNTSPoint()).ToList();
                                var ptsf = GeoFac.CreateIntersectsSelector(_pts);
                                _pts = _pts.Except(ptsf(label)).Where(pt => item.RainPortSymbols.All(x => !x.Intersects(pt.Buffer(THESAURUSFORTIFICATION)))).ToList();
                                if (_pts.Count > BATHYDRACONIDAE)
                                {
                                    foreach (var r in _pts.Select(pt => GRect.Create(pt.ToPoint2d(), THESAURUSFORTIFICATION)))
                                    {
                                        geoData.RainPortSymbols.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.RainPortSymbols.Add(pl);
                                        item.RainPortSymbols.Add(pl);
                                        DrawTextLazy(THESAURUSMISOGYNIST, pl.GetCenter());
                                    }
                                }
                            }
                        }
                        else
                        if (text.Contains(THESAURUSINNOCENT) && text.Contains(THESAURUSCONSOLIDATE))
                        {
                            var lst = labellinesGeosf(label);
                            if (lst.Count == PIEZOELECTRICAL)
                            {
                                var labelLineGeo = lst[BATHYDRACONIDAE];
                                var pts = GeoFac.GetLabelLineEndPoints(GeoFac.GetLines(labelLineGeo).ToList(), label, DOLICHOCEPHALOUS);
                                var _pts = pts.Select(x => x.ToNTSPoint()).ToList();
                                var ptsf = GeoFac.CreateIntersectsSelector(_pts);
                                _pts = _pts.Except(ptsf(label)).Where(pt => item.Ditches.All(x => !x.Intersects(pt.Buffer(THESAURUSFORTIFICATION)))).ToList();
                                if (_pts.Count > BATHYDRACONIDAE)
                                {
                                    foreach (var r in _pts.Select(pt => GRect.Create(pt.ToPoint2d(), THESAURUSFORTIFICATION)))
                                    {
                                        geoData.Ditches.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.Ditches.Add(pl);
                                        item.Ditches.Add(pl);
                                        DrawTextLazy(THESAURUSMISOGYNIST, pl.GetCenter());
                                    }
                                }
                            }
                        }
                    }
                }
                {
                    var labelsf = F(item.Labels);
                    {
                        var pipesf = F(item.VerticalPipes);
                        foreach (var label in item.Labels)
                        {
                            if (!IsRainLabel(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                            var lst = labellinesGeosf(label);
                            if (lst.Count == PIEZOELECTRICAL)
                            {
                                var labelline = lst[BATHYDRACONIDAE];
                                if (pipesf(GeoFac.CreateGeometry(label, labelline)).Count == BATHYDRACONIDAE)
                                {
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(DOLICHOCEPHALOUS)).ToList(), label, radius: DOLICHOCEPHALOUS);
                                    if (points.Count == PIEZOELECTRICAL)
                                    {
                                        var pt = points[BATHYDRACONIDAE];
                                        if (!labelsf(pt.ToNTSPoint()).Any())
                                        {
                                            var r = GRect.Create(pt, STANDOFFISHNESS);
                                            geoData.VerticalPipes.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.VerticalPipes.Add(pl);
                                            item.VerticalPipes.Add(pl);
                                            DrawTextLazy(THESAURUSMISOGYNIST, pl.GetCenter());
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
                            if (lst.Count == PIEZOELECTRICAL)
                            {
                                var labellinesGeo = lst[BATHYDRACONIDAE];
                                if (labelsf(labellinesGeo).Count != PIEZOELECTRICAL) continue;
                                var lines = ExplodeGLineSegments(labellinesGeo).Where(x => x.IsValid).Distinct().ToList();
                                var geos = lines.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                                var f = F(geos);
                                var tmp = f(label).ToList();
                                if (tmp.Count == PIEZOELECTRICAL)
                                {
                                    var l1 = tmp[BATHYDRACONIDAE];
                                    tmp = f(l1).Where(x => x != l1).ToList();
                                    if (tmp.Count == PIEZOELECTRICAL)
                                    {
                                        var l2 = tmp[BATHYDRACONIDAE];
                                        if (lines[geos.IndexOf(l2)].IsHorizontal(DOLICHOCEPHALOUS))
                                        {
                                            tmp = f(l2).Where(x => x != l1 && x != l2).ToList();
                                            if (tmp.Count == PIEZOELECTRICAL)
                                            {
                                                var l3 = tmp[BATHYDRACONIDAE];
                                                var seg = lines[geos.IndexOf(l3)];
                                                var pts = new List<Point>() { seg.StartPoint.ToNTSPoint(), seg.EndPoint.ToNTSPoint() };
                                                var _tmp = pts.Except(GeoFac.CreateIntersectsSelector(pts)(l2.Buffer(INTERLINGUISTICS, EndCapStyle.Square))).ToList();
                                                if (_tmp.Count == PIEZOELECTRICAL)
                                                {
                                                    var ptGeo = _tmp[BATHYDRACONIDAE];
                                                    var pipes = pipesf(ptGeo);
                                                    if (pipes.Count == PIEZOELECTRICAL)
                                                    {
                                                        var pipe = pipes[BATHYDRACONIDAE];
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
                                    if (lst.Count == PIEZOELECTRICAL)
                                    {
                                        var labelline = lst[BATHYDRACONIDAE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == BATHYDRACONIDAE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(DOLICHOCEPHALOUS)).ToList(), label, radius: DOLICHOCEPHALOUS);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, QUOTATIONPATRONAL);
                                                geoData.GravityWaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain.GravityWaterBuckets.Add(pl);
                                                item.GravityWaterBuckets.Add(pl);
                                                DrawTextLazy(THESAURUSMISOGYNIST, pl.GetCenter());
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
                                    if (lst.Count == PIEZOELECTRICAL)
                                    {
                                        var labelline = lst[BATHYDRACONIDAE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == BATHYDRACONIDAE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(DOLICHOCEPHALOUS)).ToList(), label, radius: DOLICHOCEPHALOUS);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, QUOTATIONPATRONAL);
                                                geoData.SideWaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain.SideWaterBuckets.Add(pl);
                                                item.SideWaterBuckets.Add(pl);
                                                DrawTextLazy(THESAURUSMISOGYNIST, pl.GetCenter());
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
                                    if (lst.Count == PIEZOELECTRICAL)
                                    {
                                        var labelline = lst[BATHYDRACONIDAE];
                                        if (f(GeoFac.CreateGeometry(label, labelline)).Count == BATHYDRACONIDAE)
                                        {
                                            var lines = ExplodeGLineSegments(labelline);
                                            var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(DOLICHOCEPHALOUS)).ToList(), label, radius: DOLICHOCEPHALOUS);
                                            foreach (var pt in points)
                                            {
                                                if (labelsf(pt.ToNTSPoint()).Any()) continue;
                                                var r = GRect.Create(pt, QUOTATIONPATRONAL);
                                                geoData._87WaterBuckets.Add(r);
                                                var pl = r.ToPolygon();
                                                cadDataMain._87WaterBuckets.Add(pl);
                                                item._87WaterBuckets.Add(pl);
                                                DrawTextLazy(THESAURUSMISOGYNIST, pl.GetCenter());
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
                        for (int i = BATHYDRACONIDAE; i < THESAURUSINTELLECT; i++)
                        {
                            var ok = THESAURUSESPECIALLY;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == PIEZOELECTRICAL && pipes.Count == PIEZOELECTRICAL)
                                {
                                    var lb = labels[BATHYDRACONIDAE];
                                    var pp = pipes[BATHYDRACONIDAE];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSAMENITY;
                                    if (IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = THESAURUSNEGATIVE;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        for (int i = BATHYDRACONIDAE; i < THESAURUSINTELLECT; i++)
                        {
                            var ok = THESAURUSESPECIALLY;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > BATHYDRACONIDAE)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSAMENITY).ToList();
                                    if (labelsTxts.All(txt => IsMaybeLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = BATHYDRACONIDAE; k < pipes.Count; k++)
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
                                        ok = THESAURUSNEGATIVE;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        {
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? THESAURUSAMENITY;
                                if (!IsMaybeLabelText(lb)) continue;
                                var lst = labellinesGeosf(label);
                                if (lst.Count == PIEZOELECTRICAL)
                                {
                                    var labelline = lst[BATHYDRACONIDAE];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == PIEZOELECTRICAL)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[BATHYDRACONIDAE].ToNTSPoint());
                                        if (pipes.Count == PIEZOELECTRICAL)
                                        {
                                            var pp = pipes[BATHYDRACONIDAE];
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
                            var ok = THESAURUSESPECIALLY;
                            for (int i = BATHYDRACONIDAE; i < THESAURUSINTELLECT; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in wlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == PIEZOELECTRICAL && lst2.Count > BATHYDRACONIDAE)
                                    {
                                        var pp1 = lst1[BATHYDRACONIDAE];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (INTERLINGUISTICS < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSNEGATIVE;
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSNEGATIVE;
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
                            var ok = THESAURUSESPECIALLY;
                            for (int i = BATHYDRACONIDAE; i < THESAURUSINTELLECT; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(DOLICHOCEPHALOUS).ToGCircle(THESAURUSESPECIALLY).ToCirclePolygon(PARALLELOGRAMMIC));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > PIEZOELECTRICAL)
                                        {
                                            fs.Add(() =>
                                            {
                                                shortTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSNEGATIVE;
                                            });
                                        }
                                    }
                                    if (fs.Count == PIEZOELECTRICAL) fs[BATHYDRACONIDAE]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = BATHYDRACONIDAE; i < THESAURUSINTELLECT; i++)
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
                        foreach (var label in d.Where(x => x.Value > PIEZOELECTRICAL).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == TEREBINTHINATED)
                            {
                                var dis = pps[BATHYDRACONIDAE].GetCenter().GetDistanceTo(pps[PIEZOELECTRICAL].GetCenter());
                                if (INTERLINGUISTICS < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
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
                                        if (fds.Count > BATHYDRACONIDAE)
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
                    var nearestAiringMachinef = GeoFac.NearestNeighbourGeometryF(item.AiringMachine_Vertical.Concat(item.AiringMachine_Hanging).ToList());
                    var ok_ents = new HashSet<Geometry>();
                    var todoD = new Dictionary<Geometry, string>();
                    var pipesf = F(item.VerticalPipes);
                    var gs = GeoFac.GroupGeometries(item.CondensePipes.Concat(item.WLines).ToList());
                    foreach (var g in gs)
                    {
                        var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        var pipes = pipesf(G(cps.Concat(wlines)));
                        if (pipes.Count == PIEZOELECTRICAL)
                        {
                            var label = getLabel(pipes[BATHYDRACONIDAE]);
                            if (label != null)
                            {
                                if (cps.Count > BATHYDRACONIDAE)
                                {
                                    drData.HasCondensePipe.Add(label);
                                    ok_ents.AddRange(cps);
                                }
                                if (cps.Count == TEREBINTHINATED)
                                {
                                    drData.HasNonBrokenCondensePipes.Add(label);
                                    var airingMachine = nearestAiringMachinef(GeoFac.CreateGeometryEx(cps));
                                    if (item.AiringMachine_Hanging.Contains(airingMachine))
                                    {
                                        drData.PlsDrawCondensePipeHigher.Add(label);
                                    }
                                }
                                if (cps.Count == PIEZOELECTRICAL)
                                {
                                    var cp = cps[BATHYDRACONIDAE];
                                    todoD[cp] = label;
                                    var airingMachine = nearestAiringMachinef(cp);
                                    if (item.AiringMachine_Hanging.Contains(airingMachine))
                                    {
                                        drData.PlsDrawCondensePipeHigher.Add(label);
                                    }
                                }
                            }
                        }
                    }
                    {
                        var cpsf = F(item.CondensePipes.Except(ok_ents).ToList());
                        foreach (var kv in todoD)
                        {
                            var cp = kv.Key;
                            var label = kv.Value;
                            var pt = cp.GetCenter();
                            var cps = cpsf(new GLineSegment(pt.OffsetX(-CHEMOTROPICALLY), pt.OffsetX(CHEMOTROPICALLY)).ToLineString()).Except(ok_ents).ToList();
                            if (cps.Count == PIEZOELECTRICAL)
                            {
                                ok_ents.AddRange(cps);
                                drData.HasBrokenCondensePipes.Add(label);
                            }
                        }
                    }
                }
                IEnumerable<Geometry> getOkPipes()
                {
                    foreach (var kv in lbDict)
                    {
                        if (IsRainLabel(kv.Value))
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
                        if (gbks.Count == PIEZOELECTRICAL)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == PIEZOELECTRICAL)
                            {
                                drData.ConnectedToGravityWaterBucket.Add(lbDict[pipes[BATHYDRACONIDAE]]);
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
                        if (sbks.Count == PIEZOELECTRICAL)
                        {
                            var pipes = pipesf(wlineGeo);
                            if (pipes.Count == PIEZOELECTRICAL)
                            {
                                drData.ConnectedToSideWaterBucket.Add(lbDict[pipes[BATHYDRACONIDAE]]);
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
                                        rainPortIdDict[label] = cadDataMain.RainPortSymbols.IndexOf(rainPort);
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
                            var geo = x.Buffer(UNCONJECTURABLE); geo.UserData = cadDataMain.WaterSealingWells.IndexOf(x);
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
                                if (waterWells.Count == PIEZOELECTRICAL)
                                {
                                    var waterWell = waterWells[BATHYDRACONIDAE];
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
                            var waterWells = spacialIndex.ToList(geoData.WaterWells).Select(x => x.Expand(PSYCHOPHYSIOLOGICAL).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterWellLabels[spacialIndex[waterWells.IndexOf(waterWell)]], waterWell => spacialIndex[waterWells.IndexOf(waterWell)]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_vpipes).ToList());
                        var radius = INTERLINGUISTICS;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterWells);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(PARALLELOGRAMMIC, THESAURUSESPECIALLY)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterWells).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= PRAETERNATURALIS)
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
                                var _pts = ptsf(wp.Buffer(THESAURUSFORTIFICATION));
                                if (_pts.Count > BATHYDRACONIDAE)
                                {
                                    var kv = geoData.WrappingPipeRadius[pts.IndexOf(_pts[BATHYDRACONIDAE])];
                                    var radiusText = kv.Value;
                                    if (string.IsNullOrWhiteSpace(radiusText)) radiusText = CH2OHRCHNH2RCOOH;
                                    drData.OutletWrappingPipeRadiusStringDict[cadDataMain.WrappingPipes.IndexOf(wp)] = radiusText;
                                }
                            }
                        }
                    }
                    {
                        var hs = new HashSet<string>();
                        var geo = G(item.WLines);
                        foreach (var kv in lbDict)
                        {
                            var pipe = kv.Key;
                            var label = kv.Value;
                            if (IsRainLabel(label))
                            {
                                if (hs.Contains(label)) continue;
                                if (geo.Intersects(pipe))
                                {
                                    hs.Add(label);
                                }
                            }
                        }
                        drData.Spreadings.AddRange(lbDict.Values.Where(IsRainLabel).Except(hs));
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
                            return THESAURUSBESTRIDE;
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
                            var m = Regex.Match(kv.Value, THESAURUSANNIHILATE);
                            if (m.Success)
                            {
                                var floor = m.Groups[PIEZOELECTRICAL].Value;
                                var pipe = kv.Key;
                                var pipes = getOkPipes().ToList();
                                var wlines = wlinesf(pipe);
                                if (wlines.Count == PIEZOELECTRICAL)
                                {
                                    foreach (var pp in F(pipes)(wlines[BATHYDRACONIDAE]))
                                    {
                                        drData.RoofWaterBuckets.Add(new KeyValuePair<string, string>(lbDict[pp], floor));
                                    }
                                }
                                continue;
                            }
                        }
                        if (kv.Value.Contains(PLEASURABLENESS))
                        {
                            var floor = THESAURUSFESTER;
                            var pipe = kv.Key;
                            var pipes = getOkPipes().ToList();
                            var wlines = wlinesf(pipe);
                            if (wlines.Count == PIEZOELECTRICAL)
                            {
                                foreach (var pp in F(pipes)(wlines[BATHYDRACONIDAE]))
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
                    if (item.WaterWells.Count + item.RainPortSymbols.Count + item.WaterSealingWells.Count + item.WaterPorts.Count > BATHYDRACONIDAE)
                    {
                        var ok_fds = new HashSet<Geometry>();
                        var wlinesf = F(wlinesGeos);
                        var alone_fds = new HashSet<Geometry>();
                        var side = new MultiPoint(geoData.SideFloorDrains.Select(x => x.ToNTSPoint()).ToArray());
                        var fdsf = F(item.FloorDrains.Where(x => !x.Intersects(side)).ToList());
                        var aloneFloorDrainInfos = new List<AloneFloorDrainInfo>();
                        var bufSize = THESAURUSCRITIC;
                        foreach (var ditch in item.Ditches)
                        {
                            foreach (var wline in wlinesf(ditch.EnvelopeInternal.ToGRect().Expand(UNCONJECTURABLE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > BATHYDRACONIDAE)
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
                        foreach (var port in item.RainPortSymbols)
                        {
                            foreach (var wline in wlinesf(port.EnvelopeInternal.ToGRect().Expand(UNCONJECTURABLE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > BATHYDRACONIDAE)
                                {
                                    ok_fds.AddRange(fds);
                                    var id = cadDataMain.RainPortSymbols.IndexOf(port);
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
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(UNCONJECTURABLE).ToPolygon()))
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
                                if (fds.Count > BATHYDRACONIDAE)
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
                            foreach (var wline in wlinesf(well.EnvelopeInternal.ToGRect().Expand(UNCONJECTURABLE).ToPolygon()))
                            {
                                var fds = fdsf(wline);
                                var _fds = fds.Except(ok_fds).ToList();
                                fds = _fds.Where(x => rainpipesf(x.Buffer(bufSize)).Any()).ToList();
                                alone_fds.AddRange(_fds.Except(fds));
                                if (fds.Count > BATHYDRACONIDAE)
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
                    for (int i = BATHYDRACONIDAE; i < drData.GravityWaterBuckets.Count; i++)
                    {
                        drData.GravityWaterBucketLabels.Add(null);
                    }
                    for (int i = BATHYDRACONIDAE; i < drData.SideWaterBuckets.Count; i++)
                    {
                        drData.SideWaterBucketLabels.Add(null);
                    }
                    for (int i = BATHYDRACONIDAE; i < drData._87WaterBuckets.Count; i++)
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
                            if (labels.Count == PIEZOELECTRICAL && bks.Count == PIEZOELECTRICAL)
                            {
                                var lb = labels[BATHYDRACONIDAE];
                                var bk = bks[BATHYDRACONIDAE];
                                var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSAMENITY;
                                if (IsWaterBucketLabel(label))
                                {
                                    if (!label.ToUpper().Contains(THESAURUSCAPTIVATE))
                                    {
                                        var lst = labelsf(lb.GetCenter().OffsetY(-THESAURUSBEFRIEND).ToNTSPoint());
                                        lst.Remove(lb);
                                        if (lst.Count == PIEZOELECTRICAL)
                                        {
                                            var _label = geoData.Labels[cadDataMain.Labels.IndexOf(lst[BATHYDRACONIDAE])].Text ?? THESAURUSAMENITY;
                                            if (_label.Contains(THESAURUSCAPTIVATE))
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
                            if (i >= BATHYDRACONIDAE)
                            {
                                i = drData.GravityWaterBuckets.IndexOf(geoData.GravityWaterBuckets[i]);
                                if (i >= BATHYDRACONIDAE)
                                {
                                    drData.GravityWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain.SideWaterBuckets.IndexOf(kv.Key);
                            if (i >= BATHYDRACONIDAE)
                            {
                                i = drData.SideWaterBuckets.IndexOf(geoData.SideWaterBuckets[i]);
                                if (i >= BATHYDRACONIDAE)
                                {
                                    drData.SideWaterBucketLabels[i] = kv.Value;
                                    continue;
                                }
                            }
                            i = cadDataMain._87WaterBuckets.IndexOf(kv.Key);
                            if (i >= BATHYDRACONIDAE)
                            {
                                i = drData._87WaterBuckets.IndexOf(geoData._87WaterBuckets[i]);
                                if (i >= BATHYDRACONIDAE)
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
            _DrawingTransaction.Current.AbleToDraw = THESAURUSNEGATIVE;
        }
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
        public static void DrawGeoData(RainGeoData geoData, RainCadData cadDataMain, List<RainCadData> cadDatas)
        {
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = PIEZOELECTRICAL;
            }
            for (int storeyI = BATHYDRACONIDAE; storeyI < cadDatas.Count; storeyI++)
            {
                var item = cadDatas[storeyI];
                DrawGeoData(geoData, cadDataMain, storeyI, item);
            }
        }
    }
    public class WaterBucketItem : IEquatable<WaterBucketItem>
    {
        public override int GetHashCode()
        {
            return BATHYDRACONIDAE;
        }
        public string DN;
        public WaterBucketEnum WaterBucketType;
        public static bool operator ==(WaterBucketItem x, WaterBucketItem y)
        {
            return (x is null && y is null) || (x is not null && y is not null && x.Equals(y));
        }
        public bool Equals(WaterBucketItem other)
        {
            return this.WaterBucketType == other.WaterBucketType
                && this.DN == other.DN;
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
            string dn = GetDN(text);
            return new WaterBucketItem() { DN = dn, WaterBucketType = bkType, };
        }
        public string GetDisplayString()
        {
            switch (WaterBucketType)
            {
                case WaterBucketEnum.None:
                    throw new System.Exception();
                case WaterBucketEnum.Gravity:
                    return THESAURUSRETAINER + DN;
                case WaterBucketEnum.Side:
                    return NONSENSICALNESS + DN;
                case WaterBucketEnum._87:
                    return QUOTATIONPREMIÈRE + DN;
                default:
                    throw new System.Exception();
            }
        }
    }
    public enum OutletType
    {
        雨水井,
        雨水口,
        水封井,
        排水沟,
        散排,
    }
    public class RainGroupedPipeItem
    {
        public string OutletFloor;
        public List<string> Labels;
        public bool HasLineAtBuildingFinishedSurfice;
        public List<string> WaterWellLabels;
        public bool HasOutletWrappingPipe;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell;
        public List<string> TlLabels;
        public OutletType OutletType;
        public bool HasSingleFloorDrainDrainageForRainPort;
        public bool HasSingleFloorDrainDrainageForDitch;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public bool HasSingleFloorDrainDrainageForWaterWell;
        public List<RainGroupingPipeItem.Hanging> Hangings;
        public int FloorDrainsCountAt1F;
        public PipeType PipeType;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForDitch;
        public bool HasWaterWell => WaterWellLabels != null && WaterWellLabels.Count > BATHYDRACONIDAE;
        public List<RainGroupingPipeItem.ValueItem> Items;
        public bool HasSingleFloorDrainDrainageForWaterSealingWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForRainPort;
        public string OutletWrappingPipeRadius;
        public bool HasTl => TlLabels != null && TlLabels.Count > BATHYDRACONIDAE;
    }
    public class RainGroupingPipeItem : IEquatable<RainGroupingPipeItem>
    {
        public List<Hanging> Hangings;
        public bool HasSingleFloorDrainDrainageForWaterWell;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForRainPort;
        public bool HasSingleFloorDrainDrainageForDitch;
        public int FloorDrainsCountAt1F;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForDitch;
        public List<ValueItem> Items;
        public string OutletWrappingPipeRadius;
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
                return BATHYDRACONIDAE;
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
        public OutletType OutletType;
        public bool HasSingleFloorDrainDrainageForWaterSealingWell;
        public struct ValueItem
        {
            public bool Exist;
            public bool HasLong;
            public bool HasShort;
        }
        public string WaterWellLabel;
        public string OutletFloor;
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
        public bool HasOutletWrappingPipe;
        public bool HasLineAtBuildingFinishedSurfice;
        public bool IsSingleOutlet;
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterWell;
        public bool HasSingleFloorDrainDrainageForRainPort;
        public override int GetHashCode()
        {
            return BATHYDRACONIDAE;
        }
        public bool IsFloorDrainShareDrainageWithVerticalPipeForWaterSealingWell;
        public PipeType PipeType;
        public string Label;
    }
    public enum PipeType
    {
        Y1L, Y2L, NL, YL, FL0
    }
    public class PipeRunLocationInfo
    {
        public Point2d PlBasePt;
        public List<GLineSegment> RightSegsMiddle;
        public Point2d HangingEndPoint;
        public Point2d BasePoint;
        public string Storey;
        public List<GLineSegment> RightSegsLast;
        public List<GLineSegment> RightSegsFirst;
        public List<Vector2d> Vector2ds;
        public bool Visible;
        public List<GLineSegment> DisplaySegs;
        public List<GLineSegment> Segs;
        public Point2d EndPoint;
        public Point2d StartPoint;
    }
    public class StoreyContext
    {
        public List<StoreyInfo> StoreyInfos;
    }
    public class CommandContext
    {
        public Point3dCollection range;
        public System.Windows.Window window;
        public Diagram.ViewModel.RainSystemDiagramViewModel ViewModel;
        public StoreyContext StoreyContext;
    }
    public class ThRainService
    {
        public static void CollectFloorListDatasEx(bool focus)
        {
            if (focus) FocusMainWindow();
            ThMEPWSS.Common.FramedReadUtil.SelectFloorFramed(out _, () =>
            {
                using (DocLock)
                {
                    var range = TrySelectRangeEx();
                    if (range == null) return;
                    using var adb = AcadDatabase.Active();
                    var (ctx, brs) = GetStoreyContext(range, adb);
                    commandContext.StoreyContext = ctx;
                    InitFloorListDatas(adb, brs);
                }
            });
        }
        public static void PreFixGeoData(RainGeoData geoData)
        {
            geoData.FixData();
            foreach (var ct in geoData.Labels)
            {
                ct.Boundary = ct.Boundary.Expand(-DISCOMFORTABLENESS);
            }
            geoData.Labels = geoData.Labels.Where(x => IsMaybeLabelText(x.Text)).ToList();
            for (int i = BATHYDRACONIDAE; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(DOLICHOCEPHALOUS))
                {
                    geoData.LabelLines[i] = seg.Extend(PARALLELOGRAMMIC);
                }
                else if (seg.IsVertical(DOLICHOCEPHALOUS))
                {
                    geoData.LabelLines[i] = seg.Extend(PIEZOELECTRICAL);
                }
            }
            for (int i = BATHYDRACONIDAE; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(DOLICHOCEPHALOUS);
            }
            for (int i = BATHYDRACONIDAE; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(DOLICHOCEPHALOUS);
            }
            for (int i = BATHYDRACONIDAE; i < geoData.WLines.Count; i++)
            {
                geoData.WLines[i] = geoData.WLines[i].Extend(DOLICHOCEPHALOUS);
            }
            for (int i = BATHYDRACONIDAE; i < geoData.VerticalPipes.Count; i++)
            {
                geoData.VerticalPipes[i] = geoData.VerticalPipes[i].Expand(INTERLINGUISTICS);
            }
            {
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < CHEMOTROPICALLY && x.Height < CHEMOTROPICALLY).ToList();
            }
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSCOUNCIL)).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, INTERLINGUISTICS))).ToList();
            }
            {
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.GravityWaterBuckets = GeoFac.GroupGeometries(geoData.GravityWaterBuckets.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(THESAURUSCOUNCIL);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.CondensePipes = geoData.CondensePipes.Distinct(cmp).ToList();
                geoData.PipeKillers = geoData.PipeKillers.Distinct(cmp).ToList();
            }
            {
                for (int i = BATHYDRACONIDAE; i < geoData.WaterWellLabels.Count; i++)
                {
                    var label = geoData.WaterWellLabels[i];
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        geoData.WaterWellLabels[i] = CONTEMPTIBILITY;
                    }
                }
            }
            {
                var okPts = new HashSet<Point2d>(geoData.WrappingPipeRadius.Select(x => x.Key));
                var lbs = geoData.WrappingPipeLabels.Select(x => x.ToPolygon()).ToList();
                var lbsf = GeoFac.CreateIntersectsSelector(lbs);
                var lines = geoData.WrappingPipeLabelLines.Select(x => x.ToLineString()).ToList();
                var gs = GeoFac.GroupLinesByConnPoints(lines, HOOGMOGENDHEIDEN);
                foreach (var geo in gs)
                {
                    var segs = GeoFac.GetLines(geo).ToList();
                    var buf = segs.Where(x => x.IsHorizontal(DOLICHOCEPHALOUS)).Select(x => x.Buffer(QUOTATIONPATRONAL)).FirstOrDefault();
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
                var v = THESAURUSFORTIFICATION;
                for (int i = BATHYDRACONIDAE; i < geoData.WrappingPipes.Count; i++)
                {
                    var wp = geoData.WrappingPipes[i];
                    if (wp.Width > v * TEREBINTHINATED)
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
                    if (ct.Text.StartsWith(THESAURUSDEFICIENCY))
                    {
                        ct.Text = ct.Text.Substring(THESAURUSINTELLECT);
                    }
                    else if (ct.Text.StartsWith(THESAURUSEMBODY))
                    {
                        ct.Text = ct.Text.Substring(TEREBINTHINATED);
                    }
                    else if (ct.Text.StartsWith(DISPROPORTIONALLY))
                    {
                        ct.Text = THESAURUSELUCIDATION + ct.Text.Substring(THESAURUSINTELLECT);
                    }
                }
            }
            geoData.FixData();
        }
        public static void ConnectLabelToLabelLine(RainGeoData geoData)
        {
            static bool f(string s)
            {
                if (s == null) return THESAURUSESPECIALLY;
                if (IsMaybeLabelText(s)) return THESAURUSNEGATIVE;
                return THESAURUSESPECIALLY;
            }
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Where(x => f(x.Text)).Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(INTERLINGUISTICS)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-INTERLINGUISTICS).OffsetY(-HYPERSENSITIZED), PRAETERNATURALIS, HYPERSENSITIZED);
                var _lineHGs = f1(g.ToPolygon());
                var geo = GeoFac.NearestNeighbourGeometryF(_lineHGs)(bd.Center.ToNTSPoint());
                if (geo == null) continue;
                {
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(QUOTATIONPATRONAL, PSYCHOPHYSIOLOGICAL) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(THESAURUSCOUNCIL, PSYCHOPHYSIOLOGICAL))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(THESAURUSCOUNCIL));
                    }
                }
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
                if (geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSNEGATIVE)
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
        public static CommandContext commandContext;
    }
    public class StoreyInfo
    {
        public StoreyType StoreyType;
        public List<int> Numbers;
        public GRect Boundary;
        public Point2d ContraPoint;
    }
    public static class THRainService
    {
        public const int THESAURUSCARTOON = 20;
        public const int THESAURUSAUSPICIOUS = 501;
        public const char INSTITUTIONALISM = ',';
        public const int MINERALOCORTICOID = 8192;
        public const string THESAURUSDISPENSATION = "W-DRAIN-2";
        public const string THESAURUSVAGRANT = "WaterWellIds:";
        public const string THESAURUSLECHEROUS = "SideWaterBuckets";
        public const string THESAURUSSPELLBOUND = "W-RAIN-NOTE";
        public const string THESAURUSBOILING = "FloorDrains";
        public const string THESAURUSAGNOSTIC = "地漏";
        public const string INCONSUMPTIBILIS = @"^重力型雨水斗(DN\d+)$";
        public const int PRAETERNATURALIS = 1500;
        public const double THESAURUSADVANCEMENT = .7;
        public const string THESAURUSEXULTATION = "-0.XX";
        public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() is THESAURUSATTENDANCE && x.IsDynamicBlock).ToList();
        public const int THESAURUSTESTIMONY = 0xae;
        public const int THESAURUSMANAGEABLE = 2161;
        public const string THESAURUSCLOTHES = "AloneFloorDrainInfos:";
        public const string THESAURUSASSIGN = "重力流雨水井编号";
        public const int THESAURUSRETAIN = 8000;
        public const string THESAURUSCOMPREHEND = "可见性";
        public const int THESAURUSREGARDING = 4096;
        public const int THESAURUSLABYRINTHINE = 479;
        public const string THESAURUSINCULCATE = "雨水口";
        public static List<int> ParseFloorNums(string floorStr)
        {
            if (string.IsNullOrWhiteSpace(floorStr)) return new List<int>();
            floorStr = floorStr.Replace(DELETERIOUSNESS, INSTITUTIONALISM).Replace(THESAURUSFILIGREE, THESAURUSOVERACT).Replace(PHENYLENEDIAMINE, THESAURUSAMENITY).Replace(UNCONSTITUTIONAL, THESAURUSAMENITY).Replace(THESAURUSUNRELENTING, THESAURUSAMENITY);
            var hs = new HashSet<int>();
            foreach (var s in floorStr.Split(INSTITUTIONALISM))
            {
                if (string.IsNullOrEmpty(s)) continue;
                var m = Regex.Match(s, THESAURUSAFFILIATION);
                if (m.Success)
                {
                    var v1 = int.Parse(m.Groups[PIEZOELECTRICAL].Value);
                    var v2 = int.Parse(m.Groups[TEREBINTHINATED].Value);
                    var min = Math.Min(v1, v2);
                    var max = Math.Max(v1, v2);
                    for (int i = min; i <= max; i++)
                    {
                        hs.Add(i);
                    }
                    continue;
                }
                m = Regex.Match(s, DETERMINATIVENESS);
                if (m.Success)
                {
                    hs.Add(int.Parse(m.Value));
                }
            }
            hs.Remove(BATHYDRACONIDAE);
            return hs.OrderBy(x => x).ToList();
        }
        public const string STOICHIOMETRICALLY = "1000";
        public const string THESAURUSEXCOMMUNICATE = "W-BUSH";
        public const double THESAURUSPROPRIETOR = .01;
        public const int THESAURUSCONFECTIONERY = 1800;
        public static bool IsPL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, THESAURUSEFFUSION);
        }
        public const string TRANSUBSTANTIATE = "重力流雨水斗";
        public const string THESAURUSACCUSTOM = "87型雨水斗DN100";
        public const string CHAMAECYPARISSUS = @"(DN\d+)";
        public static bool IsToilet(string roomName)
        {
            var roomNameContains = new List<string>
            {
                THESAURUSPRELUDE,THESAURUSSEVERITY,LUCIOCEPHALIDAE,
                THESAURUSLUSTFUL,THESAURUSSATIATE,PROSELYTIZATION,
            };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSESPECIALLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSNEGATIVE;
            if (roomName.Equals(UNCONSCIENTIOUS))
                return THESAURUSNEGATIVE;
            return Regex.IsMatch(roomName, THESAURUSEMPLOYMENT);
        }
        public const string THESAURUSCONSUME = "≥600";
        public const int THESAURUSSUNLESS = 1481;
        public const string THESAURUSDEFILE = "1F";
        public const int ANTHROPOMORPHITES = 658;
        public const string THESAURUSDICTIONARY = "TCH_PIPE";
        public const int INTERLINGUISTICS = 10;
        public const int PERCHLOROETHYLENE = 7;
        public const string THESAURUSRECRIMINATION = "W-DRAI-EQPM";
        public const bool THESAURUSESPECIALLY = false;
        public const string THESAURUSSUSCEPTIBILITY = "侧入式雨水斗DN100";
        public const int GASTRONOMICALLY = 1258;
        public const string THESAURUSPERNICIOUS = "楼层编号";
        public const string TRICHOBATRACHUS = "侧排地漏";
        public const string THESAURUSGOODNESS = "W-NOTE";
        public const string PSYCHOLINGUISTICALLY = "普通地漏无存水弯";
        public static bool IsRainLabel(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label) || IsFL0(label);
        }
        public const string THESAURUSCENSOR = "通气帽系统";
        public static bool IsFL0(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return IsFL(label) && label.Contains(THESAURUSBURNING);
        }
        public const int THESAURUSBUBBLY = 2693;
        public const string THESAURUSFURTHEST = "基点 Y";
        public const string TRANSLATIONALLY = "HasNonBrokenCondensePipes：";
        public const string THESAURUSREPREHENSIBLE = "伸顶通气管";
        public const string THESAURUSELUCIDATE = "TCH_MTEXT";
        public const int PROCHLORPERAZINE = 318;
        public const string THESAURUSDEFICIENCY = "73-";
        public const string THESAURUSBURNING = "-0";
        public const double QUOTATIONCOLLARED = 5500.0;
        public const int INTERPRETATIONS = 3860;
        public static bool Is87WaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && !IsGravityWaterBucketLabel(label) && !IsSideWaterBucketLabel(label) && label.Contains(THESAURUSKNOWLEDGEABLE);
        }
        public static bool IsFL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, THESAURUSBLISTER);
        }
        public static bool IsWL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, PARALINGUISTICALLY);
        }
        public const string THESAURUSHOODLUM = @"^([^\-]*\-[A-Z])(\d+)$";
        public const string UNDERDEVELOPMENT = "长转管:";
        public const int THESAURUSCENTRAL = 431;
        public static bool IsGravityWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(QUOTATIONBROADSIDE);
        }
        public const int THESAURUSSELLING = 1712;
        public const int MELANCHOLIOUSNESS = 255;
        public const string MICROSPECTROPHOTOMETRY = "666";
        public const int PARALLELOGRAMMIC = 6;
        public const string QUOTATIONBROADSIDE = "重力";
        public const string EXCEPTIONALNESS = "HasCondensePipe：";
        public const char DELETERIOUSNESS = '，';
        public const string PROGNOSTICATIVE = @"\d+\-";
        public const string THESAURUSKNICKERS = "error occured while getting baseX and baseY";
        public const string NEUROTRANSMITTER = "W-RAIN-PIPE";
        public const int THESAURUSACCORDANCE = 111;
        public const string CHRONOSTRATIGRAPHIC = "接至排水沟";
        public const string COMFORTABLENESS = "重力雨水斗DN100";
        public const int HOOGMOGENDHEIDEN = 15;
        public const int THESAURUSCOMPOSURE = 1280;
        public const string THESAURUSSUPPOSITION = "套管";
        public const string QUOTATIONEMBRYOID = "立管检查口";
        public const int SUPERCILIOUSNESS = 580;
        public const int THESAURUSABUNDANCE = 9;
        public const string THESAURUSSATIATE = "客卫";
        public const string CHRISTIANIZATION = "~";
        public const string QUOTATIONPEIRCE = "TCH_EQUIPMENT";
        public const string THESAURUSLUSTFUL = "次卫";
        public const int THESAURUSBELIEVE = 35;
        public const double THESAURUSRECAPITULATE = 1800.0;
        public const string THESAURUSINDIGESTION = "FloorDrainShareDrainageWithVerticalPipeForDitch:";
        public const string UNCONSCIENTIOUS = "卫";
        public const string THESAURUSCANDLE = "A$C6BDE4816";
        public const string PARALINGUISTICALLY = @"^W\d?L";
        public const string THESAURUSQUANTITY = "Spreadings：";
        public const string THESAURUSAMENITY = "";
        public const int THESAURUSENDING = 230;
        public const string THESAURUSCAPSIZE = "立管：";
        public const string DISCIPLINABILIS = "SideWaterBucket";
        public const string THESAURUSOBJECTIVELY = "W-DRAI-DOME-PIPE";
        public const string QUOTATIONVITAMIN = "$W-DRAIN-2";
        public const string THESAURUSMERRIMENT = "TCH_MULTILEADER";
        public const string THESAURUSMUDDLE = @"^(Y1L|Y2L|NL)(\w*)\-(\d*)([a-zA-Z]*)$";
        public const int THESAURUSSETBACK = 121;
        public const string THESAURUSANNIHILATE = @"接(\d+F)屋面雨水斗";
        public const int THESAURUSBEFRIEND = 500;
        public const string THESAURUSBALDERDASH = "带定位立管";
        public static void FixStoreys(List<StoreyInfo> storeys)
        {
            var lst1 = storeys.Where(s => s.Numbers.Count == PIEZOELECTRICAL).Select(s => s.Numbers[BATHYDRACONIDAE]).ToList();
            foreach (var s in storeys.Where(s => s.Numbers.Count > PIEZOELECTRICAL).ToList())
            {
                var hs = new HashSet<int>(s.Numbers);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Numbers.Clear();
                s.Numbers.AddRange(hs.OrderBy(i => i));
            }
        }
        public const int ULTRAMICROTOMED = 1600;
        public const int THESAURUSBESTRIDE = 666;
        public const string THESAURUSBEGINNING = "ConnectedToSideWaterBucket";
        public static bool IsWaterBucketLabel(string label)
        {
            return label.Contains(BLAMEWORTHINESS);
        }
        public const string THESAURUSOVERCHARGE = "HasSingleFloorDrainDrainageForRainPort:";
        public const int CHEMOTROPICALLY = 700;
        public const int THESAURUSFORTIFICATION = 50;
        public const string THESAURUSFORMULATE = "HasRainPortSymbols：";
        public const string THESAURUSBLISTER = @"^F\d?L";
        public const int THESAURUSINDOCTRINATE = 453;
        public const string THESAURUSREPRESSION = "套管系统";
        public const int THESAURUSTRAGEDY = 150;
        public const int THESAURUSAPPORTION = 750;
        public const string THESAURUSEFFUSION = @"^P\d?L";
        public const int THESAURUSPERFIDY = 1070;
        public const string THESAURUSNARRATION = "WaterSealingWellIds:";
        public const int THESAURUSCORRESPONDENT = 2200;
        public const string THESAURUSALTERATION = "87WaterBuckets";
        public const string SYNERGISTICALLY = "$W-DRAIN-1";
        public const string THESAURUSLOVING = "$LIGUAN";
        public const string PROSELYTIZATION = "洗手间";
        public const int UNCONJECTURABLE = 200;
        public const string THESAURUSCONTRADICT = "散排至";
        public const string THESAURUSRETAINER = "重力雨水斗";
        public static StoreyInfo GetStoreyInfo(BlockReference br)
        {
            var props = br.DynamicBlockReferencePropertyCollection;
            return new StoreyInfo()
            {
                StoreyType = GetStoreyType((string)props.GetValue(TRANSUBSTANTIALIS)),
                Numbers = ParseFloorNums(GetStoreyNumberString(br)),
                ContraPoint = GetContraPoint(br),
                Boundary = br.Bounds.ToGRect(),
            };
        }
        public const string THESAURUSREDOUBTABLE = "W-DRAIN-5";
        public const int QUOTATIONCHOROID = 1200;
        public const string THESAURUSNESTLE = "厨房";
        public const int CHLOROFLUOROCARBONS = 800;
        public const string THESAURUSINCIDENTAL = "标高";
        public const string THESAURUSCONTRACTION = "地漏：";
        public const string QUOTATIONTELLUROUS = @"^([^\-]*)\-([A-Z])$";
        public const string OVERCURIOUSNESS = "WaterWellWrappingPipeRadiusStringDict:";
        public const int THESAURUSSCANDAL = 154;
        public const string THESAURUSOUTCRY = "阳台";
        public const int THESAURUSMANKIND = 650;
        public const int HYPERSENSITIZED = 250;
        public const string DISCRIMINATIVELY = "通气帽系统-AI2";
        public const string DISCONTINUATION = "GravityWaterBuckets";
        public const int THESAURUSADMISSION = 36;
        public const string THESAURUSSMASHING = "W-DRAI-NOTE";
        public const string THESAURUSCHAUVINISM = "H-AC-4";
        public const string THESAURUSFINANCIAL = "短转管:";
        public const int BATHYDRACONIDAE = 0;
        public const int ULTRACENTRIFUGE = 821;
        public const int PIEZOELECTRICAL = 1;
        public const string HANDCRAFTSMANSHIP = "W-DRAIN-1";
        public const double THESAURUSCOUNCIL = .1;
        public const int DOLICHOCEPHALOUS = 5;
        public const string THESAURUSBENEFIT = "*";
        public const int KONINGKLIPVISCH = 274;
        public const string THESAURUSMISOGYNIST = "FromImagination";
        public const int THESAURUSFELLOW = 0x91;
        public const string THESAURUSLAYOUT = "排出：";
        public const string THESAURUSGENTILITY = "可见性1";
        public const int THESAURUSBREAST = 1106;
        public const string QUOTATIONADJACENT = "污废合流井编号";
        public const string THESAURUSMAJESTY = "|";
        public const string FERROELECTRICALLY = "DL";
        public const string QUOTATIONALVEOLAR = "FloorDrainShareDrainageWithVerticalPipeForRainPort:";
        public const int THESAURUSYAWNING = 18;
        public const char THESAURUSFILIGREE = 'B';
        public const string QUOTATIONPREMIÈRE = "87雨水斗";
        public const string THESAURUSREMEDY = "$W-DRAIN-5";
        public const string NONSENSICALNESS = "侧入式雨水斗";
        public const string QUOTATIONVENICE = "TCH_VPIPEDIM";
        public const int THESAURUSDISPUTE = 30;
        public const string THESAURUSPUDDLE = "YL";
        public const string THESAURUSLUGGAGE = ",";
        public const int THESAURUSPACKET = 281;
        public const string THESAURUSACQUIRE = "伸顶通气2000";
        public const string UNSELFCONSCIOUSNESS = "DitchIds:";
        public const int HYDROXYNAPHTHALENE = 211;
        public const string THESAURUSBLARNEY = "$QYSD";
        public static string TryParseWrappingPipeRadiusText(string text)
        {
            if (text == null) return null;
            var t = Regex.Replace(text, THESAURUSBYPASS, THESAURUSAMENITY);
            t = Regex.Replace(t, PROGNOSTICATIVE, CONTEMPTIBILITY);
            return t;
        }
        public const int QUOTATIONPATRONAL = 100;
        public const string THESAURUSJOCULAR = "H-AC-1";
        public const string CORRESPONDINGLY = "NL";
        public const int STANDOFFISHNESS = 55;
        public const int THESAURUSNOVELIST = 306;
        public const string CHUCKLEHEADEDNESS = "GravityWaterBucket";
        public const string THESAURUSHUMORIST = "AI-空间框线";
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return IsRainLabel(label) || label.Contains(BLAMEWORTHINESS) || label.Contains(THESAURUSINCULCATE) || label.Contains(THESAURUSCONSOLIDATE);
        }
        public const int THESAURUSALCOHOL = 269;
        public const double THESAURUSDISABILITY = .0;
        public const double THESAURUSCRITIC = 1500.0;
        public const string THESAURUSHORRIFY = "基点 X";
        public const int TEREBINTHINATED = 2;
        public const string THESAURUSWRONGDOER = @"^([^\-]*\-)([A-Z])(\d+)$";
        public const string THESAURUSCIGARETTE = "连廊";
        public const string THESAURUSPERCEIVE = "Y2L";
        public const int THESAURUSNOTIFY = 745;
        public const string DISPROPORTIONALLY = "LN-";
        public const int THESAURUSSPREAD = 1879;
        public const string PHENYLENEDIAMINE = "F";
        public static bool IsSideWaterBucketLabel(string label)
        {
            return IsWaterBucketLabel(label) && label.Contains(THESAURUSPEPPER);
        }
        public const int THESAURUSPROLONG = 1679;
        public const string CONTEMPTIBILITY = "-";
        public const string THESAURUSBYPASS = @"[^\d\.\-]";
        public const int QUOTATIONDIAMONDBACK = 331;
        public const int COMMENSURATENESS = 550;
        public const int CONSTRUCTIONISM = 1000;
        public const string THESAURUSINSURANCE = "RF";
        public const string THESAURUSTEMERITY = "HasBrokenCondensePipes：";
        public const string PRESTIDIGITATION = "bk对位";
        public const char THESAURUSOVERACT = '-';
        public const string THESAURUSUNRELENTING = " ";
        public const string AUTHORITATIVENESS = "防水套管水平";
        public const string THESAURUSFESTER = "楼上";
        public const string THESAURUSASTUTE = "伸顶通气500";
        public const string THESAURUSPINPOINT = @"\$?地漏平面$";
        public const string THESAURUSELUCIDATION = "NL-";
        public const string THESAURUSRUMPLE = "C7C497AC6";
        public const double HYPERCHOLESTERO = 2500.0;
        public static string GetDN(string label, string dft = THESAURUSSPITEFUL)
        {
            if (label == null) return dft;
            var m = Regex.Match(label, CHAMAECYPARISSUS, RegexOptions.IgnoreCase);
            if (m.Success) return m.Value;
            return dft;
        }
        public const string THESAURUSABRIDGE = "150";
        public const string THESAURUSEXTEMPORE = "TH-STYLE3";
        public const int QUOTATIONEMETIC = 83;
        public const string THESAURUSBAPTIZE = "立管编号";
        public const string UNCOMPANIONABLE = ";";
        public const char THESAURUSRECKON = '$';
        public const int DISCOMFORTABLENESS = 40;
        public const int THESAURUSDEVICE = 950;
        public const string THESAURUSADVANCE = "HasSingleFloorDrainDrainageForDitch:";
        public const int THESAURUSINTRACTABLE = 24;
        public const double THESAURUSINDEMNIFY = 0.0;
        public const string THESAURUSREPULSIVE = "13#雨水口";
        public const int THESAURUSFOLLOWER = 213;
        public const string THESAURUSEMIGRATE = @"^T\d?L";
        public const string CH2OHRCHNH2RCOOH = "X.XX";
        public const string THESAURUSVERIFICATION = "建筑完成面";
        public const string THESAURUSCOURTESAN = "W-RAIN-DIMS";
        public const int THESAURUSFIXTURE = 125;
        public const string THESAURUSPRELUDE = "卫生间";
        public const int THESAURUSBEDEVIL = 0xc7;
        public const string THESAURUSEMBODY = "1-";
        public const int UNCOMPASSIONATE = 3000;
        public const string BALANOPHORACEAE = "HasSingleFloorDrainDrainageForWaterSealingWell:";
        public const string STURZKAMPFFLUGZEUG = "侧入型雨水斗DN100";
        public const int THESAURUSEVINCE = 4;
        public const double THESAURUSFOREMAN = .8;
        public const int THESAURUSSTRETCH = 621;
        public const int QUINQUARTICULARIS = 360;
        public static bool IsDL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, ANTHROPOMORPHISM);
        }
        public const int THESAURUSUNDERWATER = 450;
        public const string THESAURUSSUSTAIN = "-1F";
        public const string THESAURUSEMPLOYMENT = @"^[卫]\d$";
        public const string UNCONSTITUTIONAL = "M";
        public const string THESAURUSOBSERVABLE = "RainPortIds:";
        public const string THESAURUSKNOWLEDGEABLE = "87";
        public const int THESAURUSBEWARE = 779;
        public const string THESAURUSCOMMUNE = "西厨";
        public const char UNACCUSTOMEDNESS = 'A';
        public const string THESAURUSCONSOLIDATE = "排水沟";
        public const string THESAURUSCOMPLEX = "AI-空间名称";
        public const int THESAURUSJAGGED = 229;
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
        public const int PSYCHOPHYSIOLOGICAL = 400;
        public const int ECHOENCEPHALOGRAM = 215;
        public const int QUOTATIONSORCERER = 90;
        public const string THESAURUSSHOWER = "厨";
        public const string ANTHROPOMORPHISM = @"^D\d?L";
        public const string HYDROMETALLURGY = "RF+2";
        public const int THESAURUSIMAGINATIVE = 2000;
        public const int THESAURUSDOWNHEARTED = 180;
        public const string THESAURUSSUNKEN = "CYSD";
        public const string HYPODERMATICALLY = "FloorDrainShareDrainageWithVerticalPipeForWaterSealingWell:";
        public const string THESAURUSMANIFEST = "TCH_TEXT";
        public const string THESAURUSINNOCENT = "接";
        public static bool IsCorridor(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSCIGARETTE };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSESPECIALLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSNEGATIVE;
            return THESAURUSESPECIALLY;
        }
        public const string MEGACHIROPTERAN = "重力型雨水斗DN100";
        public static bool IsY1L(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return label.StartsWith(THESAURUSDELETE);
        }
        public const string THESAURUSBLACKOUT = "RF+1";
        public const string QUOTATIONCREEPING = "地漏套管：";
        public const int QUOTATIONCESTUI = 387;
        public static bool IsY2L(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return label.StartsWith(THESAURUSPERCEIVE);
        }
        public const string THESAURUSSPITEFUL = "DN100";
        public const int QUOTATIONLUNGEING = 3600;
        public static Point2d GetContraPoint(BlockReference br)
        {
            double dx = double.NaN;
            double dy = double.NaN;
            Point2d pt;
            foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
            {
                if (p.PropertyName == THESAURUSHORRIFY)
                {
                    dx = Convert.ToDouble(p.Value);
                }
                else if (p.PropertyName == THESAURUSFURTHEST)
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
                throw new System.Exception(THESAURUSKNICKERS);
            }
            return pt;
        }
        public const string PLEASURABLENESS = "屋面雨水斗";
        public const int THESAURUSPREAMBLE = 130;
        public const int REVOLUTIONIZATION = 350;
        public const double THESAURUSDISRUPTION = 10e5;
        public static bool IsNL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return label.StartsWith(CORRESPONDINGLY);
        }
        public const string THESAURUSDECORATIVE = "W-BUSH-NOTE";
        public const string THESAURUSCOMMUTE = "地漏系统";
        public static bool IsBalcony(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSOUTCRY };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSESPECIALLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSNEGATIVE;
            return THESAURUSESPECIALLY;
        }
        public const string THESAURUSCONGENIAL = "HasSingleFloorDrainDrainage:";
        public const string THESAURUSEGOIST = "ConnectedToGravityWaterBucket";
        public const string DETERMINATIVENESS = @"\-?\d+";
        public static bool IsKitchen(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSNESTLE, THESAURUSCOMMUNE };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSESPECIALLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSNEGATIVE;
            if (roomName.Equals(THESAURUSSHOWER))
                return THESAURUSNEGATIVE;
            return THESAURUSESPECIALLY;
        }
        public const string THESAURUSDELETE = "Y1L";
        public const string SYNDIOTACTICALLY = "DN25";
        public const string THESAURUSMACHINE = "本层通气";
        public const string QUOTATIONIXIONIAN = "空调内机--挂机";
        public const string INTERSEGMENTALLY = "排出套管：";
        public const int THESAURUSDEFICIT = 1400;
        public const string THESAURUSINSPECTOR = "W-RAIN-EQPM";
        public const int DIETHYLSTILBOESTROL = 300;
        public const string THESAURUSPEPPER = "侧入";
        public const string LUCIOCEPHALIDAE = "公卫";
        public const string THESAURUSCAPTIVATE = "DN";
        public const string TRANSUBSTANTIALIS = "楼层类型";
        public static bool IsTL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, THESAURUSEMIGRATE);
        }
        public const string THESAURUSSEVERITY = "主卫";
        public const string THESAURUSCHIRPY = "雨水井编号";
        public static string GetStoreyNumberString(BlockReference br)
        {
            var d = br.ObjectId.GetAttributesInBlockReference(THESAURUSNEGATIVE);
            d.TryGetValue(THESAURUSPERNICIOUS, out string ret);
            return ret;
        }
        public const string THESAURUSDESCEND = "-RAIN-";
        public const string THESAURUSAFFILIATION = @"(\-?\d+)-(\-?\d+)";
        public const string THESAURUSENIGMATIC = "FloorDrainShareDrainageWithVerticalPipe:";
        public const string BLAMEWORTHINESS = "雨水斗";
        public const string THESAURUSMALICE = "$";
        public const string PALAEOGEOMORPHOLOGY = "距离1";
        public const int THESAURUSINTELLECT = 3;
        public const bool THESAURUSNEGATIVE = true;
        public const string THESAURUSCONSENT = "乙字弯";
        public const char THESAURUSSLOPPY = '|';
        public const int THESAURUSSUCCINCT = 600;
        public static bool IsYL(string label)
        {
            if (string.IsNullOrEmpty(label)) return THESAURUSESPECIALLY;
            return label.StartsWith(THESAURUSPUDDLE);
        }
        public const int THESAURUSOBLITERATION = 120;
        public const string THESAURUSPERCHANCE = "空调内机--柜机";
        public const string QUOTATIONKEELED = "水封井";
        public const int CONTRADICTORIES = 1050;
        public const int THESAURUSADMIRABLE = 780;
        public const string ALSOMONOSACCHAROSE = "RoofWaterBuckets:";
        public const int THESAURUSUNAVOIDABLE = 160;
        public const string THESAURUSATTENDANCE = "楼层框定";
        public const double PTILONORHYNCHUS = 10e6;
    }
}