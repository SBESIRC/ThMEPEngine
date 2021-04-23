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
using ThMEPWSS.Uitl.DebugNs;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;

namespace ThMEPWSS.Pipe.Model
{

    public class ThWRainPipeSystem : IThWDraw, IEquatable<ThWRainPipeSystem>
    {
        public string VerticalPipeId { get; set; } = string.Empty;

        /// <summary>
        /// PipeRuns are sorted from base to top floor
        /// </summary>
        public List<ThWRainPipeRun> PipeRuns { get; set; }
        protected ThWRainPipeSystem()
        {
            PipeRuns = new List<ThWRainPipeRun>();
        }

        public void SortPipeRuns()
        {
            //todo:
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
    }

    /// <summary>
    /// 屋顶雨水管系统
    /// </summary>
    public class ThWRoofRainPipeSystem : ThWRainPipeSystem, IEquatable<ThWRoofRainPipeSystem>
    {
        /// <summary>
        /// 雨水斗
        /// </summary>
        public ThWSDWaterBucket WaterBucket { get; set; } = new ThWSDWaterBucket();

        public ThWSDOutputType OutputType { get; set; } = new ThWSDOutputType();

        public ThWRoofRainPipeSystem()
        {

        }

        public void Draw(RoofRainSystemDrawingContext ctx)
        {
            DrawWaterBucket(ctx);
            DrawPipeRuns(ctx);
            DrawUtils.DrawTextLazy(OutputType.OutputType.ToString(), 200, ctx.BasePoint);
        }

        private void DrawPipeRuns(RoofRainSystemDrawingContext ctx)
        {
            var basePt = ctx.BasePoint;
            var runs = PipeRuns.ToList();
            runs.Reverse();
            {
                //DrLazy.Default.BasePoint = basePt;
                TranslatorLazyDrawer.Test();
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
                TranslatorLazyDrawer.DrawPipeLazy(default, default);
                //DrLazy.Default.BasePoint = DrLazy.Default.BasePoint.ReplaceY(maxY);
            }
            //Dbg.ShowWhere(DrLazy.Default.BasePoint);
            for (int i = 0; i < runs.Count; i++)
            {
                var r = runs[i];
                var s = r.Storey;
                if (s != null)
                {
                    var j = ctx.WSDStoreys.IndexOf(s);
                    var bsPt = new Point3d(basePt.X, ctx.StoreyDrawingContexts[j].BasePoint.Y, 0);
                    //Dbg.ShowXLabel(DrLazy.Default.BasePoint);
                    r.Draw(bsPt);
                    //Dbg.ShowXLabel(DrLazy.Default.BasePoint);
                    //DrawUtils.DrawTextLazy(i.ToString(), bsPt);
                }
                //var pt = basePt.OffsetXY(0, i * ThWRainSystemDiagram.VERTICAL_STOREY_SPAN);
            }
            //DrLazy.Default.Break();
            //Dbg.ShowWhere(DrLazy.Default.BasePoint);
        }

        private void DrawWaterBucket(RoofRainSystemDrawingContext ctx)
        {
            var basePt = ctx.BasePoint;
            if (WaterBucket != null)
            {
                var waterBucketStorey = WaterBucket.Storey;
                if (waterBucketStorey != null && waterBucketStorey.Label.Contains("RF"))
                {
                    var j = ctx.WSDStoreys.IndexOf(waterBucketStorey);
                    var bsPt = ctx.StoreyDrawingContexts[j].BasePoint;
                    var _ctx = new WaterBucketDrawingContext()
                    {
                        BasePoint = new Point3d(basePt.X, bsPt.Y, 0)
                    };
                    WaterBucket?.Draw(_ctx);
                }
            }
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
        /// <summary>
        /// 雨水斗
        /// </summary>
        public ThWSDWaterBucket WaterBucket { get; set; } = new ThWSDWaterBucket();

        public ThWSDOutputType OutputType { get; set; } = new ThWSDOutputType();

        public ThWBalconyRainPipeSystem()
        {

        }

        override public void Draw(Point3d basePt)
        {
            if (WaterBucket != null)
            {
                var waterBucketStorey = WaterBucket.Storey;
                if (waterBucketStorey != null)
                {
                    //Dbg.PrintLine(waterBucketStorey.Label);
                    if (false)
                    {
                        //var waterBucketBasePt = new Point2d(basePt.X, waterBucketStorey.StoreyBasePoint.Y).ToPoint3d();
                        //WaterBucket?.Draw(waterBucketBasePt);
                    }
                }
            }
            for (int i = 0; i < PipeRuns.Count; i++)
            {
                ThWRainPipeRun r = PipeRuns[i];
                //draw piperun
                r.Draw(basePt.OffsetY(i * ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
                if (r.TranslatorPipe.PipeType.Equals(TranslatorTypeEnum.None))
                {

                }
            }
            DrawUtils.DrawTextLazy(OutputType.OutputType.ToString(), 100, basePt);
            //new Circle() { Center = basePt, ColorIndex = 3, Radius = 200, Thickness = 5, }.AddToCurrentSpace();
            //NoDraw.Text("ThWRoofRainPipeSystem: " + VerticalPipeId, 100, basePt.OffsetY(-1000)).AddToCurrentSpace();
            //todo: draw other device
        }

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
        /// <summary>
        /// 雨水斗
        /// </summary>
        public ThWSDWaterBucket WaterBucket { get; set; } = new ThWSDWaterBucket();

        public ThWSDOutputType OutputType { get; set; } = new ThWSDOutputType();

        public ThWCondensePipeSystem()
        {

        }

        override public void Draw(Point3d basePt)
        {
            if (WaterBucket != null)
            {
                var waterBucketStorey = WaterBucket.Storey;
                if (waterBucketStorey != null)
                {
                    //Dbg.PrintLine(waterBucketStorey.Label);
                    if (false)
                    {
                        //var waterBucketBasePt = new Point2d(basePt.X, waterBucketStorey.StoreyBasePoint.Y).ToPoint3d();
                        //WaterBucket?.Draw(waterBucketBasePt);
                    }
                }
            }
            for (int i = 0; i < PipeRuns.Count; i++)
            {
                ThWRainPipeRun r = PipeRuns[i];
                //draw piperun
                r.Draw(basePt.OffsetY(i * ThWRainSystemDiagram.VERTICAL_STOREY_SPAN));
                if (r.TranslatorPipe.PipeType.Equals(TranslatorTypeEnum.None))
                {

                }
            }
            DrawUtils.DrawTextLazy(OutputType.OutputType.ToString(), 100, basePt);
            //new Circle() { Center = basePt, ColorIndex = 3, Radius = 200, Thickness = 5, }.AddToCurrentSpace();
            //NoDraw.Text("ThWRoofRainPipeSystem: " + VerticalPipeId, 100, basePt.OffsetY(-1000)).AddToCurrentSpace();
            //todo: draw other device
        }

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
