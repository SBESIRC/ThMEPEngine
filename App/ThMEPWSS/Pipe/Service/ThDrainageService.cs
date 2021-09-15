namespace ThMEPWSS.ReleaseNs.DrainageSystemNs
{
    using AcHelper;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.Colors;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Geometry;
    using Autodesk.AutoCAD.Runtime;
    using DotNetARX;
    using Dreambuild.AutoCAD;
    using Linq2Acad;
    using NetTopologySuite.Geometries;
    using NFox.Cad;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using ThCADCore.NTS;
    using ThCADExtension;
    using ThMEPEngineCore.Algorithm;
    using ThMEPEngineCore.Engine;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Diagram.ViewModel;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Service;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using static THDrainageService;
    using static ThMEPWSS.Assistant.DrawUtils;
    using ThMEPEngineCore.Model.Common;
    using NetTopologySuite.Operation.Buffer;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;
    public static class TempExts
    {
        public static Point2d ToPoint2d(this Point pt)
        {
            return new Point2d(pt.X, pt.Y);
        }
    }
    public class TempGeoFac
    {
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
    public class FixingLogic1
    {
        public enum FlFixType
        {
            NoFix,
            MiddleHigher,
            Lower,
            Higher,
        }
        public enum FlCaseEnum
        {
            Unknown,
            Case1,
            Case2,
            Case3,
        }
        public static FlCaseEnum GetFlCase(string label, string storey, bool existsXXRoomAtUpperStorey, int circlesCount)
        {
            FlCaseEnum f()
            {
                if (label == null) return FlCaseEnum.Unknown;
                if (storey == null) return FlCaseEnum.Unknown;
                if (existsXXRoomAtUpperStorey)
                {
                    if (circlesCount > TEREBINTHINATED)
                    {
                        return FlCaseEnum.Case1;
                    }
                    if (circlesCount == TEREBINTHINATED)
                    {
                        return FlCaseEnum.Case2;
                    }
                }
                else
                {
                    if (circlesCount == TEREBINTHINATED)
                    {
                        return FlCaseEnum.Case3;
                    }
                }
                return FlCaseEnum.Unknown;
            }
            var ret = f();
            return ret;
        }
        public static FlFixType GetFlFixType(string label, string storey, bool existsXXRoomAtUpperStorey, int circlesCount)
        {
            FlFixType f()
            {
                if (label == null) return FlFixType.NoFix;
                if (storey == null) return FlFixType.NoFix;
                if (existsXXRoomAtUpperStorey)
                {
                    if (circlesCount > TEREBINTHINATED)
                    {
                        return FlFixType.MiddleHigher;
                    }
                    if (circlesCount == TEREBINTHINATED)
                    {
                        return FlFixType.Lower;
                    }
                }
                else
                {
                    if (circlesCount == TEREBINTHINATED)
                    {
                        return FlFixType.Higher;
                    }
                }
                return FlFixType.NoFix;
            }
            var ret = f();
            return ret;
        }
    }
    public class FixingLogic2
    {
        public enum PlFixType
        {
            NoFix,
        }
        public enum PlCaseEnum
        {
            Unknown,
            Case1,
            Case3,
        }
        public static PlCaseEnum GetPlCase(string label, string storey, bool existsXXRoomAtUpperStorey, int circlesCount)
        {
            PlCaseEnum f()
            {
                if (label == null) return PlCaseEnum.Unknown;
                if (storey == null) return PlCaseEnum.Unknown;
                if (existsXXRoomAtUpperStorey)
                {
                    if (circlesCount > TEREBINTHINATED)
                    {
                        return PlCaseEnum.Case1;
                    }
                    if (circlesCount == TEREBINTHINATED)
                    {
                        return PlCaseEnum.Case1;
                    }
                }
                else
                {
                    if (circlesCount == TEREBINTHINATED)
                    {
                        return PlCaseEnum.Case3;
                    }
                }
                return PlCaseEnum.Unknown;
            }
            var ret = f();
            return ret;
        }
        public static PlFixType GetPlFixType(string label, string storey, bool existsXXRoomAtUpperStorey, int circlesCount)
        {
            return PlFixType.NoFix;
        }
    }
    public class LabelItem
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
        static readonly Regex re = new Regex(QUOTATIONASSERTORY);
        public static LabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new LabelItem()
            {
                Label = label,
                Prefix = m.Groups[PIEZOELECTRICAL].Value,
                D1S = m.Groups[TEREBINTHINATED].Value,
                D2S = m.Groups[THESAURUSINTELLECT].Value,
                Suffix = m.Groups[THESAURUSEVINCE].Value,
            };
        }
    }
