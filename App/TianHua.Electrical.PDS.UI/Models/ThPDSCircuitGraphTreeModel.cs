using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSCircuitGraphTreeModel : NotifyPropertyChangedBase
    {
        public int Id { get; set; }
        string _Name;
        public string Name
        {
            get => _Name;
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        bool? _IsChecked = false;
        public bool? IsChecked
        {
            get => _IsChecked;
            set
            {
                if (value != _IsChecked)
                {
                    _IsChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }
        bool _IsSelected;
        public bool IsSelected
        {
            get => _IsSelected;
            set
            {
                if (value != _IsSelected)
                {
                    _IsSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        bool _IsExpanded;
        public bool IsExpanded
        {
            get => _IsExpanded;
            set
            {
                if (value != _IsExpanded)
                {
                    _IsExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        ObservableCollection<ThPDSCircuitGraphTreeModel> _DataList;
        public ObservableCollection<ThPDSCircuitGraphTreeModel> DataList
        {
            get => _DataList;
            set
            {
                if (value != _DataList)
                {
                    _DataList = value;
                    OnPropertyChanged(nameof(DataList));
                }
            }
        }
    }
}