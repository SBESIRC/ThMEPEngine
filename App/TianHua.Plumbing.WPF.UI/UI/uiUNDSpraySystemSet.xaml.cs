using System;
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
using ThMEPWSS.UndergroundSpraySystem.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiUNDSpraySystemSet.xaml 的交互逻辑
    /// </summary>
    public partial class uiUNDSpraySystemSet : ThCustomWindow
    {
        public SprayVMSet setViewModel;
        public uiUNDSpraySystemSet(SprayVMSet viewModel = null)
        {
            InitializeComponent();
            this.Title = "参数设置";
            setViewModel = viewModel;
            //orgViewModel = viewModel;
            if (null == viewModel)
                setViewModel = new SprayVMSet();
            this.DataContext = setViewModel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TextBox_TextChanged_FloorGap(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9')
                {
                    newStr += item;
                }
            }
            ((TextBox)e.Source).Text = newStr;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox).Text.Length >= 6)
                (sender as TextBox).Text = Convert.ToString(99999);
            else
            {
                int number = int.Parse((sender as TextBox).Text);
                if (number < 5000)
                    (sender as TextBox).Text = Convert.ToString(5000);
                else
                    e.Handled = false;
            }
        }

        private void FloorLineGap_KeyPress(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }
    }
}
