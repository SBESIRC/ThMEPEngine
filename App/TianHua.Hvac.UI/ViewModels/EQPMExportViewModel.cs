using System.Windows.Input;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using CommunityToolkit.Mvvm.Input;
using ThMEPHVAC.EQPMFanModelEnums;

namespace TianHua.Hvac.UI.ViewModels
{
    class EQPMExportViewModel : NotifyPropertyChangedBase
    {
        public EQPMExportViewModel() 
        {
            var allItems = CommonUtil.EnumDescriptionToList<EnumScenario>();
            CheckListItems = new ObservableCollection<UListCheckItemViewModel>();
            allItems.ForEach(c => c.IsChecked = true);
            allItems.ForEach(c => CheckListItems.Add(new UListCheckItemViewModel(c)));
            IsSelectAll = true;
        }
        private bool? isSelectAll { get; set; }
        public bool? IsSelectAll 
        {
            get { return isSelectAll; }
            set 
            {
                isSelectAll = value;
                IsSelectAllChange();
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListCheckItemViewModel> _listCheckItems;
        public ObservableCollection<UListCheckItemViewModel> CheckListItems 
        {
            get { return _listCheckItems; }
            set 
            {
                _listCheckItems = value;
                this.RaisePropertyChanged();
            }
        }

        RelayCommand listCheckedChange;
        public ICommand ListCheckedChange
        {
            get
            {
                if (listCheckedChange == null)
                    listCheckedChange = new RelayCommand(() => ItemSelectChange());

                return listCheckedChange;
            }
        }

        void IsSelectAllChange() 
        {
            if (IsSelectAll == null)
                return;
            foreach (var item in CheckListItems) 
            {
                item.IsChecked = IsSelectAll;
            }
            RaisePropertyChanged("CheckListItems");
        }
        void ItemSelectChange() 
        {
            bool isAllSelect = true;
            bool haveSelect = false;
            foreach (var item in CheckListItems) 
            {
                if(item.IsChecked != true) 
                {
                    isAllSelect = false;
                }
                else if (item.IsChecked == true) 
                {
                    haveSelect = true;
                }
            }
            if (isAllSelect)
            {
                IsSelectAll = true;
            }
            else if(haveSelect) 
            {
                IsSelectAll = null;
            }
            else
            {
                IsSelectAll = false;
            }
        }
    }
}
