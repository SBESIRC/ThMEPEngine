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
    public class DrainageLayoutManager
    {
        List<DBTextInfo> textInfos;
        int i1;
        int j1;
        List<BlockInfo> brInfos;
        int i2;
        int j2;
        List<LineInfo> lineInfos;
        int i3;
        int j3;
        List<CircleInfo> circleInfos;
        int i4;
        int j4;
        List<DimInfo> dimInfos;
        int i5;
        int j5;
        public DrainageLayoutManager(List<DBTextInfo> textInfos, List<BlockInfo> brInfos, List<LineInfo> lineInfos, List<CircleInfo> circleInfos, List<DimInfo> dimInfos)
        {
            this.textInfos = textInfos;
            this.brInfos = brInfos;
            this.lineInfos = lineInfos;
            this.circleInfos = circleInfos;
            this.dimInfos = dimInfos;
            TakeStopSnap();
        }
        public void TakeStartSnap()
        {
            i1 = textInfos.Count;
            i2 = brInfos.Count;
            i3 = lineInfos.Count;
            i4 = circleInfos.Count;
            i5 = dimInfos.Count;
        }
        public void TakeStopSnap()
        {
            j1 = textInfos.Count;
            j2 = brInfos.Count;
            j3 = lineInfos.Count;
            j4 = circleInfos.Count;
            j5 = dimInfos.Count;
        }
        public void MoveElements(Vector2d v) => MoveElements(v.ToVector3d());
        public void MoveElements(Vector3d v)
        {
            var v2 = v.ToVector2d();
            foreach (var info in textInfos)
            {
                info.BasePoint += v;
            }
            foreach (var info in brInfos)
            {
                info.BasePoint += v;
            }
            foreach (var info in lineInfos)
            {
                info.Line = info.Line.Offset(v2);
            }
            foreach (var info in circleInfos)
            {
                info.Circle = info.Circle.OffsetXY(v2.X, v2.Y);
            }
            foreach (var info in dimInfos)
            {
                info.Point1 = info.Point1.Offset(v);
                info.Point2 = info.Point2.Offset(v);
            }
        }
        public DrainageLayoutManager GetSnapshot()
        {
            return new(textInfos.GetRange(i1, j1 - i1), brInfos.GetRange(i2, j2 - i2), lineInfos.GetRange(i3, j3 - i3), circleInfos.GetRange(i4, j4 - i4), dimInfos.GetRange(i5, j5 - i5));
        }
        public IEnumerable<LineInfo> GetLineInfos()
        {
            for (int i = i3; i < j3; i++)
            {
                yield return lineInfos[i];
            }
        }
        public IEnumerable<Point3d> GetBrBasePoints()
        {
            for (int i = i2; i < j2; i++)
            {
                yield return brInfos[i].BasePoint;
            }
        }
        public IEnumerable<Point2d> GetLineVertices()
        {
            for (int i = i3; i < j3; i++)
            {
                yield return lineInfos[i].Line.StartPoint;
                yield return lineInfos[i].Line.EndPoint;
            }
        }
    }
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
    public class FixingLogic1
    {
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
    public class FixingLogic2
    {
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
    public class DrainageLabelItem
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
        static readonly Regex re = new Regex(THESAURUSENCOMPASS);
        public static DrainageLabelItem Parse(string label)
        {
            if (label == null) return null;
            var m = re.Match(label);
            if (!m.Success) return null;
            return new DrainageLabelItem()
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
            public FlFixType FlFixType;
            public FlCaseEnum FlCaseEnum;
            public PlFixType PlFixType;
            public PlCaseEnum PlCaseEnum;
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
        Unknown, Y1L, Y2L, NL, YL, FL0, PLTL, WLTL, WLTLFL, PL, FL, WL, DL, TL,
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
    public class PipeRun : IEquatable<PipeRun>
    {
        public int Index;
        public string Storey;
        public bool Exists;
        public bool HasLong;
        public bool HasShort;
        public bool HasCleaningPort;
        public bool HasBasin;
        public int FDSCount;
        public int CPSCount;
        public int WPSCount;
        public string WaterBucket;
        public static bool operator ==(PipeRun me, PipeRun other)
        {
            return me.Equals(other);
        }
        public static bool operator !=(PipeRun me, PipeRun other) => !(me == other);
        public bool Equals(PipeRun other)
        {
            if (this.Index != other.Index) return INTRAVASCULARLY;
            if (this.Storey != other.Storey) return INTRAVASCULARLY;
            if (this.Exists != other.Exists) return INTRAVASCULARLY;
            if (this.Exists == other.Exists == INTRAVASCULARLY) return THESAURUSOBSTINACY;
            if (this.HasLong != other.HasLong) return INTRAVASCULARLY;
            if (this.HasShort != other.HasShort) return INTRAVASCULARLY;
            if (this.HasCleaningPort != other.HasCleaningPort) return INTRAVASCULARLY;
            if (this.HasBasin != other.HasBasin) return INTRAVASCULARLY;
            if (this.FDSCount != other.FDSCount) return INTRAVASCULARLY;
            if (this.CPSCount != other.CPSCount) return INTRAVASCULARLY;
            if (this.WPSCount != other.WPSCount) return INTRAVASCULARLY;
            if (this.WaterBucket != other.WaterBucket) return INTRAVASCULARLY;
            return THESAURUSOBSTINACY;
        }
        public override int GetHashCode()
        {
            return THESAURUSSTAMPEDE;
        }
    }
    public class PipeLine : IEquatable<PipeLine>
    {
        public List<string> Labels = new();
        public readonly List<PipeRun> Runs = new();
        public PipeType PipeType;
        public string Outlet;
        public string WPRadius;
        public static bool operator ==(PipeLine me, PipeLine other)
        {
            return me.Equals(other);
        }
        public static bool operator !=(PipeLine me, PipeLine other) => !(me == other);
        public bool Equals(PipeLine other)
        {
            if (this.PipeType != other.PipeType) return INTRAVASCULARLY;
            if (this.Outlet != other.Outlet) return INTRAVASCULARLY;
            if (this.WPRadius != other.WPRadius) return INTRAVASCULARLY;
            for (int i = THESAURUSSTAMPEDE; i < Runs.Count; i++)
            {
                if (this.Runs[i] != other.Runs[i]) return INTRAVASCULARLY;
            }
            return THESAURUSOBSTINACY;
        }
        public override int GetHashCode()
        {
            return THESAURUSSTAMPEDE;
        }
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
            var vecs = new List<Vector2d> { new Vector2d(THESAURUSDOMESTIC, -QUOTATIONWITTIG), new Vector2d(PROKELEUSMATIKOS, THESAURUSSTAMPEDE) };
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
            public List<Vector2d> vecs2 => new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, QUOTATIONBASTARD + dy), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, -COOPERATIVENESS - dy) };
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
                void _DrawWrappingPipe(Point2d basePt)
                {
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
                    if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC));
                    if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC));
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
                    void drawDomePipes(IEnumerable<GLineSegment> segs)
                    {
                        var ok = INTRAVASCULARLY;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
                                ok = THESAURUSOBSTINACY;
                            }
                            dome_lines.Add(seg);
                        }
                    }
                    void drawVentPipe(GLineSegment seg)
                    {
                        if (seg.IsValid) vent_lines.Add(seg);
                    }
                    void drawVentPipes(IEnumerable<GLineSegment> segs)
                    {
                        var ok = INTRAVASCULARLY;
                        foreach (var seg in segs.Where(s => s.IsValid))
                        {
                            if (!ok)
                            {
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
                                    DrawDSCurve(p, INTRAVASCULARLY, getDSCurveValue());
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
                    void DrawOutlets1(Point2d basePoint1, double width, ThwOutput output, bool isRainWaterWell = INTRAVASCULARLY, Vector2d? fixv = null)
                    {
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
                                    drawDomePipes(segs.Take(INTROPUNITIVENESS));
                                }
                                else if (output.LinesCount > THESAURUSHOUSING)
                                {
                                    segs.RemoveAt(THESAURUSDESTITUTE);
                                    if (!output.HasVerticalLine2) segs.RemoveAt(SUPERLATIVENESS);
                                    segs.RemoveAt(INTROPUNITIVENESS);
                                    drawDomePipes(segs);
                                }
                            }
                            var pts = vecs.ToPoint2ds(basePoint1);
                            if (output.HasWrappingPipe1) _DrawWrappingPipe(pts[INTROPUNITIVENESS].OffsetX(THESAURUSHYPNOTIC));
                            if (output.HasWrappingPipe2) _DrawWrappingPipe(pts[QUOTATIONEDIBLE].OffsetX(THESAURUSHYPNOTIC));
                            if (output.HasWrappingPipe3) _DrawWrappingPipe(pts[THESAURUSSCARCE].OffsetX(THESAURUSHYPNOTIC));
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
                            drawDomePipes(segs);
                            DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSPERMUTATION);
                        }
                        if (output.HangingCount == THESAURUSHOUSING)
                        {
                            var hang = output.Hanging1;
                            Point2d lastPt = pt2;
                            {
                                var segs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, INDIGESTIBLENESS), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE) }.ToGLineSegments(lastPt);
                                drawDomePipes(segs);
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
                            drawDomePipes(vs1.ToGLineSegments(pt3));
                            drawHanging(pts.Last(), output.Hanging1);
                            var dx = output.Hanging1.FloorDrainsCount == THESAURUSPERMUTATION ? POLYOXYMETHYLENE : THESAURUSSTAMPEDE;
                            var vs2 = new List<Vector2d> { new Vector2d(HYDROCOTYLACEAE + dx, THESAURUSSTAMPEDE), new Vector2d(QUOTATIONDEFLUVIUM, QUOTATIONDEFLUVIUM) };
                            drawDomePipes(vs2.ToGLineSegments(pts[THESAURUSHOUSING]));
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
                        void _DrawCheckPoint(Point2d basePt, bool leftOrRight)
                        {
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
                                        DrawOutlets1(basePt, THESAURUSEXECRABLE, output);
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
                                void _DrawFloorDrain(Point3d basePt, bool leftOrRight, int i, int j)
                                {
                                    var p1 = basePt.ToPoint2d();
                                    {
                                        if (_shouldDrawRaiseWashingMachineSymbol())
                                        {
                                            var fixVec = new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE);
                                            var p = p1 + new Vector2d(THESAURUSSTAMPEDE, INCONSIDERABILIS) + new Vector2d(-THESAURUSCAVERN, THESAURUSSHROUD) + fixVec;
                                            fdBsPts.Add(p);
                                            var vecs = new List<Vector2d> { new Vector2d(-THESAURUSSOMETIMES, THESAURUSSTAMPEDE), fixVec, new Vector2d(-THESAURUSQUAGMIRE, THESAURUSQUAGMIRE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFORMULATE), new Vector2d(-ELECTROMYOGRAPH, THESAURUSSTAMPEDE) };
                                            var segs = vecs.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(THROMBOEMBOLISM, THESAURUSSTAMPEDE));
                                            drawDomePipes(segs);
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
                                void _DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight, int i, int j)
                                {
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
                                        DrawDSCurve(p2, leftOrRight, getDSCurveValue());
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
                                            double fixY = -THESAURUSPERVADE - THESAURUSQUAGMIRE;
                                            if (gpItem.Items[i].HasLong)
                                            {
                                                fixY += THESAURUSQUAGMIRE;
                                            }
                                            DrawPipeButtomHeightSymbol(THESAURUSEUPHORIA, THESAURUSSTAMPEDE, info.EndPoint.OffsetY(HEIGHT / THESAURUSCOMMUNICATION + fixY));
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
                                    _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSOBSTINACY, i, j);
                                    _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, i, j);
                                    if (run.Is4Tune)
                                    {
                                        var st = info.StartPoint;
                                        var p1 = new List<Vector2d> { new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMISSIONARY) }.GetLastPoint(st);
                                        var p2 = new List<Vector2d> { new Vector2d(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSMISSIONARY) }.GetLastPoint(st);
                                        _DrawWrappingPipe(p1);
                                        _DrawWrappingPipe(p2);
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
                                        drawDomePipes(segs);
                                        _DrawDSCurve(vec7, p1, isLeftOrRight, i, j);
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
                                        var isFDHigher = gpItem.Hangings[i].FlFixType == FlFixType.Higher && hanging.FloorDrainsCount > THESAURUSSTAMPEDE && run.HasLongTranslator && run.IsLongTranslatorToLeftOrRight;
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
                                            f = () => { drawDomePipes(_segs); };
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSHOUSING)
                                        {
                                            var p = segs.Last(INTROPUNITIVENESS).EndPoint;
                                            _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p + new Vector2d(THESAURUSHYPNOTIC, -THESAURUSGETAWAY));
                                        }
                                        if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                        {
                                            var p2 = segs.Last(INTROPUNITIVENESS).EndPoint;
                                            var p1 = segs.Last(THESAURUSHOUSING).EndPoint;
                                            _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j);
                                            _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j);
                                            Get2FloorDrainDN(out string v1, out string v2);
                                            DrawNoteText(v1, p1 + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                            DrawNoteText(v2, p2 + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                            if (!hanging.IsSeries)
                                            {
                                                drawDomePipes(new GLineSegment[] { segs.Last(THESAURUSPERMUTATION) });
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
                                                    drawDomePipes(_segs);
                                                    drawDomePipes(new GLineSegment[] { seg });
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
                                            _DrawDSCurve(vec7, p1, isLeftOrRight, i, j);
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
                                        drawDomePipes(segs);
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
                                                _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j);
                                                _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j);
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
                                                _DrawFloorDrain(segs1.Last().EndPoint.ToPoint3d(), THESAURUSOBSTINACY, i, j);
                                                _DrawFloorDrain(segs2.Last().EndPoint.ToPoint3d(), INTRAVASCULARLY, i, j);
                                            }
                                            ok = THESAURUSOBSTINACY;
                                        }
                                        Action f = null;
                                        if (!ok)
                                        {
                                            if (gpItem.Hangings[i].FlCaseEnum != FlCaseEnum.Case1)
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
                                                    _DrawFloorDrain(p.ToPoint3d(), isLeftOrRight, i, j);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p + new Vector2d(THESAURUSHYPNOTIC, -THESAURUSGETAWAY));
                                                }
                                                if (hanging.FloorDrainsCount == THESAURUSPERMUTATION)
                                                {
                                                    var p2 = segs.Last(INTROPUNITIVENESS).EndPoint;
                                                    var p1 = segs.Last(THESAURUSHOUSING).EndPoint;
                                                    _DrawFloorDrain(p1.ToPoint3d(), isLeftOrRight, i, j);
                                                    _DrawFloorDrain(p2.ToPoint3d(), isLeftOrRight, i, j);
                                                    Get2FloorDrainDN(out string v1, out string v2);
                                                    DrawNoteText(v1, p1 + new Vector2d(THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                                    DrawNoteText(v2, p2 + new Vector2d(PHYSIOLOGICALLY - THESAURUSCAVERN, -THESAURUSDOMESTIC));
                                                }
                                                f = () => drawDomePipes(_segs);
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
                                                if (gpItem.Hangings[i].FlCaseEnum == FlCaseEnum.Case1)
                                                {
                                                    var p2 = p1 + vec7;
                                                    var segs1 = new List<Vector2d> { new Vector2d(-INCONSIDERABILIS + THESAURUSBISEXUAL + THESAURUSQUAGMIRE, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, -POLYOXYMETHYLENE - THESAURUSSPIRIT - THESAURUSQUAGMIRE), new Vector2d(MISAPPREHENSIVE, -MISAPPREHENSIVE) }.ToGLineSegments(p2);
                                                    drawDomePipes(segs1);
                                                    {
                                                        Vector2d v = default;
                                                        var b = isLeftOrRight;
                                                        if (b && getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                                        {
                                                            b = INTRAVASCULARLY;
                                                            v = new Vector2d(-THESAURUSCAVERN, -QUOTATIONWITTIG);
                                                        }
                                                        _DrawDSCurve(default(Vector2d), p2 + v, b, i, j);
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
                                                    _DrawDSCurve(vec7, p1, isLeftOrRight, i, j);
                                                    if (getDSCurveValue() == THESAURUSDISCIPLINARIAN)
                                                    {
                                                        var segs1 = new List<Vector2d> { new Vector2d(THESAURUSFLUTTER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, -THESAURUSPERVADE) }.ToGLineSegments(p1.OffsetY(HYPERDISYLLABLE));
                                                        f = () => { drawDomePipes(segs1); };
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
                                var h = THESAURUSDOMESTIC;
                                Point2d pt1, pt2;
                                {
                                    pt1 = info.EndPoint.OffsetY(h);
                                    pt2 = info.EndPoint;
                                    if (run.HasLongTranslator)
                                    {
                                        pt1 = info.EndPoint.OffsetY(DETERMINATENESS + QUINQUARTICULAR);
                                    }
                                }
                                _DrawCheckPoint(pt1.OffsetY(PHYSIOLOGICALLY - CONSCRIPTIONIST + THESAURUSDOMESTIC), THESAURUSOBSTINACY);
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
                                        drawDomePipes(segs);
                                        _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, THESAURUSPERMUTATION);
                                    }
                                    else
                                    {
                                        var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-THESAURUSHYPNOTIC));
                                        drawDomePipes(segs);
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
                                DrawShortTranslatorLabel(info.Segs.First().Center, run.IsShortTranslatorToLeftOrRight);
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
                                        case FlFixType.NoFix:
                                            break;
                                        case FlFixType.MiddleHigher:
                                            fixY = THESAURUSFEELER / QUOTATIONBASTARD * HEIGHT;
                                            break;
                                        case FlFixType.Lower:
                                            fixY = -THESAURUSATTENDANCE / THESAURUSPERMUTATION / QUOTATIONBASTARD * HEIGHT;
                                            break;
                                        case FlFixType.Higher:
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
                                drawDomePipes(segs);
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
                                    drawVentPipes(segs);
                                }
                            }
                            void TestRightSegsLast()
                            {
                                var segs = info.RightSegsLast;
                                if (segs != null)
                                {
                                    drawVentPipes(segs);
                                }
                            }
                            void TestRightSegsFirst()
                            {
                                var segs = info.RightSegsFirst;
                                if (segs != null)
                                {
                                    drawVentPipes(segs);
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
                                            drawVentPipes(segs);
                                        }
                                    }
                                    else if (gpItem.MinTl + THESAURUSASPIRATION == storey)
                                    {
                                        var segs = info.RightSegsLast;
                                        if (segs != null)
                                        {
                                            drawVentPipes(segs);
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
                                            drawVentPipes(segs);
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
                                            drawDomePipes(segs);
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
                                            drawDomePipes(segs);
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
                                                        drawDomePipes(segs);
                                                        var seg = new List<Vector2d> { new Vector2d(-THESAURUSDICTATORIAL, -THESAURUSFICTION), new Vector2d(THESAURUSINCARCERATE, THESAURUSSTAMPEDE) }.ToGLineSegments(segs.First().StartPoint)[THESAURUSHOUSING];
                                                        DrawDimLabel(seg.StartPoint, seg.EndPoint, new Vector2d(THESAURUSSTAMPEDE, -POLYOXYMETHYLENE), METACOMMUNICATION, QUOTATIONBENJAMIN);
                                                    }
                                                }
                                                else
                                                {
                                                    var vecs = new List<Vector2d> { new Vector2d(-THESAURUSINHERIT, -DEMONSTRATIONIST), new Vector2d(THESAURUSMIRTHFUL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), new Vector2d(THESAURUSSTAMPEDE, THESAURUSFICTION) };
                                                    var segs = vecs.ToGLineSegments(info.EndPoint).Skip(THESAURUSHOUSING).ToList();
                                                    drawDomePipes(segs);
                                                    DrawFloorDrain((segs.Last().EndPoint + new Vector2d(THESAURUSCAVERN, THESAURUSINTRACTABLE)).ToPoint3d(), THESAURUSOBSTINACY, ADENOHYPOPHYSIS);
                                                }
                                            }
                                        }
                                        DrawOutlets1(basePt, THESAURUSEXECRABLE, output, isRainWaterWell: THESAURUSOBSTINACY);
                                    }
                                }
                                else if (gpItem.IsSingleOutlet)
                                {
                                    void DrawOutlets3(Point2d basePoint)
                                    {
                                        var values = output.DirtyWaterWellValues;
                                        var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), new Vector2d(-THESAURUSPERVADE, -THESAURUSPERVADE), new Vector2d(-THESAURUSFORESTALL, THESAURUSSTAMPEDE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSDERELICTION), new Vector2d(THESAURUSLEARNER, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, INVULNERABLENESS) };
                                        var segs = vecs.ToGLineSegments(basePoint);
                                        segs.RemoveAt(INTROPUNITIVENESS);
                                        drawDomePipes(segs);
                                        DrawDiryWaterWells1(segs[THESAURUSPERMUTATION].EndPoint + new Vector2d(-THESAURUSDOMESTIC, THESAURUSHYPNOTIC), values);
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC));
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC));
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
                                    DrawOutlets3(info.EndPoint);
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
                                            drawDomePipes(segs);
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
                                            drawDomePipes(segs);
                                            var p3 = segs.First().StartPoint;
                                            drawDomePipe(new GLineSegment(p3, p3.OffsetY(THESAURUSEQUATION)));
                                        }
                                    }
                                    DrawOutlets1(info.EndPoint, THESAURUSEXECRABLE, output, fixv: new Vector2d(THESAURUSSTAMPEDE, -THESAURUSSURPRISED));
                                }
                                else if (gpItem.HasBasinInKitchenAt1F)
                                {
                                    output.HasWrappingPipe2 = output.HasWrappingPipe1;
                                    output.DN1 = getBasinDN();
                                    output.DN2 = IRRESPONSIBLENESS;
                                    void DrawOutlets4(Point2d basePoint, double HEIGHT)
                                    {
                                        var v = PERIODONTOCLASIA;
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
                                        if (output.HasWrappingPipe1) _DrawWrappingPipe(segs[INTROPUNITIVENESS].StartPoint.OffsetX(THESAURUSHYPNOTIC));
                                        if (output.HasWrappingPipe2) _DrawWrappingPipe(segs[THESAURUSPERMUTATION].EndPoint.OffsetX(THESAURUSHYPNOTIC));
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
                                                static void DrawStoreyHeightSymbol(Point2d basePt, string layer, string label)
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
                                            }
                                        }
                                        DrawNoteText(output.DN1, segs[INTROPUNITIVENESS].StartPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                                        DrawNoteText(output.DN2, segs[THESAURUSPERMUTATION].EndPoint.OffsetXY(THESAURUSATTACHMENT, THESAURUSENTREPRENEUR));
                                        if (output.HasCleaningPort1) DrawCleaningPort(segs[QUOTATIONEDIBLE].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                                        if (output.HasCleaningPort2) DrawCleaningPort(segs[THESAURUSPERMUTATION].StartPoint.ToPoint3d(), INTRAVASCULARLY, THESAURUSHOUSING);
                                        var p = segs[THESAURUSCOMMUNICATION].EndPoint;
                                        var fixY = THESAURUSHYPNOTIC + HEIGHT / THESAURUSCOMMUNICATION;
                                        var p1 = p.OffsetX(-THESAURUSPERVADE) + new Vector2d(-THESAURUSOFFEND + THESAURUSDOMESTIC, fixY);
                                        DrawDSCurve(p1, THESAURUSOBSTINACY, v);
                                        var p2 = p1.OffsetY(-fixY);
                                        segs.Add(new GLineSegment(p1, p2));
                                        if (v == THESAURUSDISCIPLINARIAN)
                                        {
                                            var p5 = segs[INTROPUNITIVENESS].StartPoint;
                                            var _segs = new List<Vector2d> { new Vector2d(THESAURUSCHORUS, THESAURUSSTAMPEDE), new Vector2d(THESAURUSPERVADE, THESAURUSPERVADE), new Vector2d(THESAURUSSTAMPEDE, THESAURUSCELESTIAL), new Vector2d(-QUOTATIONWITTIG, THESAURUSSTAMPEDE) }.ToGLineSegments(p5);
                                            segs = segs.Take(INTROPUNITIVENESS).ToList();
                                            segs.AddRange(_segs);
                                        }
                                        drawDomePipes(segs);
                                    }
                                    DrawOutlets4(info.EndPoint, HEIGHT);
                                }
                                else
                                {
                                    DrawOutlets1(info.EndPoint, THESAURUSEXECRABLE, output);
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
            geoData.ODLines.AddRange(geoData.DLines);
            ThDrainageService.PreFixGeoData(geoData);
            if (noWL && geoData.Labels.Any(x => IsWL(x.Text)))
            {
                MessageBox.Show(PARALINGUISTICALLY);
            }
            var (_drDatas, exInfo) = _CreateDrainageDrawingData(geoData, THESAURUSOBSTINACY);
            drDatas = _drDatas;
            return (drDatas, exInfo, THESAURUSOBSTINACY);
        }
        public static (List<DrainageDrawingData>, ExtraInfo) CreateDrainageDrawingData(DrainageGeoData geoData, bool noDraw)
        {
            geoData.ODLines.AddRange(geoData.DLines);
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
            geoData.StoreyInfos.AddRange(storeys);
            geoData.StoreyItems.AddRange(storeysItems);
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
        public static void CollectDrainageGeoData(Point3dCollection range, AcadDatabase adb, out List<StoreyItem> storeysItems, out DrainageGeoData geoData)
        {
            var storeys = GetStoreys(range, adb);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            geoData.StoreyInfos.AddRange(storeys);
            DrainageService.CollectGeoData(range, adb, geoData);
            geoData.StoreyItems.AddRange(storeysItems);
            geoData.Flush();
        }
        public static void DrawDrainageSystemDiagram(DrainageSystemDiagramViewModel viewModel, bool focus)
        {
            if (DrawWLPipeSystem()) return;
            if (frame is null) return;
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
                            if (storey == HYDROCHLOROFLUOROCARBON)
                            {
                                var v2 = drDatas.Select(x => { x.FdsCountAt2F.TryGetValue(label, out int vv); return vv; }).MaxOrZero();
                                return Math.Max(v, v2);
                            }
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
                            if (drData.HasOutletWrappingPipe.Contains(label)) return THESAURUSOBSTINACY;
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
        public static void DrawDSCurve(Point2d p2, bool leftOrRight, string value)
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
        public static Coordinate Clone(Coordinate o) => new(o.X, o.Y);
        public static Point Clone(Point o) => new(o.X, o.Y);
        public static LineString Clone(LineString o) => new(o.Coordinates);
        public static Polygon Clone(Polygon o) => new(o.Shell);
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
                    if (name.Contains(THESAURUSSTRAIGHTFORWARD) || name.Contains(THESAURUSEMPTINESS) || name.Contains(THESAURUSHYPOCRISY) || name.Contains(THESAURUSRECIPE) || name.Contains(THESAURUSRAFFLE))
                    {
                        var bd = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix).ToGRect(THESAURUSENTREPRENEUR);
                        reg(fs, bd, pipes);
                        return;
                    }
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
            foreach (var group in adb.Groups)
            {
                var lst = new List<Geometry>();
                foreach (var id in group.GetAllEntityIds())
                {
                    var entity = adb.Element<Entity>(id);
                    var dxfName = entity.GetRXClass().DxfName.ToUpper();
                    if (entity.Layer is THESAURUSCONTROVERSY)
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
                    if (dxfName is DISORGANIZATION && GetEffectiveLayer(entity.Layer) is THESAURUSCONTROVERSY)
                    {
                        dynamic o = entity;
                        var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint);
                        if (seg.IsValid) lst.Add(seg.ToLineString());
                        continue;
                    }
                    if (!entity.Visible || !isDrainageLayer(entity.Layer)) continue;
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
                {
                    {
                        if (entity is Circle c && isDrainageLayer(c.Layer) && c.Radius > THESAURUSSTAMPEDE)
                        {
                            geoData.circles.Add(c.ToGCircle());
                        }
                    }
                    {
                        if (entity is BlockReference br && br.BlockTableRecord.IsValid && br.GetEffectiveName() is THESAURUSCONFRONTATION or PARATHYROIDECTOMY)
                        {
                            var c = EntityTool.GetCircles(br).Where(x => x.Visible).Select(x => x.ToGCircle()).Where(x => x.Radius > THESAURUSINCOMPLETE).FirstOrDefault();
                            if (c.IsValid)
                            {
                                geoData.FloorDrains.Add(c.ToCirclePolygon(SUPERLATIVENESS).ToGRect());
                                geoData.FloorDrainRings.Add(c);
                            }
                        }
                    }
                }
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
            var circlePts = geoData.circles.Select(x => x.ToCirclePolygon(SUPERLATIVENESS).Tag(x)).ToList();
            var circlePtsf = GeoFac.CreateIntersectsSelector(circlePts);
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
                        if (!IsDraiLabel(text)) continue;
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
                        if (!IsDraiLabel(text)) continue;
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
                        var pts = lbDict.Where(x => IsDraiLabel(x.Value)).Select(x => x.Key.GetCenter().ToNTSPoint().Tag(x.Key)).ToList();
                        var ptsf = GeoFac.CreateIntersectsSelector(pts);
                        foreach (var g in geoData.Groups)
                        {
                            var geo = g.ToGeometry();
                            if (wrappingPipesf(geo).Any())
                            {
                                foreach (var pt in ptsf(geo))
                                {
                                    drData.HasOutletWrappingPipe.Add(lbDict[pt.UserData as Geometry]);
                                }
                            }
                        }
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
                {
                    foreach (var pp in item.VerticalPipes)
                    {
                        if (!lbDict.ContainsKey(pp))
                        {
                            lbDict[pp] = null;
                        }
                    }
                }
                {
                    var f = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                    foreach (var pp in item.VerticalPipes)
                    {
                        if (string.IsNullOrEmpty(lbDict[pp]))
                        {
                            var pps = f(pp.ToGRect().Expand(THESAURUSENTREPRENEUR).ToPolygon()).Where(x => IsWantedLabelText(lbDict[x])).ToList();
                            if (pps.Count == THESAURUSHOUSING)
                            {
                                lbDict[pp] = lbDict[pps[THESAURUSSTAMPEDE]];
                                drData.ShortTranslatorLabels.Add(lbDict[pps[THESAURUSSTAMPEDE]]);
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
                exItem.LabelDict = lbDict.Select(x => new Tuple<Geometry, string>(x.Key, x.Value)).ToList();
                if (geoData.StoreyItems[si].Labels?.Contains(THESAURUSREGION) ?? INTRAVASCULARLY)
                {
                    for (int i = THESAURUSSTAMPEDE; i < geoData.StoreyItems.Count; i++)
                    {
                        if ((geoData.StoreyItems[i].Labels?.Contains(HYDROCHLOROFLUOROCARBON) ?? INTRAVASCULARLY) && i != si)
                        {
                            var v = geoData.StoreyInfos[i].ContraPoint - geoData.StoreyInfos[si].ContraPoint;
                            var r1 = geoData.StoreyInfos[si].Boundary.ToPolygon();
                            var r2 = geoData.StoreyInfos[i].Boundary.ToPolygon();
                            var cPts = circlePtsf(r1);
                            var ccs = cPts.Select(x => x.UserData).Cast<GCircle>().Select(x => x.ToCirclePolygon(SUPERLATIVENESS).Tag(x)).ToList();
                            var fds = GeoFac.CreateContainsSelector(geoData.FloorDrainRings.Select(x => x.ToCirclePolygon(SUPERLATIVENESS).Tag(x)).ToList())(r2).Select(x => x.Offset(-v));
                            var fdPtsf = GeoFac.CreateIntersectsSelector(fds.Select(x => x.GetCenter().ToNTSPoint().Tag(x)).ToList());
                            var lbPtsf = GeoFac.CreateIntersectsSelector(lbDict.Where(x => IsFL(x.Value) && !IsFL0(x.Value)).Select(x => x.Key.GetCenter().ToNTSPoint().Tag(x.Value)).ToList());
                            var bufs = GeoFac.GroupGeometries(item.DLines.Concat(ccs).Concat(fds).ToList()).Select(x => x.ToGeometry().Buffer(THESAURUSHOUSING)).ToList();
                            foreach (var buf in bufs)
                            {
                                var lbPt = lbPtsf(buf).FirstOrDefault();
                                if (lbPt is not null)
                                {
                                    var lb = lbPt.UserData as string;
                                    var fdPts = fdPtsf(buf).Where(x => ccs.Any(c => c.Intersects(x))).ToList();
                                    if (fdPts.Count > THESAURUSSTAMPEDE)
                                    {
                                        drData.FdsCountAt2F[lb] = fdPts.Count;
                                    }
                                }
                            }
                        }
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
        public HashSet<string> HasOutletWrappingPipe;
        public HashSet<string> Shunts;
        public HashSet<string> _4tunes;
        public HashSet<string> HasRainPortSymbolsForFL0;
        public HashSet<string> IsConnectedToFloorDrainForFL0;
        public Dictionary<string, int> FdsCountAt2F;
        public void Init()
        {
            FdsCountAt2F ??= new Dictionary<string, int>();
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
            HasOutletWrappingPipe ??= new HashSet<string>();
        }
    }
    public class DrainageGeoData
    {
        public List<StoreyItem> StoreyItems;
        public List<GRect> Storeys;
        public List<KeyValuePair<string, Geometry>> RoomData;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public HashSet<GLineSegment> ODLines;
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
        public List<List<Geometry>> Groups;
        public HashSet<GCircle> circles;
        public List<GCircle> FloorDrainRings;
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
            Groups ??= new List<List<Geometry>>();
            StoreyItems ??= new List<StoreyItem>();
            ODLines ??= new HashSet<GLineSegment>();
            circles ??= new HashSet<GCircle>();
            FloorDrainRings ??= new List<GCircle>();
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
        public static bool IsNumStorey(string storey)
        {
            return GetStoreyScore(storey) < ushort.MaxValue;
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
            var items = pipeIds.Select(id => DrainageLabelItem.Parse(id)).Where(m => m != null).ToList();
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
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, Action f)
        {
            if (seg.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(seg.ToLineString(), f));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GLineSegment seg, List<Geometry> lst)
        {
            if (seg.IsValid) reg(fs, seg, () => { lst.Add(seg.ToLineString()); });
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, Action f)
        {
            if (r.IsValid) fs.Add(new KeyValuePair<Geometry, Action>(r.ToPolygon(), f));
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, GRect r, List<Geometry> lst)
        {
            if (r.IsValid) reg(fs, r, () => { lst.Add(r.ToPolygon()); });
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, Action f)
        {
            reg(fs, ct.Boundary, f);
        }
        public static void reg(List<KeyValuePair<Geometry, Action>> fs, CText ct, List<CText> lst)
        {
            reg(fs, ct, () => { lst.Add(ct); });
        }
        public static bool DrawWLPipeSystem()
        {
            frame = null;
            if (((DrainageSystemDiagram.commandContext.StoreyContext.StoreyInfos?.Count) ?? THESAURUSSTAMPEDE) is THESAURUSSTAMPEDE)
            {
                return INTRAVASCULARLY;
            }
            frame = GeoFac.CreateGeometry(DrainageSystemDiagram.commandContext.StoreyContext.StoreyInfos.Select(x => x.Boundary.ToPolygon())).Envelope.ToGRect().Expand(POLYOXYMETHYLENE).ToPt3dCollection();
            {
                var vm = DrainageSystemDiagramViewModel.Singleton;
                static bool isRainLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSABJURE);
                static bool isDraiLayer(string layer) => GetEffectiveLayer(layer).Contains(THESAURUSREMNANT);
                static bool isDrainageLayer(string layer) => isRainLayer(layer) || isDraiLayer(layer);
                static string GetEffectiveLayer(string entityLayer)
                {
                    return GetEffectiveName(entityLayer);
                }
                static string GetEffectiveName(string str)
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
                static string GetEffectiveBRName(string brName)
                {
                    return GetEffectiveName(brName);
                }
                static bool IsWantedBlock(BlockTableRecord blockTableRecord)
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
                bool isInXref = INTRAVASCULARLY;
                var storeyInfos = new List<StoreyInfo>();
                var gb = INTRAVASCULARLY;
                var glabelLines = new List<Geometry>();
                var gdlines = new List<Geometry>();
                var gvlines = new List<Geometry>();
                var gwlines = new List<Geometry>();
                var gcircles = new List<Geometry>();
                var gwaterbuckets = new List<Geometry>();
                var gwrappingPipes = new List<Geometry>();
                var gRainPorts = new List<Geometry>();
                var gDitches = new List<Geometry>();
                var gcts = new List<CText>();
                var gfloorDrains = new List<Geometry>();
                var gwells = new List<Geometry>();
                var gmultileaderdrainageshooters = new List<Geometry>();
                var gmultileaderwbushshooters = new List<Geometry>();
                var gpipevaluesshooters = new List<Geometry>();
                FocusMainWindow();
                if (!TrySelectPoint(out Point3d basePoint)) return INTRAVASCULARLY;
if (!ThRainSystemService.ImportElementsFromStdDwg()) return INTRAVASCULARLY;
                using var lck = DocLock;
                using var adb = AcadDatabase.Active();
                using var tr = new _DrawingTransaction(adb);
                static string TryParseWrappingPipeDNText(string text)
                {
                    if (text is null) return null;
                    var t = Regex.Replace(text, THESAURUSRESUSCITATE, THESAURUSDEPLORE, RegexOptions.IgnoreCase);
                    t = Regex.Replace(t, QUOTATION3BABOVE, THESAURUSDEPLORE);
                    t = Regex.Replace(t, THESAURUSMISTRUST, THESAURUSSPECIFICATION);
                    t = t.Replace(INTELLECTUALNESS, THESAURUSDEPLORE);
                    return t;
                }
                Point3dCollection range = frame;
                {
                    var brs = GetStoreyBlockReferences(adb);
                    var _brs = new List<BlockReference>();
                    foreach (var br in brs)
                    {
                        var info = GetStoreyInfo(br);
                        if (range?.ToGRect().ToPolygon().Contains(info.Boundary.ToPolygon()) ?? THESAURUSOBSTINACY)
                        {
                            _brs.Add(br);
                            storeyInfos.Add(info);
                        }
                    }
                    FixStoreys(storeyInfos);
                }
                var storeyst = CreateEnvelopeTester(storeyInfos.Select(x => x.Boundary).ToList());
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
                StoreyInfo getStoreyInfo(string label)
                {
                    if (label is null) return null;
                    foreach (var info in storeyInfos)
                    {
                        if (IsNumStorey(label))
                        {
                            if (info.Numbers.Contains(GetStoreyScore(label))) return info;
                        }
                        else if (label == THESAURUSARGUMENTATIVE)
                        {
                            if (info.StoreyType == StoreyType.LargeRoof) return info;
                        }
                    }
                    var smalls = storeyInfos.Where(x => x.StoreyType == StoreyType.SmallRoof).OrderByDescending(x => x.Boundary.CenterY).ToList();
                    if (label == ANTHROPOMORPHICALLY)
                    {
                        return smalls.FirstOrDefault();
                    }
                    if (label == THESAURUSSCUFFLE)
                    {
                        if (smalls.Count >= THESAURUSPERMUTATION) return smalls[THESAURUSHOUSING];
                        return null;
                    }
                    return null;
                }
                var allStoreys = new List<string>();
                var maxNumStorey = storeyInfos.SelectMany(x => x.Numbers).Max();
                for (int i = THESAURUSSTAMPEDE; i < maxNumStorey; i++)
                {
                    allStoreys.Add((i + THESAURUSHOUSING) + THESAURUSASPIRATION);
                }
                allStoreys.Add(THESAURUSARGUMENTATIVE);
                allStoreys.Add(ANTHROPOMORPHICALLY);
                allStoreys.Add(THESAURUSSCUFFLE);
                IEnumerable<string> getStoreys(StoreyInfo info) => allStoreys.Where(s => getStoreyInfo(s) == info);
                string getHigherStorey(string label)
                {
                    return allStoreys.TryGet(allStoreys.IndexOf(label) + THESAURUSHOUSING);
                }
                string getLowerStorey(string label)
                {
                    return allStoreys.TryGet(allStoreys.IndexOf(label) - THESAURUSHOUSING);
                }
                static Func<Envelope, bool> CreateEnvelopeTester(ICollection<GRect> rects)
                {
                    if (rects.Count == THESAURUSSTAMPEDE) return r => INTRAVASCULARLY;
                    var engine = new NetTopologySuite.Index.Strtree.STRtree<object>(rects.Count > THESAURUSACRIMONIOUS ? rects.Count : THESAURUSACRIMONIOUS);
                    foreach (var r in rects) engine.Insert(r.ToEnvolope(), null);
                    return envo =>
                    {
                        if (envo is null) throw new ArgumentNullException();
                        return engine.Query(envo).Any();
                    };
                }
                var gVPipes = gcircles.Select(x => new GCircle(x.GetCenter(), x.ToGRect().InnerRadius)).Where(x => x.Radius > THESAURUSDRAGOON).Select(x => x.ToCirclePolygon(SUPERLATIVENESS)).Where(x => storeyst(x.EnvelopeInternal)).ToList();
                var gcps = gcircles.Select(x => new GCircle(x.GetCenter(), x.ToGRect().InnerRadius)).Where(x => x.Radius <= THESAURUSDRAGOON).Select(x => x.ToCirclePolygon(SUPERLATIVENESS)).Where(x => storeyst(x.EnvelopeInternal)).ToList();
                    {
                        var lines = glabelLines;
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
                                        segs.AddRange(GeoFac.GetLines(new MultiLineString(GeoFac.GetManyLines(lns).Select(x => x.ToLineString()).ToArray()).Difference(new MultiPoint(pts.ToArray()).GetCenter().ToGRect(SUPERLATIVENESS).ToPolygon())).Where(x => x.Length > THESAURUSHOUSING));
                                    }
                                }
                            }
                            else
                            {
                                segs.AddRange(GeoFac.GetManyLines(lns).Select(x => x.Extend(-THESAURUSCOMMUNICATION)).Where(x => x.Length > THESAURUSHOUSING));
                            }
                        }
                        glabelLines = segs.Select(x => x.ToLineString()).OfType<Geometry>().ToList();
                    }
                {
                    var pipesf = GeoFac.CreateIntersectsSelector(gVPipes);
                    foreach (var st in gmultileaderdrainageshooters)
                    {
                        if (st.UserData is not string label) continue;
                        if (label.Contains(INTELLECTUALNESS))
                        {
                            var arr = label.Split(THESAURUSHABITAT);
                            if (arr.Length == THESAURUSPERMUTATION)
                            {
                                if (Regex.IsMatch(arr[THESAURUSHOUSING], UREDINIOMYCETES))
                                {
                                    if (IsDrainageLabel(arr[THESAURUSSTAMPEDE]))
                                    {
                                        label = arr[THESAURUSSTAMPEDE];
                                    }
                                }
                            }
                        }
                        if (IsDrainageLabel(label))
                        {
                            foreach (var vp in pipesf(st))
                            {
                                vp.UserData = label;
                            }
                        }
                    }
                    foreach (var st in gmultileaderdrainageshooters)
                    {
                        if (st.UserData is not string label) continue;
                        if (label.Contains(THESAURUSELIGIBLE))
                        {
                            if (label.Contains(THESAURUSADVENT))
                            {
                                gRainPorts.Add(GeoFac.CreateGeometryEx(GeoFac.GetPoints(st).Select(x => x.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
                            }
                            else if (label.Contains(QUOTATIONMALTESE))
                            {
                                gDitches.Add(GeoFac.CreateGeometryEx(GeoFac.GetPoints(st).Select(x => x.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
                            }
                        }
                        if (label.Contains(THESAURUSLECHER))
                        {
                            string name = null, dn = null;
                            if (label.Contains(THESAURUSPLUMMET) || label.Contains(ALSOHEAVENWARDS))
                            {
                                name = THESAURUSTOPICAL;
                            }
                            else if (label.Contains(THESAURUSPROLONG))
                            {
                                name = THESAURUSBANDAGE;
                            }
                            else if (label.Contains(QUOTATIONSPENSERIAN))
                            {
                                name = THESAURUSCONSERVATION;
                            }
                            var m = Regex.Match(label, UREDINIOMYCETES);
                            if (m.Success) dn = m.Groups[THESAURUSSTAMPEDE].Value;
                            if (name is not null)
                            {
                                dn ??= THESAURUSIMPETUOUS;
                                foreach (var pt in GeoFac.GetPoints(st))
                                {
                                    gwaterbuckets.Add(pt.Tag(name + INTELLECTUALNESS + dn));
                                }
                            }
                        }
                    }
                    var ctsf = GeoFac.CreateIntersectsSelector(gcts.Select(x => x.ToPolygon()).ToList());
                    foreach (var geo in GeoFac.GroupGeometries(GeoFac.GetManyLines(glabelLines).Select(x => x.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList()).Select(x => GeoFac.CreateGeometry(x)))
                    {
                        var labellines = GeoFac.GetLines(geo).ToList();
                        var hlines = labellines.Where(x => x.IsHorizontal(THESAURUSCOMMUNICATION)).ToList();
                        var pts = GeoFac.GetAlivePoints(labellines, THESAURUSCOMMUNICATION).Select(x => x.ToNTSPoint()).Where(x => !hlines.Any(seg => seg.Buffer(THESAURUSCOMMUNICATION, EndCapStyle.Square).Intersects(x))).ToList();
                        if (hlines.Count == THESAURUSHOUSING && pts.Count > THESAURUSSTAMPEDE)
                        {
                            foreach (var seg in hlines)
                            {
                                var p = seg.Center;
                                {
                                    var dy = THESAURUSARRIVE;
                                    var pls = ctsf(new GLineSegment(p, p.OffsetY(dy)).ToLineString()).Where(x => IsDrainageLabel(x.UserData as string)).ToList();
                                    for (int i = THESAURUSHOUSING; i <= DISPENSABLENESS; i++)
                                    {
                                        if (pls.Count > THESAURUSSTAMPEDE) break;
                                        dy = HYPERDISYLLABLE + i * THESAURUSACRIMONIOUS;
                                        pls = ctsf(new GLineSegment(p, p.OffsetY(dy)).ToLineString()).Where(x => IsDrainageLabel(x.UserData as string)).ToList();
                                    }
                                    if (pls.Count == THESAURUSHOUSING)
                                    {
                                        foreach (var pl in pls)
                                        {
                                            foreach (var pt in pts)
                                            {
                                                pt.UserData = pl.UserData;
                                                foreach (var pipe in pipesf(pt))
                                                {
                                                    if (pipe.UserData is not null)
                                                    {
                                                        continue;
                                                    }
                                                    pipe.UserData = pt.UserData;
                                                }
                                            }
                                        }
                                    }
                                }
                                {
                                    static bool IsWantedLabel(string label)
                                    {
                                        if (label is null) return INTRAVASCULARLY;
                                        return label.Contains(THESAURUSELIGIBLE);
                                    }
                                    var pls = ctsf(new GLineSegment(p, p.OffsetY(THESAURUSSURPRISED)).ToLineString()).Where(x => IsWantedLabel(x.UserData as string)).ToList();
                                    if (pls.Count == THESAURUSHOUSING)
                                    {
                                        foreach (var pl in pls)
                                        {
                                            var label = pl.UserData as string;
                                            foreach (var pt in pts)
                                            {
                                                pt.UserData = pl.UserData;
                                            }
                                            if (label.Contains(THESAURUSADVENT))
                                            {
                                                gRainPorts.Add(GeoFac.CreateGeometryEx(pts.Select(x => x.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
                                            }
                                            else if (label.Contains(QUOTATIONMALTESE))
                                            {
                                                gDitches.Add(GeoFac.CreateGeometryEx(pts.Select(x => x.ToPoint2d().ToGRect(THESAURUSENTREPRENEUR).ToPolygon()).ToList()));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                var vPipesct = GeoFac.CreateContainsSelector(gVPipes);
                var silabels = new List<HashSet<string>>();
                var siwells = new List<List<Geometry>>();
                var sifds = new List<List<Geometry>>();
                var sicps = new List<List<Geometry>>();
                var siLabels = new List<List<HashSet<string>>>();
                var siOutlets = new List<Dictionary<string, string>>();
                var siWpRadiusD = new List<Dictionary<string, string>>();
                var siWaterBucketD = new List<Dictionary<string, string>>();
                var siwps = new List<List<Geometry>>();
                var siHasLong = new List<HashSet<string>>();
                var siHasShort = new List<HashSet<string>>();
                using (var prq = new PriorityQueue(THESAURUSINCOMPLETE))
                {
                    foreach (var info in storeyInfos)
                    {
                        var labels = new HashSet<string>();
                        silabels.Add(labels);
                        var labelss = new List<HashSet<string>>();
                        siLabels.Add(labelss);
                        var wells = new List<Geometry>();
                        siwells.Add(wells);
                        var fds = new List<Geometry>();
                        sifds.Add(fds);
                        var cps = new List<Geometry>();
                        sicps.Add(cps);
                        var outletD = new Dictionary<string, string>();
                        siOutlets.Add(outletD);
                        var wpRadiusdD = new Dictionary<string, string>();
                        siWpRadiusD.Add(wpRadiusdD);
                        var wps = new List<Geometry>();
                        siwps.Add(wps);
                        var waterBucketD = new Dictionary<string, string>();
                        siWaterBucketD.Add(waterBucketD);
                        var hasLongHS = new HashSet<string>();
                        siHasLong.Add(hasLongHS);
                        var hasShortHS = new HashSet<string>();
                        siHasShort.Add(hasShortHS);
                        var bdpl = info.Boundary.ToPolygon();
                        var vps = vPipesct(bdpl);
                        var vpsf = GeoFac.CreateIntersectsSelector(vps);
                        var fdsf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gfloorDrains)(bdpl));
                        var cpsf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gcps)(bdpl));
                        var wpsf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gwrappingPipes)(bdpl));
                        var multileaderwbushshootersf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gmultileaderwbushshooters)(bdpl));
                        var rainPortsf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gRainPorts)(bdpl));
                        var ditchesf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gDitches)(bdpl));
                        var wellsf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gwells.Select(x => x.Buffer(THESAURUSDERELICTION)).ToList())(bdpl));
                        var wlineGeosf = GeoFac.CreateIntersectsSelector(GeoFac.GroupLinesByConnPoints(GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(GeoFac.GetManyLines(gwlines).Select(x => x.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList())(bdpl))(bdpl), THESAURUSCOMMUNICATION).ToList());
                        var dlineGeosf = GeoFac.CreateIntersectsSelector(GeoFac.GroupLinesByConnPoints(GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(GeoFac.GetManyLines(gdlines).Select(x => x.Extend(THESAURUSCOMMUNICATION).ToLineString()).ToList())(bdpl))(bdpl), THESAURUSCOMMUNICATION).ToList());
                        prq.Enqueue((int)GeoCalState.GroupPipe, () =>
                        {
                            var vps = vPipesct(info.Boundary.ToPolygon());
                            var vpsnf = GeoFac.NearestNeighboursGeometryF(vps.OfType<Geometry>().ToList());
                            var okvps = new HashSet<Geometry>();
                            foreach (var vp in vps)
                            {
                                var label = vp.UserData as string;
                                if (!IsDraiLabel(label))
                                {
                                    continue;
                                }
                                {
                                    labels.Add(label);
                                    if (IsTL(label))
                                    {
                                        var center = vp.GetCenter();
                                        var _vps = vpsnf(vp.GetCenter().ToGRect(POLYOXYMETHYLENE).ToPolygon(), vps.Count).Where(x => !okvps.Contains(x)).Where(x => x.GetCenter().GetDistanceTo(center) < POLYOXYMETHYLENE).ToList();
                                        {
                                            var fls = new List<Geometry>();
                                            var wls = new List<Geometry>();
                                            foreach (var pp in _vps)
                                            {
                                                var lb = pp.UserData as string;
                                                if (fls.Count == THESAURUSSTAMPEDE && wls.Count == THESAURUSSTAMPEDE)
                                                {
                                                    if (IsFL(lb)) fls.Add(pp);
                                                    else if (IsWL(lb)) wls.Add(pp);
                                                }
                                                else if (fls.Count == THESAURUSHOUSING && wls.Count == THESAURUSSTAMPEDE)
                                                {
                                                    if (IsWL(lb)) wls.Add(pp);
                                                }
                                                if (wls.Count == THESAURUSHOUSING && fls.Count == THESAURUSSTAMPEDE)
                                                {
                                                    if (IsFL(lb)) fls.Add(pp);
                                                }
                                                if (fls.Count == THESAURUSHOUSING && wls.Count == THESAURUSHOUSING) break;
                                            }
                                            if (fls.Count == THESAURUSHOUSING && wls.Count == THESAURUSHOUSING)
                                            {
                                                okvps.AddRange(wls);
                                                okvps.AddRange(fls);
                                                okvps.Add(vp);
                                                var lbs = new HashSet<string>();
                                                labelss.Add(lbs);
                                                foreach (var wl in wls)
                                                {
                                                    lbs.Add(wl.UserData as string);
                                                }
                                                foreach (var fl in fls)
                                                {
                                                    lbs.Add(fl.UserData as string);
                                                }
                                                lbs.Add(label);
                                                continue;
                                            }
                                        }
                                        {
                                            var pls = new List<Geometry>();
                                            var wls = new List<Geometry>();
                                            foreach (var pp in _vps)
                                            {
                                                var lb = pp.UserData as string;
                                                if (IsWL(lb))
                                                {
                                                    wls.Add(pp);
                                                    break;
                                                }
                                                if (IsPL(lb))
                                                {
                                                    pls.Add(pp);
                                                    break;
                                                }
                                            }
                                            if (pls.Count == THESAURUSHOUSING || wls.Count == THESAURUSHOUSING)
                                            {
                                                okvps.AddRange(wls);
                                                okvps.AddRange(pls);
                                                okvps.Add(vp);
                                                var lbs = new HashSet<string>();
                                                labelss.Add(lbs);
                                                foreach (var wl in wls)
                                                {
                                                    lbs.Add(wl.UserData as string);
                                                }
                                                foreach (var fl in pls)
                                                {
                                                    lbs.Add(fl.UserData as string);
                                                }
                                                lbs.Add(label);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        });
                        prq.Enqueue((int)GeoCalState.MarkCompsToPipe, () =>
                        {
                            prq.Enqueue((int)GeoCalState.MarkTranslator, () =>
                            {
                                foreach (var vp in vps)
                                {
                                    var label = vp.UserData as string;
                                    if (string.IsNullOrEmpty(label))
                                    {
                                        var ok = INTRAVASCULARLY;
                                        {
                                            var wlGeo = GeoFac.CreateGeometry(wlineGeosf(vp));
                                            foreach (var pp in vpsf(wlGeo))
                                            {
                                                var lb = pp.UserData as string;
                                                if (IsDrainageLabel(lb))
                                                {
                                                    vp.UserData = lb;
                                                    if (vp.GetCenter().GetDistanceTo(pp.GetCenter()) > THESAURUSHYPNOTIC)
                                                    {
                                                        hasLongHS.Add(lb);
                                                    }
                                                    else
                                                    {
                                                        hasShortHS.Add(lb);
                                                    }
                                                    ok = THESAURUSOBSTINACY;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!ok)
                                        {
                                            var dlGeo = GeoFac.CreateGeometry(dlineGeosf(vp));
                                            foreach (var pp in vpsf(dlGeo))
                                            {
                                                var lb = pp.UserData as string;
                                                if (IsDrainageLabel(lb))
                                                {
                                                    vp.UserData = lb;
                                                    if (vp.GetCenter().GetDistanceTo(pp.GetCenter()) > THESAURUSHYPNOTIC)
                                                    {
                                                        hasLongHS.Add(lb);
                                                        {
                                                            var shootersf = GeoFac.CreateIntersectsSelector(GeoFac.CreateContainsSelector(gpipevaluesshooters)(bdpl));
                                                            var shooters = shootersf(vp).Concat(shootersf(pp)).Distinct().ToList();
                                                            if (shooters.Count == THESAURUSPERMUTATION)
                                                            {
                                                                var m1 = Regex.Match(shooters[THESAURUSSTAMPEDE].UserData as string, NEUROTRANSMITTER);
                                                                var m2 = Regex.Match(shooters[THESAURUSHOUSING].UserData as string, NEUROTRANSMITTER);
                                                                if (m1.Success && m2.Success)
                                                                {
                                                                    var arr = new double[] { double.Parse(m1.Groups[THESAURUSHOUSING].Value), double.Parse(m2.Groups[THESAURUSHOUSING].Value) };
                                                                    var min = arr.Min();
                                                                    var max = arr.Max();
                                                                    if (max / min > THESAURUSPERMUTATION)
                                                                    {
                                                                        hasLongHS.Remove(lb);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        hasShortHS.Add(lb);
                                                    }
                                                    ok = THESAURUSOBSTINACY;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                {
                                    var shooters = GeoFac.CreateContainsSelector(gmultileaderdrainageshooters)(bdpl);
                                    foreach (var g in shooters.Where(x => IsDrainageLabel(x.UserData as string)).GroupBy(x => x.UserData))
                                    {
                                        var lst = g.ToList();
                                        if (lst.Count == THESAURUSPERMUTATION)
                                        {
                                            if (lst[THESAURUSSTAMPEDE].GetCenter().GetDistanceTo(lst[THESAURUSHOUSING].GetCenter()) > THESAURUSHYPNOTIC)
                                            {
                                                hasLongHS.Add(g.Key as string);
                                            }
                                        }
                                    }
                                }
                                foreach (var vp in vps)
                                {
                                    var label = vp.UserData as string;
                                    if (string.IsNullOrEmpty(label))
                                    {
                                        foreach (var pp in vpsf(vp.ToGRect().Expand(THESAURUSENTREPRENEUR).ToPolygon()))
                                        {
                                            var lb = pp.UserData as string;
                                            if (IsDrainageLabel(lb))
                                            {
                                                vp.UserData = lb;
                                                hasShortHS.Add(lb);
                                                break;
                                            }
                                        }
                                    }
                                }
                            });
                            prq.Enqueue((int)GeoCalState.MarkCompsToPipe, () =>
                            {
                                foreach (var vp in vps)
                                {
                                    var label = vp.UserData as string;
                                    if (IsDrainageLabel(label))
                                    {
                                        labels.Add(label);
                                        var wlGeo = GeoFac.CreateGeometry(wlineGeosf(vp));
                                        foreach (var wp in wpsf(wlGeo))
                                        {
                                            foreach (var st in multileaderwbushshootersf(wp))
                                            {
                                                var text = TryParseWrappingPipeDNText(st.UserData as string);
                                                if (text is not null)
                                                {
                                                    wpRadiusdD[label] = text;
                                                    break;
                                                }
                                            }
                                            wps.Add(wp.Clone().Tag(label));
                                        }
                                        if (IsRainLabel(label))
                                        {
                                            foreach (var fd in fdsf(wlGeo))
                                            {
                                                fds.Add(fd.Clone().Tag(label));
                                            }
                                            foreach (var cp in cpsf(wlGeo))
                                            {
                                                cps.Add(cp.Clone().Tag(label));
                                            }
                                            if (rainPortsf(wlGeo).Any())
                                            {
                                                outletD[label] = THESAURUSADVENT;
                                            }
                                            else if (ditchesf(wlGeo).Any())
                                            {
                                                outletD[label] = VICISSITUDINOUS;
                                            }
                                            else if (wellsf(wlGeo).Any())
                                            {
                                                outletD[label] = THESAURUSINTENTIONAL;
                                            }
                                        }
                                        else if (IsDraiLabel(label))
                                        {
                                            if (IsDraiFL(label))
                                            {
                                                foreach (var fd in fdsf(GeoFac.CreateGeometry(dlineGeosf(vp))))
                                                {
                                                    fds.Add(fd.Clone().Tag(label));
                                                }
                                            }
                                        }
                                    }
                                }
                            });
                        });
                        prq.Enqueue((int)GeoCalState.MarkWaterBucketToPipe, () =>
                        {
                            foreach (var bk in GeoFac.CreateContainsSelector(gwaterbuckets)(bdpl))
                            {
                                var storeys = getStoreys(info).ToList();
                                if (storeys.Count > THESAURUSSTAMPEDE)
                                {
                                    var s = storeys[THESAURUSSTAMPEDE];
                                    var lower = getLowerStorey(s);
                                    if (lower is not null)
                                    {
                                        var lowerInfo = getStoreyInfo(lower);
                                        if (lowerInfo is not null)
                                        {
                                            var v = lowerInfo.ContraPoint - info.ContraPoint;
                                            var bktext = bk.UserData as string;
                                            var shooter = (bk.GetCenter() + v).ToGRect(QUOTATIONWITTIG).ToPolygon();
                                            if (!string.IsNullOrEmpty(bktext))
                                            {
                                                var vps = vPipesct(lowerInfo.Boundary.ToPolygon());
                                                foreach (var vp in vps)
                                                {
                                                    var label = vp.UserData as string;
                                                    if (IsY1L(label))
                                                    {
                                                        if (shooter.Intersects(vp))
                                                        {
                                                            waterBucketD[label] = bktext;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
                var allLabels = silabels.SelectMany(x => x).ToHashSet();
                List<PipeLine> pipeLines = new();
                static bool IsDraiType(PipeType type)
                {
                    var s = type.ToString();
                    return type != PipeType.FL0 && (s.Contains(THESAURUSBASELESS) || s.Contains(THESAURUSDECLAIM) || s.Contains(THESAURUSPOSSESSIVE) || s.Contains(INCORRESPONDENCE) || s.Contains(THESAURUSCONFIRM));
                }
                static bool IsRainType(PipeType type)
                {
                    return type is PipeType.Y1L or PipeType.Y2L or PipeType.YL or PipeType.NL or PipeType.FL0;
                }
                static PipeType GetPipeType(string label)
                {
                    if (IsY1L(label)) return PipeType.Y1L;
                    if (IsY2L(label)) return PipeType.Y2L;
                    if (IsNL(label)) return PipeType.NL;
                    if (IsYL(label)) return PipeType.YL;
                    if (IsFL0(label)) return PipeType.FL0;
                    if (IsFL(label)) return PipeType.FL;
                    if (IsPL(label)) return PipeType.PL;
                    if (IsWL(label)) return PipeType.WL;
                    if (IsDL(label)) return PipeType.DL;
                    if (IsTL(label)) return PipeType.TL;
                    return PipeType.Unknown;
                }
                var okLabels = new HashSet<string>();
                foreach (var label in allLabels)
                {
                    if (okLabels.Contains(label)) continue;
                    var pipeLine = new PipeLine();
                    var lineLabels = new HashSet<string>() { label };
                    pipeLines.Add(pipeLine);
                    PipeType pipeType = default;
                    string outlet = default;
                    string wpRadius = default;
                    for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                    {
                        var s = allStoreys[i];
                        var info = getStoreyInfo(s);
                        string getWaterBucket()
                        {
                            if (info is null) return null;
                            siWaterBucketD[storeyInfos.IndexOf(info)].TryGetValue(label, out var bk);
                            return bk;
                        }
                        bool getHasLong()
                        {
                            if (info is null) return INTRAVASCULARLY;
                            var ret = siHasLong[storeyInfos.IndexOf(info)].Contains(label);
                            if (ret)
                            {
                                var lst = getStoreys(info).ToList();
                                if (lst.Count > THESAURUSHOUSING)
                                {
                                    return s == lst.OrderBy(GetStoreyScore).Last();
                                }
                            }
                            return ret;
                        }
                        bool getHasShort()
                        {
                            if (info is null) return INTRAVASCULARLY;
                            var ret = !getHasLong() && siHasShort[storeyInfos.IndexOf(info)].Contains(label);
                            if (ret)
                            {
                                var lst = getStoreys(info).ToList();
                                if (lst.Count > THESAURUSHOUSING)
                                {
                                    return s == lst.OrderBy(GetStoreyScore).Last();
                                }
                            }
                            return ret;
                        }
                        int getWpsCount()
                        {
                            if (info is null) return THESAURUSSTAMPEDE;
                            var c = THESAURUSSTAMPEDE;
                            foreach (var wp in siwps[storeyInfos.IndexOf(info)])
                            {
                                if (wp.UserData as string == label) ++c;
                            }
                            return c;
                        }
                        int getFdsCount()
                        {
                            if (info is null) return THESAURUSSTAMPEDE;
                            var c = THESAURUSSTAMPEDE;
                            foreach (var fd in sifds[storeyInfos.IndexOf(info)])
                            {
                                if (fd.UserData as string == label) ++c;
                            }
                            if (c > THESAURUSPERMUTATION) c = THESAURUSPERMUTATION;
                            return c;
                        }
                        int getCpsCount()
                        {
                            if (info is null) return THESAURUSSTAMPEDE;
                            var c = THESAURUSSTAMPEDE;
                            foreach (var cp in sicps[storeyInfos.IndexOf(info)])
                            {
                                if (cp.UserData as string == label) ++c;
                            }
                            if (c > THESAURUSPERMUTATION) c = THESAURUSPERMUTATION;
                            return c;
                        }
                        bool getExists()
                        {
                            if (info is null) return INTRAVASCULARLY;
                            if (silabels[storeyInfos.IndexOf(info)].Contains(label)) return THESAURUSOBSTINACY;
                            if (siLabels[storeyInfos.IndexOf(info)].SelectMany(x => x).Contains(label)) return THESAURUSOBSTINACY;
                            return INTRAVASCULARLY;
                        }
                        string getWpRadius()
                        {
                            if (i != THESAURUSSTAMPEDE) return null;
                            if (info is null) return null;
                            siWpRadiusD[storeyInfos.IndexOf(info)].TryGetValue(label, out var r);
                            return r;
                        }
                        string getOutlet()
                        {
                            if (IsDraiType(getPipeType())) return THESAURUSPAGEANT;
                            if (i != THESAURUSSTAMPEDE) return null;
                            if (info is null) return null;
                            siOutlets[storeyInfos.IndexOf(info)].TryGetValue(label, out var outlet);
                            return outlet;
                        }
                        PipeType getPipeType()
                        {
                            if (info is null) return PipeType.Unknown;
                            var labels = siLabels[storeyInfos.IndexOf(info)].FirstOrDefault(x => x.Contains(label));
                            if (labels is null)
                            {
                                okLabels.Add(label);
                                lineLabels.Add(label);
                                return GetPipeType(label);
                            }
                            okLabels.AddRange(labels);
                            lineLabels.AddRange(labels);
                            if (labels.Count == INTROPUNITIVENESS)
                            {
                                return PipeType.WLTLFL;
                            }
                            if (labels.Count == THESAURUSPERMUTATION)
                            {
                                if (labels.Any(IsTL) && labels.Any(IsPL)) return PipeType.PLTL;
                                if (labels.Any(IsTL) && labels.Any(IsWL)) return PipeType.WLTL;
                            }
                            return PipeType.Unknown;
                        }
                        var type = getPipeType();
                        if (pipeType == PipeType.Unknown) pipeType = type;
                        pipeLine.Runs.Add(new() { Index = i, Storey = s, HasLong = INTRAVASCULARLY, HasShort = INTRAVASCULARLY, Exists = getExists(), });
                        var r = pipeLine.Runs.Last();
                        if (r.Exists)
                        {
                            r.HasLong = getHasLong();
                            r.HasShort = getHasShort();
                            r.FDSCount = getFdsCount();
                            r.CPSCount = getCpsCount();
                            r.WPSCount = getWpsCount();
                            if (r.WPSCount > r.FDSCount) r.WPSCount = r.FDSCount;
                            outlet ??= getOutlet();
                            wpRadius ??= getWpRadius();
                            if (pipeType.ToString().Contains(THESAURUSDECLAIM))
                            {
                                if (GetStoreyScore(s) < GetStoreyScore(allStoreys[maxNumStorey]))
                                {
                                    r.HasCleaningPort = THESAURUSOBSTINACY;
                                }
                            }
                        }
                        r.WaterBucket = getWaterBucket();
                    }
                    for (int i = pipeLine.Runs.Count - THESAURUSHOUSING; i >= THESAURUSSTAMPEDE;)
                    {
                        var r = pipeLine.Runs[i];
                        if (r.WaterBucket is not null)
                        {
                            --i;
                            while (i >= THESAURUSSTAMPEDE)
                            {
                                pipeLine.Runs[i].WaterBucket = null;
                                --i;
                            }
                        }
                        else
                        {
                            --i;
                        }
                    }
                    if (pipeType == PipeType.Unknown) pipeType = PipeType.FL;
                    pipeLine.PipeType = pipeType;
                    pipeLine.Outlet = outlet;
                    wpRadius ??= THESAURUSRECTIFY;
                    pipeLine.WPRadius = wpRadius;
                    if (pipeType == PipeType.FL)
                    {
                        if (pipeLine.Runs.Where(r => r.Exists).All(x => x.FDSCount == THESAURUSSTAMPEDE))
                        {
                            foreach (var r in pipeLine.Runs)
                            {
                                if (r.Exists)
                                {
                                    if (GetStoreyScore(r.Storey) < GetStoreyScore(allStoreys[maxNumStorey]))
                                    {
                                        r.HasBasin = THESAURUSOBSTINACY;
                                    }
                                }
                            }
                        }
                    }
                    pipeLine.Labels.AddRange(lineLabels.OrderBy(y => GetPipeType(y)).ThenBy(y => y));
                }
                var gpItems = pipeLines.GroupBy(x => x).Select(x => x.OrderBy(y => GetPipeType(y.Labels.First())).ThenBy(y => y.Labels.First()).ToList()).OrderBy(x => GetPipeType(x.First().Labels.First())).ThenBy(x => x.First().Labels.First()).ToList();
                using (var prq = new PriorityQueue(THESAURUSINCOMPLETE))
                {
                    var textInfos = new List<DBTextInfo>(THESAURUSREPERCUSSION);
                    var brInfos = new List<BlockInfo>(THESAURUSREPERCUSSION);
                    var lineInfos = new List<LineInfo>(THESAURUSREPERCUSSION);
                    var circleInfos = new List<CircleInfo>(THESAURUSREPERCUSSION);
                    var dimInfos = new List<DimInfo>(THESAURUSREPERCUSSION);
                    var lm = new DrainageLayoutManager(textInfos, brInfos, lineInfos, circleInfos, dimInfos);
                    var HEIGHT = THESAURUSINCOMING;
                    var heights = new List<int>(allStoreys.Count);
                    {
                        var s = THESAURUSSTAMPEDE;
                        var _vm = FloorHeightsViewModel.Instance;
                        for (int i = THESAURUSSTAMPEDE; i < allStoreys.Count; i++)
                        {
                            heights.Add(s);
                            var v = _vm.GeneralFloor;
                            if (_vm.ExistsSpecialFloor) v = _vm.Items.FirstOrDefault(m => test(m.Floor, GetStoreyScore(allStoreys[i])))?.Height ?? v;
                            s += v;
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
                        }
                    }
                    const double rfoy = ThWSDStorey.RF_OFFSET_Y;
                    var jCount = gpItems.Count;
                    var SPANX = BALANOPHORACEAE;
                    var DETECTLENGTH = SPANX * jCount * QUOTATIONEDIBLE;
                    var OFFSETX1 = THESAURUSLEGISLATION;
                    var OFFSETX2 = LAUTENKLAVIZIMBEL;
                    var lineLen = OFFSETX1 + jCount * SPANX + OFFSETX2;
                    Point3d GetBasePoint(int i, int j)
                    {
                        return GetStoreyBasePoint(i).OffsetX(OFFSETX1 + j * SPANX);
                    }
                    Point3d GetStoreyBasePoint(int i)
                    {
                        return basePoint.OffsetY(HEIGHT * i);
                    }
                    foreach (var i in Enumerable.Range(THESAURUSSTAMPEDE, allStoreys.Count).OrderByDescending(x => x))
                    {
                        var storey = allStoreys[i];
                        string getStoreyHeightText()
                        {
                            if (storey is THESAURUSREGION) return MULTINATIONALLY;
                            var ret = (heights[i] / LAUTENKLAVIZIMBEL).ToString(THESAURUSINFINITY); ;
                            if (ret == THESAURUSINFINITY) return MULTINATIONALLY;
                            return ret;
                        }
                        var bsPt1 = GetStoreyBasePoint(i);
                        DrawStoreyLine(storey, bsPt1, lineLen, getStoreyHeightText());
                        void DrawStoreyLine(string label, Point3d basePt, double lineLen, string text)
                        {
                            {
                                var line = DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
                                Dr.SetLabelStylesForWNote(line, dbt);
                                DrawBlockReference(blkName: THESAURUSSUPERFICIAL, basePt: basePt.OffsetX(QUOTATIONPITUITARY), layer: COSTERMONGERING, props: new Dictionary<string, string>() { { THESAURUSSUPERFICIAL, text } });
                            }
                            if (label == THESAURUSARGUMENTATIVE)
                            {
                                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + rfoy, THESAURUSSTAMPEDE), new Point3d(basePt.X + lineLen, basePt.Y + rfoy, THESAURUSSTAMPEDE));
                                var dbt = DrawTextLazy(THESAURUSSHADOWY, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + rfoy + ThWSDStorey.INDEX_TEXT_OFFSET_Y, THESAURUSSTAMPEDE));
                                Dr.SetLabelStylesForWNote(line, dbt);
                            }
                        }
                    }
                    var iRF = allStoreys.IndexOf(THESAURUSARGUMENTATIVE);
                    var iMax = allStoreys.Count - THESAURUSHOUSING;
                    static string GetPipeLayer(PipeType pipeType)
                    {
                        if (pipeType is PipeType.TL) return THUNDERSTRICKEN;
                        if (pipeType is PipeType.FL) return THESAURUSADVERSITY;
                        return IsDraiType(pipeType) ? THESAURUSCONTROVERSY : INSTRUMENTALITY;
                    }
                    static string GetNoteLayer(PipeType pipeType)
                    {
                        return IsDraiType(pipeType) ? THESAURUSSTRIPED : CIRCUMCONVOLUTION;
                    }
                    static string GetEQPMLayer(PipeType pipeType)
                    {
                        return IsDraiType(pipeType) ? THESAURUSJUBILEE : DENDROCHRONOLOGIST;
                    }
                    foreach (var j in Enumerable.Range(THESAURUSSTAMPEDE, jCount))
                    {
                        var gpItem = gpItems[j].First();
                        var labels = gpItems[j].SelectMany(x => x.Labels).ToHashSet();
                        var eMax = gpItem.Runs.Where(x => x.Exists).Select(x => x.Index).Max();
                        var pipeLineInfos = new List<LineInfo>(THESAURUSREPERCUSSION);
                        var dx = QUOTATIONTRANSFERABLE;
                        var vsels = new List<Point3d>();
                        var vkills = new List<Point3d>();
                        var vdrills = new List<GRect>();
                        foreach (var i in Enumerable.Range(THESAURUSSTAMPEDE, allStoreys.Count).OrderByDescending(x => x))
                        {
                            var px = GetBasePoint(i, j).OffsetX(dx);
                            if (gpItem.Outlet is null)
                            {
                                prq.Enqueue((int)LayoutState.SanPaiMark, () =>
                                {
                                    var eMin = gpItem.Runs.Where(x => x.Exists).Select(x => x.Index).Min();
                                    if (eMin is THESAURUSSTAMPEDE) return;
                                    if (i == eMin)
                                    {
                                        if (gpItem.Runs.TryGet(i + THESAURUSHOUSING)?.WaterBucket is not null)
                                        {
                                            vsels.Add(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSURPRISED));
                                            vkills.Add(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSURPRISED - ASSOCIATIONISTS));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSURPRISED), px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONPITUITARY)), CIRCUMCONVOLUTION));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONPITUITARY), px.OffsetXY(-REPRESENTATIONAL, -QUOTATIONPITUITARY)), CIRCUMCONVOLUTION));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSFRISKY, -QUOTATIONWITTIG), THESAURUSEXECUTIVE + gpItem.Runs[i].Storey, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                        }
                                    }
                                });
                            }
                            if (gpItem.Runs[i].WaterBucket is not null)
                            {
                                var bk = gpItem.Runs[i].WaterBucket;
                                var name = bk.Split(THESAURUSHABITAT)[THESAURUSSTAMPEDE];
                                var dn = bk.Split(THESAURUSHABITAT)[THESAURUSHOUSING];
                                if (name.Contains(THESAURUSPROLONG) || name.Contains(QUOTATIONSPENSERIAN))
                                {
                                    var p = px;
                                    px = px.OffsetY(THESAURUSUNDERSTANDING);
                                    if (i == iRF) px = px.OffsetY(rfoy);
                                    brInfos.Add(new BlockInfo(THESAURUSCURDLE, DENDROCHRONOLOGIST, px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSBEHOVE)));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-CONSUMMATIVENESS, UNACCEPTABLENESS), px.OffsetXY(-THESAURUSDEPUTIZE, THESAURUSHEARTLESS)), CIRCUMCONVOLUTION));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDEPUTIZE, THESAURUSHEARTLESS), px.OffsetXY(-QUOTATIONWORSTED, THESAURUSHEARTLESS)), CIRCUMCONVOLUTION));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-QUOTATIONWORSTED, THESAURUSHEARTLESS), name, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSVISIBLE, QUOTATIONETHIOPS), dn, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                    vsels.Add(px.OffsetY(-THESAURUSBEHOVE));
                                    vkills.Add(px.OffsetY(-THESAURUSBEHOVE + ASSOCIATIONISTS));
                                    px = p;
                                }
                                else
                                {
                                    var p = px;
                                    if (i == iRF) px = px.OffsetY(rfoy);
                                    brInfos.Add(new BlockInfo(THESAURUSPRECOCIOUS, DENDROCHRONOLOGIST, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, PHOSPHORYLATION), px.OffsetXY(-THESAURUSMISUNDERSTANDING, THESAURUSINSTITUTE)), CIRCUMCONVOLUTION));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSMISUNDERSTANDING, THESAURUSINSTITUTE), px.OffsetXY(-THESAURUSINCONCLUSIVE, THESAURUSINSTITUTE)), CIRCUMCONVOLUTION));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSINCONCLUSIVE, THESAURUSINSTITUTE), name, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-ALSOBIPINNATISECT, CATECHOLAMINERGIC), dn, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                    px = p;
                                }
                                if (eMax + THESAURUSHOUSING == i)
                                {
                                    lm.TakeStartSnap();
                                    if (i == iRF)
                                    {
                                        lineInfos.Add(new LineInfo(new GLineSegment(px, px.OffsetXY(THESAURUSSTAMPEDE, rfoy)), INSTRUMENTALITY));
                                    }
                                    if (gpItem.Runs[i - THESAURUSHOUSING].HasLong || gpItem.Runs[i - THESAURUSHOUSING].HasShort)
                                    {
                                        var p = px;
                                        px = px.OffsetY(-HEIGHT);
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC), px.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE)), INSTRUMENTALITY));
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE), px.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE)), INSTRUMENTALITY));
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE), px.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS)), INSTRUMENTALITY));
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC)), INSTRUMENTALITY));
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS), px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), INSTRUMENTALITY));
                                        px = p;
                                    }
                                    else
                                    {
                                        lineInfos.Add(new LineInfo(new GLineSegment(px, px.OffsetXY(THESAURUSSTAMPEDE, -HEIGHT)), INSTRUMENTALITY));
                                    }
                                    lm.TakeStopSnap();
                                    pipeLineInfos.AddRange(lm.GetLineInfos());
                                    var pts = lm.GetLineVertices().OrderByDescending(x => x.Y).ToList();
                                    if (pts.Count > THESAURUSSTAMPEDE) dx += pts.Last().X - pts.First().X;
                                }
                            }
                            if (gpItem.Runs[i].Exists)
                            {
                                {
                                    if (gpItem.PipeType is PipeType.PLTL or PipeType.WLTL)
                                    {
                                        lm.TakeStartSnap();
                                        if (i == THESAURUSSTAMPEDE)
                                        {
                                            if (gpItem.Runs[i].HasLong)
                                            {
                                                brInfos.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, px.OffsetXY(-THESAURUSSMOULDER, THESAURUSBEAUTIFY)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION, });
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), px.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS), px.OffsetXY(-ACETYLSALICYLIC, THESAURUSEFFULGENT)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-ACETYLSALICYLIC, THESAURUSEFFULGENT), px.OffsetXY(-ACETYLSALICYLIC, THESAURUSINDISPENSABLE)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSREPRESSIVE)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSREPRESSIVE), px.OffsetXY(-THESAURUSPERVADE, THESAURUSMADNESS)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, THESAURUSMADNESS), px.OffsetXY(-QUOTATIONAFGHAN, THESAURUSMADNESS)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-QUOTATIONAFGHAN, THESAURUSMADNESS), px.OffsetXY(-THESAURUSDICTATORIAL, CONTRADISTINGUISHED)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDICTATORIAL, CONTRADISTINGUISHED), px.OffsetXY(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDICTATORIAL, ACANTHORHYNCHUS), px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSEFFICACY)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSEFFICACY), px.OffsetXY(ALSOMEGACEPHALOUS, PALAEOICHTHYOLOGY)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(ALSOMEGACEPHALOUS, PALAEOICHTHYOLOGY), px.OffsetXY(-THESAURUSSHALLOW, PALAEOICHTHYOLOGY)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSSHALLOW, PALAEOICHTHYOLOGY), px.OffsetXY(-THESAURUSEUPHORIA, IMMEASURABLENESS)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSEUPHORIA, IMMEASURABLENESS), px.OffsetXY(-THESAURUSEUPHORIA, ALSOWATERLANDIAN)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSEUPHORIA, ALSOWATERLANDIAN), px.OffsetXY(-THESAURUSDICTATORIAL, INTERNATIONALLY)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-ACETYLSALICYLIC, THESAURUSINDISPENSABLE), px.OffsetXY(-THESAURUSWINDING, THESAURUSBEAUTIFY)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSWINDING, THESAURUSBEAUTIFY), px.OffsetXY(-THESAURUSSMOULDER, THESAURUSBEAUTIFY)), THESAURUSCONTROVERSY));
                                                brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                                dimInfos.Add(new DimInfo(px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE), px.OffsetXY(-THESAURUSDICTATORIAL, INTERNATIONALLY), new(POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, QUOTATIONBENJAMIN));
                                                dimInfos.Add(new DimInfo(px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE), px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSDOMESTIC), new(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), CONSECUTIVENESS, QUOTATIONBENJAMIN));
                                            }
                                            else
                                            {
                                                brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                                brInfos.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION });
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSDISCERNIBLE)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSDISCERNIBLE), px.OffsetXY(THESAURUSSTAMPEDE, INTERNATIONALLY)), THUNDERSTRICKEN));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), px.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                                                dimInfos.Add(new DimInfo(px, px.OffsetXY(THESAURUSSTAMPEDE, INTERNATIONALLY), new(POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, QUOTATIONBENJAMIN));
                                                dimInfos.Add(new DimInfo(px, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC), new(-POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), CONSECUTIVENESS, QUOTATIONBENJAMIN));
                                            }
                                        }
                                        else if (i == eMax)
                                        {
                                            if (i == iRF)
                                            {
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, rfoy), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            }
                                        }
                                        else if (i == eMax - THESAURUSHOUSING)
                                        {
                                            brInfos.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION });
                                            brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, INTROJECTIONISM), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSEUPHORIA), px.OffsetXY(THESAURUSHYPNOTIC, INTROJECTIONISM)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), px.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                                        }
                                        else
                                        {
                                            brInfos.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION });
                                            brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSGETAWAY)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-MISAPPREHENSIVE, REPRESENTATIONAL), px.OffsetXY(MISAPPREHENSIVE, REPRESENTATIONAL)), THESAURUSSTRIPED));
                                        }
                                        lm.TakeStopSnap();
                                        pipeLineInfos.AddRange(lm.GetLineInfos().Where(x => x.LayerName.Contains(THESAURUSPENNILESS)));
                                        var pts = lm.GetLineInfos().Where(x => x.LayerName.Contains(THESAURUSPENNILESS)).Select(x => x.Line).YieldPoints().OrderByDescending(x => x.Y).ToList();
                                        if (pts.Count > THESAURUSSTAMPEDE) dx += pts.Last().X - pts.First().X;
                                    }
                                    else if (gpItem.PipeType is PipeType.WLTLFL)
                                    {
                                        lm.TakeStartSnap();
                                        if (i == THESAURUSSTAMPEDE)
                                        {
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), px.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)), THUNDERSTRICKEN));
                                        }
                                        else if (i == eMax)
                                        {
                                            if (i == iRF)
                                            {
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, rfoy), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                                            }
                                        }
                                        else if (i == eMax - THESAURUSHOUSING)
                                        {
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), px.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)), THUNDERSTRICKEN));
                                        }
                                        else
                                        {
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, QUOTATIONBASTARD), px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE)), THESAURUSCONTROVERSY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSPRIVILEGE), px.OffsetXY(THESAURUSSTAMPEDE, INCONSIDERABILIS)), THUNDERSTRICKEN));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSREFRACTORY), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDICTATORIAL)), THUNDERSTRICKEN));
                                        }
                                        lm.TakeStopSnap();
                                        pipeLineInfos.AddRange(lm.GetLineInfos().Where(x => x.LayerName.Contains(THESAURUSDEVIANT)));
                                    }
                                    else
                                    {
                                        var h = HEIGHT;
                                        if (i == iRF && i == eMax) h = rfoy;
                                        if (i != eMax || (i == iRF && i == eMax))
                                        {
                                            lm.TakeStartSnap();
                                            if (gpItem.Runs[i].HasLong)
                                            {
                                                if (i != iRF)
                                                {
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC), px.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE)), GetPipeLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, CORYNOCARPACEAE), px.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE)), GetPipeLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-QUOTATIONAFGHAN, CORYNOCARPACEAE), px.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS)), GetPipeLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD), px.OffsetXY(THESAURUSSTAMPEDE, ACETYLSALICYLIC)), GetPipeLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDICTATORIAL, PROBLEMATICALNESS), px.OffsetXY(-THESAURUSDICTATORIAL, THESAURUSSTAMPEDE)), GetPipeLayer(gpItem.PipeType)));
                                                }
                                            }
                                            else if (gpItem.Runs[i].HasShort)
                                            {
                                                if (i != iRF)
                                                {
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, COOPERATIVENESS), px.OffsetXY(THESAURUSSTAMPEDE, QUOTATIONBASTARD)), GetPipeLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, COOPERATIVENESS), px.OffsetXY(-THESAURUSPERVADE, THESAURUSSTAMPEDE)), GetPipeLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDOMESTIC, ALSOMULTIFLORAL), px.OffsetXY(THESAURUSACQUIESCENT, ALSOMULTIFLORAL)), GetNoteLayer(gpItem.PipeType)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDOMESTIC, ALSOMULTIFLORAL), px.OffsetXY(-THESAURUSHESITANCY, THESAURUSUNOCCUPIED)), GetNoteLayer(gpItem.PipeType)));
                                                    textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSDOMESTIC, THESAURUSCELESTIAL), THESAURUSTENACIOUS, GetNoteLayer(gpItem.PipeType), CONTROVERSIALLY));
                                                }
                                            }
                                            else
                                            {
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, h), px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE)), GetPipeLayer(gpItem.PipeType)));
                                            }
                                            lm.TakeStopSnap();
                                            pipeLineInfos.AddRange(lm.GetLineInfos());
                                            var pts = lm.GetLineVertices().OrderByDescending(x => x.Y).ToList();
                                            if (pts.Count > THESAURUSSTAMPEDE) dx += pts.Last().X - pts.First().X;
                                        }
                                    }
                                }
                                if (gpItem.PipeType is PipeType.FL && gpItem.Runs[i].HasBasin && i != THESAURUSSTAMPEDE && i != eMax)
                                {
                                    lm.TakeStartSnap();
                                    brInfos.Add(new BlockInfo(UNACCEPTABILITY, THESAURUSJUBILEE, px.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY)) { Scale = THESAURUSPERMUTATION, DynaDict = new() { { THESAURUSENTERPRISE, PERIODONTOCLASIA } } });
                                    brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSATTENDANT, PHOTOSYNTHETICALLY), px.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY)), THESAURUSADVERSITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, PROMORPHOLOGIST), px.OffsetXY(-THESAURUSPERVADE, PHOTOSYNTHETICALLY)), THESAURUSADVERSITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, PHOTOSYNTHETICALLY), px.OffsetXY(-THESAURUSATTENDANT, PHOTOSYNTHETICALLY)), THESAURUSADVERSITY));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSATTENDANT, THESAURUSEFFICACY), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                    lm.TakeStopSnap();
                                    var m = lm.GetSnapshot();
                                    prq.Enqueue((int)LayoutState.Basin, () =>
                                    {
                                        var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                        var pt = m.GetLineVertices().OrderBy(x => x.X).Last();
                                        var target = line.Intersection(new GLineSegment(pt.OffsetX(-SPANX), pt.OffsetX(SPANX)).ToLineString());
                                        if (target is Point p)
                                        {
                                            var v = p.ToPoint2d() - pt;
                                            m.MoveElements(v);
                                        }
                                    });
                                }
                                if (i == eMax)
                                {
                                    if (gpItem.PipeType == PipeType.Y1L)
                                    {
                                    }
                                    else
                                    {
                                        lm.TakeStartSnap();
                                        var layer = GetPipeLayer(gpItem.PipeType);
                                        if (gpItem.PipeType is PipeType.WLTLFL) layer = THUNDERSTRICKEN;
                                        var canPeopleBeOnRoof = RainSystemDiagramViewModel.Singleton.Params.CouldHavePeopleOnRoof;
                                        brInfos.Add(new BlockInfo(THESAURUSNARCOTIC, layer, px.OffsetXY(THESAURUSSTAMPEDE, i == iRF ? rfoy : THESAURUSSTAMPEDE)) { DynaDict = new() { { QUINQUAGENARIAN, i >= iRF ? (canPeopleBeOnRoof ? THESAURUSPARTNER : THESAURUSINEFFECTUAL) : HYPERVENTILATION } } });
                                        lm.TakeStopSnap();
                                        var m = lm.GetSnapshot();
                                        prq.Enqueue((int)LayoutState.FixAirBlock, () =>
                                        {
                                            var vlineInfos = pipeLineInfos.Where(x => x.Line.IsVertical(THESAURUSHOUSING)).ToList();
                                            if (vlineInfos.Count > THESAURUSSTAMPEDE)
                                            {
                                                var pt = vlineInfos.Select(x => x.Line).YieldPoints().OrderBy(x => x.Y).Last();
                                                var p = m.GetBrBasePoints().First();
                                                if (p.Y < pt.Y)
                                                {
                                                    m.MoveElements(pt - p.ToPoint2d());
                                                }
                                            }
                                        });
                                    }
                                }
                                if (gpItem.Runs[i].CPSCount == THESAURUSPERMUTATION)
                                {
                                    lm.TakeStartSnap();
                                    circleInfos.Add(new CircleInfo(px.OffsetXY(-REPRESENTATIONAL, QUOTATIONPITUITARY), VÖLKERWANDERUNG, DENDROCHRONOLOGIST));
                                    circleInfos.Add(new CircleInfo(px.OffsetXY(-INCONSIDERABILIS, QUOTATIONPITUITARY), VÖLKERWANDERUNG, DENDROCHRONOLOGIST));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSMAGNETIC), px.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER)), INSTRUMENTALITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER), px.OffsetXY(-THESAURUSDISAGREEABLE, THESAURUSENDANGER)), INSTRUMENTALITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDISAGREEABLE, THESAURUSENDANGER), px.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER)), INSTRUMENTALITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER), px.OffsetXY(-THESAURUSEUPHORIA, QUOTATIONWITTIG)), INSTRUMENTALITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDISAGREEABLE, THESAURUSENDANGER), px.OffsetXY(-THESAURUSDISAGREEABLE, QUOTATIONWITTIG)), INSTRUMENTALITY));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, THESAURUSDERELICTION), THESAURUSDISREPUTABLE, THESAURUSINVOICE, CONTROVERSIALLY));
                                    lm.TakeStopSnap();
                                    var m = lm.GetSnapshot();
                                    prq.Enqueue((int)LayoutState.CondensePipe, () =>
                                    {
                                        var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                        var pt = m.GetLineVertices().OrderBy(x => x.Y).First();
                                        var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                        if (target is Point p)
                                        {
                                            var v = p.ToPoint2d() - pt;
                                            m.MoveElements(v);
                                        }
                                    });
                                }
                                if (gpItem.Runs[i].CPSCount == THESAURUSHOUSING)
                                {
                                    lm.TakeStartSnap();
                                    circleInfos.Add(new CircleInfo(px.OffsetXY(-REPRESENTATIONAL, QUOTATIONPITUITARY), VÖLKERWANDERUNG, DENDROCHRONOLOGIST));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSMAGNETIC), px.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER)), INSTRUMENTALITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDETERMINED, THESAURUSENDANGER), px.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER)), INSTRUMENTALITY));
                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSEUPHORIA, THESAURUSENDANGER), px.OffsetXY(-THESAURUSEUPHORIA, QUOTATIONWITTIG)), INSTRUMENTALITY));
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, THESAURUSGETAWAY), THESAURUSDISREPUTABLE, THESAURUSINVOICE, CONTROVERSIALLY));
                                    lm.TakeStopSnap();
                                    var m = lm.GetSnapshot();
                                    prq.Enqueue((int)LayoutState.CondensePipe, () =>
                                    {
                                        var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                        var pt = m.GetLineVertices().OrderBy(x => x.X).Last();
                                        var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                        if (target is Point p)
                                        {
                                            var v = p.ToPoint2d() - pt;
                                            m.MoveElements(v);
                                        }
                                    });
                                }
                                if (i == THESAURUSSTAMPEDE)
                                {
                                    prq.Enqueue((int)LayoutState.Outlet, () =>
                                    {
                                        px = GetBasePoint(i, j).OffsetX(dx);
                                        if (gpItem.Runs[i].WPSCount == THESAURUSHOUSING)
                                        {
                                            brInfos.Add(new BlockInfo(THESAURUSSTRINGENT, THESAURUSDEFAULTER, px.OffsetXY(-THESAURUSDICTATORIAL, -THESAURUSBELLOW)));
                                        }
                                        if (gpItem.Runs[i].FDSCount == THESAURUSHOUSING)
                                        {
                                            if (IsRainType(gpItem.PipeType))
                                            {
                                                textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSDISAGREEABLE, -THESAURUSCOMPOUND), QUOTATIONBREWSTER, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS), px.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS), px.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS), px.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDISAGREEABLE, -REACTIONARINESS), px.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS)), INSTRUMENTALITY));
                                                brInfos.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, px.OffsetXY(THESAURUSEXCHANGE, -PORTMANTOLOGISM)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                                                var vecs = new List<Vector2d> { new Vector2d(THESAURUSSTAMPEDE, -THESAURUSBELLOW), new Vector2d(THESAURUSSTAMPEDE, -THESAURUSINHERIT), new Vector2d(-THESAURUSSCINTILLATE, THESAURUSINHERIT) };
                                                var pts = vecs.ToPoint2ds(px.ToPoint2d());
                                                dimInfos.Add(new DimInfo(pts[THESAURUSHOUSING], pts[INTROPUNITIVENESS], pts[THESAURUSPERMUTATION] - pts[THESAURUSHOUSING], METACOMMUNICATION, THESAURUSINVOICE));
                                            }
                                        }
                                        {
                                            if (IsDraiType(gpItem.PipeType))
                                            {
                                                if (gpItem.PipeType is PipeType.WLTLFL)
                                                {
                                                    brInfos.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, px.OffsetXY(-PROGNOSTICATORY, -THESAURUSHALLUCINATE)));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSHYPNOTIC, -THROMBOEMBOLISM)), THESAURUSCONTROVERSY));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, -THROMBOEMBOLISM), px.OffsetXY(ALSOMEGACEPHALOUS, -THESAURUSSATIATE)), THESAURUSCONTROVERSY));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(ALSOMEGACEPHALOUS, -THESAURUSSATIATE), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSSATIATE)), THESAURUSCONTROVERSY));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSSTAMPEDE), px.OffsetXY(-THESAURUSHYPNOTIC, -QUOTATIONQUICHE)), THESAURUSCONTROVERSY));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, -QUOTATIONQUICHE), px.OffsetXY(-THESAURUSTRAUMATIC, -STENTOROPHŌNIKOS)), THESAURUSCONTROVERSY));
                                                    lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSTRAUMATIC, -STENTOROPHŌNIKOS), px.OffsetXY(-THESAURUSINHERIT, -STENTOROPHŌNIKOS)), THESAURUSCONTROVERSY));
                                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSASTUTE, -THESAURUSASSIMILATE), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-SPECTROFLUORIMETER, -THESAURUSRUINOUS), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                }
                                                else if (gpItem.PipeType is PipeType.TL)
                                                {
                                                }
                                                else
                                                {
                                                    var layer = GetPipeLayer(gpItem.PipeType);
                                                    if (gpItem.Runs[i].HasBasin)
                                                    {
                                                        brInfos.Add(new BlockInfo(UNACCEPTABILITY, THESAURUSJUBILEE, px.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY)) { Scale = THESAURUSPERMUTATION, DynaDict = new() { { THESAURUSENTERPRISE, PERIODONTOCLASIA } } });
                                                        brInfos.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, px.OffsetXY(-DIFFERENTIATEDNESS, -THESAURUSIMPRACTICABLE)));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY), px.OffsetXY(-THESAURUSRECONCILE, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSRECONCILE, -POLYOXYMETHYLENE), px.OffsetXY(-THESAURUSHOMICIDAL, -POLYOXYMETHYLENE)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHOMICIDAL, -POLYOXYMETHYLENE), px.OffsetXY(-POLYOXYMETHYLENE, -METROPOLITANATE)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-POLYOXYMETHYLENE, -METROPOLITANATE), px.OffsetXY(-POLYOXYMETHYLENE, -THESAURUSHYPNOTIC)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-POLYOXYMETHYLENE, THESAURUSDIFFICULTY), px.OffsetXY(-POLYOXYMETHYLENE, -THESAURUSHYPNOTIC)), layer));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-IMAGINATIVENESS, -THESAURUSLOITER), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-IMAGINATIVENESS, -THESAURUSLAWYER), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                    }
                                                    else if (gpItem.Runs[i].FDSCount == THESAURUSPERMUTATION)
                                                    {
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), px.OffsetXY(OTHERWORLDLINESS, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(SCIENTIFICALNESS, -THESAURUSPRETTY), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), px.OffsetXY(TRIGONOCEPHALIC, -THESAURUSISOLATION)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSFAINTLY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(QUOTATIONBUBONIC, -THESAURUSCOMATOSE), px.OffsetXY(SCIENTIFICALNESS, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(QUOTATIONBUBONIC, -THESAURUSCOMATOSE), px.OffsetXY(QUOTATIONBUBONIC, -THESAURUSISOLATION)), layer));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSHEADSTRONG, -THESAURUSCONTEMPT), QUOTATIONDOPPLER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-QUOTATIONELECTROMOTIVE, -THESAURUSHOMICIDAL), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSNECESSITOUS, -THESAURUSMATTER), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                        brInfos.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, px.OffsetXY(-PROGNOSTICATORY, -THESAURUSIMPRACTICABLE)));
                                                        brInfos.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, px.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH)) { ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, QUOTATIONBARBADOS } }, });
                                                        brInfos.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, px.OffsetXY(REPRESENTATIONAL, -THESAURUSINTRENCH)) { ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, QUOTATIONBARBADOS } }, });
                                                    }
                                                    else if (gpItem.Runs[i].FDSCount == THESAURUSHOUSING)
                                                    {
                                                        brInfos.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, px.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSINTRENCH)) { ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, QUOTATIONBARBADOS } }, });
                                                        brInfos.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, px.OffsetXY(-PROGNOSTICATORY, -THESAURUSIMPRACTICABLE)));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), px.OffsetXY(OTHERWORLDLINESS, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(OTHERWORLDLINESS, -THESAURUSPRETTY), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSPRETTY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(TRIGONOCEPHALIC, -THESAURUSCOMATOSE), px.OffsetXY(TRIGONOCEPHALIC, -THESAURUSISOLATION)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSFAINTLY)), layer));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSABSORBENT, -OLIGOSACCHARIDES), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-QUOTATIONELECTROMOTIVE, -THESAURUSHOMICIDAL), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                    }
                                                    else if (gpItem.PipeType is PipeType.PLTL or PipeType.WLTL)
                                                    {
                                                        brInfos.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, px.OffsetXY(-THESAURUSRECONCILE, -THESAURUSIMPRACTICABLE)));
                                                        brInfos.Add(new BlockInfo(THESAURUSDENOUNCE, THESAURUSJUBILEE, px.OffsetXY(-THESAURUSDERELICTION, -THESAURUSHYPNOTIC)) { Rotate = Math.PI / THESAURUSPERMUTATION, Scale = THESAURUSPERMUTATION, });
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE)), THESAURUSCONTROVERSY));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSCOMATOSE), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY)), THESAURUSCONTROVERSY));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSPRETTY), px.OffsetXY(-THESAURUSINTEGRITY, -THESAURUSPRETTY)), THESAURUSCONTROVERSY));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSINTEGRITY, -POLYOXYMETHYLENE), px.OffsetXY(-THESAURUSDISTASTEFUL, -POLYOXYMETHYLENE)), THESAURUSCONTROVERSY));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDISTASTEFUL, -POLYOXYMETHYLENE), px.OffsetXY(-THESAURUSDERELICTION, -METROPOLITANATE)), THESAURUSCONTROVERSY));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDERELICTION, -METROPOLITANATE), px.OffsetXY(-THESAURUSDERELICTION, -THESAURUSHYPNOTIC)), THESAURUSCONTROVERSY));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSPROGRESSIVE, -THESAURUSLOITER), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSPROGRESSIVE, -THESAURUSLAWYER), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                    }
                                                    else
                                                    {
                                                        brInfos.Add(new BlockInfo(THESAURUSLANDMARK, THESAURUSJUBILEE, px.OffsetXY(-PROGNOSTICATORY, -THESAURUSHALLUCINATE)));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONCOLERIDGE), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY)), layer));
                                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSFAINTLY), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSFAINTLY)), layer));
                                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-QUOTATIONELECTROMOTIVE, -THESAURUSHOMICIDAL), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                brInfos.Add(new BlockInfo(THESAURUSSUPERFICIAL, CIRCUMCONVOLUTION, px.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY)) { PropDict = new() { { THESAURUSSUPERFICIAL, gpItem.WPRadius } } });
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW)), INSTRUMENTALITY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG)), INSTRUMENTALITY));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-REPRESENTATIONAL, -THESAURUSBELLOW), px.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY), px.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                                                textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, -INCOMPREHENSIBILIS), IRRESPONSIBLENESS, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                                switch (gpItem.Outlet)
                                                {
                                                    case THESAURUSINTENTIONAL:
                                                        {
                                                            brInfos.Add(new BlockInfo(THESAURUSGAUCHE, DENDROCHRONOLOGIST, px.OffsetXY(-PROGNOSTICATORY, -THESAURUSINSINCERE)) { PropDict = new() { { THESAURUSSPECIFICATION, THESAURUSSPECIFICATION } } });
                                                        }
                                                        break;
                                                    case THESAURUSADVENT:
                                                        {
                                                            brInfos.Add(new BlockInfo(THESAURUSEMPHASIS, DENDROCHRONOLOGIST, px.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW)));
                                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS), px.OffsetXY(-THESAURUSUNGRATEFUL, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSUPPOSITION, -QUOTATION1ASHANKS), CHRISTADELPHIAN, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                                        }
                                                        break;
                                                    case VICISSITUDINOUS:
                                                        {
                                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSINHERIT, -THESAURUSPROSPEROUS), px.OffsetXY(-THESAURUSUNGRATEFUL, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSUPPOSITION, -QUOTATION1ASHANKS), QUOTATIONSECOND, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                                        }
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                    });
                                }
                                if (i == THESAURUSSTAMPEDE)
                                {
                                    if (gpItem.PipeType is not (PipeType.WLTLFL or PipeType.WLTL or PipeType.PLTL))
                                    {
                                        lm.TakeStartSnap();
                                        brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                        dimInfos.Add(new DimInfo(px, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC), new(POLYOXYMETHYLENE, THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), THESAURUSDUBIETY, THESAURUSINVOICE));
                                        lm.TakeStopSnap();
                                        var m = lm.GetSnapshot();
                                        prq.Enqueue((int)LayoutState.CheckPoint, () =>
                                        {
                                            var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                            var pt = m.GetBrBasePoints().First();
                                            var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                            if (target is Point p)
                                            {
                                                var v = p.ToPoint3d() - pt;
                                                m.MoveElements(v);
                                            }
                                        });
                                    }
                                }
                                if (gpItem.Runs[i].FDSCount == THESAURUSPERMUTATION)
                                {
                                    if (i == THESAURUSSTAMPEDE)
                                    {
                                    }
                                    else
                                    {
                                        if (IsRainType(gpItem.PipeType))
                                        {
                                            lm.TakeStartSnap();
                                            brInfos.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, px.OffsetXY(-NEUROPSYCHIATRIST, -THESAURUSINTRENCH)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                                            brInfos.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, px.OffsetXY(NEUROPSYCHIATRIST, -THESAURUSINTRENCH)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, ScaleEx = new(-THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), });
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -SEMICONSCIOUSNESS), px.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY), px.OffsetXY(-THESAURUSEUPHORIA, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -SEMICONSCIOUSNESS), px.OffsetXY(THESAURUSDETERMINED, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDETERMINED, -QUOTATIONPITUITARY), px.OffsetXY(THESAURUSEUPHORIA, -QUOTATIONPITUITARY)), INSTRUMENTALITY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, -THESAURUSGETAWAY), QUOTATIONBREWSTER, THESAURUSINVOICE, CONTROVERSIALLY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(PSEUDEPIGRAPHOS, -THESAURUSMANIFESTATION), QUOTATIONBREWSTER, THESAURUSINVOICE, CONTROVERSIALLY));
                                            lm.TakeStopSnap();
                                            var m = lm.GetSnapshot();
                                            prq.Enqueue((int)LayoutState.FloorDrain, () =>
                                            {
                                                var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                                var pt = m.GetLineVertices().OrderBy(x => x.Y).First();
                                                var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                                if (target is Point p)
                                                {
                                                    var v = p.ToPoint2d() - pt;
                                                    m.MoveElements(v);
                                                }
                                            });
                                        }
                                        else
                                        {
                                            lm.TakeStartSnap();
                                            brInfos.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, px.OffsetXY(-MAXILLOPALATINE, -THESAURUSINTRENCH)) { ScaleEx = new(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, ACCOMMODATINGLY } }, });
                                            brInfos.Add(new BlockInfo(PERSUADABLENESS, THESAURUSJUBILEE, px.OffsetXY(-THESAURUSATTENDANT, -THESAURUSINTRENCH)) { ScaleEx = new(THESAURUSPERMUTATION, THESAURUSPERMUTATION, THESAURUSPERMUTATION), DynaDict = new() { { THESAURUSENTERPRISE, ACCOMMODATINGLY } }, });
                                            brInfos.Add(new BlockInfo(THESAURUSAGILITY, THESAURUSJUBILEE, px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSDOMESTIC)) { ScaleEx = new(-THESAURUSHOUSING, THESAURUSHOUSING, THESAURUSHOUSING) });
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-MAXILLOPALATINE, -THESAURUSINTRENCH), px.OffsetXY(-THESAURUSINDECOROUS, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSINDECOROUS, -THESAURUSEXPERIMENT), px.OffsetXY(-THESAURUSMISAPPREHEND, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-SYNAESTHETICALLY, -THESAURUSEXPERIMENT), px.OffsetXY(-THESAURUSFLUTTER, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSFLUTTER, -THESAURUSEXPERIMENT), px.OffsetXY(-THESAURUSATTENDANT, -THESAURUSINTRENCH)), THESAURUSADVERSITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSFLUTTER, -THESAURUSEXPERIMENT), px.OffsetXY(-THESAURUSALLEGIANCE, -THESAURUSEXPERIMENT)), THESAURUSADVERSITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSALLEGIANCE, -THESAURUSEXPERIMENT), px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSINTRENCH)), THESAURUSADVERSITY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSINHERIT, -THESAURUSDESTRUCTION), QUOTATIONBREWSTER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-OLIGOMENORRHOEA, -THESAURUSDESTRUCTION), QUOTATIONDOPPLER, THESAURUSSTRIPED, CONTROVERSIALLY));
                                            lm.TakeStopSnap();
                                            var m = lm.GetSnapshot();
                                            prq.Enqueue((int)LayoutState.FloorDrain, () =>
                                            {
                                                var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                                var pt = m.GetLineVertices().OrderBy(x => x.X).Last();
                                                var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                                if (target is Point p)
                                                {
                                                    var v = p.ToPoint2d() - pt;
                                                    m.MoveElements(v);
                                                }
                                            });
                                        }
                                    }
                                }
                                if (gpItem.Runs[i].FDSCount == THESAURUSHOUSING)
                                {
                                    if (i == THESAURUSSTAMPEDE)
                                    {
                                        if (INTRAVASCULARLY)
                                        {
                                            {
                                                var seg1 = new GLineSegment(px.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSBELLOW), px.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSOBSERVANCE));
                                                var seg2 = new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), px.OffsetXY(THESAURUSSTAMPEDE, -THESAURUSOBSERVANCE));
                                                var pt1 = seg1.StartPoint.ToPoint3d();
                                                var pt2 = pt1.OffsetX(seg2.X1 - seg1.X1);
                                                dimInfos.Add(new DimInfo(pt1, pt2, new(THESAURUSSTAMPEDE, -seg2.Length, THESAURUSSTAMPEDE), METACOMMUNICATION, THESAURUSINVOICE));
                                            }
                                            brInfos.Add(new BlockInfo(THESAURUSSUPERFICIAL, CIRCUMCONVOLUTION, px.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY)));
                                            brInfos.Add(new BlockInfo(THESAURUSSTRINGENT, THESAURUSDEFAULTER, px.OffsetXY(-THESAURUSDICTATORIAL, -THESAURUSBELLOW)));
                                            brInfos.Add(new BlockInfo(PERSUADABLENESS, DENDROCHRONOLOGIST, px.OffsetXY(THESAURUSEXCHANGE, -PORTMANTOLOGISM)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-REPRESENTATIONAL, -THESAURUSBELLOW), px.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-INCONSIDERABILIS, -THESAURUSDESULTORY), px.OffsetXY(-REPRESENTATIONAL, -THESAURUSDESULTORY)), CIRCUMCONVOLUTION));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSMAYHEM, -THESAURUSBELLOW), px.OffsetXY(-THESAURUSMAYHEM, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSMAYHEM, -THESAURUSPROSPEROUS), px.OffsetXY(-QUOTATIONELECTRICIAN, -THESAURUSPROSPEROUS)), CIRCUMCONVOLUTION));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG), px.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSPERVADE, -THESAURUSBELLOW), px.OffsetXY(-THESAURUSINHERIT, -THESAURUSBELLOW)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS), px.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(QUOTATIONFOREGONE, -RETROSPECTIVENESS), px.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSLOITER, -RETROSPECTIVENESS), px.OffsetXY(-THESAURUSSCINTILLATE, -THESAURUSBELLOW)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSSTAMPEDE), px.OffsetXY(THESAURUSSTAMPEDE, -QUOTATIONWITTIG)), INSTRUMENTALITY));
                                            lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDISAGREEABLE, -REACTIONARINESS), px.OffsetXY(THESAURUSDISAGREEABLE, -THESAURUSVIGOROUS)), INSTRUMENTALITY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSUNGRATEFUL, -QUOTATION1ASHANKS), QUOTATIONSECOND, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, -INCOMPREHENSIBILIS), QUOTATIONDOPPLER, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                            textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, -THESAURUSCOMPOUND), QUOTATIONBREWSTER, CIRCUMCONVOLUTION, CONTROVERSIALLY));
                                        }
                                    }
                                    else
                                    {
                                        lm.TakeStartSnap();
                                        brInfos.Add(new BlockInfo(PERSUADABLENESS, GetEQPMLayer(gpItem.PipeType), px.OffsetXY(-NEUROPSYCHIATRIST, -THESAURUSINTRENCH)) { DynaDict = new() { { THESAURUSENTERPRISE, ADENOHYPOPHYSIS } }, Scale = THESAURUSPERMUTATION, });
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, -SEMICONSCIOUSNESS), px.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY)), GetPipeLayer(gpItem.PipeType)));
                                        lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDETERMINED, -QUOTATIONPITUITARY), px.OffsetXY(-THESAURUSEUPHORIA, -QUOTATIONPITUITARY)), GetPipeLayer(gpItem.PipeType)));
                                        textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSSEDATE, -THESAURUSGETAWAY), QUOTATIONBREWSTER, GetNoteLayer(gpItem.PipeType), CONTROVERSIALLY));
                                        lm.TakeStopSnap();
                                        var m = lm.GetSnapshot();
                                        prq.Enqueue((int)LayoutState.FloorDrain, () =>
                                        {
                                            var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                            var pt = m.GetLineVertices().OrderBy(x => x.X).Last();
                                            var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                            if (target is Point p)
                                            {
                                                var v = p.ToPoint2d() - pt;
                                                m.MoveElements(v);
                                            }
                                        });
                                    }
                                }
                            }
                        }
                        string label1 = null, label2 = null, label3 = null, label4 = null, label5 = null, label6 = null;
                        if (gpItem.PipeType is PipeType.PLTL)
                        {
                            var pllabels = ConvertLabelStrings(labels.Where(IsPL)).OrderBy(x => x).ToList();
                            var tllabels = ConvertLabelStrings(labels.Where(IsTL)).OrderBy(x => x).ToList();
                            if (pllabels.Count == THESAURUSPERMUTATION)
                            {
                                label1 = pllabels[THESAURUSSTAMPEDE];
                                label2 = pllabels[THESAURUSHOUSING];
                            }
                            else if (pllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = pllabels.Count / THESAURUSPERMUTATION + pllabels.Count % THESAURUSPERMUTATION;
                                var c2 = pllabels.Count - c1;
                                label1 = pllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label2 = pllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label1 = pllabels.JoinWith(THESAURUSCAVALIER);
                                label2 = null;
                            }
                            if (tllabels.Count == THESAURUSPERMUTATION)
                            {
                                label3 = tllabels[THESAURUSSTAMPEDE];
                                label4 = tllabels[THESAURUSHOUSING];
                            }
                            else if (tllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = tllabels.Count / THESAURUSPERMUTATION + tllabels.Count % THESAURUSPERMUTATION;
                                var c2 = tllabels.Count - c1;
                                label3 = tllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label4 = tllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label3 = tllabels.JoinWith(THESAURUSCAVALIER);
                                label4 = null;
                            }
                        }
                        else if (gpItem.PipeType is PipeType.WLTL)
                        {
                            var wllabels = ConvertLabelStrings(labels.Where(IsWL)).OrderBy(x => x).ToList();
                            var tllabels = ConvertLabelStrings(labels.Where(IsTL)).OrderBy(x => x).ToList();
                            if (wllabels.Count == THESAURUSPERMUTATION)
                            {
                                label1 = wllabels[THESAURUSSTAMPEDE];
                                label2 = wllabels[THESAURUSHOUSING];
                            }
                            else if (wllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = wllabels.Count / THESAURUSPERMUTATION + wllabels.Count % THESAURUSPERMUTATION;
                                var c2 = wllabels.Count - c1;
                                label1 = wllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label2 = wllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label1 = wllabels.JoinWith(THESAURUSCAVALIER);
                                label2 = null;
                            }
                            if (tllabels.Count == THESAURUSPERMUTATION)
                            {
                                label3 = tllabels[THESAURUSSTAMPEDE];
                                label4 = tllabels[THESAURUSHOUSING];
                            }
                            else if (tllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = tllabels.Count / THESAURUSPERMUTATION + tllabels.Count % THESAURUSPERMUTATION;
                                var c2 = tllabels.Count - c1;
                                label3 = tllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label4 = tllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label3 = tllabels.JoinWith(THESAURUSCAVALIER);
                                label4 = null;
                            }
                        }
                        else if (gpItem.PipeType is PipeType.WLTLFL)
                        {
                            var wllabels = ConvertLabelStrings(labels.Where(IsWL)).OrderBy(x => x).ToList();
                            var tllabels = ConvertLabelStrings(labels.Where(IsTL)).OrderBy(x => x).ToList();
                            var fllabels = ConvertLabelStrings(labels.Where(IsDraiFL)).OrderBy(x => x).ToList();
                            if (wllabels.Count == THESAURUSPERMUTATION)
                            {
                                label1 = wllabels[THESAURUSSTAMPEDE];
                                label2 = wllabels[THESAURUSHOUSING];
                            }
                            else if (wllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = wllabels.Count / THESAURUSPERMUTATION + wllabels.Count % THESAURUSPERMUTATION;
                                var c2 = wllabels.Count - c1;
                                label1 = wllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label2 = wllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label1 = wllabels.JoinWith(THESAURUSCAVALIER);
                                label2 = null;
                            }
                            if (tllabels.Count == THESAURUSPERMUTATION)
                            {
                                label3 = tllabels[THESAURUSSTAMPEDE];
                                label4 = tllabels[THESAURUSHOUSING];
                            }
                            else if (tllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = tllabels.Count / THESAURUSPERMUTATION + tllabels.Count % THESAURUSPERMUTATION;
                                var c2 = tllabels.Count - c1;
                                label3 = tllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label4 = tllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label3 = tllabels.JoinWith(THESAURUSCAVALIER);
                                label4 = null;
                            }
                            if (fllabels.Count == THESAURUSPERMUTATION)
                            {
                                label5 = fllabels[THESAURUSSTAMPEDE];
                                label6 = fllabels[THESAURUSHOUSING];
                            }
                            else if (fllabels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = fllabels.Count / THESAURUSPERMUTATION + fllabels.Count % THESAURUSPERMUTATION;
                                var c2 = fllabels.Count - c1;
                                label5 = fllabels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label6 = fllabels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label5 = fllabels.JoinWith(THESAURUSCAVALIER);
                                label6 = null;
                            }
                        }
                        else
                        {
                            var _labels = ConvertLabelStrings(labels).OrderBy(x => x).ToList();
                            if (_labels.Count == THESAURUSPERMUTATION)
                            {
                                label1 = _labels[THESAURUSSTAMPEDE];
                                label2 = _labels[THESAURUSHOUSING];
                            }
                            else if (_labels.Count > THESAURUSPERMUTATION)
                            {
                                var c1 = _labels.Count / THESAURUSPERMUTATION + _labels.Count % THESAURUSPERMUTATION;
                                var c2 = _labels.Count - c1;
                                label1 = _labels.Take(c1).JoinWith(THESAURUSCAVALIER);
                                label2 = _labels.Skip(c1).Take(c2).JoinWith(THESAURUSCAVALIER);
                            }
                            else
                            {
                                label1 = _labels.JoinWith(THESAURUSCAVALIER);
                                label2 = null;
                            }
                        }
                        var siPipeLabels = new HashSet<int>();
                        var siPipeDNs = new HashSet<int>();
                        var c = gpItem.Runs.Count(x => x.Exists);
                        if (c > THESAURUSSTAMPEDE)
                        {
                            if (c == THESAURUSHOUSING)
                            {
                                siPipeLabels.Add(gpItem.Runs.First(x => x.Exists).Index);
                            }
                            else
                            {
                                var min = gpItem.Runs.Where(x => x.Exists).Select(x => x.Index).Min();
                                var max = gpItem.Runs.Where(x => x.Exists).Select(x => x.Index).Max();
                                if (c <= INTROPUNITIVENESS)
                                {
                                    siPipeLabels.Add(min);
                                    siPipeLabels.Add(max);
                                }
                                else if (c <= SUPERLATIVENESS)
                                {
                                    siPipeLabels.Add(min + THESAURUSHOUSING);
                                    siPipeLabels.Add(max - THESAURUSHOUSING);
                                    siPipeDNs.Add(min);
                                    siPipeDNs.Add(max);
                                }
                                else
                                {
                                    siPipeLabels.Add(min + THESAURUSPERMUTATION);
                                    siPipeLabels.Add(max - THESAURUSPERMUTATION);
                                    siPipeDNs.Add(min + THESAURUSHOUSING);
                                    siPipeDNs.Add(max - THESAURUSHOUSING);
                                }
                            }
                        }
                        foreach (var i in siPipeDNs.OrderBy(x => x))
                        {
                            var px = GetBasePoint(i, j);
                            if (gpItem.PipeType is PipeType.PLTL or PipeType.WLTL)
                            {
                                textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSHYPNOTIC, MISAPPREHENSIVE), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY) { Rotation = Math.PI / THESAURUSPERMUTATION });
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSATTACHMENT, MISAPPREHENSIVE), IRRESPONSIBLENESS, THESAURUSSTRIPED, CONTROVERSIALLY) { Rotation = Math.PI / THESAURUSPERMUTATION });
                            }
                            else
                            {
                            }
                        }
                        if (siPipeLabels.Count >= THESAURUSPERMUTATION)
                        {
                            if (siPipeLabels.Max() == eMax)
                            {
                                siPipeLabels.Remove(siPipeLabels.Max());
                            }
                        }
                        foreach (var i in siPipeLabels.OrderBy(x => x))
                        {
                            var px = GetBasePoint(i, j);
                            if (gpItem.PipeType is PipeType.PLTL or PipeType.WLTL)
                            {
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSHYPNOTIC), px.OffsetXY(-THESAURUSDOMESTIC, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSDOMESTIC, THESAURUSFORMULATE), px.OffsetXY(-THESAURUSVISITOR, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), px.OffsetXY(THESAURUSFORMULATE, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSFORMULATE, THESAURUSFORMULATE), px.OffsetXY(THESAURUSCONTEMPTUOUS, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSVISITOR, THESAURUSATTACHMENT), label1, THESAURUSSTRIPED, CONTROVERSIALLY));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSFORMULATE, THESAURUSATTACHMENT), label3, THESAURUSSTRIPED, CONTROVERSIALLY));
                                if (label2 != null)
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSVISITOR, THESAURUSHYPNOTIC), label2, THESAURUSSTRIPED, CONTROVERSIALLY));
                                if (label4 != null)
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSFORMULATE, THESAURUSHYPNOTIC), label4, THESAURUSSTRIPED, CONTROVERSIALLY));
                            }
                            else if (gpItem.PipeType is PipeType.WLTLFL)
                            {
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSHYPNOTIC, MISAPPREHENSIVE), px.OffsetXY(THESAURUSFORMULATE, THESAURUSDERELICTION)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSFORMULATE, THESAURUSDERELICTION), px.OffsetXY(THESAURUSLUSTFUL, THESAURUSDERELICTION)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSHYPNOTIC, THESAURUSHYPNOTIC), px.OffsetXY(-THESAURUSFORMULATE, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(-THESAURUSFORMULATE, THESAURUSFORMULATE), px.OffsetXY(-THESAURUSLUSTFUL, THESAURUSFORMULATE)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, POLYOXYMETHYLENE), px.OffsetXY(THESAURUSDOMESTIC, REPRESENTATIONAL)), THESAURUSSTRIPED));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDOMESTIC, REPRESENTATIONAL), px.OffsetXY(QUOTATIONINFEUDATION, REPRESENTATIONAL)), THESAURUSSTRIPED));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSLUSTFUL, THESAURUSATTACHMENT), label1, THESAURUSSTRIPED, CONTROVERSIALLY));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(-THESAURUSLUSTFUL, THESAURUSENDANGER), label2, THESAURUSSTRIPED, CONTROVERSIALLY));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSDOMESTIC, THESAURUSCAPITALISM), label3, THESAURUSSTRIPED, CONTROVERSIALLY));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSDOMESTIC, QUOTATIONCOLERIDGE), label4, THESAURUSSTRIPED, CONTROVERSIALLY));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSFORMULATE, CONSCRIPTIONIST), label5, THESAURUSSTRIPED, CONTROVERSIALLY));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSFORMULATE, PHYSIOLOGICALLY), label6, THESAURUSSTRIPED, CONTROVERSIALLY));
                            }
                            else
                            {
                                lm.TakeStartSnap();
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSSTAMPEDE, THESAURUSHYPNOTIC), px.OffsetXY(THESAURUSDOMESTIC, THESAURUSFORMULATE)), GetNoteLayer(gpItem.PipeType)));
                                lineInfos.Add(new LineInfo(new GLineSegment(px.OffsetXY(THESAURUSDOMESTIC, THESAURUSFORMULATE), px.OffsetXY(QUOTATIONINFEUDATION, THESAURUSFORMULATE)), GetNoteLayer(gpItem.PipeType)));
                                textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSDOMESTIC, THESAURUSATTACHMENT), label1, GetNoteLayer(gpItem.PipeType), CONTROVERSIALLY));
                                if (label2 != null)
                                {
                                    textInfos.Add(new DBTextInfo(px.OffsetXY(THESAURUSDOMESTIC, THESAURUSENDANGER), label2, GetNoteLayer(gpItem.PipeType), CONTROVERSIALLY));
                                }
                                lm.TakeStopSnap();
                                var m = lm.GetSnapshot();
                                prq.Enqueue((int)LayoutState.PipeLabel, () =>
                                {
                                    var line = GeoFac.CreateGeometry(pipeLineInfos.Select(x => x.Line.ToLineString()).ToArray());
                                    var pt = m.GetLineVertices().OrderBy(x => x.Y).First();
                                    var target = line.Intersection(new GLineSegment(pt.OffsetX(-DETECTLENGTH), pt.OffsetX(DETECTLENGTH)).ToLineString());
                                    if (target is Point p)
                                    {
                                        var v = p.ToPoint2d() - pt;
                                        m.MoveElements(v);
                                    }
                                });
                            }
                        }
                        prq.Enqueue((int)LayoutState.FixPipeVLines, () =>
                        {
                            if (vsels.Count + vkills.Count + vdrills.Count == THESAURUSSTAMPEDE) return;
                            if (pipeLineInfos.Count == THESAURUSSTAMPEDE) return;
                            var layer = pipeLineInfos.First().LayerName;
                            var vlineInfos = pipeLineInfos.Where(x => x.Line.IsVertical(THESAURUSHOUSING)).ToList();
                            if (vlineInfos.Count == THESAURUSSTAMPEDE) return;
                            foreach (var info in vlineInfos)
                            {
                                lineInfos.Remove(info);
                            }
                            var vlines = vlineInfos.Select(x => x.Line.ToLineString()).ToList();
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
                            foreach (var line in GeoFac.GetManyLines(vlines))
                            {
                                lineInfos.Add(new LineInfo(line, layer));
                            }
                        });
                    }
                    prq.Enqueue((int)LayoutState.Finished, () =>
                    {
                        foreach (var info in lineInfos)
                        {
                            var line = DrawLineSegmentLazy(info.Line);
                            if (!string.IsNullOrEmpty(info.LayerName))
                            {
                                line.Layer = info.LayerName;
                            }
                            ByLayer(line);
                        }
                        foreach (var info in circleInfos)
                        {
                            var c = DrawCircleLazy(info.Circle.Center, info.Circle.Radius);
                            if (!string.IsNullOrEmpty(info.LayerName)) c.Layer = info.LayerName;
                            ByLayer(c);
                        }
                        foreach (var info in textInfos)
                        {
                            var dbt = DrawTextLazy(info.Text, info.BasePoint.ToPoint2d());
                            dbt.Rotation = info.Rotation;
                            dbt.WidthFactor = THESAURUSDISPASSIONATE;
                            dbt.Height = THESAURUSENDANGER;
                            if (!string.IsNullOrEmpty(info.LayerName)) dbt.Layer = info.LayerName;
                            if (!string.IsNullOrEmpty(info.TextStyle)) DrawingQueue.Enqueue(adb => { SetTextStyle(dbt, info.TextStyle); });
                            ByLayer(dbt);
                        }
                        foreach (var info in brInfos)
                        {
                            DrawBlockReference(info.BlockName, info.BasePoint, layer: info.LayerName, cb: br =>
                            {
                                ByLayer(br);
                                if (info.ScaleEx.HasValue) br.ScaleFactors = info.ScaleEx.Value;
                                {
                                    if (info.DynaDict != null && br.IsDynamicBlock) br.DynamicBlockReferencePropertyCollection.Cast<DynamicBlockReferenceProperty>().Where(x => !x.ReadOnly).Join(info.DynaDict, x => x.PropertyName, y => y.Key, (x, y) => x.Value = y.Value).Count();
                                }
                            }, props: info.PropDict, scale: info.Scale, rotateDegree: info.Rotate.AngleToDegree());
                        }
                        foreach (var info in dimInfos)
                        {
                            var dim = new AlignedDimension
                            {
                                XLine1Point = info.Point1,
                                XLine2Point = info.Point2,
                                DimLinePoint = GeoAlgorithm.MidPoint(info.Point1, info.Point2) + info.Vector,
                                DimensionText = info.Text,
                                Layer = info.Layer
                            };
                            ByLayer(dim);
                            DrawEntityLazy(dim);
                        }
                    });
                }
                FlushDQ(adb);
                void handleEntity(Entity entity, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
                {
                    if (!IsLayerVisible(entity)) return;
                    var dxfName = entity.GetRXClass().DxfName.ToUpper();
                    var entityLayer = entity.Layer;
                    entityLayer = GetEffectiveLayer(entityLayer);
                    static bool isDLineLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && !layer.Contains(THESAURUSDEVIANT);
                    static bool isVentLayer(string layer) => layer != null && layer.Contains(THESAURUSREMNANT) && layer.Contains(THESAURUSINCENSE) && layer.Contains(THESAURUSDEVIANT);
                    if (!dxfName.StartsWith(QUOTATIONCHROMIC))
                    {
                        var bdx = entity.Bounds;
                        if (bdx.HasValue)
                        {
                            var bd = bdx.Value;
                            bd.TransformBy(matrix);
                            if (!storeyst(bd.ToEnvelope())) return;
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
                                reg(fs, bd, gRainPorts);
                                return;
                            }
                        }
                    }
                    {
                        if (dxfName == QUOTATIONSWALLOW && entityLayer is THESAURUSINVOICE)
                        {
                            var r = entity.Bounds.ToGRect().TransformBy(matrix);
                            reg(fs, r, gRainPorts);
                        }
                    }
                    {
                        if (entity is Circle c && isDrainageLayer(entityLayer))
                        {
                            if (THESAURUSSTAMPEDE < c.Radius && c.Radius <= HYPERDISYLLABLE)
                            {
                                var bd = c.Bounds.ToGRect().TransformBy(matrix);
                                reg(fs, bd, gcircles);
                                return;
                            }
                        }
                    }
                    {
                        if (entity is Circle c)
                        {
                            if (THESAURUSSTAMPEDE < c.Radius && c.Radius <= HYPERDISYLLABLE)
                            {
                                if (isDrainageLayer(c.Layer))
                                {
                                    var r = c.Bounds.ToGRect().TransformBy(matrix);
                                    reg(fs, r, gcircles);
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
                            reg(fs, seg, gwlines);
                            return;
                        }
                        else if (entity is Polyline pl)
                        {
                            foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                var seg = ln.ToGLineSegment().TransformBy(matrix);
                                reg(fs, seg, gwlines);
                            }
                            return;
                        }
                        if (dxfName is DISORGANIZATION)
                        {
                            dynamic o = entity;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                            reg(fs, seg, gwlines);
                            return;
                        }
                    }
                    if (isDLineLayer(entityLayer))
                    {
                        if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, gdlines);
                            return;
                        }
                        else if (entity is Polyline pl)
                        {
                            foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                var seg = ln.ToGLineSegment().TransformBy(matrix);
                                reg(fs, seg, gdlines);
                            }
                            return;
                        }
                        if (dxfName is DISORGANIZATION)
                        {
                            dynamic o = entity;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                            reg(fs, seg, gdlines);
                            return;
                        }
                    }
                    if (isVentLayer(entityLayer))
                    {
                        if (entity is Line line && line.Length > THESAURUSSTAMPEDE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, gvlines);
                            return;
                        }
                        else if (entity is Polyline pl)
                        {
                            foreach (var ln in pl.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                var seg = ln.ToGLineSegment().TransformBy(matrix);
                                reg(fs, seg, gvlines);
                            }
                            return;
                        }
                        if (dxfName is DISORGANIZATION)
                        {
                            dynamic o = entity;
                            var seg = new GLineSegment((Point3d)o.StartPoint, (Point3d)o.EndPoint).TransformBy(matrix);
                            reg(fs, seg, gvlines);
                            return;
                        }
                    }
                    if (dxfName is DISORGANIZATION)
                    {
                        if (entityLayer is THESAURUSSINCERE || entityLayer.Contains(SEROEPIDEMIOLOGY))
                        {
                            foreach (var c in entity.ExplodeToDBObjectCollection().OfType<Circle>().Where(IsLayerVisible))
                            {
                                if (c.Radius > THESAURUSSTAMPEDE && isDrainageLayer(c.Layer))
                                {
                                    var bd = c.Bounds.ToGRect().TransformBy(matrix);
                                    reg(fs, bd, gcircles);
                                }
                            }
                        }
                    }
                    {
                        if (isDrainageLayer(entityLayer) && entity is Line line && line.Length > THESAURUSSTAMPEDE)
                        {
                            var seg = line.ToGLineSegment().TransformBy(matrix);
                            reg(fs, seg, glabelLines);
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
                                    reg(fs, seg, glabelLines);
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
                            reg(fs, ct, gcts);
                        }
                        return;
                    }
                    {
                        static bool g(string t) => !t.StartsWith(THESAURUSNOTATION) && !t.ToLower().Contains(PERPENDICULARITY) && !t.ToUpper().Contains(THESAURUSIMPOSTER);
                        if (entity is DBText dbt && isDrainageLayer(entityLayer) && g(dbt.TextString))
                        {
                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                            var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                            reg(fs, ct, gcts);
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
                            reg(fs, ct, gcts);
                        }
                        return;
                    }
                    if (dxfName == THESAURUSINHARMONIOUS)
                    {
                        {
                            dynamic o = entity.AcadObject;
                            string UpText = o.UpText;
                            string DownText = o.DownText;
                            var ents = entity.ExplodeToDBObjectCollection();
                            var segs = ents.OfType<Line>().Select(x => x.ToGLineSegment()).Where(x => x.IsValid).Distinct().ToList();
                            var points = GeoFac.GetAlivePoints(segs, THESAURUSHOUSING);
                            var pts = points.Select(x => x.ToNTSPoint()).ToList();
                            points = points.Except(GeoFac.CreateIntersectsSelector(pts)(GeoFac.CreateGeometryEx(segs.Where(x => x.IsHorizontal(THESAURUSHOUSING)).Select(x => x.Extend(THESAURUSPERMUTATION).Buffer(THESAURUSHOUSING)).ToList())).Select(pts).ToList(points)).ToList();
                            if (entityLayer is QUOTATIONSTANLEY or THESAURUSDEFAULTER)
                            {
                                if (points.Count == THESAURUSSTAMPEDE) return;
                                string label = null;
                                if (!string.IsNullOrWhiteSpace(UpText) && !string.IsNullOrWhiteSpace(DownText))
                                {
                                    label = UpText + INTELLECTUALNESS + DownText;
                                }
                                else
                                {
                                    label = UpText + DownText;
                                }
                                if (string.IsNullOrWhiteSpace(label)) return;
                                gmultileaderwbushshooters.Add(GeoFac.CreateGeometry(points.Select(x => x.ToNTSPoint())).Tag(label));
                                return;
                            }
                            else if (isDrainageLayer(entityLayer))
                            {
                                if (points.Count == THESAURUSSTAMPEDE) return;
                                string label = null;
                                if (!string.IsNullOrWhiteSpace(UpText) && !string.IsNullOrWhiteSpace(DownText))
                                {
                                    label = UpText + INTELLECTUALNESS + DownText;
                                }
                                else
                                {
                                    label = UpText + DownText;
                                }
                                if (string.IsNullOrWhiteSpace(label)) return;
                                gmultileaderdrainageshooters.Add(GeoFac.CreateGeometry(points.Select(x => x.ToNTSPoint())).Tag(label));
                                return;
                            }
                            if (gb)
                            {
                                var colle = entity.ExplodeToDBObjectCollection();
                                {
                                    foreach (var e in colle.OfType<Entity>().Where(e => e.GetRXClass().DxfName.ToUpper() is THESAURUSDURESS or THESAURUSFACILITATE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible))
                                    {
                                        foreach (var dbt in e.ExplodeToDBObjectCollection().OfType<DBText>().Where(x => !string.IsNullOrWhiteSpace(x.TextString)).Where(IsLayerVisible))
                                        {
                                            var bd = dbt.Bounds.ToGRect().TransformBy(matrix);
                                            var ct = new CText() { Text = dbt.TextString, Boundary = bd };
                                            if (IsDrainageLabel(ct.Text)) reg(fs, ct, gcts);
                                        }
                                    }
                                    foreach (var seg in colle.OfType<Line>().Where(x => x.Length > THESAURUSSTAMPEDE).Where(x => isDrainageLayer(x.Layer)).Where(IsLayerVisible).Select(x => x.ToGLineSegment().TransformBy(matrix)))
                                    {
                                        reg(fs, seg, glabelLines);
                                    }
                                }
                            }
                            return;
                        }
                        return;
                    }
                    if (isDrainageLayer(entityLayer))
                    {
                        {
                            if (entity is Line line)
                            {
                                var seg = line.ToGLineSegment().TransformBy(matrix);
                                reg(fs, seg, glabelLines);
                                return;
                            }
                        }
                        {
                            if (entity is Polyline pl)
                                {
                                    foreach (var line in pl.ExplodeToDBObjectCollection().OfType<Line>().Where(x => x.Length > THESAURUSSTAMPEDE))
                                    {
                                        var seg = line.ToGLineSegment().TransformBy(matrix);
                                        reg(fs, seg, glabelLines);
                                    }
                                    return;
                                }
                        }
                    }
                }
                void handleBlockReference(BlockReference br, Matrix3d matrix, List<KeyValuePair<Geometry, Action>> fs)
                {
                    if (!br.ObjectId.IsValid || !br.BlockTableRecord.IsValid) return;
                    if (!br.Visible) return;
                    if (IsLayerVisible(br))
                    {
                        var _name = br.GetEffectiveName() ?? THESAURUSDEPLORE;
                        var name = GetEffectiveBRName(_name);
                        {
                            if (isDrainageLayer(br.Layer))
                                if (name.Contains(INTELLECTUALISTS))
                                {
                                    var center = br.Bounds.ToGRect().Center.ToPoint3d().TransformBy(matrix);
                                    if (name is SUPERINDUCEMENT && br.IsDynamicBlock)
                                    {
                                        var props = br.DynamicBlockReferencePropertyCollection;
                                        foreach (DynamicBlockReferenceProperty prop in props)
                                        {
                                            if (prop.PropertyName == QUINQUAGENARIAN)
                                            {
                                                var propValue = prop.Value.ToString();
                                                if (!string.IsNullOrEmpty(propValue))
                                                {
                                                    gpipevaluesshooters.Add(center.ToNTSPoint().Tag(propValue));
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    var bd = center.ToGRect(THESAURUSENTREPRENEUR);
                                    reg(fs, bd, gcircles);
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
                                    reg(fs, bd, gwrappingPipes);
                                }
                            }
                            return;
                        }
                        if (name.Contains(THESAURUSINDULGENT))
                        {
                            var bd = br.Bounds.ToGRect().TransformBy(matrix);
                            if (bd.IsValid)
                            {
                                if (bd.Width < POLYOXYMETHYLENE && bd.Height < POLYOXYMETHYLENE)
                                {
                                    reg(fs, bd, gfloorDrains);
                                }
                            }
                            return;
                        }
                        if (name.Contains(QUOTATIONBITTER))
                        {
                            var bd = br.Bounds.ToGRect().TransformBy(matrix);
                            if (bd.IsValid)
                            {
                                if (bd.Width < POLYOXYMETHYLENE && bd.Height < POLYOXYMETHYLENE)
                                {
                                    reg(fs, bd, gwells);
                                }
                            }
                            return;
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
            return THESAURUSOBSTINACY;
        }
        public static Point3dCollection frame;
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
            for (int i = INTROPUNITIVENESS; i < THESAURUSACRIMONIOUS; i++)
            {
                if (label.StartsWith(THESAURUSREGENERATE + i + THESAURUSREALLY))
                {
                    return THESAURUSOBSTINACY;
                }
            }
            return label.StartsWith(THESAURUSUNBEATABLE);
        }
        public static bool IsRainLabel(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return IsY1L(label) || IsY2L(label) || IsNL(label) || IsYL(label) || IsFL0(label);
        }
        public static bool IsDrainageLabel(string label)
        {
            return IsRainLabel(label) || IsDraiLabel(label);
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
        public const string THESAURUSINDULGENT = "地漏";
        public const string INCORRESPONDENCE = "DL";
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
        public const string HYPERVENTILATION = "本层通气";
        public const string THESAURUSNARCOTIC = "通气帽系统-AI2";
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
        public const string THESAURUSDISREPUTABLE = "DN25";
        public const int THESAURUSATTACHMENT = 750;
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
        public const string THESAURUSEXECUTIVE = "散排至";
        public const int PHOSPHORYLATION = 125;
        public const int THESAURUSMISUNDERSTANDING = 357;
        public const int THESAURUSBEHOVE = 83;
        public const int THESAURUSMAGNETIC = 220;
        public const int REPRESENTATIONAL = 1400;
        public const int THESAURUSBELLOW = 621;
        public const int DOCTRINARIANISM = 1200;
        public const int THESAURUSINHERIT = 2000;
        public const int THESAURUSDERELICTION = 600;
        public const int ACANTHORHYNCHUS = 479;
        public const int THESAURUSLOITER = 950;
        public const int PORTMANTOLOGISM = 387;
        public const int QUOTATIONCOLERIDGE = 1050;
        public const int THESAURUSEXPERIMENT = 269;
        public const int MISAPPREHENSIVE = 200;
        public const string QUOTATIONSECOND = "接至排水沟";
        public const int QUOTATIONETHIOPS = 187;
        public const int OTHERWORLDLINESS = 499;
        public const int THESAURUSPRETTY = 1600;
        public const int THESAURUSDIFFICULTY = 360;
        public const int THESAURUSDETERMINED = 130;
        public const int CONSCRIPTIONIST = 650;
        public const int VÖLKERWANDERUNG = 30;
        public const string INTERNALIZATION = "≥600";
        public const int THESAURUSGETAWAY = 450;
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
        public const int THESAURUSITEMIZE = 666;
        public const string THESAURUSPRECOCIOUS = "屋面雨水斗";
        public const int THESAURUSEXCESS = 255;
        public const int THESAURUSDELIGHT = 0x91;
        public const int THESAURUSCRADLE = 0xc7;
        public const int HYPOSTASIZATION = 0xae;
        public const int THESAURUSDISCOLOUR = 211;
        public const string QUOTATIONDOPPLER = "DN75";
        public const string THESAURUSTOPICAL = "重力雨水斗";
        public const string THESAURUSBANDAGE = "侧入式雨水斗";
        public const string THESAURUSCONSERVATION = "87雨水斗";
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
        public const string THESAURUSLECHER = "雨水斗";
        public const string THESAURUSPLUMMET = "重力";
        public const string QUOTATIONSPENSERIAN = "87";
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
        public const string MÖNCHENGLADBACH = "通气帽系统-AI";
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
        public const string THESAURUSRESIGNED = "多通道";
        public const string PHOTOAUTOTROPHIC = "洗衣机";
        public const string THESAURUSACQUISITIVE = "单排";
        public const string QUOTATIONCHILLI = "设置乙字弯";
        public const int THESAURUSDRAGOON = 33;
        public const string QUOTATIONROBERT = "单盆洗手台";
        public const string THESAURUSDELIVER = "双盆洗手台";
        public const string CYLINDRICALNESS = "坐便器";
        public const string THESAURUSEMPHASIS = "$TwtSys$00000132";
        public const string THESAURUSIMPETUOUS = "DNXXX";
        public const string INTELLECTUALNESS = "\n";
        public const string QUOTATIONCHROMIC = "TCH";
        public const string QUOTATIONMALTESE = "沟";
        public const string THESAURUSPROLONG = "侧";
        public const int THESAURUSASSURANCE = 505;
        public const int DETERMINATENESS = 239;
        public const string THESAURUSREALLY = "L";
        public const double ALSOMONOSIPHONIC = 3e4;
        public const string THESAURUSSTRAIGHTFORWARD = "废水立管";
        public const string THESAURUSEMPTINESS = "污水立管";
        public const string THESAURUSHYPOCRISY = "污废合流立管";
        public const string THESAURUSRECIPE = "沉箱立管";
        public const string THESAURUSRAFFLE = "通气立管";
        public const string HYDROCHLOROFLUOROCARBON = "2F";
        public const string PARATHYROIDECTOMY = "地漏-AI";
        public const string PARALINGUISTICALLY = "暂不支持污废分流，仅生成废水系统图";
        public const int THESAURUSSCINTILLATE = 1231;
        public const string THESAURUSBASELESS = "FL";
        public const string THESAURUSPOSSESSIVE = "WL";
        public const double THESAURUSLEGISLATION = 8000.0;
        public const int INTROJECTIONISM = 1075;
        public const int THESAURUSSEDATE = 1100;
        public const int PROGNOSTICATORY = 2400;
        public const int THESAURUSHALLUCINATE = 1571;
        public const int THESAURUSFAINTLY = 1171;
        public const int QUOTATIONELECTROMOTIVE = 1250;
        public const int THESAURUSHOMICIDAL = 1121;
        public const int THESAURUSDESULTORY = 1821;
        public const int THESAURUSPROSPEROUS = 1321;
        public const int THESAURUSUNGRATEFUL = 3400;
        public const int INCOMPREHENSIBILIS = 571;
        public const int THESAURUSSUPPOSITION = 3350;
        public const int QUOTATION1ASHANKS = 1271;
        public const string CHRISTADELPHIAN = "接至雨水口";
        public const int NEUROPSYCHIATRIST = 1120;
        public const int SEMICONSCIOUSNESS = 680;
        public const int PSEUDEPIGRAPHOS = 276;
        public const int THESAURUSMANIFESTATION = 468;
        public const int MAXILLOPALATINE = 2180;
        public const int THESAURUSATTENDANT = 910;
        public const int THESAURUSLAWYER = 1550;
        public const int THESAURUSINDECOROUS = 2059;
        public const int THESAURUSMISAPPREHEND = 1140;
        public const int SYNAESTHETICALLY = 1040;
        public const int THESAURUSDESTRUCTION = 790;
        public const int OLIGOMENORRHOEA = 840;
        public const int THESAURUSOBSERVANCE = 3042;
        public const int THESAURUSMAYHEM = 2050;
        public const int QUOTATIONELECTRICIAN = 3450;
        public const int THESAURUSVIGOROUS = 853;
        public const int QUOTATIONFOREGONE = 531;
        public const int RETROSPECTIVENESS = 1122;
        public const int REACTIONARINESS = 547;
        public const int THESAURUSCOMPOUND = 1073;
        public const int THESAURUSVISITOR = 1323;
        public const int THESAURUSCONTEMPTUOUS = 1595;
        public const int QUOTATIONINFEUDATION = 1455;
        public const string SEROEPIDEMIOLOGY = "EQPM";
        public const string INTELLECTUALISTS = "立管";
        public const string QUOTATIONBITTER = "井";
        public const string THESAURUSENCOMPASS = @"^(F\d?L|T\d?L|P\d?L|D\d?L|W\d?L|Y\d?L|N\d?L)(\w*)\-(\w*)([a-zA-Z]*)$";
        public const string THESAURUSRESUSCITATE = @"\-?DN\d+";
        public const char THESAURUSHABITAT = '\n';
        public const string ALSOHEAVENWARDS = "屋面";
        public const string NEUROTRANSMITTER = @"DN(\d{1,5})";
        public const string THESAURUSPAGEANT = "污废合流井";
        public const string THESAURUSRECTIFY = "0.XX";
        public const int THESAURUSFRISKY = 1350;
        public const double THESAURUSUNDERSTANDING = .2446;
        public const string THESAURUSCURDLE = "侧排雨水斗系统";
        public const int CONSUMMATIVENESS = 215;
        public const int UNACCEPTABLENESS = 235;
        public const int THESAURUSDEPUTIZE = 567;
        public const int THESAURUSHEARTLESS = 587;
        public const int QUOTATIONWORSTED = 2052;
        public const int THESAURUSVISIBLE = 2002;
        public const int THESAURUSINSTITUTE = 482;
        public const int THESAURUSINCONCLUSIVE = 1597;
        public const int ALSOBIPINNATISECT = 1547;
        public const int CATECHOLAMINERGIC = 82;
        public const int CORYNOCARPACEAE = 1579;
        public const int PROBLEMATICALNESS = 1459;
        public const int THESAURUSSMOULDER = 2444;
        public const int THESAURUSBEAUTIFY = 1398;
        public const int THESAURUSEFFULGENT = 679;
        public const int THESAURUSINDISPENSABLE = 1292;
        public const int THESAURUSREPRESSIVE = 1020;
        public const int THESAURUSMADNESS = 899;
        public const int THESAURUSEFFICACY = 820;
        public const int PALAEOICHTHYOLOGY = 699;
        public const int THESAURUSSHALLOW = 1179;
        public const int IMMEASURABLENESS = 578;
        public const int ALSOWATERLANDIAN = 365;
        public const int THESAURUSWINDING = 1844;
        public const string THESAURUSPENNILESS = "DOME";
        public const int THESAURUSREFRACTORY = 1275;
        public const int ALSOMULTIFLORAL = 1178;
        public const int THESAURUSACQUIESCENT = 1155;
        public const int THESAURUSUNOCCUPIED = 1740;
        public const int PROMORPHOLOGIST = 149;
        public const int QUOTATIONQUICHE = 870;
        public const int THESAURUSTRAUMATIC = 421;
        public const int STENTOROPHŌNIKOS = 991;
        public const int THESAURUSASTUTE = 1348;
        public const int THESAURUSASSIMILATE = 914;
        public const int SPECTROFLUORIMETER = 1355;
        public const int THESAURUSRUINOUS = 1356;
        public const int DIFFERENTIATEDNESS = 3300;
        public const int THESAURUSIMPRACTICABLE = 1721;
        public const int THESAURUSRECONCILE = 2900;
        public const int METROPOLITANATE = 879;
        public const int IMAGINATIVENESS = 2150;
        public const int TRIGONOCEPHALIC = 620;
        public const int SCIENTIFICALNESS = 1099;
        public const int QUOTATIONBUBONIC = 1220;
        public const int THESAURUSHEADSTRONG = 291;
        public const int THESAURUSCONTEMPT = 1939;
        public const int THESAURUSMATTER = 1933;
        public const int THESAURUSABSORBENT = 1268;
        public const int OLIGOSACCHARIDES = 1950;
        public const int THESAURUSINTEGRITY = 2500;
        public const int THESAURUSDISTASTEFUL = 721;
        public const int THESAURUSPROGRESSIVE = 1750;
        public const int THESAURUSINSINCERE = 1021;
        public const int THESAURUSLUSTFUL = 1755;
        public const double THESAURUSARRIVE = 100.0;
        public const string THESAURUSREGENERATE = "Y";
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
        public static bool IsDraiFL(string label)
        {
            return IsFL(label) && !IsFL0(label);
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
        public static bool IsDraiLabel(string label)
        {
            if (label == null) return INTRAVASCULARLY;
            return (IsFL(label) && !IsFL0(label)) || IsPL(label) || IsTL(label) || IsDL(label) || IsWL(label);
        }
    }
    public class BlockInfo
    {
        public string LayerName;
        public string BlockName;
        public Point3d BasePoint;
        public double Rotate;
        public double Scale;
        public Scale3d? ScaleEx;
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
    public class DBTextInfo
    {
        public string LayerName;
        public string TextStyle;
        public Point3d BasePoint;
        public string Text;
        public double Rotation;
        public DBTextInfo(Point3d point, string text, string layerName, string textStyle)
        {
            text ??= THESAURUSDEPLORE;
            this.LayerName = layerName;
            this.TextStyle = textStyle;
            this.BasePoint = point;
            this.Text = text;
        }
    }
    public class DimInfo
    {
        public string Layer;
        public string Text;
        public Point3d Point1;
        public Point3d Point2;
        public Vector3d Vector;
        public DimInfo(Point2d pt1, Point2d pt2, Vector2d v, string text, string layer) : this(pt1.ToPoint3d(), pt2.ToPoint3d(), v.ToVector3d(), text, layer) { }
        public DimInfo(Point3d pt1, Point3d pt2, Vector3d v, string text, string layer)
        {
            this.Layer = layer;
            this.Point1 = pt1;
            this.Point2 = pt2;
            this.Text = text ?? THESAURUSDEPLORE;
            this.Vector = v;
        }
    }
    public class CircleInfo
    {
        public GCircle Circle;
        public string LayerName;
        public CircleInfo(Point3d center, double radius, string layerName) : this(new GCircle(center.X, center.Y, radius), layerName)
        {
        }
        public CircleInfo(GCircle circle, string layerName)
        {
            this.Circle = circle;
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
        public List<ThMEPWSS.ReleaseNs.DrainageSystemNs.StoreyItem> storeysItems;
        public ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageGeoData geoData;
        public DrainageSystemDiagramViewModel vm;
    }
    public enum LayoutState
    {
        CheckPoint, Basin, FloorDrain, CondensePipe, SanPaiMark, Outlet, PipeLabel, FixPipeVLines, FixAirBlock, Finished,
    }
    public enum GeoCalState
    {
        MarkTranslator, GroupPipe, MarkCompsToPipe, MarkWaterBucketToPipe, Finished,
    }
}