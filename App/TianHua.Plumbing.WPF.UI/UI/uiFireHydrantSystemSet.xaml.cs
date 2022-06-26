using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    public partial class UiFireHydrantSystemSet : ThCustomWindow
    {
        public FireHydrantSystemSetViewModel setViewModel;
        public UiFireHydrantSystemSet(FireHydrantSystemSetViewModel viewModel = null)
        {
            InitializeComponent();
            Title = "参数设置";
            setViewModel = viewModel;
            if (null == viewModel)
            {
                setViewModel = new FireHydrantSystemSetViewModel();
            }
            DataContext = setViewModel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            //输入框数据校验
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

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((sender as TextBox).Text.Length >= 6)
            {
                (sender as TextBox).Text = Convert.ToString(99999);
            }
            else
            {
                int number = int.Parse((sender as TextBox).Text);
                if (number < 5000)
                {
                    (sender as TextBox).Text = Convert.ToString(5000);
                }
                else
                {
                    e.Handled = false;
                }
            }
        }
    }
}
