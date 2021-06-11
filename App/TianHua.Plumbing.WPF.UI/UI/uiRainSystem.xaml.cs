using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiRainSystem : ThCustomWindow
    {
        private RainSystemDiagramViewModel ViewModel = new RainSystemDiagramViewModel();
        public uiRainSystem()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
            Loaded += (s, e) => { ThMEPWSS.Pipe.Service.ThRainSystemService.commandContext = new ThMEPWSS.Pipe.Service.ThRainSystemService.CommandContext() { rainSystemDiagramViewModel = ViewModel, window = this }; };
            Closed += (s, e) => { ThMEPWSS.Pipe.Service.ThRainSystemService.commandContext = null; };
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new uiRainSystemParams(ViewModel.Params);
            uiParams.ShowDialog();

        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cmd = new ThRainSystemDiagramCmd(ViewModel))
            {
                cmd.Execute();
            }
        }

        //Init storeys
        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.InitFloorListDatas();
        }
        //这明明是“新建楼层图框”
        private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThMEPWSS.Common.Utils.CreateFloorFraming();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
