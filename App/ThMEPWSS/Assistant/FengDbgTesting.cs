//this file is for debugging only by Feng


using System;
using System.Text;
#pragma warning disable

namespace ThMEPWSS.DebugNs
{
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
    using static StaticMethods;
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
            var f = GeometryFac.CreateGeometrySelector(GetAllEntities());
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
    public class Sankaku
    {
        [Feng("🔴一些工具")]
        public static void qu0k4p()
        {
            FengDbgTest.qt8czw.AddButtons1(typeof(ThDebugClass.qu0jxf));
        }
        //        Dbg.FocusMainWindow();
        //using (Dbg.DocumentLock)
        //using (var adb = AcadDatabase.Active())
        //using (var tr = new DrawingTransaction(adb))
        //{
        //	var db = adb.Database;
        //    Dbg.BuildAndSetCurrentLayer(db);
        //}
        [Feng]
        public static void xxx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                
            }
        }
        [Feng]
        public static void yyy()
        {
            NewMethod5();
        }
        [Feng]
        public static void zzz()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var lines=Dbg.SelectEntities(adb).OfType<Line>().ToList();

            }
        }
        private static void NewMethod5()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0;
                var HEIGHT = 1800.0;
                var COUNT = 20;
                var basePt = Dbg.SelectPoint();
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePt.OffsetY(HEIGHT * i);
                    NewMethod3(storey, bsPt1, lineLen);
                    for (int j = 0; j < COUNT; j++)
                    {
                        var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);

                        if (j == 0)
                        {
                            Dr.DrawCheckPoint(bsPt2);
                        }
                        else if (j == 1)
                        {
                            DU.DrawBlockReference("清扫口系统", bsPt2.OffsetY(HEIGHT - 300), scale: 2, cb: br =>
                              {
                                  br.Layer = "W-DRAI-EQPM";
                                  br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                              });
                        }
                        else if (j == 2)
                        {
                            DU.DrawBlockReference("洗涤盆排水", bsPt2.OffsetXY(-1000, HEIGHT - 1720 - 80 + 200), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                if (br.IsDynamicBlock)
                                {
                                    br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                                }
                            });
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, 200), bsPt2.OffsetXY(0, 200));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 3)
                        {
                            Dr.DrawFloorDrain(bsPt2.OffsetX(180 - 1000));
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, -550), bsPt2.OffsetXY(0, -550));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 4)
                        {
                            DU.DrawBlockReference("阳台支管块", bsPt2.OffsetXY(-328257, 35827), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                            });
                        }
                        else if (j == 5)
                        {
                            var line = DU.DrawLineLazy(bsPt2.OffsetXY(-200, HEIGHT - 550), bsPt2.OffsetXY(200, HEIGHT - 550));
                            line.Layer = "W-DRAI-NOTE";
                            line.ColorIndex = 256;
                        }
                    }
                }
                for (int j = 0; j < COUNT; j++)
                {
                    var x = basePt.X + OFFSET_X + (j + 1) * SPAN_X;
                    var y1 = basePt.Y;
                    var y2 = y1 + HEIGHT * (storeys.Count - 1);
                    {
                        var line = DU.DrawLineLazy(x, y1, x, y2);
                        line.Layer = "W-DRAI-DOME-PIPE";
                        line.ColorIndex = 256;
                    }
                    {
                        var line = DU.DrawLineLazy(x + 300, y1, x + 300, y2);
                        line.Layer = "W-DRAI-VENT-PIPE";
                        line.ColorIndex = 256;
                    }
                }

                {
                    var bsPt = basePt.OffsetY(-1000);
                    DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-RAIN-EQPM";
                   });
                }
                {
                    var bsPt = basePt.OffsetXY(500, -1000);
                    DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                   });
                }
            }
        }

        private static void NewMethod4()
        {
            var OFFSET_X = 2500;
            var SPAN_X = 5500;
            var HEIGHT = 1800;
            var COUNT = 20;
            var basePt = Dbg.SelectPoint();
            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePt.OffsetY(HEIGHT * i);
                NewMethod3(storey, bsPt1, lineLen);
                for (int j = 0; j < COUNT; j++)
                {
                    var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);
                    Dbg.ShowXLabel(bsPt2);
                }
            }
        }

        private static void NewMethod3(string label, Point3d basePt, double lineLen)
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

        public static void jjj()
        {

        }
        //雨水斗对位研究
        private static void NewMethod2()
        {
            var r1 = "{'type':'GRect','values':[1292293.678676707,581253.83417658461,1392293.678676707,699247.960999981]}".FromCadJson<GRect>();
            var r2 = "{'type':'GRect','values':[1292293.678676707,431253.83417658461,1392293.678676707,549247.960999981]}".FromCadJson<GRect>();
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "重力流雨水斗"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "GYYR"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                //foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e=>e.Layer=="W-RAIN-EQPM"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                var pts1 = new List<Point2d>();
                var pts2 = new List<Point2d>();
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "GYYR"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r1.ContainsRect(bd))
                    {
                        pts1.Add(bd.Center);
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "重力流雨水斗"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r1.ContainsRect(bd))
                    {
                        pts1.Add(bd.Center);
                    }
                }
                //foreach (var pt in pts1)
                //{
                //    Dbg.ShowWhere(pt);
                //}

                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r2.ContainsRect(bd))
                    {
                        pts2.Add(bd.Center);
                    }
                }
                //foreach (var pt in pts2)
                //{
                //    Dbg.ShowWhere(pt);
                //}

                var p1 = r1.LeftTop;
                var p2 = r2.LeftTop;
                var pts3 = pts1.Select(p => (p - p1).ToPoint2d()).ToList();
                var pts4 = pts2.Select(p => (p - p2).ToPoint2d()).ToList();
                for (int i = 0; i < pts3.Count; i++)
                {
                    for (int j = 0; j < pts4.Count; j++)
                    {
                        if (pts3[i].GetDistanceTo(pts4[j]) < 50)
                        {
                            Dbg.ShowWhere(pts1[i]);
                            Dbg.ShowWhere(pts2[j]);
                        }
                    }
                }
            }
        }

        private static void NewMethod1()
        {
            var p1 = new Point2d(0, 10);
            var p2 = new Point2d(10, 0);
            var p3 = new Point2d(10, 10);
            var p4 = Point2d.Origin;
            var line1 = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p3.ToNTSCoordinate(), p4.ToNTSCoordinate() });
            var line2 = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p1.ToNTSCoordinate(), p2.ToNTSCoordinate() });
            var geo = line1.Intersection(line2) as Point;
            Dbg.PrintLine(geo.ToString());
        }

        private static void NewMethod()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                {
                    var p1 = new Point2d(0, 10);
                    var p2 = new Point2d(10, 0);
                    var p3 = new Point2d(20, 0);
                    var line = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p2.ToNTSCoordinate(), p3.ToNTSCoordinate() });
                    var p = p1.ToNTSPoint();
                    Dbg.PrintLine(p.Distance(line));
                }

                {
                    var p1 = new Point2d(0, 10);
                    var p2 = new Point2d(10, 0);
                    var p3 = new Point2d(-10, 0);
                    var line = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p2.ToNTSCoordinate(), p3.ToNTSCoordinate() });
                    var p = p1.ToNTSPoint();
                    Dbg.PrintLine(p.Distance(line));
                }
            }
        }

        private static void qu0lsq()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var r1 = new GRect(0, 0, 10, 10);
                //var r2 = new GRect(5, 5, 20, 20);
                //var geo = GeometryFac.CreateGeometry(new Geometry[] { r1.ToPolygon(), r2.ToPolygon() });
                //DU.DrawGeometryLazy(geo);
                //Dbg.ChangeCadScreenTo(geo.EnvelopeInternal.ToGRect());

                var r1 = new GRect(0, 0, 10, 10).ToPolygon();
                var r2 = new GRect(5, 5, 20, 20).ToPolygon();
                //var geo = r1.Union(r2);
                var geo = r1.Intersection(r2);
                //var geo = r1.Difference(r2);
                DU.DrawGeometryLazy(geo);
                //Dbg.ChangeCadScreenTo(geo.EnvelopeInternal.ToGRect());

                Dbg.FocusMainWindow();
            }
        }
    }
    public class FengDbgTesting
    {
        public static RainSystemGeoData qtdtsl()
        {
            var labelLines = new List<GLineSegment>();
            var cts = new List<CText>();
            var pipes = new List<GRect>();
            var storeys = new List<GRect>();
            var wLines = new List<GLineSegment>();
            var condensePipes = new List<GRect>();
            var floorDrains = new List<GRect>();
            var waterWells = new List<GRect>();
            var waterPortSymbols = new List<GRect>();
            var waterPort13s = new List<GRect>();
            var wrappingPipes = new List<GRect>();


            var files = Util1.getFiles();
            var file = files.First();
            var range = Util1.getRangeDict()[file].ToPoint3dCollection();

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
            {
                //Dbg.BuildAndSetCurrentLayer(adb.Database);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        else
                        {
                            yield return ent;
                        }
                    }
                }
                var entities = GetEntities().ToList();


                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Name.Contains("雨水井编号")));
                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<Circle>()
                        .Where(c => c.Layer == "W-RAIN-EQPM")
                        .Where(c => 20 < c.Radius && c.Radius < 40));
                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                }
                {
                    var ents = new List<Entity>();
                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                }
                {
                    foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                    {
                        labelLines.Add(e.ToGLineSegment());
                    }

                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                    {
                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                    }
                }

                {
                    var pps = new List<Entity>();
                    var blockNameOfVerticalPipe = "带定位立管";
                    pps.AddRange(entities.OfType<BlockReference>()
                     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                     .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                    storeysRecEngine.Recognize(adb.Database, range);
                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                    {
                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                        storeys.Add(bd);
                    }
                }

                {
                    foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                    {
                        if (e is Line line && line.Length > 0)
                        {
                            wLines.Add(line.ToGLineSegment());
                        }
                        else if (ThRainSystemService.IsTianZhengElement(e.GetType()))
                        {
                            //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                            //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                            //{
                            //    wLines.Add(ln.ToGLineSegment());
                            //}
                            foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                            {
                                if (ln.Length > 0)
                                {
                                    wLines.Add(ln.ToGLineSegment());
                                }
                            }
                        }
                    }
                }
            }
            var geoData = new RainSystemGeoData();
            geoData.Init();
            geoData.Storeys.AddRange(storeys);
            geoData.LabelLines.AddRange(labelLines);
            geoData.WLines.AddRange(wLines);
            geoData.Labels.AddRange(cts);
            geoData.VerticalPipes.AddRange(pipes);
            geoData.CondensePipes.AddRange(condensePipes);
            geoData.FloorDrains.AddRange(floorDrains);
            geoData.WaterWells.AddRange(waterWells);
            geoData.WaterPortSymbols.AddRange(waterPortSymbols);
            geoData.WaterPort13s.AddRange(waterPort13s);
            geoData.WrappingPipes.AddRange(wrappingPipes);
            //geoData.SideWaterBuckets.AddRange(xxx);
            //geoData.GravityWaterBuckets.AddRange(xxx);

            geoData.FixData();
            return geoData;
        }
        const string qtdwh3 = @"D:\DATA\temp\637571012711826922.json";
        [Feng]
        public static void qtntzx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = DU.DrawingTransaction)
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);

                //天正立管的bound是空的。。。中心点是对的
                //var e = Dbg.SelectEntity<Entity>(adb);
                ////DU.DrawBoundaryLazy(e);
                ////Dbg.ShowWhere(e);
                //Dbg.PrintLine(e.Bounds.ToGRect().Width);
                //Dbg.PrintLine(e.Bounds.ToGRect().Height);
                ////var pl=DU.DrawRectLazy(e.Bounds.ToGRect());
                ////pl.ConstantWidth = 100;
                ////Dbg.ShowWhere(pl);


            }

        }
        [Feng]
        public static void qtntzs()
        {
            Util1.qtbzkf();
        }
        [Feng("输出图纸分析结果")]
        public static void qtnu0e()
        {


            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                using (var adb = AcadDatabase.Active())
                {
                    //Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();


                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                        foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                    }

                    {
                        //!!!
                        var pps = new List<Entity>();
                        //pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                        pps.AddRange(entities.Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM") && ThRainSystemService.IsTianZhengElement(x.GetType())));
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect(50);
                        }
                        foreach (var pp in pps.Distinct())
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));
                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                DU.Dispose();
                var range = Dbg.SelectRange();
                var basePt = Dbg.SelectPoint();
                ThRainSystemService.ImportElementsFromStdDwg();
                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = getGeoData(range);
                ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                ThRainSystemService.PreFixGeoData(geoData, 150);
                geoData.FixData();
                var cadDataMain = RainSystemCadData.Create(geoData);
                var cadDatas = cadDataMain.SplitByStorey();
                var sv = new RainSystemService()
                {
                    Storeys = storeys,
                    GeoData = geoData,
                    CadDataMain = cadDataMain,
                    CadDatas = cadDatas,
                };
                sv.CreateDrawingDatas();
                if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                DU.Dispose();
            }

        }
        [Feng]
        public static void qtnu08()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Polyline>(adb);
                //Dbg.PrintLine(e.HasBulges);
                //Dbg.PrintLine(e.NumberOfVertices);//3
                //(e.ExplodeToDBObjectCollection()[0] as BlockReference).ToDataItem();
                //Dbg.PrintLine(e.ExplodeToDBObjectCollection()[0].ObjectId.ToString());
            }
        }
        public static bool IsTianZhengWaterPort(Entity e)
        {
            if ((e.Layer == "W-RAIN-EQPM" || e.Layer == "W-RAIN-NOTE" || e.Layer == "W-RAIN-DIMS") && ThRainSystemService.IsTianZhengElement(e.GetType()))
            {
                var lst = e.ExplodeToDBObjectCollection();
                if (lst.Count == 1)
                {
                    if (lst[0] is BlockReference br)
                    {
                        var lst2 = br.ExplodeToDBObjectCollection();
                        if (lst2.Count == 1)
                        {
                            if (lst2[0] is Polyline pl)
                            {
                                if (pl.HasBulges && pl.NumberOfVertices == 3)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        [Feng("💰准备打开多张图纸")]
        public static void qtjr2w()
        {
            var files = Util1.getFiles();
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                AddButton((i + 1) + " " + Path.GetFileName(file), () =>
                     {
                         Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
                     });
            }
            AddButton("地上给水排水平面图模板_20210125", () =>
            {
                var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\地上给水排水平面图模板_20210125.dwg";
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
            });
        }
        [Feng("GetLayers")]
        public static void xx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                Dbg.SetText(adb.Layers.Select(x => x.Name));
            }
        }
        [Feng("test03")]
        public static void qti0pn()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                using (var adb = AcadDatabase.Active())
                {
                    //Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();


                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        ents.AddRange(entities.Where(e => IsTianZhengWaterPort(e)));
                        waterPortSymbols.AddRange(ents.Distinct().Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                        foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                    }

                    {
                        //!!!
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<Circle>().Where(c => 40 <= c.Radius && c.Radius <= 60));
                        pps.AddRange(entities
                            .Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")
                            && ThRainSystemService.IsTianZhengElement(x.GetType()))
                             .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Any())
                            );
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect(50);
                        }
                        foreach (var pp in pps.Distinct())
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));
                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 350);
        }
        [Feng("标出所有的立管的正确boundary")]
        public static void qtntze()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                    if (c != null)
                    {
                        DU.DrawBoundaryLazy(c);
                    }
                }
            }
        }
        [Feng("试试这个自动连接在test04上的效果")]
        public static void qtp1sh()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Entity>()
                  .Where(e => e.Layer == "W-RAIN-PIPE").OfType<Line>()
                  .Select(e => e.ToGLineSegment())
                  .ToList();
                var rs = new List<GRect>();
                foreach (var e in adb.ModelSpace.OfType<Circle>().Where(e => e.Layer == "W-RAIN-DIMS"))
                {
                    var r = e.Bounds.ToGRect();
                    if (r.IsValid)
                    {
                        rs.Add(r);
                    }
                }
                foreach (var seg in GetSegsToConnect(lines, rs, 200))//这个值不要设置得太大 处理这种情况的时候会有些问题 - --
                {
                    var pl = DU.DrawLineSegmentLazy(seg);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }
        }
        [Feng]
        public static void GetColorIndex()
        {
            Dbg.FocusMainWindow();
            using (var adb = AcadDatabase.Active())
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var db = adb.Database;
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.ShowString(e.ColorIndex.ToString());
            }
        }
        [Feng("再试试...")]
        public static void qtp8xj()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Line>()
                  .Where(e => e.ColorIndex == 4)
                  .Select(e => e.ToGLineSegment())
                  .ToList();

                foreach (var seg in GetSegsToConnect(lines, null, 200, radius: 15))//这个值不要设置得太大 处理这种情况的时候会有些问题 - --
                {
                    var pl = DU.DrawLineSegmentLazy(seg, 50);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }

        }
        [Feng("vec")]
        public static void qtpe9p()
        {
            var v1 = new Point2d(1, 1) - Point2d.Origin;
            var v2 = new Point2d(-1, -1) - Point2d.Origin;
            Dbg.PrintLine(GeoAlgorithm.AngleToDegree(v1.GetAngleTo(v2)));
        }

        [Feng("搞定")]
        public static void qtpbrr()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Entity>()
                  .Where(e => e.Layer == "W-DRAI-DOME-PIPE" && ThRainSystemService.IsTianZhengElement(e.GetType()))
                  .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                  .Select(e => e.ToGLineSegment())
                  .ToList();
                var rs = new List<GRect>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                    if (c != null)
                    {
                        //DU.DrawBoundaryLazy(c);
                        var r = c.Bounds.ToGRect();
                        if (r.IsValid)
                        {
                            rs.Add(r);
                        }
                    }
                }
                foreach (var seg in GetSegsToConnect(lines, rs, 8000))
                {
                    var pl = DU.DrawLineSegmentLazy(seg, 50);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }

        }
        [Feng("定版吧")]
        public static void qtn8ij()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var lines = adb.ModelSpace.OfType<Entity>()
                  .Where(e => e.Layer == "W-RAIN-PIPE" && ThRainSystemService.IsTianZhengElement(e.GetType()))
                  .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                  .Select(e => e.ToGLineSegment())
                  .ToList();
                var rs = new List<GRect>();
                foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                    if (c != null)
                    {
                        //DU.DrawBoundaryLazy(c);
                        var r = c.Bounds.ToGRect();
                        if (r.IsValid)
                        {
                            rs.Add(r);
                        }
                    }
                }
                foreach (var seg in GetSegsToConnect(lines, rs, 8000))//后面排除下一些值，中间不要穿过别的东西
                {
                    var pl = DU.DrawLineSegmentLazy(seg, 100);
                    //pl.ConstantWidth = 50;
                    //Dbg.ShowWhere(seg.StartPoint);
                }
            }

        }
        static bool IsContrary(double v1, double v2)
        {
            return v1 < 0 && v2 > 0 || v1 > 0 && v2 < 0;
        }
        //以后有需要再写个支持任意角度的，因为现在没有测试数据，先不深入了
        //使用的时候最好再对结果extend .1
        public static IEnumerable<GLineSegment> GetSegsToConnect(List<GLineSegment> lines, List<GRect> rects, double extends, double bufSize = 2.5, double radius = 10)
        {
            if (extends <= 0) throw new ArgumentException();
            var h = GeometryFac.LineGrouppingHelper.Create(lines);
            h.InitPointGeos(radius);
            h.DoGroupingByPoint();
            h.CalcAlonePoints();
            h.DistinguishAloneRings();
            if (rects != null) h.KillAloneRings(rects.Where(r => r.IsValid).Distinct().Select(r => r.ToPolygon()).ToArray());

            //foreach (var r in h.YieldAloneRings())
            //{
            //    DU.DrawEntitiesLazy(r.ToDbObjects().OfType<Entity>().ToArray());
            //}
            var extSegs = h.GetExtendedGLineSegmentsByFlags(extends).ToList();
            {
                var newSegs = extSegs.Where(seg => seg.IsHorizontal(5)).Distinct().ToList();
                var geos = newSegs.Select(seg => seg.Buffer(bufSize)).ToList();

                foreach (var g in GeometryFac.GroupGeometries(geos).Where(g => g.Count == 2))
                {
                    var geo1 = g[0];
                    var geo2 = g[1];
                    var seg1 = newSegs[geos.IndexOf(geo1)];
                    var seg2 = newSegs[geos.IndexOf(geo2)];
                    //if (!IsContrary(seg1.StartPoint.X - seg1.EndPoint.X, seg2.StartPoint.X - seg2.EndPoint.X)) continue;

                    //var xArr = new double[] { seg1.StartPoint.X, seg1.EndPoint.X, seg2.StartPoint.X, seg2.EndPoint.X };
                    //var minX = xArr.Min();
                    //var maxX = xArr.Max();
                    //yield return new GLineSegment(minX, seg1.Y1, maxX, seg1.Y1);

                    //var kv = GetMiddleValue(seg1.StartPoint.X, seg1.EndPoint.X, seg2.StartPoint.X, seg2.EndPoint.X);
                    //yield return new GLineSegment(kv.Key, seg1.Y1, kv.Value, seg1.Y1);

                    //DU.DrawLineSegmentBufferLazy(seg1, 10);
                    //DU.DrawLineSegmentBufferLazy(seg2, 10);

                    var x1 = seg1.StartPoint.X;
                    var x2 = seg1.EndPoint.X;
                    var x3 = seg2.StartPoint.X;
                    var x4 = seg2.EndPoint.X;
                    if (!IsContrary(x1 - x2, x3 - x4)) continue;
                    yield return new GLineSegment(x1, seg1.Y1, x3, seg1.Y1);
                }
            }
            {
                var newSegs = extSegs.Where(seg => seg.IsVertical(5)).Distinct().ToList();
                var geos = newSegs.Select(seg => seg.Buffer(bufSize)).ToList();

                var gs = GeometryFac.GroupGeometries(geos).Where(g => g.Count == 2).ToList();
                //foreach (var g in gs)
                //{
                //    //if (g.Count > 2)
                //    {
                //        DU.DrawEntitiesLazy(g.SelectMany(x => x.ToDbObjects().OfType<Entity>()).ToArray());
                //    }
                //}
                foreach (var g in gs)
                {
                    var geo1 = g[0];
                    var geo2 = g[1];
                    var seg1 = newSegs[geos.IndexOf(geo1)];
                    var seg2 = newSegs[geos.IndexOf(geo2)];

                    //DU.DrawLineSegmentBufferLazy(seg1, 10);
                    //DU.DrawLineSegmentBufferLazy(seg2, 10);

                    //if (!IsContrary(seg1.StartPoint.Y - seg1.EndPoint.Y, seg2.StartPoint.Y - seg2.EndPoint.Y)) continue;

                    //var yArr = new double[] { seg1.StartPoint.Y, seg1.EndPoint.Y, seg2.StartPoint.Y, seg2.EndPoint.Y };
                    //var minY = yArr.Min();
                    //var maxY = yArr.Max();
                    //yield return new GLineSegment(seg1.X1, minY, seg1.X1, maxY);

                    //var kv = GetMiddleValue(seg1.StartPoint.Y, seg1.EndPoint.Y, seg2.StartPoint.Y, seg2.EndPoint.Y);
                    //yield return new GLineSegment(seg1.X1, kv.Key, seg1.X1, kv.Value);

                    //DU.DrawLineSegmentBufferLazy(seg1, 10);
                    //DU.DrawLineSegmentBufferLazy(seg2, 10);

                    var y1 = seg1.StartPoint.Y;
                    var y2 = seg1.EndPoint.Y;
                    var y3 = seg2.StartPoint.Y;
                    var y4 = seg2.EndPoint.Y;
                    if (!IsContrary(y1 - y2, y3 - y4)) continue;
                    yield return new GLineSegment(seg1.X1, y1, seg1.X1, y3);
                }
            }
        }
        static void Sort(ref double v1, ref double v2)
        {
            if (v1 > v2)
            {
                Swap(ref v1, ref v2);
            }
        }

        private static void Swap(ref double v1, ref double v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }


        static KeyValuePair<double, double> GetMiddleValue(double v1, double v2, double v3, double v4)
        {
            var lst = new List<double>() { v1, v2, v3, v4 };
            var min = lst.Min();
            var max = lst.Max();
            lst.Remove(min);
            lst.Remove(max);
            min = lst.Min();
            max = lst.Max();
            return new KeyValuePair<double, double>(min, max);
        }
        [Feng("再试试")]
        public static void qtn8ip()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var lines = adb.ModelSpace.OfType<Entity>()
                   .Where(e => e.Layer == "W-RAIN-PIPE" && ThRainSystemService.IsTianZhengElement(e.GetType()))
                   .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                   .ToList();
                var res = ThMEPEngineCore.Algorithm.ThLineMerger.Merge(lines);
                foreach (var line in res)
                {
                    //em...
                    DU.DrawLineSegmentLazy(line.ToGLineSegment());
                }
            }
        }
        [Feng("继续测试")]
        public static void qtn8it()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var segs = adb.ModelSpace.OfType<Entity>()
                    .Where(e => e.Layer == "W-RAIN-PIPE" && ThRainSystemService.IsTianZhengElement(e.GetType()))
                    .SelectMany(e => e.ExplodeToDBObjectCollection().OfType<Line>())
                    .Select(e => e.ToGLineSegment()).ToList();
                var h = GeometryFac.LineGrouppingHelper.Create(segs);
                h.InitPointGeos(10);
                h.DoGroupingByPoint();
                h.CalcAlonePoints();
                h.DistinguishAloneRings();
                {
                    var rs = new List<GRect>();
                    foreach (var br in adb.ModelSpace.OfType<BlockReference>().Where(e => e.Layer == "W-RAIN-EQPM"))
                    {
                        var c = Util1.YieldVisibleEntities(adb, br).OfType<Circle>().FirstOrDefault();
                        if (c != null)
                        {
                            //DU.DrawBoundaryLazy(c);
                            var r = c.Bounds.ToGRect();
                            if (r.IsValid)
                            {
                                rs.Add(r);
                            }
                        }
                    }
                    h.KillAloneRings(rs.Distinct().Select(r => r.ToPolygon()).ToArray());

                }
                Dbg.PrintLine(h.GetAloneRingsCount());
                var lst = new List<GLineSegment>();
                foreach (var seg in h.GetExtendedGLineSegmentsByFlags(200.1))
                {
                    if (!seg.IsHorizontal(5)) continue;
                    //DU.DrawPolyLineLazy(seg);
                    lst.Add(seg);
                }
                {
                    lst = lst.Distinct().ToList();
                    Dbg.PrintLine(lst.Distinct().Count() == lst.Count);
                    var geos = lst.Select(seg => seg.Buffer(2.5)).ToList();
                    //Dbg.PrintLine(lst.Count);
                    //Dbg.PrintLine(geos.Count);
                    foreach (var geo in geos)
                    {
                        //Dbg.PrintLine(geo.ToDbObjects().Count);
                        foreach (var pl in geo.ToDbObjects().OfType<Polyline>())
                        {
                            //DU.DrawEntityLazy(pl);
                        }
                    }

                    {
                        var gs = ThRainSystemService.GroupPolylines(geos.SelectMany(geo => geo.ToDbObjects().OfType<Polyline>()).ToList()).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            //DU.DrawEntitiesLazy(g);
                        }
                        var rs = geos.Select(x => x.EnvelopeInternal.ToGRect()).ToList();
                        foreach (var r in rs)
                        {
                            //DU.DrawRectLazy(r);
                        }

                    }
                    //var gs = GeometryFac.GroupGeometries(geos);
                    {
                        Dbg.PrintLine(geos.Distinct().Count() == geos.Count);
                        //for (int i = 0; i < geos.Count; i++)
                        //{
                        //    for (int j = i + 1; j < geos.Count; j++)
                        //    {
                        //        var o1 = geos[i];
                        //        var o2 = geos[j];
                        //        var m = geos.IndexOf(o1);
                        //        var n = geos.IndexOf(o2);
                        //        if (m != i || n != j)
                        //        {
                        //            Dbg.PrintLine($"{i} {j } {m} {n}");
                        //        }
                        //    }
                        //}
                        //ThRainSystemService.Triangle(geos, (o1, o2) =>
                        //{
                        //    if (o1.ToIPreparedGeometry().Intersects(o2))
                        //    {
                        //        Dbg.ShowWhere(o2.ToGRect());
                        //        var i = geos.IndexOf(o1);
                        //        var j = geos.IndexOf(o2);
                        //        Dbg.PrintLine($"{} {}");
                        //    }
                        //});
                    }
                    {
                        var gs = GeometryFac.GroupGeometries(geos).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            var geo1 = g[0];
                            var geo2 = g[1];
                            var seg1 = lst[geos.IndexOf(geo1)];
                            var seg2 = lst[geos.IndexOf(geo2)];

                            //DU.DrawPolyLineLazy(seg1);
                            //DU.DrawPolyLineLazy(seg2);

                            //Dbg.ShowWhere(GRect.Create(seg1.StartPoint,.1));
                            //Dbg.ShowWhere(GRect.Create(seg2.StartPoint, .1));
                            //Dbg.ShowWhere(seg1.StartPoint);

                            //var l = new List<Geometry>() { seg1.ToLineString(), seg2.ToLineString() };
                            //var ret=LineMerge(l);
                            //foreach (var pl in ret.Select(x => x.ToDbObjects()).OfType<Entity>())
                            //{
                            //    DU.DrawEntityLazy(pl);
                            //}

                            var objs = new DBObjectCollection() { seg1.ToCadLine(), seg2.ToCadLine() };
                            objs = objs.LineMerge();
                            //foreach (var x in objs.OfType<Entity>())
                            //{
                            //    DU.DrawEntityLazy(x);
                            //}
                            foreach (var pl in objs.OfType<Polyline>())
                            {
                                //var lines = pl.ExplodeToDBObjectCollection().OfType<Line>();
                                //foreach (var line in lines)
                                //{
                                //    DU.DrawEntityLazy(line);
                                //}
                                var res = ThMEPEngineCore.Algorithm.ThLineMerger.Merge(pl.ExplodeToDBObjectCollection().OfType<Line>().ToList());
                            }

                        }
                    }
                }
            }
        }
        public static IList<Geometry> LineMerge(IEnumerable<Geometry> geos)
        {
            var merger = new LineMerger();
            merger.Add(geos);
            return merger.GetMergedLineStrings();
        }
        [Feng("test01")]
        public static void qtjrmd()
        {
            //这版对标准图纸是OK的
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                using (var adb = AcadDatabase.Active())
                {
                    //Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();


                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                    }
                    {
                        foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        var blockNameOfVerticalPipe = "带定位立管";
                        pps.AddRange(entities.OfType<BlockReference>()
                         .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                         .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    {
                        foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                        {
                            if (e is Line line && line.Length > 0)
                            {
                                wLines.Add(line.ToGLineSegment());
                            }
                            else if (ThRainSystemService.IsTianZhengElement(e.GetType()))
                            {
                                //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                                //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                                //{
                                //    wLines.Add(ln.ToGLineSegment());
                                //}
                                foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                                {
                                    if (ln.Length > 0)
                                    {
                                        wLines.Add(ln.ToGLineSegment());
                                    }
                                }
                            }
                        }
                    }
                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 150);
        }
        //粗看OK
        [Feng("test02")]
        public static void qtlabu()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();
                var waterWellDNs = new List<string>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
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
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        foreach (var ent in entities.OfType<Entity>().Where(x => x.Layer == "W-RAIN-DIMS" && ThRainSystemService.IsTianZhengElement(x.GetType())))
                        {
                            foreach (var br in ent.ExplodeToDBObjectCollection().OfType<BlockReference>())
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Polyline>().Where(x => x.Layer == "0"))
                                {
                                    ents.Add(e);
                                }
                            }
                        }
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                        foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-DIMS" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                        Util1.CollectTianzhengVerticalPipes(labelLines, cts, entities);
                    }

                    {
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<BlockReference>()
                         //.Where(x => x.Layer == "W-RAIN-EQPM")
                         //.Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == "$LIGUAN")//图块炸开的时候就失效了
                         .Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName == "$LIGUAN" : x.Layer == "W-RAIN-EQPM")
                         );
                        foreach (var pp in pps)
                        {
                            pipes.Add(GRect.Create(pp.Bounds.ToGRect().Center, 55));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

                }

                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            var files = Util1.getFiles();
            qtjrlj(getGeoData, 150);
        }

        public static void qtm55x()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                //var e = Dbg.SelectEntity<Line>(adb);
                //var si = new ThCADCoreNTSSpatialIndex(new DBObjectCollection() { e });
                //var ret = si.SelectCrossingPolygon(Dbg.SelectRange().ToSRect().ToGRect().ToCadPolyline());
                //foreach (var x in ret.OfType<Entity>())
                //{
                //    Dbg.ShowWhere(x);
                //}

                //var line1=Dbg.SelectEntity<Line>(adb);
                //var line2 = Dbg.SelectEntity<Line>(adb);
                //Dbg.PrintLine(line1.ToGLineSegment().Buffer(100).ToIPreparedGeometry().Intersects(line2.ToGLineSegment().Buffer(100)));
                //var lst = new List<Geometry>() { line1.ToGLineSegment().Buffer(100), line2.ToGLineSegment().Buffer(100) };
                //Dbg.PrintLine(GeometryFac.GroupGeometries(lst)[0].Count);
            }
            //var g1 = GRect.Create(Point3d.Origin, 100).ToLinearRing();
            //var g2 = GRect.Create(Point3d.Origin, 100).OffsetXY(10,0).ToLinearRing();
            //Dbg.PrintText(g1.ToIPreparedGeometry().Intersects(g2).ToString());
            //var g1 = new Polygon(GRect.Create(Point3d.Origin, 100).ToLinearRing());
            //var g2 = new Polygon(GRect.Create(Point3d.Origin, 100).OffsetXY(10, 0).ToLinearRing());
            //Dbg.PrintText(g1.ToIPreparedGeometry().Intersects(g2).ToString());

        }

        public static void qtm55s()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var segs = adb.ModelSpace.OfType<Line>().Where(e => e.Layer == "W-RAIN-PIPE").Select(e => e.ToGLineSegment()).ToList();
                var h = GeometryFac.LineGrouppingHelper.Create(segs);
                h.InitPointGeos(10);
                h.DoGroupingByPoint();
                h.CalcAlonePoints();
                h.DistinguishAloneRings();
                var lst = new List<GLineSegment>();
                foreach (var seg in h.GetExtendedGLineSegmentsByFlags(200.1))
                {
                    if (!seg.IsHorizontal(5)) continue;
                    //DU.DrawPolyLineLazy(seg);
                    lst.Add(seg);
                }
                {
                    lst = lst.Distinct().ToList();
                    Dbg.PrintLine(lst.Distinct().Count() == lst.Count);
                    var geos = lst.Select(seg => seg.Buffer(2.5)).ToList();
                    //Dbg.PrintLine(lst.Count);
                    //Dbg.PrintLine(geos.Count);
                    foreach (var geo in geos)
                    {
                        //Dbg.PrintLine(geo.ToDbObjects().Count);
                        foreach (var pl in geo.ToDbObjects().OfType<Polyline>())
                        {
                            //DU.DrawEntityLazy(pl);
                        }
                    }

                    {
                        var gs = ThRainSystemService.GroupPolylines(geos.SelectMany(geo => geo.ToDbObjects().OfType<Polyline>()).ToList()).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            //DU.DrawEntitiesLazy(g);
                        }
                        var rs = geos.Select(x => x.EnvelopeInternal.ToGRect()).ToList();
                        foreach (var r in rs)
                        {
                            //DU.DrawRectLazy(r);
                        }

                    }
                    //var gs = GeometryFac.GroupGeometries(geos);
                    {
                        Dbg.PrintLine(geos.Distinct().Count() == geos.Count);
                        //for (int i = 0; i < geos.Count; i++)
                        //{
                        //    for (int j = i + 1; j < geos.Count; j++)
                        //    {
                        //        var o1 = geos[i];
                        //        var o2 = geos[j];
                        //        var m = geos.IndexOf(o1);
                        //        var n = geos.IndexOf(o2);
                        //        if (m != i || n != j)
                        //        {
                        //            Dbg.PrintLine($"{i} {j } {m} {n}");
                        //        }
                        //    }
                        //}
                        //ThRainSystemService.Triangle(geos, (o1, o2) =>
                        //{
                        //    if (o1.ToIPreparedGeometry().Intersects(o2))
                        //    {
                        //        Dbg.ShowWhere(o2.ToGRect());
                        //        var i = geos.IndexOf(o1);
                        //        var j = geos.IndexOf(o2);
                        //        Dbg.PrintLine($"{} {}");
                        //    }
                        //});
                    }
                    {
                        var gs = GeometryFac.GroupGeometries(geos).Where(g => g.Count == 2).ToList();
                        foreach (var g in gs)
                        {
                            var geo1 = g[0];
                            var geo2 = g[1];
                            var seg1 = lst[geos.IndexOf(geo1)];
                            var seg2 = lst[geos.IndexOf(geo2)];
                            DU.DrawPolyLineLazy(seg1);
                            DU.DrawPolyLineLazy(seg2);
                            //Dbg.ShowWhere(GRect.Create(seg1.StartPoint,.1));
                            //Dbg.ShowWhere(GRect.Create(seg2.StartPoint, .1));
                        }
                    }
                }
            }
        }
        [Feng("DrawWrappingPipe")]
        public static void qtnu0v()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                Dr.DrawWrappingPipe(Dbg.SelectPoint());


            }

        }
        [Feng]
        public static void qtsmf9()
        {
            var file = @"E:\thepa_workingSpace\210525\test01.txt";
            var drs1 = File.ReadAllText(file).FromCadJson<List<RainSystemDrawingData>>();
            var file2 = @"E:\thepa_workingSpace\210526\新建文本文档.txt";
            var drs2 = File.ReadAllText(file2).FromCadJson<List<RainSystemDrawingData>>();
            var sb = new StringBuilder(4096);
            for (int i = 0; i < drs1.Count; i++)
            {
                var d1 = drs1[i];
                var d2 = drs2[i];
                qtpp9s(sb, d1, d2);
            }
            Dbg.PrintText(sb.ToString());
        }
        static string newFileFn()
        {
            var file = $@"Y:\{DateTime.Now.Ticks}.json";
            return file;
        }
        public static List<RainSystemGeoData> LoadWaterBucketGeoDataList()
        {
            var file = @"D:\DATA\temp\637577092816488915.json";
            var list = File.ReadAllText(file).FromCadJson<List<RainSystemGeoData>>();
            return list;
        }
        [Feng("序列化所有图纸的雨水斗位置信息")]
        public static void qu0j7u()
        {
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<RainSystemGeoData>();
            using (Dbg.DocumentLock)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    var range = d[file].ToPoint3dCollection();
                    using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                    {
                        var geoData = new RainSystemGeoData();
                        geoData.Init();
                        ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        list.Add(geoData);
                    }
                }
            }
            {
                var file = newFileFn();
                File.WriteAllText(file, list.ToCadJson());
            }
        }
        public static List<List<RainSystemDrawingData>> LoadStdDrawingDatas()
        {
            var file = @"D:\DATA\temp\637577104526532228.json";
            var list = File.ReadAllText(file).FromCadJson<List<List<RainSystemDrawingData>>>();
            return list;
        }
        public class BenchMark : IDisposable
        {
            Stopwatch sw = new Stopwatch();
            public BenchMark()
            {
                sw.Start();
            }
            public void Dispose()
            {
                sw.Stop();
                Dbg.PrintLine(sw.Elapsed.TotalSeconds);
            }
        }
        [Feng("找东西")]
        public static void llllll()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                IEnumerable<Entity> GetEntities()
                {
                    //return adb.ModelSpace.OfType<Entity>();
                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                    {
                        if (ent is BlockReference br && br.Layer == "0")
                        {
                            var r = br.Bounds.ToGRect();
                            if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                            {
                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                {
                                    yield return e;
                                }
                            }
                        }
                        else
                        {
                            yield return ent;
                        }
                    }
                }
                var entities = GetEntities().ToList();


                //var pps = new List<Entity>();
                //pps.AddRange(entities.OfType<Circle>().Where(c => 40 <= c.Radius && c.Radius <= 60));
                //pps.AddRange(entities
                //    .Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")
                //    && ThRainSystemService.IsTianZhengElement(x.GetType()))
                //     .Where(x => x.ExplodeToDBObjectCollection().OfType<Circle>().Any())
                //    );
                //Dbg.PrintLine(pps.Count);
                //static GRect getRealBoundaryForPipe(Entity ent)
                //{
                //    return ent.Bounds.ToGRect(50);
                //}
                //foreach (var pp in pps.Distinct())
                //{
                //    Dbg.ShowWhere(pp);
                //}
            }
        }
        [Feng]
        public static void ZoomAll()
        {
            Dbg.ZoomAll();
        }



        private static void NewMethod2(int i)
        {
            var stdDrawingDatas = LoadStdDrawingDatas();

            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            using (Dbg.DocumentLock)
            {
                var getGeoData = fs[i];
                if (getGeoData == null)
                {
                    return;
                }
                var file = files[i];
                var range = d[file].ToPoint3dCollection();
                using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                {
                    var geoData = buckets[i];
                    //var labelHeight = hs[i];
                    var labelHeight = 250;
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);

                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    {
                        var bk = buckets[i];
                        geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                        geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                    }
                    geoData.FixData();
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    //DU.Dispose();
                    {
                        using (var _adb = AcadDatabase.Active())
                        {
                            Dbg.BuildAndSetCurrentLayer(_adb.Database);
                        }
                    }
                    DU.Draw();

                    var drDatas = sv.DrawingDatas;

                }
            }
        }
        [Feng]
        public static void temptest()
        {
            //ThRainSystemService.ImportElementsFromStdDwg();
            //using (var adb = AcadDatabase.Active())
            //{
            //    Dbg.PrintText(adb.TextStyles.Names().ToJson());
            //}

            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var ents = Dbg.SelectEntities(adb).OfType<Entity>().ToList();
                foreach (var e in ents)
                {
                    Dbg.PrintLine(e.GetType().FullName);
                }
            }
        }
        //dim示例
        private static void NewMethod4()
        {
            Dbg.FocusMainWindow();
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            using (var tr = DrawUtils.DrawingTransaction)
            {
                var pt1 = Dbg.SelectPoint();
                var pt2 = pt1.OffsetY(99.9);

                //不传string的话就自动跟踪，注意默认的字体超小的。。。
                //var dim = new AlignedDimension(pt1,pt2,GeTools.MidPoint(pt1,pt2).OffsetX(1000),null, adb.Database.StandardDimStyle());

                var dim = new AlignedDimension();
                dim.XLine1Point = pt1;
                dim.XLine2Point = pt2;
                //dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).PolarPoint(Math.PI / 2, 1000);
                dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(20);
                dim.DimensionText = "<>";//也可以这样
                dim.ColorIndex = 4;

                DU.DrawEntityLazy(dim);
                Dbg.ChangeCadScreenTo(dim.Bounds.ToGRect());

            }
        }

        //天正信息提取
        private static void NewMethod3()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                if (false) Dbg.PrintLine(System.ComponentModel.TypeDescriptor.GetClassName(e.AcadObject));
                var dict = new Dictionary<string, object>();
                foreach (System.ComponentModel.PropertyDescriptor pi in System.ComponentModel.TypeDescriptor.GetProperties(e.AcadObject))
                {
                    var t = pi.PropertyType;
                    if (t == typeof(string) || t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(long) || t == typeof(Point2d) || t == typeof(Point3d))
                    {
                        var v = pi.GetValue(e.AcadObject);
                        dict[pi.Name] = v;
                    }
                }
                Dbg.PrintText(dict.ToCadJson());
            }
        }

        [Feng("什么情况")]
        public static void qtt4vv()
        {
            var testing = new HashSet<int>() { 0, 2, };

            var stdDrawingDatas = LoadStdDrawingDatas();

            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            using (Dbg.DocumentLock)
            {
                var i = 2;
                var getGeoData = fs[i];
                if (getGeoData == null)
                {
                    return;
                }
                var file = files[i];
                var range = d[file].ToPoint3dCollection();
                using (var adb = AcadDatabase.Active())
                {
                    var geoData = buckets[i];
                    var labelHeight = hs[i];
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);

                    DU.Dispose();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    {
                        var bk = buckets[i];
                        geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                        geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                    }
                    geoData.FixData();
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    DU.Dispose();
                    var drDatas = sv.DrawingDatas;
                    Dbg.PrintText(drDatas.ToCadJson());
                }
            }
        }
        [Feng("试试投入使用")]
        public static void qtt4w3()
        {
            try
            {
                //var testing = new HashSet<int>() { 0,1,2,3,4,5 };
                var testing = new HashSet<int>() { 0, 1, 2, 3, 4, 5, 9, 10, 11 };
                //var testing = new HashSet<int>() { 0 };
                //var testing = new HashSet<int>() { 4 };
                //var testing = new HashSet<int>() { 5 };
                //var testing = new HashSet<int>() { 9 };
                //var testing = new HashSet<int>() { 10 };
                //var testing = new HashSet<int>() { 11 };

                var stdDrawingDatas = LoadStdDrawingDatas();

                var buckets = LoadWaterBucketGeoDataList();
                var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
                var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
                var files = Util1.getFiles();
                var d = Util1.getRangeDict();
                var list = new List<List<RainSystemDrawingData>>();

                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        var getGeoData = fs[i];
                        if (getGeoData == null || !testing.Contains(i))
                        {
                            list.Add(null);
                            continue;
                        }

                        var file = files[i];
                        var range = d[file].ToPoint3dCollection();
                        using (Dbg.DocumentLock)
                        using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                        {
                            DU.Dispose();
                            var geoData = buckets[i];
                            var labelHeight = hs[i];
                            var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                            cl.CollectEntities();
                            cl.CollectLabelLines();
                            cl.CollectCTexts();
                            cl.CollectVerticalPipes();
                            cl.CollectWLines();
                            cl.CollectCondensePipes();
                            cl.CollectFloorDrains();
                            cl.CollectWaterWells();
                            cl.CollectWaterPortSymbols();
                            cl.CollectWaterPort13s();
                            cl.CollectWrappingPipes();
                            cl.CollectStoreys(range);


                            var storeys = ThRainSystemService.GetStoreys(range, adb);
                            {
                                var bk = buckets[i];
                                geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                                geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                            }
                            geoData.FixData();
                            ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                            geoData.FixData();
                            var cadDataMain = RainSystemCadData.Create(geoData);
                            var cadDatas = cadDataMain.SplitByStorey();


                            {
                                DU.Dispose();
                                var disList = new List<double>();
                                foreach (var data in cadDatas)
                                {
                                    var lines = data.LabelLines.Select(pl => geoData.LabelLines[cadDataMain.LabelLines.IndexOf(pl)]).Distinct().ToList();
                                    var bds = data.Labels.Select(pl => geoData.Labels[cadDataMain.Labels.IndexOf(pl)]).Select(x => x.Boundary).ToList();
                                    var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
                                    var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                                    var f1 = GeometryFac.CreateGRectContainsSelector(lineHGs);
                                    foreach (var bd in bds)
                                    {
                                        var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1000, 250);
                                        var _lineHGs = f1(g);
                                        var f2 = GeometryFac.NearestNeighbourGeometryF(_lineHGs);
                                        var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                                        var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                                        if (geo == null) continue;
                                        {
                                            var line = lineHs[lineHGs.IndexOf(geo)];
                                            var dis = line.Center.GetDistanceTo(bd.Center);
                                            if (100 <= dis && dis <= 400) disList.Add(dis);
                                        }
                                    }
                                }
                                DU.Dispose();
                                Dbg.PrintLine($"avg:{disList.Average():F0} min:{disList.Min():F0} max:{disList.Max():F0}");
                                string xx(int v)
                                {
                                    return $"{disList.Where(dis => dis > v).Count() * 100.0 / disList.Count:F2}%";
                                }
                                Dbg.PrintLine($"test:150,{xx(150)} 200,{xx(200)} 250,{xx(250)} 300,{xx(300)} 350,{xx(350)} {disList.Count}");
                            }

                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Dbg.PrintText(ex.Message);
                Dbg.PrintText(ex.StackTrace);
            }
        }

        [Feng("😎尝试自动计算高度")]
        public static void qtsmrd()
        {
            var testing = new HashSet<int>() { 1, };

            var stdDrawingDatas = LoadStdDrawingDatas();

            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            for (int i = 0; i < files.Length; i++)
            {
                var getGeoData = fs[i];
                if (getGeoData == null || !testing.Contains(i))
                {
                    list.Add(null);
                    continue;
                }
                var file = files[i];
                var range = d[file].ToPoint3dCollection();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                {
                    var geoData = buckets[i];
                    var labelHeight = hs[i];
                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    Dbg.PrintText(geoData.ToCadJson());

                    var lines = geoData.LabelLines.Distinct().ToList();
                    var bds = geoData.Labels.Select(x => x.Boundary).ToList();
                    var lineHs = lines.Where(x => x.IsHorizontal(10)).ToList();
                    var lineHGs = lineHs.Select(x => x.ToLineString()).Cast<Geometry>().ToList();
                    var f1 = GeometryFac.CreateGRectContainsSelector(lineHGs);
                    DU.Dispose();
                    var disList = new List<double>();
                    foreach (var bd in bds)
                    {
                        var g = GRect.Create(bd.Center.OffsetY(-10).OffsetY(-250), 1000, 250);
                        {
                            var e = DU.DrawRectLazy(g);
                            e.ColorIndex = 2;
                        }
                        var _lineHGs = f1(g);
                        var f2 = GeometryFac.NearestNeighbourGeometryF(_lineHGs);
                        var lineH = lineHGs.Select(lineHG => lineHs[lineHGs.IndexOf(lineHG)]).ToList();
                        var geo = f2(bd.Center.Expand(.1).ToGRect().ToPolygon());
                        if (geo == null) continue;
                        {
                            var ents = geo.ToDbObjects().OfType<Entity>().ToList();
                            //DU.DrawEntitiesLazy(ents);
                            //ents.ForEach(e => e.ColorIndex = 3);
                            var line = lineHs[lineHGs.IndexOf(geo)];
                            //{
                            //    var e = DU.DrawLineSegmentLazy(line);
                            //    e.ColorIndex = 3;
                            //}
                            //{
                            //    var seg = new GLineSegment(line.Center, bd.Center);
                            //    var e = DU.DrawLineSegmentLazy(seg);
                            //    e.ColorIndex = 3;
                            //}
                            //Dbg.PrintLine(line.GetDistanceTo(bd.Center));
                            var dis = line.Center.GetDistanceTo(bd.Center);
                            //Dbg.PrintLine(dis);
                            if (100 <= dis && dis <= 400) disList.Add(dis);
                        }
                        //if (geo != null)
                        //{
                        //    var line=lineHs[lineHGs.IndexOf(geo)];
                        //    Dbg.PrintLine(line.GetDistanceTo(bd.Center));
                        //}
                        DU.DrawRectLazy(bd);
                    }
                    foreach (var line in lineHs)
                    {
                        DU.DrawLineSegmentLazy(line);
                    }
                    Dbg.PrintLine("avg:" + disList.Average());
                    DU.Draw();
                    DU.Dispose();
                }
            }
        }

        [Feng("😋画单张图纸的骨架")]
        public static void qtsmf1()
        {
            var i = 2;
            NewMethod2(i);
        }
        [Feng("准备画骨架")]
        public static void qu0jtu()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            {
                try
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);
                    DU.Dispose();
                    var range = Dbg.SelectRange();
                    //var basePt = Dbg.SelectPoint();
                    ThRainSystemService.ImportElementsFromStdDwg();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = new RainSystemGeoData();
                    geoData.Init();

                    var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                    cl.CollectEntities();
                    cl.CollectLabelLines();
                    cl.CollectCTexts();
                    cl.CollectVerticalPipes();
                    cl.CollectWLines();
                    cl.CollectCondensePipes();
                    cl.CollectFloorDrains();
                    cl.CollectWaterWells();
                    cl.CollectWaterPortSymbols();
                    cl.CollectWaterPort13s();
                    cl.CollectWrappingPipes();
                    cl.CollectStoreys(range);

                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, -1);
                    ThRainSystemService.ConnectLabelToLabelLine(geoData);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };

                    AddButton("画骨架", () =>
                    {
                        DU.Dispose();
                        sv.CreateDrawingDatas();
                        using (Dbg.DocumentLock)
                        {
                            DU.Draw();
                        }
                    });

                    //if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                    //DU.Dispose();
                    //sv.RainSystemDiagram.Draw(basePt);
                    //DU.Draw(adb);
                }
                //catch (System.Exception ex)
                //{
                //    MessageBox.Show(ex.Message);
                //}
                finally
                {
                    DU.Dispose();
                }
            }
        }
        [Feng("😎开始融合")]
        public static void qtqxus()
        {
            try
            {
                //var testing = new HashSet<int>() { 1, };
                //var testing = new HashSet<int>() { 0, 1, 2, 3 };
                var testing = new HashSet<int>() { 0, 2, 3 };
                //var testing = new HashSet<int>() { 0 };
                //var testing = new HashSet<int>() { 4 };
                //var testing = new HashSet<int>() { 5 };
                //var testing = new HashSet<int>() { 9 };
                //var testing = new HashSet<int>() { 10 };
                //var testing = new HashSet<int>() { 11 };

                var stdDrawingDatas = LoadStdDrawingDatas();

                var buckets = LoadWaterBucketGeoDataList();
                var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
                var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
                var files = Util1.getFiles();
                var d = Util1.getRangeDict();
                var list = new List<List<RainSystemDrawingData>>();

                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        var getGeoData = fs[i];
                        if (getGeoData == null || !testing.Contains(i))
                        {
                            list.Add(null);
                            continue;
                        }

                        var file = files[i];
                        var range = d[file].ToPoint3dCollection();
                        using (Dbg.DocumentLock)
                        using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                        {
                            try
                            {
                                var geoData = buckets[i];
                                geoData.Init();
                                var cl = new ThRainSystemService.ThRainSystemServiceGeoCollector2() { adb = adb, geoData = geoData };
                                cl.CollectEntities();
                                cl.CollectLabelLines();
                                cl.CollectCTexts();
                                cl.CollectVerticalPipes();
                                cl.CollectWLines();
                                cl.CollectCondensePipes();
                                cl.CollectFloorDrains();
                                cl.CollectWaterWells();
                                cl.CollectWaterPortSymbols();
                                cl.CollectWaterPort13s();
                                cl.CollectWrappingPipes();
                                cl.CollectStoreys(range);

                                DU.Dispose();
                                var storeys = ThRainSystemService.GetStoreys(range, adb);
                                {
                                    var bk = buckets[i];
                                    geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                                    geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                                }
                                geoData.FixData();
                                //var labelHeight = hs[i];
                                //var labelHeight = 350;
                                //var labelHeight = ThRainSystemService.TryGuessLabelHeight(adb);
                                var labelHeight = -1;
                                ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                                ThRainSystemService.ConnectLabelToLabelLine(geoData);
                                geoData.FixData();
                                var cadDataMain = RainSystemCadData.Create(geoData);
                                var cadDatas = cadDataMain.SplitByStorey();
                                var sv = new RainSystemService()
                                {
                                    Storeys = storeys,
                                    GeoData = geoData,
                                    CadDataMain = cadDataMain,
                                    CadDatas = cadDatas,
                                };
                                sv.CreateDrawingDatas();
                                DU.Dispose();

                                var drDatas = sv.DrawingDatas;
                                list.Add(drDatas);
                            }
                            finally
                            {
                                adb.DiscardChanges();
                            }

                        }
                    }
                }
                {
                    var sb = new StringBuilder(4096);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var drDatas = list[i];
                        if (drDatas == null) continue;
                        var stdDrDatas = stdDrawingDatas[i];
                        if (stdDrDatas == null) continue;
                        sb.AppendLine($"---test{i + 1:D2}");
                        for (int j = 0; j < drDatas.Count; j++)
                        {
                            var d1 = stdDrDatas[j];
                            var d2 = drDatas[j];
                            qtpp9s(sb, d1, d2, $"上面的是框{j}");
                        }
                    }
                    Dbg.PrintText(sb.ToString());
                    var file = newFileFn();
                    File.WriteAllText(file, list.ToCadJson());
                }
            }
            catch (System.Exception ex)
            {
                Dbg.PrintText(ex.Message);
                Dbg.PrintText(ex.StackTrace);
            }
        }
        [Feng("😎直接跑")]
        public static void qtt6el()
        {
            ThRainSystemService.DrawRainSystemDiagram2();
        }
        public static void TestThWRainSystemDiagram_Save()
        {
            ThWRainSystemDiagramTest.Test2();
        }
        [Feng("🔴TestThWRainSystemDiagram_Load")]
        public static void TestThWRainSystemDiagram_Load()
        {
            ThRainSystemService.ImportElementsFromStdDwg();
            ThWRainSystemDiagramTest.Test1();
        }
        [Feng("🔴cad win")]
        public static void qtytu6()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var r = Dbg.SelectGRect();
                var ret = Dbg.Editor.SelectCrossingWindow(r.LeftTop.ToPoint3d(), r.RightButtom.ToPoint3d());
                if (ret.Status == PromptStatus.OK)
                {
                    var ents = ret.Value.GetObjectIds().Select(id => adb.Element<Entity>(id)).ToList();
                    foreach (var e in ents)
                    {
                        Dbg.ShowWhere(e);
                    }
                }
            }
        }
        [Feng]
        public static void qtppl7()
        {
            using var bm = new BenchMark();
            var buckets = LoadWaterBucketGeoDataList();
            var fs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetFuncs();
            var hs = ThRainSystemService.ThRainSystemServiceGeoCollector1.GetHeights();
            var files = Util1.getFiles();
            var d = Util1.getRangeDict();
            var list = new List<List<RainSystemDrawingData>>();
            using (Dbg.DocumentLock)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var getGeoData = fs[i];
                    if (getGeoData == null)
                    {
                        list.Add(null);
                        continue;
                    }
                    var file = files[i];
                    var range = d[file].ToPoint3dCollection();
                    using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                    {
                        var geoData = getGeoData(adb, range);
                        var labelHeight = hs[i];

                        DU.Dispose();
                        var storeys = ThRainSystemService.GetStoreys(range, adb);
                        //{
                        //    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        //    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        //}
                        {
                            var bk = buckets[i];
                            geoData.SideWaterBuckets.AddRange(bk.SideWaterBuckets);
                            geoData.GravityWaterBuckets.AddRange(bk.GravityWaterBuckets);
                        }
                        ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                        geoData.FixData();
                        var cadDataMain = RainSystemCadData.Create(geoData);
                        var cadDatas = cadDataMain.SplitByStorey();
                        var sv = new RainSystemService()
                        {
                            Storeys = storeys,
                            GeoData = geoData,
                            CadDataMain = cadDataMain,
                            CadDatas = cadDatas,
                        };
                        sv.CreateDrawingDatas();
                        DU.Dispose();

                        var drDatas = sv.DrawingDatas;

                        //Dbg.PrintText(storeys.ToCadJson());
                        //Dbg.PrintText(drDatas.ToCadJson());
                        list.Add(drDatas);
                    }
                }
            }
            {
                var file = newFileFn();
                //重新设置std的时候用下面这行
                //var file = @"Y:\637577104526532228.json";
                File.WriteAllText(file, list.ToCadJson());
            }
        }
        [Feng]
        public static void qtplas()
        {
            var files = Util1.getFiles();

            var d = Util1.getRangeDict();
            using (Dbg.DocumentLock)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    var range = d[file].ToPoint3dCollection();
                    using (var adb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly))
                    {
                        RainSystemGeoData getGeoData(Point3dCollection range)
                        {
                            var labelLines = new List<GLineSegment>();
                            var cts = new List<CText>();
                            var pipes = new List<GRect>();
                            var storeys = new List<GRect>();
                            var wLines = new List<GLineSegment>();
                            var condensePipes = new List<GRect>();
                            var floorDrains = new List<GRect>();
                            var waterWells = new List<GRect>();
                            var waterWellDNs = new List<string>();
                            var waterPortSymbols = new List<GRect>();
                            var waterPort13s = new List<GRect>();
                            var wrappingPipes = new List<GRect>();

                            //using (var adb = AcadDatabase.Active())
                            {
                                //Dbg.BuildAndSetCurrentLayer(adb.Database);

                                IEnumerable<Entity> GetEntities()
                                {
                                    //return adb.ModelSpace.OfType<Entity>();
                                    foreach (var ent in adb.ModelSpace.OfType<Entity>())
                                    {
                                        if (ent is BlockReference br && br.Layer == "0")
                                        {
                                            var r = br.Bounds.ToGRect();
                                            if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                            {
                                                foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                                {
                                                    yield return e;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            yield return ent;
                                        }
                                    }
                                }
                                var entities = GetEntities().ToList();


                                {
                                    var ents = new List<Entity>();
                                    ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                                    waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                                }
                                {
                                    var ents = new List<BlockReference>();
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                                    waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                                    waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                                }
                                {
                                    var ents = new List<Entity>();
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ToDataItem().EffectiveName.Contains("地漏")));
                                    floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                                }
                                {
                                    var ents = new List<Entity>();
                                    ents.AddRange(entities.OfType<Circle>()
                                        .Where(c => c.Layer == "W-RAIN-EQPM")
                                        .Where(c => 20 < c.Radius && c.Radius < 40));
                                    condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                                }
                                {
                                    var ents = new List<Entity>();
                                    ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.Layer == "W-BUSH" && x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                                    wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()).Where(r => r.Width < 1000 && r.Height < 1000));
                                }
                                {
                                    foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-RAIN-NOTE" && e.Length > 0))
                                    {
                                        labelLines.Add(e.ToGLineSegment());
                                    }

                                    foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                                    {
                                        cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                                    }
                                }

                                {
                                    var pps = new List<Entity>();
                                    var blockNameOfVerticalPipe = "带定位立管";
                                    pps.AddRange(entities.OfType<BlockReference>()
                                     .Where(x => x.Layer == ThWPipeCommon.W_RAIN_EQPM)
                                     .Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName == blockNameOfVerticalPipe));
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
                                    var storeysRecEngine = new ThStoreysRecognitionEngine();
                                    storeysRecEngine.Recognize(adb.Database, range);
                                    //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                                    foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                                    {
                                        var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                                        storeys.Add(bd);
                                    }
                                }

                                {
                                    foreach (var e in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-PIPE").ToList())
                                    {
                                        if (e is Line line && line.Length > 0)
                                        {
                                            wLines.Add(line.ToGLineSegment());
                                        }
                                        else if (ThRainSystemService.IsTianZhengElement(e.GetType()))
                                        {
                                            //var lst = e.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                                            //if (lst.Count == 1 && lst[0] is Line ln && ln.Length > 0)
                                            //{
                                            //    wLines.Add(ln.ToGLineSegment());
                                            //}
                                            foreach (var ln in e.ExplodeToDBObjectCollection().OfType<Line>())
                                            {
                                                if (ln.Length > 0)
                                                {
                                                    wLines.Add(ln.ToGLineSegment());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            var geoData = new RainSystemGeoData();
                            geoData.Init();
                            geoData.Storeys.AddRange(storeys);
                            geoData.LabelLines.AddRange(labelLines);
                            geoData.WLines.AddRange(wLines);
                            geoData.Labels.AddRange(cts);
                            geoData.VerticalPipes.AddRange(pipes);
                            geoData.CondensePipes.AddRange(condensePipes);
                            geoData.FloorDrains.AddRange(floorDrains);
                            geoData.WaterWells.AddRange(waterWells);
                            geoData.WaterWellLabels.AddRange(waterWellDNs);
                            geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                            geoData.WaterPort13s.AddRange(waterPort13s);
                            geoData.WrappingPipes.AddRange(wrappingPipes);
                            //geoData.SideWaterBuckets.AddRange(xxx);
                            //geoData.GravityWaterBuckets.AddRange(xxx);

                            geoData.FixData();
                            return geoData;
                        }
                        double labelHeight = 350;

                        DU.Dispose();
                        var storeys = ThRainSystemService.GetStoreys(range, adb);
                        var geoData = getGeoData(range);
                        ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                        geoData.FixData();
                        var cadDataMain = RainSystemCadData.Create(geoData);
                        var cadDatas = cadDataMain.SplitByStorey();
                        var sv = new RainSystemService()
                        {
                            Storeys = storeys,
                            GeoData = geoData,
                            CadDataMain = cadDataMain,
                            CadDatas = cadDatas,
                        };
                        sv.CreateDrawingDatas();
                        DU.Dispose();

                        var drDatas = sv.DrawingDatas;

                        //Dbg.PrintText(storeys.ToCadJson());
                        Dbg.PrintText(drDatas.ToCadJson());
                    }
                    if (i >= 5) break;
                }
            }

        }
        public static void qtpp9s(StringBuilder sb, RainSystemDrawingData d1, RainSystemDrawingData d2, string comment = null)
        {
            var s = 0;
            s += qtpla8(sb, d1.RoofLabels, d2.RoofLabels, nameof(d1.RoofLabels));
            s += qtpla8(sb, d1.BalconyLabels, d2.BalconyLabels, nameof(d1.BalconyLabels));
            s += qtpla8(sb, d1.CondenseLabels, d2.CondenseLabels, nameof(d1.CondenseLabels));
            s += qtpla8(sb, d1.CommentLabels, d2.CommentLabels, nameof(d1.CommentLabels));
            s += qtpla8(sb, d1.LongTranslatorLabels, d2.LongTranslatorLabels, nameof(d1.LongTranslatorLabels));
            s += qtpla8(sb, d1.ShortTranslatorLabels, d2.ShortTranslatorLabels, nameof(d1.ShortTranslatorLabels));
            s += qtpla8(sb, d1.GravityWaterBucketTranslatorLabels, d2.GravityWaterBucketTranslatorLabels, nameof(d1.GravityWaterBucketTranslatorLabels));
            s += qtpla8(sb, d1.FloorDrains, d2.FloorDrains, nameof(d1.FloorDrains));
            s += qtpla8(sb, d1.FloorDrainsWrappingPipes, d2.FloorDrainsWrappingPipes, nameof(d1.FloorDrainsWrappingPipes));
            s += qtpla8(sb, d1.WaterWellWrappingPipes, d2.WaterWellWrappingPipes, nameof(d1.WaterWellWrappingPipes));
            s += qtpla8(sb, d1.RainPortWrappingPipes, d2.RainPortWrappingPipes, nameof(d1.RainPortWrappingPipes));
            s += qtpla8(sb, d1.CondensePipes, d2.CondensePipes, nameof(d1.CondensePipes));
            s += qtpla8(sb, d1.OutputTypes, d2.OutputTypes, nameof(d1.OutputTypes));
            s += qtpla8(sb, d1.PipeLabelToWaterWellLabels, d2.PipeLabelToWaterWellLabels, nameof(d1.PipeLabelToWaterWellLabels));
            if (comment != null && s > 0) sb.AppendLine(comment);
        }
        public static int qtpla8<T>(StringBuilder sb, IList<T> lst1, IList<T> lst2, string comment)
        {
            var s = 0;
            var l1 = lst1.Except(lst2).ToList();
            var l2 = lst2.Except(lst1).ToList();
            if (l1.Count > 0)
            {
                sb.AppendLine("- " + comment);
                sb.AppendLine(l1.ToCadJson());
                s += l1.Count;
            }
            if (l2.Count > 0)
            {
                sb.AppendLine("+ " + comment);
                sb.AppendLine(l2.ToCadJson());
                s += l2.Count;
            }
            return s;
        }
        [Feng("test05")]
        public static void qtnetd()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "块")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //此处的“地漏”字样被放在了“可见性”里
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100));
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect();
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }

            qtjrlj(getGeoData, 250);
        }
        [Feng("test12")]
        public static void qtpiuh()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                        foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<Circle>().Where(c => c.Layer == "W-RAIN-DIMS" && 40 < c.Radius && c.Radius < 100));
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect();
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 350);
        }
        [Feng("test11")]
        public static void qtpgkd()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 <= c.Radius && c.Radius <= 30));//暂时调成30
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                        foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        //pps.AddRange(entities.OfType<Circle>().Where(c => 30 < c.Radius && c.Radius < 100));//最好加上图层
                        pps.AddRange(entities.OfType<BlockReference>()
                            .Where(e =>
                            {
                                return e.ObjectId.IsValid && (e.Layer == "W-RAIN-PIPE-RISR" || e.Layer == "W-DRAI-NOTE")
                                 && !e.ToDataItem().EffectiveName.Contains("井");
                            }));
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect();
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 250);
        }
        [Feng("test10")]
        public static void qtpi1j()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        //return adb.ModelSpace.OfType<Entity>();
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 80000 && r.Height > 15000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("套管") : x.Layer == "W-BUSH"));
                        wrappingPipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        //从可见性里拿
                        //ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 <= c.Radius && c.Radius <= 30));//暂时调成30
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-NOTE"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                        foreach (var ent in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        pps.AddRange(entities.OfType<Circle>().Where(c => 30 < c.Radius && c.Radius < 100));//暂时调成30
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect();
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities.OfType<Entity>()));

                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 250);
        }
        [Feng("test06")]
        public static void qtpfjy()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 50000 && r.Height > 10000 && r.Height < 25000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid ? x.ToDataItem().EffectiveName.Contains("地漏") : x.Layer == "W-DRAI-FLDR"));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-NOTE" || e.Layer == "W-FRPT-NOTE") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }
                        //处理label线混用问题
                        //foreach (var e in entities.OfType<Line>().Where(e => e.Layer == "W-DRAI-NOTE" && e.Length > 0))
                        //{
                        //    labelLines.Add(e.ToGLineSegment());
                        //}

                        foreach (var ent in entities.OfType<Entity>().Where(e => e.Layer == "W-RAIN-NOTE" && ThRainSystemService.IsTianZhengElement(e.GetType())))
                        {
                            foreach (var e in ent.ExplodeToDBObjectCollection().OfType<DBText>())
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                        foreach (var e in entities.OfType<DBText>().Where(x => x.Layer == "W-RAIN-NOTE" || x.Layer == "W-FRPT-NOTE"))
                        {
                            //if (e.TextString.Contains("雨水口") || ThRainSystemService.IsWantedLabelText(e.TextString))
                            {
                                cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                            }
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        var q = entities.OfType<Entity>().Where(x => (x.Layer == "WP_KTN_LG" || x.Layer == "W-RAIN-EQPM")).Where(e =>
                        {
                            if (e is Circle) return true;
                            if (ThRainSystemService.IsTianZhengElement(e.GetType()))
                            {
                                return e.ExplodeToDBObjectCollection().OfType<Circle>().Any();
                            }
                            return false;
                        });
                        pps.AddRange(q);
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            //var ents = ent.ExplodeToDBObjectCollection().OfType<Circle>().ToList();
                            //var et = ents.FirstOrDefault(e => Convert.ToInt32(GeoAlgorithm.GetBoundaryRect(e).Width) == 100);
                            //if (et != null) return GeoAlgorithm.GetBoundaryRect(et);
                            //return GeoAlgorithm.GetBoundaryRect(ent);
                            return GRect.Create(ent.Bounds.ToGRect().Center, 50);
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities));
                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }
            qtjrlj(getGeoData, 250);
        }
        [Feng("test04")]
        public static void qtlbfr()
        {
            RainSystemGeoData getGeoData(Point3dCollection range)
            {
                var labelLines = new List<GLineSegment>();
                var cts = new List<CText>();
                var pipes = new List<GRect>();
                var storeys = new List<GRect>();
                var wLines = new List<GLineSegment>();
                var condensePipes = new List<GRect>();
                var floorDrains = new List<GRect>();
                var waterWells = new List<GRect>();
                var waterWellDNs = new List<string>();
                var waterPortSymbols = new List<GRect>();
                var waterPort13s = new List<GRect>();
                var wrappingPipes = new List<GRect>();

                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                using (var tr = new DrawingTransaction(adb))
                {
                    Dbg.BuildAndSetCurrentLayer(adb.Database);

                    IEnumerable<Entity> GetEntities()
                    {
                        foreach (var ent in adb.ModelSpace.OfType<Entity>())
                        {
                            if (ent is BlockReference br && br.Layer == "0")
                            {
                                var r = br.Bounds.ToGRect();
                                if (r.Width > 35000 && r.Width < 50000 && r.Height > 10000 && r.Height < 25000)
                                {
                                    foreach (var e in br.ExplodeToDBObjectCollection().OfType<Entity>())
                                    {
                                        yield return e;
                                    }
                                }
                            }
                            else
                            {
                                yield return ent;
                            }
                        }
                    }
                    var entities = GetEntities().ToList();

                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Spline>().Where(x => x.Layer == "W-RAIN-DIMS"));
                        waterPortSymbols.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<BlockReference>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.Name.Contains("雨水井编号")));
                        waterWells.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                        waterWellDNs.AddRange(ents.Select(e => e.GetAttributesStrValue("-") ?? ""));

                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR" || x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("地漏")));
                        floorDrains.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<BlockReference>().Where(x => x.ObjectId.IsValid && x.ToDataItem().EffectiveName.Contains("雨水口")));
                        waterPort13s.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }
                    {
                        var ents = new List<Entity>();
                        ents.AddRange(entities.OfType<Circle>()
                            .Where(c => c.Layer == "W-RAIN-EQPM")
                            .Where(c => 20 < c.Radius && c.Radius < 40));
                        condensePipes.AddRange(ents.Select(e => e.Bounds.ToGRect()));
                    }

                    {
                        foreach (var e in entities.OfType<Line>().Where(e => (e.Layer == "W-RAIN-DIMS") && e.Length > 0))
                        {
                            labelLines.Add(e.ToGLineSegment());
                        }

                        foreach (var e in entities.OfType<DBText>().Where(e => e.Layer == "W-RAIN-DIMS"))
                        {
                            cts.Add(new CText() { Text = e.TextString, Boundary = e.Bounds.ToGRect() });
                        }
                    }

                    {
                        var pps = new List<Entity>();
                        var q = entities.OfType<Circle>().Where(c => 40 < c.Radius && c.Radius < 100);
                        pps.AddRange(q);
                        static GRect getRealBoundaryForPipe(Entity ent)
                        {
                            return ent.Bounds.ToGRect();
                        }
                        foreach (var pp in pps)
                        {
                            pipes.Add(getRealBoundaryForPipe(pp));
                        }
                    }

                    {
                        var storeysRecEngine = new ThStoreysRecognitionEngine();
                        storeysRecEngine.Recognize(adb.Database, range);
                        //var ents = storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>().Select(x => adb.Element<Entity>(x.ObjectId)).ToList();
                        foreach (var item in storeysRecEngine.Elements.OfType<ThMEPEngineCore.Model.Common.ThStoreys>())
                        {
                            var bd = adb.Element<Entity>(item.ObjectId).Bounds.ToGRect();
                            storeys.Add(bd);
                        }
                    }

                    wLines.AddRange(Util1.GetWRainLines(entities));

                }
                var geoData = new RainSystemGeoData();
                geoData.Init();
                geoData.Storeys.AddRange(storeys);
                geoData.LabelLines.AddRange(labelLines);
                geoData.WLines.AddRange(wLines);
                geoData.Labels.AddRange(cts);
                geoData.VerticalPipes.AddRange(pipes);
                geoData.CondensePipes.AddRange(condensePipes);
                geoData.FloorDrains.AddRange(floorDrains);
                geoData.WaterWells.AddRange(waterWells);
                geoData.WaterWellLabels.AddRange(waterWellDNs);
                geoData.WaterPortSymbols.AddRange(waterPortSymbols);
                geoData.WaterPort13s.AddRange(waterPort13s);
                geoData.WrappingPipes.AddRange(wrappingPipes);
                //geoData.SideWaterBuckets.AddRange(xxx);
                //geoData.GravityWaterBuckets.AddRange(xxx);

                geoData.FixData();
                return geoData;
            }

            qtjrlj(getGeoData, 350);
        }
        static void qtjrlj(Func<Point3dCollection, RainSystemGeoData> getGeoData, double labelHeight)
        {

            AddButton("直接跑", () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                {
                    try
                    {
                        DU.Dispose();
                        var range = Dbg.SelectRange();
                        var basePt = Dbg.SelectPoint();
                        ThRainSystemService.ImportElementsFromStdDwg();
                        var storeys = ThRainSystemService.GetStoreys(range, adb);
                        var geoData = getGeoData(range);
                        ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                        ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                        ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                        geoData.FixData();
                        var cadDataMain = RainSystemCadData.Create(geoData);
                        var cadDatas = cadDataMain.SplitByStorey();
                        var sv = new RainSystemService()
                        {
                            Storeys = storeys,
                            GeoData = geoData,
                            CadDataMain = cadDataMain,
                            CadDatas = cadDatas,
                        };
                        sv.CreateDrawingDatas();
                        if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                        DU.Dispose();
                        sv.RainSystemDiagram.Draw(basePt);
                        DU.Draw(adb);
                    }
                    //catch (System.Exception ex)
                    //{
                    //    MessageBox.Show(ex.Message);
                    //}
                    finally
                    {
                        DU.Dispose();
                    }
                }
            });
            AddButton("输出绘图数据", () =>
            {
                Dbg.FocusMainWindow();
                using (Dbg.DocumentLock)
                using (var adb = AcadDatabase.Active())
                {
                    DU.Dispose();
                    var range = Dbg.SelectRange();
                    //var basePt = Dbg.SelectPoint();
                    ThRainSystemService.ImportElementsFromStdDwg();
                    var storeys = ThRainSystemService.GetStoreys(range, adb);
                    var geoData = getGeoData(range);
                    ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                    ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    geoData.FixData();
                    var cadDataMain = RainSystemCadData.Create(geoData);
                    var cadDatas = cadDataMain.SplitByStorey();
                    var sv = new RainSystemService()
                    {
                        Storeys = storeys,
                        GeoData = geoData,
                        CadDataMain = cadDataMain,
                        CadDatas = cadDatas,
                    };
                    sv.CreateDrawingDatas();
                    Dbg.PrintText(sv.DrawingDatas.ToCadJson());
                    DU.Dispose();
                }
            });
            AddLazyAction("准备画骨架", adb =>
            {
                var range = Dbg.SelectRange();

                var storeys = ThRainSystemService.GetStoreys(range, adb);
                var geoData = getGeoData(range);
                geoData.FixData();
                DrawSkeletonLazy(geoData);

                AddLazyAction("生成绘图数据并绘制", adb =>
                {
                    ThRainSystemService.PreFixGeoData(geoData, labelHeight);
                    BuildDrawingDatas(storeys, geoData);
                });
            });
        }
        [Feng("💰自定义空间索引")]
        public static void qthme9()
        {

            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var line1 = Dbg.SelectEntity<Line>(adb);
                var seg1 = line1.ToGLineSegment();
                var si = GLineSegmentConnectionNTSSpacialIndex.Create(new GLineSegment[] { seg1 }, 10);
                var lst = si.SelectCrossingGRect(Dbg.SelectGRect());
                Dbg.PrintLine(lst.Count.ToString());
            }
        }
        public static void qtla9m(IList<Geometry> geos)
        {
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            Polygon polygon = null;
            var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(polygon);
            var q = engine.Query(polygon.EnvelopeInternal).Where(geo => gf.Intersects(geo));
        }
        public static Polygon ToNTSPolygon(Polyline poly)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                return poly.ToNTSPolygon();
            }
        }
        public static List<Polygon> ToNTSPolygons(IList<Polyline> polys)
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                return polys.Select(pl => pl.ToNTSPolygon()).ToList();
            }
        }



        public static void qtk54o()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var numPoints = 6;
                // 获取圆的外接矩形
                var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
                {
                    NumPoints = numPoints,
                    Size = 2 * 1000,
                    Centre = Dbg.SelectPoint().ToNTSCoordinate(),
                };
                var ring = shapeFactory.CreateCircle().Shell;
                DU.DrawLinearRing(ring);
            }

        }
        public class GLineSegmentConnectionNTSSpacialIndex : NTSSpacialIndexAB<GLineSegment>
        {
            double radius;
            protected GLineSegmentConnectionNTSSpacialIndex() : base() { }
            public static GLineSegmentConnectionNTSSpacialIndex Create(IEnumerable<GLineSegment> lines, double radius)
            {
                var si = new GLineSegmentConnectionNTSSpacialIndex();
                si.radius = radius;
                foreach (var seg in lines)
                {
                    si.dict[si.ToNTSGeometry(seg)] = seg;
                }
                si.InitEngine();
                return si;
            }
            public override Geometry ToNTSGeometry(GLineSegment seg)
            {
                var points1 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.StartPoint, this.radius));
                var points2 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.EndPoint, this.radius));
                var ring1 = new LinearRing(points1);
                var ring2 = new LinearRing(points2);
                var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
                return geo;
            }
        }
        public class GCircleNTSSpacialIndex : NTSSpacialIndexAB<GCircle>
        {
            protected GCircleNTSSpacialIndex() : base() { }
            public static GCircleNTSSpacialIndex Create(IEnumerable<GCircle> lines)
            {
                var si = new GCircleNTSSpacialIndex();
                foreach (var seg in lines)
                {
                    si.dict[si.ToNTSGeometry(seg)] = seg;
                }
                si.InitEngine();
                return si;
            }
            public override Geometry ToNTSGeometry(GCircle circle)
            {
                var numPoints = 6;
                // 获取圆的外接矩形
                var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
                {
                    NumPoints = numPoints,
                    Size = 2 * circle.Radius,
                    Centre = circle.Center.ToNTSCoordinate(),
                };
                return shapeFactory.CreateCircle().Shell;
            }
        }


        public abstract class NTSSpacialIndexAB<T>
        {
            public NetTopologySuite.Index.Strtree.STRtree<Geometry> Engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            public Dictionary<Geometry, T> dict = new Dictionary<Geometry, T>();
            public void InitEngine()
            {
                if (dict.Keys.Count == 0) throw new System.Exception("索引数组为空");
                dict.Keys.ForEach(g => Engine.Insert(g.EnvelopeInternal, g));
            }
            public static Polygon ToNTSPolygon(Polyline polyLine)
            {
                var geometry = polyLine.ToNTSLineString();
                if (geometry is LinearRing ring)
                {
                    return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(ring);
                }
                else
                {
                    //return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    return null;
                }
            }
            public List<T> SelectCrossingGRect(GRect gRect)
            {
                var geometry = ConvertToNTSPolygon(gRect);
                if (geometry == null) return new List<T>();
                return CrossingFilter(
                    Query(geometry.EnvelopeInternal),
                    ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry))
                    .ToList();
            }

            public static Polygon ConvertToNTSPolygon(GRect gRect)
            {
                if (!gRect.IsValid) return null;
                Coordinate[] points = GeoNTSConvertion.ConvertToCoordinateArray(gRect);
                var ring = ThCADCoreNTSService.Instance.GeometryFactory.CreateLinearRing(points);
                var geometry = ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(ring);
                return geometry;
            }



            public List<T> SelectCrossingPolygon(Polyline polyline)
            {
                var geometry = ToNTSPolygon(polyline);
                if (geometry == null) return new List<T>();
                return CrossingFilter(
                    Query(geometry.EnvelopeInternal),
                    ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geometry))
                    .ToList();
            }
            public IEnumerable<T> CrossingFilter(IEnumerable<T> objs, NetTopologySuite.Geometries.Prepared.IPreparedGeometry preparedGeometry)
            {
                return objs.Where(o => Intersects(preparedGeometry, o));
            }
            public bool Intersects(NetTopologySuite.Geometries.Prepared.IPreparedGeometry preparedGeometry, T key)
            {
                return preparedGeometry.Intersects(ToNTSGeometry(key));
            }
            private IEnumerable<T> Query(Envelope envelope)
            {
                foreach (var geometry in Engine.Query(envelope))
                {
                    if (dict.TryGetValue(geometry, out T value)) yield return value;
                }
            }
            public abstract Geometry ToNTSGeometry(T key);
        }

        private static void NewMethod()
        {
            ThWRainSystemDiagram.DrawingTest();
        }




        private static void DrawRainSystemDiagram(ThWRainSystemDiagram dg, Point3d basePt)
        {
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                dg.Draw(basePt);
            }
        }
        [Feng("ThRainSystemService.DrawRainSystemDiagram1();")]
        public static void qthlws()
        {
            ThRainSystemService.DrawRainSystemDiagram1();
        }
        public static void qty6kj()
        {
            var storeys = Util1.LoadCadData<Dictionary<string, List<ThStoreysData>>>(FengKeys.StoreysJsonData210519).Values.First();
            var geoData = File.ReadAllText(qtdwh3).FromCadJson<RainSystemGeoData>();
            {
                //导入雨水斗数据
                var items = LoadData<List<RainSystemGeoData>>(FengKeys.WaterBucketsJsonData210519, Util1.cvt4);
                var data = items.First();
                geoData.SideWaterBuckets.AddRange(data.SideWaterBuckets);
                geoData.GravityWaterBuckets.AddRange(data.GravityWaterBuckets);
            }
            NewMethod1(storeys, geoData);

        }

        private static void NewMethod1(List<ThStoreysData> storeys, RainSystemGeoData geoData)
        {


            AddButton("打印立管label", () =>
            {
                ThRainSystemService.PreFixGeoData(geoData, 150);
                geoData.FixData();
                var cadDataMain = RainSystemCadData.Create(geoData);
                var cadDatas = cadDataMain.SplitByStorey();
                var sb = new StringBuilder(8192);
                for (int i = 0; i < geoData.Storeys.Count; i++)
                {
                    var r = geoData.Storeys[i];
                    var s = storeys[i];
                    sb.AppendLine("楼层");
                    sb.AppendLine(s.Storeys.ToJson());
                    sb.AppendLine(s.StoreyType.ToString());
                    var item = cadDatas[i];

                    var wantedLabels = new List<string>();
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        if (RainSystemService.IsWantedText(m.Text))
                        {
                            wantedLabels.Add(m.Text);
                        }
                    }
                    sb.AppendLine("立管");
                    sb.AppendLine(ThRainSystemService.GetRoofLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetBalconyLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetCondenseLabels(wantedLabels).ToJson());
                    {
                        var lst = wantedLabels.Where(x => ThRainSystemService.HasGravityLabelConnected(x)).ToList();
                        if (lst.Count > 0)
                        {
                            sb.AppendLine("特殊处理的text");
                            sb.AppendLine(lst.ToJson());
                        }
                    }
                }
                Dbg.PrintText(sb.ToString());

            });
            AddLazyAction("生成绘图数据", adb =>
            {
                ThRainSystemService.PreFixGeoData(geoData, 150);
                BuildDrawingDatas(storeys, geoData);
            });

            //qtduqc(geoData);
        }

        private static void BuildDrawingDatas(List<ThStoreysData> storeys, RainSystemGeoData geoData)
        {

            geoData.FixData();
            var cadDataMain = RainSystemCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();
            var sv = new RainSystemService()
            {
                Storeys = storeys,
                GeoData = geoData,
                CadDataMain = cadDataMain,
                CadDatas = cadDatas,
            };
            sv.CreateDrawingDatas();

            AddButton("画最终输出", () =>
            {
                Dbg.FocusMainWindow();
                var basePt = Dbg.SelectPoint();
                ThRainSystemService.ImportElementsFromStdDwg();
                if (sv.RainSystemDiagram == null) sv.CreateRainSystemDiagram();
                DrawRainSystemDiagram(sv.RainSystemDiagram, basePt);
            });
        }
        [Feng]
        public static void qtduwv()
        {
            var geoData = qtdtsl();
            var file = @"D:\DATA\temp\" + DateTime.Now.Ticks + ".json";
            File.WriteAllText(file, geoData.ToCadJson());
            Dbg.PrintLine(file);
        }

        private static void qtduvb()
        {
            var geoData = File.ReadAllText(qtdwh3).FromCadJson<RainSystemGeoData>();
            DrawSkeletonLazy(geoData);
        }

        private static void qtdtx9()
        {
            DrawSkeletonLazy(qtdtsl());
        }
        public static void AddButton(string name, Action f)
        {
            Util1.AddButton(name, f);
        }
        public static void AddLazyAction(string name, Action<AcadDatabase> f)
        {
            Util1.AddLazyAction(name, f);
        }
        private static void DrawSkeletonLazy(RainSystemGeoData geoData)
        {
            var cadDataMain = RainSystemCadData.Create(geoData);
            var cadDatas = cadDataMain.SplitByStorey();

            Util1.AddLazyAction("画骨架", adb =>
            {
                //Dbg.PrintLine(lst.Count);
                for (int i = 0; i < cadDatas.Count; i++)
                {
                    {
                        var s = geoData.Storeys[i];
                        var e = DU.DrawRectLazy(s);
                        e.ColorIndex = 1;
                    }
                    var item = cadDatas[i];
                    foreach (var o in item.LabelLines)
                    {
                        var j = cadDataMain.LabelLines.IndexOf(o);
                        var m = geoData.LabelLines[j];
                        var e = DU.DrawLineSegmentLazy(m);
                        e.ColorIndex = 1;
                    }
                    foreach (var o in item.WLines)
                    {
                        var j = cadDataMain.WLines.IndexOf(o);
                        var m = geoData.WLines[j];
                        var e = DU.DrawLineSegmentLazy(m);
                        e.ColorIndex = 4;
                    }
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                        e.ColorIndex = 2;
                        var _pl = DU.DrawRectLazy(m.Boundary);
                        _pl.ColorIndex = 2;
                    }
                    foreach (var o in item.VerticalPipes)
                    {
                        var j = cadDataMain.VerticalPipes.IndexOf(o);
                        var m = geoData.VerticalPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 3;
                    }

                    foreach (var o in item.CondensePipes)
                    {
                        var j = cadDataMain.CondensePipes.IndexOf(o);
                        var m = geoData.CondensePipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 2;
                    }
                    foreach (var o in item.FloorDrains)
                    {
                        var j = cadDataMain.FloorDrains.IndexOf(o);
                        var m = geoData.FloorDrains[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 6;
                    }
                    foreach (var o in item.WaterWells)
                    {
                        var j = cadDataMain.WaterWells.IndexOf(o);
                        var m = geoData.WaterWells[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                    }
                    {
                        var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                        foreach (var o in item.WaterPortSymbols)
                        {
                            var j = cadDataMain.WaterPortSymbols.IndexOf(o);
                            var m = geoData.WaterPortSymbols[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                        foreach (var o in item.WaterPort13s)
                        {
                            var j = cadDataMain.WaterPort13s.IndexOf(o);
                            var m = geoData.WaterPort13s[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                    }
                    {
                        var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                        foreach (var o in item.WrappingPipes)
                        {
                            var j = cadDataMain.WrappingPipes.IndexOf(o);
                            var m = geoData.WrappingPipes[j];
                            var e = DU.DrawRectLazy(m);
                            e.Color = cl;
                        }
                    }
                }
            });
        }
    }
    public class ListDict<K, V> : Dictionary<K, List<V>>
    {
        public void Add(K key, V value)
        {
            var d = this;
            if (!d.TryGetValue(key, out List<V> lst))
            {
                lst = new List<V>() { value };
                d[key] = lst;
            }
            else
            {
                lst.Add(value);
            }
        }
    }
    public class CountDict<K> : IEnumerable<KeyValuePair<K, int>>
    {
        Dictionary<K, int> d = new Dictionary<K, int>();
        public int this[K key]
        {
            get
            {
                d.TryGetValue(key, out int value); return value;
            }
            set
            {
                d[key] = value;
            }
        }

        public IEnumerator<KeyValuePair<K, int>> GetEnumerator()
        {
            foreach (var kv in d)
            {
                if (kv.Value > 0) yield return kv;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    public static class qtdueu
    {
        public static string ToCadJson(this object obj)
        {
            return Util1.ToJson(obj);
        }
        public static T FromCadJson<T>(this string json)
        {
            return Util1.FromJson<T>(json);
        }
    }
    public class FengAttribute : Attribute
    {
        public string Title;
        public FengAttribute() { }
        public FengAttribute(string title) { this.Title = title; }
    }
    public class RainSystemService
    {
        public RainSystemCadData CadDataMain;
        public List<RainSystemCadData> CadDatas;
        public List<ThStoreysData> Storeys;
        public RainSystemGeoData GeoData;
        public ThWRainSystemDiagram RainSystemDiagram;
        public List<RainSystemDrawingData> DrawingDatas;

        public void BuildRainSystemDiagram<T>(string label, T sys, VerticalPipeType sysType) where T : ThWRainPipeSystem
        {
            for (int i = 0; i < RainSystemDiagram.WSDStoreys.Count; i++)
            {
                var run = new ThWRainPipeRun()
                {
                    MainRainPipe = new ThWSDPipe()
                    {
                        Label = label,
                        DN = "DN100",
                    },
                    Storey = RainSystemDiagram.WSDStoreys[i],
                    TranslatorPipe = new ThWSDTranslatorPipe(),
                };
                var bd = run.Storey.Boundary;
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var storeyI = Storeys.IndexOf(storey);
                    List<string> labels = sysType switch
                    {
                        VerticalPipeType.RoofVerticalPipe => DrawingDatas[storeyI].RoofLabels,
                        VerticalPipeType.BalconyVerticalPipe => DrawingDatas[storeyI].BalconyLabels,
                        VerticalPipeType.CondenseVerticalPipe => DrawingDatas[storeyI].CondenseLabels,
                        _ => throw new NotSupportedException(),
                    };
                    AddPipeRuns(label, sys, run, storeyI, labels);
                }
            }
        }
        private void AddPipeRuns<T>(string label, T sys, ThWRainPipeRun run, int storeyI, List<string> labels) where T : ThWRainPipeSystem
        {
            var drData = DrawingDatas[storeyI];
            if (labels.Contains(label))
            {
                if (drData.ShortTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Short;
                }
                else if (drData.GravityWaterBucketTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Gravity;
                }
                else if (drData.LongTranslatorLabels.Contains(label))
                {
                    run.TranslatorPipe.TranslatorType = TranslatorTypeEnum.Long;
                }
                foreach (var kv in drData.PipeLabelToWaterWellLabels)
                {
                    if (kv.Key == label)
                    {
                        sys.OutputType.Label = kv.Value;
                        break;
                    }
                }
                foreach (var kv in drData.OutputTypes)
                {
                    if (kv.Key == label)
                    {
                        sys.OutputType.OutputType = kv.Value;
                        if (kv.Value == RainOutputTypeEnum.WaterWell)
                        {
                            foreach (var _kv in drData.WaterWellWrappingPipes)
                            {
                                if (_kv.Key == label && _kv.Value > 0)
                                {
                                    sys.OutputType.HasDrivePipe = true;
                                    break;
                                }
                            }
                        }
                        else if (kv.Value == RainOutputTypeEnum.RainPort)
                        {
                            foreach (var _kv in drData.RainPortWrappingPipes)
                            {
                                if (_kv.Key == label && _kv.Value > 0)
                                {
                                    sys.OutputType.HasDrivePipe = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                {
                    foreach (var kv in drData.CondensePipes)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value.Key; i++)
                            {
                                var cp = new ThWSDCondensePipe();
                                run.CondensePipes.Add(cp);
                                run.HasBrokenCondensePipe = kv.Value.Value;
                            }
                        }
                    }
                    foreach (var kv in drData.FloorDrains)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                run.FloorDrains.Add(new ThWSDFloorDrain());
                            }
                        }
                    }
                    foreach (var kv in drData.FloorDrainsWrappingPipes)
                    {
                        if (kv.Key == label)
                        {
                            for (int i = 0; i < kv.Value; i++)
                            {
                                if (i < run.FloorDrains.Count)
                                {
                                    run.FloorDrains[i].HasDrivePipe = true;
                                }
                            }
                        }
                    }
                }
                sys.PipeRuns.Add(run);
                AddPipeRunsForRF(label, sys);
                sys.PipeRuns = sys.PipeRuns.OrderBy(run => RainSystemDiagram.WSDStoreys.IndexOf(run.Storey)).ToList();
                ThWRainSystemDiagram.SetCheckPoints(sys);
                ThWRainSystemDiagram.SetCheckPoints(sys.PipeRuns);

            }

        }
        static void SortStoreys(List<ThWSDStorey> wsdStoreys)
        {
            static int getScore(string label)
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
            wsdStoreys.Sort((x, y) => getScore(x.Label) - getScore(y.Label));
        }
        public void CreateRainSystemDiagram()
        {
            var wsdStoreys = new List<ThWSDStorey>();
            CollectStoreys(wsdStoreys);
            SortStoreys(wsdStoreys);
            var dg = new ThWRainSystemDiagram();
            this.RainSystemDiagram = dg;
            dg.WSDStoreys.AddRange(wsdStoreys);

            foreach (var label in DrawingDatas.SelectMany(drData => drData.RoofLabels).Distinct())
            {
                var sys = new ThWRoofRainPipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.RoofVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.RoofVerticalPipe);
            }
            foreach (var label in DrawingDatas.SelectMany(drData => drData.BalconyLabels).Distinct())
            {
                var sys = new ThWBalconyRainPipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.BalconyVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.BalconyVerticalPipe);
            }
            foreach (var label in DrawingDatas.SelectMany(drData => drData.CondenseLabels).Distinct())
            {
                var sys = new ThWCondensePipeSystem()
                {
                    VerticalPipeId = label,
                    OutputType = new ThWSDOutputType(),
                };
                dg.CondenseVerticalRainPipes.Add(sys);
                BuildRainSystemDiagram(label, sys, VerticalPipeType.CondenseVerticalPipe);
            }
            fixDiagramData(RainSystemDiagram);
        }

        static void fixDiagramData(ThWRainSystemDiagram dg)
        {
            //根据实际业务修正

            fixOutput(dg.RoofVerticalRainPipes);
            fixOutput(dg.BalconyVerticalRainPipes);
            fixOutput(dg.CondenseVerticalRainPipes);
        }

        private static void fixOutput<T>(IList<T> systems) where T : ThWRainPipeSystem
        {
            foreach (var sys in systems)
            {
                //没有1楼的一律散排
                var r = sys.PipeRuns.FirstOrDefault(r => r.Storey?.Label == "1F");
                if (r == null)
                {
                    sys.OutputType.OutputType = RainOutputTypeEnum.None;
                }
            }
        }

        public void CollectStoreys(List<ThWSDStorey> wsdStoreys)
        {
            if (false)
            {
                var lst = Storeys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.StandardStorey || s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey)
              .SelectMany(s => s.Storeys).ToList();
                var min = lst.Min();
                var max = lst.Max();
            }
            List<string> GetVerticalPipeNotes(ThStoreysData storey)
            {
                var storeyI = Storeys.IndexOf(storey);
                if (storeyI < 0) return new List<string>();
                return DrawingDatas[storeyI].GetAllLabels();
            }
            {
                var largeRoofVPTexts = new List<string>();
                foreach (var storey in Storeys)
                {
                    var bd = storey.Boundary;
                    switch (storey.StoreyType)
                    {
                        case ThMEPEngineCore.Model.Common.StoreyType.LargeRoof:
                            {
                                largeRoofVPTexts = GetVerticalPipeNotes(storey);

                                var vps1 = new List<ThWSDPipe>();
                                largeRoofVPTexts.ForEach(pt =>
                                {
                                    vps1.Add(new ThWSDPipe() { Label = pt, });
                                });

                                wsdStoreys.Add(new ThWSDStorey() { Label = $"RF", Boundary = bd, VerticalPipes = vps1 });
                                break;
                            }
                        case ThMEPEngineCore.Model.Common.StoreyType.SmallRoof:
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.StandardStorey:
                        case ThMEPEngineCore.Model.Common.StoreyType.NonStandardStorey:
                            storey.Storeys.ForEach(i => wsdStoreys.Add(new ThWSDStorey() { Label = $"{i}F", Boundary = bd, }));
                            break;
                        case ThMEPEngineCore.Model.Common.StoreyType.Unknown:
                        default:
                            break;
                    }
                }
                {
                    var storeys = Storeys.Where(s => s.StoreyType == ThMEPEngineCore.Model.Common.StoreyType.SmallRoof).ToList();
                    if (storeys.Count == 1)
                    {
                        var storey = storeys[0];
                        var bd = storey.Boundary;
                        var smallRoofVPTexts = GetVerticalPipeNotes(storey);
                        var rf1Storey = new ThWSDStorey() { Label = $"RF+1", Boundary = bd };
                        wsdStoreys.Add(rf1Storey);

                        if (largeRoofVPTexts.Count > 0)
                        {
                            var rf2VerticalPipeText = smallRoofVPTexts.Except(largeRoofVPTexts);

                            if (rf2VerticalPipeText.Count() == 0)
                            {
                                //just has rf + 1, do nothing
                                var vps1 = new List<ThWSDPipe>();
                                smallRoofVPTexts.ForEach(pt =>
                                {
                                    vps1.Add(new ThWSDPipe() { Label = pt, });
                                });
                                rf1Storey.VerticalPipes = vps1;
                            }
                            else
                            {
                                //has rf + 1, rf + 2
                                var rf1VerticalPipeObjects = new List<ThWSDPipe>();
                                var rf1VerticalPipeTexts = smallRoofVPTexts.Except(rf2VerticalPipeText);
                                rf1VerticalPipeTexts.ForEach(pt =>
                                {
                                    rf1VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt });
                                });
                                rf1Storey.VerticalPipes = rf1VerticalPipeObjects;

                                var rf2VerticalPipeObjects = new List<ThWSDPipe>();
                                rf2VerticalPipeText.ForEach(pt =>
                                {
                                    rf2VerticalPipeObjects.Add(new ThWSDPipe() { Label = pt });
                                });

                                wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Boundary = bd, VerticalPipes = rf2VerticalPipeObjects });
                            }
                        }
                    }
                    else if (storeys.Count == 2)
                    {
                        var s1 = storeys[0];
                        var s2 = storeys[1];
                        var bd1 = s1.Boundary;
                        var bd2 = s2.Boundary;
                        SwapBy2DSpace(ref s1, ref s2, bd1, bd2);
                        var vpts1 = GetVerticalPipeNotes(s1);
                        var vpts2 = GetVerticalPipeNotes(s2);
                        var vps1 = vpts1.Select(vpt => new ThWSDPipe() { Label = vpt }).ToList();
                        var vps2 = vpts2.Select(vpt => new ThWSDPipe() { Label = vpt }).ToList();
                        wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+1", Boundary = bd1, VerticalPipes = vps1 });
                        wsdStoreys.Add(new ThWSDStorey() { Label = $"RF+2", Boundary = bd2, VerticalPipes = vps2 });
                    }
                }
            }
        }
        static void Swap<T>(ref T v1, ref T v2)
        {
            var tmp = v1;
            v1 = v2;
            v2 = tmp;
        }
        static void SwapBy2DSpace<T>(ref T v1, ref T v2, GRect bd1, GRect bd2)
        {
            var deltaX = Math.Abs(bd1.MinX - bd2.MinX);
            var deltaY = Math.Abs(bd1.MaxY - bd2.MaxY);
            if (deltaY > bd1.Height)
            {
                if (bd2.MaxY > bd1.MaxY)
                {
                    Swap(ref v1, ref v2);
                }
            }
            else if (deltaX > bd1.Width)
            {
                if (bd2.MinX < bd1.MinX)
                {
                    Swap(ref v1, ref v2);
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
        public static List<Geometry> ToList(params List<Geometry>[] plss)
        {
            return plss.SelectMany(pls => pls).ToList();
        }
        public static bool IsWantedText(string text)
        {
            return ThRainSystemService.IsWantedLabelText(text) || ThRainSystemService.HasGravityLabelConnected(text)
                || text.StartsWith("YL") || IsWaterPortLabel(text);
        }
        public static bool IsWaterPortLabel(string text)
        {
            return text.Contains("接至") && text.Contains("雨水口");
        }
        public void CreateDrawingDatas()
        {
            var cadDataMain = CadDataMain;
            var geoData = GeoData;
            var cadDatas = CadDatas;
            var storeys = Storeys;

            var drawingDatas = new List<RainSystemDrawingData>();
            this.DrawingDatas = drawingDatas;

            var sb = new StringBuilder(8192);
            for (int storeyI = 0; storeyI < cadDatas.Count; storeyI++)
            {
                var drData = new RainSystemDrawingData();
                drawingDatas.Add(drData);
                {
                    var s = storeys[storeyI];
                    sb.AppendLine("楼层");
                    sb.AppendLine(s.Storeys.ToJson());
                    sb.AppendLine(s.StoreyType.ToString());
                }
                {
                    var s = geoData.Storeys[storeyI];
                    var e = DU.DrawRectLazy(s);
                    e.ColorIndex = 1;
                }
                var item = cadDatas[storeyI];
                {
                    var wantedLabels = new List<string>();
                    foreach (var pl in item.Labels)
                    {
                        var j = cadDataMain.Labels.IndexOf(pl);
                        var m = geoData.Labels[j];
                        if (IsWantedText(m.Text))
                        {
                            wantedLabels.Add(m.Text);
                        }
                    }
                    sb.AppendLine("立管");
                    sb.AppendLine(ThRainSystemService.GetRoofLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetBalconyLabels(wantedLabels).ToJson());
                    sb.AppendLine(ThRainSystemService.GetCondenseLabels(wantedLabels).ToJson());
                    drData.RoofLabels.AddRange(ThRainSystemService.GetRoofLabels(wantedLabels));
                    drData.BalconyLabels.AddRange(ThRainSystemService.GetBalconyLabels(wantedLabels));
                    drData.CondenseLabels.AddRange(ThRainSystemService.GetCondenseLabels(wantedLabels));
                    drData.CommentLabels.AddRange(wantedLabels.Where(x => ThRainSystemService.HasGravityLabelConnected(x)));
                }
                {
                    //凭空生成一个雨水口
                    if (false)
                    {
                        var f = GeometryFac.CreateGeometrySelector(item.LabelLines);
                        foreach (var lb in item.Labels)
                        {
                            var j = cadDataMain.Labels.IndexOf(lb);
                            var m = geoData.Labels[j];
                            var label = m.Text;
                            if (IsWaterPortLabel(label))
                            {
                                var ok_ents = new HashSet<Geometry>();
                                var lines = f(lb);
                                if (lines.Count == 1)
                                {
                                    var line1 = lines[0];
                                    ok_ents.Add(line1);
                                    lines = f(line1).Except(ok_ents).ToList();
                                    if (lines.Count == 1)
                                    {
                                        var line2 = lines[0];
                                        ok_ents.Add(line2);
                                        lines = f(line2).Except(ok_ents).ToList();
                                        if (lines.Count == 1)
                                        {
                                            var line3 = lines[0];
                                            ok_ents.Add(line3);
                                            var seg2 = geoData.LabelLines[cadDataMain.LabelLines.IndexOf(line2)];
                                            var seg3 = geoData.LabelLines[cadDataMain.LabelLines.IndexOf(line3)];
                                            var pt = ThRainSystemService.GetTargetPoint(seg2, seg3);
                                            var r = GRect.Create(pt, 50);
                                            geoData.WaterPortSymbols.Add(r);
                                            var pl = r.ToPolygon();
                                            cadDataMain.WaterPortSymbols.Add(pl);
                                            item.WaterPortSymbols.Add(pl);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    {
                        var gs = GeometryFac.GroupGeometries(item.LabelLines).Where(g => g.Count >= 3).ToList();
                        var labels = new List<Geometry>();
                        foreach (var lb in item.Labels)
                        {
                            var j = cadDataMain.Labels.IndexOf(lb);
                            var m = geoData.Labels[j];
                            var label = m.Text;
                            if (IsWaterPortLabel(label))
                            {
                                labels.Add(lb);
                            }
                        }
                        var f = GeometryFac.CreateGeometrySelector(item.LabelLines);
                        foreach (var lb in labels)
                        {
                            var _lines = f(GRect.Create(lb.EnvelopeInternal.ToGRect().Center, 100).ToPolygon());
                            if (_lines.Count == 1)
                            {
                                var firstLine = _lines[0];
                                var g = gs.FirstOrDefault(g => g.Contains(firstLine));
                                if (g != null)
                                {
                                    var segs = g.SelectInts(cadDataMain.LabelLines).ToList().Select(geoData.LabelLines).ToList();
                                    var h = GeometryFac.LineGrouppingHelper.Create(segs);
                                    h.InitPointGeos(radius: 10);
                                    h.DoGroupingByPoint();
                                    h.CalcAlonePoints();
                                    var pointGeos = h.AlonePoints;
                                    {
                                        var hLabelLines = item.LabelLines.SelectInts(cadDataMain.LabelLines).ToList().Select(geoData.LabelLines).Where(seg => seg.IsHorizontal(5)).ToList();
                                        var lst = item.Labels.Distinct().ToList();
                                        lst.Remove(lb);
                                        lst.AddRange(hLabelLines.Select(seg => seg.Buffer(10)));
                                        lst.Add(firstLine);
                                        lst.AddRange(item.VerticalPipes);
                                        lst.AddRange(item.WaterPort13s);
                                        lst.AddRange(item.WaterWells);
                                        lst.AddRange(item.WaterPortSymbols);
                                        var _geo = GeometryFac.CreateGeometry(lst.Distinct().ToArray());
                                        pointGeos = pointGeos.Except(GeometryFac.CreateGeometrySelector(pointGeos)(_geo)).Distinct().ToList();
                                    }
                                    foreach (var geo in pointGeos)
                                    {
                                        var pt = geo.EnvelopeInternal.ToGRect().Center;
                                        var r = GRect.Create(pt, 50);
                                        geoData.WaterPortSymbols.Add(r);
                                        var pl = r.ToPolygon();
                                        cadDataMain.WaterPortSymbols.Add(pl);
                                        item.WaterPortSymbols.Add(pl);
                                    }
                                }
                            }
                        }

                    }

                }
                foreach (var o in item.LabelLines)
                {
                    var j = cadDataMain.LabelLines.IndexOf(o);
                    var m = geoData.LabelLines[j];
                    var e = DU.DrawLineSegmentLazy(m);
                    e.ColorIndex = 1;
                }

                List<List<Geometry>> labelLinesGroup;
                {
                    var gs = GeometryFac.GroupGeometries(item.LabelLines);

                    ////group labellines test
                    //foreach (var g in gs)
                    //{
                    //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                    //    r.Expand(3);
                    //    var pl = DU.DrawRectLazy(r);
                    //    pl.ColorIndex = 3;
                    //}

                    labelLinesGroup = gs;
                }

                foreach (var pl in item.Labels)
                {
                    var j = cadDataMain.Labels.IndexOf(pl);
                    var m = geoData.Labels[j];
                    var e = DU.DrawTextLazy(m.Text, m.Boundary.LeftButtom.ToPoint3d());
                    e.ColorIndex = 2;
                    var _pl = DU.DrawRectLazy(m.Boundary);
                    _pl.ColorIndex = 2;
                }
                foreach (var o in item.VerticalPipes)
                {
                    var j = cadDataMain.VerticalPipes.IndexOf(o);
                    var m = geoData.VerticalPipes[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 3;
                }
                foreach (var o in item.FloorDrains)
                {
                    var j = cadDataMain.FloorDrains.IndexOf(o);
                    var m = geoData.FloorDrains[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 6;
                }
                foreach (var o in item.CondensePipes)
                {
                    var j = cadDataMain.CondensePipes.IndexOf(o);
                    var m = geoData.CondensePipes[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 2;
                }
                foreach (var o in item.WaterWells)
                {
                    var j = cadDataMain.WaterWells.IndexOf(o);
                    var m = geoData.WaterWells[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 7;
                }
                foreach (var o in item.SideWaterBuckets)
                {
                    var j = cadDataMain.SideWaterBuckets.IndexOf(o);
                    var m = geoData.SideWaterBuckets[j];
                    var e = DU.DrawRectLazy(m);
                    e.ColorIndex = 7;
                    Dbg.ShowXLabel(m.Center, 100);
                }


                {
                    var cl = Color.FromRgb(0xff, 0x9f, 0x7f);
                    foreach (var o in item.WaterPortSymbols)
                    {
                        var j = cadDataMain.WaterPortSymbols.IndexOf(o);
                        var m = geoData.WaterPortSymbols[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                    foreach (var o in item.WaterPort13s)
                    {
                        var j = cadDataMain.WaterPort13s.IndexOf(o);
                        var m = geoData.WaterPort13s[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                }
                {
                    var cl = Color.FromRgb(0x91, 0xc7, 0xae);
                    foreach (var o in item.WrappingPipes)
                    {
                        var j = cadDataMain.WrappingPipes.IndexOf(o);
                        var m = geoData.WrappingPipes[j];
                        var e = DU.DrawRectLazy(m);
                        e.Color = cl;
                    }
                }
                var shortTranslatorLabels = new HashSet<string>();
                Dictionary<Geometry, string> lbDict = new Dictionary<Geometry, string>();

                {
                    var ok_ents = new HashSet<Geometry>();
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var lst = ToList(labelLines, labels, pipes);
                            var gs = GeometryFac.GroupGeometries(lst);
                            foreach (var g in gs)
                            {
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                {
                                    //过滤掉不正确的text
                                    var tmp = new List<Geometry>();
                                    foreach (var lb in _labels)
                                    {
                                        var label = geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text;
                                        if (ThRainSystemService.IsWantedLabelText(label) || label.StartsWith("YL"))
                                        {
                                            tmp.Add(lb);
                                        }
                                    }
                                    _labels = tmp;
                                }
                                if (_labels.Count == 1 && _pipes.Count == 1)
                                {
                                    var pp = _pipes[0];
                                    if (lbDict.ContainsKey(pp)) continue;
                                    var lb = _labels[0];
                                    var j = cadDataMain.Labels.IndexOf(lb);
                                    var m = geoData.Labels[j];
                                    var label = m.Text;
                                    lbDict[pp] = label;
                                    //OK，识别成功
                                    ok_ents.Add(pp);
                                    ok_ents.Add(lb);
                                    break;
                                }
                            }
                        }
                    }
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var lst = ToList(labelLines, labels, pipes);
                            var gs = GeometryFac.GroupGeometries(lst);
                            foreach (var g in gs)
                            {
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_labels.Count == 1 && _pipes.Count == 2)
                                {
                                    var pp1 = _pipes[0];
                                    var pp2 = _pipes[1];
                                    {
                                        if (!lbDict.ContainsKey(pp1))
                                        {
                                            var lb = _labels[0];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp1] = label;
                                            ok_ents.Add(pp1);
                                        }
                                        if (!lbDict.ContainsKey(pp2))
                                        {
                                            var lb = _labels[0];
                                            var j = cadDataMain.Labels.IndexOf(lb);
                                            var m = geoData.Labels[j];
                                            var label = m.Text;
                                            lbDict[pp2] = label;
                                            ok_ents.Add(pp2);
                                        }
                                        shortTranslatorLabels.Add(lbDict[pp1]);
                                    }
                                }
                            }
                        }
                    }
                    //上面的提取一遍，然后再提取一遍
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var gs = GeometryFac.GroupGeometries(ToList(labelLines, labels, pipes));
                            foreach (var g in gs)
                            {
                                //{
                                //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                                //    r.Expand(3);
                                //    var pl = DU.DrawRectLazy(r);
                                //    pl.ColorIndex = 3;
                                //}
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_pipes.Count == _labels.Count)
                                {
                                    //foreach (var pp in pps)
                                    //{
                                    //    DU.DrawTextLazy("xx", pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    //}
                                    _pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(_pipes).ToList();
                                    _labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(_labels).ToList();
                                    for (int k = 0; k < _pipes.Count; k++)
                                    {
                                        var pp = _pipes[k];
                                        var lb = _labels[k];
                                        var j = cadDataMain.Labels.IndexOf(lb);
                                        var m = geoData.Labels[j];
                                        var label = m.Text;
                                        lbDict[pp] = label;
                                        //DU.DrawTextLazy(label, pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    }
                                    //OK，识别成功
                                    ok_ents.AddRange(_pipes);
                                    ok_ents.AddRange(_labels);
                                }
                                //这是原先识别短转管的代码，碰到某种case，会出问题，先注释掉
                                //else if (lbs.Count == 1)
                                //{
                                //    var lb = lbs[0];
                                //    var j = cadDataMain.Labels.IndexOf(lb);
                                //    var m = geoData.Labels[j];
                                //    var label = m.Text;
                                //    foreach (var pp in pps)
                                //    {
                                //        lbDict[pp] = label;
                                //        shortTranslatorLabels.Add(label);
                                //    }
                                //}
                            }
                        }
                    }

                    //再提取一遍
                    {
                        var labels = item.Labels.Except(ok_ents).ToList();
                        var pipes = item.VerticalPipes.Except(ok_ents).ToList();
                        foreach (var labelLines in labelLinesGroup)
                        {
                            var gs = GeometryFac.GroupGeometries(ToList(labelLines, labels, pipes));
                            foreach (var g in gs)
                            {
                                if (!g.Any(pl => labelLines.Contains(pl))) continue;
                                var _labels = g.Where(pl => labels.Contains(pl)).ToList();
                                var _pipes = g.Where(pl => pipes.Contains(pl)).ToList();
                                var _labelLines = g.Where(pl => labelLines.Contains(pl)).ToList();
                                if (!AllNotEmpty(_labels, _pipes, _labelLines)) continue;
                                if (_pipes.Count == _labels.Count)
                                {
                                    //foreach (var pp in pps)
                                    //{
                                    //    DU.DrawTextLazy("xx", pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    //}
                                    _pipes = ThRainSystemService.SortGeometrysBy2DSpacePosition(_pipes).ToList();
                                    _labels = ThRainSystemService.SortGeometrysBy2DSpacePosition(_labels).ToList();
                                    for (int k = 0; k < _pipes.Count; k++)
                                    {
                                        var pp = _pipes[k];
                                        var lb = _labels[k];
                                        var j = cadDataMain.Labels.IndexOf(lb);
                                        var m = geoData.Labels[j];
                                        var label = m.Text;
                                        lbDict[pp] = label;
                                        //DU.DrawTextLazy(label, pp.Bounds.ToGRect().LeftTop.ToPoint3d());
                                    }
                                    //OK，识别成功
                                    ok_ents.AddRange(_pipes);
                                    ok_ents.AddRange(_labels);
                                }
                            }
                        }
                    }
                }
                List<List<Geometry>> wLinesGroups;
                {
                    var gs = GeometryFac.GroupGeometries(item.WLines);
                    wLinesGroups = gs;

                    //var wlines = item.WLines;
                    //var gwlines = wlines.Select(wl => cadDataMain.WLines.IndexOf(wl)).Select(i => geoData.WLines[i]).ToList();
                    //var h = GeometryFac.LineGrouppingHelper.Create(gwlines);
                    //h.InitPointGeos(20);
                    //h.DoGroupingByPoint();
                    //{
                    //    var gs = h.GeoGroupsByPoint;
                    //    var pts = h.DoublePoints;
                    //    var gs2 = new List<List<Geometry>>();
                    //    foreach (var g in gs)
                    //    {
                    //        var _g = new List<Geometry>();
                    //        foreach (var pt in g)
                    //        {
                    //            var i = pts.IndexOf(pt);
                    //            _g.Add(wlines[i]);
                    //        }
                    //        gs2.Add(_g);
                    //    }
                    //    wLinesGroups = gs2;
                    //}

                    //foreach (var g in gs)
                    //{
                    //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                    //    r.Expand(3);
                    //    var pl = DU.DrawRectLazy(r);
                    //    pl.ColorIndex = 3;
                    //}

                }
                foreach (var o in item.WLines)
                {
                    var j = cadDataMain.WLines.IndexOf(o);
                    var m = geoData.WLines[j];
                    var e = DU.DrawLineSegmentLazy(m);
                    e.ColorIndex = 4;
                    //if (m.IsVertical(5))
                    //{
                    //    var ee = DU.DrawGeometryLazy(m);
                    //    ee.ColorIndex = 4;
                    //    ee.ConstantWidth = 100;
                    //}
                    //{
                    //    //DU.DrawTextLazy(m.AngleDegree.ToString(),100, m.StartPoint.ToPoint3d());
                    //}
                }

                var longTranslatorLabels = new HashSet<string>();
                {
                    foreach (var wlines in wLinesGroups)
                    {
                        var gs = GeometryFac.GroupGeometries(ToList(wlines, item.VerticalPipes));
                        foreach (var g in gs)
                        {
                            var _pipes = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                            if (!AllNotEmpty(_pipes, _wlines)) continue;

                            var pps1 = _pipes.Where(x => lbDict.ContainsKey(x)).ToList();
                            var pps2 = _pipes.Where(x => !lbDict.ContainsKey(x)).ToList();
                            if (pps1.Count == 1 && pps2.Count == 1)
                            {
                                var pp1 = pps1[0];
                                var pp2 = pps2[0];
                                //两根立管都要与wline相连才行
                                bool test(Geometry pipe)
                                {
                                    var lst = ToList(_wlines);
                                    lst.Add(pipe);
                                    var _gs = GeometryFac.GroupGeometries(lst);
                                    foreach (var _g in _gs)
                                    {
                                        if (!_g.Contains(pipe)) continue;
                                        var __wlines = _g.Where(pl => _wlines.Contains(pl)).ToList();
                                        if (!AllNotEmpty(__wlines)) continue;
                                        return true;
                                    }
                                    return false;
                                }
                                if (test(pp1) && test(pp2))
                                {
                                    var label = lbDict[pp1];
                                    lbDict[pp2] = label;
                                    //连线的长度小于等于300。而且连线只有一条直线的情况是短转管
                                    var isShort = false;
                                    {
                                        var lst = ToList(_wlines);
                                        lst.Add(pp1);
                                        lst.Add(pp2);
                                        var _gs = GeometryFac.GroupGeometries(lst).Where(_g => _g.Count == 3 && _g.Contains(pp1) && _g.Contains(pp2)).ToList();
                                        foreach (var _g in _gs)
                                        {
                                            var __wlines = _g.Where(pl => _wlines.Contains(pl)).ToList();
                                            if (__wlines.Count == 1)
                                            {
                                                var gWLine = geoData.WLines[cadDataMain.WLines.IndexOf(__wlines[0])];
                                                if (gWLine.Length <= 300)
                                                {
                                                    isShort = true;
                                                    shortTranslatorLabels.Add(label);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    //然后才是长转管
                                    if (!isShort) longTranslatorLabels.Add(label);
                                }
                            }
                        }
                    }
                }
                {
                    //临时修复wline中间被其他wline横插一脚的情况
                    foreach (var wline in item.WLines)
                    {
                        var lst = ToList(item.VerticalPipes);
                        lst.Add(wline);
                        var gs = GeometryFac.GroupGeometries(lst);
                        foreach (var g in gs)
                        {
                            if (!g.Contains(wline)) continue;
                            var _pipes = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            if (!AllNotEmpty(_pipes)) continue;

                            var pps1 = _pipes.Where(x => lbDict.ContainsKey(x)).ToList();
                            var pps2 = _pipes.Where(x => !lbDict.ContainsKey(x)).ToList();
                            if (pps1.Count == 1 && pps2.Count == 1)
                            {
                                var pp1 = pps1[0];
                                var pp2 = pps2[0];
                                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(wline);
                                //两根立管都要与wline相连才行
                                if (gf.Intersects(pp1) && gf.Intersects(pp2))
                                {
                                    var label = lbDict[pp1];
                                    lbDict[pp2] = label;
                                    //连线的长度小于等于300。而且连线只有一条直线的情况是短转管
                                    var isShort = false;
                                    if (geoData.WLines[cadDataMain.WLines.IndexOf(wline)].Length <= 300)
                                    {
                                        isShort = true;
                                        shortTranslatorLabels.Add(label);
                                    }
                                    //然后才是长转管
                                    if (!isShort) longTranslatorLabels.Add(label);
                                }
                            }
                        }
                    }
                }


                {
                    //var pps = new List<GRect>();
                    //foreach (var o in item.VerticalPipes)
                    //{
                    //    var j = cadData.VerticalPipes.IndexOf(o);
                    //    var m = geoData.VerticalPipes[j];
                    //    pps.Add(m);
                    //}
                    GRect getRect(Geometry o)
                    {
                        return geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(o)];
                    }
                    ThRainSystemService.Triangle(item.VerticalPipes, (_pp1, _pp2) =>
                    {
                        Geometry pp1, pp2;
                        if (lbDict.ContainsKey(_pp1) && !lbDict.ContainsKey(_pp2))
                        {
                            pp1 = _pp1; pp2 = _pp2;
                        }
                        else if (!lbDict.ContainsKey(_pp1) && lbDict.ContainsKey(_pp2))
                        {
                            pp1 = _pp2; pp2 = _pp1;
                        }
                        else
                        {
                            return;
                        }
                        var r1 = getRect(pp1);
                        var r2 = getRect(pp2);
                        if (r1.Center.GetDistanceTo(r2.Center) < r1.OuterRadius + r2.OuterRadius + 5)
                        {
                            var label = lbDict[pp1];
                            lbDict[pp2] = label;
                            shortTranslatorLabels.Add(label);
                        }
                    });
                }
                {
                    var pipes = item.VerticalPipes.Where(p => lbDict.ContainsKey(p)).ToList();
                    ThRainSystemService.Triangle(pipes, (p1, p2) =>
                    {
                        if (lbDict[p1] != lbDict[p2]) return;
                        var label = lbDict[p1];
                        if (p1.EnvelopeInternal.ToGRect().Center.GetDistanceTo(p2.EnvelopeInternal.ToGRect().Center) <= 300)
                        {
                            longTranslatorLabels.Remove(label);
                            shortTranslatorLabels.Add(label);
                        }
                    });
                }

                var _longTranslatorLabels = longTranslatorLabels.Distinct().ToList();
                _longTranslatorLabels.Sort();
                sb.AppendLine("长转管:" + _longTranslatorLabels.JoinWith(","));
                drData.LongTranslatorLabels.AddRange(_longTranslatorLabels);

                var _shortTranslatorLabels = shortTranslatorLabels.ToList();
                _shortTranslatorLabels.Sort();
                sb.AppendLine("短转管:" + _shortTranslatorLabels.JoinWith(","));
                drData.ShortTranslatorLabels.AddRange(_shortTranslatorLabels);

                #region 地漏
                //var floorDrainsLabelAndCount = new CountDict<string>();
                var floorDrainsLabelAndEnts = new ListDict<string, Geometry>();
                var floorDrainsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                {
                    //foreach (var group in wLinesGroups)
                    {
                        //var gs =GeometryFac.GroupGeometries(ToList(group, item.VerticalPipes, item.FloorDrains));
                        var gs = GeometryFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.FloorDrains));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var fds = g.Where(pl => item.FloorDrains.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                            //var wrappingPipes = g.Where(pl => item.WrappingPipes.Contains(pl)).ToList();
                            var wrappingPipes = new List<Geometry>();
                            {
                                var f = GeometryFac.CreateGeometrySelector(item.WrappingPipes);
                                foreach (var wline in wlines)
                                {
                                    wrappingPipes.AddRange(f(wline));
                                }
                                wrappingPipes = wrappingPipes.Distinct().ToList();
                            }

                            if (!AllNotEmpty(pps, fds, wlines)) continue;

                            //{
                            //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                            //    r.Expand(10);
                            //    var pl = DU.DrawRectLazy(r);
                            //    pl.ColorIndex = 1;
                            //}

                            foreach (var pp in pps)
                            {
                                //新的逻辑
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label == null) continue;

                                    {
                                        var lst = ToList(wlines, fds);
                                        lst.Add(pp);
                                        var _gs = GeometryFac.GroupGeometries(lst);
                                        foreach (var _g in _gs)
                                        {
                                            if (!_g.Contains(pp)) continue;
                                            var _fds = g.Where(pl => fds.Contains(pl)).ToList();
                                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                                            if (!AllNotEmpty(_fds, _wlines)) continue;
                                            {
                                                //pipe和wline不相交的情况，跳过
                                                var f = GeometryFac.CreateGeometrySelector(_wlines);
                                                if (f(pp).Count == 0) continue;
                                            }
                                            foreach (var fd in _fds)
                                            {
                                                floorDrainsLabelAndEnts.Add(label, fd);
                                            }
                                            {
                                                //套管还要在wline上才行
                                                var _wrappingPipes = new List<Geometry>();
                                                var f = GeometryFac.CreateGeometrySelector(wrappingPipes);
                                                foreach (var wline in _wlines)
                                                {
                                                    _wrappingPipes.AddRange(f(wline));
                                                }
                                                _wrappingPipes = wrappingPipes.Distinct().ToList();
                                                foreach (var wp in _wrappingPipes)
                                                {
                                                    floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                                }
                                            }
                                        }
                                    }
                                    if (false)
                                    {
                                        var lst = ToList(wlines, fds, wrappingPipes);
                                        lst.Add(pp);
                                        var _gs = GeometryFac.GroupGeometries(lst);
                                        foreach (var _g in _gs)
                                        {
                                            var _fds = g.Where(pl => fds.Contains(pl)).ToList();
                                            var _wlines = g.Where(pl => wlines.Contains(pl)).ToList();
                                            var _wrappingPipes = g.Where(pl => wrappingPipes.Contains(pl)).ToList();
                                            var _pps = g.Where(pl => pl == pp).ToList();
                                            if (!AllNotEmpty(_fds, _wlines, _pps)) continue;
                                            {
                                                //pipe和wline不相交的情况，跳过
                                                var f = GeometryFac.CreateGeometrySelector(_wlines);
                                                if (f(pp).Count == 0) continue;
                                            }
                                            foreach (var fd in _fds)
                                            {
                                                floorDrainsLabelAndEnts.Add(label, fd);
                                            }
                                            {
                                                //套管还要在wline上才行
                                                var __gs = GeometryFac.GroupGeometries(ToList(_wrappingPipes, _wlines));
                                                foreach (var __g in __gs)
                                                {
                                                    var __wlines = __g.Where(pl => _wlines.Contains(pl)).ToList();
                                                    var wps = __g.Where(pl => _wrappingPipes.Contains(pl)).ToList();
                                                    if (!AllNotEmpty(wps, __wlines)) continue;
                                                    foreach (var wp in wps)
                                                    {
                                                        floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                ////原先的逻辑
                                //if (false)
                                //{
                                //    lbDict.TryGetValue(pp, out string label);
                                //    if (label != null)
                                //    {
                                //        //floorDrainsLabelAndCount[label] += fds.Count;
                                //        foreach (var fd in fds)
                                //        {
                                //            floorDrainsLabelAndEnts.Add(label, fd);
                                //        }
                                //        //foreach (var wp in wrappingPipes)
                                //        //{
                                //        //    floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                //        //}
                                //        //套管还要在wline上才行
                                //        var _gs = GeometryFac.GroupGeometries(ToList(wrappingPipes, item.WLines));
                                //        foreach (var _g in _gs)
                                //        {
                                //            var _wlines = _g.Where(pl => item.WLines.Contains(pl)).ToList();
                                //            var wps = _g.Where(pl => item.WrappingPipes.Contains(pl)).ToList();
                                //            if (!AllNotEmpty(wps, _wlines)) continue;
                                //            foreach (var wp in wps)
                                //            {
                                //                floorDrainsWrappingPipesLabelAndEnts.Add(label, wp);
                                //            }
                                //        }
                                //    }
                                //}
                            }
                        }
                    }
                }
                //sb.AppendLine("地漏:" + floorDrainsLabelAndCount.Select(kv => $"{kv.Key}({kv.Value})").JoinWith(","));
                sb.AppendLine("地漏:" + floorDrainsLabelAndEnts
                    .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                sb.AppendLine("地漏套管:" + floorDrainsWrappingPipesLabelAndEnts
       .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                foreach (var kv in floorDrainsLabelAndEnts)
                {
                    drData.FloorDrains.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                foreach (var kv in floorDrainsWrappingPipesLabelAndEnts)
                {
                    drData.FloorDrainsWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                #endregion

                #region 冷凝管
                var condensePipesLabelAndEnts = new ListDict<string, Geometry>();
                {
                    //foreach (var group in wLinesGroups)
                    {
                        //var gs =GeometryFac.GroupGeometries(ToList(group, item.VerticalPipes, item.CondensePipes));
                        var gs = GeometryFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.CondensePipes));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                            var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                            if (!AllNotEmpty(pps, cps, wlines)) continue;

                            if (pps.Count != 1) continue;
                            //{
                            //    var r = GeoAlgorithm.GetEntitiesBoundaryRect(g);
                            //    r.Expand(10);
                            //    var pl = DU.DrawRectLazy(r);
                            //    pl.ColorIndex = 5;
                            //}
                            var pp = pps[0];
                            lbDict.TryGetValue(pp, out string label);
                            if (label != null)
                            {
                                //floorDrainsLabelAndCount[label] += fds.Count;
                                foreach (var cp in cps)
                                {
                                    condensePipesLabelAndEnts.Add(label, cp);
                                }
                            }
                        }
                    }
                }
                //sb.AppendLine("冷凝管:" + condensePipesLabelAndEnts
                //   .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));

                //生成辅助线
                var brokenCondensePipeLines = new List<GLineSegment>();
                {
                    var wlines = item.WLines.Select(o => geoData.WLines[cadDataMain.WLines.IndexOf(o)]).ToList();
                    var li = wlines.SelectInts(wl => wl.IsHorizontal(5)).ToList();
                    ThRainSystemService.Triangle(li, (i, j) =>
                    {
                        var kvs = GeoAlgorithm.YieldPoints(wlines[i], wlines[j]).ToList();
                        var pts = kvs.Flattern().ToList();
                        var tol = 5;
                        var _y = pts[0].Y;
                        if (pts.All(pt => GeoAlgorithm.InRange(pt.Y, _y, tol)))
                        {
                            var dis = kvs.Select(kv => kv.Key.GetDistanceTo(kv.Value)).Min();
                            if (dis > 100 && dis < 300)
                            {
                                var x1 = pts.Select(pt => pt.X).Min();
                                var x2 = pts.Select(pt => pt.X).Max();
                                var newSeg = new GLineSegment(x1, _y, x2, _y);
                                if (newSeg.Length > 0) brokenCondensePipeLines.Add(newSeg);
                                //var pl=DU.DrawGeometryLazy(newSeg);
                                //pl.ConstantWidth = 20;
                            }
                        }
                    });
                }
                //收集断开的冷凝管
                var brokenCondensePipes = new List<List<Geometry>>();
                {
                    var bkCondensePipeLines = brokenCondensePipeLines.Select(seg => seg.Buffer(10)).ToList();
                    var gs = GeometryFac.GroupGeometries(ToList(item.CondensePipes, bkCondensePipeLines));
                    foreach (var g in gs)
                    {
                        var cps = g.Where(pl => item.CondensePipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => bkCondensePipeLines.Contains(pl)).ToList();
                        if (!AllNotEmpty(cps, wlines)) continue;
                        if (cps.Count < 2) continue;
                        brokenCondensePipes.Add(cps);
                    }
                }
                {
                    IEnumerable<KeyValuePair<string, KeyValuePair<int, bool>>> GetCondensePipesData()
                    {
                        foreach (var kv in condensePipesLabelAndEnts)
                        {
                            List<Geometry> f()
                            {
                                var lst = kv.Value.ToList();
                                foreach (var cp1 in lst)
                                {
                                    foreach (var lst2 in brokenCondensePipes)
                                    {
                                        foreach (var cp2 in lst2)
                                        {
                                            if (cp1 == cp2)
                                            {
                                                return lst2;
                                            }
                                        }
                                    }
                                }
                                return null;
                            }
                            var ret = f();
                            if (ret == null)
                            {
                                //yield return $"{kv.Key}({kv.Value.Count},非断开)";
                                yield return new KeyValuePair<string, KeyValuePair<int, bool>>(kv.Key, new KeyValuePair<int, bool>(kv.Value.Count, false));
                            }
                            else
                            {
                                //yield return $"{kv.Key}({ret.Count},断开)";
                                yield return new KeyValuePair<string, KeyValuePair<int, bool>>(kv.Key, new KeyValuePair<int, bool>(ret.Count, true));
                            }
                        }
                    }
                    var lst = GetCondensePipesData().ToList();
                    sb.AppendLine("冷凝管:" + lst.Select(kv => kv.Value.Value ? $"{kv.Key}({kv.Value.Value},断开)" : $"{kv.Key}({kv.Value.Value},非断开)").JoinWith(","));
                    drData.CondensePipes.AddRange(lst);
                }

                #endregion

                var waterWellsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                var rainPortsWrappingPipesLabelAndEnts = new ListDict<string, Geometry>();
                var outputDict = new Dictionary<string, RainOutputTypeEnum>();

                bool ok = false;
                void CollectOutputs(List<Geometry> item_WLines, List<Geometry> item_WaterWells,
                    List<Geometry> item_WrappingPipes, List<Geometry> item_WaterPortSymbols, List<Geometry> item_VerticalPipes)
                {
                    if (ok)
                    {
                        foreach (var geo in item_WLines)
                        {
                            DU.DrawGeometryLazy(geo);
                        }
                    }
                    var wlinesGeo = new List<Geometry>();
                    {
                        var gs = GeometryFac.GroupGeometries(item_WLines);
                        foreach (var g in gs)
                        {
                            wlinesGeo.Add(GeometryFac.CreateGeometry(g.ToArray()));
                        }
                    }
                    var filtWLines = GeometryFac.CreateGeometrySelector(wlinesGeo);
                    {
                        var f1 = GeometryFac.CreateGeometrySelector(item_WrappingPipes);
                        var f2 = GeometryFac.CreateGeometrySelector(item_WaterPortSymbols);
                        var gs = GeometryFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, item_WaterPortSymbols));
                        foreach (var g in gs)
                        {
                            var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                            var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                            var symbols = g.Where(pl => item_WaterPortSymbols.Contains(pl)).ToList();
                            if (!AllNotEmpty(pps, wlines, symbols)) continue;
                            foreach (var pp in pps)
                            {
                                lbDict.TryGetValue(pp, out string label);
                                if (label != null)
                                {
                                    if (outputDict.ContainsKey(label)) continue;
                                    outputDict[label] = RainOutputTypeEnum.RainPort;
                                    var lst = new List<Geometry>();
                                    lst.Add(pp);
                                    lst.AddRange(wlines);
                                    var geo = GeometryFac.CreateGeometry(lst.ToArray());
                                    var wp = f1(geo).FirstOrDefault();
                                    if (wp != null)
                                    {
                                        rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                    }
                                }
                            }
                        }
                        foreach (var pipe in item_VerticalPipes)
                        {
                            void f()
                            {
                                lbDict.TryGetValue(pipe, out string label);
                                if (label != null)
                                {
                                    if (!outputDict.ContainsKey(label))
                                    {
                                        var wps = f1(pipe);
                                        if (wps.Count == 1)
                                        {
                                            var wp = wps[0];
                                            var wlines = filtWLines(wp);
                                            foreach (var wline in wlines)
                                            {
                                                if (f2(wline).Count > 0)
                                                {
                                                    outputDict[label] = RainOutputTypeEnum.RainPort;
                                                    rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }
                    {
                        var pipeLabelToWaterWellLabels = new List<KeyValuePair<string, string>>();
                        var ok_wells = new HashSet<Geometry>();
                        {
                            var gs = GeometryFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, item_WaterWells));
                            foreach (var g in gs)
                            {
                                var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                                var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                                var wells = g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                //var wrappingPipes = g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                if (!AllNotEmpty(pps, wlines, wells)) continue;
                                foreach (var pp in pps)
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label != null)
                                    {
                                        if (outputDict.ContainsKey(label)) continue;
                                        outputDict[label] = RainOutputTypeEnum.WaterWell;
                                        foreach (var w in wells)
                                        {
                                            var wellLabel = GeoData.WaterWellLabels[CadDataMain.WaterWells.IndexOf(w)];
                                            pipeLabelToWaterWellLabels.Add(new KeyValuePair<string, string>(label, wellLabel));
                                        }
                                        ok_wells.AddRange(wells);

                                        ////套管还要在wline上才行
                                        //var _lst = ToList(wrappingPipes, wlines);
                                        //_lst.Add(pp);
                                        //var _gs = GeometryFac.GroupGeometries(_lst);
                                        //foreach (var _g in _gs)
                                        //{
                                        //    if (!_g.Contains(pp)) continue;
                                        //    //var _wells = _g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                        //    var _wlines = _g.Where(pl => item_WLines.Contains(pl)).ToList();
                                        //    var wps = _g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                        //    if (!AllNotEmpty(wps, _wlines)) continue;
                                        //    foreach (var wp in wps)
                                        //    {
                                        //        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                        //    }
                                        //}
                                        {
                                            //检查是否有套管
                                            void f()
                                            {
                                                foreach (var wp in item_WrappingPipes)
                                                {
                                                    var lst = ToList(wlines);
                                                    lst.Add(wp);
                                                    lst.Add(pp);
                                                    var _gs = GeometryFac.GroupGeometries(lst);
                                                    foreach (var _g in _gs)
                                                    {
                                                        if (!_g.Contains(wp)) continue;
                                                        if (!_g.Contains(pp)) continue;
                                                        if (!g.Any(pl => wlines.Contains(pl))) continue;
                                                        //OK,有套管
                                                        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                                        return;
                                                    }
                                                }
                                            }
                                            f();
                                        }


                                    }
                                }
                            }
                        }
                        {
                            var _wells = item_WaterWells.Except(ok_wells).ToList();
                            var gwells = _wells.Select(o => geoData.WaterWells[cadDataMain.WaterWells.IndexOf(o)]).ToList();
                            for (int k = 0; k < gwells.Count; k++)
                            {
                                gwells[k] = GRect.Create(gwells[k].Center, 1500);
                            }
                            var shadowWells = gwells.Select(r => r.ToLinearRing()).Cast<Geometry>().ToList();
                            var gs = GeometryFac.GroupGeometries(ToList(item_WLines, item_VerticalPipes, shadowWells, item.WaterPort13s));
                            foreach (var g in gs)
                            {
                                var pps = g.Where(pl => item_VerticalPipes.Contains(pl)).ToList();
                                var wlines = g.Where(pl => item_WLines.Contains(pl)).ToList();
                                var wells = g.Where(pl =>
                                {
                                    var k = shadowWells.IndexOf(pl);
                                    if (k < 0) return false;
                                    return item_WaterWells.Contains(_wells[k]);
                                }).ToList();
                                //var wrappingPipes = g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();

                                if (!AllNotEmpty(pps, wlines, wells)) continue;
                                foreach (var pp in pps)
                                {
                                    lbDict.TryGetValue(pp, out string label);
                                    if (label != null)
                                    {
                                        if (outputDict.ContainsKey(label)) continue;
                                        outputDict[label] = RainOutputTypeEnum.WaterWell;
                                        foreach (var w in wells)
                                        {
                                            var wellLabel = GeoData.WaterWellLabels[CadDataMain.WaterWells.IndexOf(_wells[shadowWells.IndexOf(w)])];
                                            pipeLabelToWaterWellLabels.Add(new KeyValuePair<string, string>(label, wellLabel));
                                        }
                                        ok_wells.AddRange(wells);

                                        ////检查是否有套管
                                        //if (wrappingPipes.Count > 0)
                                        //{
                                        //    //套管还要在wline上才行
                                        //    var _lst = ToList(wrappingPipes, wlines);
                                        //    _lst.Add(pp);
                                        //    var _gs = GeometryFac.GroupGeometries(_lst);
                                        //    foreach (var _g in _gs)
                                        //    {
                                        //        if (!_g.Contains(pp)) continue;
                                        //        //var _wells = _g.Where(pl => item_WaterWells.Contains(pl)).ToList();
                                        //        var _wlines = _g.Where(pl => item_WLines.Contains(pl)).ToList();
                                        //        var wps = _g.Where(pl => item_WrappingPipes.Contains(pl)).ToList();
                                        //        if (!AllNotEmpty(wps, _wlines)) continue;
                                        //        foreach (var wp in wps)
                                        //        {
                                        //            waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                        //        }
                                        //    }
                                        //}

                                        {
                                            //检查是否有套管
                                            void f()
                                            {
                                                foreach (var wp in item_WrappingPipes)
                                                {
                                                    var lst = ToList(wlines);
                                                    lst.Add(wp);
                                                    lst.Add(pp);
                                                    var _gs = GeometryFac.GroupGeometries(lst);
                                                    foreach (var _g in _gs)
                                                    {
                                                        if (!_g.Contains(wp)) continue;
                                                        if (!_g.Contains(pp)) continue;
                                                        if (!g.Any(pl => wlines.Contains(pl))) continue;
                                                        //OK,有套管
                                                        waterWellsWrappingPipesLabelAndEnts.Add(label, wp);
                                                        return;
                                                    }
                                                }
                                            }
                                            f();
                                        }
                                    }
                                }
                            }
                        }

                        sb.AppendLine("pipeLabelToWaterWellLabels：" + pipeLabelToWaterWellLabels.ToCadJson());
                        drData.PipeLabelToWaterWellLabels.AddRange(pipeLabelToWaterWellLabels);
                    }
                    {
                        var f1 = GeometryFac.CreateGeometrySelector(item_WrappingPipes);
                        var f3 = GeometryFac.CreateGeometrySelector(item_WaterPortSymbols);
                        foreach (var pipe in item_VerticalPipes)
                        {
                            lbDict.TryGetValue(pipe, out string label);
                            if (label != null)
                            {
                                if (!outputDict.ContainsKey(label))
                                {
                                    var wrappingPipes = f1(pipe);
                                    if (wrappingPipes.Count == 1)
                                    {
                                        var wp = wrappingPipes[0];
                                        var _wlines = filtWLines(wp);
                                        if (_wlines.Count == 1)
                                        {
                                            var wline = _wlines[0];
                                            var symbols = f3(wline);
                                            if (symbols.Count == 1)
                                            {
                                                var symbol = symbols[0];
                                                outputDict[label] = RainOutputTypeEnum.RainPort;
                                                rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        var f2 = GeometryFac.CreateGeometrySelector(item.WaterPort13s);
                        foreach (var pipe in item_VerticalPipes)
                        {
                            void f()
                            {
                                lbDict.TryGetValue(pipe, out string label);
                                if (label != null)
                                {
                                    if (!outputDict.ContainsKey(label))
                                    {
                                        foreach (var wlineG in filtWLines(pipe))
                                        {
                                            if (f2(wlineG).Any())
                                            {
                                                outputDict[label] = RainOutputTypeEnum.RainPort;
                                                foreach (var wp in f1(pipe).Concat(f1(wlineG).Distinct()))
                                                {
                                                    rainPortsWrappingPipesLabelAndEnts.Add(label, wp);
                                                }
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            f();
                        }
                    }
                }

                CollectOutputs(item.WLines, item.WaterWells, item.WrappingPipes, item.WaterPortSymbols, item.VerticalPipes);
                {
                    var wlines1 = item.WLines.Select(pl => geoData.WLines[cadDataMain.WLines.IndexOf(pl)]).ToList();
                    var rs = item.WaterWells.Select(pl => geoData.WaterWells[cadDataMain.WaterWells.IndexOf(pl)])
                        .Concat(item.WrappingPipes.Select(pl => geoData.WrappingPipes[cadDataMain.WrappingPipes.IndexOf(pl)]))
                        .Concat(item.WaterPortSymbols.Select(pl => geoData.WaterPortSymbols[cadDataMain.WaterPortSymbols.IndexOf(pl)]))
                        .Concat(item.VerticalPipes.Select(pl => geoData.VerticalPipes[cadDataMain.VerticalPipes.IndexOf(pl)]))
                        .ToList();

                    void f5(List<GLineSegment> segs)
                    {
                        var _wlines1 = wlines1.Select(seg => seg.Buffer(10)).ToList();
                        var f1 = GeometryFac.CreateGeometrySelector(_wlines1);
                        var _wlines2 = segs.Select(seg => seg.Extend(-20).Buffer(10)).ToList();
                        var _wlines3 = segs.Select(seg => seg.Extend(.1).Buffer(10)).ToList();
                        var f2 = GeometryFac.CreateGeometrySelector(_wlines2);
                        var _rs = item.WaterWells.Concat(item.WrappingPipes).Concat(item.WaterPortSymbols).Concat(item.VerticalPipes).Distinct().ToArray();
                        var __wlines2 = _wlines2.Except(f2(GeometryFac.CreateGeometry(_rs))).Distinct().ToList();
                        var __wlines3 = __wlines2.SelectInts(_wlines2).ToList().Select(_wlines3).ToList();
                        var wlines = _wlines1.Except(f1(GeometryFac.CreateGeometry(__wlines2.ToArray()))).Concat(__wlines3).Distinct().ToList();

                        //FengDbgTesting.AddLazyAction("看看自动连接OK不", adb =>
                        //{
                        //    Dbg.BuildAndSetCurrentLayer(adb.Database);
                        //    foreach (var wline in wlines)
                        //    {
                        //        DU.DrawEntitiesLazy(wline.ToDbObjects().OfType<Entity>().ToList());
                        //    }
                        //    DU.Draw();
                        //});
                        {
                            foreach (var wline in __wlines3)
                            {
                                DU.DrawEntitiesLazy(wline.ToDbObjects().OfType<Entity>().ToList());
                            }
                        }
                        CollectOutputs(wlines, item.WaterWells, item.WrappingPipes, item.WaterPortSymbols, item.VerticalPipes);
                    }
                    {
                        var segs = FengDbgTesting.GetSegsToConnect(wlines1, rs, 8000, radius: 15).Distinct().ToList();
                        //FengDbgTesting.AddLazyAction("看看自动连接OK不", adb =>
                        //{
                        //    Dbg.BuildAndSetCurrentLayer(adb.Database);
                        //    foreach (var seg in segs)
                        //    {
                        //        DU.DrawLineSegmentLazy(seg);
                        //    }
                        //    DU.Draw();
                        //});
                        f5(segs);
                    }
                    {
                        var segs = item.WLinesAddition.SelectInts(cadDataMain.WLinesAddition).ToList().Select(geoData.WLinesAddition).ToList();
                        ok = true;
                        f5(segs);
                    }
                }


                sb.AppendLine("排出方式：" + outputDict.ToCadJson());
                sb.AppendLine("雨水井套管:" + waterWellsWrappingPipesLabelAndEnts
    .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                sb.AppendLine("雨水口套管:" + rainPortsWrappingPipesLabelAndEnts
 .Where(kv => kv.Value.Count > 0).Select(kv => $"{kv.Key}({kv.Value.Distinct().Count()})").JoinWith(","));
                foreach (var kv in outputDict)
                {
                    drData.OutputTypes.Add(kv);
                }
                foreach (var kv in waterWellsWrappingPipesLabelAndEnts)
                {
                    drData.WaterWellWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }
                foreach (var kv in rainPortsWrappingPipesLabelAndEnts)
                {
                    drData.RainPortWrappingPipes.Add(new KeyValuePair<string, int>(kv.Key, kv.Value.Distinct().Count()));
                }

                var gravityWaterBucketTranslatorLabels = new List<string>();
                {
                    var gs = GeometryFac.GroupGeometries(ToList(item.WLines, item.VerticalPipes, item.GravityWaterBuckets));
                    foreach (var g in gs)
                    {
                        var pps = g.Where(pl => item.VerticalPipes.Contains(pl)).ToList();
                        var wlines = g.Where(pl => item.WLines.Contains(pl)).ToList();
                        var gbks = g.Where(pl => item.GravityWaterBuckets.Contains(pl)).ToList();
                        if (!AllNotEmpty(pps, wlines, gbks)) continue;

                        foreach (var pp in pps)
                        {
                            lbDict.TryGetValue(pp, out string label);
                            if (label != null)
                            {
                                gravityWaterBucketTranslatorLabels.Add(label);
                            }
                        }
                    }
                }
                gravityWaterBucketTranslatorLabels = gravityWaterBucketTranslatorLabels.Distinct().ToList();
                gravityWaterBucketTranslatorLabels.Sort();
                sb.AppendLine("重力雨水斗转管:" + gravityWaterBucketTranslatorLabels.JoinWith(","));
                drData.GravityWaterBucketTranslatorLabels.AddRange(gravityWaterBucketTranslatorLabels);


                {
                    //补丁：把部分重力雨水斗变成87雨水斗
                    var _87bks = new List<Geometry>();
                    var labels = item.Labels.Where(lb => geoData.Labels[cadDataMain.Labels.IndexOf(lb)].Text == "87型雨水斗").ToList();
                    foreach (var label in labels)
                    {
                        var lst = item.LabelLines.ToList();
                        lst.Add(label);
                        lst.AddRange(item.GravityWaterBuckets);
                        var gs = GeometryFac.GroupGeometries(lst);
                        foreach (var g in gs)
                        {
                            if (!g.Contains(label)) continue;
                            var labelLines = g.Where(pl => item.LabelLines.Contains(pl)).ToList();
                            var bks = g.Where(pl => item.GravityWaterBuckets.Contains(pl)).ToList();
                            if (!AllNotEmpty(labelLines, bks)) continue;
                            _87bks.AddRange(bks);
                        }
                    }
                    _87bks = _87bks.Distinct().ToList();
                    item._87WaterBuckets.AddRange(_87bks);
                    cadDataMain._87WaterBuckets.AddRange(_87bks);
                    geoData._87WaterBuckets.AddRange(_87bks.Select(pl => geoData.GravityWaterBuckets[cadDataMain.GravityWaterBuckets.IndexOf(pl)]));
                    foreach (var bk in _87bks)
                    {
                        item.GravityWaterBuckets.Remove(bk);
                        var i = cadDataMain.GravityWaterBuckets.IndexOf(bk);
                        cadDataMain.GravityWaterBuckets.RemoveAt(i);
                        geoData.GravityWaterBuckets.RemoveAt(i);
                    }
                    foreach (var o in item.GravityWaterBuckets)
                    {
                        var j = cadDataMain.GravityWaterBuckets.IndexOf(o);
                        var m = geoData.GravityWaterBuckets[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                        Dbg.ShowXLabel(m.Center, 200);
                    }
                    foreach (var o in item._87WaterBuckets)
                    {
                        var j = cadDataMain._87WaterBuckets.IndexOf(o);
                        var m = geoData._87WaterBuckets[j];
                        var e = DU.DrawRectLazy(m);
                        e.ColorIndex = 7;
                        Dbg.ShowXLabel(m.Center, 500);
                    }
                }

                {
                    //标出所有的立管编号（看看识别成功了没）
                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            DU.DrawTextLazy(label, pp.ToGRect().LeftTop.ToPoint3d());
                        }
                    }

                    foreach (var pp in item.VerticalPipes)
                    {
                        lbDict.TryGetValue(pp, out string label);
                        if (label != null)
                        {
                            var r = GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(pp)];
                            drData.VerticalPipes.Add(new KeyValuePair<string, GRect>(label, r));
                        }
                    }
                }
            }

            if (ShouldPrintReadableDrawingData) Dbg.PrintText(sb.ToString());
        }
        static bool ShouldPrintReadableDrawingData = true;
        //static bool ShouldPrintReadableDrawingData = false;

        public void AddPipeRunsForRF<T>(string roofPipeNote, T sys) where T : ThWRainPipeSystem
        {
            var runs = sys.PipeRuns;
            var WSDStoreys = RainSystemDiagram.WSDStoreys;
            bool HasGravityLabelConnected(GRect bd, string pipeId)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var drData = DrawingDatas[i];
                    return drData.GravityWaterBucketTranslatorLabels.Contains(pipeId);
                }
                return false;
            }
            List<Extents3d> GetRelatedGravityWaterBucket(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i].GravityWaterBuckets.Select(o => GeoData.GravityWaterBuckets[CadDataMain.GravityWaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            List<Extents3d> GetRelated87WaterBucket(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i]._87WaterBuckets.Select(o => GeoData._87WaterBuckets[CadDataMain._87WaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            List<Extents3d> GetSideWaterBucketsInRange(GRect bd)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    return CadDatas[i].SideWaterBuckets.Select(o => GeoData.SideWaterBuckets[CadDataMain.SideWaterBuckets.IndexOf(o)]).Select(x => x.ToExtents2d().ToExtents3d()).ToList();
                }
                return new List<Extents3d>();
            }
            WaterBucketEnum GetRelatedSideWaterBucket(Point3d center)
            {
                var p = center.ToPoint2d();
                foreach (var bd in GeoData.SideWaterBuckets)
                {
                    if (bd.ContainsPoint(p)) return WaterBucketEnum.Side;
                }
                return WaterBucketEnum.None;
            }
            bool GetCenterOfVerticalPipe(GRect bd, string label, ref Point3d center)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var lst = CadDatas[i].VerticalPipes.Select(o => GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(o)]);
                    var drData = DrawingDatas[i];
                    foreach (var kv in drData.VerticalPipes)
                    {
                        if (kv.Key == label)
                        {
                            center = kv.Value.Center.ToPoint3d();
                            return true;
                        }
                    }
                }
                return false;
            }
            TranslatorTypeEnum GetTranslatorType(GRect bd, string label)
            {
                var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(bd));
                if (storey != null)
                {
                    var i = Storeys.IndexOf(storey);
                    var lst = CadDatas[i].VerticalPipes.Select(o => GeoData.VerticalPipes[CadDataMain.VerticalPipes.IndexOf(o)]);
                    var drData = DrawingDatas[i];
                    if (drData.ShortTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Short;
                    if (drData.GravityWaterBucketTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Gravity;
                    if (drData.LongTranslatorLabels.Contains(label)) return TranslatorTypeEnum.Long;
                }
                return TranslatorTypeEnum.None;
            }
            foreach (var s in WSDStoreys)
            {
                if (s.Label == "RF+2" || s.Label == "RF+1")
                {
                    var matchedPipe = s.VerticalPipes.FirstOrDefault(vp => vp.Label == roofPipeNote);
                    if (matchedPipe != null) runs.Add(new ThWRainPipeRun() { Storey = s, MainRainPipe = matchedPipe });
                }
                else
                {
                    var translatorType = GetTranslatorType(s.Boundary, roofPipeNote);
                    if (translatorType == TranslatorTypeEnum.Gravity)
                    {
                        //重力雨水斗转管
                        sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s };
                    }
                }

                //var storey = Storeys.FirstOrDefault(s => s.Boundary.Equals(s.Boundary));
                //if (storey == null) continue;
                if (sys.WaterBucket.Storey == null)
                {
                    //water bucket, too slow
                    Point3d roofPipeCenter = new Point3d();
                    var ok = GetCenterOfVerticalPipe(s.Boundary, roofPipeNote, ref roofPipeCenter);

                    if (ok)
                    {
                        var waterBucketType = GetRelatedSideWaterBucket(roofPipeCenter);

                        //side
                        if (!waterBucketType.Equals(WaterBucketEnum.None))
                        {
                            if (s.VerticalPipes.Select(p => p.Label).Contains(roofPipeNote))
                            {
                                //Dbg.ShowWhere(roofPipeCenter);
                                sys.WaterBucket = new ThWSDWaterBucket() { Type = waterBucketType, Storey = s };
                                break;
                            }
                        }
                    }
                    //尝试通过对位得到侧入雨水斗
                    var allSideWaterBucketsInThisRange = GetSideWaterBucketsInRange(s.Boundary);

                    if (sys.WaterBucket.Storey == null && allSideWaterBucketsInThisRange.Count > 0)
                    {
                        var lowerStorey = RainSystemDiagram.GetLowerStorey(s);
                        if (lowerStorey != null)
                        {
                            Point3d roofPipeCenterInLowerStorey = new Point3d();
                            var brst2 = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote, ref roofPipeCenterInLowerStorey);
                            if (brst2)
                            {
                                var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                //compute ucs
                                foreach (var wbe in allSideWaterBucketsInThisRange)
                                {
                                    var minPt = wbe.MinPoint;
                                    var maxPt = wbe.MaxPoint;

                                    var basePt = s.Boundary.LeftTop.ToPoint3d();

                                    var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                    var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                    var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                    if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                    {
                                        sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Side, Storey = s, };
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    //gravity
                    if (sys.WaterBucket.Storey == null)
                    {
                        //尝试通过对位得到重力雨水斗
                        var allWaterBucketsInThisRange = GetRelatedGravityWaterBucket(s.Boundary);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            var lowerStorey = RainSystemDiagram.GetLowerStorey(s);
                            if (lowerStorey != null)
                            {
                                Point3d roofPipeCenterInLowerStorey = new Point3d();

                                var brst2 = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote, ref roofPipeCenterInLowerStorey);
                                if (brst2)
                                {
                                    var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                    var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                    //compute ucs
                                    foreach (var wbe in allWaterBucketsInThisRange)
                                    {
                                        var minPt = wbe.MinPoint;
                                        var maxPt = wbe.MaxPoint;

                                        var basePt = s.Boundary.LeftTop.ToPoint3d();

                                        var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                        var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                        var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                        if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                        {
                                            sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = s, };
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //87waterbucket
                    if (sys.WaterBucket.Storey == null)
                    {
                        //尝试通过对位得到87waterbucket
                        var allWaterBucketsInThisRange = GetRelated87WaterBucket(s.Boundary);
                        if (allWaterBucketsInThisRange.Count > 0)
                        {
                            var lowerStorey = RainSystemDiagram.GetLowerStorey(s);
                            if (lowerStorey != null)
                            {
                                Point3d roofPipeCenterInLowerStorey = new Point3d();

                                var brst2 = GetCenterOfVerticalPipe(lowerStorey.Boundary, roofPipeNote, ref roofPipeCenterInLowerStorey);
                                if (brst2)
                                {
                                    var lowerBasePt = lowerStorey.Boundary.LeftTop.ToPoint3d();
                                    var pipeCenterInLowerUcs = new Point3d(roofPipeCenterInLowerStorey.X - lowerBasePt.X, roofPipeCenterInLowerStorey.Y - lowerBasePt.Y, 0);

                                    //compute ucs
                                    foreach (var wbe in allWaterBucketsInThisRange)
                                    {
                                        var minPt = wbe.MinPoint;
                                        var maxPt = wbe.MaxPoint;

                                        var basePt = s.Boundary.LeftTop.ToPoint3d();

                                        var minPtInUcs = new Point3d(minPt.X - basePt.X, minPt.Y - basePt.Y, 0);
                                        var maxPtInUcs = new Point3d(maxPt.X - basePt.X, maxPt.Y - basePt.Y, 0);

                                        var extentInUcs = new Extents3d(minPtInUcs, maxPtInUcs);

                                        if (extentInUcs.IsPointIn(pipeCenterInLowerUcs))
                                        {
                                            sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum._87, Storey = s, };
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //for gravity bucket, still need to check label
                    //尝试通过 label 得到重力雨水斗
                    var hasWaterBucket = HasGravityLabelConnected(s.Boundary, roofPipeNote);
                    if (hasWaterBucket)
                    {
                        sys.WaterBucket = new ThWSDWaterBucket() { Type = WaterBucketEnum.Gravity, Storey = RainSystemDiagram.GetHigerStorey(s) };

                        runs.Add(new ThWRainPipeRun()
                        {
                            Storey = RainSystemDiagram.GetHigerStorey(s),
                        });
                        break;
                    }
                }
            }
        }
    }

    public class RainSystemDrawingData
    {
        public List<string> RoofLabels = new List<string>();
        public List<string> BalconyLabels = new List<string>();
        public List<string> CondenseLabels = new List<string>();
        public List<string> GetAllLabels()
        {
            return RoofLabels.Concat(BalconyLabels).Concat(CondenseLabels).Distinct().ToList();
        }
        public List<string> CommentLabels = new List<string>();
        public List<string> LongTranslatorLabels = new List<string>();
        public List<string> ShortTranslatorLabels = new List<string>();
        public List<string> GravityWaterBucketTranslatorLabels = new List<string>();
        public List<KeyValuePair<string, int>> FloorDrains = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> FloorDrainsWrappingPipes = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> WaterWellWrappingPipes = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> RainPortWrappingPipes = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, KeyValuePair<int, bool>>> CondensePipes = new List<KeyValuePair<string, KeyValuePair<int, bool>>>();
        public List<KeyValuePair<string, RainOutputTypeEnum>> OutputTypes = new List<KeyValuePair<string, RainOutputTypeEnum>>();
        public List<KeyValuePair<string, string>> PipeLabelToWaterWellLabels = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, GRect>> VerticalPipes = new List<KeyValuePair<string, GRect>>();
    }
    public static class GeoNTSConvertion
    {
        public static Coordinate[] ConvertToCoordinateArray(GRect gRect)
        {
            var pt = gRect.LeftTop.ToPoint3d().ToNTSCoordinate();
            return new Coordinate[]
{
pt,
gRect.RightTop.ToPoint3d().ToNTSCoordinate(),
gRect.RightButtom.ToPoint3d().ToNTSCoordinate(),
gRect.LeftButtom.ToPoint3d().ToNTSCoordinate(),
pt,
};
        }
    }
    public static class GeometryFac
    {
        static readonly NetTopologySuite.Index.Strtree.GeometryItemDistance itemDist = new NetTopologySuite.Index.Strtree.GeometryItemDistance();
        public static Func<Point3d, Geometry> NearestNeighbourPoint3dF(List<Geometry> geos)
        {
            if (geos.Count == 0) return geometry => null;
            else if (geos.Count == 1) return geometry => geos[0];
            var f = NearestNeighbourGeometryF(geos);
            return pt => f(pt.ToNTSPoint());
        }
        public static Func<Geometry, Geometry> NearestNeighbourGeometryF(List<Geometry> geos)
        {
            if (geos.Count == 0) return geometry => null;
            else if (geos.Count == 1) return geometry => geos[0];
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geometry => engine.NearestNeighbour(geometry.EnvelopeInternal, geometry, itemDist);
        }
        public static Func<Point3d, int, List<Geometry>> NearestNeighboursPoint3dF(List<Geometry> geos)
        {
            if (geos.Count == 0) return (pt, num) => new List<Geometry>();
            var f = NearestNeighboursGeometryF(geos);
            return (pt, num) => f(pt.ToNTSPoint(), num);
        }
        public static Func<Geometry, int, List<Geometry>> NearestNeighboursGeometryF(List<Geometry> geos)
        {
            if (geos.Count == 0) return (geometry, num) => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return (geometry, num) =>
            {
                if (num <= geos.Count) return geos.ToList();
                var neighbours = engine.NearestNeighbour(geometry.EnvelopeInternal, geometry, itemDist, num)
        .Where(o => !o.EqualsExact(geometry)).ToList();
                return neighbours;
            };
        }
        public static IEnumerable<KeyValuePair<int, int>> GroupGeometriesFast(List<Geometry> geos)
        {
            if (geos.Count == 0) yield break;
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            for (int i = 0; i < geos.Count; i++)
            {
                var geo = geos[i];
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.BinarySearch(g)).Where(j => i < j))
                {
                    yield return new KeyValuePair<int, int>(i, j);
                }
            }
        }
        public class LineGrouppingHelper
        {
            public List<GLineSegment> LineSegs;
            public List<Geometry> DoublePoints;
            public List<Geometry> Points1;
            public List<Geometry> Points2;
            public List<Geometry> Buffers;
            public List<List<Geometry>> GeoGroupsByPoint;
            public List<List<Geometry>> GeoGroupsByBuffer;
            private LineGrouppingHelper() { }
            public static LineGrouppingHelper Create(List<GLineSegment> lineSegs)
            {
                var o = new LineGrouppingHelper();
                o.LineSegs = lineSegs;
                return o;
            }
            public void DoGroupingByPoint()
            {
                GeoGroupsByPoint = GroupGeometries(DoublePoints);
            }
            public void DoGroupingByBuffer()
            {
                GeoGroupsByBuffer = GroupGeometries(Buffers);
            }
            public List<Geometry> AlonePoints;
            public void CalcAlonePoints()
            {
                var hs = new HashSet<Geometry>();
                var tokill = new HashSet<Geometry>();
                var lst = Points1.Concat(Points2).ToList();
                foreach (var r in lst)
                {
                    if (!hs.Contains(r))
                    {
                        hs.Add(r);
                    }
                    else
                    {
                        tokill.Add(r);
                    }
                }
                var gs = GroupGeometries(lst.Except(tokill).ToList());
                AlonePoints = gs.Where(g => g.Count == 1).Select(g => g[0]).ToList();
            }
            public bool[] IsAlone1;
            public bool[] IsAlone2;
            public IEnumerable<Geometry> YieldAloneRings()
            {
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i]) yield return Points1[i];
                    if (IsAlone2[i]) yield return Points2[i];
                }
            }
            public void DistinguishAloneRings()
            {
                IsAlone1 = new bool[LineSegs.Count];
                IsAlone2 = new bool[LineSegs.Count];
                foreach (var aloneRing in AlonePoints)
                {
                    int i;
                    i = Points1.IndexOf(aloneRing);
                    if (i >= 0)
                    {
                        IsAlone1[i] = true;
                        continue;
                    }
                    i = Points2.IndexOf(aloneRing);
                    if (i >= 0)
                    {
                        IsAlone2[i] = true;
                        continue;
                    }
                }
                AlonePoints = null;
            }
            public IEnumerable<GLineSegment> GetExtendedGLineSegmentsByFlags(double ext)
            {
                for (int i = 0; i < LineSegs.Count; i++)
                {
                    var b1 = IsAlone1[i];
                    var b2 = IsAlone2[i];
                    if (b1 && b2)
                    {
                        var seg = LineSegs[i];
                        //yield return seg.Extend(ext);
                        {
                            //var seg = LineSegs[i];
                            var sp = seg.StartPoint;
                            var ep = seg.EndPoint;
                            var vec = sp - ep;
                            if (vec.Length == 0) continue;
                            var k = ext / vec.Length;
                            ep = sp;
                            sp += vec * k;
                            yield return new GLineSegment(ep, sp);
                        }
                        {
                            //var seg = LineSegs[i];
                            var sp = seg.StartPoint;
                            var ep = seg.EndPoint;
                            var vec = ep - sp;
                            if (vec.Length == 0) continue;
                            var k = ext / vec.Length;
                            sp = ep;
                            ep += vec * k;
                            yield return new GLineSegment(sp, ep);
                        }
                    }
                    if (b1)
                    {
                        var seg = LineSegs[i];
                        var sp = seg.StartPoint;
                        var ep = seg.EndPoint;
                        var vec = sp - ep;
                        if (vec.Length == 0) continue;
                        var k = ext / vec.Length;
                        ep = sp;
                        sp += vec * k;
                        yield return new GLineSegment(ep, sp);
                    }
                    if (b2)
                    {
                        var seg = LineSegs[i];
                        var sp = seg.StartPoint;
                        var ep = seg.EndPoint;
                        var vec = ep - sp;
                        if (vec.Length == 0) continue;
                        var k = ext / vec.Length;
                        sp = ep;
                        ep += vec * k;
                        yield return new GLineSegment(sp, ep);
                    }
                }
            }
            public int GetAloneRingsCount()
            {
                var s = 0;
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i]) s++;
                    if (IsAlone2[i]) s++;
                }
                return s;
            }
            public void KillAloneRings(Geometry[] geos)
            {
                if (geos.Length == 0) return;
                var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
                for (int i = 0; i < IsAlone1.Length; i++)
                {
                    if (IsAlone1[i])
                    {
                        var geo = Points1[i];
                        engine.Insert(geo.EnvelopeInternal, geo);
                    }
                    if (IsAlone2[i])
                    {
                        var geo = Points2[i];
                        engine.Insert(geo.EnvelopeInternal, geo);
                    }
                }
                {
                    var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(geos);
                    var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                    foreach (var r in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)))
                    {
                        for (int i = 0; i < IsAlone1.Length; i++)
                        {
                            if (Points1[i] == r) IsAlone1[i] = false;
                            if (Points2[i] == r) IsAlone2[i] = false;
                        }
                    }
                }
            }
            public void InitBufferGeos(double dis)
            {
                Buffers = new List<Geometry>(LineSegs.Count);
                foreach (var seg in LineSegs)
                {
                    Buffers.Add(seg.Buffer(dis));
                }
            }
            public void InitPointGeos(double radius, int numPoints = 6)
            {
                DoublePoints = new List<Geometry>(LineSegs.Count);
                Points1 = new List<Geometry>(LineSegs.Count);
                Points2 = new List<Geometry>(LineSegs.Count);
                GeometricShapeFactory.NumPoints = numPoints;
                GeometricShapeFactory.Size = 2 * radius;
                foreach (var seg in LineSegs)
                {
                    GeometricShapeFactory.Centre = seg.StartPoint.ToNTSCoordinate();
                    //var point1 = GeometricShapeFactory.CreateCircle().Shell;
                    var point1 = GeometricShapeFactory.CreateCircle();
                    GeometricShapeFactory.Centre = seg.EndPoint.ToNTSCoordinate();
                    //var point2 = GeometricShapeFactory.CreateCircle().Shell;
                    var point2 = GeometricShapeFactory.CreateCircle();
                    var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { point1, point2 });
                    DoublePoints.Add(geo);
                    Points1.Add(point1);
                    Points2.Add(point2);
                }
            }
            public static List<GLineSegment> TryConnect(List<GLineSegment> segs, double dis)
            {
                var lst = new List<GLineSegment>();
                ThRainSystemService.Triangle(segs.Count, (i, j) =>
                {
                    var seg1 = segs[i];
                    var seg2 = segs[j];
                    var angleTol = 5;
                    if (seg1.IsHorizontal(angleTol) && seg2.IsHorizontal(angleTol) || seg1.IsVertical(angleTol) && seg2.IsVertical(angleTol))
                    {
                        if (GeoAlgorithm.GetMinConnectionDistance(seg1, seg2) < dis)
                        {
                            lst.Add(new GLineSegment(seg1.EndPoint, seg2.StartPoint));
                        }
                    }
                });
                return lst;
            }
        }
        public static readonly NetTopologySuite.Utilities.GeometricShapeFactory GeometricShapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory);
        public static List<Geometry> CreateGeometries(IList<GLineSegment> lineSegments, double radius, int numPoints = 6)
        {
            var ret = new List<Geometry>(lineSegments.Count);
            GeometricShapeFactory.NumPoints = numPoints;
            GeometricShapeFactory.Size = 2 * radius;
            foreach (var seg in lineSegments)
            {
                GeometricShapeFactory.Centre = seg.StartPoint.ToNTSCoordinate();
                var ring1 = GeometricShapeFactory.CreateCircle().Shell;
                GeometricShapeFactory.Centre = seg.EndPoint.ToNTSCoordinate();
                var ring2 = GeometricShapeFactory.CreateCircle().Shell;
                var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
                ret.Add(geo);
            }
            return ret;
        }

        public static IEnumerable<KeyValuePair<int, int>> GroupGLineSegmentsToKVIndex(IList<GLineSegment> lineSegments, double radius, int numPoints = 6)
        {
            var geos = CreateGeometries(lineSegments, radius, numPoints);
            return _GroupGeometriesToKVIndex(geos);
        }
        public static Geometry CreateGeometry(IEnumerable<Geometry> geomList)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(geomList);
        }
        public static Geometry CreateGeometry(params Geometry[] geos)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(geos);
        }
        public static List<List<Geometry>> GroupGeometries(List<Geometry> geos)
        {
            var geosGroup = new List<List<Geometry>>();
            GroupGeometries(geos, geosGroup);
            return geosGroup;
        }
        public static void GroupGeometries(List<Geometry> geos, List<List<Geometry>> geosGroup)
        {
            if (geos.Count == 0) return;
            var pairs = _GroupGeometriesToKVIndex(geos).ToArray();
            var dict = new ListDict<int>();
            var h = new BFSHelper()
            {
                Pairs = pairs,
                TotalCount = geos.Count,
                Callback = (g, i) => dict.Add(g.root, i),
            };
            h.BFS();
            dict.ForEach((_i, l) =>
            {
                geosGroup.Add(l.Select(i => geos[i]).ToList());
            });
        }
        public static Geometry ToNTSGeometry(GLineSegment seg, double radius)
        {
            var points1 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.StartPoint, radius));
            var points2 = GeoNTSConvertion.ConvertToCoordinateArray(GRect.Create(seg.EndPoint, radius));
            var ring1 = new LinearRing(points1);
            var ring2 = new LinearRing(points2);
            var geo = ThCADCoreNTSService.Instance.GeometryFactory.BuildGeometry(new Geometry[] { ring1, ring2 });
            return geo;
        }
        public static LinearRing CreateCircleLinearRing(Point3d center, double radius, int numPoints = 6)
        {
            var shapeFactory = new NetTopologySuite.Utilities.GeometricShapeFactory(ThCADCoreNTSService.Instance.GeometryFactory)
            {
                NumPoints = numPoints,
                Size = 2 * radius,
                Centre = center.ToNTSCoordinate(),
            };
            var ring = shapeFactory.CreateCircle().Shell;
            return ring;
        }
        public static Func<GRect, List<Geometry>> CreateGRectContainsSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return r =>
            {
                if (!r.IsValid) return new List<Geometry>();
                var poly = new Polygon(r.ToLinearRing());
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Contains(g)).ToList();
            };
        }
        public static Func<GRect, List<Geometry>> CreateGRectSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return r =>
            {
                if (!r.IsValid) return new List<Geometry>();
                var poly = new Polygon(r.ToLinearRing());
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<Geometry, List<Geometry>> CreateGeometrySelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return geo =>
            {
                if (geo == null) throw new ArgumentNullException();
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                return engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<LinearRing, List<Geometry>> CreateLinearRingSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return r =>
            {
                if (r == null) throw new ArgumentNullException();
                var poly = new Polygon(r);
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static Func<Polygon, List<Geometry>> CreatePolygonSelector(List<Geometry> geos)
        {
            if (geos.Count == 0) return r => new List<Geometry>();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            return poly =>
            {
                if (poly == null) throw new ArgumentNullException();
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(poly);
                return engine.Query(poly.EnvelopeInternal).Where(g => gf.Intersects(g)).ToList();
            };
        }
        public static IEnumerable<KeyValuePair<int, int>> _GroupGeometriesToKVIndex(List<Geometry> geos)
        {
            if (geos.Count == 0) yield break;
            geos = geos.Distinct().ToList();
            var engine = new NetTopologySuite.Index.Strtree.STRtree<Geometry>();
            foreach (var geo in geos) engine.Insert(geo.EnvelopeInternal, geo);
            for (int i = 0; i < geos.Count; i++)
            {
                var geo = geos[i];
                var gf = ThCADCoreNTSService.Instance.PreparedGeometryFactory.Create(geo);
                foreach (var j in engine.Query(geo.EnvelopeInternal).Where(g => gf.Intersects(g)).Select(g => geos.IndexOf(g)).Where(j => i < j))
                {
                    yield return new KeyValuePair<int, int>(i, j);
                }
            }
        }
    }


}

