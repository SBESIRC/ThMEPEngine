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
    using StoreyContext = Pipe.Model.StoreyContext;
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
                    if (circlesCount > PHOTOGONIOMETER)
                    {
                        return FlCaseEnum.Case1;
                    }
                    if (circlesCount == PHOTOGONIOMETER)
                    {
                        return FlCaseEnum.Case2;
                    }
                }
                else
                {
                    if (circlesCount == PHOTOGONIOMETER)
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
                    if (circlesCount > PHOTOGONIOMETER)
                    {
                        return FlFixType.MiddleHigher;
                    }
                    if (circlesCount == PHOTOGONIOMETER)
                    {
                        return FlFixType.Lower;
                    }
                }
                else
                {
                    if (circlesCount == PHOTOGONIOMETER)
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
                    if (circlesCount > PHOTOGONIOMETER)
                    {
                        return PlCaseEnum.Case1;
                    }
                    if (circlesCount == PHOTOGONIOMETER)
                    {
                        return PlCaseEnum.Case1;
                    }
                }
                else
                {
                    if (circlesCount == PHOTOGONIOMETER)
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
        static readonly Regex re = new Regex(THESAURUSENGRAVING);
        public static LabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new LabelItem()
            {
                Label = label,
                Prefix = m.Groups[ADRENOCORTICOTROPHIC].Value,
                D1S = m.Groups[PHOTOGONIOMETER].Value,
                D2S = m.Groups[THESAURUSPOSTSCRIPT].Value,
                Suffix = m.Groups[THESAURUSTITTER].Value,
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
                return NARCOTRAFICANTE;
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
            return NARCOTRAFICANTE;
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
        public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > NARCOTRAFICANTE;
        public List<DrainageGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public int MinTl;
        public int MaxTl;
        public bool HasTl => TlLabels != null && TlLabels.Count > NARCOTRAFICANTE;
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
                var s = NARCOTRAFICANTE;
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
        public static void DrawWashingMachineRaisingSymbol(Point2d bsPt, bool isLeftOrRight)
        {
            if (isLeftOrRight)
            {
                var v = new Vector2d(THESAURUSUNTIRING, -THESAURUSPRESCRIPTION);
                DrawBlockReference(THESAURUSENDOWMENT, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSUNDERSTATE;
                    br.ScaleFactors = new Scale3d(PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, THESAURUSCOUNTER);
                    }
                });
            }
            else
            {
                var v = new Vector2d(-THESAURUSUNTIRING, -THESAURUSPRESCRIPTION);
                DrawBlockReference(THESAURUSENDOWMENT, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSUNDERSTATE;
                    br.ScaleFactors = new Scale3d(-PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, THESAURUSCOUNTER);
                    }
                });
            }
        }
        public static double LONG_TRANSLATOR_HEIGHT1 = QUOTATION1BRICKETY;
        public static double CHECKPOINT_OFFSET_Y = THESAURUSINEVITABLE;
        public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreyItem> storeysItems, Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys, DrainageSystemDiagramViewModel viewModel)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + QUOTATIONHOUSEMAID).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - ADRENOCORTICOTROPHIC;
            var end = NARCOTRAFICANTE;
            var OFFSET_X = THESAURUSWOMANLY;
            var SPAN_X = THESAURUSCONTINUATION + HYDROSTATICALLY + THESAURUSATTRACTION;
            var HEIGHT = THESAURUSINFERENCE;
            {
                if (viewModel?.Params?.StoreySpan is double v)
                {
                    HEIGHT = v;
                }
            }
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSINFERENCE;
            var __dy = THESAURUSINTENTIONAL;
            DrawDrainageSystemDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, viewModel);
        }
        public class Opt
        {
            double fixY;
            double _dy;
            public List<Vector2d> vecs0 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -THESAURUSINSTEAD - dy) };
            public List<Vector2d> vecs1 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy + fixY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - _dy - dy - fixY) };
            public List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy - __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - _dy - dy + __dy) };
            public List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy + __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - _dy - dy - __dy) };
            public List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -THESAURUSESTRANGE - dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            public List<Vector2d> vecs3 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSHORRENDOUS - _dy - dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            public List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy - __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSHORRENDOUS - _dy - dy + __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            public List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSINSTEAD + dy), new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy + __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSHORRENDOUS - _dy - dy - __dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) };
            public List<Vector2d> vecs4 => vecs1.GetYAxisMirror();
            public List<Vector2d> vecs5 => vecs2.GetYAxisMirror();
            public List<Vector2d> vecs6 => vecs3.GetYAxisMirror();
            public List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
            public List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
            public List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
            public Vector2d vec7 => new Vector2d(-THESAURUSITINERANT, THESAURUSITINERANT);
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
                    var heights = new List<int>(allStoreys.Count);
                    var s = NARCOTRAFICANTE;
                    var _vm = FloorHeightsViewModel.Instance;
                    bool test(string x, int t)
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
                        string getStoreyHeightText()
                        {
                            if (storey is THESAURUSALCOHOLIC) return THESAURUSLUXURIANT;
                            var ret = (heights[i] / PSYCHOHISTORICAL).ToString(THESAURUSFLIGHT); ;
                            if (ret == THESAURUSFLIGHT) return THESAURUSLUXURIANT;
                            return ret;
                        }
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen, getStoreyHeightText());
                    }
                }
                void _DrawWrappingPipe(Point2d basePt, string shadow)
                {
                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                    {
                        Dr.DrawSimpleLabel(basePt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                    }
                    DrawBlockReference(THESAURUSCELEBRATE, basePt.ToPoint3d(), br =>
                    {
                        br.Layer = THESAURUSPRELIMINARY;
                        ByLayer(br);
                    });
                }
                void DrawOutlets5(Point2d basePoint, ThwOutput output, DrainageGroupedPipeItem gpItem)
                {
                    var values = output.DirtyWaterWellValues;
                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -KONSTITUTSIONNYĬ), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSCUPIDITY - THESAURUSALCOVE, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, VENTRILOQUISTIC), new Vector2d(THESAURUSDETAIL, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, DISCONTINUAUNCE) };
                    var segs = vecs.ToGLineSegments(basePoint);
                    segs.RemoveAt(THESAURUSPOSTSCRIPT);
                    DrawDiryWaterWells1(segs[PHOTOGONIOMETER].EndPoint + new Vector2d(-THESAURUSALCOVE, THESAURUSINTENTIONAL), values);
                    if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[THESAURUSPOSTSCRIPT].StartPoint.OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
                    if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[PHOTOGONIOMETER].EndPoint.OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
                    DrawNoteText(output.DN1, segs[THESAURUSPOSTSCRIPT].StartPoint.OffsetX(THESAURUSARTISAN));
                    DrawNoteText(output.DN2, segs[PHOTOGONIOMETER].EndPoint.OffsetX(THESAURUSARTISAN));
                    if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSTITTER].StartPoint.ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                    if (output.HasCleaningPort2) DrawCleaningPort(segs[PHOTOGONIOMETER].StartPoint.ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                    var p = segs[THESAURUSFACTOR].EndPoint;
                    DrawFloorDrain((p.OffsetX(-CONSTITUTIONALLY) + new Vector2d(-THESAURUSDETACHMENT + THESAURUSALCOVE, NARCOTRAFICANTE)).ToPoint3d(), THESAURUSSEMBLANCE);
                }
                string getDSCurveValue()
                {
                    return viewModel?.Params?.厨房洗涤盆 ?? THESAURUSRECEPTACLE;
                }
                for (int j = NARCOTRAFICANTE; j < COUNT; j++)
                {
                    var dome_lines = new List<GLineSegment>(PHOTOSENSITIZING);
                    var vent_lines = new List<GLineSegment>(PHOTOSENSITIZING);
                    var dome_layer = THESAURUSOVERWHELM;
                    var vent_layer = IMMUNOELECTROPHORESIS;
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid) dome_lines.Add(seg);
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs, string shadow)
                    {
                        var ok = UNTRACEABLENESS;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                {
                                    Dr.DrawSimpleLabel(seg.StartPoint, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC), dome_layer);
                                }
                                ok = THESAURUSSEMBLANCE;
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
                        var ok = UNTRACEABLENESS;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                {
                                    Dr.DrawSimpleLabel(seg.StartPoint, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC), vent_layer);
                                }
                                ok = THESAURUSSEMBLANCE;
                            }
                            vent_lines.Add(seg);
                        }
                    }
                    string getWashingMachineFloorDrainDN()
                    {
                        return viewModel?.Params?.WashingMachineFloorDrainDN ?? THESAURUSATAVISM;
                    }
                    string getOtherFloorDrainDN()
                    {
                        return viewModel?.Params?.OtherFloorDrainDN ?? THESAURUSATAVISM;
                    }
                    void Get2FloorDrainDN(out string v1, out string v2)
                    {
                        v1 = viewModel?.Params?.WashingMachineFloorDrainDN ?? THESAURUSATAVISM;
                        v2 = v1;
                        if (v2 == THESAURUSATAVISM) v2 = THESAURUSOVERCHARGE;
                    }
                    bool getCouldHavePeopleOnRoof()
                    {
                        return viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSSEMBLANCE;
                    }
                    var gpItem = pipeGroupItems[j];
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.Concat(gpItem.TlLabels.Yield()).ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
                    {
                        var storey = allNumStoreyLabels[i];
                        var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                        {
                            HasLongTranslator = gpItem.Items[i].HasLong,
                            HasShortTranslator = gpItem.Items[i].HasShort,
                            HasCleaningPort = gpItem.Hangings.TryGet(i + ADRENOCORTICOTROPHIC)?.HasCleaningPort ?? UNTRACEABLENESS,
                            HasCheckPoint = gpItem.Hangings[i].HasCheckPoint,
                            HasDownBoardLine = gpItem.Hangings[i].HasDownBoardLine,
                            DrawLongHLineHigher = gpItem.Items[i].DrawLongHLineHigher,
                            Is4Tune = gpItem.Hangings[i].Is4Tune,
                        } : null;
                        runs.Add(run);
                    }
                    for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
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
                        if (floorDrainsCount > NARCOTRAFICANTE || hasSCurve)
                        {
                            var run = runs.TryGet(i - ADRENOCORTICOTROPHIC);
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
                        foreach (var r in runs)
                        {
                            if (r?.HasShortTranslator == THESAURUSSEMBLANCE)
                            {
                                r.IsShortTranslatorToLeftOrRight = UNTRACEABLENESS;
                                if (r.HasLongTranslator && r.IsLongTranslatorToLeftOrRight)
                                {
                                    r.IsShortTranslatorToLeftOrRight = THESAURUSSEMBLANCE;
                                }
                            }
                        }
                    }
                    Point2d drawHanging(Point2d start, Hanging hanging)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(ALSOBENEVENTINE, NARCOTRAFICANTE), new Vector2d(THESAURUSREGULATE, NARCOTRAFICANTE), new Vector2d(AUTHORITARIANISM, NARCOTRAFICANTE), new Vector2d(THESAURUSHUMANE, NARCOTRAFICANTE) };
                        var segs = vecs.ToGLineSegments(start);
                        {
                            var _segs = segs.ToList();
                            if (hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC)
                            {
                                _segs.RemoveAt(THESAURUSPOSTSCRIPT);
                            }
                            _segs.RemoveAt(PHOTOGONIOMETER);
                            DrawDomePipes(_segs);
                        }
                        {
                            var pts = vecs.ToPoint2ds(start);
                            {
                                var pt = pts[ADRENOCORTICOTROPHIC];
                                var v = new Vector2d(THESAURUSITINERANT, THESAURUSITINERANT);
                                if (getDSCurveValue() == JUXTAPOSITIONAL)
                                {
                                    v = default;
                                }
                                var p = pt + v;
                                if (hanging.HasSCurve)
                                {
                                    DrawSCurve(v, pt, UNTRACEABLENESS);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    if (!p.Equals(pt))
                                    {
                                        dome_lines.Add(new GLineSegment(p, pt));
                                    }
                                    DrawDSCurve(p, UNTRACEABLENESS, getDSCurveValue(), THESAURUSREDOUND);
                                }
                            }
                            if (hanging.FloorDrainsCount >= ADRENOCORTICOTROPHIC)
                            {
                                DrawFloorDrain(pts[PHOTOGONIOMETER].ToPoint3d(), UNTRACEABLENESS);
                            }
                            if (hanging.FloorDrainsCount >= PHOTOGONIOMETER)
                            {
                                DrawFloorDrain(pts[THESAURUSTITTER].ToPoint3d(), UNTRACEABLENESS);
                            }
                        }
                        start = segs.Last().EndPoint;
                        return start;
                    }
                    void DrawOutlets1(string shadow, Point2d basePoint1, double width, ThwOutput output ,bool isRainWaterWell = UNTRACEABLENESS, Vector2d? fixv = null)
                    {
                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                        {
                            Dr.DrawSimpleLabel(basePoint1.OffsetY(-THESAURUSINDUSTRY), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                        }
                        Point2d pt2, pt3;
                        if (output.DirtyWaterWellValues != null)
                        {
                            var v = new Vector2d(-THESAURUSDIRECTIVE - THESAURUSALCOVE, -VENTRILOQUISTIC-QUINALBARBITONE);
                            var pt = basePoint1 + v;
                            if (fixv.HasValue)
                            {
                                pt += fixv.Value;
                            }
                            var values = output.DirtyWaterWellValues;
                            DrawDiryWaterWells1(pt, values, isRainWaterWell);
                        }
                        {
                            var dx = width - PLURALISTICALLY;
                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -QUINALBARBITONE-THESAURUSRESIST), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -VENTRILOQUISTIC), new Vector2d(RETROGRESSIVELY + dx, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, THESAURUSMARIJUANA), new Vector2d(-THESAURUSRECOVERY - dx, -THESAURUSINSTEAD), new Vector2d(MILLIONAIRESHIP + dx, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY) };
                            {
                                var segs = vecs.ToGLineSegments(basePoint1);
                                if (output.LinesCount == ADRENOCORTICOTROPHIC)
                                {
                                    drawDomePipes(segs.Take(THESAURUSPOSTSCRIPT), THESAURUSREDOUND);
                                }
                                else if (output.LinesCount > ADRENOCORTICOTROPHIC)
                                {
                                    segs.RemoveAt(ARCHAEOLOGICALLY);
                                    if (!output.HasVerticalLine2) segs.RemoveAt(QUOTATIONLENTIFORM);
                                    segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                    drawDomePipes(segs, THESAURUSREDOUND);
                                }
                            }
                            var pts = vecs.ToPoint2ds(basePoint1);
                            if (output.HasWrappingPipe1) _DrawWrappingPipe(pts[THESAURUSPOSTSCRIPT].OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
                            if (output.HasWrappingPipe2) _DrawWrappingPipe(pts[THESAURUSTITTER].OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
                            if (output.HasWrappingPipe3) _DrawWrappingPipe(pts[THESAURUSIMPORT].OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
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
                                    static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = THESAURUSSCAVENGER)
                                    {
                                        DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, label } }, cb: br => { ByLayer(br); });
                                    }
                                    var p1 = pts[THESAURUSPOSTSCRIPT].OffsetX(THESAURUSALCOVE);
                                    var p2 = p1.OffsetY(-QUOTATIONCAPABLE);
                                    var p3 = p2.OffsetX(HYDROSTATICALLY);
                                    var layer = THESAURUSPROCEEDING;
                                    DrawLine(layer, new GLineSegment(p1, p2));
                                    DrawLine(layer, new GLineSegment(p3, p2));
                                    DrawStoreyHeightSymbol(p3, THESAURUSPROCEEDING, gpItem.OutletWrappingPipeRadius);
                                    {
                                        var _shadow = THESAURUSREDOUND;
                                        if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > ADRENOCORTICOTROPHIC)
                                        {
                                            Dr.DrawSimpleLabel(p3, INCOMBUSTIBLENESS + _shadow.Substring(ADRENOCORTICOTROPHIC));
                                        }
                                    }
                                }
                            }
                            var v = new Vector2d(THESAURUSARTISAN, UNDERACHIEVEMENT);
                            DrawNoteText(output.DN1, pts[THESAURUSPOSTSCRIPT] + v);
                            DrawNoteText(output.DN2, pts[THESAURUSTITTER] + v);
                            DrawNoteText(output.DN3, pts[THESAURUSIMPORT] + v);
                            if (output.HasCleaningPort1) DrawCleaningPort(pts[PHOTOGONIOMETER].ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                            if (output.HasCleaningPort2) DrawCleaningPort(pts[THESAURUSFACTOR].ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                            if (output.HasCleaningPort3) DrawCleaningPort(pts[EXTRAJUDICIALIS].ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                            pt2 = pts[QUOTATIONLENTIFORM];
                            pt3 = pts.Last();
                        }
                        if (output.HasLargeCleaningPort)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, PRESUMPTIOUSNESS) };
                            var segs = vecs.ToGLineSegments(pt3);
                            drawDomePipes(segs, THESAURUSREDOUND);
                            DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), UNTRACEABLENESS, PHOTOGONIOMETER);
                        }
                        if (output.HangingCount == ADRENOCORTICOTROPHIC)
                        {
                            var hang = output.Hanging1;
                            Point2d lastPt = pt2;
                            {
                                var segs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, THESAURUSBURROW), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY) }.ToGLineSegments(lastPt);
                                drawDomePipes(segs, THESAURUSREDOUND);
                                lastPt = segs.Last().EndPoint;
                            }
                            {
                                lastPt = drawHanging(lastPt, output.Hanging1);
                            }
                        }
                        else if (output.HangingCount == PHOTOGONIOMETER)
                        {
                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSINDISPOSED, THESAURUSINDISPOSED), new Vector2d(SUBMICROSCOPICALLY, SUBMICROSCOPICALLY) };
                            var pts = vs1.ToPoint2ds(pt3);
                            drawDomePipes(vs1.ToGLineSegments(pt3), THESAURUSREDOUND);
                            drawHanging(pts.Last(), output.Hanging1);
                            var dx = output.Hanging1.FloorDrainsCount == PHOTOGONIOMETER ? QUOTATIONTRILINEAR : NARCOTRAFICANTE;
                            var vs2 = new List<Vector2d> { new Vector2d(THESAURUSMEANING + dx, NARCOTRAFICANTE), new Vector2d(SUBMICROSCOPICALLY, SUBMICROSCOPICALLY) };
                            drawDomePipes(vs2.ToGLineSegments(pts[ADRENOCORTICOTROPHIC]), THESAURUSREDOUND);
                            drawHanging(vs2.ToPoint2ds(pts[ADRENOCORTICOTROPHIC]).Last(), output.Hanging2);
                        }
                    }
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                    {
                        {
                        }
                        {
                            foreach (var info in arr)
                            {
                                if (info?.Storey == THESAURUSADHERE)
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
                        int counterPipeButtomHeightSymbol = NARCOTRAFICANTE;
                        bool hasDrawedSCurveLabel = UNTRACEABLENESS;
                        bool hasDrawedDSCurveLabel = UNTRACEABLENESS;
                        bool hasDrawedCleaningPort = UNTRACEABLENESS;
                        void _DrawLabel(string text1, string text2, Point2d basePt, bool leftOrRight, double height)
                        {
                            var w = PLURALISTICALLY - ELECTROMYOGRAPH;
                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, height), new Vector2d(leftOrRight ? -w : w, NARCOTRAFICANTE) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForDraiNote(lines.ToArray());
                            var p = segs.Last().EndPoint.OffsetY(UNDERACHIEVEMENT);
                            if (!string.IsNullOrEmpty(text1))
                            {
                                var t = DrawTextLazy(text1, THESAURUSDETEST, p);
                                Dr.SetLabelStylesForDraiNote(t);
                            }
                            if (!string.IsNullOrEmpty(text2))
                            {
                                var t = DrawTextLazy(text2, THESAURUSDETEST, p.OffsetXY(THESAURUSCONCEITED, -THESAURUSALCOVE));
                                Dr.SetLabelStylesForDraiNote(t);
                            }
                        }
                        void _DrawHorizontalLineOnPipeRun(Point3d basePt)
                        {
                            if (gpItem.Labels.Any(x => IsFL(x)))
                            {
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == PHOTOGONIOMETER)
                                {
                                    var p = basePt.ToPoint2d();
                                    var h = HEIGHT * LYMPHANGIOMATOUS;
                                    if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
                                    {
                                        h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSLEVITATE;
                                    }
                                    p = p.OffsetY(h);
                                    DrawPipeButtomHeightSymbol(QUOTATIONMENOMINEE, HEIGHT * THESAURUSBARBARISM, p);
                                }
                            }
                            DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                        }
                        void _DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                        {
                            if (!hasDrawedSCurveLabel)
                            {
                                hasDrawedSCurveLabel = THESAURUSSEMBLANCE;
                                _DrawLabel(NEUROTRANSMITTER, THESAURUSCLATTER, p1 + new Vector2d(-THESAURUSFLUENT, DECONTEXTUALIZE), THESAURUSSEMBLANCE, THESAURUSINSTEAD);
                            }
                            DrawSCurve(vec7, p1, leftOrRight);
                        }
                        void _DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
                        {
                            if (gpItem.Labels.Any(x => IsPL(x)))
                            {
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == PHOTOGONIOMETER)
                                {
                                    var p = basePt.ToPoint2d();
                                    DrawPipeButtomHeightSymbol(QUOTATIONMENOMINEE, HEIGHT * THESAURUSBARBARISM, p);
                                }
                            }
                            var p1 = basePt.ToPoint2d();
                            if (!hasDrawedCleaningPort && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                            {
                                hasDrawedCleaningPort = THESAURUSSEMBLANCE;
                                _DrawLabel(THESAURUSPROPEL, THESAURUSINFLAME, p1 + new Vector2d(-THESAURUSLUSTRE, THESAURUSATTENDANT), THESAURUSSEMBLANCE, ELECTROPHORETIC);
                            }
                            DrawCleaningPort(basePt, leftOrRight, scale);
                        }
                        void _DrawCheckPoint(Point2d basePt, bool leftOrRight, string shadow)
                        {
                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                            {
                                Dr.DrawSimpleLabel(basePt, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
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
                                if (storey == THESAURUSALCOHOLIC)
                                {
                                    var basePt = info.EndPoint;
                                    if (output != null)
                                    {
                                        DrawOutlets1(THESAURUSREDOUND, basePt, PLURALISTICALLY, output);
                                    }
                                }
                            }
                            bool shouldRaiseWashingMachine()
                            {
                                return viewModel?.Params?.ShouldRaiseWashingMachine ?? UNTRACEABLENESS;
                            }
                            bool _shouldDrawRaiseWashingMachineSymbol()
                            {
                                return UNTRACEABLENESS;
                            }
                            bool shouldDrawRaiseWashingMachineSymbol(Hanging hanging)
                            {
                                return UNTRACEABLENESS;
                            }
                            void handleHanging(Hanging hanging, bool isLeftOrRight)
                            {
                                var linesDfferencers = new List<Polygon>();
                                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, int i, int j, string shadow)
                                {
                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                    {
                                        Dr.DrawSimpleLabel(basePt.ToPoint2D(), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                    }
                                    var p1 = basePt.ToPoint2d();
                                    {
                                        if (_shouldDrawRaiseWashingMachineSymbol())
                                        {
                                            var fixVec = new Vector2d(-HYDROSTATICALLY, NARCOTRAFICANTE);
                                            var p = p1 + new Vector2d(NARCOTRAFICANTE, THESAURUSBEAUTIFUL) + new Vector2d(-AUTHORITARIANISM, THESAURUSINGRAINED) + fixVec;
                                            fdBsPts.Add(p);
                                            var vecs = new List<Vector2d> { new Vector2d(-ENTHOUSIASTIKOS, NARCOTRAFICANTE), fixVec, new Vector2d(-THESAURUSITINERANT, THESAURUSITINERANT), new Vector2d(NARCOTRAFICANTE, THESAURUSSACRIFICE), new Vector2d(-QUOTATIONCHIPPING, NARCOTRAFICANTE) };
                                            var segs = vecs.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(THESAURUSREGULATE, NARCOTRAFICANTE));
                                            drawDomePipes(segs, THESAURUSREDOUND);
                                            DrainageSystemDiagram.DrawWashingMachineRaisingSymbol(segs.Last().EndPoint, THESAURUSSEMBLANCE);
                                            return;
                                        }
                                    }
                                    {
                                        var p = p1 + new Vector2d(-AUTHORITARIANISM + (leftOrRight ? NARCOTRAFICANTE : THESAURUSDEFERENCE), THESAURUSDROUGHT);
                                        fdBsPts.Add(p);
                                        floorDrainCbs[new GRect(basePt, basePt.OffsetXY(leftOrRight ? -THESAURUSALCOVE : THESAURUSALCOVE, HYDROSTATICALLY)).ToPolygon()] = new FloorDrainCbItem()
                                        {
                                            BasePt = basePt.ToPoint2D(),
                                            Name = ELECTROMAGNETISM,
                                            LeftOrRight = leftOrRight,
                                        };
                                        return;
                                    }
                                }
                                void _DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight, int i, int j, string shadow)
                                {
                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                    {
                                        Dr.DrawSimpleLabel(p1, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                    }
                                    if (!hasDrawedDSCurveLabel && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                                    {
                                        hasDrawedDSCurveLabel = THESAURUSSEMBLANCE;
                                        var p2 = p1 + new Vector2d(-THESAURUSUSABLE, TRIBOELECTRICITY - THESAURUSINTENTIONAL);
                                        if (getDSCurveValue() == JUXTAPOSITIONAL)
                                        {
                                            p2 += new Vector2d(THESAURUSEVENTUALLY, -THESAURUSDROUGHT);
                                        }
                                        _DrawLabel(THESAURUSPARSON, THESAURUSCLATTER, p2, THESAURUSSEMBLANCE, THESAURUSINSTEAD);
                                    }
                                    {
                                        var v = vec7;
                                        if (getDSCurveValue() == JUXTAPOSITIONAL)
                                        {
                                            v = default;
                                            p1 = p1.OffsetY(THESAURUSINDUSTRY);
                                        }
                                        var p2 = p1 + v;
                                        if (!p1.Equals(p2))
                                        {
                                            dome_lines.Add(new GLineSegment(p1, p2));
                                        }
                                        DrawDSCurve(p2, leftOrRight, getDSCurveValue(), THESAURUSREDOUND);
                                    }
                                }
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == ADRENOCORTICOTROPHIC && thwPipeLine.Labels.Any(x => IsFL(x)))
                                {
                                    if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                    {
                                        DrawPipeButtomHeightSymbol(QUOTATIONMENOMINEE, HEIGHT * THESAURUSBARBARISM, info.StartPoint.OffsetY(-THESAURUSDROUGHT - INCONSEQUENTIALITY));
                                    }
                                    else
                                    {
                                        var c = gpItem.Hangings[i]?.FloorDrainsCount ?? NARCOTRAFICANTE;
                                        if (c > NARCOTRAFICANTE)
                                        {
                                            if (c == PHOTOGONIOMETER && !gpItem.Hangings[i].IsSeries)
                                            {
                                                DrawPipeButtomHeightSymbol(THESAURUSINTENTIONAL, HEIGHT * THESAURUSBARBARISM, info.StartPoint.OffsetXY(THESAURUSHUMANE, -THESAURUSDROUGHT));
                                                var vecs = new List<Vector2d> { new Vector2d(-HYDROSTATICALLY, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -VENTRILOQUISTIC), new Vector2d(-THESAURUSDIRECTIVE, NARCOTRAFICANTE) };
                                                var segs = vecs.ToGLineSegments(new List<Vector2d> { new Vector2d(-THESAURUSHUMANE, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -THESAURUSDROUGHT) }.GetLastPoint(info.StartPoint));
                                                DrawPipeButtomHeightSymbol(segs.Last().EndPoint, segs);
                                            }
                                            else
                                            {
                                                DrawPipeButtomHeightSymbol(QUOTATIONMENOMINEE, HEIGHT * THESAURUSBARBARISM, info.StartPoint.OffsetY(-THESAURUSDROUGHT));
                                            }
                                        }
                                        else
                                        {
                                            DrawPipeButtomHeightSymbol(QUOTATIONMENOMINEE, NARCOTRAFICANTE, info.EndPoint.OffsetY(THESAURUSEVENTUALLY));
                                        }
                                    }
                                }
                                var w = THESAURUSREGULATE;
                                if (hanging.FloorDrainsCount == PHOTOGONIOMETER && !hanging.HasDoubleSCurve)
                                {
                                    w = NARCOTRAFICANTE;
                                }
                                if (hanging.FloorDrainsCount == PHOTOGONIOMETER && !hanging.HasDoubleSCurve && !hanging.IsSeries)
                                {
                                    var startPt = info.StartPoint.OffsetY(-INDEMNIFICATION - ADRENOCORTICOTROPHIC);
                                    var delta = run.Is4Tune ? NARCOTRAFICANTE : THESAURUSINDUSTRY + UNDERACHIEVEMENT;
                                    var _vecs1 = new List<Vector2d> { new Vector2d(-CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(-ALSOBENEVENTINE, NARCOTRAFICANTE), };
                                    var _vecs2 = new List<Vector2d> { new Vector2d(CONSTITUTIONALLY + delta, CONSTITUTIONALLY + delta), new Vector2d(ALSOBENEVENTINE - delta, NARCOTRAFICANTE), };
                                    var segs1 = _vecs1.ToGLineSegments(startPt);
                                    var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                                    DrawDomePipes(segs1);
                                    DrawDomePipes(segs2);
                                    _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSSEMBLANCE, i, j, THESAURUSREDOUND);
                                    _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), UNTRACEABLENESS, i, j, THESAURUSREDOUND);
                                    if (run.Is4Tune)
                                    {
                                        var st = info.StartPoint;
                                        var p1 = new List<Vector2d> { new Vector2d(-HYDROSTATICALLY, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -THESAURUSSPACIOUS) }.GetLastPoint(st);
                                        var p2 = new List<Vector2d> { new Vector2d(THESAURUSINTENTIONAL, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -THESAURUSSPACIOUS) }.GetLastPoint(st);
                                        _DrawWrappingPipe(p1, THESAURUSREDOUND);
                                        _DrawWrappingPipe(p2, THESAURUSREDOUND);
                                    }
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (hanging.FloorDrainsCount == NARCOTRAFICANTE && hanging.HasDoubleSCurve)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSMALAISE, -THESAURUSMALAISE), new Vector2d(ALSOBENEVENTINE, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, -CONSTITUTIONALLY) };
                                        var dx = vecs.GetLastPoint(Point2d.Origin).X;
                                        var startPt = info.EndPoint.OffsetXY(-dx, HEIGHT / THESAURUSFACTOR);
                                        var segs = vecs.ToGLineSegments(startPt);
                                        var p1 = segs.Last(THESAURUSPOSTSCRIPT).StartPoint;
                                        drawDomePipes(segs, THESAURUSREDOUND);
                                        _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSREDOUND);
                                    }
                                    else
                                    {
                                        var beShort = hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                        var vecs = new List<Vector2d> { new Vector2d(-THESAURUSENTREAT, THESAURUSENTREAT), new Vector2d(NARCOTRAFICANTE, IRREMUNERABILIS), new Vector2d(-CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(beShort ? NARCOTRAFICANTE : -ALSOBENEVENTINE, NARCOTRAFICANTE), new Vector2d(-w, NARCOTRAFICANTE), new Vector2d(-AUTHORITARIANISM, NARCOTRAFICANTE), new Vector2d(-THESAURUSHUMANE, NARCOTRAFICANTE) };
                                        if (isLeftOrRight == UNTRACEABLENESS)
                                        {
                                            vecs = vecs.GetYAxisMirror();
                                        }
                                        var pt = info.Segs[THESAURUSTITTER].StartPoint.OffsetY(-THESAURUSDECREE).OffsetY(INTRAVASCULARLY - THESAURUSITINERANT);
                                        if (isLeftOrRight == UNTRACEABLENESS && run.IsLongTranslatorToLeftOrRight == THESAURUSSEMBLANCE)
                                        {
                                            var p1 = pt;
                                            var p2 = pt.OffsetX(MICROCLIMATOLOGY);
                                            drawDomePipe(new GLineSegment(p1, p2));
                                            pt = p2;
                                        }
                                        if (isLeftOrRight == THESAURUSSEMBLANCE && run.IsLongTranslatorToLeftOrRight == UNTRACEABLENESS)
                                        {
                                            var p1 = pt;
                                            var p2 = pt.OffsetX(-MICROCLIMATOLOGY);
                                            drawDomePipe(new GLineSegment(p1, p2));
                                            pt = p2;
                                        }
                                        Action f;
                                        var segs = vecs.ToGLineSegments(pt);
                                        {
                                            var _segs = segs.ToList();
                                            if (hanging.FloorDrainsCount == PHOTOGONIOMETER)
                                            {
                                                if (hanging.IsSeries)
                                                {
                                                    _segs.RemoveAt(THESAURUSFACTOR);
                                                }
                                            }
                                            else if (hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC)
                                            {
                                                _segs = segs.Take(THESAURUSFACTOR).ToList();
                                            }
                                            else if (hanging.FloorDrainsCount == NARCOTRAFICANTE)
                                            {
                                                _segs = segs.Take(THESAURUSTITTER).ToList();
                                            }
                                            if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(PHOTOGONIOMETER); }
                                            f = () => { drawDomePipes(_segs, THESAURUSREDOUND); };
                                        }
                                        if (hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC)
                                        {
                                            var p = segs.Last(THESAURUSPOSTSCRIPT).EndPoint;
                                            _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p + new Vector2d(THESAURUSINTENTIONAL, -THESAURUSFLUENT));
                                        }
                                        if (hanging.FloorDrainsCount == PHOTOGONIOMETER)
                                        {
                                            var p2 = segs.Last(THESAURUSPOSTSCRIPT).EndPoint;
                                            var p1 = segs.Last(ADRENOCORTICOTROPHIC).EndPoint;
                                            _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                            _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p1 + new Vector2d(AUTHORITARIANISM, -THESAURUSALCOVE));
                                            DrawNoteText(v2, p2 + new Vector2d(THESAURUSDEMONSTRATION - AUTHORITARIANISM, -THESAURUSALCOVE));
                                            if (!hanging.IsSeries)
                                            {
                                                drawDomePipes(new GLineSegment[] { segs.Last(PHOTOGONIOMETER) }, THESAURUSREDOUND);
                                            }
                                            {
                                                var _segs = new List<Vector2d> { new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(THESAURUSDISPUTATION, NARCOTRAFICANTE), new Vector2d(THESAURUSINDUSTRY, NARCOTRAFICANTE), new Vector2d(TRANSUBSTANTIATE, NARCOTRAFICANTE), new Vector2d(INCONVERTIBILIS, -CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, -THESAURUSCOMMENSURATE), new Vector2d(CONSTITUTIONALLY, -CONSTITUTIONALLY) }.ToGLineSegments(p1);
                                                _segs.RemoveAt(PHOTOGONIOMETER);
                                                var seg = new List<Vector2d> { new Vector2d(THESAURUSREGULATE, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY) }.ToGLineSegments(p1)[ADRENOCORTICOTROPHIC];
                                                f = () =>
                                                {
                                                    drawDomePipes(_segs, THESAURUSREDOUND);
                                                    drawDomePipes(new GLineSegment[] { seg }, THESAURUSREDOUND);
                                                };
                                            }
                                        }
                                        {
                                            var p = segs.Last(THESAURUSPOSTSCRIPT).EndPoint;
                                            var seg = new List<Vector2d> { new Vector2d(THESAURUSPRODUCT, -THESAURUSANONYMOUS), new Vector2d(NARCOTRAFICANTE, -THESAURUSSUBSIDIARY) }.ToGLineSegments(p)[ADRENOCORTICOTROPHIC];
                                            var pt1 = segs.First().StartPoint;
                                            var pt2 = pt1.OffsetY(THESAURUSSUBSIDIARY);
                                            var dim = DrawDimLabel(pt1, pt2, new Vector2d(QUOTATIONTRILINEAR, NARCOTRAFICANTE), BASIDIOMYCOTINA, THESAURUSAGGRIEVED);
                                        }
                                        if (hanging.HasSCurve)
                                        {
                                            var p1 = segs.Last(THESAURUSPOSTSCRIPT).StartPoint;
                                            _DrawSCurve(vec7, p1, isLeftOrRight);
                                        }
                                        if (hanging.HasDoubleSCurve)
                                        {
                                            var p1 = segs.Last(THESAURUSPOSTSCRIPT).StartPoint;
                                            _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSREDOUND);
                                        }
                                        f?.Invoke();
                                    }
                                }
                                else
                                {
                                    if (gpItem.IsFL0)
                                    {
                                        DrawFloorDrain((info.StartPoint + new Vector2d(-ENFRANCHISEMENT, -THESAURUSDROUGHT)).ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY);
                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -INCOMMODIOUSNESS), new Vector2d(-CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(-THESAURUSNEIGHBOURHOOD, NARCOTRAFICANTE) };
                                        var segs = vecs.ToGLineSegments(info.StartPoint).Skip(ADRENOCORTICOTROPHIC).ToList();
                                        drawDomePipes(segs, THESAURUSREDOUND);
                                    }
                                    else
                                    {
                                        var beShort = hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                        var vecs = new List<Vector2d> { new Vector2d(-CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(beShort ? NARCOTRAFICANTE : -ALSOBENEVENTINE, NARCOTRAFICANTE), new Vector2d(-w, NARCOTRAFICANTE), new Vector2d(-AUTHORITARIANISM, NARCOTRAFICANTE), new Vector2d(-THESAURUSHUMANE, NARCOTRAFICANTE) };
                                        if (isLeftOrRight == UNTRACEABLENESS)
                                        {
                                            vecs = vecs.GetYAxisMirror();
                                        }
                                        var startPt = info.StartPoint.OffsetY(-INDEMNIFICATION - ADRENOCORTICOTROPHIC);
                                        if (hanging.FloorDrainsCount == NARCOTRAFICANTE && hanging.HasDoubleSCurve)
                                        {
                                            startPt = info.EndPoint.OffsetY(-THESAURUSBELOVED + HEIGHT / THESAURUSFACTOR);
                                        }
                                        var ok = UNTRACEABLENESS;
                                        if (hanging.FloorDrainsCount == PHOTOGONIOMETER && !hanging.HasDoubleSCurve)
                                        {
                                            if (hanging.IsSeries)
                                            {
                                                var segs = vecs.ToGLineSegments(startPt);
                                                var _segs = segs.ToList();
                                                linesDfferencers.Add(GRect.Create(_segs[THESAURUSPOSTSCRIPT].EndPoint, UNDERACHIEVEMENT).ToPolygon());
                                                var p2 = segs.Last(THESAURUSPOSTSCRIPT).EndPoint;
                                                var p1 = segs.Last(ADRENOCORTICOTROPHIC).EndPoint;
                                                _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                                _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                                Get2FloorDrainDN(out string v1, out string v2);
                                                DrawNoteText(v1, p1 + new Vector2d(AUTHORITARIANISM, -THESAURUSALCOVE));
                                                DrawNoteText(v2, p2 + new Vector2d(THESAURUSDEMONSTRATION - AUTHORITARIANISM, -THESAURUSALCOVE));
                                                segs = new List<Vector2d> { new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(THESAURUSDISPUTATION, NARCOTRAFICANTE), new Vector2d(THESAURUSINDUSTRY, NARCOTRAFICANTE), new Vector2d(THESAURUSTETCHY, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY) }.ToGLineSegments(p1);
                                                var p = segs[THESAURUSTITTER].StartPoint;
                                                segs.RemoveAt(PHOTOGONIOMETER);
                                                dome_lines.AddRange(segs);
                                                dome_lines.AddRange(new List<Vector2d> { new Vector2d(THESAURUSPREDOMINANT, NARCOTRAFICANTE), new Vector2d(INCONVERTIBILIS, -CONSTITUTIONALLY) }.ToGLineSegments(p));
                                            }
                                            else
                                            {
                                                var delta = THESAURUSINDUSTRY;
                                                var _vecs1 = new List<Vector2d> { new Vector2d(-CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(-ALSOBENEVENTINE, NARCOTRAFICANTE), };
                                                var _vecs2 = new List<Vector2d> { new Vector2d(CONSTITUTIONALLY + delta, CONSTITUTIONALLY + delta), new Vector2d(ALSOBENEVENTINE - delta, NARCOTRAFICANTE), };
                                                var segs1 = _vecs1.ToGLineSegments(startPt);
                                                var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                                                dome_lines.AddRange(segs1);
                                                dome_lines.AddRange(segs2);
                                                _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSSEMBLANCE, i, j, THESAURUSREDOUND);
                                                _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), UNTRACEABLENESS, i, j, THESAURUSREDOUND);
                                            }
                                            ok = THESAURUSSEMBLANCE;
                                        }
                                        Action f = null;
                                        if (!ok)
                                        {
                                            if (gpItem.Hangings[i].FlCaseEnum != FixingLogic1.FlCaseEnum.Case1)
                                            {
                                                var segs = vecs.ToGLineSegments(startPt);
                                                var _segs = segs.ToList();
                                                {
                                                    if (hanging.FloorDrainsCount == PHOTOGONIOMETER)
                                                    {
                                                        if (hanging.IsSeries)
                                                        {
                                                            _segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                                        }
                                                    }
                                                    if (hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC)
                                                    {
                                                        _segs.RemoveAt(THESAURUSTITTER);
                                                        _segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                                    }
                                                    if (hanging.FloorDrainsCount == NARCOTRAFICANTE)
                                                    {
                                                        _segs = _segs.Take(PHOTOGONIOMETER).ToList();
                                                    }
                                                    if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(PHOTOGONIOMETER); }
                                                }
                                                if (hanging.FloorDrainsCount == ADRENOCORTICOTROPHIC)
                                                {
                                                    var p = segs.Last(THESAURUSPOSTSCRIPT).EndPoint;
                                                    _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p + new Vector2d(THESAURUSINTENTIONAL, -THESAURUSFLUENT));
                                                }
                                                if (hanging.FloorDrainsCount == PHOTOGONIOMETER)
                                                {
                                                    var p2 = segs.Last(THESAURUSPOSTSCRIPT).EndPoint;
                                                    var p1 = segs.Last(ADRENOCORTICOTROPHIC).EndPoint;
                                                    _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                                    _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSREDOUND);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p1 + new Vector2d(AUTHORITARIANISM, -THESAURUSALCOVE));
                                                    DrawNoteText(v2, p2 + new Vector2d(THESAURUSDEMONSTRATION - AUTHORITARIANISM, -THESAURUSALCOVE));
                                                }
                                                f = () => drawDomePipes(_segs, THESAURUSREDOUND);
                                            }
                                        }
                                        {
                                            var segs = vecs.ToGLineSegments(startPt);
                                            if (hanging.HasSCurve)
                                            {
                                                var p1 = segs.Last(THESAURUSPOSTSCRIPT).StartPoint;
                                                _DrawSCurve(vec7, p1, isLeftOrRight);
                                            }
                                            if (hanging.HasDoubleSCurve)
                                            {
                                                var p1 = segs.Last(THESAURUSPOSTSCRIPT).StartPoint;
                                                if (gpItem.Hangings[i].FlCaseEnum == FixingLogic1.FlCaseEnum.Case1)
                                                {
                                                    var p2 = p1 + vec7;
                                                    var segs1 = new List<Vector2d> { new Vector2d(-THESAURUSBEAUTIFUL + THESAURUSREDDEN + THESAURUSITINERANT, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -QUOTATIONTRILINEAR - DEMOCRATIZATION - THESAURUSITINERANT), new Vector2d(THESAURUSENTREAT, -THESAURUSENTREAT) }.ToGLineSegments(p2);
                                                    drawDomePipes(segs1, THESAURUSREDOUND);
                                                    {
                                                        Vector2d v = default;
                                                        var b = isLeftOrRight;
                                                        if (b && getDSCurveValue() == JUXTAPOSITIONAL)
                                                        {
                                                            b = UNTRACEABLENESS;
                                                            v = new Vector2d(-AUTHORITARIANISM, -HYDROSTATICALLY);
                                                        }
                                                        _DrawDSCurve(default(Vector2d), p2 + v, b, i, j, THESAURUSREDOUND);
                                                    }
                                                    var p3 = segs1.Last().EndPoint;
                                                    var p4 = p3.OffsetY(THESAURUSDILIGENCE);
                                                    DrawDimLabel(p3, p4, new Vector2d(QUOTATIONTRILINEAR, NARCOTRAFICANTE), BASIDIOMYCOTINA, THESAURUSAGGRIEVED);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    Dr.DrawDN_2(segs1.Last().StartPoint + new Vector2d(PROBATIONERSHIP + COMMUNICABLENESS - THESAURUSINDUSTRY - MICROPUBLISHING, -THESAURUSENTREAT), THESAURUSPROCEEDING, v1);
                                                }
                                                else
                                                {
                                                    var fixY = THESAURUSDETESTABLE + THESAURUSINTENTIONAL;
                                                    _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSREDOUND);
                                                    if (getDSCurveValue() == JUXTAPOSITIONAL)
                                                    {
                                                        var segs1 = new List<Vector2d> { new Vector2d(ALSOBENEVENTINE, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, -CONSTITUTIONALLY) }.ToGLineSegments(p1.OffsetY(THESAURUSINDUSTRY));
                                                        f = () => { drawDomePipes(segs1, THESAURUSREDOUND); };
                                                    }
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p1.OffsetY(fixY));
                                                }
                                            }
                                        }
                                        f?.Invoke();
                                    }
                                }
                                if (linesDfferencers.Count > NARCOTRAFICANTE)
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
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, PHOTOGONIOMETER));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSINTENTIONAL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(PHOTOGONIOMETER, THESAURUSPOSTSCRIPT));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.FirstRightRun)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, QUOTATIONLENTIFORM));
                                    var p3 = info.EndPoint.OffsetX(-THESAURUSINTENTIONAL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, THESAURUSPOSTSCRIPT));
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
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, QUOTATIONLENTIFORM));
                                    var p3 = info.EndPoint.OffsetX(-THESAURUSINTENTIONAL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, THESAURUSPOSTSCRIPT));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.BlueToRightFirst)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, QUOTATIONLENTIFORM));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSINTENTIONAL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(ADRENOCORTICOTROPHIC, THESAURUSPOSTSCRIPT));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.BlueToLeftLast)
                                {
                                    if (run.HasLongTranslator)
                                    {
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var _dy = THESAURUSINTENTIONAL;
                                            var vs1 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY - _dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy + _dy + THESAURUSALCOVE), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSINTENTIONAL), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(ADRENOCORTICOTROPHIC).ToList());
                                        }
                                        else
                                        {
                                            var _dy = THESAURUSINTENTIONAL;
                                            var vs1 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy), new Vector2d(CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy - _dy + THESAURUSALCOVE), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSINTENTIONAL), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(ADRENOCORTICOTROPHIC).ToList());
                                        }
                                    }
                                    else if (!run.HasLongTranslator)
                                    {
                                        var vs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -PARASITICALNESS), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                        info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                    }
                                }
                                if (bi.BlueToRightLast)
                                {
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var p1 = info.EndPoint;
                                        var p2 = p1.OffsetY(HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS));
                                        var p3 = info.EndPoint.OffsetX(THESAURUSINTENTIONAL);
                                        var p4 = p3.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS));
                                        var p5 = p1.OffsetY(HEIGHT);
                                        info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, p2), new GLineSegment(p2, p5) };
                                    }
                                }
                                if (bi.BlueToLeftMiddle)
                                {
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var p1 = info.EndPoint;
                                        var p2 = p1.OffsetY(HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS));
                                        var p3 = info.EndPoint.OffsetX(-THESAURUSINTENTIONAL);
                                        var p4 = p3.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS));
                                        var segs = info.Segs.ToList();
                                        segs.Add(new GLineSegment(p2, p4));
                                        info.DisplaySegs = segs;
                                    }
                                    else if (run.HasLongTranslator)
                                    {
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var _dy = THESAURUSINTENTIONAL;
                                            var vs1 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY - _dy), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy + _dy) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSINTENTIONAL), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(ADRENOCORTICOTROPHIC).ToList());
                                        }
                                        else
                                        {
                                            var _dy = THESAURUSINTENTIONAL;
                                            var vs1 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -QUOTATION1BRICKETY + _dy), new Vector2d(CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(THESAURUSPRONOUNCED, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, -THESAURUSFRIGHT), new Vector2d(NARCOTRAFICANTE, -THESAURUSINFILTRATE - dy - _dy) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSINTENTIONAL), new Vector2d(-THESAURUSINTENTIONAL, -DOMINEERINGNESS) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(ADRENOCORTICOTROPHIC).ToList());
                                        }
                                    }
                                }
                                if (bi.BlueToRightMiddle)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSINTENTIONAL);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS));
                                    var segs = info.Segs.ToList();
                                    segs.Add(new GLineSegment(p2, p4));
                                    info.DisplaySegs = segs;
                                }
                                {
                                    var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSBEAUTIFUL), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-KONSTITUTSIONNYĬ, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -THESAURUSCESSATION), new Vector2d(-THESAURUSENTREAT, -THESAURUSENTREAT) };
                                    if (bi.HasLongTranslatorToLeft)
                                    {
                                        var vs = vecs;
                                        info.DisplaySegs = vecs.ToGLineSegments(info.StartPoint);
                                        if (!bi.IsLast)
                                        {
                                            var pt = vs.Take(vs.Count - ADRENOCORTICOTROPHIC).GetLastPoint(info.StartPoint);
                                            info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -PHYTOPLANKTONIC) }.ToGLineSegments(pt));
                                        }
                                    }
                                    if (bi.HasLongTranslatorToRight)
                                    {
                                        var vs = vecs.GetYAxisMirror();
                                        info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                        if (!bi.IsLast)
                                        {
                                            var pt = vs.Take(vs.Count - ADRENOCORTICOTROPHIC).GetLastPoint(info.StartPoint);
                                            info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -PHYTOPLANKTONIC) }.ToGLineSegments(pt));
                                        }
                                    }
                                }
                            }
                            if (run.LeftHanging != null)
                            {
                                run.LeftHanging.IsSeries = gpItem.Hangings.TryGet(i + ADRENOCORTICOTROPHIC)?.IsSeries ?? THESAURUSSEMBLANCE;
                                handleHanging(run.LeftHanging, THESAURUSSEMBLANCE);
                            }
                            if (run.BranchInfo != null)
                            {
                                handleBranchInfo(run, info);
                            }
                            if (run.ShowShortTranslatorLabel)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(THESAURUSIRRECONCILABLE, THESAURUSIRRECONCILABLE), new Vector2d(-QUOTATIONMORETON, QUOTATIONMORETON), new Vector2d(-THESAURUSDISOWN, NARCOTRAFICANTE) };
                                var segs = vecs.ToGLineSegments(info.EndPoint).Skip(ADRENOCORTICOTROPHIC).ToList();
                                DrawDraiNoteLines(segs);
                                DrawDraiNoteLines(segs);
                                var text = QUOTATIONTACONIC;
                                var pt = segs.Last().EndPoint;
                                DrawNoteText(text, pt);
                            }
                            if (run.HasCheckPoint)
                            {
                                var h = HEIGHT / THESAURUSCONTINGENT * THESAURUSTITTER;
                                if (!run.HasLongTranslator)
                                {
                                    if (IsPL(gpItem.Labels.First()) || gpItem.Hangings[i].HasDoubleSCurve)
                                    {
                                        h = HEIGHT / THESAURUSCONTINGENT * QUOTATIONLENTIFORM;
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
                                _DrawCheckPoint(pt1, THESAURUSSEMBLANCE, THESAURUSREDOUND);
                                if (storey == THESAURUSALCOHOLIC)
                                {
                                    var dx = -QUOTATIONTRILINEAR;
                                    if (gpItem.HasBasinInKitchenAt1F)
                                    {
                                        dx = QUOTATIONTRILINEAR;
                                    }
                                    {
                                        var dim = DrawDimLabel(pt1, pt2, new Vector2d(dx, NARCOTRAFICANTE), gpItem.PipeType == PipeType.PL ? THESAURUSFORTIFICATION : QUOTATIONOPHTHALMIA, THESAURUSAGGRIEVED);
                                        if (dx < NARCOTRAFICANTE)
                                        {
                                            dim.TextPosition = (pt1 + new Vector2d(dx, NARCOTRAFICANTE) + new Vector2d(-THESAURUSSEIZURE, -THESAURUSENTREAT) + new Vector2d(NARCOTRAFICANTE, THESAURUSINDUSTRY)).ToPoint3d();
                                        }
                                    }
                                    if (gpItem.HasTl && allStoreys[i] == gpItem.MinTl + QUOTATIONHOUSEMAID)
                                    {
                                        var k = THESAURUSINFERENCE / HEIGHT;
                                        pt1 = info.EndPoint;
                                        pt2 = pt1.OffsetY(INQUISITORIALLY * k);
                                        if (run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight)
                                        {
                                            pt2 = pt1.OffsetY(THESAURUSBEGINNER);
                                        }
                                        var dim = DrawDimLabel(pt1, pt2, new Vector2d(QUOTATIONTRILINEAR, NARCOTRAFICANTE), QUOTATIONOPHTHALMIA, THESAURUSAGGRIEVED);
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
                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSENTREAT, THESAURUSENTREAT), new Vector2d(NARCOTRAFICANTE, THESAURUSINTENTIONAL), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(IMPRESCRIPTIBLE, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, THESAURUSDETACHMENT) };
                                    if (run.IsLongTranslatorToLeftOrRight == UNTRACEABLENESS)
                                    {
                                        vecs = vecs.GetYAxisMirror();
                                    }
                                    if (run.HasShortTranslator)
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last(PHOTOGONIOMETER).StartPoint.OffsetY(-THESAURUSINTENTIONAL));
                                        drawDomePipes(segs, THESAURUSREDOUND);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, PHOTOGONIOMETER);
                                    }
                                    else
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-THESAURUSINTENTIONAL));
                                        drawDomePipes(segs, THESAURUSREDOUND);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, PHOTOGONIOMETER);
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var pt1 = segs.First().StartPoint;
                                            var pt2 = pt1.OffsetY(THESAURUSPSYCHOLOGY);
                                            var dim = DrawDimLabel(pt1, pt2, new Vector2d(-QUOTATIONTRILINEAR, NARCOTRAFICANTE), BASIDIOMYCOTINA, THESAURUSAGGRIEVED);
                                            dim.TextPosition = (pt1 + new Vector2d(-QUOTATIONTRILINEAR, NARCOTRAFICANTE) + new Vector2d(-THESAURUSPOISED, THESAURUSINTERDICT) + new Vector2d(CONSUBSTANTIALITY, THESAURUSENTREAT - THESAURUSINADMISSIBLE)).ToPoint3d();
                                        }
                                    }
                                }
                                else
                                {
                                    _DrawCleaningPort(info.StartPoint.OffsetY(-THESAURUSINTENTIONAL).ToPoint3d(), THESAURUSSEMBLANCE, PHOTOGONIOMETER);
                                }
                            }
                            if (run.HasShortTranslator)
                            {
                                DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                            }
                        }
                        var showAllFloorDrainLabel = UNTRACEABLENESS;
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var (ok, item) = gpItem.Items.TryGetValue(i + ADRENOCORTICOTROPHIC);
                            if (!ok) continue;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                            }
                        }
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var hanging = gpItem.Hangings.TryGet(i + ADRENOCORTICOTROPHIC);
                            if (hanging == null) continue;
                            var fdsCount = hanging.FloorDrainsCount;
                            if (fdsCount == NARCOTRAFICANTE) continue;
                            var wfdsCount = hanging.WashingMachineFloorDrainsCount;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (fdsCount > NARCOTRAFICANTE)
                                {
                                    if (wfdsCount > NARCOTRAFICANTE)
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
                            var hanging = gpItem.Hangings.TryGet(i + ADRENOCORTICOTROPHIC);
                            if (hanging == null) continue;
                            var fdsCount = hanging.FloorDrainsCount;
                            if (fdsCount == NARCOTRAFICANTE) continue;
                            var wfdsCount = hanging.WashingMachineFloorDrainsCount;
                            var h = THESAURUSTATTLE;
                            var ok_texts = new HashSet<string>();
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (fdsCount > NARCOTRAFICANTE)
                                {
                                    if (wfdsCount > NARCOTRAFICANTE)
                                    {
                                        wfdsCount--;
                                        h += THESAURUSBEAUTIFUL;
                                        if (hanging.RoomName != null)
                                        {
                                            var text = $"接{hanging.RoomName}洗衣机地漏";
                                            if (!ok_texts.Contains(text))
                                            {
                                                _DrawLabel(text, $"{getWashingMachineFloorDrainDN()}，余同", pt, THESAURUSSEMBLANCE, h);
                                                ok_texts.Add(text);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        h += THESAURUSBEAUTIFUL;
                                        if (hanging.RoomName != null)
                                        {
                                            _DrawLabel($"接{hanging.RoomName}地漏", $"{getWashingMachineFloorDrainDN()}，余同", pt, THESAURUSSEMBLANCE, h);
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
                                    o.Name = CYCLOHEXYLSULPHAMATE;
                                }
                                DrawFloorDrain(o.BasePt.ToPoint3d(), o.LeftOrRight, o.Name);
                            }
                        }
                    }
                    PipeRunLocationInfo[] getPipeRunLocationInfos()
                    {
                        var infos = new PipeRunLocationInfo[allStoreys.Count];
                        for (int i = NARCOTRAFICANTE; i < allStoreys.Count; i++)
                        {
                            infos[i] = new PipeRunLocationInfo() { Visible = THESAURUSSEMBLANCE, Storey = allStoreys[i], };
                        }
                        {
                            var tdx = QUOTATIONEXPANDING;
                            for (int i = start; i >= end; i--)
                            {
                                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                                var basePt = bsPt1.OffsetX(OFFSET_X + (j + ADRENOCORTICOTROPHIC) * SPAN_X) + new Vector2d(tdx, NARCOTRAFICANTE);
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                fixY = NARCOTRAFICANTE;
                                PipeRunLocationInfo drawNormal()
                                {
                                    {
                                        var vecs = vecs0;
                                        var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        infos[i].BasePoint = basePt;
                                        infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                        infos[i].HangingEndPoint = infos[i].EndPoint;
                                        infos[i].Vector2ds = vecs;
                                        infos[i].Segs = segs;
                                        infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSINTENTIONAL, NARCOTRAFICANTE)).ToList();
                                        infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM);
                                    }
                                    {
                                        var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                        infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))));
                                    }
                                    {
                                        var info = infos[i];
                                        var k = HEIGHT / THESAURUSINFERENCE;
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSINTENTIONAL, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, -UNSUBMISSIVENESS * k), new Vector2d(-THESAURUSINTENTIONAL, -INQUISITORIALLY * k) };
                                        var segs = vecs.ToGLineSegments(info.EndPoint.OffsetY(HEIGHT)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                        info.RightSegsLast = segs;
                                    }
                                    {
                                        var pt = infos[i].Segs.First().StartPoint.OffsetX(THESAURUSINTENTIONAL);
                                        var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS))) };
                                        infos[i].RightSegsFirst = segs;
                                        segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSINTENTIONAL)));
                                    }
                                    return infos[i];
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
                                _dy = run.DrawLongHLineHigher ? THESAURUSOBEISANCE : NARCOTRAFICANTE;
                                if (run.HasLongTranslator && run.HasShortTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs3;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSTITTER);
                                            segs.Add(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSPOSTSCRIPT].EndPoint.OffsetXY(-THESAURUSINTENTIONAL, -THESAURUSINTENTIONAL)));
                                            segs.Add(new GLineSegment(segs[PHOTOGONIOMETER].EndPoint, new Point2d(segs[THESAURUSFACTOR].EndPoint.X, segs[PHOTOGONIOMETER].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSPOSTSCRIPT], new GLineSegment(segs[THESAURUSPOSTSCRIPT].StartPoint, segs[NARCOTRAFICANTE].StartPoint) };
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSINTENTIONAL)));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs6;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(-CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))).Offset(ELECTROMYOGRAPH, NARCOTRAFICANTE));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSTITTER);
                                            segs.Add(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSTITTER].StartPoint));
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSINTENTIONAL)));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].Segs[THESAURUSTITTER].EndPoint;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    switch (gpItem.Hangings[i].FlFixType)
                                    {
                                        case FixingLogic1.FlFixType.NoFix:
                                            break;
                                        case FixingLogic1.FlFixType.MiddleHigher:
                                            fixY = THESAURUSPINCHED / THESAURUSINSTEAD * HEIGHT;
                                            break;
                                        case FixingLogic1.FlFixType.Lower:
                                            fixY = -THESAURUSCHRONOLOGICAL / PHOTOGONIOMETER / THESAURUSINSTEAD * HEIGHT;
                                            break;
                                        case FixingLogic1.FlFixType.Higher:
                                            fixY = THESAURUSCHRONOLOGICAL / PHOTOGONIOMETER / THESAURUSINSTEAD * HEIGHT + THESAURUSEXTERNAL / THESAURUSINSTEAD * HEIGHT;
                                            break;
                                        default:
                                            break;
                                    }
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs1;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs = segs.Take(THESAURUSTITTER).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSPOSTSCRIPT].EndPoint.OffsetXY(-THESAURUSINTENTIONAL, -THESAURUSINTENTIONAL))).ToList();
                                            segs.Add(new GLineSegment(segs[PHOTOGONIOMETER].EndPoint, new Point2d(segs[THESAURUSFACTOR].EndPoint.X, segs[PHOTOGONIOMETER].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSFACTOR);
                                            segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                            segs = new List<GLineSegment>() { segs[THESAURUSPOSTSCRIPT], new GLineSegment(segs[THESAURUSPOSTSCRIPT].StartPoint, segs[NARCOTRAFICANTE].StartPoint) };
                                            var h = HEIGHT - THESAURUSINSTEAD;
                                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -CHARACTERISTICAL), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPPROACH, NARCOTRAFICANTE), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, -ALSOCHIROPTEROUS - THESAURUSINDUSTRY - h), new Vector2d(-THESAURUSENTREAT, -THESAURUSMAESTRO) };
                                            segs = vecs.ToGLineSegments(infos[i].BasePoint.OffsetXY(THESAURUSINTENTIONAL, HEIGHT));
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs4;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(THESAURUSINTENTIONAL)).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))).Offset(ELECTROMYOGRAPH, NARCOTRAFICANTE));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle;
                                            infos[i].RightSegsLast = segs.Take(THESAURUSTITTER).YieldAfter(new GLineSegment(segs[THESAURUSPOSTSCRIPT].EndPoint, segs[THESAURUSFACTOR].StartPoint)).YieldAfter(segs[THESAURUSFACTOR]).ToList();
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[THESAURUSTITTER].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONLENTIFORM, QUOTATIONMUCOUS))) };
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].EndPoint, pt.OffsetXY(-THESAURUSINTENTIONAL, HEIGHT.ToRatioInt(EXTRAJUDICIALIS, QUOTATIONMUCOUS))));
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
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSINTENTIONAL, NARCOTRAFICANTE)).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, segs[PHOTOGONIOMETER].StartPoint), segs[PHOTOGONIOMETER] };
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[PHOTOGONIOMETER].StartPoint, segs[PHOTOGONIOMETER].EndPoint);
                                            segs[PHOTOGONIOMETER] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(NARCOTRAFICANTE);
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, r.RightButtom));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs5;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, NARCOTRAFICANTE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSINTENTIONAL, NARCOTRAFICANTE)).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONLENTIFORM).OffsetX(-CONSTITUTIONALLY);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONMUCOUS - EXTRAJUDICIALIS, QUOTATIONMUCOUS)), pt.OffsetXY(-THESAURUSINTENTIONAL, -HEIGHT.ToRatioInt(QUOTATIONMUCOUS - QUOTATIONLENTIFORM, QUOTATIONMUCOUS))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, segs[PHOTOGONIOMETER].StartPoint), segs[PHOTOGONIOMETER] };
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[PHOTOGONIOMETER].StartPoint, segs[PHOTOGONIOMETER].EndPoint);
                                            segs[PHOTOGONIOMETER] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(NARCOTRAFICANTE);
                                            segs.Add(new GLineSegment(segs[NARCOTRAFICANTE].StartPoint, r.RightButtom));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].Segs[NARCOTRAFICANTE].EndPoint;
                                }
                                else
                                {
                                    drawNormal();
                                }
                            }
                        }
                        for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
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
                        var gap = UNDERACHIEVEMENT;
                        var factor = LYMPHANGIOMATOUS;
                        double height = THESAURUSDETEST;
                        var width = height * factor * factor * Math.Max(text1?.Length ?? NARCOTRAFICANTE, text2?.Length ?? NARCOTRAFICANTE) + THESAURUSINDUSTRY;
                        if (width < THESAURUSWAYWARD) width = THESAURUSWAYWARD;
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSALCOVE, THESAURUSALCOVE), new Vector2d(width, NARCOTRAFICANTE) };
                        if (isLeftOrRight == THESAURUSSEMBLANCE)
                        {
                            vecs = vecs.GetYAxisMirror();
                        }
                        var segs = vecs.ToGLineSegments(basePt).ToList();
                        foreach (var seg in segs)
                        {
                            var line = DrawLineSegmentLazy(seg);
                            Dr.SetLabelStylesForDraiNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[ADRENOCORTICOTROPHIC].EndPoint : segs[ADRENOCORTICOTROPHIC].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(UNDERACHIEVEMENT);
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
                    for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            var segs = info.DisplaySegs ?? info.Segs;
                            if (segs != null)
                            {
                                drawDomePipes(segs, THESAURUSREDOUND);
                            }
                        }
                    }
                    {
                        var _allSmoothStoreys = new List<string>();
                        for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
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
                        var _storeys = new string[] { _allSmoothStoreys.GetAt(PHOTOGONIOMETER), _allSmoothStoreys.GetLastOrDefault(THESAURUSPOSTSCRIPT) }.SelectNotNull().Distinct().ToList();
                        if (_storeys.Count == NARCOTRAFICANTE)
                        {
                            _storeys = new string[] { _allSmoothStoreys.FirstOrDefault(), _allSmoothStoreys.LastOrDefault() }.SelectNotNull().Distinct().ToList();
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
                        foreach (var storey in _storeys)
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = infos[i];
                            {
                                string label1, label2;
                                var isLeftOrRight = !thwPipeLine.Labels.Any(x => IsFL(x));
                                var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).Distinct().OrderBy(x => x).ToList();
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
                                drawLabel(info.PlBasePt, label1, label2, isLeftOrRight);
                            }
                            if (gpItem.HasTl)
                            {
                                string label1, label2;
                                var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => IsTL(x))).Distinct().OrderBy(x => x).ToList();
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
                                drawLabel(info.PlBasePt.OffsetX(THESAURUSINTENTIONAL), label1, label2, UNTRACEABLENESS);
                            }
                        }
                    }
                    bool getShouldToggleBlueMiddleLine()
                    {
                        return viewModel?.Params?.通气H件隔层布置 ?? UNTRACEABLENESS;
                    }
                    {
                        var _storeys = new string[] { allNumStoreyLabels.GetAt(ADRENOCORTICOTROPHIC), allNumStoreyLabels.GetLastOrDefault(PHOTOGONIOMETER) }.SelectNotNull().Distinct().ToList();
                        if (_storeys.Count == NARCOTRAFICANTE)
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
                                    if (((gpItem.Hangings.TryGet(i + ADRENOCORTICOTROPHIC)?.FloorDrainsCount ?? NARCOTRAFICANTE) > NARCOTRAFICANTE)
                                        || (gpItem.Hangings.TryGet(i)?.HasDoubleSCurve ?? UNTRACEABLENESS))
                                    {
                                        v = new Vector2d(QUOTATIONTRILINEAR, NARCOTRAFICANTE);
                                    }
                                    if (gpItem.IsFL0)
                                    {
                                        Dr.DrawDN_2(info.EndPoint + v, THESAURUSPROCEEDING, viewModel?.Params?.DirtyWaterWellDN ?? SPLANCHNOPLEURE);
                                    }
                                    else
                                    {
                                        Dr.DrawDN_2(info.EndPoint + v, THESAURUSPROCEEDING);
                                    }
                                    if (gpItem.HasTl)
                                    {
                                        Dr.DrawDN_3(info.EndPoint.OffsetXY(THESAURUSINTENTIONAL, NARCOTRAFICANTE), THESAURUSPROCEEDING);
                                    }
                                }
                            }
                        }
                    }
                    var b = UNTRACEABLENESS;
                    for (int i = NARCOTRAFICANTE; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            void TestRightSegsMiddle()
                            {
                                var segs = info.RightSegsMiddle;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSREDOUND);
                                }
                            }
                            void TestRightSegsLast()
                            {
                                var segs = info.RightSegsLast;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSREDOUND);
                                }
                            }
                            void TestRightSegsFirst()
                            {
                                var segs = info.RightSegsFirst;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSREDOUND);
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
                                            drawVentPipes(segs, THESAURUSREDOUND);
                                        }
                                    }
                                    else if (gpItem.MinTl + QUOTATIONHOUSEMAID == storey)
                                    {
                                        var segs = info.RightSegsLast;
                                        if (segs != null)
                                        {
                                            drawVentPipes(segs, THESAURUSREDOUND);
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
                                                if (b) segs = segs.Take(ADRENOCORTICOTROPHIC).ToList();
                                            }
                                            drawVentPipes(segs, THESAURUSREDOUND);
                                        }
                                    }
                                }
                            }
                            Run();
                        }
                    }
                    {
                        var i = allNumStoreyLabels.IndexOf(THESAURUSALCOHOLIC);
                        if (i >= NARCOTRAFICANTE)
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
                                    DN1 = SPLANCHNOPLEURE,
                                };
                                if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                {
                                    var basePt = info.EndPoint;
                                    if (gpItem.HasRainPortForFL0)
                                    {
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -HYDROSTATICALLY), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSAPOCRYPHAL, NARCOTRAFICANTE) };
                                            var segs = vecs.ToGLineSegments(basePt);
                                            drawDomePipes(segs, THESAURUSREDOUND);
                                            var pt = segs.Last().EndPoint.ToPoint3d();
                                            {
                                                Dr.DrawRainPort(pt.OffsetX(THESAURUSALCOVE));
                                                Dr.DrawRainPortLabel(pt.OffsetX(-UNDERACHIEVEMENT));
                                                Dr.DrawStarterPipeHeightLabel(pt.OffsetX(-UNDERACHIEVEMENT + THESAURUSREFEREE));
                                            }
                                        }
                                        if (gpItem.IsConnectedToFloorDrainForFL0)
                                        {
                                            var p = basePt + new Vector2d(THESAURUSORNAMENT, -THESAURUSDROUGHT);
                                            DrawFloorDrain(p.ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY);
                                            var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -THESAURUSINVISIBLE), new Vector2d(-THESAURUSINTENTIONAL, -THESAURUSINTENTIONAL), new Vector2d(-THESAURUSBEAUTIFUL, NARCOTRAFICANTE), new Vector2d(-DELETERIOUSNESS, DELETERIOUSNESS) };
                                            var segs = vecs.ToGLineSegments(p + new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY));
                                            drawDomePipes(segs, THESAURUSREDOUND);
                                        }
                                    }
                                    else
                                    {
                                        var p = basePt + new Vector2d(THESAURUSORNAMENT, -THESAURUSDROUGHT);
                                        if (gpItem.IsFL0)
                                        {
                                            if (gpItem.IsConnectedToFloorDrainForFL0)
                                            {
                                                if (gpItem.MergeFloorDrainForFL0)
                                                {
                                                    DrawFloorDrain(p.ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY);
                                                    {
                                                        var vecs = new List<Vector2d>() { new Vector2d(NARCOTRAFICANTE, -THESAURUSINDUSTRY + THESAURUSELEVATED), new Vector2d(-THESAURUSINTENTIONAL, -THESAURUSINTENTIONAL), new Vector2d(-THESAURUSINDUSTRY - THESAURUSGRACEFUL + EXAGGERATIVENESS * PHOTOGONIOMETER, NARCOTRAFICANTE), new Vector2d(-THESAURUSINTENTIONAL, THESAURUSINTENTIONAL) };
                                                        var segs = vecs.ToGLineSegments(p + new Vector2d(-AUTHORITARIANISM, -THESAURUSANCILLARY));
                                                        drawDomePipes(segs, THESAURUSREDOUND);
                                                        var seg = new List<Vector2d> { new Vector2d(-ELECTROMYOGRAPH, -THESAURUSINVISIBLE), new Vector2d(ALSOCHALCENTERIC, NARCOTRAFICANTE) }.ToGLineSegments(segs.First().StartPoint)[ADRENOCORTICOTROPHIC];
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(NARCOTRAFICANTE, -QUOTATIONTRILINEAR), THESAURUSBLOODSHED, THESAURUSAGGRIEVED);
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSDIRECTIVE, -THESAURUSELEGIAC), new Vector2d(PHOSPHORESCENCE, NARCOTRAFICANTE), new Vector2d(THESAURUSINTENTIONAL, THESAURUSINTENTIONAL), new Vector2d(NARCOTRAFICANTE, THESAURUSINVISIBLE) };
                                                    var segs = vecs.ToGLineSegments(info.EndPoint).Skip(ADRENOCORTICOTROPHIC).ToList();
                                                    drawDomePipes(segs, THESAURUSREDOUND);
                                                    DrawFloorDrain((segs.Last().EndPoint + new Vector2d(AUTHORITARIANISM, THESAURUSANCILLARY)).ToPoint3d(), THESAURUSSEMBLANCE, TRANSUBSTANTIALLY);
                                                }
                                            }
                                        }
                                        DrawOutlets1(THESAURUSREDOUND, basePt, PLURALISTICALLY, output,  isRainWaterWell: THESAURUSSEMBLANCE);
                                    }
                                }
                                else if (gpItem.IsSingleOutlet)
                                {
                                    void DrawOutlets3(string shadow, Point2d basePoint)
                                    {
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                        {
                                            Dr.DrawSimpleLabel(basePoint.OffsetY(-THESAURUSINDUSTRY), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                        }
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -KONSTITUTSIONNYĬ), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSCUPIDITY, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, VENTRILOQUISTIC), new Vector2d(THESAURUSDETAIL, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, DISCONTINUAUNCE) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                        drawDomePipes(segs, THESAURUSREDOUND);
                                        DrawDiryWaterWells1(segs[PHOTOGONIOMETER].EndPoint + new Vector2d(-THESAURUSALCOVE, THESAURUSINTENTIONAL), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[THESAURUSPOSTSCRIPT].StartPoint.OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[PHOTOGONIOMETER].EndPoint.OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
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
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = THESAURUSSCAVENGER)
                                                {
                                                    DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, label } }, cb: br => { ByLayer(br); });
                                                }
                                                var p1 = segs[PHOTOGONIOMETER].EndPoint.OffsetX(THESAURUSALCOVE);
                                                var p2 = p1.OffsetY(-QUOTATIONCAPABLE);
                                                var p3 = p2.OffsetX(HYDROSTATICALLY);
                                                var layer = THESAURUSPROCEEDING;
                                                DrawLine(layer, new GLineSegment(p1, p2));
                                                DrawLine(layer, new GLineSegment(p3, p2));
                                                DrawStoreyHeightSymbol(p3, THESAURUSPROCEEDING, gpItem.OutletWrappingPipeRadius);
                                                {
                                                    var _shadow = THESAURUSREDOUND;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > ADRENOCORTICOTROPHIC)
                                                    {
                                                        Dr.DrawSimpleLabel(p3, INCOMBUSTIBLENESS + _shadow.Substring(ADRENOCORTICOTROPHIC));
                                                    }
                                                }
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[THESAURUSPOSTSCRIPT].StartPoint.OffsetXY(THESAURUSARTISAN, UNDERACHIEVEMENT));
                                        DrawNoteText(output.DN2, segs[PHOTOGONIOMETER].EndPoint.OffsetXY(THESAURUSARTISAN, UNDERACHIEVEMENT));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSTITTER].StartPoint.ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[PHOTOGONIOMETER].StartPoint.ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                                        DrawCleaningPort(segs[THESAURUSFACTOR].EndPoint.ToPoint3d(), THESAURUSSEMBLANCE, PHOTOGONIOMETER);
                                    }
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1 = gpItem.HasWrappingPipe;
                                    output.DN2 = SPLANCHNOPLEURE;
                                    DrawOutlets3(THESAURUSREDOUND, info.EndPoint);
                                }
                                else if (gpItem.FloorDrainsCountAt1F > NARCOTRAFICANTE)
                                {
                                    for (int k = NARCOTRAFICANTE; k < gpItem.FloorDrainsCountAt1F; k++)
                                    {
                                        var p = info.EndPoint + new Vector2d(THESAURUSORNAMENT + k * THESAURUSBEAUTIFUL, -THESAURUSDROUGHT);
                                        DrawFloorDrain(p.ToPoint3d(), UNTRACEABLENESS, IRRECONCILIABLE);
                                        var v = new Vector2d(AUTHORITARIANISM, -THESAURUSANCILLARY);
                                        Get2FloorDrainDN(out string v1, out string v2);
                                        if (k == NARCOTRAFICANTE)
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(-ELECTROCHEMISTRY + THESAURUSREDDEN, -INTERROGATIVELY), new Vector2d(UNGOVERNABILITY - THESAURUSREDDEN, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, THESAURUSINVISIBLE) };
                                            var segs = vecs.ToGLineSegments(p + v).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var v3 = gpItem.FloorDrainsCountAt1F == ADRENOCORTICOTROPHIC ? v1 : v2;
                                            var p1 = segs[NARCOTRAFICANTE].EndPoint;
                                            DrawNoteText(v3, p1.OffsetXY(-QUOTATIONTRILINEAR - THESAURUSBEWAIL, -THESAURUSALCOVE).OffsetY(-THESAURUSEFFULGENT));
                                            drawDomePipes(new List<Vector2d> { new Vector2d(-AUTHORITARIANISM, -THESAURUSHEADQUARTERS), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSDRAMATIC, NARCOTRAFICANTE) }.ToGLineSegments(p.OffsetY(-THESAURUSEFFULGENT)).Skip(ADRENOCORTICOTROPHIC), THESAURUSREDOUND);
                                        }
                                        else
                                        {
                                            var p2 = p + v;
                                            var vecs = new List<Vector2d> { new Vector2d(-CONTEMPORANEOUS, -INTERROGATIVELY), new Vector2d(HISTOCOMPATIBILITY, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, THESAURUSINVISIBLE) };
                                            var segs = vecs.ToGLineSegments(p2).Skip(ADRENOCORTICOTROPHIC).ToList();
                                            var p1 = segs[NARCOTRAFICANTE].StartPoint;
                                            DrawNoteText(v1, p1.OffsetXY(THESAURUSINDUSTRY, -THESAURUSALCOVE).OffsetY(-THESAURUSEFFULGENT));
                                            drawDomePipes(new List<Vector2d> { new Vector2d(-AUTHORITARIANISM, -THESAURUSHEADQUARTERS), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-HISTOCOMPATIBILITY, NARCOTRAFICANTE) }.ToGLineSegments(p.OffsetY(-THESAURUSEFFULGENT)).Skip(ADRENOCORTICOTROPHIC), THESAURUSREDOUND);
                                        }
                                    }
                                    DrawOutlets1(THESAURUSREDOUND, info.EndPoint, PLURALISTICALLY, output,  fixv: new Vector2d(NARCOTRAFICANTE, -THESAURUSLEVITATE));
                                }
                                else if (gpItem.HasBasinInKitchenAt1F)
                                {
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1;
                                    output.DN2 = SPLANCHNOPLEURE;
                                    output.DN1 = getOtherFloorDrainDN();
                                    void DrawOutlets4(string shadow, Point2d basePoint, double HEIGHT)
                                    {
                                        var v = THESAURUSRECEPTACLE;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                        {
                                            Dr.DrawSimpleLabel(basePoint.OffsetY(-THESAURUSINDUSTRY), INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                        }
                                        var dx = THESAURUSTATTLE;
                                        if (getDSCurveValue() == JUXTAPOSITIONAL && v == THESAURUSRECEPTACLE)
                                        {
                                            dx = THESAURUSAGREEABLE;
                                        }
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, -KONSTITUTSIONNYĬ), new Vector2d(-CONSTITUTIONALLY, -CONSTITUTIONALLY), new Vector2d(-THESAURUSCUPIDITY - THESAURUSALCOVE, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, VENTRILOQUISTIC), new Vector2d(THESAURUSDETAIL + dx, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, DISCONTINUAUNCE) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(THESAURUSPOSTSCRIPT);
                                        DrawDiryWaterWells1(segs[PHOTOGONIOMETER].EndPoint + new Vector2d(-THESAURUSALCOVE, THESAURUSINTENTIONAL), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[THESAURUSPOSTSCRIPT].StartPoint.OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[PHOTOGONIOMETER].EndPoint.OffsetX(THESAURUSINTENTIONAL), THESAURUSREDOUND);
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
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = THESAURUSSCAVENGER)
                                                {
                                                    DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, label } }, cb: br => { ByLayer(br); });
                                                }
                                                var p10 = segs[PHOTOGONIOMETER].EndPoint.OffsetX(THESAURUSALCOVE);
                                                var p20 = p10.OffsetY(-QUOTATIONCAPABLE);
                                                var p30 = p20.OffsetX(HYDROSTATICALLY);
                                                var layer = THESAURUSPROCEEDING;
                                                DrawLine(layer, new GLineSegment(p10, p20));
                                                DrawLine(layer, new GLineSegment(p30, p20));
                                                DrawStoreyHeightSymbol(p30, THESAURUSPROCEEDING, gpItem.OutletWrappingPipeRadius);
                                                {
                                                    var _shadow = THESAURUSREDOUND;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > ADRENOCORTICOTROPHIC)
                                                    {
                                                        Dr.DrawSimpleLabel(p30, INCOMBUSTIBLENESS + _shadow.Substring(ADRENOCORTICOTROPHIC));
                                                    }
                                                }
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[THESAURUSPOSTSCRIPT].StartPoint.OffsetXY(THESAURUSARTISAN, UNDERACHIEVEMENT));
                                        DrawNoteText(output.DN2, segs[PHOTOGONIOMETER].EndPoint.OffsetXY(THESAURUSARTISAN, UNDERACHIEVEMENT));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSTITTER].StartPoint.ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[PHOTOGONIOMETER].StartPoint.ToPoint3d(), UNTRACEABLENESS, ADRENOCORTICOTROPHIC);
                                        var p = segs[THESAURUSFACTOR].EndPoint;
                                        var fixY = THESAURUSINTENTIONAL + HEIGHT / THESAURUSFACTOR;
                                        var p1 = p.OffsetX(-CONSTITUTIONALLY) + new Vector2d(-THESAURUSDETACHMENT + THESAURUSALCOVE, fixY);
                                        DrawDSCurve(p1, THESAURUSSEMBLANCE, v, THESAURUSREDOUND);
                                        var p2 = p1.OffsetY(-fixY);
                                        segs.Add(new GLineSegment(p1, p2));
                                        if (v == JUXTAPOSITIONAL)
                                        {
                                            var p5 = segs[THESAURUSPOSTSCRIPT].StartPoint;
                                            var _segs = new List<Vector2d> { new Vector2d(UNOBTRUSIVENESS, NARCOTRAFICANTE), new Vector2d(CONSTITUTIONALLY, CONSTITUTIONALLY), new Vector2d(NARCOTRAFICANTE, SPHYGMOMANOMETER), new Vector2d(-HYDROSTATICALLY, NARCOTRAFICANTE) }.ToGLineSegments(p5);
                                            segs = segs.Take(THESAURUSPOSTSCRIPT).ToList();
                                            segs.AddRange(_segs);
                                        }
                                        drawDomePipes(segs, THESAURUSREDOUND);
                                    }
                                    DrawOutlets4(THESAURUSREDOUND, info.EndPoint, HEIGHT);
                                }
                                else
                                {
                                    DrawOutlets1(THESAURUSREDOUND, info.EndPoint, PLURALISTICALLY, output);
                                }
                            }
                        }
                    }
                    {
                        var linesKillers = new HashSet<Geometry>();
                        if (gpItem.IsFL0)
                        {
                            for (int i = gpItem.Items.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; --i)
                            {
                                if (gpItem.Items[i].Exist)
                                {
                                    var info = infos[i];
                                    DrawAiringSymbol(info.StartPoint, getCouldHavePeopleOnRoof(), THESAURUSCOMMOTION);
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
                            for (int i = NARCOTRAFICANTE; i < gpItem.Hangings.Count; i++)
                            {
                                var hanging = gpItem.Hangings[i];
                                if (allStoreys[i] == gpItem.MaxTl + QUOTATIONHOUSEMAID)
                                {
                                    var info = infos[i];
                                    if (info != null)
                                    {
                                        foreach (var seg in info.RightSegsFirst)
                                        {
                                            lines.Remove(seg);
                                        }
                                        var k = HEIGHT / THESAURUSINFERENCE;
                                        var vecs = new List<Vector2d> { new Vector2d(NARCOTRAFICANTE, (THESAURUSBEAUTIFUL + THESAURUSALCOVE) * k), new Vector2d(THESAURUSINTENTIONAL, (-DOMINEERINGNESS) * k), new Vector2d(NARCOTRAFICANTE, (-THESAURUSAPPORTION - THESAURUSALCOVE) * k) };
                                        var segs = vecs.ToGLineSegments(info.EndPoint).Skip(ADRENOCORTICOTROPHIC).ToList();
                                        lines.AddRange(segs);
                                        var shadow = THESAURUSREDOUND;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > ADRENOCORTICOTROPHIC)
                                        {
                                            Dr.DrawSimpleLabel(segs.First().StartPoint, INCOMBUSTIBLENESS + shadow.Substring(ADRENOCORTICOTROPHIC));
                                        }
                                    }
                                    break;
                                }
                            }
                            vent_lines = lines.ToList();
                        }
                    }
                    {
                        var auto_conn = UNTRACEABLENESS;
                        var layer = gpItem.Labels.Any(IsFL0) ? THESAURUSCOMMOTION : dome_layer;
                        if (auto_conn)
                        {
                            foreach (var g in GeoFac.GroupParallelLines(dome_lines, ADRENOCORTICOTROPHIC, QUOTATIONEXOPHTHALMIC))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: THESAURUSDEPOSIT));
                                line.Layer = layer;
                                ByLayer(line);
                            }
                            foreach (var g in GeoFac.GroupParallelLines(vent_lines, ADRENOCORTICOTROPHIC, QUOTATIONEXOPHTHALMIC))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: THESAURUSDEPOSIT));
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
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof, string layer = THESAURUSOVERWHELM)
        {
            DrawAiringSymbol(pt, canPeopleBeOnRoof ? ADVERTISEMENTAL : ORTHONORMALIZING, layer);
        }
        public static void DrawAiringSymbol(Point2d pt, string name, string layer)
        {
            DrawBlockReference(blkName: THESAURUSIGNORANCE, basePt: pt.ToPoint3d(), layer: layer, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(PHOTOCONDUCTING, name);
            });
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: STAPHYLORRHAPHY, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: THESAURUSOVERWHELM, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(THERMOREGULATORY, offsetY);
                br.ObjectId.SetDynBlockValue(PHOTOCONDUCTING, THESAURUSSURFACE);
            });
        }
        public static CommandContext commandContext;
        public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
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
            var items = pipeIds.Select(id => LabelItem.Parse(id)).Where(m => m != null).ToList();
            var rest = pipeIds.Except(items.Select(x => x.Label)).ToList();
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2).ToList());
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
        public static void CollectFloorListDatasEx(bool focus)
        {
            if (focus) FocusMainWindow();
            ThMEPWSS.Common.FramedReadUtil.SelectFloorFramed(out _, () =>
            {
                using (DocLock)
                {
                    var range = TrySelectRangeEx();
                    TryUpdateByRange(range, false);
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
                CadCache.SetCache(CadCache.CurrentFile, "SelectedRange", range);
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
        public static bool CreateDrainageDrawingData(out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            if (noWL && geoData.Labels.Any(x => IsWL(x.Text)))
            {
                MessageBox.Show(INCOMMENSURABILIS);
                drDatas = null;
                return UNTRACEABLENESS;
            }
            drDatas = _CreateDrainageDrawingData(geoData, THESAURUSSEMBLANCE);
            return THESAURUSSEMBLANCE;
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
        public static bool CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = UNTRACEABLENESS)
        {
            CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData);
            return CreateDrainageDrawingData(out drDatas, noWL, geoData);
        }
        public static bool CollectDrainageData(AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, CommandContext ctx, bool noWL = UNTRACEABLENESS)
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
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSSEMBLANCE).ToList();
            FixStoreys(storeys);
            return storeys;
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSSEMBLANCE))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { FINNLANDISIERUNG, THESAURUSOVERWHELM, THESAURUSPROCEEDING, THESAURUSUNDERSTATE, THESAURUSPRELIMINARY, VERGELTUNGSWAFFE, IMMUNOELECTROPHORESIS });
                var storeys = commandContext.StoreyContext.StoreyInfos;
                List<StoreyItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                var range = commandContext.range;
                if (range != null)
                {
                    if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSSEMBLANCE)) return;
                }
                else
                {
                    if (!CollectDrainageData(adb, out storeysItems, out drDatas, commandContext, noWL: THESAURUSSEMBLANCE)) return;
                }
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, DrainageSystemDiagram.commandContext?.ViewModel, out List<int> allNumStoreys, out List<string> allRfStoreys);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + QUOTATIONHOUSEMAID).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - ADRENOCORTICOTROPHIC;
                var end = NARCOTRAFICANTE;
                var OFFSET_X = THESAURUSWOMANLY;
                var SPAN_X = THESAURUSCONTINUATION + HYDROSTATICALLY + THESAURUSATTRACTION;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSINFERENCE;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSINFERENCE;
                var __dy = THESAURUSINTENTIONAL;
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSSEMBLANCE))
            {
                List<StoreyItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSSEMBLANCE)) return;
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
            var minS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > NARCOTRAFICANTE).Min();
            var maxS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > NARCOTRAFICANTE).Max();
            var countS = maxS - minS + ADRENOCORTICOTROPHIC;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            allRfStoreys = _storeys.Where(x => !IsNumStorey(x)).ToList();
            var allNumStoreyLabels = allNumStoreys.Select(x => x + QUOTATIONHOUSEMAID).ToList();
            bool getCanHaveDownboard()
            {
                return vm?.Params?.CanHaveDownboard ?? THESAURUSSEMBLANCE;
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
            string getWaterPortLabel(string label)
            {
                foreach (var drData in drDatas)
                {
                    if (drData.Outlets.TryGetValue(label, out string value))
                    {
                        return value;
                    }
                }
                return THESAURUSHEARTLESS;
            }
            bool hasWaterPort(string label)
            {
                return getWaterPortLabel(label) != null;
            }
            int getMinTl()
            {
                var scores = new List<int>();
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                if (scores.Count == NARCOTRAFICANTE) return NARCOTRAFICANTE;
                var ret = scores.Min() - ADRENOCORTICOTROPHIC;
                if (ret <= NARCOTRAFICANTE) return ADRENOCORTICOTROPHIC;
                return ret;
            }
            int getMaxTl()
            {
                var scores = new List<int>();
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return scores.Count == NARCOTRAFICANTE ? NARCOTRAFICANTE : scores.Max();
            }
            bool is4Tune(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return UNTRACEABLENESS;
            }
            bool getIsShunt(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return UNTRACEABLENESS;
            }
            int getSingleOutletFDCount(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return NARCOTRAFICANTE;
            }
            int getFDCount(string label, string storey)
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
            int getCirclesCount(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return NARCOTRAFICANTE;
            }
            bool isKitchen(string label, string storey)
            {
                if (IsFL0(label)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return UNTRACEABLENESS;
            }
            bool isBalcony(string label, string storey)
            {
                if (IsFL0(label)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return UNTRACEABLENESS;
            }
            bool getIsConnectedToFloorDrainForFL0(string label)
            {
                if (!IsFL0(label)) return UNTRACEABLENESS;
                bool f(string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                    return UNTRACEABLENESS;
                }
                return f(THESAURUSALCOHOLIC) || f(THESAURUSLADYLIKE);
            }
            bool getHasRainPort(string label)
            {
                if (!IsFL0(label)) return UNTRACEABLENESS;
                bool f(string storey)
                {
                    for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                    return UNTRACEABLENESS;
                }
                return f(THESAURUSALCOHOLIC) || f(THESAURUSLADYLIKE);
            }
            bool isToilet(string label, string storey)
            {
                if (IsFL0(label)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return UNTRACEABLENESS;
            }
            int getWashingMachineFloorDrainsCount(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
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
                return NARCOTRAFICANTE;
            }
            bool IsSeries(string label, string storey)
            {
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.Shunts.Contains(label))
                            {
                                return UNTRACEABLENESS;
                            }
                        }
                    }
                }
                return THESAURUSSEMBLANCE;
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
            int getFloorDrainsCountAt1F(string label)
            {
                if (!IsFL(label)) return NARCOTRAFICANTE;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            drData.FloorDrains.TryGetValue(label, out int r);
                            return r;
                        }
                    }
                }
                return NARCOTRAFICANTE;
            }
            bool getIsMerge(string label)
            {
                if (!IsFL0(label)) return UNTRACEABLENESS;
                for (int i = NARCOTRAFICANTE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSALCOHOLIC)
                        {
                            var drData = drDatas[i];
                            if (drData.Merges.Contains(label))
                            {
                                return THESAURUSSEMBLANCE;
                            }
                        }
                    }
                }
                return UNTRACEABLENESS;
            }
            bool HasKitchenWashingMachine(string label, string storey)
            {
                return UNTRACEABLENESS;
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
                    item.HasTL = THESAURUSSEMBLANCE;
                    if (item.MinTl <= NARCOTRAFICANTE || item.MaxTl <= ADRENOCORTICOTROPHIC || item.MinTl >= item.MaxTl)
                    {
                        item.HasTL = UNTRACEABLENESS;
                        item.MinTl = item.MaxTl = NARCOTRAFICANTE;
                    }
                    if (item.HasTL && item.MaxTl == maxS)
                    {
                        item.MoveTlLineUpper = THESAURUSSEMBLANCE;
                    }
                    item.TlLabel = pl.Replace(INATTENTIVENESS, QUOTATION1CBARBADOS);
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
                item.OutletWrappingPipeRadius ??= THESAURUSDISCLOSE;
            }
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
                item.FloorDrainsCountAt1F = Math.Max(item.FloorDrainsCountAt1F, getSingleOutletFDCount(label, THESAURUSALCOHOLIC));
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                foreach (var hanging in item.Hangings)
                {
                    if (hanging.FloorDrainsCount > PHOTOGONIOMETER)
                    {
                        hanging.FloorDrainsCount = PHOTOGONIOMETER;
                    }
                    if (isKitchen(label, hanging.Storey))
                    {
                        if (hanging.FloorDrainsCount == NARCOTRAFICANTE)
                        {
                            hanging.HasDoubleSCurve = THESAURUSSEMBLANCE;
                        }
                    }
                    if (isKitchen(label, hanging.Storey))
                    {
                        hanging.RoomName = THESAURUSAGHAST;
                    }
                    else if (isBalcony(label, hanging.Storey))
                    {
                        hanging.RoomName = CONGREGATIONIST;
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
                            hanging.HasDoubleSCurve = THESAURUSSEMBLANCE;
                        }
                        if (hanging.Storey == THESAURUSALCOHOLIC)
                        {
                            if (isKitchen(label, hanging.Storey))
                            {
                                hanging.HasDoubleSCurve = UNTRACEABLENESS;
                                item.HasBasinInKitchenAt1F = THESAURUSSEMBLANCE;
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
                            hanging.FloorDrainsCount = NARCOTRAFICANTE;
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
                        item.IsFL0 = THESAURUSSEMBLANCE;
                        item.HasRainPortForFL0 = getHasRainPort(item.Label);
                        item.IsConnectedToFloorDrainForFL0 = getIsConnectedToFloorDrainForFL0(item.Label);
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = ADRENOCORTICOTROPHIC;
                            hanging.HasSCurve = UNTRACEABLENESS;
                            hanging.HasDoubleSCurve = UNTRACEABLENESS;
                            hanging.HasCleaningPort = UNTRACEABLENESS;
                            if (hanging.Storey == THESAURUSALCOHOLIC)
                            {
                                hanging.FloorDrainsCount = getSingleOutletFDCount(kv.Key, THESAURUSALCOHOLIC);
                            }
                        }
                        if (item.IsConnectedToFloorDrainForFL0) item.MergeFloorDrainForFL0 = getIsMerge(kv.Key);
                    }
                }
            }
            {
                foreach (var item in pipeInfoDict.Values)
                {
                    for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                    {
                        if (!item.Items[i].Exist) continue;
                        var hanging = item.Hangings[i];
                        var storey = allNumStoreyLabels[i];
                        hanging.HasCleaningPort = IsPL(item.Label) || IsDL(item.Label);
                        hanging.HasDownBoardLine = IsPL(item.Label) || IsDL(item.Label);
                        {
                            var m = item.Items.TryGet(i - ADRENOCORTICOTROPHIC);
                            if ((m.Exist && m.HasLong) || storey == THESAURUSALCOHOLIC)
                            {
                                hanging.HasCheckPoint = THESAURUSSEMBLANCE;
                            }
                        }
                        if (hanging.HasCleaningPort)
                        {
                            hanging.HasCheckPoint = THESAURUSSEMBLANCE;
                        }
                        if (hanging.HasDoubleSCurve)
                        {
                            hanging.HasCheckPoint = THESAURUSSEMBLANCE;
                        }
                        if (hanging.WashingMachineFloorDrainsCount > NARCOTRAFICANTE)
                        {
                            hanging.HasCheckPoint = THESAURUSSEMBLANCE;
                        }
                        if (GetStoreyScore(storey) == maxS)
                        {
                            hanging.HasCleaningPort = UNTRACEABLENESS;
                            hanging.HasDownBoardLine = UNTRACEABLENESS;
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
                        item.CanHaveAring = THESAURUSSEMBLANCE;
                    }
                    if (testExist(label, maxS + QUOTATIONHOUSEMAID))
                    {
                        item.CanHaveAring = THESAURUSSEMBLANCE;
                    }
                    if (IsFL0(item.Label))
                    {
                        item.CanHaveAring = UNTRACEABLENESS;
                    }
                }
            }
            {
                if (allNumStoreys.Max() < ARCHAEOLOGICALLY)
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        item.HasTL = UNTRACEABLENESS;
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
                        if (hanging.Storey == THESAURUSALCOHOLIC)
                        {
                            if (isToilet(label, THESAURUSALCOHOLIC))
                            {
                                item.IsSingleOutlet = THESAURUSSEMBLANCE;
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
                    for (int i = item.Items.Count - ADRENOCORTICOTROPHIC; i >= NARCOTRAFICANTE; --i)
                    {
                        if (item.Items[i].Exist)
                        {
                            item.Items[i] = default;
                            break;
                        }
                    }
                }
                for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                {
                    var hanging = item.Hangings[i];
                    if (hanging == null) continue;
                    if (hanging.Storey == maxS + QUOTATIONHOUSEMAID)
                    {
                        if (item.Items[i].HasShort)
                        {
                            var m = item.Items[i];
                            m.HasShort = UNTRACEABLENESS;
                            m.HasLong = THESAURUSSEMBLANCE;
                            m.DrawLongHLineHigher = THESAURUSSEMBLANCE;
                            item.Items[i] = m;
                            hanging.HasDownBoardLine = UNTRACEABLENESS;
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
                    for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
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
                    for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                    {
                        var h1 = item.Hangings[i];
                        var h2 = item.Hangings.TryGet(i + ADRENOCORTICOTROPHIC);
                        if (item.Items[i].HasLong && item.Items.TryGet(i + ADRENOCORTICOTROPHIC).Exist && h2 != null)
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
                for (int i = NARCOTRAFICANTE; i < item.Hangings.Count; i++)
                {
                    var h1 = item.Hangings[i];
                    var h2 = item.Hangings.TryGet(i + ADRENOCORTICOTROPHIC);
                    if (h2 == null) continue;
                    if (!h2.HasCleaningPort)
                    {
                        h1.HasDownBoardLine = UNTRACEABLENESS;
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
                        h.HasDownBoardLine = UNTRACEABLENESS;
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
                if (label is null) return NARCOTRAFICANTE;
                if (IsPL((label))) return ADRENOCORTICOTROPHIC;
                if (IsFL0((label))) return PHOTOGONIOMETER;
                if (IsFL((label))) return THESAURUSPOSTSCRIPT;
                return int.MaxValue;
            }).ThenBy(x =>
            {
                return x.Labels.FirstOrDefault();
            }).ToList();
            return pipeGroupedItems;
        }
        public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * LYMPHANGIOMATOUS;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSLEVITATE;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-THESAURUSENTREAT);
            var p3 = p1.OffsetX(THESAURUSENTREAT);
            var line = DrawLineLazy(p2, p3);
            line.Layer = THESAURUSPROCEEDING;
            ByLayer(line);
        }
        public static void DrawPipeButtomHeightSymbol(Point2d p, List<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: p.ToPoint3d(),
      props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, ALSODIDACTYLOUS } },
      cb: br =>
      {
          br.Layer = THESAURUSPROCEEDING;
      });
        }
        public static void DrawPipeButtomHeightSymbol(double w, double h, Point2d p)
        {
            var vecs = new List<Vector2d> { new Vector2d(w, NARCOTRAFICANTE), new Vector2d(NARCOTRAFICANTE, h) };
            var segs = vecs.ToGLineSegments(p);
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: UNEXCEPTIONABLE, basePt: segs.Last().EndPoint.OffsetX(THESAURUSDEMONSTRATION).ToPoint3d(),
      props: new Dictionary<string, string>() { { UNEXCEPTIONABLE, ALSODIDACTYLOUS } },
      cb: br =>
      {
          br.Layer = THESAURUSPROCEEDING;
      });
        }
        public static void DrawStoreyLine(string label, Point2d basePt, double lineLen, string text)
        {
            DrawStoreyLine(label, basePt.ToPoint3d(), lineLen, text);
        }
        public static void DrawStoreyLine(string label, Point3d basePt, double lineLen, string text)
        {
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
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
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
        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = THESAURUSPROCEEDING;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = LYMPHANGIOMATOUS;
                    SetTextStyleLazy(t, THESAURUSTRAFFIC);
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
            line.Layer = THESAURUSOVERWHELM;
            ByLayer(line);
        }
        public static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSCOMMOTION;
                ByLayer(line);
            });
        }
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSPROCEEDING;
                ByLayer(line);
            });
        }
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, THESAURUSDETEST, pt);
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
                if (value is JUXTAPOSITIONAL)
                {
                    basePt += new Vector2d(AUTHORITARIANISM, -THESAURUSALCOVE).ToVector3d();
                }
                DrawBlockReference(UNSOPHISTICATEDLY, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSUNDERSTATE;
                      br.ScaleFactors = new Scale3d(PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, value);
                      }
                  });
            }
            else
            {
                DrawBlockReference(UNSOPHISTICATEDLY, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSUNDERSTATE;
                      br.ScaleFactors = new Scale3d(-PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, value);
                      }
                  });
            }
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = ELECTROMAGNETISM)
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DrawBlockReference(RECONSTRUCTIONAL, basePt, br =>
                {
                    br.Layer = THESAURUSUNDERSTATE;
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
                   br.Layer = THESAURUSUNDERSTATE;
                   ByLayer(br);
                   br.ScaleFactors = new Scale3d(-PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, value);
                   }
               });
            }
        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSFLAGRANT, basePt, br =>
                {
                    br.Layer = THESAURUSUNDERSTATE;
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, THESAURUSINFERTILE);
                        br.ObjectId.SetDynBlockValue(CONTRACEPTIVELY, (short)ADRENOCORTICOTROPHIC);
                    }
                });
            }
            else
            {
                DrawBlockReference(THESAURUSFLAGRANT, basePt,
                   br =>
                   {
                       br.Layer = THESAURUSUNDERSTATE;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-PHOTOGONIOMETER, PHOTOGONIOMETER, PHOTOGONIOMETER);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(CRYSTALLIZATIONS, THESAURUSINFERTILE);
                           br.ObjectId.SetDynBlockValue(CONTRACEPTIVELY, (short)ADRENOCORTICOTROPHIC);
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
                DrawBlockReference(THESAURUSDESECRATE, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSUNDERSTATE;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(THESAURUSITINERANT);
                });
            }
            else
            {
                DrawBlockReference(THESAURUSDESECRATE, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSUNDERSTATE;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(THESAURUSITINERANT + AUTHORITARIANISM);
                });
            }
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
        public static void DrawDiryWaterWells2(Point2d pt, List<string> values)
        {
            var dx = NARCOTRAFICANTE;
            foreach (var value in values)
            {
                DrawDirtyWaterWell(pt.OffsetX(THESAURUSALCOVE) + new Vector2d(dx, NARCOTRAFICANTE), value);
                dx += THESAURUSORNAMENT;
            }
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: THESAURUSBALLAST, basePt: basePt.OffsetY(-THESAURUSALCOVE),
          props: new Dictionary<string, string>() { { THESAURUSHEARTLESS, value } },
          cb: br =>
          {
              br.Layer = THESAURUSSANCTITY;
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
            DrawBlockReference(blkName: THESAURUSSTUPEFACTION, basePt: basePt.OffsetY(-THESAURUSALCOVE),
            props: new Dictionary<string, string>() { { THESAURUSHEARTLESS, value } },
            cb: br =>
            {
                br.Layer = THESAURUSUNDERSTATE;
                ByLayer(br);
            });
        }
        public static void DrawDiryWaterWells1(Point2d pt, List<string> values, bool isRainWaterWell = UNTRACEABLENESS)
        {
            if (values == null) return;
            if (values.Count == ADRENOCORTICOTROPHIC)
            {
                var dy = -QUOTATIONPAPILLARY;
                if (!isRainWaterWell)
                {
                    DrawDirtyWaterWell(pt.OffsetY(dy), values[NARCOTRAFICANTE]);
                }
                else
                {
                    DrawRainWaterWell(pt.OffsetY(dy), values[NARCOTRAFICANTE]);
                }
            }
            else if (values.Count >= PHOTOGONIOMETER)
            {
                var pts = GetBasePoints(pt.OffsetX(-THESAURUSORNAMENT), PHOTOGONIOMETER, values.Count, THESAURUSORNAMENT, THESAURUSORNAMENT).ToList();
                for (int i = NARCOTRAFICANTE; i < values.Count; i++)
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
    }
    public class ThDrainageService
    {
        public static void PreFixGeoData(DrainageGeoData geoData)
        {
            foreach (var ct in geoData.Labels)
            {
                ct.Text = FixVerticalPipeLabel(ct.Text);
                ct.Boundary = ct.Boundary.Expand(-THESAURUSSENILE);
            }
            geoData.FixData();
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
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSEMBASSY)).ToList();
            }
            {
                geoData.WashingMachines = GeoFac.GroupGeometries(geoData.WashingMachines.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < THESAURUSSACRIFICE && x.Height < THESAURUSSACRIFICE).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, THESAURUSCONSUL))).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(THESAURUSEMBASSY);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.WashingMachines = geoData.WashingMachines.Distinct(cmp).ToList();
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
                    if (e.LayerId.IsNull) return UNTRACEABLENESS;
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
        const int distinguishDiameter = THESAURUSENCOURAGEMENT;
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
        static bool isDrainageLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSCONTRIBUTE); 
        HashSet<Handle> ok_group_handles;
        private void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!IsLayerVisible(entity)) return;
            if (isInXref)
            {
                return;
            }
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
                        if (_dxfName is PALAEOPATHOLOGIST && GetEffectiveLayer(e.Layer) is THESAURUSOVERWHELM)
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
                if (entityLayer.ToUpper() is THESAURUSPERSPIRATION or THESAURUSTHROTTLE)
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
                if (entityLayer.ToUpper() is THESAURUSSQUASHY or THESAURUSMANNERISM)
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
                    if (dxfName is THESAURUSSHAMBLE)
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
                if (entityLayer is THESAURUSACCEDE)
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
                if (dxfName == THESAURUSSUPERVISE && entityLayer is TRANYLCYPROMINE)
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, r, rainPortSymbols);
                }
            }
            {
                if (entity is Circle c && isDrainageLayer(entityLayer))
                {
                    if (distinguishDiameter < c.Radius && c.Radius <= THESAURUSINDUSTRY)
                    {
                        var bd = c.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (THESAURUSFACTOR < c.Radius && c.Radius <= distinguishDiameter && GetEffectiveLayer(c.Layer) is THESAURUSUNDERSTATE or THESAURUSAGGRIEVED)
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
                    if (distinguishDiameter < c.Radius && c.Radius <= THESAURUSINDUSTRY)
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
            if (entityLayer is THESAURUSCOMMOTION)
            {
                if (entity is Line line && line.Length > NARCOTRAFICANTE)
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
                if (dxfName is PALAEOPATHOLOGIST)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wlines);
                    return;
                }
            }
            if (entityLayer is THESAURUSOVERWHELM or UNPROPITIOUSNESS)
            {
                if (entity is Line line && line.Length > NARCOTRAFICANTE)
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
                if (dxfName is PALAEOPATHOLOGIST)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, dlines);
                    return;
                }
            }
            if (dxfName is PALAEOPATHOLOGIST)
            {
                if (entityLayer is INTERROGATORIES or THESAURUSSANCTITY or THESAURUSUNDERSTATE)
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
                if (isDrainageLayer(entityLayer) && entity is Line line && line.Length > NARCOTRAFICANTE)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, labelLines);
                    return;
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
                    if (e is Line line && isDrainageLayer(line.Layer))
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
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                }
                return;
            }
            {
                static bool g(string t) => !t.StartsWith(THESAURUSDILUTE) && !t.ToLower().Contains(THESAURUSAPPLIANCE) && !t.ToUpper().Contains(THESAURUSDEPRESS);
                if (entity is DBText dbt && isDrainageLayer(entityLayer) && g(dbt.TextString))
                {
                    var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                    var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                    return;
                }
            }
            if (dxfName == THESAURUSSHAMBLE)
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
            if (dxfName == THESAURUSINOFFENSIVE)
            {
                if (entityLayer is THESAURUSACCEDE)
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
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSSHAMBLE or INHOMOGENEOUSLY).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible))
                    {
                        foreach (var dbt in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                        {
                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                            if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                        }
                    }
                    foreach (var seg in colle.OfType<Line>().Where(x => x.Length > NARCOTRAFICANTE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                    {
                        reg(fs, seg, labelLines);
                    }
                }
                return;
            }
        }
        const string XREF_LAYER = THESAURUSSHACKLE;
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var name = GetEffectiveBRName(br.GetEffectiveName());
                if (name is THESAURUSSTUPEFACTION)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSHEARTLESS) ?? THESAURUSREDOUND;
                    reg(fs, bd, () =>
                    {
                        waterPorts.Add(bd);
                        waterPortLabels.Add(lb);
                    });
                    return;
                }
                if (name.Contains(NEIGHBOURLINESS))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSHEARTLESS) ?? THESAURUSREDOUND;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                    });
                    return;
                }
                if (!isInXref)
                {
                    if (name.Contains(THESAURUSTRANSMISSION))
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
                                geoData.UpdateFloorDrainTypeDict(bd.Center, br.ObjectId.GetDynBlockValue(CRYSTALLIZATIONS) ?? THESAURUSREDOUND);
                            });
                            DrawRectLazy(bd, THESAURUSCONSUL);
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
                }
                {
                    if (isDrainageLayer(br.Layer))
                    {
                        if (name is THESAURUSUNITED or THESAURUSWRONGFUL)
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(UNDERACHIEVEMENT);
                            reg(fs, bd, pipes);
                            return;
                        }
                        if (name.Contains(ENTERCOMMUNICATE))
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSDERISION);
                            reg(fs, bd, pipes);
                            return;
                        }
                        if (name is COSTERMONGERING)
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(UNANSWERABLENESS);
                            reg(fs, bd, pipes);
                            return;
                        }
                    }
                    if (name is THESAURUSVOUCHER && GetEffectiveLayer(br.Layer) is QUOTATIONPERIODIC or THESAURUSUNDERSTATE or THESAURUSOVERWHELM)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(UNANSWERABLENESS);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is THESAURUSMISUNDERSTANDING && GetEffectiveLayer(br.Layer) is DISPASSIONATELY)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(UNANSWERABLENESS);
                        reg(fs, bd, pipes);
                        return;
                    }
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
                {
                    var ok = UNTRACEABLENESS;
                    if (killerNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        if (!washingMachinesNames.Any(x => name.Contains(x)))
                        {
                            reg(fs, bd, pipeKillers);
                        }
                        ok = THESAURUSSEMBLANCE;
                    }
                    if (basinNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, basins);
                        ok = THESAURUSSEMBLANCE;
                    }
                    if (washingMachinesNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, washingMachines);
                        ok = THESAURUSSEMBLANCE;
                    }
                    if (mopPoolNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, mopPools);
                        ok = THESAURUSSEMBLANCE;
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
        readonly List<string> basinNames = new List<string>() { OVERSENSITIVITY, INTERLINEATIONS, INSTITUTIONALIZE, THIOSEMICARBAZONE, THESAURUSINITIAL, THESAURUSBOUNTY, QUOTATIONPARISIAN };
        readonly List<string> mopPoolNames = new List<string>() { QUOTATIONEVENING, };
        readonly List<string> killerNames = new List<string>() { QUOTATIONPARISIAN, RUDIMENTARINESS, METALINGUISTICS, THESAURUSINHABITANT, THESAURUSSTROKE, HYDROCHARITACEAE, THESAURUSINDENT, THESAURUSCAUCUS };
        readonly List<string> washingMachinesNames = new List<string>() { THESAURUSINFIDEL, THESAURUSCINEMA };
        bool isInXref;
        static bool HandleGroupAtCurrentModelSpaceOnly = UNTRACEABLENESS;
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
                        if (dxfName is PALAEOPATHOLOGIST && GetEffectiveLayer(entity.Layer) is THESAURUSOVERWHELM)
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
                    if (entity.Layer is THESAURUSBITING)
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
                        if (l.Count == ADRENOCORTICOTROPHIC)
                        {
                            list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[NARCOTRAFICANTE]));
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
                    list.Add(new KeyValuePair<string, Geometry>(THESAURUSREDOUND, range));
                }
            }
        }
        static bool CollectRoomDataAtCurrentModelSpaceOnly = UNTRACEABLENESS;
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
                if (pls.Count == NARCOTRAFICANTE) return UNTRACEABLENESS;
            }
            return THESAURUSSEMBLANCE;
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer?.ToUpper() is THESAURUSPERSPIRATION or THESAURUSTHROTTLE)
                .SelectNotNull(ConvertToPolygon).ToList();
            var names = adb.ModelSpace.Where(x => x.Layer?.ToUpper() is THESAURUSSQUASHY or THESAURUSMANNERISM).SelectNotNull(entity =>
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
                if (dxfName == THESAURUSSHAMBLE)
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
                    if (l.Count == ADRENOCORTICOTROPHIC)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[NARCOTRAFICANTE]));
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
                list.Add(new KeyValuePair<string, Geometry>(THESAURUSREDOUND, range));
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
                var e = DrawRectLazy(s).ColorIndex = ADRENOCORTICOTROPHIC;
            }
            var sb = new StringBuilder(THESAURUSVOLITION);
            drDatas = new List<DrainageDrawingData>();
            var _kitchens = roomData.Where(x => IsKitchen(x.Key)).Select(x => x.Value).ToList();
            var _toilets = roomData.Where(x => IsToilet(x.Key)).Select(x => x.Value).ToList();
            var _nonames = roomData.Where(x => x.Key is THESAURUSREDOUND).Select(x => x.Value).ToList();
            var _balconies = roomData.Where(x => IsBalcony(x.Key)).Select(x => x.Value).ToList();
            var _kitchensf = F(_kitchens);
            var _toiletsf = F(_toilets);
            var _nonamesf = F(_nonames);
            var _balconiesf = F(_balconies);
            for (int storeyI = NARCOTRAFICANTE; storeyI < cadDatas.Count; storeyI++)
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
                    var maxDis = THESAURUSOVERFLOW;
                    var angleTolleranceDegree = ADRENOCORTICOTROPHIC;
                    var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > NARCOTRAFICANTE).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
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
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, THESAURUSNETHER).ToList();
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
                        if (lst.Count == ADRENOCORTICOTROPHIC)
                        {
                            var labelline = lst[NARCOTRAFICANTE];
                            var pipes = pipesf(GeoFac.CreateGeometry(label, labelline));
                            if (pipes.Count == NARCOTRAFICANTE)
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
                    var labelsf = F(item.Labels);
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (!IsDrainageLabelText(text)) continue;
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
                foreach (var o in item.WashingMachines)
                {
                    DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)], THESAURUSCONSUL);
                }
                foreach (var o in item.VerticalPipes)
                {
                    DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = THESAURUSPOSTSCRIPT;
                }
                foreach (var o in item.FloorDrains)
                {
                    DrawGeometryLazy(o, ents => ents.ForEach(e => e.ColorIndex = QUOTATIONLENTIFORM));
                }
                foreach (var o in item.WaterPorts)
                {
                    DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = ARCHAEOLOGICALLY;
                    DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WashingMachines)
                {
                    var e = DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = ADRENOCORTICOTROPHIC;
                }
                foreach (var o in item.CleaningPorts)
                {
                    var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                    DrawRectLazy(GRect.Create(m, THESAURUSSENILE));
                }
                {
                    var cl = Color.FromRgb(THESAURUSINDICT, UNCONSTITUTIONALISM, DIHYDROXYSTILBENE);
                    foreach (var o in item.WrappingPipes)
                    {
                        var e = DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(THESAURUSTITTER, COUNTERFEISANCE, INTERVOCALICALLY);
                    foreach (var o in item.DLines)
                    {
                        var e = DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(THESAURUSBELOVED, INSTITUTIONALIZATION, OBOEDIENTIARIUS);
                    foreach (var o in item.VLines)
                    {
                        DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                    }
                }
                foreach (var o in item.WLines)
                {
                    DrawLineSegmentLazy(geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ColorIndex = QUOTATIONLENTIFORM;
                }
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
                                    else if (IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
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
                                foreach (var dlinesGeo in dlinesGeos)
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
                                                if (!IsTL(label))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = THESAURUSSEMBLANCE;
                                                }
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
                                        if (!IsTL(label))
                                        {
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
                    foreach (var dlinesGeo in dlinesGeos)
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
                                if (waterPorts.Count == ADRENOCORTICOTROPHIC)
                                {
                                    var waterPort = waterPorts[NARCOTRAFICANTE];
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
                                            if (wrappingpipes.Count > NARCOTRAFICANTE)
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
                            var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(THESAURUSALCOVE).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = THESAURUSCONSUL;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(QUOTATIONLENTIFORM, UNTRACEABLENESS)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterPort = f5(pt.ToPoint3d());
                                if (waterPort != null)
                                {
                                    if (waterPort.GetCenter().GetDistanceTo(pt) <= ELECTROMYOGRAPH)
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
                            return THESAURUSBREEDING;
                        }).Count();
                    }
                    {
                        drData.OutletWrappingPipeDict = outletWrappingPipe;
                    }
                }
                {
                    var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, THESAURUSNETHER).ToList();
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
                            var waterWells = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(THESAURUSALCOVE).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterWells), waterWell => geoData.WaterPortLabels[spacialIndex[waterWells.IndexOf(waterWell)]], waterWell => spacialIndex[waterWells.IndexOf(waterWell)]);
                        }
                    }
                    {
                        var f2 = F(vps.Except(ok_vpipes).ToList());
                        var radius = THESAURUSCONSUL;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in wlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(QUOTATIONLENTIFORM, UNTRACEABLENESS)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(vps.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterWell = f5(pt.ToPoint3d());
                                if (waterWell != null)
                                {
                                    if (waterWell.GetCenter().GetDistanceTo(pt) <= ELECTROMYOGRAPH)
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
                        var toilet = _toilet.Buffer(VENTRILOQUISTIC);
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
                    for (double buf = THESAURUSINDUSTRY; buf <= VENTRILOQUISTIC; buf += THESAURUSINDUSTRY)
                    {
                        foreach (var kitchen in kitchens)
                        {
                            if (ok_rooms.Contains(kitchen)) continue;
                            var ok = UNTRACEABLENESS;
                            foreach (var toilet in toiletsf(kitchen.Buffer(buf)))
                            {
                                if (ok_rooms.Contains(toilet))
                                {
                                    ok = THESAURUSSEMBLANCE;
                                    break;
                                }
                                foreach (var fl in flsf(toilet))
                                {
                                    ok = THESAURUSSEMBLANCE;
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
                                var geo = GRect.Create(kv.Key, THESAURUSINDUSTRY).ToPolygon();
                                geo.UserData = kv.Value;
                                return geo;
                            }).ToList();
                            var shootersf = GeoFac.CreateIntersectsSelector(shooters);
                            foreach (var fd in fds)
                            {
                                var ok = UNTRACEABLENESS;
                                foreach (var geo in shootersf(fd))
                                {
                                    var name = (string)geo.UserData;
                                    if (!string.IsNullOrWhiteSpace(name))
                                    {
                                        if (name.Contains(THESAURUSRELEASE) || name.Contains(THESAURUSINCAPACITY))
                                        {
                                            ok = THESAURUSSEMBLANCE;
                                            break;
                                        }
                                    }
                                }
                                if (!ok)
                                {
                                    if (washingMachinesf(fd).Any())
                                    {
                                        ok = THESAURUSSEMBLANCE;
                                    }
                                }
                                if (ok)
                                {
                                    washingMachineFds.Add(fd);
                                }
                            }
                            drData.WashingMachineFloorDrains[lbDict[fl]] = washingMachineFds.Count;
                            if (fds.Count == PHOTOGONIOMETER)
                            {
                                bool is4tune;
                                bool isShunt()
                                {
                                    is4tune = UNTRACEABLENESS;
                                    var _dlines = dlinesGeosf(fl);
                                    if (_dlines.Count == NARCOTRAFICANTE) return UNTRACEABLENESS;
                                    if (fds.Count == PHOTOGONIOMETER)
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
                                                if (yyy.Count == ADRENOCORTICOTROPHIC)
                                                {
                                                    var dlines = yyy[NARCOTRAFICANTE];
                                                    if (dlines.Intersects(fds[NARCOTRAFICANTE].Buffer(THESAURUSFACTOR)) && dlines.Intersects(fds[ADRENOCORTICOTROPHIC].Buffer(THESAURUSFACTOR)) && dlines.Intersects(fl.Buffer(THESAURUSFACTOR)))
                                                    {
                                                        if (wrappingPipesf(dlines).Count >= PHOTOGONIOMETER)
                                                        {
                                                            is4tune = THESAURUSSEMBLANCE;
                                                        }
                                                        return UNTRACEABLENESS;
                                                    }
                                                }
                                                else if (yyy.Count == PHOTOGONIOMETER)
                                                {
                                                    var dl1 = yyy[NARCOTRAFICANTE];
                                                    var dl2 = yyy[ADRENOCORTICOTROPHIC];
                                                    var fd1 = fds[NARCOTRAFICANTE].Buffer(THESAURUSFACTOR);
                                                    var fd2 = fds[ADRENOCORTICOTROPHIC].Buffer(THESAURUSFACTOR);
                                                    var vp = fl.Buffer(THESAURUSFACTOR);
                                                    var geos = new List<Geometry>() { fd1, fd2, vp };
                                                    var f = F(geos);
                                                    var l1 = f(dl1);
                                                    var l2 = f(dl2);
                                                    if (l1.Count == PHOTOGONIOMETER && l2.Count == PHOTOGONIOMETER && l1.Contains(vp) && l2.Contains(vp))
                                                    {
                                                        return THESAURUSSEMBLANCE;
                                                    }
                                                    return UNTRACEABLENESS;
                                                }
                                            }
                                            catch
                                            {
                                                return UNTRACEABLENESS;
                                            }
                                        }
                                    }
                                    return UNTRACEABLENESS;
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
                            var supterDLineGeos = GeoFac.GroupGeometries(dlinesGeos.Concat(filtedFds.OfType<Polygon>().Select(x => x.Shell)).ToList()).Select(x => x.Count == ADRENOCORTICOTROPHIC ? x[NARCOTRAFICANTE] : GeoFac.CreateGeometry(x)).ToList();
                            var f = F(supterDLineGeos);
                            foreach (var wp in item.WaterPorts.Select(x => x.ToGRect().Expand(THESAURUSINDUSTRY).ToPolygon().Shell))
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
                            .Select(x => x.Count == ADRENOCORTICOTROPHIC ? x[NARCOTRAFICANTE] : GeoFac.CreateGeometry(x)).ToList();
                        foreach (var g in gs)
                        {
                            var vps = xlsf(g);
                            if (vps.Count > NARCOTRAFICANTE)
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
                            .Select(x => x.Count == ADRENOCORTICOTROPHIC ? x[NARCOTRAFICANTE] : GeoFac.CreateGeometry(x)).ToList();
                        foreach (var g in gs)
                        {
                            var vps = xlsf(g);
                            if (vps.Count > NARCOTRAFICANTE)
                            {
                                var lb = lbDict[vps.First()];
                                drData.circlesCount.TryGetValue(lb, out int v);
                                drData.circlesCount[lb] = Math.Max(v, vps.Count);
                            }
                        }
                    }
                }
                {
                    var wlinesGeos = GeoFac.GroupLinesByConnPoints(item.WLines, THESAURUSNETHER).ToList();
                    var wlinesGeosf = F(wlinesGeos);
                    var merges = new HashSet<string>();
                    var wells = item.WaterWells.Select(x => x.Buffer(HYDROSTATICALLY)).ToList();
                    if (wells.Count > NARCOTRAFICANTE)
                    {
                        var gs = GeoFac.GroupGeometries(FL0s.Concat(item.FloorDrains).Select(xl => CreateXGeoRect(xl.ToGRect())).Concat(wlinesGeos).ToList())
                        .Select(x => x.Count == ADRENOCORTICOTROPHIC ? x[NARCOTRAFICANTE] : GeoFac.CreateGeometry(x)).ToList();
                        var circlesf = F(item.FloorDrains.Concat(FL0s).ToList());
                        var gsf = F(gs);
                        foreach (var well in wells)
                        {
                            var g = G(gsf(well));
                            var circles = circlesf(g);
                            var fl0s = circles.Where(x => FL0s.Contains(x)).ToList();
                            if (fl0s.Count == NARCOTRAFICANTE) continue;
                            var fds = circles.Where(x => item.FloorDrains.Contains(x)).ToList();
                            if (fl0s.Count == ADRENOCORTICOTROPHIC && fds.Count == ADRENOCORTICOTROPHIC)
                            {
                                var fl = fl0s[NARCOTRAFICANTE];
                                var fd = fds[NARCOTRAFICANTE];
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
        const double MAX_SHORTTRANSLATOR_DISTANCE = THESAURUSINTENTIONAL;
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
                    if (lst.Count > NARCOTRAFICANTE)
                    {
                        if (f(kitchen).Count > NARCOTRAFICANTE)
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
        static List<Point2d> GetEndPoints(Geometry start, List<Point2d> points, List<Geometry> dlines)
        {
            points = points.Distinct().ToList();
            var pts = points.Select(x => new GCircle(x, THESAURUSFACTOR).ToCirclePolygon(QUOTATIONLENTIFORM)).ToList();
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
                    return GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), HYDROSTATICALLY, THESAURUSSOMETIMES).Intersects(kitchensGeo);
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
                    if (fls.Count > NARCOTRAFICANTE)
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
                        fls = flsf(kitchen.Buffer(HYDROSTATICALLY));
                        if (fls.Count > NARCOTRAFICANTE)
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
            for (int i = NARCOTRAFICANTE; i < FLs.Count; i++)
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
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter(), HYDROSTATICALLY, THESAURUSSOMETIMES));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    if (endpoints.Count == NARCOTRAFICANTE) return THESAURUSSEMBLANCE;
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
                for (int i = NARCOTRAFICANTE; i < WaterPorts.Count; i++)
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
            var bfSize = THESAURUSCONSUL;
            var o = new DrainageCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            if (UNTRACEABLENESS) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WLines.AddRange(data.WLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (UNTRACEABLENESS) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
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
            return x => x.ToGCircle(THESAURUSSEMBLANCE).ToCirclePolygon(THESAURUSSOMETIMES);
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
            if (this.Storeys.Count == NARCOTRAFICANTE) return lst;
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
    public static class THDrainageService
    {
        public static Polygon ConvertToPolygon(Polyline pl)
        {
            if (pl.NumberOfVertices <= PHOTOGONIOMETER)
                return null;
            var list = new List<Point2d>();
            for (int i = NARCOTRAFICANTE; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                if (list.Count == NARCOTRAFICANTE || !Equals(pt, list.Last()))
                {
                    list.Add(pt);
                }
            }
            if (list.Count <= PHOTOGONIOMETER) return null;
            try
            {
                var tmp = list.Select(x => x.ToNTSCoordinate()).ToList(list.Count + ADRENOCORTICOTROPHIC);
                if (!tmp[NARCOTRAFICANTE].Equals(tmp[tmp.Count - ADRENOCORTICOTROPHIC]))
                {
                    tmp.Add(tmp[NARCOTRAFICANTE]);
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
            var t = Regex.Replace(text, THESAURUSFOSTER, THESAURUSREDOUND);
            t = Regex.Replace(t, THESAURUSINDOCTRINATE, THESAURUSHEARTLESS);
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
        public static string FixVerticalPipeLabel(string label)
        {
            if (label == null) return null;
            if (label.StartsWith(THESAURUSDUDGEON))
            {
                return label.Substring(THESAURUSPOSTSCRIPT);
            }
            if (label.StartsWith(QUOTATIONAMNIOTIC))
            {
                return label.Substring(PHOTOGONIOMETER);
            }
            return label;
        }
        public static bool IsNotedLabel(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return label.Contains(THESAURUSPERMISSIVE) || label.Contains(THESAURUSDONATION);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label);
        }
        public static bool IsY1L(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return label.StartsWith(THESAURUSTERMINATION);
        }
        public static bool IsY2L(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return label.StartsWith(INHERITABLENESS);
        }
        public static bool IsNL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return label.StartsWith(BIOSYSTEMATICALLY);
        }
        public static bool IsYL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return label.StartsWith(THESAURUSHOOKED);
        }
        public static bool IsRainLabel(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label);
        }
        public static bool IsDrainageLabelText(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            if (IsFL0(label)) return UNTRACEABLENESS;
            static bool f(string label)
            {
                return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
            }
            return f(FixVerticalPipeLabel(label));
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
        public const string PALAEOPATHOLOGIST = "TCH_PIPE";
        public const string THESAURUSCOMMOTION = "W-RAIN-PIPE";
        public const string THESAURUSACCEDE = "W-BUSH-NOTE";
        public const string TRANYLCYPROMINE = "W-RAIN-DIMS";
        public const int THESAURUSINDUSTRY = 100;
        public const string THESAURUSSANCTITY = "W-RAIN-EQPM";
        public const string THESAURUSEGOTISM = "TCH_VPIPEDIM";
        public const string THESAURUSHEARTLESS = "-";
        public const string THESAURUSSHAMBLE = "TCH_TEXT";
        public const string THESAURUSSUPERVISE = "TCH_EQUIPMENT";
        public const string INHOMOGENEOUSLY = "TCH_MTEXT";
        public const string THESAURUSINOFFENSIVE = "TCH_MULTILEADER";
        public const string THESAURUSREDOUND = "";
        public const int UNDERACHIEVEMENT = 50;
        public const char THESAURUSSUFFICE = '|';
        public const string THESAURUSRANSACK = "|";
        public const char POLYCRYSTALLINE = '$';
        public const string HENDECASYLLABUS = "$";
        public const string CRYSTALLIZATIONS = "可见性";
        public const string THESAURUSPRESENTABLE = "套管";
        public const int QUOTATIONTRILINEAR = 1000;
        public const string THESAURUSUNITED = "带定位立管";
        public const string THESAURUSWRONGFUL = "立管编号";
        public const string ENTERCOMMUNICATE = "$LIGUAN";
        public const string COSTERMONGERING = "A$C6BDE4816";
        public const string THESAURUSSTUPEFACTION = "污废合流井编号";
        public const string THESAURUSOVERWHELM = "W-DRAI-DOME-PIPE";
        public const string VERGELTUNGSWAFFE = "W-RAIN-NOTE";
        public const char QUOTATIONIMPERIUM = 'B';
        public const string THESAURUSADHERE = "RF";
        public const string IMMUNOGENETICALLY = "RF+1";
        public const string THESAURUSNATURALIST = "RF+2";
        public const string QUOTATIONHOUSEMAID = "F";
        public const int THESAURUSCONSUL = 10;
        public const int THESAURUSALCOVE = 400;
        public const string THESAURUSALCOHOLIC = "1F";
        public const string THESAURUSLADYLIKE = "-1F";
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
        public const int THESAURUSARTISAN = 750;
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
        public const int THESAURUSLEVITATE = 150;
        public const int THESAURUSPOISED = 318;
        public const int THESAURUSWAYWARD = 1400;
        public const int QUOTATIONCAPABLE = 1200;
        public const int THESAURUSDIRECTIVE = 2000;
        public const int VENTRILOQUISTIC = 600;
        public const int IRREMUNERABILIS = 479;
        public const string SPLANCHNOPLEURE = "DN100";
        public const int THESAURUSUSABLE = 950;
        public const int THESAURUSENTREAT = 200;
        public const int THESAURUSCESSATION = 499;
        public const int THESAURUSDEFERENCE = 360;
        public const int THESAURUSOBEISANCE = 650;
        public const int THESAURUSENCOURAGEMENT = 30;
        public const string BASIDIOMYCOTINA = "≥600";
        public const int THESAURUSFLUENT = 450;
        public const int THESAURUSCONTINGENT = 18;
        public const int THESAURUSDEMONSTRATION = 250;
        public const double LYMPHANGIOMATOUS = .7;
        public const string THESAURUSPOSITION = ";";
        public const double QUOTATIONEXOPHTHALMIC = .01;
        public const string RECONSTRUCTIONAL = "地漏系统";
        public const string THESAURUSTRAFFIC = "TH-STYLE3";
        public const int THESAURUSIGNORE = 745;
        public const string THESAURUSINGENUOUS = "乙字弯";
        public const string ENTREPRENEURISM = "立管检查口";
        public const string THESAURUSCELEBRATE = "套管系统";
        public const string THESAURUSBALLAST = "重力流雨水井编号";
        public const string THESAURUSSCAVENGER = "666";
        public const string THESAURUSAUTOGRAPH = ",";
        public const string THESAURUSRELIGIOUS = "~";
        public const int ARCHAEOLOGICALLY = 7;
        public const int COUNTERFEISANCE = 229;
        public const int INTERVOCALICALLY = 230;
        public const int THESAURUSVOLITION = 8192;
        public const int THESAURUSOVERFLOW = 8000;
        public const int THESAURUSNETHER = 15;
        public const string COMPOSITIONALLY = "FromImagination";
        public const int UNANSWERABLENESS = 55;
        public const string QUOTATIONPERUVIAN = "X.XX";
        public const string PRESIDENTIALIST = "排出：";
        public const int THESAURUSBREEDING = 666;
        public const string THESAURUSINSOMNIA = "排出套管：";
        public const string THESAURUSINGRATIATE = "WaterWellWrappingPipeRadiusStringDict:";
        public const int THESAURUSTRAGEDY = 255;
        public const int THESAURUSINDICT = 0x91;
        public const int UNCONSTITUTIONALISM = 0xc7;
        public const int DIHYDROXYSTILBENE = 0xae;
        public const int THESAURUSBELOVED = 211;
        public const int INSTITUTIONALIZATION = 213;
        public const int OBOEDIENTIARIUS = 111;
        public const string THESAURUSPERSPIRATION = "AI-空间框线";
        public const string THESAURUSSQUASHY = "AI-空间名称";
        public const string THESAURUSDUDGEON = "73-";
        public const string QUOTATIONAMNIOTIC = "1-";
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
        public const string THESAURUSENGRAVING = @"^(F\d?L|T\d?L|P\d?L|D\d?L)(\w*)\-(\w*)([a-zA-Z]*)$";
        public const double THESAURUSUNTIRING = 383875.8169;
        public const double THESAURUSPRESCRIPTION = 250561.9571;
        public const string THESAURUSENDOWMENT = "P型存水弯";
        public const string THESAURUSCOUNTER = "板上P弯";
        public const int THESAURUSATTRACTION = 3500;
        public const int KONSTITUTSIONNYĬ = 1479;
        public const int THESAURUSCUPIDITY = 2379;
        public const int THESAURUSDETAIL = 1779;
        public const int DISCONTINUAUNCE = 579;
        public const int THESAURUSDETACHMENT = 279;
        public const string THESAURUSRECEPTACLE = "双池S弯";
        public const string IMMUNOELECTROPHORESIS = "W-DRAI-VENT-PIPE";
        public const string THESAURUSATAVISM = "DN50";
        public const string THESAURUSOVERCHARGE = "DN75";
        public const int ALSOBENEVENTINE = 789;
        public const int THESAURUSREGULATE = 1270;
        public const int THESAURUSHUMANE = 1090;
        public const string JUXTAPOSITIONAL = "双池P弯";
        public const int RETROGRESSIVELY = 5479;
        public const int THESAURUSMARIJUANA = 1079;
        public const int THESAURUSRECOVERY = 5600;
        public const int MILLIONAIRESHIP = 6079;
        public const int THESAURUSIMPORT = 8;
        public const int PRESUMPTIOUSNESS = 1379;
        public const int THESAURUSBURROW = 569;
        public const int THESAURUSINDISPOSED = 406;
        public const int SUBMICROSCOPICALLY = 404;
        public const int THESAURUSMEANING = 3150;
        public const int THESAURUSCONCEITED = 12;
        public const int QUOTATIONMENOMINEE = 1300;
        public const double THESAURUSBARBARISM = .4;
        public const string NEUROTRANSMITTER = "接阳台洗手盆排水";
        public const string THESAURUSCLATTER = "DN50，余同";
        public const int DECONTEXTUALIZE = 1190;
        public const string THESAURUSPROPEL = "接卫生间排水管";
        public const string THESAURUSINFLAME = "DN100，余同";
        public const int THESAURUSLUSTRE = 490;
        public const int THESAURUSATTENDANT = 170;
        public const int ELECTROPHORETIC = 2830;
        public const int THESAURUSBEAUTIFUL = 900;
        public const int THESAURUSINGRAINED = 330;
        public const int ENTHOUSIASTIKOS = 895;
        public const int QUOTATIONCHIPPING = 285;
        public const int THESAURUSDROUGHT = 390;
        public const string ELECTROMAGNETISM = "普通地漏P弯";
        public const int TRIBOELECTRICITY = 1330;
        public const int THESAURUSEVENTUALLY = 270;
        public const string THESAURUSPARSON = "接厨房洗涤盆排水";
        public const int INCONSEQUENTIALITY = 156;
        public const int INDEMNIFICATION = 510;
        public const int THESAURUSSPACIOUS = 389;
        public const int THESAURUSMALAISE = 45;
        public const int THESAURUSDECREE = 669;
        public const int INTRAVASCULARLY = 590;
        public const int MICROCLIMATOLOGY = 1700;
        public const int THESAURUSDISPUTATION = 919;
        public const int TRANSUBSTANTIATE = 990;
        public const int INCONVERTIBILIS = 129;
        public const int THESAURUSCOMMENSURATE = 693;
        public const int THESAURUSPRODUCT = 1591;
        public const int THESAURUSANONYMOUS = 511;
        public const int THESAURUSSUBSIDIARY = 289;
        public const string THESAURUSAGGRIEVED = "W-DRAI-DIMS";
        public const int ENFRANCHISEMENT = 1391;
        public const int INCOMMODIOUSNESS = 667;
        public const int THESAURUSNEIGHBOURHOOD = 1450;
        public const int THESAURUSTETCHY = 251;
        public const int THESAURUSPREDOMINANT = 660;
        public const int THESAURUSREDDEN = 110;
        public const int DEMOCRATIZATION = 91;
        public const int THESAURUSDILIGENCE = 320;
        public const int PROBATIONERSHIP = 427;
        public const int COMMUNICABLENESS = 183;
        public const int MICROPUBLISHING = 283;
        public const double THESAURUSDETESTABLE = 250.0;
        public const int DOMINEERINGNESS = 225;
        public const int PARASITICALNESS = 1125;
        public const int PHYTOPLANKTONIC = 280;
        public const int THESAURUSIRRECONCILABLE = 76;
        public const int QUOTATIONMORETON = 424;
        public const int THESAURUSDISOWN = 1900;
        public const string QUOTATIONTACONIC = "DN100乙字弯";
        public const string THESAURUSFORTIFICATION = "1350";
        public const int THESAURUSSEIZURE = 275;
        public const int INQUISITORIALLY = 210;
        public const int THESAURUSBEGINNER = 151;
        public const int IMPRESCRIPTIBLE = 1109;
        public const int THESAURUSPSYCHOLOGY = 420;
        public const int THESAURUSINTERDICT = 447;
        public const int CONSUBSTANTIALITY = 43;
        public const int THESAURUSINADMISSIBLE = 237;
        public const string CYCLOHEXYLSULPHAMATE = "洗衣机地漏P弯";
        public const int UNSUBMISSIVENESS = 1380;
        public const double THESAURUSPINCHED = 200.0;
        public const double THESAURUSCHRONOLOGICAL = 780.0;
        public const double THESAURUSEXTERNAL = 130.0;
        public const int CHARACTERISTICAL = 980;
        public const int THESAURUSAPPROACH = 1358;
        public const int ALSOCHIROPTEROUS = 172;
        public const int THESAURUSMAESTRO = 155;
        public const int THESAURUSREFEREE = 1650;
        public const int THESAURUSINVISIBLE = 71;
        public const int DELETERIOUSNESS = 221;
        public const int THESAURUSELEVATED = 29;
        public const int THESAURUSGRACEFUL = 1158;
        public const int EXAGGERATIVENESS = 179;
        public const int ALSOCHALCENTERIC = 880;
        public const string THESAURUSBLOODSHED = ">1500";
        public const int THESAURUSELEGIAC = 921;
        public const int PHOSPHORESCENCE = 2320;
        public const string IRRECONCILIABLE = "普通地漏S弯";
        public const int ELECTROCHEMISTRY = 3090;
        public const int INTERROGATIVELY = 371;
        public const int UNGOVERNABILITY = 2730;
        public const int THESAURUSBEWAIL = 888;
        public const int THESAURUSHEADQUARTERS = 460;
        public const int THESAURUSDRAMATIC = 2499;
        public const int CONTEMPORANEOUS = 1210;
        public const int HISTOCOMPATIBILITY = 850;
        public const int UNOBTRUSIVENESS = 2279;
        public const int SPHYGMOMANOMETER = 1239;
        public const int THESAURUSAPPORTION = 675;
        public const string THESAURUSASSASSIN = "drainage_drawing_ctx";
        public const string THESAURUSIGNORANCE = "通气帽系统-AI";
        public const string INCOMMENSURABILIS = "暂不支持污废分流";
        public const string INATTENTIVENESS = "PL";
        public const string QUOTATION1CBARBADOS = "TL";
        public const string ALSODIDACTYLOUS = "管底h+X.XX";
        public const string UNSOPHISTICATEDLY = "双格洗涤盆排水-AI";
        public const string THESAURUSFLAGRANT = "S型存水弯";
        public const string THESAURUSINFERTILE = "板上S弯";
        public const string CONTRACEPTIVELY = "翻转状态";
        public const string THESAURUSDESECRATE = "清扫口系统";
        public const int QUOTATIONPAPILLARY = 21;
        public const string THESAURUSCONTRIBUTE = "-DRAI-";
        public const string THESAURUSTHROTTLE = "AI-房间框线";
        public const string THESAURUSMANNERISM = "AI-房间名称";
        public const string UNPROPITIOUSNESS = "W-DRAI-OUT-PIPE";
        public const string INTERROGATORIES = "WP_KTN_LG";
        public const string THESAURUSDILUTE = "De";
        public const string THESAURUSAPPLIANCE = "wb";
        public const string THESAURUSDEPRESS = "kd";
        public const string THESAURUSSHACKLE = "C-XREF-EXT";
        public const string NEIGHBOURLINESS = "雨水井";
        public const string THESAURUSTRANSMISSION = "地漏平面";
        public const int THESAURUSDERISION = 60;
        public const string THESAURUSVOUCHER = "A$C58B12E6E";
        public const string QUOTATIONPERIODIC = "W-DRAI-PIEP-RISR";
        public const string THESAURUSMISUNDERSTANDING = "A$C5E4A3C21";
        public const string DISPASSIONATELY = "PIPE-喷淋";
        public const string OVERSENSITIVITY = "A-Kitchen-3";
        public const string INTERLINEATIONS = "A-Kitchen-4";
        public const string INSTITUTIONALIZE = "A-Toilet-1";
        public const string THIOSEMICARBAZONE = "A-Toilet-2";
        public const string THESAURUSINITIAL = "A-Toilet-3";
        public const string THESAURUSBOUNTY = "A-Toilet-4";
        public const string QUOTATIONPARISIAN = "-XiDiPen-";
        public const string QUOTATIONEVENING = "A-Kitchen-9";
        public const string RUDIMENTARINESS = "0$座厕";
        public const string METALINGUISTICS = "0$asdfghjgjhkl";
        public const string THESAURUSINHABITANT = "A-Toilet-";
        public const string THESAURUSSTROKE = "A-Kitchen-";
        public const string HYDROCHARITACEAE = "|lp";
        public const string THESAURUSINDENT = "|lp1";
        public const string THESAURUSCAUCUS = "|lp2";
        public const string THESAURUSINFIDEL = "A-Toilet-9";
        public const string THESAURUSCINEMA = "$xiyiji";
        public const string THESAURUSBITING = "feng_dbg_test_washing_machine";
        public const string THESAURUSCORRUPT = "ShortTranslatorLabels：";
        public const string PIPERIDINOPYRIDINE = "LongTranslatorLabels：";
        public const string EXCEPTIONABLENESS = "VerticalPipeLabels:";
        public const string THESAURUSIMPURITY = "ToiletPls:";
        public const string THESAURUSGLORIFY = "KitchenFls:";
        public const string ALTERNATIVENESS = "BalconyFls:";
        public const string THESAURUSRELEASE = "多通道";
        public const string THESAURUSINCAPACITY = "洗衣机";
        public const string ENVIRONMENTALISM = "FloorDrains：";
        public const string IMPLEMENTIFEROUS = "SingleOutletFloorDrains：";
        public const string THESAURUSMELODIOUS = "Shunts：";
        public const string THESAURUSCONVOLUTION = "circlesCount ";
        public const string THESAURUSRENOUNCE = "Merges ";
        public const string HYDROXYLAPATITE = "drainage_drDatas";
        public const string THESAURUSPERMISSIVE = "单排";
        public const string THESAURUSDONATION = "设置乙字弯";
        public const string THESAURUSRESTFUL = @"^([^\-]*)\-([A-Za-z])$";
        public const string THESAURUSPROPHETIC = @"^([^\-]*\-[A-Za-z])(\d+)$";
        public const string QUOTATIONDIAMONDBACK = @"^([^\-]*\-)([A-Za-z])(\d+)$";
        public const int THESAURUSRESIST = 329;
        public const int THESAURUSEFFULGENT = 629;
        public const double THESAURUSAGREEABLE = 270.0;
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
            if (roomName == null) return UNTRACEABLENESS;
            var roomNameContains = new List<string> { CONGREGATIONIST };
            if (string.IsNullOrEmpty(roomName))
                return UNTRACEABLENESS;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSSEMBLANCE;
            return UNTRACEABLENESS;
        }
        public static bool IsCorridor(string roomName)
        {
            if (roomName == null) return UNTRACEABLENESS;
            var roomNameContains = new List<string> { QUOTATIONPURKINJE };
            if (string.IsNullOrEmpty(roomName))
                return UNTRACEABLENESS;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSSEMBLANCE;
            return UNTRACEABLENESS;
        }
        public static bool IsWL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return Regex.IsMatch(label, QUOTATIONBITUMINOUS);
        }
        public static bool IsFL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return Regex.IsMatch(label, THESAURUSPROPITIOUS);
        }
        public static bool IsFL0(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return IsFL(label) && label.Contains(THESAURUSINTUMESCENCE);
        }
        public static bool IsPL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return Regex.IsMatch(label, THESAURUSOBLOQUY);
        }
        public static bool IsTL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return Regex.IsMatch(label, HYPERPARASITISM);
        }
        public static bool IsDL(string label)
        {
            if (label == null) return UNTRACEABLENESS;
            return Regex.IsMatch(label, ALSOPORCELLANIC);
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
                    if (reference.GetEffectiveName().Contains(THESAURUSINFIDEL))
                    {
                        using var adb = AcadDatabase.Use(reference.Database);
                        if (IsVisibleLayer(adb.Layers.Element(reference.Layer)))
                            return THESAURUSSEMBLANCE;
                    }
                }
                return UNTRACEABLENESS;
            }
            public override bool CheckLayerValid(Entity curve)
            {
                return THESAURUSSEMBLANCE;
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