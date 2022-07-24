using AcHelper;
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
    public partial class uiFloorHeatingCoil : ThCustomWindow
    {
        FloorHeatingCoilViewModel CoilViewModel;
        public static uiFloorHeatingCoil Instance;

        static uiFloorHeatingCoil()
        {
            if (Instance == null)
            {
                Instance = new uiFloorHeatingCoil();
            }
        }
        public uiFloorHeatingCoil()
        {
            InitializeComponent();
            this.MutexName = "THDNPG";
            if (CoilViewModel == null)
            {
                CoilViewModel = new FloorHeatingCoilViewModel();
            }
            DataContext = CoilViewModel;
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            FocusToCAD();
            SaveSetting();

            using (var cmd = new ThFloorHeatingCmd())
            {
                cmd.Execute();
            }
        }

        private void SaveSetting()
        {
            ThFloorHeatingCoilSetting.Instance.WithUI = true;
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
