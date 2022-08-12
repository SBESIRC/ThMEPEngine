using System.Diagnostics;
using System.Windows;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.HydrantConnectPipe.Command;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiHydrantConnectPipe.xaml 的交互逻辑
    /// </summary>
    public partial class UiHydrantConnectPipe : ThCustomWindow
    {
        private static HydrantConnectPipeViewModel ViewModel = null;
        public UiHydrantConnectPipe()
        {
            InitializeComponent();
            if (ViewModel == null)
            {
                ViewModel = new HydrantConnectPipeViewModel();
            }

            DataContext = ViewModel;
        }

        private void btnConnectPipe_Click(object sender, RoutedEventArgs e)
        {
            var cmd = new ThHydrantConnectPipeCmd(ViewModel.GetConfigInfo())
            {
                CommandName = "THDXXHS",
                ActionName = "连管",
            };
            cmd.Execute();
        }

        private void btnMarkLoop_Click(object sender, RoutedEventArgs e)
        {
            FireHydrantSystemViewModel.InsertLoopMark();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/693b4adf25cc42e5b64d0a4c89507bf5.html");
        }
    }
}
