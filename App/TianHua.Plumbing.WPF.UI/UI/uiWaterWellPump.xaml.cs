using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiWaterWellPump.xaml 的交互逻辑
    /// </summary>
    public partial class UiWaterWellPump : ThCustomWindow
    {
        private WaterwellPumpParamsViewModel ViewModel = new WaterwellPumpParamsViewModel();
        public UiWaterWellPump()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void btnWaterwellRecog_Click(object sender, RoutedEventArgs e)
        {
            uiWaterWellPumpFilter uiFilter = new uiWaterWellPumpFilter();
            if(uiFilter.ShowDialog() == true)
            {
                ViewModel.SetIdentfyConfigInfo(uiFilter.GetIdentfyConfigInfo());
            }
        }

        private void btnFixDeepWaterPump_Click(object sender, RoutedEventArgs e)
        {
            ThCreateWaterWellPumpCmd cmd = new ThCreateWaterWellPumpCmd(ViewModel);
            cmd.Execute();
        }

        private void btnGenerTable_Click(object sender, RoutedEventArgs e)
        {
            ThCreateWithdrawalFormCmd cmd = new ThCreateWithdrawalFormCmd(ViewModel);
            cmd.Execute();
        }
    }
}
