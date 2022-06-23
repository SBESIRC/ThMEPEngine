using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSCircuitGraphTreeModel : NotifyPropertyChangedBase
    {
        public string NodeUID { get; set; }
        public string Key { get; set; }
        public object Tag { get; set; }
        public bool IsRoot { get; set; }
        public ThPDSCircuitGraphTreeModel Parent { get; set; }
        public ThPDSCircuitGraphTreeModel Root { get; set; }
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
                SetIsChecked(value, true, true);
            }
        }
        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _IsChecked) return;
            _IsChecked = value;
            if (updateChildren && _IsChecked.HasValue && DataList != null)
            {
                foreach (var o in DataList)
                {
                    o.SetIsChecked(_IsChecked, true, false);
                }
            }
            if (updateParent) Parent?.VerifyCheckState();
            OnPropertyChanged(nameof(IsChecked));
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < DataList.Count; ++i)
            {
                bool? current = DataList[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            SetIsChecked(state, false, true);
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