using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using ThControlLibraryWPF.CustomControl;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TianHua.Electrical.UI.LightningProtectLeadWire
{
    /// <summary>
    /// UIThFireAlarmNew.xaml 的交互逻辑
    /// </summary>
    public partial class LightningProtectLeadWireUI : ThCustomWindow
    {
        private ThLightningProtectLeadWireVM _vm = null;
        private bool _lbStoreys_InMouseSelectionMode = false;
        private List<ListBoxItem> _lbStoreys_SelectedItems = new List<ListBoxItem>();
        public LightningProtectLeadWireUI(ThLightningProtectLeadWireVM vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = vm;
            this.Topmost = true;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public void UpdateDataContext(ThLightningProtectLeadWireVM newVm)
        {
            if (this._vm == null)
            {
                this._vm = newVm;
                Refresh();
            }
            else
            {
                if (newVm.Id != this._vm.Id)
                {
                    this._vm = newVm;
                    Refresh();
                }
            }
        }
        private void Refresh()
        {
            this.DataContext = null;
            this.DataContext = this._vm;
        }


        private void lbStoreys_lbItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // MouseDown时清空已选Item
            // 同时开始"inMouseSelectionMode"
            foreach (var item in _lbStoreys_SelectedItems)
            {
                item.ClearValue(ListBoxItem.BackgroundProperty);
                item.ClearValue(TextElement.ForegroundProperty);
            }
            _lbStoreys_SelectedItems.Clear();
            _lbStoreys_InMouseSelectionMode = true;
        }

        private void lbStoreys_lbItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // MouseUp时停止"inMouseSelectionMode"
            ListBoxItem mouseUpItem = sender as ListBoxItem;
            _lbStoreys_InMouseSelectionMode = false;
        }

        private void lbStoreys_lbItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            ListBoxItem mouseOverItem = sender as ListBoxItem;
            if (mouseOverItem != null && _lbStoreys_InMouseSelectionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                // Mouse所在的Item设置高亮
                mouseOverItem.Background = SystemColors.HighlightBrush;
                mouseOverItem.SetValue(TextElement.ForegroundProperty, SystemColors.HighlightTextBrush);
                if (!_lbStoreys_SelectedItems.Contains(mouseOverItem)) { _lbStoreys_SelectedItems.Add(mouseOverItem); }
            }
        }
    }
}
