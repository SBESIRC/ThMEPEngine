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
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl.DebugNs;
using ThMEPWSS.Uitl.ExtensionsNs;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;

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
        public ThWSDStorey HigerStorey { get; set; } = null;
        public ThWSDStorey LowerStorey { get; set; } = null;

        //such as 1F, 2F.... RF+1, RF+2
        public string Label { get; set; } = string.Empty;
        public string Elevation { get; set; } = string.Empty;
        public StoreyType StoreyType;
        public double Length = 5000; //mm
        public double HorizontalSpan = 5500;//mm

        public const double TEXT_HEIGHT = 350;
        public const double INDEX_TEXT_OFFSET_X = 2000;
        public const double INDEX_TEXT_OFFSET_Y = 130;
        public double LINE_LENGTH = 100000;
        public const double RF_OFFSET_Y = 500;

        public Point3d StoreyBasePoint;
        public List<ThWSDWaterBucket> Buckets { get; set; } = new List<ThWSDWaterBucket>();
        public List<ThWSDPipe> VerticalPipes { get; set; } = new List<ThWSDPipe>();

        public Point3dCollection Range { get; set; }

        public ObjectId ObjectID { get; set; }

        public BlockReference BlockRef { get; set; }
        public Point3d Position
        {
            get
            {
                return BlockRef.Position;
            }
        }

        public ThWSDStorey()
        {
            LayerName = "W-NOTE";
        }
        /// <summary>
        /// Draw horizental line by length,
        /// Also, draw other neccessary information
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="len"></param>
        override public void Draw(Point3d basePt)
        {
            using (var adb = AcadDatabase.Active())
            {
                adb.ModelSpace.Add(new Line(StoreyBasePoint, basePt)
                {
                    Layer = LayerName,
                });

                //draw horizontal line
                adb.ModelSpace.Add(new Line(new Point3d(basePt.X, basePt.Y, 0), new Point3d(basePt.X + LINE_LENGTH, basePt.Y, 0))
                {
                    Layer = LayerName,
                });
                adb.ModelSpace.Add(new Line(new Point3d(basePt.X + StoreyBasePoint.X, basePt.Y + StoreyBasePoint.Y, 0), new Point3d(basePt.X + StoreyBasePoint.X + LINE_LENGTH, basePt.Y + StoreyBasePoint.Y, 0))
                {
                    Layer = LayerName,
                });
                //draw Index
                adb.ModelSpace.Add(new DBText()
                {
                    Layer = LayerName,
                    Position = new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + INDEX_TEXT_OFFSET_Y, 0),
                    //TextStyleName = "TH-STYLE3",
                    TextString = Label,
                    Height = TEXT_HEIGHT,
                });
                //draw other information
                if (Label == "RF")
                {
                    adb.ModelSpace.Add(new Line(new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + RF_OFFSET_Y, 0), new Point3d(basePt.X + LINE_LENGTH, basePt.Y + RF_OFFSET_Y, 0))
                    {
                        Layer = LayerName,
                    });
                    adb.ModelSpace.Add(new DBText()
                    {
                        Layer = LayerName,
                        Position = new Point3d(basePt.X + INDEX_TEXT_OFFSET_X, basePt.Y + RF_OFFSET_Y + INDEX_TEXT_OFFSET_Y, 0),
                        TextString = "建筑完成面",
                        Height = TEXT_HEIGHT,
                    });
                }
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
        public Point3dCollection Range { get; set; }
        public ThWSDStorey Storey { get; set; }
        private string BlockName { get; set; } = string.Empty;

        public ThWSDWaterBucket()
        {
            LayerName = ThWPipeCommon.W_RAIN_EQPM;
        }

        override public void Draw(Point3d basePt)
        {
            if (Type.Equals(WaterBucketEnum.Gravity))
            {
                BlockName = "屋面雨水斗";
            }
            else if (Type.Equals(WaterBucketEnum.Side))
            {
                BlockName = "侧排雨水斗系统";
            }
            else
                return;
            if (Storey.Label == "RF")
            {
                basePt = basePt.OffsetY(ThWSDStorey.RF_OFFSET_Y);
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //draw entity
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference(LayerName, BlockName, basePt, new Scale3d(1), 0);
                //todo: draw Label
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
            DrawUtils.DrawTextLazy("ThWSDCheckPoint", 100, basePt);
            DrawUtils.DrawRectLazyFromLeftButtom(basePt, 200, 100);
        }
    }

    public class ThWSDOutputType : ThWSDDrawableElement, IEquatable<ThWSDOutputType>
    {
        public string Label { get; set; } = string.Empty;
        public string BlockName { get; set; } = string.Empty;
        public string ND { get; set; } = string.Empty;

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
            return ND.GetHashCode() ^ OutputType.GetHashCode();
        }

        public bool Equals(ThWSDOutputType other)
        {
            return ND.Equals(other.ND) && OutputType.Equals(other.OutputType);
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
