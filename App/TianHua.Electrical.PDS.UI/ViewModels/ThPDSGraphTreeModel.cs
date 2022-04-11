using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Models;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public class ThPDSGraphTreeModel : NotifyPropertyChangedBase
    {
        readonly ThPDSCircuitGraphTreeModel o;
        public ThPDSGraphTreeModel(ThPDSCircuitGraphTreeModel o)
        {
            this.o = o;
        }
        public int Id => o.Id;
        public string Name
        {
            get => o.Name;
            set
            {
                if (value != Name)
                {
                    o.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public bool? IsChecked
        {
            get => o.IsChecked;
            set
            {
                if (value != IsChecked)
                {
                    o.IsChecked = value;
                    var node = this;
                    while (node != null)
                    {
                        node.OnPropertyChanged(nameof(IsChecked));
                        node = node.Parent;
                    }
                }
            }
        }
        public ThPDSGraphTreeModel Parent;
        ObservableCollection<ThPDSGraphTreeModel> _DataList;
        public ObservableCollection<ThPDSGraphTreeModel> DataList
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