#pragma warning disable
    public class DrainageGroupingPipeItem : IEquatable<DrainageGroupingPipeItem>
    {
        public bool MoveTlLineUpper;
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
        public bool CanHaveAring;
        public bool IsFL0;
        public bool MergeFloorDrainForFL0;
        public bool HasRainPortForFL0;
        public bool IsConnectedToFloorDrainForFL0;
        public PipeType PipeType;
        public string OutletWrappingPipeRadius;
        public bool Equals(DrainageGroupingPipeItem other)
        {
            return this.HasWaterPort == other.HasWaterPort
                && this.HasWrappingPipe == other.HasWrappingPipe
                && this.MoveTlLineUpper == other.MoveTlLineUpper
                && this.HasBasinInKitchenAt1F == other.HasBasinInKitchenAt1F
                && this.CanHaveAring == other.CanHaveAring
                && this.PipeType == other.PipeType
                && this.OutletWrappingPipeRadius == other.OutletWrappingPipeRadius
                && this.IsFL0 == other.IsFL0
                && this.MergeFloorDrainForFL0 == other.MergeFloorDrainForFL0
                && this.HasRainPortForFL0 == other.HasRainPortForFL0
                && this.IsConnectedToFloorDrainForFL0 == other.IsConnectedToFloorDrainForFL0
                && this.MaxTl == other.MaxTl
                && this.MinTl == other.MinTl
                && this.IsSingleOutlet == other.IsSingleOutlet
                && this.FloorDrainsCountAt1F == other.FloorDrainsCountAt1F
                && this.Items.SeqEqual(other.Items)
                && this.Hangings.SeqEqual(other.Hangings);
        }
        public class Hanging : IEquatable<Hanging>
        {
            public string Storey;
            public int FloorDrainsCount;
            public int WashingMachineFloorDrainsCount;
            public bool IsSeries;
            public bool HasSCurve;
            public bool HasDoubleSCurve;
            public bool HasCleaningPort;
            public bool HasCheckPoint;
            public bool HasDownBoardLine;
            public bool Is4Tune;
            public string RoomName;
            public FixingLogic1.FlFixType FlFixType;
            public FixingLogic1.FlCaseEnum FlCaseEnum;
            public FixingLogic2.PlFixType PlFixType;
            public FixingLogic2.PlCaseEnum PlCaseEnum;
            public override int GetHashCode()
            {
                return BATHYDRACONIDAE;
            }
            public bool Equals(Hanging other)
            {
                return this.FloorDrainsCount == other.FloorDrainsCount
                    && this.WashingMachineFloorDrainsCount == other.WashingMachineFloorDrainsCount
                    && this.IsSeries == other.IsSeries
                    && this.HasSCurve == other.HasSCurve
                    && this.HasDoubleSCurve == other.HasDoubleSCurve
                    && this.HasCleaningPort == other.HasCleaningPort
                    && this.HasCheckPoint == other.HasCheckPoint
                    && this.HasDownBoardLine == other.HasDownBoardLine
                    && this.Storey == other.Storey
                    && this.Is4Tune == other.Is4Tune
                    && this.FlFixType == other.FlFixType
                    && this.FlCaseEnum == other.FlCaseEnum
                    && this.PlFixType == other.PlFixType
                    && this.PlCaseEnum == other.PlCaseEnum
                    ;
            }
        }
        public struct ValueItem
        {
            public bool Exist;
            public bool HasLong;
            public bool DrawLongHLineHigher;
            public bool HasShort;
        }
        public override int GetHashCode()
        {
            return BATHYDRACONIDAE;
        }
    }
    public enum PipeType
    {
        FL, PL,
    }
    public class DrainageGroupedPipeItem
    {
        public List<string> Labels;
        public List<string> WaterPortLabels;
        public bool HasWrappingPipe;
        public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > BATHYDRACONIDAE;
        public List<DrainageGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public int MinTl;
        public int MaxTl;
        public bool HasTl => TlLabels != null && TlLabels.Count > BATHYDRACONIDAE;
        public PipeType PipeType;
        public List<DrainageGroupingPipeItem.Hanging> Hangings;
        public bool IsSingleOutlet;
        public bool HasBasinInKitchenAt1F;
        public int FloorDrainsCountAt1F;
        public bool CanHaveAring;
        public bool IsFL0;
        public bool MergeFloorDrainForFL0;
        public bool MoveTlLineUpper;
        public bool HasRainPortForFL0;
        public bool IsConnectedToFloorDrainForFL0;
        public string OutletWrappingPipeRadius;
    }
    public class ThwPipeRun
    {
        public string Storey;
        public bool ShowStoreyLabel;
        public bool HasDownBoardLine;
        public bool DrawLongHLineHigher;
        public bool Is4Tune;
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
        public int LinesCount = PIEZOELECTRICAL;
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
        public int HangingCount = BATHYDRACONIDAE;
        public Hanging Hanging1;
        public Hanging Hanging2;
    }
    public class ThwPipeLine
    {
        public List<string> Labels;
        public bool? IsLeftOrMiddleOrRight;
        public List<ThwPipeRun> PipeRuns;
        public ThwOutput Output;
    }
    public class StoreyItem
    {
        public List<int> Ints;
        public List<string> Labels;
        public void Init()
        {
            Ints ??= new List<int>();
            Labels ??= new List<string>();
        }
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
                var s = BATHYDRACONIDAE;
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
    class FloorDrainCbItem
    {
        public Point2d BasePt;
        public string Name;
        public bool LeftOrRight;
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
    public class StoreyContext
    {
        public List<StoreyInfo> StoreyInfos;
    }
    public class CommandContext
    {
        public Point3dCollection range;
        public StoreyContext StoreyContext;
        public DrainageSystemDiagramViewModel ViewModel;
        public System.Windows.Window window;
    }
    public class DrainageSystemDiagram
    {
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
        public static void DrawWashingMachineRaisingSymbol(Point2d bsPt, bool isLeftOrRight)
        {
            if (isLeftOrRight)
            {
                var v = new Vector2d(THESAURUSSATISFY, -QUOTATIONRASCHIG);
                DrawBlockReference(THESAURUSPURIFY, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSRECRIMINATION;
                    br.ScaleFactors = new Scale3d(TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, THESAURUSCONDITION);
                    }
                });
            }
            else
            {
                var v = new Vector2d(-THESAURUSSATISFY, -QUOTATIONRASCHIG);
                DrawBlockReference(THESAURUSPURIFY, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSRECRIMINATION;
                    br.ScaleFactors = new Scale3d(-TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, THESAURUSCONDITION);
                    }
                });
            }
        }
        public static double LONG_TRANSLATOR_HEIGHT1 = THESAURUSADMIRABLE;
        public static double CHECKPOINT_OFFSET_Y = SUPERCILIOUSNESS;
        public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreyItem> storeysItems, Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys, DrainageSystemDiagramViewModel viewModel)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + PHENYLENEDIAMINE).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - PIEZOELECTRICAL;
            var end = BATHYDRACONIDAE;
            var OFFSET_X = HYPERCHOLESTERO;
            var SPAN_X = QUOTATIONCOLLARED + THESAURUSBEFRIEND + PSYCHOGERIATRIC;
            var HEIGHT = THESAURUSRECAPITULATE;
            {
                if (viewModel?.Params?.StoreySpan is double v)
                {
                    HEIGHT = v;
                }
            }
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSRECAPITULATE;
            var __dy = DIETHYLSTILBOESTROL;
            DrawDrainageSystemDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, viewModel);
        }
        public class Opt
        {
            double fixY;
            double _dy;
            public List<Vector2d> vecs0 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSCONFECTIONERY - dy) };
            public List<Vector2d> vecs1 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy + fixY), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - _dy - dy - fixY) };
            public List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy - __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - _dy - dy + __dy) };
            public List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy + __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - _dy - dy - __dy) };
            public List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSPROLONG - dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            public List<Vector2d> vecs3 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -ANTHROPOMORPHITES - _dy - dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            public List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy - __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -ANTHROPOMORPHITES - _dy - dy + __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            public List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSCONFECTIONERY + dy), new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy + __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -ANTHROPOMORPHITES - _dy - dy - __dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) };
            public List<Vector2d> vecs4 => vecs1.GetYAxisMirror();
            public List<Vector2d> vecs5 => vecs2.GetYAxisMirror();
            public List<Vector2d> vecs6 => vecs3.GetYAxisMirror();
            public List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
            public List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
            public List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
            public Vector2d vec7 => new Vector2d(-QUOTATIONSORCERER, QUOTATIONSORCERER);
            public Point2d basePoint;
            public List<DrainageGroupedPipeItem> pipeGroupItems;
            public List<string> allNumStoreyLabels;
            public List<string> allStoreys;
            public int start;
            public int end;
            public double OFFSET_X;
            public double SPAN_X;
            public double HEIGHT;
            public int COUNT;
            public double dy;
            public int __dy;
            public DrainageSystemDiagramViewModel viewModel;
            public static bool SHOWLINE;
            public void Draw()
            {
                {
                    var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                    for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
                    {
                        var storey = allStoreys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
                    }
                }
                void _DrawWrappingPipe(Point2d basePt, string shadow)
                {
                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                    {
                        Dr.DrawSimpleLabel(basePt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                    }
                    DrawBlockReference(THESAURUSREPRESSION, basePt.ToPoint3d(), br =>
                    {
                        br.Layer = THESAURUSEXCOMMUNICATE;
                        ByLayer(br);
                    });
                }
                void DrawOutlets5(Point2d basePoint, ThwOutput output, DrainageGroupedPipeItem gpItem)
                {
                    var values = output.DirtyWaterWellValues;
                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -HETEROTRANSPLANTED), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSORIGINATE - PSYCHOPHYSIOLOGICAL, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, THESAURUSSUCCINCT), new Vector2d(PSYCHOGENICALLY, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, THESAURUSSUPPLEMENTARY) };
                    var segs = vecs.ToGLineSegments(basePoint);
                    segs.RemoveAt(THESAURUSINTELLECT);
                    DrawDiryWaterWells1(segs[TEREBINTHINATED].EndPoint + new Vector2d(-PSYCHOPHYSIOLOGICAL, DIETHYLSTILBOESTROL), values);
                    if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[THESAURUSINTELLECT].StartPoint.OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                    if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[TEREBINTHINATED].EndPoint.OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                    DrawNoteText(output.DN1, segs[THESAURUSINTELLECT].StartPoint.OffsetX(THESAURUSAPPORTION));
                    DrawNoteText(output.DN2, segs[TEREBINTHINATED].EndPoint.OffsetX(THESAURUSAPPORTION));
                    if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSEVINCE].StartPoint.ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                    if (output.HasCleaningPort2) DrawCleaningPort(segs[TEREBINTHINATED].StartPoint.ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                    var p = segs[DOLICHOCEPHALOUS].EndPoint;
                    DrawFloorDrain((p.OffsetX(-THESAURUSSETBACK) + new Vector2d(-TRISYLLABICALLY + PSYCHOPHYSIOLOGICAL, BATHYDRACONIDAE)).ToPoint3d(), THESAURUSNEGATIVE);
                }
                string getDSCurveValue()
                {
                    return viewModel?.Params?.厨房洗涤盆 ?? THESAURUSFRIVOLITY;
                }
                for (int j = BATHYDRACONIDAE; j < COUNT; j++)
                {
                    var dome_lines = new List<GLineSegment>(THESAURUSREGARDING);
                    var vent_lines = new List<GLineSegment>(THESAURUSREGARDING);
                    var dome_layer = THESAURUSOBJECTIVELY;
                    var vent_layer = THESAURUSDECISIVE;
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid) dome_lines.Add(seg);
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs, string shadow)
                    {
                        var ok = THESAURUSESPECIALLY;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                {
                                    Dr.DrawSimpleLabel(seg.StartPoint, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL), dome_layer);
                                }
                                ok = THESAURUSNEGATIVE;
                            }
                            dome_lines.Add(seg);
                        }
                    }
                    void drawVentPipe(GLineSegment seg)
                    {
                        if (seg.IsValid) vent_lines.Add(seg);
                    }
                    void drawVentPipes(IEnumerable<GLineSegment> segs, string shadow)
                    {
                        var ok = THESAURUSESPECIALLY;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                {
                                    Dr.DrawSimpleLabel(seg.StartPoint, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL), vent_layer);
                                }
                                ok = THESAURUSNEGATIVE;
                            }
                            vent_lines.Add(seg);
                        }
                    }
                    string getWashingMachineFloorDrainDN()
                    {
                        return viewModel?.Params?.WashingMachineFloorDrainDN ?? THESAURUSTREASONABLE;
                    }
                    string getOtherFloorDrainDN()
                    {
                        return viewModel?.Params?.OtherFloorDrainDN ?? THESAURUSTREASONABLE;
                    }
                    void Get2FloorDrainDN(out string v1, out string v2)
                    {
                        v1 = viewModel?.Params?.WashingMachineFloorDrainDN ?? THESAURUSTREASONABLE;
                        v2 = v1;
                        if (v2 == THESAURUSTREASONABLE) v2 = THESAURUSDEFENSIVE;
                    }
                    bool getCouldHavePeopleOnRoof()
                    {
                        return viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSNEGATIVE;
                    }
                    var gpItem = pipeGroupItems[j];
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.Concat(gpItem.TlLabels.Yield()).ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                    {
                        var storey = allNumStoreyLabels[i];
                        var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                        {
                            HasLongTranslator = gpItem.Items[i].HasLong,
                            HasShortTranslator = gpItem.Items[i].HasShort,
                            HasCleaningPort = gpItem.Hangings.TryGet(i + PIEZOELECTRICAL)?.HasCleaningPort ?? THESAURUSESPECIALLY,
                            HasCheckPoint = gpItem.Hangings[i].HasCheckPoint,
                            HasDownBoardLine = gpItem.Hangings[i].HasDownBoardLine,
                            DrawLongHLineHigher = gpItem.Items[i].DrawLongHLineHigher,
                            Is4Tune = gpItem.Hangings[i].Is4Tune,
                        } : null;
                        runs.Add(run);
                    }
                    for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                    {
                        var floorDrainsCount = gpItem.Hangings[i].FloorDrainsCount;
                        var hasSCurve = gpItem.Hangings[i].HasSCurve;
                        var hasDoubleSCurve = gpItem.Hangings[i].HasDoubleSCurve;
                        if (hasDoubleSCurve)
                        {
                            var run = runs.TryGet(i);
                            if (run != null)
                            {
                                var hanging = run.LeftHanging ??= new Hanging();
                                hanging.HasDoubleSCurve = hasDoubleSCurve;
                            }
                        }
                        if (floorDrainsCount > BATHYDRACONIDAE || hasSCurve)
                        {
                            var run = runs.TryGet(i - PIEZOELECTRICAL);
                            if (run != null)
                            {
                                var hanging = run.LeftHanging ??= new Hanging();
                                hanging.FloorDrainsCount = floorDrainsCount;
                                hanging.HasSCurve = hasSCurve;
                            }
                        }
                    }
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
                        foreach (var r in runs)
                        {
                            if (r?.HasShortTranslator == THESAURUSNEGATIVE)
                            {
                                r.IsShortTranslatorToLeftOrRight = THESAURUSESPECIALLY;
                                if (r.HasLongTranslator && r.IsLongTranslatorToLeftOrRight)
                                {
                                    r.IsShortTranslatorToLeftOrRight = THESAURUSNEGATIVE;
                                }
                            }
                        }
                    }
                    Point2d drawHanging(Point2d start, Hanging hanging)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSCHANNEL, BATHYDRACONIDAE), new Vector2d(ALSOHEAVENWARDS, BATHYDRACONIDAE), new Vector2d(THESAURUSDOWNHEARTED, BATHYDRACONIDAE), new Vector2d(THESAURUSFABULOUS, BATHYDRACONIDAE) };
                        var segs = vecs.ToGLineSegments(start);
                        {
                            var _segs = segs.ToList();
                            if (hanging.FloorDrainsCount == PIEZOELECTRICAL)
                            {
                                _segs.RemoveAt(THESAURUSINTELLECT);
                            }
                            _segs.RemoveAt(TEREBINTHINATED);
                            DrawDomePipes(_segs);
                        }
                        {
                            var pts = vecs.ToPoint2ds(start);
                            {
                                var pt = pts[PIEZOELECTRICAL];
                                var v = new Vector2d(QUOTATIONSORCERER, QUOTATIONSORCERER);
                                if (getDSCurveValue() == INSTITUTIONALIZED)
                                {
                                    v = default;
                                }
                                var p = pt + v;
                                if (hanging.HasSCurve)
                                {
                                    DrawSCurve(v, pt, THESAURUSESPECIALLY);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    if (!p.Equals(pt))
                                    {
                                        dome_lines.Add(new GLineSegment(p, pt));
                                    }
                                    DrawDSCurve(p, THESAURUSESPECIALLY, getDSCurveValue(), THESAURUSAMENITY);
                                }
                            }
                            if (hanging.FloorDrainsCount >= PIEZOELECTRICAL)
                            {
                                DrawFloorDrain(pts[TEREBINTHINATED].ToPoint3d(), THESAURUSESPECIALLY);
                            }
                            if (hanging.FloorDrainsCount >= TEREBINTHINATED)
                            {
                                DrawFloorDrain(pts[THESAURUSEVINCE].ToPoint3d(), THESAURUSESPECIALLY);
                            }
                        }
                        start = segs.Last().EndPoint;
                        return start;
                    }
                    void DrawOutlets1(string shadow, Point2d basePoint1, double width, ThwOutput output, double dy = -THESAURUSLABYRINTHINE, bool isRainWaterWell = THESAURUSESPECIALLY, Vector2d? fixv = null)
                    {
                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                        {
                            Dr.DrawSimpleLabel(basePoint1.OffsetY(-QUOTATIONPATRONAL), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                        }
                        Point2d pt2, pt3;
                        if (output.DirtyWaterWellValues != null)
                        {
                            var v = new Vector2d(-THESAURUSIMAGINATIVE - PSYCHOPHYSIOLOGICAL, -THESAURUSSUCCINCT);
                            var pt = basePoint1 + v;
                            if (fixv.HasValue)
                            {
                                pt += fixv.Value;
                            }
                            var values = output.DirtyWaterWellValues;
                            DrawDiryWaterWells1(pt, values, isRainWaterWell);
                        }
                        {
                            var dx = width - QUOTATIONLUNGEING;
                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -THESAURUSSUCCINCT), new Vector2d(INCREDULOUSNESS + dx, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, THESAURUSINTEMPERATE), new Vector2d(-THESAURUSSEDATE - dx, -THESAURUSCONFECTIONERY), new Vector2d(THESAURUSOMNIVOROUS + dx, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK) };
                            {
                                var segs = vecs.ToGLineSegments(basePoint1);
                                if (output.LinesCount == PIEZOELECTRICAL)
                                {
                                    drawDomePipes(segs.Take(THESAURUSINTELLECT), THESAURUSAMENITY);
                                }
                                else if (output.LinesCount > PIEZOELECTRICAL)
                                {
                                    segs.RemoveAt(PERCHLOROETHYLENE);
                                    if (!output.HasVerticalLine2) segs.RemoveAt(PARALLELOGRAMMIC);
                                    segs.RemoveAt(THESAURUSINTELLECT);
                                    drawDomePipes(segs, THESAURUSAMENITY);
                                }
                            }
                            var pts = vecs.ToPoint2ds(basePoint1);
                            if (output.HasWrappingPipe1) _DrawWrappingPipe(pts[THESAURUSINTELLECT].OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                            if (output.HasWrappingPipe2) _DrawWrappingPipe(pts[THESAURUSEVINCE].OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                            if (output.HasWrappingPipe3) _DrawWrappingPipe(pts[THESAURUSMYTHICAL].OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                            if (output.HasWrappingPipe1 && !output.HasWrappingPipe2 && !output.HasWrappingPipe3)
                            {
                                if (gpItem.OutletWrappingPipeRadius != null)
                                {
                                    static void DrawLine(string layer, params GLineSegment[] segs)
                                    {
                                        var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
                                        foreach (var line in lines)
                                        {
                                            line.Layer = layer;
                                            ByLayer(line);
                                        }
                                    }
                                    static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = MICROSPECTROPHOTOMETRY)
                                    {
                                        DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, label } }, cb: br => { ByLayer(br); });
                                    }
                                    var p1 = pts[THESAURUSINTELLECT].OffsetX(PSYCHOPHYSIOLOGICAL);
                                    var p2 = p1.OffsetY(-QUOTATIONCHOROID);
                                    var p3 = p2.OffsetX(THESAURUSBEFRIEND);
                                    var layer = THESAURUSSMASHING;
                                    DrawLine(layer, new GLineSegment(p1, p2));
                                    DrawLine(layer, new GLineSegment(p3, p2));
                                    DrawStoreyHeightSymbol(p3, THESAURUSSMASHING, gpItem.OutletWrappingPipeRadius);
                                    {
                                        var _shadow = THESAURUSAMENITY;
                                        if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > PIEZOELECTRICAL)
                                        {
                                            Dr.DrawSimpleLabel(p3, THESAURUSBENEFIT + _shadow.Substring(PIEZOELECTRICAL));
                                        }
                                    }
                                }
                            }
                            var v = new Vector2d(THESAURUSAPPORTION, THESAURUSFORTIFICATION);
                            DrawNoteText(output.DN1, pts[THESAURUSINTELLECT] + v);
                            DrawNoteText(output.DN2, pts[THESAURUSEVINCE] + v);
                            DrawNoteText(output.DN3, pts[THESAURUSMYTHICAL] + v);
                            if (output.HasCleaningPort1) DrawCleaningPort(pts[TEREBINTHINATED].ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                            if (output.HasCleaningPort2) DrawCleaningPort(pts[DOLICHOCEPHALOUS].ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                            if (output.HasCleaningPort3) DrawCleaningPort(pts[THESAURUSABUNDANCE].ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                            pt2 = pts[PARALLELOGRAMMIC];
                            pt3 = pts.Last();
                        }
                        if (output.HasLargeCleaningPort)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSVACILLATE) };
                            var segs = vecs.ToGLineSegments(pt3);
                            drawDomePipes(segs, THESAURUSAMENITY);
                            DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), THESAURUSESPECIALLY, TEREBINTHINATED);
                        }
                        if (output.HangingCount == PIEZOELECTRICAL)
                        {
                            var hang = output.Hanging1;
                            Point2d lastPt = pt2;
                            {
                                var segs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, THESAURUSWITHIN), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK) }.ToGLineSegments(lastPt);
                                drawDomePipes(segs, THESAURUSAMENITY);
                                lastPt = segs.Last().EndPoint;
                            }
                            {
                                lastPt = drawHanging(lastPt, output.Hanging1);
                            }
                        }
                        else if (output.HangingCount == TEREBINTHINATED)
                        {
                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSUTILITY, THESAURUSUTILITY), new Vector2d(THESAURUSCHASTITY, THESAURUSCHASTITY) };
                            var pts = vs1.ToPoint2ds(pt3);
                            drawDomePipes(vs1.ToGLineSegments(pt3), THESAURUSAMENITY);
                            drawHanging(pts.Last(), output.Hanging1);
                            var dx = output.Hanging1.FloorDrainsCount == TEREBINTHINATED ? CONSTRUCTIONISM : BATHYDRACONIDAE;
                            var vs2 = new List<Vector2d> { new Vector2d(THESAURUSJUDGEMENT + dx, BATHYDRACONIDAE), new Vector2d(THESAURUSCHASTITY, THESAURUSCHASTITY) };
                            drawDomePipes(vs2.ToGLineSegments(pts[PIEZOELECTRICAL]), THESAURUSAMENITY);
                            drawHanging(vs2.ToPoint2ds(pts[PIEZOELECTRICAL]).Last(), output.Hanging2);
                        }
                    }
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                    {
                        {
                        }
                        {
                            foreach (var info in arr)
                            {
                                if (info?.Storey == THESAURUSINSURANCE)
                                {
                                    if (gpItem.CanHaveAring)
                                    {
                                        var pt = info.BasePoint;
                                        var seg = new GLineSegment(pt, pt.OffsetY(ThWSDStorey.RF_OFFSET_Y));
                                        drawDomePipe(seg);
                                        DrawAiringSymbol(seg.EndPoint, getCouldHavePeopleOnRoof());
                                    }
                                }
                            }
                        }
                        int counterPipeButtomHeightSymbol = BATHYDRACONIDAE;
                        bool hasDrawedSCurveLabel = THESAURUSESPECIALLY;
                        bool hasDrawedDSCurveLabel = THESAURUSESPECIALLY;
                        bool hasDrawedCleaningPort = THESAURUSESPECIALLY;
                        void _DrawLabel(string text1, string text2, Point2d basePt, bool leftOrRight, double height)
                        {
                            var w = QUOTATIONLUNGEING - PRAETERNATURALIS;
                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, height), new Vector2d(leftOrRight ? -w : w, BATHYDRACONIDAE) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForDraiNote(lines.ToArray());
                            var p = segs.Last().EndPoint.OffsetY(THESAURUSFORTIFICATION);
                            if (!string.IsNullOrEmpty(text1))
                            {
                                var t = DrawTextLazy(text1, REVOLUTIONIZATION, p);
                                Dr.SetLabelStylesForDraiNote(t);
                            }
                            if (!string.IsNullOrEmpty(text2))
                            {
                                var t = DrawTextLazy(text2, REVOLUTIONIZATION, p.OffsetXY(QUOTATIONSCORIA, -PSYCHOPHYSIOLOGICAL));
                                Dr.SetLabelStylesForDraiNote(t);
                            }
                        }
                        void _DrawHorizontalLineOnPipeRun(Point3d basePt)
                        {
                            if (gpItem.Labels.Any(x => IsFL(x)))
                            {
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == TEREBINTHINATED)
                                {
                                    var p = basePt.ToPoint2d();
                                    var h = HEIGHT * THESAURUSADVANCEMENT;
                                    if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
                                    {
                                        h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSTRAGEDY;
                                    }
                                    p = p.OffsetY(h);
                                    DrawPipeButtomHeightSymbol(THESAURUSWORKSHOP, HEIGHT * QUOTATIONBITUMINOUS, p);
                                }
                            }
                            DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                        }
                        void _DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                        {
                            if (!hasDrawedSCurveLabel)
                            {
                                hasDrawedSCurveLabel = THESAURUSNEGATIVE;
                                _DrawLabel(THESAURUSMISJUDGE, CALYMMATOBACTERIUM, p1 + new Vector2d(-THESAURUSUNDERWATER, PHILANTHROPICALLY), THESAURUSNEGATIVE, THESAURUSCONFECTIONERY);
                            }
                            DrawSCurve(vec7, p1, leftOrRight);
                        }
                        void _DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
                        {
                            if (gpItem.Labels.Any(x => IsPL(x)))
                            {
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == TEREBINTHINATED)
                                {
                                    var p = basePt.ToPoint2d();
                                    DrawPipeButtomHeightSymbol(THESAURUSWORKSHOP, HEIGHT * QUOTATIONBITUMINOUS, p);
                                }
                            }
                            var p1 = basePt.ToPoint2d();
                            if (!hasDrawedCleaningPort && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                            {
                                hasDrawedCleaningPort = THESAURUSNEGATIVE;
                                _DrawLabel(STERNOPTYCHIDAE, MISAPPREHENSIVENESS, p1 + new Vector2d(-THESAURUSCORDIAL, THESAURUSCONCEIVE), THESAURUSNEGATIVE, THESAURUSSCENERY);
                            }
                            DrawCleaningPort(basePt, leftOrRight, scale);
                        }
                        void _DrawCheckPoint(Point2d basePt, bool leftOrRight, string shadow)
                        {
                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                            {
                                Dr.DrawSimpleLabel(basePt, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                            }
                            DrawCheckPoint(basePt.ToPoint3d(), leftOrRight);
                        }
                        var fdBasePoints = new Dictionary<int, List<Point2d>>();
                        var floorDrainCbs = new Dictionary<Geometry, FloorDrainCbItem>();
                        var washingMachineFloorDrainShooters = new List<Geometry>();
                        for (int i = start; i >= end; i--)
                        {
                            var fdBsPts = new List<Point2d>();
                            fdBasePoints[i] = fdBsPts;
                            var storey = allNumStoreyLabels.TryGet(i);
                            if (storey == null) continue;
                            var run = thwPipeLine.PipeRuns.TryGet(i);
                            if (run == null) continue;
                            var info = arr[i];
                            if (info == null) continue;
                            var output = thwPipeLine.Output;
                            {
                                if (storey == THESAURUSDEFILE)
                                {
                                    var basePt = info.EndPoint;
                                    if (output != null)
                                    {
                                        DrawOutlets1(THESAURUSAMENITY, basePt, QUOTATIONLUNGEING, output);
                                    }
                                }
                            }
                            bool shouldRaiseWashingMachine()
                            {
                                return viewModel?.Params?.ShouldRaiseWashingMachine ?? THESAURUSESPECIALLY;
                            }
                            bool _shouldDrawRaiseWashingMachineSymbol()
                            {
                                return THESAURUSESPECIALLY;
                            }
                            bool shouldDrawRaiseWashingMachineSymbol(Hanging hanging)
                            {
                                return THESAURUSESPECIALLY;
                            }
                            void handleHanging(Hanging hanging, bool isLeftOrRight)
                            {
                                var linesDfferencers = new List<Polygon>();
                                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, int i, int j, string shadow)
                                {
                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                    {
                                        Dr.DrawSimpleLabel(basePt.ToPoint2D(), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                    }
                                    var p1 = basePt.ToPoint2d();
                                    {
                                        if (_shouldDrawRaiseWashingMachineSymbol())
                                        {
                                            var fixVec = new Vector2d(-THESAURUSBEFRIEND, BATHYDRACONIDAE);
                                            var p = p1 + new Vector2d(BATHYDRACONIDAE, DNIPRODZERZHINSK) + new Vector2d(-THESAURUSDOWNHEARTED, THESAURUSPROFFER) + fixVec;
                                            fdBsPts.Add(p);
                                            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSRELEASE, BATHYDRACONIDAE), fixVec, new Vector2d(-QUOTATIONSORCERER, QUOTATIONSORCERER), new Vector2d(BATHYDRACONIDAE, CHEMOTROPICALLY), new Vector2d(-THESAURUSUNABLE, BATHYDRACONIDAE) };
                                            var segs = vecs.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(ALSOHEAVENWARDS, BATHYDRACONIDAE));
                                            drawDomePipes(segs, THESAURUSAMENITY);
                                            DrainageSystemDiagram.DrawWashingMachineRaisingSymbol(segs.Last().EndPoint, THESAURUSNEGATIVE);
                                            return;
                                        }
                                    }
                                    {
                                        var p = p1 + new Vector2d(-THESAURUSDOWNHEARTED + (leftOrRight ? BATHYDRACONIDAE : QUINQUARTICULARIS), THESAURUSENTHUSE);
                                        fdBsPts.Add(p);
                                        floorDrainCbs[new GRect(basePt, basePt.OffsetXY(leftOrRight ? -PSYCHOPHYSIOLOGICAL : PSYCHOPHYSIOLOGICAL, THESAURUSBEFRIEND)).ToPolygon()] = new FloorDrainCbItem()
                                        {
                                            BasePt = basePt.ToPoint2D(),
                                            Name = THESAURUSMELODIOUS,
                                            LeftOrRight = leftOrRight,
                                        };
                                        return;
                                    }
                                }
                                void _DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight, int i, int j, string shadow)
                                {
                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                    {
                                        Dr.DrawSimpleLabel(p1, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                    }
                                    if (!hasDrawedDSCurveLabel && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                                    {
                                        hasDrawedDSCurveLabel = THESAURUSNEGATIVE;
                                        var p2 = p1 + new Vector2d(-THESAURUSDEVICE, THESAURUSVERTIGO - DIETHYLSTILBOESTROL);
                                        if (getDSCurveValue() == INSTITUTIONALIZED)
                                        {
                                            p2 += new Vector2d(INTERMINABLENESS, -THESAURUSENTHUSE);
                                        }
                                        _DrawLabel(THESAURUSOFFENDER, CALYMMATOBACTERIUM, p2, THESAURUSNEGATIVE, THESAURUSCONFECTIONERY);
                                    }
                                    {
                                        var v = vec7;
                                        if (getDSCurveValue() == INSTITUTIONALIZED)
                                        {
                                            v = default;
                                            p1 = p1.OffsetY(QUOTATIONPATRONAL);
                                        }
                                        var p2 = p1 + v;
                                        if (!p1.Equals(p2))
                                        {
                                            dome_lines.Add(new GLineSegment(p1, p2));
                                        }
                                        DrawDSCurve(p2, leftOrRight, getDSCurveValue(), THESAURUSAMENITY);
                                    }
                                }
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == PIEZOELECTRICAL && thwPipeLine.Labels.Any(x => IsFL(x)))
                                {
                                    if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                    {
                                        DrawPipeButtomHeightSymbol(THESAURUSWORKSHOP, HEIGHT * QUOTATIONBITUMINOUS, info.StartPoint.OffsetY(-THESAURUSENTHUSE - ELECTRODYNAMICAL));
                                    }
                                    else
                                    {
                                        var c = gpItem.Hangings[i]?.FloorDrainsCount ?? BATHYDRACONIDAE;
                                        if (c > BATHYDRACONIDAE)
                                        {
                                            if (c == TEREBINTHINATED && !gpItem.Hangings[i].IsSeries)
                                            {
                                                DrawPipeButtomHeightSymbol(DIETHYLSTILBOESTROL, HEIGHT * QUOTATIONBITUMINOUS, info.StartPoint.OffsetXY(THESAURUSFABULOUS, -THESAURUSENTHUSE));
                                                var vecs = new List<Vector2d> { new Vector2d(-THESAURUSBEFRIEND, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -THESAURUSSUCCINCT), new Vector2d(-THESAURUSIMAGINATIVE, BATHYDRACONIDAE) };
                                                var segs = vecs.ToGLineSegments(new List<Vector2d> { new Vector2d(-THESAURUSFABULOUS, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -THESAURUSENTHUSE) }.GetLastPoint(info.StartPoint));
                                                DrawPipeButtomHeightSymbol(segs.Last().EndPoint, segs);
                                            }
                                            else
                                            {
                                                DrawPipeButtomHeightSymbol(THESAURUSWORKSHOP, HEIGHT * QUOTATIONBITUMINOUS, info.StartPoint.OffsetY(-THESAURUSENTHUSE));
                                            }
                                        }
                                        else
                                        {
                                            DrawPipeButtomHeightSymbol(THESAURUSWORKSHOP, BATHYDRACONIDAE, info.EndPoint.OffsetY(INTERMINABLENESS));
                                        }
                                    }
                                }
                                var w = ALSOHEAVENWARDS;
                                if (hanging.FloorDrainsCount == TEREBINTHINATED && !hanging.HasDoubleSCurve)
                                {
                                    w = BATHYDRACONIDAE;
                                }
                                if (hanging.FloorDrainsCount == TEREBINTHINATED && !hanging.HasDoubleSCurve && !hanging.IsSeries)
                                {
                                    var startPt = info.StartPoint.OffsetY(-THESAURUSCONSERVATIVE - PIEZOELECTRICAL);
                                    var delta = run.Is4Tune ? BATHYDRACONIDAE : QUOTATIONPATRONAL + THESAURUSFORTIFICATION;
                                    var _vecs1 = new List<Vector2d> { new Vector2d(-THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(-THESAURUSCHANNEL, BATHYDRACONIDAE), };
                                    var _vecs2 = new List<Vector2d> { new Vector2d(THESAURUSSETBACK + delta, THESAURUSSETBACK + delta), new Vector2d(THESAURUSCHANNEL - delta, BATHYDRACONIDAE), };
                                    var segs1 = _vecs1.ToGLineSegments(startPt);
                                    var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                                    DrawDomePipes(segs1);
                                    DrawDomePipes(segs2);
                                    _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSNEGATIVE, i, j, THESAURUSAMENITY);
                                    _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), THESAURUSESPECIALLY, i, j, THESAURUSAMENITY);
                                    if (run.Is4Tune)
                                    {
                                        var st = info.StartPoint;
                                        var p1 = new List<Vector2d> { new Vector2d(-THESAURUSBEFRIEND, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -THESAURUSSHIFTLESS) }.GetLastPoint(st);
                                        var p2 = new List<Vector2d> { new Vector2d(DIETHYLSTILBOESTROL, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -THESAURUSSHIFTLESS) }.GetLastPoint(st);
                                        _DrawWrappingPipe(p1, THESAURUSAMENITY);
                                        _DrawWrappingPipe(p2, THESAURUSAMENITY);
                                    }
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (hanging.FloorDrainsCount == BATHYDRACONIDAE && hanging.HasDoubleSCurve)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHIRTSLEEVE, -QUOTATIONSHIRTSLEEVE), new Vector2d(THESAURUSCHANNEL, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, -THESAURUSSETBACK) };
                                        var dx = vecs.GetLastPoint(Point2d.Origin).X;
                                        var startPt = info.EndPoint.OffsetXY(-dx, HEIGHT / DOLICHOCEPHALOUS);
                                        var segs = vecs.ToGLineSegments(startPt);
                                        var p1 = segs.Last(THESAURUSINTELLECT).StartPoint;
                                        drawDomePipes(segs, THESAURUSAMENITY);
                                        _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSAMENITY);
                                    }
                                    else
                                    {
                                        var beShort = hanging.FloorDrainsCount == PIEZOELECTRICAL && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                        var vecs = new List<Vector2d> { new Vector2d(-UNCONJECTURABLE, UNCONJECTURABLE), new Vector2d(BATHYDRACONIDAE, THESAURUSLABYRINTHINE), new Vector2d(-THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(beShort ? BATHYDRACONIDAE : -THESAURUSCHANNEL, BATHYDRACONIDAE), new Vector2d(-w, BATHYDRACONIDAE), new Vector2d(-THESAURUSDOWNHEARTED, BATHYDRACONIDAE), new Vector2d(-THESAURUSFABULOUS, BATHYDRACONIDAE) };
                                        if (isLeftOrRight == THESAURUSESPECIALLY)
                                        {
                                            vecs = vecs.GetYAxisMirror();
                                        }
                                        var pt = info.Segs[THESAURUSEVINCE].StartPoint.OffsetY(-CORTICOSTEROIDS).OffsetY(THESAURUSDESTITUTION - QUOTATIONSORCERER);
                                        if (isLeftOrRight == THESAURUSESPECIALLY && run.IsLongTranslatorToLeftOrRight == THESAURUSNEGATIVE)
                                        {
                                            var p1 = pt;
                                            var p2 = pt.OffsetX(THESAURUSSLIVER);
                                            drawDomePipe(new GLineSegment(p1, p2));
                                            pt = p2;
                                        }
                                        if (isLeftOrRight == THESAURUSNEGATIVE && run.IsLongTranslatorToLeftOrRight == THESAURUSESPECIALLY)
                                        {
                                            var p1 = pt;
                                            var p2 = pt.OffsetX(-THESAURUSSLIVER);
                                            drawDomePipe(new GLineSegment(p1, p2));
                                            pt = p2;
                                        }
                                        Action f;
                                        var segs = vecs.ToGLineSegments(pt);
                                        {
                                            var _segs = segs.ToList();
                                            if (hanging.FloorDrainsCount == TEREBINTHINATED)
                                            {
                                                if (hanging.IsSeries)
                                                {
                                                    _segs.RemoveAt(DOLICHOCEPHALOUS);
                                                }
                                            }
                                            else if (hanging.FloorDrainsCount == PIEZOELECTRICAL)
                                            {
                                                _segs = segs.Take(DOLICHOCEPHALOUS).ToList();
                                            }
                                            else if (hanging.FloorDrainsCount == BATHYDRACONIDAE)
                                            {
                                                _segs = segs.Take(THESAURUSEVINCE).ToList();
                                            }
                                            if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(TEREBINTHINATED); }
                                            f = () => { drawDomePipes(_segs, THESAURUSAMENITY); };
                                        }
                                        if (hanging.FloorDrainsCount == PIEZOELECTRICAL)
                                        {
                                            var p = segs.Last(THESAURUSINTELLECT).EndPoint;
                                            _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p + new Vector2d(DIETHYLSTILBOESTROL, -THESAURUSUNDERWATER));
                                        }
                                        if (hanging.FloorDrainsCount == TEREBINTHINATED)
                                        {
                                            var p2 = segs.Last(THESAURUSINTELLECT).EndPoint;
                                            var p1 = segs.Last(PIEZOELECTRICAL).EndPoint;
                                            _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                            _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p1 + new Vector2d(THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL));
                                            DrawNoteText(v2, p2 + new Vector2d(HYPERSENSITIZED - THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL));
                                            if (!hanging.IsSeries)
                                            {
                                                drawDomePipes(new GLineSegment[] { segs.Last(TEREBINTHINATED) }, THESAURUSAMENITY);
                                            }
                                            {
                                                var _segs = new List<Vector2d> { new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(QUOTATIONDERNIER, BATHYDRACONIDAE), new Vector2d(QUOTATIONPATRONAL, BATHYDRACONIDAE), new Vector2d(MECHANORECEPTION, BATHYDRACONIDAE), new Vector2d(THESAURUSCRITICIZE, -THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, -INACCESSIBILITY), new Vector2d(THESAURUSSETBACK, -THESAURUSSETBACK) }.ToGLineSegments(p1);
                                                _segs.RemoveAt(TEREBINTHINATED);
                                                var seg = new List<Vector2d> { new Vector2d(ALSOHEAVENWARDS, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK) }.ToGLineSegments(p1)[PIEZOELECTRICAL];
                                                f = () =>
                                                {
                                                    drawDomePipes(_segs, THESAURUSAMENITY);
                                                    drawDomePipes(new GLineSegment[] { seg }, THESAURUSAMENITY);
                                                };
                                            }
                                        }
                                        {
                                            var p = segs.Last(THESAURUSINTELLECT).EndPoint;
                                            var seg = new List<Vector2d> { new Vector2d(UNCOMPREHENDING, -THESAURUSPERSUASION), new Vector2d(BATHYDRACONIDAE, -THESAURUSINSECTICIDE) }.ToGLineSegments(p)[PIEZOELECTRICAL];
                                            var pt1 = segs.First().StartPoint;
                                            var pt2 = pt1.OffsetY(THESAURUSINSECTICIDE);
                                            var dim = DrawDimLabel(pt1, pt2, new Vector2d(CONSTRUCTIONISM, BATHYDRACONIDAE), THESAURUSCONSUME, THESAURUSRESIGN);
                                        }
                                        if (hanging.HasSCurve)
                                        {
                                            var p1 = segs.Last(THESAURUSINTELLECT).StartPoint;
                                            _DrawSCurve(vec7, p1, isLeftOrRight);
                                        }
                                        if (hanging.HasDoubleSCurve)
                                        {
                                            var p1 = segs.Last(THESAURUSINTELLECT).StartPoint;
                                            _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSAMENITY);
                                        }
                                        f?.Invoke();
                                    }
                                }
                                else
                                {
                                    if (gpItem.IsFL0)
                                    {
                                        DrawFloorDrain((info.StartPoint + new Vector2d(-QUOTATIONARENACEOUS, -THESAURUSENTHUSE)).ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY);
                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -LACKADAISICALNESS), new Vector2d(-THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(-MAGNETOHYDRODYNAMICS, BATHYDRACONIDAE) };
                                        var segs = vecs.ToGLineSegments(info.StartPoint).Skip(PIEZOELECTRICAL).ToList();
                                        drawDomePipes(segs, THESAURUSAMENITY);
                                    }
                                    else
                                    {
                                        var beShort = hanging.FloorDrainsCount == PIEZOELECTRICAL && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                        var vecs = new List<Vector2d> { new Vector2d(-THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(beShort ? BATHYDRACONIDAE : -THESAURUSCHANNEL, BATHYDRACONIDAE), new Vector2d(-w, BATHYDRACONIDAE), new Vector2d(-THESAURUSDOWNHEARTED, BATHYDRACONIDAE), new Vector2d(-THESAURUSFABULOUS, BATHYDRACONIDAE) };
                                        if (isLeftOrRight == THESAURUSESPECIALLY)
                                        {
                                            vecs = vecs.GetYAxisMirror();
                                        }
                                        var startPt = info.StartPoint.OffsetY(-THESAURUSCONSERVATIVE - PIEZOELECTRICAL);
                                        if (hanging.FloorDrainsCount == BATHYDRACONIDAE && hanging.HasDoubleSCurve)
                                        {
                                            startPt = info.EndPoint.OffsetY(-HYDROXYNAPHTHALENE + HEIGHT / DOLICHOCEPHALOUS);
                                        }
                                        var ok = THESAURUSESPECIALLY;
                                        if (hanging.FloorDrainsCount == TEREBINTHINATED && !hanging.HasDoubleSCurve)
                                        {
                                            if (hanging.IsSeries)
                                            {
                                                var segs = vecs.ToGLineSegments(startPt);
                                                var _segs = segs.ToList();
                                                linesDfferencers.Add(GRect.Create(_segs[THESAURUSINTELLECT].EndPoint, THESAURUSFORTIFICATION).ToPolygon());
                                                var p2 = segs.Last(THESAURUSINTELLECT).EndPoint;
                                                var p1 = segs.Last(PIEZOELECTRICAL).EndPoint;
                                                _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                                _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                                Get2FloorDrainDN(out string v1, out string v2);
                                                DrawNoteText(v1, p1 + new Vector2d(THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL));
                                                DrawNoteText(v2, p2 + new Vector2d(HYPERSENSITIZED - THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL));
                                                segs = new List<Vector2d> { new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(QUOTATIONDERNIER, BATHYDRACONIDAE), new Vector2d(QUOTATIONPATRONAL, BATHYDRACONIDAE), new Vector2d(THESAURUSBRAVADO, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK) }.ToGLineSegments(p1);
                                                var p = segs[THESAURUSEVINCE].StartPoint;
                                                segs.RemoveAt(TEREBINTHINATED);
                                                dome_lines.AddRange(segs);
                                                dome_lines.AddRange(new List<Vector2d> { new Vector2d(VERGISSMEINNICHT, BATHYDRACONIDAE), new Vector2d(THESAURUSCRITICIZE, -THESAURUSSETBACK) }.ToGLineSegments(p));
                                            }
                                            else
                                            {
                                                var delta = QUOTATIONPATRONAL;
                                                var _vecs1 = new List<Vector2d> { new Vector2d(-THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(-THESAURUSCHANNEL, BATHYDRACONIDAE), };
                                                var _vecs2 = new List<Vector2d> { new Vector2d(THESAURUSSETBACK + delta, THESAURUSSETBACK + delta), new Vector2d(THESAURUSCHANNEL - delta, BATHYDRACONIDAE), };
                                                var segs1 = _vecs1.ToGLineSegments(startPt);
                                                var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                                                dome_lines.AddRange(segs1);
                                                dome_lines.AddRange(segs2);
                                                _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSNEGATIVE, i, j, THESAURUSAMENITY);
                                                _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), THESAURUSESPECIALLY, i, j, THESAURUSAMENITY);
                                            }
                                            ok = THESAURUSNEGATIVE;
                                        }
                                        Action f = null;
                                        if (!ok)
                                        {
                                            if (gpItem.Hangings[i].FlCaseEnum != FixingLogic1.FlCaseEnum.Case1)
                                            {
                                                var segs = vecs.ToGLineSegments(startPt);
                                                var _segs = segs.ToList();
                                                {
                                                    if (hanging.FloorDrainsCount == TEREBINTHINATED)
                                                    {
                                                        if (hanging.IsSeries)
                                                        {
                                                            _segs.RemoveAt(THESAURUSINTELLECT);
                                                        }
                                                    }
                                                    if (hanging.FloorDrainsCount == PIEZOELECTRICAL)
                                                    {
                                                        _segs.RemoveAt(THESAURUSEVINCE);
                                                        _segs.RemoveAt(THESAURUSINTELLECT);
                                                    }
                                                    if (hanging.FloorDrainsCount == BATHYDRACONIDAE)
                                                    {
                                                        _segs = _segs.Take(TEREBINTHINATED).ToList();
                                                    }
                                                    if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(TEREBINTHINATED); }
                                                }
                                                if (hanging.FloorDrainsCount == PIEZOELECTRICAL)
                                                {
                                                    var p = segs.Last(THESAURUSINTELLECT).EndPoint;
                                                    _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p + new Vector2d(DIETHYLSTILBOESTROL, -THESAURUSUNDERWATER));
                                                }
                                                if (hanging.FloorDrainsCount == TEREBINTHINATED)
                                                {
                                                    var p2 = segs.Last(THESAURUSINTELLECT).EndPoint;
                                                    var p1 = segs.Last(PIEZOELECTRICAL).EndPoint;
                                                    _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                                    _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSAMENITY);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p1 + new Vector2d(THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL));
                                                    DrawNoteText(v2, p2 + new Vector2d(HYPERSENSITIZED - THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL));
                                                }
                                                f = () => drawDomePipes(_segs, THESAURUSAMENITY);
                                            }
                                        }
                                        {
                                            var segs = vecs.ToGLineSegments(startPt);
                                            if (hanging.HasSCurve)
                                            {
                                                var p1 = segs.Last(THESAURUSINTELLECT).StartPoint;
                                                _DrawSCurve(vec7, p1, isLeftOrRight);
                                            }
                                            if (hanging.HasDoubleSCurve)
                                            {
                                                var p1 = segs.Last(THESAURUSINTELLECT).StartPoint;
                                                if (gpItem.Hangings[i].FlCaseEnum == FixingLogic1.FlCaseEnum.Case1)
                                                {
                                                    var p2 = p1 + vec7;
                                                    var segs1 = new List<Vector2d> { new Vector2d(-DNIPRODZERZHINSK + THESAURUSSMUDGE + QUOTATIONSORCERER, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -CONSTRUCTIONISM - THESAURUSPROGRAMME - QUOTATIONSORCERER), new Vector2d(UNCONJECTURABLE, -UNCONJECTURABLE) }.ToGLineSegments(p2);
                                                    drawDomePipes(segs1, THESAURUSAMENITY);
                                                    {
                                                        Vector2d v = default;
                                                        var b = isLeftOrRight;
                                                        if (b && getDSCurveValue() == INSTITUTIONALIZED)
                                                        {
                                                            b = THESAURUSESPECIALLY;
                                                            v = new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSBEFRIEND);
                                                        }
                                                        _DrawDSCurve(default(Vector2d), p2 + v, b, i, j, THESAURUSAMENITY);
                                                    }
                                                    var p3 = segs1.Last().EndPoint;
                                                    var p4 = p3.OffsetY(THESAURUSEXPIRE);
                                                    DrawDimLabel(p3, p4, new Vector2d(CONSTRUCTIONISM, BATHYDRACONIDAE), THESAURUSCONSUME, THESAURUSRESIGN);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    Dr.DrawDN_2(segs1.Last().StartPoint + new Vector2d(CONVENTIONALIZE + INDISCRIMINATENESS - QUOTATIONPATRONAL - INFINITESIMALLY, -UNCONJECTURABLE), THESAURUSSMASHING, v1);
                                                }
                                                else
                                                {
                                                    var fixY = QUOTATIONSENECA + DIETHYLSTILBOESTROL;
                                                    _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSAMENITY);
                                                    if (getDSCurveValue() == INSTITUTIONALIZED)
                                                    {
                                                        var segs1 = new List<Vector2d> { new Vector2d(THESAURUSCHANNEL, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, -THESAURUSSETBACK) }.ToGLineSegments(p1.OffsetY(QUOTATIONPATRONAL));
                                                        f = () => { drawDomePipes(segs1, THESAURUSAMENITY); };
                                                    }
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p1.OffsetY(fixY));
                                                }
                                            }
                                        }
                                        f?.Invoke();
                                    }
                                }
                                if (linesDfferencers.Count > BATHYDRACONIDAE)
                                {
                                    var killer = GeoFac.CreateGeometryEx(linesDfferencers);
                                    dome_lines = GeoFac.GetLines(GeoFac.CreateGeometry(dome_lines.Select(x => x.ToLineString())).Difference(killer)).ToList();
                                    linesDfferencers.Clear();
                                }
                            }
                            void handleBranchInfo(ThwPipeRun run, PipeRunLocationInfo info)
                            {
                                var bi = run.BranchInfo;
                                if (bi.FirstLeftRun)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, TEREBINTHINATED));
                                    var p3 = info.EndPoint.OffsetX(DIETHYLSTILBOESTROL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(TEREBINTHINATED, THESAURUSINTELLECT));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.FirstRightRun)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, PARALLELOGRAMMIC));
                                    var p3 = info.EndPoint.OffsetX(-DIETHYLSTILBOESTROL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, THESAURUSINTELLECT));
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
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, PARALLELOGRAMMIC));
                                    var p3 = info.EndPoint.OffsetX(-DIETHYLSTILBOESTROL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, THESAURUSINTELLECT));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.BlueToRightFirst)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, PARALLELOGRAMMIC));
                                    var p3 = info.EndPoint.OffsetX(DIETHYLSTILBOESTROL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PIEZOELECTRICAL, THESAURUSINTELLECT));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.BlueToLeftLast)
                                {
                                    if (run.HasLongTranslator)
                                    {
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var _dy = DIETHYLSTILBOESTROL;
                                            var vs1 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE - _dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy + _dy + PSYCHOPHYSIOLOGICAL), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -DIETHYLSTILBOESTROL), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(PIEZOELECTRICAL).ToList());
                                        }
                                        else
                                        {
                                            var _dy = DIETHYLSTILBOESTROL;
                                            var vs1 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy), new Vector2d(THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy - _dy + PSYCHOPHYSIOLOGICAL), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -DIETHYLSTILBOESTROL), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(PIEZOELECTRICAL).ToList());
                                        }
                                    }
                                    else if (!run.HasLongTranslator)
                                    {
                                        var vs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -CONSERVATIONIST), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                        info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                    }
                                }
                                if (bi.BlueToRightLast)
                                {
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var p1 = info.EndPoint;
                                        var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE));
                                        var p3 = info.EndPoint.OffsetX(DIETHYLSTILBOESTROL);
                                        var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE));
                                        var p5 = p1.OffsetY(HEIGHT);
                                        info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, p2), new GLineSegment(p2, p5) };
                                    }
                                }
                                if (bi.BlueToLeftMiddle)
                                {
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var p1 = info.EndPoint;
                                        var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE));
                                        var p3 = info.EndPoint.OffsetX(-DIETHYLSTILBOESTROL);
                                        var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE));
                                        var segs = info.Segs.ToList();
                                        segs.Add(new GLineSegment(p2, p4));
                                        info.DisplaySegs = segs;
                                    }
                                    else if (run.HasLongTranslator)
                                    {
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var _dy = DIETHYLSTILBOESTROL;
                                            var vs1 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE - _dy), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy + _dy) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -DIETHYLSTILBOESTROL), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(PIEZOELECTRICAL).ToList());
                                        }
                                        else
                                        {
                                            var _dy = DIETHYLSTILBOESTROL;
                                            var vs1 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSADMIRABLE + _dy), new Vector2d(THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(GASTRONOMICALLY, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, -THESAURUSOBLITERATION), new Vector2d(BATHYDRACONIDAE, -THESAURUSBEWARE - dy - _dy) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -DIETHYLSTILBOESTROL), new Vector2d(-DIETHYLSTILBOESTROL, -METHYLCARBAMATE) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(PIEZOELECTRICAL).ToList());
                                        }
                                    }
                                }
                                if (bi.BlueToRightMiddle)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE));
                                    var p3 = info.EndPoint.OffsetX(DIETHYLSTILBOESTROL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE));
                                    var segs = info.Segs.ToList();
                                    segs.Add(new GLineSegment(p2, p4));
                                    info.DisplaySegs = segs;
                                }
                                {
                                    var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -DNIPRODZERZHINSK), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-HETEROTRANSPLANTED, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -THESAURUSINTESTINAL), new Vector2d(-UNCONJECTURABLE, -UNCONJECTURABLE) };
                                    if (bi.HasLongTranslatorToLeft)
                                    {
                                        var vs = vecs;
                                        info.DisplaySegs = vecs.ToGLineSegments(info.StartPoint);
                                        if (!bi.IsLast)
                                        {
                                            var pt = vs.Take(vs.Count - PIEZOELECTRICAL).GetLastPoint(info.StartPoint);
                                            info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -CINEFLUOROGRAPHY) }.ToGLineSegments(pt));
                                        }
                                    }
                                    if (bi.HasLongTranslatorToRight)
                                    {
                                        var vs = vecs.GetYAxisMirror();
                                        info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                        if (!bi.IsLast)
                                        {
                                            var pt = vs.Take(vs.Count - PIEZOELECTRICAL).GetLastPoint(info.StartPoint);
                                            info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -CINEFLUOROGRAPHY) }.ToGLineSegments(pt));
                                        }
                                    }
                                }
                            }
                            if (run.LeftHanging != null)
                            {
                                run.LeftHanging.IsSeries = gpItem.Hangings.TryGet(i + PIEZOELECTRICAL)?.IsSeries ?? THESAURUSNEGATIVE;
                                handleHanging(run.LeftHanging, THESAURUSNEGATIVE);
                            }
                            if (run.BranchInfo != null)
                            {
                                handleBranchInfo(run, info);
                            }
                            if (run.ShowShortTranslatorLabel)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(OBSERVATIONALLY, OBSERVATIONALLY), new Vector2d(-THESAURUSDETERIORATE, THESAURUSDETERIORATE), new Vector2d(-RECRYSTALLIZATION, BATHYDRACONIDAE) };
                                var segs = vecs.ToGLineSegments(info.EndPoint).Skip(PIEZOELECTRICAL).ToList();
                                DrawDraiNoteLines(segs);
                                DrawDraiNoteLines(segs);
                                var text = THESAURUSTUSSLE;
                                var pt = segs.Last().EndPoint;
                                DrawNoteText(text, pt);
                            }
                            if (run.HasCheckPoint)
                            {
                                var h = HEIGHT / THESAURUSYAWNING * THESAURUSEVINCE;
                                if (!run.HasLongTranslator)
                                {
                                    if (IsPL(gpItem.Labels.First()) || gpItem.Hangings[i].HasDoubleSCurve)
                                    {
                                        h = HEIGHT / THESAURUSYAWNING * PARALLELOGRAMMIC;
                                    }
                                }
                                Point2d pt1, pt2;
                                if (run.HasShortTranslator)
                                {
                                    var p = info.Segs.Last().StartPoint;
                                    pt1 = p.OffsetY(h);
                                    pt2 = new Point2d(p.X, info.EndPoint.Y);
                                }
                                else
                                {
                                    pt1 = info.EndPoint.OffsetY(h);
                                    pt2 = info.EndPoint;
                                }
                                _DrawCheckPoint(pt1, THESAURUSNEGATIVE, THESAURUSAMENITY);
                                if (storey == THESAURUSDEFILE)
                                {
                                    var dx = -CONSTRUCTIONISM;
                                    if (gpItem.HasBasinInKitchenAt1F)
                                    {
                                        dx = CONSTRUCTIONISM;
                                    }
                                    {
                                        var dim = DrawDimLabel(pt1, pt2, new Vector2d(dx, BATHYDRACONIDAE), gpItem.PipeType == PipeType.PL ? THESAURUSHEARTY : STOICHIOMETRICALLY, THESAURUSRESIGN);
                                        if (dx < BATHYDRACONIDAE)
                                        {
                                            dim.TextPosition = (pt1 + new Vector2d(dx, BATHYDRACONIDAE) + new Vector2d(-THESAURUSCOMMAND, -UNCONJECTURABLE) + new Vector2d(BATHYDRACONIDAE, QUOTATIONPATRONAL)).ToPoint3d();
                                        }
                                    }
                                    if (gpItem.HasTl && allStoreys[i] == gpItem.MinTl + PHENYLENEDIAMINE)
                                    {
                                        var k = THESAURUSRECAPITULATE / HEIGHT;
                                        pt1 = info.EndPoint;
                                        pt2 = pt1.OffsetY(SUPERESSENTIALIS * k);
                                        if (run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight)
                                        {
                                            pt2 = pt1.OffsetY(METAMATHEMATICS);
                                        }
                                        var dim = DrawDimLabel(pt1, pt2, new Vector2d(CONSTRUCTIONISM, BATHYDRACONIDAE), STOICHIOMETRICALLY, THESAURUSRESIGN);
                                    }
                                }
                            }
                            if (run.HasDownBoardLine)
                            {
                                _DrawHorizontalLineOnPipeRun(info.BasePoint.ToPoint3d());
                            }
                            if (run.HasCleaningPort)
                            {
                                if (run.HasLongTranslator)
                                {
                                    var vecs = new List<Vector2d> { new Vector2d(-UNCONJECTURABLE, UNCONJECTURABLE), new Vector2d(BATHYDRACONIDAE, DIETHYLSTILBOESTROL), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(THESAURUSMODIFY, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, TRISYLLABICALLY) };
                                    if (run.IsLongTranslatorToLeftOrRight == THESAURUSESPECIALLY)
                                    {
                                        vecs = vecs.GetYAxisMirror();
                                    }
                                    if (run.HasShortTranslator)
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last(TEREBINTHINATED).StartPoint.OffsetY(-DIETHYLSTILBOESTROL));
                                        drawDomePipes(segs, THESAURUSAMENITY);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, TEREBINTHINATED);
                                    }
                                    else
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-DIETHYLSTILBOESTROL));
                                        drawDomePipes(segs, THESAURUSAMENITY);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, TEREBINTHINATED);
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var pt1 = segs.First().StartPoint;
                                            var pt2 = pt1.OffsetY(THESAURUSSTEEPLE);
                                            var dim = DrawDimLabel(pt1, pt2, new Vector2d(-CONSTRUCTIONISM, BATHYDRACONIDAE), THESAURUSCONSUME, THESAURUSRESIGN);
                                            dim.TextPosition = (pt1 + new Vector2d(-CONSTRUCTIONISM, BATHYDRACONIDAE) + new Vector2d(-PROCHLORPERAZINE, THESAURUSSATURATED) + new Vector2d(THESAURUSREBUKE, UNCONJECTURABLE - THESAURUSMEETING)).ToPoint3d();
                                        }
                                    }
                                }
                                else
                                {
                                    _DrawCleaningPort(info.StartPoint.OffsetY(-DIETHYLSTILBOESTROL).ToPoint3d(), THESAURUSNEGATIVE, TEREBINTHINATED);
                                }
                            }
                            if (run.HasShortTranslator)
                            {
                                DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                            }
                        }
                        var showAllFloorDrainLabel = THESAURUSESPECIALLY;
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var (ok, item) = gpItem.Items.TryGetValue(i + PIEZOELECTRICAL);
                            if (!ok) continue;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                            }
                        }
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var hanging = gpItem.Hangings.TryGet(i + PIEZOELECTRICAL);
                            if (hanging == null) continue;
                            var fdsCount = hanging.FloorDrainsCount;
                            if (fdsCount == BATHYDRACONIDAE) continue;
                            var wfdsCount = hanging.WashingMachineFloorDrainsCount;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (fdsCount > BATHYDRACONIDAE)
                                {
                                    if (wfdsCount > BATHYDRACONIDAE)
                                    {
                                        wfdsCount--;
                                        washingMachineFloorDrainShooters.Add(pt.ToNTSPoint());
                                    }
                                    fdsCount--;
                                }
                            }
                        }
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var hanging = gpItem.Hangings.TryGet(i + PIEZOELECTRICAL);
                            if (hanging == null) continue;
                            var fdsCount = hanging.FloorDrainsCount;
                            if (fdsCount == BATHYDRACONIDAE) continue;
                            var wfdsCount = hanging.WashingMachineFloorDrainsCount;
                            var h = THESAURUSDISABILITY;
                            var ok_texts = new HashSet<string>();
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (fdsCount > BATHYDRACONIDAE)
                                {
                                    if (wfdsCount > BATHYDRACONIDAE)
                                    {
                                        wfdsCount--;
                                        h += DNIPRODZERZHINSK;
                                        if (hanging.RoomName != null)
                                        {
                                            var text = $"接{hanging.RoomName}洗衣机地漏";
                                            if (!ok_texts.Contains(text))
                                            {
                                                _DrawLabel(text, $"{getWashingMachineFloorDrainDN()}，余同", pt, THESAURUSNEGATIVE, h);
                                                ok_texts.Add(text);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        h += DNIPRODZERZHINSK;
                                        if (hanging.RoomName != null)
                                        {
                                            _DrawLabel($"接{hanging.RoomName}地漏", $"{getWashingMachineFloorDrainDN()}，余同", pt, THESAURUSNEGATIVE, h);
                                        }
                                    }
                                    fdsCount--;
                                }
                            }
                            break;
                        }
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (showAllFloorDrainLabel)
                                {
                                }
                                else
                                {
                                }
                            }
                        }
                        {
                            var f = GeoFac.CreateIntersectsSelector(washingMachineFloorDrainShooters);
                            foreach (var kv in floorDrainCbs)
                            {
                                var o = kv.Value;
                                if (f(kv.Key).Any())
                                {
                                    o.Name = THESAURUSQUIETLY;
                                }
                                DrawFloorDrain(o.BasePt.ToPoint3d(), o.LeftOrRight, o.Name);
                            }
                        }
                    }
                    PipeRunLocationInfo[] getPipeRunLocationInfos()
                    {
                        var infos = new PipeRunLocationInfo[allStoreys.Count];
                        for (int i = BATHYDRACONIDAE; i < allStoreys.Count; i++)
                        {
                            infos[i] = new PipeRunLocationInfo() { Visible = THESAURUSNEGATIVE, Storey = allStoreys[i], };
                        }
                        {
                            var tdx = THESAURUSINDEMNIFY;
                            for (int i = start; i >= end; i--)
                            {
                                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                                var basePt = bsPt1.OffsetX(OFFSET_X + (j + PIEZOELECTRICAL) * SPAN_X) + new Vector2d(tdx, BATHYDRACONIDAE);
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                fixY = BATHYDRACONIDAE;
                                PipeRunLocationInfo drawNormal()
                                {
                                    {
                                        var vecs = vecs0;
                                        var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        infos[i].BasePoint = basePt;
                                        infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                        infos[i].HangingEndPoint = infos[i].EndPoint;
                                        infos[i].Vector2ds = vecs;
                                        infos[i].Segs = segs;
                                        infos[i].RightSegsMiddle = segs.Select(x => x.Offset(DIETHYLSTILBOESTROL, BATHYDRACONIDAE)).ToList();
                                        infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC);
                                    }
                                    {
                                        var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                        infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))));
                                    }
                                    {
                                        var info = infos[i];
                                        var k = HEIGHT / THESAURUSRECAPITULATE;
                                        var vecs = new List<Vector2d> { new Vector2d(DIETHYLSTILBOESTROL, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, -QUOTATION1AUNIPOLAR * k), new Vector2d(-DIETHYLSTILBOESTROL, -SUPERESSENTIALIS * k) };
                                        var segs = vecs.ToGLineSegments(info.EndPoint.OffsetY(HEIGHT)).Skip(PIEZOELECTRICAL).ToList();
                                        info.RightSegsLast = segs;
                                    }
                                    {
                                        var pt = infos[i].Segs.First().StartPoint.OffsetX(DIETHYLSTILBOESTROL);
                                        var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE))) };
                                        infos[i].RightSegsFirst = segs;
                                        segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, infos[i].EndPoint.OffsetX(DIETHYLSTILBOESTROL)));
                                    }
                                    return infos[i];
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
                                _dy = run.DrawLongHLineHigher ? THESAURUSMANKIND : BATHYDRACONIDAE;
                                if (run.HasLongTranslator && run.HasShortTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs3;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSEVINCE);
                                            segs.Add(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[THESAURUSINTELLECT].EndPoint.OffsetXY(-DIETHYLSTILBOESTROL, -DIETHYLSTILBOESTROL)));
                                            segs.Add(new GLineSegment(segs[TEREBINTHINATED].EndPoint, new Point2d(segs[DOLICHOCEPHALOUS].EndPoint.X, segs[TEREBINTHINATED].EndPoint.Y)));
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSINTELLECT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSINTELLECT], new GLineSegment(segs[THESAURUSINTELLECT].StartPoint, segs[BATHYDRACONIDAE].StartPoint) };
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, infos[i].EndPoint.OffsetX(DIETHYLSTILBOESTROL)));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs6;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(-THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))).Offset(PRAETERNATURALIS, BATHYDRACONIDAE));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSEVINCE);
                                            segs.Add(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[THESAURUSEVINCE].StartPoint));
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, infos[i].EndPoint.OffsetX(DIETHYLSTILBOESTROL)));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].Segs[THESAURUSEVINCE].EndPoint;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    switch (gpItem.Hangings[i].FlFixType)
                                    {
                                        case FixingLogic1.FlFixType.NoFix:
                                            break;
                                        case FixingLogic1.FlFixType.MiddleHigher:
                                            fixY = OBJECTIONABLENESS / THESAURUSCONFECTIONERY * HEIGHT;
                                            break;
                                        case FixingLogic1.FlFixType.Lower:
                                            fixY = -PALAEOICHTHYOLOGY / TEREBINTHINATED / THESAURUSCONFECTIONERY * HEIGHT;
                                            break;
                                        case FixingLogic1.FlFixType.Higher:
                                            fixY = PALAEOICHTHYOLOGY / TEREBINTHINATED / THESAURUSCONFECTIONERY * HEIGHT + MICRODENSITOMETRY / THESAURUSCONFECTIONERY * HEIGHT;
                                            break;
                                        default:
                                            break;
                                    }
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs1;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs = segs.Take(THESAURUSEVINCE).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[THESAURUSINTELLECT].EndPoint.OffsetXY(-DIETHYLSTILBOESTROL, -DIETHYLSTILBOESTROL))).ToList();
                                            segs.Add(new GLineSegment(segs[TEREBINTHINATED].EndPoint, new Point2d(segs[DOLICHOCEPHALOUS].EndPoint.X, segs[TEREBINTHINATED].EndPoint.Y)));
                                            segs.RemoveAt(DOLICHOCEPHALOUS);
                                            segs.RemoveAt(THESAURUSINTELLECT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSINTELLECT], new GLineSegment(segs[THESAURUSINTELLECT].StartPoint, segs[BATHYDRACONIDAE].StartPoint) };
                                            var h = HEIGHT - THESAURUSCONFECTIONERY;
                                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSINTANGIBLE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSEXPERIENCED, BATHYDRACONIDAE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, -THESAURUSLEANING - QUOTATIONPATRONAL - h), new Vector2d(-UNCONJECTURABLE, -THESAURUSEXCESSIVE) };
                                            segs = vecs.ToGLineSegments(infos[i].BasePoint.OffsetXY(DIETHYLSTILBOESTROL, HEIGHT));
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs4;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(DIETHYLSTILBOESTROL)).Skip(PIEZOELECTRICAL).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))).Offset(PRAETERNATURALIS, BATHYDRACONIDAE));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle;
                                            infos[i].RightSegsLast = segs.Take(THESAURUSEVINCE).YieldAfter(new GLineSegment(segs[THESAURUSINTELLECT].EndPoint, segs[DOLICHOCEPHALOUS].StartPoint)).YieldAfter(segs[DOLICHOCEPHALOUS]).ToList();
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSEVINCE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(PARALLELOGRAMMIC, THESAURUSINTRACTABLE))) };
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].EndPoint, pt.OffsetXY(-DIETHYLSTILBOESTROL, HEIGHT.ToRatioInt(THESAURUSABUNDANCE, THESAURUSINTRACTABLE))));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].EndPoint;
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
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = segs.Select(x => x.Offset(DIETHYLSTILBOESTROL, BATHYDRACONIDAE)).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, segs[TEREBINTHINATED].StartPoint), segs[TEREBINTHINATED] };
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[TEREBINTHINATED].StartPoint, segs[TEREBINTHINATED].EndPoint);
                                            segs[TEREBINTHINATED] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(BATHYDRACONIDAE);
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, r.RightButtom));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs5;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(PIEZOELECTRICAL).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, BATHYDRACONIDAE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = segs.Select(x => x.Offset(DIETHYLSTILBOESTROL, BATHYDRACONIDAE)).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / PARALLELOGRAMMIC).OffsetX(-THESAURUSSETBACK);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - THESAURUSABUNDANCE, THESAURUSINTRACTABLE)), pt.OffsetXY(-DIETHYLSTILBOESTROL, -HEIGHT.ToRatioInt(THESAURUSINTRACTABLE - PARALLELOGRAMMIC, THESAURUSINTRACTABLE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, segs[TEREBINTHINATED].StartPoint), segs[TEREBINTHINATED] };
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[TEREBINTHINATED].StartPoint, segs[TEREBINTHINATED].EndPoint);
                                            segs[TEREBINTHINATED] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(BATHYDRACONIDAE);
                                            segs.Add(new GLineSegment(segs[BATHYDRACONIDAE].StartPoint, r.RightButtom));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].Segs[BATHYDRACONIDAE].EndPoint;
                                }
                                else
                                {
                                    drawNormal();
                                }
                            }
                        }
                        for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                        {
                            var info = infos.TryGet(i);
                            if (info != null)
                            {
                                info.StartPoint = info.BasePoint.OffsetY(HEIGHT);
                            }
                        }
                        return infos;
                    }
                    var infos = getPipeRunLocationInfos();
                    handlePipeLine(thwPipeLine, infos);
                    static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight)
                    {
                        var gap = THESAURUSFORTIFICATION;
                        var factor = THESAURUSADVANCEMENT;
                        double height = REVOLUTIONIZATION;
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
                            Dr.SetLabelStylesForDraiNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[PIEZOELECTRICAL].EndPoint : segs[PIEZOELECTRICAL].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(THESAURUSFORTIFICATION);
                        if (text1 != null)
                        {
                            var t = DrawTextLazy(text1, height, txtBasePt);
                            Dr.SetLabelStylesForDraiNote(t);
                        }
                        if (text2 != null)
                        {
                            var t = DrawTextLazy(text2, height, txtBasePt.OffsetY(-height - gap));
                            Dr.SetLabelStylesForDraiNote(t);
                        }
                    }
                    for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            var segs = info.DisplaySegs ?? info.Segs;
                            if (segs != null)
                            {
                                drawDomePipes(segs, THESAURUSAMENITY);
                            }
                        }
                    }
                    {
                        var _allSmoothStoreys = new List<string>();
                        for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                        {
                            var run = runs.TryGet(i);
                            if (run != null)
                            {
                                if (!run.HasLongTranslator && !run.HasShortTranslator)
                                {
                                    _allSmoothStoreys.Add(allNumStoreyLabels[i]);
                                }
                            }
                        }
                        var _storeys = new string[] { _allSmoothStoreys.GetAt(TEREBINTHINATED), _allSmoothStoreys.GetLastOrDefault(THESAURUSINTELLECT) }.SelectNotNull().Distinct().ToList();
                        if (_storeys.Count == BATHYDRACONIDAE)
                        {
                            _storeys = new string[] { _allSmoothStoreys.FirstOrDefault(), _allSmoothStoreys.LastOrDefault() }.SelectNotNull().Distinct().ToList();
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
                        foreach (var storey in _storeys)
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = infos[i];
                            {
                                string label1, label2;
                                var isLeftOrRight = !thwPipeLine.Labels.Any(x => IsFL(x));
                                var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).Distinct().OrderBy(x => x).ToList();
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
                                drawLabel(info.PlBasePt, label1, label2, isLeftOrRight);
                            }
                            if (gpItem.HasTl)
                            {
                                string label1, label2;
                                var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => IsTL(x))).Distinct().OrderBy(x => x).ToList();
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
                                drawLabel(info.PlBasePt.OffsetX(DIETHYLSTILBOESTROL), label1, label2, THESAURUSESPECIALLY);
                            }
                        }
                    }
                    bool getShouldToggleBlueMiddleLine()
                    {
                        return viewModel?.Params?.通气H件隔层布置 ?? THESAURUSESPECIALLY;
                    }
                    {
                        var _storeys = new string[] { allNumStoreyLabels.GetAt(PIEZOELECTRICAL), allNumStoreyLabels.GetLastOrDefault(TEREBINTHINATED) }.SelectNotNull().Distinct().ToList();
                        if (_storeys.Count == BATHYDRACONIDAE)
                        {
                            _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                        }
                        foreach (var storey in _storeys)
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = infos.TryGet(i);
                            if (info != null && info.Visible)
                            {
                                var run = runs.TryGet(i);
                                if (run != null)
                                {
                                    var v = default(Vector2d);
                                    if (((gpItem.Hangings.TryGet(i + PIEZOELECTRICAL)?.FloorDrainsCount ?? BATHYDRACONIDAE) > BATHYDRACONIDAE)
                                        || (gpItem.Hangings.TryGet(i)?.HasDoubleSCurve ?? THESAURUSESPECIALLY))
                                    {
                                        v = new Vector2d(CONSTRUCTIONISM, BATHYDRACONIDAE);
                                    }
                                    if (gpItem.IsFL0)
                                    {
                                        Dr.DrawDN_2(info.EndPoint + v, THESAURUSSMASHING, viewModel?.Params?.DirtyWaterWellDN ?? THESAURUSSPITEFUL);
                                    }
                                    else
                                    {
                                        Dr.DrawDN_2(info.EndPoint + v, THESAURUSSMASHING);
                                    }
                                    if (gpItem.HasTl)
                                    {
                                        Dr.DrawDN_3(info.EndPoint.OffsetXY(DIETHYLSTILBOESTROL, BATHYDRACONIDAE), THESAURUSSMASHING);
                                    }
                                }
                            }
                        }
                    }
