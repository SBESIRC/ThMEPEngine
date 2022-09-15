using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using ThControlLibraryWPF.CustomControl;
using TianHua.Mep.UI.ViewModel;
using ThMEPEngineCore.Model.Common;
using System.Windows.Media.Imaging;
using System;

namespace TianHua.Mep.UI.UI
{
    /// <summary>
    /// uiRainSystem.xaml 的交互逻辑
    /// </summary>
    public partial class ExtractRoomOutlineUI : ThCustomWindow
    {
        private string DoorOpenImgUrl = @"/TianHua.Mep.UI;component/Resource/Image/打开.png";
        private string DoorCloseImgUrl = @"/TianHua.Mep.UI;component/Resource/Image/关闭.png";        

        private ThExtractRoomOutlineVM RoomOutlineVM { get; set; } 
        public ExtractRoomOutlineUI(ThExtractRoomOutlineVM roomOutlineVM)
        {
            InitializeComponent();            
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Topmost = true;
            this.RoomOutlineVM = roomOutlineVM;
            Refresh();
        }

        public void UpdateDataContext(ThExtractRoomOutlineVM newVm)
        {
            if (this.RoomOutlineVM == null)
            {
                this.RoomOutlineVM = newVm;
                Refresh();
            }
            else
            {
                if (newVm.Id != this.RoomOutlineVM.Id)
                {
                    this.RoomOutlineVM = newVm;
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            this.DataContext = null;
            this.DataContext = RoomOutlineVM;
            LoadShowDoorImage();
        }

        private void LoadShowDoorImage()
        {
            if (RoomOutlineVM.IsShowDoorOpenState)
            {
                imgShowDoorOutline.Source = CreateBitmapImage(DoorOpenImgUrl);
            }
            else
            {
                imgShowDoorOutline.Source = CreateBitmapImage(DoorCloseImgUrl);
            }
        }

        private BitmapImage CreateBitmapImage(string imgUrl)
        {
            //方式1，直接通过图片路径
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(imgUrl,UriKind.Relative);
            bmp.EndInit();

            //方式2，通过字节数组
            //FileStream fs = new FileStream(imgUrl, FileMode.Open);
            //byte[] buffer = new byte[fs.Length];
            //fs.Read(buffer, 0, buffer.Length);
            //fs.Close();
            //BitmapImage bmp = new BitmapImage();
            //bmp.BeginInit();
            //bmp.StreamSource = new MemoryStream(buffer);
            //bmp.EndInit();

            return bmp;
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


        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSelectLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.PickWallLayer();
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

        private void btnHelp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/43a25083da7b4db2a1789cc50b66d948.html");
        }

        private void btnMultiBuildRoomOutline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.BuildRoomOutline1();
        }

        private void btnSingleBuildRoomOutline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.BuildRoomOutline();
        }

        private void btnSaveConfig_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.SaveToDatabase();
        }

        private void imgPickDoorBlkName_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RoomOutlineVM.PickDoorBlock();
        }

        private void imgRemoveDoorBlkName_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var doorBlocks = new List<ThBlockInfo>();
            for (int i = 0; i < lbDoorBlkConfig.SelectedItems.Count; i++)
            {
                doorBlocks.Add(lbDoorBlkConfig.SelectedItems[i] as ThBlockInfo);
            }
            RoomOutlineVM.RemoveDoorBlocks(doorBlocks);
            this.lbDoorBlkConfig.ItemsSource = null;
            this.lbDoorBlkConfig.ItemsSource = RoomOutlineVM.DoorBlkInfos;
        }

        private void imgShowDoorOutline_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(RoomOutlineVM.IsShowDoorOpenState)
            {
                RoomOutlineVM.ShowDoorOutline();
            }    
            else
            {
                RoomOutlineVM.CloseDoorOutline();
            }
        }

        private void imgShowDoorOutline_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RoomOutlineVM.IsShowDoorOpenState = !RoomOutlineVM.IsShowDoorOpenState;
            LoadShowDoorImage();
        }
    }
}
