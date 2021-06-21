using ThMEPWSS.Diagram.ViewModel;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class FireHydrant : ThCustomWindow
    {
        private ThFireHydrantVM FireHydrantVM; 
        public FireHydrant(ThFireHydrantVM fireHydrantVM)
        {
            InitializeComponent();
            FireHydrantVM = fireHydrantVM;
            this.DataContext = fireHydrantVM;
        }

        private void rbFireHydrant_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if(rbFireHydrant.IsChecked ==true)
            {
                this.Height = 250;
                this.spFireHydrantPanel.Visibility = System.Windows.Visibility.Visible;
                this.spFireExtinguisherPanel.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.Height = 320;
                this.spFireHydrantPanel.Visibility = System.Windows.Visibility.Hidden;
                this.spFireExtinguisherPanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void rbFireExtinguisher_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (rbFireExtinguisher.IsChecked == true)
            {
                this.Height = 320;
                this.spFireExtinguisherPanel.Visibility = System.Windows.Visibility.Visible;
                this.spFireHydrantPanel.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.Height = 250;
                this.spFireExtinguisherPanel.Visibility = System.Windows.Visibility.Hidden;
                this.spFireHydrantPanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void rbCalculation_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTextBoxEnable((bool)rbCalculation.IsChecked, (bool)rbSelf.IsChecked);
        }

        private void rbCalculation_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTextBoxEnable((bool)rbCalculation.IsChecked, (bool)rbSelf.IsChecked);
        }

        private void rbSelf_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTextBoxEnable((bool)rbCalculation.IsChecked, (bool)rbSelf.IsChecked);
        }

        private void rbSelf_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            SetTextBoxEnable((bool)rbCalculation.IsChecked, (bool)rbSelf.IsChecked);
        }    
        private void SetTextBoxEnable(bool isCalculation,bool isSelf)
        {
            tbCalculationValue.IsEnabled = isCalculation;
            tbSelfValue.IsEnabled = isSelf;
        }

        private void ibProtectStrengthTip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string tip = "室内消火栓的布置应满足同一平面有2支消防水枪的2" +
                "股充实水柱同时达到任何部位的要求，且楼梯间及其" +
                "休息平台等安全区域可仅与一层视为同一平面。但当" +
                "建筑高度小于等于24.0米且体积小于等于5000m3的" +
                "多层仓库，可采用1支水枪充实水柱到达室内任何部位。";
            var tipDialog = new ThTipDialog("保护强度", tip);
            tipDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            tipDialog.ShowDialog();
        }

        private void ibWaterColumnLengthTip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string tip = "高层建筑、厂房、库房和室内净空高度超过8m的民" +
                "用建筑等场所的消火栓栓口动压，不应小于" +
                "0.35MPa，且消防防水枪充实水柱应按13m计算；其" +
                "他场所的消火栓栓口动压不应小于0.25MPa，且消防" +
                "水枪充实水柱应按10m计算";
            var tipDialog = new ThTipDialog("水柱长度", tip);
            tipDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            tipDialog.ShowDialog();
        }

        private void chkShowResult_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            FireHydrantVM.ShowCheckExpression();
        }

        private void chkShowResult_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            FireHydrantVM.CloseCheckExpress();
        }
    }
}
