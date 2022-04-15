using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.EQPMFanModelEnums;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.EQPMFanSelect
{
    /// <summary>
    /// UIExportTypeSelect.xaml 的交互逻辑
    /// </summary>
    public partial class UIExportTypeSelect : ThCustomWindow
    {
        EQPMExportViewModel exportViewModel;
        public UIExportTypeSelect()
        {
            InitializeComponent();
            exportViewModel = new EQPMExportViewModel();
            this.DataContext = exportViewModel;
        }
        public List<EnumScenario> GetSelectScenarios() 
        {
            var resList =new List<EnumScenario>();
            foreach (var item in exportViewModel.CheckListItems) 
            {
                if (item.IsChecked != true)
                    continue;
                resList.Add((EnumScenario)item.Value);
            }
            return resList;
        }
        private void btnOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
