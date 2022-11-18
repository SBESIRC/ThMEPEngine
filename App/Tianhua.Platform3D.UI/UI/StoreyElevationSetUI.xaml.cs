using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using Tianhua.Platform3D.UI.Interfaces;
using TianHua.Platform3D.UI.ViewModels;
using Autodesk.AutoCAD.ApplicationServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThPlatform3D;

namespace Tianhua.Platform3D.UI.UI
{
    /// <summary>
    /// StoreyTableEntrance.xaml 的交互逻辑
    /// </summary>
    public partial class StoreyElevationSetUI : UserControl,IMultiDocument
    {
        
        private static StoreyElevationSetVM _vm; // 当前文档对应的VM
        private static Dictionary<string, StoreyElevationSetVM> _documentVMDic;
        private Document ActiveDoc
        {
            get => acadApp.Application.DocumentManager.MdiActiveDocument;
        }
        public StoreyElevationSetUI()
        {
            InitializeComponent();
            this.Loaded += StoreyElevationSetUI_Loaded;
        }

        private void StoreyElevationSetUI_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }

        static StoreyElevationSetUI()
        {
            _documentVMDic = new Dictionary<string, StoreyElevationSetVM>();
        }

        public void MainUIShowInDocument()
        {
            Load();
        }

        public void DocumentActivated(DocumentCollectionEventArgs e)
        {
            Load();            
        }

        public void DocumentDestroyed(DocumentDestroyedEventArgs e)
        {
            //
        }

        public void DocumentToBeActivated(DocumentCollectionEventArgs e)
        {
            //
        }

        public void DocumentToBeDestroyed(DocumentCollectionEventArgs e)
        {
            RemoveFromDocumentVMDic(e.Document.Name);
        }

        private void Load()
        {
            if (ActiveDoc != null)
            {
                _vm = GetFromDocumentVMDic(ActiveDoc.Name);
                if (_vm == null)
                {
                    _vm = new StoreyElevationSetVM();
                    AddToDocumentVMDic(ActiveDoc.Name, _vm);
                }
                _vm.CopySourceTabName = "";                
                this.DataContext = _vm;
                LoadTabcontrolItems();
                SetTabControlFocus(_vm.ActiveTabName);
            }
        }

        private void tabStorey_SelectionChanging(object sender,MouseButtonEventArgs e)
        {
            UpdateTabStoreyActiveTabName();
        }

        private void LoadTabcontrolItems()
        {
            this.tabStorey.Items.Clear();
            if (_vm.BuildingNames.Count>0)
            {
                _vm.BuildingNames.ForEach(o => AddTabItem(o));
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
                _vm.CopySourceTabName = "";
                _vm.Save(); // 保存数据
            }
        }

        private void UpdateTabStoreyActiveTabName()
        {
            if(this.tabStorey.SelectedItem!=null)
            {
                var tabItem = this.tabStorey.SelectedItem as TabItem;
                _vm.ActiveTabName = tabItem.Name;
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
            this.tabStorey.Items.Add(tabItem);
            tabItem.Focus();
            tabItem.MouseLeftButtonDown += tabStorey_SelectionChanging;
        }

        private void copyItem_Click(object sender, RoutedEventArgs e)
        {
            if(this.tabStorey.SelectedItem==null)
            {
                MessageBox.Show("请选择要复制的Tab项","信息提示",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            _vm.CopySourceTabName = (this.tabStorey.SelectedItem as TabItem).Header.ToString();            
        }

        private void pasteItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_vm.CopySourceTabName))
            {
                MessageBox.Show("请选择要复制的源Tab项！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this.tabStorey.SelectedItem == null)
            {
                MessageBox.Show("请选择要粘贴的目标Tab项！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var targetTab = this.tabStorey.SelectedItem as TabItem;
            var copyTargetTabName = targetTab.Header.ToString();
            if (_vm.CopySourceTabName == copyTargetTabName)
            {
                MessageBox.Show("复制和粘贴的Tab项不能相同！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var tipRes = MessageBox.Show("确定把 "+ _vm.CopySourceTabName + " 中的楼层数据复制到 "+ copyTargetTabName+" 中吗？",
                "复制提示",MessageBoxButton.OKCancel,MessageBoxImage.Warning);
            if(tipRes == MessageBoxResult.OK)
            {
                _vm.CopyBuildingStoreys(copyTargetTabName);
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
            if (this.tabStorey.SelectedItem == null)
            {
                return;
            }
            var tabItem = this.tabStorey.SelectedItem as TabItem;
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
            _vm.CopySourceTabName = "";
        }

        private void SetTabControlFocus(string head)
        {
            if (!string.IsNullOrEmpty(head))
            {
                foreach (TabItem item in this.tabStorey.Items)
                {
                    if (item.Header.ToString() == head)
                    {
                        item.Focus();
                    }
                }
            }
        }

        private void deleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.tabStorey.SelectedItem == null)
            {
                return;
            }
            var tabItem = this.tabStorey.SelectedItem as TabItem;
            var tipRes = MessageBox.Show("确定要删除 [" + tabItem.Header.ToString() + "] 楼层数据吗？", "删除提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (tipRes == MessageBoxResult.OK)
            {
                this.tabStorey.Items.Remove(tabItem);
                _vm.DeleteBuilding(tabItem.Header.ToString());
                _vm.CopySourceTabName = "";
                _vm.Save(); // 保存数据
            }
        }

        private void modifyItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.tabStorey.SelectedItem == null)
            {
                return;
            }
            var tabItem = this.tabStorey.SelectedItem as TabItem;
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
                _vm.CopySourceTabName = "";
                _vm.Save(); // 保存数据
            }
        }

        private void AddToDocumentVMDic(string docName, StoreyElevationSetVM vm)
        {
            if (string.IsNullOrEmpty(docName) || vm == null)
            {
                return;
            }
            if (_documentVMDic.ContainsKey(docName))
            {
                _documentVMDic[docName] = vm;
            }
            else
            {
                _documentVMDic.Add(docName, vm);
            }
        }
        private void RemoveFromDocumentVMDic(string docName)
        {
            if (!string.IsNullOrEmpty(docName) && _documentVMDic.ContainsKey(docName))
            {
                _documentVMDic.Remove(docName);
            }
        }
        private StoreyElevationSetVM GetFromDocumentVMDic(string docName)
        {
            if (!string.IsNullOrEmpty(docName) && _documentVMDic.ContainsKey(docName))
            {
                return _documentVMDic[docName];
            }
            else
            {
                return null;
            }
        }

        private void tabStorey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var buildinName = "";
            if (this.tabStorey.SelectedItem != null)
            {
                buildinName = this.tabStorey.SelectedItem.ToString();
            }
            ConfigService.ConfigInstance.BindingBuildingName(buildinName);
        }
    }
}
