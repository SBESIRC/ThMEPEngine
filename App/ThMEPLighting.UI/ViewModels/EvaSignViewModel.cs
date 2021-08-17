using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThControlLibraryWPF.ControlUtils;
using ThMEPLighting.Common;

namespace ThMEPLighting.UI.ViewModels
{
    public class EvaSignViewModel : NotifyPropertyChangedBase
    {
        public EvaSignViewModel()
        {
            this.LightLayoutType = LayoutTypeEnum.WallLayout;

            VerticalSpaceItems.Add(new UListItemData("20",0, (double)20.0));
            VerticalSpaceItems.Add(new UListItemData("30",1, (double)30.0));
            VerticalSelectItem = VerticalSpaceItems.FirstOrDefault();
            

            ParallelSpaceItems.Add(new UListItemData("10", 0, (double)10.0));
            ParallelSpaceItems.Add(new UListItemData("15", 1, (double)15.0));
            ParallelSelectItem = ParallelSpaceItems.FirstOrDefault();

            List<int> values = new List<int>
            {
                (int)ThEnumBlockScale.DrawingScale1_100,
                (int)ThEnumBlockScale.DrawingScale1_150,
            };
            var raiseDim = CommonUtil.EnumDescriptionToList(typeof(ThEnumBlockScale), values);
            foreach (var raise in raiseDim)
            {
                BlockScaleItems.Add(raise);
            }
            BlockSacleSelectItem = BlockScaleItems.Where(c => c.Value == (int)ThEnumBlockScale.DrawingScale1_100).FirstOrDefault();
        }
        /// <summary>
        /// 指示灯排布方式
        /// </summary>
        private LayoutTypeEnum _layoutType { get; set; }
        /// <summary>
        /// 指示灯排布方式
        /// </summary>
        public LayoutTypeEnum LightLayoutType
        {
            get { return _layoutType; }
            set
            {
                _layoutType = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _verticalSpaceItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> VerticalSpaceItems
        {
            get { return _verticalSpaceItems; }
            set { _verticalSpaceItems = value;this.RaisePropertyChanged(); }
        }
        private ObservableCollection<UListItemData> _parallelSpaceItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ParallelSpaceItems
        {
            get { return _parallelSpaceItems; }
            set { _parallelSpaceItems = value; this.RaisePropertyChanged(); }
        }
        private ObservableCollection<UListItemData> _blockScaleItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> BlockScaleItems
        {
            get { return _blockScaleItems; }
            set { _blockScaleItems = value; this.RaisePropertyChanged(); }
        }
        private UListItemData _parallelSelectItem { get; set; }
        public UListItemData ParallelSelectItem 
        {
            get { return _parallelSelectItem; }
            set 
            {
                _parallelSelectItem = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _verticalSelectItem { get; set; }
        public UListItemData VerticalSelectItem 
        {
            get { return _verticalSelectItem; }
            set 
            {
                _verticalSelectItem = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _blockSacleSelectItem { get; set; }
        public UListItemData BlockSacleSelectItem
        {
            get { return _blockSacleSelectItem; }
            set
            {
                _blockSacleSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

    }
    public enum LayoutTypeEnum
    {
        WallLayout = 0,
        HostingLayout = 1
    }
}
