﻿using AcHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using ThControlLibraryWPF;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.FloorHeatingCoil;
using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Model;
using TianHua.Hvac.UI.ViewModels;

namespace TianHua.Hvac.UI.FloorHeatingCoil
{
    /// <summary>
    /// uiFloorHeatingCoil.xaml 的交互逻辑
    /// </summary>
    public partial class UiFloorHeatingCoil : ThCustomWindow
    {
        ThFloorHeatingCoilViewModel CoilViewModel;
        public static UiFloorHeatingCoil Instance;
        uiFloorHeatingCoilObstacle ObstacleUI;
        uiFloorHeatingCoilSubSetting SubSettingUI;

        static UiFloorHeatingCoil()
        {
            if (Instance == null)
            {
                Instance = new UiFloorHeatingCoil();
            }
        }
        public UiFloorHeatingCoil()
        {
            InitializeComponent();
            this.MutexName = "THDNPG";
            if (CoilViewModel == null)
            {
                CoilViewModel = new ThFloorHeatingCoilViewModel();
            }
            DataContext = CoilViewModel;

        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CoilViewModel.CleanData();
            e.Cancel = true;
            this.Hide();
        }

        private void ObstacleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObstacleUI == null)
            {
                ObstacleUI = new uiFloorHeatingCoilObstacle(CoilViewModel);
            }

            CoilViewModel.UpdateHighLight();

            ObstacleUI.WindowStartupLocation =
                System.Windows.WindowStartupLocation.CenterScreen;

            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessWindow(ObstacleUI);
            //  ObstacleUI.ShowDialog();


        }

        private void SubSettingButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubSettingUI == null)
            {
                SubSettingUI = new uiFloorHeatingCoilSubSetting(CoilViewModel);
            }

            SubSettingUI.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            SubSettingUI.ShowDialog();
        }


    }

    public class TrueFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (int)value;
            return s == int.Parse(parameter.ToString());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;
            if (!isChecked)
            {
                return null;
            }
            return int.Parse(parameter.ToString());
        }
    }

}
