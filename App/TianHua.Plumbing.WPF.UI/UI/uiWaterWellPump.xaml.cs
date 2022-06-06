using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiWaterWellPump.xaml 的交互逻辑
    /// </summary>
    public partial class UiWaterWellPump : ThCustomWindow
    {
        private static uiWaterWellPumpInfo WellInfoWidget = null;
        private static WaterwellPumpParamsViewModel ViewModel = null;
        public UiWaterWellPump()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new WaterwellPumpParamsViewModel();
            }
            if (WellInfoWidget == null)
            {
                WellInfoWidget = new uiWaterWellPumpInfo();
            }
            DataContext = ViewModel;
            MutexName = "Mutext_uiWaterWellPump";
        }

        private void btnFixDeepWaterPump_Click(object sender, RoutedEventArgs e)
        {
            ThCreateWaterWellPumpCmd cmd = new ThCreateWaterWellPumpCmd(ViewModel);
            cmd.WellConfigInfo = WellInfoWidget.GetViewModel().WellConfigInfo;
            cmd.Execute();
        }

        private void btnGenerTable_Click(object sender, RoutedEventArgs e)
        {
            ThCreateWithdrawalFormCmd cmd = new ThCreateWithdrawalFormCmd(ViewModel);
            cmd.WellConfigInfo = WellInfoWidget.GetViewModel().WellConfigInfo;
            cmd.Execute();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txtBox = sender as TextBox;

            string strText = txtBox.Text;
            if (strText.IsNullOrEmpty())
            {
                (sender as TextBox).Text = Convert.ToString(1.0);
            }
            else
            {
                double max = 9.9;
                double min = 0.0;
                double number = double.Parse(strText);
                if (number < min)
                    (sender as TextBox).Text = Convert.ToString(min);
                else if (number > max)
                    (sender as TextBox).Text = Convert.ToString(max);
                else
                    e.Handled = false;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.Decimal)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var str = ((TextBox)e.Source).Text.ToString();
            if (string.IsNullOrEmpty(str))
                return;
            var charArrs = str.ToCharArray();
            var newStr = "";
            foreach (var item in charArrs)
            {
                if (item >= '0' && item <= '9' || item == '.')
                {
                    newStr += item;
                }
            }
            ((TextBox)e.Source).Text = newStr;
        }

        private void btnCheckPump_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnSelectWell_Click(object sender, RoutedEventArgs e)
        {
            WellInfoWidget.HighlightWell();
            AcadApp.ShowModelessWindow(WellInfoWidget);
        }
    }
}
