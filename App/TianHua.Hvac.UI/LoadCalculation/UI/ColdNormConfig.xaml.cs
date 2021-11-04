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
using ThMEPHVAC.LoadCalculation.Model;

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// ColdNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class ColdNormConfig : ThCustomWindow
    {
        public NormClass _data { get; set; }
        public ColdNormConfig(NormClass data)
        {
            InitializeComponent();
            this._data = data;
            if(data.ByNorm)
            {
                RadioBtnTrue.IsChecked = true;
                RadioBtnFalse.IsChecked = false;
            }
            else
            {
                RadioBtnTrue.IsChecked = false;
                RadioBtnFalse.IsChecked = true;
            }
            NormTxt.Text = data.NormValue.ToString();
            TotalTxt.Text = data.TotalValue.ToString();
            this.NormTxt.Focus();
            this.NormTxt.SelectionStart = this.NormTxt.Text.Length;
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._data.ByNorm = RadioBtnTrue.IsChecked.Value;
            if (string.IsNullOrEmpty(NormTxt.Text))
            {
                this._data.NormValue = null;
            }
            else
            {
                this._data.NormValue = double.Parse(NormTxt.Text);
            }
            this._data.TotalValue = 0;
            if(double.TryParse(TotalTxt.Text,out double num))
            {
                this._data.TotalValue = num;
            }
            this.Close();
        }

        private void RadioBtnTrue_Checked(object sender, RoutedEventArgs e)
        {
            if (!NormTxt.IsNull() && !TotalTxt.IsNull())
            {
                NormTxt.IsEnabled = true;
                TotalTxt.IsEnabled = false;
            }
        }

        private void RadioBtnFalse_Checked(object sender, RoutedEventArgs e)
        {
            if (!NormTxt.IsNull()  && !TotalTxt.IsNull())
            {
                NormTxt.IsEnabled = false;
                TotalTxt.IsEnabled = true;
            }
        }
    }
}
