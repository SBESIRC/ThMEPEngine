using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl.DebugNs;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using DU = ThMEPWSS.Assistant.DrawUtils;

namespace ThMEPWSS.Pipe.Model
{
    public enum TranslatorTypeEnum
    {
        None,
        Long, //转管
        Short //乙字湾
    }

    public enum WaterBucketEnum
    {
        None,
        Gravity,//重力雨水斗
        Side,//侧入式雨水斗
    }

    public enum RainOutputTypeEnum
    {
        None,
        WaterWell,//雨水井
        RainPort,//雨水口
        DrainageDitch,//排水沟
    }

    public enum VerticalPipeType
    {
        RoofVerticalPipe,
        BalconyVerticalPipe,
        CondenseVerticalPipe
    }

    public class ThWSDDrawableElement : IThWDraw
    {
        //public string Label { get; set; }

        protected string LayerName { get; set; } = string.Empty;
        protected string StyleName { get; set; } = string.Empty;
        protected ThWSDDrawableElement()
        {
            LayerName = ThWPipeCommon.W_RAIN_EQPM;
        }
        virtual public void Draw(Point3d basePt, Matrix3d mat)
        {
            //throw new NotImplementedException();
        }

        virtual public void Draw(Point3d basePt)
        {
            NoDraw.Text("ThWSDDrawableElement", 100, basePt).AddToCurrentSpace();
        }
    }

    /// <summary>
    /// TianHua water system diagram pipe
    /// </summary>
    public class ThWSDPipe : ThWSDDrawableElement, IEquatable<ThWSDPipe>
    {
        /// <summary>
        /// 标注
        /// </summary>
        public string Label { get; set; } = string.Empty;

        private string _nd = string.Empty;
        public string ND
        {
            get
            {
                return _nd;
            }
            set
            {
                if (value != null)
                    _nd = value;
            }
        }

        public VerticalPipeType PipeType { get; set; }

        public override int GetHashCode()
        {
            return /*Label.GetHashCode() ^*/ ND.GetHashCode() ^ PipeType.GetHashCode();
        }

        virtual public bool Equals(ThWSDPipe other)
        {
            return /*this.Label.Equals(other.Label) &&*/ this.ND.Equals(other.ND) && this.PipeType.Equals(other.PipeType);
        }
    }

    public class ThWSDTranslatorPipe : ThWSDPipe
    {
        public TranslatorTypeEnum TranslatorType { get; set; } = TranslatorTypeEnum.None;

        override public void Draw(Point3d basePt)
        {

            DrawUtils.DrawCircleLazy(basePt, 200);
            switch (TranslatorType)
            {
                case TranslatorTypeEnum.None:
                    break;
                case TranslatorTypeEnum.Long: //长转管
                    DrawUtils.DrawTextLazy("Long Translator Pipe", 100, basePt);
                    break;
                case TranslatorTypeEnum.Short: //乙字湾
                    DrawUtils.DrawTextLazy("Short Translator Pipe", 100, basePt);
                    break;
                default:
                    break;
            }
        }

        public override bool Equals(ThWSDPipe other)
        {
            if (!(other is ThWSDTranslatorPipe)) return false;

            return base.Equals(other) && this.TranslatorType.Equals((other as ThWSDTranslatorPipe).TranslatorType);
        }
    }
    public class ThWSDStorey : ThWSDDrawableElement, IEquatable<ThWSDStorey>
    {
        [JsonIgnore]
        public ThWSDStorey HigerStorey { get; set; } = null;
        [JsonIgnore]
        public ThWSDStorey LowerStorey { get; set; } = null;

