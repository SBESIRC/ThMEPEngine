using ThControlLibraryWPF.CustomControl;
using TianHua.Plumbing.WPF.UI.ViewModels;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class FireHydrant : ThCustomWindow
    {
        public FireHydrant(ThFireHydrantVM fireHydrantVM)
        {
            InitializeComponent();
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
    }
}
