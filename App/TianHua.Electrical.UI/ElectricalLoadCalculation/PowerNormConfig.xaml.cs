using System;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.ElectricalLoadCalculation;

namespace TianHua.Electrical.UI.ElectricalLoadCalculation
{
    /// <summary>
    /// PowerNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class PowerNormConfig : ThCustomWindow
    {
        public PowerSpecifications _data { get; set; }
        public PowerNormConfig(PowerSpecifications data)
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
            MinTotalTxt.Text = data.MinTotalValue.ToString();
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
            this._data.MinTotalValue = ToNullInt(MinTotalTxt.Text);
            this._data.NormValue = ToNullInt(NormTxt.Text);
            this._data.TotalValue = 0;
            if(int.TryParse(TotalTxt.Text,out int num))
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

        public int? ToNullInt(string Value)
        {
            if (int.TryParse(Value, out int num))
            {
                return num;
            }
            else
            {
                return null;
            }
        }
    }
}
