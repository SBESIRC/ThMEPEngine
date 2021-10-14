using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiDrainageSystemSet.xaml 的交互逻辑
    /// </summary>
    public partial class uiUNDPDrainageSystemSet : ThCustomWindow
    {
        double OldSetting = 0;
        PressureDrainageSystemDiagramVieModel _viewmodel = null;
        public uiUNDPDrainageSystemSet(PressureDrainageSystemDiagramVieModel viewmodel)
        {
            InitializeComponent();
            this.Title = "参数设置";
            _viewmodel = viewmodel;
            this.DataContext = _viewmodel;
            OldSetting = viewmodel.UndpdsFloorLineSpace;
        }
        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            _viewmodel.UndpdsFloorLineSpace = OldSetting;
            this.DialogResult = false;
            this.Close();
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            //输入框数据校验
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)//光标丢失时，楼层线间距输入值判断
        {
            TextBox txtBox = sender as TextBox;
            string strText = txtBox.Text;
            if (strText.Length >= 6)
                (sender as TextBox).Text = Convert.ToString(99999);
            else
            {
                int max = 99999;
                int min = 4000;
                int number = int.Parse(strText);
                if (number < min)
                    (sender as TextBox).Text = Convert.ToString(min);
                else if (number > max)
                    (sender as TextBox).Text = Convert.ToString(max);
                else
                    e.Handled = false;
            }
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
        private void FloorLineGap_KeyPress(object sender, KeyEventArgs e)//输入键值判断，只能输入 0 到 9
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
