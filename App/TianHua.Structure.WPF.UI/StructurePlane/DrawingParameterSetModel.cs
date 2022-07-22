using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPStructure.StructPlane;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    internal class DrawingParameterSetModel : NotifyPropertyChangedBase
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
        /// <summary>
        /// 楼层集合
        /// </summary>
        public ObservableCollection<string> Storeies { get; set; }
        /// <summary>
        /// 图纸比例集合
        /// </summary>
        public ObservableCollection<string> DrawingScales { get; set; }

        public DrawingParameterSetModel()
        {
            Load();
        }

        private void Load()
        {
            Storeies = new ObservableCollection<string>(ThDrawingParameterConfig.Instance.Storeies);
            DrawingScales = new ObservableCollection<string>(ThDrawingParameterConfig.Instance.DrawingScales);

            storey = ThDrawingParameterConfig.Instance.Storey;
            drawingScale = ThDrawingParameterConfig.Instance.DrawingScale;
            defaultSlabThick = ThDrawingParameterConfig.Instance.DefaultSlabThick;
            storeySelectOption = ThDrawingParameterConfig.Instance.IsAllStorey ? StoreySelectOps.All : StoreySelectOps.Single;
        }

        public void Write()
        {
            ThDrawingParameterConfig.Instance.Storey = storey;
            ThDrawingParameterConfig.Instance.DrawingScale = drawingScale;
            ThDrawingParameterConfig.Instance.DefaultSlabThick = defaultSlabThick;
            ThDrawingParameterConfig.Instance.IsAllStorey = storeySelectOption == StoreySelectOps.All;
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
}
