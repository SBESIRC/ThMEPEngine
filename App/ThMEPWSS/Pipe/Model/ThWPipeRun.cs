using System;
using DotNetARX;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
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
            yd.OffsetXY(460, -830 - ThWRainPipeRun.FIX_Y_OFFSET);
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
            pl.Layer = "W-RAIN-NOTE";
            DU.DrawEntityLazy(pl);
            var text = "贴底板敷设";
            var height = 300;
            var width = height * 0.8 * text.Length + 200;
            var yd = new YesDraw();
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
            DU.DrawingQueue.Enqueue(adb =>
            {
                var fbk = DrawingTransaction.Cur.fbk;
                if (fbk == null)
                {
                    DU.DrawBlockReference(blkName: "套管系统", basePt: basePt.OffsetXY(-450, 0), cb: br =>
                    {
                        DU.SetLayerAndColorIndex("W-BUSH", 256, br);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "防水套管水平");
                        }
                    });
                }
                else
                {
                    var d = new Dictionary<string, object>() { { "可见性", "防水套管水平" }, };
                    fbk.InsertBlockReference(basePt.OffsetXY(-450, 0), "套管系统", before: br =>
                    {
                        DU.SetLayerAndColorIndex("W-BUSH", 256, br);

                    }, after: br =>
                    {
                        if (br.IsDynamicBlock)
                            foreach (var prop in br.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>().ToList())
                            {
                                if (!prop.ReadOnly)
                                {
                                    if (d.TryGetValue(prop.PropertyName, out object value))
                                    {
                                        prop.Value = value;
                                    }
                                }
                            }
                    });
                }
            });
        }
        public static void DrawFloorDrain(Point3d basePt)
        {
            DU.DrawingQueue.Enqueue(adb =>
            {
                var fbk = DrawingTransaction.Cur.fbk;
                if (fbk == null)
                {
                    DU.DrawBlockReference(blkName: "地漏系统", basePt: basePt.OffsetY(-390), scale: 2, cb: br =>
                    {
                        DU.SetLayerAndColorIndex(ThWPipeCommon.W_RAIN_EQPM, 256, br);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "普通地漏无存水弯");
                        }
                    });
                    return;
                }
                var d = new Dictionary<string, object>() { { "可见性", "普通地漏无存水弯" }, };
                fbk.InsertBlockReference(basePt.OffsetY(-390), "地漏系统", before: br =>
                {
                    br.ScaleFactors = new Scale3d(2);
                    DU.SetLayerAndColorIndex(ThWPipeCommon.W_RAIN_EQPM, 256, br);

                }, after: br =>
                {
                    if (br.IsDynamicBlock)
                        foreach (var prop in br.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>().ToList())
                        {
                            if (!prop.ReadOnly)
                            {
                                if (d.TryGetValue(prop.PropertyName, out object value))
                                {
                                    prop.Value = value;
                                }
                            }
                        }
                });
            });
        }
        public static void DrawCondensePipe(Point3d basePt)
        {
            var c = DU.DrawCircleLazy(basePt, 30);
            DU.SetLayerAndColorIndex("W-RAIN-EQPM", 256, c);
        }

        public static void InsetDNBlock(Point3d pt, string dn, double angle, double scale = 1)
        {
            DU.DrawingQueue.Enqueue(adb =>
            {
                var fbk = DrawingTransaction.Cur.fbk;
                if (fbk == null) return;
                var d = new Dictionary<string, object>() { { "可见性", dn }, { "角度1", angle } };
                fbk.InsertBlockReference(pt, "雨水管径100", before: br =>
                {
                    br.ScaleFactors = new Scale3d(scale);
                    br.Layer = "W-NOTE";
                }, after: br =>
                {
                    if (br.IsDynamicBlock)
                    {
                        foreach (var prop in br.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>())
                        {
                            if (!prop.ReadOnly)
                            {
                                if (d.TryGetValue(prop.PropertyName, out object value))
                                {
                                    prop.Value = value;
                                }
                            }
                        }
                    }
                });
            });
        }
        public static void DrawRainPort(Point3d basePt)
        {
            DU.DrawBlockReference(
                blkName: "$TwtSys$00000132",
                basePt: basePt.OffsetXY(-450, 0),
                cb: br => DU.SetLayerAndColorIndex("W-DRAI-NOTE", 256, br));
        }

        public static void DrawWaterWell(Point3d basePt, string DN)
        {
            DU.DrawBlockReference(
                blkName: "重力流雨水井编号",
                basePt: basePt,
                scale: 0.5,
                props: new Dictionary<string, string>() { { "-", DN ?? "" } },
                layer: "W-RAIN-EQPM");
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
                    t.WidthFactor = .7;
                    DU.SetTextStyleLazy(t, "TH-STYLE3");
                }
            }
        }
        static string fix(string str, string dft = null)
        {
            return string.IsNullOrWhiteSpace(str) ? dft : str;
        }
        public static string GetFloorDrainDN() => fix(ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.BalconyFloorDrainDN, "DN25");//阳台地漏
        public static string GetRoofRainPipeDN() => "DN100";
        public static string GetCondensePipeHorizontalDN() => fix(ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.CondensePipeHorizontalDN, "DN25");//冷凝横管
        public static string GetCondensePipeVerticalDN() => fix(ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.CondensePipeVerticalDN, "DN25");//冷凝立管
        public static string GetBalconyRainPipeDN() => fix(ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.BalconyRainPipeDN, "DN25");//阳台雨立
        public static bool GetCanPeopleBeOnRoof() => ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.CouldHavePeopleOnRoof ?? false;//屋面上人
        public static bool GetHasAiringForCondensePipe() => ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.HasAiringForCondensePipe ?? true;//冷凝立管设通气
        public static bool GetHasAirConditionerFloorDrain() => ThRainSystemService.commandContext?.rainSystemDiagramViewModel.Params.HasAirConditionerFloorDrain ?? true;//空调板夹层地漏
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
            dim.DimLinePoint = GeTools.MidPoint(pt1, pt2).OffsetX(1000);
            dim.DimensionText = "1000";
            dim.Layer = "W-RAIN-EQPM";
            dim.ColorIndex = 256;
            DU.DrawEntityLazy(dim);
        }

        public static void DrawDNLabelRight(Point3d basePt, string lb = "DN100")
        {
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
    public class ThWRainPipeRun
    {
        public const double FIX_Y_OFFSET = 520.0;
        public bool HasBrokenCondensePipe { get; set; }
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
        }
        private void drawLazy(PipeRunDrawingContext ctx)
        {
            if (Storey == null) return;
            if (Dbg.__showXLabel)
            {
                DU.DrawingQueue.Enqueue(adb =>
                {
                    var basePt = ctx.BasePoint;
                    Dbg.ShowXLabel(basePt);
                });
            }

            DrawTranslatorLazy(ctx);
            DrawCheckPointLazy(ctx);
            DrawCondensePipesLazy(ctx);
            DrawFloorDrainsLazy(ctx);
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
                    var dbt = DU.DrawTextLazy(Dr.GetFloorDrainDN(), basePt.OffsetXY(-1000, -500));
                    Dr.SetLabelStylesForRainDims(dbt);
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
                    var dbt = DU.DrawTextLazy(Dr.GetFloorDrainDN(), basePt.OffsetXY(500, -500));
                    Dr.SetLabelStylesForRainDims(dbt);
                    if (fd.HasDrivePipe)
                    {
                        Dr.DrawWrappingPipe(basePt.OffsetXY(900, -550));
                    }
                }
                if (Dr.GetHasAirConditionerFloorDrain() && ctx.ThWRainPipeRun.MainRainPipe.Label.StartsWith("Y2L"))
                {
                    var _basePt = basePt.OffsetY(-800);
                    {
                        var line = DU.DrawLineLazy(_basePt, _basePt.OffsetX(2000));
                        Dr.SetLabelStylesForWNote(line);
                    }
                    Dr.DrawFloorDrain(_basePt.OffsetX(1200 + 180));
                    var yd = new YesDraw();
                    yd.OffsetX(-1200 + 100);
                    yd.OffsetXY(-100, -100);
                    var pts = yd.GetPoint3ds(_basePt.OffsetXY(1200, -550)).ToList();
                    var lines = DU.DrawLinesLazy(pts);
                    ThWRainPipeSystem.SetPipeRunLinesStyle(lines);
                    var dbt = DU.DrawTextLazy(Dr.GetFloorDrainDN(), _basePt.OffsetXY(500, -500));
                    Dr.SetLabelStylesForRainDims(dbt);
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

                        var t = DU.DrawTextLazy(CondensePipes.First().DN, pts.GetLast(2).OffsetXY(100, 100));
                        Dr.SetLabelStylesForRainDims(t);
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
                        Dr.DrawCondensePipe(pt.OffsetX(500 * i - 1200));
                        var p1 = pt.OffsetX(500 * i - 1200);
                        var p2 = p1.OffsetY(-150);
                        var line = DU.DrawLineLazy(p1, p2);
                        ThWRainPipeSystem.SetPipeRunLineStyle(line);
                    }
                    {
                        var p1 = pt.OffsetXY(-1200, -150);
                        var p2 = pt.OffsetY(-150).OffsetX(-130);
                        var p3 = pt.OffsetY(-150).OffsetY(-130);
                        var topPt = p3;
                        ctx.TopPoint = topPt;
                        var lines = DU.DrawLinesLazy(p1, p2, p3);
                        ThWRainPipeSystem.SetPipeRunLinesStyle(lines);
                        var t = DU.DrawTextLazy(Dr.GetCondensePipeHorizontalDN(), p1.OffsetY(-120).OffsetXY(80, 160));
                        Dr.SetLabelStylesForRainDims(t);
                    }
                }
            }
        }
        public static void CalcOffsets(TranslatorTypeEnum translatorType, YesDraw yd)
        {
            switch (translatorType)
            {
                case TranslatorTypeEnum.Long:
                    yd.OffsetY(-280 - FIX_Y_OFFSET + ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
                    yd.Rotate(170, 180 + 45);
                    yd.OffsetX(-1260);
                    yd.Rotate(170, 180 + 45);
                    break;
                case TranslatorTypeEnum.Short:
                    yd.OffsetY(150);
                    yd.GoXY(-150, 0);
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
                switch (TranslatorPipe.TranslatorType)
                {
                    case TranslatorTypeEnum.Short:
                        Dr.DrawShortTranslatorLabel(ctx.BasePoint);
                        break;
                    default:
                        break;
                }
            });
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
