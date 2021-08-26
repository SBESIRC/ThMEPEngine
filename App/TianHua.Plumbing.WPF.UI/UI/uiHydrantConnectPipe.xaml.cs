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
            var cmd = new ThHydrantConnectPipeCmd(ViewModel.GetConfigInfo());
            cmd.Execute();
        }

        private void btnMarkLoop_Click(object sender, RoutedEventArgs e)
        {
            FireHydrantSystemViewModel.InsertLoopMark();
        }
    }
}
