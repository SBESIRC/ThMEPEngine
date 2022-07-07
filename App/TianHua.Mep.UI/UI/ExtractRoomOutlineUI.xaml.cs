using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;
using TianHua.Mep.UI.ViewModel;
using ThMEPEngineCore.Model.Common;

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
            RoomOutlineVM.ExtractRoomDatas();
        }

        private void btnBuildDoors_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.BuildDoors();
        }

        private void btnBuildRoomOutline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.BuildRoomOutline();
        }

        private void btnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.Confirm();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSelectLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.SelectLayer();
        }

        private void btnDeleteLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < listBox.SelectedItems.Count; i++)
            {
                layers.Add((listBox.SelectedItems[i] as ThLayerInfo).Layer);
            }
            RoomOutlineVM.RemoveLayers(layers);
            this.listBox.ItemsSource = null;
            this.listBox.ItemsSource = RoomOutlineVM.LayerInfos;
        }
    }
}
