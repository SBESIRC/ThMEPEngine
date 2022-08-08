using ThControlLibraryWPF.ControlUtils;
using ThMEPStructure.StructPlane;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public class DrawingParameterSetModel : NotifyPropertyChangedBase
    {
        private string drawingScale = "";
        /// <summary>
        /// 图纸比例
        /// </summary>
        public string DrawingScale
        {
            get=> drawingScale;
            set
            {
                drawingScale = value;
                RaisePropertyChanged("DrawingScale");
            }
        }
        private double defaultSlabThick;
        /// <summary>
        /// 默认板厚
        /// </summary>
        public double DefaultSlabThick
        {
            get => defaultSlabThick;
            set
            {
                defaultSlabThick = value;
                RaisePropertyChanged("DefaultSlabThick");
            }
        }
        private string storey = "";
        /// <summary>
        /// 楼层
        /// </summary>
        public string Storey
        {
            get => storey;
            set
            {
                storey = value;
                RaisePropertyChanged("Storey");
            }
        }
        private StoreySelectOps storeySelectOption;
        /// <summary>
        /// 楼层选择
        /// </summary>
        public StoreySelectOps StoreySelectOption
        {
            get => storeySelectOption;
            set
            {
                storeySelectOption = value;
                RaisePropertyChanged("StoreySelectOption");
            }
        }
        private DrawingTypeOps drawingTypeOption;
        /// <summary>
        /// 成图类型
        /// </summary>
        public DrawingTypeOps DrawingTypeOption
        {
            get => drawingTypeOption;
            set
            {
                drawingTypeOption = value;
                RaisePropertyChanged("DrawingTypeOption");
            }
        }
        

        public DrawingParameterSetModel()
        {
            Load();
        }

        private void Load()
        {
            this.storey = ThDrawingParameterConfig.Instance.Storey;
            storey = ThDrawingParameterConfig.Instance.Storey;
            drawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
            defaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
            storeySelectOption = ThDrawingParameterConfig.Instance.IsAllStorey ? StoreySelectOps.All : StoreySelectOps.Single;
            drawingTypeOption = Convert(ThDrawingParameterConfig.Instance.DrawingType);
        }

        public void Write()
        {
            ThDrawingParameterConfig.Instance.Storey = storey;
            ThDrawingParameterConfig.Instance.DrawingScale = drawingScale;
            ThDrawingParameterConfig.Instance.DefaultSlabThick = defaultSlabThick;
            ThDrawingParameterConfig.Instance.IsAllStorey = storeySelectOption == StoreySelectOps.All;
            ThDrawingParameterConfig.Instance.DrawingType = Convert(drawingTypeOption);
        }

        private DrawingTypeOps Convert(string drawingTypeOption)
        {
            if (drawingTypeOption == ThStructurePlaneCommon.StructurePlanName ||
                string.IsNullOrEmpty(drawingTypeOption))
            {
                return DrawingTypeOps.StructurePlan;
            }
            else
            {
                return DrawingTypeOps.WallColumnDrawing;
            }
        }
        private string Convert(DrawingTypeOps drawingTypeOption)
        {
            if (drawingTypeOption == DrawingTypeOps.StructurePlan)
            {
                return ThStructurePlaneCommon.StructurePlanName;
            }
            else
            {
                return ThStructurePlaneCommon.WallColumnDrawingName;
            }
        }
    }
    /// <summary>
    /// 选择楼层
    /// </summary>
    public enum StoreySelectOps
    {
        /// <summary>
        /// 逐层
        /// </summary>
        Single = 0,
        /// <summary>
        /// 全部
        /// </summary>
        All = 1,
    }
    /// <summary>
    /// 成图类型
    /// </summary>
    public enum DrawingTypeOps
    {
        /// <summary>
        /// 结构平面图
        /// </summary>
        StructurePlan = 0,
        /// <summary>
        /// 墙柱施工图
        /// </summary>
        WallColumnDrawing = 1,
    }
}
