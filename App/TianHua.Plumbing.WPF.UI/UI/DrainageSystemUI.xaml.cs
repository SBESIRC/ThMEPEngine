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
        private DrainageSystemDiagramViewModel viewModel = new DrainageSystemDiagramViewModel();
        public DrainageSystemUI()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            Loaded += (s, e) => { ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext =new ThMEPWSS.ReleaseNs.DrainageSystemNs.CommandContext() { ViewModel = viewModel, window = this }; };
            Closed += (s, e) => { ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext = null; };
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new DrainageSystemParamsUI(viewModel.Params);
            uiParams.ShowDialog();

        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.DrawDrainageSystemDiagram(viewModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                viewModel.CollectFloorListDatas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