#pragma warning disable
                    var b = THESAURUSESPECIALLY;
                    for (int i = BATHYDRACONIDAE; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            void TestRightSegsMiddle()
                            {
                                var segs = info.RightSegsMiddle;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSAMENITY);
                                }
                            }
                            void TestRightSegsLast()
                            {
                                var segs = info.RightSegsLast;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSAMENITY);
                                }
                            }
                            void TestRightSegsFirst()
                            {
                                var segs = info.RightSegsFirst;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSAMENITY);
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
                                            drawVentPipes(segs, THESAURUSAMENITY);
                                        }
                                    }
                                    else if (gpItem.MinTl + PHENYLENEDIAMINE == storey)
                                    {
                                        var segs = info.RightSegsLast;
                                        if (segs != null)
                                        {
                                            drawVentPipes(segs, THESAURUSAMENITY);
                                        }
                                    }
                                    else if (GetStoreyScore(storey).InRange(gpItem.MinTl, gpItem.MaxTl))
                                    {
                                        var segs = info.RightSegsMiddle;
                                        if (segs != null)
                                        {
                                            if (getShouldToggleBlueMiddleLine())
                                            {
                                                b = !b;
                                                if (b) segs = segs.Take(PIEZOELECTRICAL).ToList();
                                            }
                                            drawVentPipes(segs, THESAURUSAMENITY);
                                        }
                                    }
                                }
                            }
                            Run();
                        }
                    }
                    {
                        var i = allNumStoreyLabels.IndexOf(THESAURUSDEFILE);
                        if (i >= BATHYDRACONIDAE)
                        {
                            var storey = allNumStoreyLabels[i];
                            var info = infos.First();
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
                                    DN1 = THESAURUSSPITEFUL,
                                };
                                if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                {
                                    var basePt = info.EndPoint;
                                    if (gpItem.HasRainPortForFL0)
                                    {
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSBEFRIEND), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSSPREAD, BATHYDRACONIDAE) };
                                            var segs = vecs.ToGLineSegments(basePt);
                                            drawDomePipes(segs, THESAURUSAMENITY);
                                            var pt = segs.Last().EndPoint.ToPoint3d();
                                            {
                                                Dr.DrawRainPort(pt.OffsetX(PSYCHOPHYSIOLOGICAL));
                                                Dr.DrawRainPortLabel(pt.OffsetX(-THESAURUSFORTIFICATION));
                                                Dr.DrawStarterPipeHeightLabel(pt.OffsetX(-THESAURUSFORTIFICATION + THESAURUSPOIGNANT));
                                            }
                                        }
                                        if (gpItem.IsConnectedToFloorDrainForFL0)
                                        {
                                            var p = basePt + new Vector2d(CHLOROFLUOROCARBONS, -THESAURUSENTHUSE);
                                            DrawFloorDrain(p.ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY);
                                            var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -THESAURUSCONFOUND), new Vector2d(-DIETHYLSTILBOESTROL, -DIETHYLSTILBOESTROL), new Vector2d(-DNIPRODZERZHINSK, BATHYDRACONIDAE), new Vector2d(-TETRAHYDROXYHEXANEDIOIC, TETRAHYDROXYHEXANEDIOIC) };
                                            var segs = vecs.ToGLineSegments(p + new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE));
                                            drawDomePipes(segs, THESAURUSAMENITY);
                                        }
                                    }
                                    else
                                    {
                                        var p = basePt + new Vector2d(CHLOROFLUOROCARBONS, -THESAURUSENTHUSE);
                                        if (gpItem.IsFL0)
                                        {
                                            if (gpItem.IsConnectedToFloorDrainForFL0)
                                            {
                                                if (gpItem.MergeFloorDrainForFL0)
                                                {
                                                    DrawFloorDrain(p.ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY);
                                                    {
                                                        var vecs = new List<Vector2d>() { new Vector2d(BATHYDRACONIDAE, -QUOTATIONPATRONAL + INTERFEROMETERS), new Vector2d(-DIETHYLSTILBOESTROL, -DIETHYLSTILBOESTROL), new Vector2d(-QUOTATIONPATRONAL - THESAURUSFULSOME + DISCONNECTEDNESS * TEREBINTHINATED, BATHYDRACONIDAE), new Vector2d(-DIETHYLSTILBOESTROL, DIETHYLSTILBOESTROL) };
                                                        var segs = vecs.ToGLineSegments(p + new Vector2d(-THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE));
                                                        drawDomePipes(segs, THESAURUSAMENITY);
                                                        var seg = new List<Vector2d> { new Vector2d(-PRAETERNATURALIS, -THESAURUSCONFOUND), new Vector2d(THESAURUSFEARLESS, BATHYDRACONIDAE) }.ToGLineSegments(segs.First().StartPoint)[PIEZOELECTRICAL];
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(BATHYDRACONIDAE, -CONSTRUCTIONISM), THESAURUSDEGREE, THESAURUSRESIGN);
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSIMAGINATIVE, -DISILLUSIONIZER), new Vector2d(THESAURUSCOOPERATION, BATHYDRACONIDAE), new Vector2d(DIETHYLSTILBOESTROL, DIETHYLSTILBOESTROL), new Vector2d(BATHYDRACONIDAE, THESAURUSCONFOUND) };
                                                    var segs = vecs.ToGLineSegments(info.EndPoint).Skip(PIEZOELECTRICAL).ToList();
                                                    drawDomePipes(segs, THESAURUSAMENITY);
                                                    DrawFloorDrain((segs.Last().EndPoint + new Vector2d(THESAURUSDOWNHEARTED, THESAURUSUNAVOIDABLE)).ToPoint3d(), THESAURUSNEGATIVE, PSYCHOLINGUISTICALLY);
                                                }
                                            }
                                        }
                                        DrawOutlets1(THESAURUSAMENITY, basePt, QUOTATIONLUNGEING, output, dy: -HEIGHT * THESAURUSEXPENDABLE, isRainWaterWell: THESAURUSNEGATIVE);
                                    }
                                }
                                else if (gpItem.IsSingleOutlet)
                                {
                                    void DrawOutlets3(string shadow, Point2d basePoint)
                                    {
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                        {
                                            Dr.DrawSimpleLabel(basePoint.OffsetY(-QUOTATIONPATRONAL), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                        }
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -HETEROTRANSPLANTED), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSORIGINATE, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, THESAURUSSUCCINCT), new Vector2d(PSYCHOGENICALLY, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, THESAURUSSUPPLEMENTARY) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(THESAURUSINTELLECT);
                                        drawDomePipes(segs, THESAURUSAMENITY);
                                        DrawDiryWaterWells1(segs[TEREBINTHINATED].EndPoint + new Vector2d(-PSYCHOPHYSIOLOGICAL, DIETHYLSTILBOESTROL), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[THESAURUSINTELLECT].StartPoint.OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[TEREBINTHINATED].EndPoint.OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                                        if (output.HasWrappingPipe2)
                                        {
                                            if (gpItem.OutletWrappingPipeRadius != null)
                                            {
                                                static void DrawLine(string layer, params GLineSegment[] segs)
                                                {
                                                    var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
                                                    foreach (var line in lines)
                                                    {
                                                        line.Layer = layer;
                                                        ByLayer(line);
                                                    }
                                                }
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = MICROSPECTROPHOTOMETRY)
                                                {
                                                    DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, label } }, cb: br => { ByLayer(br); });
                                                }
                                                var p1 = segs[TEREBINTHINATED].EndPoint.OffsetX(PSYCHOPHYSIOLOGICAL);
                                                var p2 = p1.OffsetY(-QUOTATIONCHOROID);
                                                var p3 = p2.OffsetX(THESAURUSBEFRIEND);
                                                var layer = THESAURUSSMASHING;
                                                DrawLine(layer, new GLineSegment(p1, p2));
                                                DrawLine(layer, new GLineSegment(p3, p2));
                                                DrawStoreyHeightSymbol(p3, THESAURUSSMASHING, gpItem.OutletWrappingPipeRadius);
                                                {
                                                    var _shadow = THESAURUSAMENITY;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > PIEZOELECTRICAL)
                                                    {
                                                        Dr.DrawSimpleLabel(p3, THESAURUSBENEFIT + _shadow.Substring(PIEZOELECTRICAL));
                                                    }
                                                }
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[THESAURUSINTELLECT].StartPoint.OffsetXY(THESAURUSAPPORTION, THESAURUSFORTIFICATION));
                                        DrawNoteText(output.DN2, segs[TEREBINTHINATED].EndPoint.OffsetXY(THESAURUSAPPORTION, THESAURUSFORTIFICATION));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSEVINCE].StartPoint.ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[TEREBINTHINATED].StartPoint.ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                                        DrawCleaningPort(segs[DOLICHOCEPHALOUS].EndPoint.ToPoint3d(), THESAURUSNEGATIVE, TEREBINTHINATED);
                                    }
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1 = gpItem.HasWrappingPipe;
                                    output.DN2 = THESAURUSSPITEFUL;
                                    DrawOutlets3(THESAURUSAMENITY, info.EndPoint);
                                }
                                else if (gpItem.FloorDrainsCountAt1F > BATHYDRACONIDAE)
                                {
                                    for (int k = BATHYDRACONIDAE; k < gpItem.FloorDrainsCountAt1F; k++)
                                    {
                                        var p = info.EndPoint + new Vector2d(CHLOROFLUOROCARBONS + k * DNIPRODZERZHINSK, -THESAURUSENTHUSE);
                                        DrawFloorDrain(p.ToPoint3d(), THESAURUSESPECIALLY, THESAURUSPRESENCE);
                                        var v = new Vector2d(THESAURUSDOWNHEARTED, -THESAURUSUNAVOIDABLE);
                                        Get2FloorDrainDN(out string v1, out string v2);
                                        if (k == BATHYDRACONIDAE)
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSSALUTATION + THESAURUSSMUDGE, -THESAURUSSENSATION), new Vector2d(THESAURUSHOUSING - THESAURUSSMUDGE, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, THESAURUSCONFOUND) };
                                            var segs = vecs.ToGLineSegments(p + v).Skip(PIEZOELECTRICAL).ToList();
                                            var v3 = gpItem.FloorDrainsCountAt1F == PIEZOELECTRICAL ? v1 : v2;
                                            var p1 = segs[BATHYDRACONIDAE].EndPoint;
                                            DrawNoteText(v3, p1.OffsetXY(-CONSTRUCTIONISM - THESAURUSHIGHLY, -PSYCHOPHYSIOLOGICAL));
                                            drawDomePipes(new List<Vector2d> { new Vector2d(-THESAURUSDOWNHEARTED, -DENOMINATIONALIZE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-INTELLIGIBILITY, BATHYDRACONIDAE) }.ToGLineSegments(p).Skip(PIEZOELECTRICAL), THESAURUSAMENITY);
                                        }
                                        else
                                        {
                                            var p2 = p + v;
                                            var vecs = new List<Vector2d> { new Vector2d(-QUOTATIONHODDEN, -THESAURUSSENSATION), new Vector2d(THESAURUSEULOGY, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, THESAURUSCONFOUND) };
                                            var segs = vecs.ToGLineSegments(p2).Skip(PIEZOELECTRICAL).ToList();
                                            var p1 = segs[BATHYDRACONIDAE].StartPoint;
                                            DrawNoteText(v1, p1.OffsetXY(QUOTATIONPATRONAL, -PSYCHOPHYSIOLOGICAL));
                                            drawDomePipes(new List<Vector2d> { new Vector2d(-THESAURUSDOWNHEARTED, -DENOMINATIONALIZE), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSEULOGY, BATHYDRACONIDAE) }.ToGLineSegments(p).Skip(PIEZOELECTRICAL), THESAURUSAMENITY);
                                        }
                                    }
                                    DrawOutlets1(THESAURUSAMENITY, info.EndPoint, QUOTATIONLUNGEING, output, dy: -HEIGHT * THESAURUSEXPENDABLE, fixv: new Vector2d(BATHYDRACONIDAE, -THESAURUSTRAGEDY));
                                }
                                else if (gpItem.HasBasinInKitchenAt1F)
                                {
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1;
                                    output.DN2 = THESAURUSSPITEFUL;
                                    output.DN1 = getOtherFloorDrainDN();
                                    void DrawOutlets4(string shadow, Point2d basePoint, double HEIGHT)
                                    {
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                        {
                                            Dr.DrawSimpleLabel(basePoint.OffsetY(-QUOTATIONPATRONAL), THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                        }
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, -HETEROTRANSPLANTED), new Vector2d(-THESAURUSSETBACK, -THESAURUSSETBACK), new Vector2d(-THESAURUSORIGINATE - PSYCHOPHYSIOLOGICAL, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, THESAURUSSUCCINCT), new Vector2d(PSYCHOGENICALLY, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, THESAURUSSUPPLEMENTARY) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(THESAURUSINTELLECT);
                                        DrawDiryWaterWells1(segs[TEREBINTHINATED].EndPoint + new Vector2d(-PSYCHOPHYSIOLOGICAL, DIETHYLSTILBOESTROL), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[THESAURUSINTELLECT].StartPoint.OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[TEREBINTHINATED].EndPoint.OffsetX(DIETHYLSTILBOESTROL), THESAURUSAMENITY);
                                        if (output.HasWrappingPipe2)
                                        {
                                            if (gpItem.OutletWrappingPipeRadius != null)
                                            {
                                                static void DrawLine(string layer, params GLineSegment[] segs)
                                                {
                                                    var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
                                                    foreach (var line in lines)
                                                    {
                                                        line.Layer = layer;
                                                        ByLayer(line);
                                                    }
                                                }
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = MICROSPECTROPHOTOMETRY)
                                                {
                                                    DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, label } }, cb: br => { ByLayer(br); });
                                                }
                                                var p10 = segs[TEREBINTHINATED].EndPoint.OffsetX(PSYCHOPHYSIOLOGICAL);
                                                var p20 = p10.OffsetY(-QUOTATIONCHOROID);
                                                var p30 = p20.OffsetX(THESAURUSBEFRIEND);
                                                var layer = THESAURUSSMASHING;
                                                DrawLine(layer, new GLineSegment(p10, p20));
                                                DrawLine(layer, new GLineSegment(p30, p20));
                                                DrawStoreyHeightSymbol(p30, THESAURUSSMASHING, gpItem.OutletWrappingPipeRadius);
                                                {
                                                    var _shadow = THESAURUSAMENITY;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > PIEZOELECTRICAL)
                                                    {
                                                        Dr.DrawSimpleLabel(p30, THESAURUSBENEFIT + _shadow.Substring(PIEZOELECTRICAL));
                                                    }
                                                }
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[THESAURUSINTELLECT].StartPoint.OffsetXY(THESAURUSAPPORTION, THESAURUSFORTIFICATION));
                                        DrawNoteText(output.DN2, segs[TEREBINTHINATED].EndPoint.OffsetXY(THESAURUSAPPORTION, THESAURUSFORTIFICATION));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSEVINCE].StartPoint.ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[TEREBINTHINATED].StartPoint.ToPoint3d(), THESAURUSESPECIALLY, PIEZOELECTRICAL);
                                        var p = segs[DOLICHOCEPHALOUS].EndPoint;
                                        var fixY = DIETHYLSTILBOESTROL + HEIGHT / DOLICHOCEPHALOUS;
                                        var p1 = p.OffsetX(-THESAURUSSETBACK) + new Vector2d(-TRISYLLABICALLY + PSYCHOPHYSIOLOGICAL, fixY);
                                        DrawDSCurve(p1, THESAURUSNEGATIVE, getDSCurveValue(), THESAURUSAMENITY);
                                        var p2 = p1.OffsetY(-fixY);
                                        segs.Add(new GLineSegment(p1, p2));
                                        if (getDSCurveValue() == INSTITUTIONALIZED)
                                        {
                                            var p5 = segs[THESAURUSINTELLECT].StartPoint;
                                            var _segs = new List<Vector2d> { new Vector2d(THESAURUSESTEEM, BATHYDRACONIDAE), new Vector2d(THESAURUSSETBACK, THESAURUSSETBACK), new Vector2d(BATHYDRACONIDAE, THESAURUSDAMAGE), new Vector2d(-THESAURUSBEFRIEND, BATHYDRACONIDAE) }.ToGLineSegments(p5);
                                            segs = segs.Take(THESAURUSINTELLECT).ToList();
                                            segs.AddRange(_segs);
                                        }
                                        drawDomePipes(segs, THESAURUSAMENITY);
                                    }
                                    DrawOutlets4(THESAURUSAMENITY, info.EndPoint, HEIGHT);
                                }
                                else
                                {
                                    DrawOutlets1(THESAURUSAMENITY, info.EndPoint, QUOTATIONLUNGEING, output, dy: -THESAURUSCONFECTIONERY * THESAURUSEXPENDABLE);
                                }
                            }
                        }
                    }
                    {
                        var linesKillers = new HashSet<Geometry>();
                        if (gpItem.IsFL0)
                        {
                            for (int i = gpItem.Items.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; --i)
                            {
                                if (gpItem.Items[i].Exist)
                                {
                                    var info = infos[i];
                                    DrawAiringSymbol(info.StartPoint, getCouldHavePeopleOnRoof(), NEUROTRANSMITTER);
                                    break;
                                }
                            }
                        }
                        dome_lines = GeoFac.ToNodedLineSegments(dome_lines);
                        var geos = dome_lines.Select(x => x.ToLineString()).ToList();
                        dome_lines = geos.Except(GeoFac.CreateIntersectsSelector(geos)(GeoFac.CreateGeometryEx(linesKillers.ToList()))).Cast<LineString>().SelectMany(x => x.ToGLineSegments()).ToList();
                    }
                    {
                        if (gpItem.HasTl)
                        {
                            var lines = new HashSet<GLineSegment>(vent_lines);
                            for (int i = BATHYDRACONIDAE; i < gpItem.Hangings.Count; i++)
                            {
                                var hanging = gpItem.Hangings[i];
                                if (allStoreys[i] == gpItem.MaxTl + PHENYLENEDIAMINE)
                                {
                                    var info = infos[i];
                                    if (info != null)
                                    {
                                        foreach (var seg in info.RightSegsFirst)
                                        {
                                            lines.Remove(seg);
                                        }
                                        var k = HEIGHT / THESAURUSRECAPITULATE;
                                        var vecs = new List<Vector2d> { new Vector2d(BATHYDRACONIDAE, (DNIPRODZERZHINSK + PSYCHOPHYSIOLOGICAL) * k), new Vector2d(DIETHYLSTILBOESTROL, (-METHYLCARBAMATE) * k), new Vector2d(BATHYDRACONIDAE, (-THESAURUSCUDDLE - PSYCHOPHYSIOLOGICAL) * k) };
                                        var segs = vecs.ToGLineSegments(info.EndPoint).Skip(PIEZOELECTRICAL).ToList();
                                        lines.AddRange(segs);
                                        var shadow = THESAURUSAMENITY;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > PIEZOELECTRICAL)
                                        {
                                            Dr.DrawSimpleLabel(segs.First().StartPoint, THESAURUSBENEFIT + shadow.Substring(PIEZOELECTRICAL));
                                        }
                                    }
                                    break;
                                }
                            }
                            vent_lines = lines.ToList();
                        }
                    }
                    {
                        var auto_conn = THESAURUSESPECIALLY;
                        var layer = gpItem.Labels.Any(IsFL0) ? NEUROTRANSMITTER : dome_layer;
                        if (auto_conn)
                        {
                            foreach (var g in GeoFac.GroupParallelLines(dome_lines, PIEZOELECTRICAL, THESAURUSPROPRIETOR))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: THESAURUSDISRUPTION));
                                line.Layer = layer;
                                ByLayer(line);
                            }
                            foreach (var g in GeoFac.GroupParallelLines(vent_lines, PIEZOELECTRICAL, THESAURUSPROPRIETOR))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: THESAURUSDISRUPTION));
                                line.Layer = vent_layer;
                                ByLayer(line);
                            }
                        }
                        else
                        {
                            foreach (var dome_line in dome_lines)
                            {
                                var line = DrawLineSegmentLazy(dome_line);
                                line.Layer = layer;
                                ByLayer(line);
                            }
                            foreach (var _line in vent_lines)
                            {
                                var line = DrawLineSegmentLazy(_line);
                                line.Layer = vent_layer;
                                ByLayer(line);
                            }
                        }
                    }
                }
            }
        }
        public static void DrawDrainageSystemDiagram(Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, int __dy, DrainageSystemDiagramViewModel viewModel)
        {
            var o = new Opt()
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
                __dy = __dy,
                viewModel = viewModel,
            };
            o.Draw();
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof, string layer = THESAURUSOBJECTIVELY)
        {
            DrawAiringSymbol(pt, canPeopleBeOnRoof ? THESAURUSACQUIRE : THESAURUSASTUTE, layer);
        }
        public static void DrawAiringSymbol(Point2d pt, string name, string layer)
        {
            DrawBlockReference(blkName: THESAURUSEXPUNGE, basePt: pt.ToPoint3d(), layer: layer, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(THESAURUSGENTILITY, name);
            });
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: THESAURUSCENSOR, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: THESAURUSOBJECTIVELY, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(PALAEOGEOMORPHOLOGY, offsetY);
                br.ObjectId.SetDynBlockValue(THESAURUSGENTILITY, THESAURUSREPREHENSIBLE);
            });
        }
        public static CommandContext commandContext;
        public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
        {
            var items = pipeIds.Select(id => LabelItem.Parse(id)).Where(m => m != null);
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2).ToList());
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
        }
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
        public static bool CreateDrainageDrawingData(out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            if (noWL && geoData.Labels.Any(x => IsWL(x.Text)))
            {
                MessageBox.Show(ARCHIPRESBYTERATUS);
                drDatas = null;
                return THESAURUSESPECIALLY;
            }
            drDatas = _CreateDrainageDrawingData(geoData, THESAURUSNEGATIVE);
            return THESAURUSNEGATIVE;
        }
        public static List<DrainageDrawingData> CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
        {
            ThDrainageService.PreFixGeoData(geoData);
            return _CreateDrainageDrawingData(geoData, noDraw);
        }
        private static List<DrainageDrawingData> _CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
        {
            List<DrainageDrawingData> drDatas;
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            DrainageService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out drDatas);
            if (noDraw) Dispose();
            return drDatas;
        }
        public static bool CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = THESAURUSESPECIALLY)
        {
            CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData);
            return CreateDrainageDrawingData(out drDatas, noWL, geoData);
        }
        public static bool CollectDrainageData(AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, CommandContext ctx, bool noWL = THESAURUSESPECIALLY)
        {
            CollectDrainageGeoData(adb, out storeysItems, out DrainageGeoData geoData, ctx);
            return CreateDrainageDrawingData(out drDatas, noWL, geoData);
        }
        public static List<StoreyItem> GetStoreysItem(List<StoreyInfo> storeys)
        {
            var storeysItems = new List<StoreyItem>();
            foreach (var s in storeys)
            {
                var item = new StoreyItem();
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
        public static List<StoreyInfo> GetStoreys(AcadDatabase adb, CommandContext ctx)
        {
            return ctx.StoreyContext.StoreyInfos;
        }
        public static void CollectDrainageGeoData(AcadDatabase adb, out List<StoreyItem> storeysItems, out DrainageGeoData geoData, CommandContext ctx)
        {
            var storeys = GetStoreys(adb, ctx);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(adb, geoData, ctx);
            geoData.Flush();
        }
        public static List<StoreyInfo> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var geo = range?.ToGRect().ToPolygon();
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSNEGATIVE).ToList();
            FixStoreys(storeys);
            return storeys;
        }
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
        public static void CollectDrainageGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out DrainageGeoData geoData)
        {
            var storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(range, adb, geoData);
            geoData.Flush();
        }
        public static void DrawDrainageSystemDiagram(DrainageSystemDiagramViewModel viewModel, bool focus)
        {
            if (focus) FocusMainWindow();
            if (commandContext == null) return;
            if (commandContext.StoreyContext == null) return;
            if (commandContext.StoreyContext.StoreyInfos == null) return;
            if (!TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, THESAURUSNEGATIVE))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { THESAURUSGOODNESS, THESAURUSOBJECTIVELY, THESAURUSSMASHING, THESAURUSRECRIMINATION, THESAURUSEXCOMMUNICATE, THESAURUSSPELLBOUND, THESAURUSDECISIVE });
                var storeys = commandContext.StoreyContext.StoreyInfos;
                List<StoreyItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                var range = commandContext.range;
                if (range != null)
                {
                    if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSNEGATIVE)) return;
                }
                else
                {
                    if (!CollectDrainageData(adb, out storeysItems, out drDatas, commandContext, noWL: THESAURUSNEGATIVE)) return;
                }
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, DrainageSystemDiagram.commandContext?.ViewModel, out List<int> allNumStoreys, out List<string> allRfStoreys);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + PHENYLENEDIAMINE).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - PIEZOELECTRICAL;
                var end = BATHYDRACONIDAE;
                var OFFSET_X = HYPERCHOLESTERO;
                var SPAN_X = QUOTATIONCOLLARED + THESAURUSBEFRIEND + PSYCHOGERIATRIC;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSRECAPITULATE;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSRECAPITULATE;
                var __dy = DIETHYLSTILBOESTROL;
                Dispose();
                DrawDrainageSystemDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, viewModel);
                FlushDQ(adb);
            }
        }
        public static void DrawDrainageSystemDiagram()
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
                List<StoreyItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSNEGATIVE)) return;
                var vm = DrainageSystemDiagram.commandContext?.ViewModel;
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, vm, out List<int> allNumStoreys, out List<string> allRfStoreys);
                Dispose();
                DrawDrainageSystemDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys, vm);
                FlushDQ(adb);
            }
        }
        static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
        }
        public static List<DrainageGroupedPipeItem> GetDrainageGroupedPipeItems(List<DrainageDrawingData> drDatas, List<StoreyItem> storeysItems, DrainageSystemDiagramViewModel vm, out List<int> allNumStoreys, out List<string> allRfStoreys)
        {
            var _storeys = new List<string>();
            foreach (var item in storeysItems)
            {
                item.Init();
                _storeys.AddRange(item.Labels);
            }
            _storeys = _storeys.Distinct().OrderBy(GetStoreyScore).ToList();
            var minS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > BATHYDRACONIDAE).Min();
            var maxS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > BATHYDRACONIDAE).Max();
            var countS = maxS - minS + PIEZOELECTRICAL;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            allRfStoreys = _storeys.Where(x => !IsNumStorey(x)).ToList();
            var allNumStoreyLabels = allNumStoreys.Select(x => x + PHENYLENEDIAMINE).ToList();
            bool getCanHaveDownboard()
            {
                return vm?.Params?.CanHaveDownboard ?? THESAURUSNEGATIVE;
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
            string getWaterPortLabel(string label)
            {
                foreach (var drData in drDatas)
                {
                    if (drData.Outlets.TryGetValue(label, out string value))
                    {
                        return value;
                    }
                }
                return CONTEMPTIBILITY;
            }
            bool hasWaterPort(string label)
            {
                return getWaterPortLabel(label) != null;
            }
            int getMinTl()
            {
                var scores = new List<int>();
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        var drData = drDatas[i];
                        var score = GetStoreyScore(s);
                        if (score < ushort.MaxValue)
                        {
                            if (drData.VerticalPipeLabels.Any(IsTL)) scores.Add(score);
                        }
                    }
                }
                if (scores.Count == BATHYDRACONIDAE) return BATHYDRACONIDAE;
                var ret = scores.Min() - PIEZOELECTRICAL;
                if (ret <= BATHYDRACONIDAE) return PIEZOELECTRICAL;
                return ret;
            }
            int getMaxTl()
            {
                var scores = new List<int>();
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        var drData = drDatas[i];
                        var score = GetStoreyScore(s);
                        if (score < ushort.MaxValue)
                        {
                            if (drData.VerticalPipeLabels.Any(IsTL)) scores.Add(score);
                        }
                    }
                }
                return scores.Count == BATHYDRACONIDAE ? BATHYDRACONIDAE : scores.Max();
            }
            bool is4Tune(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData._4tunes.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool getIsShunt(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.Shunts.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            int getSingleOutletFDCount(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            drData.SingleOutletFloorDrains.TryGetValue(label, out int v);
                            return v;
                        }
                    }
                }
                return BATHYDRACONIDAE;
            }
            int getFDCount(string label, string storey)
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
            int getCirclesCount(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.circlesCount.TryGetValue(label, out int v))
                            {
                                return v;
                            }
                        }
                    }
                }
                return BATHYDRACONIDAE;
            }
            bool isKitchen(string label, string storey)
            {
                if (IsFL0(label)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.KitchenFls.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool isBalcony(string label, string storey)
            {
                if (IsFL0(label)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.BalconyFls.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool getIsConnectedToFloorDrainForFL0(string label)
            {
                if (!IsFL0(label)) return THESAURUSESPECIALLY;
                bool f(string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.IsConnectedToFloorDrainForFL0.Contains(label);
                            }
                        }
                    }
                    return THESAURUSESPECIALLY;
                }
                return f(THESAURUSDEFILE) || f(THESAURUSSUSTAIN);
            }
            bool getHasRainPort(string label)
            {
                if (!IsFL0(label)) return THESAURUSESPECIALLY;
                bool f(string storey)
                {
                    for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.HasRainPortSymbolsForFL0.Contains(label);
                            }
                        }
                    }
                    return THESAURUSESPECIALLY;
                }
                return f(THESAURUSDEFILE) || f(THESAURUSSUSTAIN);
            }
            bool isToilet(string label, string storey)
            {
                if (IsFL0(label)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            return drData.ToiletPls.Contains(label);
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            int getWashingMachineFloorDrainsCount(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            drData.WashingMachineFloorDrains.TryGetValue(label, out int v);
                            return v;
                        }
                    }
                }
                return BATHYDRACONIDAE;
            }
            bool IsSeries(string label, string storey)
            {
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.Shunts.Contains(label))
                            {
                                return THESAURUSESPECIALLY;
                            }
                        }
                    }
                }
                return THESAURUSNEGATIVE;
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
            int getFloorDrainsCountAt1F(string label)
            {
                if (!IsFL(label)) return BATHYDRACONIDAE;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            drData.FloorDrains.TryGetValue(label, out int r);
                            return r;
                        }
                    }
                }
                return BATHYDRACONIDAE;
            }
            bool getIsMerge(string label)
            {
                if (!IsFL0(label)) return THESAURUSESPECIALLY;
                for (int i = BATHYDRACONIDAE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSDEFILE)
                        {
                            var drData = drDatas[i];
                            if (drData.Merges.Contains(label))
                            {
                                return THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
                return THESAURUSESPECIALLY;
            }
            bool HasKitchenWashingMachine(string label, string storey)
            {
                return THESAURUSESPECIALLY;
            }
            var pipeInfoDict = new Dictionary<string, DrainageGroupingPipeItem>();
            var alllabels = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels));
            var allFls = alllabels.Where(x => IsFL(x)).ToList();
            var allPls = alllabels.Where(x => IsPL(x)).ToList();
            var FlGroupingItems = new List<DrainageGroupingPipeItem>();
            var PlGroupingItems = new List<DrainageGroupingPipeItem>();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).OrderBy(GetStoreyScore).ToList();
            foreach (var fl in allFls)
            {
                var item = new DrainageGroupingPipeItem()
                {
                    Label = fl,
                    PipeType = PipeType.FL,
                };
                item.HasWaterPort = hasWaterPort(fl);
                item.WaterPortLabel = getWaterPortLabel(fl);
                item.HasWrappingPipe = hasOutletlWrappingPipe(fl);
                item.FloorDrainsCountAt1F = getFloorDrainsCountAt1F(fl);
                item.OutletWrappingPipeRadius = getOutletWrappingPipeRadius(fl);
                item.Items = new List<DrainageGroupingPipeItem.ValueItem>();
                item.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
                foreach (var storey in allNumStoreyLabels)
                {
                    var _hasLong = hasLong(fl, storey);
                    item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(fl, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(fl, storey),
                    });
                    item.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
                    {
                        Is4Tune = is4Tune(fl, storey),
                        FloorDrainsCount = getFDCount(fl, storey),
                        WashingMachineFloorDrainsCount = getWashingMachineFloorDrainsCount(fl, storey),
                        Storey = storey,
                        IsSeries = !getIsShunt(fl, storey),
                    });
                }
                FlGroupingItems.Add(item);
                pipeInfoDict[fl] = item;
            }
            foreach (var pl in allPls)
            {
                var item = new DrainageGroupingPipeItem()
                {
                    Label = pl,
                    PipeType = PipeType.PL,
                };
                item.HasWaterPort = hasWaterPort(pl);
                item.WaterPortLabel = getWaterPortLabel(pl);
                item.HasWrappingPipe = hasOutletlWrappingPipe(pl);
                item.OutletWrappingPipeRadius = getOutletWrappingPipeRadius(pl);
                {
                    item.MinTl = getMinTl();
                    item.MaxTl = getMaxTl();
                    item.HasTL = THESAURUSNEGATIVE;
                    if (item.MinTl <= BATHYDRACONIDAE || item.MaxTl <= PIEZOELECTRICAL || item.MinTl >= item.MaxTl)
                    {
                        item.HasTL = THESAURUSESPECIALLY;
                        item.MinTl = item.MaxTl = BATHYDRACONIDAE;
                    }
                    if (item.HasTL && item.MaxTl == maxS)
                    {
                        item.MoveTlLineUpper = THESAURUSNEGATIVE;
                    }
                    item.TlLabel = pl.Replace(THESAURUSHARVEST, ETHELOTHRĒSKEIA);
                }
                item.Items = new List<DrainageGroupingPipeItem.ValueItem>();
                item.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
                foreach (var storey in allNumStoreyLabels)
                {
                    var _hasLong = hasLong(pl, storey);
                    item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                    {
                        Exist = testExist(pl, storey),
                        HasLong = _hasLong,
                        HasShort = hasShort(pl, storey),
                    });
                    item.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
                    {
                        FloorDrainsCount = getFDCount(pl, storey),
                        Storey = storey,
                    });
                }
                PlGroupingItems.Add(item);
                pipeInfoDict[pl] = item;
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                item.OutletWrappingPipeRadius ??= THESAURUSEXULTATION;
            }
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
                item.FloorDrainsCountAt1F = Math.Max(item.FloorDrainsCountAt1F, getSingleOutletFDCount(label, THESAURUSDEFILE));
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                foreach (var hanging in item.Hangings)
                {
                    if (hanging.FloorDrainsCount > TEREBINTHINATED)
                    {
                        hanging.FloorDrainsCount = TEREBINTHINATED;
                    }
                    if (isKitchen(label, hanging.Storey))
                    {
                        if (hanging.FloorDrainsCount == BATHYDRACONIDAE)
                        {
                            hanging.HasDoubleSCurve = THESAURUSNEGATIVE;
                        }
                    }
                    if (isKitchen(label, hanging.Storey))
                    {
                        hanging.RoomName = THESAURUSNESTLE;
                    }
                    else if (isBalcony(label, hanging.Storey))
                    {
                        hanging.RoomName = THESAURUSOUTCRY;
                    }
                    if (hanging.WashingMachineFloorDrainsCount > hanging.FloorDrainsCount)
                    {
                        hanging.WashingMachineFloorDrainsCount = hanging.FloorDrainsCount;
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                if (!IsFL(label)) continue;
                if (IsFL0(label)) continue;
                var item = kv.Value;
                {
                    foreach (var hanging in item.Hangings)
                    {
                        if (isKitchen(label, hanging.Storey))
                        {
                            hanging.HasDoubleSCurve = THESAURUSNEGATIVE;
                        }
                        if (hanging.Storey == THESAURUSDEFILE)
                        {
                            if (isKitchen(label, hanging.Storey))
                            {
                                hanging.HasDoubleSCurve = THESAURUSESPECIALLY;
                                item.HasBasinInKitchenAt1F = THESAURUSNEGATIVE;
                            }
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    if (IsPL(kv.Key))
                    {
                        var item = kv.Value;
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = BATHYDRACONIDAE;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    if (!IsFL(kv.Key)) continue;
                    var item = kv.Value;
                    if (IsFL0(item.Label))
                    {
                        item.IsFL0 = THESAURUSNEGATIVE;
                        item.HasRainPortForFL0 = getHasRainPort(item.Label);
                        item.IsConnectedToFloorDrainForFL0 = getIsConnectedToFloorDrainForFL0(item.Label);
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = PIEZOELECTRICAL;
                            hanging.HasSCurve = THESAURUSESPECIALLY;
                            hanging.HasDoubleSCurve = THESAURUSESPECIALLY;
                            hanging.HasCleaningPort = THESAURUSESPECIALLY;
                            if (hanging.Storey == THESAURUSDEFILE)
                            {
                                hanging.FloorDrainsCount = getSingleOutletFDCount(kv.Key, THESAURUSDEFILE);
                            }
                        }
                        if (item.IsConnectedToFloorDrainForFL0) item.MergeFloorDrainForFL0 = getIsMerge(kv.Key);
                    }
                }
            }
            {
                foreach (var item in pipeInfoDict.Values)
                {
                    for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                    {
                        if (!item.Items[i].Exist) continue;
                        var hanging = item.Hangings[i];
                        var storey = allNumStoreyLabels[i];
                        hanging.HasCleaningPort = IsPL(item.Label) || IsDL(item.Label);
                        hanging.HasDownBoardLine = IsPL(item.Label) || IsDL(item.Label);
                        {
                            var m = item.Items.TryGet(i - PIEZOELECTRICAL);
                            if ((m.Exist && m.HasLong) || storey == THESAURUSDEFILE)
                            {
                                hanging.HasCheckPoint = THESAURUSNEGATIVE;
                            }
                        }
                        if (hanging.HasCleaningPort)
                        {
                            hanging.HasCheckPoint = THESAURUSNEGATIVE;
                        }
                        if (hanging.HasDoubleSCurve)
                        {
                            hanging.HasCheckPoint = THESAURUSNEGATIVE;
                        }
                        if (hanging.WashingMachineFloorDrainsCount > BATHYDRACONIDAE)
                        {
                            hanging.HasCheckPoint = THESAURUSNEGATIVE;
                        }
                        if (GetStoreyScore(storey) == maxS)
                        {
                            hanging.HasCleaningPort = THESAURUSESPECIALLY;
                            hanging.HasDownBoardLine = THESAURUSESPECIALLY;
                        }
                    }
                }
            }
            {
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (allRfStoreys.Any(storey => testExist(label, storey)))
                    {
                        item.CanHaveAring = THESAURUSNEGATIVE;
                    }
                    if (testExist(label, maxS + PHENYLENEDIAMINE))
                    {
                        item.CanHaveAring = THESAURUSNEGATIVE;
                    }
                    if (IsFL0(item.Label))
                    {
                        item.CanHaveAring = THESAURUSESPECIALLY;
                    }
                }
            }
            {
                if (allNumStoreys.Max() < PERCHLOROETHYLENE)
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        item.HasTL = THESAURUSESPECIALLY;
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (IsPL(label))
                {
                    foreach (var hanging in item.Hangings)
                    {
                        if (hanging.Storey == THESAURUSDEFILE)
                        {
                            if (isToilet(label, THESAURUSDEFILE))
                            {
                                item.IsSingleOutlet = THESAURUSNEGATIVE;
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
                if (IsFL0(label))
                {
                    for (int i = item.Items.Count - PIEZOELECTRICAL; i >= BATHYDRACONIDAE; --i)
                    {
                        if (item.Items[i].Exist)
                        {
                            item.Items[i] = default;
                            break;
                        }
                    }
                }
                for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                {
                    var hanging = item.Hangings[i];
                    if (hanging == null) continue;
                    if (hanging.Storey == maxS + PHENYLENEDIAMINE)
                    {
                        if (item.Items[i].HasShort)
                        {
                            var m = item.Items[i];
                            m.HasShort = THESAURUSESPECIALLY;
                            m.HasLong = THESAURUSNEGATIVE;
                            m.DrawLongHLineHigher = THESAURUSNEGATIVE;
                            item.Items[i] = m;
                            hanging.HasDownBoardLine = THESAURUSESPECIALLY;
                        }
                        break;
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (IsPL(label))
                {
                    for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                    {
                        var h1 = item.Hangings[i];
                        h1.HasCleaningPort = isToilet(label, h1.Storey);
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (IsFL(label) && !IsFL0(label))
                {
                    for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                    {
                        var h1 = item.Hangings[i];
                        var h2 = item.Hangings.TryGet(i + PIEZOELECTRICAL);
                        if (item.Items[i].HasLong && item.Items.TryGet(i + PIEZOELECTRICAL).Exist && h2 != null)
                        {
                            h1.FlFixType = FixingLogic1.GetFlFixType(label, h1.Storey, isKitchen(label, h2.Storey), getCirclesCount(label, h1.Storey));
                            h2.FlCaseEnum = FixingLogic1.GetFlCase(label, h1.Storey, isKitchen(label, h2.Storey), getCirclesCount(label, h1.Storey));
                        }
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                for (int i = BATHYDRACONIDAE; i < item.Hangings.Count; i++)
                {
                    var h1 = item.Hangings[i];
                    var h2 = item.Hangings.TryGet(i + PIEZOELECTRICAL);
                    if (h2 == null) continue;
                    if (!h2.HasCleaningPort)
                    {
                        h1.HasDownBoardLine = THESAURUSESPECIALLY;
                    }
                }
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                if (!getCanHaveDownboard())
                {
                    foreach (var h in item.Hangings)
                    {
                        h.HasDownBoardLine = THESAURUSESPECIALLY;
                    }
                }
            }
            var pipeGroupedItems = new List<DrainageGroupedPipeItem>();
            var gs = pipeInfoDict.Values.GroupBy(x => x).ToList();
            foreach (var g in gs)
            {
                if (!IsFL(g.Key.Label)) continue;
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
                    HasBasinInKitchenAt1F = g.Key.HasBasinInKitchenAt1F,
                    FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                    CanHaveAring = g.Key.CanHaveAring,
                    IsFL0 = g.Key.IsFL0,
                    HasRainPortForFL0 = g.Key.HasRainPortForFL0,
                    IsConnectedToFloorDrainForFL0 = g.Key.IsConnectedToFloorDrainForFL0,
                    MergeFloorDrainForFL0 = g.Key.MergeFloorDrainForFL0,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                };
                pipeGroupedItems.Add(item);
            }
            foreach (var g in gs)
            {
                if (!IsPL(g.Key.Label)) continue;
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
                    IsSingleOutlet = g.Key.IsSingleOutlet,
                    CanHaveAring = g.Key.CanHaveAring,
                    IsFL0 = g.Key.IsFL0,
                    MoveTlLineUpper = g.Key.MoveTlLineUpper,
                    OutletWrappingPipeRadius = g.Key.OutletWrappingPipeRadius,
                };
                pipeGroupedItems.Add(item);
            }
            pipeGroupedItems = pipeGroupedItems.OrderBy(x =>
            {
                var label = x.Labels.FirstOrDefault();
                if (label is null) return BATHYDRACONIDAE;
                if (IsPL((label))) return PIEZOELECTRICAL;
                if (IsFL0((label))) return TEREBINTHINATED;
                if (IsFL((label))) return THESAURUSINTELLECT;
                return int.MaxValue;
            }).ThenBy(x =>
            {
                return x.Labels.FirstOrDefault();
            }).ToList();
            return pipeGroupedItems;
        }
        public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * THESAURUSADVANCEMENT;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSTRAGEDY;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-UNCONJECTURABLE);
            var p3 = p1.OffsetX(UNCONJECTURABLE);
            var line = DrawLineLazy(p2, p3);
            line.Layer = THESAURUSSMASHING;
            ByLayer(line);
        }
        public static void DrawPipeButtomHeightSymbol(Point2d p, List<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: p.ToPoint3d(),
      props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, THESAURUSOBSESS } },
      cb: br =>
      {
          br.Layer = THESAURUSSMASHING;
      });
        }
        public static void DrawPipeButtomHeightSymbol(double w, double h, Point2d p)
        {
            var vecs = new List<Vector2d> { new Vector2d(w, BATHYDRACONIDAE), new Vector2d(BATHYDRACONIDAE, h) };
            var segs = vecs.ToGLineSegments(p);
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: THESAURUSINCIDENTAL, basePt: segs.Last().EndPoint.OffsetX(HYPERSENSITIZED).ToPoint3d(),
      props: new Dictionary<string, string>() { { THESAURUSINCIDENTAL, THESAURUSOBSESS } },
      cb: br =>
      {
          br.Layer = THESAURUSSMASHING;
      });
        }
        public static void DrawStoreyLine(string label, Point2d basePt, double lineLen)
        {
            DrawStoreyLine(label, basePt.ToPoint3d(), lineLen);
        }
        public static void DrawStoreyLine(string label, Point3d basePt, double lineLen)
        {
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
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
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
        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = THESAURUSSMASHING;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = THESAURUSADVANCEMENT;
                    SetTextStyleLazy(t, THESAURUSEXTEMPORE);
                }
            }
        }
        public static void DrawDomePipes(params GLineSegment[] segs)
        {
            DrawDomePipes((IEnumerable<GLineSegment>)segs);
        }
        public static void DrawDomePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line => SetDomePipeLineStyle(line));
        }
        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = THESAURUSOBJECTIVELY;
            ByLayer(line);
        }
        public static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = NEUROTRANSMITTER;
                ByLayer(line);
            });
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
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, REVOLUTIONIZATION, pt);
            SetLabelStylesForDraiNote(t);
        }
        public static void DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
        {
            var p2 = p1 + vec7;
            DrawDomePipes(new GLineSegment(p1, p2));
            if (!Testing) DrawSWaterStoringCurve(p2.ToPoint3d(), leftOrRight);
        }
        public static void DrawDSCurve(Point2d p2, bool leftOrRight, string value, string shadow)
        {
            if (!Testing) DrawDoubleWashBasins(p2.ToPoint3d(), leftOrRight, value);
        }
        public static bool Testing;
        public static void DrawDoubleWashBasins(Point3d basePt, bool leftOrRight, string value)
        {
            if (leftOrRight)
            {
                if (value is INSTITUTIONALIZED)
                {
                    basePt += new Vector2d(THESAURUSDOWNHEARTED, -PSYCHOPHYSIOLOGICAL).ToVector3d();
                }
                DrawBlockReference(QUOTATIONCRIMEAN, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSRECRIMINATION;
                      br.ScaleFactors = new Scale3d(TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, value);
                      }
                  });
            }
            else
            {
                DrawBlockReference(QUOTATIONCRIMEAN, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSRECRIMINATION;
                      br.ScaleFactors = new Scale3d(-TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, value);
                      }
                  });
            }
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = THESAURUSMELODIOUS)
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSCOMMUTE, basePt, br =>
                {
                    br.Layer = THESAURUSRECRIMINATION;
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
                   br.Layer = THESAURUSRECRIMINATION;
                   ByLayer(br);
                   br.ScaleFactors = new Scale3d(-TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, value);
                   }
               });
            }
        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSDISCRETE, basePt, br =>
                {
                    br.Layer = THESAURUSRECRIMINATION;
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, THESAURUSCLIENTELE);
                        br.ObjectId.SetDynBlockValue(PHOTOMECHANICAL, (short)PIEZOELECTRICAL);
                    }
                });
            }
            else
            {
                DrawBlockReference(THESAURUSDISCRETE, basePt,
                   br =>
                   {
                       br.Layer = THESAURUSRECRIMINATION;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-TEREBINTHINATED, TEREBINTHINATED, TEREBINTHINATED);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(THESAURUSCOMPREHEND, THESAURUSCLIENTELE);
                           br.ObjectId.SetDynBlockValue(PHOTOMECHANICAL, (short)PIEZOELECTRICAL);
                       }
                   });
            }
        }
        public static AlignedDimension DrawDimLabel(Point2d pt1, Point2d pt2, Vector2d v, string text, string layer)
        {
            var dim = new AlignedDimension();
            dim.XLine1Point = pt1.ToPoint3d();
            dim.XLine2Point = pt2.ToPoint3d();
            dim.DimLinePoint = (GeoAlgorithm.MidPoint(pt1, pt2) + v).ToPoint3d();
            dim.DimensionText = text;
            dim.Layer = layer;
            ByLayer(dim);
            DrawEntityLazy(dim);
            return dim;
        }
        public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
        {
            if (leftOrRight)
            {
                DrawBlockReference(MAGNANIMOUSNESS, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSRECRIMINATION;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(QUOTATIONSORCERER);
                });
            }
            else
            {
                DrawBlockReference(MAGNANIMOUSNESS, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSRECRIMINATION;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(QUOTATIONSORCERER + THESAURUSDOWNHEARTED);
                });
            }
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
        public static void DrawDiryWaterWells2(Point2d pt, List<string> values)
        {
            var dx = BATHYDRACONIDAE;
            foreach (var value in values)
            {
                DrawDirtyWaterWell(pt.OffsetX(PSYCHOPHYSIOLOGICAL) + new Vector2d(dx, BATHYDRACONIDAE), value);
                dx += CHLOROFLUOROCARBONS;
            }
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: THESAURUSASSIGN, basePt: basePt.OffsetY(-PSYCHOPHYSIOLOGICAL),
          props: new Dictionary<string, string>() { { CONTEMPTIBILITY, value } },
          cb: br =>
          {
              br.Layer = THESAURUSINSPECTOR;
              ByLayer(br);
          });
        }
        public static void DrawRainWaterWell(Point2d basePt, string value)
        {
            DrawRainWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawDirtyWaterWell(Point2d basePt, string value)
        {
            DrawDirtyWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawDirtyWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: QUOTATIONADJACENT, basePt: basePt.OffsetY(-PSYCHOPHYSIOLOGICAL),
            props: new Dictionary<string, string>() { { CONTEMPTIBILITY, value } },
            cb: br =>
            {
                br.Layer = THESAURUSRECRIMINATION;
                ByLayer(br);
            });
        }
        public static void DrawDiryWaterWells1(Point2d pt, List<string> values, bool isRainWaterWell = THESAURUSESPECIALLY)
        {
            if (values == null) return;
            if (values.Count == PIEZOELECTRICAL)
            {
                var dy = -THESAURUSMALODOROUS;
                if (!isRainWaterWell)
                {
                    DrawDirtyWaterWell(pt.OffsetY(dy), values[BATHYDRACONIDAE]);
                }
                else
                {
                    DrawRainWaterWell(pt.OffsetY(dy), values[BATHYDRACONIDAE]);
                }
            }
            else if (values.Count >= TEREBINTHINATED)
            {
                var pts = GetBasePoints(pt.OffsetX(-CHLOROFLUOROCARBONS), TEREBINTHINATED, values.Count, CHLOROFLUOROCARBONS, CHLOROFLUOROCARBONS).ToList();
                for (int i = BATHYDRACONIDAE; i < values.Count; i++)
                {
                    if (!isRainWaterWell)
                    {
                        DrawDirtyWaterWell(pts[i], values[i]);
                    }
                    else
                    {
                        DrawRainWaterWell(pts[i], values[i]);
                    }
                }
            }
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
    }
    public class ThDrainageService
    {
        public static void PreFixGeoData(DrainageGeoData geoData)
        {
            foreach (var ct in geoData.Labels)
            {
                ct.Text = FixVerticalPipeLabel(ct.Text);
                ct.Boundary = ct.Boundary.Expand(-DISCOMFORTABLENESS);
            }
            geoData.FixData();
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
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSCOUNCIL)).ToList();
            }
            {
                geoData.WashingMachines = GeoFac.GroupGeometries(geoData.WashingMachines.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < CHEMOTROPICALLY && x.Height < CHEMOTROPICALLY).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, INTERLINGUISTICS))).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(THESAURUSCOUNCIL);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.WashingMachines = geoData.WashingMachines.Distinct(cmp).ToList();
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
                var pipes = _pipes.Select(x => x.ToPolygon()).ToList();
                var tag = new object();
                var pipesf = GeoFac.CreateIntersectsSelector(pipes);
                foreach (var _killer in geoData.PipeKillers)
                {
                    foreach (var pipe in pipesf(_killer.ToPolygon()))
                    {
                        pipe.UserData = tag;
                    }
                }
                geoData.VerticalPipes = pipes.Where(x => x.UserData == null).Select(pipes).ToList(_pipes);
            }
        }
        public static void ConnectLabelToLabelLine(DrainageGeoData geoData)
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
    }
    public class ThDrainageSystemServiceGeoCollector3
    {
        public AcadDatabase adb;
        public DrainageGeoData geoData;
        List<Polygon> roomPolygons;
        List<CText> roomNames;
        List<KeyValuePair<string, Geometry>> roomData => geoData.RoomData;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<CText> cts => geoData.Labels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GLineSegment> vlines => geoData.VLines;
        List<GLineSegment> wlines => geoData.WLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> downwaterPorts => geoData.DownWaterPorts;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<GRect> waterWells => geoData.WaterWells;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<GRect> washingMachines => geoData.WashingMachines;
        List<GRect> mopPools => geoData.MopPools;
        List<GRect> basins => geoData.Basins;
        List<GRect> pipeKillers => geoData.PipeKillers;
        List<GRect> rainPortSymbols => geoData.RainPortSymbols;
        List<KeyValuePair<Point2d, string>> wrappingPipeRadius => geoData.WrappingPipeRadius;
        public void CollectStoreys(CommandContext ctx)
        {
            storeys.AddRange(ctx.StoreyContext.StoreyInfos.Select(x => x.Boundary));
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
            }
        }
        public static IEnumerable<Entity> EnumerateVisibleEntites(AcadDatabase adb, BlockReference br)
        {
            var q = br.ExplodeToDBObjectCollection().OfType<Entity>().Where(e => e.Visible && e.Bounds.HasValue)
                .Where(e =>
                {
                    if (e.LayerId.IsNull) return THESAURUSESPECIALLY;
                    var layer = adb.Element<LayerTableRecord>(e.LayerId);
                    return !layer.IsFrozen && !layer.IsHidden && !layer.IsOff;
                });
            var xclip = br.XClipInfo();
            if (xclip.IsValid)
            {
                var gf = xclip.PreparedPolygon;
                return q.Where(e => gf.Intersects(e.Bounds.ToGRect().ToPolygon()));
            }
            else
            {
                return q;
            }
        }
        const int distinguishDiameter = THESAURUSDISPUTE;
        public static string GetEffectiveLayer(string entityLayer)
        {
            return GetEffectiveName(entityLayer);
        }
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
        public static string GetEffectiveBRName(string brName)
        {
            return GetEffectiveName(brName);
        }
        static bool isDrainageLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSSTENCH); 
        HashSet<Handle> ok_group_handles;
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
                        if (_dxfName is THESAURUSDICTIONARY && GetEffectiveLayer(e.Layer) is THESAURUSOBJECTIVELY)
                        {
                            dynamic o = e;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                            lst.Add(seg);
                        }
                    }
                    geoData.DLines.AddRange(TempGeoFac.GetMinConnSegs(lst.Where(x => x.IsValid).Distinct().ToList()));
                }
            }
            if (!CollectRoomDataAtCurrentModelSpaceOnly)
            {
                if (entityLayer.ToUpper() is THESAURUSHUMORIST or THESAURUSDEFAME)
                {
                    if (entity is Polyline pl)
                    {
                        try
                        {
                            pl = pl.Clone() as Polyline;
                            pl.TransformBy(matrix);
                        }
                        catch
                        {
                        }
                        var poly = ConvertToPolygon(pl);
                        if (poly != null)
                        {
                            roomPolygons.Add(poly);
                        }
                    }
                    return;
                }
                if (entityLayer.ToUpper() is THESAURUSCOMPLEX or THESAURUSLIBIDO)
                {
                    CText ct = null;
                    if (entity is MText mtx)
                    {
                        ct = new CText() { Text = mtx.Text, Boundary = mtx.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect().TransformBy(matrix) };
                    }
                    if (entity is DBText dbt)
                    {
                        ct = new CText() { Text = dbt.TextString, Boundary = dbt.Bounds.ToGRect().TransformBy(matrix) };
                    }
                    if (dxfName is THESAURUSMANIFEST)
                    {
                        dynamic o = entity.AcadObject;
                        string text = o.Text;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            ct = new CText() { Text = text, Boundary = entity.Bounds.ToGRect().TransformBy(matrix) };
                        }
                    }
                    if (ct != null)
                    {
                        roomNames.Add(ct);
                    }
                    return;
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
                if (entityLayer is THESAURUSCOURTESAN)
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
                if (dxfName == QUOTATIONPEIRCE && entityLayer is THESAURUSCOURTESAN)
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, r, rainPortSymbols);
                }
            }
            {
                if (entity is Circle c && isDrainageLayer(entityLayer))
                {
                    if (distinguishDiameter < c.Radius && c.Radius <= QUOTATIONPATRONAL)
                    {
                        var bd = c.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (DOLICHOCEPHALOUS < c.Radius && c.Radius <= distinguishDiameter && GetEffectiveLayer(c.Layer) is THESAURUSRECRIMINATION or THESAURUSRESIGN)
                    {
                        var bd = c.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, downwaterPorts);
                        return;
                    }
                }
            }
            {
                if (entity is Circle c)
                {
                    if (distinguishDiameter < c.Radius && c.Radius <= QUOTATIONPATRONAL)
                    {
                        if (isDrainageLayer(c.Layer))
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, pipes);
                            return;
                        }
                    }
                }
            }
            if (entityLayer is NEUROTRANSMITTER)
            {
                if (entity is Line line && line.Length > BATHYDRACONIDAE)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, wlines);
                    return;
                }
                else if (entity is Polyline pl)
                {
                    foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                    {
                        var seg = ln.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, wlines);
                    }
                    return;
                }
                if (dxfName is THESAURUSDICTIONARY)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wlines);
                    return;
                }
            }
            if (entityLayer is THESAURUSOBJECTIVELY or THESAURUSMARKET)
            {
                if (entity is Line line && line.Length > BATHYDRACONIDAE)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, dlines);
                    return;
                }
                else if (entity is Polyline pl)
                {
                    foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                    {
                        var seg = ln.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, dlines);
                    }
                    return;
                }
                if (dxfName is THESAURUSDICTIONARY)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, dlines);
                    return;
                }
            }
            if (dxfName is THESAURUSDICTIONARY)
            {
                if (entityLayer is THESAURUSINCORPORATE or THESAURUSINSPECTOR or THESAURUSRECRIMINATION)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                    {
                        if (c.Radius > distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, pipes);
                        }
                    }
                }
            }
            {
                if (isDrainageLayer(entityLayer) && entity is Line line && line.Length > BATHYDRACONIDAE)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, labelLines);
                    return;
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
                    if (e is Line line && isDrainageLayer(line.Layer))
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
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                }
                return;
            }
            {
                static bool g(string t) => !t.StartsWith(INTERPUNCTUATION) && !t.ToLower().Contains(THESAURUSHANDICRAFT) && !t.ToUpper().Contains(THESAURUSNONCONFORMIST);
                if (entity is DBText dbt && isDrainageLayer(entityLayer) && g(dbt.TextString))
                {
                    var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                    var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                    return;
                }
            }
            if (dxfName == THESAURUSMANIFEST)
            {
                dynamic o = entity.AcadObject;
                string text = o.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var ct = new CText() { Text = text, Boundary = entity.Bounds.ToGRect().TransformBy(matrix) };
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                }
                return;
            }
            if (dxfName == THESAURUSMERRIMENT)
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
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSMANIFEST or THESAURUSELUCIDATE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible))
                    {
                        foreach (var dbt in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                        {
                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                            if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                        }
                    }
                    foreach (var seg in colle.OfType<Line>().Where(x => x.Length > BATHYDRACONIDAE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                    {
                        reg(fs, seg, labelLines);
                    }
                }
                return;
            }
        }
        const string XREF_LAYER = THESAURUSNUTRITIOUS;
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var name = GetEffectiveBRName(br.GetEffectiveName());
                if (name is QUOTATIONADJACENT)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(CONTEMPTIBILITY) ?? THESAURUSAMENITY;
                    reg(fs, bd, () =>
                    {
                        waterPorts.Add(bd);
                        waterPortLabels.Add(lb);
                    });
                    return;
                }
                if (name.Contains(THESAURUSCOUNTRY))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(CONTEMPTIBILITY) ?? THESAURUSAMENITY;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                    });
                    return;
                }
                if (name.Contains(QUOTATIONARCTIC))
                {
                    if (br.IsDynamicBlock)
                    {
                        var bd = GRect.Combine(br.ExplodeToDBObjectCollection().OfType<Circle>().Where(x => x.Visible && x.Bounds.HasValue).Select(x => x.Bounds.ToGRect()));
                        if (!bd.IsValid)
                        {
                            bd = br.Bounds.ToGRect();
                        }
                        bd = bd.TransformBy(matrix);
                        reg(fs, bd, () =>
                        {
                            floorDrains.Add(bd);
                            geoData.UpdateFloorDrainTypeDict(bd.Center, br.ObjectId.GetDynBlockValue(THESAURUSCOMPREHEND) ?? THESAURUSAMENITY);
                        });
                        DrawRectLazy(bd, INTERLINGUISTICS);
                        return;
                    }
                    else
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, () =>
                        {
                            floorDrains.Add(bd);
                        });
                        return;
                    }
                }
                {
                    if (isDrainageLayer(br.Layer))
                    {
                        if (name is THESAURUSBALDERDASH or THESAURUSBAPTIZE)
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSFORTIFICATION);
                            reg(fs, bd, pipes);
                            return;
                        }
                        if (name.Contains(THESAURUSLOVING))
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(SENTIMENTALISING);
                            reg(fs, bd, pipes);
                            return;
                        }
                        if (name is THESAURUSCANDLE)
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(STANDOFFISHNESS);
                            reg(fs, bd, pipes);
                            return;
                        }
                    }
                    if (name is THESAURUSETHEREAL && GetEffectiveLayer(br.Layer) is THESAURUSEPITAPH or THESAURUSRECRIMINATION or THESAURUSOBJECTIVELY)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(STANDOFFISHNESS);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is ALSOPREPONDERANCY && GetEffectiveLayer(br.Layer) is THESAURUSADDICTION)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(STANDOFFISHNESS);
                        reg(fs, bd, pipes);
                        return;
                    }
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
                {
                    var ok = THESAURUSESPECIALLY;
                    if (killerNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        if (!washingMachinesNames.Any(x => name.Contains(x)))
                        {
                            reg(fs, bd, pipeKillers);
                        }
                        ok = THESAURUSNEGATIVE;
                    }
                    if (basinNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, basins);
                        ok = THESAURUSNEGATIVE;
                    }
                    if (washingMachinesNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, washingMachines);
                        ok = THESAURUSNEGATIVE;
                    }
                    if (mopPoolNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, mopPools);
                        ok = THESAURUSNEGATIVE;
                    }
                    if (ok) return;
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
        readonly List<string> basinNames = new List<string>() { UNINTERMITTENTLY, THESAURUSAPPETIZING, DIPTEROCARPACEAE, QUOTATIONDENNIS, PALAEOICHTHYOLOGIST, THESAURUSBONDAGE, THESAURUSINQUIRY };
        readonly List<string> mopPoolNames = new List<string>() { THESAURUSCOMPETITOR, };
        readonly List<string> killerNames = new List<string>() { THESAURUSINQUIRY, INOMETHYLCYCLOH, EXEMPLIFICATIONAL, CALLITRICHACEAE, THESAURUSPROTECTION, THESAURUSDISAPPOINTMENT, THESAURUSDISPERSE, THESAURUSAFFILIATE };
        readonly List<string> washingMachinesNames = new List<string>() { THESAURUSBRACKET, CATEGOREMATICALLY };
        bool isInXref;
        static bool HandleGroupAtCurrentModelSpaceOnly = THESAURUSESPECIALLY;
        public void CollectEntities()
        {
            if (!CollectRoomDataAtCurrentModelSpaceOnly)
            {
                roomNames ??= new List<CText>();
                roomPolygons ??= new List<Polygon>();
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
                        if (dxfName is THESAURUSDICTIONARY && GetEffectiveLayer(entity.Layer) is THESAURUSOBJECTIVELY)
                        {
                            dynamic o = entity;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                            lst.Add(seg);
                        }
                    }
                    lst = lst.Where(x => x.IsValid).Distinct().ToList();
                    geoData.DLines.AddRange(TempGeoFac.GetMinConnSegs(lst.Where(x => x.IsValid).Distinct().ToList()));
                }
            }
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                {
                    if (entity.Layer is THESAURUSFAMILIAR)
                    {
                        var bd = entity.Bounds.ToGRect();
                        if (bd.IsValid)
                        {
                            washingMachines.Add(bd);
                        }
                        return;
                    }
                }
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
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, List<GLineSegment> lst)
        {
            if (seg.IsValid) reg(fs, seg, () => { lst.Add(seg); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, List<GRect> lst)
        {
            if (r.IsValid) reg(fs, r, () => { lst.Add(r); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, List<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
        public void CollectRoomData()
        {
            if (CollectRoomDataAtCurrentModelSpaceOnly)
            {
                roomData.AddRange(DrainageService.CollectRoomData(adb));
            }
            else
            {
                var ranges = roomPolygons;
                var names = roomNames;
                var f = GeoFac.CreateIntersectsSelector(ranges);
                var list = roomData;
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
                                DrawGeometryLazy(geo);
                                ranges.Remove(geo);
                            }
                        }
                    }
                }
                foreach (var range in ranges.Except(list.Select(kv => kv.Value)))
                {
                    list.Add(new KeyValuePair<string, Geometry>(THESAURUSAMENITY, range));
                }
            }
        }
        static bool CollectRoomDataAtCurrentModelSpaceOnly = THESAURUSESPECIALLY;
    }
    public class DrainageService
    {
        public static void CollectGeoData(AcadDatabase adb, DrainageGeoData geoData, CommandContext ctx)
        {
            var cl = new ThDrainageSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(ctx);
            cl.CollectEntities();
            cl.CollectRoomData();
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = new ThDrainageSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(range);
            cl.CollectEntities();
            cl.CollectRoomData();
        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == BATHYDRACONIDAE) return THESAURUSESPECIALLY;
            }
            return THESAURUSNEGATIVE;
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer?.ToUpper() is THESAURUSHUMORIST or THESAURUSDEFAME)
                .SelectNotNull(ConvertToPolygon).ToList();
            var names = adb.ModelSpace.Where(x => x.Layer?.ToUpper() is THESAURUSCOMPLEX or THESAURUSLIBIDO).SelectNotNull(entity =>
            {
                if (entity is MText mtx)
                {
                    return new CText() { Text = mtx.Text, Boundary = mtx.ExplodeToDBObjectCollection().OfType<DBText>().First().Bounds.ToGRect() };
                }
                if (entity is DBText dbt)
                {
                    return new CText() { Text = dbt.TextString, Boundary = dbt.Bounds.ToGRect() };
                }
                var dxfName = entity.GetRXClass().DxfName.ToUpper();
                if (dxfName == THESAURUSMANIFEST)
                {
                    dynamic o = entity.AcadObject;
                    string text = o.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return new CText() { Text = text, Boundary = entity.Bounds.ToGRect() };
                    }
                }
                return null;
            }).ToList();
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
                            DrawGeometryLazy(geo);
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
        public static void CreateDrawingDatas(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas, out string logString, out List<DrainageDrawingData> drDatas)
        {
            var roomData = geoData.RoomData;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = PIEZOELECTRICAL;
            }
            var sb = new StringBuilder(MINERALOCORTICOID);
            drDatas = new List<DrainageDrawingData>();
            var _kitchens = roomData.Where(x => IsKitchen(x.Key)).Select(x => x.Value).ToList();
            var _toilets = roomData.Where(x => IsToilet(x.Key)).Select(x => x.Value).ToList();
            var _nonames = roomData.Where(x => x.Key is THESAURUSAMENITY).Select(x => x.Value).ToList();
            var _balconies = roomData.Where(x => IsBalcony(x.Key)).Select(x => x.Value).ToList();
            var _kitchensf = F(_kitchens);
            var _toiletsf = F(_toilets);
            var _nonamesf = F(_nonames);
            var _balconiesf = F(_balconies);
            for (int storeyI = BATHYDRACONIDAE; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new DrainageDrawingData();
                drData.Init();
                var item = cadDatas[storeyI];
                var storeyGeo = geoData.Storeys[storeyI].ToPolygon();
                var kitchens = _kitchensf(storeyGeo);
                var toilets = _toiletsf(storeyGeo);
                var nonames = _nonamesf(storeyGeo);
                var balconies = _balconiesf(storeyGeo);
                var kitchensf = F(kitchens);
                var toiletsf = F(toilets);
                var nonamesf = F(nonames);
                var balconiesf = F(balconies);
                {
                    var maxDis = THESAURUSRETAIN;
                    var angleTolleranceDegree = PIEZOELECTRICAL;
                    var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > BATHYDRACONIDAE).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.DownWaterPorts).Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
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
                var labellinesGeosf = F(labelLinesGeos);
                var shortTranslatorLabels = new HashSet<string>();
                var longTranslatorLabels = new HashSet<string>();
                var dlinesGroups = GG(item.DLines);
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, HOOGMOGENDHEIDEN).ToList();
                var dlinesGeosf = F(dlinesGeos);
                var washingMachinesf = F(cadDataMain.WashingMachines);
                var vlinesGroups = GG(item.VLines);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var labelsf = F(item.Labels);
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (!IsDrainageLabelText(text)) continue;
                        var lst = labellinesGeosf(label);
                        if (lst.Count == PIEZOELECTRICAL)
                        {
                            var labelline = lst[BATHYDRACONIDAE];
                            var pipes = pipesf(GeoFac.CreateGeometry(label, labelline));
                            if (pipes.Count == BATHYDRACONIDAE)
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
                    var labelsf = F(item.Labels);
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (!IsDrainageLabelText(text)) continue;
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
                foreach (var o in item.WashingMachines)
                {
                    DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)], INTERLINGUISTICS);
                }
                foreach (var o in item.VerticalPipes)
                {
                    DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = THESAURUSINTELLECT;
                }
                foreach (var o in item.FloorDrains)
                {
                    DrawGeometryLazy(o, ents => ents.ForEach(e => e.ColorIndex = PARALLELOGRAMMIC));
                }
                foreach (var o in item.WaterPorts)
                {
                    DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = PERCHLOROETHYLENE;
                    DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WashingMachines)
                {
                    var e = DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = PIEZOELECTRICAL;
                }
                foreach (var o in item.CleaningPorts)
                {
                    var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                    DrawRectLazy(GRect.Create(m, DISCOMFORTABLENESS));
                }
                {
                    var cl = Color.FromRgb(THESAURUSFELLOW, THESAURUSBEDEVIL, THESAURUSTESTIMONY);
                    foreach (var o in item.WrappingPipes)
                    {
                        var e = DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(THESAURUSEVINCE, THESAURUSJAGGED, THESAURUSENDING);
                    foreach (var o in item.DLines)
                    {
                        var e = DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(HYDROXYNAPHTHALENE, THESAURUSFOLLOWER, THESAURUSACCORDANCE);
                    foreach (var o in item.VLines)
                    {
                        DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                    }
                }
                foreach (var o in item.WLines)
                {
                    DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = PARALLELOGRAMMIC;
                }
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
                                    else if (IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
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
                                foreach (var dlinesGeo in dlinesGeos)
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
                                                if (!IsTL(label))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = THESAURUSNEGATIVE;
                                                }
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
                                        if (!IsTL(label))
                                        {
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
                    foreach (var dlinesGeo in dlinesGeos)
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
                                    if (!IsTL(label))
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
                {
                    var ok_ents = new HashSet<Geometry>();
                    var outletd = new Dictionary<string, string>();
                    var outletWrappingPipe = new Dictionary<int, string>();
                    var portd = new Dictionary<Geometry, string>();
                    {
                        void collect(Func<Geometry, List<Geometry>> waterPortsf, Func<Geometry, string> getWaterPortLabel)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var waterPorts = waterPortsf(dlinesGeo);
                                if (waterPorts.Count == PIEZOELECTRICAL)
                                {
                                    var waterPort = waterPorts[BATHYDRACONIDAE];
                                    var waterPortLabel = getWaterPortLabel(waterPort);
                                    portd[dlinesGeo] = waterPortLabel;
                                    var pipes = f2(dlinesGeo);
                                    ok_ents.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            outletd[label] = waterPortLabel;
                                            var wrappingpipes = wrappingPipesf(dlinesGeo);
                                            if (wrappingpipes.Count > BATHYDRACONIDAE)
                                            {
                                            }
                                            foreach (var wp in wrappingpipes)
                                            {
                                                outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                                portd[wp] = waterPortLabel;
                                                DrawTextLazy(waterPortLabel, wp.GetCenter());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        collect(F(item.WaterPorts), waterPort => geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)]);
                        {
                            var spacialIndex = item.WaterPorts.Select(cadDataMain.WaterPorts).ToList();
                            var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(PSYCHOPHYSIOLOGICAL).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = INTERLINGUISTICS;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(PARALLELOGRAMMIC, THESAURUSESPECIALLY)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterPort = f5(pt.ToPoint3d());
                                if (waterPort != null)
                                {
                                    if (waterPort.GetCenter().GetDistanceTo(pt) <= PRAETERNATURALIS)
                                    {
                                        var waterPortLabel = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)];
                                        portd[dlinesGeo] = waterPortLabel;
                                        foreach (var pipe in f2(dlinesGeo))
                                        {
                                            if (lbDict.TryGetValue(pipe, out string label))
                                            {
                                                outletd[label] = waterPortLabel;
                                                ok_ents.Add(pipe);
                                                var wrappingpipes = wrappingPipesf(dlinesGeo);
                                                if (wrappingpipes.Any())
                                                {
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                                    portd[wp] = waterPortLabel;
                                                    DrawTextLazy(waterPortLabel, wp.GetCenter());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    {
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
                        var pipesf = F(item.VerticalPipes);
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
                        drData.ShortTranslatorLabels.AddRange(shortTranslatorLabels);
                        drData.LongTranslatorLabels.AddRange(longTranslatorLabels);
                        drData.Outlets = outletd;
                        outletd.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
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
                    var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, HOOGMOGENDHEIDEN).ToList();
                    var wlinesGeosf = F(wlinesGeos);
                    var fdsf = F(item.FloorDrains);
                    var vps = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (IsFL0(kv.Value))
                        {
                            vps.Add(kv.Key);
                        }
                    }
                    {
                        foreach (var fl0 in vps)
                        {
                            foreach (var wl in wlinesGeos)
                            {
                                if (fdsf(wl).Any())
                                {
                                    drData.IsConnectedToFloorDrainForFL0.Add(lbDict[fl0]);
                                }
                            }
                            foreach (var dl in wlinesGeos)
                            {
                                if (fdsf(dl).Any())
                                {
                                    drData.IsConnectedToFloorDrainForFL0.Add(lbDict[fl0]);
                                }
                            }
                        }
                    }
                    var ok_vpipes = new HashSet<Geometry>();
                    var outletd = new Dictionary<string, string>();
                    var waterWellIdDict = new Dictionary<string, int>();
                    var rainPortIdDict = new Dictionary<string, int>();
                    var waterWellsIdDict = new Dictionary<Geometry, int>();
                    var waterWellsLabelDict = new Dictionary<Geometry, string>();
                    var outletWrappingPipe = new Dictionary<int, string>();
                    var hasRainPortSymbols = new HashSet<string>();
                    {
                        var rainPortsf = F(item.RainPortSymbols);
                        var pipesf = F(vps.Except(ok_vpipes).ToList());
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
                        drData.HasRainPortSymbolsForFL0 = hasRainPortSymbols;
                    }
                    {
                        void collect(Func<Geometry, List<Geometry>> waterWellsf, Func<Geometry, string> getWaterWellLabel, Func<Geometry, int> getWaterWellId)
                        {
                            var f2 = F(vps.Except(ok_vpipes).ToList());
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
                                            outletd[label] = waterWellLabel;
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
                        collect(F(item.WaterPorts), waterWell => geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterWell)], well => cadDataMain.WaterPorts.IndexOf(well));
                        {
                            var spacialIndex = item.WaterPorts.Select(cadDataMain.WaterPorts).ToList();
                            var waterWells = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(PSYCHOPHYSIOLOGICAL).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterPortLabels[spacialIndex[waterWells.IndexOf(waterWell)]], waterWell => spacialIndex[waterWells.IndexOf(waterWell)]);
                        }
                    }
                    {
                        var f2 = F(vps.Except(ok_vpipes).ToList());
                        var radius = INTERLINGUISTICS;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(PARALLELOGRAMMIC, THESAURUSESPECIALLY)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(vps.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= PRAETERNATURALIS)
                                    {
                                        var waterWellLabel = geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterWell)];
                                        waterWellsLabelDict[dlinesGeo] = waterWellLabel;
                                        waterWellsIdDict[dlinesGeo] = cadDataMain.WaterPorts.IndexOf(waterWell);
                                        foreach (var pipe in f2(dlinesGeo))
                                        {
                                            if (lbDict.TryGetValue(pipe, out string label))
                                            {
                                                outletd[label] = waterWellLabel;
                                                waterWellIdDict[label] = cadDataMain.WaterPorts.IndexOf(waterWell);
                                                ok_vpipes.Add(pipe);
                                                var wrappingpipes = wrappingPipesf(dlinesGeo);
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    outletWrappingPipe[cadDataMain.WrappingPipes.IndexOf(wp)] = label;
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
                                                    waterWellsLabelDict[wp] = waterWellLabel;
                                                    waterWellsIdDict[wp] = cadDataMain.WaterPorts.IndexOf(waterWell);
                                                    DrawTextLazy(waterWellLabel, wp.GetCenter());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var pp in item.VerticalPipes)
                {
                    lbDict.TryGetValue(pp, out string label);
                    if (label != null)
                    {
                        DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                        drData.VerticalPipeLabels.Add(label);
                    }
                }
                var FLs = new List<Geometry>();
                var FL0s = new List<Geometry>();
                var pls = new List<Geometry>();
                foreach (var kv in lbDict)
                {
                    if (IsFL(kv.Value) && !IsFL0(kv.Value))
                    {
                        FLs.Add(kv.Key);
                    }
                    else if (IsFL0(kv.Value))
                    {
                        FL0s.Add(kv.Key);
                    }
                    else if (IsPL(kv.Value))
                    {
                        pls.Add(kv.Key);
                    }
                }
                {
                    var toiletPls = new HashSet<string>();
                    var plsf = F(pls);
                    foreach (var _toilet in toilets)
                    {
                        var toilet = _toilet.Buffer(THESAURUSSUCCINCT);
                        foreach (var pl in plsf(toilet))
                        {
                            toiletPls.Add(lbDict[pl]);
                        }
                    }
                    drData.ToiletPls.AddRange(toiletPls);
                }
                {
                    var kitchenFls = new HashSet<string>();
                    var balconyFls = new HashSet<string>();
                    var ok_fls = new HashSet<Geometry>();
                    var ok_rooms = new HashSet<Geometry>();
                    var flsf = F(FLs);
                    foreach (var kitchen in kitchens)
                    {
                        if (ok_rooms.Contains(kitchen)) continue;
                        foreach (var fl in flsf(kitchen))
                        {
                            if (ok_fls.Contains(fl)) continue;
                            ok_fls.Add(fl);
                            kitchenFls.Add(lbDict[fl]);
                            ok_rooms.Add(kitchen);
                        }
                    }
                    foreach (var bal in balconies)
                    {
                        if (ok_rooms.Contains(bal)) continue;
                        foreach (var fl in flsf(bal))
                        {
                            if (ok_fls.Contains(fl)) continue;
                            ok_fls.Add(fl);
                            balconyFls.Add(lbDict[fl]);
                            ok_rooms.Add(bal);
                        }
                    }
                    for (double buf = QUOTATIONPATRONAL; buf <= THESAURUSSUCCINCT; buf += QUOTATIONPATRONAL)
                    {
                        foreach (var kitchen in kitchens)
                        {
                            if (ok_rooms.Contains(kitchen)) continue;
                            var ok = THESAURUSESPECIALLY;
                            foreach (var toilet in toiletsf(kitchen.Buffer(buf)))
                            {
                                if (ok_rooms.Contains(toilet))
                                {
                                    ok = THESAURUSNEGATIVE;
                                    break;
                                }
                                foreach (var fl in flsf(toilet))
                                {
                                    ok = THESAURUSNEGATIVE;
                                    ok_fls.Add(fl);
                                    ok_rooms.Add(toilet);
                                    kitchenFls.Add(lbDict[fl]);
                                }
                            }
                            if (ok)
                            {
                                ok_rooms.Add(kitchen);
                                continue;
                            }
                            foreach (var fl in flsf(kitchen.Buffer(buf)))
                            {
                                if (ok_fls.Contains(fl)) continue;
                                ok_fls.Add(fl);
                                kitchenFls.Add(lbDict[fl]);
                                ok_rooms.Add(kitchen);
                            }
                        }
                        foreach (var bal in balconies)
                        {
                            if (ok_rooms.Contains(bal)) continue;
                            foreach (var fl in flsf(bal.Buffer(buf)))
                            {
                                if (ok_fls.Contains(fl)) continue;
                                ok_fls.Add(fl);
                                balconyFls.Add(lbDict[fl]);
                                ok_rooms.Add(bal);
                            }
                        }
                    }
                    drData.KitchenFls.AddRange(kitchenFls);
                    drData.BalconyFls.AddRange(balconyFls);
                }
                {
                    var flsf = F(FLs);
                    var filtedFds = item.FloorDrains.Where(fd => toilets.All(toilet => !toilet.Intersects(fd))).ToList();
                    var fdsf = F(filtedFds);
                    foreach (var lineEx in GeoFac.GroupGeometries(item.FloorDrains.OfType<Polygon>().Select(x => x.Shell)
                        .Concat(FLs.OfType<Polygon>().Select(x => x.Shell))
                        .Concat(dlinesGeos).Distinct().ToList()).Select(x => GeoFac.CreateGeometry(x)))
                    {
                        var fls = flsf(lineEx);
                        foreach (var fl in fls)
                        {
                            var fds = fdsf(lineEx);
                            drData.FloorDrains[lbDict[fl]] = fds.Count;
                            var washingMachineFds = new List<Geometry>();
                            var shooters = geoData.FloorDrainTypeShooter.Select(kv =>
                            {
                                var geo = GRect.Create(kv.Key, QUOTATIONPATRONAL).ToPolygon();
                                geo.UserData = kv.Value;
                                return geo;
                            }).ToList();
                            var shootersf = GeoFac.CreateIntersectsSelector(shooters);
                            foreach (var fd in fds)
                            {
                                var ok = THESAURUSESPECIALLY;
                                foreach (var geo in shootersf(fd))
                                {
                                    var name = (string)geo.UserData;
                                    if (!string.IsNullOrWhiteSpace(name))
                                    {
                                        if (name.Contains(THESAURUSAPPRECIATION) || name.Contains(THESAURUSENFORCEMENT))
                                        {
                                            ok = THESAURUSNEGATIVE;
                                            break;
                                        }
                                    }
                                }
                                if (!ok)
                                {
                                    if (washingMachinesf(fd).Any())
                                    {
                                        ok = THESAURUSNEGATIVE;
                                    }
                                }
                                if (ok)
                                {
                                    washingMachineFds.Add(fd);
                                }
                            }
                            drData.WashingMachineFloorDrains[lbDict[fl]] = washingMachineFds.Count;
                            if (fds.Count == TEREBINTHINATED)
                            {
                                bool is4tune;
                                bool isShunt()
                                {
                                    is4tune = THESAURUSESPECIALLY;
                                    var _dlines = dlinesGeosf(fl);
                                    if (_dlines.Count == BATHYDRACONIDAE) return THESAURUSESPECIALLY;
                                    if (fds.Count == TEREBINTHINATED)
                                    {
                                        {
                                            try
                                            {
                                                var aaa = fds.YieldAfter(fl).Distinct().ToList();
                                                var jjj = GeoFac.CreateGeometry(aaa);
                                                var bbb = dlinesGeosf(jjj).Select(x => GeoFac.GetLines(x)).SelectMany(x => x).Distinct().Select(x => x.ToLineString());
                                                var ccc = GeoFac.CreateGeometry(bbb).Difference(jjj);
                                                var ddd = GeoFac.GetLines(ccc).ToList();
                                                var xxx = GeoFac.ToNodedLineSegments(ddd).Distinct().Select(x => x.ToLineString()).ToList();
                                                var yyy = GeoFac.GroupGeometries(xxx).Select(geos => GeoFac.CreateGeometry(geos)).ToList();
                                                if (yyy.Count == PIEZOELECTRICAL)
                                                {
                                                    var dlines = yyy[BATHYDRACONIDAE];
                                                    if (dlines.Intersects(fds[BATHYDRACONIDAE].Buffer(DOLICHOCEPHALOUS)) && dlines.Intersects(fds[PIEZOELECTRICAL].Buffer(DOLICHOCEPHALOUS)) && dlines.Intersects(fl.Buffer(DOLICHOCEPHALOUS)))
                                                    {
                                                        if (wrappingPipesf(dlines).Count >= TEREBINTHINATED)
                                                        {
                                                            is4tune = THESAURUSNEGATIVE;
                                                        }
                                                        return THESAURUSESPECIALLY;
                                                    }
                                                }
                                                else if (yyy.Count == TEREBINTHINATED)
                                                {
                                                    var dl1 = yyy[BATHYDRACONIDAE];
                                                    var dl2 = yyy[PIEZOELECTRICAL];
                                                    var fd1 = fds[BATHYDRACONIDAE].Buffer(DOLICHOCEPHALOUS);
                                                    var fd2 = fds[PIEZOELECTRICAL].Buffer(DOLICHOCEPHALOUS);
                                                    var vp = fl.Buffer(DOLICHOCEPHALOUS);
                                                    var geos = new List<Geometry>() { fd1, fd2, vp };
                                                    var f = F(geos);
                                                    var l1 = f(dl1);
                                                    var l2 = f(dl2);
                                                    if (l1.Count == TEREBINTHINATED && l2.Count == TEREBINTHINATED && l1.Contains(vp) && l2.Contains(vp))
                                                    {
                                                        return THESAURUSNEGATIVE;
                                                    }
                                                    return THESAURUSESPECIALLY;
                                                }
                                            }
                                            catch
                                            {
                                                return THESAURUSESPECIALLY;
                                            }
                                        }
                                    }
                                    return THESAURUSESPECIALLY;
                                }
                                if (isShunt())
                                {
                                    drData.Shunts.Add(lbDict[fl]);
                                    if (is4tune)
                                    {
                                        drData._4tunes.Add(lbDict[fl]);
                                    }
                                }
                            }
                        }
                    }
                    {
                        {
                            var supterDLineGeos = GeoFac.GroupGeometries(dlinesGeos.Concat(filtedFds.OfType<Polygon>().Select(x => x.Shell)).ToList()).Select(x => x.Count == PIEZOELECTRICAL ? x[BATHYDRACONIDAE] : GeoFac.CreateGeometry(x)).ToList();
                            var f = F(supterDLineGeos);
                            foreach (var wp in item.WaterPorts.Select(x => x.ToGRect().Expand(QUOTATIONPATRONAL).ToPolygon().Shell))
                            {
                                var dls = f(wp);
                                var dlgeo = GeoFac.CreateGeometry(dls);
                                var fds = fdsf(dlgeo);
                                var c = fds.Count;
                                {
                                    var _fls = flsf(dlgeo).Where(fl => toilets.All(toilet => !toilet.Intersects(fl))).ToList();
                                    foreach (var fl in _fls)
                                    {
                                        var label = lbDict[fl];
                                        drData.SingleOutletFloorDrains.TryGetValue(label, out int v);
                                        v = Math.Max(v, c);
                                        drData.SingleOutletFloorDrains[label] = v;
                                    }
                                }
                            }
                        }
                        {
                        }
                    }
                }
                {
                }
                {
                    {
                        var xls = FLs;
                        var xlsf = F(xls);
                        var gs = GeoFac.GroupGeometries(xls.Concat(item.DownWaterPorts).Select(xl => CreateXGeoRect(xl.ToGRect())).Concat(dlinesGeos).ToList())
                            .Select(x => x.Count == PIEZOELECTRICAL ? x[BATHYDRACONIDAE] : GeoFac.CreateGeometry(x)).ToList();
                        foreach (var g in gs)
                        {
                            var vps = xlsf(g);
                            if (vps.Count > BATHYDRACONIDAE)
                            {
                                var lb = lbDict[vps.First()];
                                drData.circlesCount.TryGetValue(lb, out int v);
                                drData.circlesCount[lb] = Math.Max(v, vps.Count);
                            }
                        }
                    }
                    {
                        var xls = pls;
                        var xlsf = F(xls);
                        var gs = GeoFac.GroupGeometries(xls.Concat(item.DownWaterPorts).Select(xl => CreateXGeoRect(xl.ToGRect())).Concat(dlinesGeos).ToList())
                            .Select(x => x.Count == PIEZOELECTRICAL ? x[BATHYDRACONIDAE] : GeoFac.CreateGeometry(x)).ToList();
                        foreach (var g in gs)
                        {
                            var vps = xlsf(g);
                            if (vps.Count > BATHYDRACONIDAE)
                            {
                                var lb = lbDict[vps.First()];
                                drData.circlesCount.TryGetValue(lb, out int v);
                                drData.circlesCount[lb] = Math.Max(v, vps.Count);
                            }
                        }
                    }
                }
                {
                    var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, HOOGMOGENDHEIDEN).ToList();
                    var wlinesGeosf = F(wlinesGeos);
                    var merges = new HashSet<string>();
                    var wells = item.WaterWells.Select(x => x.Buffer(THESAURUSBEFRIEND)).ToList();
                    if (wells.Count > BATHYDRACONIDAE)
                    {
                        var gs = GeoFac.GroupGeometries(FL0s.Concat(item.FloorDrains).Select(xl => CreateXGeoRect(xl.ToGRect())).Concat(wlinesGeos).ToList())
                        .Select(x => x.Count == PIEZOELECTRICAL ? x[BATHYDRACONIDAE] : GeoFac.CreateGeometry(x)).ToList();
                        var circlesf = F(item.FloorDrains.Concat(FL0s).ToList());
                        var gsf = F(gs);
                        foreach (var well in wells)
                        {
                            var g = G(gsf(well));
                            var circles = circlesf(g);
                            var fl0s = circles.Where(x => FL0s.Contains(x)).ToList();
                            if (fl0s.Count == BATHYDRACONIDAE) continue;
                            var fds = circles.Where(x => item.FloorDrains.Contains(x)).ToList();
                            if (fl0s.Count == PIEZOELECTRICAL && fds.Count == PIEZOELECTRICAL)
                            {
                                var fl = fl0s[BATHYDRACONIDAE];
                                var fd = fds[BATHYDRACONIDAE];
                                if (wlinesGeosf(fl).Intersect(wlinesGeosf(fd)).Any())
                                {
                                    merges.Add(lbDict[fl]);
                                }
                            }
                        }
                    }
                    drData.Merges.AddRange(merges);
                }
                drDatas.Add(drData);
            }
            logString = sb.ToString();
        }
        public static Geometry CreateXGeoRect(GRect r)
        {
            return new MultiLineString(new LineString[] {
                r.ToLinearRing(),
                new LineString(new Coordinate[] { r.LeftTop.ToNTSCoordinate(), r.RightButtom.ToNTSCoordinate() }),
                new LineString(new Coordinate[] { r.LeftButtom.ToNTSCoordinate(), r.RightTop.ToNTSCoordinate() })
            });
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = DIETHYLSTILBOESTROL;
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
        {
            return source1.Concat(source2).ToList();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
        {
            return source1.Concat(source2).Concat(source3).ToList();
        }
        public static HashSet<Geometry> GetFLsWhereSupportingFloorDrainUnderWaterPoint(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> floorDrains, List<Geometry> washMachines)
        {
            var f = GeoFac.CreateIntersectsSelector(ToList(floorDrains, washMachines));
            var hs = new HashSet<Geometry>();
            {
                var flsf = GeoFac.CreateIntersectsSelector(FLs);
                foreach (var kitchen in kitchens)
                {
                    var lst = flsf(kitchen);
                    if (lst.Count > BATHYDRACONIDAE)
                    {
                        if (f(kitchen).Count > BATHYDRACONIDAE)
                        {
                            hs.AddRange(lst);
                        }
                    }
                }
            }
            return hs;
        }
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
        static List<Point2d> GetEndPoints(Geometry start, List<Point2d> points, List<Geometry> dlines)
        {
            points = points.Distinct().ToList();
            var pts = points.Select(x => new GCircle(x, DOLICHOCEPHALOUS).ToCirclePolygon(PARALLELOGRAMMIC)).ToList();
            var dlinesGeo = GeoFac.CreateGeometry(GeoFac.CreateIntersectsSelector(dlines)(start));
            return GeoFac.CreateIntersectsSelector(pts)(dlinesGeo).Where(x => !x.Intersects(start)).Select(pts).ToList(points);
        }
        public static List<Geometry> GetKitchenAndBalconyBothFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies, List<Point2d> pts, List<Geometry> dlines)
        {
            var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
            var list = new List<Geometry>(FLs.Count);
            {
                return list;
            }
            foreach (var fl in FLs)
            {
                List<Point2d> endpoints = null;
                Geometry endpointsGeo = null;
                List<Point2d> _GetEndPoints()
                {
                    return GetEndPoints(fl, pts, dlines);
                }
                bool test1()
                {
                    return GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), THESAURUSBEFRIEND, THESAURUSADMISSION).Intersects(kitchensGeo);
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                    return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
                }
                if (test1() && test2())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
        public static List<Geometry> GetKitchenOnlyFLs(List<Geometry> FLs,
            List<Geometry> kitchens,
            List<Geometry> nonames,
            List<Geometry> balconies,
            List<Point2d> pts,
            List<Geometry> dlines,
            List<string> labels,
            List<Geometry> basins,
            List<Geometry> floorDrains,
            List<Geometry> washingMachines,
            List<bool> hasBasinList,
            List<bool> hasKitchenFloorDrainList,
            List<bool> hasKitchenWashingMachineList
            )
        {
            var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
            var list = new List<Geometry>(FLs.Count);
            var basinsf = GeoFac.CreateIntersectsSelector(basins);
            {
                var ok_fls = new HashSet<Geometry>();
                var floorDrainsf = GeoFac.CreateIntersectsSelector(floorDrains);
                var washingMachinesf = GeoFac.CreateIntersectsSelector(washingMachines);
                foreach (var kitchen in kitchens)
                {
                    var flsf = GeoFac.CreateIntersectsSelector(FLs.Except(ok_fls).ToList());
                    var fls = flsf(kitchen);
                    if (fls.Count > BATHYDRACONIDAE)
                    {
                        var hasBasin = basinsf(kitchen).Any();
                        foreach (var fl in fls)
                        {
                            list.Add(fl);
                            hasBasinList.Add(hasBasin);
                            hasKitchenFloorDrainList.Add(floorDrainsf(kitchen).Any());
                            hasKitchenWashingMachineList.Add(washingMachinesf(kitchen).Any());
                            ok_fls.Add(fl);
                        }
                    }
                    else
                    {
                        fls = flsf(kitchen.Buffer(THESAURUSBEFRIEND));
                        if (fls.Count > BATHYDRACONIDAE)
                        {
                            var hasBasin = basinsf(kitchen).Any();
                            var fl = GeoFac.NearestNeighbourGeometryF(fls)(kitchen);
                            list.Add(fl);
                            hasBasinList.Add(hasBasin);
                            hasKitchenFloorDrainList.Add(floorDrainsf(kitchen).Any());
                            hasKitchenWashingMachineList.Add(washingMachinesf(kitchen).Any());
                            ok_fls.Add(fl);
                        }
                    }
                }
                return list;
            }
            for (int i = BATHYDRACONIDAE; i < FLs.Count; i++)
            {
                var fl = FLs[i];
                var lb = labels[i];
                {
                    foreach (var kitchen in kitchens)
                    {
                        if (kitchen.Envelope.ToGRect().ToPolygon().Intersects(fl))
                        {
                            list.Add(fl);
                            hasBasinList.Add(basinsf(kitchen).Any());
                        }
                    }
                    continue;
                }
                List<Point2d> endpoints = null;
                Geometry endpointsGeo = null;
                List<Point2d> _GetEndPoints()
                {
                    return GetEndPoints(fl, pts, dlines);
                }
                bool test1()
                {
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter(), THESAURUSBEFRIEND, THESAURUSADMISSION));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    if (endpoints.Count == BATHYDRACONIDAE) return THESAURUSNEGATIVE;
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                    return kitchensGeo.Intersects(endpointsGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                    return !endpointsGeo.Intersects(GeoFac.CreateGeometryEx(nonames)) || !endpointsGeo.Intersects(GeoFac.CreateGeometryEx(balconies));
                }
                if ((test1() || test2()) && test3())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
    }
    public class DrainageDrawingData
    {
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public Dictionary<string, int> SingleOutletFloorDrains;
        public Dictionary<string, int> WashingMachineFloorDrains;
        public Dictionary<string, int> circlesCount;
        public Dictionary<string, string> Outlets;
        public HashSet<string> KitchenFls;
        public HashSet<string> BalconyFls;
        public HashSet<string> ToiletPls;
        public HashSet<string> Merges;
        public Dictionary<int, string> OutletWrappingPipeDict;
        public Dictionary<int, string> OutletWrappingPipeRadiusStringDict;
        public HashSet<string> Shunts;
        public HashSet<string> _4tunes;
        public HashSet<string> HasRainPortSymbolsForFL0;
        public HashSet<string> IsConnectedToFloorDrainForFL0;
        public void Init()
        {
            VerticalPipeLabels ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            SingleOutletFloorDrains ??= new Dictionary<string, int>();
            WashingMachineFloorDrains ??= new Dictionary<string, int>();
            circlesCount ??= new Dictionary<string, int>();
            Outlets ??= new Dictionary<string, string>();
            Shunts ??= new HashSet<string>();
            _4tunes ??= new HashSet<string>();
            KitchenFls ??= new HashSet<string>();
            BalconyFls ??= new HashSet<string>();
            ToiletPls ??= new HashSet<string>();
            HasRainPortSymbolsForFL0 ??= new HashSet<string>();
            IsConnectedToFloorDrainForFL0 ??= new HashSet<string>();
            Merges ??= new HashSet<string>();
            OutletWrappingPipeDict ??= new Dictionary<int, string>();
            OutletWrappingPipeRadiusStringDict ??= new Dictionary<int, string>();
        }
    }
    public class DrainageGeoData
    {
        public List<GRect> Storeys;
        public List<KeyValuePair<string, Geometry>> RoomData;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public List<GLineSegment> DLines;
        public List<GLineSegment> VLines;
        public List<GLineSegment> WLines;
        public List<GRect> VerticalPipes;
        public List<GRect> WrappingPipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterPorts;
        public List<GRect> WaterWells;
        public List<string> WaterPortLabels;
        public List<GRect> WashingMachines;
        public List<GRect> Basins;
        public List<GRect> MopPools;
        public List<Point2d> CleaningPorts;
        public List<Point2d> SideFloorDrains;
        public List<GRect> PipeKillers;
        public List<GRect> DownWaterPorts;
        public List<GRect> RainPortSymbols;
        public List<KeyValuePair<Point2d, string>> FloorDrainTypeShooter;
        public List<KeyValuePair<Point2d, string>> WrappingPipeRadius;
        public List<GLineSegment> WrappingPipeLabelLines;
        public List<CText> WrappingPipeLabels;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            RoomData ??= new List<KeyValuePair<string, Geometry>>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            VerticalPipes ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterWells ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            WashingMachines ??= new List<GRect>();
            Basins ??= new List<GRect>();
            CleaningPorts ??= new List<Point2d>();
            SideFloorDrains ??= new List<Point2d>();
            PipeKillers ??= new List<GRect>();
            MopPools ??= new List<GRect>();
            DownWaterPorts ??= new List<GRect>();
            RainPortSymbols ??= new List<GRect>();
            FloorDrainTypeShooter ??= new List<KeyValuePair<Point2d, string>>();
            WrappingPipeRadius ??= new List<KeyValuePair<Point2d, string>>();
            WrappingPipeLabelLines ??= new List<GLineSegment>();
            WrappingPipeLabels ??= new List<CText>();
        }
        Dictionary<Point2d, string> floorDrainTypeDict;
        public void UpdateFloorDrainTypeDict(Point2d bd, string v)
        {
            floorDrainTypeDict ??= new Dictionary<Point2d, string>();
            if (!string.IsNullOrWhiteSpace(v))
            {
                floorDrainTypeDict[bd] = v;
            }
        }
        public void Flush()
        {
            if (floorDrainTypeDict != null)
            {
                FloorDrainTypeShooter.AddRange(floorDrainTypeDict);
                floorDrainTypeDict = null;
            }
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
            DownWaterPorts = DownWaterPorts.Where(x => x.IsValid).Distinct().ToList();
            RainPortSymbols = RainPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            {
                var d = new Dictionary<GRect, string>();
                for (int i = BATHYDRACONIDAE; i < WaterPorts.Count; i++)
                {
                    var well = WaterPorts[i];
                    var label = WaterPortLabels[i];
                    if (!string.IsNullOrWhiteSpace(label) || !d.ContainsKey(well))
                    {
                        d[well] = label;
                    }
                }
                WaterPorts.Clear();
                WaterPortLabels.Clear();
                foreach (var kv in d)
                {
                    WaterPorts.Add(kv.Key);
                    WaterPortLabels.Add(kv.Value);
                }
            }
            WaterWells = WaterWells.Where(x => x.IsValid).Distinct().ToList();
            WashingMachines = WashingMachines.Where(x => x.IsValid).Distinct().ToList();
            Basins = Basins.Where(x => x.IsValid).Distinct().ToList();
            MopPools = MopPools.Where(x => x.IsValid).Distinct().ToList();
            PipeKillers = PipeKillers.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipeRadius = WrappingPipeRadius.Distinct().ToList();
            SideFloorDrains = SideFloorDrains.Distinct().ToList();
            WrappingPipeLabelLines = WrappingPipeLabelLines.Distinct().ToList();
        }
        public DrainageGeoData Clone()
        {
            return (DrainageGeoData)MemberwiseClone();
        }
        public DrainageGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<DrainageGeoData>();
        }
    }
    public class DrainageCadData
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
        public List<Geometry> WaterWells;
        public List<Geometry> WashingMachines;
        public List<Geometry> CleaningPorts;
        public List<Geometry> SideFloorDrains;
        public List<Geometry> PipeKillers;
        public List<Geometry> Basins;
        public List<Geometry> MopPools;
        public List<Geometry> DownWaterPorts;
        public List<Geometry> RainPortSymbols;
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
            WaterWells ??= new List<Geometry>();
            WashingMachines ??= new List<Geometry>();
            CleaningPorts ??= new List<Geometry>();
            SideFloorDrains ??= new List<Geometry>();
            PipeKillers ??= new List<Geometry>();
            Basins ??= new List<Geometry>();
            MopPools ??= new List<Geometry>();
            DownWaterPorts ??= new List<Geometry>();
            RainPortSymbols ??= new List<Geometry>();
        }
        public static DrainageCadData Create(DrainageGeoData data)
        {
            var bfSize = INTERLINGUISTICS;
            var o = new DrainageCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            if (THESAURUSESPECIALLY) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WLines.AddRange(data.WLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (THESAURUSESPECIALLY) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.WaterWells.AddRange(data.WaterWells.Select(ConvertWaterPortsF()));
            o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
            o.Basins.AddRange(data.Basins.Select(ConvertWashingMachinesF()));
            o.MopPools.AddRange(data.MopPools.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            o.PipeKillers.AddRange(data.PipeKillers.Select(x => x.ToPolygon()));
            o.DownWaterPorts.AddRange(data.DownWaterPorts.Select(x => x.ToPolygon()));
            o.RainPortSymbols.AddRange(data.RainPortSymbols.Select(x => x.ToPolygon()));
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
            return x => new GCircle(x, DISCOMFORTABLENESS).ToCirclePolygon(THESAURUSADMISSION);
        }
        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(PRAETERNATURALIS).ToCirclePolygon(PARALLELOGRAMMIC);
        }
        private static Func<GRect, Polygon> ConvertWaterPortsF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertFloorDrainsF()
        {
            return x => x.ToGCircle(THESAURUSNEGATIVE).ToCirclePolygon(THESAURUSADMISSION);
        }
        public static Func<GRect, Polygon> ConvertVerticalPipesF()
        {
            return x => x.ToPolygon();
        }
        private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
        {
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(THESAURUSADMISSION);
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
            return x => x.Extend(THESAURUSCOUNCIL).ToLineString();
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
            ret.AddRange(WaterWells);
            ret.AddRange(WashingMachines);
            ret.AddRange(CleaningPorts);
            ret.AddRange(SideFloorDrains);
            ret.AddRange(PipeKillers);
            ret.AddRange(Basins);
            ret.AddRange(MopPools);
            ret.AddRange(DownWaterPorts);
            ret.AddRange(RainPortSymbols);
            return ret;
        }
        public List<DrainageCadData> SplitByStorey()
        {
            var lst = new List<DrainageCadData>(this.Storeys.Count);
            if (this.Storeys.Count == BATHYDRACONIDAE) return lst;
            var f = GeoFac.CreateIntersectsSelector(GetAllEntities());
            foreach (var storey in this.Storeys)
            {
                var objs = f(storey);
                var o = new DrainageCadData();
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
                o.WaterWells.AddRange(objs.Where(x => this.WaterWells.Contains(x)));
                o.WashingMachines.AddRange(objs.Where(x => this.WashingMachines.Contains(x)));
                o.CleaningPorts.AddRange(objs.Where(x => this.CleaningPorts.Contains(x)));
                o.SideFloorDrains.AddRange(objs.Where(x => this.SideFloorDrains.Contains(x)));
                o.PipeKillers.AddRange(objs.Where(x => this.PipeKillers.Contains(x)));
                o.Basins.AddRange(objs.Where(x => this.Basins.Contains(x)));
                o.MopPools.AddRange(objs.Where(x => this.MopPools.Contains(x)));
                o.DownWaterPorts.AddRange(objs.Where(x => this.DownWaterPorts.Contains(x)));
                o.RainPortSymbols.AddRange(objs.Where(x => this.RainPortSymbols.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public DrainageCadData Clone()
        {
            return (DrainageCadData)MemberwiseClone();
        }
    }
    public class StoreyInfo
    {
        public StoreyType StoreyType;
        public List<int> Numbers;
        public Point2d ContraPoint;
        public GRect Boundary;
    }
    public static class THDrainageService
    {
        public static Polygon ConvertToPolygon(Polyline pl)
        {
            if (pl.NumberOfVertices <= TEREBINTHINATED)
                return null;
            var list = new List<Point2d>();
            for (int i = BATHYDRACONIDAE; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                if (list.Count == BATHYDRACONIDAE || !Equals(pt, list.Last()))
                {
                    list.Add(pt);
                }
            }
            if (list.Count <= TEREBINTHINATED) return null;
            try
            {
                var tmp = list.Select(x => x.ToNTSCoordinate()).ToList(list.Count + PIEZOELECTRICAL);
                if (!tmp[BATHYDRACONIDAE].Equals(tmp[tmp.Count - PIEZOELECTRICAL]))
                {
                    tmp.Add(tmp[BATHYDRACONIDAE]);
                }
                var ring = new LinearRing(tmp.ToArray());
                return new Polygon(ring);
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }
        public static string TryParseWrappingPipeRadiusText(string text)
        {
            if (text == null) return null;
            var t = Regex.Replace(text, THESAURUSBYPASS, THESAURUSAMENITY);
            t = Regex.Replace(t, PROGNOSTICATIVE, CONTEMPTIBILITY);
            return t;
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
        public static string GetStoreyNumberString(BlockReference br)
        {
            var d = br.ObjectId.GetAttributesInBlockReference(THESAURUSNEGATIVE);
            d.TryGetValue(THESAURUSPERNICIOUS, out string ret);
            return ret;
        }
        public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() is THESAURUSATTENDANCE && x.IsDynamicBlock).ToList();
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
        public static string FixVerticalPipeLabel(string label)
        {
            if (label == null) return null;
            if (label.StartsWith(THESAURUSDEFICIENCY))
            {
                return label.Substring(THESAURUSINTELLECT);
            }
            if (label.StartsWith(THESAURUSEMBODY))
            {
                return label.Substring(TEREBINTHINATED);
            }
            return label;
        }
        public static bool IsNotedLabel(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return label.Contains(THESAURUSTORTURE) || label.Contains(THESAURUSINTRUDE);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label);
        }
        public static bool IsY1L(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return label.StartsWith(THESAURUSDELETE);
        }
        public static bool IsY2L(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return label.StartsWith(THESAURUSPERCEIVE);
        }
        public static bool IsNL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return label.StartsWith(CORRESPONDINGLY);
        }
        public static bool IsYL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return label.StartsWith(THESAURUSPUDDLE);
        }
        public static bool IsRainLabel(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label);
        }
        public static bool IsDrainageLabelText(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            if (IsFL0(label)) return THESAURUSESPECIALLY;
            static bool f(string label)
            {
                return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
            }
            return f(FixVerticalPipeLabel(label));
        }
        public const double THESAURUSCOUNCIL = .1;
        public const int BATHYDRACONIDAE = 0;
        public const bool THESAURUSESPECIALLY = false;
        public const int THESAURUSADMISSION = 36;
        public const bool THESAURUSNEGATIVE = true;
        public const int DISCOMFORTABLENESS = 40;
        public const int PRAETERNATURALIS = 1500;
        public const int PARALLELOGRAMMIC = 6;
        public const int THESAURUSREGARDING = 4096;
        public const string THESAURUSDICTIONARY = "TCH_PIPE";
        public const string NEUROTRANSMITTER = "W-RAIN-PIPE";
        public const int PIEZOELECTRICAL = 1;
        public const int TEREBINTHINATED = 2;
        public const double THESAURUSDISRUPTION = 10e5;
        public const string THESAURUSDECORATIVE = "W-BUSH-NOTE";
        public const int QUOTATIONPATRONAL = 100;
        public const string THESAURUSINSPECTOR = "W-RAIN-EQPM";
        public const string QUOTATIONVENICE = "TCH_VPIPEDIM";
        public const string CONTEMPTIBILITY = "-";
        public const string THESAURUSMANIFEST = "TCH_TEXT";
        public const string THESAURUSELUCIDATE = "TCH_MTEXT";
        public const string THESAURUSMERRIMENT = "TCH_MULTILEADER";
        public const string THESAURUSAMENITY = "";
        public const int THESAURUSFORTIFICATION = 50;
        public const char THESAURUSSLOPPY = '|';
        public const string THESAURUSMAJESTY = "|";
        public const char THESAURUSRECKON = '$';
        public const string THESAURUSMALICE = "$";
        public const string THESAURUSCOMPREHEND = "可见性";
        public const string THESAURUSSUPPOSITION = "套管";
        public const int CONSTRUCTIONISM = 1000;
        public const string THESAURUSBALDERDASH = "带定位立管";
        public const string THESAURUSBAPTIZE = "立管编号";
        public const string THESAURUSLOVING = "$LIGUAN";
        public const string THESAURUSCANDLE = "A$C6BDE4816";
        public const string QUOTATIONADJACENT = "污废合流井编号";
        public const string THESAURUSOBJECTIVELY = "W-DRAI-DOME-PIPE";
        public const string THESAURUSSPELLBOUND = "W-RAIN-NOTE";
        public const string THESAURUSCOURTESAN = "W-RAIN-DIMS";
        public const string QUOTATIONPEIRCE = "TCH_EQUIPMENT";
        public const char THESAURUSFILIGREE = 'B';
        public const string THESAURUSINSURANCE = "RF";
        public const string THESAURUSBLACKOUT = "RF+1";
        public const string HYDROMETALLURGY = "RF+2";
        public const string PHENYLENEDIAMINE = "F";
        public const int INTERLINGUISTICS = 10;
        public const int PSYCHOPHYSIOLOGICAL = 400;
        public const string THESAURUSDEFILE = "1F";
        public const string THESAURUSSUSTAIN = "-1F";
        public const string THESAURUSGOODNESS = "W-NOTE";
        public const string THESAURUSRECRIMINATION = "W-DRAI-EQPM";
        public const string THESAURUSEXCOMMUNICATE = "W-BUSH";
        public const string THESAURUSSMASHING = "W-DRAI-NOTE";
        public const double HYPERCHOLESTERO = 2500.0;
        public const double QUOTATIONCOLLARED = 5500.0;
        public const double THESAURUSRECAPITULATE = 1800.0;
        public const string THESAURUSACQUIRE = "伸顶通气2000";
        public const string THESAURUSASTUTE = "伸顶通气500";
        public const string THESAURUSGENTILITY = "可见性1";
        public const string THESAURUSCENSOR = "通气帽系统";
        public const string PALAEOGEOMORPHOLOGY = "距离1";
        public const string THESAURUSREPREHENSIBLE = "伸顶通气管";
        public const string STOICHIOMETRICALLY = "1000";
        public const int SUPERCILIOUSNESS = 580;
        public const string THESAURUSINCIDENTAL = "标高";
        public const int COMMENSURATENESS = 550;
        public const string THESAURUSVERIFICATION = "建筑完成面";
        public const int THESAURUSCONFECTIONERY = 1800;
        public const int THESAURUSSETBACK = 121;
        public const int GASTRONOMICALLY = 1258;
        public const int THESAURUSOBLITERATION = 120;
        public const int THESAURUSBEWARE = 779;
        public const int THESAURUSPROLONG = 1679;
        public const int ANTHROPOMORPHITES = 658;
        public const int QUOTATIONSORCERER = 90;
        public const int CHLOROFLUOROCARBONS = 800;
        public const int THESAURUSBEFRIEND = 500;
        public const int THESAURUSSPREAD = 1879;
        public const int THESAURUSDOWNHEARTED = 180;
        public const int THESAURUSUNAVOIDABLE = 160;
        public const string PSYCHOLINGUISTICALLY = "普通地漏无存水弯";
        public const string THESAURUSBENEFIT = "*";
        public const double THESAURUSINDEMNIFY = 0.0;
        public const int THESAURUSADMIRABLE = 780;
        public const int CHEMOTROPICALLY = 700;
        public const double THESAURUSDISABILITY = .0;
        public const int DIETHYLSTILBOESTROL = 300;
        public const int THESAURUSINTRACTABLE = 24;
        public const int THESAURUSABUNDANCE = 9;
        public const int DOLICHOCEPHALOUS = 5;
        public const int THESAURUSEVINCE = 4;
        public const int THESAURUSINTELLECT = 3;
        public const int QUOTATIONLUNGEING = 3600;
        public const int REVOLUTIONIZATION = 350;
        public const int THESAURUSTRAGEDY = 150;
        public const int PROCHLORPERAZINE = 318;
        public const int THESAURUSDEFICIT = 1400;
        public const int QUOTATIONCHOROID = 1200;
        public const int THESAURUSIMAGINATIVE = 2000;
        public const int THESAURUSSUCCINCT = 600;
        public const int THESAURUSLABYRINTHINE = 479;
        public const int THESAURUSAPPORTION = 750;
        public const string THESAURUSSPITEFUL = "DN100";
        public const int THESAURUSDEVICE = 950;
        public const int UNCONJECTURABLE = 200;
        public const int QUINQUARTICULARIS = 360;
        public const int THESAURUSMANKIND = 650;
        public const int THESAURUSDISPUTE = 30;
        public const string THESAURUSCONSUME = "≥600";
        public const int THESAURUSUNDERWATER = 450;
        public const int THESAURUSYAWNING = 18;
        public const int HYPERSENSITIZED = 250;
        public const double THESAURUSADVANCEMENT = .7;
        public const string UNCOMPANIONABLE = ";";
        public const double THESAURUSPROPRIETOR = .01;
        public const string THESAURUSCOMMUTE = "地漏系统";
        public const string THESAURUSEXTEMPORE = "TH-STYLE3";
        public const int THESAURUSNOTIFY = 745;
        public const string THESAURUSCONSENT = "乙字弯";
        public const string QUOTATIONEMBRYOID = "立管检查口";
        public const string THESAURUSREPRESSION = "套管系统";
        public const string THESAURUSASSIGN = "重力流雨水井编号";
        public const string MICROSPECTROPHOTOMETRY = "666";
        public const string THESAURUSLUGGAGE = ",";
        public const int PERCHLOROETHYLENE = 7;
        public const int THESAURUSJAGGED = 229;
        public const int THESAURUSENDING = 230;
        public const int MINERALOCORTICOID = 8192;
        public const int THESAURUSRETAIN = 8000;
        public const int HOOGMOGENDHEIDEN = 15;
        public const string THESAURUSMISOGYNIST = "FromImagination";
        public const int STANDOFFISHNESS = 55;
        public const string CH2OHRCHNH2RCOOH = "X.XX";
        public const string THESAURUSLAYOUT = "排出：";
        public const int THESAURUSBESTRIDE = 666;
        public const string INTERSEGMENTALLY = "排出套管：";
        public const string OVERCURIOUSNESS = "WaterWellWrappingPipeRadiusStringDict:";
        public const int MELANCHOLIOUSNESS = 255;
        public const int THESAURUSFELLOW = 0x91;
        public const int THESAURUSBEDEVIL = 0xc7;
        public const int THESAURUSTESTIMONY = 0xae;
        public const int HYDROXYNAPHTHALENE = 211;
        public const int THESAURUSFOLLOWER = 213;
        public const int THESAURUSACCORDANCE = 111;
        public const string THESAURUSHUMORIST = "AI-空间框线";
        public const string THESAURUSCOMPLEX = "AI-空间名称";
        public const string THESAURUSDEFICIENCY = "73-";
        public const string THESAURUSEMBODY = "1-";
        public const string THESAURUSBYPASS = @"[^\d\.\-]";
        public const string PROGNOSTICATIVE = @"\d+\-";
        public const string TRANSUBSTANTIALIS = "楼层类型";
        public const char DELETERIOUSNESS = '，';
        public const char INSTITUTIONALISM = ',';
        public const char THESAURUSOVERACT = '-';
        public const string UNCONSTITUTIONAL = "M";
        public const string THESAURUSUNRELENTING = " ";
        public const string THESAURUSAFFILIATION = @"(\-?\d+)-(\-?\d+)";
        public const string DETERMINATIVENESS = @"\-?\d+";
        public const string THESAURUSPERNICIOUS = "楼层编号";
        public const string THESAURUSATTENDANCE = "楼层框定";
        public const string THESAURUSHORRIFY = "基点 X";
        public const string THESAURUSFURTHEST = "基点 Y";
        public const string THESAURUSKNICKERS = "error occured while getting baseX and baseY";
        public const string THESAURUSPRELUDE = "卫生间";
        public const string THESAURUSSEVERITY = "主卫";
        public const string LUCIOCEPHALIDAE = "公卫";
        public const string THESAURUSLUSTFUL = "次卫";
        public const string THESAURUSSATIATE = "客卫";
        public const string PROSELYTIZATION = "洗手间";
        public const string UNCONSCIENTIOUS = "卫";
        public const string THESAURUSEMPLOYMENT = @"^[卫]\d$";
        public const string THESAURUSNESTLE = "厨房";
        public const string THESAURUSCOMMUNE = "西厨";
        public const string THESAURUSSHOWER = "厨";
        public const string THESAURUSOUTCRY = "阳台";
        public const string THESAURUSCIGARETTE = "连廊";
        public const string THESAURUSDELETE = "Y1L";
        public const string THESAURUSPERCEIVE = "Y2L";
        public const string CORRESPONDINGLY = "NL";
        public const string THESAURUSPUDDLE = "YL";
        public const string PARALINGUISTICALLY = @"^W\d?L";
        public const string THESAURUSBLISTER = @"^F\d?L";
        public const string THESAURUSBURNING = "-0";
        public const string THESAURUSEFFUSION = @"^P\d?L";
        public const string THESAURUSEMIGRATE = @"^T\d?L";
        public const string ANTHROPOMORPHISM = @"^D\d?L";
        public const string QUOTATIONASSERTORY = @"^(F\d?L|T\d?L|P\d?L|D\d?L)(\w*)\-(\w*)([a-zA-Z]*)$";
        public const double THESAURUSSATISFY = 383875.8169;
        public const double QUOTATIONRASCHIG = 250561.9571;
        public const string THESAURUSPURIFY = "P型存水弯";
        public const string THESAURUSCONDITION = "板上P弯";
        public const int PSYCHOGERIATRIC = 3500;
        public const int HETEROTRANSPLANTED = 1479;
        public const int THESAURUSORIGINATE = 2379;
        public const int PSYCHOGENICALLY = 1779;
        public const int THESAURUSSUPPLEMENTARY = 579;
        public const int TRISYLLABICALLY = 279;
        public const string THESAURUSFRIVOLITY = "双池S弯";
        public const string THESAURUSDECISIVE = "W-DRAI-VENT-PIPE";
        public const string THESAURUSTREASONABLE = "DN50";
        public const string THESAURUSDEFENSIVE = "DN75";
        public const int THESAURUSCHANNEL = 789;
        public const int ALSOHEAVENWARDS = 1270;
        public const int THESAURUSFABULOUS = 1090;
        public const string INSTITUTIONALIZED = "双池P弯";
        public const int INCREDULOUSNESS = 5479;
        public const int THESAURUSINTEMPERATE = 1079;
        public const int THESAURUSSEDATE = 5600;
        public const int THESAURUSOMNIVOROUS = 6079;
        public const int THESAURUSMYTHICAL = 8;
        public const int THESAURUSVACILLATE = 1379;
        public const int THESAURUSWITHIN = 569;
        public const int THESAURUSUTILITY = 406;
        public const int THESAURUSCHASTITY = 404;
        public const int THESAURUSJUDGEMENT = 3150;
        public const int QUOTATIONSCORIA = 12;
        public const int THESAURUSWORKSHOP = 1300;
        public const double QUOTATIONBITUMINOUS = .4;
        public const string THESAURUSMISJUDGE = "接阳台洗手盆排水";
        public const string CALYMMATOBACTERIUM = "DN50，余同";
        public const int PHILANTHROPICALLY = 1190;
        public const string STERNOPTYCHIDAE = "接卫生间排水管";
        public const string MISAPPREHENSIVENESS = "DN100，余同";
        public const int THESAURUSCORDIAL = 490;
        public const int THESAURUSCONCEIVE = 170;
        public const int THESAURUSSCENERY = 2830;
        public const int DNIPRODZERZHINSK = 900;
        public const int THESAURUSPROFFER = 330;
        public const int THESAURUSRELEASE = 895;
        public const int THESAURUSUNABLE = 285;
        public const int THESAURUSENTHUSE = 390;
        public const string THESAURUSMELODIOUS = "普通地漏P弯";
        public const int THESAURUSVERTIGO = 1330;
        public const int INTERMINABLENESS = 270;
        public const string THESAURUSOFFENDER = "接厨房洗涤盆排水";
        public const int ELECTRODYNAMICAL = 156;
        public const int THESAURUSCONSERVATIVE = 510;
        public const int THESAURUSSHIFTLESS = 389;
        public const int QUOTATIONSHIRTSLEEVE = 45;
        public const int CORTICOSTEROIDS = 669;
        public const int THESAURUSDESTITUTION = 590;
        public const int THESAURUSSLIVER = 1700;
        public const int QUOTATIONDERNIER = 919;
        public const int MECHANORECEPTION = 990;
        public const int THESAURUSCRITICIZE = 129;
        public const int INACCESSIBILITY = 693;
        public const int UNCOMPREHENDING = 1591;
        public const int THESAURUSPERSUASION = 511;
        public const int THESAURUSINSECTICIDE = 289;
        public const string THESAURUSRESIGN = "W-DRAI-DIMS";
        public const int QUOTATIONARENACEOUS = 1391;
        public const int LACKADAISICALNESS = 667;
        public const int MAGNETOHYDRODYNAMICS = 1450;
        public const int THESAURUSBRAVADO = 251;
        public const int VERGISSMEINNICHT = 660;
        public const int THESAURUSSMUDGE = 110;
        public const int THESAURUSPROGRAMME = 91;
        public const int THESAURUSEXPIRE = 320;
        public const int CONVENTIONALIZE = 427;
        public const int INDISCRIMINATENESS = 183;
        public const int INFINITESIMALLY = 283;
        public const double QUOTATIONSENECA = 250.0;
        public const int METHYLCARBAMATE = 225;
        public const int CONSERVATIONIST = 1125;
        public const int THESAURUSINTESTINAL = 499;
        public const int CINEFLUOROGRAPHY = 280;
        public const int OBSERVATIONALLY = 76;
        public const int THESAURUSDETERIORATE = 424;
        public const int RECRYSTALLIZATION = 1900;
        public const string THESAURUSTUSSLE = "DN100乙字弯";
        public const string THESAURUSHEARTY = "1350";
        public const int THESAURUSCOMMAND = 275;
        public const int SUPERESSENTIALIS = 210;
        public const int METAMATHEMATICS = 151;
        public const int THESAURUSMODIFY = 1109;
        public const int THESAURUSSTEEPLE = 420;
        public const int THESAURUSSATURATED = 447;
        public const int THESAURUSREBUKE = 43;
        public const int THESAURUSMEETING = 237;
        public const string THESAURUSQUIETLY = "洗衣机地漏P弯";
        public const int QUOTATION1AUNIPOLAR = 1380;
        public const double OBJECTIONABLENESS = 200.0;
        public const double PALAEOICHTHYOLOGY = 780.0;
        public const double MICRODENSITOMETRY = 130.0;
        public const int THESAURUSINTANGIBLE = 980;
        public const int THESAURUSEXPERIENCED = 1358;
        public const int THESAURUSLEANING = 172;
        public const int THESAURUSEXCESSIVE = 155;
        public const int THESAURUSPOIGNANT = 1650;
        public const int THESAURUSCONFOUND = 71;
        public const int TETRAHYDROXYHEXANEDIOIC = 221;
        public const int INTERFEROMETERS = 29;
        public const int THESAURUSFULSOME = 1158;
        public const int DISCONNECTEDNESS = 179;
        public const int THESAURUSFEARLESS = 880;
        public const string THESAURUSDEGREE = ">1500";
        public const int DISILLUSIONIZER = 921;
        public const int THESAURUSCOOPERATION = 2320;
        public const double THESAURUSEXPENDABLE = .2778;
        public const string THESAURUSPRESENCE = "普通地漏S弯";
        public const int THESAURUSSALUTATION = 3090;
        public const int THESAURUSSENSATION = 371;
        public const int THESAURUSHOUSING = 2730;
        public const int THESAURUSHIGHLY = 888;
        public const int DENOMINATIONALIZE = 460;
        public const int INTELLIGIBILITY = 2499;
        public const int QUOTATIONHODDEN = 1210;
        public const int THESAURUSEULOGY = 850;
        public const int THESAURUSESTEEM = 2279;
        public const int THESAURUSDAMAGE = 1239;
        public const int THESAURUSCUDDLE = 675;
        public const string INFUNDIBULIFORM = "drainage_drawing_ctx";
        public const string THESAURUSEXPUNGE = "通气帽系统-AI";
        public const string ARCHIPRESBYTERATUS = "暂不支持污废分流";
        public const string THESAURUSHARVEST = "PL";
        public const string ETHELOTHRĒSKEIA = "TL";
        public const string THESAURUSOBSESS = "管底h+X.XX";
        public const string QUOTATIONCRIMEAN = "双格洗涤盆排水-AI";
        public const string THESAURUSDISCRETE = "S型存水弯";
        public const string THESAURUSCLIENTELE = "板上S弯";
        public const string PHOTOMECHANICAL = "翻转状态";
        public const string MAGNANIMOUSNESS = "清扫口系统";
        public const int THESAURUSMALODOROUS = 21;
        public const string THESAURUSSTENCH = "-DRAI-";
        public const string THESAURUSDEFAME = "AI-房间框线";
        public const string THESAURUSLIBIDO = "AI-房间名称";
        public const string THESAURUSMARKET = "W-DRAI-OUT-PIPE";
        public const string THESAURUSINCORPORATE = "WP_KTN_LG";
        public const string INTERPUNCTUATION = "De";
        public const string THESAURUSHANDICRAFT = "wb";
        public const string THESAURUSNONCONFORMIST = "kd";
        public const string THESAURUSNUTRITIOUS = "C-XREF-EXT";
        public const string THESAURUSCOUNTRY = "雨水井";
        public const string QUOTATIONARCTIC = "地漏平面";
        public const int SENTIMENTALISING = 60;
        public const string THESAURUSETHEREAL = "A$C58B12E6E";
        public const string THESAURUSEPITAPH = "W-DRAI-PIEP-RISR";
        public const string ALSOPREPONDERANCY = "A$C5E4A3C21";
        public const string THESAURUSADDICTION = "PIPE-喷淋";
        public const string UNINTERMITTENTLY = "A-Kitchen-3";
        public const string THESAURUSAPPETIZING = "A-Kitchen-4";
        public const string DIPTEROCARPACEAE = "A-Toilet-1";
        public const string QUOTATIONDENNIS = "A-Toilet-2";
        public const string PALAEOICHTHYOLOGIST = "A-Toilet-3";
        public const string THESAURUSBONDAGE = "A-Toilet-4";
        public const string THESAURUSINQUIRY = "-XiDiPen-";
        public const string THESAURUSCOMPETITOR = "A-Kitchen-9";
        public const string INOMETHYLCYCLOH = "0$座厕";
        public const string EXEMPLIFICATIONAL = "0$asdfghjgjhkl";
        public const string CALLITRICHACEAE = "A-Toilet-";
        public const string THESAURUSPROTECTION = "A-Kitchen-";
        public const string THESAURUSDISAPPOINTMENT = "|lp";
        public const string THESAURUSDISPERSE = "|lp1";
        public const string THESAURUSAFFILIATE = "|lp2";
        public const string THESAURUSBRACKET = "A-Toilet-9";
        public const string CATEGOREMATICALLY = "$xiyiji";
        public const string THESAURUSFAMILIAR = "feng_dbg_test_washing_machine";
        public const string THESAURUSMERCHANT = "ShortTranslatorLabels：";
        public const string CHARACTERISTICAL = "LongTranslatorLabels：";
        public const string THESAURUSBESTOW = "VerticalPipeLabels:";
        public const string THESAURUSORIGIN = "ToiletPls:";
        public const string THESAURUSCOMPARATIVE = "KitchenFls:";
        public const string THESAURUSOPAQUE = "BalconyFls:";
        public const string THESAURUSAPPRECIATION = "多通道";
        public const string THESAURUSENFORCEMENT = "洗衣机";
        public const string ALSOASTEREOGNOSIA = "FloorDrains：";
        public const string THESAURUSJEALOUSY = "SingleOutletFloorDrains：";
        public const string NEUROHYPOPHYSIS = "Shunts：";
        public const string THESAURUSCREATOR = "circlesCount ";
        public const string THESAURUSHONOUR = "Merges ";
        public const string THESAURUSPOSSIBILITY = "drainage_drDatas";
        public const string THESAURUSTORTURE = "单排";
        public const string THESAURUSINTRUDE = "设置乙字弯";
        public const string THESAURUSEXULTATION = "-0.XX";
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
        public static bool IsBalcony(string roomName)
        {
            if (roomName == null) return THESAURUSESPECIALLY;
            var roomNameContains = new List<string> { THESAURUSOUTCRY };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSESPECIALLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSNEGATIVE;
            return THESAURUSESPECIALLY;
        }
        public static bool IsCorridor(string roomName)
        {
            if (roomName == null) return THESAURUSESPECIALLY;
            var roomNameContains = new List<string> { THESAURUSCIGARETTE };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSESPECIALLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSNEGATIVE;
            return THESAURUSESPECIALLY;
        }
        public static bool IsWL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, PARALINGUISTICALLY);
        }
        public static bool IsFL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, THESAURUSBLISTER);
        }
        public static bool IsFL0(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return IsFL(label) && label.Contains(THESAURUSBURNING);
        }
        public static bool IsPL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, THESAURUSEFFUSION);
        }
        public static bool IsTL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, THESAURUSEMIGRATE);
        }
        public static bool IsDL(string label)
        {
            if (label == null) return THESAURUSESPECIALLY;
            return Regex.IsMatch(label, ANTHROPOMORPHISM);
        }
        public class ThModelExtractionVisitor : ThDistributionElementExtractionVisitor
        {
            public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Autodesk.AutoCAD.DatabaseServices.Entity dbObj, Matrix3d matrix)
            {
                if (dbObj is BlockReference blkref)
                {
                    HandleBlockReference(elements, blkref, matrix);
                }
            }
            public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
            {
            }
            private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
            {
                return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
            }
            public override bool IsDistributionElement(Entity entity)
            {
                if (entity is BlockReference reference)
                {
                    if (reference.GetEffectiveName().Contains(THESAURUSBRACKET))
                    {
                        using var adb = AcadDatabase.Use(reference.Database);
                        if (IsVisibleLayer(adb.Layers.Element(reference.Layer)))
                            return THESAURUSNEGATIVE;
                    }
                }
                return THESAURUSESPECIALLY;
            }
            public override bool CheckLayerValid(Entity curve)
            {
                return THESAURUSNEGATIVE;
            }
            private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
            {
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Geometry = blkref.GetTransformedCopy(matrix),
                });
            }
        }
    }
    public static class GeometryExtensions
    {
        public static Geometry Clone(this Geometry geo)
        {
            if (geo is null) return null;
            if (geo is Point pt) return Clone(pt);
            if (geo is LineString ls) return Clone(ls);
            if (geo is Polygon pl) return Clone(pl);
            if (geo is MultiPoint mpt) return new MultiPoint(mpt.Geometries.Cast<Point>().Select(Clone).ToArray());
            if (geo is MultiLineString mls) return new MultiLineString(mls.Geometries.Cast<LineString>().Select(Clone).ToArray());
            if (geo is MultiPolygon mpl) return new MultiPolygon(mpl.Geometries.Cast<Polygon>().Select(Clone).ToArray());
            throw new NotSupportedException();
        }
        public static Coordinate Clone(Coordinate o) => new Coordinate(o.X, o.Y);
        public static Point Clone(Point o) => new Point(o.X, o.Y);
        public static LineString Clone(LineString o) => new LineString(o.Coordinates);
        public static Polygon Clone(Polygon o) => new Polygon(o.Shell);
        public static IEnumerable<Point> Clone(this IEnumerable<Point> geos) => geos.Select(Clone);
        public static IEnumerable<LineString> Clone(this IEnumerable<LineString> geos) => geos.Select(Clone);
        public static IEnumerable<Polygon> Clone(this IEnumerable<Polygon> geos) => geos.Select(Clone);
        public static IEnumerable<Geometry> ToBaseGeometries(this Geometry geometry)
        {
            if (geometry is Point or LineString or Polygon)
            {
                yield return geometry;
            }
            else if (geometry is GeometryCollection colle)
            {
                foreach (var geo in colle.Geometries)
                {
                    foreach (var r in ToBaseGeometries(geo))
                    {
                        yield return r;
                    }
                }
            }
        }
    }
}
namespace ThMEPWSS.Diagram.ViewModel
{
    using AcHelper;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using Linq2Acad;
    using NFox.Cad;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using ThControlLibraryWPF.ControlUtils;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe;
    using ThMEPWSS.Uitl;
    using static ThMEPWSS.ReleaseNs.DrainageSystemNs.THDrainageService;
    [Serializable]
    public class BaseClone<T>
    {
        public virtual T Clone()
        {
            var memoryStream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, this);
            memoryStream.Position = BATHYDRACONIDAE;
            return (T)formatter.Deserialize(memoryStream);
        }
    }
    [Serializable]
    public class RainSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        private List<string> _FloorListDatas = new List<string>();
        public List<string> FloorListDatas
        {
            get
            {
                return _FloorListDatas;
            }
            set
            {
                _FloorListDatas = value;
                this.RaisePropertyChanged();
            }
        }
        private RainSystemDiagramParamsViewModel _Params = new RainSystemDiagramParamsViewModel();
        public RainSystemDiagramParamsViewModel Params
        {
            get
            {
                return _Params;
            }
            set
            {
                _Params = value;
                this.RaisePropertyChanged();
            }
        }
        public void InitFloorListDatas(bool focus)
        {
            if (DateTime.Now == DateTime.MinValue) FloorListDatas = SystemDiagramUtils.GetFloorListDatas();
            else
            {
                try
                {
                    ThMEPWSS.ReleaseNs.RainSystemNs.ThRainService.CollectFloorListDatasEx(focus);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
    [Serializable]
    public class DrainageSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        private List<string> _FloorListDatas = new List<string>();
        public List<string> FloorListDatas
        {
            get
            {
                return _FloorListDatas;
            }
            set
            {
                _FloorListDatas = value;
                this.RaisePropertyChanged();
            }
        }
        private DrainageSystemDiagramParamsViewModel _Params = new DrainageSystemDiagramParamsViewModel();
        public DrainageSystemDiagramParamsViewModel Params
        {
            get
            {
                return _Params;
            }
            set
            {
                _Params = value;
                this.RaisePropertyChanged();
            }
        }
        public void CollectFloorListDatas(bool focus)
        {
            try
            {
                if (DateTime.Now == DateTime.MinValue)
                {
                    Pipe.Service.ThDrainageService.CollectFloorListDatas();
                }
                else
                {
                    ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.CollectFloorListDatasEx(focus);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
    [Serializable]
    public class DrainageSystemDiagramParamsViewModel : NotifyPropertyChangedBase
    {
        public void CopyTo(DrainageSystemDiagramParamsViewModel other)
        {
            foreach (var pi in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                pi.SetValue(other, pi.GetValue(this));
            }
        }
        public DrainageSystemDiagramParamsViewModel Clone()
        {
            return this.ToJson().FromJson<DrainageSystemDiagramParamsViewModel>();
        }
        private double _StoreySpan = THESAURUSCONFECTIONERY; 
        public double StoreySpan
        {
            get
            {
                if (_StoreySpan <= BATHYDRACONIDAE)
                {
                    _StoreySpan = THESAURUSIMAGINATIVE;
                    this.RaisePropertyChanged();
                }
                return _StoreySpan;
            }
            set
            {
                _StoreySpan = value;
                this.RaisePropertyChanged();
            }
        }
        private string _WashingMachineFloorDrainDN = THESAURUSTREASONABLE;
        public string WashingMachineFloorDrainDN
        {
            get
            {
                return _WashingMachineFloorDrainDN;
            }
            set
            {
                _WashingMachineFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _OtherFloorDrainDN = THESAURUSTREASONABLE;
        public string OtherFloorDrainDN
        {
            get
            {
                return _OtherFloorDrainDN;
            }
            set
            {
                _OtherFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _DirtyWaterWellDN = THESAURUSTREASONABLE;
        public string DirtyWaterWellDN
        {
            get
            {
                return _DirtyWaterWellDN;
            }
            set
            {
                _DirtyWaterWellDN = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _ShouldRaiseWashingMachine = THESAURUSESPECIALLY;
        public bool ShouldRaiseWashingMachine
        {
            get
            {
                return _ShouldRaiseWashingMachine;
            }
            set
            {
                _ShouldRaiseWashingMachine = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _CouldHavePeopleOnRoof = THESAURUSNEGATIVE;
        public bool CouldHavePeopleOnRoof
        {
            get
            {
                return _CouldHavePeopleOnRoof;
            }
            set
            {
                _CouldHavePeopleOnRoof = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _CanHaveDownboard = THESAURUSNEGATIVE;
        public bool CanHaveDownboard
        {
            get
            {
                return _CanHaveDownboard;
            }
            set
            {
                _CanHaveDownboard = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _通气H件隔层布置;
        public bool 通气H件隔层布置
        {
            get
            {
                return _通气H件隔层布置;
            }
            set
            {
                _通气H件隔层布置 = value;
                this.RaisePropertyChanged();
            }
        }
        private string _厨房洗涤盆 = THESAURUSFRIVOLITY;
        public string 厨房洗涤盆
        {
            get
            {
                return _厨房洗涤盆;
            }
            set
            {
                _厨房洗涤盆 = value;
                this.RaisePropertyChanged();
            }
        }
    }
    [Serializable]
    public class RainSystemDiagramParamsViewModel : NotifyPropertyChangedBase
    {
        public void CopyTo(RainSystemDiagramParamsViewModel other)
        {
            foreach (var pi in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                pi.SetValue(other, pi.GetValue(this));
            }
        }
        public RainSystemDiagramParamsViewModel Clone()
        {
            return this.ToJson().FromJson<RainSystemDiagramParamsViewModel>();
        }
        private double _StoreySpan = THESAURUSCONFECTIONERY; 
        public double StoreySpan
        {
            get
            {
                if (_StoreySpan <= BATHYDRACONIDAE)
                {
                    _StoreySpan = THESAURUSIMAGINATIVE;
                    this.RaisePropertyChanged();
                }
                return _StoreySpan;
            }
            set
            {
                _StoreySpan = value;
                this.RaisePropertyChanged();
            }
        }
        private string _BalconyFloorDrainDN = THESAURUSTREASONABLE;
        public string BalconyFloorDrainDN
        {
            get
            {
                return _BalconyFloorDrainDN;
            }
            set
            {
                _BalconyFloorDrainDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _CondensePipeDN = THESAURUSTREASONABLE;
        public string CondensePipeVerticalDN
        {
            get
            {
                return _CondensePipeDN;
            }
            set
            {
                _CondensePipeDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _CondensePipeHorizontalDN = THESAURUSTREASONABLE;
        public string CondensePipeHorizontalDN
        {
            get
            {
                return _CondensePipeHorizontalDN;
            }
            set
            {
                _CondensePipeHorizontalDN = value;
                this.RaisePropertyChanged();
            }
        }
        private string _BalconyRainPipeDN = THESAURUSSPITEFUL;
        public string BalconyRainPipeDN
        {
            get
            {
                return _BalconyRainPipeDN;
            }
            set
            {
                _BalconyRainPipeDN = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _HasAirConditionerFloorDrain = THESAURUSESPECIALLY;
        public bool HasAirConditionerFloorDrain
        {
            get
            {
                return _HasAirConditionerFloorDrain;
            }
            set
            {
                _HasAirConditionerFloorDrain = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _HasAiringForCondensePipe = THESAURUSESPECIALLY;
        public bool HasAiringForCondensePipe
        {
            get
            {
                return _HasAiringForCondensePipe;
            }
            set
            {
                _HasAiringForCondensePipe = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _CouldHavePeopleOnRoof = THESAURUSNEGATIVE;
        public bool CouldHavePeopleOnRoof
        {
            get
            {
                return _CouldHavePeopleOnRoof;
            }
            set
            {
                _CouldHavePeopleOnRoof = value;
                this.RaisePropertyChanged();
            }
        }
    }
}