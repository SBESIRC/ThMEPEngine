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
        static readonly Regex re = new Regex(THESAURUSADVICE);
        public static LabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new LabelItem()
            {
                Label = label,
                Prefix = m.Groups[THESAURUSACCESSION].Value,
                D1S = m.Groups[THESAURUSACCIDENT].Value,
                D2S = m.Groups[THESAURUSACCUSTOMED].Value,
                Suffix = m.Groups[THESAURUSACCUSTOM].Value,
            };
        }
    }
#pragma warning disable
    public class DrainageGroupingPipeItem : IEquatable<DrainageGroupingPipeItem>
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
        public bool CanHaveAring;
        public bool IsFL0;
        public bool Equals(DrainageGroupingPipeItem other)
        {
            return this.HasWaterPort == other.HasWaterPort
                && this.HasWrappingPipe == other.HasWrappingPipe
                && this.HasBasinInKitchenAt1F == other.HasBasinInKitchenAt1F
                && this.CanHaveAring == other.CanHaveAring
                && this.IsFL0 == other.IsFL0
                && this.MaxTl == other.MaxTl
                && this.MinTl == other.MinTl
                && this.IsSingleOutlet == other.IsSingleOutlet
                && this.FloorDrainsCountAt1F == other.FloorDrainsCountAt1F
                && this.Items.SeqEqual(other.Items)
                && this.Hangings.SeqEqual(other.Hangings);
        }
        public class Hanging : IEquatable<Hanging>
        {
            public int FloorDrainsCount;
            public bool IsSeries;
            public bool HasSCurve;
            public bool HasDoubleSCurve;
            public bool HasCleaningPort;
            public bool HasCheckPoint;
            public bool HasDownBoardLine;
            public override int GetHashCode()
            {
                return QUOTATIONSHAKES;
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
            return QUOTATIONSHAKES;
        }
    }
    public enum PipeType
    {
        FL, PL,
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
    public class DrainageGroupedPipeItem
    {
        public List<string> Labels;
        public List<string> WaterPortLabels;
        public bool HasWrappingPipe;
        public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > QUOTATIONSHAKES;
        public List<DrainageGroupingPipeItem.ValueItem> Items;
        public List<string> TlLabels;
        public int MinTl;
        public int MaxTl;
        public bool HasTl => TlLabels != null && TlLabels.Count > QUOTATIONSHAKES;
        public PipeType PipeType;
        public List<DrainageGroupingPipeItem.Hanging> Hangings;
        public bool IsSingleOutlet;
        public bool HasBasinInKitchenAt1F;
        public int FloorDrainsCountAt1F;
        public bool CanHaveAring;
        public bool IsFL0;
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
        public int LinesCount = THESAURUSACCESSION;
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
        public int HangingCount = QUOTATIONSHAKES;
        public Hanging Hanging1;
        public Hanging Hanging2;
    }
    public class ThwPipeLine
    {
        public List<string> Labels;
        public bool? IsLeftOrMiddleOrRight;
        public double AiringValue;
        public List<ThwPipeRun> PipeRuns;
        public ThwOutput Output;
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
                var s = QUOTATIONSHAKES;
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
                e.Layer = QUOTATIONABSORBENT;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = PHARMACEUTICALS;
                    SetTextStyleLazy(t, ACTINOMYCETALES);
                }
            }
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-ACHONDROPLASTIC, ACCOMMODATIONAL), new Vector2d(-THESAURUSACTION, QUOTATIONSHAKES) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[THESAURUSACCESSION].EndPoint : segs[THESAURUSACCESSION].StartPoint;
            var text = THESAURUSACTIVATE;
            var height = POLYOXYMETHYLENE;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawWashingMachineRaisingSymbol(Point2d bsPt, bool isLeftOrRight)
        {
            if (isLeftOrRight)
            {
                var v = new Vector2d(RECOMMENDATIONS, -THESAURUSADVISABLE);
                DrawBlockReference(THESAURUSADVISE, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSABSENT;
                    br.ScaleFactors = new Scale3d(THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSADVISORY);
                    }
                });
            }
            else
            {
                var v = new Vector2d(-RECOMMENDATIONS, -THESAURUSADVISABLE);
                DrawBlockReference(THESAURUSADVISE, (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = THESAURUSABSENT;
                    br.ScaleFactors = new Scale3d(-THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSADVISORY);
                    }
                });
            }
        }
        public static double LONG_TRANSLATOR_HEIGHT1 = ACCOUNTABLENESS;
        public static double CHECKPOINT_OFFSET_Y = THESAURUSACCORDANCE;
        public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreysItem> storeysItems, Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys)
        {
            var allNumStoreyLabels = allNumStoreys.Select(i => i + UNINTENTIONALLY).ToList();
            var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
            var start = allStoreys.Count - THESAURUSACCESSION;
            var end = QUOTATIONSHAKES;
            var OFFSET_X = ACCOMMODATINGLY;
            var SPAN_X = ACCOMMODATIVENESS + ACCOMMODATIONAL + THESAURUSACCOMMODATION;
            var HEIGHT = THESAURUSACCOMPANIMENT;
            var COUNT = pipeGroupItems.Count;
            var dy = HEIGHT - THESAURUSACCOMPANIMENT;
            var __dy = THESAURUSACCURATE;
            DrawDrainageSystemDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, null);
        }
        public static void DrawDrainageSystemDiagram(Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, int __dy, DrainageSystemDiagramViewModel viewModel)
        {
            static void DrawSegs(List<GLineSegment> segs) { for (int k = QUOTATIONSHAKES; k < segs.Count; k++) DrawTextLazy(k.ToString(), segs[k].StartPoint); }
            var vecs0 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCOUNT - dy) };
            var vecs1 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy) };
            var vecs8 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS - __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy + __dy) };
            var vecs11 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS + __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy - __dy) };
            var vecs2 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATE - dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
            var vecs3 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATION - dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
            var vecs9 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS - __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATION - dy + __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
            var vecs13 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, THESAURUSACCOUNT + dy), new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS + __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -THESAURUSACCUMULATION - dy - __dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE) };
            var vecs4 = vecs1.GetYAxisMirror();
            var vecs5 = vecs2.GetYAxisMirror();
            var vecs6 = vecs3.GetYAxisMirror();
            var vecs10 = vecs9.GetYAxisMirror();
            var vecs12 = vecs11.GetYAxisMirror();
            var vecs14 = vecs13.GetYAxisMirror();
            var vec7 = new Vector2d(-ACCUMULATIVENESS, ACCUMULATIVENESS);
            {
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                for (int i = QUOTATIONSHAKES; i < allStoreys.Count; i++)
                {
                    var storey = allStoreys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
                }
            }
            PipeRunLocationInfo[] getPipeRunLocationInfos(Point2d basePoint, ThwPipeLine thwPipeLine, int j)
            {
                var infos = new PipeRunLocationInfo[allStoreys.Count];
                for (int i = QUOTATIONSHAKES; i < allStoreys.Count; i++)
                {
                    infos[i] = new PipeRunLocationInfo() { Visible = THESAURUSABDOMINAL, Storey = allStoreys[i], };
                }
                {
                    var tdx = THESAURUSACCURACY;
                    for (int i = start; i >= end; i--)
                    {
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        var basePt = bsPt1.OffsetX(OFFSET_X + (j + THESAURUSACCESSION) * SPAN_X) + new Vector2d(tdx, QUOTATIONSHAKES);
                        var run = thwPipeLine.PipeRuns.TryGet(i);
                        PipeRunLocationInfo drawNormal()
                        {
                            {
                                var vecs = vecs0;
                                var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                var dx = vecs.Sum(v => v.X);
                                tdx += dx;
                                infos[i].BasePoint = basePt;
                                infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                infos[i].HangingEndPoint = infos[i].EndPoint;
                                infos[i].Vector2ds = vecs;
                                infos[i].Segs = segs;
                                infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSACCURATE, QUOTATIONSHAKES)).ToList();
                                infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER);
                            }
                            {
                                var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))));
                            }
                            {
                                var segs = infos[i].RightSegsMiddle.ToList();
                                segs[QUOTATIONSHAKES] = new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[THESAURUSACCESSION].StartPoint);
                                infos[i].RightSegsLast = segs;
                            }
                            {
                                var pt = infos[i].Segs.First().StartPoint.OffsetX(THESAURUSACCURATE);
                                var segs = new List<GLineSegment>() { new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION))) };
                                infos[i].RightSegsFirst = segs;
                                segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSACCURATE)));
                            }
                            return infos[i];
                        }
                        if (i == start)
                        {
                            drawNormal().Visible = THESAURUSABDOMEN;
                            continue;
                        }
                        if (run == null)
                        {
                            drawNormal().Visible = THESAURUSABDOMEN;
                            continue;
                        }
                        if (run.HasLongTranslator && run.HasShortTranslator)
                        {
                            if (run.IsLongTranslatorToLeftOrRight)
                            {
                                {
                                    var vecs = vecs3;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    infos[i].BasePoint = basePt;
                                    infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    infos[i].Vector2ds = vecs;
                                    infos[i].Segs = segs;
                                    infos[i].RightSegsMiddle = vecs9.ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                    infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(THESAURUSACCOUNTABLE);
                                }
                                {
                                    var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                    infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    segs.RemoveAt(THESAURUSACCUSE);
                                    segs.RemoveAt(THESAURUSACCUSTOM);
                                    segs.Add(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSTOMED].EndPoint.OffsetXY(-THESAURUSACCURATE, -THESAURUSACCURATE)));
                                    segs.Add(new GLineSegment(segs[THESAURUSACCIDENT].EndPoint, new Point2d(segs[THESAURUSACCUSE].EndPoint.X, segs[THESAURUSACCIDENT].EndPoint.Y)));
                                    segs.RemoveAt(THESAURUSACCUSE);
                                    segs.RemoveAt(THESAURUSACCUSTOMED);
                                    segs = new List<GLineSegment>() { segs[THESAURUSACCUSTOMED], new GLineSegment(segs[THESAURUSACCUSTOMED].StartPoint, segs[QUOTATIONSHAKES].StartPoint) };
                                    infos[i].RightSegsLast = segs;
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSACCURATE)));
                                    infos[i].RightSegsFirst = segs;
                                }
                            }
                            else
                            {
                                {
                                    var vecs = vecs6;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    infos[i].BasePoint = basePt;
                                    infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    infos[i].Vector2ds = vecs;
                                    infos[i].Segs = segs;
                                    infos[i].RightSegsMiddle = vecs14.ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                    infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(-THESAURUSACCOUNTABLE);
                                }
                                {
                                    var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                    infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))).Offset(ADRENOCORTICOTROPHIC, QUOTATIONSHAKES));
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    segs.RemoveAt(THESAURUSACCUSE);
                                    segs.RemoveAt(THESAURUSACCUSTOM);
                                    segs.Add(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSTOM].StartPoint));
                                    infos[i].RightSegsLast = segs;
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, infos[i].EndPoint.OffsetX(THESAURUSACCURATE)));
                                    infos[i].RightSegsFirst = segs;
                                }
                            }
                            infos[i].HangingEndPoint = infos[i].Segs[THESAURUSACCUSTOM].EndPoint;
                        }
                        else if (run.HasLongTranslator)
                        {
                            if (run.IsLongTranslatorToLeftOrRight)
                            {
                                {
                                    var vecs = vecs1;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    infos[i].BasePoint = basePt;
                                    infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    infos[i].Vector2ds = vecs;
                                    infos[i].Segs = segs;
                                    infos[i].RightSegsMiddle = vecs8.ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                    infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER);
                                }
                                {
                                    var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                    infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    segs = segs.Take(THESAURUSACCUSTOM).YieldAfter(segs.Last()).YieldAfter(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSTOMED].EndPoint.OffsetXY(-THESAURUSACCURATE, -THESAURUSACCURATE))).ToList();
                                    segs.Add(new GLineSegment(segs[THESAURUSACCIDENT].EndPoint, new Point2d(segs[THESAURUSACCUSE].EndPoint.X, segs[THESAURUSACCIDENT].EndPoint.Y)));
                                    segs.RemoveAt(THESAURUSACCUSE);
                                    segs.RemoveAt(THESAURUSACCUSTOMED);
                                    segs = new List<GLineSegment>() { segs[THESAURUSACCUSTOMED], new GLineSegment(segs[THESAURUSACCUSTOMED].StartPoint, segs[QUOTATIONSHAKES].StartPoint) };
                                    infos[i].RightSegsLast = segs;
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
                                    infos[i].RightSegsFirst = segs;
                                }
                            }
                            else
                            {
                                {
                                    var vecs = vecs4;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    infos[i].BasePoint = basePt;
                                    infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    infos[i].Vector2ds = vecs;
                                    infos[i].Segs = segs;
                                    infos[i].RightSegsMiddle = vecs12.ToGLineSegments(basePt.OffsetX(THESAURUSACCURATE)).Skip(THESAURUSACCESSION).ToList();
                                    infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER);
                                }
                                {
                                    var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                    infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))).Offset(ADRENOCORTICOTROPHIC, QUOTATIONSHAKES));
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle;
                                    infos[i].RightSegsLast = segs.Take(THESAURUSACCUSTOM).YieldAfter(new GLineSegment(segs[THESAURUSACCUSTOMED].EndPoint, segs[THESAURUSACCUSE].StartPoint)).YieldAfter(segs[THESAURUSACCUSE]).ToList();
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    var pt = segs[THESAURUSACCUSTOM].EndPoint;
                                    segs = new List<GLineSegment>() { new GLineSegment(pt, pt.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION))) };
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].EndPoint, pt.OffsetXY(-THESAURUSACCURATE, HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION))));
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
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    infos[i].BasePoint = basePt;
                                    infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    infos[i].Vector2ds = vecs;
                                    infos[i].Segs = segs;
                                    infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSACCURATE, QUOTATIONSHAKES)).ToList();
                                    infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(THESAURUSACCOUNTABLE);
                                }
                                {
                                    var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                    infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))));
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[THESAURUSACCIDENT].StartPoint), segs[THESAURUSACCIDENT] };
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    var r = new GRect(segs[THESAURUSACCIDENT].StartPoint, segs[THESAURUSACCIDENT].EndPoint);
                                    segs[THESAURUSACCIDENT] = new GLineSegment(r.LeftTop, r.RightButtom);
                                    segs.RemoveAt(QUOTATIONSHAKES);
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, r.RightButtom));
                                    infos[i].RightSegsFirst = segs;
                                }
                            }
                            else
                            {
                                {
                                    var vecs = vecs5;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(THESAURUSACCESSION).ToList();
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    infos[i].BasePoint = basePt;
                                    infos[i].EndPoint = basePt + new Vector2d(dx, QUOTATIONSHAKES);
                                    infos[i].Vector2ds = vecs;
                                    infos[i].Segs = segs;
                                    infos[i].RightSegsMiddle = segs.Select(x => x.Offset(THESAURUSACCURATE, QUOTATIONSHAKES)).ToList();
                                    infos[i].PlBasePt = infos[i].EndPoint.OffsetY(HEIGHT / QUOTATIONSPENSER).OffsetX(-THESAURUSACCOUNTABLE);
                                }
                                {
                                    var pt = infos[i].RightSegsMiddle.First().StartPoint;
                                    infos[i].RightSegsMiddle.Add(new GLineSegment(pt.OffsetY(-HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONACCUSATIVE, THESAURUSACCUSATION)), pt.OffsetXY(-THESAURUSACCURATE, -HEIGHT.ToRatioInt(THESAURUSACCUSATION - QUOTATIONSPENSER, THESAURUSACCUSATION))));
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    infos[i].RightSegsLast = new List<GLineSegment>() { new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, segs[THESAURUSACCIDENT].StartPoint), segs[THESAURUSACCIDENT] };
                                }
                                {
                                    var segs = infos[i].RightSegsMiddle.ToList();
                                    var r = new GRect(segs[THESAURUSACCIDENT].StartPoint, segs[THESAURUSACCIDENT].EndPoint);
                                    segs[THESAURUSACCIDENT] = new GLineSegment(r.LeftTop, r.RightButtom);
                                    segs.RemoveAt(QUOTATIONSHAKES);
                                    segs.Add(new GLineSegment(segs[QUOTATIONSHAKES].StartPoint, r.RightButtom));
                                    infos[i].RightSegsFirst = segs;
                                }
                            }
                            infos[i].HangingEndPoint = infos[i].Segs[QUOTATIONSHAKES].EndPoint;
                        }
                        else
                        {
                            drawNormal();
                        }
                    }
                }
                for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
                {
                    var info = infos.TryGet(i);
                    if (info != null)
                    {
                        info.StartPoint = info.BasePoint.OffsetY(HEIGHT);
                    }
                }
                return infos;
            }
            for (int j = QUOTATIONSHAKES; j < COUNT; j++)
            {
                var dome_lines = new List<GLineSegment>(THESAURUSABANDONED);
                var vent_lines = new List<GLineSegment>(THESAURUSABANDONED);
                var dome_layer = THESAURUSABSTAIN;
                var vent_layer = THESAURUSABSTENTION;
                void drawDomePipe(GLineSegment seg)
                {
                    if (seg.IsValid) dome_lines.Add(seg);
                }
                void drawDomePipes(IEnumerable<GLineSegment> segs)
                {
                    dome_lines.AddRange(segs.Where(x => x.IsValid));
                }
                var gpItem = pipeGroupItems[j];
                var thwPipeLine = new ThwPipeLine();
                thwPipeLine.Labels = gpItem.Labels.Concat(gpItem.TlLabels.Yield()).ToList();
                var runs = thwPipeLine.PipeRuns = new List<ThwPipeRun>();
                for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
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
                for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
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
                    if (floorDrainsCount > QUOTATIONSHAKES || hasSCurve)
                    {
                        var run = runs.TryGet(i - THESAURUSACCESSION);
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
                    for (int i = runs.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; i--)
                    {
                        var r = runs[i];
                        if (r == null) continue;
                        if (r.HasLongTranslator)
                        {
                            if (!flag.HasValue)
                            {
                                flag = THESAURUSABDOMINAL;
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
                        if (r?.HasShortTranslator == THESAURUSABDOMINAL)
                        {
                            r.IsShortTranslatorToLeftOrRight = THESAURUSABDOMEN;
                        }
                    }
                }
                void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                {
                    {
                    }
                    {
                        foreach (var info in arr)
                        {
                            if (info?.Storey == INTERCHANGEABLY)
                            {
                                if (gpItem.CanHaveAring)
                                {
                                    var pt = info.BasePoint;
                                    var seg = new GLineSegment(pt, pt.OffsetY(ThWSDStorey.RF_OFFSET_Y));
                                    drawDomePipe(seg);
                                    DrawAiringSymbol(seg.EndPoint, viewModel?.Params?.CouldHavePeopleOnRoof ?? THESAURUSABDOMEN);
                                }
                            }
                        }
                    }
                    int counterPipeButtomHeightSymbol = QUOTATIONSHAKES;
                    bool hasDrawedSCurveLabel = THESAURUSABDOMEN;
                    bool hasDrawedDSCurveLabel = THESAURUSABDOMEN;
                    bool hasDrawedCleaningPort = THESAURUSABDOMEN;
                    void _DrawLabel(string text1, string text2, Point2d basePt, bool leftOrRight, double height)
                    {
                        var w = DEHYDROGENATION - ADRENOCORTICOTROPHIC;
                        var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, height), new Vector2d(leftOrRight ? -w : w, QUOTATIONSHAKES) };
                        var segs = vecs.ToGLineSegments(basePt);
                        var lines = DrawLineSegmentsLazy(segs);
                        Dr.SetLabelStylesForDraiNote(lines.ToArray());
                        var p = segs.Last().EndPoint.OffsetY(INCOMPREHENSIBLE);
                        if (!string.IsNullOrEmpty(text1))
                        {
                            var t = DrawTextLazy(text1, POLYOXYMETHYLENE, p);
                            Dr.SetLabelStylesForDraiNote(t);
                        }
                        if (!string.IsNullOrEmpty(text2))
                        {
                            var t = DrawTextLazy(text2, POLYOXYMETHYLENE, p.OffsetXY(THESAURUSALLIANCE, -ACHONDROPLASTIC));
                            Dr.SetLabelStylesForDraiNote(t);
                        }
                    }
                    void _DrawHorizontalLineOnPipeRun(Point3d basePt)
                    {
                        if (gpItem.Labels.Any(x => IsFL(x)))
                        {
                            ++counterPipeButtomHeightSymbol;
                            if (counterPipeButtomHeightSymbol == THESAURUSACCIDENT)
                            {
                                var p = basePt.ToPoint2d();
                                var h = HEIGHT * PHARMACEUTICALS;
                                if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
                                {
                                    h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSACQUISITIVE;
                                }
                                p = p.OffsetY(h);
                                DrawPipeButtomHeightSymbol(HEIGHT, p);
                            }
                        }
                        DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                    }
                    void _DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                    {
                        if (!hasDrawedSCurveLabel)
                        {
                            hasDrawedSCurveLabel = THESAURUSABDOMINAL;
                            _DrawLabel(THESAURUSAMICABLE, QUOTATIONCAXTON, p1 + new Vector2d(-THESAURUSACQUITTAL, THESAURUSADVOCATE), THESAURUSABDOMINAL, THESAURUSACCOUNT);
                        }
                        DrawSCurve(vec7, p1, leftOrRight);
                    }
                    void _DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                    {
                        if (!hasDrawedDSCurveLabel && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                        {
                            hasDrawedDSCurveLabel = THESAURUSABDOMINAL;
                            _DrawLabel(THESAURUSAMMUNITION, QUOTATIONCAXTON, p1 + new Vector2d(-THESAURUSACKNOWLEDGE, AEROTHERMODYNAMICS - THESAURUSACCURATE), THESAURUSABDOMINAL, THESAURUSACCOUNT);
                        }
                        DrawDSCurve(vec7, p1, leftOrRight);
                    }
                    void _DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
                    {
                        if (gpItem.Labels.Any(x => IsPL(x)))
                        {
                            ++counterPipeButtomHeightSymbol;
                            if (counterPipeButtomHeightSymbol == THESAURUSACCIDENT)
                            {
                                var p = basePt.ToPoint2d();
                                DrawPipeButtomHeightSymbol(HEIGHT, p);
                            }
                        }
                        var p1 = basePt.ToPoint2d();
                        if (!hasDrawedCleaningPort && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                        {
                            hasDrawedCleaningPort = THESAURUSABDOMINAL;
                            _DrawLabel(THESAURUSAMNESTY, QUOTATIONAMNESTY, p1 + new Vector2d(-THESAURUSAFFABLE, THESAURUSAFFAIR), THESAURUSABDOMINAL, THESAURUSAFFECT);
                        }
                        DrawCleaningPort(basePt, leftOrRight, scale);
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
                            if (storey == ACCLIMATIZATION)
                            {
                                var basePt = info.EndPoint;
                                if (output != null)
                                {
                                    DrawOutlets1(basePt, DEHYDROGENATION, output, _DrawDomePipes: drawDomePipes);
                                }
                            }
                        }
                        bool shouldRaiseWashingMachine()
                        {
                            return viewModel?.Params?.ShouldRaiseWashingMachine ?? THESAURUSABDOMEN;
                        }
                        bool _shouldDrawRaiseWashingMachineSymbol()
                        {
                            return gpItem.Hangings[i].HasDoubleSCurve && gpItem.Hangings[i].FloorDrainsCount == THESAURUSACCESSION && shouldRaiseWashingMachine() && gpItem.Items[i].HasKitchenWashingMachine;
                        }
                        bool shouldDrawRaiseWashingMachineSymbol(Hanging hanging)
                        {
                            return hanging.HasDoubleSCurve && hanging.FloorDrainsCount == THESAURUSACCESSION && shouldRaiseWashingMachine() && gpItem.Items[i].HasKitchenWashingMachine;
                        }
                        void handleHanging(Hanging hanging, bool isLeftOrRight)
                        {
                            void _DrawFloorDrain(Point3d basePt, bool leftOrRight)
                            {
                                var p1 = basePt.ToPoint2d();
                                {
                                    if (_shouldDrawRaiseWashingMachineSymbol())
                                    {
                                        var fixVec = new Vector2d(-ACCOMMODATIONAL, QUOTATIONSHAKES);
                                        var p = p1 + new Vector2d(QUOTATIONSHAKES, THESAURUSAFFECTATION) + new Vector2d(-THESAURUSADVANTAGE, PRETENSIOUSNESS) + fixVec;
                                        fdBsPts.Add(p);
                                        var vecs = new List<Vector2d> { new Vector2d(-THESAURUSAFFECTED, QUOTATIONSHAKES), fixVec, new Vector2d(-ACCUMULATIVENESS, ACCUMULATIVENESS), new Vector2d(QUOTATIONSHAKES, THESAURUSADDICTION), new Vector2d(-THESAURUSAFFECTING, QUOTATIONSHAKES) };
                                        var segs = vecs.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(THESAURUSAFFECTION, QUOTATIONSHAKES));
                                        drawDomePipes(segs);
                                        DrainageSystemDiagram.DrawWashingMachineRaisingSymbol(segs.Last().EndPoint, THESAURUSABDOMINAL);
                                        return;
                                    }
                                }
                                {
                                    var p = p1 + new Vector2d(-THESAURUSADVANTAGE, AFFECTIONATENESS);
                                    fdBsPts.Add(p);
                                    floorDrainCbs[new GRect(basePt, basePt.OffsetXY(-ACHONDROPLASTIC, ACCOMMODATIONAL)).ToPolygon()] = new FloorDrainCbItem()
                                    {
                                        BasePt = basePt.ToPoint2D(),
                                        Name = THESAURUSAGILITY,
                                        LeftOrRight = leftOrRight,
                                    };
                                    return;
                                }
                            }
                            ++counterPipeButtomHeightSymbol;
                            if (counterPipeButtomHeightSymbol == THESAURUSACCESSION && thwPipeLine.Labels.Any(x => IsFL(x)))
                            {
                                if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                {
                                    DrawPipeButtomHeightSymbol(HEIGHT, info.StartPoint.OffsetY(-AFFECTIONATENESS - THESAURUSAFFECTIONATE));
                                }
                                else
                                {
                                    DrawPipeButtomHeightSymbol(HEIGHT, info.StartPoint.OffsetY(-AFFECTIONATENESS));
                                }
                            }
                            if (run.HasLongTranslator)
                            {
                                var beShort = hanging.FloorDrainsCount == THESAURUSACCESSION && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                var vecs = new List<Vector2d> { new Vector2d(-THESAURUSACOLYTE, THESAURUSACOLYTE), new Vector2d(QUOTATIONSHAKES, QUOTATIONCARLYLE), new Vector2d(-THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(beShort ? QUOTATIONSHAKES : -THESAURUSAFFILIATE, QUOTATIONSHAKES), new Vector2d(-THESAURUSAFFECTION, QUOTATIONSHAKES), new Vector2d(-THESAURUSADVANTAGE, QUOTATIONSHAKES), new Vector2d(-THESAURUSAFFILIATION, QUOTATIONSHAKES) };
                                if (isLeftOrRight == THESAURUSABDOMEN)
                                {
                                    vecs = vecs.GetYAxisMirror();
                                }
                                var pt = info.Segs[THESAURUSACCUSTOM].StartPoint.OffsetY(-THESAURUSAFFINITY).OffsetY(THESAURUSAFFIRM - ACCUMULATIVENESS);
                                if (isLeftOrRight == THESAURUSABDOMEN && run.IsLongTranslatorToLeftOrRight == THESAURUSABDOMINAL)
                                {
                                    var p1 = pt;
                                    var p2 = pt.OffsetX(CONSCIENTIOUSLY);
                                    drawDomePipe(new GLineSegment(p1, p2));
                                    pt = p2;
                                }
                                if (isLeftOrRight == THESAURUSABDOMINAL && run.IsLongTranslatorToLeftOrRight == THESAURUSABDOMEN)
                                {
                                    var p1 = pt;
                                    var p2 = pt.OffsetX(-CONSCIENTIOUSLY);
                                    drawDomePipe(new GLineSegment(p1, p2));
                                    pt = p2;
                                }
                                var segs = vecs.ToGLineSegments(pt);
                                {
                                    var _segs = segs.ToList();
                                    if (hanging.FloorDrainsCount == THESAURUSACCIDENT)
                                    {
                                        if (hanging.IsSeries) _segs.RemoveAt(THESAURUSACCUSE);
                                    }
                                    else if (hanging.FloorDrainsCount == THESAURUSACCESSION)
                                    {
                                        _segs = segs.Take(THESAURUSACCUSE).ToList();
                                    }
                                    else if (hanging.FloorDrainsCount == QUOTATIONSHAKES)
                                    {
                                        _segs = segs.Take(THESAURUSACCUSTOM).ToList();
                                    }
                                    if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(THESAURUSACCIDENT); }
                                    drawDomePipes(_segs);
                                }
                                if (hanging.FloorDrainsCount >= THESAURUSACCESSION)
                                {
                                    _DrawFloorDrain(segs.Last(THESAURUSACCUSTOMED).EndPoint.ToPoint3d(), isLeftOrRight);
                                }
                                if (hanging.FloorDrainsCount >= THESAURUSACCIDENT)
                                {
                                    _DrawFloorDrain(segs.Last(THESAURUSACCESSION).EndPoint.ToPoint3d(), isLeftOrRight);
                                    if (hanging.IsSeries)
                                    {
                                        DrawDomePipes(segs.Last(THESAURUSACCIDENT));
                                    }
                                }
                                if (hanging.HasSCurve)
                                {
                                    var p1 = segs.Last(THESAURUSACCUSTOMED).StartPoint;
                                    _DrawSCurve(vec7, p1, isLeftOrRight);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    var p1 = segs.Last(THESAURUSACCUSTOMED).StartPoint;
                                    _DrawDSCurve(vec7, p1, isLeftOrRight);
                                }
                            }
                            else
                            {
                                if (gpItem.IsFL0)
                                {
                                    DrawFloorDrain((info.StartPoint + new Vector2d(-THESAURUSAFFIRMATION, -AFFECTIONATENESS)).ToPoint3d(), THESAURUSABDOMINAL, THESAURUSACTUATE);
                                    var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFIRMATIVE), new Vector2d(-THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSAFFLICT, QUOTATIONSHAKES) };
                                    var segs = vecs.ToGLineSegments(info.StartPoint).Skip(THESAURUSACCESSION).ToList();
                                    drawDomePipes(segs);
                                }
                                else
                                {
                                    var beShort = hanging.FloorDrainsCount == THESAURUSACCESSION && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(beShort ? QUOTATIONSHAKES : -THESAURUSAFFILIATE, QUOTATIONSHAKES), new Vector2d(-THESAURUSAFFECTION, QUOTATIONSHAKES), new Vector2d(-THESAURUSADVANTAGE, QUOTATIONSHAKES), new Vector2d(-THESAURUSAFFILIATION, -AUTHORITARIANISM) };
                                    if (isLeftOrRight == THESAURUSABDOMEN)
                                    {
                                        vecs = vecs.GetYAxisMirror();
                                    }
                                    var startPt = info.StartPoint.OffsetY(-THESAURUSAFFLICTION);
                                    if (hanging.FloorDrainsCount == QUOTATIONSHAKES && hanging.HasDoubleSCurve)
                                    {
                                        startPt = info.EndPoint.OffsetY(-THESAURUSADJUDICATION + HEIGHT / THESAURUSACCUSE);
                                    }
                                    var segs = vecs.ToGLineSegments(startPt);
                                    {
                                        var _segs = segs.ToList();
                                        if (hanging.FloorDrainsCount == THESAURUSACCIDENT)
                                        {
                                            if (hanging.IsSeries) _segs.RemoveAt(THESAURUSACCUSTOMED);
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSACCESSION)
                                        {
                                            _segs.RemoveAt(THESAURUSACCUSTOM);
                                            _segs.RemoveAt(THESAURUSACCUSTOMED);
                                        }
                                        if (hanging.FloorDrainsCount == QUOTATIONSHAKES)
                                        {
                                            _segs = _segs.Take(THESAURUSACCIDENT).ToList();
                                        }
                                        if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(THESAURUSACCIDENT); }
                                        drawDomePipes(_segs);
                                    }
                                    if (hanging.FloorDrainsCount >= THESAURUSACCESSION)
                                    {
                                        _DrawFloorDrain(segs.Last(THESAURUSACCUSTOMED).EndPoint.ToPoint3d(), isLeftOrRight);
                                    }
                                    if (hanging.FloorDrainsCount >= THESAURUSACCIDENT)
                                    {
                                        _DrawFloorDrain(segs.Last(THESAURUSACCESSION).EndPoint.ToPoint3d(), isLeftOrRight);
                                    }
                                    if (hanging.HasSCurve)
                                    {
                                        var p1 = segs.Last(THESAURUSACCUSTOMED).StartPoint;
                                        _DrawSCurve(vec7, p1, isLeftOrRight);
                                    }
                                    if (hanging.HasDoubleSCurve)
                                    {
                                        var p1 = segs.Last(THESAURUSACCUSTOMED).StartPoint;
                                        _DrawDSCurve(vec7, p1, isLeftOrRight);
                                    }
                                }
                            }
                        }
                        void handleBranchInfo(ThwPipeRun run, PipeRunLocationInfo info)
                        {
                            var bi = run.BranchInfo;
                            if (bi.FirstLeftRun)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, THESAURUSACCIDENT));
                                var p3 = info.EndPoint.OffsetX(THESAURUSACCURATE);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCIDENT, THESAURUSACCUSTOMED));
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                            }
                            if (bi.FirstRightRun)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, QUOTATIONSPENSER));
                                var p3 = info.EndPoint.OffsetX(-THESAURUSACCURATE);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, THESAURUSACCUSTOMED));
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
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, QUOTATIONSPENSER));
                                var p3 = info.EndPoint.OffsetX(-THESAURUSACCURATE);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, THESAURUSACCUSTOMED));
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                            }
                            if (bi.BlueToRightFirst)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, QUOTATIONSPENSER));
                                var p3 = info.EndPoint.OffsetX(THESAURUSACCURATE);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(THESAURUSACCESSION, THESAURUSACCUSTOMED));
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                            }
                            if (bi.BlueToLeftLast)
                            {
                                if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        var _dy = THESAURUSACCURATE;
                                        var vs1 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS - _dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy + _dy + ACHONDROPLASTIC), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSACCURATE), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSACCESSION).ToList());
                                    }
                                    else
                                    {
                                        var _dy = THESAURUSACCURATE;
                                        var vs1 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS + _dy), new Vector2d(THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy - _dy + ACHONDROPLASTIC), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSACCURATE), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSACCESSION).ToList());
                                    }
                                }
                                else if (!run.HasLongTranslator)
                                {
                                    var vs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFLUENT), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                    info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                }
                            }
                            if (bi.BlueToRightLast)
                            {
                                if (!run.HasLongTranslator && !run.HasShortTranslator)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION));
                                    var p3 = info.EndPoint.OffsetX(THESAURUSACCURATE);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION));
                                    var p5 = p1.OffsetY(HEIGHT);
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, p2), new GLineSegment(p2, p5) };
                                }
                            }
                            if (bi.BlueToLeftMiddle)
                            {
                                if (!run.HasLongTranslator && !run.HasShortTranslator)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION));
                                    var p3 = info.EndPoint.OffsetX(-THESAURUSACCURATE);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION));
                                    var segs = info.Segs.ToList();
                                    segs.Add(new GLineSegment(p2, p4));
                                    info.DisplaySegs = segs;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        var _dy = THESAURUSACCURATE;
                                        var vs1 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS - _dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy + _dy) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSACCURATE), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSACCESSION).ToList());
                                    }
                                    else
                                    {
                                        var _dy = THESAURUSACCURATE;
                                        var vs1 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -ACCOUNTABLENESS + _dy), new Vector2d(THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(THESAURUSACCREDIT, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, -THESAURUSACCRUE), new Vector2d(QUOTATIONSHAKES, -ACCULTURATIONAL - dy - _dy) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSACCURATE), new Vector2d(-THESAURUSACCURATE, -THESAURUSAFFLUENCE) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(THESAURUSACCESSION).ToList());
                                    }
                                }
                            }
                            if (bi.BlueToRightMiddle)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(QUOTATIONACCUSATIVE, THESAURUSACCUSATION));
                                var p3 = info.EndPoint.OffsetX(THESAURUSACCURATE);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(QUOTATIONSPENSER, THESAURUSACCUSATION));
                                var segs = info.Segs.ToList();
                                segs.Add(new GLineSegment(p2, p4));
                                info.DisplaySegs = segs;
                            }
                            {
                                var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFECTATION), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-THESAURUSAFFORD, QUOTATIONSHAKES), new Vector2d(QUOTATIONSHAKES, -AFFRANCHISEMENT), new Vector2d(-THESAURUSACOLYTE, -THESAURUSACOLYTE) };
                                if (bi.HasLongTranslatorToLeft)
                                {
                                    var vs = vecs;
                                    info.DisplaySegs = vecs.ToGLineSegments(info.StartPoint);
                                    if (!bi.IsLast)
                                    {
                                        var pt = vs.Take(vs.Count - THESAURUSACCESSION).GetLastPoint(info.StartPoint);
                                        info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFRAY) }.ToGLineSegments(pt));
                                    }
                                }
                                if (bi.HasLongTranslatorToRight)
                                {
                                    var vs = vecs.GetYAxisMirror();
                                    info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                    if (!bi.IsLast)
                                    {
                                        var pt = vs.Take(vs.Count - THESAURUSACCESSION).GetLastPoint(info.StartPoint);
                                        info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFRAY) }.ToGLineSegments(pt));
                                    }
                                }
                            }
                        }
                        if (run.LeftHanging != null)
                        {
                            run.LeftHanging.IsSeries = gpItem.Hangings[i].IsSeries;
                            handleHanging(run.LeftHanging, THESAURUSABDOMINAL);
                        }
                        if (run.RightHanging != null)
                        {
                            run.RightHanging.IsSeries = gpItem.Hangings[i].IsSeries;
                            handleHanging(run.RightHanging, THESAURUSABDOMEN);
                        }
                        if (run.BranchInfo != null)
                        {
                            handleBranchInfo(run, info);
                        }
                        if (run.ShowShortTranslatorLabel)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(THESAURUSAFFRONT, THESAURUSAFFRONT), new Vector2d(-QUOTATIONAFGHAN, QUOTATIONAFGHAN), new Vector2d(-QUOTATIONMALICE, QUOTATIONSHAKES) };
                            var segs = vecs.ToGLineSegments(info.EndPoint).Skip(THESAURUSACCESSION).ToList();
                            DrawDraiNoteLines(segs);
                            DrawDraiNoteLines(segs);
                            var text = THESAURUSAFRAID;
                            var pt = segs.Last().EndPoint;
                            DrawNoteText(text, pt);
                        }
                        if (run.HasCheckPoint)
                        {
                            var h = HEIGHT / THESAURUSACCUSTOMED;
                            if (run.HasLongTranslator)
                            {
                                h = THESAURUSAFFRAY;
                            }
                            if (run.HasShortTranslator)
                            {
                                DrawCheckPoint(info.Segs.Last().StartPoint.OffsetY(h).ToPoint3d(), THESAURUSABDOMINAL);
                            }
                            else
                            {
                                DrawCheckPoint(info.EndPoint.OffsetY(h).ToPoint3d(), THESAURUSABDOMINAL);
                            }
                        }
                        if (run.HasHorizontalShortLine)
                        {
                            _DrawHorizontalLineOnPipeRun(info.BasePoint.ToPoint3d());
                        }
                        if (run.HasCleaningPort)
                        {
                            if (run.HasLongTranslator)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(-THESAURUSACOLYTE, THESAURUSACOLYTE), new Vector2d(QUOTATIONSHAKES, THESAURUSACCURATE), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(THESAURUSAFRESH, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(QUOTATIONSHAKES, QUOTATIONAFRICAN) };
                                if (run.IsLongTranslatorToLeftOrRight == THESAURUSABDOMEN)
                                {
                                    vecs = vecs.GetYAxisMirror();
                                }
                                if (run.HasShortTranslator)
                                {
                                    var segs = vecs.ToGLineSegments(info.Segs.Last(THESAURUSACCIDENT).StartPoint.OffsetY(-THESAURUSACCURATE));
                                    drawDomePipes(segs);
                                    _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSACCIDENT);
                                }
                                else
                                {
                                    var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-THESAURUSACCURATE));
                                    drawDomePipes(segs);
                                    _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSACCIDENT);
                                }
                            }
                            else
                            {
                                _DrawCleaningPort(info.StartPoint.OffsetY(-THESAURUSACCURATE).ToPoint3d(), THESAURUSABDOMINAL, THESAURUSACCIDENT);
                            }
                        }
                        if (run.HasShortTranslator)
                        {
                            DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                        }
                    }
                    var showAllFloorDrainLabel = THESAURUSABDOMEN;
                    var HasBalconyWashingMachineFloorDrain = THESAURUSABDOMEN;
                    var HasBalconyNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                    var HasKitchenWashingMachineFloorDrain = THESAURUSABDOMEN;
                    var HasKitchenNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                    for (int i = start; i >= end; i--)
                    {
                        if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                        var (ok, item) = gpItem.Items.TryGetValue(i + THESAURUSACCESSION);
                        if (!ok) continue;
                        foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                        {
                            if (item.HasBalconyWashingMachineFloorDrain)
                            {
                                item.HasBalconyWashingMachineFloorDrain = THESAURUSABDOMEN;
                                washingMachineFloorDrainShooters.Add(pt.ToNTSPoint());
                                continue;
                            }
                            if (item.HasBalconyNonWashingMachineFloorDrain)
                            {
                                item.HasBalconyNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                                continue;
                            }
                            if (item.HasKitchenWashingMachineFloorDrain)
                            {
                                item.HasKitchenWashingMachineFloorDrain = THESAURUSABDOMEN;
                                continue;
                            }
                            if (item.HasKitchenNonWashingMachineFloorDrain)
                            {
                                item.HasKitchenNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                                continue;
                            }
                        }
                    }
                    for (int i = start; i >= end; i--)
                    {
                        if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                        var (ok, item) = gpItem.Items.TryGetValue(i + THESAURUSACCESSION);
                        if (!ok) continue;
                        foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                        {
                            if (showAllFloorDrainLabel)
                            {
                                if (item.HasBalconyWashingMachineFloorDrain)
                                {
                                    item.HasBalconyWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    _DrawLabel(QUOTATIONAMNIOTIC, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSAFFECTATION);
                                    continue;
                                }
                                if (item.HasBalconyNonWashingMachineFloorDrain)
                                {
                                    item.HasBalconyNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    _DrawLabel(THESAURUSAMORPHOUS, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSACCOUNT);
                                    continue;
                                }
                                if (item.HasKitchenWashingMachineFloorDrain)
                                {
                                    item.HasKitchenWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    _DrawLabel(MISCONSTRUCTION, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSAFFECTATION);
                                    continue;
                                }
                                if (item.HasKitchenNonWashingMachineFloorDrain)
                                {
                                    item.HasKitchenNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    _DrawLabel(THESAURUSAMOUNT, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSACCOUNT);
                                    continue;
                                }
                                _DrawLabel(item.ToCadJson(), null, pt, THESAURUSABDOMINAL, THESAURUSACCOUNT);
                            }
                            else
                            {
                                if (!HasBalconyWashingMachineFloorDrain && item.HasBalconyWashingMachineFloorDrain)
                                {
                                    item.HasBalconyWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    HasBalconyWashingMachineFloorDrain = THESAURUSABDOMINAL;
                                    _DrawLabel(QUOTATIONAMNIOTIC, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSAFFECTATION);
                                    continue;
                                }
                                if (!HasBalconyNonWashingMachineFloorDrain && item.HasBalconyNonWashingMachineFloorDrain)
                                {
                                    item.HasBalconyNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    HasBalconyNonWashingMachineFloorDrain = THESAURUSABDOMINAL;
                                    _DrawLabel(THESAURUSAMORPHOUS, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSACCOUNT);
                                    continue;
                                }
                                if (!HasKitchenWashingMachineFloorDrain && item.HasKitchenWashingMachineFloorDrain)
                                {
                                    item.HasKitchenWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    HasKitchenWashingMachineFloorDrain = THESAURUSABDOMINAL;
                                    _DrawLabel(MISCONSTRUCTION, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSAFFECTATION);
                                    continue;
                                }
                                if (!HasKitchenNonWashingMachineFloorDrain && item.HasKitchenNonWashingMachineFloorDrain)
                                {
                                    item.HasKitchenNonWashingMachineFloorDrain = THESAURUSABDOMEN;
                                    HasKitchenNonWashingMachineFloorDrain = THESAURUSABDOMINAL;
                                    _DrawLabel(THESAURUSAMOUNT, THESAURUSAMOROUS, pt, THESAURUSABDOMINAL, THESAURUSACCOUNT);
                                    continue;
                                }
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
                                o.Name = AMPHIARTHRODIAL;
                            }
                            DrawFloorDrain(o.BasePt.ToPoint3d(), o.LeftOrRight, o.Name);
                        }
                    }
                }
                var infos = getPipeRunLocationInfos(basePoint, thwPipeLine, j);
                handlePipeLine(thwPipeLine, infos);
                static void drawLabel(Point2d basePt, string text1, string text2, bool isLeftOrRight)
                {
                    var vecs = new List<Vector2d> { new Vector2d(ACHONDROPLASTIC, ACHONDROPLASTIC), new Vector2d(ACRIMONIOUSNESS, QUOTATIONSHAKES) };
                    if (isLeftOrRight == THESAURUSABDOMINAL)
                    {
                        vecs = vecs.GetYAxisMirror();
                    }
                    var segs = vecs.ToGLineSegments(basePt).ToList();
                    foreach (var seg in segs)
                    {
                        var line = DrawLineSegmentLazy(seg);
                        Dr.SetLabelStylesForDraiNote(line);
                    }
                    var txtBasePt = isLeftOrRight ? segs[THESAURUSACCESSION].EndPoint : segs[THESAURUSACCESSION].StartPoint;
                    txtBasePt = txtBasePt.OffsetY(INCOMPREHENSIBLE);
                    if (text1 != null)
                    {
                        var t = DrawTextLazy(text1, POLYOXYMETHYLENE, txtBasePt);
                        Dr.SetLabelStylesForDraiNote(t);
                    }
                    if (text2 != null)
                    {
                        var t = DrawTextLazy(text2, POLYOXYMETHYLENE, txtBasePt.OffsetY(-POLYOXYMETHYLENE - INCOMPREHENSIBLE));
                        Dr.SetLabelStylesForDraiNote(t);
                    }
                }
                for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
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
                }
                {
                    var _allSmoothStoreys = new List<string>();
                    for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
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
                    var _storeys = new string[] { _allSmoothStoreys.GetAt(THESAURUSACCIDENT), _allSmoothStoreys.GetLastOrDefault(THESAURUSACCUSTOMED) }.SelectNotNull().Distinct().ToList();
                    if (_storeys.Count == QUOTATIONSHAKES)
                    {
                        _storeys = new string[] { _allSmoothStoreys.FirstOrDefault(), _allSmoothStoreys.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                    }
                    _storeys = _storeys.Where(storey =>
                    {
                        var i = allNumStoreyLabels.IndexOf(storey);
                        var info = infos.TryGet(i);
                        return info != null && info.Visible;
                    }).ToList();
                    if (_storeys.Count == QUOTATIONSHAKES)
                    {
                        _storeys = allNumStoreyLabels.Where(storey =>
                        {
                            var i = allNumStoreyLabels.IndexOf(storey);
                            var info = infos.TryGet(i);
                            return info != null && info.Visible;
                        }).Take(THESAURUSACCESSION).ToList();
                    }
                    foreach (var storey in _storeys)
                    {
                        var i = allNumStoreyLabels.IndexOf(storey);
                        var info = infos[i];
                        {
                            string label1, label2;
                            var isLeftOrRight = !thwPipeLine.Labels.Any(x => IsFL(x));
                            var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).Distinct().OrderBy(x => x).ToList();
                            if (labels.Count == THESAURUSACCIDENT)
                            {
                                label1 = labels[QUOTATIONSHAKES];
                                label2 = labels[THESAURUSACCESSION];
                            }
                            else
                            {
                                label1 = labels.JoinWith(THESAURUSACRIMONIOUS);
                                label2 = null;
                            }
                            drawLabel(info.PlBasePt, label1, label2, isLeftOrRight);
                        }
                        if (gpItem.HasTl)
                        {
                            string label1, label2;
                            var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => IsTL(x))).Distinct().OrderBy(x => x).ToList();
                            if (labels.Count == THESAURUSACCIDENT)
                            {
                                label1 = labels[QUOTATIONSHAKES];
                                label2 = labels[THESAURUSACCESSION];
                            }
                            else
                            {
                                label1 = labels.JoinWith(THESAURUSACRIMONIOUS);
                                label2 = null;
                            }
                            drawLabel(info.PlBasePt.OffsetX(THESAURUSACCURATE), label1, label2, THESAURUSABDOMEN);
                        }
                    }
                }
                {
                    var _storeys = new string[] { allNumStoreyLabels.GetAt(THESAURUSACCESSION), allNumStoreyLabels.GetLastOrDefault(THESAURUSACCIDENT) }.SelectNotNull().Distinct().ToList();
                    if (_storeys.Count == QUOTATIONSHAKES)
                    {
                        _storeys = new string[] { allNumStoreyLabels.FirstOrDefault(), allNumStoreyLabels.LastOrDefault() }.SelectNotNull().Distinct().ToList();
                    }
                }
