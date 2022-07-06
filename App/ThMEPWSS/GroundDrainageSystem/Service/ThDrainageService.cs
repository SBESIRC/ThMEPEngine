namespace ThMEPWSS.Pipe.Service
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using ThMEPWSS.JsonExtensionsNs;
    using static ThMEPWSS.Assistant.DrawUtils;
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
    using ThMEPWSS.Pipe;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using static THDrainageService;

    public class RainSystemCadData
    {
        public List<Geometry> Storeys;
        public List<Geometry> LabelLines;
        public List<Geometry> WLines;
        public List<Geometry> WLinesAddition;
        public List<Geometry> Labels;
        public List<Geometry> VerticalPipes;
        public List<Geometry> CondensePipes;
        public List<Geometry> FloorDrains;
        public List<Geometry> WaterWells;
        public List<Geometry> WaterPortSymbols;
        public List<Geometry> WaterPort13s;
        public List<Geometry> WrappingPipes;
        public List<Geometry> SideWaterBuckets;
        public List<Geometry> GravityWaterBuckets;
        public List<Geometry> _87WaterBuckets;
        public void Init()
        {
            Storeys ??= new List<Geometry>();
            LabelLines ??= new List<Geometry>();
            WLines ??= new List<Geometry>();
            WLinesAddition ??= new List<Geometry>();
            Labels ??= new List<Geometry>();
            VerticalPipes ??= new List<Geometry>();
            CondensePipes ??= new List<Geometry>();
            FloorDrains ??= new List<Geometry>();
            WaterWells ??= new List<Geometry>();
            WaterPortSymbols ??= new List<Geometry>();
            WaterPort13s ??= new List<Geometry>();
            WrappingPipes ??= new List<Geometry>();
            SideWaterBuckets ??= new List<Geometry>();
            GravityWaterBuckets ??= new List<Geometry>();
            _87WaterBuckets ??= new List<Geometry>();
        }
        public static RainSystemCadData Create(RainSystemGeoData data)
        {
            var bfSize = 10;
            var o = new RainSystemCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.LabelLines.AddRange(data.LabelLines.Select(x => x.Buffer(bfSize)));
            o.WLines.AddRange(data.WLines.Select(x => x.Buffer(bfSize)));
            o.WLinesAddition.AddRange(data.WLinesAddition.Select(x => x.Buffer(bfSize)));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));
            o.VerticalPipes.AddRange(data.VerticalPipes.Select(x => x.ToPolygon()));
            o.CondensePipes.AddRange(data.CondensePipes.Select(x => x.ToPolygon()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(x => x.ToPolygon()));
            o.WaterWells.AddRange(data.WaterWells.Select(x => x.ToPolygon()));
            o.WaterPortSymbols.AddRange(data.WaterPortSymbols.Select(x => x.ToPolygon()));
            o.WaterPort13s.AddRange(data.WaterPort13s.Select(x => x.ToPolygon()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(x => x.ToPolygon()));
            o.SideWaterBuckets.AddRange(data.SideWaterBuckets.Select(x => x.ToPolygon()));
            o.GravityWaterBuckets.AddRange(data.GravityWaterBuckets.Select(x => x.ToPolygon()));
            o._87WaterBuckets.AddRange(data._87WaterBuckets.Select(x => x.ToPolygon()));
            return o;
        }
        public List<Geometry> GetAllEntities()
        {
            var ret = new List<Geometry>(4096);
            ret.AddRange(Storeys);
            ret.AddRange(LabelLines);
            ret.AddRange(WLines);
            ret.AddRange(WLinesAddition);
            ret.AddRange(Labels);
            ret.AddRange(VerticalPipes);
            ret.AddRange(CondensePipes);
            ret.AddRange(FloorDrains);
            ret.AddRange(WaterWells);
            ret.AddRange(WaterPortSymbols);
            ret.AddRange(WaterPort13s);
            ret.AddRange(WrappingPipes);
            ret.AddRange(SideWaterBuckets);
            ret.AddRange(GravityWaterBuckets);
            ret.AddRange(_87WaterBuckets);
            return ret;
        }
        public List<RainSystemCadData> SplitByStorey()
        {
            var lst = new List<RainSystemCadData>(this.Storeys.Count);
            if (this.Storeys.Count == 0) return lst;
            var f = GeoFac.CreateIntersectsSelector(GetAllEntities());
            foreach (var storey in this.Storeys)
            {
                var objs = f(storey);
                var o = new RainSystemCadData();
                o.Init();
                o.LabelLines.AddRange(objs.Where(x => this.LabelLines.Contains(x)));
                o.WLines.AddRange(objs.Where(x => this.WLines.Contains(x)));
                o.WLinesAddition.AddRange(objs.Where(x => this.WLinesAddition.Contains(x)));
                o.Labels.AddRange(objs.Where(x => this.Labels.Contains(x)));
                o.VerticalPipes.AddRange(objs.Where(x => this.VerticalPipes.Contains(x)));
                o.CondensePipes.AddRange(objs.Where(x => this.CondensePipes.Contains(x)));
                o.FloorDrains.AddRange(objs.Where(x => this.FloorDrains.Contains(x)));
                o.WaterWells.AddRange(objs.Where(x => this.WaterWells.Contains(x)));
                o.WaterPortSymbols.AddRange(objs.Where(x => this.WaterPortSymbols.Contains(x)));
                o.WaterPort13s.AddRange(objs.Where(x => this.WaterPort13s.Contains(x)));
                o.WrappingPipes.AddRange(objs.Where(x => this.WrappingPipes.Contains(x)));
                o.SideWaterBuckets.AddRange(objs.Where(x => this.SideWaterBuckets.Contains(x)));
                o.GravityWaterBuckets.AddRange(objs.Where(x => this.GravityWaterBuckets.Contains(x)));
                o._87WaterBuckets.AddRange(objs.Where(x => this._87WaterBuckets.Contains(x)));
                lst.Add(o);
            }
            return lst;
        }
        public RainSystemCadData Clone()
        {
            return (RainSystemCadData)MemberwiseClone();
        }
    }
    public class RainSystemGeoData
    {
        public List<GRect> Storeys;

        public List<GLineSegment> LabelLines;
        public List<GLineSegment> WLines;
        public List<GLineSegment> WLinesAddition;
        public List<CText> Labels;
        public List<GRect> VerticalPipes;
        public List<GRect> CondensePipes;
        public List<GRect> FloorDrains;
        public List<GRect> WaterWells;
        public List<string> WaterWellLabels;
        public List<GRect> WaterPortSymbols;
        public List<GRect> WaterPort13s;
        public List<GRect> WrappingPipes;

        public List<GRect> SideWaterBuckets;
        public List<GRect> GravityWaterBuckets;
        public List<GRect> _87WaterBuckets;

        public void Init()
        {
            LabelLines ??= new List<GLineSegment>();
            WLines ??= new List<GLineSegment>();
            WLinesAddition ??= new List<GLineSegment>();
            Labels ??= new List<CText>();
            VerticalPipes ??= new List<GRect>();
            Storeys ??= new List<GRect>();
            CondensePipes ??= new List<GRect>();
            FloorDrains ??= new List<GRect>();
            WaterWells ??= new List<GRect>();
            WaterWellLabels ??= new List<string>();
            WaterPortSymbols ??= new List<GRect>();
            WaterPort13s ??= new List<GRect>();
            WrappingPipes ??= new List<GRect>();
            SideWaterBuckets ??= new List<GRect>();
            GravityWaterBuckets ??= new List<GRect>();
            _87WaterBuckets ??= new List<GRect>();
        }
        public void FixData()
        {
            LabelLines = LabelLines.Where(x => x.Length > 0).Distinct().ToList();
            WLines = WLines.Where(x => x.Length > 0).Distinct().ToList();
            WLinesAddition = WLinesAddition.Where(x => x.Length > 0).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            CondensePipes = CondensePipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterWells = WaterWells.Where(x => x.IsValid).Distinct().ToList();
            WaterPortSymbols = WaterPortSymbols.Where(x => x.IsValid).Distinct().ToList();
            WaterPort13s = WaterPort13s.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            SideWaterBuckets = SideWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            GravityWaterBuckets = GravityWaterBuckets.Where(x => x.IsValid).Distinct().ToList();
            _87WaterBuckets = _87WaterBuckets.Where(x => x.IsValid).Distinct().ToList();
        }
        public RainSystemGeoData Clone()
        {
            return (RainSystemGeoData)MemberwiseClone();
        }
        public RainSystemGeoData DeepClone()
        {
            return this.ToCadJson().FromCadJson<RainSystemGeoData>();
        }
    }
    public class ThStoreysData
    {
        public GRect Boundary;
        public List<int> Storeys;
        public ThMEPEngineCore.Model.Common.StoreyType StoreyType;
    }
    public abstract class DrainageText
    {
        public string RawString;
        public class TLDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"TL(\d+)\-(\d+)");
        }
        public class FLDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"FL(\d+)\-(\d+)");
        }
        public class PLDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"PL(\d+)\-(\d+)");
        }
        public class ToiletDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"(接自)?(\d+)F卫生间单排");
        }
        public class KitchenDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"(接自)?(\d+)F厨房单排");
        }
        public class FallingBoardAreaFloorDrainText : DrainageText
        {
            //仅31F顶层板下设置乙字弯
            //一层底板设置乙字弯
            //这个不管
        }
        public class UnderboardShortTranslatorSettingsDrainageText : DrainageText
        {
            public static readonly Regex re = new Regex(@"降板区地漏DN(\d+)");

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
        public List<ThDrainageService.WLGrouper.ToiletGrouper> toiletGroupers;

        //1)	必然负担一个洗涤盆下水点。不用读图上任何信息；
        //2)	若厨房内存在任何地漏图块或洗衣机图块（图块名称包含A-Toilet-9），则必然负担一个地漏下水点。
        //最多同时负担一个洗涤盆下水店和一个地漏下水点。
        public HashSet<string> KitchenOnlyFls;

        //1)	图层名称包含“W-DRAI-EPQM”且半径大于40的圆视为洗手台下水点
        //2)	地漏图块视为地漏下水点
        public HashSet<string> BalconyOnlyFLs;

        //综合厨房和阳台的点位
        public HashSet<string> KitchenAndBalconyFLs;

        //FL+TL
        //若FL附近300的范围内存在TL且该TL不属于PL，则将FL与TL设为一组。系统图上和PL+TL的唯一区别是废水管要表达卫生洁具。
        public HashSet<string> MustHaveCleaningPort;

        //6.3.8	水管井的FL
        //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
        //水管井的判断：
        //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
        public HashSet<string> WaterPipeWellFLs;

        public HashSet<string> MustHaveFloorDrains;
        public HashSet<string> SingleOutlet;
        public bool HasToilet;

        public List<string> Comments;


        public HashSet<string> HasKitchenBasin;//目前只在1F判断，其他楼全部加上去，后面不知道会不会主动去识别
        public HashSet<string> HasKitchenFloorDrain;//
        public HashSet<string> HasKitchenWashingMachine;//
        //上面两个同时存在，标记为“厨房洗衣机地漏”，无洗衣机则标记为“厨房地漏”

        public HashSet<string> HasBalconyWashingMachine;
        public HashSet<string> HasMopPool;
        //类似的还有“阳台洗衣机地漏”，“阳台地漏”，厨房只有1个，阳台可能有2个

        public HashSet<string> Shunts;//如果有两个地漏，判断下是不是并联
        public void Init()
        {
            VerticalPipeLabels ??= new HashSet<string>();
            LongTranslatorLabels ??= new HashSet<string>();
            ShortTranslatorLabels ??= new HashSet<string>();
            FloorDrains ??= new Dictionary<string, int>();
            CleaningPorts ??= new HashSet<string>();
            Outlets ??= new Dictionary<string, string>();
            WrappingPipes ??= new HashSet<string>();
            toiletGroupers ??= new List<ThDrainageService.WLGrouper.ToiletGrouper>();
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
            HasMopPool ??= new HashSet<string>();
            Shunts ??= new HashSet<string>();
        }
    }

    public partial class DrainageService
    {
        public AcadDatabase adb;
        public DrainageSystemDiagram DrainageSystemDiagram;
        public List<ThStoreysData> Storeys;
        public DrainageGeoData GeoData;
        public DrainageCadData CadDataMain;
        public List<DrainageCadData> CadDatas;
        public List<DrainageDrawingData> drawingDatas;
        public List<KeyValuePair<string, Geometry>> roomData;
        public static DrainageGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return null;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = GetStoreys(range, adb);
                var geoData = new DrainageGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }

        public static void DrawDrainageSystemDiagram2(Dictionary<string, object> ctx)
        {
            if (commandContext != null) return;
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return;
            //if (!TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                try
                {
                    Dispose();
                    var storeys = GetStoreys(range, adb);
                    var geoData = new DrainageGeoData();
                    geoData.Init();
                    CollectGeoData(range, adb, geoData);
                    ThDrainageService.PreFixGeoData(geoData);
                    ThDrainageService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = DrainageCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new DrainageService()
                    {
                        adb = adb,
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    if (sv.DrainageSystemDiagram == null) { }
                }
                finally
                {
                    Dispose();
                }
            }
        }
        public static void CollectGeoData(Geometry range, AcadDatabase adb, DrainageGeoData geoData, CommandContext ctx)
        {
            var cl = CollectGeoData(adb, geoData);
            cl.CollectStoreys(range, ctx);
        }
        public static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = CollectGeoData(adb, geoData);
            cl.CollectStoreys(range);
        }

        private static ThDrainageSystemServiceGeoCollector CollectGeoData(AcadDatabase adb, DrainageGeoData geoData)
        {
            var cl = new ThDrainageSystemServiceGeoCollector() { adb = adb, geoData = geoData };
            cl.CollectEntities();
            cl.CollectDLines();
            cl.CollectVLines();
            cl.CollectLabelLines();
            cl.CollectCTexts();
            cl.CollectVerticalPipes();
            cl.CollectWrappingPipes();
            cl.CollectWaterPorts();
            cl.CollectFloorDrains();
            cl.CollectCleaningPorts();
            cl.CollectKillers();
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
        public static void DrawGeoData(DrainageGeoData geoData)
        {
            foreach (var s in geoData.Storeys) DrawRectLazy(s).ColorIndex = 1;
            foreach (var o in geoData.LabelLines) DrawLineSegmentLazy(o).ColorIndex = 1;
            foreach (var o in geoData.Labels)
            {
                DrawTextLazy(o.Text, o.Boundary.LeftButtom).ColorIndex = 2;
                DrawRectLazy(o.Boundary).ColorIndex = 2;
            }
            foreach (var o in geoData.VerticalPipes) DrawRectLazy(o).ColorIndex = 3;
            foreach (var o in geoData.FloorDrains) DrawRectLazy(o).ColorIndex = 6;
            foreach (var o in geoData.WaterPorts) DrawRectLazy(o).ColorIndex = 7;
            {
                var cl = Color.FromRgb(4, 229, 230);
                foreach (var o in geoData.DLines) DrawLineSegmentLazy(o).Color = cl;
            }
        }

        const double MAX_SHORTTRANSLATOR_DISTANCE = 300;//150;

        public static void CreateDrawingDatas(DrainageGeoData geoData, DrainageCadData cadDataMain, List<DrainageCadData> cadDatas, out string logString, out List<DrainageDrawingData> drDatas, List<KeyValuePair<string, Geometry>> roomData = null)
        {
            _DrawingTransaction.Current.AbleToDraw = false;
            roomData ??= new List<KeyValuePair<string, Geometry>>();
            Func<List<Geometry>, Func<Geometry, List<Geometry>>> F = GeoFac.CreateIntersectsSelector;
            Func<IEnumerable<Geometry>, Geometry> G = GeoFac.CreateGeometry;
            Func<List<Geometry>, List<List<Geometry>>> GG = GeoFac.GroupGeometries;
            static List<Geometry> GeosGroupToGeos(List<List<Geometry>> geosGrp) => geosGrp.Select(lst => GeoFac.CreateGeometry(lst)).ToList();
            foreach (var s in geoData.Storeys)
            {
                var e = DrawRectLazy(s).ColorIndex = 1;
            }
            var sb = new StringBuilder(8192);
            drDatas = new List<DrainageDrawingData>();
            for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
            {
                sb.AppendLine($"===框{storeyI}===");
                var drData = new DrainageDrawingData();
                drData.Init();
                var item = cadDatas[storeyI];

                {
                    var maxDis = 8000;
                    var angleTolleranceDegree = 1;
                    var waterPortCvt = DrainageCadData.ConvertWaterPortsLargerF();
                    var lines = GeoFac.AutoConn(item.DLines.Where(x => x.Length > 0).Distinct().ToList().Select(cadDataMain.DLines).ToList(geoData.DLines).ToList(),
                        GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.FloorDrains).Concat(item.WaterPorts.Select(cadDataMain.WaterPorts).ToList(geoData.WaterPorts).Select(waterPortCvt)).ToList()),
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
                var dlinesGeos = GeosGroupToGeos(dlinesGroups);
                var vlinesGroups = GG(item.VLines);
                var vlinesGeos = GeosGroupToGeos(vlinesGroups);
                var wrappingPipesf = F(item.WrappingPipes);

                //自动补上缺失的立管
                {
                    var pipesf = F(item.VerticalPipes);
                    foreach (var label in item.Labels)
                    {
                        if (!ThDrainageService.IsMaybeLabelText(geoData.Labels[cadDataMain.Labels.IndexOf(label)].Text)) continue;
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
                                    DrawTextLazy("脑补的", pl.GetCenter());
                                }
                            }
                        }
                    }
                }


                DrawTextLazy($"===框{storeyI}===", geoData.Storeys[storeyI].LeftTop);
                foreach (var o in item.LabelLines)
                {
                    DrawLineSegmentLazy(geoData.LabelLines[cadDataMain.LabelLines.IndexOf(o)]).ColorIndex = 1;
                }
                foreach (var pl in item.Labels)
                {
                    var m = geoData.Labels[cadDataMain.Labels.IndexOf(pl)];
                    var e = DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var _pl = DrawRectLazy(m.Boundary);
                    _pl.ColorIndex = 2;
                }
                foreach (var o in item.PipeKillers)
                {
                    DrawRectLazy(geoData.PipeKillers[cadDataMain.PipeKillers.IndexOf(o)]).Color = Color.FromRgb(255, 255, 55);
                }
                foreach (var o in item.WashingMachines)
                {
                    DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)], 10);
                }
                foreach (var o in item.VerticalPipes)
                {
                    DrawRectLazy(geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)]).ColorIndex = 3;
                }
                foreach (var o in item.FloorDrains)
                {
                    DrawRectLazy(geoData.FloorDrains[cadDataMain.FloorDrains.IndexOf(o)]).ColorIndex = 6;
                }
                foreach (var o in item.WaterPorts)
                {
                    DrawRectLazy(geoData.WaterPorts[cadDataMain.WaterPorts.IndexOf(o)]).ColorIndex = 7;
                    DrawTextLazy(geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(o)], o.GetCenter());
                }
                foreach (var o in item.WashingMachines)
                {
                    var e = DrawRectLazy(geoData.WashingMachines[cadDataMain.WashingMachines.IndexOf(o)]).ColorIndex = 1;
                }
                foreach (var o in item.CleaningPorts)
                {
                    var m = geoData.CleaningPorts[cadDataMain.CleaningPorts.IndexOf(o)];
                    if (false) DrawGeometryLazy(new GCircle(m, 50).ToCirclePolygon(36), ents => ents.ForEach(e => e.ColorIndex = 7));
                    DrawRectLazy(GRect.Create(m, 40));
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var o in item.WrappingPipes)
                    {
                        var e = DrawRectLazy(geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(o)]);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(4, 229, 230);
                    foreach (var o in item.DLines)
                    {
                        var e = DrawLineSegmentLazy(geoData.DLines[cadDataMain.DLines.IndexOf(o)]).Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(211, 213, 111);
                    foreach (var o in item.VLines)
                    {
                        DrawLineSegmentLazy(geoData.VLines[cadDataMain.VLines.IndexOf(o)]).Color = cl;
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
                                    if (ThDrainageService.IsMaybeLabelText(label))
                                    {
                                        lbDict[pp] = label;
                                        ok_ents.Add(pp);
                                        ok_ents.Add(lb);
                                        ok = true;
                                    }
                                    else if (ThDrainageService.IsNotedLabel(label))
                                    {
                                        notedPipesDict[pp] = label;
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
                                    if (labelsTxts.All(txt => ThDrainageService.IsMaybeLabelText(txt)))
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
                                if (!ThDrainageService.IsMaybeLabelText(lb)) continue;
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
                                foreach (var dlinesGeo in dlinesGeos)
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
                                                //通气立管没有乙字弯
                                                if (!IsTL(label))
                                                {
                                                    shortTranslatorLabels.Add(label);
                                                    lbDict[pp2] = label;
                                                    ok = true;
                                                }
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
                                        //通气立管没有乙字弯
                                        if (!IsTL(label))
                                        {
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
                    {
                        var pipes1f = F(lbDict.Where(kv => IsTL(kv.Value)).Select(kv => kv.Key).ToList());
                        var pipes2f = F(item.VerticalPipes.Where(p => !lbDict.ContainsKey(p)).ToList());
                        foreach (var vlinesGeo in vlinesGeos)
                        {
                            var lst = pipes1f(vlinesGeo);
                            if (lst.Count == 1)
                            {
                                var pp1 = lst[0];
                                lst = pipes2f(vlinesGeo);
                                if (lst.Count == 1)
                                {
                                    var pp2 = lst[0];
                                    if (pp1.GetCenter().GetDistanceTo(pp2.GetCenter()) > MAX_SHORTTRANSLATOR_DISTANCE)
                                    {
                                        var label = lbDict[pp1];
                                        longTranslatorLabels.Add(label);
                                        lbDict[pp2] = label;
                                    }
                                }
                            }
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
                        foreach (var label in d.Where(x => x.Value > 1).Select(x => x.Key))
                        {
                            var pps = pipes.Where(p => getLabel(p) == label).ToList();
                            if (pps.Count == 2)
                            {
                                var dis = pps[0].GetCenter().GetDistanceTo(pps[1].GetCenter());
                                if (10 < dis && dis <= MAX_SHORTTRANSLATOR_DISTANCE)
                                {
                                    //通气立管没有乙字弯
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

                //关联地漏
                {
                    var dict = new Dictionary<string, int>();
                    {
                        var pipesf = GeoFac.CreateIntersectsSelector(item.VerticalPipes);
                        var gs = GeoFac.GroupGeometriesEx(dlinesGeos, item.FloorDrains);
                        foreach (var g in gs)
                        {
                            var fds = g.Where(pl => item.FloorDrains.Contains(pl)).ToList();
                            var dlines = g.Where(pl => dlinesGeos.Contains(pl)).ToList();
                            if (!AllNotEmpty(fds, dlines)) continue;
                            var pipes = pipesf(GeoFac.CreateGeometry(fds.Concat(dlines).ToList()));
                            foreach (var lb in pipes.Select(getLabel).Where(lb => lb != null).Distinct())
                            {
                                dict[lb] = fds.Count;
                            }
                        }
                    }
                    {

                        //210712:马力说暂时去掉这个规则
                        //如果地漏没连dline，那么以中心500范围内对应最近的立管

                        //var pipesf = F(item.VerticalPipes);
                        //foreach (var fd in item.FloorDrains.Except(F(item.FloorDrains)(G(item.DLines))))
                        //{
                        //    foreach (var pipe in pipesf(new GCircle(fd.GetCenter(), 500).ToCirclePolygon(6)))
                        //    {
                        //        if (lbDict.TryGetValue(pipe, out string label))
                        //        {
                        //            dict.TryGetValue(label, out int count);
                        //            if (count == 0)
                        //            {
                        //                dict[label] = 1;
                        //                break;
                        //            }
                        //        }
                        //    }
                        //}

                    }
                    //sb.AppendLine("地漏：" + dict.ToJson());
                    drData.FloorDrains = dict;
                }

                //关联清扫口
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
                        void collect(Func<Geometry, List<Geometry>> waterPortsf, Func<Geometry, string> getWaterPortLabel)
                        {
                            var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                            foreach (var dlinesGeo in dlinesGeos)
                            {
                                var waterPorts = waterPortsf(dlinesGeo);
                                if (waterPorts.Count == 1)
                                {
                                    var waterPort = waterPorts[0];
                                    var waterPortLabel = getWaterPortLabel(waterPort);
                                    portd[dlinesGeo] = waterPortLabel;
                                    //DrawTextLazy(waterPortLabel, dlinesGeo.GetCenter());
                                    var pipes = f2(dlinesGeo);
                                    ok_ents.AddRange(pipes);
                                    foreach (var pipe in pipes)
                                    {
                                        if (lbDict.TryGetValue(pipe, out string label))
                                        {
                                            outletd[label] = waterPortLabel;
                                            var wrappingpipes = wrappingPipesf(dlinesGeo);
                                            if (wrappingpipes.Count > 0)
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

                        //先提取直接连接的
                        collect(F(item.WaterPorts), waterPort => geoData.WaterPortLabels[cadDataMain.WaterPorts.IndexOf(waterPort)]);
                        //再处理挨得特别近但就是没连接的
                        {
                            var spacialIndex = item.WaterPorts.Select(cadDataMain.WaterPorts).ToList();
                            var waterPorts = spacialIndex.ToList(geoData.WaterPorts).Select(x => x.Expand(400).ToPolygon()).Cast<Geometry>().ToList();
                            collect(F(waterPorts), waterPort => geoData.WaterPortLabels[spacialIndex[waterPorts.IndexOf(waterPort)]]);
                        }
                    }
                    {
                        //再处理挨得有点远没直接连接的
                        var f2 = F(item.VerticalPipes.Except(ok_ents).ToList());
                        var radius = 10;
                        var f5 = GeoFac.NearestNeighbourPoint3dF(item.WaterPorts);
                        foreach (var dlinesGeo in dlinesGeos)
                        {
                            var segs = ExplodeGLineSegments(dlinesGeo);
                            var pts = GeoFac.GetAlivePoints(segs.Distinct().ToList(), radius: radius);
                            {
                                var _pts = pts.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToGeometryList();
                                var killer = GeoFac.CreateGeometryEx(item.VerticalPipes.Concat(item.WaterPorts).Concat(item.CleaningPorts).Concat(item.FloorDrains).Distinct().ToList());
                                pts = pts.Except(F(_pts)(killer).Select(_pts).ToList(pts)).ToList();
                            }
                            foreach (var pt in pts)
                            {
                                var waterPort = f5(pt.ToPoint3d());
                                if (waterPort != null)
                                {
                                    if (waterPort.GetCenter().GetDistanceTo(pt) <= 1500)
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
                        //给套管做标记
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

                    //{
                    //    foreach (var kv in portd)
                    //    {
                    //        if(lbDict.TryGetValue(kv.Key,out string label))
                    //        {
                    //            if (!outletd.ContainsKey(label))
                    //            {
                    //                outletd[label] = kv.Value;
                    //            }
                    //        }
                    //    }
                    //}
                    {
                        sb.AppendLine("排出：" + outletd.ToJson());
                        drData.Outlets = outletd;

                        outletd.Join(lbDict, kv => kv.Key, kv => kv.Value, (kv1, kv2) =>
                        {
                            var num = kv1.Value;
                            var pipe = kv2.Key;
                            DrawTextLazy(num, pipe.ToGRect().RightButtom);
                            return 666;
                        }).Count();
                    }
                    {
                        sb.AppendLine("套管：" + has_wrappingpipes.ToJson());
                        drData.WrappingPipes.AddRange(has_wrappingpipes);
                    }
                }

                //“仅31F顶层板下设置乙字弯”的处理（😉不处理）
                //标出所有的立管编号（看看识别成功了没）
                foreach (var pp in item.VerticalPipes)
                {
                    lbDict.TryGetValue(pp, out string label);
                    if (label != null)
                    {
                        DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                    }
                }


                //立管分组
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
                    var lst = ThDrainageService.WLGeoGrouper.ToiletGrouper.DoGroup(pls, tls, dls, fls);
                    var toiletGroupers = new List<ThDrainageService.WLGrouper.ToiletGrouper>();
                    foreach (var itm in lst)
                    {
                        var _pls = itm.PLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _tls = itm.TLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _dls = itm.DLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var _fls = itm.FLs.Join(lbDict, x => lbDict[x], x => x.Value, (x, y) => y.Value).Distinct().ToList();
                        var m = new ThDrainageService.WLGrouper.ToiletGrouper()
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
                //6.3.8	水管井的FL
                {
                    //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                    //水管井的判断：
                    //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
                    var f = F(lbDict.Where(kv => IsFL(kv.Value)).Select(x => x.Key).ToList());
                    var hs = new HashSet<string>();
                    foreach (var kv in roomData)
                    {
                        if (kv.Key == "水" || kv.Key.Contains("水井") || kv.Key.Contains("水管井"))
                        {
                            hs.AddRange(f(kv.Value).Select(x => lbDict[x]));
                        }
                    }
                    drData.WaterPipeWellFLs.AddRange(hs);
                    sb.AppendLine("WaterPipeWellFLs：" + drData.WaterPipeWellFLs.OrderBy(x => x).ToJson());
                }


                {
                    var fls = new List<Geometry>();
                    var pls = new List<Geometry>();
                    foreach (var kv in lbDict)
                    {
                        if (IsFL(kv.Value))
                        {
                            fls.Add(kv.Key);
                        }
                        else if (IsPL(kv.Value))
                        {
                            pls.Add(kv.Key);
                        }
                    }
                    var kitchens = roomData.Where(x => IsKitchen(x.Key)).Select(x => x.Value).ToList();
                    var toilets = roomData.Where(x => IsToilet(x.Key)).Select(x => x.Value).ToList();
                    var nonames = roomData.Where(x => x.Key is "").Select(x => x.Value).ToList();
                    var balconys = roomData.Where(x => IsBalcony(x.Key)).Select(x => x.Value).ToList();

                    var pts = GeoFac.GetAlivePoints(item.DLines.Select(cadDataMain.DLines).ToList(geoData.DLines), radius: 5);

                    {
                        //单排
                        var plsf = F(pls);
                        foreach (var toilet in toilets)
                        {
                            foreach (var pl in plsf(toilet.EnvelopeInternal.ToGRect().ToPolygon()))
                            {
                                drData.SingleOutlet.Add(lbDict[pl]);
                            }
                        }
                        sb.AppendLine("单排:" + drData.SingleOutlet.OrderBy(x => x).ToJson());
                    }
                    {
                        if (toilets.Count > 0)
                        {
                            drData.HasToilet = true;
                        }
                    }

                    {
                        //1)	必然负担一个洗涤盆下水点。不用读图上任何信息；
                        //2)	若厨房内存在任何地漏图块或洗衣机图块（图块名称包含A-Toilet-9），则必然负担一个地漏下水点。
                        //最多同时负担一个洗涤盆下水店和一个地漏下水点。
                        var hasBasinList = new List<bool>();
                        var hasKitchenFloorDrainList = new List<bool>();
                        var hasKitchenWashingMachineList = new List<bool>();
                        var _fls1 = DrainageService.GetKitchenOnlyFLs(fls, kitchens, nonames,
                            balconys, pts, item.DLines,
                            fls.Select(x => lbDict[x]).ToList(),
                            item.Basins,
                            item.FloorDrains,
                            item.WashingMachines,
                            hasBasinList,
                            hasKitchenFloorDrainList,
                            hasKitchenWashingMachineList
                            );
                        for (int i = 0; i < _fls1.Count; i++)
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
                        var _fls4 = DrainageService.GetFLsWhereSupportingFloorDrainUnderWaterPoint(fls, kitchens, item.FloorDrains, item.WashingMachines);
                        foreach (var fl in _fls4)
                        {
                            var label = lbDict[fl];
                            drData.MustHaveFloorDrains.Add(label);
                            drData.Comments.Add("GetFLsWhereSupportingFloorDrainUnderWaterPoint - " + fl);
                        }
                        List<bool> hasWashingMachineList = new List<bool>();
                        List<int> floorDrainsCountList = new List<int>();
                        List<bool> hasMopPoolList = new List<bool>();
                        List<bool> isShuntList = new List<bool>();
                        var _fls2 = DrainageService.GetBalconyOnlyFLs(fls, kitchens, nonames, balconys, pts, item.DLines, fls.Select(x => lbDict[x]).ToList(), item.WashingMachines, item.MopPools, item.FloorDrains, hasWashingMachineList, floorDrainsCountList, hasMopPoolList, isShuntList);
                        //1)	图层名称包含“W-DRAI-EPQM”且半径大于40的圆视为洗手台下水点
                        //2)	地漏图块视为地漏下水点
                        for (int i = 0; i < _fls2.Count; i++)
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
                                if (drData.FloorDrains[label] == 0) drData.FloorDrains[label] = 1;
                            }
                            if (hasMopPool) drData.HasMopPool.Add(label);
                            if (isShuntList[i]) drData.Shunts.Add(label);
                        }
                        //综合厨房和阳台的点位
                        var _fls3 = DrainageService.GetKitchenAndBalconyBothFLs(fls, kitchens, nonames, balconys, pts, item.DLines);
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




                    sb.AppendLine("KitchenOnlyFls：" + drData.KitchenOnlyFls.OrderBy(x => x).ToJson());
                    sb.AppendLine("BalconyOnlyFLs：" + drData.BalconyOnlyFLs.OrderBy(x => x).ToJson());
                    sb.AppendLine("KitchenAndBalconyFLs：" + drData.KitchenAndBalconyFLs.OrderBy(x => x).ToJson());
                    sb.AppendLine("MustHaveFloorDrains：" + drData.MustHaveFloorDrains.OrderBy(x => x).ToJson());

                    sb.AppendLine("HasKitchenFloorDrain：" + drData.HasKitchenFloorDrain.OrderBy(x => x).ToJson());
                    sb.AppendLine("HasKitchenWashingMachine：" + drData.HasKitchenWashingMachine.OrderBy(x => x).ToJson());
                    sb.AppendLine("HasKitchenBasin：" + drData.HasKitchenBasin.OrderBy(x => x).ToJson());
                    sb.AppendLine("HasBalconyWashingMachine：" + drData.HasBalconyWashingMachine.OrderBy(x => x).ToJson());
                    sb.AppendLine("HasMopPool：" + drData.HasMopPool.OrderBy(x => x).ToJson());

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
            _DrawingTransaction.Current.AbleToDraw = true;
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
            var ranges = adb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == "AI-空间框线").Select(x => ThCADCore.NTS.ThCADCoreNTSDbExtension.ToNTSPolygon(x)).Cast<Geometry>().ToList();
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
            //6.3.2	只负责厨房的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	FL的500范围内有厨房空间，或
            //2）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在厨房空间内
            //且以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意所有结束点不在没有名称的空间或不在阳台空间内。

            //210713 马力说改成150范围内

            var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
            var list = new List<Geometry>(FLs.Count);
            var basinsf = GeoFac.CreateIntersectsSelector(basins);


            {
                var ok_fls = new HashSet<Geometry>();
                //210714 写到这里
                var floorDrainsf = GeoFac.CreateIntersectsSelector(floorDrains);
                var washingMachinesf = GeoFac.CreateIntersectsSelector(washingMachines);
                foreach (var kitchen in kitchens)
                {
                    var flsf = GeoFac.CreateIntersectsSelector(FLs.Except(ok_fls).ToList());
                    var fls = flsf(kitchen);
                    if (fls.Count > 0)
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
                        fls = flsf(kitchen.Buffer(500));
                        if (fls.Count > 0)
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



            for (int i = 0; i < FLs.Count; i++)
            {
                var fl = FLs[i];
                var lb = labels[i];
                {
                    //210713 暂时这样处理
                    foreach (var kitchen in kitchens)
                    {
                        //if (kitchen.Intersects(fl))
                        //210714 暂时这样处理
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
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter(), 500, 36));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    if (endpoints.Count == 0) return true;
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
        //找出负担地漏下水点的FL
        public static HashSet<Geometry> GetFLsWhereSupportingFloorDrainUnderWaterPoint(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> floorDrains, List<Geometry> washMachines)
        {
            var f = GeoFac.CreateIntersectsSelector(ToList(floorDrains, washMachines));
            var hs = new HashSet<Geometry>();
            {
                var flsf = GeoFac.CreateIntersectsSelector(FLs);
                foreach (var kitchen in kitchens)
                {
                    var lst = flsf(kitchen);
                    if (lst.Count > 0)
                    {
                        if (f(kitchen).Count > 0)
                        {
                            hs.AddRange(lst);
                        }
                    }
                }
            }
            return hs;
        }
        static List<Point2d> GetEndPoints(Geometry start, List<Point2d> points, List<Geometry> dlines)
        {
            //以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的结束点
            points = points.Distinct().ToList();
            var pts = points.Select(x => new GCircle(x, 5).ToCirclePolygon(6)).ToList();
            var dlinesGeo = GeoFac.CreateGeometry(GeoFac.CreateIntersectsSelector(dlines)(start));
            return GeoFac.CreateIntersectsSelector(pts)(dlinesGeo).Where(x => !x.Intersects(start)).Select(pts).ToList(points);
        }
        public static List<Geometry> GetBalconyOnlyFLs(List<Geometry> FLs,
            List<Geometry> kitchens,
            List<Geometry> nonames,
            List<Geometry> balconies,
            List<Point2d> pts,
            List<Geometry> dlines,
            List<string> labels,
            List<Geometry> washingMachines,
            List<Geometry> mopPools,
            List<Geometry> floorDrains,
            List<bool> hasWashingMachineList,
            List<int> floorDrainsCountList,
            List<bool> hasMopPoolList,
            List<bool> isShuntList)
        {
            //6.3.3	只负责阳台的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。且

            //2）	FL的500范围内没有厨房空间，且
            //210713  范围改成150


            //3）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的所有结束点不在在厨房空间内


            //210713 改成 1 and (2 or 3)
            var nearestBalconyf = GeoFac.NearestNeighbourGeometryF(balconies);
            var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
            var list = new List<Geometry>(FLs.Count);
            var washingMachinesf = GeoFac.CreateIntersectsSelector(washingMachines);
            var floorDrainsf = GeoFac.CreateIntersectsSelector(floorDrains);
            var mopPoolsf = GeoFac.CreateIntersectsSelector(mopPools);
            {
                var dlinesf = GeoFac.CreateIntersectsSelector(dlines);
                var ok_fls = new HashSet<Geometry>();
                //210715 写到这里
                foreach (var balcony in balconies)
                {
                    var flsf = GeoFac.CreateIntersectsSelector(FLs.Except(ok_fls).ToList());
                    var fls = flsf(balcony);
                    bool isShunt(Geometry fl)
                    {
                        var _dlines = dlinesf(fl);
                        if (_dlines.Count == 0) return false;
                        var fds = floorDrainsf(GeoFac.CreateGeometry(_dlines));
                        if (fds.Count == 2)
                        {
                            foreach (var geo in GeoFac.GroupGeometries(GeoFac.ToNodedLineSegments(GeoFac.GetLines(GeoFac.CreateGeometry(dlinesf(GeoFac.CreateGeometry(floorDrains.YieldAfter(fl).Distinct()))).Difference(fl)).ToList()).Select(x => x.ToLineString()).ToList()).Select(geos => GeoFac.CreateGeometry(geos)))
                            {
                                if (geo.Intersects(fds[0]) && geo.Intersects(fds[1]))
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return false;
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
                    if (fls.Count > 0)
                    {
                        foreach (var fl in fls)
                        {
                            emit(fl);
                        }
                    }
                    else
                    {
                        fls = flsf(balcony.Buffer(500));
                        if (fls.Count > 0)
                        {
                            var fl = GeoFac.NearestNeighbourGeometryF(fls)(balcony);
                            emit(fl);
                        }
                    }
                }
                return list;
            }

            for (int i = 0; i < FLs.Count; i++)
            {
                var fl = FLs[i];
                var lb = labels[i];
                //{
                //    //210713 暂时这样处理
                //    foreach (var balcony in balconies)
                //    {
                //        if (balcony.Intersects(fl))
                //        {
                //            list.Add(fl);
                //        }
                //    }
                //    continue;
                //}



                List<Point2d> endpoints = null;
                Geometry endpointsGeo = null;
                List<Point2d> _GetEndPoints()
                {
                    return GetEndPoints(fl, pts, dlines);
                }
                bool test1()
                {
                    endpoints ??= _GetEndPoints();
                    if (endpoints.Count == 0) return false;
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                    return endpointsGeo.Intersects(GeoFac.CreateGeometryEx(ToList(nonames, balconies)));
                }
                bool test2()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 150, 36).Intersects(kitchensGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints.Select(x => x.ToNTSPoint()));
                    return !kitchensGeo.Intersects(endpointsGeo);
                }
                //if (test1() && test2() && test3())
                if (test1() && (test2() || test3()))
                {
                    list.Add(fl);
                    var bal = nearestBalconyf(fl);
                    if (bal == null)
                    {
                        hasWashingMachineList.Add(false);
                        floorDrainsCountList.Add(0);
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
        public static List<Geometry> GetKitchenAndBalconyBothFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies, List<Point2d> pts, List<Geometry> dlines)
        {
            //6.3.4	厨房阳台兼用FL
            //-	判断方法
            //1)	FL的500范围内有厨房空间，且
            //2)	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometryEx(kitchens);
            var list = new List<Geometry>(FLs.Count);
            //210713 暂时这样处理
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
                    return GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
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
    }
    public partial class DrainageSystemDiagram
    {
        public static readonly Point2d[] LONG_TRANSLATOR_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-1379, -121), new Point2d(-1500, -241) };
        public static readonly Point2d[] SHORT_TRANSLATOR_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121) };
        public static double LONG_TRANSLATOR_HEIGHT1 = 780;
        public static double CHECKPOINT_OFFSET_Y = 580;
        public static readonly Point2d[] LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 500), new Point2d(-79, 621), new Point2d(1029, 621), new Point2d(1150, 741), new Point2d(1150, 1021) };

        public static List<DrainageGroupedPipeItem> GetDrainageGroupedPipeItems(List<DrainageDrawingData> drDatas, List<StoreysItem> storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys)
        {
            //Draw(drDatas, pt, storeysItems);
            //开始写分组逻辑

            //Console.WriteLine(storeysItems.ToCadJson());

            {
                //prefix drData

                foreach (var drData in drDatas)
                {

                }

            }

            var allFls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsFL(x)));
            var allTls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsTL(x)));
            var allPls = new HashSet<string>(drDatas.SelectMany(x => x.VerticalPipeLabels).Where(x => IsPL(x)));

            var storeys = new List<string>();
            foreach (var item in storeysItems)
            {
                item.Init();
                //storeys.AddRange(item.Labels.Where(isWantedStorey));
                storeys.AddRange(item.Labels);
            }
            storeys = storeys.Distinct().OrderBy(GetStoreyScore).ToList();
            var flstoreylist = new List<KeyValuePair<string, string>>();
            {
                for (int i = 0; i < storeysItems.Count; i++)
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
            //Console.WriteLine(flstoreylist.ToCadJson());
            var pls = new HashSet<string>();
            var tls = new HashSet<string>();
            var pltlList = new List<PlTlStoreyItem>();
            var fltlList = new List<FlTlStoreyItem>();
            for (int i = 0; i < storeysItems.Count; i++)
            {
                var item = storeysItems[i];
                foreach (var gp in drDatas[i].toiletGroupers)
                {
                    if (gp.WLType == ThDrainageService.WLType.PL_TL && gp.PLs.Count == 1 && gp.TLs.Count == 1)
                    {
                        var pl = gp.PLs[0];
                        var tl = gp.TLs[0];
                        foreach (var s in item.Labels.Where(x => storeys.Contains(x)))
                        {
                            var x = new PlTlStoreyItem() { pl = pl, tl = tl, storey = s };
                            pls.Add(pl);
                            tls.Add(tl);
                            pltlList.Add(x);
                        }
                    }
                    else if (gp.WLType == ThDrainageService.WLType.FL_TL && gp.FLs.Count == 1 && gp.TLs.Count == 1)
                    {
                        var fl = gp.FLs[0];
                        var tl = gp.TLs[0];
                        foreach (var s in item.Labels.Where(x => storeys.Contains(x)))
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
                for (int i = 0; i < storeysItems.Count; i++)
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
            var plLongTransList = new List<KeyValuePair<string, string>>();
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    var item = storeysItems[i];
                    foreach (var s in item.Labels)
                    {
                        foreach (var pl in drDatas[i].LongTranslatorLabels.Where(x => IsPL(x)))
                        {
                            plLongTransList.Add(new KeyValuePair<string, string>(pl, s));
                        }
                    }
                }
            }
            var flLongTransList = new List<KeyValuePair<string, string>>();
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    var item = storeysItems[i];
                    foreach (var s in item.Labels)
                    {
                        foreach (var fl in drDatas[i].LongTranslatorLabels.Where(x => IsFL(x)))
                        {
                            flLongTransList.Add(new KeyValuePair<string, string>(fl, s));
                        }
                    }
                }
            }
            var plShortTransList = new List<KeyValuePair<string, string>>();
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    var item = storeysItems[i];
                    foreach (var s in item.Labels)
                    {
                        foreach (var pl in drDatas[i].ShortTranslatorLabels.Where(x => IsPL(x)))
                        {
                            plShortTransList.Add(new KeyValuePair<string, string>(pl, s));
                        }
                    }
                }
            }
            var flShortTransList = new List<KeyValuePair<string, string>>();
            {
                for (int i = 0; i < storeysItems.Count; i++)
                {
                    var item = storeysItems[i];
                    foreach (var s in item.Labels)
                    {
                        foreach (var fl in drDatas[i].ShortTranslatorLabels.Where(x => IsFL(x)))
                        {
                            flShortTransList.Add(new KeyValuePair<string, string>(fl, s));
                        }
                    }
                }
            }
            var minS = storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Min();
            var maxS = storeys.Where(IsNumStorey).Select(x => GetStoreyScore(x)).Max();
            var countS = maxS - minS + 1;
            {
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
                bool hasLong(string label, string storey, PipeType pipeType)
                {
                    if (pipeType == PipeType.FL)
                    {
                        return flLongTransList.Any(x => x.Key == label && x.Value == storey);
                    }
                    if (pipeType == PipeType.PL)
                    {
                        return plLongTransList.Any(x => x.Key == label && x.Value == storey);
                    }
                    throw new System.Exception();
                }
                bool hasShort(string label, string storey, PipeType pipeType)
                {
                    if (pipeType == PipeType.FL)
                    {
                        return flShortTransList.Any(x => x.Key == label && x.Value == storey);
                    }
                    if (pipeType == PipeType.PL)
                    {
                        return plShortTransList.Any(x => x.Key == label && x.Value == storey);
                    }
                    throw new System.Exception();
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
                    return "-";//210714
                }
                bool hasWaterPort(string label)
                {
                    return getWaterPortLabel(label) != null;
                }
                int getMinTl(string tl)
                {
                    var scores = new List<int>();
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    if (scores.Count == 0) return 0;
                    var ret = scores.Min() - 1;
                    if (ret <= 0) return 1;
                    return ret;
                }
                int getMaxTl(string tl)
                {
                    var scores = new List<int>();
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    return scores.Count == 0 ? 0 : scores.Max();
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
                    {
                        for (int i = 0; i < storeysItems.Count; i++)
                        {
                            foreach (var s in storeysItems[i].Labels)
                            {
                                if (s == storey)
                                {
                                    var drData = drDatas[i];
                                    //必然负担一个地漏下水点。
                                    if (drData.MustHaveFloorDrains.Contains(label))
                                    {
                                        if (ret == 0) ret = 1;
                                    }
                                    //最多同时负担一个洗涤盆下水店和一个地漏下水点。
                                    if (drData.KitchenOnlyFls.Contains(label))
                                    {
                                        if (ret > 1) ret = 1;
                                    }
                                }
                            }
                        }
                    }
                    return ret;
                }
                bool hasSCurve(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                // 210703 看来不是
                                ////绘图说明上暗示的
                                //if (drData.BalconyOnlyFLs.Contains(label))
                                //{
                                //    return true;
                                //}

                                if (drData.HasMopPool.Contains(label))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                bool IsSeries(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                if (drData.Shunts.Contains(label))
                                {
                                    return false;
                                }

                            }
                        }
                    }
                    return true;
                }
                bool hasDoubleSCurve(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                //1)必然负担一个洗涤盆下水点。不用读图上任何信息；
                                if (drData.KitchenOnlyFls.Contains(label) || drData.KitchenAndBalconyFLs.Contains(label))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                bool hasBasinInKitchenAt1F(string label)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == "1F")
                            {
                                var drData = drDatas[i];
                                return drData.HasKitchenBasin.Contains(label);
                            }
                        }
                    }
                    return false;
                }
                //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                bool IsWaterPipeWellFL(string label)
                {
                    if (IsFL(label))
                    {
                        foreach (var drData in drDatas)
                        {
                            if (drData.WaterPipeWellFLs.Contains(label)) return true;
                        }
                    }
                    return false;
                }
                bool hasCleaningPort(string label, string storey)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == storey)
                            {
                                var drData = drDatas[i];
                                //若FL附近300的范围内存在TL且该TL不属于PL，则将FL与TL设为一组。系统图上和PL+TL的唯一区别是废水管要表达卫生洁具。
                                if (drData.MustHaveCleaningPort.Contains(label))
                                {
                                    return true;
                                }

                            }
                        }
                    }
                    return false;
                }
                bool hasWrappingPipe(string label)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == "1F")
                            {
                                var drData = drDatas[i];
                                return drData.WrappingPipes.Contains(label);
                            }
                        }
                    }
                    return false;
                }
                bool IsSingleOutlet(string label)
                {
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == "1F")
                            {
                                var drData = drDatas[i];
                                return drData.SingleOutlet.Contains(label);
                            }
                        }
                    }
                    return false;
                }
                int getFloorDrainsCountAt1F(string label)
                {
                    if (!IsFL(label)) return 0;
                    for (int i = 0; i < storeysItems.Count; i++)
                    {
                        foreach (var s in storeysItems[i].Labels)
                        {
                            if (s == "1F")
                            {
                                var drData = drDatas[i];
                                drData.FloorDrains.TryGetValue(label, out int r);
                                return r;
                            }
                        }
                    }
                    return 0;
                }
                bool IsKitchenOnlyFl(string label, string storey)
                {
                    if (!IsFL(label)) return false;
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    return false;
                }
                bool IsBalconyOnlyFl(string label, string storey)
                {
                    if (!IsFL(label)) return false;
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    return false;
                }
                bool HasKitchenFloorDrain(string label, string storey)
                {
                    if (!IsFL(label) || !IsKitchenOnlyFl(label, storey)) return false;
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    return false;
                }
                bool HasKitchenWashingMachine(string label, string storey)
                {
                    if (!IsFL(label) || !IsKitchenOnlyFl(label, storey)) return false;
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    return false;
                }
                bool HasBalconyWashingMachineFloorDrain(string label, string storey)
                {
                    if (!IsFL(label) || !IsBalconyOnlyFl(label, storey)) return false;
                    for (int i = 0; i < storeysItems.Count; i++)
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
                    return false;
                }
                bool HasBalconyNonWashingMachineFloorDrain(string label, string storey)
                {
                    if (!IsFL(label) || !IsBalconyOnlyFl(label, storey)) return false;
                    var fdCount = getFDCount(label, storey);
                    var hasBalconyWashingMachineFloorDrain = HasBalconyWashingMachineFloorDrain(label, storey);
                    //有个1个地漏且不属于洗衣机地漏，那就是阳台地漏
                    if (fdCount == 1 && !hasBalconyWashingMachineFloorDrain)
                    {
                        return true;
                    }
                    return fdCount > 1;
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
                        var _hasLong = hasLong(fl, storey, PipeType.FL);
                        item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                        {
                            Exist = testExist(fl, storey),
                            HasLong = _hasLong,
                            HasShort = hasShort(fl, storey, PipeType.FL),
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
                        //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                        foreach (var hanging in item.Hangings)
                        {
                            hanging.FloorDrainsCount = 1;
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
                        var _hasLong = hasLong(pl, storey, PipeType.PL);
                        item.Items.Add(new DrainageGroupingPipeItem.ValueItem()
                        {
                            Exist = testExist(pl, storey),
                            HasLong = _hasLong,
                            HasShort = hasShort(pl, storey, PipeType.PL),
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
                            item.HasTL = true;
                            //var kv = tlMinMaxStoreyScoreDict[tl];
                            //item.MinTl = kv.Key;
                            //item.MaxTl = kv.Value;
                            item.MinTl = getMinTl(tl);
                            item.MaxTl = getMaxTl(tl);
                            item.TlLabel = tl;

                        }
                    }
                    plGroupingItems.Add(item);
                    pipeInfoDict[pl] = item;
                }


                //修复业务bug

                //01_蓝光钰泷府二期--PL和DL不可能连出地漏。就算用户在平面图上画了地漏并连接到了立管，也不表达在系统图上。系统图不表达卫生间的详图的元素。
                //02_湖北交投颐和华府--PL的表达错误。PL是不会在系统图表达地漏的。
                {
                    foreach (var item in plGroupingItems)
                    {
                        foreach (var hanging in item.Hangings.Yield())
                        {
                            hanging.FloorDrainsCount = 0;
                        }
                    }
                }
                //01_蓝光钰泷府二期--编号横杠后数字为0的FL的处理。编号横杠后数字为0的FL，是为管井服务的。本身只会接一个地漏。不可能接其他排水设施。因此可以不读图直接根据逻辑画。
                {
                    foreach (var item in flGroupingItems)
                    {
                        if (IsFL0(item.Label))
                        {
                            foreach (var hanging in item.Hangings.Yield())
                            {
                                hanging.FloorDrainsCount = 1;//存疑，跟上文“若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。”有冲突，暂时以这里为准。
                                hanging.HasSCurve = false;
                                hanging.HasDoubleSCurve = false;
                                hanging.HasCleaningPort = false;

                                hanging.IsFL0 = true;//FL0的地漏特殊处理
                            }
                        }
                    }
                }
                //全都有清扫口和检查口
                //修正：FL：只应该有立管检查口 PL和DL：应该有检查口、清扫口和降板线
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        foreach (var hanging in item.Hangings.Yield())
                        {
                            //hanging.HasCleaningPort = true;
                            //hanging.HasCheckPoint = true;
                            hanging.HasCleaningPort = IsPL(item.Label) || IsDL(item.Label);
                            hanging.HasDownBoardLine = IsPL(item.Label) || IsDL(item.Label);
                            hanging.HasCheckPoint = true;
                        }
                    }
                }
                //全都有套管
                {
                    foreach (var item in pipeInfoDict.Values)
                    {
                        item.HasWrappingPipe = true;
                    }
                }
                //小于7层 就不需要TL了
                {
                    if (allNumStoreys.Max() < 7)
                    {
                        foreach (var item in pipeInfoDict.Values)
                        {
                            item.HasTL = false;
                        }
                    }
                }


                //开始分组

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
                    };
                    pipeGroupItems.Add(item);
                }
                pipeGroupItems = pipeGroupItems.OrderBy(x => GetLabelScore(x.Labels.FirstOrDefault())).ToList();
                Console.WriteLine(pipeGroupItems.ToCadJson());
                return pipeGroupItems;
            }
        }

        public class DrainageGroupedPipeItem
        {
            public List<string> Labels;
            public List<string> WaterPortLabels;
            public bool HasWrappingPipe;
            public bool HasWaterPort => WaterPortLabels != null && WaterPortLabels.Count > 0;
            public List<DrainageGroupingPipeItem.ValueItem> Items;
            public List<string> TlLabels;
            public int MinTl;
            public int MaxTl;
            public bool HasTl => TlLabels != null && TlLabels.Count > 0;
            public PipeType PipeType;
            public List<DrainageGroupingPipeItem.Hanging> Hangings;
            public bool IsSingleOutlet;
            public bool HasBasinInKitchenAt1F;
            public int FloorDrainsCountAt1F;
        }
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
            public bool Equals(DrainageGroupingPipeItem other)
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
            using (var tr = new _DrawingTransaction(adb, true))
            {
                List<StoreysItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: true)) return;
                Console.WriteLine(drDatas.ToCadJson());
                Console.WriteLine(storeysItems.ToCadJson());
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                Dispose();
                DrawDrainageSystemDiagram(drDatas, storeysItems, basePoint, pipeGroupItems, allNumStoreys, allRfStoreys);
                FlushDQ(adb);
            }
        }
        public static void DrawStoreyLine(string label, Point2d basePt, double lineLen)
        {
            DrawStoreyLine(label, basePt.ToPoint3d(), lineLen);
        }
        public static void DrawStoreyLine(string label, Point3d basePt, double lineLen)
        {
            {
                var line = DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
                DrawBlockReference(blkName: "标高", basePt: basePt.OffsetX(550), layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", "" } });
            }
            if (label == "RF")
            {
                var line = DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0));
                var dbt = DrawTextLazy("建筑完成面", ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
        }
        public static IEnumerable<Point2d> GetBasePoints(Point2d basePoint, int maxCol, int num, double width, double height)
        {
            int i = 0, j = 0;
            for (int k = 0; k < num; k++)
            {
                yield return new Point2d(basePoint.X + i * width, basePoint.Y - j * height);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = 0;
                }
            }
        }
        public static IEnumerable<Point3d> GetBasePoints(Point3d basePoint, int maxCol, int num, double width, double height)
        {
            int i = 0, j = 0;
            for (int k = 0; k < num; k++)
            {
                yield return new Point3d(basePoint.X + i * width, basePoint.Y - j * height, 0);
                i++;
                if (i >= maxCol)
                {
                    j++;
                    i = 0;
                }
            }
        }
        public static void DrawWashBasin(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference("洗涤盆排水", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
            else
            {
                DrawBlockReference("洗涤盆排水", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(-2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
        }
        public static void DrawDoubleWashBasins(Point3d basePt, bool leftOrRight)
        {
            basePt = basePt.OffsetY(300);
            if (leftOrRight)
            {
                DrawBlockReference("双格洗涤盆排水", basePt,
                  br =>
                  {
                      br.Layer = "W-DRAI-EQPM";
                      br.ScaleFactors = new Scale3d(2, 2, 2);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                      }
                  });
            }
            else
            {
                DrawBlockReference("双格洗涤盆排水", basePt,
                  br =>
                  {
                      br.Layer = "W-DRAI-EQPM";
                      br.ScaleFactors = new Scale3d(-2, 2, 2);
                      if (br.IsDynamicBlock)
                      {
                          br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                      }
                  });
            }
        }
        public void Draw(Point3d basePoint)
        {
            BeginToDrawDrainageSystemDiagram(basePoint);
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

            public bool IsFL0;
        }
        public enum GDirection
        {
            E, W, S, N, ES, EN, WS, WN,
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
        public class ThwPipeLine
        {
            public List<string> Labels;
            public bool? IsLeftOrMiddleOrRight;
            public double AiringValue;
            public List<ThwPipeRun> PipeRuns;
            public ThwOutput Output;
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
                    var s = 0;
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
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-RAIN-NOTE";
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static void DrawShortTranslatorLabel(Point2d basePt, bool isLeftOrRight)
        {
            var vecs = new List<Vector2d> { new Vector2d(-800, 1000), new Vector2d(-745, 0) };
            if (!isLeftOrRight) vecs = vecs.GetYAxisMirror();
            var segs = vecs.ToGLineSegments(basePt);
            var wordPt = isLeftOrRight ? segs[1].EndPoint : segs[1].StartPoint;
            var text = "乙字弯";
            var height = 350;
            var lines = DrawLineSegmentsLazy(segs);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DrawTextLazy(text, height, wordPt);
            SetLabelStylesForRainNote(t);
        }
        public static void DrawWashingMachineRaisingSymbol(Point2d bsPt, bool isLeftOrRight)
        {
            if (isLeftOrRight)
            {
                var v = new Vector2d(383875.8169, -250561.9571);
                DrawBlockReference("P型存水弯", (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                    }
                });
            }
            else
            {
                var v = new Vector2d(-383875.8169, -250561.9571);
                DrawBlockReference("P型存水弯", (bsPt - v).ToPoint3d(), br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(-2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上P弯");
                    }
                });
            }
        }
        public static void DrawDrainageSystemDiagram(List<DrainageDrawingData> drDatas, List<StoreysItem> storeysItems, Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<int> allNumStoreys, List<string> allRfStoreys)
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
            DrawDrainageSystemDiagram(basePoint, pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, DrainageSystemDiagramViewModel.Singleton);
        }
        public static void DrawAiringSymbol(Point2d pt, bool canPeopleBeOnRoof)
        {
            //var offsetY = canPeopleBeOnRoof ? 2000.0 : 500.0;
            var offsetY = canPeopleBeOnRoof ? 1850.0 : 350.0;//马力说的
            DrawAiringSymbol(pt, offsetY);
            Dr.DrawDN_1(pt, "W-DRAI-NOTE");
        }
        public static void DrawAiringSymbol(Point2d pt, double offsetY)
        {
            DrawBlockReference(blkName: "通气帽系统", basePt: pt.OffsetY(offsetY).ToPoint3d(), layer: "W-DRAI-DOME-PIPE", cb: br =>
            {
                br.ObjectId.SetDynBlockValue("距离1", offsetY);
                br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
            });
        }
        public static CommandContext commandContext { get => ThDrainageService.commandContext; set => ThDrainageService.commandContext = value; }

        public static void DrawDrainageSystemDiagram(DrainageSystemDiagramViewModel viewModel)
        {
            FocusMainWindow();
            if (commandContext == null) return;
            if (commandContext.StoreyContext == null) return;
            if (commandContext.StoreyContext.thStoreysDatas == null) return;
            if (!TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;

            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb, true))
            {
                Dispose();
                LayerThreeAxes(new List<string>() { "W-NOTE", "W-DRAI-DOME-PIPE", "W-DRAI-NOTE", "W-DRAI-EQPM", "W-BUSH", "W-RAIN-NOTE", "W-DRAI-VENT-PIPE" });
                var storeys = commandContext.StoreyContext.thStoreysDatas;
                List<StoreysItem> storeysItems;
                List<DrainageDrawingData> drDatas;
                var range = commandContext.range;
                if (range != null)
                {
                    if (!CollectDrainageData(range, adb, out storeysItems, out drDatas, noWL: true)) return;
                }
                else
                {
                    if (!CollectDrainageData(GeoFac.CreateGeometryEx(storeys.Select(x => x.Boundary.ToPolygon()).Cast<Geometry>().ToList()), adb, out storeysItems, out drDatas, commandContext, noWL: true)) return;
                }
                var pipeGroupItems = GetDrainageGroupedPipeItems(drDatas, storeysItems, out List<int> allNumStoreys, out List<string> allRfStoreys);
                var allNumStoreyLabels = allNumStoreys.Select(i => i + "F").ToList();
                var allStoreys = allNumStoreyLabels.Concat(allRfStoreys).ToList();
                var start = allStoreys.Count - 1;
                var end = 0;
                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0 + 500 + 3500;
                var HEIGHT = viewModel?.Params?.StoreySpan ?? 1800.0;
                var COUNT = pipeGroupItems.Count;
                var dy = HEIGHT - 1800.0;
                var __dy = 300;
                Dispose();
                DrawDrainageSystemDiagram(basePt.ToPoint2d(), pipeGroupItems, allNumStoreyLabels, allStoreys, start, end, OFFSET_X, SPAN_X, HEIGHT, COUNT, dy, __dy, viewModel);
                FlushDQ(adb);
            }
        }

        public static void StartToDrawDrainageSystemDiagram(Point3d basePoint)
        {
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            //var HEIGHT = 5000.0;
            var COUNT = 20;

            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                DrawStoreyLine(storey, bsPt1, lineLen);
            }
            var outputStartPointOffsets = new Vector2d[COUNT];
            var groups = Enumerable.Range(1, COUNT).Select(i => GenThwPipeLineGroup(storeys)).ToList();

            {
                var start = storeys.Count - 1;
                var end = 0;
                for (int j = 0; j < COUNT; j++)
                {
                    var v = default(Vector2d);
                    for (int i = start; i >= end; i--)
                    {
                        var storey = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + v.ToVector3d();

                            var grp = groups[j];
                            //if (grp.LinesCount == 2 && grp.PL != null && grp.TL != null)
                            if (grp.PL != null)
                            {
                                var r = grp.PL.PipeRuns.FirstOrDefault(r => r.Storey == storey);
                                if (r != null)
                                {
                                    if (r.HasLongTranslator && r.HasShortTranslator)
                                    {
                                        if (r.IsLongTranslatorToLeftOrRight)
                                        {
                                            var height1 = LONG_TRANSLATOR_HEIGHT1;
                                            var points1 = LONG_TRANSLATOR_POINTS;
                                            var points2 = SHORT_TRANSLATOR_POINTS;
                                            var lastPt1 = points1.Last();
                                            NewMethod2(HEIGHT, ref v, basePt, points1, points2, height1);
                                            if (r.HasCheckPoint)
                                            {
                                                DrawPipeCheckPoint(basePt.OffsetXY(lastPt1.X, HEIGHT - height1 + lastPt1.Y - CHECKPOINT_OFFSET_Y), true);
                                            }
                                            if (r.HasHorizontalShortLine)
                                            {
                                                DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                            }
                                        }
                                        else
                                        {
                                            var points1 = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
                                            var points2 = SHORT_TRANSLATOR_POINTS.GetYAxisMirror();
                                            NewMethod3(HEIGHT, ref v, basePt, points1, points2);
                                            var lastPt1 = points1.Last();
                                            var height1 = LONG_TRANSLATOR_HEIGHT1;
                                            if (r.HasCheckPoint)
                                            {
                                                DrawPipeCheckPoint(basePt.OffsetXY(lastPt1.X, HEIGHT - height1 + lastPt1.Y - CHECKPOINT_OFFSET_Y), false);
                                            }
                                            if (r.HasHorizontalShortLine)
                                            {
                                                DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                            }
                                        }
                                    }
                                    else if (r.HasLongTranslator)
                                    {
                                        if (r.IsLongTranslatorToLeftOrRight)
                                        {
                                            var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                            {
                                                var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300);
                                                var segs = LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS.ToGLineSegments(startPoint.ToPoint3d());
                                                DrawDomePipes(segs);
                                                if (r.HasCleaningPort)
                                                {
                                                    DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), true, 2);
                                                }
                                            }
                                            if (r.HasHorizontalShortLine)
                                            {
                                                DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                            }
                                        }
                                        else
                                        {
                                            var lastPt = NewMethod5(HEIGHT, ref v, basePt);
                                            {
                                                var pt = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300);
                                                var segs = LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS.GetYAxisMirror().ToGLineSegments(pt.ToPoint3d());
                                                DrawDomePipes(segs);
                                                if (r.HasCheckPoint)
                                                {
                                                    DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), false, 2);
                                                }
                                            }
                                            if (r.HasHorizontalShortLine)
                                            {
                                                DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                            }
                                        }
                                    }
                                    else if (r.HasShortTranslator)
                                    {
                                        if (r.IsShortTranslatorToLeftOrRight)
                                        {
                                            var points = SHORT_TRANSLATOR_POINTS;
                                            NewMethod1(HEIGHT, ref v, basePt, points);
                                            if (r.HasCheckPoint)
                                            {
                                                DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), true);
                                            }
                                            if (r.HasHorizontalShortLine)
                                            {
                                                DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                            }
                                            if (r.ShowShortTranslatorLabel)
                                            {
                                                var startPt = basePt.ToPoint2d() + v;
                                                var vecs = new List<Vector2d> { new Vector2d(76, 76), new Vector2d(-424, 424), new Vector2d(-1900, 0) };
                                                var segs = vecs.ToGLineSegments(startPt);
                                                segs.RemoveAt(0);
                                                DrawDraiNoteLines(segs);
                                                var t = DrawTextLazy("DN100乙字弯", 350, segs.Last().EndPoint);
                                                SetLabelStylesForDraiNote(t);
                                            }
                                        }
                                        else
                                        {
                                            var points = SHORT_TRANSLATOR_POINTS.GetYAxisMirror();
                                            NewMethod1(HEIGHT, ref v, basePt, points);
                                            if (r.HasCheckPoint)
                                            {
                                                DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), false);
                                            }
                                            if (r.HasHorizontalShortLine)
                                            {
                                                DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DrawDomePipes(new GLineSegment(basePt, basePt.OffsetY(HEIGHT)));
                                        if (r.HasCheckPoint)
                                        {
                                            DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), true);
                                        }
                                        if (r.HasHorizontalShortLine)
                                        {
                                            DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                        }
                                    }

                                    if (false)
                                    {
                                        var vecs = new List<Vector2d> { new Vector2d(0, -700), new Vector2d(-121, -121), new Vector2d(-1259, 0), new Vector2d(-121, -121), new Vector2d(0, -859) };
                                        var segs = vecs.ToGLineSegments(basePt.OffsetY(HEIGHT));
                                        DrawDomePipes(segs);
                                        v += new Vector2d(vecs.Sum(v => v.X), 0);
                                    }
                                }
                            }
                        }
                    }
                    outputStartPointOffsets[j] = v;
                }
            }


        }

        public static void DrawDrainageSystemDiagram(Point2d basePoint, List<DrainageGroupedPipeItem> pipeGroupItems, List<string> allNumStoreyLabels, List<string> allStoreys, int start, int end, double OFFSET_X, double SPAN_X, double HEIGHT, int COUNT, double dy, int __dy, DrainageSystemDiagramViewModel viewModel)
        {
            static void DrawSegs(List<GLineSegment> segs) { for (int k = 0; k < segs.Count; k++) DrawTextLazy(k.ToString(), segs[k].StartPoint); }
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
                    DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
                }
            }

            var dome_lines = new List<GLineSegment>(4096);
            var vent_lines = new List<GLineSegment>(4096);
            var dome_layer = "W-DRAI-DOME-PIPE";
            var vent_layer = "W-DRAI-VENT-PIPE";
            //var vent_layer = "W-RAIN-EQPM";
            void drawDomePipe(GLineSegment seg)
            {
                if (seg.Length > 0) dome_lines.Add(seg);
            }
            void drawDomePipes(IEnumerable<GLineSegment> segs)
            {
                dome_lines.AddRange(segs.Where(x => x.Length > 0));
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
                                    //var segs = arr[i].RightSegsMiddle.ToList();
                                    //segs.RemoveAt(0);
                                    //var r = new GRect(segs[4].StartPoint, segs[4].EndPoint);
                                    //segs[4] = new GLineSegment(r.LeftTop, r.RightButtom);
                                    //segs.Add(new GLineSegment(r.RightButtom, segs[0].StartPoint));
                                    //arr[i].RightSegsFirst = segs;
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
                thwPipeLine.Labels = gpItem.Labels.Concat(gpItem.TlLabels.Yield()).ToList();
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

                void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                {
                    {
                        ////draw comments
                        //var info = arr.Where(x => x != null).FirstOrDefault();
                        //if (info != null)
                        //{
                        //    var dy = -3000;
                        //    if (thwPipeLine.Labels != null)
                        //    {
                        //        foreach (var comment in thwPipeLine.Labels)
                        //        {
                        //            if (!string.IsNullOrEmpty(comment))
                        //            {
                        //                DrawTextLazy(comment, 350, info.EndPoint.OffsetY(-HEIGHT * ((IList)arr).IndexOf(info)).OffsetY(dy));
                        //            }
                        //            dy -= 350;
                        //        }
                        //    }
                        //}
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
                    int counterPipeButtomHeightSymbol = 0;
                    bool hasDrawedSCurveLabel = false;
                    bool hasDrawedDSCurveLabel = false;
                    //bool hasDrawedFloorDrain = false;
                    bool hasDrawedCleaningPort = false;
                    void _DrawLabel(string text, Point2d basePt, bool leftOrRight, double height)
                    {
                        var vecs = new List<Vector2d> { new Vector2d(0, height), new Vector2d(leftOrRight ? -3600 : 3600, 0) };
                        var segs = vecs.ToGLineSegments(basePt);
                        var lines = DrawLineSegmentsLazy(segs);
                        Dr.SetLabelStylesForDraiNote(lines.ToArray());
                        var t = DrawTextLazy(text, 350, segs.Last().EndPoint.OffsetY(50));
                        Dr.SetLabelStylesForDraiNote(t);
                    }
                    void _DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
                    {
                        ++counterPipeButtomHeightSymbol;
                        if (counterPipeButtomHeightSymbol == 2)
                        {
                            var p = basePt.ToPoint2d();
                            var h = HEIGHT * .7;
                            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
                            {
                                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + 150;
                            }
                            p = p.OffsetY(h);
                            DrawPipeButtomHeightSymbol(HEIGHT, p);
                        }
                        DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                    }
                    void _DrawSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                    {
                        if (!hasDrawedSCurveLabel)
                        {
                            hasDrawedSCurveLabel = true;
                            _DrawLabel("接阳台洗手盆排水DN50，余同", p1 + new Vector2d(-450, 1190), true, 1800);
                        }
                        DrawSCurve(vec7, p1, leftOrRight);
                    }
                    void _DrawDSCurve(Vector2d vec7, Point2d p1, bool leftOrRight)
                    {
                        if (!hasDrawedDSCurveLabel && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                        {
                            hasDrawedDSCurveLabel = true;
                            _DrawLabel("接厨房洗涤盆排水DN50，余同", p1 + new Vector2d(-950, 1330), true, 1800);
                        }
                        DrawDSCurve(vec7, p1, leftOrRight);
                    }
                    //地漏要区分阳台、厨房、洗衣机、非洗衣机

                    //void _DrawFloorDrain(Point3d basePt, bool leftOrRight)
                    //{
                    //    var p1 = basePt.ToPoint2d();
                    //    if (!hasDrawedFloorDrain && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                    //    {
                    //        //hasDrawedFloorDrain = true;
                    //        _DrawLabel("接厨房洗衣机地漏DN75，余同" , p1 + new Vector2d(-180, 390), true, 900);
                    //        //DrawTextLazy(gpItem.Items.Select(x => x.HasBalconyWashingMachineFloorDrain).ToJson(), p1 + new Vector2d(-180, 390));
                    //        //DrawTextLazy(gpItem.Items.Select(x => x.HasBalconyNonWashingMachineFloorDrain).ToJson(), p1 + new Vector2d(-180, 390+300));
                    //    }
                    //    DrawFloorDrain(basePt, leftOrRight);
                    //}
                    void _DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
                    {
                        var p1 = basePt.ToPoint2d();
                        if (!hasDrawedCleaningPort && !thwPipeLine.Labels.Any(x => IsFL0(x)))
                        {
                            hasDrawedCleaningPort = true;
                            _DrawLabel("接卫生间排水管DN100，余同", p1 + new Vector2d(-490, 170), true, 2830);
                        }
                        DrawCleaningPort(basePt, leftOrRight, scale);
                    }
                    //var kitchenWashingMachineFloorDrains = new List<Point2d>();
                    //var kitchenNonWashingMachineFloorDrains = new List<Point2d>();
                    //var balconyWashingMachineFloorDrains = new List<Point2d>();
                    //var balconyNonWashingMachineFloorDrains = new List<Point2d>();
                    var fdBasePoints = new Dictionary<int, List<Point2d>>();
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
                            if (storey == "1F")
                            {
                                var basePt = info.EndPoint;
                                if (output != null)
                                {
                                    DrawOutlets1(basePt, 3600, output);
                                }
                            }
                        }

                        //是否进行洗衣机抬高（厨房有洗衣机才触发）
                        bool shouldRaiseWashingMachine()
                        {
                            return viewModel?.Params?.ShouldRaiseWashingMachine ?? false;
                        }
                        bool _shouldDrawRaiseWashingMachineSymbol()
                        {
                            return gpItem.Hangings[i].HasDoubleSCurve && gpItem.Hangings[i].FloorDrainsCount == 1 && shouldRaiseWashingMachine() && gpItem.Items[i].HasKitchenWashingMachine;
                        }
                        bool shouldDrawRaiseWashingMachineSymbol(Hanging hanging)
                        {
                            return hanging.HasDoubleSCurve && hanging.FloorDrainsCount == 1 && shouldRaiseWashingMachine() && gpItem.Items[i].HasKitchenWashingMachine;
                        }
                        void handleHanging(Hanging hanging, bool isLeftOrRight)
                        {
                            void _DrawFloorDrain(Point3d basePt, bool leftOrRight)
                            {
                                var p1 = basePt.ToPoint2d();
                                {
                                    //洗衣机抬高
                                    if (_shouldDrawRaiseWashingMachineSymbol())
                                    {
                                        var fixVec = new Vector2d(-500, 0);//马力说再调节下
                                        var p = p1 + new Vector2d(0, 900) + new Vector2d(-180, 330) + fixVec;
                                        fdBsPts.Add(p);
                                        var vecs = new List<Vector2d> { new Vector2d(-895, 0), fixVec, new Vector2d(-90, 90), new Vector2d(0, 700), new Vector2d(-285, 0) };
                                        var segs = vecs.ToGLineSegments(basePt.ToPoint2d() + new Vector2d(1270, 0));
                                        drawDomePipes(segs);
                                        DrainageSystemDiagram.DrawWashingMachineRaisingSymbol(segs.Last().EndPoint, true);
                                        return;
                                    }
                                }
                                {
                                    var p = p1 + new Vector2d(-180, 390);
                                    fdBsPts.Add(p);
                                    DrawFloorDrain(basePt, leftOrRight);
                                    return;
                                }
                            }
                            ++counterPipeButtomHeightSymbol;
                            //FL的话，标高画在第一个hanging上
                            if (counterPipeButtomHeightSymbol == 1 && thwPipeLine.Labels.Any(x => IsFL(x)))
                            {
                                if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                                {
                                    DrawPipeButtomHeightSymbol(HEIGHT, info.StartPoint.OffsetY(-390 - 156));
                                }
                                else
                                {
                                    DrawPipeButtomHeightSymbol(HEIGHT, info.StartPoint.OffsetY(-390));
                                }
                            }

                            if (run.HasLongTranslator)
                            {
                                var beShort = hanging.FloorDrainsCount == 1 && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                var vecs = new List<Vector2d> { new Vector2d(-200, 200), new Vector2d(0, 479), new Vector2d(-121, 121), new Vector2d(beShort ? 0 : -789, 0), new Vector2d(-1270, 0), new Vector2d(-180, 0), new Vector2d(-1090, 0) };
                                if (isLeftOrRight == false)
                                {
                                    vecs = vecs.GetYAxisMirror();
                                }
                                var pt = info.Segs[4].StartPoint.OffsetY(-669).OffsetY(590 - 90);
                                if (isLeftOrRight == false && run.IsLongTranslatorToLeftOrRight == true)
                                {
                                    var p1 = pt;
                                    var p2 = pt.OffsetX(1700);
                                    drawDomePipe(new GLineSegment(p1, p2));
                                    pt = p2;
                                }
                                if (isLeftOrRight == true && run.IsLongTranslatorToLeftOrRight == false)
                                {
                                    var p1 = pt;
                                    var p2 = pt.OffsetX(-1700);
                                    drawDomePipe(new GLineSegment(p1, p2));
                                    pt = p2;
                                }
                                var segs = vecs.ToGLineSegments(pt);
                                {
                                    var _segs = segs.ToList();
                                    if (hanging.FloorDrainsCount == 2)
                                    {
                                        if (hanging.IsSeries) _segs.RemoveAt(5);
                                    }
                                    else if (hanging.FloorDrainsCount == 1)
                                    {
                                        _segs = segs.Take(5).ToList();
                                    }
                                    else if (hanging.FloorDrainsCount == 0)
                                    {
                                        _segs = segs.Take(4).ToList();
                                    }
                                    if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(2); }
                                    drawDomePipes(_segs);
                                }
                                if (hanging.FloorDrainsCount >= 1)
                                {
                                    _DrawFloorDrain(segs.Last(3).EndPoint.ToPoint3d(), isLeftOrRight);
                                }
                                if (hanging.FloorDrainsCount >= 2)
                                {
                                    _DrawFloorDrain(segs.Last(1).EndPoint.ToPoint3d(), isLeftOrRight);
                                    if (hanging.IsSeries)
                                    {
                                        DrawDomePipes(segs.Last(2));
                                    }
                                }

                                if (hanging.HasSCurve)
                                {
                                    var p1 = segs.Last(3).StartPoint;
                                    _DrawSCurve(vec7, p1, isLeftOrRight);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    var p1 = segs.Last(3).StartPoint;
                                    _DrawDSCurve(vec7, p1, isLeftOrRight);
                                }
                            }
                            else
                            {
                                if (hanging.IsFL0)
                                {
                                    //ShowXLabel(info.EndPoint);
                                    DrawFloorDrain((info.StartPoint + new Vector2d(-1391, -390)).ToPoint3d(), true, "普通地漏无存水弯");
                                    var vecs = new List<Vector2d> { new Vector2d(0, -667), new Vector2d(-121, 121), new Vector2d(-1450, 0) };
                                    var segs = vecs.ToGLineSegments(info.StartPoint).Skip(1).ToList();
                                    drawDomePipes(segs);
                                }
                                else
                                {
                                    var beShort = hanging.FloorDrainsCount == 1 && !hanging.HasSCurve && !hanging.HasDoubleSCurve;
                                    var vecs = new List<Vector2d> { new Vector2d(-121, 121), new Vector2d(beShort ? 0 : -789, 0), new Vector2d(-1270, 0), new Vector2d(-180, 0), new Vector2d(-1090, -15) };
                                    if (isLeftOrRight == false)
                                    {
                                        vecs = vecs.GetYAxisMirror();
                                    }
                                    var segs = vecs.ToGLineSegments(info.StartPoint.OffsetY(-510));
                                    {
                                        var _segs = segs.ToList();
                                        if (hanging.FloorDrainsCount == 2)
                                        {
                                            if (hanging.IsSeries) _segs.RemoveAt(3);
                                        }
                                        if (hanging.FloorDrainsCount == 1)
                                        {
                                            _segs.RemoveAt(4);
                                            _segs.RemoveAt(3);
                                        }
                                        if (hanging.FloorDrainsCount == 0)
                                        {
                                            _segs = _segs.Take(2).ToList();
                                        }
                                        if (shouldDrawRaiseWashingMachineSymbol(hanging)) { _segs.RemoveAt(2); }
                                        drawDomePipes(_segs);
                                    }
                                    if (hanging.FloorDrainsCount >= 1)
                                    {
                                        _DrawFloorDrain(segs.Last(3).EndPoint.ToPoint3d(), isLeftOrRight);
                                    }
                                    if (hanging.FloorDrainsCount >= 2)
                                    {
                                        _DrawFloorDrain(segs.Last(1).EndPoint.ToPoint3d(), isLeftOrRight);
                                    }
                                    if (hanging.HasSCurve)
                                    {
                                        var p1 = segs.Last(3).StartPoint;
                                        _DrawSCurve(vec7, p1, isLeftOrRight);
                                    }
                                    if (hanging.HasDoubleSCurve)
                                    {
                                        var p1 = segs.Last(3).StartPoint;
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
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 2));
                                var p3 = info.EndPoint.OffsetX(300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(2, 3));
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                            }
                            if (bi.FirstRightRun)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 6));
                                var p3 = info.EndPoint.OffsetX(-300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(1, 3));
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
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 6));
                                var p3 = info.EndPoint.OffsetX(-300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(1, 3));
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                            }
                            if (bi.BlueToRightFirst)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(1, 6));
                                var p3 = info.EndPoint.OffsetX(300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(1, 3));
                                info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p1, p2), new GLineSegment(p2, p4) };
                            }
                            if (bi.BlueToLeftLast)
                            {
                                if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        var _dy = 300;
                                        var vs1 = new List<Vector2d> { new Vector2d(0, -780 - _dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy + _dy + 400), new Vector2d(-300, -225) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                    }
                                    else
                                    {
                                        var _dy = 300;
                                        var vs1 = new List<Vector2d> { new Vector2d(0, -780 + _dy), new Vector2d(121, -121), new Vector2d(1258, 0), new Vector2d(121, -120), new Vector2d(0, -779 - dy - _dy + 400), new Vector2d(-300, -225) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                    }
                                }
                                else if (!run.HasLongTranslator)
                                {
                                    var vs = new List<Vector2d> { new Vector2d(0, -1125), new Vector2d(-300, -225) };
                                    info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                }
                            }
                            if (bi.BlueToRightLast)
                            {
                                if (!run.HasLongTranslator && !run.HasShortTranslator)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(9, 24));
                                    var p3 = info.EndPoint.OffsetX(300);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(6, 24));
                                    var p5 = p1.OffsetY(HEIGHT);
                                    info.DisplaySegs = new List<GLineSegment>() { new GLineSegment(p4, p2), new GLineSegment(p2, p5) };
                                }
                            }
                            if (bi.BlueToLeftMiddle)
                            {
                                if (!run.HasLongTranslator && !run.HasShortTranslator)
                                {
                                    var p1 = info.EndPoint;
                                    var p2 = p1.OffsetY(HEIGHT.ToRatioInt(9, 24));
                                    var p3 = info.EndPoint.OffsetX(-300);
                                    var p4 = p3.OffsetY(HEIGHT.ToRatioInt(6, 24));
                                    var segs = info.Segs.ToList();
                                    segs.Add(new GLineSegment(p2, p4));
                                    info.DisplaySegs = segs;
                                }
                                else if (run.HasLongTranslator)
                                {
                                    if (run.IsLongTranslatorToLeftOrRight)
                                    {
                                        var _dy = 300;
                                        var vs1 = new List<Vector2d> { new Vector2d(0, -780 - _dy), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy + _dy) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                    }
                                    else
                                    {
                                        var _dy = 300;
                                        var vs1 = new List<Vector2d> { new Vector2d(0, -780 + _dy), new Vector2d(121, -121), new Vector2d(1258, 0), new Vector2d(121, -120), new Vector2d(0, -779 - dy - _dy) };
                                        var segs = info.DisplaySegs = vs1.ToGLineSegments(info.StartPoint);
                                        var vs2 = new List<Vector2d> { new Vector2d(0, -300), new Vector2d(-300, -225) };
                                        info.DisplaySegs.AddRange(vs2.ToGLineSegments(info.StartPoint).Skip(1).ToList());
                                    }
                                }
                            }
                            if (bi.BlueToRightMiddle)
                            {
                                var p1 = info.EndPoint;
                                var p2 = p1.OffsetY(HEIGHT.ToRatioInt(9, 24));
                                var p3 = info.EndPoint.OffsetX(300);
                                var p4 = p3.OffsetY(HEIGHT.ToRatioInt(6, 24));
                                var segs = info.Segs.ToList();
                                segs.Add(new GLineSegment(p2, p4));
                                info.DisplaySegs = segs;
                            }
                            {
                                var vecs = new List<Vector2d> { new Vector2d(0, -900), new Vector2d(-121, -121), new Vector2d(-1479, 0), new Vector2d(0, -499), new Vector2d(-200, -200) };
                                if (bi.HasLongTranslatorToLeft)
                                {
                                    var vs = vecs;
                                    info.DisplaySegs = vecs.ToGLineSegments(info.StartPoint);
                                    if (!bi.IsLast)
                                    {
                                        var pt = vs.Take(vs.Count - 1).GetLastPoint(info.StartPoint);
                                        info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(0, -280) }.ToGLineSegments(pt));
                                    }
                                }
                                if (bi.HasLongTranslatorToRight)
                                {
                                    var vs = vecs.GetYAxisMirror();
                                    info.DisplaySegs = vs.ToGLineSegments(info.StartPoint);
                                    if (!bi.IsLast)
                                    {
                                        var pt = vs.Take(vs.Count - 1).GetLastPoint(info.StartPoint);
                                        info.DisplaySegs.AddRange(new List<Vector2d> { new Vector2d(0, -280) }.ToGLineSegments(pt));
                                    }
                                }
                            }
                        }
                        if (run.LeftHanging != null)
                        {
                            run.LeftHanging.IsSeries = gpItem.Hangings[i].IsSeries;
                            handleHanging(run.LeftHanging, true);
                        }
                        if (run.RightHanging != null)
                        {
                            run.RightHanging.IsSeries = gpItem.Hangings[i].IsSeries;
                            handleHanging(run.RightHanging, false);
                        }
                        if (run.BranchInfo != null)
                        {
                            handleBranchInfo(run, info);
                        }
                        if (run.ShowShortTranslatorLabel)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(76, 76), new Vector2d(-424, 424), new Vector2d(-1900, 0) };
                            var segs = vecs.ToGLineSegments(info.EndPoint).Skip(1).ToList();
                            DrawDraiNoteLines(segs);
                            DrawDraiNoteLines(segs);
                            var text = "DN100乙字弯";
                            var pt = segs.Last().EndPoint;
                            DrawNoteText(text, pt);
                        }
                        if (run.HasCheckPoint)
                        {
                            if (run.HasShortTranslator)
                            {
                                DrawPipeCheckPoint(info.Segs.Last().StartPoint.OffsetY(280).ToPoint3d(), true);
                            }
                            else
                            {
                                DrawPipeCheckPoint(info.EndPoint.OffsetY(280).ToPoint3d(), true);
                            }
                        }
                        if (run.HasHorizontalShortLine)
                        {
                            _DrawHorizontalLineOnPipeRun(HEIGHT, info.BasePoint.ToPoint3d());
                        }
                        if (run.HasCleaningPort)
                        {
                            if (run.HasLongTranslator)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(-200, 200), new Vector2d(0, 300), new Vector2d(121, 121), new Vector2d(1109, 0), new Vector2d(121, 121), new Vector2d(0, 279) };
                                if (run.IsLongTranslatorToLeftOrRight == false)
                                {
                                    vecs = vecs.GetYAxisMirror();
                                }
                                if (run.HasShortTranslator)
                                {
                                    var segs = vecs.ToGLineSegments(info.Segs.Last(2).StartPoint.OffsetY(-300));
                                    drawDomePipes(segs);
                                    _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, 2);
                                }
                                else
                                {
                                    var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-300));
                                    drawDomePipes(segs);
                                    _DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, 2);
                                }
                            }
                            else
                            {
                                _DrawCleaningPort(info.StartPoint.OffsetY(-300).ToPoint3d(), true, 2);
                            }

                        }

                        if (run.HasShortTranslator)
                        {
                            DrawShortTranslatorLabel(info.Segs.Last().Center, run.IsShortTranslatorToLeftOrRight);
                        }
                        if (viewModel?.Params?.ShouldRaiseWashingMachine ?? false)
                        {
                        }
                    }


                    var showAllFloorDrainLabel = false;
                    //showAllFloorDrainLabel = true;

                    var HasBalconyWashingMachineFloorDrain = false;
                    var HasBalconyNonWashingMachineFloorDrain = false;
                    var HasKitchenWashingMachineFloorDrain = false;
                    var HasKitchenNonWashingMachineFloorDrain = false;
                    for (int i = start; i >= end; i--)
                    {
                        if (thwPipeLine.Labels.Any(x => IsFL0(x))) continue;
                        var (ok, item) = gpItem.Items.TryGetValue(i + 1);
                        if (!ok) continue;

                        foreach (var pt in fdBasePoints[i].OrderBy(p => p.X))
                        {
                            if (showAllFloorDrainLabel)
                            {
                                if (item.HasBalconyWashingMachineFloorDrain)
                                {
                                    item.HasBalconyWashingMachineFloorDrain = false;
                                    _DrawLabel("接阳台洗衣机地漏DN75，余同", pt, true, 900);
                                    continue;
                                }
                                if (item.HasBalconyNonWashingMachineFloorDrain)
                                {
                                    item.HasBalconyNonWashingMachineFloorDrain = false;
                                    _DrawLabel("接阳台地漏DN75，余同", pt, true, 1800);
                                    continue;
                                }
                                if (item.HasKitchenWashingMachineFloorDrain)
                                {
                                    item.HasKitchenWashingMachineFloorDrain = false;
                                    _DrawLabel("接厨房洗衣机地漏DN75，余同", pt, true, 900);
                                    continue;
                                }
                                if (item.HasKitchenNonWashingMachineFloorDrain)
                                {
                                    item.HasKitchenNonWashingMachineFloorDrain = false;
                                    _DrawLabel("接厨房地漏DN75，余同", pt, true, 1800);
                                    continue;
                                }
                                _DrawLabel(item.ToCadJson(), pt, true, 1800);
                            }
                            else
                            {
                                if (!HasBalconyWashingMachineFloorDrain && item.HasBalconyWashingMachineFloorDrain)
                                {
                                    item.HasBalconyWashingMachineFloorDrain = false;
                                    HasBalconyWashingMachineFloorDrain = true;
                                    _DrawLabel("接阳台洗衣机地漏DN75，余同", pt, true, 900);
                                    continue;
                                }
                                if (!HasBalconyNonWashingMachineFloorDrain && item.HasBalconyNonWashingMachineFloorDrain)
                                {
                                    item.HasBalconyNonWashingMachineFloorDrain = false;
                                    HasBalconyNonWashingMachineFloorDrain = true;
                                    _DrawLabel("接阳台地漏DN75，余同", pt, true, 1800);
                                    continue;
                                }
                                if (!HasKitchenWashingMachineFloorDrain && item.HasKitchenWashingMachineFloorDrain)
                                {
                                    item.HasKitchenWashingMachineFloorDrain = false;
                                    HasKitchenWashingMachineFloorDrain = true;
                                    _DrawLabel("接厨房洗衣机地漏DN75，余同", pt, true, 900);
                                    continue;
                                }
                                if (!HasKitchenNonWashingMachineFloorDrain && item.HasKitchenNonWashingMachineFloorDrain)
                                {
                                    item.HasKitchenNonWashingMachineFloorDrain = false;
                                    HasKitchenNonWashingMachineFloorDrain = true;
                                    _DrawLabel("接厨房地漏DN75，余同", pt, true, 1800);
                                    continue;
                                }
                            }
                        }
                    }
                    //foreach (var pt in kitchenWashingMachineFloorDrains.OrderByDescending(x=>x.Y))
                    //{
                    //    ShowXLabel(pt);
                    //    _DrawLabel("接厨房洗衣机地漏DN75，余同", pt, true, 900);
                    //}
                    //foreach (var pt in kitchenNonWashingMachineFloorDrains.OrderByDescending(x => x.Y))
                    //{
                    //    ShowXLabel(pt);
                    //    _DrawLabel("接厨房地漏DN75，余同", pt, true, 1800);
                    //}
                    //foreach (var pt in balconyWashingMachineFloorDrains.OrderByDescending(x => x.Y))
                    //{
                    //    ShowXLabel(pt);
                    //    _DrawLabel("接阳台洗衣机地漏DN75，余同", pt, true, 900);
                    //}
                    //foreach (var pt in balconyNonWashingMachineFloorDrains.OrderByDescending(x => x.Y))
                    //{
                    //    ShowXLabel(pt);
                    //    _DrawLabel("接阳台地漏DN75，余同", pt, true, 1800);
                    //}
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
                        var line = DrawLineSegmentLazy(seg);
                        Dr.SetLabelStylesForDraiNote(line);
                    }
                    var txtBasePt = isLeftOrRight ? segs[1].EndPoint : segs[1].StartPoint;
                    txtBasePt = txtBasePt.OffsetY(50);
                    if (text1 != null)
                    {
                        var t = DrawTextLazy(text1, 350, txtBasePt);
                        Dr.SetLabelStylesForDraiNote(t);
                    }
                    if (text2 != null)
                    {
                        var t = DrawTextLazy(text2, 350, txtBasePt.OffsetY(-350 - 50));
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
                            var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => !IsTL(x))).ToList();
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
                        if (gpItem.HasTl)
                        {
                            string label1, label2;
                            var labels = ConvertLabelStrings(thwPipeLine.Labels.Where(x => IsTL(x))).ToList();
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
                            drawLabel(info.PlBasePt.OffsetX(300), label1, label2, false);
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
                                Dr.DrawDN_2(info.EndPoint, "W-DRAI-NOTE");
                                if (gpItem.HasTl)
                                {
                                    Dr.DrawDN_3(info.EndPoint.OffsetXY(300, 0), "W-DRAI-NOTE");
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
                                else if (gpItem.MinTl + "F" == storey)
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
                            //FL-0 1楼统一加个这个地漏
                            if (thwPipeLine.Labels.Any(x => IsFL0(x)))
                            {
                                var p = info.EndPoint + new Vector2d(800, -390);
                                DrawFloorDrain(p.ToPoint3d(), true, "普通地漏无存水弯");
                                var vecs = new List<Vector2d>() { new Vector2d(0, -100 + 29), new Vector2d(-300, -300), new Vector2d(-100 - 1158 + 179 * 2, 0), new Vector2d(-300, 300) };
                                var segs = vecs.ToGLineSegments(p + new Vector2d(-180, -160));
                                drawDomePipes(segs);
                                DrawOutlets1(info.EndPoint, 3600, output, dy: -HEIGHT * .2778);
                            }
                            else if (gpItem.IsSingleOutlet)
                            {
                                output.HasCleaningPort2 = true;
                                output.HasWrappingPipe2 = true;
                                output.DN2 = "DN100";
                                DrawOutlets3(info.EndPoint, output);
                            }
                            else if (gpItem.FloorDrainsCountAt1F > 0)
                            {
                                var p = info.EndPoint + new Vector2d(800, -390);
                                DrawFloorDrain(p.ToPoint3d(), true, "普通地漏无存水弯");
                                var vecs = new List<Vector2d>() { new Vector2d(0, -100 + 29), new Vector2d(-300, -300), new Vector2d(-100 - 1158 + 179 * 2, 0), new Vector2d(-300, 300) };
                                var segs = vecs.ToGLineSegments(p + new Vector2d(-180, -160));
                                drawDomePipes(segs);
                                DrawOutlets1(info.EndPoint, 3600, output, dy: -HEIGHT * .2778);
                            }
                            else if (gpItem.HasBasinInKitchenAt1F)
                            {
                                output.HasCleaningPort2 = true;
                                output.HasWrappingPipe2 = true;
                                output.DN2 = "DN100";
                                DrawOutlets4(info.EndPoint, output);
                            }
                            else
                            {
                                DrawOutlets1(info.EndPoint, 3600, output, dy: -HEIGHT * .2778);
                            }

                        }
                    }
                }

            }


            {
                var auto_conn = false;
                auto_conn = true;
                if (auto_conn)
                {
                    foreach (var g in GeoFac.GroupParallelLines(dome_lines, 1, .01))
                    {
                        var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: 10e5));
                        line.Layer = dome_layer;
                        ByLayer(line);
                    }
                    foreach (var g in GeoFac.GroupParallelLines(vent_lines, 1, .01))
                    {
                        var line = DrawLineSegmentLazy(GeoFac.GetCenterLine(g, work_around: 10e5));
                        line.Layer = vent_layer;
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
                    foreach (var _line in vent_lines)
                    {
                        var line = DrawLineSegmentLazy(_line);
                        line.Layer = vent_layer;
                        ByLayer(line);
                    }

                }

            }
        }



        //画单排
        public static void DrawOutlets2(Point2d basePoint)
        {
            var output = new ThwOutput();
            output.DirtyWaterWellValues = new List<string>() { "1", "2", "3" };
            output.HasWrappingPipe1 = true;
            output.HasWrappingPipe2 = true;
            output.HasCleaningPort1 = true;
            output.HasCleaningPort2 = true;
            output.HasLargeCleaningPort = true;
            output.DN1 = "DN100";
            output.DN2 = "DN100";
            DrawOutlets3(basePoint, output);
        }
        public static void DrawOutlets3(Point2d basePoint, ThwOutput output)
        {
            var values = output.DirtyWaterWellValues;
            var vecs = new List<Vector2d> { new Vector2d(0, -1479), new Vector2d(-121, -121), new Vector2d(-2379, 0), new Vector2d(0, 600), new Vector2d(1779, 0), new Vector2d(121, 121), new Vector2d(0, 579) };
            var segs = vecs.ToGLineSegments(basePoint);
            segs.RemoveAt(3);
            DrawDomePipes(segs);
            DrawDiryWaterWells1(segs[2].EndPoint + new Vector2d(-400, 300), values);
            if (output.HasWrappingPipe1) DrawWrappingPipe(segs[3].StartPoint.OffsetX(300));
            if (output.HasWrappingPipe2) DrawWrappingPipe(segs[2].EndPoint.OffsetX(300));
            DrawNoteText(output.DN1, segs[3].StartPoint.OffsetX(750));
            DrawNoteText(output.DN2, segs[2].EndPoint.OffsetX(750));
            if (output.HasCleaningPort1) DrawCleaningPort(segs[4].StartPoint.ToPoint3d(), false, 1);
            if (output.HasCleaningPort2) DrawCleaningPort(segs[2].StartPoint.ToPoint3d(), false, 1);
            DrawCleaningPort(segs[5].EndPoint.ToPoint3d(), true, 2);
        }
        public static void DrawOutlets4(Point2d basePoint, ThwOutput output)
        {
            var values = output.DirtyWaterWellValues;
            var vecs = new List<Vector2d> { new Vector2d(0, -1479), new Vector2d(-121, -121), new Vector2d(-2379 - 400, 0), new Vector2d(0, 600), new Vector2d(1779, 0), new Vector2d(121, 121), new Vector2d(0, 579) };
            var segs = vecs.ToGLineSegments(basePoint);
            segs.RemoveAt(3);
            DrawDomePipes(segs);
            DrawDiryWaterWells1(segs[2].EndPoint + new Vector2d(-400, 300), values);
            if (output.HasWrappingPipe1) DrawWrappingPipe(segs[3].StartPoint.OffsetX(300));
            if (output.HasWrappingPipe2) DrawWrappingPipe(segs[2].EndPoint.OffsetX(300));
            DrawNoteText(output.DN1, segs[3].StartPoint.OffsetX(750));
            DrawNoteText(output.DN2, segs[2].EndPoint.OffsetX(750));
            if (output.HasCleaningPort1) DrawCleaningPort(segs[4].StartPoint.ToPoint3d(), false, 1);
            if (output.HasCleaningPort2) DrawCleaningPort(segs[2].StartPoint.ToPoint3d(), false, 1);
            var p = segs[5].EndPoint;
            DrawDoubleWashBasins((p.OffsetX(-121) + new Vector2d(-279 + 400, 0)).ToPoint3d(), true);
        }
        public static void DrawOutlets5(Point2d basePoint, ThwOutput output, DrainageGroupedPipeItem gpItem)
        {
            var values = output.DirtyWaterWellValues;
            var vecs = new List<Vector2d> { new Vector2d(0, -1479), new Vector2d(-121, -121), new Vector2d(-2379 - 400, 0), new Vector2d(0, 600), new Vector2d(1779, 0), new Vector2d(121, 121), new Vector2d(0, 579) };
            var segs = vecs.ToGLineSegments(basePoint);
            segs.RemoveAt(3);
            DrawDomePipes(segs);
            DrawDiryWaterWells1(segs[2].EndPoint + new Vector2d(-400, 300), values);
            if (output.HasWrappingPipe1) DrawWrappingPipe(segs[3].StartPoint.OffsetX(300));
            if (output.HasWrappingPipe2) DrawWrappingPipe(segs[2].EndPoint.OffsetX(300));
            DrawNoteText(output.DN1, segs[3].StartPoint.OffsetX(750));
            DrawNoteText(output.DN2, segs[2].EndPoint.OffsetX(750));
            if (output.HasCleaningPort1) DrawCleaningPort(segs[4].StartPoint.ToPoint3d(), false, 1);
            if (output.HasCleaningPort2) DrawCleaningPort(segs[2].StartPoint.ToPoint3d(), false, 1);
            var p = segs[5].EndPoint;
            //DrawDoubleWashBasins((p.OffsetX(-121) + new Vector2d(-279 + 400, 0)).ToPoint3d(), true);
            DrawFloorDrain((p.OffsetX(-121) + new Vector2d(-279 + 400, 0)).ToPoint3d(), true);
        }
        //管底标高
        public static void DrawPipeButtomHeightSymbol(double HEIGHT, Point2d p)
        {
            var vecs = new List<Vector2d> { new Vector2d(800, 0), new Vector2d(0, HEIGHT * .4) };
            var segs = vecs.ToGLineSegments(p);
            var lines = DrawLineSegmentsLazy(segs);
            Dr.SetLabelStylesForDraiNote(lines.ToArray());
            DrawBlockReference(blkName: "标高", basePt: segs.Last().EndPoint.OffsetX(250).ToPoint3d(),
 props: new Dictionary<string, string>() { { "标高", "管底H+X.XX" } },
 cb: br =>
 {
     br.Layer = "W-DRAI-NOTE";
 });
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
            static readonly Regex re = new Regex(@"^(F\d?L|T\d?L|P\d?L|D\d?L)(\w*)\-(\w*)([a-zA-Z]*)$");
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

        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-DRAI-NOTE";
                ByLayer(e);
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static void DrawDomePipes(params GLineSegment[] segs)
        {
            DrawDomePipes((IEnumerable<GLineSegment>)segs);
        }
        public static void DrawDomePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.Length > 0));
            lines.ForEach(line => SetDomePipeLineStyle(line));
        }
        public static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.Length > 0));
            lines.ForEach(line =>
            {
                line.Layer = "W-RAIN-PIPE";
                ByLayer(line);
            });
        }
        public static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DrawLineSegmentsLazy(segs.Where(x => x.Length > 0));
            lines.ForEach(line =>
            {
                line.Layer = "W-DRAI-NOTE";
                ByLayer(line);
            });
        }
        public static void qvg1qe()
        {
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var db = adb.Database;
                var basePt = SelectPoint().ToPoint2d();
                var vecs = new List<Vector2d> { new Vector2d(0, -479), new Vector2d(-121, -121), new Vector2d(-1879, 0) };
                var pts = vecs.ToPoint2ds(basePt);
                var segs = vecs.ToGLineSegments(basePt);
                DrainageSystemDiagram.DrawDomePipes(segs);
                DrawWrappingPipe(pts.Last().OffsetX(300));
                DrawCleaningPort(pts[2].ToPoint3d(), false, 1);
                DrawDiryWaterWells1(basePt + new Vector2d(-2000 - 400, -600), new List<string>() { "1", "2" });
                DrawNoteText("DN100", pts[3].OffsetX(750));
            }
        }
        public static void qvg1vf()
        {
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var db = adb.Database;
                var basePt = SelectPoint().ToPoint2d();
                var vecs = new List<Vector2d> { new Vector2d(0, -1479), new Vector2d(-121, -121), new Vector2d(-2379, 0), new Vector2d(0, 600), new Vector2d(1779, 0), new Vector2d(121, 121), new Vector2d(0, 579), new Vector2d(-1600, -700), new Vector2d(0, -600) };
                var pts = vecs.ToPoint2ds(basePt);
                var segs = vecs.ToGLineSegments(basePt);
                {
                    var _segs = segs.ToList();
                    _segs.RemoveAt(8);
                    _segs.RemoveAt(7);
                    _segs.RemoveAt(3);
                    DrawDomePipes(_segs);
                }
                {
                    DrawCleaningPort(pts[2].ToPoint3d(), false, 1);
                    DrawCleaningPort(pts[5].ToPoint3d(), false, 1);
                    DrawCleaningPort(pts[7].ToPoint3d(), true, 2);
                    DrawWrappingPipe(pts[8]);
                    DrawWrappingPipe(pts[9]);
                    DrawDiryWaterWells1(basePt + new Vector2d(-2000 - 400 - 500, -600 - 400 - 400), new List<string>() { "1", "2" });
                    DrawNoteText("DN100", pts[3].OffsetX(750));
                    DrawNoteText("DN100", pts[4].OffsetX(750));
                }
            }
        }
        public static void qvg7cd()
        {
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var basePt = SelectPoint().ToPoint2d();
                DrawLineSegmentLazy(new GLineSegment(basePt, basePt.OffsetX(550 + 3038)), "W-NOTE");
                DrawStoreyHeightSymbol(basePt.OffsetX(550), "-0.35");
                var p1 = basePt + new Vector2d(2830, -390);
                DrawBlockReference("地漏系统", p1.ToPoint3d(), br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏S弯");
                    }
                });
                var vecs = new List<Vector2d> { new Vector2d(180, -460), new Vector2d(0, -94), new Vector2d(141, -141), new Vector2d(1359, 0) };
                var segs = vecs.ToGLineSegments(p1).Skip(1).ToList();
                DrawDomePipes(segs);
                DrawWrappingPipe(segs[1].EndPoint.OffsetX(150));
                DrawNoteText("DN100", segs[1].EndPoint.OffsetX(450));
                var p2 = segs.Last().EndPoint;
                DrawDiryWaterWells2(p2, new List<string>() { "1", "2" });
            }
        }
        public static void qvg7cs()
        {
            FocusMainWindow();
            using (DocLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new _DrawingTransaction(adb))
            {
                var basePt = SelectPoint().ToPoint2d();
                //DrawStoreyHeightSymbol(basePt, "-0.75");
                //DrawLineSegmentLazy(new GLineSegment(basePt, basePt.OffsetX(2450 )), "W-NOTE");
                DrawBlockReference("侧排地漏", basePt.ToPoint3d(), cb: br => br.Layer = "W-DRAI-FLDR");
            }
        }

        public static void DrawStoreyHeightSymbol(Point2d basePt, string label = "666")
        {
            DrawBlockReference(blkName: "标高", basePt: basePt.ToPoint3d(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", label } }, cb: br => { ByLayer(br); });
        }

        public static void DrawWrappingPipe(Point2d basePt)
        {
            DrawWrappingPipe(basePt.ToPoint3d());
        }
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DrawBlockReference("套管系统", basePt, br =>
            {
                br.Layer = "W-BUSH";
                ByLayer(br);
            });
        }

        public static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * .7;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + 150;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-200);
            var p3 = p1.OffsetX(200);
            var line = DrawLineLazy(p2, p3);
            line.Layer = "W-DRAI-NOTE";
            ByLayer(line);
        }

        public static Point2d NewMethod5(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), false);
            return lastPt;
        }

        public static Point2d NewMethod4(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS;
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), true);
            return lastPt;
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight, string value = "普通地漏P弯")
        {
            if (Testing) return;
            if (leftOrRight)
            {
                DrawBlockReference("地漏系统", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", value);
                    }
                });
            }
            else
            {
                DrawBlockReference("地漏系统", basePt,
               br =>
               {
                   br.Layer = "W-DRAI-EQPM";
                   ByLayer(br);
                   br.ScaleFactors = new Scale3d(-2, 2, 2);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue("可见性", value);
                   }
               });
            }

        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DrawBlockReference("S型存水弯", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    ByLayer(br);
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                        br.ObjectId.SetDynBlockValue("翻转状态", (short)1);
                    }
                });
            }
            else
            {
                DrawBlockReference("S型存水弯", basePt,
                   br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                       ByLayer(br);
                       br.ScaleFactors = new Scale3d(-2, 2, 2);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                           br.ObjectId.SetDynBlockValue("翻转状态", (short)1);
                       }
                   });
            }
        }
        public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
        {
            if (leftOrRight)
            {
                DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                });
            }
            else
            {
                DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    ByLayer(br);
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90 + 180);
                });
            }
        }
        public static void DrawPipeCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DrawBlockReference(blkName: "立管检查口", basePt: basePt,
      cb: br =>
      {
          if (leftOrRight)
          {
              br.ScaleFactors = new Scale3d(-1, 1, 1);
          }
          ByLayer(br);
          br.Layer = "W-DRAI-EQPM";
      });
        }

        public static void NewMethod2(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2, double height1)
        {

            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var v = new Vector2d(lastPt1.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
            {
                var height2 = HEIGHT + lastPt2.Y;
                var segs = points2.ToGLineSegments(basePt.OffsetY(HEIGHT - height2));
                DrawDomePipes(segs);
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        public static void NewMethod3(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2)
        {
            var height1 = LONG_TRANSLATOR_HEIGHT1;
            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var v = new Vector2d(lastPt1.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
            {
                var height2 = HEIGHT + lastPt2.Y;
                var segs = points2.ToGLineSegments(basePt.OffsetY(HEIGHT - height2));
                var lines = DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        public static void NewMethod1(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = HEIGHT + lastPt.Y;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
                v += new Vector2d(lastPt.X, 0);
            }
        }

        public static void NewMethod(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = LONG_TRANSLATOR_HEIGHT1;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));

            {
                var p1 = basePt.OffsetX(points.Last().X);
                var p2 = p1.OffsetY(HEIGHT - height + lastPt.Y);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            v += new Vector2d(lastPt.X, 0);
        }
        public static void DrawDirtyWaterWell(Point2d basePt, string value)
        {
            DrawDirtyWaterWell(basePt.ToPoint3d(), value);
        }
        public static void DrawDirtyWaterWell(Point3d basePt, string value)
        {
            DrawBlockReference(blkName: "污废合流井编号", basePt: basePt.OffsetY(-400),
            props: new Dictionary<string, string>() { { "-", value } },
            cb: br =>
            {
                br.Layer = "W-DRAI-EQPM";
                ByLayer(br);
            });
        }
        public static void SetVentPipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-VENT-PIPE";
            ByLayer(line);
        }

        public static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-DOME-PIPE";
            ByLayer(line);
        }
    }
    public class DrainageGeoData
    {
        public List<GRect> Storeys;
        public List<CText> Labels;
        public List<GLineSegment> LabelLines;
        public List<GLineSegment> DLines;//排水立管专用转管
        public List<GLineSegment> VLines;//通气立管专用转管
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
        }
        public void FixData()
        {
            Init();
            Storeys = Storeys.Where(x => x.IsValid).Distinct().ToList();
            Labels = Labels.Where(x => x.Boundary.IsValid).Distinct().ToList();
            LabelLines = LabelLines.Where(x => x.Length > 0).Distinct().ToList();
            DLines = DLines.Where(x => x.Length > 0).Distinct().ToList();
            VLines = VLines.Where(x => x.Length > 0).Distinct().ToList();
            VerticalPipes = VerticalPipes.Where(x => x.IsValid).Distinct().ToList();
            WrappingPipes = WrappingPipes.Where(x => x.IsValid).Distinct().ToList();
            FloorDrains = FloorDrains.Where(x => x.IsValid).Distinct().ToList();
            WaterPorts = WaterPorts.Where(x => x.IsValid).Distinct().ToList();
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
        }
        public static DrainageCadData Create(DrainageGeoData data)
        {
            var bfSize = 10;
            var o = new DrainageCadData();
            o.Init();
            o.Storeys.AddRange(data.Storeys.Select(x => x.ToPolygon()));
            o.Labels.AddRange(data.Labels.Select(x => x.Boundary.ToPolygon()));

            if (false) o.LabelLines.AddRange(data.LabelLines.Select(NewMethod(bfSize)));
            else o.LabelLines.AddRange(data.LabelLines.Select(ConvertLabelLinesF()));
            o.DLines.AddRange(data.DLines.Select(ConvertDLinesF()));
            o.VLines.AddRange(data.VLines.Select(ConvertVLinesF()));
            o.WrappingPipes.AddRange(data.WrappingPipes.Select(ConvertWrappingPipesF()));
            if (false) o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesPreciseF()));
            else o.VerticalPipes.AddRange(data.VerticalPipes.Select(ConvertVerticalPipesF()));
            o.FloorDrains.AddRange(data.FloorDrains.Select(ConvertFloorDrainsF()));
            o.WaterPorts.AddRange(data.WaterPorts.Select(ConvertWaterPortsF()));
            o.WashingMachines.AddRange(data.WashingMachines.Select(ConvertWashingMachinesF()));
            o.Basins.AddRange(data.Basins.Select(ConvertWashingMachinesF()));
            o.MopPools.AddRange(data.MopPools.Select(ConvertWashingMachinesF()));
            o.CleaningPorts.AddRange(data.CleaningPorts.Select(ConvertCleaningPortsF()));
            o.SideFloorDrains.AddRange(data.SideFloorDrains.Select(ConvertSideFloorDrains()));
            o.PipeKillers.AddRange(data.PipeKillers.Select(ConvertVerticalPipesF()));
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
            return ret;
        }
        public List<DrainageCadData> SplitByStorey()
        {
            var lst = new List<DrainageCadData>(this.Storeys.Count);
            if (this.Storeys.Count == 0) return lst;
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
        public static List<ThStoreysData> GetStoreys(Geometry range, AcadDatabase adb, CommandContext ctx)
        {
            return ctx.StoreyContext.thStoreysDatas;
        }
        public static List<ThStoreysData> GetStoreys(Point3dCollection range, AcadDatabase adb)
        {
            var storeysRecEngine = new ThStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            var storeys = new List<ThStoreysData>();
            foreach (var s in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
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
            FixStoreys(storeys);
            return storeys;
        }
        public static void FixStoreys(List<ThStoreysData> storeys)
        {
            var lst1 = storeys.Where(s => s.Storeys.Count == 1).Select(s => s.Storeys[0]).ToList();
            foreach (var s in storeys.Where(s => s.Storeys.Count > 1).ToList())
            {
                var hs = new HashSet<int>(s.Storeys);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Storeys.Clear();
                s.Storeys.AddRange(hs.OrderBy(i => i));
            }
        }
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
                //
            }
            private static bool IsVisibleLayer(LayerTableRecord layerTableRecord)
            {
                return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
            }
            public override bool IsDistributionElement(Entity entity)
            {
                if (entity is BlockReference reference)
                {
                    //return reference.GetEffectiveName().Contains("A-Toilet-9") ;
                    if (reference.GetEffectiveName().Contains("A-Toilet-9"))
                    {
                        using var adb = AcadDatabase.Use(reference.Database);
                        if (IsVisibleLayer(adb.Layers.Element(reference.Layer)))
                            return true;
                    }

                }
                return false;
            }

            public override bool CheckLayerValid(Entity curve)
            {
                return true;
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
    public class CommandContext
    {
        public Point3dCollection range;
        public StoreyContext StoreyContext;
        public DrainageSystemDiagramViewModel ViewModel;
        public System.Windows.Window window;
    }
    public class ThDrainageService
    {
        public static CommandContext commandContext;
        public static void ConnectLabelToLabelLine(DrainageGeoData geoData)
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

        public static void PreFixGeoData(DrainageGeoData geoData)
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
            {
                //处理立管重叠的情况
                geoData.VerticalPipes = geoData.VerticalPipes.Distinct(new GRect.EqualityComparer(.1)).ToList();
            }
            {
                //处理洗衣机重叠的情况
                geoData.WashingMachines = GeoFac.GroupGeometries(geoData.WashingMachines.Select(x => x.ToPolygon()).Cast<Geometry>().ToList()).Select(x => GeoFac.CreateGeometryEx(x).Envelope.ToGRect()).ToList();
            }
            {
                //处理标注重叠的情况
                geoData.Labels = geoData.Labels.Distinct(CreateEqualityComparer<CText>((x, y) => x.Text == y.Text && x.Boundary.EqualsTo(y.Boundary, 10))).ToList();
            }
            {
                //其他的也顺手处理下好了。。。
                var cmp = new GRect.EqualityComparer(.1);
                geoData.FloorDrains = geoData.FloorDrains.Distinct(cmp).ToList();
                geoData.WashingMachines = geoData.WashingMachines.Distinct(cmp).ToList();
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
        public static bool HasAiringHorizontalPipe(string storeyLabel, List<Geometry> PLs, List<Geometry> FLs, List<Geometry> toilets)
        {
            //6.4	通气立管的横管
            //在通气立管出现的每一层都和PL（+DL）或FL进行连通，直至有排水点位的最高层为止。
            //出现通气立管的最高层的判断：
            //在普通楼层（数字+F）找到PL或FL附近300范围内有卫生间的最高楼层即可，这个楼层就是连接通气立管的最高楼层。
            bool test3()
            {
                if (FLs.Count == 0 || toilets.Count == 0) return false;
                var toiletsGeo = GeoFac.CreateGeometry(toilets);
                foreach (var fl in FLs)
                {
                    if (GeoFac.CreateCirclePolygon(fl.GetCenter(), 300, 36).Intersects(toiletsGeo)) return true;
                }
                return false;
            }
            return Regex.IsMatch(storeyLabel, @"^\d+F$") && ((PLs.Count > 0) || test3());
        }
        public static bool IsNotedLabel(string label)
        {
            //接至厨房单排、接至卫生间单排。。。
            return label.Contains("单排") || label.Contains("设置乙字弯");
        }
        public static bool IsWantedLabelText(string label)
        {
            if (label == null) return false;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label);
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return false;
            return IsFL(label) || IsPL(label) || IsTL(label) || IsDL(label)
                || label.StartsWith("Y1L") || label.StartsWith("Y2L") || label.StartsWith("NL") || label.StartsWith("YL")
                || label.Contains("单排")
                || label.StartsWith("RML") || label.StartsWith("RMHL")
                || label.StartsWith("J1L") || label.StartsWith("J2L")
                ;
        }
        public static void FixStoreys(List<ThStoreysData> storeys)
        {
            var lst1 = storeys.Where(s => s.Storeys.Count == 1).Select(s => s.Storeys[0]).ToList();
            foreach (var s in storeys.Where(s => s.Storeys.Count > 1).ToList())
            {
                var hs = new HashSet<int>(s.Storeys);
                foreach (var _s in lst1) hs.Remove(_s);
                s.Storeys.Clear();
                s.Storeys.AddRange(hs.OrderBy(i => i));
            }
        }
        public static void CollectFloorListDatasEx()
        {
            static StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb, List<ThMEPWSS.Model.FloorFramed> resFloors)
            {
                var ctx = new StoreyContext();

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
                FixStoreys(storeys);
                ctx.thStoreysDatas = storeys;
                return ctx;
            }

            FocusMainWindow();
            if (ThMEPWSS.Common.FramedReadUtil.SelectFloorFramed(out List<ThMEPWSS.Model.FloorFramed> resFloors))
            {
                var ctx = commandContext;
                using var adb = AcadDatabase.Active();
                ctx.StoreyContext = GetStoreyContext(null, adb, resFloors);
                InitFloorListDatas(adb);
            }
        }
        public static bool CollectFloorListDatas()
        {
            FocusMainWindow();
            var range = TrySelectRange();
            if (range == null) return false;
            var ctx = commandContext;
            ctx.range = range;
            using var adb = AcadDatabase.Active();
            ctx.StoreyContext = GetStoreyContext(range, adb);
            InitFloorListDatas(adb);
            return true;
        }
        public static StoreyContext GetStoreyContext(Point3dCollection range, AcadDatabase adb)
        {
            var ctx = new StoreyContext();
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
            FixStoreys(storeys);
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
        public class WLGrouper
        {
            public class ToiletGrouper
            {
                public List<string> PLs;//污废合流立管
                public List<string> TLs;//通气立管
                public List<string> DLs;//沉箱立管
                public List<string> FLs;//废水立管
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
        public enum WLType
        {
            PL, PL_TL, PL_DL, PL_TL_DL, FL, FL_TL,
        }
        public class WLGeoGrouper
        {
            public class ToiletGrouper
            {
                public List<Geometry> PLs;//污废合流立管
                public List<Geometry> TLs;//通气立管
                public List<Geometry> DLs;//沉箱立管
                public List<Geometry> FLs;//废水立管
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
                        var range = GeoFac.CreateCirclePolygon(pl.GetCenter(), 300, 12);
                        //在每一根PL的每一层300范围内找TL
                        var tls = GeoFac.CreateIntersectsSelector(TLs.Except(ok_pipes).ToList())(range);
                        ok_pipes.AddRange(tls);
                        //在每一根PL的每一层300范围内找DL
                        var dls = GeoFac.CreateIntersectsSelector(DLs.Except(ok_pipes).ToList())(range);
                        ok_pipes.AddRange(dls);
                        var o = new ToiletGrouper();
                        list.Add(o);
                        o.Init();
                        o.PLs.Add(pl);
                        o.TLs.AddRange(tls);
                        o.DLs.AddRange(dls);
                        if (tls.Count == 0 && dls.Count == 0)
                        {
                            o.WLType = WLType.PL;
                        }
                        else if (tls.Count > 0 && dls.Count > 0)
                        {
                            o.WLType = WLType.PL_TL_DL;
                        }
                        else if (tls.Count > 0 && dls.Count == 0)
                        {
                            o.WLType = WLType.PL_TL;
                        }
                        else if (tls.Count == 0 && dls.Count > 0)
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
                        //在每一根FL的每一层300范围内找TL
                        var range = GeoFac.CreateCirclePolygon(fl.GetCenter(), 300, 12);
                        var tls = GeoFac.CreateIntersectsSelector(TLs.Except(ok_pipes).ToList())(range);
                        ok_pipes.AddRange(tls);
                        if (tls.Count == 0)
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


    }

}


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
    using System.Text;
    using static ThMEPWSS.Assistant.DrawUtils;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using NetTopologySuite.Geometries;
    using ThMEPWSS.ReleaseNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Model;
    using System.IO;
    using System.Windows.Forms;
    using System.Diagnostics;
    using static THDrainageService;
    using System.Text.RegularExpressions;

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
        List<GRect> wrappingPipes => geoData.WrappingPipes;
        List<GRect> floorDrains => geoData.FloorDrains;
        List<Point2d> sideFloorDrains => geoData.SideFloorDrains;
        List<GRect> waterPorts => geoData.WaterPorts;
        List<string> waterPortLabels => geoData.WaterPortLabels;
        List<GRect> storeys => geoData.Storeys;
        List<Point2d> cleaningPorts => geoData.CleaningPorts;
        List<GRect> washingMachines => geoData.WashingMachines;
        List<GRect> mopPools => geoData.MopPools;
        List<GRect> basins => geoData.Basins;
        List<GRect> pipeKillers => geoData.PipeKillers;
        public void CollectStoreys(Geometry range, CommandContext ctx)
        {
            storeys.AddRange(ctx.StoreyContext.thStoreysDatas.Select(x => x.Boundary));
        }
        public void CollectKillers()
        {
            foreach (var c in entities.Where(x => x.Layer is "feng_dbg_test_washing_machine"))
            {
                washingMachines.Add(c.Bounds.ToGRect());
            }
            DoExtract(adb, (br, m) =>
            {
                var ok = false;
                var basinNames = new List<string>() { "A-Kitchen-3", "A-Kitchen-4", "A-Toilet-1", "A-Toilet-2", "A-Toilet-3", "A-Toilet-4", "-XiDiPen-" };
                var washingMachinesNames = new List<string>() { "A-Toilet-9", "$xiyiji" };
                var mopPoolNames = new List<string>() { "A-Kitchen-9", };
                var killerNames = new List<string>() { "-XiDiPen-", "0$座厕", "0$asdfghjgjhkl", "A-Toilet-", "A-Kitchen-", "|lp", "|lp1", "|lp2" };
                var name = br.GetEffectiveName();
                if (killerNames.Any(x => name.Contains(x)))
                {
                    var e = br.GetTransformedCopy(m);
                    var r = e.Bounds.ToGRect();
                    if (!r.IsValid) return true;
                    pipeKillers.Add(r);
                    if (name.Contains("A-Toilet-9"))
                    {
                        washingMachines.Add(r);
                    }
                    ok = true;
                }
                if (basinNames.Any(x => name.Contains(x)))
                {
                    var e = br.GetTransformedCopy(m);
                    var r = e.Bounds.ToGRect();
                    if (!r.IsValid) return true;
                    basins.Add(r);
                    ok = true;
                }
                if (washingMachinesNames.Any(x => name.Contains(x)))
                {
                    var e = br.GetTransformedCopy(m);
                    var r = e.Bounds.ToGRect();
                    if (!r.IsValid) return true;
                    washingMachines.Add(r);
                    ok = true;
                }
                if (mopPoolNames.Any(x => name.Contains(x)))
                {
                    var e = br.GetTransformedCopy(m);
                    var r = e.Bounds.ToGRect();
                    if (!r.IsValid) return true;
                    mopPools.Add(r);
                    ok = true;
                }
                return ok;
            });
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

        //int distinguishDiameter = 35;//在雨水系统图中用这个值
        int distinguishDiameter = 20;//在排水系统图中，居然还有半径25的圆的立管。。。不管冷凝管了
        public void CollectWrappingPipes()
        {
            var ents = new List<Entity>();
            ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.GetEffectiveName().Contains("套管") : x.Layer == "W-BUSH"));
            wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
        }
        public void CollectVerticalPipes()
        {
            static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-RAIN-EQPM" or "VPIPE-污水" or "VPIPE-废水" or "W-DRAI-DIMS";
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

        public void CollectCTexts()
        {
            {
                foreach (var e in entities.OfType<Entity>().Where(e => e.Layer is "W-DRAI-DIMS" or "W-RAIN-DIMS" && e.GetRXClass().DxfName.ToUpper() == "TCH_VPIPEDIM"))
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
                static bool f(string layer) => layer is "W-DRAI-EQPM" or "W-DRAI-NOTE" or "W-DRAI-DIMS" or "W-RAIN-NOTE" or "W-RAIN-DIMS" or "W-WSUP-DIMS" or "W-WSUP-NOTE";
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
    public partial class DrainageSystemDiagram
    {
        public static void draw7(List<DrainageDrawingData> drDatas, Point2d basePoint)
        {
            var labels = drDatas.SelectMany(drData => drData.VerticalPipeLabels).Distinct().ToList();
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            var COUNT = labels.Count;
            var STOREY_COUNT = drDatas.Count;
            var dy = HEIGHT - 1800.0;
            //var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").ToList();
            var groups = new List<ThwPipeLineGroup>();
            foreach (var label in labels)
            {
                var group = new ThwPipeLineGroup();
                groups.Add(group);
                var ppl = group.PL = new ThwPipeLine();
                ppl.Labels = new List<string>() { label };
                ppl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var storey = storeys[i];
                    var drData = drDatas[i];
                    var run = new ThwPipeRun()
                    {
                        Storey = storey,
                        HasShortTranslator = drData.ShortTranslatorLabels.Contains(label),
                        HasLongTranslator = drData.LongTranslatorLabels.Contains(label),
                        IsShortTranslatorToLeftOrRight = true,
                        IsLongTranslatorToLeftOrRight = true,
                        HasCheckPoint = true,
                        HasCleaningPort = drData.CleaningPorts.Contains(label),
                    };
                    ppl.PipeRuns.Add(run);
                    {
                        bool? flag = null;
                        for (int i1 = ppl.PipeRuns.Count - 1; i1 >= 0; i1--)
                        {
                            var r = ppl.PipeRuns[i1];
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
                        if (drData.FloorDrains.TryGetValue(label, out int count))
                        {
                            if (count > 0)
                            {
                                var hanging = run.LeftHanging = new Hanging();
                                hanging.FloorDrainsCount = count;
                                hanging.HasSCurve = true;
                            }
                        }
                    }
                    {
                        var b = run.BranchInfo = new BranchInfo();
                        b.MiddleLeftRun = true;
                    }
                    {
                        if (storey == "1F")
                            if (drData.Outlets.TryGetValue(label, out string well))
                            {
                                var o = ppl.Output = new ThwOutput();
                                o.DirtyWaterWellValues = new List<string>();
                                o.DirtyWaterWellValues.Add(well);
                            }
                    }

                }
            }

            DrawVer1(new DrawingOption()
            {
                BasePoint = basePoint,
                OFFSET_X = OFFSET_X,
                SPAN_X = SPAN_X,
                HEIGHT = HEIGHT,
                COUNT = COUNT,
                Storeys = storeys,
                Groups = groups
            });
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
        public static bool CollectDrainageData(Point3dCollection range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, bool noWL = false)
        {
            CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData);
            return CreateDrainageDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CollectDrainageData(Geometry range, AcadDatabase adb, out List<StoreysItem> storeysItems, out List<DrainageDrawingData> drDatas, CommandContext ctx, bool noWL = false)
        {
            CollectDrainageGeoData(range, adb, out storeysItems, out DrainageGeoData geoData, ctx);
            return CreateDrainageDrawingData(adb, out drDatas, noWL, geoData);
        }
        public static bool CreateDrainageDrawingData(AcadDatabase adb, out List<DrainageDrawingData> drDatas, bool noWL, DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            //考虑到污废合流的场景占比在全国范围项目的比例较高，因此优先支持污废合流。判断方式：
            //通过框选范围内的立管判断。若存在污水立管（WL开头），则程序中断，并提醒暂不支持污废分流。
            if (noWL && geoData.Labels.Any(x => IsWL(x.Text)))
            {
                MessageBox.Show("暂不支持污废分流");
                drDatas = null;
                return false;
            }
            drDatas = _CreateDrainageDrawingData(adb, geoData, true);
            return true;
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

        public static void CollectDrainageGeoData(Geometry range, AcadDatabase adb, out List<StoreysItem> storeysItems, out DrainageGeoData geoData, CommandContext ctx)
        {
            var storeys = GetStoreys(range, adb, ctx);
            FixStoreys(storeys);
            storeysItems = GetStoreysItem(storeys);
            geoData = new DrainageGeoData();
            geoData.Init();
            DrainageService.CollectGeoData(range, adb, geoData, ctx);
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
        public static void Draw(List<DrainageDrawingData> drDatas, Point2d basePoint, List<StoreysItem> storeysItems)
        {
            var allLabels = drDatas.SelectMany(drData => drData.VerticalPipeLabels).Where(lb => ThDrainageService.IsWantedLabelText(lb)).Distinct().ToList();
            //var pipeLabels = allLabels.Where(lb => lb.StartsWith()).OrderBy(x => x).ToList();
            var pipeLabels = allLabels.Where(lb => IsFL(lb) || IsPL(lb)).OrderBy(x => x).ToList();

            var pipesCmpInfos = new List<PipeCmpInfo>();
            foreach (var label in pipeLabels)
            {
                var info = new PipeCmpInfo();
                pipesCmpInfos.Add(info);
                info.label = label;
                info.PipeRuns = new List<PipeCmpInfo.PipeRunCmpInfo>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var drData = drDatas[i];
                    var runInfo = new PipeCmpInfo.PipeRunCmpInfo();
                    runInfo.HasShortTranslator = drData.ShortTranslatorLabels.Contains(label);
                    runInfo.HasLongTranslator = drData.LongTranslatorLabels.Contains(label);
                    runInfo.HasCleaningPort = drData.CleaningPorts.Contains(label);
                    runInfo.HasWrappingPipe = drData.WrappingPipes.Contains(label);
                    info.PipeRuns.Add(runInfo);
                }
                info.IsWaterPortOutlet = drDatas.Any(x => x.Outlets.ContainsKey(label));
            }
            var list = pipesCmpInfos.GroupBy(x => x).ToList();

            var OFFSET_X = 2500.0;
            var SPAN_X = 6000.0;
            //var HEIGHT = 1800.0;
            var HEIGHT = 3000.0;
            var COUNT = list.Count;
            var STOREY_COUNT = drDatas.Count;



            var pipeLineGroups = new List<ThwPipeLineGroup>();
            var storeys = storeysItems.Where(x => x.Labels != null).SelectMany(x => x.Labels).ToList();
            SortStoreys(storeys);
            foreach (var g in list)
            {
                var info = g.Key;
                var _labels = g.Select(x => x.label).ToList();
                var label = g.Key.label;
                var group = new ThwPipeLineGroup();
                pipeLineGroups.Add(group);
                var ppl = group.PL = new ThwPipeLine();
                ppl.Labels = g.Select(x => x.label).ToList();
                var runs = ppl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < drDatas.Count; i++)
                {
                    var drData = drDatas[i];

                    foreach (var storey in storeysItems[i].Labels.Yield())
                    {
                        if (!IsNumStorey(storey)) continue;
                        var run = new ThwPipeRun()
                        {
                            Storey = storey,
                            HasShortTranslator = info.PipeRuns[i].HasShortTranslator,
                            HasLongTranslator = info.PipeRuns[i].HasLongTranslator,
                            IsShortTranslatorToLeftOrRight = true,
                            IsLongTranslatorToLeftOrRight = true,
                            HasCheckPoint = true,
                            HasCleaningPort = info.PipeRuns[i].HasCleaningPort,
                        };
                        //如果有转管，那就加个清扫口
                        if (run.HasLongTranslator || run.HasShortTranslator)
                        {
                            run.HasCleaningPort = true;
                        }
                        //针对pl，设置清扫口
                        if (IsPL(label))
                        {
                            run.HasCleaningPort = true;
                            //只要有通气立管，就一定有横管
                            run.HasHorizontalShortLine = true;
                        }
                        runs.Add(run);

                        {
                            {
                                bool? flag = null;
                                for (int i1 = ppl.PipeRuns.Count - 1; i1 >= 0; i1--)
                                {
                                    var r = ppl.PipeRuns[i1];
                                    if (r.HasLongTranslator)
                                    {
                                        if (!flag.HasValue)
                                        {
                                            flag = IsPL(label) || IsFL(label);
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
                                if (drData.FloorDrains.TryGetValue(label, out int count))
                                {
                                    if (count > 0)
                                    {
                                        var hanging = run.LeftHanging = new Hanging();
                                        hanging.FloorDrainsCount = count;
                                        hanging.HasSCurve = true;
                                    }
                                }
                                else if (IsFL(label))
                                {
                                    var hanging = run.LeftHanging = new Hanging();
                                    hanging.FloorDrainsCount = count;
                                    hanging.HasDoubleSCurve = true;
                                }
                            }
                            {
                                var b = run.BranchInfo = new BranchInfo();
                                if (IsPL(label) || IsFL(label))
                                {
                                    b.MiddleLeftRun = true;
                                }
                                else if (IsTL(label))
                                {
                                    b.BlueToLeftMiddle = true;
                                }
                            }
                            {
                                if (storey == "1F")
                                {
                                    var o = ppl.Output = new ThwOutput();
                                    o.DN1 = "DN100";
                                    o.HasWrappingPipe1 = g.Key.PipeRuns.FirstOrDefault().HasWrappingPipe;
                                    var hs = new HashSet<string>();
                                    foreach (var _label in g.Select(x => x.label))
                                    {
                                        if (drData.Outlets.TryGetValue(_label, out string well))
                                        {
                                            hs.Add(well);
                                        }
                                    }
                                    o.DirtyWaterWellValues = hs.OrderBy(x => { long.TryParse(x, out long v); return v; }).ToList();
                                    ppl.Labels.Add("===");
                                    ppl.Labels.Add("outlets:");
                                    ppl.Labels.AddRange(o.DirtyWaterWellValues);
                                }

                            }
                        }
                    }



                }
            }
            var pipeLineGroups2 = new List<ThwPipeLineGroup>();


            var maxStorey = storeys.Where(IsNumStorey).FindByMax(GetStoreyScore);
            //var minStorey = storeys.Where(x => GetScore(x) < ).FindByMin(GetScore);
            var minStorey = "3F";

            Console.WriteLine(drDatas.Select(x => x.toiletGroupers).ToCadJson());


            for (int j = 0; j < pipeLineGroups.Count; j++)
            {
                var group = new ThwPipeLineGroup();
                pipeLineGroups2.Add(group);
                var ttl = group.TL = new ThwPipeLine();
                var runs = ttl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < pipeLineGroups[j].PL.PipeRuns.Count; i++)
                {
                    var run = new ThwPipeRun();
                    runs.Add(run);
                    run.Storey = pipeLineGroups[j].PL.PipeRuns[i].Storey;
                    run.HasLongTranslator = pipeLineGroups[j].PL.PipeRuns[i].HasLongTranslator;
                    run.IsLongTranslatorToLeftOrRight = pipeLineGroups[j].PL.PipeRuns[i].IsLongTranslatorToLeftOrRight;
                    run.HasShortTranslator = pipeLineGroups[j].PL.PipeRuns[i].HasShortTranslator;
                    run.IsShortTranslatorToLeftOrRight = pipeLineGroups[j].PL.PipeRuns[i].IsShortTranslatorToLeftOrRight;
                }
                for (int i = 0; i < runs.Count; i++)
                {
                    var run = runs[i];
                    {
                        var s = GetStoreyScore(run.Storey);
                        var s1 = GetStoreyScore(maxStorey);
                        var s2 = GetStoreyScore(minStorey);
                        if (!(s1 >= s && s >= s2))
                        {
                            runs[i] = null;
                            continue;
                        }
                    }
                    var bi = run.BranchInfo = new BranchInfo();
                    if (run.Storey == maxStorey)
                    {
                        bi.BlueToLeftFirst = true;
                    }
                    else if (run.Storey == minStorey)
                    {
                        bi.BlueToLeftLast = true;
                    }
                    else
                    {
                        bi.BlueToLeftMiddle = true;
                    }
                }
            }
            var maxStoreyIndex = storeys.IndexOf(maxStorey);

            DrawVer1(new DrawingOption()
            {
                BasePoint = basePoint,
                OFFSET_X = OFFSET_X,
                SPAN_X = SPAN_X,
                HEIGHT = HEIGHT,
                COUNT = COUNT,
                Storeys = storeys,
                Groups = pipeLineGroups,
                Layer = "W-DRAI-DOME-PIPE",
                MaxStoreyIndex = maxStoreyIndex,
            });

            if (pipeLineGroups2.Count > 0)
            {
                DrawVer1(new DrawingOption()
                {
                    BasePoint = basePoint,
                    OFFSET_X = OFFSET_X,
                    SPAN_X = SPAN_X,
                    HEIGHT = HEIGHT,
                    COUNT = COUNT,
                    Storeys = storeys,
                    Groups = pipeLineGroups2,
                    Layer = "W-DRAI-EQPM",
                    DrawStoreyLine = false,
                    IsDebugging = true,
                    MaxStoreyIndex = maxStoreyIndex,
                });
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
        public class DrawingOption
        {
            public int MaxStoreyIndex;
            public string Layer;
            public Point2d BasePoint;
            public double OFFSET_X;
            public double SPAN_X;
            public double HEIGHT;
            public double COUNT;
            public List<string> Storeys;
            public bool DrawStoreyLine = true;
            public bool IsDebugging;
            public List<ThwPipeLineGroup> Groups;
            public DrawingOption Clone()
            {
                return (DrawingOption)MemberwiseClone();
            }
        }

    }
    public class StoreyContext
    {
        public List<ThStoreysData> thStoreysDatas;
        public List<ThMEPEngineCore.Model.Common.ThStoreys> thStoreys;
        public List<ObjectId> GetObjectIds()
        {
            return thStoreys.Select(o => o.ObjectId).ToList();
        }
    }
}