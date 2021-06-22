using System.Windows;
using System.Windows.Input;
using ThMEPWSS.ViewModel;
using TianHua.Plumbing.WPF.UI.Validations;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class FlushPointUI : Window
    {
        private ThFlushPointVM FlushVM { get; set; }
        
        public FlushPointUI(ThFlushPointVM flushVM)
        {
            InitializeComponent();
            FlushVM = flushVM;
            this.DataContext = FlushVM;
        }

        private void tbProtectRadius_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double value;
            if(!double.TryParse(e.Text,out value))
            {
                e.Handled = true;
            }
        }

        private void tbProtectRadius_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void chkOtherSpace_Checked(object sender, RoutedEventArgs e)
        {
            this.chkNecesaryArrangeSpacePoints.IsEnabled = true;
            this.chkParkingAreaPoints.IsEnabled = true;
        }

        private void chkOtherSpace_Unchecked(object sender, RoutedEventArgs e)
        {
            this.chkNecesaryArrangeSpacePoints.IsChecked = false;
            this.chkParkingAreaPoints.IsChecked = false;
            this.chkNecesaryArrangeSpacePoints.IsEnabled = false;
            this.chkParkingAreaPoints.IsEnabled = false;
        }

        private void tbProtectRadius_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.TextBox;
            if(!new ProtectRadiusRule().Validate(tb.Text))
            {
                double value = 0.0;
                if (double.TryParse(tb.Text, out value))
                {
                    FlushVM.Parameter.ProtectRadius = value;
                }
                tb.Focus();
            }
        }
    }
}
