using ThMEPWSS.ViewModel;
using ThControlLibraryWPF.CustomControl;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class RoomOutlineUI : ThCustomWindow
    {
        public ThRoomOutlineVM RoomOutlineVM { get; private set; } = new ThRoomOutlineVM();
        public RoomOutlineUI()
        {
            InitializeComponent();
            this.DataContext = RoomOutlineVM;
            this.Topmost = true;
        }

        private void ThCustomWindow_Closed(object sender, System.EventArgs e)
        {
            RoomOutlineVM.ResetCurrentLayer();
        }
    }
}
