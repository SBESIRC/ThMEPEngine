using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThMEPWSS.Assistant;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.DebugNs;
using ThMEPWSS.Uitl.ExtensionsNs;
using DU = ThMEPWSS.Assistant.DrawUtils;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWRainPipeSystem : IThWDraw, IEquatable<ThWRainPipeSystem>, IComparable<ThWRainPipeSystem>
    {
        /// <summary>
        /// 雨水斗
        /// </summary>
        public ThWSDWaterBucket WaterBucket { get; set; } = new ThWSDWaterBucket();

        public ThWSDOutputType OutputType { get; set; } = new ThWSDOutputType();

        public string VerticalPipeId { get; set; } = string.Empty;

        /// <summary>
        /// PipeRuns are sorted from base to top floor
        /// </summary>
        public List<ThWRainPipeRun> PipeRuns { get; set; }
        protected ThWRainPipeSystem()
        {
            PipeRuns = new List<ThWRainPipeRun>();
        }

        public void Draw(RainSystemDrawingContext ctx)
        {
            DrawWaterBucket(ctx);
            DrawPipeRuns(ctx);
        }

        private void DrawPipeRuns(RainSystemDrawingContext ctx)
        {
            var texts = ConvertLabelStrings(ctx).ToList();

            var basePoint = ctx.BasePoint;
            var runs = PipeRuns.ToList();
            runs.Reverse();
            {
                var maxY = double.MinValue;
                for (int i = 0; i < runs.Count; i++)
                {
                    var r = runs[i];
                    var s = r.Storey;
                    if (s != null)
                    {
                        var j = ctx.WSDStoreys.IndexOf(s);
                        var y = ctx.StoreyDrawingContexts[j].BasePoint.Y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            var ctxs = new PipeRunDrawingContext[runs.Count];
            for (int i = 0; i < runs.Count; i++)
            {
                var r = runs[i];
                var s = r.Storey;
                if (s != null)
                {
                    var j = ctx.WSDStoreys.IndexOf(s);
                    var bsPt = new Point3d(basePoint.X, ctx.StoreyDrawingContexts[j].BasePoint.Y, 0);
                    var c = new PipeRunDrawingContext() { ThWRainPipeRun = r };
                    ctxs[i] = c;
                    c.BasePoint = bsPt;
                }
            }

            var pts = new List<Point3d>(4096);
            {
                if (ctx.WaterBucketPoint is Point3d pt) pts.Add(pt);
            }

            for (int i = 0; i < runs.Count; i++)
            {
                var r = runs[i];
                var s = r.Storey;
                if (s != null)
                {
                    var c = ctxs[i];
                    if (s.Label == "3F")
                    {
                        DrawDNText(ctx, c);
                    }
                }
            }
            var ok = false;
            {
                var re = new Regex(@"(\d+)F");
                var storeys = ctx.WSDStoreys.Where(s => re.IsMatch(s.Label)).ToList();
                {
                    var targetStorey = storeys.GetLastOrDefault(2);
                    for (int i = 0; i < runs.Count; i++)
                    {
                        var r = runs[i];
                        var s = r.Storey;
                        if (s != null && s.Label == targetStorey?.Label)
                        {
                            var c = ctxs[i];
                            DrawDNText(ctx, c);
                        }
                    }
                }
                {
                    var targetStorey = storeys.GetLastOrDefault(3);
                    if (targetStorey != null)
                    {
                        for (int i = 0; i < runs.Count; i++)
                        {
                            var r = runs[i];
                            var s = r.Storey;
                            if (s != null && s.Label == targetStorey?.Label)
                            {
                                var c = ctxs[i];
                                var pt = c.BasePoint;
                                DrawPipeLabels(texts, pt);
                                ok = true;
                            }
                        }
                    }
                }
            }
            {
                var storeys = runs.Select(x => x.Storey).Where(x => !string.IsNullOrEmpty(x?.Label)).ToList();
                //如果楼层熟练小于5层 则在最高层进行立管编号的标注？
                if (!ok || storeys.Count < 5)
                {
                    var s = storeys.LastOrDefault();
                    if (s != null)
                    {
                        Vector3d v = default;
                        if (s.Label == "RF+2") v = new Vector3d(0, -400, 0);
                        DrawPipeLabels(texts, ctxs[runs.FindIndex(r => r.Storey == s)].BasePoint + v);
                    }
                }

            }
            double sdx = 0;
            for (int i = 0; i < runs.Count; i++)
            {
                var r = runs[i];
                var s = r.Storey;
                if (s != null)
                {
                    var c = ctxs[i];
                    r.Draw(c);
                    ThWRainPipeRun.CalcOffsets(r.TranslatorPipe.TranslatorType, c.YesDraw);
                    var dx = c.YesDraw.GetCurX();
                    c.BasePoint = c.BasePoint.OffsetX(sdx);
                    if (c.TopPoint is Point3d pt) pts.Add(pt);

                    //pts.AddRange(c.YesDraw.GetPoint3ds(c.BasePoint).Skip(1));
                    {
                        if (r.TranslatorPipe.TranslatorType == TranslatorTypeEnum.Long)
                        {
                            DU.DrawingQueue.Enqueue(adb =>
                            {
                                var pts = c.YesDraw.GetPoint3ds(c.BasePoint).ToList();
                                if (pts.Count >= 4)
                                {
                                    var pt = pts[3];
                                    Dr.DrawUnderBoardLabelAtRightButtom(pt.OffsetX(180));
                                    Dr.DrawDNLabel(pt.OffsetX(180).OffsetXY(50, -50 - 250));
                                }
                            });
                        }
                    }
                    {
                        var basePt = c.BasePoint;
                        var _pts = c.YesDraw.GetPoint3ds(basePt).Skip(1).ToList();
                        if (_pts.Count > 0)
                        {
                            GeoAlgorithm.GetCornerCoodinate(_pts, out double minX, out double minY, out double maxX, out double maxY);
                            if (maxY < basePoint.Y)
                            {
                                pts.Add(basePt);
                            }
                            pts.AddRange(_pts);
                            if (s.Label == "1F")
                            {
                                pts.Add(basePt.ReplaceX(_pts.Last().X));
                            }
                        }
                        else
                        {
                            pts.Add(basePt);
                        }
                    }

                    sdx += dx;
                }
            }
            if (OutputType.OutputType != RainOutputTypeEnum.None)
            {
                if (pts.Count > 0)
                {
                    var yd = new YesDraw();
                    yd.GoY(-500 - 800);
                    yd.OffsetXY(-200, -200);
                    yd.OffsetX(-1600);
                    var dx = yd.GetCurX();
                    pts.AddRange(yd.GetPoint3ds(pts.Last()));
                    sdx += dx;
                }
            }
            if (pts.Any())
            {
                ctx.OutputBasePoint = pts.Last();
                if (ctx.StoreyDrawingContexts.FirstOrDefault(x => x.Storey?.Label == "RF")?.BasePoint is Point3d pt)
                {

                    if (pts.Last().Y == pt.Y)
                    {
                        pt = pt.ReplaceX(pts.Last().X).OffsetY(500);
                        pts[pts.Count - 1] = pt;
                        DU.DrawingQueue.Enqueue(adb =>
                        {
                            if (ctx.RainSystemDiagram.ScatteredOutputs.Contains(this))
                            {
                                DrawPipeLinesLazy(pts);
                            }
                        });
                        return;
                    }

                }
            }
            {
                if (ctx.WaterBucketPoint is Point3d pt)
                {
                    pts = pts.Where(p => p.Y <= pt.Y).YieldBefore(pt).ToList();
                }
            }
            if (OutputType.OutputType == RainOutputTypeEnum.None)
            {
                if (WaterBucket.Storey != null)
                {
                    var r = runs.Last();
                    if (r != null)
                    {
                        if (runs.Count == 1)
                        {
                            var s = ctx.RainSystemDiagram.GetLowerStorey(r.Storey);
                            if (s != null)
                            {
                                var x = pts.Last().X;
                                var y = ctx.StoreyDrawingContexts[ctx.WSDStoreys.IndexOf(s)].BasePoint.Y;
                                pts.Add(new Point3d(x, y + 200, 0));
                            }
                        }
                        else
                        {
                            var storeys = runs.Select(x => ctx.RainSystemDiagram.GetStoreyIndex(x.Storey?.Label)).Where(x => x >= 0).OrderBy(x => x).Select(x => ctx.WSDStoreys[x]).ToList();
                            if (storeys.Count > 0)
                            {
                                pts[pts.Count - 1] = pts[pts.Count - 1].ReplaceY(ctx.StoreyDrawingContexts[ctx.RainSystemDiagram.GetStoreyIndex(storeys.First().Label)].BasePoint.Y + 200);
                            }

                        }
                    }
                }
                else
                {

                }
            }
            //只有阳台雨水立管（Y2）和冷凝水立管（NL）才需要通气
            //界面中的开关控制冷凝水立管是否通气
            if (VerticalPipeId.StartsWith("Y2") ||
                (VerticalPipeId.StartsWith("NL") && Dr.GetHasAiringForCondensePipe()))
            {
                //。如果立管出现在RF层，则通气管伸到屋顶上（上人500 不上人2000），否则在本层设通气。
                var r = runs.FirstOrDefault(r => r.Storey?.Label == "RF");
                if (r != null)
                {
                    if (pts.Count > 0)
                    {
                        //var p = ctx.StoreyDrawingContexts[ctx.WSDStoreys.Count - 1].BasePoint;
                        var p = ctx.StoreyDrawingContexts[ctx.WSDStoreys.IndexOf(ctx.RainSystemDiagram.GetStorey("RF"))].BasePoint;
                        var canPeopleBeOnRoof = Dr.GetCanPeopleBeOnRoof();
                        var offsetY = canPeopleBeOnRoof ? 500.0 : 2000.0;
                        if (DateTime.Now == DateTime.MinValue)
                        {
                            pts.Insert(0, pts.First().ReplaceY(p.Y).OffsetY(ThWSDStorey.TEXT_HEIGHT).OffsetY(offsetY));
                            {
                                var cd = new CircleDraw();
                                cd.Rotate(250, 180 + 45);
                                cd.Rotate(250, -45);
                                var lines = cd.GetLines(pts.First()).ToList();
                                DU.DrawEntitiesLazy(lines);
                                SetPipeRunLinesStyle(lines);
                            }
                        }
                        else
                        {
                            var pt = pts.First().ReplaceY(p.Y).OffsetY(ThWSDStorey.TEXT_HEIGHT).OffsetY(500.0 + 150);
                            DU.DrawBlockReference(blkName: "通气帽系统", basePt: pt, layer: "W-DRAI-DOME-PIPE", cb: br =>
                            {
                                br.ObjectId.SetDynBlockValue("距离1", 500.0);
                                br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
                            });
                        }
                    }
                }
                else
                {
                    r = runs.FirstOrDefault();
                    if (r != null)
                    {
                        if (pts.Count > 0)
                        {
                            var s = r.Storey;
                            var y = ctx.StoreyDrawingContexts[ctx.RainSystemDiagram.GetStoreyIndex(s.Label)].BasePoint.Y;
                            if (y <= pts.First().Y) y = pts.First().Y;
                            if (DateTime.Now == DateTime.MinValue)
                            {
                                pts.Insert(0, pts.First().ReplaceY(y).OffsetY(500));
                                {
                                    var cd = new CircleDraw();
                                    cd.Rotate(250, 180 + 45);
                                    cd.Rotate(250, -45);
                                    var lines = cd.GetLines(pts.First()).ToList();
                                    DU.DrawEntitiesLazy(lines);
                                    SetPipeRunLinesStyle(lines);
                                }
                            }
                            else
                            {
                                var canPeopleBeOnRoof = Dr.GetCanPeopleBeOnRoof();
                                var offsetY = canPeopleBeOnRoof ? 500.0 : 2000.0;
                                var pt = pts.First().ReplaceY(y).OffsetY(500.0 + 150);
                                DU.DrawBlockReference(blkName: "通气帽系统", basePt: pt, layer: "W-DRAI-DOME-PIPE", cb: br =>
                                {
                                    br.ObjectId.SetDynBlockValue("距离1", 500.0);
                                    br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
                                });
                            }
                        }
                    }
                }
            }
            DU.DrawingQueue.Enqueue(adb =>
            {
                if (ctx.RainSystemDiagram.ScatteredOutputs.Contains(this))
                {
                    if (pts.Count > 0)
                    {
                        var i = pts.Count - 1;
                        var pt = pts[i];
                        pts.RemoveAt(i);
                        pt = pt.OffsetY(150 + 120);
                        pts.Add(pt.OffsetY(120));
                        pts.Add(pt.OffsetX(-120));
                        pts.Add(pt.OffsetX(-1500 + 120));
                        pts.Add(pt.OffsetX(-1500).OffsetY(-120));
                    }
                }
                DrawPipeLinesLazy(pts);
            });

            //todo:组内做块
            {
                string f(ThWRainPipeRun r)
                {
                    var fds1 = r.FloorDrains.Where(x => x.HasDrivePipe).Select(x => "FloorDrain_HasDrivePipe").ToList();
                    var fds2 = r.FloorDrains.Where(x => !x.HasDrivePipe).Select(x => "FloorDrain_NoDrivePipe").ToList();
                    var cps1 = r.CondensePipes.Where(x => x.HasDrivePipe).Select(x => "CondensePipe_HasDrivePipe").ToList();
                    var cps2 = r.CondensePipes.Where(x => !x.HasDrivePipe).Select(x => "CondensePipe_NoDrivePipe").ToList();
                    var key = fds1.Concat(fds2).Concat(cps1).Concat(cps2).Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x).JoinWith(",");
                    return key;
                }
                var gs = runs.GroupBy(r => f(r)).ToList();
                foreach (var g in gs)
                {
                    if (string.IsNullOrEmpty(g.Key)) continue;
                    //Dbg.PrintLine(g.Key);
                }
            }


            //for (int i = 0; i < runs.Count; i++)
            //{
            //    var r = runs[i];
            //    var s = r.Storey;
            //    if (s != null)
            //    {
            //        var c = ctxs[i];

            //    }
            //}


        }

        private static void DrawPipeLabels(List<string> texts, Point3d pt)
        {//立管编号1 立管编号2
            if (texts.Count == 1)
            {
                DU.DrawingQueue.Enqueue(adb => Dr.DrawPipeLabel(pt, texts[0], ""));
            }
            else if (texts.Count == 2)
            {
                DU.DrawingQueue.Enqueue(adb => Dr.DrawPipeLabel(pt, texts[0], texts[1]));
            }
            else
            {
                DU.DrawingQueue.Enqueue(adb => Dr.DrawPipeLabel(pt, texts.JoinWith(";"), ""));
            }
        }

        private static void DrawDNText(RainSystemDrawingContext ctx, PipeRunDrawingContext c)
        {
            var dn = ctx.VerticalPipeType switch
            {
                VerticalPipeType.RoofVerticalPipe => Dr.GetRoofRainPipeDN(),
                VerticalPipeType.BalconyVerticalPipe => Dr.GetBalconyRainPipeDN(),
                VerticalPipeType.CondenseVerticalPipe => Dr.GetCondensePipeVerticalDN(),
                _ => throw new NotSupportedException(),
            };
            DU.DrawingQueue.Enqueue(adb => Dr.DrawDNLabelRight(c.BasePoint, dn));
        }

        private static IEnumerable<string> ConvertLabelStrings(RainSystemDrawingContext ctx)
        {
            var pipeIds = ctx.ThWRainPipeSystemGroup.Cast<ThWRainPipeSystem>().Select(p => p.VerticalPipeId).ToList();

            var items = pipeIds.Select(id => LabelItem.Parse(id)).Where(m => m != null);
            var gs = items.GroupBy(m => VTFac.Create(m.Prefix, m.D1, m.Suffix)).Select(l => l.OrderBy(x => x.D2).ToList());
            foreach (var g in gs)
            {
                if (g.Count == 1)
                {
                    //Dbg.PrintLine(g.First().Label);
                    yield return g.First().Label;
                }
                else if (g.Count > 2 && g.Count == g.Last().D2 - g.First().D2 + 1)
                {
                    var m = g.First();
                    //Dbg.PrintLine($"{m.Prefix}{m.D1}-{g.First().D2}{m.Suffix}~{g.Last().D2}{m.Suffix}");
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
                    //Dbg.PrintLine(sb.ToString());
                    yield return sb.ToString();
                }
            }
        }
     
        private static void DrawPipeLinesLazy(List<Point3d> pts)
        {
            var lines = DU.DrawLinesLazy(YesDraw.FixLines(pts));
            SetPipeRunLinesStyle(lines);

            //var pl = EntityFactory.CreatePolyline(DrLazy.FixLines(pts));
            //DU.DrawEntityLazy(pl);
        }

        public static void SetPipeRunLinesStyle(IList<Line> lines)
        {
            lines.ForEach(SetPipeRunLineStyle);
        }

        public static void SetPipeRunLineStyle(Line line)
        {
            line.Layer = "W-RAIN-PIPE";
            line.ColorIndex = 256;
        }

        private void DrawWaterBucket(RainSystemDrawingContext ctx)
        {
            var basePt = ctx.BasePoint;
            if (WaterBucket != null)
            {
                var waterBucketStorey = WaterBucket.Storey;
                if (waterBucketStorey != null /*&& waterBucketStorey.Label.Contains("RF")*/)
                {
                    var j = ctx.WSDStoreys.IndexOf(waterBucketStorey);
                    var bsPt = ctx.StoreyDrawingContexts[j].BasePoint;
                    var pt = new Point3d(basePt.X, bsPt.Y, 0);
                    var _ctx = new WaterBucketDrawingContext()
                    {
                        BasePoint = pt,
                        RainSystemDrawingContext = ctx,
                    };
                    WaterBucket.Draw(_ctx);
                }
            }
        }

        virtual public void Draw(Point3d basePt, Matrix3d mat)
        {
            //todo:
        }

        virtual public void Draw(Point3d basePt)
        {
            foreach (var r in PipeRuns)
            {
                //draw pipe
            }

            //todo: draw other device
        }

        public override int GetHashCode()
        {
            var hashCode = 1;
            foreach (var r in PipeRuns)
            {
                hashCode ^= r.GetHashCode();
            }

            return hashCode;
        }

        public bool Equals(ThWRainPipeSystem other)
        {
            if (other == null) return false;

            if (this.PipeRuns.Count != other.PipeRuns.Count) return false;

            for (int i = 0; i < this.PipeRuns.Count; ++i)
            {
                if (!PipeRuns[i].Equals(other.PipeRuns[i]))
                    return false;
            }

            return true;
        }

        public int CompareTo(ThWRainPipeSystem other)
        {
            if (this.VerticalPipeId.Equals(other.VerticalPipeId))
                return 0; //equal

            var dThisId = GetNormalizedId(VerticalPipeId);
            var dOtherId = GetNormalizedId(other.VerticalPipeId);

            if (dThisId > dOtherId) return 1;

            return -1;
        }

        private double GetNormalizedId(string id)
        {
            //var suffixes = new List<string>() {"a","b","c","d","e","f"};
            var suffixToNumDic = new Dictionary<string, string>()
            {
                {"a","1"}, { "b", "2" }, { "c", "3" },
                { "d", "4" }, { "e", "5" }, { "f", "6" }
            };

            var newIdString = id.Replace("Y1L", "").
                Replace("Y2L", "").Replace("NL", "").
                Replace("N1L", "").Replace("-", "").Replace("'", ".1");

            foreach (var k in suffixToNumDic.Keys)
            {
                newIdString = newIdString.Replace(k, suffixToNumDic[k]);
            }

            if (!double.TryParse(newIdString, out double dId))
                return 0;

            return dId;
        }
    }

    /// <summary>
    /// 屋顶雨水管系统
    /// </summary>
    public class ThWRoofRainPipeSystem : ThWRainPipeSystem, IEquatable<ThWRoofRainPipeSystem>
    {
        public ThWRoofRainPipeSystem()
        {
        }
        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode() ^ WaterBucket.GetHashCode() ^ OutputType.GetHashCode();
            return hashCode;
        }
        public bool Equals(ThWRoofRainPipeSystem other)
        {
            if (other == null) return false;
            return WaterBucket.Equals(other.WaterBucket) && OutputType.Equals(other.OutputType) && base.Equals(other);
        }
    }

    /// <summary>
    /// 阳台雨水管系统
    /// </summary>
    public class ThWBalconyRainPipeSystem : ThWRainPipeSystem, IEquatable<ThWBalconyRainPipeSystem>
    {
        public ThWBalconyRainPipeSystem()
        {
        }
        //override public void Draw(Point3d basePt)
        //{
        //    if (WaterBucket != null)
        //    {
        //        var waterBucketStorey = WaterBucket.Storey;
        //        if (waterBucketStorey != null)
        //        {
        //            //Dbg.PrintLine(waterBucketStorey.Label);
        //            if (false)
        //            {
        //                //var waterBucketBasePt = new Point2d(basePt.X, waterBucketStorey.StoreyBasePoint.Y).ToPoint3d();
        //                //WaterBucket?.Draw(waterBucketBasePt);
        //            }
        //        }
        //    }
        //    for (int i = 0; i < PipeRuns.Count; i++)
        //    {
        //        ThWRainPipeRun r = PipeRuns[i];
        //        //draw piperun
        //        r.Draw(basePt.OffsetY(i * ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
        //        if (r.TranslatorPipe.PipeType.Equals(TranslatorTypeEnum.None))
        //        {

        //        }
        //    }
        //    DrawUtils.DrawTextLazy(OutputType.OutputType.ToString(), 100, basePt);
        //    //new Circle() { Center = basePt, ColorIndex = 3, Radius = 200, Thickness = 5, }.AddToCurrentSpace();
        //    //NoDraw.Text("ThWRoofRainPipeSystem: " + VerticalPipeId, 100, basePt.OffsetY(-1000)).AddToCurrentSpace();
        //    //todo: draw other device
        //}

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ WaterBucket.GetHashCode() ^ OutputType.GetHashCode();
        }

        public bool Equals(ThWBalconyRainPipeSystem other)
        {
            if (other == null) return false;

            return WaterBucket.Equals(other.WaterBucket) && OutputType.Equals(other.OutputType) && base.Equals(other);
        }
    }

    /// <summary>
    /// 冷凝管系统
    /// </summary>
    public class ThWCondensePipeSystem : ThWRainPipeSystem, IEquatable<ThWCondensePipeSystem>
    {
        public ThWCondensePipeSystem()
        {
        }
        //override public void Draw(Point3d basePt)
        //{
        //    if (WaterBucket != null)
        //    {
        //        var waterBucketStorey = WaterBucket.Storey;
        //        if (waterBucketStorey != null)
        //        {
        //            //Dbg.PrintLine(waterBucketStorey.Label);
        //            if (false)
        //            {
        //                //var waterBucketBasePt = new Point2d(basePt.X, waterBucketStorey.StoreyBasePoint.Y).ToPoint3d();
        //                //WaterBucket?.Draw(waterBucketBasePt);
        //            }
        //        }
        //    }
        //    for (int i = 0; i < PipeRuns.Count; i++)
        //    {
        //        ThWRainPipeRun r = PipeRuns[i];
        //        //draw piperun
        //        r.Draw(basePt.OffsetY(i * ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
        //        if (r.TranslatorPipe.PipeType.Equals(TranslatorTypeEnum.None))
        //        {

        //        }
        //    }
        //    DrawUtils.DrawTextLazy(OutputType.OutputType.ToString(), 100, basePt);
        //    //new Circle() { Center = basePt, ColorIndex = 3, Radius = 200, Thickness = 5, }.AddToCurrentSpace();
        //    //NoDraw.Text("ThWRoofRainPipeSystem: " + VerticalPipeId, 100, basePt.OffsetY(-1000)).AddToCurrentSpace();
        //    //todo: draw other device
        //}

        public override int GetHashCode()
        {
            return OutputType.GetHashCode();
        }

        public bool Equals(ThWCondensePipeSystem other)
        {
            if (other == null) return false;

            return OutputType.Equals(other.OutputType) && base.Equals(other);
        }
    }

}