        //such as 1F, 2F.... RF+1, RF+2
        public string Label { get; set; } = string.Empty;
        public string Elevation { get; set; } = string.Empty;
        public StoreyType StoreyType;
        public const double TEXT_HEIGHT = 350;
        public const double INDEX_TEXT_OFFSET_X = 2000;
        public const double INDEX_TEXT_OFFSET_Y = 130;
        public const double RF_OFFSET_Y = 500;
        
        
        public List<ThWSDWaterBucket> Buckets { get; set; } = new List<ThWSDWaterBucket>();
        public List<ThWSDPipe> VerticalPipes { get; set; } = new List<ThWSDPipe>();
        [JsonIgnore]
        public Point3dCollection Range { get; set; }
        [JsonIgnore]
        public ObjectId ObjectID { get; set; }
        [JsonIgnore]
        public BlockReference BlockRef { get; set; }
        [JsonIgnore]
        public Point3d Position
        {
            get
            {
                return BlockRef.Position;
            }
        }
        public void Draw(StoreyDrawingContext ctx)
        {
            var basePt = ctx.BasePoint;
            var lineLen = ctx.StoreyLineLength;
            var layer = "W-NOTE";
            {
                var line = DU.DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                line.Layer = layer;
                var dbt = DU.DrawTextLazy(Label, TEXT_HEIGHT, new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + INDEX_TEXT_OFFSET_Y, 0));
                dbt.Layer = layer;
            }
            if (Label == "RF")
            {
                var line = DU.DrawLineLazy(new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + RF_OFFSET_Y, 0), new Point3d(basePt.X + lineLen, basePt.Y + RF_OFFSET_Y, 0));
                line.Layer = layer;
                var dbt = DU.DrawTextLazy("建筑完成面", TEXT_HEIGHT, new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + RF_OFFSET_Y + INDEX_TEXT_OFFSET_Y, 0));
                dbt.Layer = layer;
            }
        }
        public override int GetHashCode()
        {
            return this.Label.GetHashCode();
        }
        public bool Equals(ThWSDStorey other)
        {
            return this.Label.Equals(other.Label);
        }
    }
    public class ThWSDWaterBucket : ThWSDDrawableElement, IEquatable<ThWSDWaterBucket>
    {
        public string Label { get; set; } = string.Empty;
        public string ND { get; set; } = string.Empty;
        public WaterBucketEnum Type { get; set; }
        public ThWSDStorey Storey { get; set; }

        public ThWSDWaterBucket()
        {
            //LayerName = ThWPipeCommon.W_RAIN_EQPM;
        }

        public void Draw(WaterBucketDrawingContext ctx)
        {
            var basePt = ctx.BasePoint;
            if (Storey.Label == "RF")
            {
                basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
            }
            switch (Type)
            {
                case WaterBucketEnum.Gravity:
                    Dr.DrawGravityWaterBucket(basePt);
                    Dr.DrawGravityWaterBucketLabel(basePt);
                    break;
                case WaterBucketEnum.Side:
                    Dr.DrawSideWaterBucket(basePt);
                    Dr.DrawSideWaterBucketLabel(basePt);
                    break;
                default:
                    break;
            }
        }

        public bool Equals(ThWSDWaterBucket other)
        {
            return this.Label.Equals(other.Label) && this.ND.Equals(other.ND) && this.Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            return Label.GetHashCode() ^ ND.GetHashCode() ^ Type.GetHashCode();
        }
    }
    public class ThWSDDrain : ThWSDDrawableElement, IEquatable<ThWSDDrain>
    {
        /// <summary>
        /// 地漏标注
        /// </summary>
        public string Label { get; set; } = string.Empty;
        public string ND { get; set; } = string.Empty;
        private string BlockName { get; set; } = string.Empty;
        /// <summary>
        /// 是否有套管
        /// </summary>
        public bool HasDrivePipe { get; set; }

        public ThWSDDrain()
        {
            BlockName = ThWPipeCommon.W_RAIN_EQPM;
        }

        public bool Equals(ThWSDDrain other)
        {
            return this.Label.Equals(other.Label) && this.ND.Equals(other.ND);
        }

        public override int GetHashCode()
        {
            return this.Label.GetHashCode() ^ this.ND.GetHashCode();
        }
        public override void Draw(Point3d basePt)
        {
            DrawUtils.DrawTextLazy("ThWSDDrain", 100, basePt);
        }
    }

    public class ThWSDCondensePipe : ThWSDDrawableElement, IEquatable<ThWSDCondensePipe>
    {
        /// <summary>
        /// 标注
        /// </summary>
        public string Label { get; set; } = string.Empty;
        public string ND { get; set; } = string.Empty;
        /// <summary>
        /// 是否有套管
        /// </summary>
        public bool HasDrivePipe { get; set; } = false;

        public override int GetHashCode()
        {
            return this.Label.GetHashCode() ^ ND.GetHashCode() ^ HasDrivePipe.GetHashCode();
        }
        public bool Equals(ThWSDCondensePipe other)
        {
            return this.Label.Equals(other.Label) && this.ND.Equals(other.ND) && this.HasDrivePipe.Equals(other.HasDrivePipe);
        }
        public override void Draw(Point3d basePt)
        {
            DrawUtils.DrawTextLazy("ThWSDCondensePipe", 100, basePt);
        }
    }

    public class ThWSDCheckPoint : ThWSDDrawableElement, IEquatable<ThWSDCheckPoint>
    {
        public bool HasCheckPoint { get; set; } = false;
        public string Label { get; set; } = string.Empty;
        public string ND { get; set; } = string.Empty;

        public override int GetHashCode()
        {
            return this.Label.GetHashCode() ^ ND.GetHashCode();
        }
        public bool Equals(ThWSDCheckPoint other)
        {
            return this.Label.Equals(other.Label) && this.ND.Equals(other.ND);
        }
        public override void Draw(Point3d basePt)
        {
            if (HasCheckPoint)
            {
                DrawUtils.DrawTextLazy("ThWSDCheckPoint", 100, basePt);
                DrawUtils.DrawRectLazyFromLeftButtom(basePt, 200, 100);
            }
        }
    }

    public class ThWSDOutputType : ThWSDDrawableElement, IEquatable<ThWSDOutputType>
    {
        public string Label { get; set; } = string.Empty;
        public string BlockName { get; set; } = string.Empty;
        public string ND { get; set; } = string.Empty;

        /// <summary>
        /// 是否有套管
        /// </summary>
        public bool HasDrivePipe { get; set; } = false;

        public ThWSDOutputType()
        {
            LayerName = ThWPipeCommon.W_RAIN_EQPM;
        }

        public RainOutputTypeEnum OutputType { get; set; }

        override public void Draw(Point3d basePt)
        {
            //todo: set block name due to output type
            //draw
        }

        public override int GetHashCode()
        {
            return ND.GetHashCode() ^ OutputType.GetHashCode() ^ HasDrivePipe.GetHashCode();
        }

        public bool Equals(ThWSDOutputType other)
        {
            return ND.Equals(other.ND) && OutputType.Equals(other.OutputType) && HasDrivePipe.Equals(other.HasDrivePipe);
        }
    }

    public class ThWSystemDiagram : ThWSDDrawableElement
    {
        protected ThWSystemDiagram()
        {

        }

        override public void Draw(Point3d basePt, Matrix3d mat)
        {
            throw new NotImplementedException();
        }

        override public void Draw(Point3d basePt)
        {
            throw new NotImplementedException();
        }
    }
}
