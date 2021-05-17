using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using DU = ThMEPWSS.Assistant.DrawUtils;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWPipeRun : IThWDraw
    {
        virtual public void Draw(Point3d basePt, Matrix3d mat)
        {
            throw new NotImplementedException();
        }

        virtual public void Draw(Point3d basePt)
        {
            throw new NotImplementedException();
        }
    }
    public class DrLazy
    {
        public static readonly DrLazy Default = new DrLazy();
        public Point3d BasePoint;
        public Config cfg = new Config();
        public List<Point3d> curPts = new List<Point3d>();
        public List<List<Point3d>> PointLists = new List<List<Point3d>>();
        public class Config
        {
            public string Layer = "W-RAIN-PIPE";
            public int ColorIndex = 256;
            public Config Clone()
            {
                return (Config)this.MemberwiseClone();
            }
        }
        public void LineTo(Point3d target)
        {
            if (target != BasePoint)
            {
                var list = curPts;
                list.Add(BasePoint);
                list.Add(target);
                BasePoint = target;
            }
        }
        public void Break()
        {
            if (curPts.Count > 0)
            {
                PointLists.Add(curPts);
                curPts = new List<Point3d>();
            }
        }
        public void DrawNormalLine()
        {
            Point3d p;
            var basePt = BasePoint;
            p = basePt.OffsetXY(0, -ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            var list = curPts;
            list.Add(basePt);
            list.Add(p);
            BasePoint = p;
        }
        public void DrawShortTranslator()
        {
            Point3d p;
            var basePt = BasePoint;
            var list = curPts;
            list.Add(basePt);
            var t = GeoAlgorithm.GetXY(170, 45);
            list.Add(basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN + t.Item2));
            p = basePt.OffsetXY(-t.Item1, -ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            list.Add(p);
            BasePoint = p;
        }

        public void DrawLongTranslator()
        {
            Point3d p;
            var basePt = BasePoint;
            var list = curPts;
            p = basePt;
            list.Add(p);
            p = p.OffsetY(-280);
            list.Add(p);
            p = p.Rotate(170, 180 + 45);
            list.Add(p);
            p = p.OffsetX(-1260);
            list.Add(p);
            p = p.Rotate(170, 180 + 45);
            list.Add(p);
            p = basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN).ReplaceX(p.X);
            list.Add(p);
            BasePoint = p;
        }
        public void DrawLazy(Action<Point3d> cb)
        {
            var basePt = BasePoint;
            cb(basePt);
        }
        public void DrawLazy()
        {
            Break();
            var layer = cfg.Layer;
            var colorIndex = cfg.ColorIndex;
            var ptLists = PointLists;
            PointLists = new List<List<Point3d>>();
            DU.DrawingQueue.Enqueue(adb =>
            {
                foreach (var pts in ptLists.Select(lst => YesDraw.FixLines(lst)))
                {
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        var line = new Line() { StartPoint = pts[i], EndPoint = pts[i + 1] };
                        line.Layer = layer;
                        line.ColorIndex = colorIndex;
                        adb.ModelSpace.Add(line);
                    }
                }
            });
        }
    }
    public static class Dr
    {

        public static void DrawStarterPipeHeightLabel(Point3d basePt)
        {
            var text = "起端管底标高-0.65";
            var height = 300;
            var width = height * .7 * text.Length;
            var yd = new YesDraw();
            yd.OffsetXY(0, -700 - 500);
            yd.OffsetX(-width);
            var pts = yd.GetPoint3ds(basePt).ToList();
            var lines = DU.DrawLinesLazy(pts);
            Dr.SetLabelStylesForRainNote(lines.ToArray());
            var t = DU.DrawTextLazy(text, height, pts.Last().OffsetXY(100, 50));
            Dr.SetLabelStylesForRainNote(t);
        }
        public static void DrawRainPortLabel(Point3d basePt)
        {
            var text = "接至雨水口";
            DrawLabel(basePt, text);
        }

        public static void DrawLabel(Point3d basePt, string text)
        {
            var height = 300;
            var width = height * .8 * text.Length;
            var yd = new YesDraw();
            yd.OffsetXY(0, -700);
            yd.OffsetX(-width);
            var pts = yd.GetPoint3ds(basePt).ToList();
            var lines = DU.DrawLinesLazy(pts);
            Dr.SetLabelStylesForRainNote(lines.ToArray());
            var t = DU.DrawTextLazy(text, height, pts.Last().OffsetXY(50, 50));
            Dr.SetLabelStylesForRainNote(t);
        }

        public static void DrawUnderBoardLabelAtLeftTop(Point3d basePt)
        {
            var text = "贴底板敷设";
            var height = 300;
            var width = height * 0.8 * text.Length + 200;
            var yd = new YesDraw();
            //yd.Rotate(505, 90 + 45);
            yd.OffsetXY(460, -830);
            yd.OffsetX(width);
            var pts = yd.GetPoint3ds(basePt).ToList();
            var lines = DU.DrawLinesLazy(pts);
            Dr.SetLabelStylesForRainNote(lines.ToArray());
            var t = DU.DrawTextLazy(text, height, pts[1].OffsetXY(100, 50));
            Dr.SetLabelStylesForRainNote(t);
        }
        public static void DrawUnderBoardLabelAtRightButtom(Point3d basePt)
        {
            var pl = ThMEPWSS.Pipe.Service.PolylineTools.CreatePolyline(new Point3d[] { basePt.OffsetXY(-75, -75), basePt.OffsetXY(75, 75) });
            pl.ConstantWidth = 25;
            DU.DrawEntityLazy(pl);
            var text = "贴底板敷设";
            var height = 300;
            var width = height * 0.8 * text.Length + 200;
            var yd = new YesDraw();
            //yd.Rotate(505, 90 + 45);
            yd.OffsetXY(-450, 830);
            yd.OffsetX(-width);
            var pts = yd.GetPoint3ds(basePt).ToList();
            var lines = DU.DrawLinesLazy(pts);
            Dr.SetLabelStylesForRainNote(lines.ToArray());
            var t = DU.DrawTextLazy(text, height, pts.Last().OffsetXY(100, 50));
            Dr.SetLabelStylesForRainNote(t);
        }
        public static void DrawWrappingPipe(Point3d basePt)
        {
            DU.DrawBlockReference(blkName: "*U349", basePt: basePt.OffsetXY(-450, 0), cb: br => DU.SetLayerAndColorIndex("W-BUSH", 256, br));
        }
        public static void DrawFloorDrain(Point3d basePt)
        {
            DU.DrawBlockReference(blkName: "地漏系统", basePt: basePt.OffsetY(-390), scale: 2, cb: br =>
            {
                DU.SetLayerAndColorIndex(ThWPipeCommon.W_RAIN_EQPM, 256, br);
                if (br.IsDynamicBlock)
                {
                    br.ObjectId.SetDynBlockValue("可见性", "普通地漏无存水弯");
                }
            });
            //DU.DrawBlockReference(blkName: "*U348", basePt: basePt.OffsetY(-390), scale: 2, cb: br => DU.SetLayerAndColorIndex(ThWPipeCommon.W_RAIN_EQPM, 256, br));
        }
        public static void DrawCondensePipe(Point3d basePt)
        {
            var c = DU.DrawCircleLazy(basePt, 30);
            DU.SetLayerAndColorIndex("W-RAIN-EQPM", 256, c);
        }
        public static void DrawRainPort(Point3d basePt)
        {
            DU.DrawBlockReference(blkName: "$TwtSys$00000132", basePt: basePt.OffsetXY(-450, 0), cb: br => DU.SetLayerAndColorIndex("W-DRAI-NOTE", 256, br));
        }

        public static void DrawWaterWell(Point3d basePt, string DN)
        {
            DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: basePt,
                scale: 0.5,
                props: new Dictionary<string, string>() { { "-", DN ?? "" } },
               layer: "W-RAIN-EQPM"
               );
        }

        public static void DrawShortTranslatorLabel(Point3d basePt)
        {
            var text = "DN100乙字弯";
            var height = 300;
            var width = height * .7 * text.Length + 10;
            var yd = new YesDraw();
            yd.OffsetXY(-800, 1000);
            yd.OffsetX(-width);
            basePt = basePt.OffsetXY(-67, 83);
            var pts = yd.GetPoint3ds(basePt).ToList();
            var lines = DU.DrawLinesLazy(pts);
            SetLabelStylesForRainNote(lines.ToArray());
            var t = DU.DrawTextLazy(text, height, pts.Last().OffsetXY(100, 50));
            SetLabelStylesForRainNote(t);
        }

        public static void DrawLabelLeft(Point3d basePt, string text)
        {
            var height = 300;
            {
                var width = height * 0.8 * text.Length + 200;
                var yd = new YesDraw();
                yd.Rotate(505, 90 + 45);
                yd.OffsetX(-width);
                var pts = yd.GetPoint3ds(basePt).ToList();
                var lines = DU.DrawLinesLazy(pts);
                SetLabelStylesForRainNote(lines.ToArray());
                var t = DU.DrawTextLazy(text, height, pts.Last().OffsetXY(100, 50));
                SetLabelStylesForRainNote(t);
            }
            return;

            {
                var t = DU.DrawTextLazy(text, height, basePt.OffsetXY(-2854, 954));
                //t.TextStyleName = "TH-STYLE3";
                //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
                var line = DU.DrawTextUnderlineLazy(t, 10, 10);
                SetLabelStylesForRainNote(t, line);
                line = DU.DrawLineLazy(line.EndPoint, basePt.OffsetXY(-60, 60));
                SetLabelStylesForRainNote(line);
            }
        }
        public static void DrawLabelRight(Point3d basePt, string txt)
        {
            return;
            var h = 300;
            var t = DU.DrawTextLazy(txt, h, basePt.OffsetXY(2854, 954));
            var line = DU.DrawTextUnderlineLazy(t, 10, 10);
            SetLabelStylesForRainNote(t, line);
            line = DU.DrawLineLazy(line.StartPoint, basePt.OffsetXY(60, -60));
            SetLabelStylesForRainNote(line);
        }
        public static void SetLabelStylesForRainNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-RAIN-NOTE";
                e.ColorIndex = 256;
                if (e is DBText t)
                {
                    t.WidthFactor = .7;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static void SetLabelStylesForRainDims(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-RAIN-DIMS";
                e.ColorIndex = 256;
                if (e is DBText t)
                {
                    t.WidthFactor = 0.8;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }

        public static void SetLabelStylesForWNote(params Entity[] ents)
        {
            foreach (var e in ents)
            {
                e.Layer = "W-NOTE";
                e.ColorIndex = 256;
                if (e is DBText t)
                {
                    t.WidthFactor = 0.7;
                    t.Height = 350;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        public static void DrawLabelRight(Point3d basePt, string txt1, string txt2)
        {
            var h = 300;
            var t1 = DU.DrawTextLazy(txt1, h, basePt.OffsetXY(2854, 954));
            var t2 = DU.DrawTextLazy(txt2, h, basePt.OffsetXY(2854, 954));
            var line = DU.DrawTextUnderlineLazy(t1, 10, 10);
            line = DU.DrawLineLazy(line.StartPoint, basePt.OffsetXY(60, -60));
            SetLabelStylesForRainNote(line, t1, t2);
        }
        public static void DrawNormalLine(Point3d basePt)
        {
            basePt = basePt.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            var line = DrawUtils.DrawLineLazy(basePt, basePt.OffsetXY(0, -ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
            line.Layer = "W-RAIN-PIPE";
            line.ColorIndex = 256;
        }
        public static double DrawShortTranslator(Point3d basePt)
        {
            Point3d p;
            basePt = basePt.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            var list = new List<Point3d>();
            list.Add(basePt);
            var t = GeoAlgorithm.GetXY(170, 45);
            list.Add(basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN + t.Item2));
            p = basePt.OffsetXY(-t.Item1, -ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            list.Add(p);
            var lines = DrawUtils.DrawLinesLazy(list);
            lines.ForEach(line =>
            {
                line.Layer = "W-RAIN-PIPE";
                line.ColorIndex = 256;
            });
            var deltax = p.X - basePt.X;
            return deltax;
        }
        public static double DrawLongTranslator(Point3d basePt)
        {
            Point3d p;
            basePt = basePt.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            var list = new List<Point3d>();
            p = basePt;
            list.Add(p);
            p = p.OffsetY(-280);
            list.Add(p);
            p = p.Rotate(170, 180 + 45);
            list.Add(p);
            p = p.OffsetX(-1260);
            list.Add(p);
            p = p.Rotate(170, 180 + 45);
            list.Add(p);
            p = basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN).ReplaceX(p.X);
            list.Add(p);
            var lines = DrawUtils.DrawLinesLazy(list);
            lines.ForEach(line =>
            {
                line.Layer = "W-RAIN-PIPE";
                line.ColorIndex = 256;
            });
            var deltax = p.X - basePt.X;
            return deltax;
        }
        public static void DrawSideWaterBucketLabel(Point3d basePt, string label = "侧入式雨水斗DN100")
        {
            DrawLabelForRainNote(basePt, label);
        }
        public static void DrawPipeLabel(Point3d basePt, string text1, string text2)
        {
            var height = 350;
            var width = Math.Max(height * 0.7 * text1.Length, height * 0.7 * text2.Length);
            var yd = new YesDraw();
            yd.Rotate(505, 45);
            yd.OffsetX(width);
            basePt = basePt.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN * .2);
            var pts = yd.GetPoint3ds(basePt).ToList();
            var lines = DU.DrawLinesLazy(pts);
            SetLabelStylesForRainNote(lines.ToArray());
            var t1 = DU.DrawTextLazy(text1, height, pts[1].OffsetXY(100, 50));
            var t2 = DU.DrawTextLazy(text2, height, pts[1].OffsetXY(100, -50 - height));
            SetLabelStylesForRainNote(t1, t2);
        }
        private static void DrawLabelForRainNote(Point3d basePt, string text)
        {
            var height = 350;
            {
                var width = height * 0.7 * text.Length + 200;
                var yd = new YesDraw();
                yd.Rotate(505, 90 + 45);
                yd.OffsetX(-width);
                var pts = yd.GetPoint3ds(basePt).ToList();
                var lines = DU.DrawLinesLazy(pts);
                SetLabelStylesForRainNote(lines.ToArray());
                var t = DU.DrawTextLazy(text, height, pts.Last().OffsetXY(100, 50));
                SetLabelStylesForRainNote(t);
            }
            return;

            {
                var t = DU.DrawTextLazy(text, height, basePt.OffsetXY(-4000, 954));
                //t.TextStyleName = "TH-STYLE3";
                //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
                //t.Objectxxxxxxxxxxxxxx(t,,"TH-STYLE3");
                var line = DU.DrawTextUnderlineLazy(t, 10, 10);
                SetLabelStylesForRainNote(t, line);
                line = DU.DrawLineLazy(line.EndPoint, basePt.OffsetXY(-60, 60));
                SetLabelStylesForRainNote(line);
            }
        }

        public static void DrawSideWaterBucket(Point3d basePt)
        {
            DU.DrawBlockReference("侧排雨水斗系统", basePt, layer: "W-RAIN-EQPM", cb: br => br.ColorIndex = 256);
        }
        public static void DrawGravityWaterBucketLabel(Point3d basePt, string label = "重力雨水斗DN100")
        {
            DrawLabelForRainNote(basePt, label);
        }

        public static void DrawGravityWaterBucket(Point3d basePt)
        {
            DU.DrawBlockReference("屋面雨水斗", basePt, layer: "W-RAIN-EQPM", cb: br => br.ColorIndex = 256);
        }

        public static void DrawCheckPoint(Point3d basePt)
        {
            //var pt = basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            DU.DrawBlockReference("立管检查口", basePt.OffsetY(800), br =>
            {
                br.Layer = "W-RAIN-EQPM";
                br.Rotation = GeoAlgorithm.AngleFromDegree(180);
            });
        }

        public static void DrawCheckPointLabel(Point3d basePt)
        {
            DrawDNLabelLeft(basePt);
            DrawDimLabelRight(basePt);
        }

        private static void DrawDimLabelRight(Point3d basePt)
        {
            var pt1 = basePt;
            var pt2 = pt1.OffsetY(800);
            var dim = new AlignedDimension();
            dim.XLine1Point = pt1;
            dim.XLine2Point = pt2;
            //dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).PolarPoint(Math.PI / 2, 1000);
            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(1000);
            dim.DimensionText = "1000";
            //dim.ColorIndex = 3;
            dim.Layer = "W-RAIN-EQPM";
            dim.ColorIndex = 256;
            DU.DrawEntityLazy(dim);
        }

        public static void DrawDNLabelRight(Point3d basePt, string lb = "DN100")
        {
            //var t = DU.DrawTextLazy(lb, 250, basePt);
            //t.Rotate(basePt.OffsetX(100), GeoAlgorithm.AngleFromDegree(90));
            //SetLabelStylesForRainDims(t);

            basePt = basePt.OffsetXY(300, 200);
            var t = DU.DrawTextLazy(lb, 250, basePt);
            t.Rotate(basePt, GeoAlgorithm.AngleFromDegree(90));
            SetLabelStylesForRainDims(t);
        }
        public static void DrawDNLabel(Point3d basePt, string lb = "DN100")
        {
            var t = DU.DrawTextLazy(lb, 250, basePt);
            SetLabelStylesForRainDims(t);
        }
        public static void DrawDNLabelLeft(Point3d basePt, string lb = "DN100")
        {
            //var w = (250 * 0.8 * lb.Length);
            //var offsetX = -250;
            //var offsetY = (ThWRainSystemDiagram.VERTICAL_STOREY_SPAN - w) / 2;

            basePt = basePt.OffsetXY(-300, 200);
            var t = DU.DrawTextLazy(lb, 250, basePt);
            t.Rotate(basePt, GeoAlgorithm.AngleFromDegree(90));
            SetLabelStylesForRainDims(t);
        }
    }
    public class PipeRunDrawingContext
    {
        public Point3d BasePoint;
        public YesDraw YesDraw = new YesDraw();
        public Point3d? TopPoint;
        public ThWRainPipeRun ThWRainPipeRun;
    }
    public class ThWRainPipeRun //: ThWPipeRun, IEquatable<ThWRainPipeRun>
    {
        public bool HasBrokenCondensePipe;
        /// <summary>
        /// 楼层
        /// </summary>
        public ThWSDStorey Storey { get; set; } = new ThWSDStorey();

        /// <summary>
        /// 主雨水管
        /// </summary>
        public ThWSDPipe MainRainPipe { get; set; } = new ThWSDPipe();

        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThWSDFloorDrain> FloorDrains { get; set; } = new List<ThWSDFloorDrain>();

        /// <summary>
        /// 冷凝管
        /// </summary>
        public List<ThWSDCondensePipe> CondensePipes { get; set; } = new List<ThWSDCondensePipe>();

        /// <summary>
        /// 转管
        /// </summary>
        public ThWSDTranslatorPipe TranslatorPipe { get; set; } = new ThWSDTranslatorPipe();

        /// <summary>
        /// 检查口
        /// </summary>
        public ThWSDCheckPoint CheckPoint { get; set; } = new ThWSDCheckPoint();

        /// <summary>
        /// 冷凝管是否要画在偏下方
        /// </summary>
        public bool IsLow => CondensePipes.Select(cp => cp.IsLow).FirstOrDefault();

        public ThWRainPipeRun()
        {
        }
        public void Draw(PipeRunDrawingContext ctx)
        {
            drawLazy(ctx);
            //draw(ctx);
        }
        private void drawLazy(PipeRunDrawingContext ctx)
        {
            if (Storey == null) return;
            DU.DrawingQueue.Enqueue(adb =>
            {
                var basePt = ctx.BasePoint;
                Dbg.ShowXLabel(basePt);
            });

            DrawTranslatorLazy(ctx);
            DrawCheckPointLazy(ctx);
            DrawCondensePipesLazy(ctx);
            DrawFloorDrainsLazy(ctx);
        }
        private void draw(PipeRunDrawingContext ctx)
        {
            DrawTranslator(ctx);
            DrawCheckPoint(ctx);
            DrawCondensePipes(ctx);
            DrawFloorDrains(ctx);
        }

        private void DrawCheckPointLazy(PipeRunDrawingContext ctx)
        {
            DU.DrawingQueue.Enqueue(adb =>
            {
                DrawCheckPoint(ctx);
            });
        }
        Point3d GetBasePoint(Point3d pt)
        {
            if (TranslatorPipe.TranslatorType == TranslatorTypeEnum.Long)
            {
                var yd = new YesDraw();
                ThWRainPipeRun.CalcOffsets(TranslatorPipe.TranslatorType, yd);
                var dx = yd.GetCurX();
                return pt.OffsetX(dx);
            }
            return pt;
        }
        private void DrawCheckPoint(PipeRunDrawingContext ctx)
        {
            var basePt = GetBasePoint(ctx.BasePoint);
            if (CheckPoint.HasCheckPoint)
            {
                Dr.DrawCheckPoint(basePt);
                Dr.DrawCheckPointLabel(basePt);
            }
        }

        private void DrawFloorDrainsLazy(PipeRunDrawingContext ctx)
        {
            DU.DrawingQueue.Enqueue(adb =>
            {
                DrawFloorDrains(ctx);
            });
        }

        private void DrawFloorDrains(PipeRunDrawingContext ctx)
        {
            var basePt = GetBasePoint(ctx.BasePoint);
            var fds = FloorDrains.Where(fd => fd.HasDrivePipe).Concat(FloorDrains.Where(fd => !fd.HasDrivePipe)).ToList();
            if (fds.Count == 1 || fds.Count == 2)
            {
                {
                    var fd = fds[0];
                    Dr.DrawFloorDrain(basePt.OffsetX(-1200 + 180));
                    var yd = new YesDraw();
                    yd.OffsetX(1200 - 100);
                    yd.OffsetXY(100, -100);
                    var pts = yd.GetPoint3ds(basePt.OffsetXY(-1200, -550)).ToList();
                    var lines = DU.DrawLinesLazy(pts);
                    ThWRainPipeSystem.SetPipeRunLinesStyle(lines);
                    if (fd.HasDrivePipe)
                    {
                        Dr.DrawWrappingPipe(basePt.OffsetY(-550));
                    }
                }
                if (fds.Count == 2)
                {
                    var fd = fds[1];
                    Dr.DrawFloorDrain(basePt.OffsetX(1200 + 180));
                    var yd = new YesDraw();
                    yd.OffsetX(-1200 + 100);
                    yd.OffsetXY(-100, -100);
                    var pts = yd.GetPoint3ds(basePt.OffsetXY(1200, -550)).ToList();
                    var lines = DU.DrawLinesLazy(pts);
                    ThWRainPipeSystem.SetPipeRunLinesStyle(lines);
                    if (fd.HasDrivePipe)
                    {
                        Dr.DrawWrappingPipe(basePt.OffsetXY(900, -550));
                    }
                }
                return;
            }
            for (int i = 0; i < fds.Count; i++)
            {
                var fd = fds[i];
                var pt = basePt;
                Dr.DrawFloorDrain(pt.OffsetX(-1000 + 1900 * i));
                pt = pt.OffsetY(-550);
                if (i > 0)
                {
                    pt = pt.OffsetX(-1000 + 1900 * i - 180);
                }
                var line = DU.DrawLineLazy(pt.OffsetX(-1000 - 180), pt);
                {
                    var p2 = pt.OffsetX(-1000 - 180).OffsetXY(100, 100);
                    if (i > 0)
                    {
                        p2 = p2.OffsetX(500);
                    }
                    DU.DrawTextLazy(fd.DN, p2);
                }
                ThWRainPipeSystem.SetPipeRunLineStyle(line);
                if (fd.HasDrivePipe)
                {
                    Dr.DrawWrappingPipe(pt);
                }
            }
        }

        private void DrawCondensePipesLazy(PipeRunDrawingContext ctx)
        {
            DrawCondensePipes(ctx);
        }

        private void DrawCondensePipes(PipeRunDrawingContext ctx)
        {
            var basePt = GetBasePoint(ctx.BasePoint);
            if (CondensePipes.Count > 0)
            {
                if (HasBrokenCondensePipe)
                {
                    for (int i = 0; i < CondensePipes.Count; i++)
                    {
                        //var cp = CondensePipes[i];
                        //var p1 = pt.OffsetXY(-500 - 900, 300 * i);
                        //Dr.DrawCondensePipe(p1);
                        //var p2 = p1.OffsetY(-150);
                        //var p3 = p2.ReplaceX(basePt.X);
                        //DU.DrawLineLazy(p2, p3);
                        //var p4 = p2.OffsetY(120);
                        //DU.DrawLineLazy(p2, p4);
                        {
                            var yd = new YesDraw();
                            yd.OffsetXY(-150, 150);
                            yd.OffsetX(-1000);
                            yd.OffsetY(150);
                            var topPt = basePt.OffsetY(30 + 650 * i);
                            ctx.TopPoint = topPt;
                            var pts = yd.GetPoint3ds(topPt).ToList();
                            var lines = DU.DrawLinesLazy(YesDraw.FixLines(pts));
                            ThWRainPipeSystem.SetPipeRunLinesStyle(lines);
                            Dr.DrawCondensePipe(pts.Last().OffsetXY(-100, 100));
                            //todo:文字变图块
                            var t = DU.DrawTextLazy(CondensePipes.First().DN, pts.GetLast(2).OffsetXY(100, 100));
                            Dr.SetLabelStylesForRainDims(t);
                        }

                    }
                }
                else
                {
                    var pt = basePt.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN / 2);
                    if (ctx.ThWRainPipeRun.CheckPoint.HasCheckPoint || ctx.ThWRainPipeRun.IsLow)
                    {
                        pt = pt.OffsetY(-600);
                    }
                    for (int i = 0; i < CondensePipes.Count; i++)
                    {
                        var cp = CondensePipes[i];
                        Dr.DrawCondensePipe(pt.OffsetX(500 * i - 1200));
                        var p1 = pt.OffsetX(500 * i - 1200);
                        var p2 = p1.OffsetY(-150);
                        var line = DU.DrawLineLazy(p1, p2);
                        ThWRainPipeSystem.SetPipeRunLineStyle(line);
                    }
                    {
                        var p1 = pt.OffsetXY(-1200, -150);
                        //var p2 = pt.OffsetY(-150);
                        //var line = DU.DrawLineLazy(p1, p2);
                        //ThWRainPipeSystem.SetPipeRunLineStyle(line);
                        var p2 = pt.OffsetY(-150).OffsetX(-130);
                        var p3 = pt.OffsetY(-150).OffsetY(-130);
                        var topPt = p3;
                        ctx.TopPoint = topPt;
                        var lines = DU.DrawLinesLazy(p1, p2, p3);
                        ThWRainPipeSystem.SetPipeRunLinesStyle(lines);
                        var t = DU.DrawTextLazy(CondensePipes.First().DN, p1.OffsetY(-120).OffsetXY(80, 160));
                        Dr.SetLabelStylesForRainDims(t);
                    }
                }
            }
        }
        public static void DrawBalconyCondensePipes()
        {
            //室内机的冷凝水（若存在）在系统图上都放在左侧。数量和高度需根据平面图的画法判断。高度存在两层。
            //Case1:一个点位 高/低
            //高：在外参中，冷凝水点2000范围内若存在Ah1或Ah2（可能多个），且Ah2距离最近（下文称为Ah2近）。若2000范围内不存在，则默认为高。
            //低：Ah1近
            //Case2:两个点位 同层 高/低
            //两个点位连接到立管的直线没有发生中断，则这两个点位的高度视为相同。Ah2近则两个点位都高，Ah1近则两个点位都低。
            //Case3:两个点位 不同高
            //两个点位连接到立管上，其中一个发生了中断（至少30间距）则视为两个点不在一个高度上。风险是用户绘制的质量。
            //室外机冷凝地漏也分高低两层。
            //Type1:高层地漏
            //平面图上绘制的不带穿墙套管的地漏认为是高层的室外机冷凝地漏。若此立管没有接雨水地漏则将此地漏置于左上角，否则置于右上角。
            //Type2:低层地漏
            //低层地漏并不表达在图纸上，需用逻辑判断。
            //若一根立管负担了2个室内机冷凝水排水，且在UI上勾选了“空调板夹层地漏”，才要在系统图上增加一个地漏。
            //综合复杂情况
            //左侧自上而下 雨水地漏、2个高层室内机冷凝水点位
            //右侧自上而下 室外机冷凝水高层地漏、室外机冷凝水低层地漏
            //左侧自上而下 室外机冷凝水高层地漏、室内机冷凝水高层点位、室内机冷凝水低层点位
            //右侧 室外机冷凝水低层地漏
        }
        public static void CalcOffsets(TranslatorTypeEnum translatorType, YesDraw yd)
        {
            switch (translatorType)
            {
                case TranslatorTypeEnum.None:
                    break;
                case TranslatorTypeEnum.Long:
                    yd.OffsetY(-280 + ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
                    yd.Rotate(170, 180 + 45);
                    yd.OffsetX(-1260);
                    yd.Rotate(170, 180 + 45);
                    break;
                case TranslatorTypeEnum.Short:
                    yd.OffsetY(150);
                    yd.GoXY(-150, 0);
                    break;
                case TranslatorTypeEnum.Gravity:
                    yd.OffsetY(-280);
                    yd.Rotate(170, 180 + 45);
                    yd.OffsetX(-1260);
                    yd.Rotate(170, 180 + 45);
                    break;
                default:
                    break;
            }
        }
        private void DrawTranslatorLazy(PipeRunDrawingContext ctx)
        {
            if (Storey == null) return;
            DU.DrawingQueue.Enqueue(adb =>
            {
                var basePt = ctx.BasePoint;
                switch (TranslatorPipe.TranslatorType)
                {
                    case TranslatorTypeEnum.None:
                        break;
                    case TranslatorTypeEnum.Long:
                        if (false) Dr.DrawLabelLeft(basePt, "Long");
                        break;
                    case TranslatorTypeEnum.Short:
                        Dr.DrawShortTranslatorLabel(basePt);
                        break;
                    case TranslatorTypeEnum.Gravity:
                        if (false) Dr.DrawLabelLeft(basePt, "Gravity");
                        break;
                    default:
                        break;
                }
            });
        }

        private void DrawTranslator(PipeRunDrawingContext ctx)
        {
            if (Storey == null) return;
            var basePt = ctx.BasePoint;
            switch (TranslatorPipe.TranslatorType)
            {
                case TranslatorTypeEnum.None:
                    Dr.DrawNormalLine(basePt);
                    break;
                case TranslatorTypeEnum.Long:
                    Dr.DrawLongTranslator(basePt);
                    break;
                case TranslatorTypeEnum.Short:
                    Dr.DrawShortTranslator(basePt);
                    Dr.DrawShortTranslatorLabel(basePt);
                    break;
                case TranslatorTypeEnum.Gravity:
                    Dr.DrawLongTranslator(basePt);
                    break;
                default:
                    break;
            }
        }

        public override int GetHashCode()
        {
            return this.Storey.GetHashCode();
        }
        public bool Equals(ThWRainPipeRun other)
        {
            return this.Storey.Equals(other.Storey)
                && this.MainRainPipe.Equals(other.MainRainPipe)
                && FloorDrains.Count.Equals(other.FloorDrains.Count)
                && CondensePipes.Count.Equals(other.CondensePipes.Count)
                && TranslatorPipe.Equals(other.TranslatorPipe)
                && CheckPoint.Equals(other.CheckPoint);
        }
    }
}
