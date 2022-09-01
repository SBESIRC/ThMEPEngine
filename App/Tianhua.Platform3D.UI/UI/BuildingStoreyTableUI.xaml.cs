using System.Windows.Controls;
using System.Collections.ObjectModel;
using ThMEPIFC.Model;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// StoreyTableEntrance.xaml 的交互逻辑
    /// </summary>
    public partial class BuildingStoreyTableUI : UserControl
    {
        public BuildingStoreyTableUI(ObservableCollection<ThEditStoreyInfo> dataText)
        {
            InitializeComponent();
            this.datagrid1.ItemsSource = dataText;
        }
    }
}
