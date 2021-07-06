

namespace ThMEPWSS.Pipe.Service
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.Geometry;
    using Dreambuild.AutoCAD;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.DebugNs;
    using ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs;
    using Linq2Acad;
    using ThMEPWSS.Assistant;

    public partial class DrainageSystemDiagram
    {
        public static void draw14(Point2d basePoint)
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
                DrawStoreyLine(storey, basePoint, lineLen);
            }
            var outputStartPointOffsets = new Vector2d[COUNT];
        }
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
            o.Output.DirtyWaterWellValues = new List<string>() { "1", "19" };
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
            //{
            //    o.TL = new ThwPipeLine();
            //    var tl = o.TL;
            //    tl.Label1 = "TL1-1,2";
            //    tl.PipeRuns = new List<ThwPipeRun>();
            //    var start = 2 - 1;
            //    var end = 31 - 1;
            //    for (int i = start; i <= end; i++)
            //    {
            //        var storey = storeys[i];
            //        var r = new ThwPipeRun() { HasCheckPoint = true, Storey = storey };
            //        tl.PipeRuns.Add(r);
            //        if (i == start) r.IsFirstItem = true;
            //        if (i == end) r.IsLastItem = true;
            //    }
            //}
            return o;
        }
        public static void DrawOutputs1(Point2d basePoint1, double width, ThwOutput output)
        {
            Point2d pt2, pt3;
            if (output.DirtyWaterWellValues != null)
            {
                var v = new Vector2d(-2000 - 400, -600);
                var pt = basePoint1 + v;
                var values = output.DirtyWaterWellValues;
                DrawDiryWaterWells1(pt, values);
            }
            {
                var dx = width - 3600;
                var vecs = new List<Vector2d> { new Vector2d(0, -479), new Vector2d(-121, -121), new Vector2d(-1879, 0), new Vector2d(0, -600), new Vector2d(5479 + dx, 0), new Vector2d(121, 121), new Vector2d(0, 1079), new Vector2d(-5600 - dx, -1800), new Vector2d(6079 + dx, 0), new Vector2d(121, 121) };
                {
                    var segs = vecs.ToGLineSegments(basePoint1);
                    if (output.LinesCount == 1)
                    {
                        DrawDomePipes(segs.Take(3));
                    }
                    else if (output.LinesCount > 1)
                    {
                        segs.RemoveAt(7);
                        if (!output.HasVerticalLine2) segs.RemoveAt(6);
                        segs.RemoveAt(3);
                        DrawDomePipes(segs);
                    }
                }
                var pts = vecs.ToPoint2ds(basePoint1);
                if (output.HasWrappingPipe1) DrawWrappingPipe(pts[3].OffsetX(300));
                if (output.HasWrappingPipe2) DrawWrappingPipe(pts[4].OffsetX(300));
                if (output.HasWrappingPipe3) DrawWrappingPipe(pts[8].OffsetX(300));
                DrawNoteText(output.DN1, pts[3].OffsetX(750));
                DrawNoteText(output.DN2, pts[4].OffsetX(750));
                DrawNoteText(output.DN3, pts[8].OffsetX(750));
                if (output.HasCleaningPort1) DrawCleaningPort(pts[2].ToPoint3d(), false, 1);
                if (output.HasCleaningPort2) DrawCleaningPort(pts[5].ToPoint3d(), false, 1);
                if (output.HasCleaningPort3) DrawCleaningPort(pts[9].ToPoint3d(), false, 1);
                pt2 = pts[6];
                pt3 = pts.Last();
            }
            if (output.HasLargeCleaningPort)
            {
                var vecs = new List<Vector2d> { new Vector2d(0, 1379) };
                var segs = vecs.ToGLineSegments(pt3);
                DrawDomePipes(segs);
                DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), false, 2);
            }
            if (output.HangingCount == 1)
            {
                var hang = output.Hanging1;
                Point2d lastPt = pt2;
                {
                    var segs = new List<Vector2d> { new Vector2d(0, 569), new Vector2d(121, 121) }.ToGLineSegments(lastPt);
                    DrawDomePipes(segs);
                    lastPt = segs.Last().EndPoint;
                }
                {
                    lastPt = drawHanging(lastPt, output.Hanging1);
                }
            }
            else if (output.HangingCount == 2)
            {
                var vs1 = new List<Vector2d> { new Vector2d(406, 406), new Vector2d(404, 404) };
                var pts = vs1.ToPoint2ds(pt3);
                DrawDomePipes(vs1.ToGLineSegments(pt3));
                drawHanging(pts.Last(), output.Hanging1);
                var dx = output.Hanging1.FloorDrainsCount == 2 ? 1000 : 0;
                var vs2 = new List<Vector2d> { new Vector2d(3150 + dx, 0), new Vector2d(404, 404) };
                DrawDomePipes(vs2.ToGLineSegments(pts[1]));
                drawHanging(vs2.ToPoint2ds(pts[1]).Last(), output.Hanging2);
            }
        }
        public static void DrawDiryWaterWells2(Point2d pt, List<string> values)
        {
            var dx = 0;
            foreach (var value in values)
            {
                DrawDirtyWaterWell(pt.OffsetX(400) + new Vector2d(dx, 0), value);
                dx += 800;
            }
        }
        public static void DrawDiryWaterWells1(Point2d pt, List<string> values)
        {
            if (values.Count == 1)
            {
                DrawDirtyWaterWell(pt, values[0]);
            }
            else if (values.Count >= 2)
            {
                var pts = GetBasePoints(pt.OffsetX(-800), 2, values.Count, 800, 800).ToList();
                for (int i = 0; i < values.Count; i++)
                {
                    DrawDirtyWaterWell(pts[i], values[i]);
                }
            }
        }

        public static Point2d drawHanging(Point2d start, Hanging hanging)
        {
            var vecs = new List<Vector2d> { new Vector2d(789, 0), new Vector2d(1270, 0), new Vector2d(180, 0), new Vector2d(1090, 0) };
            var segs = vecs.ToGLineSegments(start);
            {
                var _segs = segs.ToList();
                if (hanging.FloorDrainsCount == 1)
                {
                    _segs.RemoveAt(3);
                }
                _segs.RemoveAt(2);
                DrawDomePipes(_segs);
            }
            {
                var pts = vecs.ToPoint2ds(start);
                {
                    var pt = pts[1];
                    var v = new Vector2d(90, 90);
                    if (hanging.HasSCurve)
                    {
                        DrawSCurve(v, pt, false);
                    }
                    if (hanging.HasDoubleSCurve)
                    {
                        DrawDSCurve(v, pt, false);
                    }
                }
                if (hanging.FloorDrainsCount >= 1)
                {
                    DrawFloorDrain(pts[2].ToPoint3d(), false);
                }
                if (hanging.FloorDrainsCount >= 2)
                {
                    DrawFloorDrain(pts[4].ToPoint3d(), false);
                }
            }
            start = segs.Last().EndPoint;
            return start;
        }

        public static void draw6(Point2d basePoint)
        {
            var o = new ThwOutput();
            //o.DirtyWaterWellValues = new List<string>() { "1",  };
            //o.DirtyWaterWellValues = new List<string>() { "1", "2" };
            o.DirtyWaterWellValues = new List<string>() { "1", "2", "3" };
            o.HasVerticalLine2 = false;
            o.HasWrappingPipe1 = true;
            o.HasWrappingPipe2 = false;
            o.HasWrappingPipe3 = true;
            o.DN1 = "DN100";
            o.DN2 = "DN666";
            o.DN3 = "DN100";
            o.HasCleaningPort1 = true;
            o.HasCleaningPort2 = true;
            o.HasCleaningPort3 = true;
            o.HasLargeCleaningPort = true;
            //o.HangingCount = 1;
            o.HangingCount = 2;
            o.LinesCount = 3;
            o.Hanging1 = new Hanging() { FloorDrainsCount = 1, HasDoubleSCurve = true };
            o.Hanging2 = new Hanging() { FloorDrainsCount = 2, HasDoubleSCurve = true };
            DrawOutputs1(basePoint, 3600, o);
        }
        public static void draw5(Point2d basePoint)
        {
            var OFFSET_X = 2500.0;
            var SPAN_X = 5500.0;
            var HEIGHT = 1800.0;
            //var HEIGHT = 5000.0;
            var COUNT = 20;
            var STOREY_COUNT = 32;
            var dy = HEIGHT - 1800.0;
            var storeys = Enumerable.Range(1, STOREY_COUNT).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            var groups = Enumerable.Range(1, COUNT).Select(i => GenThwPipeLineGroup(storeys)).ToList();
            setValues(groups);
            {
                groups[1].TL = groups[1].PL.ToCadJson().FromCadJson<ThwPipeLine>();
                groups[1].TL.PipeRuns[28] = null;
                groups[1].DL = groups[1].PL;
            }

            DrawVer1(new DrawingOption()
            {
                basePoint = basePoint,
                OFFSET_X = OFFSET_X,
                SPAN_X = SPAN_X,
                HEIGHT = HEIGHT,
                COUNT = COUNT,
                storeys = storeys,
                groups = groups
            });

        }
        public static void DrawVer1(DrawingOption option)
        {
            var maxStoreyIndex = option.maxStoreyIndex;
            var test = option.test;
            var layer = option.layer;
            if (string.IsNullOrWhiteSpace(layer)) layer = "W-DRAI-DOME-PIPE";
            void drawPipe(GLineSegment seg)
            {
                var line = DU.DrawLineSegmentLazy(seg);
                line.Layer = layer;
                line.ColorIndex = 256;
            }
            void drawPipes(IEnumerable<GLineSegment> segs)
            {
                var lines = DU.DrawLineSegmentsLazy(segs);
                foreach (var line in lines)
                {
                    line.Layer = layer;
                    line.ColorIndex = 256;
                }
            }
            var basePoint = option.basePoint;
            var OFFSET_X = option.OFFSET_X;
            var SPAN_X = option.SPAN_X;
            var HEIGHT = option.HEIGHT;
            var COUNT = option.COUNT;
            var groups = option.groups;
            var storeys = option.storeys;
            var dy = HEIGHT - 1800.0;
            {
                var drawStoreyLine = option.drawStoreyLine;
                if (drawStoreyLine)
                {
                    var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                    for (int i = 0; i < storeys.Count; i++)
                    {
                        var storey = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        DrawStoreyLine(storey, bsPt1.ToPoint3d(), lineLen);
                    }
                }
            }
            var vecs0 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -1800 - dy) };
            var vecs1 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -779 - dy) };
            var vecs2 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -1679 - dy), new Vector2d(-121, -121) };
            var vecs3 = new List<Vector2d> { new Vector2d(0, 1800 + dy), new Vector2d(0, -780), new Vector2d(-121, -121), new Vector2d(-1258, 0), new Vector2d(-121, -120), new Vector2d(0, -658 - dy), new Vector2d(-121, -121) };
            var vecs4 = vecs1.GetYAxisMirror();
            var vecs5 = vecs2.GetYAxisMirror();
            var vecs6 = vecs3.GetYAxisMirror();
            var vec7 = new Vector2d(-90, 90);
            var start = maxStoreyIndex == 0 ? storeys.Count - 1 : maxStoreyIndex + 1;
            var end = 0;
            PipeRunLocationInfo[] getPipeRunLocationInfos(Point2d basePoint, ThwPipeLine thwPipeLine, int j)
            {
                var arr = new PipeRunLocationInfo[storeys.Count];

                for (int i = 0; i < storeys.Count; i++)
                {
                    arr[i] = new PipeRunLocationInfo();
                }

                {
                    var tdx = 0.0;
                    for (int i = start; i >= end; i--)
                    {
                        var run = thwPipeLine.PipeRuns.TryGet(i);
                        if (run == null) continue;

                        var storey = storeys[i];
                        var bsPt1 = basePoint.OffsetY(HEIGHT * i);
                        {
                            var basePt = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X) + new Vector2d(tdx, 0);
                            if (i == start)
                            {
                                arr[i] = null;
                                continue;
                            }
                            if (run.HasLongTranslator && run.HasShortTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    var vecs = vecs3;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                }
                                else
                                {
                                    var vecs = vecs6;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                }
                            }
                            else if (run.HasLongTranslator)
                            {
                                if (run.IsLongTranslatorToLeftOrRight)
                                {
                                    var vecs = vecs1;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                }
                                else
                                {
                                    var vecs = vecs4;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                }
                            }
                            else if (run.HasShortTranslator)
                            {
                                if (run.IsShortTranslatorToLeftOrRight)
                                {
                                    var vecs = vecs2;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                }
                                else
                                {
                                    var vecs = vecs5;
                                    var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                    var dx = vecs.Sum(v => v.X);
                                    tdx += dx;
                                    arr[i].BasePoint = basePt;
                                    arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                    arr[i].Vector2ds = vecs;
                                    arr[i].Segs = segs;
                                }
                            }
                            else
                            {
                                //normal
                                var vecs = vecs0;
                                var segs = vecs.ToGLineSegments(basePt).Skip(1).ToList();

                                var dx = vecs.Sum(v => v.X);
                                tdx += dx;
                                arr[i].BasePoint = basePt;
                                arr[i].EndPoint = basePt + new Vector2d(dx, 0);
                                arr[i].Vector2ds = vecs;
                                arr[i].Segs = segs;
                            }
                        }
                    }
                }

                for (int i = 0; i < storeys.Count; i++)
                {
                    arr[i].StartPoint = arr[i].BasePoint.OffsetY(HEIGHT);
                }

                return arr;
            }


            for (int j = 0; j < COUNT; j++)
            {

                void handlePipeLine(ThwPipeLine thwPipeLine, PipeRunLocationInfo[] arr)
                {
                    for (int i = start; i >= end; i--)
                    {
                        var storey = storeys.TryGet(i);
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
                                //o.HasVerticalLine2 = false;
                                //o.HasWrappingPipe1 = true;
                                //o.HasWrappingPipe2 = false;
                                //o.HasWrappingPipe3 = true;
                                //o.HasCleaningPort1 = true;
                                //o.HasCleaningPort2 = true;
                                //o.HasCleaningPort3 = true;
                                //o.HasLargeCleaningPort = false;
                                ////o.HangingCount = 1;
                                //o.HangingCount = 2;
                                //o.Hanging1 = new Hanging() { FloorDrainsCount = 1, HasDoubleSCurve = true };
                                //o.Hanging2 = new Hanging() { FloorDrainsCount = 2, HasDoubleSCurve = true };
                                //drawOutputs(basePt, 3600, o);
                                if (thwPipeLine.Output != null) DrawOutputs1(basePt, 3600, thwPipeLine.Output);
                            }
                        }
                        {
                            if (i == end)
                            {
                                var dy = -3000;
                                if (thwPipeLine.Comments != null)
                                {
                                    foreach (var comment in thwPipeLine.Comments)
                                    {
                                        if (!string.IsNullOrEmpty(comment))
                                        {
                                            DU.DrawTextLazy(comment, 350, info.EndPoint.OffsetY(dy));
                                        }
                                        dy -= 350;
                                    }
                                }
                            }
                        }
                        void handleHanging(Hanging hanging, bool isLeftOrRight)
                        {
                            if (run.HasLongTranslator)
                            {
                                var vecs = new List<Vector2d> { new Vector2d(-200, 200), new Vector2d(0, 479), new Vector2d(-121, 121), new Vector2d(-789, 0), new Vector2d(-1270, 0), new Vector2d(-180, 0), new Vector2d(-1090, 0) };
                                if (isLeftOrRight == false)
                                {
                                    vecs = vecs.GetYAxisMirror();
                                }
                                var pt = info.Segs[4].StartPoint.OffsetY(-669).OffsetY(590 - 90);
                                if (isLeftOrRight == false && run.IsLongTranslatorToLeftOrRight == true)
                                {
                                    var p1 = pt;
                                    var p2 = pt.OffsetX(1700);
                                    drawPipe(new GLineSegment(p1, p2));
                                    pt = p2;
                                }
                                if (isLeftOrRight == true && run.IsLongTranslatorToLeftOrRight == false)
                                {
                                    var p1 = pt;
                                    var p2 = pt.OffsetX(-1700);
                                    drawPipe(new GLineSegment(p1, p2));
                                    pt = p2;
                                }
                                var segs = vecs.ToGLineSegments(pt);
                                {
                                    var _segs = segs.ToList();
                                    if (hanging.FloorDrainsCount == 2)
                                    {
                                        _segs.RemoveAt(5);
                                    }
                                    else if (hanging.FloorDrainsCount == 1)
                                    {
                                        _segs = segs.Take(5).ToList();
                                    }
                                    else if (hanging.FloorDrainsCount == 0)
                                    {
                                        _segs = segs.Take(4).ToList();
                                    }
                                    drawPipes(_segs);
                                }
                                if (hanging.FloorDrainsCount >= 1)
                                {
                                    DrawFloorDrain(segs.Last(3).EndPoint.ToPoint3d(), isLeftOrRight);
                                }
                                if (hanging.FloorDrainsCount >= 2)
                                {
                                    DrawFloorDrain(segs.Last(1).EndPoint.ToPoint3d(), isLeftOrRight);
                                    if (hanging.IsSeries)
                                    {
                                        DrawDomePipes(segs.Last(2));
                                    }
                                }

                                if (hanging.HasSCurve)
                                {
                                    var p1 = segs.Last(3).StartPoint;
                                    DrawSCurve(vec7, p1, isLeftOrRight);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    var p1 = segs.Last(3).StartPoint;
                                    DrawDSCurve(vec7, p1, isLeftOrRight);
                                }
                            }
                            else
                            {
                                var vecs = new List<Vector2d> { new Vector2d(-121, 121), new Vector2d(-789, 0), new Vector2d(-1270, 0), new Vector2d(-180, 0), new Vector2d(-1090, -15) };
                                if (isLeftOrRight == false)
                                {
                                    vecs = vecs.GetYAxisMirror();
                                }
                                var segs = vecs.ToGLineSegments(info.StartPoint.OffsetY(-510));
                                {
                                    var _segs = segs.ToList();
                                    if (hanging.FloorDrainsCount == 2)
                                    {
                                        _segs.RemoveAt(3);
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
                                    drawPipes(_segs);
                                }
                                if (hanging.FloorDrainsCount >= 1)
                                {
                                    DrawFloorDrain(segs.Last(3).EndPoint.ToPoint3d(), isLeftOrRight);
                                }
                                if (hanging.FloorDrainsCount >= 2)
                                {
                                    DrawFloorDrain(segs.Last(1).EndPoint.ToPoint3d(), isLeftOrRight);
                                }
                                if (hanging.HasSCurve)
                                {
                                    var p1 = segs.Last(3).StartPoint;
                                    DrawSCurve(vec7, p1, isLeftOrRight);
                                }
                                if (hanging.HasDoubleSCurve)
                                {
                                    var p1 = segs.Last(3).StartPoint;
                                    DrawDSCurve(vec7, p1, isLeftOrRight);
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
                            handleHanging(run.LeftHanging, true);
                        }
                        if (run.RightHanging != null)
                        {
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
                                DrawPipeCheckPoint(info.Segs.Last().StartPoint.OffsetY(280).ToPoint3d(), false);
                            }
                            else
                            {
                                DrawPipeCheckPoint(info.EndPoint.OffsetY(280).ToPoint3d(), false);
                            }
                        }
                        if (run.HasHorizontalShortLine)
                        {
                            DrawHorizontalLineOnPipeRun(HEIGHT, info.BasePoint.ToPoint3d());
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
                                    drawPipes(segs);
                                    DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, 2);
                                }
                                else
                                {
                                    var segs = vecs.ToGLineSegments(info.Segs.Last().StartPoint.OffsetY(-300));
                                    drawPipes(segs);
                                    DrawCleaningPort(segs.Last().EndPoint.ToPoint3d(), run.IsLongTranslatorToLeftOrRight, 2);
                                }
                            }
                            else
                            {
                                DrawCleaningPort(info.StartPoint.OffsetY(-300).ToPoint3d(), true, 2);
                            }

                        }
                    }
                }


                var grp = groups[j];
                var dx = 0;
                if (grp.PL != null)
                {
                    var thwPipeLine = grp.PL;
                    var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                    handlePipeLine(thwPipeLine, arr);
                    for (int i = 0; i < storeys.Count; i++)
                    {
                        var info = arr[i];
                        var segs = info.DisplaySegs ?? info.Segs;
                        if (segs != null)
                        {
                            drawPipes(segs);
                        }
                    }
                }
                if (grp.TL != null)
                {
                    var thwPipeLine = grp.TL;
                    dx += 300;
                    var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                    handlePipeLine(thwPipeLine, arr);
                    for (int i = 0; i < storeys.Count; i++)
                    {
                        var info = arr[i];
                        var segs = info.DisplaySegs ?? info.Segs;
                        if (segs != null)
                        {
                            DrawBluePipes(segs);
                        }
                        //if (info.DisplaySegs != null)
                        //{
                        //    drawPipes(info.Segs);
                        //}
                    }
                }
                if (grp.DL != null)
                {
                    var thwPipeLine = grp.DL;
                    dx += 300;
                    var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                    handlePipeLine(thwPipeLine, arr);
                    for (int i = 0; i < storeys.Count; i++)
                    {
                        var info = arr[i];
                        var segs = info.DisplaySegs ?? info.Segs;
                        if (segs != null)
                        {
                            drawPipes(segs);
                        }
                    }
                }
                if (grp.FL != null)
                {
                    var thwPipeLine = grp.FL;
                    dx += 300;
                    var arr = getPipeRunLocationInfos(basePoint.OffsetX(dx), thwPipeLine, j);
                    handlePipeLine(thwPipeLine, arr);
                    for (int i = 0; i < storeys.Count; i++)
                    {
                        var info = arr[i];
                        var segs = info.DisplaySegs ?? info.Segs;
                        if (segs != null)
                        {
                            drawPipes(segs);
                        }
                    }
                }



            }
        }


        public static bool Testing = false;
        //public static bool Testing = true;
        public static void DrawNoteText(string text, Point2d pt)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = DU.DrawTextLazy(text, 350, pt);
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
namespace ThMEPWSS.Pipe.Service.DrainageServiceNs.ExtensionsNs.DoubleExtensionsNs
{
    using System;
    public static class _DoubleConvertionNs
    {
        public static int ToRatioInt(this double value, int nominator, int denominator)
        {
            return Convert.ToInt32(value / denominator * nominator);
        }
    }
}