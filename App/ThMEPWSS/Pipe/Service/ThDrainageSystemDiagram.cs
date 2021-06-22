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

    public partial class DrainageSystemDiagram
    {
        public static void draw1(Point3d basePoint)
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
                            if (i != start)
                            {
                                //long translator left
                                if (storey == "3F")
                                {
                                    var lastPt = NewMethod4(HEIGHT, ref v, basePt);

                                    {
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300);
                                        var segs = LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), true, 2);
                                    }

                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else if (storey == "12F")
                                {
                                    {
                                        var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300).OffsetY(221 - 90);
                                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 679), new Point2d(-321, 800), new Point2d(-1110, 800), new Point2d(-2380, 800) };
                                        var p0 = points.Last().TransformBy(startPoint);
                                        var p1 = p0.OffsetX(-180);
                                        var p2 = p1.OffsetX(-1000);
                                        {
                                            DrawFloorDrain(p0.ToPoint3d(), true);
                                            DrawFloorDrain(p2.ToPoint3d(), true);
                                            var p3 = points[4].TransformBy(startPoint).ToPoint3d();
                                            var p4 = p3.OffsetXY(-90, 90);
                                            DrawDomePipes(new GLineSegment(p3, p4));
                                            DrawWashBasin(p4, true);
                                        }

                                        var segs = points.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else if (storey == "11F")
                                {
                                    {
                                        var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300).OffsetY(221 - 90);
                                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 679), new Point2d(-321, 800), new Point2d(-1110, 800), new Point2d(-2380, 800) };
                                        var p0 = points.Last().TransformBy(startPoint);
                                        var p1 = p0.OffsetX(-180);
                                        var p2 = p1.OffsetX(-1000);
                                        {
                                            DrawFloorDrain(p0.ToPoint3d(), true);
                                            DrawFloorDrain(p2.ToPoint3d(), true);
                                            var p3 = points[4].TransformBy(startPoint).ToPoint3d();
                                            var p4 = p3.OffsetXY(-90, 90);
                                            DrawDomePipes(new GLineSegment(p3, p4));
                                            DrawDoubleWashBasins(p4, true);
                                        }

                                        var segs = points.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else if (storey == "10F")
                                {
                                    {
                                        var lastPt = NewMethod4(HEIGHT, ref v, basePt);
                                        var startPoint = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300).OffsetY(221 - 90);
                                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 679), new Point2d(-321, 800), new Point2d(-1110, 800), new Point2d(-2380, 800) };
                                        var p0 = points.Last().TransformBy(startPoint);
                                        var p1 = p0.OffsetX(-180);
                                        var p2 = p1.OffsetX(-1000);
                                        {
                                            DrawFloorDrain(p0.ToPoint3d(), true);
                                            DrawFloorDrain(p2.ToPoint3d(), true);
                                            var p3 = points[4].TransformBy(startPoint).ToPoint3d();
                                            var p4 = p3.OffsetXY(-90, 90);
                                            DrawDomePipes(new GLineSegment(p3, p4));
                                            DrawSWaterStoringCurve(p4, true);
                                        }

                                        var segs = points.ToGLineSegments(startPoint.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //short translator left
                                else if (storey == "4F")
                                {
                                    var points = SHORT_TRANSLATOR_POINTS;
                                    NewMethod1(HEIGHT, ref v, basePt, points);
                                    DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), true);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //long and short translator left
                                else if (storey == "5F")
                                {
                                    var height1 = LONG_TRANSLATOR_HEIGHT1;
                                    var points1 = LONG_TRANSLATOR_POINTS;
                                    var points2 = SHORT_TRANSLATOR_POINTS;
                                    var lastPt1 = points1.Last();
                                    NewMethod2(HEIGHT, ref v, basePt, points1, points2, height1);
                                    DrawPipeCheckPoint(basePt.OffsetXY(lastPt1.X, HEIGHT - height1 + lastPt1.Y - CHECKPOINT_OFFSET_Y), true);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //long translator right
                                else if (storey == "7F")
                                {
                                    var lastPt = NewMethod5(HEIGHT, ref v, basePt);

                                    {
                                        var pt = lastPt.TransformBy(basePt.OffsetY(HEIGHT - LONG_TRANSLATOR_HEIGHT1)).OffsetY(-300);
                                        var segs = LEFT_LONG_TRANSLATOR_CLEANING_PORT_POINTS.GetYAxisMirror().ToGLineSegments(pt.ToPoint3d());
                                        DrawDomePipes(segs);
                                        DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), false, 2);
                                    }
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //short translator right
                                else if (storey == "8F")
                                {
                                    var points = SHORT_TRANSLATOR_POINTS.GetYAxisMirror();
                                    NewMethod1(HEIGHT, ref v, basePt, points);
                                    DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), false);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                //long and short translator right
                                else if (storey == "9F")
                                {
                                    var points1 = LONG_TRANSLATOR_POINTS.GetYAxisMirror();
                                    var points2 = SHORT_TRANSLATOR_POINTS.GetYAxisMirror();
                                    NewMethod3(HEIGHT, ref v, basePt, points1, points2);
                                    var lastPt1 = points1.Last();
                                    var height1 = LONG_TRANSLATOR_HEIGHT1;
                                    DrawPipeCheckPoint(basePt.OffsetXY(lastPt1.X, HEIGHT - height1 + lastPt1.Y - CHECKPOINT_OFFSET_Y), false);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                                else
                                {
                                    DrawDomePipes(new GLineSegment(basePt, basePt.OffsetY(HEIGHT)));
                                    DrawPipeCheckPoint(basePt.OffsetY(HEIGHT / 2), true);
                                    DrawHorizontalLineOnPipeRun(HEIGHT, basePt);
                                }
                            }
                        }
                    }
                    outputStartPointOffsets[j] = v;
                }
            }
