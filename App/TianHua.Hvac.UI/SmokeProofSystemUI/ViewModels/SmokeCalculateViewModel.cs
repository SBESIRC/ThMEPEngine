using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using System.Windows.Controls;
using TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofUserControl;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.ViewModels
{
    class SmokeCalculateViewModel : NotifyPropertyChangedBase
    {
        public SmokeCalculateViewModel()
        {
            InitItems();
        }

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

        private UTableItem _selectTableItem { get; set; }
        public UTableItem SelectTableItem
        {
            get { return _selectTableItem; }
            set
            {
                _selectTableItem = value;
                this.RaisePropertyChanged();
            }
        }

        public void InitItems()
        {
            this.FunctionTableItems = new ObservableCollection<UTableItem>() { 
                new UTableItem("消防电梯前室", new FireElevatorFrontRoomUserControl()),
                new UTableItem("独立或合用前室（楼梯间自然）", new SeparateOrSharedNaturalUserControl()),
                new UTableItem("独立或合用前室（楼梯间送风）", new SeparateOrSharedWindUserControl()),
                new UTableItem("楼梯间（前室不送风）", new StaircaseNoWindUserControl()),
                new UTableItem("楼梯间（前室送风）", new StaircaseWindUserControl()),
                new UTableItem("封闭避难层（间）、避难走道", new EvacuationWalkUserControl()),
                new UTableItem("避难走道前室", new EvacuationFrontUserControl()),
            };
            SelectTableItem = FunctionTableItems[0];
        }
    }

    class UTableItem
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
}
