using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Text.RegularExpressions;
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

        private void tbProtectRadius_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double value;
            if (!double.TryParse(e.Text, out value))
            {
                e.Handled = true;
            }
        }

        private void tbProtectRadius_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
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

        private void tbNearbyDistance_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string content = textBox.Text + e.Text;
            Regex numbeRegex = new Regex("^[.][0-9]+$|^[0-9]*[.]{0,1}[0-9]*$");
            e.Handled =!numbeRegex.IsMatch(content);
            textBox.Text = textBox.Text.Trim();
        }
        private void tbNearbyDistance_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Space)
            //    e.Handled = true;
            if (
                (
                e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9 &&
                e.KeyboardDevice.Modifiers != ModifierKeys.Shift
                ) ||
                (
                e.Key >= Key.D0 && e.Key <= Key.D9 &&
                e.KeyboardDevice.Modifiers != ModifierKeys.Shift
                ) ||
                e.Key == Key.Back || e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Enter || e.Key == Key.Decimal || e.Key == Key.OemPeriod)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                {
                    e.Handled = false;
                }
            }
            else
            {
                e.Handled = true;
            }
        }


        private void tbNearbyDistance_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (!new NearbyDistanceRule().Validate(tb.Text))
            {
                double value ;
                if (double.TryParse(tb.Text, out value))
                {
                    FlushVM.Parameter.NearbyDistance = value;
                }
                tb.Focus();
            }
        }

        private void rbAreaFullLayout_Checked(object sender, RoutedEventArgs e)
        {
            tbNearbyDistance.IsEnabled = false;
        }

        private void rbNearbyDrainageFacility_Checked(object sender, RoutedEventArgs e)
        {
            tbNearbyDistance.IsEnabled = true;
        }
    }
}
