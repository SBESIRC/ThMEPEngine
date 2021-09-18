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
using AcHelper;
using ThControlLibraryWPF.CustomControl;
using ThMEPElectrical.FireAlarm.ViewModels;
using ThMEPElectrical.FireAlarm.Commands;
using ThMEPEngineCore.Command;


namespace TianHua.Electrical.UI
{
    /// <summary>
    /// uiThFireAlarm.xaml 的交互逻辑
    /// </summary>
    public partial class uiThFireAlarm : ThCustomWindow
    {
        static FireAlarmViewModel UiConfigs = null;
        public uiThFireAlarm()
        {
            InitializeComponent();
            if (UiConfigs == null)
            {
                UiConfigs = new FireAlarmViewModel();
            }
            DataContext = UiConfigs;
            MutexName = "Mutext_uiThFireAlarm";
        }

        private void btnLayout_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new FireAlarmLayoutCommand(UiConfigs))
            {
                FocusToCAD();
                cmd.Execute();
            }
        }

        private void btnRouting_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new FireAlarmRouteCableCommand(UiConfigs))
            {
                cmd.Execute();
            }
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
