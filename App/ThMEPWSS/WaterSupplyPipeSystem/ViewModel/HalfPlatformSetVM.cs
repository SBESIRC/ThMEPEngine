using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.Diagram.ViewModel
{
    public class HalfPlatformSetVM : NotifyPropertyChangedBase
    {
        public HalfPlatformSetVM()
        {
            SupplyFloorDynamicRadios = new ObservableCollection<DynamicRadioButton>();
            SupplyFloorDynamicRadios.Add(new DynamicRadioButton() { Content = "+0.5层", GroupName = "type4", IsChecked = false });
            SupplyFloorDynamicRadios.Add(new DynamicRadioButton() { Content = "-0.5层", GroupName = "type4", IsChecked = true });

            HalfLayingDynamicRadios = new ObservableCollection<DynamicRadioButton>();
            HalfLayingDynamicRadios.Add(new DynamicRadioButton() { Content = "穿梁敷设", GroupName = "type5", IsChecked = false });
            HalfLayingDynamicRadios.Add(new DynamicRadioButton() { Content = "垫层敷设", GroupName = "type5", IsChecked = true });

            EntryLocationDynamicRadios = new ObservableCollection<DynamicRadioButton>();
            EntryLocationDynamicRadios.Add(new DynamicRadioButton() { Content = "入户门", GroupName = "type6", IsChecked = false });
            EntryLocationDynamicRadios.Add(new DynamicRadioButton() { Content = "半平台", GroupName = "type6", IsChecked = true });

            PipeThroughWellDynamicRadios = new ObservableCollection<DynamicRadioButton>();
            PipeThroughWellDynamicRadios.Add(new DynamicRadioButton() { Content = "是", GroupName = "type7", IsChecked = false });
            PipeThroughWellDynamicRadios.Add(new DynamicRadioButton() { Content = "否", GroupName = "type7", IsChecked = true });

            FirstFloorMeterLocationDynamicRadios = new ObservableCollection<DynamicRadioButton>();
            FirstFloorMeterLocationDynamicRadios.Add(new DynamicRadioButton() { Content = "一层水井/大堂", GroupName = "type8", IsChecked = false });
            FirstFloorMeterLocationDynamicRadios.Add(new DynamicRadioButton() { Content = "半平台", GroupName = "type8", IsChecked = true });


            OutRoofStairwellDynamicRadios = new ObservableCollection<DynamicRadioButton>();
            OutRoofStairwellDynamicRadios.Add(new DynamicRadioButton() { Content = "是", GroupName = "type9", IsChecked = false });
            OutRoofStairwellDynamicRadios.Add(new DynamicRadioButton() { Content = "否", GroupName = "type9", IsChecked = true });
        }
        

        private ObservableCollection<DynamicRadioButton> supplyFloorDynamicRadios { get; set; }
        /// <summary>
        /// 水表供水楼层数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> SupplyFloorDynamicRadios
        {
            get { return supplyFloorDynamicRadios; }
            set
            {
                this.supplyFloorDynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButton> halfLayingDynamicRadios { get; set; }
        /// <summary>
        /// 半平台敷设方式数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> HalfLayingDynamicRadios
        {
            get { return halfLayingDynamicRadios; }
            set
            {
                this.halfLayingDynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButton> entryLocationDynamicRadios { get; set; }
        /// <summary>
        /// 入户点位置数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> EntryLocationDynamicRadios
        {
            get { return entryLocationDynamicRadios; }
            set
            {
                this.entryLocationDynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButton> pipeThroughWellDynamicRadios { get; set; }
        /// <summary>
        /// 供水支管是否穿水井地板数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> PipeThroughWellDynamicRadios
        {
            get { return pipeThroughWellDynamicRadios; }
            set
            {
                this.pipeThroughWellDynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButton> firstFloorMeterLocationDynamicRadios { get; set; }
        /// <summary>
        /// 一层水表位置 数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> FirstFloorMeterLocationDynamicRadios
        {
            get { return firstFloorMeterLocationDynamicRadios; }
            set
            {
                this.firstFloorMeterLocationDynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicRadioButton> outRoofStairwellDynamicRadios { get; set; }
        /// <summary>
        /// 是否有出屋面楼梯间 数据列表
        /// </summary>
        public ObservableCollection<DynamicRadioButton> OutRoofStairwellDynamicRadios
        {
            get { return outRoofStairwellDynamicRadios; }
            set
            {
                this.outRoofStairwellDynamicRadios = value;
                this.RaisePropertyChanged();
            }
        }


        public HalfPlatformSetVM Clone()
        {
            var cloned = new HalfPlatformSetVM();
            return cloned;
        }
    }
}
