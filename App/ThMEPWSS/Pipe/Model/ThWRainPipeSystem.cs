using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Assistant;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Uitl;
using ThMEPWSS.Uitl.DebugNs;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
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
                    var c = new PipeRunDrawingContext();
                    ctxs[i] = c;
                    c.BasePoint = bsPt;
                   
                }
            }

            var pts = new List<Point3d>(4096);
            {
                if (ctx.WaterBucketPoint is Point3d pt) pts.Add(pt);
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
                    if (false)
                    {
                        var rect = c.YesDraw.GetGRect(c.BasePoint, false);
                        if (!rect.Equals(default)) DU.DrawRectLazy(rect);
                    }

                    //pts.AddRange(c.YesDraw.GetPoint3ds(c.BasePoint).Skip(1));

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
                    yd.GoY(-500);
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
            }
            DrawPipeLinesLazy(pts);

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
