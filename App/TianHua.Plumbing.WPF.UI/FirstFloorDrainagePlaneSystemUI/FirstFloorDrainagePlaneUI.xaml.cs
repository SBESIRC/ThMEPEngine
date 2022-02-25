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
using TianHua.Plumbing.WPF.UI.UI;

namespace TianHua.Plumbing.WPF.UI.FirstFloorDrainagePlaneSystemUI
{
    /// <summary>
    /// FirstFloorDrainagePlaneUI.xaml 的交互逻辑
    /// </summary>
    public partial class FirstFloorDrainagePlaneUI : ThCustomWindow
    {
        public FirstFloorDrainagePlaneUI()
        {
            InitializeComponent();
        }

        private void btnPipeLine_Click(object sender, RoutedEventArgs e)
        {
            var config = uiBlockNameConfig.staticUIBlockName.GetBlockNameList();
            ThFirstFloorDrainageCmd drainageCmd = new ThFirstFloorDrainageCmd(config);
            drainageCmd.Execute();
        }

        private void btnSltFloor_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnDrawWall_Click(object sender, RoutedEventArgs e)
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (currentDb.Database.GetDocument().LockDocument())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThWSSCommon.OutFrameLayerName), false);
                currentDb.Database.SetCurrentLayer(ThWSSCommon.OutFrameLayerName);
                ThMEPWSS.Common.Utils.FocusToCAD();
            }
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
        }
    }
}
