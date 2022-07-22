using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Structure.WPF.UI.StructurePlane
{
    public partial class DrawingParameterSetUI : ThCustomWindow
    {
        private DrawingParameterSetVM ViewModel;
        public DrawingParameterSetUI(DrawingParameterSetVM viewmodel)
        {
            InitializeComponent();
            ViewModel = viewmodel;
            this.DataContext = ViewModel;
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            ViewModel.Run();
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

        private void rbAllStorey_Checked(object sender, RoutedEventArgs e)
        {
            cbStoreies.IsEnabled = false;
        }

        private void rbSingleStorey_Checked(object sender, RoutedEventArgs e)
        {
            cbStoreies.IsEnabled = true;
        }
    }
}
