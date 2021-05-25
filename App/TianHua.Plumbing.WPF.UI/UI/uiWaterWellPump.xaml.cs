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
using ThMEPWSS.Diagram.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiWaterWellPump.xaml 的交互逻辑
    /// </summary>
    public partial class uiWaterWellPump : ThCustomWindow
    {
        public WaterwellPumpParamsViewModel ViewModel = new WaterwellPumpParamsViewModel();
        public uiWaterWellPump()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            var rst = ViewModel.FilterByWatewellSize;
        }
    }
}
