using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPLighting.UI.ViewModels
{
    public class ExampleViewModel : NotifyPropertyChangedBase
    {
        public ExampleViewModel() 
        {


            ListViewDatas.Add(new UListItemData("测试1", 0));
            ListViewDatas.Add(new UListItemData("测试2", 1));
            ListViewDatas.Add(new UListItemData("测试3", 2));
            ListViewDatas.Add(new UListItemData("测试4", 3));
            ListSelectItem = ListViewDatas.FirstOrDefault();
        }
        private ObservableCollection<UListItemData> _listViewDatas = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ListViewDatas
        {
            get { return _listViewDatas; }
            set { _listViewDatas = value; this.RaisePropertyChanged(); }
        }
        private UListItemData _listSelectItem { get; set; }
        public UListItemData ListSelectItem
        {
            get { return _listSelectItem; }
            set
            {
                _listSelectItem = value;
                this.RaisePropertyChanged();
            }
        }
        RelayCommand listSelectedChange;
        public RelayCommand ListSelectedChange 
        {
            get 
            {
                if (listSelectedChange == null) 
                    listSelectedChange = new RelayCommand(() => TestSelectedChange());

                return listSelectedChange;
            }
            set { listSelectedChange = value; }
        }
        private void TestSelectedChange ()
        {
        
        }
    }
}
