using ThMEPWSS.ViewModel;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class SprinklerCheckersUI : ThCustomWindow
    {
        public SprinklerCheckersUI(ThSprinklerCheckerVM VM)
        {
            InitializeComponent();
            this.DataContext = VM;
        }

        private void rbUpSprinkler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            IsEnabledOpen();
        }

        private void rbDownSprinkler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            IsEnabledOpen();
            this.chkItem8.IsEnabled = false;
            this.chkItem8.IsChecked = false;
        }

        private void rbSideSprinkler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            IsEnabledOpen();
            this.chkItem1.IsEnabled = false;
            this.chkItem2.IsEnabled = false;
            this.chkItem3.IsEnabled = false;
            this.chkItem7.IsEnabled = false;
            this.chkItem8.IsEnabled = false;
            this.chkItem1.IsChecked = false;
            this.chkItem2.IsChecked = false;
            this.chkItem3.IsChecked = false;
            this.chkItem7.IsChecked = false;
            this.chkItem8.IsChecked = false;
        }

        private void IsEnabledOpen()
        {
            this.chkItem1.IsEnabled = true;
            this.chkItem2.IsEnabled = true;
            this.chkItem3.IsEnabled = true;
            this.chkItem6.IsEnabled = true;
            this.chkItem7.IsEnabled = true;
            this.chkItem8.IsEnabled = true;
            this.chkItem9.IsEnabled = true;
            this.chkItem1.IsChecked = true;
            this.chkItem2.IsChecked = true;
            this.chkItem3.IsChecked = true;
            this.chkItem6.IsChecked = true;
            this.chkItem7.IsChecked = true;
            this.chkItem8.IsChecked = true;
            this.chkItem9.IsChecked = true;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int value;
            if(string.IsNullOrEmpty(e.Text))
            {
                return;
            }
            if (!int.TryParse(e.Text, out value))
            {
                e.Handled = true;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //var tb = sender as System.Windows.Controls.TextBox;
            //if (tb.Name == "tbAboveBeam")
            //{
            //    if (new AboveBeamRule().Validate(tb.Text))
            //    {
            //        tb.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            //    }
            //    else
            //    {
            //        tb.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            //    }
            //}
        }

        private void lblAboveBeam_MouseDown(object sender, MouseButtonEventArgs e)
        {
            chkItem9.IsChecked = !chkItem9.IsChecked;
        }
    }
}
