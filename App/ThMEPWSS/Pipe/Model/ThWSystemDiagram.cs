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
using ThMEPWSS.Uitl.ExtensionsNs;
using DU = ThMEPWSS.Assistant.DrawUtils;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.Pipe.Model
{
    public enum TranslatorTypeEnum
    {
        None,
        Short, //乙字湾
        Long, //长转管
    }

    public enum WaterBucketEnum
    {
        None,
        Gravity,//重力雨水斗
        Side,//侧入式雨水斗
        _87,//87雨水斗
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

        private string _dn = string.Empty;
        public string DN
        {
            get => _dn;
            set => _dn = value ?? string.Empty;
        }

        public VerticalPipeType PipeType { get; set; }

        public override int GetHashCode()
        {
            return /*Label.GetHashCode() ^*/ DN.GetHashCode() ^ PipeType.GetHashCode();
        }

        virtual public bool Equals(ThWSDPipe other)
        {
            return /*this.Label.Equals(other.Label) &&*/ this.DN.Equals(other.DN) && this.PipeType.Equals(other.PipeType);
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
        //such as 1F, 2F.... RF+1, RF+2
        public string Label { get; set; } = string.Empty;
        public string Elevation { get; set; } = string.Empty;
        public StoreyType StoreyType;
        public const double TEXT_HEIGHT = 350;
        public const double INDEX_TEXT_OFFSET_X = 2000;
        public const double INDEX_TEXT_OFFSET_Y = 130;
        public const double RF_OFFSET_Y = 500;

        [JsonIgnore]
        public GRect Boundary;
        public List<ThWSDWaterBucket> Buckets { get; set; } = new List<ThWSDWaterBucket>();
        //用于RF层
        public List<ThWSDPipe> VerticalPipes { get; set; } = new List<ThWSDPipe>();
        [JsonIgnore]
        public Point3dCollection Range { get; set; }
        [JsonIgnore]
        public ObjectId ObjectID { get; set; }
        [JsonIgnore]
        public BlockReference BlockRef { get; set; }
        [JsonIgnore]
        public Point3d Position => BlockRef.Position;
        public void Draw(StoreyDrawingContext ctx)
        {
            var basePt = ctx.BasePoint;
            var lineLen = ctx.StoreyLineLength;

            {
                var line = DU.DrawLineLazy(basePt.X, basePt.Y, basePt.X + lineLen, basePt.Y);
                var dbt = DU.DrawTextLazy(Label, TEXT_HEIGHT, new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
                DU.DrawBlockReference(blkName: "标高", basePt: basePt.OffsetX(550), layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", "" } });
            }
            if (Label == "RF")
            {
                var line = DU.DrawLineLazy(new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + RF_OFFSET_Y, 0), new Point3d(basePt.X + lineLen, basePt.Y + RF_OFFSET_Y, 0));
                var dbt = DU.DrawTextLazy("建筑完成面", TEXT_HEIGHT, new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + RF_OFFSET_Y + INDEX_TEXT_OFFSET_Y, 0));
                Dr.SetLabelStylesForWNote(line, dbt);
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
        public string DN { get; set; } = "DN100";
        public WaterBucketEnum Type { get; set; }
        public ThWSDStorey Storey { get; set; }
        public GRect Boundary;
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
            ctx.RainSystemDrawingContext.WaterBucketPoint = basePt;
            switch (Type)
            {
                case WaterBucketEnum.Gravity:
                    {
                        Dr.DrawGravityWaterBucket(basePt);
                        Dr.DrawGravityWaterBucketLabel(basePt.OffsetXY(0, 125), $"重力雨水斗{DN}");
                    }
                    break;
                case WaterBucketEnum.Side:
                    {
                        var relativeYOffsetToStorey = -83;
                        var pt = basePt.OffsetY(relativeYOffsetToStorey);
                        ctx.RainSystemDrawingContext.WaterBucketPoint = pt;
                        Dr.DrawSideWaterBucket(pt);
                        Dr.DrawSideWaterBucketLabel(pt.OffsetXY(-215, 318), $"侧入式雨水斗{DN}");
                    }
                    break;
                case WaterBucketEnum._87:
                    {
                        Dr.DrawGravityWaterBucket(basePt);
                        Dr.DrawGravityWaterBucketLabel(basePt.OffsetXY(0, 125), $"87雨水斗{DN}");
                    }
                    break;
                default:
                    break;
            }
        }

        public bool Equals(ThWSDWaterBucket other)
        {
            return this.Label.Equals(other.Label) && this.DN.Equals(other.DN) && this.Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            return Label.GetHashCode() ^ DN.GetHashCode() ^ Type.GetHashCode();
        }
    }
    public class ThWSDFloorDrain : IEquatable<ThWSDFloorDrain>
    {
        /// <summary>
        /// 地漏标注
        /// </summary>
        public string Label { get; set; } = string.Empty;
        public string DN { get; set; } = string.Empty;
        /// <summary>
        /// 是否有套管
        /// </summary>
        public bool HasDrivePipe { get; set; }

        public ThWSDFloorDrain()
        {
        }

        public bool Equals(ThWSDFloorDrain other)
        {
            return this.Label.Equals(other.Label) && this.DN.Equals(other.DN);
        }

        public override int GetHashCode()
        {
            return this.Label.GetHashCode() ^ this.DN.GetHashCode();
        }
        public void Draw(Point3d basePt)
        {

        }
    }

    public class ThWSDCondensePipe : IEquatable<ThWSDCondensePipe>
    {
        /// <summary>
        /// 标注
        /// </summary>
        public string Label { get; set; } = string.Empty;
        public string DN { get; set; } = "DN25";
        /// <summary>
        /// 是否有套管
        /// </summary>
        public bool HasDrivePipe { get; set; } = false;
        public bool IsLow;
        public override int GetHashCode()
        {
            return this.Label.GetHashCode() ^ DN.GetHashCode() ^ HasDrivePipe.GetHashCode();
        }
        public bool Equals(ThWSDCondensePipe other)
        {
            return this.Label.Equals(other.Label) && this.DN.Equals(other.DN) && this.HasDrivePipe.Equals(other.HasDrivePipe);
        }
    }

    public class ThWSDCheckPoint : IEquatable<ThWSDCheckPoint>
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
        public void Draw(Point3d basePt)
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
        public string DN { get; set; } = string.Empty;

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
            return DN.GetHashCode() ^ OutputType.GetHashCode() ^ HasDrivePipe.GetHashCode();
        }

        public bool Equals(ThWSDOutputType other)
        {
            return DN.Equals(other.DN) && OutputType.Equals(other.OutputType) && HasDrivePipe.Equals(other.HasDrivePipe);
        }
    }

    public abstract class ThWSystemDiagram
    {
    }
}