#pragma warning disable
            for (int j = 0; j < COUNT; j++)
            {
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    {
                        if (storey == "1F")
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + outputStartPointOffsets[j].ToVector3d();
                            if (DateTime.Now != DateTime.MinValue)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(0, -479), new Vector2d(-121, -121), new Vector2d(-1579, 0), new Vector2d(-300, 0) };
                                {
                                    var segs = vecs.ToGLineSegments(basePt);
                                    DrawDomePipes(segs);
                                    DrawDirtyWaterWell(segs.Last().EndPoint.OffsetX(-400).ToPoint3d(), "666");
                                    DrawCleaningPort(segs[1].EndPoint.ToPoint3d(), false, 1);
                                    DrawWrappingPipe(segs[2].EndPoint.ToPoint3d());
                                }
                                if (j == 1)
                                {
                                    var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600);
                                    var vecs2 = new List<Vector2d> { new Vector2d(2379, 0), new Vector2d(121, 121), new Vector2d(0, 569) };
                                    {
                                        var segs = vecs2.ToGLineSegments(bsPt);
                                        DrawDomePipes(segs);
                                    }
                                    {
                                        var vecs3 = new List<Vector2d> { new Vector2d(121, 121), new Vector2d(789, 0), new Vector2d(1270, 0), new Vector2d(180, 0), new Vector2d(1090, 0) };
                                        {
                                            var segs = vecs3.ToGLineSegments(vecs2.GetLastPoint(bsPt));
                                            {
                                                var _segs = segs.ToList();
                                                _segs.RemoveAt(3);
                                                DrawDomePipes(_segs);
                                            }
                                            {
                                                var p1 = segs[1].EndPoint;
                                                var p2 = p1.OffsetXY(90, 90);
                                                DrawDomePipes(new GLineSegment(p1, p2));
                                                DrawSWaterStoringCurve(p2.ToPoint3d(), false);
                                                var p3 = segs[2].EndPoint;
                                                var p4 = segs[4].EndPoint;
                                                DrawFloorDrain(p3.ToPoint3d(), false);
                                                DrawFloorDrain(p4.ToPoint3d(), false);
                                            }
                                        }
                                    }
                                }
                                else if (j == 4)
                                {
                                    var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600);
                                    var vecs1 = new List<Vector2d> { new Vector2d(2120, 0), new Vector2d(406, 406), new Vector2d(404, 404), new Vector2d(879, 0), new Vector2d(1180, 0) };
                                    var vecs2 = new List<Vector2d> { new Vector2d(3150, 0), new Vector2d(404, 404), new Vector2d(1270, 0), new Vector2d(180, 0), new Vector2d(1090, 0) };
                                    var segs1 = vecs1.ToGLineSegments(bsPt);
                                    DrawDomePipes(segs1);
                                    var segs2 = vecs2.ToGLineSegments(segs1[1].EndPoint);
                                    {
                                        var _segs2 = segs2.ToList();
                                        _segs2.RemoveAt(3);
                                        DrawDomePipes(_segs2);
                                    }
                                    {
                                        var p1 = segs1[3].EndPoint;
                                        var p2 = p1.OffsetXY(90, 90);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                        DrawDoubleWashBasins(p2.ToPoint3d(), false);
                                    }

                                    DrawFloorDrain(segs1[4].EndPoint.ToPoint3d(), false);

                                    {
                                        var p1 = segs2[1].EndPoint;
                                        var p2 = p1.OffsetXY(90, 90);
                                        DrawDomePipes(new GLineSegment(p1, p2));
                                        DrawSWaterStoringCurve(p2.ToPoint3d(), false);
                                    }
                                    DrawFloorDrain(segs2[2].EndPoint.ToPoint3d(), false);
                                    DrawFloorDrain(segs2[4].EndPoint.ToPoint3d(), false);
                                }
                                else if (j == 6)
                                {
                                    {
                                        var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600);
                                        var vecs1 = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(4879, 0), new Vector2d(121, 121), new Vector2d(0, 1079) };
                                        var segs = vecs1.ToGLineSegments(bsPt);
                                        DrawDomePipes(segs);
                                        DrawWrappingPipe(segs[0].EndPoint.ToPoint3d());
                                        DrawCleaningPort(segs[1].EndPoint.ToPoint3d(), false, 1);
                                    }
                                    {
                                        var bsPt = vecs.GetLastPoint(basePt).OffsetY(-600 - 600);
                                        var vecs2 = new List<Vector2d> { new Vector2d(300, 0), new Vector2d(5479, 0), new Vector2d(121, 121), new Vector2d(0, 1379) };
                                        var segs = vecs2.ToGLineSegments(bsPt);
                                        DrawDomePipes(segs);
                                        DrawWrappingPipe(segs[0].EndPoint.ToPoint3d());
                                        DrawCleaningPort(segs[1].EndPoint.ToPoint3d(), false, 1);
                                        DrawCleaningPort(segs[3].EndPoint.ToPoint3d(), false, 2);
                                    }
                                }
                            }
                            else
                            {
                                var height = 480;
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-2000, -121) };
                                var segs = points.ToGLineSegments(basePt.OffsetY(-height));
                                DrawDomePipes(segs);
                                DrawDomePipes(new GLineSegment(basePt, basePt.OffsetY(-height)));
                                DrawDirtyWaterWell(points.Last().TransformBy(basePt).ToPoint3d().OffsetY(-height), "666");
                            }
                        }
                    }
                }
            }

            if (false)
            {
                for (int j = 0; j < COUNT; j++)
                {
                    var start = storeys.IndexOf("31F");
                    var end = storeys.IndexOf("3F");
                    for (int i = start; i >= end; i--)
                    {
                        var s = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);
                            if (i == start)
                            {
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(300, -300), new Point2d(300, -600) };
                                var segs = points.ToGLineSegments(basePt.OffsetY(600));
                                DrawDomePipes(segs);
                            }
                            else if (i == end)
                            {
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(0, -900), new Point2d(-300, -1200) };
                                var segs = points.ToGLineSegments(basePt.OffsetXY(300, HEIGHT));
                                DrawDomePipes(segs);
                            }
                            else
                            {
                                var points = new Point2d[] { new Point2d(0, 0), new Point2d(0, -900), new Point2d(-300, -1200) };
                                var segs = points.ToGLineSegments(basePt.OffsetXY(300, HEIGHT));
                                DrawDomePipes(segs);
                                DrawDomePipes(new GLineSegment(points[1].TransformBy(basePt.OffsetXY(300, HEIGHT)), basePt.OffsetX(300).ToPoint2d()));
                            }
                        }
                    }
                }

            }
            for (int j = 0; j < COUNT; j++)
            {
                var x = basePoint.X + OFFSET_X + (j + 1) * SPAN_X;
                var y1 = basePoint.Y;
                var y2 = y1 + HEIGHT * (storeys.Count - 1);
                //{
                //    var line = DU.DrawLineLazy(x, y1, x, y2);
                //    SetDomePipeLineStyle(line);
                //}
                //{
                //    var line = DU.DrawLineLazy(x + 300, y1, x + 300, y2);
                //    SetVentPipeLineStyle(line);
                //}
            }


            {
                // var bsPt = basePoint.OffsetXY(500, -1000);
                // DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                //scale: 0.5,
                //props: new Dictionary<string, string>() { { "-", "666" } },
                //cb: br =>
                //{
                //    br.Layer = "W-DRAI-EQPM";
                //});
            }
        }

        public static ThwPipeLineGroup GenThwPipeLineGroup(List<string> storeys)
        {
            var o = new ThwPipeLineGroup();
            o.Output = new ThwOutput();
            o.Output.PortValues = new List<string>() { "1", "19" };
            o.Output.HasWrappingPipe1 = true;
            o.Output.HasCleaningPort1 = true;
            {
                o.PL = new ThwPipeLine();
                var pl = o.PL;
                pl.Label1 = "PL1-1,2";
                pl.PipeRuns = new List<ThwPipeRun>();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    pl.PipeRuns.Add(new ThwPipeRun() { HasCheckPoint = true, Storey = storey });
                }
                {
                    var r = pl.PipeRuns[1];
                    r.HasLongTranslator = true;
                    r.HasCleaningPort = true;
                    r.HasHorizontalShortLine = true;
                    r.IsLongTranslatorToLeftOrRight = true;
                }
                {
                    var r = pl.PipeRuns[2];
                    r.HasLongTranslator = true;
                    r.HasCleaningPort = true;
                    r.HasHorizontalShortLine = true;
                    r.IsLongTranslatorToLeftOrRight = false;
                }
                {
                    var r = pl.PipeRuns[3];
                    r.HasCleaningPort = true;
                    r.HasHorizontalShortLine = true;
                    r.ShowStoreyLabel = true;//to support
                    r.HasShortTranslator = true;
                    r.HasLongTranslator = true;
                    r.IsLongTranslatorToLeftOrRight = true;
                }
                {
                    var r = pl.PipeRuns[4];
                    r.HasShortTranslator = true;
                    r.IsShortTranslatorToLeftOrRight = true;
                    r.ShowShortTranslatorLabel = true;
                }
                {
                    var r = pl.PipeRuns[5];
                    r.HasLongTranslator = true;
                    r.HasShortTranslator = true;
                    r.IsLongTranslatorToLeftOrRight = true;
                    r.ShowShortTranslatorLabel = true;
                }
                for (int i = 3; i < 10; i++)
                {
                    var r = pl.PipeRuns[i];
                    r.HasCleaningPort = true;
                    r.HasHorizontalShortLine = true;
                }
            }
            {
                o.TL = new ThwPipeLine();
                var tl = o.TL;
                tl.Label1 = "TL1-1,2";
                tl.PipeRuns = new List<ThwPipeRun>();
                var start = 2 - 1;
                var end = 31 - 1;
                for (int i = start; i <= end; i++)
                {
                    var storey = storeys[i];
                    var r = new ThwPipeRun() { HasCheckPoint = true, Storey = storey };
                    tl.PipeRuns.Add(r);
                    if (i == start) r.IsFirstItem = true;
                    if (i == end) r.IsLastItem = true;
                }
            }
            return o;
        }
        public static void draw5(Point2d basePoint)
        {
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            //var HEIGHT = 5000.0;
            var COUNT = 20;

            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            var groups = Enumerable.Range(1, COUNT).Select(i => GenThwPipeLineGroup(storeys)).ToList();

            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
            }

            var vecs1 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779) };
            var vecs2 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -1679), new Vector2d(-121, -121) };
            var vecs3 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658), new Vector2d(-121, -121) };
            var vecs4 = vecs1.GetYAxisMirror();
            var vecs5 = vecs2.GetYAxisMirror();
            var vecs6 = vecs3.GetYAxisMirror();

            var start = storeys.Count - 1;
            var end = 0;
            var runsLocationInfoMatrix = new PipeRunLocationInfo[COUNT, storeys.Count];
            for (int j = 0; j < COUNT; j++)
                for (int i = 0; i < storeys.Count; i++)
                {
                    runsLocationInfoMatrix[j, i] = new PipeRunLocationInfo();
                }

            //if (grp.LinesCount == 2 && grp.PL != null && grp.TL != null)
            for (int j = 0; j < COUNT; j++)
            {
                var grp = groups[j];
                if (grp.PL != null)
                {
                    var tdx = 0.0;
                    for (int i = start; i >= end; i--)
                    {
                        var run = grp.PL.PipeRuns[i];
                        if (run == null) continue;

                        var storey = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + new Vector2d(tdx, 0);
                            if (i == start) continue;
                            if (run.HasLongTranslator && run.HasShortTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    var vecs = vecs3;
                                    DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                    runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                                }
                                else
                                {
                                    var vecs = vecs6;
                                    DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                    runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                                }
                            }
                            else if (run.HasLongTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    var vecs = vecs1;
                                    DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                    runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                                }
                                else
                                {
                                    var vecs = vecs4;
                                    DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                    runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                                }
                            }
                            else if (run.HasShortTranslator)
                            {
                                if (run.IsShortTranslatorToLeftOrRight)
                                {
                                    var vecs = vecs2;
                                    DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                    runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                                }
                                else
                                {
                                    var vecs = vecs5;
                                    DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                    runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                                }
                            }
                            else
                            {
                                //normal
                                var vecs = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -1800) };
                                DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                                var dx = vecs.Sum(v => v.X);
                                tdx += dx;
                                runsLocationInfoMatrix[j, i].BasePoint = basePt;
                                runsLocationInfoMatrix[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                            }
                        }
                    }
                }

            }


            for (int j = 0; j < COUNT; j++)
                for (int i = 0; i < storeys.Count; i++)
                {
                    runsLocationInfoMatrix[j, i].StartPoint = runsLocationInfoMatrix[j, i].BasePoint.OffsetY(HEIGHT);
                }

            for (int j = 0; j < COUNT; j++)
            {
                var grp = groups[j];
                if (grp.PL != null)
                {
                    for (int i = start; i >= end; i--)
                    {
                        var run = grp.PL.PipeRuns[i];
                        if (run == null) continue;

                        var info = runsLocationInfoMatrix[j, i];

                        if (run.ShowShortTranslatorLabel)
                        {
                            var vecs = new List<Vector2d> { new Vector2d(76, 76), new Vector2d(-424, 424), new Vector2d(-1900, 0) };
                            var segs = vecs.ToGLineSegments(info.EndPoint).Skip(1).ToList();
                            DrawDraiNoteLines(segs);
                            DrawDraiNoteLines(segs);
                            var t = DU.DrawTextLazy("DN100乙字弯", 350, segs.Last().EndPoint);
                            SetLabelStylesForDraiNote(t);
                        }
                        if (run.HasCheckPoint)
                        {
                            DrawPipeCheckPoint(info.EndPoint.OffsetY(200).ToPoint3d(), false);
                        }
                        if (run.HasHorizontalShortLine)
                        {
                            DrawHorizontalLineOnPipeRun(HEIGHT, info.BasePoint.ToPoint3d());
                        }
                    }
                }
            }

        }
        public static void draw4(Point2d basePoint)
        {
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            //var HEIGHT = 5000.0;
            var COUNT = 20;

            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            var groups = Enumerable.Range(1, COUNT).Select(i => GenThwPipeLineGroup(storeys)).ToList();

            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
            }

            var vecs1 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779) };
            var vecs2 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -1679), new Vector2d(-121, -121) };
            var vecs3 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658), new Vector2d(-121, -121) };
            var vecs4 = vecs1.GetYAxisMirror();
            var vecs5 = vecs2.GetYAxisMirror();
            var vecs6 = vecs3.GetYAxisMirror();

            var start = storeys.Count - 1;
            var end = 0;
            var arrarr = new PipeRunLocationInfo[COUNT, storeys.Count];
            for (int j = 0; j < COUNT; j++)
                for (int i = 0; i < storeys.Count; i++)
                {
                    arrarr[j, i] = new PipeRunLocationInfo();
                }


            for (int j = 0; j < COUNT; j++)
            {
                var tdx = 0.0;
                for (int i = start; i >= end; i--)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    {
                        var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + new Vector2d(tdx, 0);

                        if (i == 3)
                        {
                            //long left
                            var vecs = vecs1;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                        else if (i == 4)
                        {
                            //short left
                            var vecs = vecs2;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                        else if (i == 5)
                        {
                            //long short left
                            var vecs = vecs3;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                        else if (i == 6)
                        {
                            //long right
                            var vecs = vecs4;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                        else if (i == 7)
                        {
                            //short right
                            var vecs = vecs5;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                        else if (i == 8)
                        {
                            //long short right
                            var vecs = vecs6;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                        else if (i != start)
                        {
                            //normal
                            var vecs = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -1800) };
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            var dx = vecs.Sum(v => v.X);
                            tdx += dx;
                            arrarr[j, i].BasePoint = basePt;
                            arrarr[j, i].EndPoint = basePt + new Vector2d(dx, 0);
                        }
                    }
                }
            }


            for (int j = 0; j < COUNT; j++)
                for (int i = 0; i < storeys.Count; i++)
                {
                    Dbg.ShowXLabel(arrarr[j, i].EndPoint);
                }


        }
        public static void draw3(Point2d basePoint)
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
                DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
            }
            var start = storeys.Count - 1;
            var end = 0;
            for (int j = 0; j < COUNT; j++)
            {
                var dx = 0.0;
                for (int i = start; i >= end; i--)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                    {
                        var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + new Vector2d(dx, 0);
                        var basePt2 = basePt.OffsetY(HEIGHT);
                        var vecs1 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779) };
                        var vecs2 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -1679), new Vector2d(-121, -121) };
                        var vecs3 = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658), new Vector2d(-121, -121) };
                        //Dbg.ShowXLabel(basePt);
                        if (i == 3)
                        {
                            //long left
                            var vecs = vecs1;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                        else if (i == 4)
                        {
                            //short left
                            var vecs = vecs2;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                        else if (i == 5)
                        {
                            //long short left
                            var vecs = vecs3;
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                        else if (i == 6)
                        {
                            //long right
                            var vecs = vecs1.GetYAxisMirror();
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                        else if (i == 7)
                        {
                            //short right
                            var vecs = vecs2.GetYAxisMirror();
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                        else if (i == 8)
                        {
                            //long short right
                            var vecs = vecs3.GetYAxisMirror();
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                        else if (i != start)
                        {
                            //normal
                            var vecs = new List<Vector2d> { new Vector2d(0, 1800), new Vector2d(0, -1800) };
                            DrawDomePipes(vecs.ToGLineSegments(basePt).Skip(1));
                            dx += vecs.Sum(v => v.X);
                        }
                    }
                }
            }
        }
        public static void draw2(Point3d basePoint)
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
                                                var t = DU.DrawTextLazy("DN100乙字弯", 350, segs.Last().EndPoint);
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
    }
}