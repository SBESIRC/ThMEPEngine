using System;
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
    public partial class DrainageSystemUI : ThCustomWindow
    {
        private DrainageSystemDiagramViewModel ViewModel = new DrainageSystemDiagramViewModel();
        public DrainageSystemUI()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
            Loaded += (s, e) => { ThMEPWSS.Pipe.Service.ThDrainageService.commandContext = new ThMEPWSS.Pipe.Service.ThDrainageService.CommandContext() { ViewModel = ViewModel, window = this }; };
            Closed += (s, e) => { ThMEPWSS.Pipe.Service.ThDrainageService.commandContext = null; };
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new DrainageSystemParamsUI(ViewModel.Params);
            uiParams.ShowDialog();

        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThMEPWSS.Pipe.Service.DrainageSystemDiagram.DrawDrainageSystemDiagram(ViewModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.CollectFloorListDatas();
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
