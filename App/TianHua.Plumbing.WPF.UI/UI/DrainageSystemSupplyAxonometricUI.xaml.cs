using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
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
using AcHelper.Commands;

using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.ViewModel;
using ThMEPWSS.DrainageSystemDiagram;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// DrainageSystemSupplyAxonometricUI.xaml 的交互逻辑
    /// </summary>
    public partial class DrainageSystemSupplyAxonometricUI : ThCustomWindow
    {
        private static DrainageSystemSupplyAxonoViewModel axonoVM = null;
        public DrainageSystemSupplyAxonometricUI()
        {
            InitializeComponent();
            if (axonoVM == null)
            {
                axonoVM = new DrainageSystemSupplyAxonoViewModel();
            }

            var alpha = THDrainageADUISetting.Instance.alpha;
            this.DataContext = axonoVM;

        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Active.Document == null)
                return;

            THDrainageADUISetting.Instance.alpha = axonoVM.alpha;
            axonoVM.scenarioValue[axonoVM.scenario.Value] = axonoVM.alpha;
            CommandHandlerBase.ExecuteFromCommandLine(false, "-THJSZC");
            FocusToCAD();
        }

        private void scenarioSelect_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            double alpha = axonoVM.scenarioValue[axonoVM.scenario.Value];
            axonoVM.alpha = alpha;
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
