﻿using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.FanLayout.Command;
using ThMEPHVAC.FanLayout.ViewModel;

namespace TianHua.Hvac.UI.UI
{
    /// <summary>
    /// uiFanLayoutMainWidget.xaml 的交互逻辑
    /// </summary>
    public partial class uiFanLayoutMainWidget : ThCustomWindow
    {
        private static ThFanLayoutViewModel ViewModel = null;
        public uiFanLayoutMainWidget()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new ThFanLayoutViewModel();
            }
            this.DataContext = ViewModel;
        }

        private void btnInsertFan_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel.thFanLayoutConfigInfo.WAFConfigInfo = FanWAFWidget.GetFanWAFConfigInfo();
            ViewModel.thFanLayoutConfigInfo.WEXHConfigInfo = FanWEXHWidget.GetFanWEXHConfigInfo();
            ViewModel.thFanLayoutConfigInfo.CEXHConfigInfo = FanCEXHWidget.GetFanCEXHConfigInfo();
            var cmd = new ThFanLayoutExtractCmd();
            cmd.thFanLayoutConfigInfo = ViewModel.thFanLayoutConfigInfo;
            cmd.Execute();
        }

        private void btnExportMat_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
