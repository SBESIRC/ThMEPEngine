using AcHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// uiFloorHeatingCoilObstacle.xaml 的交互逻辑
    /// </summary>
    public partial class uiFloorHeatingCoilObstacle : ThCustomWindow
    {
        ThFloorHeatingCoilViewModel CoilViewModel;

        public uiFloorHeatingCoilObstacle(ThFloorHeatingCoilViewModel vm)
        {
            InitializeComponent();
            CoilViewModel = vm;
            DataContext = CoilViewModel;
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CoilViewModel.CleanHighlight();
            //DialogResult = false;
            Hide();
            e.Cancel = true;
        }
    }
}
