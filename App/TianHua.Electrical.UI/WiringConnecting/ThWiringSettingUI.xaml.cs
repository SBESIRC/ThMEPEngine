﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using TianHua.Electrical.UI.WiringConnecting.ViewModel;

namespace TianHua.Electrical.UI.WiringConnecting
{
    /// <summary>
    /// UIEmgLightLayout.xaml 的交互逻辑
    /// </summary>
    public partial class ThWiringSettingUI : Window
    {
        private static WiringConnectingViewModel settingVM = null;
        public ThWiringSettingUI()
        {
            InitializeComponent();
            if (settingVM == null)
            {
                settingVM = new WiringConnectingViewModel();
            }
            this.DataContext = settingVM;
        }
    
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
