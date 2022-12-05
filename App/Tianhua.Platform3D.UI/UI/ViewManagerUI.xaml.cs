using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF.CustomControl;
using ThPlatform3D.Model;
using Tianhua.Platform3D.UI.ViewModels;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class ViewManagerUI : ThCustomWindow
    {
        private ViewManagerVM _vm;
        private static ViewDetailUI _viewDetailUI;
        public ViewManagerUI(ViewManagerVM vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm;
            this.Topmost = true; 
        }

        public void UpdateDataContext(ViewManagerVM vm)
        {
            if(this._vm!=null && vm.Id==this._vm.Id)
            {
                return;
            }
            else
            {
                this.DataContext = null;
                this._vm = vm;
                this.DataContext = vm;
            }
        }

        private void imgAddView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_vm.PrjectFiles.Count == 1)
            {
                // 一个文档能属于一个项目
                this.Hide();
                var vm = new ViewDetailVM();
                vm.SetStoreyInfoMap(_vm.StoreyInfoMap);
                ShowViewDetailUI(vm);
                _viewDetailUI.Closing += _viewDetailUI_Closing;
            }
            else
            {
                // 当前打开的Dwg若不属于任何项目或属于多个项目
                // 暂时不支持
                if(_vm.PrjectFiles.Count==0)
                {
                    MessageBox.Show("当前打开的文件不属于任何项目！","信息提示");
                }
                else
                {
                    MessageBox.Show("当前打开的文件属于多个项目！", "信息提示");
                }
            }
        }

        private void _viewDetailUI_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewDetailUI.IsGoOn)
            {
                _vm.Insert(_viewDetailUI.VM.ViewDetailInfo);
            }
            _viewDetailUI.Closing -= _viewDetailUI_Closing;
            this.Show();
        }

        private void imgEraseView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.dgViewManager.SelectedItems.Count > 0)
            {
                if (_vm.PrjectFiles.Count == 1)
                {
                    // 一个文档能属于一个项目
                    var eraseViewInfoLst = new List<ThViewDetailInfo>();
                    foreach(ThViewDetailInfo viewInfo in this.dgViewManager.SelectedItems)
                    {
                        eraseViewInfoLst.Add(viewInfo);
                    }
                    _vm.Delete(eraseViewInfoLst);
                }
                else
                {
                    // 当前打开的Dwg若不属于任何项目或属于多个项目
                    // 暂时不支持
                    if (_vm.PrjectFiles.Count == 0)
                    {
                        MessageBox.Show("当前打开的文件不属于任何项目！", "信息提示");
                    }
                    else
                    {
                        MessageBox.Show("当前打开的文件属于多个项目！", "信息提示");
                    }
                }
            }
        }

       
        private void imgEditView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(this.dgViewManager.SelectedItems.Count==1)
            {
                if (_vm.PrjectFiles.Count == 1)
                {
                    // 一个文档能属于一个项目
                    this.Hide();
                    var detailInfo = this.dgViewManager.SelectedItems[0] as ThViewDetailInfo;
                    var vm = new ViewDetailVM(detailInfo);
                    vm.SetStoreyInfoMap(_vm.StoreyInfoMap);
                    ShowViewDetailUI(vm);
                    _viewDetailUI.Closing += _viewDetailUI_Closing1;
                }
                else
                {
                    // 当前打开的Dwg若不属于任何项目或属于多个项目
                    // 暂时不支持
                    if (_vm.PrjectFiles.Count == 0)
                    {
                        MessageBox.Show("当前打开的文件不属于任何项目！", "信息提示");
                    }
                    else
                    {
                        MessageBox.Show("当前打开的文件属于多个项目！", "信息提示");
                    }
                }
            }
        }

        private void _viewDetailUI_Closing1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewDetailUI.IsGoOn)
            {
                _vm.Update(_viewDetailUI.VM.ViewDetailInfo);
            }
            _viewDetailUI.Closing -= _viewDetailUI_Closing1;
            this.Show();
        }

        private void ShowViewDetailUI(ViewDetailVM vm)
        {
            if (vm == null)
            {
                return;
            }
            if(_viewDetailUI==null)
            {
                _viewDetailUI = new ViewDetailUI(vm);
                acadApp.ShowModelessWindow(_viewDetailUI);
            }
            else
            {
                if (_viewDetailUI != null && _viewDetailUI.IsLoaded)
                {
                    _viewDetailUI.Show();
                    return;
                }
                else
                {
                    _viewDetailUI.UpdateDataContext(vm);
                    _viewDetailUI.Show();
                }
            }
        }

        private void btnSelectAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            for(int i=0;i<_vm.ViewDetailInfos.Count;i++)
            {
                DataGridRow row = this.dgViewManager.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if(row==null)
                {
                    continue;
                }
                else
                {
                    row.IsSelected = true;
                }
            }
        }

        private void btnCancelSelectAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int i = 0; i < _vm.ViewDetailInfos.Count; i++)
            {
                DataGridRow row = this.dgViewManager.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row == null)
                {
                    continue;
                }
                else
                {
                    row.IsSelected = false;
                }
            }
        }

        private void btnInsertSelectedView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //

        }

        private void cbBuildingNo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateViewDetailInfos();
        }

        private void cbViewType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateViewDetailInfos();
        }

        private void cbViewScale_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _vm.UpdateViewDetailInfos();
        }

        private void ThCustomWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //
        }
    }
}
