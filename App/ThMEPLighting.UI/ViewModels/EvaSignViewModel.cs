using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;

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
    }
    public enum LayoutTypeEnum
    {
        WallLayout = 0,
        HostingLayout = 1
    }
}
