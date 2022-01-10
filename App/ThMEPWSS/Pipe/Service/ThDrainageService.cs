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
                    if (circlesCount > THESAURUSPERMUTATION)
                    {
                        return FlCaseEnum.Case1;
                    }
                    if (circlesCount == THESAURUSPERMUTATION)
                    {
                        return FlCaseEnum.Case2;
                    }
                }
                else
                {
                    if (circlesCount == THESAURUSPERMUTATION)
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
                    if (circlesCount > THESAURUSPERMUTATION)
                    {
                        return FlFixType.MiddleHigher;
                    }
                    if (circlesCount == THESAURUSPERMUTATION)
                    {
                        return FlFixType.Lower;
                    }
                }
                else
                {
                    if (circlesCount == THESAURUSPERMUTATION)
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
                    if (circlesCount > THESAURUSPERMUTATION)
                    {
                        return PlCaseEnum.Case1;
                    }
                    if (circlesCount == THESAURUSPERMUTATION)
                    {
                        return PlCaseEnum.Case1;
                    }
                }
                else
                {
                    if (circlesCount == THESAURUSPERMUTATION)
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
        static readonly Regex re = new Regex(TRANSLITERATIONS);
        public static LabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new LabelItem()
            {
                Label = label,
                Prefix = m.Groups[THESAURUSHOUSING].Value,
                D1S = m.Groups[THESAURUSPERMUTATION].Value,
                D2S = m.Groups[INTROPUNITIVENESS].Value,
                Suffix = m.Groups[QUOTATIONEDIBLE].Value,
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
                return THESAURUSSTAMPEDE;
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
            return THESAURUSSTAMPEDE;
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
        public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > THESAURUSSTAMPEDE;
        public List<DrainageGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public int MinTl;
        public int MaxTl;
        public bool HasTl;
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
                var s = THESAURUSSTAMPEDE;
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
            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSDOMESTIC, QUOTATIONWITTIG), new Vector2d(-PROKELEUSMATIKOS, THESAURUSSTAMPEDE) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[THESAURUSHOUSING].EndPoint : segs[THESAURUSHOUSING].StartPoint;
            var text = THESAURUSTENACIOUS;
            var height = THESAURUSENDANGER;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawWashingMachineRaisingSymbol(Point2d bsPt, bool isLeftOrRight)
        {
            if (isLeftOrRight)
            {
                var v = new Vector2d(HYDROELECTRICITY, -THESAURUSMARRIAGE);
                DrawBlockReference(SUCCESSLESSNESS, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSJUBILEE;
                    br.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, THESAURUSLINING);
                    }
                });
            }
            else
            {
                var v = new Vector2d(-HYDROELECTRICITY, -THESAURUSMARRIAGE);
                DrawBlockReference(SUCCESSLESSNESS, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSJUBILEE;
                    br.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, THESAURUSLINING);
                    }
                });
            }
        }
        public static double LONG_TRANSLATOR_HEIGHT1 = SUBCATEGORIZING;
        public static double CHECKPOINT_OFFSET_Y = THESAURUSNECESSITOUS;
        public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreyItem> storeysItems, Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys, DrainageSystemDiagramViewModel viewModel, ExtraInfo exInfo)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + THESAURUSASPIRATION).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - THESAURUSHOUSING;
            var end = THESAURUSSTAMPEDE;
            var OFFSET_X = QUOTATIONLETTERS;
            var SPAN_X = BALANOPHORACEAE + QUOTATIONWITTIG + THESAURUSNAUGHT;
            var HEIGHT = THESAURUSINCOMING;
            {
                if (viewModel?.Params?.StoreySpan is double v)
                {
                    HEIGHT = v;
                }
            }
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSINCOMING;
            var __dy = THESAURUSHYPNOTIC;
            DrawDrainageSystemDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, viewModel, exInfo);
        }
        public class Opt
        {
            double fixY;
            double _dy;
            public List<Vector2d> vecs0 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONBASTARD - dy) };
            public List<Vector2d> vecs1 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy + fixY), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - _dy - dy - fixY) };
            public List<Vector2d> vecs8 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - _dy - dy + __dy) };
            public List<Vector2d> vecs11 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - _dy - dy - __dy) };
            public List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -COOPERATIVENESS - dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            public List<Vector2d> vecs3 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - _dy - dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            public List<Vector2d> vecs9 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - _dy - dy + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            public List<Vector2d> vecs13 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy + __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSERRAND - _dy - dy - __dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) };
            public List<Vector2d> vecs4 => vecs1.GetYAxisMirror();
            public List<Vector2d> vecs5 => vecs2.GetYAxisMirror();
            public List<Vector2d> vecs6 => vecs3.GetYAxisMirror();
            public List<Vector2d> vecs10 => vecs9.GetYAxisMirror();
            public List<Vector2d> vecs12 => vecs11.GetYAxisMirror();
            public List<Vector2d> vecs14 => vecs13.GetYAxisMirror();
            public Vector2d vec7 => new Vector2d(-THESAURUSQUAGMIRE, THESAURUSQUAGMIRE);
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
            public ExtraInfo exInfo;
            public static bool SHOWLINE;
            public void Draw()
            {
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
                        string getStoreyHeightText()
                        {
                            if (storey is THESAURUSREGION) return MULTINATIONALLY;
                            var ret = (heights[i] / LAUTENKLAVIZIMBEL).ToString(THESAURUSINFINITY); ;
                            if (ret == THESAURUSINFINITY) return MULTINATIONALLY;
                            return ret;
                        }
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen, getStoreyHeightText());
                    }
                }
                void _DrawWrappingPipe(Point2d basePt, string shadow)
                {
                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                    {
                        Dr.DrawSimpleLabel(basePt, THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                    }
                    DrawBlockReference(THESAURUSSTRINGENT, basePt.ToPoint3d(), br =>
                    {
                        br.Layer = THESAURUSDEFAULTER;
                        ByLayer(br);
                    });
                }
                void DrawOutlets5(Point2d basePoint, ThwOutput output, DrainageGroupedPipeItem gpItem)
                {
                    var values = output.DirtyWaterWellValues;
                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL - THESAURUSDOMESTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
                    var segs = vecs.ToGLineSegments(basePoint);
                    segs.RemoveAt(INTROPUNITIVENESS);
                    DrawDiryWaterWells1(segs[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
                    if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
                    if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
                    DrawNoteText(output.DN1, segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSATTACHMENT));
                    DrawNoteText(output.DN2, segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSATTACHMENT));
                    if (output.HasCleaningPort1) DrawCleaningPort(segs[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                    if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                    var p = segs[THESAURUSCOMMUNICATION].EndPoint;
                    DrawFloorDrain((p.OffsetX(-THESAURUSPERVADE) + new Vector2d(-THESAURUSOFFEND + THESAURUSDOMESTIC, THESAURUSSTAMPEDE)).ToPoint3d(), THESAURUSOBSTINACY);
                }
                string getDSCurveValue()
                {
                    return viewModel?.Params?.Basin ?? PERIODONTOCLASIA;
                }
                bool getShouldToggleBlueMiddleLine()
                {
                    return viewModel?.Params?.H ?? INTRAVASCULARLY;
                }
                for (int j = THESAURUSSTAMPEDE; j < COUNT; j++)
                {
                    var dome_lines = new List<GLineSegment>(THESAURUSREPERCUSSION);
                    var vent_lines = new List<GLineSegment>(THESAURUSREPERCUSSION);
                    var dome_layer = THESAURUSCONTROVERSY;
                    var vent_layer = THUNDERSTRICKEN;
                    void drawDomePipe(GLineSegment seg)
                    {
                        if (seg.IsValid) dome_lines.Add(seg);
                    }
                    void drawDomePipes(IEnumerable<GLineSegment> segs, string shadow)
                    {
                        var ok = INTRAVASCULARLY;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                {
                                    Dr.DrawSimpleLabel(seg.StartPoint, THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING), dome_layer);
                                }
                                ok = THESAURUSOBSTINACY;
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
                        var ok = INTRAVASCULARLY;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                {
                                    Dr.DrawSimpleLabel(seg.StartPoint, THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING), vent_layer);
                                }
                                ok = THESAURUSOBSTINACY;
                            }
                            vent_lines.Add(seg);
                        }
                    }
                    string getWashingMachineFloorDrainDN()
                    {
                        return viewModel?.Params?.WashingMachineFloorDrainDN ?? QUOTATIONBREWSTER;
                    }
                    string getBasinDN()
                    {
                        return viewModel?.Params?.BasinDN ?? QUOTATIONBREWSTER;
                    }
                    string getOtherFloorDrainDN()
                    {
                        return viewModel?.Params?.OtherFloorDrainDN ?? QUOTATIONBREWSTER;
                    }
                    void Get2FloorDrainDN(out string v1, out string v2)
                    {
                        v1 = viewModel?.Params?.WashingMachineFloorDrainDN ?? QUOTATIONBREWSTER;
                        v2 = v1;
                        if (v2 == QUOTATIONBREWSTER) v2 = QUOTATIONDOPPLER;
                    }
                    bool getCouldHavePeopleOnRoof()
                    {
                        return viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSOBSTINACY;
                    }
                    var gpItem = pipeGroupItems[j];
                    var thwPipeLine = new ThwPipeLine();
                    thwPipeLine.Labels = gpItem.Labels.Concat(gpItem.TlLabels.Yield()).ToList();
                    var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                    if (gpItem.PipeType == PipeType.FL)
                    {
                        dome_layer = THESAURUSADVERSITY;
                    }
                    for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
                    {
                        var storey = allNumStoreyLabels[i];
                        var run = gpItem.Items[i].Exist ? new ThwPipeRun()
                        {
                            HasLongTranslator = gpItem.Items[i].HasLong,
                            HasShortTranslator = gpItem.Items[i].HasShort,
                            HasCleaningPort = gpItem.Hangings.TryGet(i + THESAURUSHOUSING)?.HasCleaningPort ?? INTRAVASCULARLY,
                            HasCheckPoint = gpItem.Hangings[i].HasCheckPoint,
                            HasDownBoardLine = gpItem.Hangings[i].HasDownBoardLine,
                            DrawLongHLineHigher = gpItem.Items[i].DrawLongHLineHigher,
                            Is4Tune = gpItem.Hangings[i].Is4Tune,
                        } : null;
                        runs.Add(run);
                    }
                    for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
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
                        if (floorDrainsCount > THESAURUSSTAMPEDE || hasSCurve)
                        {
                            var run = runs.TryGet(i - THESAURUSHOUSING);
                            if (run != null)
                            {
                                var hanging = run.LeftHanging ??= new Hanging();
                                hanging.FloorDrainsCount = floorDrainsCount;
                                hanging.HasSCurve = hasSCurve;
                            }
                        }
                    }
                    for (int i = runs.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; i--)
                    {
                        var r = runs[i];
                        if (r == null) continue;
                        if (r.HasLongTranslator)
                        {
                            r.IsLongTranslatorToLeftOrRight = THESAURUSOBSTINACY;
                        }
                    }
                    {
                        foreach (var r in runs)
                        {
                            if (r?.HasShortTranslator == THESAURUSOBSTINACY)
                            {
                                r.IsShortTranslatorToLeftOrRight = INTRAVASCULARLY;
                                if (r.HasLongTranslator && r.IsLongTranslatorToLeftOrRight)
                                {
                                    r.IsShortTranslatorToLeftOrRight = THESAURUSOBSTINACY;
                                }
                            }
                        }
                    }
                    Point2d drawHanging(Point2d start, Hanging hanging)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE), new Vector2d(THESAURUSCAVERN, THESAURUSSTAMPEDE), new Vector2d(THESAURUSJACKPOT, THESAURUSSTAMPEDE) };
                        var segs = vecs.ToGLineSegments(start);
                        {
                            var _segs = segs.ToList();
                            if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                            {
                                _segs.RemoveAt(INTROPUNITIVENESS);
                            }
                            _segs.RemoveAt(THESAURUSPERMUTATION);
                            DrawDomePipes(_segs);
                        }
                        {
                            var pts = vecs.ToPoint2ds(start);
                            {
                                var pt = pts[THESAURUSHOUSING];
                                var v = new Vector2d(THESAURUSQUAGMIRE, THESAURUSQUAGMIRE);
                                if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                {
                                    v = default;
                                }
                                var p = pt + v;
                                if (hanging.HasSCurve)
                                {
                                    DrawSCurve(v, pt, INTRAVASCULARLY);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    if (!p.Equals(pt))
                                    {
                                        dome_lines.Add(new GLineSegment(p, pt));
                                    }
                                    DrawDSCurve(p, INTRAVASCULARLY, getDSCurveValue(), THESAURUSDEPLORE);
                                }
                            }
                            if (hanging.FloorDrainsCount >= THESAURUSHOUSING)
                            {
                                DrawFloorDrain(pts[THESAURUSPERMUTATION].ToPoint3d(), INTRAVASCULARLY);
                            }
                            if (hanging.FloorDrainsCount >= THESAURUSPERMUTATION)
                            {
                                DrawFloorDrain(pts[QUOTATIONEDIBLE].ToPoint3d(), INTRAVASCULARLY);
                            }
                        }
                        start = segs.Last().EndPoint;
                        return start;
                    }
                    void DrawOutlets1(string shadow, Point2d basePoint1, double width, ThwOutput output, bool isRainWaterWell = INTRAVASCULARLY, Vector2d? fixv = null)
                    {
                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                        {
                            Dr.DrawSimpleLabel(basePoint1.OffsetY(-HYPERDISYLLABLE), THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                        }
                        Point2d pt2, pt3;
                        if (output.DirtyWaterWellValues != null)
                        {
                            var v = new Vector2d(-THESAURUSINHERIT - THESAURUSDOMESTIC, -THESAURUSDERELICTION - QUOTATIONPITUITARY);
                            var pt = basePoint1 + v;
                            if (fixv.HasValue)
                            {
                                pt += fixv.Value;
                            }
                            var values = output.DirtyWaterWellValues;
                            DrawDiryWaterWells1(pt, values, isRainWaterWell);
                        }
                        {
                            var dx = width - THESAURUSEXECRABLE;
                            var fixY = QUOTATIONTRANSFERABLE;
                            if (output.LinesCount == THESAURUSHOUSING)
                            {
                                fixY = -THESAURUSPRIMARY;
                            }
                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONPITUITARY - CONSPICUOUSNESS + fixY), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDERELICTION), new Vector2d(QUOTATIONDENNIS + dx, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSBLESSING), new Vector2d(-THESAURUSCLIMATE - dx, -QUOTATIONBASTARD), new Vector2d(THESAURUSCOLOSSAL + dx, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) };
                            {
                                var segs = vecs.ToGLineSegments(basePoint1);
                                if (output.LinesCount == THESAURUSHOUSING)
                                {
                                    drawDomePipes(segs.Take(INTROPUNITIVENESS), THESAURUSDEPLORE);
                                }
                                else if (output.LinesCount > THESAURUSHOUSING)
                                {
                                    segs.RemoveAt(THESAURUSDESTITUTE);
                                    if (!output.HasVerticalLine2) segs.RemoveAt(SUPERLATIVENESS);
                                    segs.RemoveAt(INTROPUNITIVENESS);
                                    drawDomePipes(segs, THESAURUSDEPLORE);
                                }
                            }
                            var pts = vecs.ToPoint2ds(basePoint1);
                            if (output.HasWrappingPipe1) _DrawWrappingPipe(pts[INTROPUNITIVENESS].OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
                            if (output.HasWrappingPipe2) _DrawWrappingPipe(pts[QUOTATIONEDIBLE].OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
                            if (output.HasWrappingPipe3) _DrawWrappingPipe(pts[THESAURUSSCARCE].OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
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
                                    static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = HELIOCENTRICISM)
                                    {
                                        DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, label } }, cb: br => { ByLayer(br); });
                                    }
                                    var p1 = pts[INTROPUNITIVENESS].OffsetX(THESAURUSDOMESTIC);
                                    var p2 = p1.OffsetY(-DOCTRINARIANISM);
                                    var p3 = p2.OffsetX(QUOTATIONWITTIG);
                                    var layer = THESAURUSSTRIPED;
                                    DrawLine(layer, new GLineSegment(p1, p2));
                                    DrawLine(layer, new GLineSegment(p3, p2));
                                    DrawStoreyHeightSymbol(p3, THESAURUSSTRIPED, gpItem.OutletWrappingPipeRadius);
                                    {
                                        var _shadow = THESAURUSDEPLORE;
                                        if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > THESAURUSHOUSING)
                                        {
                                            Dr.DrawSimpleLabel(p3, THESAURUSFEATURE + _shadow.Substring(THESAURUSHOUSING));
                                        }
                                    }
                                }
                            }
                            var v = new Vector2d(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR);
                            DrawNoteText(output.DN1, pts[INTROPUNITIVENESS] + v);
                            DrawNoteText(output.DN2, pts[QUOTATIONEDIBLE] + v);
                            DrawNoteText(output.DN3, pts[THESAURUSSCARCE] + v);
                            if (output.HasCleaningPort1) DrawCleaningPort(pts[THESAURUSPERMUTATION].ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                            if (output.HasCleaningPort2) DrawCleaningPort(pts[THESAURUSCOMMUNICATION].ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                            if (output.HasCleaningPort3) DrawCleaningPort(pts[THESAURUSACTUAL].ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                            pt2 = pts[SUPERLATIVENESS];
                            pt3 = pts.Last();
                        }
                        if (output.HasLargeCleaningPort)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONAFGHAN) };
                            var segs = vecs.ToGLineSegments(pt3);
                            drawDomePipes(segs, THESAURUSDEPLORE);
                            DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSPERMUTATION);
                        }
                        if (output.HangingCount == THESAURUSHOUSING)
                        {
                            var hang = output.Hanging1;
                            Point2d lastPt = pt2;
                            {
                                var segs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, INDIGESTIBLENESS), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) }.ToGLineSegments(lastPt);
                                drawDomePipes(segs, THESAURUSDEPLORE);
                                lastPt = segs.Last().EndPoint;
                            }
                            {
                                lastPt = drawHanging(lastPt, output.Hanging1);
                            }
                        }
                        else if (output.HangingCount == THESAURUSPERMUTATION)
                        {
                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSNECESSITY, THESAURUSNECESSITY), new Vector2d(QUOTATIONDEFLUVIUM, QUOTATIONDEFLUVIUM) };
                            var pts = vs1.ToPoint2ds(pt3);
                            drawDomePipes(vs1.ToGLineSegments(pt3), THESAURUSDEPLORE);
                            drawHanging(pts.Last(), output.Hanging1);
                            var dx = output.Hanging1.FloorDrainsCount == THESAURUSPERMUTATION ? POLYOXYMETHYLENE : THESAURUSSTAMPEDE;
                            var vs2 = new List<Vector2d> { new Vector2d(HYDROCOTYLACEAE + dx, THESAURUSSTAMPEDE), new Vector2d(QUOTATIONDEFLUVIUM, QUOTATIONDEFLUVIUM) };
                            drawDomePipes(vs2.ToGLineSegments(pts[THESAURUSHOUSING]), THESAURUSDEPLORE);
                            drawHanging(vs2.ToPoint2ds(pts[THESAURUSHOUSING]).Last(), output.Hanging2);
                        }
                    }
                    void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                    {
                        {
                        }
                        {
                            foreach (var info in arr)
                            {
                                if (info?.Storey == THESAURUSARGUMENTATIVE)
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
                        int counterPipeButtomHeightSymbol = THESAURUSSTAMPEDE;
                        bool hasDrawedSCurveLabel = INTRAVASCULARLY;
                        bool hasDrawedDSCurveLabel = INTRAVASCULARLY;
                        bool hasDrawedCleaningPort = INTRAVASCULARLY;
                        void _DrawLabel(string text1, string text2, Point2d basePt, bool leftOrRight, double height)
                        {
                            var w = THESAURUSEXECRABLE - THESAURUSDICTATORIAL;
                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, height), new Vector2d(leftOrRight ? -w : w, THESAURUSSTAMPEDE) };
                            var segs = vecs.ToGLineSegments(basePt);
                            var lines = DrawLineSegmentsLazy(segs);
                            Dr.SetLabelStylesForDraiNote(lines.ToArray());
                            var p = segs.Last().EndPoint.OffsetY(THESAURUSENTREPRENEUR);
                            if (!string.IsNullOrEmpty(text1))
                            {
                                var t = DrawTextLazy(text1, THESAURUSENDANGER, p);
                                Dr.SetLabelStylesForDraiNote(t);
                            }
                            if (!string.IsNullOrEmpty(text2))
                            {
                                var t = DrawTextLazy(text2, THESAURUSENDANGER, p.OffsetXY(THESAURUSMORTUARY, -THESAURUSDOMESTIC));
                                Dr.SetLabelStylesForDraiNote(t);
                            }
                        }
                        void _DrawHorizontalLineOnPipeRun(Point3d basePt)
                        {
                            if (gpItem.Labels.Any(x => IsFL(x)))
                            {
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == THESAURUSPERMUTATION)
                                {
                                    var p = basePt.ToPoint2d();
                                    var h = HEIGHT * THESAURUSDISPASSIONATE;
                                    if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
                                    {
                                        h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSSURPRISED;
                                    }
                                    p = p.OffsetY(h);
                                    DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, p);
                                }
                            }
                            DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                        }
                        void _DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                        {
                            if (!hasDrawedSCurveLabel)
                            {
                                hasDrawedSCurveLabel = THESAURUSOBSTINACY;
                                _DrawLabel(THESAURUSMOLEST, THESAURUSSCOUNDREL, p1 + new Vector2d(-THESAURUSGETAWAY, THIGMOTACTICALLY), THESAURUSOBSTINACY, QUOTATIONBASTARD);
                            }
                            DrawSCurve(vec7, p1, leftOrRight);
                        }
                        void _DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
                        {
                            if (gpItem.Labels.Any(x => IsPL(x)))
                            {
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == THESAURUSPERMUTATION)
                                {
                                    var p = basePt.ToPoint2d();
                                    DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, p);
                                }
                            }
                            var p1 = basePt.ToPoint2d();
                            if (!hasDrawedCleaningPort && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                            {
                                hasDrawedCleaningPort = THESAURUSOBSTINACY;
                                _DrawLabel(QUOTATIONHUMPBACK, DISCOMMODIOUSNESS, p1 + new Vector2d(-THESAURUSOUTLANDISH, RETROGRESSIVELY), THESAURUSOBSTINACY, PROCRASTINATORY);
                            }
                            DrawCleaningPort(basePt, leftOrRight, scale);
                        }
                        void _DrawCheckPoint(Point2d basePt, bool leftOrRight, string shadow)
                        {
                            if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                            {
                                Dr.DrawSimpleLabel(basePt, THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
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
                                if (storey == THESAURUSREGION)
                                {
                                    var basePt = info.EndPoint;
                                    if (output != null)
                                    {
                                        DrawOutlets1(THESAURUSDEPLORE, basePt, THESAURUSEXECRABLE, output);
                                    }
                                }
                            }
                            bool shouldRaiseWashingMachine()
                            {
                                return viewModel?.Params?.ShouldRaiseWashingMachine ?? INTRAVASCULARLY;
                            }
                            bool _shouldDrawRaiseWashingMachineSymbol()
                            {
                                return INTRAVASCULARLY;
                            }
                            bool shouldDrawRaiseWashingMachineSymbol(Hanging hanging)
                            {
                                return INTRAVASCULARLY;
                            }
                            void handleHanging(Hanging hanging, bool isLeftOrRight)
                            {
                                var linesDfferencers = new List<Polygon>();
                                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, int i, int j, string shadow)
                                {
                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                    {
                                        Dr.DrawSimpleLabel(basePt.ToPoint2D(), THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                                    }
                                    var p1 = basePt.ToPoint2d();
                                    {
                                        if (_shouldDrawRaiseWashingMachineSymbol())
                                        {
                                            var fixVec = new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE);
                                            var p = p1 + new Vector2d(THESAURUSSTAMPEDE, INCONSIDERABILIS) + new Vector2d(-THESAURUSCAVERN, THESAURUSSHROUD) + fixVec;
                                            fdBsPts.Add(p);
                                            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSSOMETIMES, THESAURUSSTAMPEDE), fixVec, new Vector2d(-THESAURUSQUAGMIRE, THESAURUSQUAGMIRE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFORMULATE), new Vector2d(-ELECTROMYOGRAPH, THESAURUSSTAMPEDE) };
                                            var segs = vecs.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE));
                                            drawDomePipes(segs, THESAURUSDEPLORE);
                                            DrainageSystemDiagram.DrawWashingMachineRaisingSymbol(segs.Last().EndPoint, THESAURUSOBSTINACY);
                                            return;
                                        }
                                    }
                                    {
                                        var p = p1 + new Vector2d(-THESAURUSCAVERN + (leftOrRight ? THESAURUSSTAMPEDE : THESAURUSDIFFICULTY), THESAURUSINTRENCH);
                                        fdBsPts.Add(p);
                                        floorDrainCbs[new GRect(basePt, basePt.OffsetXY(leftOrRight ? -THESAURUSDOMESTIC : THESAURUSDOMESTIC, QUOTATIONWITTIG)).ToPolygon()] = new FloorDrainCbItem()
                                        {
                                            BasePt = basePt.ToPoint2D(),
                                            Name = ACCOMMODATINGLY,
                                            LeftOrRight = leftOrRight,
                                        };
                                        return;
                                    }
                                }
                                void _DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight, int i, int j, string shadow)
                                {
                                    if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                    {
                                        Dr.DrawSimpleLabel(p1, THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                                    }
                                    if (!hasDrawedDSCurveLabel && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                                    {
                                        hasDrawedDSCurveLabel = THESAURUSOBSTINACY;
                                        var p2 = p1 + new Vector2d(-THESAURUSLOITER, THESAURUSSECLUSION - THESAURUSHYPNOTIC);
                                        if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                        {
                                            p2 += new Vector2d(PHOTOSYNTHETICALLY, -THESAURUSINTRENCH);
                                        }
                                        _DrawLabel(THESAURUSPUGNACIOUS, THESAURUSSCOUNDREL, p2, THESAURUSOBSTINACY, QUOTATIONBASTARD);
                                    }
                                    {
                                        var v = vec7;
                                        if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                        {
                                            v = default;
                                            p1 = p1.OffsetY(HYPERDISYLLABLE);
                                        }
                                        var p2 = p1 + v;
                                        if (!p1.Equals(p2))
                                        {
                                            dome_lines.Add(new GLineSegment(p1, p2));
                                        }
                                        DrawDSCurve(p2, leftOrRight, getDSCurveValue(), THESAURUSDEPLORE);
                                    }
                                }
                                ++counterPipeButtomHeightSymbol;
                                if (counterPipeButtomHeightSymbol == THESAURUSHOUSING && thwPipeLine.Labels.Any(x => IsFL(x)))
                                {
                                    if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                    {
                                        DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, info.StartPoint.OffsetY(-THESAURUSINTRENCH - UNAPPREHENSIBLE));
                                    }
                                    else
                                    {
                                        var c = gpItem.Hangings[i]?.FloorDrainsCount ?? THESAURUSSTAMPEDE;
                                        if (c > THESAURUSSTAMPEDE)
                                        {
                                            if (c == THESAURUSPERMUTATION && !gpItem.Hangings[i].IsSeries)
                                            {
                                                DrawPipeButtomHeightSymbol(THESAURUSHYPNOTIC, HEIGHT * THESAURUSRIBALD, info.StartPoint.OffsetXY(THESAURUSJACKPOT, -THESAURUSINTRENCH));
                                                var vecs = new List<Vector2d> { new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDERELICTION), new Vector2d(-THESAURUSINHERIT, THESAURUSSTAMPEDE) };
                                                var segs = vecs.ToGLineSegments(new List<Vector2d> { new Vector2d(-THESAURUSJACKPOT, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSINTRENCH) }.GetLastPoint(info.StartPoint));
                                                DrawPipeButtomHeightSymbol(segs.Last().EndPoint, segs);
                                            }
                                            else
                                            {
                                                DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, HEIGHT * THESAURUSRIBALD, info.StartPoint.OffsetY(-THESAURUSINTRENCH));
                                            }
                                        }
                                        else
                                        {
                                            DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, THESAURUSSTAMPEDE, info.EndPoint.OffsetY(HEIGHT / THESAURUSCOMMUNICATION));
                                        }
                                    }
                                }
                                var w = THROMBOEMBOLISM;
                                if (hanging.FloorDrainsCount == THESAURUSPERMUTATION && !hanging.HasDoubleSCurve)
                                {
                                    w = THESAURUSSTAMPEDE;
                                }
                                if (hanging.FloorDrainsCount == THESAURUSPERMUTATION && !hanging.HasDoubleSCurve && !hanging.IsSeries)
                                {
                                    var startPt = info.StartPoint.OffsetY(-THESAURUSNOTABLE - THESAURUSHOUSING);
                                    var delta = run.Is4Tune ? THESAURUSSTAMPEDE : HYPERDISYLLABLE + THESAURUSENTREPRENEUR;
                                    var _vecs1 = new List<Vector2d> { new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(-THESAURUSFLUTTER, THESAURUSSTAMPEDE), };
                                    var _vecs2 = new List<Vector2d> { new Vector2d(THESAURUSPERVADE + delta, THESAURUSPERVADE + delta), new Vector2d(THESAURUSFLUTTER - delta, THESAURUSSTAMPEDE), };
                                    var segs1 = _vecs1.ToGLineSegments(startPt);
                                    var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                                    DrawDomePipes(segs1);
                                    DrawDomePipes(segs2);
                                    _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSOBSTINACY, i, j, THESAURUSDEPLORE);
                                    _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, i, j, THESAURUSDEPLORE);
                                    if (run.Is4Tune)
                                    {
                                        var st = info.StartPoint;
                                        var p1 = new List<Vector2d> { new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMISSIONARY) }.GetLastPoint(st);
                                        var p2 = new List<Vector2d> { new Vector2d(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMISSIONARY) }.GetLastPoint(st);
                                        _DrawWrappingPipe(p1, THESAURUSDEPLORE);
                                        _DrawWrappingPipe(p2, THESAURUSDEPLORE);
                                    }
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE && hanging.HasDoubleSCurve)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(ANTICONVULSANTS, -ANTICONVULSANTS), new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) };
                                        var dx = vecs.GetLastPoint(Point2d.Origin).X;
                                        var startPt = info.EndPoint.OffsetXY(-dx, HEIGHT / THESAURUSCOMMUNICATION + ANTICONVULSANTS);
                                        var segs = vecs.ToGLineSegments(startPt);
                                        var p1 = segs.Last(INTROPUNITIVENESS).StartPoint;
                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                        _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSDEPLORE);
                                        var dn = getBasinDN();
                                        DrawNoteText(dn, p1 - new Vector2d(-ANTICONVULSANTS, -THESAURUSASSURANCE));
                                    }
                                    else
                                    {
                                        var beShort = hanging.FloorDrainsCount == THESAURUSHOUSING && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                        var vecs = new List<Vector2d> { new Vector2d(-MISAPPREHENSIVE, MISAPPREHENSIVE), new Vector2d(THESAURUSSTAMPEDE, ACANTHORHYNCHUS), new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(beShort ? THESAURUSSTAMPEDE : -THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(-w, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSCAVERN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSJACKPOT, THESAURUSSTAMPEDE) };
                                        if (isLeftOrRight == INTRAVASCULARLY)
                                        {
                                            vecs = vecs.GetYAxisMirror();
                                        }
                                        var pt = info.Segs[QUOTATIONEDIBLE].StartPoint.OffsetY(-THESAURUSPHANTOM).OffsetY(THESAURUSCONSIGNMENT - THESAURUSQUAGMIRE);
                                        if (isLeftOrRight == INTRAVASCULARLY && run.IsLongTranslatorToLeftOrRight == THESAURUSOBSTINACY)
                                        {
                                            var p1 = pt;
                                            var p2 = pt.OffsetX(ACETYLSALICYLIC);
                                            drawDomePipe(new GLineSegment(p1, p2));
                                            pt = p2;
                                        }
                                        if (isLeftOrRight == THESAURUSOBSTINACY && run.IsLongTranslatorToLeftOrRight == INTRAVASCULARLY)
                                        {
                                            var p1 = pt;
                                            var p2 = pt.OffsetX(-ACETYLSALICYLIC);
                                            drawDomePipe(new GLineSegment(p1, p2));
                                            pt = p2;
                                        }
                                        var isFDHigher = gpItem.Hangings[i].FlFixType == FixingLogic1.FlFixType.Higher && hanging.FloorDrainsCount > THESAURUSSTAMPEDE && run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight;
                                        if (isFDHigher)
                                        {
                                            pt = pt.OffsetY(-PREREGISTRATION);
                                        }
                                        Action f;
                                        var segs = vecs.ToGLineSegments(pt);
                                        {
                                            var _segs = segs.ToList();
                                            if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                            {
                                                if (hanging.IsSeries)
                                                {
                                                    _segs.RemoveAt(THESAURUSCOMMUNICATION);
                                                }
                                            }
                                            else if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                                            {
                                                _segs = segs.Take(THESAURUSCOMMUNICATION).ToList();
                                            }
                                            else if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE)
                                            {
                                                _segs = segs.Take(QUOTATIONEDIBLE).ToList();
                                            }
                                            if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(THESAURUSPERMUTATION); }
                                            f = () => { drawDomePipes(_segs, THESAURUSDEPLORE); };
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                                        {
                                            var p = segs.Last(INTROPUNITIVENESS).EndPoint;
                                            _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p + new Vector2d(THESAURUSHYPNOTIC, -THESAURUSGETAWAY));
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                        {
                                            var p2 = segs.Last(INTROPUNITIVENESS).EndPoint;
                                            var p1 = segs.Last(THESAURUSHOUSING).EndPoint;
                                            _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                            _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p1 + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                            DrawNoteText(v2, p2 + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                            if (!hanging.IsSeries)
                                            {
                                                drawDomePipes(new GLineSegment[] { segs.Last(THESAURUSPERMUTATION) }, THESAURUSDEPLORE);
                                            }
                                            {
                                                var fixY = QUOTATIONTRANSFERABLE;
                                                if (isFDHigher)
                                                {
                                                    fixY = PREREGISTRATION;
                                                }
                                                var _segs = new List<Vector2d> { new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSJOURNALIST, THESAURUSSTAMPEDE), new Vector2d(HYPERDISYLLABLE, THESAURUSSTAMPEDE), new Vector2d(CONSTITUTIVENESS, THESAURUSSTAMPEDE), new Vector2d(THESAURUSALLEGIANCE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -ALSOSESQUIALTERAL + fixY), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(p1);
                                                _segs.RemoveAt(THESAURUSPERMUTATION);
                                                var seg = new List<Vector2d> { new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) }.ToGLineSegments(p1)[THESAURUSHOUSING];
                                                f = () =>
                                                {
                                                    drawDomePipes(_segs, THESAURUSDEPLORE);
                                                    drawDomePipes(new GLineSegment[] { seg }, THESAURUSDEPLORE);
                                                };
                                            }
                                        }
                                        {
                                            var p = segs.Last(INTROPUNITIVENESS).EndPoint;
                                            var seg = new List<Vector2d> { new Vector2d(THESAURUSCUSTOMARY, -APOLLINARIANISM), new Vector2d(THESAURUSSTAMPEDE, -TRICHINELLIASIS) }.ToGLineSegments(p)[THESAURUSHOUSING];
                                            var pt1 = segs.First().StartPoint;
                                            var pt2 = pt1.OffsetY(TRICHINELLIASIS);
                                            var fixY = QUOTATIONTRANSFERABLE;
                                            if (isFDHigher)
                                            {
                                                fixY = PREREGISTRATION;
                                            }
                                            pt1 = pt1.OffsetY(fixY);
                                            pt2 = pt2.OffsetY(fixY);
                                            var dim = DrawDimLabel(pt1, pt2, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), INTERNALIZATION, QUOTATIONBENJAMIN);
                                        }
                                        if (hanging.HasSCurve)
                                        {
                                            var p1 = segs.Last(INTROPUNITIVENESS).StartPoint;
                                            _DrawSCurve(vec7, p1, isLeftOrRight);
                                        }
                                        if (hanging.HasDoubleSCurve)
                                        {
                                            var p1 = segs.Last(INTROPUNITIVENESS).StartPoint;
                                            _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSDEPLORE);
                                        }
                                        f?.Invoke();
                                    }
                                }
                                else
                                {
                                    if (gpItem.IsFL0)
                                    {
                                        DrawFloorDrain((info.StartPoint + new Vector2d(-THESAURUSSATIATE, -THESAURUSINTRENCH)).ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSINFLEXIBLE), new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(-THESAURUSCAPITALISM, THESAURUSSTAMPEDE) };
                                        var segs = vecs.ToGLineSegments(info.StartPoint).Skip(THESAURUSHOUSING).ToList();
                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                    }
                                    else
                                    {
                                        var beShort = hanging.FloorDrainsCount == THESAURUSHOUSING && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                        var vecs = new List<Vector2d> { new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(beShort ? THESAURUSSTAMPEDE : -THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(-w, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSCAVERN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSJACKPOT, THESAURUSSTAMPEDE) };
                                        if (isLeftOrRight == INTRAVASCULARLY)
                                        {
                                            vecs = vecs.GetYAxisMirror();
                                        }
                                        var startPt = info.StartPoint.OffsetY(-THESAURUSNOTABLE - THESAURUSHOUSING);
                                        if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE && hanging.HasDoubleSCurve)
                                        {
                                            startPt = info.EndPoint.OffsetY(-THESAURUSDISCOLOUR + HEIGHT / THESAURUSCOMMUNICATION);
                                        }
                                        var ok = INTRAVASCULARLY;
                                        if (hanging.FloorDrainsCount == THESAURUSPERMUTATION && !hanging.HasDoubleSCurve)
                                        {
                                            if (hanging.IsSeries)
                                            {
                                                var segs = vecs.ToGLineSegments(startPt);
                                                var _segs = segs.ToList();
                                                linesDfferencers.Add(GRect.Create(_segs[INTROPUNITIVENESS].EndPoint, THESAURUSENTREPRENEUR).ToPolygon());
                                                var p2 = segs.Last(INTROPUNITIVENESS).EndPoint;
                                                var p1 = segs.Last(THESAURUSHOUSING).EndPoint;
                                                _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                                _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                                Get2FloorDrainDN(out string v1, out string v2);
                                                DrawNoteText(v1, p1 + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                                DrawNoteText(v2, p2 + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                                segs = new List<Vector2d> { new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSJOURNALIST, THESAURUSSTAMPEDE), new Vector2d(HYPERDISYLLABLE, THESAURUSSTAMPEDE), new Vector2d(QUINQUARTICULAR, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(p1);
                                                var p = segs[QUOTATIONEDIBLE].StartPoint;
                                                segs.RemoveAt(THESAURUSPERMUTATION);
                                                dome_lines.AddRange(segs);
                                                dome_lines.AddRange(new List<Vector2d> { new Vector2d(THESAURUSINVADE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSALLEGIANCE, -THESAURUSPERVADE) }.ToGLineSegments(p));
                                            }
                                            else
                                            {
                                                var delta = HYPERDISYLLABLE;
                                                var _vecs1 = new List<Vector2d> { new Vector2d(-THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(-THESAURUSFLUTTER, THESAURUSSTAMPEDE), };
                                                var _vecs2 = new List<Vector2d> { new Vector2d(THESAURUSPERVADE + delta, THESAURUSPERVADE + delta), new Vector2d(THESAURUSFLUTTER - delta, THESAURUSSTAMPEDE), };
                                                var segs1 = _vecs1.ToGLineSegments(startPt);
                                                var segs2 = _vecs2.ToGLineSegments(startPt.OffsetY(-delta));
                                                dome_lines.AddRange(segs1);
                                                dome_lines.AddRange(segs2);
                                                _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSOBSTINACY, i, j, THESAURUSDEPLORE);
                                                _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, i, j, THESAURUSDEPLORE);
                                            }
                                            ok = THESAURUSOBSTINACY;
                                        }
                                        Action f = null;
                                        if (!ok)
                                        {
                                            if (gpItem.Hangings[i].FlCaseEnum != FixingLogic1.FlCaseEnum.Case1)
                                            {
                                                var segs = vecs.ToGLineSegments(startPt);
                                                var _segs = segs.ToList();
                                                {
                                                    if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                                    {
                                                        if (hanging.IsSeries)
                                                        {
                                                            _segs.RemoveAt(INTROPUNITIVENESS);
                                                        }
                                                    }
                                                    if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                                                    {
                                                        _segs.RemoveAt(QUOTATIONEDIBLE);
                                                        _segs.RemoveAt(INTROPUNITIVENESS);
                                                    }
                                                    if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE)
                                                    {
                                                        _segs = _segs.Take(THESAURUSPERMUTATION).ToList();
                                                    }
                                                    if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(THESAURUSPERMUTATION); }
                                                }
                                                if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                                                {
                                                    var p = segs.Last(INTROPUNITIVENESS).EndPoint;
                                                    _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p + new Vector2d(THESAURUSHYPNOTIC, -THESAURUSGETAWAY));
                                                }
                                                if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                                {
                                                    var p2 = segs.Last(INTROPUNITIVENESS).EndPoint;
                                                    var p1 = segs.Last(THESAURUSHOUSING).EndPoint;
                                                    _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                                    _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j, THESAURUSDEPLORE);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p1 + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                                    DrawNoteText(v2, p2 + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                                }
                                                f = () => drawDomePipes(_segs, THESAURUSDEPLORE);
                                            }
                                        }
                                        {
                                            var segs = vecs.ToGLineSegments(startPt);
                                            if (hanging.HasSCurve)
                                            {
                                                var p1 = segs.Last(INTROPUNITIVENESS).StartPoint;
                                                _DrawSCurve(vec7, p1, isLeftOrRight);
                                            }
                                            if (hanging.HasDoubleSCurve)
                                            {
                                                var p1 = segs.Last(INTROPUNITIVENESS).StartPoint;
                                                if (gpItem.Hangings[i].FlCaseEnum == FixingLogic1.FlCaseEnum.Case1)
                                                {
                                                    var p2 = p1 + vec7;
                                                    var segs1 = new List<Vector2d> { new Vector2d(-INCONSIDERABILIS + THESAURUSBISEXUAL + THESAURUSQUAGMIRE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -POLYOXYMETHYLENE - THESAURUSSPIRIT - THESAURUSQUAGMIRE), new Vector2d(MISAPPREHENSIVE, -MISAPPREHENSIVE) }.ToGLineSegments(p2);
                                                    drawDomePipes(segs1, THESAURUSDEPLORE);
                                                    {
                                                        Vector2d v = default;
                                                        var b = isLeftOrRight;
                                                        if (b && getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                                        {
                                                            b = INTRAVASCULARLY;
                                                            v = new Vector2d(-THESAURUSCAVERN, -QUOTATIONWITTIG);
                                                        }
                                                        _DrawDSCurve(default(Vector2d), p2 + v, b, i, j, THESAURUSDEPLORE);
                                                    }
                                                    var p3 = segs1.Last().EndPoint;
                                                    var p4 = p3.OffsetY(THESAURUSCANDIDATE);
                                                    DrawDimLabel(p3, p4, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), INTERNALIZATION, QUOTATIONBENJAMIN);
                                                    var dn = getBasinDN();
                                                    Dr.DrawDN_2(segs1.Last().StartPoint + new Vector2d(ALUMINOSILICATES + THESAURUSEXHILARATION - HYPERDISYLLABLE - POLIOENCEPHALITIS, -MISAPPREHENSIVE), THESAURUSSTRIPED, dn);
                                                }
                                                else
                                                {
                                                    var fixY = THESAURUSADJUST + THESAURUSHYPNOTIC;
                                                    _DrawDSCurve(vec7, p1, isLeftOrRight, i, j, THESAURUSDEPLORE);
                                                    if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                                    {
                                                        var segs1 = new List<Vector2d> { new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(p1.OffsetY(HYPERDISYLLABLE));
                                                        f = () => { drawDomePipes(segs1, THESAURUSDEPLORE); };
                                                    }
                                                    var dn = getBasinDN();
                                                    DrawNoteText(dn, p1.OffsetY(fixY));
                                                }
                                            }
                                        }
                                        f?.Invoke();
                                    }
                                }
                                if (linesDfferencers.Count > THESAURUSSTAMPEDE)
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
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, THESAURUSPERMUTATION));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSPERMUTATION, INTROPUNITIVENESS));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.FirstRightRun)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, SUPERLATIVENESS));
                                    var p3 = info.EndPoint.OffsetX(-THESAURUSHYPNOTIC);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, INTROPUNITIVENESS));
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
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, SUPERLATIVENESS));
                                    var p3 = info.EndPoint.OffsetX(-THESAURUSHYPNOTIC);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, INTROPUNITIVENESS));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.BlueToRightFirst)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, SUPERLATIVENESS));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSHOUSING, INTROPUNITIVENESS));
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                                }
                                if (bi.BlueToLeftLast)
                                {
                                    if (run.HasLongTranslator)
                                    {
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var _dy = THESAURUSHYPNOTIC;
                                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING - _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy + _dy + THESAURUSDOMESTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSHOUSING).ToList());
                                        }
                                        else
                                        {
                                            var _dy = THESAURUSHYPNOTIC;
                                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy - _dy + THESAURUSDOMESTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSHOUSING).ToList());
                                        }
                                    }
                                    else if (!run.HasLongTranslator)
                                    {
                                        var vs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMERITORIOUS), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                        info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                    }
                                }
                                if (bi.BlueToRightLast)
                                {
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var p1 = info.EndPoint;
                                        var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE));
                                        var p3 = info.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                                        var p4 = p3.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE));
                                        var p5 = p1.OffsetY(HEIGHT);
                                        info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, p2), new GLineSegment(p2, p5) };
                                    }
                                }
                                if (bi.BlueToLeftMiddle)
                                {
                                    if (!run.HasLongTranslator && !run.HasShortTranslator)
                                    {
                                        var p1 = info.EndPoint;
                                        var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE));
                                        var p3 = info.EndPoint.OffsetX(-THESAURUSHYPNOTIC);
                                        var p4 = p3.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE));
                                        var segs = info.Segs.ToList();
                                        segs.Add(new GLineSegment(p2, p4));
                                        info.DisplaySegs = segs;
                                    }
                                    else if (run.HasLongTranslator)
                                    {
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var _dy = THESAURUSHYPNOTIC;
                                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING - _dy), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy + _dy) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSHOUSING).ToList());
                                        }
                                        else
                                        {
                                            var _dy = THESAURUSHYPNOTIC;
                                            var vs1 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -SUBCATEGORIZING + _dy), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSUNEVEN, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSUNCOMMITTED), new Vector2d(THESAURUSSTAMPEDE, -CONTRADISTINGUISHED - dy - _dy) };
                                            var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                            var vs2 = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSHYPNOTIC), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHALTER) };
                                            info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSHOUSING).ToList());
                                        }
                                    }
                                }
                                if (bi.BlueToRightMiddle)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSHYPNOTIC);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE));
                                    var segs = info.Segs.ToList();
                                    segs.Add(new GLineSegment(p2, p4));
                                    info.DisplaySegs = segs;
                                }
                                {
                                    var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -INCONSIDERABILIS), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSCOMATOSE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -OTHERWORLDLINESS), new Vector2d(-MISAPPREHENSIVE, -MISAPPREHENSIVE) };
                                    if (bi.HasLongTranslatorToLeft)
                                    {
                                        var vs = vecs;
                                        info.DisplaySegs = vecs.ToGLineSegments(info.StartPoint);
                                        if (!bi.IsLast)
                                        {
                                            var pt = vs.Take(vs.Count - THESAURUSHOUSING).GetLastPoint(info.StartPoint);
                                            info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSRATION) }.ToGLineSegments(pt));
                                        }
                                    }
                                    if (bi.HasLongTranslatorToRight)
                                    {
                                        var vs = vecs.GetYAxisMirror();
                                        info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                        if (!bi.IsLast)
                                        {
                                            var pt = vs.Take(vs.Count - THESAURUSHOUSING).GetLastPoint(info.StartPoint);
                                            info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSRATION) }.ToGLineSegments(pt));
                                        }
                                    }
                                }
                            }
                            if (run.LeftHanging != null)
                            {
                                run.LeftHanging.IsSeries = gpItem.Hangings.TryGet(i + THESAURUSHOUSING)?.IsSeries ?? THESAURUSOBSTINACY;
                                handleHanging(run.LeftHanging, THESAURUSOBSTINACY);
                            }
                            if (run.BranchInfo != null)
                            {
                                handleBranchInfo(run, info);
                            }
                            if (run.ShowShortTranslatorLabel)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(THESAURUSNEGATE, THESAURUSNEGATE), new Vector2d(-QUOTATIONRHEUMATOID, QUOTATIONRHEUMATOID), new Vector2d(-THESAURUSSENSITIVE, THESAURUSSTAMPEDE) };
                                var segs = vecs.ToGLineSegments(info.EndPoint).Skip(THESAURUSHOUSING).ToList();
                                DrawDraiNoteLines(segs);
                                DrawDraiNoteLines(segs);
                                var text = THESAURUSECHELON;
                                var pt = segs.Last().EndPoint;
                                DrawNoteText(text, pt);
                            }
                            if (run.HasCheckPoint)
                            {
                                var h = HEIGHT / ACANTHOCEPHALANS * QUOTATIONEDIBLE;
                                Point2d pt1, pt2;
                                {
                                    pt1 = info.EndPoint.OffsetY(h);
                                    pt2 = info.EndPoint;
                                    if (run.HasLongTranslator)
                                    {
                                        pt1 = info.EndPoint.OffsetY(DETERMINATENESS + QUINQUARTICULAR);
                                    }
                                }
                                _DrawCheckPoint(pt1, THESAURUSOBSTINACY, THESAURUSDEPLORE);
                                if (storey == THESAURUSREGION)
                                {
                                    var dx = -POLYOXYMETHYLENE;
                                    if (gpItem.HasBasinInKitchenAt1F)
                                    {
                                        dx = POLYOXYMETHYLENE;
                                    }
                                    {
                                        var dim = DrawDimLabel(pt1, pt2, new Vector2d(dx, THESAURUSSTAMPEDE), gpItem.PipeType == PipeType.PL ? CONSECUTIVENESS : THESAURUSDUBIETY, QUOTATIONBENJAMIN);
                                        if (dx < THESAURUSSTAMPEDE)
                                        {
                                            dim.TextPosition = (pt1 + new Vector2d(dx, THESAURUSSTAMPEDE) + new Vector2d(-THESAURUSBOMBARD, -MISAPPREHENSIVE) + new Vector2d(THESAURUSSTAMPEDE, HYPERDISYLLABLE)).ToPoint3d();
                                        }
                                    }
                                    if (gpItem.HasTl && allStoreys[i] == gpItem.MinTl + THESAURUSASPIRATION)
                                    {
                                        var k = THESAURUSINCOMING / HEIGHT;
                                        pt1 = info.EndPoint;
                                        pt2 = pt1.OffsetY(INTERNATIONALLY * k);
                                        if (run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight)
                                        {
                                            pt2 = pt1.OffsetY(THESAURUSEVIDENT);
                                        }
                                        var dim = DrawDimLabel(pt1, pt2, new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, QUOTATIONBENJAMIN);
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
                                    var vecs = new List<Vector2d> { new Vector2d(-MISAPPREHENSIVE, MISAPPREHENSIVE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSHYPNOTIC), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSINDOMITABLE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSOFFEND) };
                                    if (run.IsLongTranslatorToLeftOrRight == INTRAVASCULARLY)
                                    {
                                        vecs = vecs.GetYAxisMirror();
                                    }
                                    if (run.HasShortTranslator)
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last(THESAURUSPERMUTATION).StartPoint.OffsetY(-THESAURUSHYPNOTIC));
                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSPERMUTATION);
                                    }
                                    else
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-THESAURUSHYPNOTIC));
                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSPERMUTATION);
                                        if (run.IsLongTranslatorToLeftOrRight)
                                        {
                                            var pt1 = segs.First().StartPoint;
                                            var pt2 = pt1.OffsetY(THESAURUSDISCERNIBLE);
                                            var dim = DrawDimLabel(pt1, pt2, new Vector2d(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE), INTERNALIZATION, QUOTATIONBENJAMIN);
                                            dim.TextPosition = (pt1 + new Vector2d(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE) + new Vector2d(-PRESBYTERIANIZE, THESAURUSDEGREE) + new Vector2d(THESAURUSSENTIMENTALITY, MISAPPREHENSIVE - ULTRASONICATION)).ToPoint3d();
                                        }
                                    }
                                }
                                else
                                {
                                    _DrawCleaningPort(info.StartPoint.OffsetY(-THESAURUSHYPNOTIC).ToPoint3d(), THESAURUSOBSTINACY, THESAURUSPERMUTATION);
                                }
                            }
                            if (run.HasShortTranslator)
                            {
                                DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                            }
                        }
                        var showAllFloorDrainLabel = INTRAVASCULARLY;
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var (ok, item) = gpItem.Items.TryGetValue(i + THESAURUSHOUSING);
                            if (!ok) continue;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                            }
                        }
                        for (int i = start; i >= end; i--)
                        {
                            if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                            var hanging = gpItem.Hangings.TryGet(i + THESAURUSHOUSING);
                            if (hanging == null) continue;
                            var fdsCount = hanging.FloorDrainsCount;
                            if (fdsCount == THESAURUSSTAMPEDE) continue;
                            var wfdsCount = hanging.WashingMachineFloorDrainsCount;
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (fdsCount > THESAURUSSTAMPEDE)
                                {
                                    if (wfdsCount > THESAURUSSTAMPEDE)
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
                            var hanging = gpItem.Hangings.TryGet(i + THESAURUSHOUSING);
                            if (hanging == null) continue;
                            var fdsCount = hanging.FloorDrainsCount;
                            if (fdsCount == THESAURUSSTAMPEDE) continue;
                            var wfdsCount = hanging.WashingMachineFloorDrainsCount;
                            var h = QUOTATIONTRANSFERABLE;
                            var ok_texts = new HashSet<string>();
                            foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                            {
                                if (fdsCount > THESAURUSSTAMPEDE)
                                {
                                    if (wfdsCount > THESAURUSSTAMPEDE)
                                    {
                                        wfdsCount--;
                                        h += INCONSIDERABILIS;
                                        if (hanging.RoomName != null)
                                        {
                                            var text = $"接{hanging.RoomName}洗衣机地漏";
                                            if (!ok_texts.Contains(text))
                                            {
                                                _DrawLabel(text, $"{getWashingMachineFloorDrainDN()}，余同", pt, THESAURUSOBSTINACY, h);
                                                ok_texts.Add(text);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        h += INCONSIDERABILIS;
                                        if (hanging.RoomName != null)
                                        {
                                            _DrawLabel($"接{hanging.RoomName}地漏", $"{getWashingMachineFloorDrainDN()}，余同", pt, THESAURUSOBSTINACY, h);
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
                                    o.Name = THESAURUSSYNTHETIC;
                                }
                                DrawFloorDrain(o.BasePt.ToPoint3d(), o.LeftOrRight, o.Name);
                            }
                        }
                    }
                    PipeRunLocationInfo[] getPipeRunLocationInfos()
                    {
                        var infos = new PipeRunLocationInfo[allStoreys.Count];
                        for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                        {
                            infos[i] = new PipeRunLocationInfo() { Visible = THESAURUSOBSTINACY, Storey = allStoreys[i], };
                        }
                        {
                            var tdx = UNDENOMINATIONAL;
                            for (int i = start; i >= end; i--)
                            {
                                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                                var basePt = bsPt1.OffsetX(OFFSET_X + (j + THESAURUSHOUSING) * SPAN_X) + new Vector2d(tdx, THESAURUSSTAMPEDE);
                                var run = thwPipeLine.PipeRuns.TryGet(i);
                                fixY = THESAURUSSTAMPEDE;
                                PipeRunLocationInfo drawNormal()
                                {
                                    {
                                        var vecs = vecs0;
                                        var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                        var dx = vecs.Sum(v => v.X);
                                        tdx += dx;
                                        infos[i].BasePoint = basePt;
                                        infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                        infos[i].HangingEndPoint = infos[i].EndPoint;
                                        infos[i].Vector2ds = vecs;
                                        infos[i].Segs = segs;
                                        infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                                        infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                                    }
                                    {
                                        var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                        infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                                    }
                                    {
                                        var info = infos[i];
                                        var k = HEIGHT / THESAURUSINCOMING;
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMOTIONLESS * k), new Vector2d(-THESAURUSHYPNOTIC, -INTERNATIONALLY * k) };
                                        var segs = vecs.ToGLineSegments(info.EndPoint.OffsetY(HEIGHT)).Skip(THESAURUSHOUSING).ToList();
                                        info.RightSegsLast = segs;
                                    }
                                    {
                                        var pt = infos[i].Segs.First().StartPoint.OffsetX(THESAURUSHYPNOTIC);
                                        var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE))) };
                                        infos[i].RightSegsFirst = segs;
                                        segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                                    }
                                    return infos[i];
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
                                _dy = run.DrawLongHLineHigher ? CONSCRIPTIONIST : THESAURUSSTAMPEDE;
                                if (run.HasLongTranslator && run.HasShortTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs3;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(QUOTATIONEDIBLE);
                                            segs.Add(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[INTROPUNITIVENESS].EndPoint.OffsetXY(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC)));
                                            segs.Add(new GLineSegment(segs[THESAURUSPERMUTATION].EndPoint, new Point2d(segs[THESAURUSCOMMUNICATION].EndPoint.X, segs[THESAURUSPERMUTATION].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(INTROPUNITIVENESS);
                                            segs = new List<GLineSegment>() { segs[INTROPUNITIVENESS], new GLineSegment(segs[INTROPUNITIVENESS].StartPoint, segs[THESAURUSSTAMPEDE].StartPoint) };
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs6;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(-THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))).Offset(THESAURUSDICTATORIAL, THESAURUSSTAMPEDE));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(QUOTATIONEDIBLE);
                                            segs.Add(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[QUOTATIONEDIBLE].StartPoint));
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSHYPNOTIC)));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].Segs[QUOTATIONEDIBLE].EndPoint;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    switch (gpItem.Hangings[i].FlFixType)
                                    {
                                        case FixingLogic1.FlFixType.NoFix:
                                            break;
                                        case FixingLogic1.FlFixType.MiddleHigher:
                                            fixY = THESAURUSFEELER / QUOTATIONBASTARD * HEIGHT;
                                            break;
                                        case FixingLogic1.FlFixType.Lower:
                                            fixY = -THESAURUSATTENDANCE / THESAURUSPERMUTATION / QUOTATIONBASTARD * HEIGHT;
                                            break;
                                        case FixingLogic1.FlFixType.Higher:
                                            fixY = THESAURUSATTENDANCE / THESAURUSPERMUTATION / QUOTATIONBASTARD * HEIGHT + THESAURUSINACCURACY / QUOTATIONBASTARD * HEIGHT;
                                            break;
                                        default:
                                            break;
                                    }
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        {
                                            var vecs = vecs1;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            segs = segs.Take(QUOTATIONEDIBLE).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[INTROPUNITIVENESS].EndPoint.OffsetXY(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC))).ToList();
                                            segs.Add(new GLineSegment(segs[THESAURUSPERMUTATION].EndPoint, new Point2d(segs[THESAURUSCOMMUNICATION].EndPoint.X, segs[THESAURUSPERMUTATION].EndPoint.Y)));
                                            segs.RemoveAt(THESAURUSCOMMUNICATION);
                                            segs.RemoveAt(INTROPUNITIVENESS);
                                            segs = new List<GLineSegment>() { segs[INTROPUNITIVENESS], new GLineSegment(segs[INTROPUNITIVENESS].StartPoint, segs[THESAURUSSTAMPEDE].StartPoint) };
                                            var h = HEIGHT - QUOTATIONBASTARD;
                                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSEXCHANGE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-UNPERISHABLENESS, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSDENTIST - HYPERDISYLLABLE - h), new Vector2d(-MISAPPREHENSIVE, -NOVAEHOLLANDIAE) };
                                            segs = vecs.ToGLineSegments(infos[i].BasePoint.OffsetXY(THESAURUSHYPNOTIC, HEIGHT));
                                            infos[i].RightSegsLast = segs;
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs4;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(THESAURUSHYPNOTIC)).Skip(THESAURUSHOUSING).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))).Offset(THESAURUSDICTATORIAL, THESAURUSSTAMPEDE));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle;
                                            infos[i].RightSegsLast = segs.Take(QUOTATIONEDIBLE).YieldAfter(new GLineSegment(segs[INTROPUNITIVENESS].EndPoint, segs[THESAURUSCOMMUNICATION].StartPoint)).YieldAfter(segs[THESAURUSCOMMUNICATION]).ToList();
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var pt = segs[QUOTATIONEDIBLE].EndPoint;
                                            segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(SUPERLATIVENESS, THESAURUSREVERSE))) };
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].EndPoint, pt.OffsetXY(-THESAURUSHYPNOTIC, HEIGHT.ToRatioInt(THESAURUSACTUAL, THESAURUSREVERSE))));
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
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, segs[THESAURUSPERMUTATION].StartPoint), segs[THESAURUSPERMUTATION] };
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[THESAURUSPERMUTATION].StartPoint, segs[THESAURUSPERMUTATION].EndPoint);
                                            segs[THESAURUSPERMUTATION] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(THESAURUSSTAMPEDE);
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, r.RightButtom));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    else
                                    {
                                        {
                                            var vecs = vecs5;
                                            var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSHOUSING).ToList();
                                            var dx = vecs.Sum(v => v.X);
                                            tdx += dx;
                                            infos[i].BasePoint = basePt;
                                            infos[i].EndPoint = basePt + new Vector2d(dx, THESAURUSSTAMPEDE);
                                            infos[i].Vector2ds = vecs;
                                            infos[i].Segs = segs;
                                            infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)).ToList();
                                            infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / SUPERLATIVENESS).OffsetX(-THESAURUSPERVADE);
                                        }
                                        {
                                            var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                            infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSREVERSE - THESAURUSACTUAL, THESAURUSREVERSE)), pt.OffsetXY(-THESAURUSHYPNOTIC, -HEIGHT.ToRatioInt(THESAURUSREVERSE - SUPERLATIVENESS, THESAURUSREVERSE))));
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, segs[THESAURUSPERMUTATION].StartPoint), segs[THESAURUSPERMUTATION] };
                                        }
                                        {
                                            var segs = infos[i].RightSegsMiddle.ToList();
                                            var r = new GRect(segs[THESAURUSPERMUTATION].StartPoint, segs[THESAURUSPERMUTATION].EndPoint);
                                            segs[THESAURUSPERMUTATION] = new GLineSegment(r.LeftTop, r.RightButtom);
                                            segs.RemoveAt(THESAURUSSTAMPEDE);
                                            segs.Add(new GLineSegment(segs[THESAURUSSTAMPEDE].StartPoint, r.RightButtom));
                                            infos[i].RightSegsFirst = segs;
                                        }
                                    }
                                    infos[i].HangingEndPoint = infos[i].Segs[THESAURUSSTAMPEDE].EndPoint;
                                }
                                else
                                {
                                    drawNormal();
                                }
                            }
                        }
                        for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
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
                        var gap = THESAURUSENTREPRENEUR;
                        var factor = THESAURUSDISPASSIONATE;
                        double height = THESAURUSENDANGER;
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
                            Dr.SetLabelStylesForDraiNote(line);
                        }
                        var txtBasePt = isLeftOrRight ? segs[THESAURUSHOUSING].EndPoint : segs[THESAURUSHOUSING].StartPoint;
                        txtBasePt = txtBasePt.OffsetY(THESAURUSENTREPRENEUR);
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
                    for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            var segs = info.DisplaySegs ?? info.Segs;
                            if (segs != null)
                            {
                                drawDomePipes(segs, THESAURUSDEPLORE);
                            }
                        }
                    }
                    {
                        var hasPipeLabelStoreys = new HashSet<string>();
                        {
                            var _allSmoothStoreys = new List<string>();
                            for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
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
                            var _storeys = new string[] { _allSmoothStoreys.GetAt(THESAURUSPERMUTATION), _allSmoothStoreys.GetLastOrDefault(INTROPUNITIVENESS) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                _storeys = new string[] { _allSmoothStoreys.FirstOrDefault(), _allSmoothStoreys.LastOrDefault() }.SelectNotNull().Distinct().ToList();
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
                            hasPipeLabelStoreys.AddRange(_storeys);
                            foreach (var storey in _storeys)
                            {
                                var i = allNumStoreyLabels.IndexOf(storey);
                                var info = infos[i];
                                {
                                    string label1, label2;
                                    var isLeftOrRight = !thwPipeLine.Labels.Any(x => IsFL(x));
                                    var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).Distinct().OrderBy(x => x).ToList();
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
                                    drawLabel(info.PlBasePt, label1, label2, isLeftOrRight);
                                }
                                if (gpItem.HasTl)
                                {
                                    string label1, label2;
                                    var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => IsTL(x))).Distinct().OrderBy(x => x).ToList();
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
                                    drawLabel(info.PlBasePt.OffsetX(THESAURUSHYPNOTIC), label1, label2, INTRAVASCULARLY);
                                }
                            }
                        }
                        {
                            List<string> _storeys;
                            var _allSmoothStoreys = new List<string>();
                            for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
                            {
                                var run = runs.TryGet(i);
                                if (run != null)
                                {
                                    var s = allNumStoreyLabels[i];
                                    if (!run.HasLongTranslator && !run.HasShortTranslator && !hasPipeLabelStoreys.Contains(s))
                                    {
                                        _allSmoothStoreys.Add(s);
                                    }
                                }
                            }
                            _storeys = new string[] { _allSmoothStoreys.GetAt(THESAURUSHOUSING), _allSmoothStoreys.GetLastOrDefault(THESAURUSPERMUTATION) }.SelectNotNull().Distinct().ToList();
                            if (_storeys.Count == THESAURUSSTAMPEDE)
                            {
                                _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSHOUSING), allNumStoreyLabels.GetLastOrDefault(THESAURUSPERMUTATION) }.SelectNotNull().Distinct().ToList();
                            }
                            if (_storeys.Count == THESAURUSSTAMPEDE)
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
                                        if (((gpItem.Hangings.TryGet(i + THESAURUSHOUSING)?.FloorDrainsCount ?? THESAURUSSTAMPEDE) > THESAURUSSTAMPEDE)
                                            || (gpItem.Hangings.TryGet(i)?.HasDoubleSCurve ?? INTRAVASCULARLY))
                                        {
                                            v = new Vector2d(POLYOXYMETHYLENE, THESAURUSSTAMPEDE);
                                        }
                                        if (gpItem.IsFL0)
                                        {
                                            Dr.DrawDN_2(info.EndPoint + v, THESAURUSSTRIPED, viewModel?.Params?.DirtyWaterWellDN ?? IRRESPONSIBLENESS);
                                        }
                                        else
                                        {
                                            Dr.DrawDN_2(info.EndPoint + v, THESAURUSSTRIPED);
                                        }
                                        if (gpItem.HasTl)
                                        {
                                            Dr.DrawDN_3(info.EndPoint.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), THESAURUSSTRIPED);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    var b = INTRAVASCULARLY;
                    for (int i = THESAURUSSTAMPEDE; i < allNumStoreyLabels.Count; i++)
                    {
                        var info = infos.TryGet(i);
                        if (info != null && info.Visible)
                        {
                            void TestRightSegsMiddle()
                            {
                                var segs = info.RightSegsMiddle;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSDEPLORE);
                                }
                            }
                            void TestRightSegsLast()
                            {
                                var segs = info.RightSegsLast;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSDEPLORE);
                                }
                            }
                            void TestRightSegsFirst()
                            {
                                var segs = info.RightSegsFirst;
                                if (segs != null)
                                {
                                    drawVentPipes(segs, THESAURUSDEPLORE);
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
                                            drawVentPipes(segs, THESAURUSDEPLORE);
                                        }
                                    }
                                    else if (gpItem.MinTl + THESAURUSASPIRATION == storey)
                                    {
                                        var segs = info.RightSegsLast;
                                        if (segs != null)
                                        {
                                            drawVentPipes(segs, THESAURUSDEPLORE);
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
                                                if (b) segs = segs.Take(THESAURUSHOUSING).ToList();
                                            }
                                            drawVentPipes(segs, THESAURUSDEPLORE);
                                        }
                                    }
                                }
                            }
                            Run();
                        }
                    }
                    {
                        var i = allNumStoreyLabels.IndexOf(THESAURUSREGION);
                        if (i >= THESAURUSSTAMPEDE)
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
                                    DN1 = IRRESPONSIBLENESS,
                                };
                                if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                {
                                    var basePt = info.EndPoint;
                                    if (gpItem.HasRainPortForFL0)
                                    {
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSMAIDENLY, THESAURUSSTAMPEDE) };
                                            var segs = vecs.ToGLineSegments(basePt);
                                            drawDomePipes(segs, THESAURUSDEPLORE);
                                            var pt = segs.Last().EndPoint.ToPoint3d();
                                            {
                                                Dr.DrawRainPort(pt.OffsetX(THESAURUSDOMESTIC));
                                                Dr.DrawRainPortLabel(pt.OffsetX(-THESAURUSENTREPRENEUR));
                                                Dr.DrawStarterPipeHeightLabel(pt.OffsetX(-THESAURUSENTREPRENEUR + THESAURUSMEDIATION));
                                            }
                                        }
                                        if (gpItem.IsConnectedToFloorDrainForFL0)
                                        {
                                            var p = basePt + new Vector2d(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH);
                                            DrawFloorDrain(p.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSFICTION), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC), new Vector2d(-INCONSIDERABILIS, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSNEGLIGENCE, THESAURUSNEGLIGENCE) };
                                            var segs = vecs.ToGLineSegments(p + new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE));
                                            drawDomePipes(segs, THESAURUSDEPLORE);
                                        }
                                    }
                                    else
                                    {
                                        var p = basePt + new Vector2d(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH);
                                        if (gpItem.IsFL0)
                                        {
                                            if (gpItem.IsConnectedToFloorDrainForFL0)
                                            {
                                                if (gpItem.MergeFloorDrainForFL0)
                                                {
                                                    DrawFloorDrain(p.ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                    {
                                                        var vecs = new List<Vector2d>() { new Vector2d(THESAURUSSTAMPEDE, -HYPERDISYLLABLE + THESAURUSCREDITABLE), new Vector2d(-THESAURUSHYPNOTIC, -THESAURUSHYPNOTIC), new Vector2d(-HYPERDISYLLABLE - THESAURUSJINGLE + ALSOMEGACEPHALOUS * THESAURUSPERMUTATION, THESAURUSSTAMPEDE), new Vector2d(-THESAURUSHYPNOTIC, THESAURUSHYPNOTIC) };
                                                        var segs = vecs.ToGLineSegments(p + new Vector2d(-THESAURUSCAVERN, -THESAURUSINTRACTABLE));
                                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                                        var seg = new List<Vector2d> { new Vector2d(-THESAURUSDICTATORIAL, -THESAURUSFICTION), new Vector2d(THESAURUSINCARCERATE, THESAURUSSTAMPEDE) }.ToGLineSegments(segs.First().StartPoint)[THESAURUSHOUSING];
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(THESAURUSSTAMPEDE, -POLYOXYMETHYLENE), METACOMMUNICATION, QUOTATIONBENJAMIN);
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSINHERIT, -DEMONSTRATIONIST), new Vector2d(THESAURUSMIRTHFUL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                                                    var segs = vecs.ToGLineSegments(info.EndPoint).Skip(THESAURUSHOUSING).ToList();
                                                    drawDomePipes(segs, THESAURUSDEPLORE);
                                                    DrawFloorDrain((segs.Last().EndPoint + new Vector2d(THESAURUSCAVERN, THESAURUSINTRACTABLE)).ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                }
                                            }
                                        }
                                        DrawOutlets1(THESAURUSDEPLORE, basePt, THESAURUSEXECRABLE, output, isRainWaterWell: THESAURUSOBSTINACY);
                                    }
                                }
                                else if (gpItem.IsSingleOutlet)
                                {
                                    void DrawOutlets3(string shadow, Point2d basePoint)
                                    {
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                        {
                                            Dr.DrawSimpleLabel(basePoint.OffsetY(-HYPERDISYLLABLE), THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                                        }
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(INTROPUNITIVENESS);
                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                        DrawDiryWaterWells1(segs[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
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
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = HELIOCENTRICISM)
                                                {
                                                    DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, label } }, cb: br => { ByLayer(br); });
                                                }
                                                var p1 = segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSDOMESTIC);
                                                var p2 = p1.OffsetY(-DOCTRINARIANISM);
                                                var p3 = p2.OffsetX(QUOTATIONWITTIG);
                                                var layer = THESAURUSSTRIPED;
                                                DrawLine(layer, new GLineSegment(p1, p2));
                                                DrawLine(layer, new GLineSegment(p3, p2));
                                                DrawStoreyHeightSymbol(p3, THESAURUSSTRIPED, gpItem.OutletWrappingPipeRadius);
                                                {
                                                    var _shadow = THESAURUSDEPLORE;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > THESAURUSHOUSING)
                                                    {
                                                        Dr.DrawSimpleLabel(p3, THESAURUSFEATURE + _shadow.Substring(THESAURUSHOUSING));
                                                    }
                                                }
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[INTROPUNITIVENESS].StartPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                                        DrawNoteText(output.DN2, segs[THESAURUSPERMUTATION].EndPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                                        DrawCleaningPort(segs[THESAURUSCOMMUNICATION].EndPoint.ToPoint3d(), THESAURUSOBSTINACY, THESAURUSPERMUTATION);
                                    }
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1 = gpItem.HasWrappingPipe;
                                    output.DN2 = IRRESPONSIBLENESS;
                                    DrawOutlets3(THESAURUSDEPLORE, info.EndPoint);
                                }
                                else if (gpItem.FloorDrainsCountAt1F > THESAURUSSTAMPEDE)
                                {
                                    for (int k = THESAURUSSTAMPEDE; k < gpItem.FloorDrainsCountAt1F; k++)
                                    {
                                        var p = info.EndPoint + new Vector2d(THESAURUSDISAGREEABLE + k * INCONSIDERABILIS, -THESAURUSINTRENCH);
                                        DrawFloorDrain(p.ToPoint3d(), INTRAVASCULARLY, QUOTATIONBARBADOS);
                                        var v = new Vector2d(THESAURUSCAVERN, -THESAURUSINTRACTABLE);
                                        Get2FloorDrainDN(out string v1, out string v2);
                                        if (k == THESAURUSSTAMPEDE)
                                        {
                                            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSLINGER + THESAURUSBISEXUAL, -METALINGUISTICS), new Vector2d(ORTHOPAEDICALLY - THESAURUSBISEXUAL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                                            var segs = vecs.ToGLineSegments(p + v).Skip(THESAURUSHOUSING).ToList();
                                            var v3 = gpItem.FloorDrainsCountAt1F == THESAURUSHOUSING ? v1 : v2;
                                            var p1 = segs[THESAURUSSTAMPEDE].EndPoint;
                                            DrawNoteText(v3, p1.OffsetXY(-POLYOXYMETHYLENE - THESAURUSREPRODUCTION, -THESAURUSDOMESTIC).OffsetY(-THESAURUSEQUATION));
                                            segs = new List<Vector2d> { new Vector2d(-THESAURUSCAVERN, -THESAURUSECLECTIC), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSINGLORIOUS, THESAURUSSTAMPEDE) }.ToGLineSegments(p.OffsetY(-THESAURUSEQUATION)).Skip(THESAURUSHOUSING).ToList();
                                            drawDomePipes(segs, THESAURUSDEPLORE);
                                            var p3 = segs.First().StartPoint;
                                            drawDomePipe(new GLineSegment(p3, p3.OffsetY(THESAURUSEQUATION)));
                                        }
                                        else
                                        {
                                            var p2 = p + v;
                                            var vecs = new List<Vector2d> { new Vector2d(-QUOTATIONMASTOID, -METALINGUISTICS), new Vector2d(THESAURUSISOLATION, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                                            var segs = vecs.ToGLineSegments(p2).Skip(THESAURUSHOUSING).ToList();
                                            var p1 = segs[THESAURUSSTAMPEDE].StartPoint;
                                            DrawNoteText(v1, p1.OffsetXY(HYPERDISYLLABLE, -THESAURUSDOMESTIC).OffsetY(-THESAURUSEQUATION));
                                            segs = new List<Vector2d> { new Vector2d(-THESAURUSCAVERN, -THESAURUSECLECTIC), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSISOLATION, THESAURUSSTAMPEDE) }.ToGLineSegments(p.OffsetY(-THESAURUSEQUATION)).Skip(THESAURUSHOUSING).ToList();
                                            drawDomePipes(segs, THESAURUSDEPLORE);
                                            var p3 = segs.First().StartPoint;
                                            drawDomePipe(new GLineSegment(p3, p3.OffsetY(THESAURUSEQUATION)));
                                        }
                                    }
                                    DrawOutlets1(THESAURUSDEPLORE, info.EndPoint, THESAURUSEXECRABLE, output, fixv: new Vector2d(THESAURUSSTAMPEDE, -THESAURUSSURPRISED));
                                }
                                else if (gpItem.HasBasinInKitchenAt1F)
                                {
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1;
                                    output.DN1 = getBasinDN();
                                    output.DN2 = IRRESPONSIBLENESS;
                                    void DrawOutlets4(string shadow, Point2d basePoint, double HEIGHT)
                                    {
                                        var v = PERIODONTOCLASIA;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                        {
                                            Dr.DrawSimpleLabel(basePoint.OffsetY(-HYPERDISYLLABLE), THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                                        }
                                        var dx = QUOTATIONTRANSFERABLE;
                                        if (getDSCurveValue() == THESAURUSDISCIPLINARIAN && v == PERIODONTOCLASIA)
                                        {
                                            dx = THESAURUSFLIRTATIOUS;
                                        }
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL - THESAURUSDOMESTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER + dx, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(INTROPUNITIVENESS);
                                        DrawDiryWaterWells1(segs[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC), THESAURUSDEPLORE);
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
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label = HELIOCENTRICISM)
                                                {
                                                    DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.ToPoint3d(), layer: layer, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, label } }, cb: br => { ByLayer(br); });
                                                }
                                                var p10 = segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSDOMESTIC);
                                                var p20 = p10.OffsetY(-DOCTRINARIANISM);
                                                var p30 = p20.OffsetX(QUOTATIONWITTIG);
                                                var layer = THESAURUSSTRIPED;
                                                DrawLine(layer, new GLineSegment(p10, p20));
                                                DrawLine(layer, new GLineSegment(p30, p20));
                                                DrawStoreyHeightSymbol(p30, THESAURUSSTRIPED, gpItem.OutletWrappingPipeRadius);
                                                {
                                                    var _shadow = THESAURUSDEPLORE;
                                                    if (SHOWLINE && !string.IsNullOrEmpty(_shadow) && _shadow.Length > THESAURUSHOUSING)
                                                    {
                                                        Dr.DrawSimpleLabel(p30, THESAURUSFEATURE + _shadow.Substring(THESAURUSHOUSING));
                                                    }
                                                }
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[INTROPUNITIVENESS].StartPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                                        DrawNoteText(output.DN2, segs[THESAURUSPERMUTATION].EndPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                                        var p = segs[THESAURUSCOMMUNICATION].EndPoint;
                                        var fixY = THESAURUSHYPNOTIC + HEIGHT / THESAURUSCOMMUNICATION;
                                        var p1 = p.OffsetX(-THESAURUSPERVADE) + new Vector2d(-THESAURUSOFFEND + THESAURUSDOMESTIC, fixY);
                                        DrawDSCurve(p1, THESAURUSOBSTINACY, v, THESAURUSDEPLORE);
                                        var p2 = p1.OffsetY(-fixY);
                                        segs.Add(new GLineSegment(p1, p2));
                                        if (v == THESAURUSDISCIPLINARIAN)
                                        {
                                            var p5 = segs[INTROPUNITIVENESS].StartPoint;
                                            var _segs = new List<Vector2d> { new Vector2d(THESAURUSCHORUS, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSCELESTIAL), new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE) }.ToGLineSegments(p5);
                                            segs = segs.Take(INTROPUNITIVENESS).ToList();
                                            segs.AddRange(_segs);
                                        }
                                        drawDomePipes(segs, THESAURUSDEPLORE);
                                    }
                                    DrawOutlets4(THESAURUSDEPLORE, info.EndPoint, HEIGHT);
                                }
                                else
                                {
                                    DrawOutlets1(THESAURUSDEPLORE, info.EndPoint, THESAURUSEXECRABLE, output);
                                }
                            }
                        }
                    }
                    {
                        var linesKillers = new HashSet<Geometry>();
                        if (gpItem.IsFL0)
                        {
                            for (int i = gpItem.Items.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; --i)
                            {
                                if (gpItem.Items[i].Exist)
                                {
                                    var info = infos[i];
                                    DrawAiringSymbol(info.StartPoint, getCouldHavePeopleOnRoof(), INSTRUMENTALITY);
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
                            for (int i = THESAURUSSTAMPEDE; i < gpItem.Hangings.Count; i++)
                            {
                                var hanging = gpItem.Hangings[i];
                                if (allStoreys[i] == gpItem.MaxTl + THESAURUSASPIRATION)
                                {
                                    var info = infos[i];
                                    if (info != null)
                                    {
                                        foreach (var seg in info.RightSegsFirst)
                                        {
                                            lines.Remove(seg);
                                        }
                                        var k = HEIGHT / THESAURUSINCOMING;
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, (INCONSIDERABILIS + THESAURUSDOMESTIC) * k), new Vector2d(THESAURUSHYPNOTIC, (-THESAURUSHALTER) * k), new Vector2d(THESAURUSSTAMPEDE, (-THESAURUSPRIVILEGE - THESAURUSDOMESTIC) * k) };
                                        var segs = vecs.ToGLineSegments(info.EndPoint).Skip(THESAURUSHOUSING).ToList();
                                        lines.AddRange(segs);
                                        var shadow = THESAURUSDEPLORE;
                                        if (SHOWLINE && !string.IsNullOrEmpty(shadow) && shadow.Length > THESAURUSHOUSING)
                                        {
                                            Dr.DrawSimpleLabel(segs.First().StartPoint, THESAURUSFEATURE + shadow.Substring(THESAURUSHOUSING));
                                        }
                                    }
                                    break;
                                }
                            }
                            vent_lines = lines.ToList();
                        }
                    }
                    {
                        var auto_conn = INTRAVASCULARLY;
                        var layer = gpItem.Labels.Any(IsFL0) ? INSTRUMENTALITY : dome_layer;
                        if (auto_conn)
                        {
                            foreach (var g in GeoFac.GroupParallelLines(dome_lines, THESAURUSHOUSING, UNCONSEQUENTIAL))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: ELECTROLUMINESCENT));
                                line.Layer = layer;
                                ByLayer(line);
                            }
                            foreach (var g in GeoFac.GroupParallelLines(vent_lines, THESAURUSHOUSING, UNCONSEQUENTIAL))
                            {
                                var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: ELECTROLUMINESCENT));
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
        public static void DrawDrainageSystemDiagram(Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, int __dy, DrainageSystemDiagramViewModel viewModel, ExtraInfo exInfo)
        {
            exInfo.vm = viewModel;
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
                exInfo = exInfo,
            };
            o.Draw();
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof, string layer = THESAURUSCONTROVERSY)
        {
            DrawAiringSymbol(pt, canPeopleBeOnRoof ? THESAURUSPARTNER : THESAURUSINEFFECTUAL, layer);
        }
        public static void DrawAiringSymbol(Point2d pt, string name, string layer)
        {
            DrawBlockReference(blkName: MÖNCHENGLADBACH, basePt: pt.ToPoint3d(), layer: layer, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(QUINQUAGENARIAN, name);
            });
        }
        public static CommandContext commandContext;
        public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
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
            var items = pipeIds.Select(id => LabelItem.Parse(id)).Where(m => m != null).ToList();
            var rest = pipeIds.Except(items.Select(x => x.Label)).ToList();
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2).ToList());
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
            var storeys = new List<StoreyInfo>();
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
        public static (List<DrainageDrawingData>, ExtraInfo, bool) CreateDrainageDrawingData(out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            if (noWL && geoData.Labels.Any(x => IsWL(x.Text)))
            {
                MessageBox.Show(THESAURUSREBATE);
                drDatas = null;
                return (null, null, INTRAVASCULARLY);
            }
            var (_drDatas, exInfo) = _CreateDrainageDrawingData(geoData, THESAURUSOBSTINACY);
            drDatas = _drDatas;
            return (drDatas, exInfo, THESAURUSOBSTINACY);
        }
        public static (List<DrainageDrawingData>, ExtraInfo) CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
        {
            ThDrainageService.PreFixGeoData(geoData);
            return _CreateDrainageDrawingData(geoData, noDraw);
        }
        private static (List<DrainageDrawingData>, ExtraInfo) _CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
        {
            List<DrainageDrawingData> drDatas;
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            var exInfo = DrainageService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out drDatas);
            if (noDraw) Dispose();
            return (drDatas, exInfo);
        }
        public static ExtraInfo CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = INTRAVASCULARLY)
        {
            CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData);
            var m = CreateDrainageDrawingData(out drDatas, noWL, geoData);
            m.Item2.drDatas = m.Item1;
            m.Item2.OK = m.Item3;
            return m.Item2;
        }
        public static ExtraInfo CollectDrainageData(AcadDatabase adb, out List<StoreyItem> storeysItems, out List<DrainageDrawingData> drDatas, CommandContext ctx, bool noWL = INTRAVASCULARLY)
        {
            CollectDrainageGeoData(adb, out storeysItems, out DrainageGeoData geoData, ctx);
            var m = CreateDrainageDrawingData(out drDatas, noWL, geoData);
            m.Item2.drDatas = m.Item1;
            m.Item2.OK = m.Item3;
            return m.Item2;
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
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY).ToList();
            FixStoreys(storeys);
            return storeys;
        }
        public static void FixStoreys(List<StoreyInfo> storeys)
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { COSTERMONGERING, THESAURUSCONTROVERSY, THESAURUSSTRIPED, THESAURUSJUBILEE, THESAURUSDEFAULTER, CIRCUMCONVOLUTION, THUNDERSTRICKEN });
                var storeys = commandContext.StoreyContext.StoreyInfos;
                List<StoreyItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                var range = commandContext.range;
                ExtraInfo exInfo;
                if (range != null)
                {
                    exInfo = CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSOBSTINACY);
                    if (!exInfo.OK) return;
                }
                else
                {
                    exInfo = CollectDrainageData(adb, out storeysItems, out drDatas, commandContext, noWL: THESAURUSOBSTINACY);
                    if (!exInfo.OK) return;
                }
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, DrainageSystemDiagram.commandContext?.ViewModel, out List<int> allNumStoreys, out List<string> allRfStoreys);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + THESAURUSASPIRATION).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - THESAURUSHOUSING;
                var end = THESAURUSSTAMPEDE;
                var OFFSET_X = QUOTATIONLETTERS;
                var SPAN_X = BALANOPHORACEAE + QUOTATIONWITTIG + THESAURUSNAUGHT;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSINCOMING;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSINCOMING;
                var __dy = THESAURUSHYPNOTIC;
                Dispose();
                DrawDrainageSystemDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, viewModel, exInfo);
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSOBSTINACY))
            {
                List<StoreyItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                var exInfo = CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSOBSTINACY);
                if (!exInfo.OK) return;
                var vm = DrainageSystemDiagram.commandContext?.ViewModel;
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, vm, out List<int> allNumStoreys, out List<string> allRfStoreys);
                Dispose();
                DrawDrainageSystemDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys, vm, exInfo);
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
            var minS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > THESAURUSSTAMPEDE).Min();
            var maxS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > THESAURUSSTAMPEDE).Max();
            var countS = maxS - minS + THESAURUSHOUSING;
            allNumStoreys = new List<int>();
            for (int storey = minS; storey <= maxS; storey++)
            {
                allNumStoreys.Add(storey);
            }
            allRfStoreys = _storeys.Where(x => !IsNumStorey(x)).ToList();
            var allNumStoreyLabels = allNumStoreys.Select(x => x + THESAURUSASPIRATION).ToList();
            bool getCanHaveDownboard()
            {
                return vm?.Params?.CanHaveDownboard ?? THESAURUSOBSTINACY;
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
            string getWaterPortLabel(string label)
            {
                foreach (var drData in drDatas)
                {
                    if (drData.Outlets.TryGetValue(label, out string value))
                    {
                        return value;
                    }
                }
                return THESAURUSSPECIFICATION;
            }
            bool hasWaterPort(string label)
            {
                return getWaterPortLabel(label) != null;
            }
            int getMinTl(string label)
            {
                var scores = new List<int>();
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        var drData = drDatas[i];
                        var score = GetStoreyScore(s);
                        if (score < ushort.MaxValue)
                        {
                            if (drData.VerticalPipeLabels.Contains(label)) scores.Add(score);
                        }
                    }
                }
                if (scores.Count == THESAURUSSTAMPEDE) return THESAURUSSTAMPEDE;
                var ret = scores.Min() - THESAURUSHOUSING;
                if (ret <= THESAURUSSTAMPEDE) return THESAURUSHOUSING;
                return ret;
            }
            int getMaxTl(string label)
            {
                var scores = new List<int>();
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        var drData = drDatas[i];
                        var score = GetStoreyScore(s);
                        if (score < ushort.MaxValue)
                        {
                            if (drData.VerticalPipeLabels.Contains(label)) scores.Add(score);
                        }
                    }
                }
                return scores.Count == THESAURUSSTAMPEDE ? THESAURUSSTAMPEDE : scores.Max();
            }
            bool is4Tune(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool getIsShunt(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            int getSingleOutletFDCount(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return THESAURUSSTAMPEDE;
            }
            int getFDCount(string label, string storey)
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
            int getCirclesCount(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return THESAURUSSTAMPEDE;
            }
            bool isKitchen(string label, string storey)
            {
                if (IsFL0(label)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool isBalcony(string label, string storey)
            {
                if (IsFL0(label)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            bool getIsConnectedToFloorDrainForFL0(string label)
            {
                if (!IsFL0(label)) return INTRAVASCULARLY;
                bool f(string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return INTRAVASCULARLY;
                }
                return f(THESAURUSREGION) || f(THESAURUSTABLEAU);
            }
            bool getHasRainPort(string label)
            {
                if (!IsFL0(label)) return INTRAVASCULARLY;
                bool f(string storey)
                {
                    for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                    return INTRAVASCULARLY;
                }
                return f(THESAURUSREGION) || f(THESAURUSTABLEAU);
            }
            bool isToilet(string label, string storey)
            {
                if (IsFL0(label)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return INTRAVASCULARLY;
            }
            int getWashingMachineFloorDrainsCount(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
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
                return THESAURUSSTAMPEDE;
            }
            bool IsSeries(string label, string storey)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == storey)
                        {
                            var drData = drDatas[i];
                            if (drData.Shunts.Contains(label))
                            {
                                return INTRAVASCULARLY;
                            }
                        }
                    }
                }
                return THESAURUSOBSTINACY;
            }
            bool hasOutletlWrappingPipe(string label)
            {
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSREGION)
                        {
                            var drData = drDatas[i];
                            return drData.OutletWrappingPipeDict.ContainsValue(label);
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
                        if (s == THESAURUSREGION)
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
                if (!IsFL(label)) return THESAURUSSTAMPEDE;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSREGION)
                        {
                            var drData = drDatas[i];
                            drData.FloorDrains.TryGetValue(label, out int r);
                            return r;
                        }
                    }
                }
                return THESAURUSSTAMPEDE;
            }
            bool getIsMerge(string label)
            {
                if (!IsFL0(label)) return INTRAVASCULARLY;
                for (int i = THESAURUSSTAMPEDE; i < storeysItems.Count; i++)
                {
                    foreach (var s in storeysItems[i].Labels)
                    {
                        if (s == THESAURUSREGION)
                        {
                            var drData = drDatas[i];
                            if (drData.Merges.Contains(label))
                            {
                                return THESAURUSOBSTINACY;
                            }
                        }
                    }
                }
                return INTRAVASCULARLY;
            }
            bool HasKitchenWashingMachine(string label, string storey)
            {
                return INTRAVASCULARLY;
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
                    item.TlLabel = pl.Replace(THESAURUSDECLAIM, THESAURUSCONFIRM);
                    item.MinTl = getMinTl(item.TlLabel);
                    item.MaxTl = getMaxTl(item.TlLabel);
                    item.HasTL = THESAURUSOBSTINACY;
                    if (item.MinTl <= THESAURUSSTAMPEDE || item.MaxTl <= THESAURUSHOUSING || item.MinTl >= item.MaxTl)
                    {
                        item.HasTL = INTRAVASCULARLY;
                        item.MinTl = item.MaxTl = THESAURUSSTAMPEDE;
                    }
                    if (item.HasTL && item.MaxTl == maxS)
                    {
                        item.MoveTlLineUpper = THESAURUSOBSTINACY;
                    }
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
                item.OutletWrappingPipeRadius ??= THESAURUSLEGACY;
            }
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
                item.FloorDrainsCountAt1F = Math.Max(item.FloorDrainsCountAt1F, getSingleOutletFDCount(label, THESAURUSREGION));
            }
            foreach (var kv in pipeInfoDict)
            {
                var label = kv.Key;
                var item = kv.Value;
                foreach (var hanging in item.Hangings)
                {
                    if (hanging.FloorDrainsCount > THESAURUSPERMUTATION)
                    {
                        hanging.FloorDrainsCount = THESAURUSPERMUTATION;
                    }
                    if (isKitchen(label, hanging.Storey))
                    {
                        if (hanging.FloorDrainsCount == THESAURUSSTAMPEDE)
                        {
                            hanging.HasDoubleSCurve = THESAURUSOBSTINACY;
                        }
                    }
                    if (isKitchen(label, hanging.Storey))
                    {
                        hanging.RoomName = THESAURUSPEDESTRIAN;
                    }
                    else if (isBalcony(label, hanging.Storey))
                    {
                        hanging.RoomName = NATIONALIZATION;
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
                            hanging.HasDoubleSCurve = THESAURUSOBSTINACY;
                        }
                        if (hanging.Storey == THESAURUSREGION)
                        {
                            if (isKitchen(label, hanging.Storey))
                            {
                                hanging.HasDoubleSCurve = INTRAVASCULARLY;
                                item.HasBasinInKitchenAt1F = THESAURUSOBSTINACY;
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
                            hanging.FloorDrainsCount = THESAURUSSTAMPEDE;
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
                        item.IsFL0 = THESAURUSOBSTINACY;
                        item.HasRainPortForFL0 = getHasRainPort(item.Label);
                        item.IsConnectedToFloorDrainForFL0 = getIsConnectedToFloorDrainForFL0(item.Label);
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = THESAURUSHOUSING;
                            hanging.HasSCurve = INTRAVASCULARLY;
                            hanging.HasDoubleSCurve = INTRAVASCULARLY;
                            hanging.HasCleaningPort = INTRAVASCULARLY;
                            if (hanging.Storey == THESAURUSREGION)
                            {
                                hanging.FloorDrainsCount = getSingleOutletFDCount(kv.Key, THESAURUSREGION);
                            }
                        }
                        if (item.IsConnectedToFloorDrainForFL0) item.MergeFloorDrainForFL0 = getIsMerge(kv.Key);
                    }
                }
            }
            {
                foreach (var item in pipeInfoDict.Values)
                {
                    for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                    {
                        if (!item.Items[i].Exist) continue;
                        var hanging = item.Hangings[i];
                        var storey = allNumStoreyLabels[i];
                        hanging.HasCleaningPort = IsPL(item.Label) || IsDL(item.Label);
                        hanging.HasDownBoardLine = IsPL(item.Label) || IsDL(item.Label);
                        {
                            var m = item.Items.TryGet(i - THESAURUSHOUSING);
                            if ((m.Exist && m.HasLong) || storey == THESAURUSREGION)
                            {
                                hanging.HasCheckPoint = THESAURUSOBSTINACY;
                            }
                        }
                        if (hanging.HasCleaningPort)
                        {
                            hanging.HasCheckPoint = THESAURUSOBSTINACY;
                        }
                        if (hanging.HasDoubleSCurve)
                        {
                            hanging.HasCheckPoint = THESAURUSOBSTINACY;
                        }
                        if (hanging.WashingMachineFloorDrainsCount > THESAURUSSTAMPEDE)
                        {
                            hanging.HasCheckPoint = THESAURUSOBSTINACY;
                        }
                        if (GetStoreyScore(storey) == maxS)
                        {
                            hanging.HasCleaningPort = INTRAVASCULARLY;
                            hanging.HasDownBoardLine = INTRAVASCULARLY;
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
                        item.CanHaveAring = THESAURUSOBSTINACY;
                    }
                    if (testExist(label, maxS + THESAURUSASPIRATION))
                    {
                        item.CanHaveAring = THESAURUSOBSTINACY;
                    }
                    if (IsFL0(item.Label))
                    {
                        item.CanHaveAring = INTRAVASCULARLY;
                    }
                }
            }
            {
                if (allNumStoreys.Max() < THESAURUSDESTITUTE)
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        item.HasTL = INTRAVASCULARLY;
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
                        if (hanging.Storey == THESAURUSREGION)
                        {
                            if (isToilet(label, THESAURUSREGION))
                            {
                                item.IsSingleOutlet = THESAURUSOBSTINACY;
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
                    for (int i = item.Items.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE; --i)
                    {
                        if (item.Items[i].Exist)
                        {
                            item.Items[i] = default;
                            break;
                        }
                    }
                }
                for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                {
                    var hanging = item.Hangings[i];
                    if (hanging == null) continue;
                    if (hanging.Storey == maxS + THESAURUSASPIRATION)
                    {
                        if (item.Items[i].HasShort || item.Items[i].HasLong)
                        {
                            var m = item.Items[i];
                            m.HasShort = INTRAVASCULARLY;
                            m.HasLong = THESAURUSOBSTINACY;
                            m.DrawLongHLineHigher = THESAURUSOBSTINACY;
                            item.Items[i] = m;
                            hanging.HasDownBoardLine = INTRAVASCULARLY;
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
                    for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
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
                    for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                    {
                        var h1 = item.Hangings[i];
                        var h2 = item.Hangings.TryGet(i + THESAURUSHOUSING);
                        if (item.Items[i].HasLong && item.Items.TryGet(i + THESAURUSHOUSING).Exist && h2 != null)
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
                for (int i = THESAURUSSTAMPEDE; i < item.Hangings.Count; i++)
                {
                    var h1 = item.Hangings[i];
                    var h2 = item.Hangings.TryGet(i + THESAURUSHOUSING);
                    if (h2 == null) continue;
                    if (!h2.HasCleaningPort)
                    {
                        h1.HasDownBoardLine = INTRAVASCULARLY;
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
                        h.HasDownBoardLine = INTRAVASCULARLY;
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
                    HasTl = g.Key.HasTL,
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
                if (label is null) return THESAURUSSTAMPEDE;
                if (IsPL((label))) return THESAURUSHOUSING;
                if (IsFL0((label))) return THESAURUSPERMUTATION;
                if (IsFL((label))) return INTROPUNITIVENESS;
                return int.MaxValue;
            }).ThenBy(x =>
            {
                return x.Labels.FirstOrDefault();
            }).ToList();
            return pipeGroupedItems;
        }
        public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * THESAURUSDISPASSIONATE;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSSURPRISED;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-MISAPPREHENSIVE);
            var p3 = p1.OffsetX(MISAPPREHENSIVE);
            var line = DrawLineLazy(p2, p3);
            line.Layer = THESAURUSSTRIPED;
            ByLayer(line);
        }
        public static void DrawPipeButtomHeightSymbol(Point2d p, List<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: p.ToPoint3d(),
      props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, QUOTATIONSTRETTO } },
      cb: br =>
      {
          br.Layer = THESAURUSSTRIPED;
      });
        }
        public static void DrawPipeButtomHeightSymbol(double w, double h, Point2d p)
        {
            var vecs = new List<Vector2d> { new Vector2d(w, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, h) };
            var segs = vecs.ToGLineSegments(p);
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: segs.Last().EndPoint.OffsetX(PHYSIOLOGICALLY).ToPoint3d(),
      props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, QUOTATIONSTRETTO } },
      cb: br =>
      {
          br.Layer = THESAURUSSTRIPED;
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
        public static void SortStoreys(List<string> storeys)
        {
            storeys.Sort((x, y) => GetStoreyScore(x) - GetStoreyScore(y));
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
        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = THESAURUSSTRIPED;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = THESAURUSDISPASSIONATE;
                    SetTextStyleLazy(t, CONTROVERSIALLY);
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
            line.Layer = THESAURUSCONTROVERSY;
            ByLayer(line);
        }
        public static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = INSTRUMENTALITY;
                ByLayer(line);
            });
        }
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSSTRIPED;
                ByLayer(line);
            });
        }
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, THESAURUSENDANGER, pt);
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
                if (value is THESAURUSDISCIPLINARIAN)
                {
                    basePt += new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC).ToVector3d();
                }
                DrawBlockReference(UNACCEPTABILITY, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSJUBILEE;
                      br.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
                      }
                  });
            }
            else
            {
                DrawBlockReference(UNACCEPTABILITY, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSJUBILEE;
                      br.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
                      }
                  });
            }
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = ACCOMMODATINGLY)
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DrawBlockReference(PERSUADABLENESS, basePt, br =>
                {
                    br.Layer = THESAURUSJUBILEE;
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
                DrawBlockReference(PERSUADABLENESS, basePt,
               br =>
               {
                   br.Layer = THESAURUSJUBILEE;
                   ByLayer(br);
                   br.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, value);
                   }
               });
            }
        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference(CARCINOGENICITY, basePt, br =>
                {
                    br.Layer = THESAURUSJUBILEE;
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, FISSIPAROUSNESS);
                        br.ObjectId.SetDynBlockValue(THESAURUSCASCADE, (short)THESAURUSHOUSING);
                    }
                });
            }
            else
            {
                DrawBlockReference(CARCINOGENICITY, basePt,
                   br =>
                   {
                       br.Layer = THESAURUSJUBILEE;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(THESAURUSENTERPRISE, FISSIPAROUSNESS);
                           br.ObjectId.SetDynBlockValue(THESAURUSCASCADE, (short)THESAURUSHOUSING);
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
                DrawBlockReference(THESAURUSDENOUNCE, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSJUBILEE;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(THESAURUSQUAGMIRE);
                });
            }
            else
            {
                DrawBlockReference(THESAURUSDENOUNCE, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSJUBILEE;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(THESAURUSQUAGMIRE + THESAURUSCAVERN);
                });
            }
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
        public static void DrawDiryWaterWells2(Point2d pt, List<string> values)
        {
            var dx = THESAURUSSTAMPEDE;
            foreach (var value in values)
            {
                DrawDirtyWaterWell(pt.OffsetX(THESAURUSDOMESTIC) + new Vector2d(dx, THESAURUSSTAMPEDE), value);
                dx += THESAURUSDISAGREEABLE;
            }
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: THESAURUSGAUCHE, basePt: basePt.OffsetY(-THESAURUSDOMESTIC),
          props: new Dictionary<string, string>() { { THESAURUSSPECIFICATION, value } },
          cb: br =>
          {
              br.Layer = DENDROCHRONOLOGIST;
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
            DrawBlockReference(blkName: THESAURUSLANDMARK, basePt: basePt.OffsetY(-THESAURUSDOMESTIC),
            props: new Dictionary<string, string>() { { THESAURUSSPECIFICATION, value } },
            cb: br =>
            {
                br.Layer = THESAURUSJUBILEE;
                ByLayer(br);
            });
        }
        public static void DrawDiryWaterWells1(Point2d pt, List<string> values, bool isRainWaterWell = INTRAVASCULARLY)
        {
            if (values == null) return;
            if (values.Count == THESAURUSHOUSING)
            {
                var dy = -THESAURUSCOORDINATE;
                if (!isRainWaterWell)
                {
                    DrawDirtyWaterWell(pt.OffsetY(dy), values[THESAURUSSTAMPEDE]);
                }
                else
                {
                    DrawRainWaterWell(pt.OffsetY(dy), values[THESAURUSSTAMPEDE]);
                }
            }
            else if (values.Count >= THESAURUSPERMUTATION)
            {
                var pts = GetBasePoints(pt.OffsetX(-THESAURUSDISAGREEABLE), THESAURUSPERMUTATION, values.Count, THESAURUSDISAGREEABLE, THESAURUSDISAGREEABLE).ToList();
                for (int i = THESAURUSSTAMPEDE; i < values.Count; i++)
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
    }
    public class ThDrainageService
    {
        public static void PreFixGeoData(DrainageGeoData geoData)
        {
            foreach (var ct in geoData.Labels)
            {
                ct.Text = FixVerticalPipeLabel(ct.Text);
                ct.Boundary = ct.Boundary.Expand(-DISPENSABLENESS);
            }
            geoData.FixData();
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
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSACRIMONIOUS)).ToList();
            }
            {
                geoData.WashingMachines = GeoFac.GroupGeometries(geoData.WashingMachines.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < THESAURUSFORMULATE && x.Height < THESAURUSFORMULATE).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, THESAURUSACRIMONIOUS))).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(ASSOCIATIONISTS);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.WashingMachines = geoData.WashingMachines.Distinct(cmp).ToList();
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
                if (s == null) return INTRAVASCULARLY;
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
                    if (e.LayerId.IsNull) return INTRAVASCULARLY;
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
        const int distinguishDiameter = VÖLKERWANDERUNG;
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
        static bool isDrainageLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSREMNANT); 
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
            static bool isDLineLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && !layer.Contains(THESAURUSDEVIANT);
            static bool isVentLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && layer.Contains(THESAURUSDEVIANT);
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
                        if (_dxfName is DISORGANIZATION && isDLineLayer(GetEffectiveLayer(e.Layer)))
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
                if (entityLayer.ToUpper() is EXTRAORDINARINESS or THESAURUSUNSPEAKABLE)
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
                if (entityLayer.ToUpper() is THESAURUSEMBOLDEN or QUOTATIONGOLDEN)
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
                    if (dxfName is THESAURUSDURESS)
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
                if (dxfName == QUOTATIONSWALLOW && entityLayer is THESAURUSINVOICE)
                {
                    var r = entity.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, r, rainPortSymbols);
                }
            }
            {
                if (entity is Circle c && isDrainageLayer(entityLayer))
                {
                    if (distinguishDiameter < c.Radius && c.Radius <= HYPERDISYLLABLE)
                    {
                        var bd = c.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (THESAURUSCOMMUNICATION < c.Radius && c.Radius <= distinguishDiameter && GetEffectiveLayer(c.Layer) is THESAURUSJUBILEE or QUOTATIONBENJAMIN)
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
                    if (distinguishDiameter < c.Radius && c.Radius <= HYPERDISYLLABLE)
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
            if (entityLayer is INSTRUMENTALITY)
            {
                if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                if (dxfName is DISORGANIZATION)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, wlines);
                    return;
                }
            }
            if (isDLineLayer(entityLayer))
            {
                if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
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
                if (dxfName is DISORGANIZATION)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, dlines);
                    return;
                }
            }
            if (isVentLayer(entityLayer))
            {
                if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, vlines);
                    return;
                }
                else if (entity is Polyline pl)
                {
                    foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                    {
                        var seg = ln.ToGLineSegment().TransformBy(matrix);
                        reg(fs, seg, vlines);
                    }
                    return;
                }
                if (dxfName is DISORGANIZATION)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, vlines);
                    return;
                }
            }
            if (dxfName is DISORGANIZATION)
            {
                if (entityLayer is THESAURUSSINCERE or THESAURUSJUBILEE)
                {
                    foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                    {
                        if (c.Radius > distinguishDiameter)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, pipes);
                        }
                        else if (THESAURUSCOMMUNICATION < c.Radius && c.Radius <= distinguishDiameter && GetEffectiveLayer(c.Layer) is THESAURUSJUBILEE or QUOTATIONBENJAMIN)
                        {
                            var bd = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, bd, downwaterPorts);
                        }
                    }
                }
            }
            {
                if (isDrainageLayer(entityLayer) && entity is Line line && line.Length > THESAURUSSTAMPEDE)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, labelLines);
                    return;
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
                    if (e is Line line && isDrainageLayer(line.Layer))
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
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                }
                return;
            }
            {
                static bool g(string t) => !t.StartsWith(THESAURUSNOTATION) && !t.ToLower().Contains(PERPENDICULARITY) && !t.ToUpper().Contains(THESAURUSIMPOSTER);
                if (entity is DBText dbt && isDrainageLayer(entityLayer) && g(dbt.TextString))
                {
                    var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                    var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                    return;
                }
            }
            if (dxfName == THESAURUSDURESS)
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
            if (dxfName == THESAURUSINHARMONIOUS)
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
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSDURESS or THESAURUSFACILITATE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible))
                    {
                        foreach (var dbt in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                        {
                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                            if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                        }
                    }
                    foreach (var seg in colle.OfType<Line>().Where(x => x.Length > THESAURUSSTAMPEDE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                    {
                        reg(fs, seg, labelLines);
                    }
                }
                return;
            }
        }
        const string XREF_LAYER = THESAURUSPREFERENCE;
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var _name = br.GetEffectiveName() ?? THESAURUSDEPLORE;
                var name = GetEffectiveBRName(_name);
                if (name is THESAURUSDENOUNCE)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var pl = new Polygon(br.Bounds.ToGRect().ToLinearRing(matrix));
                    fs.Add(new KeyValuePair<Geometry, Action>(bd.ToPolygon(), () =>
                    {
                        geoData.CleaningPorts.Add(pl);
                        geoData.CleaningPortBasePoints.Add(br.Position.TransformBy(matrix).ToPoint2d());
                    }));
                    return;
                }
                if (xstNames.Contains(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, geoData.xsts);
                    return;
                }
                if (zbqNames.Contains(name))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    reg(fs, bd, geoData.zbqs);
                    return;
                }
                if (name is THESAURUSLANDMARK)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSSPECIFICATION) ?? THESAURUSDEPLORE;
                    reg(fs, bd, () =>
                    {
                        waterPorts.Add(bd);
                        waterPortLabels.Add(lb);
                    });
                    return;
                }
                if (name.Contains(THESAURUSINTENTIONAL))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSSPECIFICATION) ?? THESAURUSDEPLORE;
                    reg(fs, bd, () =>
                    {
                        waterWells.Add(bd);
                    });
                    return;
                }
                if (!isInXref)
                {
                    if (name.Contains(THESAURUSCONFRONTATION) && _name.Count(x => x == SUPERREGENERATIVE) < THESAURUSPERMUTATION)
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
                                geoData.UpdateFloorDrainTypeDict(bd.Center, br.ObjectId.GetDynBlockValue(THESAURUSENTERPRISE) ?? THESAURUSDEPLORE);
                            });
                            DrawRectLazy(bd, THESAURUSACRIMONIOUS);
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
                        if (name is SUPERINDUCEMENT or THESAURUSURBANITY)
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSENTREPRENEUR);
                            reg(fs, bd, pipes);
                            return;
                        }
                        if (name.Contains(THESAURUSSPECIMEN))
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSHESITANCY);
                            reg(fs, bd, pipes);
                            return;
                        }
                        if (name is THESAURUSMANIKIN)
                        {
                            var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSLUMBERING);
                            reg(fs, bd, pipes);
                            return;
                        }
                    }
                    if (name is REPRESENTATIVES && GetEffectiveLayer(br.Layer) is THESAURUSCORRELATION or THESAURUSJUBILEE or THESAURUSCONTROVERSY)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSLUMBERING);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is THESAURUSUNINTERESTED && GetEffectiveLayer(br.Layer) is QUOTATIONCORNISH)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSLUMBERING);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is SUPERINDUCEMENT or THESAURUSURBANITY)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), THESAURUSENTREPRENEUR);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name.Contains(THESAURUSSPECIMEN))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
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
                {
                    var ok = INTRAVASCULARLY;
                    if (killerNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        if (!washingMachinesNames.Any(x => name.Contains(x)))
                        {
                            reg(fs, bd, pipeKillers);
                        }
                        ok = THESAURUSOBSTINACY;
                    }
                    if (basinNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, basins);
                        ok = THESAURUSOBSTINACY;
                    }
                    if (washingMachinesNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, washingMachines);
                        ok = THESAURUSOBSTINACY;
                    }
                    if (mopPoolNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, mopPools);
                        ok = THESAURUSOBSTINACY;
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
        readonly List<string> basinNames = new List<string>() { CONSUBSTANTIATUS, THESAURUSCHIVALROUS, THESAURUSGRUESOME, THESAURUSDAPPER, QUOTATIONPYRIFORM, THESAURUSEXTORTIONATE, THESAURUSDRASTIC };
        readonly List<string> mopPoolNames = new List<string>() { THESAURUSHUMOUR, };
        readonly List<string> killerNames = new List<string>() { THESAURUSDRASTIC, HYPERSENSITIZED, THESAURUSHOODLUM, PHYTOGEOGRAPHER, THESAURUSABOUND, THESAURUSALLURE, THESAURUSMACHINERY, THESAURUSESCALATE };
        readonly List<string> washingMachinesNames = new List<string>() { THESAURUSCLINICAL, THESAURUSBALEFUL };
        bool isInXref;
        static bool HandleGroupAtCurrentModelSpaceOnly = INTRAVASCULARLY;
        HashSet<string> VerticalAiringMachineNames;
        HashSet<string> HangingAiringMachineNames;
        HashSet<string> xstNames;
        HashSet<string> zbqNames;
        public void CollectEntities()
        {
            {
                var dict = ThMEPWSS.ViewModel.BlockConfigService.GetBlockNameListDict();
                dict.TryGetValue(THESAURUSMARSHY, out List<string> lstVertical);
                if (lstVertical != null) this.VerticalAiringMachineNames = new HashSet<string>(lstVertical);
                dict.TryGetValue(THESAURUSPROFANITY, out List<string> lstHanging);
                if (lstHanging != null) this.HangingAiringMachineNames = new HashSet<string>(lstHanging);
                HashSet<string> hs1 = null;
                dict.TryGetValue(QUOTATIONROBERT, out List<string> lst1);
                if (lst1 != null) hs1 = new HashSet<string>(lst1);
                HashSet<string> hs2 = null;
                dict.TryGetValue(THESAURUSDELIVER, out List<string> lst2);
                if (lst2 != null) hs2 = new HashSet<string>(lst2);
                HashSet<string> hs3 = null;
                dict.TryGetValue(CYLINDRICALNESS, out List<string> lst3);
                if (lst3 != null) hs3 = new HashSet<string>(lst3);
                hs1 ??= new HashSet<string>();
                hs2 ??= new HashSet<string>();
                hs3 ??= new HashSet<string>();
                this.xstNames = new HashSet<string>(hs1.Concat(hs2));
                this.zbqNames = new HashSet<string>(hs3);
                this.VerticalAiringMachineNames ??= new HashSet<string>();
                this.HangingAiringMachineNames ??= new HashSet<string>();
            }
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
                        if (dxfName is DISORGANIZATION && GetEffectiveLayer(entity.Layer) is THESAURUSCONTROVERSY)
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
                    if (entity.Layer is THESAURUSARCHER)
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
                        if (l.Count == THESAURUSHOUSING)
                        {
                            list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[THESAURUSSTAMPEDE]));
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
                    list.Add(new KeyValuePair<string, Geometry>(THESAURUSDEPLORE, range));
                }
            }
        }
        static bool CollectRoomDataAtCurrentModelSpaceOnly = THESAURUSOBSTINACY;
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
                if (pls.Count == THESAURUSSTAMPEDE) return INTRAVASCULARLY;
            }
            return THESAURUSOBSTINACY;
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = new HashSet<Geometry>(adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer?.ToUpper() is EXTRAORDINARINESS or THESAURUSUNSPEAKABLE)
                .SelectNotNull(ConvertToPolygon).ToList());
            var names = adb.ModelSpace.Where(x => x.Layer?.ToUpper() is THESAURUSEMBOLDEN or QUOTATIONGOLDEN).SelectNotNull(entity =>
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
                if (dxfName == THESAURUSDURESS)
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
            var f = GeoFac.CreateIntersectsSelector(ranges.ToList());
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == THESAURUSHOUSING)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[THESAURUSSTAMPEDE]));
                    }
                    else if (l.Count > THESAURUSSTAMPEDE)
                    {
                        var tmp = l.Select(x => x.Area).Where(x => x > THESAURUSSTAMPEDE).ToList();
                        if (tmp.Count > THESAURUSSTAMPEDE)
                        {
                            var min = tmp.Min();
                            foreach (var geo in l)
                            {
                                if (geo.Area == min)
                                {
                                    list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), geo));
                                    foreach (var x in l) ranges.Remove(x);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            foreach (var range in ranges.Except(list.Select(kv => kv.Value)))
            {
                list.Add(new KeyValuePair<string, Geometry>(THESAURUSDEPLORE, range));
            }
            return list;
        }
        public static ExtraInfo CreateDrawingDatas(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas, out string logString, out List<DrainageDrawingData> drDatas)
        {
            var extraInfo = new ExtraInfo() { Items = new List<ExtraInfo.Item>(), CadDatas = cadDatas, geoData = geoData, };
            var roomData = geoData.RoomData;
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = THESAURUSHOUSING;
            }
            var sb = new StringBuilder(THESAURUSEXCEPTION);
            drDatas = new List<DrainageDrawingData>();
            var _kitchens = roomData.Where(x => IsKitchen(x.Key)).Select(x => x.Value).ToList();
            var _toilets = roomData.Where(x => IsToilet(x.Key)).Select(x => x.Value).ToList();
            var _nonames = roomData.Where(x => x.Key is THESAURUSDEPLORE).Select(x => x.Value).ToList();
            var _balconies = roomData.Where(x => IsBalcony(x.Key)).Select(x => x.Value).ToList();
            var _kitchensf = F(_kitchens);
            var _toiletsf = F(_toilets);
            var _nonamesf = F(_nonames);
            var _balconiesf = F(_balconies);
            for (int si = THESAURUSSTAMPEDE; si < cadDatas.Count; si++)
            {
                var drData = new DrainageDrawingData();
                drData.Init();
                var item = cadDatas[si];
                var exItem = new ExtraInfo.Item();
                extraInfo.Items.Add(exItem);
                var storeyGeo = geoData.Storeys[si].ToPolygon();
                var kitchens = _kitchensf(storeyGeo);
                var toilets = _toiletsf(storeyGeo);
                var nonames = _nonamesf(storeyGeo);
                var balconies = _balconiesf(storeyGeo);
                var kitchensf = F(kitchens);
                var toiletsf = F(toilets);
                var nonamesf = F(nonames);
                var balconiesf = F(balconies);
                {
                    var maxDis = THESAURUSEPICUREAN;
                    var angleTolleranceDegree = THESAURUSHOUSING;
                    var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > THESAURUSSTAMPEDE).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
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
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, DINOFLAGELLATES).ToList();
                var dlinesGeosf = F(dlinesGeos);
                var washingMachinesf = F(cadDataMain.WashingMachines);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var labelsf = F(item.Labels);
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (!IsDrainageLabelText(text)) continue;
                        var lst = labellinesGeosf(label);
                        if (lst.Count == THESAURUSHOUSING)
                        {
                            var labelline = lst[THESAURUSSTAMPEDE];
                            var pipes = pipesf(GeoFac.CreateGeometry(label, labelline));
                            if (pipes.Count == THESAURUSSTAMPEDE)
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
                    var labelsf = F(item.Labels);
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        var text = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text;
                        if (!IsDrainageLabelText(text)) continue;
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
                foreach (var o in item.WashingMachines)
                {
                    DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)], THESAURUSACRIMONIOUS);
                }
                foreach (var o in item.VerticalPipes)
                {
                    DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = INTROPUNITIVENESS;
                }
                foreach (var o in item.FloorDrains)
                {
                    DrawGeometryLazy(o, ents => ents.ForEach(e => e.ColorIndex = SUPERLATIVENESS));
                }
                foreach (var o in item.WaterPorts)
                {
                    DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = THESAURUSDESTITUTE;
                    DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WashingMachines)
                {
                    var e = DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = THESAURUSHOUSING;
                }
                foreach (var o in item.DownWaterPorts)
                {
                    DrawRectLazy(geoData.DownWaterPorts[cadDataMain.DownWaterPorts.IndexOf(o)]);
                }
                {
                    var cl = Color.FromRgb(THESAURUSDELIGHT, THESAURUSCRADLE, HYPOSTASIZATION);
                    foreach (var o in item.WrappingPipes)
                    {
                        var e = DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(QUOTATIONEDIBLE, SYNTHLIBORAMPHUS, THESAURUSPRIVATE);
                    foreach (var o in item.DLines)
                    {
                        var e = DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                    }
                }
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
                                    else if (IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
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
                                foreach (var dlinesGeo in dlinesGeos)
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
                                                if (!IsTL(label))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = THESAURUSOBSTINACY;
                                                }
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
                                        if (!IsTL(label))
                                        {
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
                    foreach (var dlinesGeo in dlinesGeos)
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
                                if (waterPorts.Count == THESAURUSHOUSING)
                                {
                                    var waterPort = waterPorts[THESAURUSSTAMPEDE];
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
                                            if (wrappingpipes.Count > THESAURUSSTAMPEDE)
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
                            var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(THESAURUSDOMESTIC).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = THESAURUSACRIMONIOUS;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(SUPERLATIVENESS, INTRAVASCULARLY)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterPort = f5(pt.ToPoint3d());
                                if (waterPort != null)
                                {
                                    if (waterPort.GetCenter().GetDistanceTo(pt) <= THESAURUSDICTATORIAL)
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
                            return THESAURUSITEMIZE;
                        }).Count();
                    }
                    {
                        drData.OutletWrappingPipeDict = outletWrappingPipe;
                    }
                }
                {
                    var fdsf = F(item.FloorDrains);
                    var vps = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (IsFL0(kv.Value))
                        {
                            vps.Add(kv.Key);
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
                exItem.LabelDict = lbDict.Select(x => new Tuple<Geometry, string>(x.Key, x.Value)).ToList();
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
                        var toilet = _toilet.Buffer(THESAURUSDERELICTION);
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
                    for (double buf = HYPERDISYLLABLE; buf <= THESAURUSDERELICTION; buf += HYPERDISYLLABLE)
                    {
                        foreach (var kitchen in kitchens)
                        {
                            if (ok_rooms.Contains(kitchen)) continue;
                            var ok = INTRAVASCULARLY;
                            foreach (var toilet in toiletsf(kitchen.Buffer(buf)))
                            {
                                if (ok_rooms.Contains(toilet))
                                {
                                    ok = THESAURUSOBSTINACY;
                                    break;
                                }
                                foreach (var fl in flsf(toilet))
                                {
                                    ok = THESAURUSOBSTINACY;
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
                                var geo = GRect.Create(kv.Key, HYPERDISYLLABLE).ToPolygon();
                                geo.UserData = kv.Value;
                                return geo;
                            }).ToList();
                            var shootersf = GeoFac.CreateIntersectsSelector(shooters);
                            foreach (var fd in fds)
                            {
                                var ok = INTRAVASCULARLY;
                                foreach (var geo in shootersf(fd))
                                {
                                    var name = (string)geo.UserData;
                                    if (!string.IsNullOrWhiteSpace(name))
                                    {
                                        if (name.Contains(THESAURUSRESIGNED) || name.Contains(PHOTOAUTOTROPHIC))
                                        {
                                            ok = THESAURUSOBSTINACY;
                                            break;
                                        }
                                    }
                                }
                                if (!ok)
                                {
                                    if (washingMachinesf(fd).Any())
                                    {
                                        ok = THESAURUSOBSTINACY;
                                    }
                                }
                                if (ok)
                                {
                                    washingMachineFds.Add(fd);
                                }
                            }
                            drData.WashingMachineFloorDrains[lbDict[fl]] = washingMachineFds.Count;
                            if (fds.Count == THESAURUSPERMUTATION)
                            {
                                bool is4tune;
                                bool isShunt()
                                {
                                    is4tune = INTRAVASCULARLY;
                                    var _dlines = dlinesGeosf(fl);
                                    if (_dlines.Count == THESAURUSSTAMPEDE) return INTRAVASCULARLY;
                                    if (fds.Count == THESAURUSPERMUTATION)
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
                                                if (yyy.Count == THESAURUSHOUSING)
                                                {
                                                    var dlines = yyy[THESAURUSSTAMPEDE];
                                                    if (dlines.Intersects(fds[THESAURUSSTAMPEDE].Buffer(THESAURUSCOMMUNICATION)) && dlines.Intersects(fds[THESAURUSHOUSING].Buffer(THESAURUSCOMMUNICATION)) && dlines.Intersects(fl.Buffer(THESAURUSCOMMUNICATION)))
                                                    {
                                                        if (wrappingPipesf(dlines).Count >= THESAURUSPERMUTATION)
                                                        {
                                                            is4tune = THESAURUSOBSTINACY;
                                                        }
                                                        return INTRAVASCULARLY;
                                                    }
                                                }
                                                else if (yyy.Count == THESAURUSPERMUTATION)
                                                {
                                                    var dl1 = yyy[THESAURUSSTAMPEDE];
                                                    var dl2 = yyy[THESAURUSHOUSING];
                                                    var fd1 = fds[THESAURUSSTAMPEDE].Buffer(THESAURUSCOMMUNICATION);
                                                    var fd2 = fds[THESAURUSHOUSING].Buffer(THESAURUSCOMMUNICATION);
                                                    var vp = fl.Buffer(THESAURUSCOMMUNICATION);
                                                    var geos = new List<Geometry>() { fd1, fd2, vp };
                                                    var f = F(geos);
                                                    var l1 = f(dl1);
                                                    var l2 = f(dl2);
                                                    if (l1.Count == THESAURUSPERMUTATION && l2.Count == THESAURUSPERMUTATION && l1.Contains(vp) && l2.Contains(vp))
                                                    {
                                                        return THESAURUSOBSTINACY;
                                                    }
                                                    return INTRAVASCULARLY;
                                                }
                                            }
                                            catch
                                            {
                                                return INTRAVASCULARLY;
                                            }
                                        }
                                    }
                                    return INTRAVASCULARLY;
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
                            var supterDLineGeos = GeoFac.GroupGeometries(dlinesGeos.Concat(filtedFds.OfType<Polygon>().Select(x => x.Shell)).ToList()).Select(x => x.Count == THESAURUSHOUSING ? x[THESAURUSSTAMPEDE] : GeoFac.CreateGeometry(x)).ToList();
                            var f = F(supterDLineGeos);
                            foreach (var wp in item.WaterPorts.Select(x => x.ToGRect().Expand(HYPERDISYLLABLE).ToPolygon().Shell))
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
                            .Select(x => x.Count == THESAURUSHOUSING ? x[THESAURUSSTAMPEDE] : GeoFac.CreateGeometry(x)).ToList();
                        foreach (var g in gs)
                        {
                            var vps = xlsf(g);
                            if (vps.Count > THESAURUSSTAMPEDE)
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
                            .Select(x => x.Count == THESAURUSHOUSING ? x[THESAURUSSTAMPEDE] : GeoFac.CreateGeometry(x)).ToList();
                        foreach (var g in gs)
                        {
                            var vps = xlsf(g);
                            if (vps.Count > THESAURUSSTAMPEDE)
                            {
                                var lb = lbDict[vps.First()];
                                drData.circlesCount.TryGetValue(lb, out int v);
                                drData.circlesCount[lb] = Math.Max(v, vps.Count);
                            }
                        }
                    }
                }
                drDatas.Add(drData);
            }
            logString = sb.ToString();
            extraInfo.drDatas = drDatas;
            return extraInfo;
        }
        public static Geometry CreateXGeoRect(GRect r)
        {
            return new MultiLineString(new LineString[] {
                r.ToLinearRing(),
                new LineString(new Coordinate[] { r.LeftTop.ToNTSCoordinate(), r.RightButtom.ToNTSCoordinate() }),
                new LineString(new Coordinate[] { r.LeftButtom.ToNTSCoordinate(), r.RightTop.ToNTSCoordinate() })
            });
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = THESAURUSHYPNOTIC;
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
                    if (lst.Count > THESAURUSSTAMPEDE)
                    {
                        if (f(kitchen).Count > THESAURUSSTAMPEDE)
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
        static List<Point2d> GetEndPoints(Geometry start, List<Point2d> points, List<Geometry> dlines)
        {
            points = points.Distinct().ToList();
            var pts = points.Select(x => new GCircle(x, THESAURUSCOMMUNICATION).ToCirclePolygon(SUPERLATIVENESS)).ToList();
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
                    return GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), QUOTATIONWITTIG, THESAURUSDISINGENUOUS).Intersects(kitchensGeo);
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
                    if (fls.Count > THESAURUSSTAMPEDE)
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
                        fls = flsf(kitchen.Buffer(QUOTATIONWITTIG));
                        if (fls.Count > THESAURUSSTAMPEDE)
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
            for (int i = THESAURUSSTAMPEDE; i < FLs.Count; i++)
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
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter(), QUOTATIONWITTIG, THESAURUSDISINGENUOUS));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    if (endpoints.Count == THESAURUSSTAMPEDE) return THESAURUSOBSTINACY;
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
        public List<Geometry> CleaningPorts;
        public HashSet<Point2d> CleaningPortBasePoints;
        public List<Point2d> SideFloorDrains;
        public List<GRect> PipeKillers;
        public List<GRect> DownWaterPorts;
        public List<GRect> RainPortSymbols;
        public List<KeyValuePair<Point2d, string>> FloorDrainTypeShooter;
        public List<KeyValuePair<Point2d, string>> WrappingPipeRadius;
        public List<GLineSegment> WrappingPipeLabelLines;
        public List<CText> WrappingPipeLabels;
        public List<StoreyInfo> StoreyInfos;
        public List<GRect> zbqs;
        public List<GRect> xsts;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            RoomData ??= new List<KeyValuePair<string, Geometry>>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            zbqs ??= new List<GRect>();
            xsts ??= new List<GRect>();
            VerticalPipes ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterWells ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            WashingMachines ??= new List<GRect>();
            Basins ??= new List<GRect>();
            CleaningPorts ??= new List<Geometry>();
            CleaningPortBasePoints ??= new HashSet<Point2d>();
            SideFloorDrains ??= new List<Point2d>();
            PipeKillers ??= new List<GRect>();
            MopPools ??= new List<GRect>();
            DownWaterPorts ??= new List<GRect>();
            RainPortSymbols ??= new List<GRect>();
            FloorDrainTypeShooter ??= new List<KeyValuePair<Point2d, string>>();
            WrappingPipeRadius ??= new List<KeyValuePair<Point2d, string>>();
            WrappingPipeLabelLines ??= new List<GLineSegment>();
            WrappingPipeLabels ??= new List<CText>();
            StoreyInfos ??= new List<StoreyInfo>();
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
            zbqs = zbqs.Where(x => x.IsValid).Distinct().ToList();
            xsts = xsts.Where(x => x.IsValid).Distinct().ToList();
            DownWaterPorts = DownWaterPorts.Where(x => x.IsValid).Distinct().ToList();
            RainPortSymbols = RainPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            {
                var d = new Dictionary<GRect, string>();
                for (int i = THESAURUSSTAMPEDE; i < WaterPorts.Count; i++)
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
        public List<Geometry> VerticalPipes;
        public List<Geometry> WrappingPipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterPorts;
        public List<Geometry> WaterWells;
        public List<Geometry> WashingMachines;
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
            VerticalPipes ??= new List<Geometry>();
            WrappingPipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterPorts ??= new List<Geometry>();
            WaterWells ??= new List<Geometry>();
            WashingMachines ??= new List<Geometry>();
            SideFloorDrains ??= new List<Geometry>();
            PipeKillers ??= new List<Geometry>();
            Basins ??= new List<Geometry>();
            MopPools ??= new List<Geometry>();
            DownWaterPorts ??= new List<Geometry>();
            RainPortSymbols ??= new List<Geometry>();
        }
        public static DrainageCadData Create(DrainageGeoData data)
        {
            var bfSize = THESAURUSACRIMONIOUS;
            var o = new DrainageCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            if (INTRAVASCULARLY) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (INTRAVASCULARLY) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.WaterWells.AddRange(data.WaterWells.Select(ConvertWaterPortsF()));
            o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
            o.Basins.AddRange(data.Basins.Select(ConvertWashingMachinesF()));
            o.MopPools.AddRange(data.MopPools.Select(ConvertWashingMachinesF()));
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
            return x => x.ToGCircle(THESAURUSOBSTINACY).ToCirclePolygon(THESAURUSDISINGENUOUS);
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
            ret.AddRange(VerticalPipes);
            ret.AddRange(WrappingPipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterPorts);
            ret.AddRange(WaterWells);
            ret.AddRange(WashingMachines);
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
            if (this.Storeys.Count == THESAURUSSTAMPEDE) return lst;
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
                o.VerticalPipes.AddRange(objs.Where(x => this.VerticalPipes.Contains(x)));
                o.WrappingPipes.AddRange(objs.Where(x => this.WrappingPipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterPorts.AddRange(objs.Where(x => this.WaterPorts.Contains(x)));
                o.WaterWells.AddRange(objs.Where(x => this.WaterWells.Contains(x)));
                o.WashingMachines.AddRange(objs.Where(x => this.WashingMachines.Contains(x)));
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
            if (pl.NumberOfVertices <= THESAURUSPERMUTATION)
                return null;
            var list = new List<Point2d>();
            for (int i = THESAURUSSTAMPEDE; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                if (list.Count == THESAURUSSTAMPEDE || !Equals(pt, list.Last()))
                {
                    list.Add(pt);
                }
            }
            if (list.Count <= THESAURUSPERMUTATION) return null;
            try
            {
                var tmp = list.Select(x => x.ToNTSCoordinate()).ToList(list.Count + THESAURUSHOUSING);
                if (!tmp[THESAURUSSTAMPEDE].Equals(tmp[tmp.Count - THESAURUSHOUSING]))
                {
                    tmp.Add(tmp[THESAURUSSTAMPEDE]);
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
            var t = Regex.Replace(text, UREDINIOMYCETES, THESAURUSDEPLORE, RegexOptions.IgnoreCase);
            t = Regex.Replace(t, QUOTATION3BABOVE, THESAURUSDEPLORE);
            t = Regex.Replace(t, THESAURUSMISTRUST, THESAURUSSPECIFICATION);
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
        public static StoreyInfo GetStoreyInfo(BlockReference br)
        {
            var props = br.DynamicBlockReferencePropertyCollection;
            return new StoreyInfo()
            {
                StoreyType = GetStoreyType((string)props.GetValue(ADSIGNIFICATION)),
                Numbers = ParseFloorNums(GetStoreyNumberString(br)),
                ContraPoint = GetContraPoint(br),
                Boundary = br.Bounds.ToGRect(),
            };
        }
        public static string GetStoreyNumberString(BlockReference br)
        {
            var d = br.ObjectId.GetAttributesInBlockReference(THESAURUSOBSTINACY);
            d.TryGetValue(THESAURUSFLAGRANT, out string ret);
            return ret;
        }
        public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() is THESAURUSSTICKY && x.IsDynamicBlock).ToList();
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
        public static string FixVerticalPipeLabel(string label)
        {
            if (label == null) return null;
            if (label.StartsWith(THESAURUSSTUTTER))
            {
                return label.Substring(INTROPUNITIVENESS);
            }
            if (label.StartsWith(JUNGERMANNIALES))
            {
                return label.Substring(THESAURUSPERMUTATION);
            }
            return label;
        }
        public static bool IsNotedLabel(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return label.Contains(THESAURUSACQUISITIVE) || label.Contains(QUOTATIONCHILLI);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label);
        }
        public static bool IsY1L(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return label.StartsWith(CHRISTIANIZATION);
        }
        public static bool IsY2L(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return label.StartsWith(UNPREMEDITATEDNESS);
        }
        public static bool IsNL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return label.StartsWith(THESAURUSFINICKY);
        }
        public static bool IsYL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return label.StartsWith(THESAURUSUNBEATABLE);
        }
        public static bool IsRainLabel(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label);
        }
        public static bool IsDrainageLabelText(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            if (IsFL0(label)) return INTRAVASCULARLY;
            static bool f(string label)
            {
                return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
            }
            return f(FixVerticalPipeLabel(label));
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
        public const string QUOTATIONSTANLEY = "W-BUSH-NOTE";
        public const string THESAURUSDEFAULTER = "W-BUSH";
        public const string THESAURUSINVOICE = "W-RAIN-DIMS";
        public const int HYPERDISYLLABLE = 100;
        public const string DENDROCHRONOLOGIST = "W-RAIN-EQPM";
        public const string THESAURUSWINDFALL = "TCH_VPIPEDIM";
        public const string THESAURUSSPECIFICATION = "-";
        public const string THESAURUSDURESS = "TCH_TEXT";
        public const string QUOTATIONSWALLOW = "TCH_EQUIPMENT";
        public const string THESAURUSFACILITATE = "TCH_MTEXT";
        public const string THESAURUSINHARMONIOUS = "TCH_MULTILEADER";
        public const string THESAURUSDEPLORE = "";
        public const int THESAURUSENTREPRENEUR = 50;
        public const char THESAURUSCONTEND = '|';
        public const string MULTIPROCESSING = "|";
        public const char SUPERREGENERATIVE = '$';
        public const string THESAURUSCOURIER = "$";
        public const string THESAURUSENTERPRISE = "可见性";
        public const string THESAURUSTHOROUGHBRED = "套管";
        public const int POLYOXYMETHYLENE = 1000;
        public const string SUPERINDUCEMENT = "带定位立管";
        public const string THESAURUSURBANITY = "立管编号";
        public const string THESAURUSSPECIMEN = "$LIGUAN";
        public const string THESAURUSMANIKIN = "A$C6BDE4816";
        public const string THESAURUSLANDMARK = "污废合流井编号";
        public const string THESAURUSCONTROVERSY = "W-DRAI-DOME-PIPE";
        public const string CIRCUMCONVOLUTION = "W-RAIN-NOTE";
        public const char CHROMATOGRAPHER = 'B';
        public const string THESAURUSARGUMENTATIVE = "RF";
        public const string ANTHROPOMORPHICALLY = "RF+1";
        public const string THESAURUSSCUFFLE = "RF+2";
        public const string THESAURUSASPIRATION = "F";
        public const int THESAURUSDOMESTIC = 400;
        public const string THESAURUSREGION = "1F";
        public const string THESAURUSTABLEAU = "-1F";
        public const string THESAURUSLEGACY = "-0.XX";
        public const string COSTERMONGERING = "W-NOTE";
        public const string THESAURUSJUBILEE = "W-DRAI-EQPM";
        public const string THESAURUSSTRIPED = "W-DRAI-NOTE";
        public const double QUOTATIONLETTERS = 2500.0;
        public const double BALANOPHORACEAE = 5500.0;
        public const double THESAURUSINCOMING = 1800.0;
        public const string THESAURUSPARTNER = "伸顶通气2000";
        public const string THESAURUSINEFFECTUAL = "伸顶通气500";
        public const string QUINQUAGENARIAN = "可见性1";
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
        public const int THESAURUSATTACHMENT = 750;
        public const string THESAURUSFEATURE = "*";
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
        public const int THESAURUSSURPRISED = 150;
        public const int DOCTRINARIANISM = 1200;
        public const int THESAURUSINHERIT = 2000;
        public const int THESAURUSDERELICTION = 600;
        public const int ACANTHORHYNCHUS = 479;
        public const int THESAURUSLOITER = 950;
        public const int MISAPPREHENSIVE = 200;
        public const int OTHERWORLDLINESS = 499;
        public const int THESAURUSDIFFICULTY = 360;
        public const int CONSCRIPTIONIST = 650;
        public const int VÖLKERWANDERUNG = 30;
        public const string INTERNALIZATION = "≥600";
        public const int THESAURUSGETAWAY = 450;
        public const int ACANTHOCEPHALANS = 18;
        public const int PHYSIOLOGICALLY = 250;
        public const double THESAURUSDISPASSIONATE = .7;
        public const string CONTROVERSIALLY = "TH-STYLE3";
        public const string THESAURUSCAVALIER = ";";
        public const double UNCONSEQUENTIAL = .01;
        public const string PERSUADABLENESS = "地漏系统";
        public const int PROKELEUSMATIKOS = 745;
        public const string THESAURUSTENACIOUS = "乙字弯";
        public const string THESAURUSAGILITY = "立管检查口";
        public const string THESAURUSSTRINGENT = "套管系统";
        public const string THESAURUSGAUCHE = "重力流雨水井编号";
        public const string HELIOCENTRICISM = "666";
        public const string THESAURUSJAILER = @"^([^\-]*)\-([A-Za-z])$";
        public const string DEMATERIALISING = ",";
        public const string THESAURUSEXCREMENT = "~";
        public const string THESAURUSCAPRICIOUS = @"^([^\-]*\-[A-Za-z])(\d+)$";
        public const string UNIMPRESSIONABLE = @"^([^\-]*\-)([A-Za-z])(\d+)$";
        public const int THESAURUSDESTITUTE = 7;
        public const int SYNTHLIBORAMPHUS = 229;
        public const int THESAURUSPRIVATE = 230;
        public const int THESAURUSEXCEPTION = 8192;
        public const int THESAURUSEPICUREAN = 8000;
        public const int DINOFLAGELLATES = 15;
        public const int THESAURUSHESITANCY = 60;
        public const string THESAURUSJOBBER = "FromImagination";
        public const int THESAURUSLUMBERING = 55;
        public const string THESAURUSCROUCH = "X.XX";
        public const string THESAURUSHAUGHTY = "排出：";
        public const int THESAURUSITEMIZE = 666;
        public const string THESAURUSDECIPHER = "排出套管：";
        public const string THESAURUSREFRESH = "WaterWellWrappingPipeRadiusStringDict:";
        public const int THESAURUSEXCESS = 255;
        public const int THESAURUSDELIGHT = 0x91;
        public const int THESAURUSCRADLE = 0xc7;
        public const int HYPOSTASIZATION = 0xae;
        public const int THESAURUSDISCOLOUR = 211;
        public const string QUOTATIONDOPPLER = "DN75";
        public const string THESAURUSSTUTTER = "73-";
        public const string JUNGERMANNIALES = "1-";
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
        public const string AUTOLITHOGRAPHIC = "-0";
        public const string THESAURUSABUNDANT = @"^P\d?L";
        public const string THESAURUSOPTIONAL = @"^T\d?L";
        public const string DIASTEREOISOMER = @"^D\d?L";
        public const string TRANSLITERATIONS = @"^(F\d?L|T\d?L|P\d?L|D\d?L)(\w*)\-(\w*)([a-zA-Z]*)$";
        public const double HYDROELECTRICITY = 383875.8169;
        public const double THESAURUSMARRIAGE = 250561.9571;
        public const string SUCCESSLESSNESS = "P型存水弯";
        public const string THESAURUSLINING = "板上P弯";
        public const int THESAURUSNAUGHT = 3500;
        public const int THESAURUSCOMATOSE = 1479;
        public const int THESAURUSFORESTALL = 2379;
        public const int THESAURUSLEARNER = 1779;
        public const int INVULNERABLENESS = 579;
        public const int THESAURUSOFFEND = 279;
        public const string PERIODONTOCLASIA = "双池S弯";
        public const string THUNDERSTRICKEN = "W-DRAI-VENT-PIPE";
        public const string QUOTATIONBREWSTER = "DN50";
        public const string THESAURUSADVERSITY = "W-DRAI-WAST-PIPE";
        public const int THESAURUSFLUTTER = 789;
        public const int THROMBOEMBOLISM = 1270;
        public const int THESAURUSJACKPOT = 1090;
        public const string THESAURUSDISCIPLINARIAN = "双池P弯";
        public const int THESAURUSPRIMARY = 171;
        public const int CONSPICUOUSNESS = 329;
        public const int QUOTATIONDENNIS = 5479;
        public const int THESAURUSBLESSING = 1079;
        public const int THESAURUSCLIMATE = 5600;
        public const int THESAURUSCOLOSSAL = 6079;
        public const int THESAURUSSCARCE = 8;
        public const int QUOTATIONAFGHAN = 1379;
        public const int INDIGESTIBLENESS = 569;
        public const int THESAURUSNECESSITY = 406;
        public const int QUOTATIONDEFLUVIUM = 404;
        public const int HYDROCOTYLACEAE = 3150;
        public const int THESAURUSMORTUARY = 12;
        public const int THESAURUSEUPHORIA = 1300;
        public const double THESAURUSRIBALD = .4;
        public const string THESAURUSMOLEST = "接阳台洗手盆排水";
        public const string THESAURUSSCOUNDREL = "DN50，余同";
        public const int THIGMOTACTICALLY = 1190;
        public const string QUOTATIONHUMPBACK = "接卫生间排水管";
        public const string DISCOMMODIOUSNESS = "DN100，余同";
        public const int THESAURUSOUTLANDISH = 490;
        public const int RETROGRESSIVELY = 170;
        public const int PROCRASTINATORY = 2830;
        public const int INCONSIDERABILIS = 900;
        public const int THESAURUSSHROUD = 330;
        public const int THESAURUSSOMETIMES = 895;
        public const int ELECTROMYOGRAPH = 285;
        public const int THESAURUSINTRENCH = 390;
        public const string ACCOMMODATINGLY = "普通地漏P弯";
        public const int THESAURUSSECLUSION = 1330;
        public const int PHOTOSYNTHETICALLY = 270;
        public const string THESAURUSPUGNACIOUS = "接厨房洗涤盆排水";
        public const int UNAPPREHENSIBLE = 156;
        public const int THESAURUSNOTABLE = 510;
        public const int THESAURUSMISSIONARY = 389;
        public const int ANTICONVULSANTS = 45;
        public const int THESAURUSPHANTOM = 669;
        public const int THESAURUSCONSIGNMENT = 590;
        public const int ACETYLSALICYLIC = 1700;
        public const int PREREGISTRATION = 520;
        public const int THESAURUSJOURNALIST = 919;
        public const int CONSTITUTIVENESS = 990;
        public const int THESAURUSALLEGIANCE = 129;
        public const int ALSOSESQUIALTERAL = 693;
        public const int THESAURUSCUSTOMARY = 1591;
        public const int APOLLINARIANISM = 511;
        public const int TRICHINELLIASIS = 289;
        public const string QUOTATIONBENJAMIN = "W-DRAI-DIMS";
        public const int THESAURUSSATIATE = 1391;
        public const int THESAURUSINFLEXIBLE = 667;
        public const int THESAURUSCAPITALISM = 1450;
        public const int QUINQUARTICULAR = 251;
        public const int THESAURUSINVADE = 660;
        public const int THESAURUSBISEXUAL = 110;
        public const int THESAURUSSPIRIT = 91;
        public const int THESAURUSCANDIDATE = 320;
        public const int ALUMINOSILICATES = 427;
        public const int THESAURUSEXHILARATION = 183;
        public const int POLIOENCEPHALITIS = 283;
        public const double THESAURUSADJUST = 250.0;
        public const int THESAURUSHALTER = 225;
        public const int THESAURUSMERITORIOUS = 1125;
        public const int THESAURUSRATION = 280;
        public const int THESAURUSNEGATE = 76;
        public const int QUOTATIONRHEUMATOID = 424;
        public const int THESAURUSSENSITIVE = 1900;
        public const string THESAURUSECHELON = "DN100乙字弯";
        public const string CONSECUTIVENESS = "1350";
        public const int THESAURUSBOMBARD = 275;
        public const int INTERNATIONALLY = 210;
        public const int THESAURUSEVIDENT = 151;
        public const int THESAURUSINDOMITABLE = 1109;
        public const int THESAURUSDISCERNIBLE = 420;
        public const int PRESBYTERIANIZE = 318;
        public const int THESAURUSDEGREE = 447;
        public const int THESAURUSSENTIMENTALITY = 43;
        public const int ULTRASONICATION = 237;
        public const string THESAURUSSYNTHETIC = "洗衣机地漏P弯";
        public const int THESAURUSMOTIONLESS = 1380;
        public const double THESAURUSFEELER = 200.0;
        public const double THESAURUSATTENDANCE = 780.0;
        public const double THESAURUSINACCURACY = 130.0;
        public const int THESAURUSEXCHANGE = 980;
        public const int UNPERISHABLENESS = 1358;
        public const int THESAURUSDENTIST = 172;
        public const int NOVAEHOLLANDIAE = 155;
        public const int THESAURUSMEDIATION = 1650;
        public const int THESAURUSFICTION = 71;
        public const int THESAURUSNEGLIGENCE = 221;
        public const int THESAURUSCREDITABLE = 29;
        public const int THESAURUSJINGLE = 1158;
        public const int ALSOMEGACEPHALOUS = 179;
        public const int THESAURUSINCARCERATE = 880;
        public const string METACOMMUNICATION = ">1500";
        public const int DEMONSTRATIONIST = 921;
        public const int THESAURUSMIRTHFUL = 2320;
        public const string QUOTATIONBARBADOS = "普通地漏S弯";
        public const int THESAURUSLINGER = 3090;
        public const int METALINGUISTICS = 371;
        public const int ORTHOPAEDICALLY = 2730;
        public const int THESAURUSREPRODUCTION = 888;
        public const int THESAURUSEQUATION = 629;
        public const int THESAURUSECLECTIC = 460;
        public const int THESAURUSINGLORIOUS = 2499;
        public const int QUOTATIONMASTOID = 1210;
        public const int THESAURUSISOLATION = 850;
        public const double THESAURUSFLIRTATIOUS = 270.0;
        public const int THESAURUSCHORUS = 2279;
        public const int THESAURUSCELESTIAL = 1239;
        public const int THESAURUSPRIVILEGE = 675;
        public const string MULTILATERALIZE = "drainage_drawing_ctx";
        public const string MÖNCHENGLADBACH = "通气帽系统-AI";
        public const string THESAURUSREBATE = "暂不支持污废分流";
        public const string THESAURUSDECLAIM = "PL";
        public const string THESAURUSCONFIRM = "TL";
        public const string QUOTATIONSTRETTO = "管底h+X.XX";
        public const string UNACCEPTABILITY = "双格洗涤盆排水-AI";
        public const string CARCINOGENICITY = "S型存水弯";
        public const string FISSIPAROUSNESS = "板上S弯";
        public const string THESAURUSCASCADE = "翻转状态";
        public const string THESAURUSDENOUNCE = "清扫口系统";
        public const int THESAURUSCOORDINATE = 21;
        public const string THESAURUSREMNANT = "-DRAI-";
        public const string THESAURUSINCENSE = "-PIPE";
        public const string THESAURUSDEVIANT = "VENT";
        public const string EXTRAORDINARINESS = "AI-空间框线";
        public const string THESAURUSUNSPEAKABLE = "AI-房间框线";
        public const string THESAURUSEMBOLDEN = "AI-空间名称";
        public const string QUOTATIONGOLDEN = "AI-房间名称";
        public const string THESAURUSSINCERE = "WP_KTN_LG";
        public const string THESAURUSNOTATION = "De";
        public const string PERPENDICULARITY = "wb";
        public const string THESAURUSIMPOSTER = "kd";
        public const string THESAURUSPREFERENCE = "C-XREF-EXT";
        public const string THESAURUSINTENTIONAL = "雨水井";
        public const string THESAURUSCONFRONTATION = "地漏平面";
        public const string REPRESENTATIVES = "A$C58B12E6E";
        public const string THESAURUSCORRELATION = "W-DRAI-PIEP-RISR";
        public const string THESAURUSUNINTERESTED = "A$C5E4A3C21";
        public const string QUOTATIONCORNISH = "PIPE-喷淋";
        public const string CONSUBSTANTIATUS = "A-Kitchen-3";
        public const string THESAURUSCHIVALROUS = "A-Kitchen-4";
        public const string THESAURUSGRUESOME = "A-Toilet-1";
        public const string THESAURUSDAPPER = "A-Toilet-2";
        public const string QUOTATIONPYRIFORM = "A-Toilet-3";
        public const string THESAURUSEXTORTIONATE = "A-Toilet-4";
        public const string THESAURUSDRASTIC = "-XiDiPen-";
        public const string THESAURUSHUMOUR = "A-Kitchen-9";
        public const string HYPERSENSITIZED = "0$座厕";
        public const string THESAURUSHOODLUM = "0$asdfghjgjhkl";
        public const string PHYTOGEOGRAPHER = "A-Toilet-";
        public const string THESAURUSABOUND = "A-Kitchen-";
        public const string THESAURUSALLURE = "|lp";
        public const string THESAURUSMACHINERY = "|lp1";
        public const string THESAURUSESCALATE = "|lp2";
        public const string THESAURUSCLINICAL = "A-Toilet-9";
        public const string THESAURUSBALEFUL = "$xiyiji";
        public const string THESAURUSARCHER = "feng_dbg_test_washing_machine";
        public const string INTERLINGUISTIC = "ShortTranslatorLabels：";
        public const string MICROCHIROPTERAN = "LongTranslatorLabels：";
        public const string THESAURUSLITERATE = "VerticalPipeLabels:";
        public const string THESAURUSCATARACT = "ToiletPls:";
        public const string THESAURUSDISCREDIT = "KitchenFls:";
        public const string THESAURUSSINCERITY = "BalconyFls:";
        public const string THESAURUSRESIGNED = "多通道";
        public const string PHOTOAUTOTROPHIC = "洗衣机";
        public const string THESAURUSTROPICAL = "FloorDrains：";
        public const string THESAURUSSUNDRY = "SingleOutletFloorDrains：";
        public const string THESAURUSVACUUM = "Shunts：";
        public const string THESAURUSSELECTIVE = "circlesCount ";
        public const string MICROELECTRONICS = "drainage_drDatas";
        public const string THESAURUSACQUISITIVE = "单排";
        public const string QUOTATIONCHILLI = "设置乙字弯";
        public const string QUOTATIONROBERT = "单盆洗手台";
        public const string THESAURUSDELIVER = "双盆洗手台";
        public const string CYLINDRICALNESS = "坐便器";
        public const int THESAURUSASSURANCE = 505;
        public const int DETERMINATENESS = 239;
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
            if (roomName == null) return INTRAVASCULARLY;
            var roomNameContains = new List<string> { NATIONALIZATION };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsCorridor(string roomName)
        {
            if (roomName == null) return INTRAVASCULARLY;
            var roomNameContains = new List<string> { THESAURUSSPECIES };
            if (string.IsNullOrEmpty(roomName))
                return INTRAVASCULARLY;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSOBSTINACY;
            return INTRAVASCULARLY;
        }
        public static bool IsWL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSANTIDOTE);
        }
        public static bool IsFL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSDISSOLVE);
        }
        public static bool IsFL0(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsFL(label) && label.Contains(AUTOLITHOGRAPHIC);
        }
        public static bool IsPL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSABUNDANT);
        }
        public static bool IsTL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return Regex.IsMatch(label, THESAURUSOPTIONAL);
        }
        public static bool IsDL(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return Regex.IsMatch(label, DIASTEREOISOMER);
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
                    if (reference.GetEffectiveName().Contains(THESAURUSCLINICAL))
                    {
                        using var adb = AcadDatabase.Use(reference.Database);
                        if (IsVisibleLayer(adb.Layers.Element(reference.Layer)))
                            return THESAURUSOBSTINACY;
                    }
                }
                return INTRAVASCULARLY;
            }
            public override bool CheckLayerValid(Entity curve)
            {
                return THESAURUSOBSTINACY;
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
    public class ExtraInfo
    {
        public bool OK;
        public class Item
        {
            public int Index;
            public List<Tuple<Geometry, string>> LabelDict;
        }
        public List<DrainageCadData> CadDatas;
        public List<Item> Items;
        public List<DrainageDrawingData> drDatas;
        public List<DrainageSystemNs.StoreyItem> storeysItems;
        public DrainageSystemNs.DrainageGeoData geoData;
        public DrainageSystemDiagramViewModel vm;
    }
}