#pragma warning disable
                for (int i = QUOTATIONSHAKES; i < allNumStoreyLabels.Count; i++)
                {
                    var info = infos.TryGet(i);
                    if (info != null && info.Visible)
                    {
                        void TestRightSegsMiddle()
                        {
                            var segs = info.RightSegsMiddle;
                            if (segs != null)
                            {
                                vent_lines.AddRange(segs);
                            }
                        }
                        void TestRightSegsLast()
                        {
                            var segs = info.RightSegsLast;
                            if (segs != null)
                            {
                                vent_lines.AddRange(segs);
                            }
                        }
                        void TestRightSegsFirst()
                        {
                            var segs = info.RightSegsFirst;
                            if (segs != null)
                            {
                                vent_lines.AddRange(segs);
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
                                        vent_lines.AddRange(segs);
                                    }
                                }
                                else if (gpItem.MinTl + UNINTENTIONALLY == storey)
                                {
                                    var segs = info.RightSegsLast;
                                    if (segs != null)
                                    {
                                        vent_lines.AddRange(segs);
                                    }
                                }
                                else if (GetStoreyScore(storey).InRange(gpItem.MinTl, gpItem.MaxTl))
                                {
                                    var segs = info.RightSegsMiddle;
                                    if (segs != null)
                                    {
                                        vent_lines.AddRange(segs);
                                    }
                                }
                            }
                        }
                        Run();
                    }
                }
                {
                    var i = allNumStoreyLabels.IndexOf(ACCLIMATIZATION);
                    if (i >= QUOTATIONSHAKES)
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
                                DN1 = ACETYLSALICYLIC,
                            };
                            if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                            {
                                var p = info.EndPoint + new Vector2d(ACHLOROPHYLLOUS, -AFFECTIONATENESS);
                                DrawOutlets1(info.EndPoint, DEHYDROGENATION, output, _DrawDomePipes: drawDomePipes, dy: -HEIGHT * PALAEONTOLOGIST, isRainWaterWell: THESAURUSABDOMINAL);
                            }
                            else if (gpItem.IsSingleOutlet)
                            {
                                output.HasWrappingPipe2 = THESAURUSABDOMINAL;
                                output.DN2 = ACETYLSALICYLIC;
                                DrawOutlets3(info.EndPoint, output, _DrawDomePipes: drawDomePipes);
                            }
                            else if (gpItem.FloorDrainsCountAt1F > QUOTATIONSHAKES)
                            {
                                var p = info.EndPoint + new Vector2d(ACHLOROPHYLLOUS, -AFFECTIONATENESS);
                                DrawFloorDrain(p.ToPoint3d(), THESAURUSABDOMINAL, THESAURUSACTUATE);
                                var vecs = new List<Vector2d>() { new Vector2d(QUOTATIONSHAKES, -INATTENTIVENESS + RECLASSIFICATION), new Vector2d(-THESAURUSACCURATE, -THESAURUSACCURATE), new Vector2d(-INATTENTIVENESS - PARTHENOGENESIS + ELECTROPHORESIS * THESAURUSACCIDENT, QUOTATIONSHAKES), new Vector2d(-THESAURUSACCURATE, THESAURUSACCURATE) };
                                var segs = vecs.ToGLineSegments(p + new Vector2d(-THESAURUSADVANTAGE, -THESAURUSACQUAINTED));
                                drawDomePipes(segs);
                                DrawOutlets1(info.EndPoint, DEHYDROGENATION, output, _DrawDomePipes: drawDomePipes, dy: -HEIGHT * PALAEONTOLOGIST);
                            }
                            else if (gpItem.HasBasinInKitchenAt1F)
                            {
                                output.HasWrappingPipe2 = THESAURUSABDOMINAL;
                                output.DN2 = ACETYLSALICYLIC;
                                DrawOutlets4(info.EndPoint, output, _DrawDomePipes: drawDomePipes);
                            }
                            else
                            {
                                DrawOutlets1(info.EndPoint, DEHYDROGENATION, output, _DrawDomePipes: drawDomePipes, dy: -HEIGHT * PALAEONTOLOGIST);
                            }
                        }
                    }
                }
                {
                    var linesKillers = new HashSet<Geometry>();
                    if (gpItem.IsFL0)
                    {
                        for (int i = gpItem.Items.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; --i)
                        {
                            if (gpItem.Items[i].Exist)
                            {
                                var info = infos[i];
                                linesKillers.Add(GRect.Create(info.StartPoint.OffsetY(-THESAURUSACCESSION), THESAURUSABANDON).ToPolygon());
                                break;
                            }
                        }
                    }
                    dome_lines = GeoFac.ToNodedLineSegments(dome_lines);
                    var geos = dome_lines.Select(x => x.ToLineString()).ToList();
                    dome_lines = geos.Except(GeoFac.CreateIntersectsSelector(geos)(GeoFac.CreateGeometryEx(linesKillers.ToList()))).Cast<LineString>().SelectMany(x => x.ToGLineSegments()).ToList();
                }
                {
                    var auto_conn = THESAURUSABDOMEN;
                    var layer = gpItem.Labels.Any(IsFL0) ? THESAURUSABSORB : dome_layer;
                    if (auto_conn)
                    {
                        foreach (var g in GeoFac.GroupParallelLines(dome_lines, THESAURUSACCESSION, THESAURUSACRIMONY))
                        {
                            var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: GOSSWEILERODENDRON));
                            line.Layer = layer;
                            ByLayer(line);
                        }
                        foreach (var g in GeoFac.GroupParallelLines(vent_lines, THESAURUSACCESSION, THESAURUSACRIMONY))
                        {
                            var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: GOSSWEILERODENDRON));
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
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof)
        {
            DrawAiringSymbol(pt, canPeopleBeOnRoof ? THESAURUSAMBUSH : THESAURUSAMENABLE);
        }
        public static void DrawAiringSymbol(Point2d pt, string name)
        {
            DrawBlockReference(blkName: THESAURUSAMENDMENT, basePt: pt.ToPoint3d(), layer: THESAURUSABSTAIN, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(THESAURUSACCOMPLISHMENT, name);
            });
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: THESAURUSACCOMPLISH, basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: THESAURUSABSTAIN, cb: br =>
            {
                br.ObjectId.SetDynBlockValue(ACCOMPLISSEMENT, offsetY);
                br.ObjectId.SetDynBlockValue(THESAURUSACCOMPLISHMENT, ACCOMPLISHMENTS);
            });
        }
        public static CommandContext commandContext;
        public static IEnumerable<string> ConvertLabelStrings(IEnumerable<string> pipeIds)
        {
            var items = pipeIds.Select(id => LabelItem.Parse(id)).Where(m => m != null);
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2).ToList());
            foreach (var g in gs)
            {
                if (g.Count == THESAURUSACCESSION)
                {
                    yield return g.First().Label;
                }
                else if (g.Count > THESAURUSACCIDENT && g.Count == g.Last().D2 - g.First().D2 + THESAURUSACCESSION)
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
                    for (int i = QUOTATIONSHAKES; i < g.Count; i++)
                    {
                        var m = g[i];
                        sb.Append($"{m.D2S}{m.Suffix}");
                        if (i != g.Count - THESAURUSACCESSION)
                        {
                            sb.Append(THESAURUSADAPTABLE);
                        }
                    }
                    yield return sb.ToString();
                }
            }
        }
        public static void DrawOutlets2(Point2d basePoint, Action<IEnumerable<GLineSegment>> _DrawDomePipes)
        {
            var output = new ThwOutput();
            output.DirtyWaterWellValues = new List<string>() { INSTRUMENTALITY, THESAURUSAGENDA, THESAURUSAGGRAVATE };
            output.HasWrappingPipe1 = THESAURUSABDOMINAL;
            output.HasWrappingPipe2 = THESAURUSABDOMINAL;
            output.HasCleaningPort1 = THESAURUSABDOMINAL;
            output.HasCleaningPort2 = THESAURUSABDOMINAL;
            output.HasLargeCleaningPort = THESAURUSABDOMINAL;
            output.DN1 = ACETYLSALICYLIC;
            output.DN2 = ACETYLSALICYLIC;
            DrawOutlets3(basePoint, output, _DrawDomePipes: _DrawDomePipes);
        }
        public static void DrawOutlets3(Point2d basePoint, ThwOutput output, Action<IEnumerable<GLineSegment>> _DrawDomePipes)
        {
            var values = output.DirtyWaterWellValues;
            var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFORD), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-EXCOMMUNICATION, QUOTATIONSHAKES), new Vector2d(QUOTATIONSHAKES, UNCHRONOLOGICAL), new Vector2d(THESAURUSAGGRAVATION, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(QUOTATIONSHAKES, THESAURUSAGGREGATE) };
            var segs = vecs.ToGLineSegments(basePoint);
            segs.RemoveAt(THESAURUSACCUSTOMED);
            _DrawDomePipes(segs);
            DrawDiryWaterWells1(segs[THESAURUSACCIDENT].EndPoint + new Vector2d(-ACHONDROPLASTIC, THESAURUSACCURATE), values);
            if (output.HasWrappingPipe1) DrawWrappingPipe(segs[THESAURUSACCUSTOMED].StartPoint.OffsetX(THESAURUSACCURATE));
            if (output.HasWrappingPipe2) DrawWrappingPipe(segs[THESAURUSACCIDENT].EndPoint.OffsetX(THESAURUSACCURATE));
            DrawNoteText(output.DN1, segs[THESAURUSACCUSTOMED].StartPoint.OffsetX(ACQUISITIVENESS));
            DrawNoteText(output.DN2, segs[THESAURUSACCIDENT].EndPoint.OffsetX(ACQUISITIVENESS));
            if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSACCUSTOM].StartPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
            if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSACCIDENT].StartPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
            DrawCleaningPort(segs[THESAURUSACCUSE].EndPoint.ToPoint3d(), THESAURUSABDOMINAL, THESAURUSACCIDENT);
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
                if (geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSABDOMINAL)
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
        public static void DrawOutlets4(Point2d basePoint, ThwOutput output, Action<IEnumerable<GLineSegment>> _DrawDomePipes)
        {
            var values = output.DirtyWaterWellValues;
            var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFORD), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-EXCOMMUNICATION - ACHONDROPLASTIC, QUOTATIONSHAKES), new Vector2d(QUOTATIONSHAKES, UNCHRONOLOGICAL), new Vector2d(THESAURUSAGGRAVATION, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(QUOTATIONSHAKES, THESAURUSAGGREGATE) };
            var segs = vecs.ToGLineSegments(basePoint);
            segs.RemoveAt(THESAURUSACCUSTOMED);
            _DrawDomePipes(segs);
            DrawDiryWaterWells1(segs[THESAURUSACCIDENT].EndPoint + new Vector2d(-ACHONDROPLASTIC, THESAURUSACCURATE), values);
            if (output.HasWrappingPipe1) DrawWrappingPipe(segs[THESAURUSACCUSTOMED].StartPoint.OffsetX(THESAURUSACCURATE));
            if (output.HasWrappingPipe2) DrawWrappingPipe(segs[THESAURUSACCIDENT].EndPoint.OffsetX(THESAURUSACCURATE));
            DrawNoteText(output.DN1, segs[THESAURUSACCUSTOMED].StartPoint.OffsetX(ACQUISITIVENESS));
            DrawNoteText(output.DN2, segs[THESAURUSACCIDENT].EndPoint.OffsetX(ACQUISITIVENESS));
            if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSACCUSTOM].StartPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
            if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSACCIDENT].StartPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
            var p = segs[THESAURUSACCUSE].EndPoint;
            var fixY = QUOTATIONANABOLIC;
            var p1 = p.OffsetX(-THESAURUSACCOUNTABLE) + new Vector2d(-QUOTATIONAFRICAN + ACHONDROPLASTIC, fixY);
            DrawDoubleWashBasins(p1.ToPoint3d(), THESAURUSABDOMINAL);
            var p2 = p1.OffsetY(-fixY);
            DrawDomePipes(new GLineSegment[] { new GLineSegment(p1, p2) });
        }
        public static bool CreateDrainageDrawingData(AcadDatabase adb, out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            if (noWL && geoData.Labels.Any(x => IsWL(x.Text)))
            {
                MessageBox.Show(THESAURUSAGGRESSION);
                drDatas = null;
                return THESAURUSABDOMEN;
            }
            drDatas = _CreateDrainageDrawingData(adb, geoData, THESAURUSABDOMINAL);
            return THESAURUSABDOMINAL;
        }
        public static List<DrainageDrawingData> CreateDrainageDrawingData(AcadDatabase adb, DrainageGeoData geoData, bool noDraw)
        {
            ThDrainageService.PreFixGeoData(geoData);
            return _CreateDrainageDrawingData(adb, geoData, noDraw);
        }
        private static List<DrainageDrawingData> _CreateDrainageDrawingData(AcadDatabase adb, DrainageGeoData geoData, bool noDraw)
        {
            List<DrainageDrawingData> drDatas;
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            var roomData = DrainageService.CollectRoomData(adb);
            DrainageService.CreateDrawingDatas(geoData, cadDataMain, cadDatas, out string logString, out drDatas, roomData: roomData);
            if (noDraw) Dispose();
            return drDatas;
        }
        public static bool CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = THESAURUSABDOMEN)
        {
            CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData);
            return CreateDrainageDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CollectDrainageData(AcadDatabase adb, out List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, CommandContext ctx, bool noWL = THESAURUSABDOMEN)
        {
            CollectDrainageGeoData(adb, out storeysItems, out DrainageGeoData geoData, ctx);
            return CreateDrainageDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static List<StoreysItem> GetStoreysItem(List<StoreyInfo> storeys)
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
                            item.Labels = new List<string>() { INTERCHANGEABLY };
                        }
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                        break;
                    case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                    case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                        {
                            item.Ints = s.Numbers.OrderBy(x => x).ToList();
                            item.Labels = item.Ints.Select(x => x + UNINTENTIONALLY).ToList();
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
        public static void CollectDrainageGeoData(AcadDatabase adb, out List<StoreysItem> storeysItems, out DrainageGeoData geoData, CommandContext ctx)
        {
            var storeys = GetStoreys(adb, ctx);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(adb, geoData, ctx);
        }
        public static List<StoreyInfo> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var geo = range?.ToGRect().ToPolygon();
            var storeys = GetStoreyBlockReferences(adb).Select(x => GetStoreyInfo(x)).Where(info => geo?.Contains(info.Boundary.ToPolygon()) ?? THESAURUSABDOMINAL).ToList();
            FixStoreys(storeys);
            return storeys;
        }
        public static void FixStoreys(List<StoreyInfo> storeys)
        {
            var lst1 = storeys.Where(s => s.Numbers.Count == THESAURUSACCESSION).Select(s => s.Numbers[QUOTATIONSHAKES]).ToList();
            foreach (var s in storeys.Where(s => s.Numbers.Count > THESAURUSACCESSION).ToList())
            {
                var hs = new HashSet<int>(s.Numbers);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Numbers.Clear();
                s.Numbers.AddRange(hs.OrderBy(i => i));
            }
        }
        public static void CollectDrainageGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out DrainageGeoData geoData)
        {
            var storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(range, adb, geoData);
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSABDOMINAL))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { THESAURUSACCOMMODATE, THESAURUSABSTAIN, THESAURUSABORTIVE, THESAURUSABSENT, REPRESENTATIONAL, QUOTATIONABSORBENT, THESAURUSABSTENTION });
                var storeys = commandContext.StoreyContext.StoreyInfos;
                List<StoreysItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                var range = commandContext.range;
                if (range != null)
                {
                    if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSABDOMINAL)) return;
                }
                else
                {
                    if (!CollectDrainageData(adb, out storeysItems, out drDatas, commandContext, noWL: THESAURUSABDOMINAL)) return;
                }
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + UNINTENTIONALLY).ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - THESAURUSACCESSION;
                var end = QUOTATIONSHAKES;
                var OFFSET_X = ACCOMMODATINGLY;
                var SPAN_X = ACCOMMODATIVENESS + ACCOMMODATIONAL + THESAURUSACCOMMODATION;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? THESAURUSACCOMPANIMENT;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - THESAURUSACCOMPANIMENT;
                var __dy = THESAURUSACCURATE;
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
            using (var tr = new _DrawingTransaction(adb, THESAURUSABDOMINAL))
            {
                List<StoreysItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: THESAURUSABDOMINAL)) return;
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                Dispose();
                DrawDrainageSystemDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);
                FlushDQ(adb);
            }
        }
        public static void DrawOutlets5(Point2d basePoint, ThwOutput output, DrainageGroupedPipeItem gpItem)
        {
            var values = output.DirtyWaterWellValues;
            var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, -THESAURUSAFFORD), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-EXCOMMUNICATION - ACHONDROPLASTIC, QUOTATIONSHAKES), new Vector2d(QUOTATIONSHAKES, UNCHRONOLOGICAL), new Vector2d(THESAURUSAGGRAVATION, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(QUOTATIONSHAKES, THESAURUSAGGREGATE) };
            var segs = vecs.ToGLineSegments(basePoint);
            segs.RemoveAt(THESAURUSACCUSTOMED);
            DrawDomePipes(segs);
            DrawDiryWaterWells1(segs[THESAURUSACCIDENT].EndPoint + new Vector2d(-ACHONDROPLASTIC, THESAURUSACCURATE), values);
            if (output.HasWrappingPipe1) DrawWrappingPipe(segs[THESAURUSACCUSTOMED].StartPoint.OffsetX(THESAURUSACCURATE));
            if (output.HasWrappingPipe2) DrawWrappingPipe(segs[THESAURUSACCIDENT].EndPoint.OffsetX(THESAURUSACCURATE));
            DrawNoteText(output.DN1, segs[THESAURUSACCUSTOMED].StartPoint.OffsetX(ACQUISITIVENESS));
            DrawNoteText(output.DN2, segs[THESAURUSACCIDENT].EndPoint.OffsetX(ACQUISITIVENESS));
            if (output.HasCleaningPort1) DrawCleaningPort(segs[THESAURUSACCUSTOM].StartPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
            if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSACCIDENT].StartPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
            var p = segs[THESAURUSACCUSE].EndPoint;
            DrawFloorDrain((p.OffsetX(-THESAURUSACCOUNTABLE) + new Vector2d(-QUOTATIONAFRICAN + ACHONDROPLASTIC, QUOTATIONSHAKES)).ToPoint3d(), THESAURUSABDOMINAL);
        }
        static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
        }
        public static List<DrainageGroupedPipeItem> GetDrainageGroupedPipeItems(List<DrainageDrawingData> drDatas, List<StoreysItem> storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys)
        {
            {
                foreach (var drData in drDatas)
                {
                }
            }
            var allFls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsFL(x)));
            var allTls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsTL(x)));
            var allPls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsPL(x)));
            var _storeys = new List<string>();
            foreach (var item in storeysItems)
            {
                item.Init();
                _storeys.AddRange(item.Labels);
            }
            _storeys = _storeys.Distinct().OrderBy(GetStoreyScore).ToList();
            var flstoreylist = new List<KeyValuePair<string, string>>();
            {
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    var item = storeysItems[i];
                    foreach (var fl in drDatas[i].VerticalPipeLabels.Where(x => IsFL(x)))
                    {
                        foreach (var s in item.Labels)
                        {
                            flstoreylist.Add(new KeyValuePair<string, string>(fl, s));
                        }
                    }
                }
            }
            var pls = new HashSet<string>();
            var tls = new HashSet<string>();
            var pltlList = new List<PlTlStoreyItem>();
            var fltlList = new List<FlTlStoreyItem>();
            for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
            {
                var item = storeysItems[i];
                foreach (var gp in drDatas[i].toiletGroupers)
                {
                    if (gp.WLType == WLType.PL_TL && gp.PLs.Count == THESAURUSACCESSION && gp.TLs.Count == THESAURUSACCESSION)
                    {
                        var pl = gp.PLs[QUOTATIONSHAKES];
                        var tl = gp.TLs[QUOTATIONSHAKES];
                        foreach (var s in item.Labels.Where(x => _storeys.Contains(x)))
                        {
                            var x = new PlTlStoreyItem() { pl = pl, tl = tl, storey = s };
                            pls.Add(pl);
                            tls.Add(tl);
                            pltlList.Add(x);
                        }
                    }
                    else if (gp.WLType == WLType.FL_TL && gp.FLs.Count == THESAURUSACCESSION && gp.TLs.Count == THESAURUSACCESSION)
                    {
                        var fl = gp.FLs[QUOTATIONSHAKES];
                        var tl = gp.TLs[QUOTATIONSHAKES];
                        foreach (var s in item.Labels.Where(x => _storeys.Contains(x)))
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
                for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                {
                    var item = storeysItems[i];
                    foreach (var pl in drDatas[i].VerticalPipeLabels.Where(x => IsPL(x)))
                    {
                        if (pls.Contains(pl)) continue;
                        foreach (var s in item.Labels)
                        {
                            plstoreylist.Add(new KeyValuePair<string, string>(pl, s));
                        }
                    }
                }
            }
            var minS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > QUOTATIONSHAKES).Min();
            var maxS = _storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Where(x => x > QUOTATIONSHAKES).Max();
            var countS = maxS - minS + THESAURUSACCESSION;
            {
                allNumStoreys = new List<int>();
                for (int storey = minS; storey <= maxS; storey++)
                {
                    allNumStoreys.Add(storey);
                }
                allRfStoreys = _storeys.Where(x => !IsNumStorey(x)).ToList();
                var allNumStoreyLabels = allNumStoreys.Select(x => x + UNINTENTIONALLY).ToList();
                bool testExist(string label, string storey)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
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
                    return THESAURUSABDOMEN;
                }
                bool hasLong(string label, string storey)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.LongTranslatorLabels.Contains(label))
                                {
                                    var tmp = storeysItems[i].Labels.Where(IsNumStorey).ToList();
                                    if (tmp.Count > THESAURUSACCESSION)
                                    {
                                        var floor = tmp.Select(GetStoreyScore).Max() + UNINTENTIONALLY;
                                        if (storey != floor) return THESAURUSABDOMEN;
                                    }
                                    return THESAURUSABDOMINAL;
                                }
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool hasShort(string label, string storey)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
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
                                        if (tmp.Count > THESAURUSACCESSION)
                                        {
                                            var floor = tmp.Select(GetStoreyScore).Max() + UNINTENTIONALLY;
                                            if (storey != floor) return THESAURUSABDOMEN;
                                        }
                                    }
                                    return THESAURUSABDOMINAL;
                                }
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
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
                    return THESAURUSACCEPT;
                }
                bool hasWaterPort(string label)
                {
                    return getWaterPortLabel(label) != null;
                }
                int getMinTl(string tl)
                {
                    var scores = new List<int>();
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            var drData = drDatas[i];
                            var score = GetStoreyScore(s);
                            if (score < ushort.MaxValue)
                            {
                                if (drData.HasToilet) scores.Add(score);
                            }
                        }
                    }
                    if (scores.Count == QUOTATIONSHAKES) return QUOTATIONSHAKES;
                    var ret = scores.Min() - THESAURUSACCESSION;
                    if (ret <= QUOTATIONSHAKES) return THESAURUSACCESSION;
                    return ret;
                }
                int getMaxTl(string tl)
                {
                    var scores = new List<int>();
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            var drData = drDatas[i];
                            var score = GetStoreyScore(s);
                            if (score < ushort.MaxValue)
                            {
                                if (drData.HasToilet) scores.Add(score);
                            }
                        }
                    }
                    return scores.Count == QUOTATIONSHAKES ? QUOTATIONSHAKES : scores.Max();
                }
                int getFDCount(string label, string storey)
                {
                    int _getFDCount()
                    {
                        for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
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
                        return QUOTATIONSHAKES;
                    }
                    var ret = _getFDCount();
                    {
                        for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == storey)
                                {
                                    var drData = drDatas[i];
                                    if (drData.MustHaveFloorDrains.Contains(label))
                                    {
                                        if (ret == QUOTATIONSHAKES) ret = THESAURUSACCESSION;
                                    }
                                    if (drData.KitchenOnlyFls.Contains(label))
                                    {
                                        if (ret > THESAURUSACCESSION) ret = THESAURUSACCESSION;
                                    }
                                }
                            }
                        }
                    }
                    return ret;
                }
                bool hasSCurve(string label, string storey)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.HasMopPool.Contains(label))
                                {
                                    return THESAURUSABDOMINAL;
                                }
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool IsSeries(string label, string storey)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.Shunts.Contains(label))
                                {
                                    return THESAURUSABDOMEN;
                                }
                            }
                        }
                    }
                    return THESAURUSABDOMINAL;
                }
                bool hasDoubleSCurve(string label, string storey)
                {
                    if (storey == ACCLIMATIZATION) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.KitchenOnlyFls.Contains(label) || drData.KitchenAndBalconyFLs.Contains(label))
                                {
                                    return THESAURUSABDOMINAL;
                                }
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool hasBasinInKitchenAt1F(string label)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == ACCLIMATIZATION)
                            {
                                var drData = drDatas[i];
                                if (drData.KitchenOnlyFls.Contains(label) || drData.KitchenAndBalconyFLs.Contains(label))
                                {
                                    return THESAURUSABDOMINAL;
                                }
                            }
                        }
                    }
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == ACCLIMATIZATION)
                            {
                                var drData = drDatas[i];
                                return drData.HasKitchenBasin.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool IsWaterPipeWellFL(string label)
                {
                    if (IsFL(label))
                    {
                        foreach (var drData in drDatas)
                        {
                            if (drData.WaterPipeWellFLs.Contains(label)) return THESAURUSABDOMINAL;
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool hasCleaningPort(string label, string storey)
                {
                    if (!IsNumStorey(storey)) return THESAURUSABDOMEN;
                    if (GetStoreyScore(storey) == maxS)
                    {
                        return THESAURUSABDOMEN;
                    }
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.MustHaveCleaningPort.Contains(label))
                                {
                                    return THESAURUSABDOMINAL;
                                }
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool hasWrappingPipe(string label)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == ACCLIMATIZATION)
                            {
                                var drData = drDatas[i];
                                return drData.WrappingPipes.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool IsSingleOutlet(string label)
                {
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == ACCLIMATIZATION)
                            {
                                var drData = drDatas[i];
                                return drData.SingleOutlet.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                int getFloorDrainsCountAt1F(string label)
                {
                    if (!IsFL(label)) return QUOTATIONSHAKES;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == ACCLIMATIZATION)
                            {
                                var drData = drDatas[i];
                                drData.FloorDrains.TryGetValue(label, out int r);
                                return r;
                            }
                        }
                    }
                    return QUOTATIONSHAKES;
                }
                bool IsKitchenOnlyFl(string label, string storey)
                {
                    if (!IsFL(label)) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.KitchenOnlyFls.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool IsBalconyOnlyFl(string label, string storey)
                {
                    if (!IsFL(label)) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.BalconyOnlyFLs.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool HasKitchenFloorDrain(string label, string storey)
                {
                    if (!IsFL(label) || !IsKitchenOnlyFl(label, storey)) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.HasKitchenFloorDrain.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool HasKitchenWashingMachine(string label, string storey)
                {
                    if (!IsFL(label) || !IsKitchenOnlyFl(label, storey)) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.HasKitchenWashingMachine.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool HasBalconyWashingMachineFloorDrain(string label, string storey)
                {
                    if (!IsFL(label) || !IsBalconyOnlyFl(label, storey)) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.SureWashingMachineFloorDrain.Contains(label)) return THESAURUSABDOMINAL;
                            }
                        }
                    }
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                return drData.HasBalconyWashingMachine.Contains(label);
                            }
                        }
                    }
                    return THESAURUSABDOMEN;
                }
                bool HasBalconyNonWashingMachineFloorDrain(string label, string storey)
                {
                    if (!IsFL(label) || !IsBalconyOnlyFl(label, storey)) return THESAURUSABDOMEN;
                    for (int i = QUOTATIONSHAKES; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.SureMopPoolFloorDrain.Contains(label)) return THESAURUSABDOMINAL;
                            }
                        }
                    }
                    var fdCount = getFDCount(label, storey);
                    var hasBalconyWashingMachineFloorDrain = HasBalconyWashingMachineFloorDrain(label, storey);
                    if (fdCount == THESAURUSACCESSION && !hasBalconyWashingMachineFloorDrain)
                    {
                        return THESAURUSABDOMINAL;
                    }
                    return fdCount > THESAURUSACCESSION;
                }
                var pipeInfoDict = new Dictionary<string, DrainageGroupingPipeItem>();
                var flGroupingItems = new List<DrainageGroupingPipeItem>();
                var plGroupingItems = new List<DrainageGroupingPipeItem>();
                foreach (var fl in allFls)
                {
                    var item = new DrainageGroupingPipeItem();
                    item.Label = fl;
                    item.HasWaterPort = hasWaterPort(fl);
                    item.HasBasinInKitchenAt1F = hasBasinInKitchenAt1F(fl);
                    item.WaterPortLabel = getWaterPortLabel(fl);
                    item.HasWrappingPipe = hasWrappingPipe(fl);
                    item.Items = new List<DrainageGroupingPipeItem.ValueItem>();
                    item.Hangings = new List<DrainageGroupingPipeItem.Hanging>();
                    item.IsSingleOutlet = IsSingleOutlet(fl);
                    item.FloorDrainsCountAt1F = getFloorDrainsCountAt1F(fl);
                    foreach (var storey in allNumStoreyLabels)
                    {
                        var _hasLong = hasLong(fl, storey);
                        item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                        {
                            Exist = testExist(fl, storey),
                            HasLong = _hasLong,
                            HasShort = hasShort(fl, storey),
                            HasKitchenWashingMachine = HasKitchenWashingMachine(fl, storey),
                            HasKitchenNonWashingMachineFloorDrain = HasKitchenFloorDrain(fl, storey) && !HasKitchenWashingMachine(fl, storey),
                            HasKitchenWashingMachineFloorDrain = HasKitchenFloorDrain(fl, storey) && HasKitchenWashingMachine(fl, storey),
                            HasBalconyWashingMachineFloorDrain = HasBalconyWashingMachineFloorDrain(fl, storey),
                            HasBalconyNonWashingMachineFloorDrain = HasBalconyNonWashingMachineFloorDrain(fl, storey),
                        });
                        item.Hangings.Add(new DrainageGroupingPipeItem.Hanging()
                        {
                            FloorDrainsCount = getFDCount(fl, storey),
                            HasCleaningPort = hasCleaningPort(fl, storey) || _hasLong,
                            HasSCurve = hasSCurve(fl, storey),
                            HasDoubleSCurve = hasDoubleSCurve(fl, storey),
                            IsSeries = IsSeries(fl, storey),
                        });
                    }
                    if (IsWaterPipeWellFL(fl))
                    {
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = THESAURUSACCESSION;
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
                    item.IsSingleOutlet = IsSingleOutlet(pl);
                    foreach (var storey in allNumStoreyLabels)
                    {
                        var _hasLong = hasLong(pl, storey);
                        item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                        {
                            Exist = testExist(pl, storey),
                            HasLong = _hasLong,
                            HasShort = hasShort(pl, storey),
                            HasKitchenNonWashingMachineFloorDrain = HasKitchenFloorDrain(pl, storey),
                            HasKitchenWashingMachineFloorDrain = HasKitchenWashingMachine(pl, storey),
                            HasBalconyWashingMachineFloorDrain = HasBalconyWashingMachineFloorDrain(pl, storey),
                            HasBalconyNonWashingMachineFloorDrain = HasBalconyNonWashingMachineFloorDrain(pl, storey),
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
                            item.HasTL = THESAURUSABDOMINAL;
                            item.MinTl = getMinTl(tl);
                            item.MaxTl = getMaxTl(tl);
                            item.TlLabel = tl;
                        }
                    }
                    plGroupingItems.Add(item);
                    pipeInfoDict[pl] = item;
                }
                {
                    foreach (var item in plGroupingItems)
                    {
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = QUOTATIONSHAKES;
                        }
                    }
                }
                {
                    foreach (var item in flGroupingItems)
                    {
                        if (IsFL0(item.Label))
                        {
                            item.IsFL0 = THESAURUSABDOMINAL;
                            foreach (var hanging in item.Hangings)
                            {
                                hanging.FloorDrainsCount = THESAURUSACCESSION;
                                hanging.HasSCurve = THESAURUSABDOMEN;
                                hanging.HasDoubleSCurve = THESAURUSABDOMEN;
                                hanging.HasCleaningPort = THESAURUSABDOMEN;
                            }
                        }
                    }
                }
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        for (int i = QUOTATIONSHAKES; i < item.Hangings.Count; i++)
                        {
                            if (!item.Items[i].Exist) continue;
                            var hanging = item.Hangings[i];
                            var storey = allNumStoreyLabels[i];
                            hanging.HasCleaningPort = IsPL(item.Label) || IsDL(item.Label);
                            hanging.HasDownBoardLine = IsPL(item.Label) || IsDL(item.Label);
                            {
                                var m = item.Items.TryGet(i - THESAURUSACCESSION);
                                if ((m.Exist && m.HasLong) || storey == ACCLIMATIZATION)
                                {
                                    hanging.HasCheckPoint = THESAURUSABDOMINAL;
                                }
                            }
                            if (hanging.HasCleaningPort)
                            {
                                hanging.HasCheckPoint = THESAURUSABDOMINAL;
                            }
                            if (hanging.HasDoubleSCurve)
                            {
                                hanging.HasCheckPoint = THESAURUSABDOMINAL;
                            }
                            if (GetStoreyScore(storey) == maxS)
                            {
                                hanging.HasCleaningPort = THESAURUSABDOMEN;
                            }
                        }
                    }
                }
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        item.HasWrappingPipe = THESAURUSABDOMINAL;
                    }
                    foreach (var kv in pipeInfoDict)
                    {
                        var label = kv.Key;
                        var item = kv.Value;
                        if (allRfStoreys.Any(storey => testExist(label, storey)))
                        {
                            item.CanHaveAring = THESAURUSABDOMINAL;
                        }
                    }
                }
                {
                    if (allNumStoreys.Max() < THESAURUSADAPTATION)
                    {
                        foreach (var item in pipeInfoDict.Values)
                        {
                            item.HasTL = THESAURUSABDOMEN;
                        }
                    }
                }
                foreach (var kv in pipeInfoDict)
                {
                    var label = kv.Key;
                    var item = kv.Value;
                    if (IsFL0(label))
                    {
                        for (int i = item.Items.Count - THESAURUSACCESSION; i >= QUOTATIONSHAKES; --i)
                        {
                            if (item.Items[i].Exist)
                            {
                                item.Items[i] = default;
                                break;
                            }
                        }
                    }
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
                        HasBasinInKitchenAt1F = g.Key.HasBasinInKitchenAt1F,
                        FloorDrainsCountAt1F = g.Key.FloorDrainsCountAt1F,
                        CanHaveAring = g.Key.CanHaveAring,
                        IsFL0 = g.Key.IsFL0,
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
                        IsSingleOutlet = g.Key.IsSingleOutlet,
                        CanHaveAring = g.Key.CanHaveAring,
                        IsFL0 = g.Key.IsFL0,
                    };
                    pipeGroupItems.Add(item);
                }
                pipeGroupItems = pipeGroupItems.OrderBy(x => GetLabelScore(x.Labels.FirstOrDefault())).ToList();
                return pipeGroupItems;
            }
        }
        public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * PHARMACEUTICALS;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + THESAURUSACQUISITIVE;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-THESAURUSACOLYTE);
            var p3 = p1.OffsetX(THESAURUSACOLYTE);
            var line = DrawLineLazy(p2, p3);
            line.Layer = THESAURUSABORTIVE;
            ByLayer(line);
        }
        public static void DrawPipeButtomHeightSymbol(double HEIGHT, Point2d p)
        {
            var vecs = new List<Vector2d> { new Vector2d(ACHLOROPHYLLOUS + ACCOMMODATIONAL, QUOTATIONSHAKES), new Vector2d(QUOTATIONSHAKES, HEIGHT * THESAURUSAGGRESSIVE) };
            var segs = vecs.ToGLineSegments(p);
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: THESAURUSACCORDING, basePt: segs.Last().EndPoint.OffsetX(THESAURUSADMINISTER).ToPoint3d(),
      props: new Dictionary<string, string>() { { THESAURUSACCORDING, THESAURUSAGGRESSOR } },
      cb: br =>
      {
          br.Layer = THESAURUSABORTIVE;
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
                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, QUOTATIONSHAKES));
                Dr.SetLabelStylesForWNote(line, dbt);
                DrawBlockReference(blkName: THESAURUSACCORDING, basePt: basePt.OffsetX(CORRESPONDINGLY), layer: THESAURUSACCOMMODATE, props: new Dictionary<string, string>() { { THESAURUSACCORDING, THESAURUSACCEPTABLE } });
            }
            if (label == INTERCHANGEABLY)
            {
                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, QUOTATIONSHAKES), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, QUOTATIONSHAKES));
                var dbt = DrawTextLazy(THESAURUSACCORDINGLY, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, QUOTATIONSHAKES));
                Dr.SetLabelStylesForWNote(line, dbt);
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
                return THESAURUSACCESS + label;
            }
            if (IsFL(label))
            {
                return APPROACHABILITY + label;
            }
            return label;
        }
        public static int GetStoreyScore(string label)
        {
            if (label == null) return QUOTATIONSHAKES;
            switch (label)
            {
                case INTERCHANGEABLY: return ushort.MaxValue;
                case THESAURUSACCESSIBLE: return ushort.MaxValue + THESAURUSACCESSION;
                case THESAURUSACCESSORY: return ushort.MaxValue + THESAURUSACCIDENT;
                default:
                    {
                        int.TryParse(label.Replace(UNINTENTIONALLY, THESAURUSACCEPTABLE), out int ret);
                        return ret;
                    }
            }
        }
        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = THESAURUSABORTIVE;
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = PHARMACEUTICALS;
                    SetTextStyleLazy(t, ACTINOMYCETALES);
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
            line.Layer = THESAURUSABSTAIN;
            ByLayer(line);
        }
        public static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSABSORB;
                ByLayer(line);
            });
        }
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.IsValid));
            lines.ForEach(line =>
            {
                line.Layer = THESAURUSABORTIVE;
                ByLayer(line);
            });
        }
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DrawTextLazy(text, POLYOXYMETHYLENE, pt);
            SetLabelStylesForDraiNote(t);
        }
        public static void DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
        {
            var p2 = p1 + vec7;
            DrawDomePipes(new GLineSegment(p1, p2));
            if (!Testing) DrawSWaterStoringCurve(p2.ToPoint3d(), leftOrRight);
        }
        public static void DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
        {
            var p2 = p1 + vec7;
            DrawDomePipes(new GLineSegment(p1, p2));
            if (!Testing) DrawDoubleWashBasins(p2.ToPoint3d(), leftOrRight);
        }
        public static bool Testing;
        public static void DrawDoubleWashBasins(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference(ALLOTETRAPLOIDY, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSABSENT;
                      br.ScaleFactors = new Scale3d(THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSAGHAST);
                      }
                  });
            }
            else
            {
                DrawBlockReference(ALLOTETRAPLOIDY, basePt,
                  br =>
                  {
                      br.Layer = THESAURUSABSENT;
                      br.ScaleFactors = new Scale3d(-THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSAGHAST);
                      }
                  });
            }
        }
        public static void DrawWrappingPipe(Point2d basePt)
        {
            DrawWrappingPipe(basePt.ToPoint3d());
        }
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DrawBlockReference(THESAURUSACTIVE, basePt, br =>
            {
                br.Layer = REPRESENTATIONAL;
                ByLayer(br);
            });
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = THESAURUSAGILITY)
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSACUMEN, basePt, br =>
                {
                    br.Layer = THESAURUSABSENT;
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, value);
                    }
                });
            }
            else
            {
                DrawBlockReference(THESAURUSACUMEN, basePt,
               br =>
               {
                   br.Layer = THESAURUSABSENT;
                   ByLayer(br);
                   br.ScaleFactors = new Scale3d(-THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, value);
                   }
               });
            }
        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSAGITATE, basePt, br =>
                {
                    br.Layer = THESAURUSABSENT;
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSAGITATION);
                        br.ObjectId.SetDynBlockValue(THESAURUSAGITATOR, (short)THESAURUSACCESSION);
                    }
                });
            }
            else
            {
                DrawBlockReference(THESAURUSAGITATE, basePt,
                   br =>
                   {
                       br.Layer = THESAURUSABSENT;
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-THESAURUSACCIDENT, THESAURUSACCIDENT, THESAURUSACCIDENT);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue(THESAURUSACTIVITY, THESAURUSAGITATION);
                           br.ObjectId.SetDynBlockValue(THESAURUSAGITATOR, (short)THESAURUSACCESSION);
                       }
                   });
            }
        }
        public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
        {
            if (leftOrRight)
            {
                DrawBlockReference(THESAURUSABSOLUTION, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSABSENT;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(ACCUMULATIVENESS);
                });
            }
            else
            {
                DrawBlockReference(THESAURUSABSOLUTION, basePt, scale: scale, cb: br =>
                {
                    br.Layer = THESAURUSABSENT;
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(ACCUMULATIVENESS + THESAURUSADVANTAGE);
                });
            }
        }
        public static void DrawCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DrawBlockReference(blkName: CHARACTERISTICS, basePt: basePt,
      cb: br =>
      {
          if (leftOrRight)
          {
              br.ScaleFactors = new Scale3d(-THESAURUSACCESSION, THESAURUSACCESSION, THESAURUSACCESSION);
          }
          ByLayer(br);
          br.Layer = THESAURUSABSENT;
      });
        }
        public static Point2d drawHanging(Point2d start, Hanging hanging)
        {
            var vecs = new List<Vector2d> { new Vector2d(THESAURUSAFFILIATE, QUOTATIONSHAKES), new Vector2d(THESAURUSAFFECTION, QUOTATIONSHAKES), new Vector2d(THESAURUSADVANTAGE, QUOTATIONSHAKES), new Vector2d(THESAURUSAFFILIATION, QUOTATIONSHAKES) };
            var segs = vecs.ToGLineSegments(start);
            {
                var _segs = segs.ToList();
                if (hanging.FloorDrainsCount == THESAURUSACCESSION)
                {
                    _segs.RemoveAt(THESAURUSACCUSTOMED);
                }
                _segs.RemoveAt(THESAURUSACCIDENT);
                DrawDomePipes(_segs);
            }
            {
                var pts = vecs.ToPoint2ds(start);
                {
                    var pt = pts[THESAURUSACCESSION];
                    var v = new Vector2d(ACCUMULATIVENESS, ACCUMULATIVENESS);
                    if (hanging.HasSCurve)
                    {
                        DrawSCurve(v, pt, THESAURUSABDOMEN);
                    }
                    if (hanging.HasDoubleSCurve)
                    {
                        DrawDSCurve(v, pt, THESAURUSABDOMEN);
                    }
                }
                if (hanging.FloorDrainsCount >= THESAURUSACCESSION)
                {
                    DrawFloorDrain(pts[THESAURUSACCIDENT].ToPoint3d(), THESAURUSABDOMEN);
                }
                if (hanging.FloorDrainsCount >= THESAURUSACCIDENT)
                {
                    DrawFloorDrain(pts[THESAURUSACCUSTOM].ToPoint3d(), THESAURUSABDOMEN);
                }
            }
            start = segs.Last().EndPoint;
            return start;
        }
        public static void DrawOutlets1(Point2d basePoint1, double width, ThwOutput output, Action<IEnumerable<GLineSegment>> _DrawDomePipes, double dy = -QUOTATIONCARLYLE, bool isRainWaterWell = THESAURUSABDOMEN)
        {
            Point2d pt2, pt3;
            if (output.DirtyWaterWellValues != null)
            {
                var v = new Vector2d(-ACHONDROPLASIAC - ACHONDROPLASTIC, -UNCHRONOLOGICAL);
                var pt = basePoint1 + v;
                var values = output.DirtyWaterWellValues;
                DrawDiryWaterWells1(pt, values, isRainWaterWell);
            }
            {
                var dx = width - DEHYDROGENATION;
                var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, dy), new Vector2d(-THESAURUSACCOUNTABLE, -THESAURUSACCOUNTABLE), new Vector2d(-ACKNOWLEDGEABLE, QUOTATIONSHAKES), new Vector2d(QUOTATIONSHAKES, -UNCHRONOLOGICAL), new Vector2d(MARSIPOBRANCHII + dx, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE), new Vector2d(QUOTATIONSHAKES, THESAURUSAGNOSTIC), new Vector2d(-AGRANULOCYTOSIS - dx, -THESAURUSACCOUNT), new Vector2d(THESAURUSAGREEABLE + dx, QUOTATIONSHAKES), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE) };
                {
                    var segs = vecs.ToGLineSegments(basePoint1);
                    if (output.LinesCount == THESAURUSACCESSION)
                    {
                        _DrawDomePipes(segs.Take(THESAURUSACCUSTOMED));
                    }
                    else if (output.LinesCount > THESAURUSACCESSION)
                    {
                        segs.RemoveAt(THESAURUSADAPTATION);
                        if (!output.HasVerticalLine2) segs.RemoveAt(QUOTATIONSPENSER);
                        segs.RemoveAt(THESAURUSACCUSTOMED);
                        _DrawDomePipes(segs);
                    }
                }
                var pts = vecs.ToPoint2ds(basePoint1);
                if (output.HasWrappingPipe1) DrawWrappingPipe(pts[THESAURUSACCUSTOMED].OffsetX(THESAURUSACCURATE));
                if (output.HasWrappingPipe2) DrawWrappingPipe(pts[THESAURUSACCUSTOM].OffsetX(THESAURUSACCURATE));
                if (output.HasWrappingPipe3) DrawWrappingPipe(pts[THESAURUSAGREEMENT].OffsetX(THESAURUSACCURATE));
                DrawNoteText(output.DN1, pts[THESAURUSACCUSTOMED].OffsetX(ACQUISITIVENESS));
                DrawNoteText(output.DN2, pts[THESAURUSACCUSTOM].OffsetX(ACQUISITIVENESS));
                DrawNoteText(output.DN3, pts[THESAURUSAGREEMENT].OffsetX(ACQUISITIVENESS));
                if (output.HasCleaningPort1) DrawCleaningPort(pts[THESAURUSACCIDENT].ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
                if (output.HasCleaningPort2) DrawCleaningPort(pts[THESAURUSACCUSE].ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
                if (output.HasCleaningPort3) DrawCleaningPort(pts[QUOTATIONACCUSATIVE].ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCESSION);
                pt2 = pts[QUOTATIONSPENSER];
                pt3 = pts.Last();
            }
            if (output.HasLargeCleaningPort)
            {
                var vecs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, AGRIBUSINESSMAN) };
                var segs = vecs.ToGLineSegments(pt3);
                _DrawDomePipes(segs);
                DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), THESAURUSABDOMEN, THESAURUSACCIDENT);
            }
            if (output.HangingCount == THESAURUSACCESSION)
            {
                var hang = output.Hanging1;
                Point2d lastPt = pt2;
                {
                    var segs = new List<Vector2d> { new Vector2d(QUOTATIONSHAKES, AGRICULTURALIST), new Vector2d(THESAURUSACCOUNTABLE, THESAURUSACCOUNTABLE) }.ToGLineSegments(lastPt);
                    _DrawDomePipes(segs);
                    lastPt = segs.Last().EndPoint;
                }
                {
                    lastPt = drawHanging(lastPt, output.Hanging1);
                }
            }
            else if (output.HangingCount == THESAURUSACCIDENT)
            {
                var vs1 = new List<Vector2d> { new Vector2d(THESAURUSAGRICULTURE, THESAURUSAGRICULTURE), new Vector2d(THESAURUSAGROUND, THESAURUSAGROUND) };
                var pts = vs1.ToPoint2ds(pt3);
                _DrawDomePipes(vs1.ToGLineSegments(pt3));
                drawHanging(pts.Last(), output.Hanging1);
                var dx = output.Hanging1.FloorDrainsCount == THESAURUSACCIDENT ? THESAURUSABSTRACTED : QUOTATIONSHAKES;
                var vs2 = new List<Vector2d> { new Vector2d(LYMPHADENOPATHY + dx, QUOTATIONSHAKES), new Vector2d(THESAURUSAGROUND, THESAURUSAGROUND) };
                _DrawDomePipes(vs2.ToGLineSegments(pts[THESAURUSACCESSION]));
                drawHanging(vs2.ToPoint2ds(pts[THESAURUSACCESSION]).Last(), output.Hanging2);
            }
        }
        public static void DrawDiryWaterWells2(Point2d pt, List<string> values)
        {
            var dx = QUOTATIONSHAKES;
            foreach (var value in values)
            {
                DrawDirtyWaterWell(pt.OffsetX(ACHONDROPLASTIC) + new Vector2d(dx, QUOTATIONSHAKES), value);
                dx += ACHLOROPHYLLOUS;
            }
        }
        public static void DrawRainWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: THESAURUSACTUALLY, basePt: basePt.OffsetY(-ACHONDROPLASTIC),
          props: new Dictionary<string, string>() { { THESAURUSACCEPT, value } },
          cb: br =>
          {
              br.Layer = THESAURUSABSENCE;
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
            DrawBlockReference(blkName: THESAURUSACCEPTANCE, basePt: basePt.OffsetY(-ACHONDROPLASTIC),
            props: new Dictionary<string, string>() { { THESAURUSACCEPT, value } },
            cb: br =>
            {
                br.Layer = THESAURUSABSENT;
                ByLayer(br);
            });
        }
        public static void DrawDiryWaterWells1(Point2d pt, List<string> values, bool isRainWaterWell = THESAURUSABDOMEN)
        {
            if (values == null) return;
            if (values.Count == THESAURUSACCESSION)
            {
                if (!isRainWaterWell)
                {
                    DrawDirtyWaterWell(pt, values[QUOTATIONSHAKES]);
                }
                else
                {
                    DrawRainWaterWell(pt, values[QUOTATIONSHAKES]);
                }
            }
            else if (values.Count >= THESAURUSACCIDENT)
            {
                var pts = GetBasePoints(pt.OffsetX(-ACHLOROPHYLLOUS), THESAURUSACCIDENT, values.Count, ACHLOROPHYLLOUS, ACHLOROPHYLLOUS).ToList();
                for (int i = QUOTATIONSHAKES; i < values.Count; i++)
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
            int i = QUOTATIONSHAKES, j = QUOTATIONSHAKES;
            for (int k = QUOTATIONSHAKES; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = QUOTATIONSHAKES;
                }
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = QUOTATIONSHAKES, j = QUOTATIONSHAKES;
            for (int k = QUOTATIONSHAKES; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, QUOTATIONSHAKES);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = QUOTATIONSHAKES;
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
            }
            geoData.FixData();
            for (int i = QUOTATIONSHAKES; i < geoData.LabelLines.Count; i++)
            {
                var seg = geoData.LabelLines[i];
                if (seg.IsHorizontal(THESAURUSACCUSE))
                {
                    geoData.LabelLines[i] = seg.Extend(QUOTATIONSPENSER);
                }
                else if (seg.IsVertical(THESAURUSACCUSE))
                {
                    geoData.LabelLines[i] = seg.Extend(THESAURUSACCESSION);
                }
            }
            for (int i = QUOTATIONSHAKES; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(THESAURUSACCUSE);
            }
            for (int i = QUOTATIONSHAKES; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(THESAURUSACCUSE);
            }
            {
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(THESAURUSABANDON)).ToList();
            }
            {
                geoData.WashingMachines = GeoFac.GroupGeometries(geoData.WashingMachines.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = GeoFac.GroupGeometries(geoData.FloorDrains.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
                geoData.FloorDrains = geoData.FloorDrains.Where(x => x.Width < THESAURUSADDICTION && x.Height < THESAURUSADDICTION).ToList();
            }
            {
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, THESAURUSACCLAIM))).ToList();
            }
            {
                var cmp = new GRect.EqualityComparer(THESAURUSABANDON);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.WashingMachines = geoData.WashingMachines.Distinct(cmp).ToList();
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
                if (s == null) return THESAURUSABDOMEN;
                if (IsMaybeLabelText(s)) return THESAURUSABDOMINAL;
                return THESAURUSABDOMEN;
            }
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Where(x => f(x.Text)).Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(THESAURUSACCLAIM)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).ToList();
            var f1 = GeoFac.CreateContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-THESAURUSACCLAIM).OffsetY(-THESAURUSADMINISTER), ADRENOCORTICOTROPHIC, THESAURUSADMINISTER);
                var _lineHGs = f1(g.ToPolygon());
                var geo = GeoFac.NearestNeighbourGeometryF(_lineHGs)(bd.Center.ToNTSPoint());
                if (geo == null) continue;
                {
                    var line = lineHs[lineHGs.IndexOf(geo)];
                    var dis = line.Center.GetDistanceTo(bd.Center);
                    if (dis.InRange(INATTENTIVENESS, ACHONDROPLASTIC) || Math.Abs(line.Center.Y - bd.Center.Y).InRange(THESAURUSABANDON, ACHONDROPLASTIC))
                    {
                        geoData.LabelLines.Add(new GLineSegment(bd.Center, line.Center).Extend(THESAURUSABANDON));
                    }
                }
            }
        }
    }
    public class ThDrainageSystemServiceGeoCollector3
    {
        public AcadDatabase adb;
        public DrainageGeoData geoData;
        List<GLineSegment> labelLines => geoData.LabelLines;
        List<CText> cts => geoData.Labels;
        List<GLineSegment> dlines => geoData.DLines;
        List<GLineSegment> vlines => geoData.VLines;
        List<GRect> pipes => geoData.VerticalPipes;
        List<GRect> downwaterPorts => geoData.DownWaterPorts;
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<GRect> washingMachines => geoData.WashingMachines;
        List<GRect> mopPools => geoData.MopPools;
        List<GRect> basins => geoData.Basins;
        List<GRect> pipeKillers => geoData.PipeKillers;
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
                    if (e.LayerId.IsNull) return THESAURUSABDOMEN;
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
        const int distinguishDiameter = THESAURUSACQUIREMENT;
        private void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!IsLayerVisible(entity)) return;
            if (isInXref)
            {
                return;
            }
            static bool f(string layer) => layer is THESAURUSABSENT or THESAURUSABORTIVE or THESAURUSABRASIVE;
            var dxfName = entity.GetRXClass().DxfName.ToUpper();
            {
                if (f(entity.Layer) && entity is Line line && line.Length > QUOTATIONSHAKES)
                {
                    var seg = line.ToGLineSegment().TransformBy(matrix);
                    reg(fs, seg, labelLines);
                    return;
                }
            }
            {
                if (entity is Circle c && f(entity.Layer))
                {
                    if (distinguishDiameter < c.Radius && c.Radius <= INATTENTIVENESS)
                    {
                        var bd = c.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (THESAURUSACCUSE < c.Radius && c.Radius <= distinguishDiameter && c.Layer is THESAURUSABSENT or THESAURUSABRASIVE)
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
                    if (distinguishDiameter < c.Radius && c.Radius <= INATTENTIVENESS)
                    {
                        if (f(c.Layer))
                        {
                            var r = c.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, pipes);
                            return;
                        }
                    }
                }
            }
            if (entity.Layer is THESAURUSABSTAIN or THESAURUSABSTEMIOUS)
            {
                if (entity is Line line && line.Length > QUOTATIONSHAKES)
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
                if (dxfName is CONVENTIONALIZED)
                {
                    dynamic o = entity;
                    var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                    reg(fs, seg, dlines);
                    return;
                }
            }
            if (dxfName is CONVENTIONALIZED)
            {
                if (entity.Layer is THESAURUSABSTRUSE or THESAURUSABSENCE or THESAURUSABSENT)
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
            if (dxfName == THESAURUSACCELERATE)
            {
                dynamic o = entity.AcadObject;
                var text = (string)o.DimStyleText + THESAURUSACCEPT + (string)o.VPipeNum;
                var colle = entity.ExplodeToDBObjectCollection();
                var ts = new List<DBText>();
                foreach (var e in colle.OfType<Entity>().Where(IsLayerVisible))
                {
                    if (e is Line line)
                    {
                        if (line.Length > QUOTATIONSHAKES)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, labelLines);
                            continue;
                        }
                    }
                    else if (e.GetRXClass().DxfName.ToUpper() == THESAURUSACCELERATION)
                    {
                        ts.AddRange(e.ExplodeToDBObjectCollection().OfType<DBText>().Where(IsLayerVisible));
                        continue;
                    }
                }
                if (ts.Count > QUOTATIONSHAKES)
                {
                    GRect bd;
                    if (ts.Count == THESAURUSACCESSION) bd = ts[QUOTATIONSHAKES].Bounds.ToGRect();
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
                static bool g(string t) => !t.StartsWith(THESAURUSACCENT) && !t.ToLower().Contains(PLENIPOTENTIARY) && !t.ToUpper().Contains(THESAURUSAMBASSADOR);
                if (entity is DBText dbt && f(entity.Layer) && g(dbt.TextString))
                {
                    var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                    var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                    if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                    return;
                }
            }
            if (dxfName == THESAURUSACCELERATION)
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
            if (dxfName == THESAURUSADULTERATE)
            {
                var colle = entity.ExplodeToDBObjectCollection();
                {
                    foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSACCELERATION or QUOTATIONALMAIN).Where(IsLayerVisible))
                    {
                        foreach (var dbt in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                        {
                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                            if (IsMaybeLabelText(ct.Text)) reg(fs, ct, cts);
                        }
                    }
                    foreach (var seg in colle.OfType<Line>().Where(x => x.Length > QUOTATIONSHAKES).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                    {
                        reg(fs, seg, labelLines);
                    }
                }
                return;
            }
        }
        private void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
        {
            if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
            if (!br.Visible) return;
            if (IsLayerVisible(br))
            {
                var name = br.GetEffectiveName();
                if (name is THESAURUSACCEPTANCE)
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    var lb = br.GetAttributesStrValue(THESAURUSACCEPT) ?? THESAURUSACCEPTABLE;
                    reg(fs, bd, () =>
                    {
                        waterPorts.Add(bd);
                        waterPortLabels.Add(lb);
                    });
                    return;
                }
                if (name.Contains(ACKNOWLEDGEMENT) || name is ADSIGNIFICATION)
                {
                    if (br.IsDynamicBlock)
                    {
                        var bd = GRect.Combine(EnumerateVisibleEntites(adb, br).Select(x => x.Bounds.ToGRect()).Where(x => x.IsValid));
                        bd = bd.TransformBy(matrix);
                        reg(fs, bd, floorDrains);
                        return;
                    }
                    else
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, floorDrains);
                        return;
                    }
                }
                {
                    static bool f(string layer) => layer is THESAURUSABSENT or THESAURUSABSENCE or THESAURUSABUNDANT or THESAURUSALIGHT or THESAURUSABRASIVE;
                    if (name is THESAURUSABSTRACTION or THESAURUSABUSIVE)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(INCOMPREHENSIBLE);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name.Contains(THESAURUSABSURD))
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(INCOMPREHENSIBILITY);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is AMBIDEXTROUSNESS)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSABSOLUTE);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is THESAURUSACADEMIC && br.Layer is QUOTATIONABYSSINIAN or THESAURUSABSENT or THESAURUSABSTAIN)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSABSOLUTE);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is ACANTHOPTERYGII && br.Layer is HYPOCHONDRIACAL)
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSABSOLUTE);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name is THESAURUSABSTRACTION or THESAURUSABUSIVE)
                    {
                        var bd = GRect.Create(br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix), INCOMPREHENSIBLE);
                        reg(fs, bd, pipes);
                        return;
                    }
                    if (name.Contains(THESAURUSABSURD))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, pipes);
                        return;
                    }
                }
                if (name.Contains(THESAURUSABSTRACT))
                {
                    var bd = br.Bounds.ToGRect().TransformBy(matrix);
                    if (bd.IsValid)
                    {
                        if (bd.Width < THESAURUSABSTRACTED && bd.Height < THESAURUSABSTRACTED)
                        {
                            reg(fs, bd, wrappingPipes);
                        }
                    }
                    return;
                }
                {
                    var ok = THESAURUSABDOMEN;
                    if (killerNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        if (!washingMachinesNames.Any(x => name.Contains(x)))
                        {
                            reg(fs, bd, pipeKillers);
                        }
                        ok = THESAURUSABDOMINAL;
                        var x = adb.Layers.Element(br.Layer);
                    }
                    if (basinNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, basins);
                        ok = THESAURUSABDOMINAL;
                    }
                    if (washingMachinesNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, washingMachines);
                        ok = THESAURUSABDOMINAL;
                    }
                    if (mopPoolNames.Any(x => name.Contains(x)))
                    {
                        var bd = br.Bounds.ToGRect().TransformBy(matrix);
                        reg(fs, bd, mopPools);
                        ok = THESAURUSABDOMINAL;
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
        readonly List<string> basinNames = new List<string>() { THESAURUSALACRITY, AUTOBIOGRAPHICAL, QUOTATIONPERFIDIOUS, CRYSTALLIZATION, THESAURUSALCOHOL, THESAURUSALCOHOLIC, THESAURUSALCOVE };
        readonly List<string> mopPoolNames = new List<string>() { PHYSIOTHERAPIST, };
        readonly List<string> killerNames = new List<string>() { THESAURUSALCOVE, ADMINISTRATIONS, QUOTATIONALFVÉN, MAGNETOHYDRODYNAMIC, MAGNETOHYDRODYNAMICS, EXTRATERRESTRIAL, THESAURUSALIENATE, THESAURUSALIENATION };
        readonly List<string> washingMachinesNames = new List<string>() { QUOTATION1BBLACK, REPRESENTATIVES };
        bool isInXref;
        public void CollectEntities()
        {
            foreach (var entity in adb.ModelSpace.OfType<Entity>())
            {
                {
                    if (entity.Layer is DISSATISFACTION)
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
                        isInXref = THESAURUSABDOMEN;
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
                return THESAURUSABDOMEN;
            }
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return THESAURUSABDOMEN;
            }
            if (!blockTableRecord.Explodable)
            {
                return THESAURUSABDOMEN;
            }
            return THESAURUSABDOMINAL;
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, List<GLineSegment> lst)
        {
            reg(fs, seg, () => { lst.Add(seg); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, List<GRect> lst)
        {
            reg(fs, r, () => { lst.Add(r); });
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        private static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, List<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
    }
    public class DrainageService
    {
        public static void CollectGeoData(AcadDatabase adb, DrainageGeoData geoData, CommandContext ctx)
        {
            var cl = new ThDrainageSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(ctx);
            cl.CollectEntities();
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = new ThDrainageSystemServiceGeoCollector3() { adb = adb, geoData = geoData };
            cl.CollectStoreys(range);
            cl.CollectEntities();
        }
        static bool AllNotEmpty(params List<Geometry>[] plss)
        {
            foreach (var pls in plss)
            {
                if (pls.Count == QUOTATIONSHAKES) return THESAURUSABDOMEN;
            }
            return THESAURUSABDOMINAL;
        }
        static Polygon ConvertToPolygon(Polyline pl)
        {
            if (pl.NumberOfVertices <= THESAURUSACCIDENT)
                return null;
            var list = new List<Point2d>();
            for (int i = QUOTATIONSHAKES; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                if (list.Count == QUOTATIONSHAKES || !Equals(pt, list.Last()))
                {
                    list.Add(pt);
                }
            }
            if (list.Count <= THESAURUSACCIDENT) return null;
            try
            {
                var tmp = list.Select(x => x.ToNTSCoordinate()).ToList(list.Count + THESAURUSACCESSION);
                if (!tmp[QUOTATIONSHAKES].Equals(tmp[tmp.Count - THESAURUSACCESSION]))
                {
                    tmp.Add(tmp[QUOTATIONSHAKES]);
                }
                var ring = new LinearRing(tmp.ToArray());
                return new Polygon(ring);
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }
        public static List<KeyValuePair<string, Geometry>> CollectRoomData(AcadDatabase adb)
        {
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer?.ToUpper() is THESAURUSADJUSTMENT or THESAURUSAMBIGUOUS)
                .SelectNotNull(ConvertToPolygon).ToList();
            var names = adb.ModelSpace.Where(x => x.Layer?.ToUpper() is EXTEMPORANEOUSLY or THESAURUSAMBITION).SelectNotNull(entity =>
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
                if (dxfName == THESAURUSACCELERATION)
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
                    if (l.Count == THESAURUSACCESSION)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text.Trim(), l[QUOTATIONSHAKES]));
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
                list.Add(new KeyValuePair<string, Geometry>(THESAURUSACCEPTABLE, range));
            }
            return list;
        }
        public static void CreateDrawingDatas(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas, out string logString, out List<DrainageDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData = null)
        {
            _DrawingTransaction.Current.AbleToDraw = THESAURUSABDOMEN;
            roomData ??= new List<KeyValuePair<string, Geometry>>();
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = THESAURUSACCESSION;
            }
            var sb = new StringBuilder(THESAURUSADDICT);
            drDatas = new List<DrainageDrawingData>();
            var _kitchens = roomData.Where(x => IsKitchen(x.Key)).Select(x => x.Value).ToList();
            var _toilets = roomData.Where(x => IsToilet(x.Key)).Select(x => x.Value).ToList();
            var _nonames = roomData.Where(x => x.Key is THESAURUSACCEPTABLE).Select(x => x.Value).ToList();
            var _balconies = roomData.Where(x => IsBalcony(x.Key)).Select(x => x.Value).ToList();
            var _kitchensf = F(_kitchens);
            var _toiletsf = F(_toilets);
            var _nonamesf = F(_nonames);
            var _balconiesf = F(_balconies);
            for (int storeyI = QUOTATIONSHAKES; storeyI < cadDatas.Count; storeyI++)
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
                    var maxDis = THESAURUSABRUPT;
                    var angleTolleranceDegree = THESAURUSACCESSION;
                    var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > QUOTATIONSHAKES).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
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
                var labellinesf = F(labelLinesGeos);
                var shortTranslatorLabels = new HashSet<string>();
                var longTranslatorLabels = new HashSet<string>();
                var dlinesGroups = GG(item.DLines);
                var dlinesGeos = GeoFac.GroupLinesByConnPoints(item.DLines, AUTHORITARIANISM).ToList();
                var vlinesGroups = GG(item.VLines);
                var wrappingPipesf = F(item.WrappingPipes);
                {
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        if (!IsMaybeLabelText(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
                        var lst = labellinesf(label);
                        if (lst.Count == THESAURUSACCESSION)
                        {
                            var labelline = lst[QUOTATIONSHAKES];
                            if (pipesf(GeoFac.CreateGeometry(label, labelline)).Count == QUOTATIONSHAKES)
                            {
                                var lines = ExplodeGLineSegments(labelline);
                                var points = GeoFac.GetLabelLineEndPoints(lines.Distinct(new GLineSegment.EqualityComparer(THESAURUSACCUSE)).ToList(), label, radius: THESAURUSACCUSE);
                                if (points.Count == THESAURUSACCESSION)
                                {
                                    var pt = points[QUOTATIONSHAKES];
                                    var r = GRect.Create(pt, THESAURUSABSOLUTE);
                                    geoData.VerticalPipes.Add(r);
                                    var pl = r.ToPolygon();
                                    cadDataMain.VerticalPipes.Add(pl);
                                    item.VerticalPipes.Add(pl);
                                    DrawTextLazy(THESAURUSADDICTED, pl.GetCenter());
                                }
                            }
                        }
                    }
                }
                foreach (var o in item.LabelLines)
                {
                    DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = THESAURUSACCESSION;
                }
                foreach (var pl in item.Labels)
                {
                    var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                    var e = DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = THESAURUSACCIDENT;
                    var _pl = DrawRectLazy(m.Boundary);
                    _pl.ColorIndex = THESAURUSACCIDENT;
                }
                foreach (var o in item.PipeKillers)
                {
                    DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(THESAURUSADJACENT, THESAURUSADJACENT, THESAURUSABSOLUTE);
                }
                foreach (var o in item.WashingMachines)
                {
                    DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)], THESAURUSACCLAIM);
                }
                foreach (var o in item.VerticalPipes)
                {
                    DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = THESAURUSACCUSTOMED;
                }
                foreach (var o in item.FloorDrains)
                {
                    DrawGeometryLazy(o, ents => ents.ForEach(e => e.ColorIndex = QUOTATIONSPENSER));
                }
                foreach (var o in item.WaterPorts)
                {
                    DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = THESAURUSADAPTATION;
                    DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WashingMachines)
                {
                    var e = DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = THESAURUSACCESSION;
                }
                foreach (var o in item.CleaningPorts)
                {
                    var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                    DrawRectLazy(GRect.Create(m, INTENSIFICATION));
                }
                {
                    var cl = Color.FromRgb(THESAURUSADJOURNMENT, DISCONTINUATION, THESAURUSADJUDICATE);
                    foreach (var o in item.WrappingPipes)
                    {
                        var e = DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(THESAURUSACCUSTOM, FAMILIARIZATION, THESAURUSADDENDUM);
                    foreach (var o in item.DLines)
                    {
                        var e = DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(THESAURUSADJUDICATION, THESAURUSADJUNCT, THESAURUSADJUST);
                    foreach (var o in item.VLines)
                    {
                        DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
                    }
                }
                {
                    {
                        var ok_ents = new HashSet<Geometry>();
                        for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                        {
                            var ok = THESAURUSABDOMEN;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == THESAURUSACCESSION && pipes.Count == THESAURUSACCESSION)
                                {
                                    var lb = labels[QUOTATIONSHAKES];
                                    var pp = pipes[QUOTATIONSHAKES];
                                    var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSACCEPTABLE;
                                    if (IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = THESAURUSABDOMINAL;
                                    }
                                    else if (IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
                                        ok_ents.Add(lb);
                                        ok = THESAURUSABDOMINAL;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                        {
                            var ok = THESAURUSABDOMEN;
                            var labelsf = F(item.Labels.Except(ok_ents).ToList());
                            var pipesf = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var labelLinesGeo in labelLinesGeos)
                            {
                                var labels = labelsf(labelLinesGeo);
                                var pipes = pipesf(labelLinesGeo);
                                if (labels.Count == pipes.Count && labels.Count > QUOTATIONSHAKES)
                                {
                                    var labelsTxts = labels.Select(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text ?? THESAURUSACCEPTABLE).ToList();
                                    if (labelsTxts.All(txt => IsMaybeLabelText(txt)))
                                    {
                                        pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(pipes).ToList();
                                        labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(labels).ToList();
                                        for (int k = QUOTATIONSHAKES; k < pipes.Count; k++)
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
                                        ok = THESAURUSABDOMINAL;
                                    }
                                }
                            }
                            if (!ok) break;
                        }
                        {
                            foreach (var label in item.Labels.Except(ok_ents).ToList())
                            {
                                var lb = geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text ?? THESAURUSACCEPTABLE;
                                if (!IsMaybeLabelText(lb)) continue;
                                var lst = labellinesf(label);
                                if (lst.Count == THESAURUSACCESSION)
                                {
                                    var labelline = lst[QUOTATIONSHAKES];
                                    var lines = ExplodeGLineSegments(labelline);
                                    var points = GeoFac.GetLabelLineEndPoints(lines, label);
                                    if (points.Count == THESAURUSACCESSION)
                                    {
                                        var pipes = F(item.VerticalPipes.Except(lbDict.Keys).ToList())(points[QUOTATIONSHAKES].ToNTSPoint());
                                        if (pipes.Count == THESAURUSACCESSION)
                                        {
                                            var pp = pipes[QUOTATIONSHAKES];
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
                            var ok = THESAURUSABDOMEN;
                            for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                var pipes2f = F(pipes2);
                                foreach (var dlinesGeo in dlinesGeos)
                                {
                                    var lst1 = pipes1f(dlinesGeo);
                                    var lst2 = pipes2f(dlinesGeo);
                                    if (lst1.Count == THESAURUSACCESSION && lst2.Count > QUOTATIONSHAKES)
                                    {
                                        var pp1 = lst1[QUOTATIONSHAKES];
                                        var label = lbDict[pp1];
                                        var c = pp1.GetCenter();
                                        foreach (var pp2 in lst2)
                                        {
                                            var dis = c.GetDistanceTo(pp2.GetCenter());
                                            if (THESAURUSACCLAIM < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                if (!IsTL(label))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = THESAURUSABDOMINAL;
                                                }
                                            }
                                            else if (dis > MAX_SHORTTRANSLATOR_DISTANCE)
                                            {
                                                longTranslatorLabels.Add(label);
                                                lbDict[pp2] = label;
                                                ok = THESAURUSABDOMINAL;
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
                            var ok = THESAURUSABDOMEN;
                            for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
                            {
                                var (pipes1, pipes2) = getPipes();
                                var pipes1f = F(pipes1);
                                foreach (var pp2 in pipes2)
                                {
                                    var pps1 = pipes1f(pp2.ToGRect().Expand(THESAURUSACCUSE).ToGCircle(THESAURUSABDOMEN).ToCirclePolygon(QUOTATIONSPENSER));
                                    var fs = new List<Action>();
                                    foreach (var pp1 in pps1)
                                    {
                                        var label = lbDict[pp1];
                                        if (!IsTL(label))
                                        {
                                            if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > THESAURUSACCESSION)
                                            {
                                                fs.Add(() =>
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = THESAURUSABDOMINAL;
                                                });
                                            }
                                        }
                                    }
                                    if (fs.Count == THESAURUSACCESSION) fs[QUOTATIONSHAKES]();
                                }
                                if (!ok) break;
                            }
                            return ok;
                        }
                        for (int i = QUOTATIONSHAKES; i < THESAURUSACCUSTOMED; i++)
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
                        foreach (var label in d.Where(x => x.Value > THESAURUSACCESSION).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == THESAURUSACCIDENT)
                            {
                                var dis = pps[QUOTATIONSHAKES].GetCenter().GetDistanceTo(pps[THESAURUSACCESSION].GetCenter());
                                if (THESAURUSACCLAIM < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
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
                var pipeToFloorDrainGeoD = new Dictionary<Geometry, List<Geometry>>();
                {
                    var floorDrainD = new Dictionary<string, int>();
                    {
                        var ok_fds = new HashSet<Geometry>();
                        var dlinesGeosf = F(dlinesGeos);
                        {
                            var fdsf = F(item.FloorDrains);
                            foreach (var pipe in item.VerticalPipes)
                            {
                                var label = getLabel(pipe);
                                if (label != null)
                                {
                                    var dlines = dlinesGeosf(pipe);
                                    if (dlines.Any())
                                    {
                                        var pts = GeoFac.ToNodedLineSegments(dlines.SelectMany(x => GeoFac.GetLines(x)).ToList()).SelectMany(x => new Point2d[] { x.StartPoint, x.EndPoint }).ToList();
                                        var fds = fdsf(GeoFac.CreateGeometry(pts.Select(x => x.ToNTSPoint())));
                                        ok_fds.AddRange(fds);
                                        floorDrainD[label] = fds.Count;
                                        pipeToFloorDrainGeoD[pipe] = fds;
                                    }
                                }
                            }
                        }
                    }
                    {
                    }
                    drData.FloorDrains = floorDrainD;
                }
                {
                    var f = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                    var hs = new HashSet<string>();
                    var gs = GeoFac.GroupGeometries(dlinesGeos.Concat(item.CleaningPorts).ToList());
                    foreach (var g in gs)
                    {
                        var dlines = g.Where(pl => dlinesGeos.Contains(pl)).ToList();
                        var ports = g.Where(pl => item.CleaningPorts.Contains(pl)).ToList();
                        if (!AllNotEmpty(ports, dlines)) continue;
                        var pipes = f(GeoFac.CreateGeometry(ports.Concat(dlines).ToList()));
                        hs.AddRange(pipes.Select(getLabel).Where(lb => lb != null));
                    }
                    drData.CleaningPorts.AddRange(hs);
                }
                {
                    var ok_ents = new HashSet<Geometry>();
                    var outletd = new Dictionary<string, string>();
                    var has_wrappingpipes = new HashSet<string>();
                    var portd = new Dictionary<Geometry, string>();
                    {
                        void collect(Func<Geometry, List<Geometry>> waterPortsf, Func<Geometry, string> getWaterPortLabel)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var waterPorts = waterPortsf(dlinesGeo);
                                if (waterPorts.Count == THESAURUSACCESSION)
                                {
                                    var waterPort = waterPorts[QUOTATIONSHAKES];
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
                                            if (wrappingpipes.Count > QUOTATIONSHAKES)
                                            {
                                                has_wrappingpipes.Add(label);
                                            }
                                            foreach (var wp in wrappingpipes)
                                            {
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
                            var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(ACHONDROPLASTIC).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
                        }
                    }
                    {
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = THESAURUSACCLAIM;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(QUOTATIONSPENSER, THESAURUSABDOMEN)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterPort = f5(pt.ToPoint3d());
                                if (waterPort != null)
                                {
                                    if (waterPort.GetCenter().GetDistanceTo(pt) <= ADRENOCORTICOTROPHIC)
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
                                                    has_wrappingpipes.Add(label);
                                                }
                                                foreach (var wp in wrappingpipes)
                                                {
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
                        drData.Outlets = outletd;
                        outletd.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                        {
                            var num = kv1.Value;
                            var pipe = kv2.Key;
                            DrawTextLazy(num, pipe.ToGRect().RightButtom);
                            return THESAURUSADDITIONAL;
                        }).Count();
                    }
                    {
                        drData.WrappingPipes.AddRange(has_wrappingpipes);
                    }
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
                    var pls = new List<Geometry>();
                    var dls = new List<Geometry>();
                    var tls = new List<Geometry>();
                    var fls = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (IsPL(kv.Value))
                        {
                            pls.Add(kv.Key);
                        }
                        else if (IsDL(kv.Value))
                        {
                            dls.Add(kv.Key);
                        }
                        else if (IsTL(kv.Value))
                        {
                            tls.Add(kv.Key);
                        }
                        else if (IsFL(kv.Value))
                        {
                            fls.Add(kv.Key);
                        }
                    }
                    var lst = WLGeoGrouper.ToiletGrouper.DoGroup(pls, tls, dls, fls);
                    var toiletGroupers = new List<WLGrouper.ToiletGrouper>();
                    foreach (var itm in lst)
                    {
                        var _pls = itm.PLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _tls = itm.TLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _dls = itm.DLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _fls = itm.FLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var m = new WLGrouper.ToiletGrouper()
                        {
                            WLType = itm.WLType,
                            PLs = _pls,
                            TLs = _tls,
                            DLs = _dls,
                            FLs = _fls,
                        };
                        m.Init();
                        toiletGroupers.Add(m);
                    }
                    drData.toiletGroupers.AddRange(toiletGroupers);
                }
                {
                    var f = F(lbDict.Where(kv => IsFL(kv.Value)).Select(x => x.Key).ToList());
                    var hs = new HashSet<string>();
                    foreach (var kv in roomData)
                    {
                        if (kv.Key == ALIMENTATIVENESS || kv.Key.Contains(THESAURUSALIMONY) || kv.Key.Contains(DIHYDROXYANTHRAQUINONE))
                        {
                            hs.AddRange(f(kv.Value).Select(x => lbDict[x]));
                        }
                    }
                    drData.WaterPipeWellFLs.AddRange(hs);
                }
                {
                    var FLs = new List<Geometry>();
                    var pls = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (IsFL(kv.Value))
                        {
                            FLs.Add(kv.Key);
                        }
                        else if (IsPL(kv.Value))
                        {
                            pls.Add(kv.Key);
                        }
                    }
                    var pts = GeoFac.GetAlivePoints(item.DLines.Select(cadDataMain.DLines).ToList(geoData.DLines), radius: THESAURUSACCUSE);
                    {
                        {
                            var plsf = F(pls);
                            var ok_toilets = new HashSet<Geometry>();
                            foreach (var toilet in toilets)
                            {
                                var ok = THESAURUSABDOMEN;
                                foreach (var pl in plsf(toilet.EnvelopeInternal.ToGRect().ToPolygon()))
                                {
                                    drData.SingleOutlet.Add(lbDict[pl]);
                                    ok = THESAURUSABDOMINAL;
                                }
                                if (ok) ok_toilets.Add(toilet);
                            }
                            foreach (var toilet in toilets.Except(ok_toilets))
                            {
                                foreach (var pl in plsf(toilet.Buffer(UNCHRONOLOGICAL)))
                                {
                                    drData.SingleOutlet.Add(lbDict[pl]);
                                }
                            }
                        }
                        {
                            var flsf = F(FLs);
                            var ok_kitchens = new HashSet<Geometry>();
                            foreach (var kitchen in kitchens)
                            {
                                var ok = THESAURUSABDOMEN;
                                foreach (var fl in flsf(kitchen.EnvelopeInternal.ToGRect().ToPolygon()))
                                {
                                    drData.SingleOutlet.Add(lbDict[fl]);
                                    ok = THESAURUSABDOMINAL;
                                }
                                if (ok) ok_kitchens.Add(kitchen);
                            }
                            foreach (var kitchen in kitchens.Except(ok_kitchens))
                            {
                                foreach (var fl in flsf(kitchen.Buffer(UNCHRONOLOGICAL)))
                                {
                                    drData.SingleOutlet.Add(lbDict[fl]);
                                }
                            }
                        }
                    }
                    drData.HasToilet = toilets.Count > QUOTATIONSHAKES;
                    {
                        var hasBasinList = new List<bool>();
                        var hasKitchenFloorDrainList = new List<bool>();
                        var hasKitchenWashingMachineList = new List<bool>();
                        var _fls1 = DrainageService.GetKitchenOnlyFLs(FLs, kitchens, nonames,
                            balconies, pts, item.DLines,
                            FLs.Select(x => lbDict[x]).ToList(),
                            item.Basins,
                            item.FloorDrains,
                            item.WashingMachines,
                            hasBasinList,
                            hasKitchenFloorDrainList,
                            hasKitchenWashingMachineList
                            );
                        for (int i = QUOTATIONSHAKES; i < _fls1.Count; i++)
                        {
                            var fl = _fls1[i];
                            var label = lbDict[fl];
                            drData.KitchenOnlyFls.Add(label);
                            if (hasBasinList[i])
                            {
                                drData.HasKitchenBasin.Add(label);
                            }
                            if (hasKitchenFloorDrainList[i])
                            {
                                drData.HasKitchenFloorDrain.Add(label);
                            }
                            if (hasKitchenWashingMachineList[i])
                            {
                                drData.HasKitchenWashingMachine.Add(label);
                            }
                        }
                        var _fls4 = DrainageService.GetFLsWhereSupportingFloorDrainUnderWaterPoint(FLs, kitchens, item.FloorDrains, item.WashingMachines);
                        foreach (var fl in _fls4)
                        {
                            var label = lbDict[fl];
                            drData.MustHaveFloorDrains.Add(label);
                            drData.Comments.Add(THESAURUSALLEGATION + fl);
                        }
                        var hasWashingMachineList = new List<bool>();
                        var floorDrainsCountList = new List<int>();
                        var hasMopPoolList = new List<bool>();
                        var isShuntList = new List<bool>();
                        List<Geometry> GetBalconyOnlyFLs(List<Geometry> fls, List<Geometry> balconies, List<Geometry> floorDrains)
                        {
                            var dlines = item.DLines;
                            var labels = fls.Select(x => lbDict[x]).ToList();
                            var washingMachines = item.WashingMachines;
                            var mopPools = item.MopPools;
                            var nearestBalconyf = GeoFac.NearestNeighbourGeometryF(balconies);
                            var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
                            var list = new List<Geometry>(fls.Count);
                            var washingMachinesf = GeoFac.CreateIntersectsSelector(washingMachines);
                            var floorDrainsf = GeoFac.CreateIntersectsSelector(floorDrains);
                            var mopPoolsf = GeoFac.CreateIntersectsSelector(mopPools);
                            {
                                var dlinesf = GeoFac.CreateIntersectsSelector(dlines);
                                var ok_fls = new HashSet<Geometry>();
                                foreach (var balcony in balconies)
                                {
                                    var flsf = GeoFac.CreateIntersectsSelector(fls.Except(ok_fls).ToList());
                                    var _fls = flsf(balcony);
                                    bool isShunt(Geometry fl)
                                    {
                                        var _dlines = dlinesf(fl);
                                        if (_dlines.Count == QUOTATIONSHAKES) return THESAURUSABDOMEN;
                                        var fds = floorDrainsf(GeoFac.CreateGeometry(_dlines));
                                        if (fds.Count == THESAURUSACCIDENT)
                                        {
                                            foreach (var geo in GeoFac.GroupGeometries(GeoFac.ToNodedLineSegments(GeoFac.GetLines(GeoFac.CreateGeometry(dlinesf(GeoFac.CreateGeometry(floorDrains.YieldAfter(fl).Distinct()))).Difference(fl)).ToList()).Select(x => x.ToLineString()).ToList()).Select(geos => GeoFac.CreateGeometry(geos)))
                                            {
                                                if (geo.Intersects(fds[QUOTATIONSHAKES]) && geo.Intersects(fds[THESAURUSACCESSION]))
                                                {
                                                    return THESAURUSABDOMEN;
                                                }
                                            }
                                            return THESAURUSABDOMINAL;
                                        }
                                        return THESAURUSABDOMEN;
                                    }
                                    void emit(Geometry fl)
                                    {
                                        list.Add(fl);
                                        ok_fls.Add(fl);
                                        hasWashingMachineList.Add(washingMachinesf(balcony).Any());
                                        floorDrainsCountList.Add(floorDrainsf(balcony).Count);
                                        hasMopPoolList.Add(mopPoolsf(balcony).Any());
                                        isShuntList.Add(isShunt(fl));
                                    }
                                    if (_fls.Count > QUOTATIONSHAKES)
                                    {
                                        foreach (var fl in _fls)
                                        {
                                            emit(fl);
                                        }
                                    }
                                    else
                                    {
                                        _fls = flsf(balcony.Buffer(ACCOMMODATIONAL));
                                        if (_fls.Count > QUOTATIONSHAKES)
                                        {
                                            var fl = GeoFac.NearestNeighbourGeometryF(_fls)(balcony);
                                            emit(fl);
                                        }
                                    }
                                }
                                return list;
                            }
                            for (int i = QUOTATIONSHAKES; i < fls.Count; i++)
                            {
                                var fl = fls[i];
                                var lb = labels[i];
                                List<Point2d> endpoints = null;
                                Geometry endpointsGeo = null;
                                List<Point2d> _GetEndPoints()
                                {
                                    return GetEndPoints(fl, pts, dlines);
                                }
                                bool test1()
                                {
                                    endpoints ??= _GetEndPoints();
                                    if (endpoints.Count == QUOTATIONSHAKES) return THESAURUSABDOMEN;
                                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                                    return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
                                }
                                bool test2()
                                {
                                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), THESAURUSACQUISITIVE, INTERDIGITATING).Intersects(kitchensGeo);
                                }
                                bool test3()
                                {
                                    endpoints ??= _GetEndPoints();
                                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                                    return !kitchensGeo.Intersects(endpointsGeo);
                                }
                                if (test1() && (test2() || test3()))
                                {
                                    list.Add(fl);
                                    var bal = nearestBalconyf(fl);
                                    if (bal == null)
                                    {
                                        hasWashingMachineList.Add(THESAURUSABDOMEN);
                                        floorDrainsCountList.Add(QUOTATIONSHAKES);
                                    }
                                    else
                                    {
                                        hasWashingMachineList.Add(washingMachinesf(bal).Any());
                                        floorDrainsCountList.Add(floorDrainsf(bal).Count);
                                    }
                                }
                            }
                            return list;
                        }
                        var ok_fls = new HashSet<Geometry>();
                        var ok_bals = new HashSet<Geometry>();
                        var ok_fds = new HashSet<Geometry>();
                        var flsf = F(FLs);
                        var fdsf = F(item.FloorDrains);
                        var pipesf = F(item.VerticalPipes);
                        var washingMachinesf = F(item.WashingMachines);
                        foreach (var bal in balconies)
                        {
                            var washingMachine = washingMachinesf(bal).FirstOrDefault();
                            if (washingMachine is null) continue;
                            var fds = fdsf(bal);
                            if (fds.Count == THESAURUSACCIDENT)
                            {
                                fds = GeoFac.NearestNeighboursPoint3dF(fds)(washingMachine.GetCenter().ToPoint3d(), THESAURUSACCIDENT);
                                Geometry fl1 = null, fl2 = null;
                                foreach (var fl in FLs)
                                {
                                    if (pipeToFloorDrainGeoD.TryGetValue(fl, out List<Geometry> _fds))
                                    {
                                        if (_fds.Contains(fds[QUOTATIONSHAKES]))
                                        {
                                            fl1 = fl;
                                        }
                                        else if (_fds.Contains(fds[THESAURUSACCESSION]))
                                        {
                                            fl2 = fl;
                                        }
                                        if (fl1 != null && fl2 != null) break;
                                    }
                                }
                                if (fl1 != null && fl2 != null)
                                {
                                    var lb1 = lbDict[fl1];
                                    var lb2 = lbDict[fl2];
                                    drData.BalconyOnlyFLs.Add(lb1);
                                    drData.BalconyOnlyFLs.Add(lb2);
                                    drData.HasBalconyWashingMachine.Add(lb1);
                                    drData.HasNonBalconyWashingMachineFloorDrain.Add(lb2);
                                    int count;
                                    drData.FloorDrains.TryGetValue(lb1, out count);
                                    drData.FloorDrains[lb1] = Math.Max(THESAURUSACCESSION, count);
                                    drData.FloorDrains.TryGetValue(lb2, out count);
                                    drData.FloorDrains[lb2] = Math.Max(THESAURUSACCESSION, count);
                                    ok_bals.Add(bal);
                                    ok_fds.AddRange(fds);
                                    ok_fls.Add(fl1);
                                    ok_fls.Add(fl2);
                                    continue;
                                }
                            }
                        }
                        {
                            var _fls2 = GetBalconyOnlyFLs(FLs.Except(ok_fls).ToList(), balconies.Except(ok_bals).ToList(), item.FloorDrains.Except(ok_fds).ToList());
                            for (int i = QUOTATIONSHAKES; i < _fls2.Count; i++)
                            {
                                var fl = _fls2[i];
                                var label = lbDict[fl];
                                var hasWashingMachine = hasWashingMachineList[i];
                                var floorDrainsCount = floorDrainsCountList[i];
                                var hasMopPool = hasMopPoolList[i];
                                drData.BalconyOnlyFLs.Add(label);
                                drData.FloorDrains.TryGetValue(label, out int count);
                                drData.FloorDrains[label] = Math.Max(floorDrainsCount, count);
                                if (hasWashingMachine)
                                {
                                    drData.HasBalconyWashingMachine.Add(label);
                                    if (drData.FloorDrains[label] == QUOTATIONSHAKES) drData.FloorDrains[label] = THESAURUSACCESSION;
                                }
                                if (hasMopPool) drData.HasMopPool.Add(label);
                                if (isShuntList[i]) drData.Shunts.Add(label);
                            }
                        }
                        {
                            var _dlinesGeos = GG(item.DLines.Concat(item.DownWaterPorts.OfType<Polygon>().Select(x => x.Shell)).Distinct().ToList()).Select(G).ToList();
                            IEnumerable<Geometry> getFlPipes()
                            {
                                foreach (var kv in lbDict)
                                {
                                    if (IsFL(kv.Value))
                                    {
                                        yield return kv.Key;
                                    }
                                }
                            }
                            var vpsf = F(getFlPipes().ToList());
                            foreach (var dlinesGeo in _dlinesGeos)
                            {
                                var pipes = vpsf(dlinesGeo);
                                if (pipes.Count > QUOTATIONSHAKES)
                                {
                                    if (fdsf(dlinesGeo).Count > QUOTATIONSHAKES)
                                    {
                                        foreach (var pipe in pipes)
                                        {
                                            drData.SureWashingMachineFloorDrain.Add(lbDict[pipe]);
                                        }
                                    }
                                }
                            }
                        }
                        var _fls3 = DrainageService.GetKitchenAndBalconyBothFLs(FLs, kitchens, nonames, balconies, pts, item.DLines);
                        foreach (var fl in _fls3)
                        {
                            var label = lbDict[fl];
                            drData.KitchenAndBalconyFLs.Add(label);
                        }
                        foreach (var fl in _fls3)
                        {
                            var label = lbDict[fl];
                            drData.MustHaveFloorDrains.Add(label);
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
            _DrawingTransaction.Current.AbleToDraw = THESAURUSABDOMINAL;
        }
        const double MAX_SHORTTRANSLATOR_DISTANCE = THESAURUSACCURATE;
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
                    if (lst.Count > QUOTATIONSHAKES)
                    {
                        if (f(kitchen).Count > QUOTATIONSHAKES)
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
                    if (ls.NumPoints == THESAURUSACCIDENT) yield return new GLineSegment(ls[QUOTATIONSHAKES].ToPoint2d(), ls[THESAURUSACCESSION].ToPoint2d());
                    else if (ls.NumPoints > THESAURUSACCIDENT)
                    {
                        for (int i = QUOTATIONSHAKES; i < ls.NumPoints - THESAURUSACCESSION; i++)
                        {
                            yield return new GLineSegment(ls[i].ToPoint2d(), ls[i + THESAURUSACCESSION].ToPoint2d());
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
            var pts = points.Select(x => new GCircle(x, THESAURUSACCUSE).ToCirclePolygon(QUOTATIONSPENSER)).ToList();
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
                    return GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), ACCOMMODATIONAL, INTERDIGITATING).Intersects(kitchensGeo);
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
                    if (fls.Count > QUOTATIONSHAKES)
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
                        fls = flsf(kitchen.Buffer(ACCOMMODATIONAL));
                        if (fls.Count > QUOTATIONSHAKES)
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
            for (int i = QUOTATIONSHAKES; i < FLs.Count; i++)
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
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter(), ACCOMMODATIONAL, INTERDIGITATING));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    if (endpoints.Count == QUOTATIONSHAKES) return THESAURUSABDOMINAL;
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
    public class WLGeoGrouper
    {
        public class ToiletGrouper
        {
            public List<Geometry> PLs;
            public List<Geometry> TLs;
            public List<Geometry> DLs;
            public List<Geometry> FLs;
            public WLType WLType;
            public void Init()
            {
                PLs ??= new List<Geometry>();
                TLs ??= new List<Geometry>();
                DLs ??= new List<Geometry>();
                FLs ??= new List<Geometry>();
            }
            public static List<ToiletGrouper> DoGroup(List<Geometry> PLs, List<Geometry> TLs, List<Geometry> DLs, List<Geometry> FLs)
            {
                var list = new List<ToiletGrouper>();
                var ok_pipes = new HashSet<Geometry>();
                foreach (var pl in PLs)
                {
                    ok_pipes.Add(pl);
                    var range = GeoFac.CreateCirclePolygon(pl.GetCenter(), THESAURUSACCURATE, THESAURUSALLIANCE);
                    var tls = GeoFac.CreateIntersectsSelector(TLs.Except(ok_pipes).ToList())(range);
                    ok_pipes.AddRange(tls);
                    var dls = GeoFac.CreateIntersectsSelector(DLs.Except(ok_pipes).ToList())(range);
                    ok_pipes.AddRange(dls);
                    var o = new ToiletGrouper();
                    list.Add(o);
                    o.Init();
                    o.PLs.Add(pl);
                    o.TLs.AddRange(tls);
                    o.DLs.AddRange(dls);
                    if (tls.Count == QUOTATIONSHAKES && dls.Count == QUOTATIONSHAKES)
                    {
                        o.WLType = WLType.PL;
                    }
                    else if (tls.Count > QUOTATIONSHAKES && dls.Count > QUOTATIONSHAKES)
                    {
                        o.WLType = WLType.PL_TL_DL;
                    }
                    else if (tls.Count > QUOTATIONSHAKES && dls.Count == QUOTATIONSHAKES)
                    {
                        o.WLType = WLType.PL_TL;
                    }
                    else if (tls.Count == QUOTATIONSHAKES && dls.Count > QUOTATIONSHAKES)
                    {
                        o.WLType = WLType.PL_DL;
                    }
                    else
                    {
                        throw new System.Exception(nameof(ToiletGrouper));
                    }
                }
                foreach (var fl in FLs)
                {
                    ok_pipes.Add(fl);
                    var o = new ToiletGrouper();
                    list.Add(o);
                    o.Init();
                    o.FLs.Add(fl);
                    var range = GeoFac.CreateCirclePolygon(fl.GetCenter(), THESAURUSACCURATE, THESAURUSALLIANCE);
                    var tls = GeoFac.CreateIntersectsSelector(TLs.Except(ok_pipes).ToList())(range);
                    ok_pipes.AddRange(tls);
                    if (tls.Count == QUOTATIONSHAKES)
                    {
                        o.WLType = WLType.FL;
                    }
                    else
                    {
                        o.WLType = WLType.FL_TL;
                    }
                }
                return list;
            }
        }
    }
    public class DrainageDrawingData
    {
        public HashSet<string> VerticalPipeLabels;
        public HashSet<string> LongTranslatorLabels;
        public HashSet<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public HashSet<string> CleaningPorts;
        public Dictionary<string, string> Outlets;
        public HashSet<string> WrappingPipes;
        public List<WLGrouper.ToiletGrouper> toiletGroupers;
        public HashSet<string> KitchenOnlyFls;
        public HashSet<string> BalconyOnlyFLs;
        public HashSet<string> KitchenAndBalconyFLs;
        public HashSet<string> MustHaveCleaningPort;
        public HashSet<string> WaterPipeWellFLs;
        public HashSet<string> MustHaveFloorDrains;
        public HashSet<string> SingleOutlet;
        public bool HasToilet;
        public List<string> Comments;
        public HashSet<string> HasKitchenBasin;
        public HashSet<string> HasKitchenFloorDrain;
        public HashSet<string> HasKitchenWashingMachine;
        public HashSet<string> HasBalconyWashingMachine;
        public HashSet<string> HasNonBalconyWashingMachineFloorDrain;
        public HashSet<string> HasMopPool;
        public HashSet<string> Shunts;
        public HashSet<string> SureWashingMachineFloorDrain;
        public HashSet<string> SureMopPoolFloorDrain;
        public void Init()
        {
            VerticalPipeLabels ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            CleaningPorts ??= new HashSet<string>();
            Outlets ??= new Dictionary<string, string>();
            WrappingPipes ??= new HashSet<string>();
            toiletGroupers ??= new List<WLGrouper.ToiletGrouper>();
            KitchenOnlyFls ??= new HashSet<string>();
            BalconyOnlyFLs ??= new HashSet<string>();
            KitchenAndBalconyFLs ??= new HashSet<string>();
            MustHaveCleaningPort ??= new HashSet<string>();
            MustHaveFloorDrains ??= new HashSet<string>();
            WaterPipeWellFLs ??= new HashSet<string>();
            Comments ??= new List<string>();
            SingleOutlet ??= new HashSet<string>();
            HasKitchenFloorDrain ??= new HashSet<string>();
            HasKitchenWashingMachine ??= new HashSet<string>();
            HasKitchenBasin ??= new HashSet<string>();
            HasBalconyWashingMachine ??= new HashSet<string>();
            HasNonBalconyWashingMachineFloorDrain ??= new HashSet<string>();
            HasMopPool ??= new HashSet<string>();
            Shunts ??= new HashSet<string>();
            SureWashingMachineFloorDrain ??= new HashSet<string>();
            SureMopPoolFloorDrain ??= new HashSet<string>();
        }
    }
    public class DrainageGeoData
    {
        public List<GRect> Storeys;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public List<GLineSegment> DLines;
        public List<GLineSegment> VLines;
        public List<GRect> VerticalPipes;
        public List<GRect> WrappingPipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterPorts;
        public List<string> WaterPortLabels;
        public List<GRect> WashingMachines;
        public List<GRect> Basins;
        public List<GRect> MopPools;
        public List<Point2d> CleaningPorts;
        public List<Point2d> SideFloorDrains;
        public List<GRect> PipeKillers;
        public List<GRect> DownWaterPorts;
        public void Init()
        {
            Storeys ??= new List<GRect>();
            Labels ??= new List<CText>();
            LabelLines ??= new List<GLineSegment>();
            DLines ??= new List<GLineSegment>();
            VLines ??= new List<GLineSegment>();
            VerticalPipes ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterPorts ??= new List<GRect>();
            WaterPortLabels ??= new List<string>();
            WashingMachines ??= new List<GRect>();
            Basins ??= new List<GRect>();
            CleaningPorts ??= new List<Point2d>();
            SideFloorDrains ??= new List<Point2d>();
            PipeKillers ??= new List<GRect>();
            MopPools ??= new List<GRect>();
            DownWaterPorts ??= new List<GRect>();
        }
        public void FixData()
        {
            Init();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.IsValid).Distinct().ToList();
            DLines = DLines.Where(x => x.IsValid).Distinct().ToList();
            VLines = VLines.Where(x => x.IsValid).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            DownWaterPorts = DownWaterPorts.Where(x => x.IsValid).Distinct().ToList();
            {
                var d = new Dictionary<GRect, string>();
                for (int i = QUOTATIONSHAKES; i < WaterPorts.Count; i++)
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
            WashingMachines = WashingMachines.Where(x => x.IsValid).Distinct().ToList();
            Basins = Basins.Where(x => x.IsValid).Distinct().ToList();
            MopPools = MopPools.Where(x => x.IsValid).Distinct().ToList();
            PipeKillers = PipeKillers.Where(x => x.IsValid).Distinct().ToList();
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
        public List<Geometry> WashingMachines;
        public List<Geometry> CleaningPorts;
        public List<Geometry> SideFloorDrains;
        public List<Geometry> PipeKillers;
        public List<Geometry> Basins;
        public List<Geometry> MopPools;
        public List<Geometry> DownWaterPorts;
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
            WashingMachines ??= new List<Geometry>();
            CleaningPorts ??= new List<Geometry>();
            SideFloorDrains ??= new List<Geometry>();
            PipeKillers ??= new List<Geometry>();
            Basins ??= new List<Geometry>();
            MopPools ??= new List<Geometry>();
            DownWaterPorts ??= new List<Geometry>();
        }
        public static DrainageCadData Create(DrainageGeoData data)
        {
            var bfSize = THESAURUSACCLAIM;
            var o = new DrainageCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            if (THESAURUSABDOMEN) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (THESAURUSABDOMEN) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
            o.Basins.AddRange(data.Basins.Select(ConvertWashingMachinesF()));
            o.MopPools.AddRange(data.MopPools.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            o.PipeKillers.AddRange(data.PipeKillers.Select(ConvertVerticalPipesF()));
            o.DownWaterPorts.AddRange(data.DownWaterPorts.Select(ConvertVerticalPipesF()));
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
            return x => new GCircle(x, INTENSIFICATION).ToCirclePolygon(INTERDIGITATING);
        }
        public static Func<GRect, Polygon> ConvertWashingMachinesF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertWaterPortsLargerF()
        {
            return x => x.Center.ToGCircle(ADRENOCORTICOTROPHIC).ToCirclePolygon(QUOTATIONSPENSER);
        }
        private static Func<GRect, Polygon> ConvertWaterPortsF()
        {
            return x => x.ToPolygon();
        }
        public static Func<GRect, Polygon> ConvertFloorDrainsF()
        {
            return x => x.ToGCircle(THESAURUSABDOMINAL).ToCirclePolygon(INTERDIGITATING);
        }
        public static Func<GRect, Polygon> ConvertVerticalPipesF()
        {
            return x => x.ToPolygon();
        }
        private static Func<GRect, Polygon> ConvertVerticalPipesPreciseF()
        {
            return x => new GCircle(x.Center, x.InnerRadius).ToCirclePolygon(INTERDIGITATING);
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
            return x => x.Extend(THESAURUSABANDON).ToLineString();
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(THESAURUSABANDONED);
            ret.AddRange(Storeys);
            ret.AddRange(Labels);
            ret.AddRange(LabelLines);
            ret.AddRange(DLines);
            ret.AddRange(VLines);
            ret.AddRange(VerticalPipes);
            ret.AddRange(WrappingPipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterPorts);
            ret.AddRange(WashingMachines);
            ret.AddRange(CleaningPorts);
            ret.AddRange(SideFloorDrains);
            ret.AddRange(PipeKillers);
            ret.AddRange(Basins);
            ret.AddRange(MopPools);
            ret.AddRange(DownWaterPorts);
            return ret;
        }
        public List<DrainageCadData> SplitByStorey()
        {
            var lst = new List<DrainageCadData>(this.Storeys.Count);
            if (this.Storeys.Count == QUOTATIONSHAKES) return lst;
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
                o.WashingMachines.AddRange(objs.Where(x => this.WashingMachines.Contains(x)));
                o.CleaningPorts.AddRange(objs.Where(x => this.CleaningPorts.Contains(x)));
                o.SideFloorDrains.AddRange(objs.Where(x => this.SideFloorDrains.Contains(x)));
                o.PipeKillers.AddRange(objs.Where(x => this.PipeKillers.Contains(x)));
                o.Basins.AddRange(objs.Where(x => this.Basins.Contains(x)));
                o.MopPools.AddRange(objs.Where(x => this.MopPools.Contains(x)));
                o.DownWaterPorts.AddRange(objs.Where(x => this.DownWaterPorts.Contains(x)));
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
            floorStr = floorStr.Replace(THESAURUSAMPLIFY, THESAURUSAMPUTATE).Replace(APPROACHABILITY, ELECTROMAGNETISM).Replace(UNINTENTIONALLY, THESAURUSACCEPTABLE).Replace(ELECTRODYNAMICS, THESAURUSACCEPTABLE).Replace(THESAURUSAMUSEMENT, THESAURUSACCEPTABLE);
            var hs = new HashSet<int>();
            foreach (var s in floorStr.Split(THESAURUSAMPUTATE))
            {
                if (string.IsNullOrEmpty(s)) continue;
                var m = Regex.Match(s, QUOTATIONAMYGDALOID);
                if (m.Success)
                {
                    var v1 = int.Parse(m.Groups[THESAURUSACCESSION].Value);
                    var v2 = int.Parse(m.Groups[THESAURUSACCIDENT].Value);
                    var min = Math.Min(v1, v2);
                    var max = Math.Max(v1, v2);
                    for (int i = min; i <= max; i++)
                    {
                        hs.Add(i);
                    }
                    continue;
                }
                m = Regex.Match(s, POLYSACCHARIDES);
                if (m.Success)
                {
                    hs.Add(int.Parse(m.Value));
                }
            }
            hs.Remove(QUOTATIONSHAKES);
            return hs.OrderBy(x => x).ToList();
        }
        public static StoreyInfo GetStoreyInfo(BlockReference br)
        {
            var props = br.DynamicBlockReferencePropertyCollection;
            return new StoreyInfo()
            {
                StoreyType = GetStoreyType((string)props.GetValue(AMPHITHEATRICAL)),
                Numbers = ParseFloorNums(GetStoreyNumberString(br)),
                ContraPoint = GetContraPoint(br),
                Boundary = br.Bounds.ToGRect(),
            };
        }
        public static string GetStoreyNumberString(BlockReference br)
        {
            var d = br.ObjectId.GetAttributesInBlockReference(THESAURUSABDOMINAL);
            d.TryGetValue(DEDIFFERENTIATION, out string ret);
            return ret;
        }
        public static List<BlockReference> GetStoreyBlockReferences(AcadDatabase adb) => adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() is ALTERNATIVENESS && x.IsDynamicBlock).ToList();
        public static Point2d GetContraPoint(BlockReference br)
        {
            double dx = double.NaN;
            double dy = double.NaN;
            Point2d pt;
            foreach (DynamicBlockReferenceProperty p in br.DynamicBlockReferencePropertyCollection)
            {
                if (p.PropertyName == THESAURUSALMANAC)
                {
                    dx = Convert.ToDouble(p.Value);
                }
                else if (p.PropertyName == THESAURUSALMIGHTY)
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
                throw new System.Exception(THESAURUSALMOST);
            }
            return pt;
        }
        public static string FixVerticalPipeLabel(string label)
        {
            if (label == null) return null;
            if (label.StartsWith(THESAURUSADMINISTRATION))
            {
                return label.Substring(THESAURUSACCUSTOMED);
            }
            if (label.StartsWith(ADMINISTRATIVUS))
            {
                return label.Substring(THESAURUSACCIDENT);
            }
            return label;
        }
        public static bool IsNotedLabel(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return label.Contains(ADMINISTRATIVELY) || label.Contains(THESAURUSAIRING);
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label);
        }
        public static bool IsY1L(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return label.StartsWith(THESAURUSABILITY);
        }
        public static bool IsY2L(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return label.StartsWith(ABITURIENTENEXAMEN);
        }
        public static bool IsNL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return label.StartsWith(THESAURUSABJECT);
        }
        public static bool IsYL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return label.StartsWith(THESAURUSABJURE);
        }
        public static bool IsRainLabel(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label);
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            static bool f(string label)
            {
                return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
            }
            return f(FixVerticalPipeLabel(label));
        }
        public const int QUOTATIONSHAKES = 0;
        public const int INTENSIFICATION = 40;
        public const int INTERDIGITATING = 36;
        public const int ADRENOCORTICOTROPHIC = 1500;
        public const int QUOTATIONSPENSER = 6;
        public const double THESAURUSABANDON = .1;
        public const int THESAURUSABANDONED = 4096;
        public const string THESAURUSABASEMENT = "卫生间";
        public const string THESAURUSABATEMENT = "主卫";
        public const string THESAURUSABBREVIATE = "公卫";
        public const string THESAURUSABBREVIATION = "次卫";
        public const string THESAURUSABDICATE = "客卫";
        public const string THESAURUSABDICATION = "洗手间";
        public const bool THESAURUSABDOMEN = false;
        public const bool THESAURUSABDOMINAL = true;
        public const string THESAURUSABDUCT = "卫";
        public const string THESAURUSABERRANT = @"^[卫]\d$";
        public const string THESAURUSABERRATION = "厨房";
        public const string REFRANGIBILITIES = "西厨";
        public const string THESAURUSABEYANCE = "厨";
        public const string THESAURUSABHORRENT = "阳台";
        public const string THESAURUSABIDING = "连廊";
        public const string THESAURUSABILITY = "Y1L";
        public const string ABITURIENTENEXAMEN = "Y2L";
        public const string THESAURUSABJECT = "NL";
        public const string THESAURUSABJURE = "YL";
        public const string THESAURUSABLAZE = @"^W\d?L";
        public const string THESAURUSABLUTION = @"^F\d?L";
        public const string THESAURUSABNEGATION = "-0";
        public const string THESAURUSABNORMAL = @"^P\d?L";
        public const string PROGNOSTICATION = @"^T\d?L";
        public const string THESAURUSABOLISH = @"^D\d?L";
        public const string THESAURUSABORTIVE = "W-DRAI-NOTE";
        public const string THESAURUSABRASIVE = "W-DRAI-DIMS";
        public const int THESAURUSABRUPT = 8000;
        public const string THESAURUSABSENCE = "W-RAIN-EQPM";
        public const string THESAURUSABSENT = "W-DRAI-EQPM";
        public const int THESAURUSABSOLUTE = 55;
        public const string THESAURUSABSOLUTION = "清扫口系统";
        public const string THESAURUSABSORB = "W-RAIN-PIPE";
        public const string QUOTATIONABSORBENT = "W-RAIN-NOTE";
        public const string THESAURUSABSTAIN = "W-DRAI-DOME-PIPE";
        public const string THESAURUSABSTEMIOUS = "W-DRAI-OUT-PIPE";
        public const string THESAURUSABSTENTION = "W-DRAI-VENT-PIPE";
        public const string REPRESENTATIONAL = "W-BUSH";
        public const string THESAURUSABSTRACT = "套管";
        public const int THESAURUSABSTRACTED = 1000;
        public const string THESAURUSABSTRACTION = "带定位立管";
        public const int INATTENTIVENESS = 100;
        public const string THESAURUSABSTRUSE = "WP_KTN_LG";
        public const int INCOMPREHENSIBLE = 50;
        public const string THESAURUSABSURD = "$LIGUAN";
        public const string THESAURUSABUNDANT = "VPIPE-污水";
        public const string THESAURUSABUSIVE = "立管编号";
        public const string QUOTATIONABYSSINIAN = "W-DRAI-PIEP-RISR";
        public const string THESAURUSACADEMIC = "A$C58B12E6E";
        public const string HYPOCHONDRIACAL = "PIPE-喷淋";
        public const string ACANTHOPTERYGII = "A$C5E4A3C21";
        public const string CONVENTIONALIZED = "TCH_PIPE";
        public const int INCOMPREHENSIBILITY = 60;
        public const string THESAURUSACCELERATE = "TCH_VPIPEDIM";
        public const string THESAURUSACCELERATION = "TCH_TEXT";
        public const string THESAURUSACCENT = "De";
        public const string THESAURUSACCEPT = "-";
        public const string THESAURUSACCEPTABLE = "";
        public const string THESAURUSACCEPTANCE = "污废合流井编号";
        public const string ACKNOWLEDGEMENT = "地漏";
        public const char THESAURUSACCESS = 'A';
        public const char APPROACHABILITY = 'B';
        public const string INTERCHANGEABLY = "RF";
        public const string THESAURUSACCESSIBLE = "RF+1";
        public const int THESAURUSACCESSION = 1;
        public const string THESAURUSACCESSORY = "RF+2";
        public const int THESAURUSACCIDENT = 2;
        public const string UNINTENTIONALLY = "F";
        public const int THESAURUSACCLAIM = 10;
        public const string ACCLIMATIZATION = "1F";
        public const string THESAURUSACCOMMODATE = "W-NOTE";
        public const double ACCOMMODATINGLY = 2500.0;
        public const double ACCOMMODATIVENESS = 5500.0;
        public const int ACCOMMODATIONAL = 500;
        public const int THESAURUSACCOMMODATION = 3500;
        public const double THESAURUSACCOMPANIMENT = 1800.0;
        public const string THESAURUSACCOMPLISH = "通气帽系统";
        public const string ACCOMPLISSEMENT = "距离1";
        public const string THESAURUSACCOMPLISHMENT = "可见性1";
        public const string ACCOMPLISHMENTS = "伸顶通气管";
        public const int THESAURUSACCORDANCE = 580;
        public const string THESAURUSACCORDING = "标高";
        public const int CORRESPONDINGLY = 550;
        public const string THESAURUSACCORDINGLY = "建筑完成面";
        public const int THESAURUSACCOUNT = 1800;
        public const int ACCOUNTABLENESS = 780;
        public const int THESAURUSACCOUNTABLE = 121;
        public const int THESAURUSACCREDIT = 1258;
        public const int THESAURUSACCRUE = 120;
        public const int ACCULTURATIONAL = 779;
        public const int THESAURUSACCUMULATE = 1679;
        public const int THESAURUSACCUMULATION = 658;
        public const int ACCUMULATIVENESS = 90;
        public const double THESAURUSACCURACY = 0.0;
        public const int THESAURUSACCURATE = 300;
        public const int THESAURUSACCUSATION = 24;
        public const int QUOTATIONACCUSATIVE = 9;
        public const int THESAURUSACCUSE = 5;
        public const int THESAURUSACCUSTOM = 4;
        public const int THESAURUSACCUSTOMED = 3;
        public const int POLYOXYMETHYLENE = 350;
        public const int DEHYDROGENATION = 3600;
        public const string ACETYLSALICYLIC = "DN100";
        public const int ACHLOROPHYLLOUS = 800;
        public const int ACHONDROPLASIAC = 2000;
        public const int ACHONDROPLASTIC = 400;
        public const int UNCHRONOLOGICAL = 600;
        public const int QUOTATIONCARLYLE = 479;
        public const int ACKNOWLEDGEABLE = 1879;
        public const int THESAURUSACKNOWLEDGE = 950;
        public const int THESAURUSACOLYTE = 200;
        public const int THESAURUSACQUAINTED = 160;
        public const int THESAURUSACQUIREMENT = 30;
        public const int ACQUISITIVENESS = 750;
        public const int THESAURUSACQUISITIVE = 150;
        public const int THESAURUSACQUITTAL = 450;
        public const double PHARMACEUTICALS = .7;
        public const int ACRIMONIOUSNESS = 1400;
        public const string THESAURUSACRIMONIOUS = ";";
        public const double THESAURUSACRIMONY = .01;
        public const string ACTINOMYCETALES = "TH-STYLE3";
        public const int THESAURUSACTION = 745;
        public const string THESAURUSACTIVATE = "乙字弯";
        public const string CHARACTERISTICS = "立管检查口";
        public const string THESAURUSACTIVE = "套管系统";
        public const string THESAURUSACTIVITY = "可见性";
        public const string THESAURUSACTUALLY = "重力流雨水井编号";
        public const string THESAURUSACTUATE = "普通地漏无存水弯";
        public const string THESAURUSACUMEN = "地漏系统";
        public const string THESAURUSADAPTABLE = ",";
        public const int THESAURUSADAPTATION = 7;
        public const int FAMILIARIZATION = 229;
        public const int THESAURUSADDENDUM = 230;
        public const int THESAURUSADDICT = 8192;
        public const string THESAURUSADDICTED = "FromImagination";
        public const int THESAURUSADDICTION = 700;
        public const string CORTICOSTEROIDS = "清扫口：";
        public const string THESAURUSADDITION = "排出：";
        public const int THESAURUSADDITIONAL = 666;
        public const string THESAURUSADDRESS = "立管：";
        public const string THESAURUSADDUCE = "长转管:";
        public const string THESAURUSADEQUACY = "短转管:";
        public const string THESAURUSADEQUATE = "地漏：";
        public const int THESAURUSADJACENT = 255;
        public const int THESAURUSADJOURNMENT = 0x91;
        public const int DISCONTINUATION = 0xc7;
        public const int THESAURUSADJUDICATE = 0xae;
        public const int THESAURUSADJUDICATION = 211;
        public const int THESAURUSADJUNCT = 213;
        public const int THESAURUSADJUST = 111;
        public const string THESAURUSADJUSTMENT = "AI-空间框线";
        public const string EXTEMPORANEOUSLY = "AI-空间名称";
        public const int THESAURUSADMINISTER = 250;
        public const string THESAURUSADMINISTRATION = "73-";
        public const string ADMINISTRATIVUS = "1-";
        public const string ADMINISTRATIVELY = "单排";
        public const int AUTHORITARIANISM = 15;
        public const string ADSIGNIFICATION = "DL";
        public const string THESAURUSADULTERATE = "TCH_MULTILEADER";
        public const int THESAURUSADVANTAGE = 180;
        public const string THESAURUSADVICE = @"^(F\d?L|T\d?L|P\d?L|D\d?L)(\w*)\-(\w*)([a-zA-Z]*)$";
        public const double RECOMMENDATIONS = 383875.8169;
        public const double THESAURUSADVISABLE = 250561.9571;
        public const string THESAURUSADVISE = "P型存水弯";
        public const string THESAURUSADVISORY = "板上P弯";
        public const int THESAURUSADVOCATE = 1190;
        public const int AEROTHERMODYNAMICS = 1330;
        public const int THESAURUSAFFABLE = 490;
        public const int THESAURUSAFFAIR = 170;
        public const int THESAURUSAFFECT = 2830;
        public const int THESAURUSAFFECTATION = 900;
        public const int PRETENSIOUSNESS = 330;
        public const int THESAURUSAFFECTED = 895;
        public const int THESAURUSAFFECTING = 285;
        public const int THESAURUSAFFECTION = 1270;
        public const int AFFECTIONATENESS = 390;
        public const int THESAURUSAFFECTIONATE = 156;
        public const int THESAURUSAFFILIATE = 789;
        public const int THESAURUSAFFILIATION = 1090;
        public const int THESAURUSAFFINITY = 669;
        public const int THESAURUSAFFIRM = 590;
        public const int CONSCIENTIOUSLY = 1700;
        public const int THESAURUSAFFIRMATION = 1391;
        public const int THESAURUSAFFIRMATIVE = 667;
        public const int THESAURUSAFFLICT = 1450;
        public const int THESAURUSAFFLICTION = 510;
        public const int THESAURUSAFFLUENCE = 225;
        public const int THESAURUSAFFLUENT = 1125;
        public const int THESAURUSAFFORD = 1479;
        public const int AFFRANCHISEMENT = 499;
        public const int THESAURUSAFFRAY = 280;
        public const int THESAURUSAFFRONT = 76;
        public const int QUOTATIONAFGHAN = 424;
        public const int QUOTATIONMALICE = 1900;
        public const string THESAURUSAFRAID = "DN100乙字弯";
        public const int THESAURUSAFRESH = 1109;
        public const int QUOTATIONAFRICAN = 279;
        public const int RECLASSIFICATION = 29;
        public const int PARTHENOGENESIS = 1158;
        public const int ELECTROPHORESIS = 179;
        public const double PALAEONTOLOGIST = .2778;
        public const double GOSSWEILERODENDRON = 10e5;
        public const string INSTRUMENTALITY = "1";
        public const string THESAURUSAGENDA = "2";
        public const string THESAURUSAGGRAVATE = "3";
        public const int EXCOMMUNICATION = 2379;
        public const int THESAURUSAGGRAVATION = 1779;
        public const int THESAURUSAGGREGATE = 579;
        public const string THESAURUSAGGRESSION = "暂不支持污废分流";
        public const double THESAURUSAGGRESSIVE = .4;
        public const string THESAURUSAGGRESSOR = "管底H+X.XX";
        public const string THESAURUSAGHAST = "双池S弯";
        public const string THESAURUSAGILITY = "普通地漏P弯";
        public const string THESAURUSAGITATE = "S型存水弯";
        public const string THESAURUSAGITATION = "板上S弯";
        public const string THESAURUSAGITATOR = "翻转状态";
        public const int MARSIPOBRANCHII = 5479;
        public const int THESAURUSAGNOSTIC = 1079;
        public const int AGRANULOCYTOSIS = 5600;
        public const int THESAURUSAGREEABLE = 6079;
        public const int THESAURUSAGREEMENT = 8;
        public const int AGRIBUSINESSMAN = 1379;
        public const int AGRICULTURALIST = 569;
        public const int THESAURUSAGRICULTURE = 406;
        public const int THESAURUSAGROUND = 404;
        public const int LYMPHADENOPATHY = 3150;
        public const string THESAURUSAIRING = "设置乙字弯";
        public const string DISSATISFACTION = "feng_dbg_test_washing_machine";
        public const string THESAURUSALACRITY = "A-Kitchen-3";
        public const string AUTOBIOGRAPHICAL = "A-Kitchen-4";
        public const string QUOTATIONPERFIDIOUS = "A-Toilet-1";
        public const string CRYSTALLIZATION = "A-Toilet-2";
        public const string THESAURUSALCOHOL = "A-Toilet-3";
        public const string THESAURUSALCOHOLIC = "A-Toilet-4";
        public const string THESAURUSALCOVE = "-XiDiPen-";
        public const string QUOTATION1BBLACK = "A-Toilet-9";
        public const string REPRESENTATIVES = "$xiyiji";
        public const string PHYSIOTHERAPIST = "A-Kitchen-9";
        public const string ADMINISTRATIONS = "0$座厕";
        public const string QUOTATIONALFVÉN = "0$asdfghjgjhkl";
        public const string MAGNETOHYDRODYNAMIC = "A-Toilet-";
        public const string MAGNETOHYDRODYNAMICS = "A-Kitchen-";
        public const string EXTRATERRESTRIAL = "|lp";
        public const string THESAURUSALIENATE = "|lp1";
        public const string THESAURUSALIENATION = "|lp2";
        public const string THESAURUSALIGHT = "VPIPE-废水";
        public const string QUOTATIONGOLDSMITH = "套管：";
        public const string ALIMENTATIVENESS = "水";
        public const string THESAURUSALIMONY = "水井";
        public const string DIHYDROXYANTHRAQUINONE = "水管井";
        public const string QUOTATIONALKALINE = "WaterPipeWellFLs：";
        public const string UREIDOHYDANTOIN = "单排:";
        public const string THESAURUSALLEGATION = "GetFLsWhereSupportingFloorDrainUnderWaterPoint - ";
        public const string THESAURUSALLEGE = "KitchenOnlyFls：";
        public const string THESAURUSALLEGIANCE = "BalconyOnlyFLs：";
        public const string THESAURUSALLEGORICAL = "KitchenAndBalconyFLs：";
        public const string THESAURUSALLEGORY = "MustHaveFloorDrains：";
        public const string NATIONALIZATION = "HasKitchenFloorDrain：";
        public const string THESAURUSALLERGIC = "HasKitchenWashingMachine：";
        public const string HYPERSENSITIVITY = "HasKitchenBasin：";
        public const string THESAURUSALLERGY = "HasBalconyWashingMachine：";
        public const string THESAURUSALLEVIATE = "HasMopPool：";
        public const int THESAURUSALLIANCE = 12;
        public const string QUOTATIONALMAIN = "TCH_MTEXT";
        public const string THESAURUSALMANAC = "基点 X";
        public const string THESAURUSALMIGHTY = "基点 Y";
        public const string THESAURUSALMOST = "error occured while getting baseX and baseY";
        public const string ALTERNATIVENESS = "楼层框定";
        public const string PLENIPOTENTIARY = "wb";
        public const string THESAURUSAMBASSADOR = "kd";
        public const string AMBIDEXTROUSNESS = "A$C6BDE4816";
        public const string THESAURUSAMBIGUOUS = "AI-房间框线";
        public const string THESAURUSAMBITION = "AI-房间名称";
        public const string THESAURUSAMBUSH = "伸顶通气2000";
        public const string THESAURUSAMENABLE = "伸顶通气500";
        public const string THESAURUSAMENDMENT = "通气帽系统-AI";
        public const string THESAURUSAMICABLE = "接阳台洗手盆排水";
        public const string QUOTATIONCAXTON = "DN50，余同";
        public const string THESAURUSAMMUNITION = "接厨房洗涤盆排水";
        public const string THESAURUSAMNESTY = "接卫生间排水管";
        public const string QUOTATIONAMNESTY = "DN100，余同";
        public const string QUOTATIONAMNIOTIC = "接阳台洗衣机地漏";
        public const string THESAURUSAMOROUS = "DN75，余同";
        public const string THESAURUSAMORPHOUS = "接阳台地漏";
        public const string MISCONSTRUCTION = "接厨房洗衣机地漏";
        public const string THESAURUSAMOUNT = "接厨房地漏";
        public const string AMPHIARTHRODIAL = "洗衣机地漏P弯";
        public const string ALLOTETRAPLOIDY = "双格洗涤盆排水-AI";
        public const string AMPHITHEATRICAL = "楼层类型";
        public const char THESAURUSAMPLIFY = '，';
        public const char THESAURUSAMPUTATE = ',';
        public const char ELECTROMAGNETISM = '-';
        public const string ELECTRODYNAMICS = "M";
        public const string THESAURUSAMUSEMENT = " ";
        public const string QUOTATIONAMYGDALOID = @"(\-?\d+)-(\-?\d+)";
        public const string POLYSACCHARIDES = @"\-?\d+";
        public const string DEDIFFERENTIATION = "楼层编号";
        public const int QUOTATIONANABOLIC = 660;
        public static bool IsToilet(string roomName)
        {
            var roomNameContains = new List<string>
            {
                THESAURUSABASEMENT,THESAURUSABATEMENT,THESAURUSABBREVIATE,
                THESAURUSABBREVIATION,THESAURUSABDICATE,THESAURUSABDICATION,
            };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            if (roomName.Equals(THESAURUSABDUCT))
                return THESAURUSABDOMINAL;
            return Regex.IsMatch(roomName, THESAURUSABERRANT);
        }
        public static bool IsKitchen(string roomName)
        {
            var roomNameContains = new List<string> { THESAURUSABERRATION, REFRANGIBILITIES };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            if (roomName.Equals(THESAURUSABEYANCE))
                return THESAURUSABDOMINAL;
            return THESAURUSABDOMEN;
        }
        public static bool IsBalcony(string roomName)
        {
            if (roomName == null) return THESAURUSABDOMEN;
            var roomNameContains = new List<string> { THESAURUSABHORRENT };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            return THESAURUSABDOMEN;
        }
        public static bool IsCorridor(string roomName)
        {
            if (roomName == null) return THESAURUSABDOMEN;
            var roomNameContains = new List<string> { THESAURUSABIDING };
            if (string.IsNullOrEmpty(roomName))
                return THESAURUSABDOMEN;
            if (roomNameContains.Any(c => roomName.Contains(c)))
                return THESAURUSABDOMINAL;
            return THESAURUSABDOMEN;
        }
        public static bool IsWL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return Regex.IsMatch(label, THESAURUSABLAZE);
        }
        public static bool IsFL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return Regex.IsMatch(label, THESAURUSABLUTION);
        }
        public static bool IsFL0(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return IsFL(label) && label.Contains(THESAURUSABNEGATION);
        }
        public static bool IsPL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return Regex.IsMatch(label, THESAURUSABNORMAL);
        }
        public static bool IsTL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return Regex.IsMatch(label, PROGNOSTICATION);
        }
        public static bool IsDL(string label)
        {
            if (label == null) return THESAURUSABDOMEN;
            return Regex.IsMatch(label, THESAURUSABOLISH);
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
                    if (reference.GetEffectiveName().Contains(QUOTATION1BBLACK))
                    {
                        using var adb = AcadDatabase.Use(reference.Database);
                        if (IsVisibleLayer(adb.Layers.Element(reference.Layer)))
                            return THESAURUSABDOMINAL;
                    }
                }
                return THESAURUSABDOMEN;
            }
            public override bool CheckLayerValid(Entity curve)
            {
                return THESAURUSABDOMINAL;
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
    public enum WLType
    {
        PL, PL_TL, PL_DL, PL_TL_DL, FL, FL_TL,
    }
    public class WLGrouper
    {
        public class ToiletGrouper
        {
            public List<string> PLs;
            public List<string> TLs;
            public List<string> DLs;
            public List<string> FLs;
            public WLType WLType;
            public void Init()
            {
                PLs ??= new List<string>();
                TLs ??= new List<string>();
                DLs ??= new List<string>();
                FLs ??= new List<string>();
            }
        }
    }
    public class StoreyInfo
    {
        public StoreyType StoreyType;
        public List<int> Numbers;
        public Point2d ContraPoint;
        public GRect Boundary;
    }
}