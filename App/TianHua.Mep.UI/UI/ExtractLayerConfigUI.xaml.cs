using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using ThMEPEngineCore.Model.Common;
using TianHua.Mep.UI.ViewModel;

namespace TianHua.Mep.UI.UI
{
    public partial class ExtractLayerConfigUI : ThCustomWindow
    {
        private ThExtractLayerConfigVM ExtractLayerConfigVM { get; set; }
        private bool _list1_InMouseSelectionMode = false;
        private List<ListBoxItem> _list1_SelectedItems = new List<ListBoxItem>();
        private bool _list2_InMouseSelectionMode = false;
        private List<ListBoxItem> _list2_SelectedItems = new List<ListBoxItem>();
        public ExtractLayerConfigUI()
        {
            ExtractLayerConfigVM= new ThExtractLayerConfigVM();
            InitializeComponent();
            this.DataContext = ExtractLayerConfigVM;
        }

        private void rbLayer_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.btnAddBeamLayer.IsEnabled = false;
            //this.btnDelBeamLayer.IsEnabled = false;
            //this.listBox1.IsEnabled = false;
        }
        private void rbDB_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.btnAddBeamLayer.IsEnabled = false;
            //this.btnDelBeamLayer.IsEnabled = false;
            //this.listBox1.IsEnabled = false;
        }

        private void btnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractLayerConfigVM.Save();
            this.Close();
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void rbBeamArea_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.btnAddBeamLayer.IsEnabled = true;
            //this.btnDelBeamLayer.IsEnabled = true;
            //this.listBox1.IsEnabled = true;
        }
        private void btnAddBeamLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractLayerConfigVM.PickBeamLayer();
        }

        private void btnDelBeamLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < listBox1.SelectedItems.Count; i++)
            {
                layers.Add((listBox1.SelectedItems[i] as ThLayerInfo).Layer);
            }
            ExtractLayerConfigVM.RemoveBeamLayers(layers);
            this.listBox1.ItemsSource = null;
            this.listBox1.ItemsSource = ExtractLayerConfigVM.BeamLayerInfos;
        }

        private void btnAddShearWallLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtractLayerConfigVM.PickShearWallLayer();
        }

        private void btnDelShearWallLayer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var layers = new List<string>();
            for (int i = 0; i < listBox2.SelectedItems.Count; i++)
            {
                layers.Add((listBox2.SelectedItems[i] as ThLayerInfo).Layer);
            }
            ExtractLayerConfigVM.RemoveShearWallLayers(layers);
            this.listBox2.ItemsSource = null;
            this.listBox2.ItemsSource = ExtractLayerConfigVM.ShearWallLayerInfos;
        }

        private void rbDefault_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.btnAddBeamLayer.IsEnabled = false;
            //this.btnDelBeamLayer.IsEnabled = false;
            //this.listBox2.IsEnabled = false;
        }

        private void rbLayerConfig_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.btnAddBeamLayer.IsEnabled = true;
            //this.btnDelBeamLayer.IsEnabled = true;
            //this.listBox2.IsEnabled = true;
        }
        private void listBox1_lbItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // MouseDown时清空已选Item
            // 同时开始"inMouseSelectionMode"
            foreach (var item in _list1_SelectedItems)
            {
                item.ClearValue(ListBoxItem.BackgroundProperty);
                item.ClearValue(TextElement.ForegroundProperty);
            }
            _list1_SelectedItems.Clear();
            _list1_InMouseSelectionMode = true;
        }

        private void listBox1_lbItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // MouseUp时停止"inMouseSelectionMode"
            ListBoxItem mouseUpItem = sender as ListBoxItem;
            _list1_InMouseSelectionMode = false;
        }

        private void listBox1_lbItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ListBoxItem mouseOverItem = sender as ListBoxItem;
            if (mouseOverItem != null && _list1_InMouseSelectionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                // Mouse所在的Item设置高亮
                mouseOverItem.Background = SystemColors.HighlightBrush;
                mouseOverItem.SetValue(TextElement.ForegroundProperty, SystemColors.HighlightTextBrush);
                if (!_list1_SelectedItems.Contains(mouseOverItem)) { _list1_SelectedItems.Add(mouseOverItem); }
            }
        }

        private void listBox2_lbItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // MouseDown时清空已选Item
            // 同时开始"inMouseSelectionMode"
            foreach (var item in _list2_SelectedItems)
            {
                item.ClearValue(ListBoxItem.BackgroundProperty);
                item.ClearValue(TextElement.ForegroundProperty);
            }
            _list2_SelectedItems.Clear();
            _list2_InMouseSelectionMode = true;
        }

        private void listBox2_lbItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // MouseUp时停止"inMouseSelectionMode"
            ListBoxItem mouseUpItem = sender as ListBoxItem;
            _list2_InMouseSelectionMode = false;
        }

        private void listBox2_lbItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ListBoxItem mouseOverItem = sender as ListBoxItem;
            if (mouseOverItem != null && _list2_InMouseSelectionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                // Mouse所在的Item设置高亮
                mouseOverItem.Background = SystemColors.HighlightBrush;
                mouseOverItem.SetValue(TextElement.ForegroundProperty, SystemColors.HighlightTextBrush);
                if (!_list2_SelectedItems.Contains(mouseOverItem)) { _list2_SelectedItems.Add(mouseOverItem); }
            }
        }
    }
}
