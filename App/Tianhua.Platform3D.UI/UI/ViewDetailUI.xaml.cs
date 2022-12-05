using System.Windows;
using ThControlLibraryWPF.CustomControl;
using Tianhua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class ViewDetailUI : ThCustomWindow
    {
        private ViewDetailVM _vm;
        private bool _isGoOn = false;
        public bool IsGoOn => _isGoOn;
        public ViewDetailVM VM => _vm;
        public ViewDetailUI(ViewDetailVM vm)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this._vm = vm;
            this.DataContext = this._vm;
            this.Topmost = true;
        }

        public void UpdateDataContext(ViewDetailVM vm)
        {
            if(_vm!=null && _vm.Id==vm.Id)
            {
                return;
            }
            this._vm = vm;
            this.DataContext=null;           
        }

        private void btnFloorSectionSelect_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            _vm.SelectFloorSection();
            this.Show();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            // 数据验证
            if(_vm.ValideData())
            {
                _isGoOn = true;
                this.Close();
            }
            else
            {
                // 提示数据无效，请重新编辑
            }
        }

        private void imgQuickLocate_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Hide();
            _vm.QuickLocate();
            this.Show();
        }

        private void imgCreateSection_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Hide();
            _vm.CreateSection();
            this.Show();
        }

        private void cbViewType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_vm.ViewDetailInfo.ViewType == "平面图")
            {
                this.viewZoneGrid.Visibility = Visibility.Visible;
                this.outDoorFloorGrid.Visibility = Visibility.Collapsed;
                this.viewSectionDirectionSP1.Visibility = Visibility.Collapsed;
                this.viewSectionDirectionSP2.Visibility = Visibility.Collapsed;
                this.elevationViewSectionDirectionSP.Visibility = Visibility.Collapsed;
            }
            else if(_vm.ViewDetailInfo.ViewType == "剖面图")
            {                
                this.outDoorFloorGrid.Visibility = Visibility.Visible;
                this.viewSectionDirectionSP1.Visibility = Visibility.Visible;
                this.viewSectionDirectionSP2.Visibility = Visibility.Visible;
                this.viewZoneGrid.Visibility = Visibility.Collapsed;                
                this.elevationViewSectionDirectionSP.Visibility = Visibility.Collapsed;
            }
            else if(_vm.ViewDetailInfo.ViewType == "立面图")
            {
                this.outDoorFloorGrid.Visibility = Visibility.Visible;
                this.elevationViewSectionDirectionSP.Visibility = Visibility.Visible;
                this.viewZoneGrid.Visibility = Visibility.Collapsed;
                this.viewSectionDirectionSP1.Visibility = Visibility.Collapsed;
                this.viewSectionDirectionSP2.Visibility = Visibility.Collapsed;
            }
            _vm.UpdateViewName();
        }

        private void cbBuildingNo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateFloors();
            _vm.UpdateViewName();
        }

        private void cbFloor_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateViewName();
        }

        private void cbViewScale_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateViewName();
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
    }
}
