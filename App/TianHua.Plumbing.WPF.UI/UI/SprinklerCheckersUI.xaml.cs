using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;

using ThMEPWSS;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class SprinklerCheckersUI : ThCustomWindow
    {
        private ThSprinklerCheckerVM VM;
        public SprinklerCheckersUI(ThSprinklerCheckerVM vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.VM = vm;
        }

        private void rbUpSprinkler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            IsEnabledOpen();
            this.chkItem5.IsEnabled = false;
            this.chkItem5.IsChecked = false;
            this.chkItem11.IsEnabled = false;
            this.chkItem11.IsChecked = false;
        }

        private void rbDownSprinkler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            IsEnabledOpen();
            this.chkItem4.IsEnabled = false;
            this.chkItem4.IsChecked = false;
            this.chkItem5.IsEnabled = false;
            this.chkItem5.IsChecked = false;
            this.chkItem8.IsEnabled = false;
            this.chkItem8.IsChecked = false;
        }

        private void rbSideSprinkler_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            IsEnabledOpen();
            this.chkItem1.IsEnabled = false;
            this.chkItem2.IsEnabled = false;
            this.chkItem3.IsEnabled = false;
            this.chkItem4.IsEnabled = false;
            this.chkItem7.IsEnabled = false;
            this.chkItem8.IsEnabled = false;
            this.chkItem11.IsEnabled = false;
            this.chkItem12.IsEnabled = false;
            this.chkItem1.IsChecked = false;
            this.chkItem2.IsChecked = false;
            this.chkItem3.IsChecked = false;
            this.chkItem4.IsChecked = false;
            this.chkItem7.IsChecked = false;
            this.chkItem8.IsChecked = false;
            this.chkItem11.IsChecked = false;
            this.chkItem12.IsChecked = false;
        }

        private void IsEnabledOpen()
        {
            this.chkItem1.IsEnabled = true;
            this.chkItem2.IsEnabled = true;
            this.chkItem3.IsEnabled = true;
            this.chkItem4.IsEnabled = true;
            this.chkItem5.IsEnabled = true;
            this.chkItem6.IsEnabled = true;
            this.chkItem7.IsEnabled = true;
            this.chkItem8.IsEnabled = true;
            this.chkItem9.IsEnabled = true;
            this.chkItem10.IsEnabled = true;
            this.chkItem11.IsEnabled = true;
            this.chkItem12.IsEnabled = true;
            this.chkItem1.IsChecked = true;
            this.chkItem2.IsChecked = true;
            this.chkItem3.IsChecked = true;
            this.chkItem4.IsChecked = true;
            this.chkItem5.IsChecked = true;
            this.chkItem6.IsChecked = true;
            this.chkItem7.IsChecked = true;
            this.chkItem8.IsChecked = true;
            this.chkItem9.IsChecked = true;
            this.chkItem10.IsChecked = true;
            this.chkItem11.IsChecked = true;
            this.chkItem12.IsChecked = true;
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

        private void lblDistance_MouseDown(object sender, MouseButtonEventArgs e)
        {
            chkItem12.IsChecked = !chkItem12.IsChecked;
        }

        private void tbMiddle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            chkItem12.IsChecked = !chkItem12.IsChecked;
        }

        private void tbBottom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            chkItem12.IsChecked = !chkItem12.IsChecked;
        }

        private void btnSet_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            var layerNum = btn.Tag.ToString();
            VM.SelectAll(layerNum);
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            var layerNum = btn.Tag.ToString();
            VM.Cancel(layerNum);
        }

        private void btnVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @"http://thlearning.thape.com.cn/kng/view/video/045ceb423e8040df8b435f34d5b317c8.html");
        }
    }
}
