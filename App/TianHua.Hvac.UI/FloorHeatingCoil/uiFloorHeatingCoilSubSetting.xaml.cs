using AcHelper;
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
    /// uiFloorHeatingCoilSubSetting.xaml 的交互逻辑
    /// </summary>
    public partial class UiFloorHeatingCoilSubSetting : ThCustomWindow
    {
        ThFloorHeatingCoilViewModel CoilViewModel;
        public UiFloorHeatingCoilSubSetting(ThFloorHeatingCoilViewModel vm)
        {
            InitializeComponent();
            this.MutexName = "THDNPG";
            CoilViewModel = vm;
            DataContext = CoilViewModel;
        }
        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Close();
        }

    }
}
