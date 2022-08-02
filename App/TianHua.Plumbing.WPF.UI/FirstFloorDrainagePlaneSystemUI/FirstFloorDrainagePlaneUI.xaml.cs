using AcHelper;
using AcHelper.Commands;
using DotNetARX;
using Linq2Acad;
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
using ThCADExtension;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS;
using ThMEPWSS.Command;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using TianHua.Plumbing.WPF.UI.Command;
using TianHua.Plumbing.WPF.UI.UI;

namespace TianHua.Plumbing.WPF.UI.FirstFloorDrainagePlaneSystemUI
{
    /// <summary>
    /// FirstFloorDrainagePlaneUI.xaml 的交互逻辑
    /// </summary>
    public partial class FirstFloorDrainagePlaneUI : ThCustomWindow
    {
        FirstFloorPlaneViewModel firstFloorPlaneViewModel = new FirstFloorPlaneViewModel();
        public FirstFloorDrainagePlaneUI()
        {
            this.DataContext = firstFloorPlaneViewModel;
            InitializeComponent();
        }

        private void btnPipeLine_Click(object sender, RoutedEventArgs e)
        {
            ThPSPMParameter param = new ThPSPMParameter();
            param.paraSettingViewModel = ParameterSetUI.paramSetting;
            param.firstFloorPlaneViewModel = firstFloorPlaneViewModel;
            param.config = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
            ThWSSUIService.Instance.PSPMParameter = param;

            //聚焦到CAD
            SetFocusToDwgView();

            CommandHandlerBase.ExecuteFromCommandLine(false, "THPSLY");
        }

        private void btnParamSet_Click(object sender, RoutedEventArgs e)
        {
            ParameterSetUI parameterSetUI = new ParameterSetUI();
            parameterSetUI.ShowDialog();
        }

        private void btnDrawWall_Click(object sender, RoutedEventArgs e)
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (currentDb.Database.GetDocument().LockDocument())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThWSSCommon.OutFrameLayerName), false);
                currentDb.Database.SetCurrentLayer(ThWSSCommon.OutFrameLayerName);
                currentDb.Database.SetLayerColor(ThWSSCommon.OutFrameLayerName, 2);
                ThMEPWSS.Common.Utils.FocusToCAD();
            }

            ThDrawOutDoorCmd thDrawOutDoorCmd = new ThDrawOutDoorCmd(ThWSSCommon.OutFrameLayerName);
            thDrawOutDoorCmd.SubExecute();
        }

        private void btnRainPipe_Click(object sender, RoutedEventArgs e)
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (currentDb.Database.GetDocument().LockDocument())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThWSSCommon.OutdoorRainPipeLayerName), false);
                currentDb.Database.SetCurrentLayer(ThWSSCommon.OutdoorRainPipeLayerName);
                ThMEPWSS.Common.Utils.FocusToCAD();
            }

            DrawPline();
        }

        private void btnDrainagePipe_Click(object sender, RoutedEventArgs e)
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (currentDb.Database.GetDocument().LockDocument())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThWSSCommon.OutdoorSewagePipeLayerName), false);
                currentDb.Database.SetCurrentLayer(ThWSSCommon.OutdoorSewagePipeLayerName);
                ThMEPWSS.Common.Utils.FocusToCAD();
            }

            DrawPline();
        }

        private void DrawPline()
        {
            Active.Document.SendCommand("_Pline" + "\n");
        }

        /// <summary>
        /// 聚焦到CAD
        /// </summary>
        private void SetFocusToDwgView()
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
