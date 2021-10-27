﻿using System;
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
    public partial class AccidentAirConfig : ThCustomWindow
    {
        public LampblackClass _data { get; set; }
        public AccidentAirConfig(LampblackClass data)
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
            ProportionTxt.Text = data.Proportion.ToString();
            AirNumTxt.Text = data.AirNum.ToString();
            TotalTxt.Text = data.TotalValue.ToString();
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._data.ByNorm = RadioBtnTrue.IsChecked.Value;
            this._data.Proportion = double.Parse(ProportionTxt.Text);
            this._data.AirNum = int.Parse(AirNumTxt.Text);
            this._data.TotalValue = int.Parse(TotalTxt.Text);
            this.Close();
        }
    }
}
