using AcHelper;
using Linq2Acad;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Command;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiDrainageSystemSet.xaml 的交互逻辑
    /// </summary>
    public partial class uiUNDPDrainageSystemInfoCheck : ThCustomWindow
    {
        public static PressureDrainageSystemDiagramVieModel viewmodel;
        public uiUNDPDrainageSystemInfoCheck()
        {
            InitializeComponent();
            if (null == viewmodel)
                viewmodel = new PressureDrainageSystemDiagramVieModel();          
            this.DataContext = viewmodel;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        private void Cancle_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {         
            this.DialogResult = true;
            this.Close();
            var extent = viewmodel.SelectRegionForInfoTable();
            viewmodel.InfoRegion = extent;
            viewmodel.HasInfoTablesRoRead = true;
        }
    }
}
