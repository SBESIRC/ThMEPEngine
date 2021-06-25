namespace ThMEPWSS.Pipe.Service
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
    using static ThMEPWSS.DebugNs.ThPublicMethods;
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
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
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




    public class DrainageDrawingData
    {
        public List<string> VerticalPipeLabels;
        public List<string> LongTranslatorLabels;
        public List<string> ShortTranslatorLabels;
        public Dictionary<string, int> FloorDrains;
        public List<string> CleaningPorts;
        public Dictionary<string, string> Outlets;
        public List<string> WrappingPipes;
        public void Init()
        {
            VerticalPipeLabels ??= new List<string>();
            LongTranslatorLabels ??= new List<string>();
            ShortTranslatorLabels ??= new List<string>();
            FloorDrains ??= new Dictionary<string, int>();
            CleaningPorts ??= new List<string>();
            Outlets ??= new Dictionary<string, string>();
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
        public static void TestDrawingDatasCreation(DrainageGeoData geoData)
        {
            ThDrainageService.PreFixGeoData(geoData);
            ThDrainageService.ConnectLabelToLabelLine(geoData);
            geoData.FixData();
            var cadDataMain = DrainageCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            TestDrawingDatasCreation(geoData, cadDataMain, cadDatas);
        }
        public static DrainageGeoData CollectGeoData()
        {
            if (commandContext != null) return null;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return null;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = new DrainageGeoData();
                geoData.Init();
                CollectGeoData(range, adb, geoData);
                return geoData;
            }
        }
        public static void DrawDrainageSystemDiagram2()
        {
            if (commandContext != null) return;
            Dbg.FocusMainWindow();
            var range = Dbg.TrySelectRange();
            if (range == null) return;
            //if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
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
                    sv.CreateDrawingDatas();
                    //DU.Draw();
                    //DU.Dispose();
                    if (sv.DrainageSystemDiagram == null) sv.CreateDrainageSystemDiagram();

                    //DU.Dispose();
                    //sv.RainSystemDiagram.Draw(basePt);
                    //DU.Draw(adb);
                    //Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                }
                finally
                {
                    DU.Dispose();
                }
            }
        }

        private static void CollectGeoData(Point3dCollection range, AcadDatabase adb, DrainageGeoData geoData)
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
            cl.CollectStoreys(range);
        }

        public static void DrawDrainageSystemDiagram3()
        {
            Dbg.FocusMainWindow();
            if (!Dbg.TrySelectPoint(out Point3d basePt)) return;
            DU.Dispose();
            if (commandContext == null) return;
            if (commandContext.StoreyContext == null) return;
            if (commandContext.range == null) return;
            if (commandContext.StoreyContext.thStoreysDatas == null) return;
            if (!ThRainSystemService.ImportElementsFromStdDwg()) return;
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                try
                {
                    DU.Dispose();
                    var range = commandContext.range;
                    var storeys = commandContext.StoreyContext.thStoreysDatas;
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
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();

                }
                finally
                {
                    DU.Dispose();
                }

            }

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
        public void CreateDrawingDatas()
        {
            //roomData ??= CollectRoomData(adb);
            TestDrawingDatasCreation(GeoData, CadDataMain, CadDatas);
        }
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
            {
                var cl = Color.FromRgb(4, 229, 230);
                foreach (var o in geoData.DLines) DU.DrawLineSegmentLazy(o).Color = cl;
            }
        }

        const double MAX_SHORTTRANSLATOR_DISTANCE = 150;

        public void CreateDrainageSystemDiagram()
        {

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
            var names = adb.ModelSpace.OfType<MText>().Where(x => x.Layer == "AI-空间名称").Select(x => new CText() { Text = x.Text, Boundary = x.Bounds.ToGRect() }).ToList();
            var f = GeoFac.CreateGeometrySelector(ranges);
            var list = new List<KeyValuePair<string, Geometry>>(names.Count);
            foreach (var name in names)
            {
                if (name.Boundary.IsValid)
                {
                    var l = f(name.Boundary.ToPolygon());
                    if (l.Count == 1)
                    {
                        list.Add(new KeyValuePair<string, Geometry>(name.Text, l[0]));
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
        public static List<Geometry> GetKitchenOnlyFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies)
        {
            //6.3.2	只负责厨房的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	FL的500范围内有厨房空间，或
            //2）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在厨房空间内
            //且以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意所有结束点不在没有名称的空间或不在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Geometry> endpoints = null;
                Geometry endpointsGeo = null;
                List<Geometry> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter().ToNTSPoint());
                }
                bool test1()
                {
                    return kitchensGeo.Intersects(GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36));
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return kitchensGeo.Intersects(endpointsGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometry(ToList(nonames, balconies)));
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
            var f = GeoFac.CreateGeometrySelector(ToList(floorDrains, washMachines));
            var hs = new HashSet<Geometry>();
            {
                var flsf = GeoFac.CreateGeometrySelector(FLs);
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
        static List<Geometry> GetEndPoints(Point start)
        {
            //以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的
            //todo
            throw new NotImplementedException();
        }
        public static List<Geometry> GetBalconyOnlyFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies)
        {
            //6.3.3	只负责阳台的FL
            //-	判断方法
            //找到所有的FL立管，若：
            //1）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。且
            //2）	FL的500范围内没有厨房空间，且
            //3）	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的所有结束点不在在厨房空间内
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Geometry> endpoints = null;
                Geometry endpointsGeo = null;
                List<Geometry> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter().ToNTSPoint());
                }
                bool test1()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometry(ToList(nonames, balconies)));
                }
                bool test2()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
                }
                bool test3()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return !kitchensGeo.Intersects(endpointsGeo);
                }
                if (test1() && test2() && test3())
                {
                    list.Add(fl);
                }
            }
            return list;
        }
        public static List<Geometry> GetKitchenAndBalconyBothFLs(List<Geometry> FLs, List<Geometry> kitchens, List<Geometry> nonames, List<Geometry> balconies)
        {
            //6.3.4	厨房阳台兼用FL
            //-	判断方法
            //1)	FL的500范围内有厨房空间，且
            //2)	以FL圆心为起点，以图层为W-DRAI-DOME-PIPE或W-DRAI-WAST-PIPE水平水管的管线结构的任意一个结束点在没有名称的空间或在阳台空间内。
            var kitchensGeo = GeoFac.CreateGeometry(kitchens);
            var list = new List<Geometry>(FLs.Count);
            foreach (var fl in FLs)
            {
                List<Geometry> endpoints = null;
                Geometry endpointsGeo = null;
                List<Geometry> _GetEndPoints()
                {
                    return GetEndPoints(fl.GetCenter().ToNTSPoint());
                }
                bool test1()
                {
                    return !GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 500, 36).Intersects(kitchensGeo);
                }
                bool test2()
                {
                    endpoints ??= _GetEndPoints();
                    endpointsGeo ??= GeoFac.CreateGeometry(endpoints);
                    return endpointsGeo.Intersects(GeoFac.CreateGeometry(ToList(nonames, balconies)));
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
        public static void DrawStoreyLine(string label, Point3d basePt, double lineLen)
        {
            {
                var line = DU.DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DU.DrawTextLazy(label, ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
            }
            if (label == "RF")
            {
                var line = DU.DrawLineLazy(new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0), new Point3d(basePt.X + lineLen, basePt.Y + ThWSDStorey.RF_OFFSET_Y, 0));
                var dbt = DU.DrawTextLazy("建筑完成面", ThWSDStorey.TEXT_HEIGHT, new Point3d(basePt.X + ThWSDStorey.INDEX_TEXT_OFFSET_X, basePt.Y + ThWSDStorey.RF_OFFSET_Y + ThWSDStorey.INDEX_TEXT_OFFSET_Y, 0));
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
                DU.DrawBlockReference("洗涤盆排水", basePt, br =>
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
                DU.DrawBlockReference("洗涤盆排水", basePt, br =>
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
                DU.DrawBlockReference("双格洗涤盆排水", basePt,
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
                DU.DrawBlockReference("双格洗涤盆排水", basePt,
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
            draw1(basePoint);
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
            public string Label1;
            public string Label2;
            public string Comment;
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

        }

        public class PipeRunLocationInfo
        {
            public Point2d BasePoint;
            public Point2d StartPoint;
            public Point2d EndPoint;
            public List<Vector2d> Vector2ds;
            public List<GLineSegment> Segs;
            public List<GLineSegment> DisplaySegs;
        }

        public static void SetLabelStylesForDraiNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-DRAI-NOTE";
                e.ColorIndex = 256;
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        private static void DrawDomePipes(params GLineSegment[] segs)
        {
            DrawDomePipes((IEnumerable<GLineSegment>)segs);
        }
        private static void DrawDomePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
        }
        private static void DrawBluePipes(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line =>
            {
                line.Layer = "W-RAIN-PIPE";
                line.ColorIndex = 256;
            });
        }
        private static void DrawDraiNoteLines(IEnumerable<GLineSegment> segs)
        {
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line =>
            {
                line.Layer = "W-DRAI-NOTE";
                line.ColorIndex = 256;
            });
        }
        private static void DrawWrappingPipe(Point2d basePt)
        {
            DrawWrappingPipe(basePt.ToPoint3d());
        }
        private static void DrawWrappingPipe(Point3d basePt)
        {
            DU.DrawBlockReference("套管系统", basePt, br =>
            {
                br.Layer = "W-BUSH";
            });
        }

        private static void DrawHorizontalLineOnPipeRun(double HEIGHT, Point3d basePt)
        {
            var h = HEIGHT * .7;
            if (HEIGHT - h > LONG_TRANSLATOR_HEIGHT1)
            {
                h = HEIGHT - LONG_TRANSLATOR_HEIGHT1 + 150;
            }
            var p1 = basePt.OffsetY(h);
            var p2 = p1.OffsetX(-200);
            var p3 = p1.OffsetX(200);
            var line = DU.DrawLineLazy(p2, p3);
            line.Layer = "W-DRAI-NOTE";
        }

        private static Point2d NewMethod5(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), false);
            return lastPt;
        }

        private static Point2d NewMethod4(double HEIGHT, ref Vector2d v, Point3d basePt)
        {
            var points = LONG_TRANSLATOR_POINTS;
            NewMethod(HEIGHT, ref v, basePt, points);
            var lastPt = points.Last();
            DrawPipeCheckPoint(basePt.OffsetXY(lastPt.X, HEIGHT - LONG_TRANSLATOR_HEIGHT1 + lastPt.Y - CHECKPOINT_OFFSET_Y), true);
            return lastPt;
        }
        public static void DrawFloorDrain(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("地漏系统", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("地漏系统", basePt,
               br =>
               {
                   br.Layer = "W-DRAI-EQPM";
                   br.ScaleFactors = new Scale3d(-2, 2, 2);
                   if (br.IsDynamicBlock)
                   {
                       br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                   }
               });
            }

        }
        public static void DrawSWaterStoringCurve(Point3d basePt, bool leftOrRight)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("S型存水弯", basePt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                    }
                });
            }
            else
            {
                DU.DrawBlockReference("S型存水弯", basePt,
                   br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                       br.ScaleFactors = new Scale3d(-2, 2, 2);
                       if (br.IsDynamicBlock)
                       {
                           br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                       }
                   });
            }
        }
        public static void DrawCleaningPort(Point3d basePt, bool leftOrRight, double scale)
        {
            if (leftOrRight)
            {
                DU.DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                });
            }
            else
            {
                DU.DrawBlockReference("清扫口系统", basePt, scale: scale, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90 + 180);
                });
            }
        }
        public static void DrawPipeCheckPoint(Point3d basePt, bool leftOrRight)
        {
            DU.DrawBlockReference(blkName: "立管检查口", basePt: basePt,
cb: br =>
{
    if (leftOrRight)
    {
        br.ScaleFactors = new Scale3d(-1, 1, 1);
    }
    br.Layer = "W-DRAI-EQPM";
});
        }

        private static void NewMethod2(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2, double height1)
        {

            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
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
        private static void NewMethod3(double HEIGHT, ref Vector2d vec, Point3d basePt, Point2d[] points1, Point2d[] points2)
        {
            var height1 = LONG_TRANSLATOR_HEIGHT1;
            var lastPt1 = points1.Last();
            var lastPt2 = points2.Last();
            {
                var segs = points1.ToGLineSegments(basePt.OffsetY(HEIGHT - height1));
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }

            {
                var p1 = basePt.OffsetXY(lastPt1.X, -lastPt2.Y);
                var p2 = p1.OffsetY(HEIGHT - height1 + lastPt1.Y + lastPt2.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height1);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
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
                var lines = DU.DrawLineSegmentsLazy(segs);
                lines.ForEach(line => SetDomePipeLineStyle(line));
            }
            {
                var v = new Vector2d(lastPt2.X, 0);
                vec += v;
                basePt += v.ToVector3d();
            }
        }
        private static void NewMethod1(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = HEIGHT + lastPt.Y;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
                v += new Vector2d(lastPt.X, 0);
            }
        }

        private static void NewMethod(double HEIGHT, ref Vector2d v, Point3d basePt, Point2d[] points)
        {
            var lastPt = points.Last();
            var height = LONG_TRANSLATOR_HEIGHT1;
            var segs = points.ToGLineSegments(basePt.OffsetY(HEIGHT - height));
            var lines = DU.DrawLineSegmentsLazy(segs);
            lines.ForEach(line => SetDomePipeLineStyle(line));

            {
                var p1 = basePt.OffsetX(points.Last().X);
                var p2 = p1.OffsetY(HEIGHT - height + lastPt.Y);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
                SetDomePipeLineStyle(line);
            }
            {
                var p1 = basePt.OffsetY(HEIGHT - height);
                var p2 = basePt.OffsetY(HEIGHT);
                var line = DU.DrawLineSegmentLazy(new GLineSegment(p1, p2));
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
            DU.DrawBlockReference(blkName: "污废合流井编号", basePt: basePt.OffsetY(-400),
            props: new Dictionary<string, string>() { { "-", value } },
            cb: br =>
            {
                br.Layer = "W-DRAI-EQPM";
            });
        }
        private static void SetVentPipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-VENT-PIPE";
            line.ColorIndex = 256;
        }

        private static void SetDomePipeLineStyle(Line line)
        {
            line.Layer = "W-DRAI-DOME-PIPE";
            line.ColorIndex = 256;
        }
    }
    public class ThDrainageService
    {

        public static void ConnectLabelToLabelLine(DrainageGeoData geoData)
        {
            var lines = geoData.LabelLines.Distinct().ToList();
            var bds = geoData.Labels.Select(x => x.Boundary).ToList();
            var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
            var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
            var f1 = GeoFac.CreateGRectContainsSelector(lineHGs);
            foreach (var bd in bds)
            {
                var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1500, 250);
                {
                    var e = DU.DrawRectLazy(g);
                    e.ColorIndex = 2;
                }
                var _lineHGs = f1(g);
                var f2 = GeoFac.NearestNeighbourGeometryF(_lineHGs);
                var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                if (geo == null) continue;
                {
                    var ents = geo.ToDbObjects().OfType<Entity>().ToList();
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
            for (int i = 0; i < geoData.DLines.Count; i++)
            {
                geoData.DLines[i] = geoData.DLines[i].Extend(5);
            }
            for (int i = 0; i < geoData.VLines.Count; i++)
            {
                geoData.VLines[i] = geoData.VLines[i].Extend(5);
            }
        }
        public static bool HasWL(string label)
        {
            //暂不支持污废分流
            return label.StartsWith("WL");
        }
        public static double GetAiringValue()
        {
            //伸顶通气的值，上人屋面伸顶2000，不上人屋面伸顶500。从面板读取。
            return GetCanPeopleBeOnRoof() ? 2000 : 500;
        }
        public static bool GetCanPeopleBeOnRoof()
        {
            return false;
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
            return label.StartsWith("FL") || label.StartsWith("PL") || label.StartsWith("TL") || label.StartsWith("DL");
        }
        public static bool IsMaybeLabelText(string label)
        {
            if (label == null) return false;
            return label.StartsWith("FL") || label.StartsWith("PL") || label.StartsWith("TL") || label.StartsWith("DL") || label.StartsWith("Y1L") || label.StartsWith("Y2L") || label.StartsWith("NL") || label.StartsWith("YL") || label.Contains("单排");
        }

        public class WLGrouper
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
                public static List<ToiletGrouper> CollectFLTLs(List<ToiletGrouper> group, List<Geometry> FLs, List<Geometry> TLs)
                {
                    //6.3.5	废水立管（FL）+通气立管（TL）
                    //若FL附近300的范围内存在TL且该THL不属于PL，则将FL与TL设为一组。系统图上和PL+TL的唯一区别是废水管要表达卫生洁具。
                    var tls = new List<Geometry>();
                    foreach (var item in group)
                    {
                        if (item.PLs.Count > 0 && item.TLs.Count > 0)
                        {
                            tls.AddRange(item.TLs);
                        }
                    }
                    var list = new List<ToiletGrouper>();
                    List<Geometry> _tls = null;
                    foreach (var fl in FLs)
                    {
                        _tls ??= TLs.Except(tls).ToList();
                        var f = GeoFac.CreateGeometrySelector(_tls);
                        var range = GeoFac.CreateCirclePolygon(fl.GetCenter().ToPoint3d(), 300, 36);
                        var lst = f(range);
                        if (lst.Count > 0)
                        {
                            tls.AddRange(lst);
                            _tls = null;
                            var item = new ToiletGrouper();
                            list.Add(item);
                            item.Init();
                            item.FLs.Add(fl);
                            item.TLs.AddRange(lst);
                        }
                    }
                    return list;
                }
                public static List<Geometry> GetWaterPipeWellFLs(List<KeyValuePair<string, Geometry>> roomData, List<Geometry> FLs)
                {
                    //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
                    //水管井的判断：
                    //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
                    var rooms = new List<Geometry>();
                    foreach (var kv in roomData)
                    {
                        if (kv.Key == "水" || kv.Key.Contains("水井") || kv.Key.Contains("水管井"))
                        {
                            rooms.Add(kv.Value);
                        }
                    }
                    return GeoFac.CreateGeometrySelector(FLs)(GeoFac.CreateGeometry(rooms));
                }
                public static List<ToiletGrouper> DoGroup(List<Geometry> PLs, List<Geometry> TLs, List<Geometry> DLs)
                {
                    var list = new List<ToiletGrouper>();
                    var hs = new HashSet<Geometry>(PLs.Concat(TLs).Concat(DLs));
                    foreach (var pl in PLs)
                    {
                        hs.Add(pl);
                        var range = GeoFac.CreateCirclePolygon(pl.GetCenter().ToPoint3d(), 300, 12);
                        //在每一根PL的每一层300范围内找TL
                        var tls = GeoFac.CreateGeometrySelector(TLs.Except(hs).ToList())(range);
                        hs.AddRange(tls);
                        //在每一根PL的每一层300范围内找DL
                        var dls = GeoFac.CreateGeometrySelector(DLs.Except(hs).ToList())(range);
                        hs.AddRange(dls);
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
                    return list;
                }
            }
            public enum WLType
            {
                PL, PL_TL, PL_DL, PL_TL_DL,
            }
        }


    }
}


