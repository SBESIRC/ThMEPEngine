using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// StoreyTableEntrance.xaml 的交互逻辑
    /// </summary>
    public partial class StoreyElevationSetUI : UserControl
    {
        private StoreyElevationSetVM _vm;
        private string _copySourceTabName = "";
        public StoreyElevationSetUI()
        {
            InitializeComponent();
            _vm = new StoreyElevationSetVM();
            this.DataContext = _vm;
            LoadTabcontrolItems();            
        }

        private void LoadTabcontrolItems()
        {
            if(_vm.BuildingNames.Count>0)
            {
                _vm.BuildingNames.ForEach(o => AddTabItem(o));
                SetTabControlFocus(_vm.BuildingNames.First());
            }
        }

        private void btnAddBuilding_Click(object sender, RoutedEventArgs e)
        {
            var inputVM = new TextInputVM(_vm.BuildingNames)
            {
                InputTip = "请输入新增的单体名称",
                InputValue = "",
            };
            var inputUI = new TextInputUI(inputVM);
            inputUI.ShowDialog();
            if(inputUI.IsSuccess)
            {
                _vm.AddNewBuilding(inputVM.InputValue);
                AddTabItem(inputVM.InputValue);
                ClearCopySourceTabName();
                _vm.Save(); // 保存数据
            }
        }

        private void AddTabItem(string head)
        {
            var tabItem = new TabItem()
            {
                Header = head,
            };
            var dataSource = _vm.GetBuildingStoreys(head);
            tabItem.Content = new BuildingStoreyTableUI(dataSource);
            this.tabControl1.Items.Add(tabItem);
            tabItem.Focus();
        }

        private void btnModifyBuilding_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnDeleteBuilding_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void copyItem_Click(object sender, RoutedEventArgs e)
        {
            if(this.tabControl1.SelectedItem==null)
            {
                MessageBox.Show("请选择要复制的Tab项","信息提示",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            _copySourceTabName = (this.tabControl1.SelectedItem as TabItem).Header.ToString();            
        }

        private void pasteItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_copySourceTabName))
            {
                MessageBox.Show("请选择要复制的源Tab项！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this.tabControl1.SelectedItem == null)
            {
                MessageBox.Show("请选择要粘贴的目标Tab项！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var targetTab = this.tabControl1.SelectedItem as TabItem;
            var copyTargetTabName = targetTab.Header.ToString();
            if (_copySourceTabName == copyTargetTabName)
            {
                MessageBox.Show("复制和粘贴的Tab项不能相同！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var tipRes = MessageBox.Show("确定把 "+ _copySourceTabName+" 中的楼层数据复制到 "+ copyTargetTabName+" 中吗？",
                "复制提示",MessageBoxButton.OKCancel,MessageBoxImage.Warning);
            if(tipRes == MessageBoxResult.OK)
            {
                _vm.CopyBuildingStoreys(_copySourceTabName, copyTargetTabName);
                var buildingTbleUI = targetTab.Content as BuildingStoreyTableUI;
                buildingTbleUI.datagrid1.ItemsSource = null;
                buildingTbleUI.datagrid1.ItemsSource = _vm.GetBuildingStoreys(copyTargetTabName);
                _vm.Save(); // 保存数据
            }            
        }

        private void editStoreyItem_Click(object sender, RoutedEventArgs e)
        {
            EditStoreyItem();
        }
        private void tabControl1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditStoreyItem();
        }
        private void EditStoreyItem()
        {
            if (this.tabControl1.SelectedItem == null)
            {
                return;
            }
            var tabItem = this.tabControl1.SelectedItem as TabItem;
            var buildStoryes = _vm.GetBuildingStoreys(tabItem.Header.ToString());
            // 弹出EditStoreyUI
            var editStoreyVM = new EditStoreyVM(buildStoryes);
            var editStoreyUI = new EditStoreyUI(editStoreyVM);            
            editStoreyUI.ShowDialog();
            _vm.UpdateBuildingStoryes(tabItem.Header.ToString(), editStoreyVM.Source);


            var buildingTbleUI = tabItem.Content as BuildingStoreyTableUI;
            buildingTbleUI.datagrid1.ItemsSource = null;
            buildingTbleUI.datagrid1.ItemsSource = editStoreyVM.Source;

            // 保存数据
            _vm.Save();

            // 清空
            ClearCopySourceTabName();
        }

        private void ClearCopySourceTabName()
        {
            _copySourceTabName = "";
        }

        private void SetTabControlFocus(string head)
        {
            foreach(TabItem item in this.tabControl1.Items)
            {
                if(item.Header.ToString() == head)
                {
                    item.Focus();
                }
            }
        }

        private void deleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.tabControl1.SelectedItem == null)
            {
                return;
            }
            var tabItem = this.tabControl1.SelectedItem as TabItem;
            var tipRes = MessageBox.Show("确定要删除 [" + tabItem.Header.ToString() + "] 楼层数据吗？", "删除提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (tipRes == MessageBoxResult.OK)
            {
                this.tabControl1.Items.Remove(tabItem);
                _vm.DeleteBuilding(tabItem.Header.ToString());
                ClearCopySourceTabName();
                _vm.Save(); // 保存数据
            }
        }

        private void modifyItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.tabControl1.SelectedItem == null)
            {
                return;
            }
            var tabItem = this.tabControl1.SelectedItem as TabItem;
            var tabName = tabItem.Header.ToString();
            var inputVM = new TextInputVM(_vm.BuildingNames)
            {
                InputTip = "请输入修改的单体名称",
                InputValue = tabName,
            };
            var inputUI = new TextInputUI(inputVM);
            inputUI.ShowDialog();
            if (inputUI.IsSuccess)
            {
                tabItem.Header = inputVM.InputValue;
                _vm.UpdateBuildingName(tabName, inputVM.InputValue);
                ClearCopySourceTabName();
                _vm.Save(); // 保存数据
            }
        }
    }
}
