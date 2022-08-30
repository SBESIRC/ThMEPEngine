using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThCADExtension;
using ThControlLibraryWPF.CustomControl;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using TianHua.Platform3D.UI.Model;
using TianHua.Platform3D.UI.ViewModels;

namespace Tianhua.Platform3D.UI.UI
{
    public partial class EditStoreyUI : ThCustomWindow
    {
        private EditStoreyVM _vm;
        public EditStoreyUI(EditStoreyVM vm)
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.
                WindowStartupLocation.CenterScreen;
            this._vm = vm;
            this.DataContext = this._vm;
            this.Topmost = true;
        }

        private void btnInitStorey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // 这个流程暂时不用
            var initVM = new InitStoreyVM();
            var initUI = new InitStoreyUI(initVM);
            initUI.Owner = this;
            initUI.ShowDialog();
            if (initUI.IsSuccess)
            {
                initVM.Init();
                _vm.InitSource(initVM.Storeys);
                UpdateDataGridDataSource();
            }
        }

        private void UpdateDataGridDataSource()
        {
            this.datagrid1.ItemsSource = null;
            this.datagrid1.ItemsSource = _vm.Source;
        }

        private void btnLoadStorey_Click(object sender, RoutedEventArgs e)
        {
            var opts = new PromptOpenFileOptions("\n选择要单体楼层信息文件")
            {
                Filter = "单体楼层信息文件(*.storeys.txt) (*.txt)|*.txt",
            };

            var pfnr =  Active.Editor.GetFileNameForOpen(opts);
            if (pfnr.Status == PromptStatus.OK)
            {
                var storeyFileName = pfnr.StringResult;
                var storeyInfos = ThParseStoreyService.ParseFromTxt(storeyFileName);
                if(storeyInfos.Count>0)
                {
                    var editStoreyInfos = storeyInfos.Select(o =>
                    {
                        return new ThEditStoreyInfo()
                        {
                            StoreyName = o.StoreyName,
                            Elevation = o.Elevation,
                            Top_Elevation = o.Top_Elevation,
                            Bottom_Elevation = o.Bottom_Elevation,
                            Description = o.Description,
                            FloorNo = o.FloorNo,
                            Height = o.Height,
                            StdFlrNo = o.StdFlrNo,
                        };
                    }).ToList();

                    _vm.InitSource(editStoreyInfos);
                    UpdateDataGridDataSource();
                }
            }
            else
            {
                return;
            }
        }

        private void btnInsertStorey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(this.datagrid1.SelectedItems.Count ==0)
            {
                MessageBox.Show("请选择要插入的楼层位置！","信息提示",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            var selFirstStorey = this.datagrid1.SelectedItems[0] as ThEditStoreyInfo;
            var insertStoreyVM = new InsertStoreyVM(_vm.BelowStoreys,_vm.UpperStoreys,_vm.RoofStoreys,selFirstStorey);
            var insertStoreyUI = new InsertStoreyUI(insertStoreyVM);
            insertStoreyUI.Owner = this;
            insertStoreyUI.ShowDialog();
            if(insertStoreyUI.IsSuccess)
            {
                var insertedStoryes = insertStoreyVM.BuildFinalStoreys();
                if(insertedStoryes.Count>0)
                {
                    _vm.UpdateSource(insertedStoryes);
                    _vm.CalculateBuildingStoreyElevation();
                    UpdateDataGridDataSource();
                }
            }
        }

        private void btnAdjustStoreyHeight_Click(object sender, System.Windows.RoutedEventArgs e)
        {            
            if(this.datagrid1.SelectedItems.Count == 0)
            {
                return;
            }
            var inputValue = "";
            if (this.datagrid1.SelectedItems.Count==1)
            {
                var storeyInfo = this.datagrid1.SelectedItems[0] as ThIfcStoreyInfo;
                inputValue = storeyInfo.Height.ToString();
            }
            var textInputVM = new TextInputVM(new List<string>())
            {
                InputTip = "请输入层高（毫米）",
                InputValue = inputValue,
            };
            var textInputUI = new TextInputUI(textInputVM);
            textInputUI.Owner = this;
            textInputUI.ShowDialog();
            if(textInputUI.IsSuccess)
            {
                var newInputValue = textInputVM.InputValue.Trim();
                if (string.IsNullOrEmpty(newInputValue))
                {
                    MessageBox.Show("输入的层高不能为空！","信息提示",MessageBoxButton.OK,MessageBoxImage.Information);
                    return;
                }
                else if (!ThStringTools.IsPositiveInteger(newInputValue))
                {
                    MessageBox.Show("层高只能输入正整数", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    var height = int.Parse(newInputValue);
                    if(height<=0)
                    {
                        MessageBox.Show("层高只能输入正整数", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        foreach(var item in this.datagrid1.SelectedItems)
                        {
                            var rowIndex = this.datagrid1.Items.IndexOf(item);
                            _vm.UpdateHeight(rowIndex, height.ToString());
                        }
                        _vm.CalculateBuildingStoreyElevation();
                        UpdateDataGridDataSource();
                    }
                }
            }
        }

        private void btnSetFirstStoreyBottomElevation_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(this.datagrid1.Items.Count>0)
            {
                var firstStorey = _vm.FindFirstStorey();
                if(firstStorey == null)
                {
                    return;
                }
                var textInputVM = new TextInputVM(new List<string>())
                {
                    InputTip = "请输入首层标高（毫米）",
                    InputValue = firstStorey.Bottom_Elevation,
                };
                var textInputUI = new TextInputUI(textInputVM);
                textInputUI.Owner = this;
                textInputUI.ShowDialog();
                if(textInputUI.IsSuccess)
                {
                    var newInputValue = textInputVM.InputValue.Trim();
                    if (string.IsNullOrEmpty(newInputValue))
                    {
                        MessageBox.Show("输入的首层标高不能为空！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else if (!ThStringTools.IsDouble(newInputValue))
                    {
                        MessageBox.Show("标高只能输入数值,不能输入其它字符!", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        firstStorey.Bottom_Elevation = newInputValue;
                        _vm.CalculateBuildingStoreyElevation();
                        UpdateDataGridDataSource();
                    }
                }
            }
        }

        private void btnRelatePaper_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(this.datagrid1.SelectedItems.Count==0)
            {
                MessageBox.Show("请选择要关联图纸的楼层项", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var paperNames = new List<string>();
            foreach(var item in this.datagrid1.SelectedItems)
            {
                var storeyInfo = item as ThEditStoreyInfo;
                if(!string.IsNullOrEmpty(storeyInfo.PaperName))
                {
                    foreach(var paperName in storeyInfo.PaperName.Split(','))
                    {
                        if(!paperNames.Contains(paperName) && !string.IsNullOrEmpty(paperName))
                        {
                            paperNames.Add(paperName);
                        }                       
                    }
                }
            }

            var relatePaperVm = new RelatePaperVM();
            paperNames.ForEach(o => relatePaperVm.SetValue(o, true));
            var relatePaperUI = new RelatePaperUI(relatePaperVm);
            relatePaperUI.Owner = this;
            relatePaperUI.ShowDialog();
            if(relatePaperUI.IsSuccess)
            {
                var selectPaperNames = relatePaperVm.GetSelectItemNames();
                var joinPaperName = string.Join(",", selectPaperNames);
                foreach (var item in this.datagrid1.SelectedItems)
                {
                    var rowIndex = this.datagrid1.Items.IndexOf(item);
                    _vm.UpdateRelatePaperName(rowIndex, joinPaperName);
                }
                UpdateDataGridDataSource();
            }
        }

        private void btnDeleteStorey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(this.datagrid1.SelectedItems.Count==0)
            {
                return;
            }
            var storeyNames = new List<string>();
            var storeyIndexes = new List<int>();
            foreach(var item in this.datagrid1.SelectedItems)
            {
                storeyIndexes.Add(this.datagrid1.Items.IndexOf(item));
                storeyNames.Add((item as ThEditStoreyInfo).PaperName);
            }

            string tipMessage = "确定要删除"+string.Join(",",storeyNames)+ "楼层数据吗？";

            var mbr = MessageBox.Show(tipMessage, "删除提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if(mbr == MessageBoxResult.OK)
            {
                // 更新数据和View
                _vm.RemoveStoreyes(storeyIndexes);
                UpdateDataGridDataSource();
            }
        }

        private void btnSetJiaCengHeight_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void btnElecDropHeight_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void btnUpdateStoreyName_Click(object sender, RoutedEventArgs e)
        {
            if(_vm.Source.Count==0)
            {
                MessageBox.Show("当前表中无任何记录，无法重置楼层名！","信息提示",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            var underGroundStoreyNumbers = _vm.BelowStoreys.Count;
            var aboveGroundStoreyNumbers = _vm.UpperStoreys.Count;
            var roofStoreyNumbers = _vm.RoofStoreys.Count;
            var initVM = new InitStoreyVM(underGroundStoreyNumbers, aboveGroundStoreyNumbers, roofStoreyNumbers);
            var initUI = new InitStoreyUI(initVM);
            initUI.Owner = this;
            initUI.ShowDialog();
            if(!initVM.IsEqualToTotalCount)
            {
                MessageBox.Show("重置的楼层数与表中的记录数["+ _vm.Source.Count+ "] 不一致,无法重置！", "信息提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                 initVM.Init();
                _vm.ResetStoreyName(initVM.Storeys);
                UpdateDataGridDataSource();
            }
        }
        [Obsolete("重置记录中的楼层名，暂时不用")]
        private void UnusenessCode1()
        {
            // 修改楼层名Code
            int selectStoreyCount = this.datagrid1.SelectedItems.Count;
            if (selectStoreyCount > 0)
            {
                var storeyIndexes = new List<int>();
                foreach (var item in this.datagrid1.SelectedItems)
                {
                    storeyIndexes.Add(this.datagrid1.Items.IndexOf(item));
                }
                bool isContinuous = true;
                for (int i = 1; i < storeyIndexes.Count; i++)
                {
                    if ((storeyIndexes[i] - storeyIndexes[i - 1]) != 1)
                    {
                        isContinuous = false;
                        break;
                    }
                }
                if (isContinuous)
                {
                    // 选择起始楼层
                    var firstStoreyInfo = this.datagrid1.SelectedItems[0] as ThEditStoreyInfo;
                    var storeyNameVM = new StoreyNameInputVM(firstStoreyInfo.StoreyName, selectStoreyCount);
                    var storeyNameUI = new StoreyNameInputUI(storeyNameVM);
                    storeyNameUI.ShowDialog();
                    if (storeyNameUI.IsSuccess)
                    {
                        if (storeyNameVM.IsAscending)
                        {
                            int startIndex = storeyNameVM.InputValue;
                            foreach (var item in this.datagrid1.SelectedItems)
                            {
                                var index = this.datagrid1.Items.IndexOf(item);
                                var storeyName = storeyNameVM.Prefix + startIndex.ToString() + "F";
                                _vm.UpdateStoreyName(index, storeyName);
                                startIndex++;
                            }
                        }
                        else
                        {
                            int startIndex = storeyNameVM.InputValue;
                            foreach (var item in this.datagrid1.SelectedItems)
                            {
                                var index = this.datagrid1.Items.IndexOf(item);
                                var storeyName = storeyNameVM.Prefix + startIndex.ToString() + "F";
                                _vm.UpdateStoreyName(index, storeyName);
                                startIndex--;
                            }
                        }
                        UpdateDataGridDataSource();
                    }
                }
            }
        }
    }
}
