﻿using AcHelper;
using AcHelper.Commands;
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
using ThMEPHVAC.LoadCalculation.Command;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// ColdNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class RoomNumber : ThCustomWindow
    {
        public RoomNumber()
        {
            InitializeComponent();
            this.PrefixContentTxt.Text = ThLoadCalculationUIService.Instance.Parameter.PerfixContent;
            this.StartingNumlblTxt.Text = ThLoadCalculationUIService.Instance.Parameter.StartingNum;
            this.NumberIndicationlbl.Content = HasPrefix.IsChecked.Value ? this.PrefixContentTxt.Text : "" + this.StartingNumlblTxt.Text;
            this.StartingNumlblTxt.Focus();
            this.StartingNumlblTxt.SelectionStart = this.StartingNumlblTxt.Text.Length;
        }

        private void PrefixContentTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!HasPrefix.IsNull() && !PrefixContentTxt.IsNull() && !StartingNumlblTxt.IsNull() && !NumberIndicationlbl.IsNull())
                this.NumberIndicationlbl.Content = (HasPrefix.IsChecked.Value ? this.PrefixContentTxt.Text : "") + this.StartingNumlblTxt.Text;
        }

        private void StartingNumlblTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!HasPrefix.IsNull() && !PrefixContentTxt.IsNull() && !StartingNumlblTxt.IsNull() && !NumberIndicationlbl.IsNull())
                this.NumberIndicationlbl.Content = (HasPrefix.IsChecked.Value ? this.PrefixContentTxt.Text : "") + this.StartingNumlblTxt.Text;
        }

        private void HasPrefix_Checked(object sender, RoutedEventArgs e)
        {
            if (!HasPrefix.IsNull() && !PrefixContentTxt.IsNull() && !StartingNumlblTxt.IsNull() && !NumberIndicationlbl.IsNull())
                this.NumberIndicationlbl.Content = HasPrefix.IsChecked.Value ? this.PrefixContentTxt.Text : "" + this.StartingNumlblTxt.Text;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ThLoadCalculationUIService.Instance.Parameter.HasPrefix = HasPrefix.IsChecked.Value;
            ThLoadCalculationUIService.Instance.Parameter.PerfixContent = this.PrefixContentTxt.Text;
            ThLoadCalculationUIService.Instance.Parameter.StartingNum = this.StartingNumlblTxt.Text;
            FocusToCAD();
            using (var cmd = new ThRoomFunctionNumIncreaseCmd())
            {
                cmd.Execute();
            }

            this.StartingNumlblTxt.Text = ThLoadCalculationUIService.Instance.Parameter.StartingNum;
        }
        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }

    }
}
