using System;
using System.Windows;
using System.Windows.Forms;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using static ThMEPWSS.Assistant.DrawUtils;
using MessageBox = System.Windows.MessageBox;

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
            this.Topmost = true;
            Loaded += (s, e) => { ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext =new ThMEPWSS.ReleaseNs.DrainageSystemNs.CommandContext() { ViewModel = viewModel, window = this }; };
            Closed += (s, e) => { ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.commandContext = null; };
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new DrainageSystemParamsUI(viewModel.Params);
            uiParams.Topmost = true;
            uiParams.ShowDialog();
        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide(); FocusMainWindow();
                ThMEPWSS.ReleaseNs.DrainageSystemNs.DrainageSystemDiagram.DrawDrainageSystemDiagram(viewModel,false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Show();
            }
        }
        //选择楼层
        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide(); FocusMainWindow();
                viewModel.CollectFloorListDatas(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Show();
            }
        }
        //这明明是“新建楼层图框”
        private void btnSelectFloor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide(); FocusMainWindow();
                ThMEPWSS.Common.Utils.CreateFloorFraming(false);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Show();
            }
        }
    }
}
