﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
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
        private string DoorOpenImgUrl = @"/TianHua.Mep.UI;component/Resource/Image/打开.png";
        private string DoorCloseImgUrl = @"/TianHua.Mep.UI;component/Resource/Image/关闭.png";
        private Dictionary<string, bool> _controlNameEnableDict=new Dictionary<string, bool>();

        private bool _lbArchwallLayerConfig_InMouseSelectionMode = false;
        private List<ListBoxItem> _lbArchwallLayerConfig_SelectedItems = new List<ListBoxItem>();
        private bool _lbDoorBlkConfig_InMouseSelectionMode = false;
        private List<ListBoxItem> _lbDoorBlkConfig_SelectedItems = new List<ListBoxItem>();
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
            InitButtonNameEnableDict();
            UpdateControlEnable();
        }

        private void InitButtonNameEnableDict()
        {
            if (_controlNameEnableDict.Count == 0)
            {
                _controlNameEnableDict.Add(this.btnExtractDoor.Name, true);
                _controlNameEnableDict.Add(this.btnExtractWall.Name, true);
                _controlNameEnableDict.Add(this.btnSaveConfig.Name, true);
                _controlNameEnableDict.Add(this.btnMultiBuildRoomOutline.Name, true);
                _controlNameEnableDict.Add(this.btnSingleBuildRoomOutline.Name, true);
                _controlNameEnableDict.Add(this.btnSelectLayer.Name, true);
                _controlNameEnableDict.Add(this.btnDeleteLayer.Name, true);
                _controlNameEnableDict.Add(this.imgPickDoorBlkName.Name,true);
                _controlNameEnableDict.Add(this.imgRemoveDoorBlkName.Name, true);
                _controlNameEnableDict.Add(this.imgShowDoorOutline.Name, true);
            }
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
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            RoomOutlineVM.BuildDoors();
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
        }

        private void btnSelectLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            RoomOutlineVM.PickWallLayer();
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
        }

        private void btnDeleteLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < lbArchwallLayerConfig.SelectedItems.Count; i++)
            {
                layers.Add((lbArchwallLayerConfig.SelectedItems[i] as ThLayerInfo).Layer);
            }
            RoomOutlineVM.RemoveLayers(layers);
            this.lbArchwallLayerConfig.ItemsSource = null;
            this.lbArchwallLayerConfig.ItemsSource = RoomOutlineVM.LayerInfos;
        }

        private void btnHelp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/43a25083da7b4db2a1789cc50b66d948.html");
        }

        private void btnMultiBuildRoomOutline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            RoomOutlineVM.BuildRoomOutline1();
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
        }

        private void btnSingleBuildRoomOutline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            RoomOutlineVM.BuildRoomOutline();
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
        }

        private void btnSaveConfig_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RoomOutlineVM.SaveToDatabase();
        }

        private void imgPickDoorBlkName_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            RoomOutlineVM.PickDoorBlock();
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
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
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            if (RoomOutlineVM.IsShowDoorOpenState)
            {
                RoomOutlineVM.ShowDoorOutline();
            }    
            else
            {
                RoomOutlineVM.CloseDoorOutline();
            }
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
        }

        private void imgShowDoorOutline_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RoomOutlineVM.IsShowDoorOpenState = !RoomOutlineVM.IsShowDoorOpenState;
            LoadShowDoorImage();
        }

        private void SetControlNameEnable(string controlName,bool isEnable)
        {
            if(_controlNameEnableDict.ContainsKey(controlName))
            {
                _controlNameEnableDict[controlName] = isEnable;
            }
        }

        private void OpenOrCloseControlNameDict(bool isEnable)
        {
            _controlNameEnableDict.Keys
               .OfType<string>()
               .Select(o => o)
               .ToList()
               .ForEach(o => SetControlNameEnable(o, isEnable));
        }

        private void UpdateControlEnable()
        {
            foreach(var item in _controlNameEnableDict)
            {
                switch(item.Key)
                {
                    case "btnExtractWall":
                        this.btnExtractWall.IsEnabled = item.Value;
                        break;
                    case "btnExtractDoor":
                        this.btnExtractDoor.IsEnabled = item.Value;
                        break;
                    case "btnSaveConfig":
                        this.btnSaveConfig.IsEnabled = item.Value;
                        break;
                    case "btnMultiBuildRoomOutline":
                        this.btnMultiBuildRoomOutline.IsEnabled = item.Value;
                        break;
                    case "btnSingleBuildRoomOutline":
                        this.btnSingleBuildRoomOutline.IsEnabled = item.Value;
                        break;
                    case "btnHelp":
                        this.btnHelp.IsEnabled = item.Value;
                        break;
                    case "imgPickDoorBlkName":
                        this.imgPickDoorBlkName.IsEnabled = item.Value;
                        break;
                    case "btnSelectLayer":
                        this.btnSelectLayer.IsEnabled = item.Value;
                        break;
                    case "btnDeleteLayer":
                        this.btnDeleteLayer.IsEnabled = item.Value;
                        break;
                    case "imgRemoveDoorBlkName":
                        this.imgRemoveDoorBlkName.IsEnabled = item.Value;
                        break;
                    case "imgShowDoorOutline":
                        this.imgShowDoorOutline.IsEnabled = item.Value;
                        break;
                }
            }
        }

        private void lbArchwallLayerConfig_lbItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // MouseDown时清空已选Item
            // 同时开始"inMouseSelectionMode"
            foreach (var item in _lbArchwallLayerConfig_SelectedItems)
            {
                item.ClearValue(ListBoxItem.BackgroundProperty);
                item.ClearValue(TextElement.ForegroundProperty);
            }
            _lbArchwallLayerConfig_SelectedItems.Clear();
            _lbArchwallLayerConfig_InMouseSelectionMode = true;
        }

        private void lbArchwallLayerConfig_lbItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // MouseUp时停止"inMouseSelectionMode"
            ListBoxItem mouseUpItem = sender as ListBoxItem;
            _lbArchwallLayerConfig_InMouseSelectionMode = false;
        }

        private void lbArchwallLayerConfig_lbItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ListBoxItem mouseOverItem = sender as ListBoxItem;
            if (mouseOverItem != null && _lbArchwallLayerConfig_InMouseSelectionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                // Mouse所在的Item设置高亮
                mouseOverItem.Background = SystemColors.HighlightBrush;
                mouseOverItem.SetValue(TextElement.ForegroundProperty, SystemColors.HighlightTextBrush);
                if (!_lbArchwallLayerConfig_SelectedItems.Contains(mouseOverItem)) { _lbArchwallLayerConfig_SelectedItems.Add(mouseOverItem); }
            }
        }

        private void lbDoorBlkConfig_lbItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // MouseDown时清空已选Item
            // 同时开始"inMouseSelectionMode"
            foreach (var item in _lbDoorBlkConfig_SelectedItems)
            {
                item.ClearValue(ListBoxItem.BackgroundProperty);
                item.ClearValue(TextElement.ForegroundProperty);
            }
            _lbDoorBlkConfig_SelectedItems.Clear();
            _lbDoorBlkConfig_InMouseSelectionMode = true;
        }

        private void lbDoorBlkConfig_lbItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // MouseUp时停止"inMouseSelectionMode"
            ListBoxItem mouseUpItem = sender as ListBoxItem;
            _lbDoorBlkConfig_InMouseSelectionMode = false;
        }

        private void lbDoorBlkConfig_lbItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ListBoxItem mouseOverItem = sender as ListBoxItem;
            if (mouseOverItem != null && _lbDoorBlkConfig_InMouseSelectionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                // Mouse所在的Item设置高亮
                mouseOverItem.Background = SystemColors.HighlightBrush;
                mouseOverItem.SetValue(TextElement.ForegroundProperty, SystemColors.HighlightTextBrush);
                if (!_lbDoorBlkConfig_SelectedItems.Contains(mouseOverItem)) { _lbDoorBlkConfig_SelectedItems.Add(mouseOverItem); }
            }
        }
    }
}
