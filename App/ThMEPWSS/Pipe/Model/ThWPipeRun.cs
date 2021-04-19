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
    public static class TranslatorLazyDrawer
    {
        class Context
        {
            public Point3d BasePoint;
            public Point3d Start;
            public Point3d End;
            public TranslatorTypeEnum TranslatorType;
        }
        static List<Context> Contexts = new List<Context>();
        public static void DrawNormal(Point3d basePt)
        {
            //DU.DrawTextLazy("DrawNormal", basePt);
            //Contexts.Add(new Context() { BasePoint = basePt, TranslatorType = TranslatorTypeEnum.None });
            Dr.DrawNormalLine(basePt);
        }
        public static void DrawShort(Point3d basePt)
        {
            //DU.DrawTextLazy("DrawShort", basePt);
            //Contexts.Add(new Context() { BasePoint = basePt, TranslatorType = TranslatorTypeEnum.Short });
            Dr.DrawShortTranslator(basePt);
        }
        public static void DrawLong(Point3d basePt)
        {
            //DU.DrawTextLazy("DrawLong", basePt);
            //Contexts.Add(new Context() { BasePoint = basePt, TranslatorType = TranslatorTypeEnum.Long });
            Dr.DrawLongTranslator(basePt);
        }
        public static void Test()
        {

        }
        public static void DrawPipeLazy(Point3d start, Point3d ent)
        {
            //if (Contexts.Count == 0) return;
            //var list = Contexts.OrderByDescending(ctx => ctx.BasePoint.Y).ToList();
            //Contexts.Clear();
            //var points = new List<Point3d>();
            
            //foreach (var item in list)
            //{
            //    switch (item.TranslatorType)
            //    {
            //        case TranslatorTypeEnum.None:
            //            {
            //                item.Start = item.BasePoint.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            //                item.End = item.BasePoint;
            //            }
            //            break;
            //        case TranslatorTypeEnum.Long:
            //            {
            //                item.Start = item.BasePoint.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            //                item.End = item.Start.OffsetXY(-3000, -1000);
            //            }
            //            break;
            //        case TranslatorTypeEnum.Short:
            //            {
            //                item.Start = item.BasePoint.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            //                item.End = item.Start.OffsetXY(-1000, -1000);
            //            }
            //            break;
            //        default:
            //            break;
            //    }
            //}
            //foreach (var item in list)
            //{
            //    DU.DrawLineLazy(item.Start, item.End);
            //}
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
        public static List<Point3d> FixLines(List<Point3d> pts)
        {
            if (pts.Count < 2) return pts;
            var ret = new List<Point3d>(pts.Count);
            Point3d p1 = pts[0], p2 = pts[1];
            for (int i = 2; i < pts.Count; i++)
            {
                var p3 = pts[i];
                var v1 = p2.ToPoint2D() - p1.ToPoint2D();
                var v2 = p3.ToPoint2D() - p2.ToPoint2D();
                if (
                    (Math.Abs(GeoAlgorithm.AngleToDegree(v1.Angle) - GeoAlgorithm.AngleToDegree(v2.Angle)) < 1)
                    ||
                    (GeoAlgorithm.Distance(p1, p2) < 1)
                    ||
                    (GeoAlgorithm.Distance(p2, p3) < 1)
                    )
                {
                }
                else
                {
                    ret.Add(p1);
                    p1 = p2;
                }
                p2 = p3;
            }
            ret.Add(p1);
            ret.Add(p2);
            return ret;
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
                foreach (var pts in ptLists.Select(lst => FixLines(lst)))
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
        public static void DrawWaterWell(Point3d basePt)
        {
            DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: basePt,
                scale: 0.5,
                props: new Dictionary<string, string>() { { "-", "666" } },
               layer: "W-RAIN-EQPM"
               );
        }

        public static void DrawShortTranslatorLabel(Point3d basePt)
        {
            var h = 300;
            var t = DU.DrawTextLazy("DN100乙字弯", h, basePt.OffsetXY(-2854, 954));
            t.Layer = "W-RAIN-NOTE";
            t.ColorIndex = 256;
            //t.TextStyleName = "TH-STYLE3";
            //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
            //t.ObjectId.SetTextStyle("TH-STYLE3");
            var line = DU.DrawTextUnderlineLazy(t, 10, 10);
            line.Layer = "W-RAIN-NOTE";
            line.ColorIndex = 256;
            line = DU.DrawLineLazy(line.EndPoint, basePt.OffsetXY(-60, 60));
            line.Layer = "W-RAIN-NOTE";
            line.ColorIndex = 256;
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
        public static void DrawSideWaterBucketLabel(Point3d basePt)
        {
            var h = 300;
            var t = DU.DrawTextLazy("侧入式雨水斗DN100", h, basePt.OffsetXY(-4000, 954));
            t.Layer = "W-RAIN-NOTE";
            t.ColorIndex = 256;
            //t.TextStyleName = "TH-STYLE3";
            //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
            //t.ObjectId.SetTextStyle("TH-STYLE3");
            var line = DU.DrawTextUnderlineLazy(t, 10, 10);
            line.Layer = "W-RAIN-NOTE";
            line.ColorIndex = 256;
            line = DU.DrawLineLazy(line.EndPoint, basePt.OffsetXY(-60, 60));
            line.Layer = "W-RAIN-NOTE";
            line.ColorIndex = 256;
        }
        public static void DrawSideWaterBucket(Point3d basePt)
        {
            DU.DrawBlockReference("侧排雨水斗系统", basePt, layer: "W-RAIN-EQPM", cb: br => br.ColorIndex = 256);
        }
        public static void DrawGravityWaterBucketLabel(Point3d basePt)
        {
            var h = 300;
            var t = DU.DrawTextLazy("重力雨水斗DN100", h, basePt.OffsetXY(-3500, 954));
            t.Layer = "W-RAIN-NOTE";
            t.ColorIndex = 256;
            //t.TextStyleName = "TH-STYLE3";
            //var tb = AcHelper.Collections.Tables.GetTextStyle("TH-STYLE3");
            //t.ObjectId.SetTextStyle("TH-STYLE3");
            var line = DU.DrawTextUnderlineLazy(t, 10, 10);
            line.Layer = "W-RAIN-NOTE";
            line.ColorIndex = 256;
            line = DU.DrawLineLazy(line.EndPoint, basePt.OffsetXY(-60, 60));
            line.Layer = "W-RAIN-NOTE";
            line.ColorIndex = 256;
        }

        public static void DrawGravityWaterBucket(Point3d basePt)
        {
            DU.DrawBlockReference("屋面雨水斗", basePt, layer: "W-RAIN-EQPM", cb: br => br.ColorIndex = 256);
        }

        public static void DrawCheckPoint(Point3d basePt)
        {
            //var pt = basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            DU.DrawBlockReference("立管检查口", basePt.OffsetY(ThWRainSystemDiagram.VERTICAL_STOREY_SPAN / 2), br =>
            {
                br.Layer = "W-RAIN-EQPM";
                br.Rotation = GeoAlgorithm.AngleFromDegree(180);
            });
        }

        public static void DrawCheckPointLabel(Point3d basePt)
        {
            //var pt = basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            var t = DU.DrawTextLazy("DN100", 200, basePt);
            t.Rotate(basePt.OffsetX(-400), GeoAlgorithm.AngleFromDegree(90));
        }
    }
    public class ThWRainPipeRun //: ThWPipeRun, IEquatable<ThWRainPipeRun>
    {
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
        public List<ThWSDDrain> FloorDrains { get; set; } = new List<ThWSDDrain>();

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

        public ThWRainPipeRun()
        {
            //FloorDrains = new List<ThWFloorDrain>();
            //CondensePipes = new List<ThWCondensePipe>();
        }

        //override public void Draw(Point3d _basePt)
        public void Draw(Point3d basePt)
        {
            //DU.DrawRectLazyFromLeftButtom(basePt.OffsetXY(100,100),2000,1000);

            if (CheckPoint.HasCheckPoint)
            {
                Dr.DrawCheckPoint(basePt);
                Dr.DrawCheckPointLabel(basePt);
                //DrLazy.Default.DrawLazy(Dr.DrawCheckPoint);
                //DrLazy.Default.DrawLazy(Dr.DrawCheckPointLabel);
                //Dbg.ShowXLabel(DrLazy.Default.BasePoint);
            }

            //DrawGravityWaterBucket(basePt);
            //DrawSideWaterBucket(basePt);
            if (Storey != null)
            {
                switch (TranslatorPipe.TranslatorType)
                {
                    case TranslatorTypeEnum.None:
                        if (false) Dr.DrawNormalLine(basePt);
                        TranslatorLazyDrawer.DrawNormal(basePt);
                        //DrLazy.Default.DrawNormalLine();
                        break;
                    case TranslatorTypeEnum.Long:
                        if (false) Dr.DrawLongTranslator(basePt);
                        TranslatorLazyDrawer.DrawLong(basePt);
                        //DrLazy.Default.DrawLongTranslator();
                        break;
                    case TranslatorTypeEnum.Short:
                        if (false) Dr.DrawShortTranslator(basePt);
                        if (false) Dr.DrawShortTranslatorLabel(basePt);
                        TranslatorLazyDrawer.DrawShort(basePt);
                        Dr.DrawShortTranslatorLabel(basePt);
                        //DrLazy.Default.DrawShortTranslator();
                        //DrLazy.Default.DrawLazy(Dr.DrawShortTranslatorLabel);
                        break;
                    default:
                        break;
                }
                if (false) OldTestCode(basePt);
                if (false)
                {
                    Dr.DrawCheckPoint(basePt);
                    Dr.DrawNormalLine(basePt);
                    Dr.DrawLongTranslator(basePt);
                    Dr.DrawShortTranslator(basePt);
                    Dr.DrawShortTranslatorLabel(basePt);
                    Dr.DrawWaterWell(basePt);
                    return;
                }
            }
            else
            {
                //DrawUtils.DrawTextLazy($"雨水立管，TranslatorPipe.Label:{TranslatorPipe.Label} Storey is null ...", 100, basePt);
            }

            //NoDraw.Text("ThWRainPipeRun " + TranslatorPipe.Label, 100, basePt).AddToCurrentSpace();
            //return;
            //MainRainPipe.Draw(basePt);
            //todo

        }



        private void OldTestCode(Point3d basePt)
        {
            DrawUtils.DrawTextLazy($"雨水立管，Label:{TranslatorPipe.Label} Storey.Label:{Storey.Label}", 100, basePt);

            var r = DrawUtils.DrawRectLazyFromLeftTop(basePt.OffsetXY(100, -100), 5000, 1500);
            r.ColorIndex = 4;
            int i = 2, j = 2;
            int delta = 200;
            FloorDrains.ForEach(o => o.Draw(basePt.OffsetXY(i++ * delta, -j * delta)));
            j++;
            CondensePipes.ForEach(o => o.Draw(basePt.OffsetXY(i++ * delta, -j * delta)));
            j++;
            TranslatorPipe.Draw(basePt.OffsetXY(i++ * delta, -j * delta));
            DrawUtils.DrawCircleLazy(basePt, 500);
            switch (TranslatorPipe.TranslatorType)
            {
                case TranslatorTypeEnum.None:
                    {
                        DrawUtils.DrawLineLazy(basePt, basePt.OffsetY(-ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
                    }
                    break;
                case TranslatorTypeEnum.Long:
                case TranslatorTypeEnum.Short:
                    {
                        var len1 = 200;
                        DrawUtils.DrawLineLazy(basePt, basePt.OffsetY(len1));
                        DrawUtils.DrawLineLazy(basePt.OffsetY(len1), basePt.OffsetXY(300, len1));
                        DrawUtils.DrawLineLazy(basePt.OffsetXY(300, len1), basePt.OffsetXY(300, -ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
                        //DrawUtils.DrawLineLazy(basePt, basePt.OffsetY(-j * delta));
                        //DrawUtils.DrawLineLazy(basePt.OffsetXY(deltaX ,- j * delta), basePt.OffsetXY(deltaX ,- ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
                        //DrawUtils.DrawLineLazy(basePt.OffsetY(-j * delta), basePt.OffsetXY(deltaX, -j * delta));
                    }
                    break;
                default:
                    break;
            }
            j++;
            CheckPoint.Draw(basePt.OffsetXY(i++ * delta, -j * delta));
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
