using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.ReleaseNs.RainSystemNs;
using static ThMEPWSS.Assistant.DrawUtils;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.MessageBox;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiRainSystem : ThCustomWindow
    {
        private RainSystemDiagramViewModel viewModel = new RainSystemDiagramViewModel();
        public uiRainSystem()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            this.Topmost = true;
            Loaded += (s, e) => { ThRainService.commandContext = new ThRainService.CommandContext() { ViewModel = viewModel, window = this }; };
            Closed += (s, e) => { ThRainService.commandContext = null; };
            //this.Title += " 最后更新：2021/7/26 15:43";

            hint.Visibility = Visibility.Visible;
            hint.Text = "2021/8/2 10:08";
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            var uiParams = new uiRainSystemParams(viewModel.Params);
            uiParams.Topmost = true;
            uiParams.ShowDialog();
        }

        //run command
        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide(); FocusMainWindow();
                RainDiagram.DrawRainDiagram(viewModel, true);
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

        //选择楼层
        private void ImageButton_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide(); FocusMainWindow();
                viewModel.InitFloorListDatas(false);
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
