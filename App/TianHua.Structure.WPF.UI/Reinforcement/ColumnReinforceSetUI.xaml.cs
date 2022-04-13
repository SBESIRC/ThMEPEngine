using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public partial class ColumnReinforceSetUI : ThCustomWindow
    {
        private ColumnReinforceSetVM ViewModel;
        public ColumnReinforceSetUI()
        {
            InitializeComponent();
            ViewModel = new ColumnReinforceSetVM();
            this.DataContext = ViewModel;
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Set();
            this.Close();
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Reset();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9.-]+");
            e.Handled = re.IsMatch(e.Text);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void tbProtectThick_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //
        }
    }
}
