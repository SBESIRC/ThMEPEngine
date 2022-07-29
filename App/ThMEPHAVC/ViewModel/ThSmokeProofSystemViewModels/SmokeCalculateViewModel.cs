using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using System.Windows.Controls;

namespace ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels
{
    public class SmokeCalculateViewModel : NotifyPropertyChangedBase
    {
        public SmokeCalculateViewModel()
        {
            InitItems();
        }

        /// <summary>
        /// 应用场景列表
        /// </summary>
        private ObservableCollection<UTableItem> _functionTableItems;
        public ObservableCollection<UTableItem> FunctionTableItems
        {
            get { return _functionTableItems; }
            set
            {
                _functionTableItems = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 应用场景选择项
        /// </summary>
        private UTableItem _selectTableItem;
        public UTableItem SelectTableItem
        {
            get { return _selectTableItem; }
            set
            {
                _selectTableItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 系统名称
        /// </summary>
        private string _systemName = "";
        public string SystemName
        {
            get { return _systemName; }
            set
            {
                _systemName = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 机械送风/自然送风
        /// </summary>
        private ObservableCollection<UTableItem> _airSupplyTableItems;
        public ObservableCollection<UTableItem> AirSupplyTableItems
        {
            get { return _airSupplyTableItems; }
            set
            {
                _airSupplyTableItems = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 机械送风/自然送风选择项
        /// </summary>
        private UTableItem _airSupplySelectTableItem;
        public UTableItem AirSupplySelectTableItem
        {
            get { return _airSupplySelectTableItem; }
            set
            {
                _airSupplySelectTableItem = value;
                this.RaisePropertyChanged();
            }
        }

        public void InitItems()
        {
            this.AirSupplyTableItems = new ObservableCollection<UTableItem>()
            {
                new UTableItem("自然送风", null),
                new UTableItem("机械送风", null),
            };
            AirSupplySelectTableItem = AirSupplyTableItems[1];
        }
    }

    public class UTableItem
    {
        public string ItemUid { get; }
        public string Title { get; }
        public UserControl ShowUserControl { get; }
        public UTableItem(string title, UserControl userControl)
        {
            this.Title = title;
            this.ItemUid = Guid.NewGuid().ToString();
            this.ShowUserControl = userControl;
        }
    }

    public enum FlexDataKeyType
    {
        MianVm,

        UserControlVm,

        Volume,
    }
}
