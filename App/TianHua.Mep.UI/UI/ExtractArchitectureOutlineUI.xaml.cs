using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using ThControlLibraryWPF.CustomControl;
using TianHua.Mep.UI.ViewModel;
using ThMEPEngineCore.Model.Common;

namespace TianHua.Mep.UI.UI
{
    public partial class ExtractArchitectureOutlineUI : ThCustomWindow
    {       
        private Dictionary<string, bool> _controlNameEnableDict=new Dictionary<string, bool>();
        private bool _lbArchwallLayerConfig_InMouseSelectionMode = false;
        private List<ListBoxItem> _lbArchwallLayerConfig_SelectedItems = new List<ListBoxItem>();
        private ThExtractArchitectureOutlineVM _architectureOutlineVM; 
        public ExtractArchitectureOutlineUI(ThExtractArchitectureOutlineVM roomOutlineVM)
        {
            InitializeComponent();            
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Topmost = true;
            this._architectureOutlineVM = roomOutlineVM;
            Refresh();
        }

        public void UpdateDataContext(ThExtractArchitectureOutlineVM newVm)
        {
            if (this._architectureOutlineVM == null)
            {
                this._architectureOutlineVM = newVm;
                Refresh();
            }
            else
            {
                if (newVm.Id != this._architectureOutlineVM.Id)
                {
                    this._architectureOutlineVM = newVm;
                    Refresh();
                }
            }
        }

        private void Refresh()
        {
            this.DataContext = null;
            this.DataContext = _architectureOutlineVM;
            InitButtonNameEnableDict();
            UpdateControlEnable();
        }

        private void InitButtonNameEnableDict()
        {
            if (_controlNameEnableDict.Count == 0)
            {
                _controlNameEnableDict.Add(this.btnExtractWall.Name, true);
                _controlNameEnableDict.Add(this.btnSaveConfig.Name, true);
                _controlNameEnableDict.Add(this.btnBuildArchitectureOutline.Name, true);
                _controlNameEnableDict.Add(this.btnSelectLayer.Name, true);
                _controlNameEnableDict.Add(this.btnDeleteLayer.Name, true);               
            }
        }

        private void ThCustomWindow_Closed(object sender, System.EventArgs e)
        {
        }

        private void btnExtractWall_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _architectureOutlineVM.ExtractDatas();
        }

        private void btnSelectLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            _architectureOutlineVM.PickWallLayer();
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
            _architectureOutlineVM.RemoveLayers(layers);
            this.lbArchwallLayerConfig.ItemsSource = null;
            this.lbArchwallLayerConfig.ItemsSource = _architectureOutlineVM.LayerInfos;
        }

        private void btnHelp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("http://thlearning.thape.com.cn/kng/view/video/43a25083da7b4db2a1789cc50b66d948.html");
        }

        private void btnSaveConfig_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _architectureOutlineVM.SaveToDatabase();
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
                    case "btnSaveConfig":
                        this.btnSaveConfig.IsEnabled = item.Value;
                        break;
                    case "btnBuildArchitectureOutline":
                        this.btnBuildArchitectureOutline.IsEnabled = item.Value;
                        break;
                    case "btnHelp":
                        this.btnHelp.IsEnabled = item.Value;
                        break;
                    case "btnSelectLayer":
                        this.btnSelectLayer.IsEnabled = item.Value;
                        break;
                    case "btnDeleteLayer":
                        this.btnDeleteLayer.IsEnabled = item.Value;
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

        private void btnBuildArchitectureOutline_Click(object sender, RoutedEventArgs e)
        {
            OpenOrCloseControlNameDict(false);
            UpdateControlEnable();
            _architectureOutlineVM.BuildArchitectureOutline();
            OpenOrCloseControlNameDict(true);
            UpdateControlEnable();
        }
    }
}
