using ThControlLibraryWPF.CustomControl;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class ExtractRoomOutlineUI : ThCustomWindow
    {
        private ThExtractRoomOutlineVM RoomOutlineVM { get; set; } = new ThExtractRoomOutlineVM();
        public ExtractRoomOutlineUI()
        {
            InitializeComponent();
            this.DataContext = RoomOutlineVM;
            this.Topmost = true;
        }

        private void ThCustomWindow_Closed(object sender, System.EventArgs e)
        {
        }

        private void btnExtractWall_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.ExtractWalls();
        }

        private void btnBuildRoomOutline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.BuildRoomOutline();
        }

        private void btnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.Confirm();
            this.Close();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSelectLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.SelectLayer();
        }
    }
}
